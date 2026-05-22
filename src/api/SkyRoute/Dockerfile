FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer-cached restore
COPY Directory.Packages.props Directory.Packages.props
COPY Directory.Build.props Directory.Build.props
COPY SkyRoute.slnx ./
COPY src/SkyRoute.Api/SkyRoute.Api.csproj                             src/SkyRoute.Api/
COPY src/SkyRoute.BusinessLogic/SkyRoute.BusinessLogic.csproj         src/SkyRoute.BusinessLogic/
COPY src/SkyRoute.DataAccessLayer/SkyRoute.DataAccessLayer.csproj     src/SkyRoute.DataAccessLayer/
COPY test/SkyRoute.Api.UnitTest/SkyRoute.Api.UnitTest.csproj          test/SkyRoute.Api.UnitTest/
COPY test/SkyRoute.BusinessLogic.UnitTest/SkyRoute.BusinessLogic.UnitTest.csproj test/SkyRoute.BusinessLogic.UnitTest/
COPY test/SkyRoute.DataAccessLayer.UnitTest/SkyRoute.DataAccessLayer.UnitTest.csproj test/SkyRoute.DataAccessLayer.UnitTest/

RUN dotnet restore

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish src/SkyRoute.Api/SkyRoute.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SkyRoute.Api.dll"]
