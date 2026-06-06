# syntax=docker/dockerfile:1-labs

# Multi-stage Dockerfile for Tasklet application which packages the Vue SPA into
# The .NET servers's wwwroot and deploys it as a single image. Static files are
# served with a cache header for CDN's (front through CloudFront, Google CDN, etc.).
# Though for Sqlite, it's not going to be fun in Google Cloud...

#*----------------------------------------------------------------------------------
# Stage 1: Build the Vue SPA
#*----------------------------------------------------------------------------------
FROM node:24-alpine AS web-build
WORKDIR /srcroot

COPY ./src/web/package.json ./src/web/yarn.lock ./src/web/
RUN yarn install --frozen-lockfile --cwd ./src/web

COPY ./src/web ./src/web
RUN yarn --cwd ./src/web build

#*----------------------------------------------------------------------------------
# Stage 2: Build the .NET Backend
#*----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /srcroot

# Copy only the files needed for restore first (to leverage Docker layer caching)
COPY --parents ./src/**/*.csproj ./

# Restore dependencies
RUN dotnet restore ./src/backend/runtime/Tasklet.API.csproj

# Now copy the rest of the source code for the server
COPY ./src/backend/runtime  ./src/backend/runtime

# Copy the Vue SPA build output into the server's wwwroot
COPY --from=web-build /srcroot/src/web/dist ./src/backend/runtime/wwwroot/app

# Build the application without restore (already restored above)
RUN dotnet publish ./src/backend/runtime/Tasklet.API.csproj \
  --configuration Release \
  --no-restore \
  --output /app/publish

#*----------------------------------------------------------------------------------
# Stage 3: Build the .NET Runtime Image
#*----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Tasklet.API.dll"]
