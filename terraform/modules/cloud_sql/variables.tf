variable "project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "region" {
  description = "The GCP region for the instance"
  type        = string
}

variable "instance_name" {
  description = "The name of the database instance"
  type        = string
}

variable "db_name" {
  description = "The name of the database"
  type        = string
}

variable "machine_type" {
  description = "The machine type of the instance"
  type        = string
  default     = "db-f1-micro"
}

variable "high_availability" {
  description = "Whether to enable high availability"
  type        = bool
  default     = false
}

variable "disk_size" {
  description = "The disk size in GB"
  type        = number
  default     = 10
}

variable "disk_type" {
  description = "The disk type (PD_SSD or PD_HDD)"
  type        = string
  default     = "PD_SSD"
}

variable "backup_enabled" {
  description = "Whether to enable backups"
  type        = bool
  default     = true
}

variable "backup_start_time" {
  description = "The start time for backups (HH:MM)"
  type        = string
  default     = "02:00"
}

variable "point_in_time_recovery" {
  description = "Whether to enable point-in-time recovery"
  type        = bool
  default     = true
}

variable "maintenance_window_day" {
  description = "The day of the maintenance window (1-7, 1 is Monday)"
  type        = number
  default     = 7
}

variable "maintenance_window_hour" {
  description = "The hour of the maintenance window (0-23)"
  type        = number
  default     = 3
}

variable "maintenance_window_update_track" {
  description = "The update track of maintenance (canary or stable)"
  type        = string
  default     = "stable"
}

variable "public_ip" {
  description = "Whether to assign a public IP"
  type        = bool
  default     = false
}

variable "private_network" {
  description = "The private network for the instance (self_link)"
  type        = string
  default     = ""
}

variable "authorized_networks" {
  description = "Map of authorized networks (name => CIDR)"
  type        = map(string)
  default     = {}
}

variable "max_connections" {
  description = "Maximum number of connections"
  type        = number
  default     = 100
}

variable "log_min_duration" {
  description = "Minimum query duration to log (ms)"
  type        = number
  default     = 300
}

variable "labels" {
  description = "Labels to apply to the instance"
  type        = map(string)
  default     = {}
}

variable "deletion_protection" {
  description = "Whether to enable deletion protection"
  type        = bool
  default     = true
}

variable "database_users" {
  description = "Map of database users to create"
  type        = map(object({
    password = string
  }))
  default     = {}
  sensitive   = true
}