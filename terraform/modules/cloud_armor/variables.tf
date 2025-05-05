variable "project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "policy_name" {
  description = "Name of the Cloud Armor security policy"
  type        = string
}

variable "policy_description" {
  description = "Description of the Cloud Armor security policy"
  type        = string
  default     = "Security policy for WAF protection"
}

variable "allowed_ip_ranges" {
  description = "List of IP ranges to allow"
  type        = list(string)
  default     = []
}

variable "blocked_countries" {
  description = "List of countries to block (using ISO 3166-1 alpha-2 country codes)"
  type        = list(string)
  default     = []
}

variable "rate_limiting_rules" {
  description = "Map of rate limiting rules"
  type        = map(object({
    expression      = string
    enforce_on_key  = string
    threshold_count = number
    interval_sec    = number
  }))
  default     = {}
}

variable "custom_rules" {
  description = "Map of custom rules"
  type        = map(object({
    action      = string
    expression  = string
    description = string
  }))
  default     = {}
}