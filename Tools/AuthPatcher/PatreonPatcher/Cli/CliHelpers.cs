
using System.CommandLine;
using System.CommandLine.IO;
using System.Runtime.InteropServices;
using PatreonPatcher.Core;
using PatreonPatcher.Core.Helpers;

static class CliHelpers
{
    public static bool IsValidUnityDirectory(string path)
    {
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path) ?? string.Empty;
        }

        return File.Exists(Path.Combine(path, Constants.UnityPlayerAssembly));
    }

    public static async Task<string?> WaitUserSelectGameExecutable(CancellationToken ct = default)
    {
        // check if OS is windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger.Error("This feature is only available on Windows.");
            throw new PlatformNotSupportedException("This feature is only available on Windows.");
        }
        var task = Task.Run(() =>
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
        }, ct);
        return await task;
    }

    public static Task WaitForUserInputAsync()
    {
        if (Console.IsOutputRedirected || Console.IsErrorRedirected)
        {
            return Task.CompletedTask;
        }
        return Task.Run(() =>
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        });
    }

    public static void WriteErrorMessage(IConsole console)
    {
        console.WriteLine(
@"
   _____                      _   _     _                                   _                                     
  / ____|                    | | | |   (_)                                 | |                                    
 | (___   ___  _ __ ___   ___| |_| |__  _ _ __   __ _   __      _____ _ __ | |_   __      ___ __ ___  _ __   __ _ 
  \___ \ / _ \| '_ ` _ \ / _ \ __| '_ \| | '_ \ / _` |  \ \ /\ / / _ \ '_ \| __|  \ \ /\ / / '__/ _ \| '_ \ / _` |
  ____) | (_) | | | | | |  __/ |_| | | | | | | | (_| |   \ V  V /  __/ | | | |_    \ V  V /| | | (_) | | | | (_| |
 |_____/ \___/|_| |_| |_|\___|\__|_| |_|_|_| |_|\__, |    \_/\_/ \___|_| |_|\__|    \_/\_/ |_|  \___/|_| |_|\__, |
                                                 __/ |                                                      __/ |
                                                |___/                                                      |___/ 
", ConsoleColor.Red);
        console.WriteLine("Sorry for the inconvenience, but the game could not be patched.");
        console.WriteLine("If you believe this is an error, please open an issue at:");
        console.WriteLine("https://github.com/OpenYiffGames/HeatGame/issues", ConsoleColor.Blue);
    }

    public static void WriteDoneMessage(IConsole console)
    {
        console.WriteLine(
@"
 ____                   _ 
|  _ \  ___  _ __   ___| |
| | | |/ _ \| '_ \ / _ \ |
| |_| | (_) | | | |  __/_|
|____/ \___/|_| |_|\___(_)
", ConsoleColor.Green);
        console.WriteLine("Patching successful!");
    }
}