using System;
using CommandSystem;
using CorePlugin;
using Exiled.API.Features;

namespace RemoteAdminLimits.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ResetLimit : ICommand, IUsageProvider
{
    public string Command { get; } = "ResetLimit";

    public string Description { get; } = "Сбрасывает лимит команд для игрока";

    public string[] Aliases { get; } = { "rlim" };

    public string[] Usage { get; } = { "Игрок" };

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.SetGroup))
        {
            response = "Недостаточно прав";
            return false;
        }
        
        if (sender.GetPlayerSender(arguments) is not Player player)
        {
            response = "Player not found";
            return false;
        }
        
        if(UsageRecorder.Usages.ContainsKey(player))
            UsageRecorder.Usages.Remove(player);
        response = $"Лимит команд игрока {player.Nickname} сброшен";
        return true;
    }
}