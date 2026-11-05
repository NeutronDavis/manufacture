# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies (layer cache)
COPY ["Manufacture.Web/Manufacture.Web.csproj", "Manufacture.Web/"]
COPY ["Manufacture.Library/Manufacture.Library.csproj", "Manufacture.Library/"]
RUN dotnet restore "Manufacture.Web/Manufacture.Web.csproj"

# Copy source code and build
COPY . .
RUN dotnet publish "Manufacture.Web/Manufacture.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Render passes PORT environment variable or defaults to 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Manufacture.Web.dll"]
