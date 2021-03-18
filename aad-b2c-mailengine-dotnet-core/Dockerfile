# Linux base image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS base
WORKDIR /src
COPY ["aad-b2c-mailengine-dotnet-core/aad-b2c-mailengine-dotnet-core.csproj", "aad-b2c-mailengine-dotnet-core/"]
RUN dotnet restore "aad-b2c-mailengine-dotnet-core/aad-b2c-mailengine-dotnet-core.csproj"
COPY . .
WORKDIR "/src/aad-b2c-mailengine-dotnet-core"
RUN dotnet build "aad-b2c-mailengine-dotnet-core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "aad-b2c-mailengine-dotnet-core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "aad-b2c-mailengine-dotnet-core.dll"]