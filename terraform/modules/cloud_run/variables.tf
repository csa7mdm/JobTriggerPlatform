variable "project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "region" {
  description = "The GCP region for the Cloud Run service"
  type        = string
}

variable "service_name" {
  description = "Name of the Cloud Run service"
  type        = string
}

variable "container_image" {
  description = "Container image to deploy"
  type        = string
}

variable "cpu" {
  description = "CPU allocation for the container"
  type        = string
  default     = "1000m"
}

variable "memory" {
  description = "Memory allocation for the container"
  type        = string
  default     = "512Mi"
}

variable "min_instances" {
  description = "Minimum number of instances"
  type        = number
  default     = 0
}

variable "max_instances" {
  description = "Maximum number of instances"
  type        = number
  default     = 10
}

variable "container_concurrency" {
  description = "Maximum number of concurrent requests per container"
  type        = number
  default     = 80
}

variable "timeout_seconds" {
  description = "Maximum request timeout"
  type        = number
  default     = 300
}

variable "environment_variables" {
  description = "Environment variables to set"
  type        = map(string)
  default     = {}
}

variable "secret_environment_variables" {
  description = "Secret environment variables to set"
  type        = map(object({
    secret_name = string
    secret_key  = string
  }))
  default     = {}
}

variable "service_account_email" {
  description = "Service account email for the Cloud Run service"
  type        = string
}

variable "allow_public_access" {
  description = "Whether to allow public access to the service"
  type        = bool
  default     = false
}

variable "invoker_members" {
  description = "List of members that can invoke the service"
  type        = list(string)
  default     = []
}

variable "vpc_connector_name" {
  description = "Name of the VPC connector"
  type        = string
  default     = ""
}

variable "vpc_network" {
  description = "VPC network for the connector"
  type        = string
  default     = "default"
}

variable "vpc_connector_cidr" {
  description = "CIDR range for the VPC connector"
  type        = string
  default     = "10.8.0.0/28"
}

variable "vpc_connector_throughput" {
  description = "Throughput for the VPC connector (in Mbps)"
  type        = number
  default     = 300
}