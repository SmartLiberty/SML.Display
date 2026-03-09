FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG PAT
WORKDIR /app
# Copy csproj, nuget.config (applying config for docker) and restore as distinct layers
COPY ./SML.Display/*.csproj ./SML.Display/
COPY ./SML.Display.Core/*.csproj ./SML.Display.Core/
COPY ./SML.Display.Tests.Integration/*.csproj ./SML.Display.Tests.Integration/
COPY ./SML.Display.Tests.Integration/docker.nuget.config ./SML.Display.Tests.Integration/nuget.config

RUN sed -i "s|</configuration>|<packageSourceCredentials><SmartDevOps><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevOps><SmartDevQA><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></SmartDevQA></packageSourceCredentials></configuration>|" ./SML.Display.Tests.Integration/nuget.config
RUN dotnet restore --configfile "./SML.Display.Tests.Integration/nuget.config" "./SML.Display.Tests.Integration/SML.Display.Tests.Integration.csproj"

# Copy everything else and build
WORKDIR /app
COPY ./SML.Display.Tests.Integration/ ./SML.Display.Tests.Integration/
COPY ./SML.Display.Core/ ./SML.Display.Core/
COPY ./SML.Display/ ./SML.Display/
WORKDIR /app/SML.Display.Tests.Integration
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY --from=build-env /app/SML.Display.Tests.Integration/out .
ENTRYPOINT dotnet test SML.Display.Tests.Integration.dll
