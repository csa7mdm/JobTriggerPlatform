output "secret_ids" {
  description = "Map of secret names to their IDs"
  value       = { for k, v in google_secret_manager_secret.secret : k => v.id }
}

output "secret_versions" {
  description = "Map of secret names to their current versions"
  value       = { for k, v in google_secret_manager_secret_version.version : k => v.version }
}