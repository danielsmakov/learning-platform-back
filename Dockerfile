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
# $$ → $ при сборке образа. Убираем CR/пробелы по краям (.env с Windows). printenv надёжнее ${VAR} в edge-кейсах.
ENTRYPOINT ["/bin/sh", "-c", "set -eu; conn=$(printenv ConnectionStrings__DefaultConnection | tr -d '\\r' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//'); test -n \"$$conn\" || { echo ConnectionStrings__DefaultConnection required >&2; exit 1; }; /app/migrate --connection \"$$conn\"; exec dotnet /app/learning-platform-back.dll"]
