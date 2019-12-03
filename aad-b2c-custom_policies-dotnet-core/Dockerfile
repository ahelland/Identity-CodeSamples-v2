# Linux base image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src
COPY ["aad-b2c-custom_policies-dotnet-core/aad-b2c-custom_policies-dotnet-core.csproj", "aad-b2c-custom_policies-dotnet-core/"]
RUN dotnet restore "aad-b2c-custom_policies-dotnet-core/aad-b2c-custom_policies-dotnet-core.csproj"
COPY . .
WORKDIR "/src/aad-b2c-custom_policies-dotnet-core"
RUN dotnet build "aad-b2c-custom_policies-dotnet-core.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "aad-b2c-custom_policies-dotnet-core.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "aad-b2c-custom_policies-dotnet-core.dll"]