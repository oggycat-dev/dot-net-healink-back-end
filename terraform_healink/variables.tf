# File này dùng để khai báo tất cả các biến mà Terraform sẽ sử dụng.
# Các giá trị thực tế sẽ được cung cấp qua file .tfvars (cho local) 
# hoặc qua GitHub Secrets (cho CI/CD).

# --- JWT Configuration ---
variable "jwt_secret_key" {
  type        = string
  description = "The secret key used for signing JWT tokens."
  sensitive   = true # Đánh dấu là biến nhạy cảm, sẽ bị ẩn trong log
}

variable "jwt_issuer" {
  type        = string
  description = "The issuer for JWT tokens."
}

variable "jwt_audience" {
  type        = string
  description = "The audience for JWT tokens."
}

variable "jwt_expire_minutes" {
  type        = number
  description = "The expiration time for JWT tokens in minutes."
  default     = 60
}

# --- Redis Configuration ---
variable "redis_connection_string" {
  type        = string
  description = "The full connection string for the Redis instance."
  sensitive   = true
}

# --- Admin Account Configuration ---
variable "admin_email" {
  type        = string
  description = "Default admin email for data seeding."
}

variable "admin_password" {
  type        = string
  description = "Default admin password for data seeding."
  sensitive   = true
}

# --- CORS Configuration ---
variable "allowed_origins" {
  type        = string
  description = "Comma-separated list of allowed origins for CORS."
}

# --- Biến cho CI/CD ---
# Biến này đã có, dùng để truyền image tag từ pipeline
variable "app_image_tag" {
  type        = string
  description = "The Docker image tag to deploy."
}

