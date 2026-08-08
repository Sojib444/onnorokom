# syntax=docker/dockerfile:1
# Multi-stage build for the ASP.NET Core API.
#
# Stages: build (restore + publish) -> final (runtime image only).
# The API project is restored/published directly; restoring the .slnx would pull in
# the test projects, which must not ship in the production image.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first so dependency restore is cached by Docker.
COPY src/AssignmentManagement.Api/AssignmentManagement.Api.csproj src/AssignmentManagement.Api/
COPY src/AssignmentManagement.Application/AssignmentManagement.Application.csproj src/AssignmentManagement.Application/
COPY src/AssignmentManagement.Domain/AssignmentManagement.Domain.csproj src/AssignmentManagement.Domain/
COPY src/AssignmentManagement.Infrastructure/AssignmentManagement.Infrastructure.csproj src/AssignmentManagement.Infrastructure/
RUN dotnet restore src/AssignmentManagement.Api/AssignmentManagement.Api.csproj

# Copy the remaining sources and publish.
COPY src/AssignmentManagement.Api/ src/AssignmentManagement.Api/
COPY src/AssignmentManagement.Application/ src/AssignmentManagement.Application/
COPY src/AssignmentManagement.Domain/ src/AssignmentManagement.Domain/
COPY src/AssignmentManagement.Infrastructure/ src/AssignmentManagement.Infrastructure/
RUN dotnet publish src/AssignmentManagement.Api/AssignmentManagement.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime image: .NET ASP.NET Core runtime plus curl (used by the compose health check).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Kestrel listens on 8080 inside the container; compose maps the public port.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Configuration (connection string, JWT, seeding) is injected via environment
# variables by docker compose / .env - never baked into the image.
ENTRYPOINT ["dotnet", "AssignmentManagement.Api.dll"]
