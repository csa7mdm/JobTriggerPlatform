provider "google" {
  project = var.project_id
  region  = var.region
  zone    = var.zone
}

provider "google-beta" {
  project = var.project_id
  region  = var.region
  zone    = var.zone
}

# Create a VPC network
resource "google_compute_network" "vpc" {
  name                    = "${var.project_prefix}-network"
  auto_create_subnetworks = false
}

# Create a subnet for the services
resource "google_compute_subnetwork" "subnet" {
  name          = "${var.project_prefix}-subnet"
  ip_cidr_range = "10.0.0.0/20"
  region        = var.region
  network       = google_compute_network.vpc.self_link
}

# Create service accounts with least privilege
module "backend_service_account" {
  source = "../../modules/iam"
  
  project_id   = var.project_id
  account_id   = "${var.project_prefix}-backend-sa"
  display_name = "Backend Service Account"
  description  = "Service account for the backend Cloud Run service"
  
  project_roles = [
    "roles/cloudsql.client",
    "roles/logging.logWriter",
    "roles/monitoring.metricWriter",
    "roles/secretmanager.secretAccessor",
  ]
  
  secret_ids = [
    module.secrets.secret_ids["DB_PASSWORD"],
    module.secrets.secret_ids["API_KEY"]
  ]
  
  enable_workload_identity = true
  kubernetes_namespace    = "default"
  kubernetes_service_account = "backend-sa"
}

module "frontend_service_account" {
  source = "../../modules/iam"
  
  project_id   = var.project_id
  account_id   = "${var.project_prefix}-frontend-sa"
  display_name = "Frontend Service Account"
  description  = "Service account for the frontend Cloud Run service"
  
  project_roles = [
    "roles/logging.logWriter",
    "roles/monitoring.metricWriter",
  ]
  
  enable_workload_identity = true
  kubernetes_namespace    = "default"
  kubernetes_service_account = "frontend-sa"
}

# Create secrets for the application
module "secrets" {
  source = "../../modules/secret_manager"
  
  project_id = var.project_id
  
  secrets = {
    "DB_PASSWORD" = var.db_password,
    "API_KEY"     = var.api_key,
    "JWT_SECRET"  = var.jwt_secret
  }
  
  secret_accessors = {
    "DB_PASSWORD" = {
      "roles/secretmanager.secretAccessor" = [
        "serviceAccount:${module.backend_service_account.service_account_email}"
      ]
    },
    "API_KEY" = {
      "roles/secretmanager.secretAccessor" = [
        "serviceAccount:${module.backend_service_account.service_account_email}"
      ]
    }
  }
}

# Create PostgreSQL database
module "database" {
  source = "../../modules/cloud_sql"
  
  project_id    = var.project_id
  region        = var.region
  instance_name = "${var.project_prefix}-postgres"
  db_name       = "deploymentportal"
  
  machine_type      = "db-g1-small"
  high_availability = false
  disk_size         = 20
  
  backup_enabled          = true
  point_in_time_recovery  = true
  
  public_ip          = false
  private_network    = google_compute_network.vpc.self_link
  
  database_users = {
    "application" = {
      password = var.db_password
    }
  }
  
  labels = {
    environment = "dev"
    managed_by  = "terraform"
  }
}

# Configure Cloud Armor WAF
module "cloud_armor" {
  source = "../../modules/cloud_armor"
  
  project_id        = var.project_id
  policy_name       = "${var.project_prefix}-security-policy"
  policy_description = "Security policy for the deployment portal"
  
  allowed_ip_ranges = var.allowed_ip_ranges
  blocked_countries = var.blocked_countries
  
  rate_limiting_rules = {
    "api-endpoints" = {
      expression      = "request.path.matches('/api/.*')"
      enforce_on_key  = "IP"
      threshold_count = 100
      interval_sec    = 60
    }
  }
  
  custom_rules = {
    "block-admin-access" = {
      action      = "deny(403)"
      expression  = "request.path.matches('/admin/.*') && !request.path.startsWith('/api/')"
      description = "Block direct access to admin endpoints"
    }
  }
}

# Deploy backend service to Cloud Run
module "backend_service" {
  source = "../../modules/cloud_run"
  
  project_id     = var.project_id
  region         = var.region
  service_name   = "${var.project_prefix}-backend"
  container_image = "${var.container_registry}/${var.project_id}/backend:${var.backend_image_tag}"
  
