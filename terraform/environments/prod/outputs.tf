output "frontend_url" {
  description = "URL of the frontend service"
  value       = module.frontend_service.service_url
}

output "backend_url" {
  description = "URL of the backend service"
  value       = module.backend_service.service_url
}

output "database_name" {
  description = "Name of the database instance"
  value       = module.database.instance_name
}

output "database_connection_name" {
  description = "Connection name of the database instance"
  value       = module.database.instance_connection_name
}

output "load_balancer_ip" {
  description = "IP address of the load balancer"
  value       = google_compute_global_forwarding_rule.default.ip_address
}

output "certificate_name" {
  description = "Name of the SSL certificate"
  value       = google_compute_managed_ssl_certificate.default.name
}