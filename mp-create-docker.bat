docker login
docker pull  mcr.microsoft.com/dotnet/samples:aspnetapp
docker run -d -p 5432:5432 --name MpServer mcr.microsoft.com/dotnet/samples:aspnetapp