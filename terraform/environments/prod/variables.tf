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

variable "project_prefix" {
  description = "Prefix for resource names"
  type        = string
  default     = "deployment-portal-prod"
}

variable "domain_name" {
  description = "Domain name for the application"
  type        = string
  default     = "deployment-portal.example.com"
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