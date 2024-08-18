dotnet build "NodeSwap" -c Release
dotnet publish "NodeSwap" -c Release
MSBuild "NodeSwap.Installer" -property:Configuration=Release