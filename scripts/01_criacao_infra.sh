#!/bin/bash
set -e

# =========================
# CONFIGURAÇÕES
# =========================

RESOURCE_GROUP="rg-lares-rm563717"
LOCATION="southafricanorth"

APP_SERVICE_PLAN="plan-lares-rm563717"
WEBAPP_NAME="lares-api-rm563717"

SQL_SERVER="sql-lares-rm563717"
SQL_DATABASE="laresdb"

SQL_ADMIN_USER="laresadmin"
SQL_ADMIN_PASSWORD="$SQL_ADMIN_PASSWORD"

# =========================
# 1. RESOURCE GROUP
# =========================

echo "Criando Resource Group..."

az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION"

# =========================
# 2. APP SERVICE PLAN
# =========================

echo "Criando App Service Plan..."

az appservice plan create \
  --name "$APP_SERVICE_PLAN" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku B1 \
  --is-linux

# =========================
# 3. WEB APP .NET
# =========================

echo "Criando Web App..."

az webapp create \
  --name "$WEBAPP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --plan "$APP_SERVICE_PLAN" \
  --runtime "DOTNETCORE:10.0"

# =========================
# 4. AZURE SQL SERVER
# =========================

echo "Criando Azure SQL Server..."

az sql server create \
  --name "$SQL_SERVER" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --admin-user "$SQL_ADMIN_USER" \
  --admin-password "$SQL_ADMIN_PASSWORD"

# =========================
# 5. AZURE SQL DATABASE
# =========================

echo "Criando Azure SQL Database..."

az sql db create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "$SQL_DATABASE" \
  --service-objective Basic

# =========================
# 6. LIBERA ACESSO DE SERVIÇOS AZURE
# =========================

echo "Configurando firewall..."

az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# =========================
# 7. LIBERA IP LOCAL DO DESENVOLVEDOR
# =========================

echo "Liberando IP atual no firewall do Azure SQL..."
MY_IP=$(curl -s https://api.ipify.org)

if [ -n "$MY_IP" ]; then
  az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER" \
    --name "LiberarIpDesenvolvedor" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP"
  echo "IP $MY_IP liberado com sucesso."
else
  echo "Aviso: Não foi possível detectar o IP público automaticamente."
fi

echo "Infraestrutura criada com sucesso."