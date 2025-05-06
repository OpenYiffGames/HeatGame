using PatreonPatcher.Cli;
using PatreonPatcher.Cli.Commands;
using PatreonPatcher.Core;
using PatreonPatcher.Core.Helpers;
using PatreonPatcher.Core.Logging;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;

[RequiresDynamicCode("Calls PatreonPatcher.src.Patcher.PatchAsync()")]
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        SystemConsole console = new();
        string logFile = Path.Combine(
            Utils.GetLocalStorageDirectory(),
            Constants.DefaultLogFileName
        );
        SystemConsoleLogSink consoleLogSink = new(console)
        {
            LogLevel = LogLevel.Info
        };
        using Logger logger = new Logger()
            .AddFileLogging(logFile, maxSizeMB: 10, logLevel: LogLevel.Debug)
            .AddOutputSink(consoleLogSink);

        Log.Logger = logger;

        PlatformID platformID = Environment.OSVersion.Platform;

        PatchCommand rootCommand = SetupRootCommand(platformID);

        CommandLineBuilder cmdBuilder = new CommandLineBuilder(rootCommand)
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

        Parser parser = cmdBuilder.Build();
        return await parser.InvokeAsync(args, console);
    }

    private static PatchCommand SetupRootCommand(PlatformID platformID)
    {
        PatchCommand rootCommand = new(platformID);
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
        Option<bool> verboseOption = new("--verbose", "Enable verbose logging")
        {
            ArgumentHelpName = "verbose"
        };
        builder.Command.AddGlobalOption(verboseOption);

        _ = builder.AddMiddleware((context) =>
        {
            bool verbose = context.ParseResult.GetValueForOption<bool>(verboseOption);
            if (verbose)
            {
                console.LogLevel = LogLevel.Debug;
                Log.Debug("Verbose logging enabled.");
            }
            Symbol symbol = context.ParseResult.CommandResult.Symbol;
            Log.Debug($"[CLI pipeline] command result symbol: {symbol.Name}");
            Log.Debug("[CLI pipeline] command result tokens: {0}", context.ParseResult.CommandResult.Tokens);
        });
    }
}