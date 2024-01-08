using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CorePlugin;
using CorePlugin.CustomEvents;
using CorePlugin.Modules;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using RemoteAdmin;

namespace RemoteAdminLimits;

public class UsageRecorder : PluginModule
{
    public override string Name => "UsageRecorder";

    private static Config Config => Plugin.Instance.Config;

    protected override void SubscribeEvents()
    {
        Handlers.RecivingCommand += OnRecivingCommand;
        Handlers.RecivedCommand += OnRecivedCommand;
        Exiled.Events.Handlers.Server.ReloadedConfigs += LoadConfig;
        Exiled.Events.Handlers.Server.WaitingForPlayers += LoadConfig;
        Exiled.Events.Handlers.Server.RoundEnded += RoundEndRemoveGroups;
    }

    protected override void UnsubscribeEvents()
    {
        Handlers.RecivingCommand -= OnRecivingCommand;
        Handlers.RecivedCommand -= OnRecivedCommand;
        Exiled.Events.Handlers.Server.ReloadedConfigs -= LoadConfig;
        Exiled.Events.Handlers.Server.WaitingForPlayers -= LoadConfig;
        Exiled.Events.Handlers.Server.RoundEnded -= RoundEndRemoveGroups;
    }

    public static Dictionary<string, Dictionary<string[], int>> Limits = new();
    public static Dictionary<Player, Dictionary<string[], int>> Usages = new();
    public static HashSet<string> UnlimitedCommands = new();
    public static HashSet<string> UniqUnlimitedCommands = new();


    private static void RoundEndRemoveGroups(RoundEndedEventArgs ev)
    {
        foreach (var player in Player.List)
        {
            if (player.GetAdminGroup() is not string adminGroup) continue;

            if (Config.RoundEndRemoveGroups.Select(s => s.ToLower()).Contains(adminGroup) && !Config.RoundEndRemoveGroups.Contains(player.UserId))
            {
                player.Group = null;
            }
        }
    }

    private static void LoadConfig()
    {
        Log.SendRaw($"[{nameof(RemoteAdminLimits)}] Loading...", ConsoleColor.Yellow);
        Limits = Config.Limits.ToDictionary(grpLimit => grpLimit.Key.ToLower(), grpLimit => grpLimit.Value.ToDictionary(cmdLimit => cmdLimit.Key.Split('|').Select(cmd => cmd.ToLower()).ToArray(), cmdLimit => cmdLimit.Value));

        LoadLimits();

        LoadUnlimitedCommands();

        Log.SendRaw($"[{nameof(RemoteAdminLimits)}] loaded successfully!", ConsoleColor.Yellow);
    }

    private static void LoadLimits()
    {
        var totalCommands = 0;
        var commandsFound = 0;
        var commandsNotFound = 0;
        var aliasesProduced = 0;

        List<ParentCommand> parentCommands = Helpers.GetAllParentCommands();
        Log.Debug($"Found {parentCommands.Count} parent commands: {string.Join(", ", parentCommands.Select(parentCommand => parentCommand.Command))}");

        Dictionary<string, Dictionary<string[], int>> newLimits = new();
        foreach (KeyValuePair<string, Dictionary<string[], int>> groupLimit in Limits)
        {
            newLimits.Add(groupLimit.Key, new Dictionary<string[], int>());
            foreach (KeyValuePair<string[], int> cmdLimit in groupLimit.Value)
            {
                HashSet<string> variants = new();
                foreach (string cmd in cmdLimit.Key)
                {
                    totalCommands++;
                    if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(cmd, out var command) || parentCommands.FirstOrDefault(parentCommand => parentCommand.TryGetCommand(cmd, out command)) is not null)
                    {
                        commandsFound++;
                        variants.Add(command.Command);
                        if (command.Aliases == null) continue;
                        foreach (string alias in command.Aliases)
                        {
                            variants.Add(alias);
                        }
                    }
                    else
                    {
                        commandsNotFound++;
                        Log.Error($"Command \"{cmd}\" of group \"{groupLimit.Key}\" not found in CommandHandler!");
                    }
                }

                Log.Debug($"Variants for {string.Join("/", cmdLimit.Key)}: {string.Join("/", variants)}");
                aliasesProduced += variants.Count;
                newLimits[groupLimit.Key].Add(variants.ToArray(), cmdLimit.Value);
            }
        }

        foreach (string onlyLimitGroup in Config.OnlyLimitGroups)
        {
            if (newLimits.ContainsKey(onlyLimitGroup)) continue;
            newLimits.Add(onlyLimitGroup, new Dictionary<string[], int>());
        }

