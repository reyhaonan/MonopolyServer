
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY MonopolyServer/*.csproj ./MonopolyServer/
RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR /src/MonopolyServer
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5217
ENV ASPNETCORE_URLS=http://+:5217

ENTRYPOINT ["dotnet", "MonopolyServer.dll"]
