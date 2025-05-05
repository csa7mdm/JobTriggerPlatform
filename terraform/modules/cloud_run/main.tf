resource "google_cloud_run_service" "service" {
  name     = var.service_name
  location = var.region
  
  template {
    spec {
      containers {
        image = var.container_image
        
        resources {
          limits = {
            cpu    = var.cpu
            memory = var.memory
          }
        }
        
        dynamic "env" {
          for_each = var.environment_variables
          content {
            name  = env.key
            value = env.value
          }
        }
        
        dynamic "env" {
          for_each = var.secret_environment_variables
          content {
            name = env.key
            value_from {
              secret_key_ref {
                name = env.value.secret_name
                key  = env.value.secret_key
              }
            }
          }
        }
      }
      
      service_account_name = var.service_account_email
      container_concurrency = var.container_concurrency
      timeout_seconds = var.timeout_seconds
    }
    
    metadata {
      annotations = {
        "autoscaling.knative.dev/minScale" = var.min_instances
        "autoscaling.knative.dev/maxScale" = var.max_instances
      }
    }
  }
  
  traffic {
    percent         = 100
    latest_revision = true
  }

  autogenerate_revision_name = true

  depends_on = [
    google_project_service.cloud_run_api
  ]
}

resource "google_project_service" "cloud_run_api" {
  service            = "run.googleapis.com"
  disable_on_destroy = false
}

resource "google_cloud_run_service_iam_member" "public" {
  count    = var.allow_public_access ? 1 : 0
  location = google_cloud_run_service.service.location
  service  = google_cloud_run_service.service.name
  role     = "roles/run.invoker"
  member   = "allUsers"
}

# Additional IAM bindings for specific principals
resource "google_cloud_run_service_iam_member" "invoker" {
  for_each = toset(var.invoker_members)
  
  location = google_cloud_run_service.service.location
  service  = google_cloud_run_service.service.name
  role     = "roles/run.invoker"
  member   = each.key
}

# VPC Connector for private networking
resource "google_vpc_access_connector" "connector" {
  count       = var.vpc_connector_name != "" ? 1 : 0
  name        = var.vpc_connector_name
  region      = var.region
  network     = var.vpc_network
  ip_cidr_range = var.vpc_connector_cidr
  max_throughput = var.vpc_connector_throughput
}