        Limits = newLimits;
        if (commandsNotFound > 0)
            Log.SendRaw($"{commandsNotFound}/{totalCommands} commands were not found in CommandHandler", ConsoleColor.Red);
        Log.SendRaw($"{commandsFound}/{totalCommands} commands were found in CommandHandler. {aliasesProduced} aliases produced", ConsoleColor.White);
    }

    private static void LoadUnlimitedCommands()
    {
        var unlimitedCommandsFound = 0;
        var unlimitedCommandsNotFound = 0;
        foreach (string command in Config.UnlimitedCommands)
        {
            if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(command, out var cmd))
            {
                unlimitedCommandsFound++;
                UnlimitedCommands.Add(cmd.Command);
                UniqUnlimitedCommands.Add(cmd.Command);
                if (cmd.Aliases == null) continue;
                foreach (string alias in cmd.Aliases)
                {
                    UnlimitedCommands.Add(alias);
                }
            }
            else
            {
                unlimitedCommandsNotFound++;
                Log.Error($"Command \"{command}\" ({nameof(Config.UnlimitedCommands)}) not found in CommandHandler!");
            }
        }

        if (unlimitedCommandsNotFound > 0)
            Log.SendRaw($"{unlimitedCommandsNotFound}/{Config.UnlimitedCommands.Length} unlimited commands were not found in CommandHandler", ConsoleColor.Red);
        Log.SendRaw($"{unlimitedCommandsFound}/{Config.UnlimitedCommands.Length} unlimited commands were found in CommandHandler. {UnlimitedCommands.Count} aliases produced", ConsoleColor.White);
    }

    private static void OnRecivingCommand(RecivingCommandEventArgs ev)
    {
        if (ev.Player is not Player player) return; //если игрок не найден
        if (player.GetAdminGroup() is not string group) return; //если группа не найдена
        if (Helpers.GetImportantCommand(ev.Query, out int index) is not string command) return; //если команда не найдена
        if (UnlimitedCommands.Contains(command)) return; //если команда не ограничена
        bool isOnlyLimitGroup = Config.OnlyLimitGroups.Contains(group);
        if (Limits.FirstOrDefault(grpLimit => grpLimit.Key == group).Value is not Dictionary<string[], int> groupLimits) return; //если группы нет в лимитах

        if (ev.Command.ToLower() == "ban" && Regex.Match(ev.Query, @"(\d*\.){2,}").Success)
        {
            ev.IsAllowed = false;
            ev.Response = "Банить можно только одного игрока за раз";
            return;
        }

        Log.Debug($"Player {player.Nickname} with group \"{group}\" sent command {command}");

        if (groupLimits.Keys.FirstOrDefault(strings => strings.Contains(command)) is not string[] aliases)
        {
            if (isOnlyLimitGroup && command.ToLower() is not ("vl" or "viewlimits"))
            {
                ev.IsAllowed = false;
                ev.Response = "Вам нельзя использовать эту команду. Используйте команду \"ViewLimits\" (VL) для просмотра доступных команд";
                Log.Debug($"Player {player.Nickname} with group \"{group}\" (OnlyLimitGroup) tried to use command {command} but group not found in limits");
            }

            return;
        } //если команды нет в лимитах

        if (!Usages.ContainsKey(player)) Usages.Add(player, new Dictionary<string[], int>());
        if (!Usages[player].ContainsKey(aliases)) Usages[player].Add(aliases, 0);
        int limit = groupLimits[aliases];

        if (Usages[player][aliases] >= limit)
        {
            ev.IsAllowed = false;
            ev.Response = $"Вы достигли лимита использования команды {string.Join("/", aliases)} ({Usages[player][aliases]}/{limit})";
            Log.Debug($"Player {player.Nickname} with group \"{group}\" reached limit of command {command}({string.Join("/", aliases)}). Usage: {Usages[player][aliases]}/{limit}");
        }
    }

    private static void OnRecivedCommand(RecivedCommandEventArgs ev)
    {
        //Log.Debug($"RaReply: ({ev.RaReply?.Command})-\"{ev.RaReply?.Response}\" Success: {ev.RaReply?.Success}");

        if (Helpers.GetImportantCommand(ev.Query, out _) is not string alias || Helpers.GetPathToCommand(alias) is not string command) return;

        if (ev.Player is not Player player) return;
        if (player.GetAdminGroup() is not string group) return;
        if (Limits.FirstOrDefault(grpLimit => grpLimit.Key == group).Value is not Dictionary<string[], int> groupLimits) return;

        if (groupLimits.Keys.FirstOrDefault(strings => strings.Contains(command)) is not string[] aliases) return;
        if (ev.RaReply is { Success: false, }) return;

        int limit = groupLimits[aliases];
        Usages[player][aliases] += ev.AffectedPlayers;

        var remaining = $"{limit - Usages[player][aliases]}/{limit}";
        Log.Debug($"Player {player.Nickname} with group \"{group}\" used limited command {command}({string.Join("/", aliases)}). It affected {ev.AffectedPlayers} players. Remaining: {remaining}");

        switch (limit - Usages[player][aliases]) //оставшиеся использования
        {
            case >= 100:
            {
                break;
            }
            case > 1:
            {
                ev.Sender.Respond($"{command.ToUpperInvariant()}#Теперь у вас осталось {remaining} использований команды {command}({string.Join("/", aliases)})");
                break;
            }
            case 1:
            {
                ev.Sender.Respond($"{command.ToUpperInvariant()}#Теперь у вас осталось {remaining} использование команды {command}({string.Join("/", aliases)})");
                break;
            }
            case 0:
            {
                ev.Sender.Respond($"{command.ToUpperInvariant()}#У вас больше не осталось использований команды {command}({string.Join("/", aliases)})");
                break;
            }
            case < 0:
            {
                ev.Sender.Respond($"{command.ToUpperInvariant()}#Вы превысили лимит использования команды {command}({string.Join("/", aliases)})");
                break;
            }
        }
    }
}