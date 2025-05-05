resource "google_compute_security_policy" "policy" {
  name        = var.policy_name
  description = var.policy_description
  project     = var.project_id
  
  # Default rule (deny all)
  rule {
    action   = "deny(403)"
    priority = 2147483647
    match {
      versioned_expr = "SRC_IPS_V1"
      config {
        src_ip_ranges = ["*"]
      }
    }
    description = "Default deny rule"
  }
  
  # Allow specific IPs
  dynamic "rule" {
    for_each = var.allowed_ip_ranges
    content {
      action   = "allow"
      priority = 1000 + index(var.allowed_ip_ranges, rule.value)
      match {
        versioned_expr = "SRC_IPS_V1"
        config {
          src_ip_ranges = [rule.value]
        }
      }
      description = "Allow specific IP range: ${rule.value}"
    }
  }
  
  # OWASP Top 10 protection rules
  
  # SQL injection protection
  rule {
    action   = "deny(403)"
    priority = 100
    match {
      expr {
        expression = "evaluatePreconfiguredExpr('sqli-stable')"
      }
    }
    description = "SQL injection protection"
  }
  
  # XSS protection
  rule {
    action   = "deny(403)"
    priority = 101
    match {
      expr {
        expression = "evaluatePreconfiguredExpr('xss-stable')"
      }
    }
    description = "Cross-site scripting protection"
  }
  
  # Local file inclusion protection
  rule {
    action   = "deny(403)"
    priority = 102
    match {
      expr {
        expression = "evaluatePreconfiguredExpr('lfi-stable')"
      }
    }
    description = "Local file inclusion protection"
  }
  
  # Remote file inclusion protection
  rule {
    action   = "deny(403)"
    priority = 103
    match {
      expr {
        expression = "evaluatePreconfiguredExpr('rfi-stable')"
      }
    }
    description = "Remote file inclusion protection"
  }
  
  # Rate limiting for API endpoints
  dynamic "rule" {
    for_each = var.rate_limiting_rules
    content {
      action   = "throttle"
      priority = 500 + index(keys(var.rate_limiting_rules), rule.key)
      match {
        expr {
          expression = rule.value.expression
        }
      }
      description = "Rate limiting for ${rule.key}"
      rate_limit_options {
        conform_action = "allow"
        exceed_action  = "deny(429)"
        enforce_on_key = rule.value.enforce_on_key
        rate_limit_threshold {
          count        = rule.value.threshold_count
          interval_sec = rule.value.interval_sec
        }
      }
    }
  }
  
  # Custom rules
  dynamic "rule" {
    for_each = var.custom_rules
    content {
      action   = rule.value.action
      priority = 1000 + index(keys(var.custom_rules), rule.key)
      match {
        expr {
          expression = rule.value.expression
        }
      }
      description = rule.value.description
    }
  }
  
  # Geo-restriction rules
  dynamic "rule" {
    for_each = length(var.blocked_countries) > 0 ? [1] : []
    content {
      action   = "deny(403)"
      priority = 200
      match {
        expr {
          expression = "origin.region_code in [${join(", ", [for country in var.blocked_countries : "'${country}'"])}]"
        }
      }
      description = "Block traffic from specific countries"
    }
  }
  
  depends_on = [
    google_project_service.compute_api
  ]
}

resource "google_project_service" "compute_api" {
  service            = "compute.googleapis.com"
  disable_on_destroy = false
}