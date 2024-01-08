using Exiled.API.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RemoteAdminLimits;

public class Config : IConfig
{
    public bool IsEnabled { get; set; } = true;
    public bool Debug { get; set; } = false;

    public bool BanOneAtaTime { get; set; } = true;
    
    [Description("Группы у которых в конце раунда отбирается админка. Вписанные сюда UserId получают иммунитет")]
    public string[] RoundEndRemoveGroups { get; set; } =
    {
        "gross", 
        "vlast",
        "centur",
        "mason",
        "admin",
        "76561199436519835@steam", //max
    };

    [Description($"Группы, которые не смогут использовать команды не указанные в {nameof(Limits)}")]
    public string[] OnlyLimitGroups { get; set; } = { "gross", "vlast", "centur", "mason", };

    [Description($"Команды, которые можно использовать бесконечно, даже группам из {nameof(OnlyLimitGroups)}")]
    public string[] UnlimitedCommands { get; set; } = { "rtime", };

    [Description($"Лимиты изпользования команд для групп. Если в лимитах группы нету определенной команды, и группы нет в {nameof(OnlyLimitGroups)} то ее можно будет использовать бесконечно")]
    public Dictionary<string, Dictionary<string, int>> Limits { get; set; } = new()
    {
        {
            "mason", new()
            {
                { "give", 2 },
                { "fc", 1 },
            }
        },
        {
            "centur", new()
            {
                { "give", 3 },
                { "forceclass", 2 },
                { "respawn_ci|respawn_mtf", 1 },
                { "broadcast", 5 },
            }
        },
        {
            "vlast", new()
            {
                { "give", 5 },
                { "givecustom", 1 },
                { "fc", 3 },
                { "respawn_ci|respawn_mtf", 2 },
                { "broadcast", 10 },
                { "cassie", 5 },
            }
        },
        {
            "gross", new()
            {
                { "give", 8 },
                { "givecustom", 1 },
                { "fc", 6 },
                { "respawn_ci|respawn_mtf", 3 },
                { "broadcast", 10 },
                { "cassie", 5 },
            }
        },
        {
            "admin", new()
            {
                { "ban|oban", 3 },
                { "rlim", 0 },
            }
        },
    };
}