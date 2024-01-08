using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using JetBrains.Annotations;
using RemoteAdmin;

namespace RemoteAdminLimits;

public static class Helpers
{
    public static List<ParentCommand> GetAllParentCommands()
    {
        List<ParentCommand> parentCommands = new();
        foreach (var command in CommandProcessor.RemoteAdminCommandHandler.AllCommands)
        {
            if (command is ParentCommand parentCommand)
                AddParentCommands(parentCommand);
        }

        void AddParentCommands(ParentCommand parentCommand)
        {
            parentCommands.Add(parentCommand);
            foreach (var subCommand in parentCommand.AllCommands)
            {
                if (subCommand is ParentCommand subParentCommand)
                {
                    AddParentCommands(subParentCommand);
                }
            }
        }

        return parentCommands;
    }

    public static string GetImportantCommand(string query, out int index)
    {
        string ret = null;
        string[] strArray = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (strArray.Length == 0)
        {
            Log.Debug($"GetImportantCommand(\"{query}\") => \"[null]\" [Length 0]");
            index = -1;
            return null;
        }

        index = 0;
        string baseCommand = strArray[0];
        ArraySegment<string> arguments = new(strArray, 1, strArray.Length - 1);
        if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(baseCommand, out var command))
        {
            while (command is ParentCommand parentCommand && arguments.Count > 0)
            {
                if (parentCommand.TryGetCommand(arguments.Array[arguments.Offset], out var subCommand))
                {
                    command = subCommand;
                    arguments = new ArraySegment<string>(arguments.Array, arguments.Offset + 1, arguments.Count - 1);
                }
                else
                {
                    break;
                }
            }

            index = arguments.Offset-1;
            ret = command.Command;
        }

        Log.Debug($"GetImportantCommand(\"{query}\") => \"{ret ?? "[null]"} (Index: {index})\"");

        return ret?.ToLower();
    }

    public static int GetRemainingUsages(string group, string command, Player player)
    {
        if (UsageRecorder.Limits[group].Keys.FirstOrDefault(cmd => cmd.Contains(command.ToLower())) is not string[] aliases)
        {
            Log.Error($"Can't find command \"{command}\" in group \"{group}\". commands: {string.Join(", ", UsageRecorder.Limits[group].Keys.Select(cmd => string.Join("/", cmd)))}");
            return int.MaxValue;
        }

        return GetRemainingUsages(group, aliases, player);
    }

    public static int GetRemainingUsages(string group, string[] aliases, Player player) => UsageRecorder.Limits[group][aliases] - (UsageRecorder.Usages.ContainsKey(player) && UsageRecorder.Usages[player].ContainsKey(aliases) ? UsageRecorder.Usages[player][aliases] : 0);

    [CanBeNull] public static string GetPathToCommand(string command)
        // Если команда содержит +, значит указан полный путь
        => command.Contains('+') ? FindExactPath(CommandProcessor.RemoteAdminCommandHandler, command.Split('+'), 0) : FindCommandPath(CommandProcessor.RemoteAdminCommandHandler, command);

    private static string FindCommandPath(CommandHandler handler, string query, string pathToCommand = "") // поиск команд по названию
    {
        if (handler.TryGetCommand(query, out var cmd))
        {
            return $"{pathToCommand}{cmd.Command}".ToLower();
        }

        foreach (var subCommand in handler.AllCommands)
        {
            if (subCommand is not ParentCommand parentCommand) continue;

            string result = FindCommandPath(parentCommand, query, $"{pathToCommand}{parentCommand.Command}+");
            if (result != null)
            {
                return result.ToLower();
            }
        }

        return null;
    }

    private static string FindExactPath(CommandHandler handler, string[] parts, int index) // поиск команды по точно указанному пути
    {
        foreach (var subCommand in handler.AllCommands)
        {
            if (subCommand is ParentCommand parentCommand && parentCommand.Command == parts[index])
            {
                // Проверяем, достигли ли мы конца массива частей
                if (index == parts.Length - 1)
                {
                    string ret = string.Join("+", parts).ToLower();
                    return ret;
                }

                // Продолжаем поиск в дочерних командах
                string result = FindExactPath(parentCommand, parts, index + 1);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }
}