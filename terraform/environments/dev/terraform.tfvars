project_id       = "deployment-portal-dev"
region           = "us-central1"
zone             = "us-central1-a"
project_prefix   = "deployment-portal-dev"
domain_name      = "dev.deployment-portal.example.com"

container_registry = "gcr.io"
backend_image_tag  = "0.1.0"
frontend_image_tag = "0.1.0"

# These should be set from environment variables or a secure source
# db_password      = "changeme"
# api_key          = "changeme"
# jwt_secret       = "changeme"

allowed_ip_ranges = [
  "192.168.1.0/24",  # Office network
  "203.0.113.0/24"   # VPN network
]

blocked_countries = [
  "RU",  # Russia
  "CN",  # China
  "IR",  # Iran
  "KP"   # North Korea
]