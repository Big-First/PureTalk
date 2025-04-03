FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PuretalkApplication/PuretalkApplication.csproj", "PuretalkApplication/"]
RUN dotnet restore "PuretalkApplication/PuretalkApplication.csproj"
COPY . .
WORKDIR "/src/PuretalkApplication"
RUN dotnet build "PuretalkApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PuretalkApplication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PuretalkApplication.dll"]
