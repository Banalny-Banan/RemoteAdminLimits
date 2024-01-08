using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using CorePlugin;
using Exiled.API.Features;

namespace RemoteAdminLimits.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ViewLimits : ICommand, IUsageProvider
{
    public string Command { get; } = "ViewLimits";

    public string Description { get; } = "Показывает доступные команды";

    public string[] Aliases { get; } = { "VL", };

    public string[] Usage { get; } = { "Игрок", };

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (sender.GetPlayerSender(arguments) is not Player player)
        {
            response = "Player not found";
            return false;
        }
        
        bool isMe = player == Player.Get(sender);

        if (player.GetAdminGroup() is not string group)
        {
            response = isMe ? "Не получилось найти вашу группу. Странно..." : $"Не получилось найти группу игрока {player.Nickname}. Странно...";
            return false;
        }

        response = $"Ваша группа: {group}\n";

        if (Plugin.Instance.Config.OnlyLimitGroups.Contains(group))
        {
            response += isMe ? "Вы можете использовать следующие команды:\n" : $"Игрок {player.Nickname} может использовать следующие команды:\n";

            response += string.Join("\n", UsageRecorder.Limits[group].Keys.Select(command => $"{string.Join("/", command).PaintByHash()} - {UsageRecorder.Limits[group][command]} (Осталось: {Helpers.GetRemainingUsages(group, command, player)})"));

            response += "\n" + string.Join("\n", UsageRecorder.UniqUnlimitedCommands.Select(command => $"{string.Join("/", command).PaintByHash()} - ∞"));
        }
        else
        {
            response += isMe ? "Вы можете использовать все команды" : $"Игрок {player.Nickname} может использовать все команды";

            if (Plugin.Instance.Config.Limits.ContainsKey(group))
                response += $"\nНо на некоторые команды есть ограничения:\n{string.Join("\n", Plugin.Instance.Config.Limits[group].Keys.Select(command => $"{command.PaintByHash()} - {Plugin.Instance.Config.Limits[group][command]} (Осталось: {Helpers.GetRemainingUsages(group, command.Contains("|") ? command.Split('|')[0] : command, player)})"))}";
        }

        return true;
    }
}