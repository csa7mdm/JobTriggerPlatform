variable "project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "account_id" {
  description = "The service account ID"
  type        = string
}

variable "display_name" {
  description = "The display name for the service account"
  type        = string
}

variable "description" {
  description = "The description for the service account"
  type        = string
  default     = ""
}

variable "project_roles" {
  description = "List of project roles to assign to the service account"
  type        = list(string)
  default     = []
}

variable "create_key" {
  description = "Whether to create a service account key"
  type        = bool
  default     = false
}

variable "enable_workload_identity" {
  description = "Whether to enable Workload Identity for this service account"
  type        = bool
  default     = false
}

variable "kubernetes_namespace" {
  description = "Kubernetes namespace for Workload Identity"
  type        = string
  default     = "default"
}

variable "kubernetes_service_account" {
  description = "Kubernetes service account for Workload Identity"
  type        = string
  default     = "default"
}

variable "secret_ids" {
  description = "List of secret IDs to grant access to"
  type        = list(string)
  default     = []
}

variable "storage_bucket_bindings" {
  description = "List of storage bucket bindings"
  type        = list(object({
    bucket = string
    role   = string
  }))
  default     = []
}