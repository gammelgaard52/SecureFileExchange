# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ./backend/SecureFileExchange.csproj ./backend/
RUN dotnet restore "./backend/SecureFileExchange.csproj"

# Copy everything else and build
COPY ./backend/ ./backend/
WORKDIR "/src/backend"
RUN dotnet publish -c Release -o /app/publish

# Copy frontend files to `wwwroot`
COPY ./frontend /app/publish/wwwroot

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "SecureFileExchange.dll"]