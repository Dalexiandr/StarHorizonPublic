using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.CCVar
{
    [CVarDefs]
    public sealed class HorizonCCVars : CVars
    {
        /*
         * Barks (Звуки речи)
         */
        public static readonly CVarDef<bool> BarksEnabled =
            CVarDef.Create("barks.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMaxPitch =
            CVarDef.Create("barks.max_pitch", 1.5f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMinPitch =
            CVarDef.Create("barks.min_pitch", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMinDelay =
            CVarDef.Create("barks.min_delay", 0.1f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMaxDelay =
            CVarDef.Create("barks.max_delay", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ReplaceTTSWithBarks =
            CVarDef.Create("barks.replace_tts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksVolume =
            CVarDef.Create("barks.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        ///     URL of the Discord webhook which will relay bans.
        /// </summary>
        public static readonly CVarDef<string> DiscordBanWebhook =
            CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

        public static readonly CVarDef<bool> EnableCustomFonts =
            CVarDef.Create("lang.enable_fonts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        ///     Paths to data
        /// </summary>
        public static readonly CVarDef<string> ShutdownTimersPath =
            CVarDef.Create("paths.default_shutdown_path", "shutdown_timers.yml", CVar.SERVERONLY | CVar.CONFIDENTIAL);

        public static readonly CVarDef<bool> ShutdownEnabled =
            CVarDef.Create("paths.shutdown_enabled", false, CVar.SERVERONLY);

        /// <summary>
        ///     Path to discord_sponsors.txt file
        /// </summary>
        public static readonly CVarDef<string> SponsorSystemDiscordSponsorsPath =
            CVarDef.Create("sponsor.discord_sponsors_path", "sponsorSystem/warp/discord_sponsors.txt", CVar.SERVERONLY);

        /// <summary>
        ///     Path to disposable.txt file
        /// </summary>
        public static readonly CVarDef<string> SponsorSystemDisposablePath =
            CVarDef.Create("sponsor.disposable_path", "sponsorSystem/warp/disposable.txt", CVar.SERVERONLY);

        /// <summary>
        ///     Path to sponsor_items.txt file
        /// </summary>
        public static readonly CVarDef<string> SponsorSystemItemsPath =
            CVarDef.Create("sponsor.items_path", "sponsorSystem/sponsor_items.txt", CVar.SERVERONLY);

        /*
         * Очистка мусора (Trash Cleanup)
         */

        /// <summary>
        /// Включена ли автоматическая очистка мусора.
        /// </summary>
        public static readonly CVarDef<bool> TrashCleanupEnabled =
            CVarDef.Create("trash.cleanup_enabled", true, CVar.SERVERONLY);

        /// <summary>
        /// Интервал в секундах между очистками мусора.
        /// </summary>
        public static readonly CVarDef<float> TrashCleanupInterval =
            CVarDef.Create("trash.cleanup_interval", 600f, CVar.SERVERONLY);

        /// <summary>
        /// Задержка в секундах после начала раунда перед активацией очистки мусора.
        /// </summary>
        public static readonly CVarDef<float> TrashCleanupStartDelay =
            CVarDef.Create("trash.cleanup_start_delay", 600f, CVar.SERVERONLY);

        /// <summary>
        /// Включение/отключение автоматического удаления мелких гридов.
        /// </summary>
        public static readonly CVarDef<bool> AutoGridCleanupEnabled =
            CVarDef.Create("shuttle.grid_cleanup_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);

        /// <summary>
        /// Включение/отключение автоматического удаления брошенных шаттлов.
        /// </summary>
        public static readonly CVarDef<bool> AutoDeleteEnabled =
            CVarDef.Create("shuttle.autodelete_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE,
                "Отключить или включить автоудаление шаттлов.");
        /*
         * Планетки
         */
        public static readonly CVarDef<bool> SpawnPlanets =
            CVarDef.Create("game.spawn_roundstart_planets", false, CVar.SERVERONLY);

    }
}
