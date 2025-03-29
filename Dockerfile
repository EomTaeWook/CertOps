FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /App

COPY CertOps ./CertOps
COPY Dll ./Dll

RUN dotnet restore "CertOps/CertOps.csproj"
RUN dotnet publish "CertOps/CertOps.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /App

COPY --from=build /App/out .

VOLUME ["/App/log"]
VOLUME ["/App/archive"]
VOLUME ["/App/config.json"]

ENTRYPOINT ["dotnet", "CertOps.dll"]