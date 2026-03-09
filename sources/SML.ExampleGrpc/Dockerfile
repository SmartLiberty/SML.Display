FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG PAT
WORKDIR /app

# Copy csproj, nuget.config (applying config for docker) and restore as distinct layers
COPY ./SML.ExampleGrpc/*.csproj ./SML.ExampleGrpc/
COPY ./SML.ExampleGrpc.Core/*.csproj ./SML.ExampleGrpc.Core/
COPY ./SML.ExampleGrpc/docker.nuget.config ./SML.ExampleGrpc/nuget.config

RUN sed -i "s|</configuration>|<packageSourceCredentials><SmartDevOps><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevOps><SmartDevQA><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevQA></packageSourceCredentials></configuration>|" ./SML.ExampleGrpc/nuget.config
RUN dotnet restore --configfile "./SML.ExampleGrpc/nuget.config" "./SML.ExampleGrpc/SML.ExampleGrpc.csproj"
# Copy everything else and build
COPY ./SML.ExampleGrpc ./SML.ExampleGrpc
COPY ./SML.ExampleGrpc.Core ./SML.ExampleGrpc.Core
WORKDIR /app/SML.ExampleGrpc
RUN dotnet publish -c Release -o out

RUN GRPC_HEALTH_PROBE_VERSION=v0.4.8 && \
    wget -qO /bin/grpc_health_probe https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/${GRPC_HEALTH_PROBE_VERSION}/grpc_health_probe-linux-amd64 && \
    chmod +x /bin/grpc_health_probe

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build-env /app/SML.ExampleGrpc/out .
COPY --from=build-env /bin/grpc_health_probe /bin/grpc_health_probe
ENTRYPOINT ["dotnet", "SML.ExampleGrpc.dll"]