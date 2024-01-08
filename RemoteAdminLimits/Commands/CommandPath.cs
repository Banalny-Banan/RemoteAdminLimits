using System;
using System.Linq;
using CommandSystem;
using CorePlugin;
using Exiled.API.Features;

namespace RemoteAdminLimits.Commands;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class CommandPath : ICommand
{
    public string Command { get; } = "CommandPath";

    public string Description { get; } = $"Finds path to the command ({nameof(RemoteAdminLimits)})";

    public string[] Aliases { get; } = { "cph" };
    

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count < 1)
        {
            response = $"Enter command name";
            return false;
        }
        response = $"\"{string.Join(' ', arguments)}\" => \"{Helpers.GetPathToCommand(arguments.At(0))}\"";
        return true;
    }
}