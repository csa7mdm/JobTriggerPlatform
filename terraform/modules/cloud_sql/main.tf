resource "google_sql_database_instance" "postgres" {
  name             = var.instance_name
  database_version = "POSTGRES_14"
  region           = var.region
  project          = var.project_id
  
  settings {
    tier              = var.machine_type
    availability_type = var.high_availability ? "REGIONAL" : "ZONAL"
    disk_size         = var.disk_size
    disk_type         = var.disk_type
    
    backup_configuration {
      enabled            = var.backup_enabled
      start_time         = var.backup_start_time
      point_in_time_recovery_enabled = var.point_in_time_recovery
    }
    
    maintenance_window {
      day          = var.maintenance_window_day
      hour         = var.maintenance_window_hour
      update_track = var.maintenance_window_update_track
    }
    
    ip_configuration {
      ipv4_enabled    = var.public_ip
      private_network = var.private_network
      
      dynamic "authorized_networks" {
        for_each = var.authorized_networks
        content {
          name  = authorized_networks.key
          value = authorized_networks.value
        }
      }
    }
    
    database_flags {
      name  = "max_connections"
      value = var.max_connections
    }
    
    database_flags {
      name  = "log_min_duration_statement"
      value = var.log_min_duration
    }
    
    user_labels = var.labels
  }
  
  deletion_protection = var.deletion_protection
  
  depends_on = [
    google_project_service.sql_admin_api
  ]
}

resource "google_sql_database" "database" {
  name     = var.db_name
  instance = google_sql_database_instance.postgres.name
  project  = var.project_id
}

resource "google_sql_user" "users" {
  for_each = var.database_users
  
  name     = each.key
  instance = google_sql_database_instance.postgres.name
  password = each.value.password
  project  = var.project_id
}

resource "google_project_service" "sql_admin_api" {
  service            = "sqladmin.googleapis.com"
  disable_on_destroy = false
}

# Private Service Access for private IP
resource "google_compute_global_address" "private_ip_address" {
  count = var.private_network != "" ? 1 : 0
  
  name          = "${var.instance_name}-private-ip"
  purpose       = "VPC_PEERING"
  address_type  = "INTERNAL"
  prefix_length = 16
  network       = var.private_network
  project       = var.project_id
}

resource "google_service_networking_connection" "private_vpc_connection" {
  count = var.private_network != "" ? 1 : 0
  
  network                 = var.private_network
  service                 = "servicenetworking.googleapis.com"
  reserved_peering_ranges = [google_compute_global_address.private_ip_address[0].name]
  
  depends_on = [
    google_project_service.service_networking_api
  ]
}

resource "google_project_service" "service_networking_api" {
  service            = "servicenetworking.googleapis.com"
  disable_on_destroy = false
}