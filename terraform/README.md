# Deployment Portal Infrastructure

This directory contains Terraform configurations for deploying the Deployment Portal application on Google Cloud Platform.

## Architecture

The infrastructure consists of:

- **Cloud Run services** for both frontend and backend components
- **Cloud SQL (PostgreSQL)** for the database
- **Secret Manager** for storing sensitive information
- **Cloud Armor** for web application firewall (WAF) protection
- **IAM** with least privilege principles applied
- **Load Balancer** with SSL termination and URL routing

## Module Structure

```
terraform/
├── environments/
│   ├── dev/        # Development environment configuration
│   └── prod/       # Production environment configuration
├── modules/
│   ├── cloud_run/         # Cloud Run service deployment
│   ├── cloud_sql/         # PostgreSQL database deployment
│   ├── secret_manager/    # Secret management
│   ├── cloud_armor/       # Web Application Firewall
│   └── iam/               # IAM with least privilege
└── main.tf                # Root module
```

## Security Features

- **Private networking** between services
- **Cloud Armor WAF** with OWASP protection, rate limiting, geo-blocking
- **Secret Manager** for all sensitive information
- **IAM with least privilege** for service accounts
- **HTTPS enforcement** with managed SSL certificates

## Usage

### Prerequisites

1. Install Terraform (version >= 1.0.0)
2. Set up Google Cloud SDK and authenticate
3. Enable required APIs in your GCP project:
   - Cloud Run API
   - Cloud SQL Admin API
   - Secret Manager API
   - Compute API
   - Service Networking API

### Deployment

1. Set environment variables for sensitive information:
   ```bash
   export TF_VAR_db_password="your-secure-password"
   export TF_VAR_api_key="your-api-key"
   export TF_VAR_jwt_secret="your-jwt-secret"
   ```

2. Initialize Terraform:
   ```bash
   terraform init
   ```

3. Plan the deployment:
   ```bash
   terraform plan
   ```

4. Apply the changes:
   ```bash
   terraform apply
   ```

5. To deploy to production instead of development:
   ```bash
   terraform apply -var="environment=prod"
   ```

### Customizing the Deployment

Edit the `terraform.tfvars` file to customize the deployment:

- Change `environment` to `"prod"` for production deployment
- Modify `allowed_ip_ranges` and `blocked_countries` for security controls
- Update `domain_name` to your actual domain

## Important Notes

- The first deployment will take some time as it creates all resources
- The database is created with deletion protection enabled by default
- In production, SSL certificates will be provisioned automatically (requires domain ownership verification)
- Sensitive values should never be committed to version control

## Maintenance

- To update the application images, update the image tags in terraform.tfvars
- To add more allowed IP ranges or blocked countries, update the respective variables
- For full environment destruction (including deletion-protected resources):
  ```bash
  terraform apply -var="environment=dev" -var="deletion_protection=false" && terraform destroy -var="environment=dev"
  ```