#tool nuget:?package=IlRepack&version=2.0.15
#r "System.Linq.Xml"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var csProjPath = "./src/SharpWield.csproj";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./src/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(csProjPath);
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    // Use MSBuild
    MSBuild(csProjPath, settings => {
      settings.SetConfiguration(configuration);
      settings.ToolVersion = MSBuildToolVersion.VS2017;
    });
});

Task("IlRepack")
    .IsDependentOn("Build")
    .Does(() =>
{
  var exeDir = buildDir + Directory(
#if NET46
      "net46"
#else
      "net45"
#endif
    );
  var exePath = exeDir + File("SharpWield.exe");

  // The order of the DLLs seems to matter! If it's wrong, il-Repack gives an error
  // about duplicate types.
  var dllNames = new [] {
      "Microsoft.CodeAnalysis.CSharp.dll",
#if NET46
      "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
      "System.ValueTuple.dll",
      "System.IO.FileSystem.Primitives.dll",
      "System.IO.FileSystem.dll",
      "System.Security.Cryptography.Algorithms.dll",
      "System.Security.Cryptography.Primitives.dll",
#else
      "Microsoft.CodeAnalysis.Scripting.CSharp.dll",
      "Microsoft.CodeAnalysis.Desktop.dll",
#endif
      "Microsoft.CodeAnalysis.dll",
      "Microsoft.CodeAnalysis.Scripting.dll",
      "System.Collections.Immutable.dll",
      "System.Reflection.Metadata.dll",
      "Mono.Cecil.dll",
      "Mono.Cecil.Mdb.dll",
      "Mono.Cecil.Pdb.dll",
      "Mono.Cecil.Rocks.dll"
    };
  var assemblyPaths = dllNames.Select(a => (exeDir + File(a)).Path).ToArray().AsEnumerable();

  ILRepack("./SharpWield.exe", exePath, assemblyPaths);
});

Task("pack")
    .IsDependentOn("IlRepack")
    .Does(() =>
{
    var propGroupElement = System.Xml.Linq.XDocument.Load(csProjPath).Document.Element("Project").Element("PropertyGroup");
    var descrip = propGroupElement.Element("Description").Value;
    var versionString = propGroupElement.Element("Version").Value;
    var authors = propGroupElement.Element("Authors").Value.Split(' ');
    NuGetPack("./SharpWield.nuspec", new NuGetPackSettings {
        // OutputDirectory = "./",
        Authors = authors,
        Description = descrip,
        Version = versionString,
        Properties = new Dictionary<string, string> {{ "Configuration", configuration}}
    });
});

Task("Default")
    .IsDependentOn("IlRepack");

RunTarget(target);