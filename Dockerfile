# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RedpandaProxy.csproj", "."]
RUN dotnet restore "RedpandaProxy.csproj"
COPY . .
RUN dotnet build "RedpandaProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedpandaProxy.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedpandaProxy.dll"]
