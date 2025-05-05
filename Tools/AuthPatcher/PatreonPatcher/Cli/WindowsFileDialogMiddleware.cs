using PatreonPatcher.Cli.Commands;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

static class WindowsFileDialogMiddleware
{
    public static void AddWindowsFileDialogMiddleware(this CommandLineBuilder builder)
    {
        builder.AddMiddleware(new System.CommandLine.Invocation.InvocationMiddleware(async (context, next) =>
        {
            var result = context.ParseResult;
            if (result.Errors.Count > 0)
            {
                await next(context);
                return;
            }

            var tokens = context.ParseResult.RootCommandResult.Tokens;

            if (tokens.Count == 0)
            {
                var ct = context.GetCancellationToken();
                var gameExecutable = await CliHelpers.WaitUserSelectGameExecutable(ct);
                if (gameExecutable is null)
                {
                    context.ExitCode = 1;
                    return;
                }
                var gameExeArg = context.ParseResult.RootCommandResult.Command.Arguments[0];
                gameExeArg.Arity = ArgumentArity.ExactlyOne;
                result = gameExeArg.Parse([gameExecutable, PatchCommand.CliModeOptionName, "false"]);
                context.ParseResult = result;
            }
            await next(context);
        }));
    }
}
