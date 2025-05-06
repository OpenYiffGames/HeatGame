using PatreonPatcher.Cli.Commands;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

internal static class WindowsFileDialogMiddleware
{
    public static void AddWindowsFileDialogMiddleware(this CommandLineBuilder builder)
    {
        _ = builder.AddMiddleware(new System.CommandLine.Invocation.InvocationMiddleware(async (context, next) =>
        {
            ParseResult result = context.ParseResult;
            if (result.Errors.Count > 0)
            {
                await next(context);
                return;
            }

            IReadOnlyList<Token> tokens = context.ParseResult.RootCommandResult.Tokens;

            if (tokens.Count == 0)
            {
                CancellationToken ct = context.GetCancellationToken();
                string? gameExecutable = await CliHelpers.WaitUserSelectGameExecutable(ct);
                if (gameExecutable is null)
                {
                    context.ExitCode = 1;
                    return;
                }
                Argument gameExeArg = context.ParseResult.RootCommandResult.Command.Arguments[0];
                gameExeArg.Arity = ArgumentArity.ExactlyOne;
                result = gameExeArg.Parse([gameExecutable, PatchCommand.CliModeOptionName, "false"]);
                context.ParseResult = result;
            }
            await next(context);
        }));
    }
}
