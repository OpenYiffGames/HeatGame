using PatreonPatcher.Core;
using PatreonPatcher.Core.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.CommandLine;
using PatreonPatcher.Cli;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using PatreonPatcher.Cli.Commands;

[RequiresDynamicCode("Calls PatreonPatcher.src.Patcher.PatchAsync()")]
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var platformID = Environment.OSVersion.Platform;

        var rootCommand = SetupRootCommand(platformID);

        var cmdBuilder = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelp()
            .UseExceptionHandler(async (e, ctx) =>
            {
                bool cliMode = args.Length > 0;
                await ExceptionHandler(ctx.Console, e, waitUserInput: !cliMode);
            });

        if (platformID == PlatformID.Win32NT)
        {
            cmdBuilder.AddWindowsFileDialogMiddleware();
        }

        var parser = cmdBuilder.Build();
        return await parser.InvokeAsync(args);
    }

    private static PatchCommand SetupRootCommand(PlatformID platformID)
    {
        var rootCommand = new PatchCommand(platformID);
        return rootCommand;
    }

    private static async Task ExceptionHandler(IConsole console, Exception e, bool waitUserInput = false)
    {
        Logger.Error("An error occurred while patching: " + e.Message);
        console.WriteLine("=================| STACK TRACE |=================");
        console.WriteLine(e.StackTrace ?? "");
        console.WriteLine("=================================================\n\n");
        CliHelpers.WriteErrorMessage(console);
        if (waitUserInput)
        {
            await CliHelpers.WaitForUserInputAsync();
        }
    }
}