# ===================================
# Database Migration Script for Healink Microservices (PowerShell)
# Supports environment variable-based configuration
# ===================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("auth", "user", "all")]
    [string]$Service,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("add", "update", "script", "list", "remove")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$MigrationName
)

# Function to write colored output
function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Function to check environment variables
function Test-EnvironmentVariables {
    param([string]$ServiceName)
    
    $connectionStringVar = "${ServiceName}_DB_CONNECTION_STRING"
    $connectionString = [Environment]::GetEnvironmentVariable($connectionStringVar)
    
    if (-not $connectionString) {
        $requiredVars = @("DB_HOST", "DB_USER", "DB_PASSWORD", "${ServiceName}_DB_NAME")
        
        foreach ($var in $requiredVars) {
            $value = [Environment]::GetEnvironmentVariable($var)
            if (-not $value) {
                Write-Error-Custom "Required environment variable '$var' is not set"
                return $false
            }
        }
    }
    
    return $true
}

# Function to run migration
function Invoke-Migration {
    param(
        [string]$ServiceName,
        [string]$Action,
        [string]$MigrationName
    )
    
    Write-Info "Running $Action for $ServiceName Service..."
    
    # Check environment variables
    if (-not (Test-EnvironmentVariables -ServiceName $ServiceName)) {
        Write-Error-Custom "Environment variables check failed for $ServiceName"
        return $false
    }
    
    # Set working directory
    $serviceDir = "src\${ServiceName}Service\${ServiceName}Service.Infrastructure"
    $startupProject = "..\${ServiceName}Service.API"
    
    if (-not (Test-Path $serviceDir)) {
        Write-Error-Custom "Service directory not found: $serviceDir"
        return $false
    }
    
    $originalLocation = Get-Location
    try {
        Set-Location $serviceDir
        
        switch ($Action) {
            "add" {
                if (-not $MigrationName) {
                    Write-Error-Custom "Migration name is required for 'add' action"
                    return $false
                }
                Write-Info "Adding migration: $MigrationName"
                dotnet ef migrations add $MigrationName --project . --startup-project $startupProject
            }
            "update" {
                Write-Info "Applying migrations to database"
                dotnet ef database update --project . --startup-project $startupProject
            }
            "script" {
                Write-Info "Generating SQL script"
                dotnet ef migrations script --project . --startup-project $startupProject
            }
            "list" {
                Write-Info "Listing migrations"
                dotnet ef migrations list --project . --startup-project $startupProject
            }
            "remove" {
                Write-Info "Removing last migration"
                dotnet ef migrations remove --project . --startup-project $startupProject
            }
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "$Action completed for $ServiceName Service"
            return $true
        } else {
            Write-Error-Custom "$Action failed for $ServiceName Service"
            return $false
        }
    }
    finally {
        Set-Location $originalLocation
    }
}

# Function to show usage
function Show-Usage {
    Write-Host @"
Usage: migrate.ps1 -Service <service> -Action <action> [-MigrationName <name>]

Services:
  auth      - Auth Service
  user      - User Service
  all       - All Services (only for update action)

Actions:
  add       - Add new migration (requires -MigrationName)
  update    - Apply migrations to database
  script    - Generate SQL script
  list      - List all migrations
  remove    - Remove last migration

Examples:
  .\migrate.ps1 -Service auth -Action add -MigrationName InitialCreate
  .\migrate.ps1 -Service user -Action update
  .\migrate.ps1 -Service all -Action update
  .\migrate.ps1 -Service auth -Action script > migrations.sql

Environment Variables:
  Required: DB_HOST, DB_USER, DB_PASSWORD, AUTH_DB_NAME, USER_DB_NAME
  Or: AUTH_DB_CONNECTION_STRING, USER_DB_CONNECTION_STRING

Note: Load .env file manually with: Get-Content .env | ForEach-Object { [Environment]::SetEnvironmentVariable($_.Split('=')[0], $_.Split('=')[1]) }
"@
}

# Main script logic
try {
    # Check if dotnet ef is available
    $dotnetEfCheck = dotnet ef 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "dotnet ef tools are not installed"
        Write-Info "Install with: dotnet tool install --global dotnet-ef"
        exit 1
    }
    
    # Load environment variables from .env file if it exists
    if (Test-Path ".env") {
        Write-Info "Loading environment variables from .env file"
        Get-Content ".env" | ForEach-Object {
            if ($_ -match "^([^=]+)=(.*)$") {
                [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
            }
        }
    }
    
    # Validate required parameters
    if ($Action -eq "add" -and -not $MigrationName) {
        Write-Error-Custom "Migration name is required for 'add' action"
        Show-Usage
        exit 1
    }
    
    # Run migrations based on service
    switch ($Service) {
        "auth" {
            $success = Invoke-Migration -ServiceName "Auth" -Action $Action -MigrationName $MigrationName
        }
        "user" {
            $success = Invoke-Migration -ServiceName "User" -Action $Action -MigrationName $MigrationName
        }
        "all" {
            if ($Action -eq "update") {
                Write-Info "Running migrations for all services..."
                $authSuccess = Invoke-Migration -ServiceName "Auth" -Action $Action -MigrationName $MigrationName
                $userSuccess = Invoke-Migration -ServiceName "User" -Action $Action -MigrationName $MigrationName
                $success = $authSuccess -and $userSuccess
            } else {
                Write-Error-Custom "The 'all' service option is only supported for 'update' action"
                exit 1
            }
        }
    }
    
    if ($success) {
        Write-Success "Migration process completed successfully!"
    } else {
        Write-Error-Custom "Migration process failed!"
        exit 1
    }
}
catch {
    Write-Error-Custom "An error occurred: $_"
    exit 1
}
