using Exiled.API.Features;
using System;
using System.Collections.Generic;
using CorePlugin.Modules;


namespace RemoteAdminLimits;

public class Plugin : Plugin<Config>
{
    public override string Prefix => "RemoteAdminLimits";
    public override string Name => Prefix;
    public override string Author => "Banalny_Banan";
    public override Version Version { get; } = new(1, 0, 0);
    public static Plugin Instance;
    
    internal List<PluginModule> Modules = new()
    {
        new UsageRecorder()
    };
    
    public override void OnEnabled()
    {
        Instance = this;
        foreach (var module in Modules)
        {
            module.EnableSafely();
        }
        
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        Instance = null;
        foreach (var module in Modules)
        {
            module.DisableSafely();
        }
        base.OnDisabled();
        
    }

}