using PatreonPatcher.Core;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PatreonPatcher.Cli.Commands;

[RequiresDynamicCode("Calls PatreonPatcher.src.Patcher.PatchAsync()")]
internal class PatchCommand : RootCommand, ICommandHandler
{
    public const string CliModeOptionName = "--cli";

    private readonly Option<bool> cliModeOption = new(
        name: CliModeOptionName,
        description: "Used internally on windows to signal if not running in CLI mode (no GUI)",
        getDefaultValue: () => true)
    {
        IsRequired = false,
        IsHidden = true
    };

    private readonly Argument<FileInfo?> gameExecutableArgument = new()
    {
        Name = "gameExecutable",
        Description = "The path to the game executable.",
    };

    public PatchCommand(PlatformID platformID) : base("")
    {
        AddArgument(gameExecutableArgument);
        AddOption(cliModeOption);
        AddValidator(CommandValidator);
        Handler = this;
        
        var arity = platformID switch
        {
            PlatformID.Win32NT => ArgumentArity.ZeroOrOne,
            PlatformID.Unix => ArgumentArity.ExactlyOne,
            _ => ArgumentArity.ExactlyOne
        };
        var workingDirectory = Environment.CurrentDirectory;
        gameExecutableArgument.Arity = arity;
        gameExecutableArgument.Completions.Add(new DirectoryFilesCompletionSource(new DirectoryInfo(workingDirectory))
        {
            MatchFilterPredicate = (file) =>
                CliHelpers.IsValidUnityDirectory(file.FullName),
            FileFilter = platformID switch
            {
                PlatformID.Win32NT => "*.exe",
                PlatformID.Unix => "*",
                _ => "*"
            }
        });
    }

    public int Invoke(InvocationContext context)
    {
        return SyncWithAsync(context);
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var gameExecutable = context.ParseResult.GetValueForArgument(gameExecutableArgument)
                ?? throw new InvalidOperationException("Game executable is null.");
        var console = context.Console;
        Patcher patcher = Patcher.Create(gameExecutable.FullName);
        bool ok = await patcher.PatchAsync();
        if (ok)
        {
            CliHelpers.WriteDoneMessage(console);
        }
        else
        {
            CliHelpers.WriteErrorMessage(console);
        }
        bool cliMode = context.ParseResult.GetValueForOption(cliModeOption);
        if (!cliMode)
        {
            await CliHelpers.WaitForUserInputAsync();
        }
        return ok ? 0 : 1;
    }

    private void CommandValidator(CommandResult command)
    {
        var gameExecutable = command.GetValueForArgument(gameExecutableArgument);
        if (gameExecutable is null)
        {
            return;
        }
        if (!gameExecutable.Exists)
        {
            command.ErrorMessage = $"The file '{gameExecutable.FullName}' does not exist.";
        }
        if (!CliHelpers.IsValidUnityDirectory(gameExecutable.DirectoryName ?? gameExecutable.FullName))
        {
            command.ErrorMessage = $"The file '{gameExecutable.FullName}' is not a valid Unity executable.";
        }
    }

    private int SyncWithAsync(InvocationContext context)
    {
        var task = InvokeAsync(context);
        return task.GetAwaiter().GetResult();
    }
}
