using System;
using System.Collections.Generic;
using CommandSystem;

namespace RemoteAdminLimits;

public static class CustomCommandHandlers
{
    public delegate bool CustomCommandHandler(string command, ArraySegment<string> arguments, ICommandSender sender, out string response);
    
    public static Dictionary<string[], CustomCommandHandler> Handlers = new()
    {
        
    };
    
    public static bool TryHandle(string command, ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        foreach (var handlerMatch in Handlers)
        {
            if (handlerMatch.Key.Contains(command.ToLower()))
            {
                return handlerMatch.Value(command, arguments, sender, out response);
            }
        }

        response = null;
        return false;
    }
}