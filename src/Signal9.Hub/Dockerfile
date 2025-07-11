FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Signal9.Hub/Signal9.Hub.csproj", "src/Signal9.Hub/"]
COPY ["src/Signal9.Shared/Signal9.Shared.csproj", "src/Signal9.Shared/"]
RUN dotnet restore "src/Signal9.Hub/Signal9.Hub.csproj"
COPY . .
WORKDIR "/src/src/Signal9.Hub"
RUN dotnet build "Signal9.Hub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Signal9.Hub.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Signal9.Hub.dll"]
