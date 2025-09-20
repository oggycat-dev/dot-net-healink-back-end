# File này dùng để khai báo tất cả các biến mà Terraform sẽ sử dụng.
# Các giá trị thực tế sẽ được cung cấp qua file .tfvars (cho local) 
# hoặc qua GitHub Secrets (cho CI/CD).

# --- MICROSERVICES CONFIGURATION ---
variable "app_image_tag" {
  type        = string
  description = "The Docker image tag to deploy."
  default     = "latest"
}

variable "auth_service_enabled" {
  type        = bool
  description = "Enable AuthService deployment"
  default     = true
}

variable "product_service_enabled" {
  type        = bool
  description = "Enable ProductService deployment"
  default     = false
}

# --- JWT Configuration ---
variable "jwt_secret_key" {
  type        = string
  description = "The secret key used for signing JWT tokens."
  sensitive   = true # Đánh dấu là biến nhạy cảm, sẽ bị ẩn trong log
  default     = "ProductAuthMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT"
}

variable "jwt_issuer" {
  type        = string
  description = "The issuer for JWT tokens."
  default     = "ProductAuthMicroservice"
}

variable "jwt_audience" {
  type        = string
  description = "The audience for JWT tokens."
  default     = "ProductAuthMicroservice.Users"
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
  default     = "redis://:ProductAuth@123@localhost:6379"
}

# --- Admin Account Configuration ---
variable "admin_email" {
  type        = string
  description = "Default admin email for data seeding."
  default     = "admin@productauth.com"
}

variable "admin_password" {
  type        = string
  description = "Default admin password for data seeding."
  sensitive   = true
  default     = "admin@123"
}

# --- CORS Configuration ---
variable "allowed_origins" {
  type        = string
  description = "Comma-separated list of allowed origins for CORS."
  default     = "https://yourdomain.com,http://localhost:3000"
}

# --- Biến cho CI/CD ---
# Biến này đã có, dùng để truyền image tag từ pipeline
variable "app_image_tag" {
  type        = string
  description = "The Docker image tag to deploy."
}

