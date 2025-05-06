using PatreonPatcher.Core.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.CommandLine;
using PatreonPatcher.Cli;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using PatreonPatcher.Cli.Commands;
using System.CommandLine.IO;
using PatreonPatcher.Core;
using PatreonPatcher.Core.Logging;

[RequiresDynamicCode("Calls PatreonPatcher.src.Patcher.PatchAsync()")]
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var console = new SystemConsole();
        var logFile = Path.Combine(
            Utils.GetLocalStorageDirectory(), 
            Constants.DefaultLogFileName
        );
        var consoleLogSink = new SystemConsoleLogSink(console)
        {
            LogLevel = LogLevel.Info
        };
        using var logger = new Logger()
            .AddFileLogging(logFile, maxSizeMB: 10, logLevel: LogLevel.Debug)
            .AddOutputSink(consoleLogSink);

        Log.Logger = logger;

        var platformID = Environment.OSVersion.Platform;

        var rootCommand = SetupRootCommand(platformID);

        var cmdBuilder = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelp()
            .UseExceptionHandler(async (e, ctx) =>
            {
                Log.Debug($"Exception caught in the exception handler: [{e}]");
                bool cliMode = args.Length > 0;
                await ExceptionHandler(ctx.Console, e, waitUserInput: !cliMode);
            });

        cmdBuilder.AddLoggingMidleware(consoleLogSink);

        if (platformID == PlatformID.Win32NT)
        {
            cmdBuilder.AddWindowsFileDialogMiddleware();
        }

        var parser = cmdBuilder.Build();
        return await parser.InvokeAsync(args, console);
    }

    private static PatchCommand SetupRootCommand(PlatformID platformID)
    {
        var rootCommand = new PatchCommand(platformID);
        return rootCommand;
    }

    private static async Task ExceptionHandler(IConsole console, Exception e, bool waitUserInput = false)
    {
        Log.Error("An error occurred while patching: " + e.Message);
        console.WriteLine("=================| STACK TRACE |=================");
        console.WriteLine(e.StackTrace ?? "");
        console.WriteLine("=================================================\n\n");
        CliHelpers.WriteErrorMessage(console);
        if (waitUserInput)
        {
            await CliHelpers.WaitForUserInputAsync();
        }
    }

    private static void AddLoggingMidleware(this CommandLineBuilder builder, SystemConsoleLogSink console)
    {
        var verboseOption = new Option<bool>("--verbose", "Enable verbose logging")
        {
            ArgumentHelpName = "verbose"
        };
        builder.Command.AddGlobalOption(verboseOption);

        builder.AddMiddleware((context) =>
        {
            var verbose = context.ParseResult.GetValueForOption<bool>(verboseOption);
            if (verbose)
            {
                console.LogLevel = LogLevel.Debug;
                Log.Debug("Verbose logging enabled.");
            }
            var symbol = context.ParseResult.CommandResult.Symbol;
            Log.Debug($"[CLI pipeline] command result symbol: {symbol.Name}");
            Log.Debug("[CLI pipeline] command result tokens: {0}", context.ParseResult.CommandResult.Tokens);
        });
    }
}