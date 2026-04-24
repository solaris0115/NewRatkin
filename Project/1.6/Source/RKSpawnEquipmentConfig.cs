using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// RK Equipment Spawn 디버그 도구 설정 (품질, 아이템 체크박스). 게임 재시작 후에도 유지됨.
    /// </summary>
    public class RKSpawnEquipmentConfig : IExposable
    {
        private static readonly string ConfigFilePath =
            Path.Combine(GenFilePaths.ConfigFolderPath, "Ratkin_DebugSpawnConfig.xml");

        private static readonly string[] DefaultEnabledDefNames =
        {
            "RK_Apparel_SpaceArmor",
            "RK_Apparel_SpaceArmorHelmet",
            "RK_HeavyShield",
            "RK_TowerShield",
            "RK_Weapon_Gunlance",
        };

        public QualityCategory quality = QualityCategory.Normal;
        public List<string> enabledDefNames = new List<string>(DefaultEnabledDefNames);

        public void ExposeData()
        {
            Scribe_Values.Look(ref quality, "quality", QualityCategory.Normal, false);
            Scribe_Collections.Look(ref enabledDefNames, "enabledDefNames", LookMode.Value);
            if (enabledDefNames == null)
            {
                enabledDefNames = new List<string>(DefaultEnabledDefNames);
            }
        }

        public static RKSpawnEquipmentConfig Load()
        {
            var config = new RKSpawnEquipmentConfig();
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    Scribe.loader.InitLoading(ConfigFilePath);
                    try
                    {
                        Scribe_Deep.Look(ref config, "RKSpawnEquipmentConfig", Array.Empty<object>());
                    }
                    finally
                    {
                        Scribe.loader.FinalizeLoading();
                    }
                }
            }
            catch (Exception ex)
            {
                RatkinLimitedLog.Warning(RatkinLogKeys.RKSpawnConfig_LoadFailed, $"[Ratkin] Failed to load debug spawn config: {ex.Message}");
                config = new RKSpawnEquipmentConfig();
            }

            if (config == null)
            {
                config = new RKSpawnEquipmentConfig();
            }

            return config;
        }

        public void Write()
        {
            try
            {
                Scribe.saver.InitSaving(ConfigFilePath, "RKSpawnEquipmentConfig");
                try
                {
                    var config = this;
                    Scribe_Deep.Look(ref config, "RKSpawnEquipmentConfig", Array.Empty<object>());
                }
                finally
                {
                    Scribe.saver.FinalizeSaving();
                }
            }
            catch (Exception ex)
            {
                RatkinLimitedLog.Warning(RatkinLogKeys.RKSpawnConfig_SaveFailed, $"[Ratkin] Failed to save debug spawn config: {ex.Message}");
            }
        }
    }
}
