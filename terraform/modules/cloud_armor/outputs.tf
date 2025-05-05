output "policy_id" {
  description = "The ID of the security policy"
  value       = google_compute_security_policy.policy.id
}

output "policy_name" {
  description = "The name of the security policy"
  value       = google_compute_security_policy.policy.name
}

output "policy_fingerprint" {
  description = "The fingerprint of the security policy"
  value       = google_compute_security_policy.policy.fingerprint
}