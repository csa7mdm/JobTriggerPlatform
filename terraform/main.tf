variable "environment" {
  description = "Environment to deploy (dev or prod)"
  type        = string
  default     = "dev"
  
  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "Environment must be either 'dev' or 'prod'."
  }
}

module "deployment_portal" {
  source = "./environments/${var.environment}"
  
  # Pass variables through to the environment module
  project_id          = var.project_id
  region              = var.region
  zone                = var.zone
  domain_name         = var.domain_name
  container_registry  = var.container_registry
  backend_image_tag   = var.backend_image_tag
  frontend_image_tag  = var.frontend_image_tag
  db_password         = var.db_password
  api_key             = var.api_key
  jwt_secret          = var.jwt_secret
  allowed_ip_ranges   = var.allowed_ip_ranges
  blocked_countries   = var.blocked_countries
}

variable "project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "region" {
  description = "The GCP region for resources"
  type        = string
  default     = "us-central1"
}

variable "zone" {
  description = "The GCP zone for resources"
  type        = string
  default     = "us-central1-a"
}

variable "domain_name" {
  description = "Domain name for the application"
  type        = string
}

variable "container_registry" {
  description = "Container registry URL"
  type        = string
  default     = "gcr.io"
}

variable "backend_image_tag" {
  description = "Tag for the backend image"
  type        = string
}

variable "frontend_image_tag" {
  description = "Tag for the frontend image"
  type        = string
}

variable "db_password" {
  description = "Password for the database"
  type        = string
  sensitive   = true
}

variable "api_key" {
  description = "API key for external services"
  type        = string
  sensitive   = true
}

variable "jwt_secret" {
  description = "Secret for JWT token signing"
  type        = string
  sensitive   = true
}

variable "allowed_ip_ranges" {
  description = "List of IP ranges to allow"
  type        = list(string)
  default     = []
}

variable "blocked_countries" {
  description = "List of countries to block"
  type        = list(string)
  default     = []
}

output "frontend_url" {
  description = "URL of the frontend service"
  value       = module.deployment_portal.frontend_url
}

output "backend_url" {
  description = "URL of the backend service"
  value       = module.deployment_portal.backend_url
}