FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG PAT
WORKDIR /app
# Copy csproj, nuget.config (applying config for docker) and restore as distinct layers
COPY ./SML.ExampleGrpc/*.csproj ./SML.ExampleGrpc/
COPY ./SML.ExampleGrpc.Core/*.csproj ./SML.ExampleGrpc.Core/
COPY ./SML.ExampleGrpc.Tests.Integration/*.csproj ./SML.ExampleGrpc.Tests.Integration/
COPY ./SML.ExampleGrpc.Tests.Integration/docker.nuget.config ./SML.ExampleGrpc.Tests.Integration/nuget.config

RUN sed -i "s|</configuration>|<packageSourceCredentials><SmartDevOps><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevOps><SmartDevQA><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevQA></packageSourceCredentials></configuration>|" ./SML.ExampleGrpc.Tests.Integration/nuget.config
RUN dotnet restore --configfile "./SML.ExampleGrpc.Tests.Integration/nuget.config" "./SML.ExampleGrpc.Tests.Integration/SML.ExampleGrpc.Tests.Integration.csproj"

# Copy everything else and build
WORKDIR /app
COPY ./SML.ExampleGrpc.Tests.Integration/ ./SML.ExampleGrpc.Tests.Integration/
COPY ./SML.ExampleGrpc.Core/ ./SML.ExampleGrpc.Core/
COPY ./SML.ExampleGrpc/ ./SML.ExampleGrpc/
WORKDIR /app/SML.ExampleGrpc.Tests.Integration
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY --from=build-env /app/SML.ExampleGrpc.Tests.Integration/out .
ENTRYPOINT dotnet test SML.ExampleGrpc.Tests.Integration.dll