#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# For production, change 'sdk' to 'aspnet'
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["api/CESMII.Marketplace.API/CESMII.Marketplace.API.csproj", "api/CESMII.Marketplace.API/"]
COPY ["common/CESMII.Common.CloudLibClient/CESMII.Common.CloudLibClient.csproj", "common/CESMII.Common.CloudLibClient/"]
COPY ["common/Opc.Ua.Cloud.Library.Client/Opc.Ua.Cloud.Library.Client.csproj", "common/Opc.Ua.Cloud.Library.Client/"]
COPY ["common/CESMII.Common.SelfServiceSignUp/CESMII.Common.SelfServiceSignUp.csproj", "common/CESMII.Common.SelfServiceSignUp/"]
COPY ["api/CESMII.Marketplace.Api.Shared/CESMII.Marketplace.Api.Shared.csproj", "api/CESMII.Marketplace.Api.Shared/"]
COPY ["api/CESMII.Marketplace.Common/CESMII.Marketplace.Common.csproj", "api/CESMII.Marketplace.Common/"]
COPY ["api/CESMII.Marketplace.DAL/CESMII.Marketplace.DAL.csproj", "api/CESMII.Marketplace.DAL/"]
COPY ["api/CESMII.Marketplace.Data/CESMII.Marketplace.Data.csproj", "api/CESMII.Marketplace.Data/"]
COPY ["api/CESMII.Marketplace.JobManager/CESMII.Marketplace.JobManager.csproj", "api/CESMII.Marketplace.JobManager/"]
RUN dotnet restore "api/CESMII.Marketplace.API/CESMII.Marketplace.API.csproj"
COPY . .
WORKDIR "/src/api/CESMII.Marketplace.API"
RUN dotnet build "CESMII.Marketplace.API.csproj" -c Debug -o /app/build

# For production, change 'Debug' to 'Release'
FROM build AS publish
RUN dotnet publish "CESMII.Marketplace.API.csproj" -c Debug -o /app/publish /p:UseAppHost=false

WORKDIR /app/build
ENTRYPOINT ["dotnet", "CESMII.Marketplace.API.dll"]