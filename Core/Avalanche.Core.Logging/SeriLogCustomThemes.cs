namespace Avalanche.Core.Logging;

public class SeriLogCustomThemes
{
    public static SystemConsoleTheme SetupCustomSeriLogThemeStyles()
    {
        Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle> customThemeStyles =
            new()
            {
             { ConsoleThemeStyle.Text , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green } },
             { ConsoleThemeStyle.SecondaryText , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Yellow } },
             { ConsoleThemeStyle.TertiaryText , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Cyan } },
             { ConsoleThemeStyle.Invalid , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Red } },
             { ConsoleThemeStyle.Null , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green } },
             { ConsoleThemeStyle.Name , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Yellow } },
             { ConsoleThemeStyle.String , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Cyan } },
             { ConsoleThemeStyle.Number , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Red } },
             { ConsoleThemeStyle.Boolean , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green } },
             { ConsoleThemeStyle.Scalar , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Yellow } },
             { ConsoleThemeStyle.LevelVerbose , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Cyan } },
             { ConsoleThemeStyle.LevelDebug , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Red } },
             { ConsoleThemeStyle.LevelInformation , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green } },
             { ConsoleThemeStyle.LevelWarning , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Yellow } },
             { ConsoleThemeStyle.LevelError , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Cyan } },
             { ConsoleThemeStyle.LevelFatal , new SystemConsoleThemeStyle { Foreground = ConsoleColor.Red } },
            };
        return new SystemConsoleTheme(customThemeStyles);
    }
}
