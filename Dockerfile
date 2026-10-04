# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PetShop.Api.csproj", "./"]
RUN dotnet restore "PetShop.Api.csproj"

COPY . .
RUN dotnet publish "PetShop.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Default ASP.NET Core port
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "PetShop.Api.dll"]
