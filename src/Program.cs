using System;
using System.Linq;
using System.Threading.Tasks;
// using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;

namespace SharpWield
{

    class Program
    {

        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.Write(GetHelpMessage());
            }
            else if (args.First().Equals("run", StringComparison.OrdinalIgnoreCase))
            {
                var scriptStream = GetEmbeddedScriptStream();
                var scriptArgs = args.Skip(1).ToArray();
#if NET46
                RunScript(scriptStream, scriptArgs).Wait();
#else
                RunScript(scriptStream, scriptArgs);
#endif
            }
            else if (args.First().Equals("export", StringComparison.OrdinalIgnoreCase))
            {
                ExportScript(args);
            }
            else if (args.First().Equals("copy-and-replace", StringComparison.OrdinalIgnoreCase))
            {
                CopyAndReplace(args);
            }
            else
            {
                Console.Error.WriteLine($"Error, unrecognized command '{args.First()}'. For help run the program again with no arguments.");
            }
        }

#if NET46
        private static async Task RunScript(System.IO.Stream scriptStream, IList<string> scriptArgs)
        {
            var options = ScriptOptions.Default
	            .AddReferences(System.Reflection.Assembly.GetAssembly(typeof(System.Linq.Enumerable)));
            var script = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create(scriptStream, options, typeof(Globals));
            await script.RunAsync( globals: new Globals(scriptArgs) );
        }
#else
        private static void RunScript(System.IO.Stream scriptStream, IList<string> scriptArgs)
        {
            var options = ScriptOptions.Default
	            .AddReferences(System.Reflection.Assembly.GetAssembly(typeof(System.Linq.Enumerable)));
            var reader = new System.IO.StreamReader(scriptStream);
            var script = Microsoft.CodeAnalysis.Scripting.CSharp.CSharpScript.Create(reader.ReadToEnd(), options);
            script.Run(globals: new Globals(scriptArgs));
        }
#endif

        private static void ExportScript(string[] args)
        {
            const string OUTPUT = "--output";
            var outFileName = GetOptionValue(
                OUTPUT,
                args, 
                onFound: val => val,
                onOmitted: () => GetThisAssemblyFileName() + ".csx",
                onMissingValue: () => null);
            if (outFileName == null)
            {
                Console.Error.WriteLine($"Missing value for {OUTPUT} argument");
                return;
            }
            if (System.IO.File.Exists(outFileName))
            {
                System.Console.WriteLine($"File '{outFileName}' already exists. Overwrite? (y/n)");
                var rslt = System.Console.ReadLine();
                if (!rslt.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            var stream = GetEmbeddedScriptStream();
            using (var fs = new System.IO.FileStream(outFileName, System.IO.FileMode.Create))
            {
                stream.CopyTo(fs);
            }
        }

        private static void CopyAndReplace(IReadOnlyList<string> args)
        {
            if (args.Count < 2)
            {
                Console.Error.WriteLine("Missing ScriptFileName argument. For help, run program without arguments.");
                return;
            }
            var scriptFileName = args[1];
            if (!System.IO.File.Exists(scriptFileName))
            {
                Console.Error.WriteLine(scriptFileName + " file doesn't exist.");
                return;
            }
            const string NEW_NAME = "--new-name";
            string newName = GetOptionValue(
                NEW_NAME,
                args,
                onFound: val => val,
                onOmitted: () => GetThisAssemblyFileName() + " Copy.exe",
                onMissingValue: () => null);
            if (newName == null)
            {
                Console.Error.WriteLine($"Missing value for {NEW_NAME} argument");
                return;
            }
            
            var thisAssem = System.Reflection.Assembly.GetExecutingAssembly();
            var assemPath = thisAssem.Location;
            var definition = Mono.Cecil.AssemblyDefinition.ReadAssembly(assemPath);
            var rsrc = definition.MainModule.Resources.Single(a => a.Name.EndsWith("Script.csx"));
            var resourceName = rsrc.Name;
            definition.MainModule.Resources.Remove(rsrc);
            
            var scriptBytes = System.IO.File.ReadAllBytes(scriptFileName);
            var er = new Mono.Cecil.EmbeddedResource(resourceName, Mono.Cecil.ManifestResourceAttributes.Public, scriptBytes);
            definition.MainModule.Resources.Add(er);
            definition.Write(newName);
        }

        private static System.IO.Stream GetEmbeddedScriptStream()
        {
            var thisAssem = System.Reflection.Assembly.GetExecutingAssembly();
            var scriptResourceName = thisAssem.GetManifestResourceNames()
                    .Where(a => a.EndsWith("Script.csx")).Single();
            var scriptStream = thisAssem.GetManifestResourceStream(scriptResourceName);
            return scriptStream;
        }

        private static T GetOptionValue<T>(
                string optionName,
                IReadOnlyList<string> args,
                Func<string, T> onFound,
                Func<T> onOmitted,
                Func<T> onMissingValue)
        {
            var optionArgInfo = args.Select((arg, index) => new {index, arg})
                    .SingleOrDefault(a => a.arg.Equals(optionName, StringComparison.OrdinalIgnoreCase));
            if (optionArgInfo == null)
            {
                return onOmitted();
            }
            if (args.Count < optionArgInfo.index + 2)
            {
                return onMissingValue();
            }
            return onFound(args[optionArgInfo.index + 1]);
        }


        private static string GetThisAssemblyFileName()
        {
            return System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetHelpMessage()
        {
            var exeName = GetThisAssemblyFileName();
            return $@"This program uses SharpWield - https://github.com/CoderNate/SharpWield

Usage:
Run the script:
    {exeName}.exe run [<ScriptArguments>...]
Export the embedded script:
    {exeName}.exe export [--output <ScriptFileName>]
Copy the exe and replace the embedded script with a new one:
    {exeName}.exe copy-and-replace <ScriptFile> [--new-name <name>]

Options:
--output    A file name for the exported script file.
--new-name  Name for the new exe file. If omitted, just appends ' Copy'.

";
        }

    }

    public class Globals
    {
        public Globals(IList<string> args)
        {
            _args = args;
        }
        private readonly IList<string> _args;
        public IList<string> Args => _args;
    }

}
