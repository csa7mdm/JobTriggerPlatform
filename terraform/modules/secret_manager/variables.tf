variable "project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "secrets" {
  description = "Map of secret names to secret values"
  type        = map(string)
  sensitive   = true
}

variable "labels" {
  description = "Labels to apply to the secrets"
  type        = map(string)
  default     = {}
}

variable "secret_accessors" {
  description = "Map of secret IDs to roles and members that need access"
  type        = map(map(list(string)))
  default     = {}
}