# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first for better layer caching
COPY cateredByLetsuwi/cateredByLetsuwi.csproj cateredByLetsuwi/
RUN dotnet restore cateredByLetsuwi/cateredByLetsuwi.csproj

# Copy source and publish
COPY . .
RUN dotnet publish cateredByLetsuwi/cateredByLetsuwi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Render injects PORT. Default to 10000 for local container tests.
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 10000

CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet cateredByLetsuwi.dll"]
