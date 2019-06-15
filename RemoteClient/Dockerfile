FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["RemoteClient/RemoteClient.csproj", "RemoteClient/"]
RUN dotnet restore "RemoteClient/RemoteClient.csproj"
COPY . .
WORKDIR "/src/RemoteClient"
RUN dotnet build "RemoteClient.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RemoteClient.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RemoteClient.dll"]