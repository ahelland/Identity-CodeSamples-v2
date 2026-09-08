#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["blazor-jwt_generator-dotnet10.csproj", "."]
RUN dotnet restore "./blazor-jwt_generator-dotnet10.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "blazor-jwt_generator-dotnet10.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "blazor-jwt_generator-dotnet10.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "blazor-jwt_generator-dotnet10.dll"]