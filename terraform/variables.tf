variable "resource_group_name" {
  description = "Name of the Azure Resource Group"
  type        = string
  default     = "rg-boxtracking"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "westeurope"
}

variable "environment" {
  description = "Environment name (Development, Staging, Production)"
  type        = string
  default     = "Development"
}

variable "prefix" {
  description = "Prefix for resource names"
  type        = string
  default     = "boxtrack"

  validation {
    condition     = can(regex("^[a-z0-9]{3,10}$", var.prefix))
    error_message = "Prefix must be 3-10 lowercase alphanumeric characters."
  }
}

variable "acr_name" {
  description = "Name of the Azure Container Registry (must be globally unique)"
  type        = string
  default     = "boxtrackingacr"

  validation {
    condition     = can(regex("^[a-z0-9]{5,50}$", var.acr_name))
    error_message = "ACR name must be 5-50 lowercase alphanumeric characters."
  }
}

variable "sentry_dsn" {
  description = "Sentry DSN for error tracking (leave empty to disable)"
  type        = string
  default     = ""
  sensitive   = true
}
