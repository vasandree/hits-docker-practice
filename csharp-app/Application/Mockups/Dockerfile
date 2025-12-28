FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Mockups/Mockups.csproj Mockups/
RUN dotnet restore Mockups/Mockups.csproj
COPY . .
WORKDIR /src/Mockups
RUN dotnet publish Mockups.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Mockups.dll"]
