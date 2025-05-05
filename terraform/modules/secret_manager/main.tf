resource "google_secret_manager_secret" "secret" {
  for_each = var.secrets
  
  secret_id = each.key
  project   = var.project_id
  
  replication {
    automatic = true
  }
  
  labels = var.labels
}

resource "google_secret_manager_secret_version" "version" {
  for_each = var.secrets
  
  secret      = google_secret_manager_secret.secret[each.key].id
  secret_data = each.value
}

resource "google_secret_manager_secret_iam_member" "secret_access" {
  for_each = {
    for i in flatten([
      for secret_id, roles in var.secret_accessors : [
        for role, members in roles : [
          for member in members : {
            secret_id = secret_id
            role      = role
            member    = member
          }
        ]
      ]
    ]) : "${i.secret_id}-${i.role}-${i.member}" => i
  }
  
  project   = var.project_id
  secret_id = each.value.secret_id
  role      = each.value.role
  member    = each.value.member
  
  depends_on = [
    google_secret_manager_secret.secret
  ]
}

resource "google_project_service" "secret_manager_api" {
  service            = "secretmanager.googleapis.com"
  disable_on_destroy = false
}