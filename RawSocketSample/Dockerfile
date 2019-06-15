FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["RawSocketSample/RawSocketSample.csproj", "RawSocketSample/"]
RUN dotnet restore "RawSocketSample/RawSocketSample.csproj"
COPY . .
WORKDIR "/src/RawSocketSample"
RUN dotnet build "RawSocketSample.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RawSocketSample.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RawSocketSample.dll"]
EXPOSE 8087