FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore "./MyComicsManagerApi.csproj" -r linux-arm

# Copy everything else and build
COPY . ./
RUN dotnet publish "./MyComicsManagerApi.csproj" -c Release -o out -r linux-arm

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim-arm32v7
WORKDIR /app
EXPOSE 5000
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "MyComicsManagerApi.dll"]