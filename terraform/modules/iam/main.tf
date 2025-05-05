resource "google_service_account" "service_account" {
  account_id   = var.account_id
  display_name = var.display_name
  project      = var.project_id
  description  = var.description
}

resource "google_project_iam_member" "project_iam_bindings" {
  for_each = toset(var.project_roles)
  
  project = var.project_id
  role    = each.key
  member  = "serviceAccount:${google_service_account.service_account.email}"
}

resource "google_service_account_key" "key" {
  count = var.create_key ? 1 : 0
  
  service_account_id = google_service_account.service_account.name
  key_algorithm      = "KEY_ALG_RSA_2048"
}

resource "google_service_account_iam_binding" "workload_identity_binding" {
  count = var.enable_workload_identity ? 1 : 0
  
  service_account_id = google_service_account.service_account.name
  role               = "roles/iam.workloadIdentityUser"
  members            = [
    "serviceAccount:${var.project_id}.svc.id.goog[${var.kubernetes_namespace}/${var.kubernetes_service_account}]"
  ]
}

resource "google_secret_manager_secret_iam_member" "secret_iam" {
  for_each = toset(var.secret_ids)
  
  project   = var.project_id
  secret_id = each.value
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.service_account.email}"
}

resource "google_storage_bucket_iam_member" "storage_iam" {
  for_each = { for binding in var.storage_bucket_bindings : "${binding.bucket}-${binding.role}" => binding }
  
  bucket = each.value.bucket
  role   = each.value.role
  member = "serviceAccount:${google_service_account.service_account.email}"
}