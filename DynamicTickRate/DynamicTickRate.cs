using System;
using FrooxEngine;
using ResoniteModLoader;

namespace DynamicTickRate;

public partial class DynamicTickRate : ResoniteMod
{
    public override string Name => "DynamicTickRate";
    public override string Author => "Raidriar796 (+ tuning by troyBORG)";
    public override string Version => "1.1.0";
    public override string Link => "https://github.com/troyBORG/DynamicTickRate";

    public static ModConfiguration? Config;

    private static StandaloneFrooxEngineRunner runner =
        (StandaloneFrooxEngineRunner)Type.GetType("FrooxEngine.Headless.Program, Resonite")!
        .GetField("runner", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
        .GetValue(null)!;

    private static TickController? Controller;

    public override void OnEngineInit()
    {
        Config = GetConfiguration();
        Config?.Save(true);

        if (!ModLoader.IsHeadless || !Config!.GetValue(Enable))
        {
            Msg("DynamicTickRate: headless only. Disable or uninstall for client.");
            return;
        }

        var initial = Config!.GetValue(MinTickRate);
        runner.TickRate = initial;

        Controller = new TickController(
            runner,
            new TickTuning
            {
                // hard caps
                MinTickRate = Config!.GetValue(MinTickRate),
                MaxTickRate = Config!.GetValue(MaxTickRate),

                // linear-ish parts
                AddedTicksPerUser = Config!.GetValue(AddedTicksPerUser),
                AddedTicksPerExtraWorld = Config!.GetValue(AddedTicksPerWorld),

                // active world threshold
                ActiveWorldUserThreshold = Config!.GetValue(ActiveWorldUserThreshold),

                // busy-world shaping
                TopKWorlds = Config!.GetValue(TopKWorlds),
                BusyWorldWeight = Config!.GetValue(BusyWorldWeight),
                PerWorldUserSoftCap = Config!.GetValue(PerWorldUserSoftCap),
                PerWorldDiminish = Config!.GetValue(PerWorldDiminish),

                // burst-join shaping
                JoinRateTicksPerJpm = Config!.GetValue(JoinRateTicksPerJpm),
                JoinWindowSeconds = Config!.GetValue(JoinWindowSeconds),

                // stability
                EmaAlpha = Config!.GetValue(EmaAlpha),
                HysteresisTicks = Config!.GetValue(HysteresisTicks),
                MinChangeIntervalSeconds = Config!.GetValue(MinChangeIntervalSeconds),
                BigJumpThreshold = Config!.GetValue(BigJumpThreshold),
                BigJumpCooldownSeconds = Config!.GetValue(BigJumpCooldownSeconds),

                // logging
                LogOnChange = Config!.GetValue(LogOnChange)
            },
            initial
        );

        // Hook world lifecycle and per-world user events
        Engine.Current.WorldManager.WorldAdded += OnWorldAddedRemoved;
        Engine.Current.WorldManager.WorldAdded += OnUserJoinLeave;

        // Backfill already existing worlds (if any)
        foreach (var w in Engine.Current.WorldManager.Worlds)
            OnWorldAddedRemoved(w);
    }
}
