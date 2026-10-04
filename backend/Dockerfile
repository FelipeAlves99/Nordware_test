FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ProductReservation.Api/ProductReservation.Api.csproj src/ProductReservation.Api/
COPY src/ProductReservation.Application/ProductReservation.Application.csproj src/ProductReservation.Application/
COPY src/ProductReservation.Domain/ProductReservation.Domain.csproj src/ProductReservation.Domain/
COPY src/ProductReservation.Infrastructure/ProductReservation.Infrastructure.csproj src/ProductReservation.Infrastructure/

RUN dotnet restore src/ProductReservation.Api/ProductReservation.Api.csproj

COPY . .
RUN dotnet publish src/ProductReservation.Api/ProductReservation.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductReservation.Api.dll"]
