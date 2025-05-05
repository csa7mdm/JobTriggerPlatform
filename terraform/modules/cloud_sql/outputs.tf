output "instance_name" {
  description = "The name of the database instance"
  value       = google_sql_database_instance.postgres.name
}

output "instance_connection_name" {
  description = "The connection name of the instance"
  value       = google_sql_database_instance.postgres.connection_name
}

output "database_name" {
  description = "The name of the database"
  value       = google_sql_database.database.name
}

output "public_ip_address" {
  description = "The public IP address of the instance"
  value       = google_sql_database_instance.postgres.public_ip_address
}

output "private_ip_address" {
  description = "The private IP address of the instance"
  value       = google_sql_database_instance.postgres.private_ip_address
}