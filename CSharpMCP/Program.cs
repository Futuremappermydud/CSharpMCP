using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Linq;

namespace MyApp
{
    internal class Program
    {
        public static CSharpDecompiler _decompiler;
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello, World!");
            var path = "D:\\SteamLibrary\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\Main.dll";
            //Console.WriteLine("Parsing " + path);
            //Console.WriteLine(typeof(FullTypeName).Assembly.GetName());

            _decompiler = new CSharpDecompiler(path, new DecompilerSettings());
            //var types = decompiler.TypeSystem.MainModule.TypeDefinitions;

            //foreach (var type in types)
            //{
                //Console.WriteLine(type.FullName);
            //}

            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();
            builder.Build().Run();
        }
    }

    [McpServerToolType]
    public static class GetTypesTool
    {
        private static IEnumerable<ITypeDefinition> GetTypeDefinitions() => Program._decompiler.TypeSystem.MainModule.TypeDefinitions;
        private static ITypeDefinition GetTypeDefinition(string type) => GetTypeDefinitions().FirstOrDefault(x => x.FullName == type);
        [McpServerTool, Description("Grabs all types from loaded assemblies.")]
        public static string GrabAssemblies() => string.Join("\n", GetTypeDefinitions().Select(x => x.FullName).Where(x=>!x.Contains("<") && !x.Contains("UnitySource")));
        //public static string GrabAssemblies() => string.Join("\n", GetTypeDefinitions().Select(x => x.FullName));
        /*[McpServerTool, Description("Grabs all methods/functions from a given type")]
        public static string GrabMethods(string type) => string.Join("\n", GetTypeDefinition(type).Methods.Select(x => $"({x.ReturnType.FullName}) {x.Name}"));
        [McpServerTool, Description("Grabs all members/fields from a given type")]
        public static string GrabMemberFields(string type) => string.Join("\n", GetTypeDefinition(type).Members.Select(x => $"({x.ReturnType.FullName}) {x.Name}"));*/
    }
}