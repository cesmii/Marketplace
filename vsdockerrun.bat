docker run -dt -v "C:\Users\paul\vsdbg\vs2017u5:/remote_debugger:rw" ^
               -v "C:\Users\paul\AppData\Roaming\Microsoft\UserSecrets:/root/.microsoft/usersecrets:ro" ^
               -v "C:\Users\paul\AppData\Roaming\ASP.NET\Https:/root/.aspnet/https:ro" ^
               -v "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Sdks\Microsoft.Docker.Sdk\tools\TokenService.Proxy\linux-x64\net6.0:/TokenService.Proxy:ro" ^
               -v "C:\CESMII.github\Marketplace\api\CESMII.Marketplace.API:/app" ^
               -v "C:\CESMII.github\Marketplace:/src/" ^
               -v "C:\Users\paul\.nuget\packages\:/root/.nuget/fallbackpackages2" ^
               -v "C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages:/root/.nuget/fallbackpackages" ^
               -e "ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=true" -e "ASPNETCORE_ENVIRONMENT=Development" ^
               -e "ASPNETCORE_URLS=https://+:443;http://+:80" ^
               -e "ASPNETCORE_HTTPS_PORT=5001" ^
               -e "DOTNET_USE_POLLING_FILE_WATCHER=1" ^
               -e "NUGET_PACKAGES=/root/.nuget/fallbackpackages2" ^
               -e "NUGET_FALLBACK_PACKAGES=/root/.nuget/fallbackpackages;/root/.nuget/fallbackpackages2" ^
               -p 5001:443 ^
               -P ^
               --name CESMII.Marketplace.API ^
               --entrypoint tail cesmiimarketplaceapi:dev -f /dev/null

