output "service_account_email" {
  description = "The email of the service account"
  value       = google_service_account.service_account.email
}

output "service_account_id" {
  description = "The ID of the service account"
  value       = google_service_account.service_account.id
}

output "service_account_name" {
  description = "The fully-qualified name of the service account"
  value       = google_service_account.service_account.name
}

output "key" {
  description = "The service account key (if created)"
  value       = var.create_key ? google_service_account_key.key[0].private_key : null
  sensitive   = true
}