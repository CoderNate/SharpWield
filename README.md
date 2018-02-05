
# SharpWield

SharpWield provides a way to run C# .csx script files without depending on an install of C# interactive (CSI.exe) being present. It uses il-Repack to bundle the Microsoft.CodeAnalysis DLLs into a single-file executable that runs an embedded Script.csx file.  Because sometimes you don't feel like using PowerShell.

First, embed your script:

	SharpWield.exe copy-and-replace MyAwesomeScript.csx --new-name MyAwesomeScript.exe

Then just run it:

	MyAwesomeScript.exe run --my-awesome-script-argument

You can also export the embedded script. The following will create MyAwesomeScript.csx:

	MyAwesomeScript.exe export

SharpWield isn't completely self-contained. It does require .NET framework 4.5 to be installed in order to work.  I haven't investigated whether il-Repack and Mono.Cecil would work with a .NET core version, so this is Windows-only.