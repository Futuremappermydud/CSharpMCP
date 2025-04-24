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
using Newtonsoft.Json.Linq;

namespace MyApp
{
    internal class Program
    {
        public static CSharpDecompiler _decompiler;
        static void Main(string[] args)
        {
            var path = "D:\\SteamLibrary\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\Managed\\Main.dll";

            _decompiler = new CSharpDecompiler(path, new DecompilerSettings());

            //Console.WriteLine(GetTypesTool.GetMemberFields("BurstSliderSpawner"));

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
        static JObject NestStrings(IEnumerable<ITypeDefinition> segments)
        {
            var root = new JObject();

            foreach (var path in segments)
            {
                var parts = path.FullName.Split('.');
                JObject current = root;

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];

                    // If this is the last segment, assign the leafValue string
                    if (i == parts.Length - 1)
                    {
                        // Overwrite or create the property
                        current[part] = path.FullName.Contains("delegate", StringComparison.InvariantCultureIgnoreCase) ? "Delegate (No members)" : "Class";
                    }
                    else
                    {
                        // Descend into (or create) the next nested object
                        if (current[part] == null || current[part].Type != JTokenType.Object)
                        {
                            current[part] = new JObject();
                        }

                        current = (JObject)current[part];
                    }
                }
            }

            return root;
        }
        private static IEnumerable<ITypeDefinition> GetTypeDefinitions() => Program._decompiler.TypeSystem.MainModule.TypeDefinitions;
        private static ITypeDefinition GetTypeDefinition(string type) => GetTypeDefinitions().FirstOrDefault(x => x.FullName == type);
        [McpServerTool, Description("Grabs all types from loaded assemblies. To narrow down results add comma separated keywords that are broad but refine the search. Keywords should be single words only not combined.")]
        public static string GetTypes(string keyword) {
            IEnumerable<string> keywords = keyword.Split(",").SelectMany(x=>x.Split(" ")).Select(x=>x.Trim());
            var types = GetTypeDefinitions();
            types = types.Where(x => !x.FullName.Contains("<") && !x.FullName.Contains("UnitySource"));
            types = types.Where(x => keywords.Any(y => x.FullName.Contains(y, StringComparison.InvariantCultureIgnoreCase)));
            var jsonObject = NestStrings(types);
            return jsonObject.ToString(Newtonsoft.Json.Formatting.None);
        }
        private static string FormatMember(IMember member)
        {
            List<string> parts = [member.Accessibility.ToString().ToLower()];
            if (member.IsStatic)
            {
                parts.Add("static");
            }
            parts.Add(member.ReturnType.Name);
            if (member is IMethod method)
            {
                var parameters = method.Parameters;
                var formattedParameters = parameters.Select(x => $"{x.Type.Name} {x.Name}");
                parts.Add($"{method.Name}({string.Join(", ", formattedParameters)})");
            }
            else
            {
                parts.Add($"{member.Name}");
            }
            return string.Join(" ", parts);
        }

        [McpServerTool, Description("Grabs all members (fields/methods) from a given type (fully typed with namespace)")]
        public static string GetMemberFields(string type)
        {
            var members = GetTypeDefinition(type).Members;
            var formattedNames = members.Select(FormatMember);
            return new JArray(formattedNames).ToString(Newtonsoft.Json.Formatting.None);
        }
        [McpServerTool, Description("Returns valid decompiled C# code from a given type (fully typed with namespace) and a given method name (no arguments)")]
        public static string DecompileMethod(string type, string methodName)
        {
            var method = GetTypeDefinition(type).Methods.FirstOrDefault(x=>x.Name == methodName);
            if (method == null)
            {
                return "Failed to find method! Make sure arguments are formatted properly";
            }
            var tree = Program._decompiler.Decompile(method.MetadataToken);
            return tree.ToString();
        }
    }
}