using PatreonPatcher;
using PatreonPatcher.Helpers;
using System.Diagnostics.CodeAnalysis;

[RequiresDynamicCode("Calls PatreonPatcher.src.Patcher.PatchAsync()")]
internal class Program
{
    private static async Task Main(string[] args)
    {
        string? gameDirectory = null;
        if (args.Length == 1)
        {
            string path = args[0];
            if (IsValidUnityDirectory(path))
            {
                gameDirectory = Path.GetDirectoryName(path);
            }
        }

        if ((gameDirectory ??= WaitUserSelectGameExecutable()) is null)
        {
            Logger.Error("Operation canceled by user.");
            return;
        }

        try
        {
            var patcher = Patcher.Create(gameDirectory!);
            var ok = await patcher.PatchAsync();
            if (ok)
            {
                ShowDoneMessage();
            }
            else
            {
                ShowErrorMessage();
            }
        }
        catch (Exception e)
        {
            Logger.Error("An error occurred while patching: " + e.Message);
            Console.WriteLine("=================| STACK TRACE |=================");
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("=================================================\n\n");
            ShowErrorMessage();

#if DEBUG
            throw;
#endif
        }
    }

    static bool IsValidUnityDirectory(string path)
    {
        if (Path.IsPathFullyQualified(path))
            path = Path.GetDirectoryName(path) ?? string.Empty;

        return File.Exists(Path.Combine(path, Constants.UnityPlayerAssembly));
    }

    static string? WaitUserSelectGameExecutable()
    {
        string? gameExecutable;
        do
        {
            gameExecutable = WindowsUtils.ShowOpenFileDialog($"AntroHeat.exe\0*.exe\0");

            if (gameExecutable == null || !IsValidUnityDirectory(gameExecutable))
            {
                if (!WindowsUtils.ShowOkCancelMessageBox("Please select the game executable or press cancel.", "Invalid game executable"))
                {
                    return null;
                }
                gameExecutable = null;
            }
        } while (gameExecutable == null);

        return gameExecutable;
    }

    public static void ShowDoneMessage()
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(
@"
 ____                   _ 
|  _ \  ___  _ __   ___| |
| | | |/ _ \| '_ \ / _ \ |
| |_| | (_) | | | |  __/_|
|____/ \___/|_| |_|\___(_)
");
        Console.ForegroundColor = prevColor;
        Console.WriteLine("Patching successful!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    public static void ShowErrorMessage()
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(
@"
   _____                      _   _     _                                   _                                     
  / ____|                    | | | |   (_)                                 | |                                    
 | (___   ___  _ __ ___   ___| |_| |__  _ _ __   __ _   __      _____ _ __ | |_   __      ___ __ ___  _ __   __ _ 
  \___ \ / _ \| '_ ` _ \ / _ \ __| '_ \| | '_ \ / _` |  \ \ /\ / / _ \ '_ \| __|  \ \ /\ / / '__/ _ \| '_ \ / _` |
  ____) | (_) | | | | | |  __/ |_| | | | | | | | (_| |   \ V  V /  __/ | | | |_    \ V  V /| | | (_) | | | | (_| |
 |_____/ \___/|_| |_| |_|\___|\__|_| |_|_|_| |_|\__, |    \_/\_/ \___|_| |_|\__|    \_/\_/ |_|  \___/|_| |_|\__, |
                                                 __/ |                                                      __/ |
                                                |___/                                                      |___/ 
");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Sorry for the inconvenience, but the game could not be patched.");
        Console.WriteLine("If you believe this is an error, please open an issue at:");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("https://github.com/OpenYiffGames/HeatGame/issues");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Press any key to exit...");
        Console.ForegroundColor = prevColor;
        Console.ReadKey();
    }
}
