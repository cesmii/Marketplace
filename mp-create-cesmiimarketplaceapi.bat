docker login
docker build -f "C:\CESMII.github\Marketplace\api\CESMII.Marketplace.API\Dockerfile" --force-rm -t cesmiimarketplaceapi:dev --target base  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=CESMII.Marketplace.API" "C:\CESMII.github\Marketplace"
docker run -dt --network mynet1 -p 5001:443 -P --name CESMII.Marketplace.API -v "C:\Users\paul\AppData\Roaming\Microsoft\UserSecrets:/root/.microsoft/usersecrets:ro" -v "C:\Users\paul\AppData\Roaming\ASP.NET\Https:/root/.aspnet/https:ro" -v "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Sdks\Microsoft.Docker.Sdk\tools\TokenService.Proxy\linux-x64\net6.0:/TokenService.Proxy:ro" -v "C:\CESMII.github\Marketplace\api\CESMII.Marketplace.API:/app" -v "C:\CESMII.github\Marketplace:/src/" -e "ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=true" -e "ASPNETCORE_ENVIRONMENT=Development" -e "ASPNETCORE_URLS=https://+:443;http://+:80" -e "ASPNETCORE_HTTPS_PORT=5001" -e "DOTNET_USE_POLLING_FILE_WATCHER=1" -e "NUGET_PACKAGES=/root/.nuget/fallbackpackages" -e "NUGET_FALLBACK_PACKAGES=/root/.nuget/fallbackpackages"  -e "EnableCloudLibSearch=false" -e "MARKETPLACE_MONGODB_CONNECTIONSTRING=mongodb://testuser:password@MyMongoDB:27017" -e "MARKETPLACE_MONGODB_DATABASE=test" --entrypoint tail cesmiimarketplaceapi:dev
REM dr run -dt --network mynet1 -p 5001:443 -P --name CESMII.Marketplace.API -v "C:\Users\paul\vsdbg\vs2017u5:/remote_debugger:rw"     -v "C:\Users\paul\AppData\Roaming\Microsoft\UserSecrets:/root/.microsoft/usersecrets:ro" -v "C:\Users\paul\AppData\Roaming\ASP.NET\Https:/root/.aspnet/https:ro" -v "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Sdks\Microsoft.Docker.Sdk\tools\TokenService.Proxy\linux-x64\net6.0:/TokenService.Proxy:ro" -v "C:\CESMII.github\Marketplace\api\CESMII.Marketplace.API:/app" -v "C:\CESMII.github\Marketplace:/src/" -v "C:\Users\paul\.nuget\packages\:/root/.nuget/fallbackpackages" -e "ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=true" -e "ASPNETCORE_ENVIRONMENT=Development" -e "ASPNETCORE_URLS=https://+:443;http://+:80" -e "ASPNETCORE_HTTPS_PORT=5001" -e "DOTNET_USE_POLLING_FILE_WATCHER=1" -e "NUGET_PACKAGES=/root/.nuget/fallbackpackages" -e "NUGET_FALLBACK_PACKAGES=/root/.nuget/fallbackpackages"   --entrypoint tail cesmiimarketplaceapi:dev -f /dev/null
dotnet test ./api/Tests/CESMII.Marketplace.RestApi/CESMII.Marketplace.RestApi.csproj



