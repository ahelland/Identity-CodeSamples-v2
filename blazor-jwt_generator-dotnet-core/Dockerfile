# Linux base image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY ["blazor-jwt_generator-dotnet-core/blazor-jwt_generator-dotnet-core.csproj", "blazor-jwt_generator-dotnet-core/"]
RUN dotnet restore "blazor-jwt_generator-dotnet-core/blazor-jwt_generator-dotnet-core.csproj"
COPY . .
WORKDIR "/src/blazor-jwt_generator-dotnet-core"
RUN dotnet build "blazor-jwt_generator-dotnet-core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "blazor-jwt_generator-dotnet-core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "blazor-jwt_generator-dotnet-core.dll"]