# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY learning-platform-back.csproj .
RUN dotnet restore learning-platform-back.csproj
COPY . .
RUN dotnet publish learning-platform-back.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS bundle
WORKDIR /src
COPY learning-platform-back.csproj .
RUN dotnet restore learning-platform-back.csproj
COPY . .
RUN dotnet tool install --global dotnet-ef --version 9.0.7
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project learning-platform-back.csproj \
    --startup-project learning-platform-back.csproj \
    --runtime linux-x64 \
    --output /bundle/migrate \
    --self-contained \
    --force

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
COPY --from=bundle /bundle/migrate ./migrate
RUN chmod +x migrate
# $$ → $ при сборке. Без pipefail после printenv sed мог вернуть 0 при пустом printenv — в migrate уходила пустая строка.
ENTRYPOINT ["/bin/sh", "-c", "set -eu; _raw=$(printenv ConnectionStrings__DefaultConnection || true); test -n \"$$_raw\" || { echo ConnectionStrings__DefaultConnection required >&2; exit 1; }; cs=$(printf '%s' \"$$_raw\" | tr -d '\\r'); cs=$(printf '%s' \"$$cs\" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//'); /app/migrate --connection \"$$cs\"; exec dotnet /app/learning-platform-back.dll"]
