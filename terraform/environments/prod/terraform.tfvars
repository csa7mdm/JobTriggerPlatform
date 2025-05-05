project_id       = "deployment-portal-prod"
region           = "us-central1"
zone             = "us-central1-a"
project_prefix   = "deployment-portal-prod"
domain_name      = "deployment-portal.example.com"

container_registry = "gcr.io"
backend_image_tag  = "1.0.0"   # Production uses stable versions
frontend_image_tag = "1.0.0"   # Production uses stable versions

# These should be set from environment variables or a secure source
# db_password      = "changeme"
# api_key          = "changeme"
# jwt_secret       = "changeme"

allowed_ip_ranges = [
  "203.0.113.0/24"   # VPN network
]

blocked_countries = [
  "RU",  # Russia
  "CN",  # China
  "IR",  # Iran
  "KP",  # North Korea
  "BY"   # Belarus
]