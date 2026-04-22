# Build context must be the solution root:
#   docker build -f src/Backend/VendSys.Api/Dockerfile -t vendex-api .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for layer-cached restore
COPY VendSys.sln .
COPY src/Backend/VendSys.Domain/VendSys.Domain.csproj             src/Backend/VendSys.Domain/
COPY src/Backend/VendSys.Application/VendSys.Application.csproj   src/Backend/VendSys.Application/
COPY src/Backend/VendSys.Infrastructure/VendSys.Infrastructure.csproj src/Backend/VendSys.Infrastructure/
COPY src/Backend/VendSys.Api/VendSys.Api.csproj                   src/Backend/VendSys.Api/

RUN dotnet restore src/Backend/VendSys.Api/VendSys.Api.csproj

# Copy source and publish
COPY src/Backend/ src/Backend/
RUN dotnet publish src/Backend/VendSys.Api/VendSys.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "VendSys.Api.dll"]
