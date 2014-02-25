Building Iron Foundry .NET source
---------------------------------

* Prerequisites:
  * Visual Studio (2013 currently used for development)
  * .NET Framework 4.5

* Before running Visual Studio:
  * Run `build.bat`. It will restore NuGet packages and do a Release-x64 build, which will also create installers.
  * NOTE: Some of the unit tests require you to run as an Administrator and will fail with `System.UnauthorizedAccessException` otherwise.  If you are just building the source, it's safe to ignore these failures for now.
  
* You're all set!
