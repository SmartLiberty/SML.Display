FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG PAT
WORKDIR /app

# Copy csproj, nuget.config (applying config for docker) and restore as distinct layers
COPY ./SML.Display/*.csproj ./SML.Display/
COPY ./SML.Display.Core/*.csproj ./SML.Display.Core/
COPY ./SML.Display/docker.nuget.config ./SML.Display/nuget.config

RUN sed -i "s|</configuration>|<packageSourceCredentials><SmartDevOps><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevOps><SmartDevQA><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevQA></packageSourceCredentials></configuration>|" ./SML.Display/nuget.config
RUN dotnet restore --configfile "./SML.Display/nuget.config" "./SML.Display/SML.Display.csproj"
# Copy everything else and build
COPY ./SML.Display ./SML.Display
COPY ./SML.Display.Core ./SML.Display.Core
WORKDIR /app/SML.Display
RUN dotnet publish -c Release -o out

RUN GRPC_HEALTH_PROBE_VERSION=v0.4.8 && \
    wget -qO /bin/grpc_health_probe https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/${GRPC_HEALTH_PROBE_VERSION}/grpc_health_probe-linux-amd64 && \
    chmod +x /bin/grpc_health_probe

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build-env /app/SML.Display/out .
COPY --from=build-env /bin/grpc_health_probe /bin/grpc_health_probe
ENTRYPOINT ["dotnet", "SML.Display.dll"]
