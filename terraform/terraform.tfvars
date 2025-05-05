environment        = "dev"   # Change to "prod" for production deployment
project_id         = "deployment-portal-dev"
domain_name        = "dev.deployment-portal.example.com"
backend_image_tag  = "latest"
frontend_image_tag = "latest"

# These sensitive values should be set via environment variables
# db_password      = "changeme"  # Or set with TF_VAR_db_password
# api_key          = "changeme"  # Or set with TF_VAR_api_key  
# jwt_secret       = "changeme"  # Or set with TF_VAR_jwt_secret

allowed_ip_ranges = [
  "192.168.1.0/24"  # Office network
]

blocked_countries = [
  "RU",  # Russia
  "CN"   # China
]