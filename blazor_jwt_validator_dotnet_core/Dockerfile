# Linux base image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS build
WORKDIR /src
COPY ["blazor_jwt_validator_dotnet_core/blazor_jwt_validator_dotnet_core.csproj", "blazor_jwt_validator_dotnet_core/"]
RUN dotnet restore "blazor_jwt_validator_dotnet_core/blazor_jwt_validator_dotnet_core.csproj"
COPY . .
WORKDIR "/src/blazor_jwt_validator_dotnet_core"
RUN dotnet build "blazor_jwt_validator_dotnet_core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "blazor_jwt_validator_dotnet_core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "blazor_jwt_validator_dotnet_core.dll"]