terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    Environment = var.environment
    Project     = "BoxTracking"
  }
}

# Container Registry (using existing from flight tracker)
data "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = "flight-tracker-rg"
}

# Container Apps Environment (without Log Analytics - using Sentry instead)
resource "azurerm_container_app_environment" "main" {
  name                = "${var.prefix}-env"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  tags = {
    Environment = var.environment
    Project     = "BoxTracking"
  }
}

# RabbitMQ Container App
resource "azurerm_container_app" "rabbitmq" {
  name                         = "${var.prefix}-rabbitmq"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "rabbitmq"
      image  = "rabbitmq:3-management"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "RABBITMQ_DEFAULT_USER"
        value = "guest"
      }

      env {
        name  = "RABBITMQ_DEFAULT_PASS"
        value = "guest"
      }
    }

    min_replicas = 1
    max_replicas = 1
  }

  ingress {
    external_enabled = false
    target_port      = 5672
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = {
    Environment = var.environment
    Project     = "BoxTracking"
  }
}

# API Container App
resource "azurerm_container_app" "api" {
  name                         = "${var.prefix}-api"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "api"
      image  = "${data.azurerm_container_registry.acr.login_server}/boxtracking-api:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "RabbitMQ__Host"
        value = azurerm_container_app.rabbitmq.name
      }

      env {
        name  = "RabbitMQ__Port"
        value = "5672"
      }

      env {
        name  = "RabbitMQ__Username"
        value = "guest"
      }

      env {
        name  = "RabbitMQ__Password"
        value = "guest"
      }

      env {
        name  = "Sentry__Dsn"
        value = var.sentry_dsn
      }
    }

    min_replicas = 1
    max_replicas = 3
  }

  registry {
    server               = data.azurerm_container_registry.acr.login_server
    username             = data.azurerm_container_registry.acr.admin_username
    password_secret_name = "registry-password"
  }

  secret {
    name  = "registry-password"
    value = data.azurerm_container_registry.acr.admin_password
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = {
    Environment = var.environment
    Project     = "BoxTracking"
  }

  depends_on = [azurerm_container_app.rabbitmq]
}

# Event Processor Container App
resource "azurerm_container_app" "processor" {
  name                         = "${var.prefix}-processor"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "processor"
      image  = "${data.azurerm_container_registry.acr.login_server}/boxtracking-processor:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "RabbitMQ__Host"
        value = azurerm_container_app.rabbitmq.name
      }

      env {
        name  = "RabbitMQ__Port"
        value = "5672"
      }

      env {
        name  = "RabbitMQ__Username"
        value = "guest"
      }

      env {
        name  = "RabbitMQ__Password"
        value = "guest"
      }

      env {
        name  = "SignalR__HubUrl"
        value = "http://${azurerm_container_app.dashboard.name}/hubs/dashboard"
      }

      env {
        name  = "Sentry__Dsn"
        value = var.sentry_dsn
      }
    }

    min_replicas = 1
    max_replicas = 2
  }

  registry {
    server               = data.azurerm_container_registry.acr.login_server
    username             = data.azurerm_container_registry.acr.admin_username
    password_secret_name = "registry-password"
  }

  secret {
    name  = "registry-password"
    value = data.azurerm_container_registry.acr.admin_password
  }

  tags = {
    Environment = var.environment
    Project     = "BoxTracking"
  }

  depends_on = [azurerm_container_app.rabbitmq, azurerm_container_app.dashboard]
}

# Dashboard Container App
resource "azurerm_container_app" "dashboard" {
  name                         = "${var.prefix}-dashboard"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "dashboard"
      image  = "${data.azurerm_container_registry.acr.login_server}/boxtracking-dashboard:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "Sentry__Dsn"
        value = var.sentry_dsn
      }
    }

    min_replicas = 1
    max_replicas = 2
  }

  registry {
    server               = data.azurerm_container_registry.acr.login_server
    username             = data.azurerm_container_registry.acr.admin_username
    password_secret_name = "registry-password"
  }

  secret {
    name  = "registry-password"
    value = data.azurerm_container_registry.acr.admin_password
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = {
    Environment = var.environment
    Project     = "BoxTracking"
  }
}

# Event Simulator Container App
resource "azurerm_container_app" "simulator" {
  name                         = "${var.prefix}-simulator"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    container {
      name   = "simulator"
      image  = "${data.azurerm_container_registry.acr.login_server}/boxtracking-simulator:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "BoxTrackingApi__BaseUrl"
        value = "https://${azurerm_container_app.api.ingress[0].fqdn}"
      }

      env {
        name  = "Sentry__Dsn"
        value = var.sentry_dsn
      }
    }

    min_replicas = 1
    max_replicas = 2
  }

  registry {
    server               = data.azurerm_container_registry.acr.login_server
    username             = data.azurerm_container_registry.acr.admin_username
    password_secret_name = "registry-password"
  }

  secret {
    name  = "registry-password"
    value = data.azurerm_container_registry.acr.admin_password
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = {
    Environment = var.environment
    Project     = "BoxTracking"
  }

  depends_on = [azurerm_container_app.api]
}
