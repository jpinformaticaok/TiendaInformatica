# ==========================================
# ETAPA 1: Compilación y Publicación (Build)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo el proyecto y restaurar (aprovecha la caché de capas)
COPY TiendaInformatica.csproj ./
RUN dotnet restore

# Copiar el resto del código y publicar en Release
COPY . ./
RUN dotnet publish TiendaInformatica.csproj -c Release -o /app/publish \
    /p:UseAppHost=false --no-restore

# ==========================================
# ETAPA 2: Entorno de Ejecución (Runtime)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Kestrel escucha en 5001 dentro del contenedor (el mapeo al host lo hace compose)
ENV ASPNETCORE_HTTP_PORTS=5001 \
    DOTNET_NOLOGO=true \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# Ejecutar como usuario no root (APP_UID viene definido en las imágenes de .NET)
USER $APP_UID

EXPOSE 5001

ENTRYPOINT ["dotnet", "TiendaInformatica.dll"]
