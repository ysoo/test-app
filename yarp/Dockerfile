#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 5001
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["yarp.csproj", "."]
RUN dotnet restore "yarp.csproj"
COPY . .
RUN dotnet build "yarp.csproj" -c Release -o /app/build
RUN dotnet publish "yarp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
EXPOSE 8443
USER app
ENTRYPOINT ["dotnet", "yarp.dll"]
