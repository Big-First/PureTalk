FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PureTalk.Profile/PureTalk.Profile.csproj", "PureTalk.Profile/"]
RUN dotnet restore "PureTalk.Profile/PureTalk.Profile.csproj"
COPY . .
WORKDIR "/src/PureTalk.Profile"
RUN dotnet build "PureTalk.Profile.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PureTalk.Profile.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PureTalk.Profile.dll"]
