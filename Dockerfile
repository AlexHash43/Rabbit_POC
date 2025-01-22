# Stage 1: Base build environment for .NET apps
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base-build
WORKDIR /source

# Copy solution and restore dependencies
COPY *.sln .
COPY Common/*.csproj ./Common/
COPY ConsumerApp/*.csproj ./ConsumerApp/
COPY PublisherApp/*.csproj ./PublisherApp/
RUN dotnet restore

# Copy the source code
COPY Common/. ./Common/
COPY ConsumerApp/. ./ConsumerApp/
COPY PublisherApp/. ./PublisherApp/

# Stage 2: Build PublisherApp
FROM base-build AS publisher-build
RUN dotnet publish ./PublisherApp/PublisherApp.csproj -c Release -o /app/publisher

# Stage 3: Build ConsumerApp
FROM base-build AS consumer-build
RUN dotnet publish ./ConsumerApp/ConsumerApp.csproj -c Release -o /app/consumer

# Stage 4: Runtime for PublisherApp
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS publisher-runtime
WORKDIR /app
COPY --from=publisher-build /app/publisher .
EXPOSE 5106
ENTRYPOINT ["dotnet", "PublisherApp.dll"]

# Stage 5: Runtime for ConsumerApp
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS consumer-runtime
WORKDIR /app
COPY --from=consumer-build /app/consumer .
EXPOSE 5027
ENTRYPOINT ["dotnet", "ConsumerApp.dll"]