  cpu            = "1000m"
  memory         = "1024Mi"
  min_instances  = 1
  max_instances  = 10
  
  environment_variables = {
    "DB_HOST" = module.database.private_ip_address
    "DB_NAME" = module.database.database_name
    "DB_USER" = "application"
  }
  
  secret_environment_variables = {
    "DB_PASSWORD" = {
      secret_name = "DB_PASSWORD"
      secret_key  = "latest"
    },
    "API_KEY" = {
      secret_name = "API_KEY"
      secret_key  = "latest"
    },
    "JWT_SECRET" = {
      secret_name = "JWT_SECRET"
      secret_key  = "latest"
    }
  }
  
  service_account_email = module.backend_service_account.service_account_email
  
  allow_public_access = false
  invoker_members     = ["serviceAccount:${module.frontend_service_account.service_account_email}"]
  
  vpc_connector_name     = google_vpc_access_connector.connector.name
  vpc_network            = google_compute_network.vpc.self_link
}

# Deploy frontend service to Cloud Run
module "frontend_service" {
  source = "../../modules/cloud_run"
  
  project_id     = var.project_id
  region         = var.region
  service_name   = "${var.project_prefix}-frontend"
  container_image = "${var.container_registry}/${var.project_id}/frontend:${var.frontend_image_tag}"
  
  cpu            = "1000m"
  memory         = "512Mi"
  min_instances  = 1
  max_instances  = 5
  
  environment_variables = {
    "BACKEND_API_URL" = module.backend_service.service_url
  }
  
  service_account_email = module.frontend_service_account.service_account_email
  
  allow_public_access = true
  
  vpc_connector_name     = google_vpc_access_connector.connector.name
  vpc_network            = google_compute_network.vpc.self_link
}

# Create VPC connector for private networking
resource "google_vpc_access_connector" "connector" {
  name          = "${var.project_prefix}-vpc-connector"
  region        = var.region
  ip_cidr_range = "10.8.0.0/28"
  network       = google_compute_network.vpc.self_link
}

# Load balancer with Cloud Armor security policy
resource "google_compute_global_forwarding_rule" "default" {
  name       = "${var.project_prefix}-lb-rule"
  target     = google_compute_target_http_proxy.default.id
  port_range = "80"
}

resource "google_compute_target_http_proxy" "default" {
  name    = "${var.project_prefix}-http-proxy"
  url_map = google_compute_url_map.default.id
}

resource "google_compute_url_map" "default" {
  name            = "${var.project_prefix}-url-map"
  default_service = google_compute_backend_service.frontend.id
  
  host_rule {
    hosts        = ["${var.domain_name}"]
    path_matcher = "allpaths"
  }
  
  path_matcher {
    name            = "allpaths"
    default_service = google_compute_backend_service.frontend.id
    
    path_rule {
      paths   = ["/api/*"]
      service = google_compute_backend_service.backend.id
    }
  }
}

resource "google_compute_backend_service" "frontend" {
  name                  = "${var.project_prefix}-frontend-backend"
  security_policy       = module.cloud_armor.policy_id
  protocol              = "HTTP"
  port_name             = "http"
  timeout_sec           = 30
  load_balancing_scheme = "EXTERNAL"
  
  backend {
    group = google_compute_region_network_endpoint_group.frontend_neg.id
  }
}

resource "google_compute_backend_service" "backend" {
  name                  = "${var.project_prefix}-backend-backend"
  security_policy       = module.cloud_armor.policy_id
  protocol              = "HTTP"
  port_name             = "http"
  timeout_sec           = 30
  load_balancing_scheme = "EXTERNAL"
  
  backend {
    group = google_compute_region_network_endpoint_group.backend_neg.id
  }
}

resource "google_compute_region_network_endpoint_group" "frontend_neg" {
  name                  = "${var.project_prefix}-frontend-neg"
  network_endpoint_type = "SERVERLESS"
  region                = var.region
  cloud_run {
    service = module.frontend_service.service_name
  }
}

resource "google_compute_region_network_endpoint_group" "backend_neg" {
  name                  = "${var.project_prefix}-backend-neg"
  network_endpoint_type = "SERVERLESS"
  region                = var.region
  cloud_run {
    service = module.backend_service.service_name
  }
}