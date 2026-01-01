FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Vista.Core.csproj", "./"]
RUN dotnet restore "Vista.Core.csproj"

COPY . .
RUN dotnet publish "Vista.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Storage klasörlerini oluştur (Logos ve Avatars için)
RUN mkdir -p /app/Storage/Logos /app/Storage/Avatars /app/Logs

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Vista.Core.dll"]
