FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json nuget.config Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY src/Fcg.Payments.Domain/Fcg.Payments.Domain.csproj src/Fcg.Payments.Domain/
COPY src/Fcg.Payments.Application/Fcg.Payments.Application.csproj src/Fcg.Payments.Application/
COPY src/Fcg.Payments.Infrastructure/Fcg.Payments.Infrastructure.csproj src/Fcg.Payments.Infrastructure/
COPY src/Fcg.Payments.WebApi/Fcg.Payments.WebApi.csproj src/Fcg.Payments.WebApi/
RUN dotnet restore src/Fcg.Payments.WebApi/Fcg.Payments.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/Fcg.Payments.WebApi/Fcg.Payments.WebApi.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Fcg.Payments.WebApi.dll"]
