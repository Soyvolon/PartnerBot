using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.CSharp;
using System;
using Microsoft.CodeAnalysis;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp;
using PartnerBot.Core.Entities.Configuration;
using PartnerBot.Discord.Commands.Conditions;

namespace PartnerBot.Discord.Commands.Admin
{
    public class EvalCommand : CommandModule
    {
        [Command("evalraw")]
        [Description("Evaluate a larger set of code. Requires a method with `public static async Task Eval(CommandContext ctx) { }` to execute the code.")]
        [RequireCessumAdmin]
        public async Task EvalRawCommandAsync(CommandContext ctx, [RemainingText] string code)
        {
            var rawCode = code;
            if (rawCode.StartsWith("```"))
            {
                rawCode = rawCode.Remove(0, rawCode.IndexOf('\n'));
                rawCode = rawCode.Remove(rawCode.LastIndexOf('\n'));
            }

            var finalCode = @$"
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

public static class __evalcompile__
{{
    {rawCode}
}}";

            await Execute(ctx, finalCode);
        }

        [Command("eval")]
        [Description("Evaluate C# code blocks.")]
        [RequireOwner]
        public async Task EvalCommandAsync(CommandContext ctx, [RemainingText] string code)
        {
            var rawCode = code;
            if (rawCode.StartsWith("```"))
            {
                rawCode = rawCode.Remove(0, rawCode.IndexOf('\n'));
                rawCode = rawCode.Remove(rawCode.LastIndexOf('\n'));
            }

            var finalCode = @$"
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

public static class __evalcompile__
{{
    public static async Task Eval(CommandContext ctx)
    {{
        {rawCode}
    }}
}}";
            await Execute(ctx, finalCode);
        }

        private async Task Execute(CommandContext ctx, string finalCode)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(finalCode);
            var systemReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IServiceProvider).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ServiceProvider).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.AddingNewEventArgs).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ConcurrentDictionary<string, string>).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=5.0.0.0").Location),
                MetadataReference.CreateFromFile("DSharpPlus.dll"),
                MetadataReference.CreateFromFile("DSharpPlus.CommandsNext.dll"),
                MetadataReference.CreateFromFile("DSharpPlus.Interactivity.dll"),
                MetadataReference.CreateFromFile("DSharpPlus.Rest.dll"),
                MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(DiscordBot))?.Location ?? "invalid.txt"),
                MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(DiscordGuildConfiguration))?.Location ?? "invalid.txt"),
                systemReference
            };

            var compliation = CSharpCompilation.Create("eval.dll")
                .WithOptions(
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(tree);

            using var ms = new MemoryStream();
            var compilationResult = compliation.Emit(ms);
            if (compilationResult.Success)
            {
                ms.Seek(0, SeekOrigin.Begin);
                using var loadContext = new EvalAssemblyLoadContext();
                Assembly asm = loadContext.LoadFromStream(ms);
                await (Task)asm.GetType("__evalcompile__").GetMethod("Eval").Invoke(null, new object[] { ctx });
            }
            else
            {
                foreach (Diagnostic codeIssue in compilationResult.Diagnostics)
                {
                    string issue = $@"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()},
                        Location: { codeIssue.Location.GetLineSpan()},
                        Severity: { codeIssue.Severity}
                                ";

                    await ctx.RespondAsync(issue);
                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                }
            }
        }

        public class EvalAssemblyLoadContext : AssemblyLoadContext, IDisposable
        {
            private bool disposedValue;

            public EvalAssemblyLoadContext() : base(isCollectible: true) { }
            protected override Assembly? Load(AssemblyName assemblyName) => base.Load(assemblyName);

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects)
                        Unload();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~EvalAssemblyLoadContext()
            // {
            //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
