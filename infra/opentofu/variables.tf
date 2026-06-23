variable "subscription_id" {
  description = "Azure subscription ID. Leave null to use ARM_SUBSCRIPTION_ID or Azure CLI authentication."
  type        = string
  default     = null
  nullable    = true
}

variable "name_prefix" {
  description = "Short lowercase prefix used for Azure resource names."
  type        = string
  default     = "brightpay-th"

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{2,31}$", var.name_prefix))
    error_message = "Use 3-32 lowercase letters, numbers, or hyphens, starting with a letter."
  }
}

variable "environment" {
  description = "Deployment environment suffix."
  type        = string
  default     = "demo"

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{1,15}$", var.environment))
    error_message = "Use 2-16 lowercase letters, numbers, or hyphens, starting with a letter."
  }
}

variable "location" {
  description = "Azure region for the demo deployment."
  type        = string
  default     = "uksouth"
}

variable "container_image" {
  description = "Published app container image, for example ghcr.io/owner/brightpay-takehome:latest."
  type        = string
}

variable "enable_log_analytics" {
  description = "Create a Log Analytics workspace for Container Apps diagnostics."
  type        = bool
  default     = false
}

variable "tags" {
  description = "Additional Azure tags."
  type        = map(string)
  default     = {}
}
