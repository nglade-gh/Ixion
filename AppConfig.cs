using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Presets de difficulté prédéfinis.
/// </summary>
public enum DifficultyPreset
{
    /// <summary>Facile — ennemis lents et fragiles.</summary>
    Easy,
    /// <summary>Normal — équilibré.</summary>
    Normal,
    /// <summary>Difficile — ennemis nombreux et résistants.</summary>
    Hard,
    /// <summary>Expert — rythme infernal.</summary>
    Expert
}

/// <summary>
/// Liaison clavier ou manette pour une action donnée.
/// </summary>
public class InputBinding
{
    /// <summary>Type de périphérique : Key ou Gamepad.</summary>
    [XmlElement("type")]
    public string Type { get; set; } = "Key";

    /// <summary>Touche ou bouton associé.</summary>
    [XmlElement("key")]
    public string Key { get; set; } = "";

    /// <summary>Index manette (0-3) si type=Gamepad.</summary>
    [XmlElement("gamepadIndex")]
    public int GamepadIndex { get; set; } = 0;
}

/// <summary>
/// Configuration audio (volumes 0.0-1.0).
/// </summary>
public class AudioConfig
{
    [XmlElement("masterVolume")]
    public float MasterVolume { get; set; } = 0.7f;

    [XmlElement("musicVolume")]
    public float MusicVolume { get; set; } = 0.9f;

    [XmlElement("sfxVolume")]
    public float SfxVolume { get; set; } = 0.05f;

    [XmlElement("muted")]
    public bool Muted { get; set; } = false;
}

/// <summary>
/// Configuration vidéo (résolution, fullscreen).
/// </summary>
public class VideoConfig
{
    [XmlElement("width")]
    public int Width { get; set; } = 600;

    [XmlElement("height")]
    public int Height { get; set; } = 800;

    [XmlElement("fullscreen")]
    public bool Fullscreen { get; set; } = false;

    /// <summary>Liste des résolutions supportées.</summary>
    [XmlIgnore]
    public static List<(int W, int H)> SupportedResolutions => new()
    {
        (600, 800),
        (800, 1000),
        (1024, 768),
        (1280, 720),
        (1920, 1080)
    };
}

/// <summary>
/// Paramètres de difficulté. Combiné avec un preset de base et des
/// valeurs de raffinement (-100 à +100 %).
/// </summary>
public class DifficultyConfig
{
    [XmlElement("preset")]
    public DifficultyPreset Preset { get; set; } = DifficultyPreset.Normal;

    // Fine-tuning (-100 to +100 percent modifier)
    [XmlElement("spawnRateModifier")]
    public int SpawnRateModifier { get; set; } = 0;

    [XmlElement("enemyHpModifier")]
    public int EnemyHpModifier { get; set; } = 0;

    [XmlElement("enemySpeedModifier")]
    public int EnemySpeedModifier { get; set; } = 0;

    [XmlElement("playerDamageMultiplier")]
    public float PlayerDamageMultiplier { get; set; } = 1.0f;

    [XmlElement("shieldStartEnergy")]
    public float ShieldStartEnergy { get; set; } = 100f;

    [XmlElement("structureHp")]
    public int StructureHp { get; set; } = 100;

    [XmlElement("laserHeatRateModifier")]
    public int LaserHeatRateModifier { get; set; } = 0;

    [XmlElement("laserCoolingRateModifier")]
    public int LaserCoolingRateModifier { get; set; } = 0;

    [XmlElement("laserIdleCoolMultiplier")]
    public float LaserIdleCoolMultiplier { get; set; } = 2.5f;

    [XmlElement("enemyBulletDamage")]
    public int EnemyBulletDamage { get; set; } = 10;

    [XmlElement("enemyContactDamage")]
    public int EnemyContactDamage { get; set; } = 20;

    // Bonus stat values (default values match bonuses.xml)
    [XmlElement("primaryUpgradeDuration")]
    public float PrimaryUpgradeDuration { get; set; } = 8f;

    [XmlElement("primaryUpgradeDamageMultiplier")]
    public float PrimaryUpgradeDamageMultiplier { get; set; } = 2f;

    [XmlElement("secondaryUpgradeDuration")]
    public float SecondaryUpgradeDuration { get; set; } = 8f;

    [XmlElement("secondaryUpgradeDamageMultiplier")]
    public float SecondaryUpgradeDamageMultiplier { get; set; } = 2f;

    [XmlElement("shieldRepairAmount")]
    public int ShieldRepairAmount { get; set; } = 30;

    [XmlElement("structureRepairAmount")]
    public int StructureRepairAmount { get; set; } = 20;

    [XmlElement("rapidCoolDuration")]
    public float RapidCoolDuration { get; set; } = 20f;

    [XmlElement("diagonalShotDuration")]
    public float DiagonalShotDuration { get; set; } = 15f;

    [XmlElement("powerShotDuration")]
    public float PowerShotDuration { get; set; } = 20f;

    [XmlElement("tripleShotDuration")]
    public float TripleShotDuration { get; set; } = 15f;

    [XmlIgnore]
    public string DifficultyLabel
    {
        get
        {
            if (SpawnRateModifier != 0 || EnemyHpModifier != 0 ||
                EnemySpeedModifier != 0 || PlayerDamageMultiplier != 1.0f ||
                ShieldStartEnergy != 100f || StructureHp != 100 ||
                LaserHeatRateModifier != 0 || LaserCoolingRateModifier != 0 ||
                LaserIdleCoolMultiplier != 2.5f ||
                EnemyBulletDamage != 10 || EnemyContactDamage != 20 ||
                PrimaryUpgradeDuration != 8f || PrimaryUpgradeDamageMultiplier != 2f ||
                SecondaryUpgradeDuration != 8f || SecondaryUpgradeDamageMultiplier != 2f ||
                ShieldRepairAmount != 30 || StructureRepairAmount != 20 ||
                RapidCoolDuration != 20f || DiagonalShotDuration != 15f ||
                PowerShotDuration != 20f || TripleShotDuration != 15f)
                return $"{Preset:F0} (custom)";
            return Preset.ToString();
        }
    }
}

/// <summary>
/// Dictionnaire pour la sérialisation XML des bindings.
/// key = action name, value = InputBinding.
/// </summary>
public class InputConfig
{
    [XmlArray("bindings")]
    [XmlArrayItem("binding")]
    public List<BindingEntry> BindingsList { get; set; } = new();

    [XmlIgnore]
    public Dictionary<string, InputBinding> Bindings => new(BindingsList.ToDictionary(e => e.Key, e => e.Value));

    public void SetBindings(Dictionary<string, InputBinding> dict)
    {
        BindingsList = new List<BindingEntry>();
        foreach (var kv in dict)
            BindingsList.Add(new BindingEntry { Key = kv.Key, Value = kv.Value });
    }

    [XmlElement("deadzone")]
    public float Deadzone { get; set; } = 0.25f;

    /// <summary>Entry helper for serializing a Dictionary.</summary>
    public class BindingEntry
    {
        [XmlElement("key")]
        public string Key { get; set; } = "";
        [XmlElement("value")]
        public InputBinding Value { get; set; } = new();
    }
}

/// <summary>
/// Configuration globale de l'application. Sérialisé en XML
/// vers <c>Config/game.xml</c>.
/// </summary>
public static class AppConfig
{
    private const string ShooterNamespace = "https://www.timc.fr/nicolas-glade/dotnet/shooter";

    private static string ConfigPath =>
        Path.Combine(AppContext.BaseDirectory, "Config", "game.xml");

    private static VideoConfig _video = new();
    private static AudioConfig _audio = new();
    private static DifficultyConfig _difficulty = new();
    private static InputConfig _input = new();

    // Preset baselines
    private static readonly Dictionary<DifficultyPreset, DifficultyConfig> _presets = new()
    {
        [DifficultyPreset.Easy] = new()
        {
            Preset = DifficultyPreset.Easy,
            SpawnRateModifier = -50,
            EnemyHpModifier = -40,
            EnemySpeedModifier = -40,
            PlayerDamageMultiplier = 1.5f,
            ShieldStartEnergy = 120f,
            StructureHp = 120,
            LaserHeatRateModifier = -80,
            LaserCoolingRateModifier = 200,
            LaserIdleCoolMultiplier = 3.0f,
            EnemyBulletDamage = 7,
            EnemyContactDamage = 14,
            PrimaryUpgradeDuration = 8f,
            PrimaryUpgradeDamageMultiplier = 2f,
            SecondaryUpgradeDuration = 8f,
            SecondaryUpgradeDamageMultiplier = 2f,
            ShieldRepairAmount = 30,
            StructureRepairAmount = 20,
            RapidCoolDuration = 20f,
            DiagonalShotDuration = 15f,
            PowerShotDuration = 20f,
            TripleShotDuration = 15f
        },
        [DifficultyPreset.Normal] = new()
        {
            Preset = DifficultyPreset.Normal,
            SpawnRateModifier = 0,
            EnemyHpModifier = 0,
            EnemySpeedModifier = 0,
            PlayerDamageMultiplier = 1.0f,
            ShieldStartEnergy = 100f,
            StructureHp = 100,
            LaserHeatRateModifier = -66,
            LaserCoolingRateModifier = 100,
            LaserIdleCoolMultiplier = 2.5f,
            EnemyBulletDamage = 10,
            EnemyContactDamage = 20,
            PrimaryUpgradeDuration = 8f,
            PrimaryUpgradeDamageMultiplier = 2f,
            SecondaryUpgradeDuration = 8f,
            SecondaryUpgradeDamageMultiplier = 2f,
            ShieldRepairAmount = 30,
            StructureRepairAmount = 20,
            RapidCoolDuration = 20f,
            DiagonalShotDuration = 15f,
            PowerShotDuration = 20f,
            TripleShotDuration = 15f
        },
        [DifficultyPreset.Hard] = new()
        {
            Preset = DifficultyPreset.Hard,
            SpawnRateModifier = 40,
            EnemyHpModifier = 50,
            EnemySpeedModifier = 30,
            PlayerDamageMultiplier = 0.8f,
            ShieldStartEnergy = 80f,
            StructureHp = 80,
            LaserHeatRateModifier = -50,
            LaserCoolingRateModifier = 75,
            LaserIdleCoolMultiplier = 2.0f,
            EnemyBulletDamage = 12,
            EnemyContactDamage = 24,
            PrimaryUpgradeDuration = 8f,
            PrimaryUpgradeDamageMultiplier = 2f,
            SecondaryUpgradeDuration = 8f,
            SecondaryUpgradeDamageMultiplier = 2f,
            ShieldRepairAmount = 30,
            StructureRepairAmount = 20,
            RapidCoolDuration = 20f,
            DiagonalShotDuration = 15f,
            PowerShotDuration = 20f,
            TripleShotDuration = 15f
        },
        [DifficultyPreset.Expert] = new()
        {
            Preset = DifficultyPreset.Expert,
            SpawnRateModifier = 100,
            EnemyHpModifier = 100,
            EnemySpeedModifier = 60,
            PlayerDamageMultiplier = 0.5f,
            ShieldStartEnergy = 50f,
            StructureHp = 50,
            LaserHeatRateModifier = -33,
            LaserCoolingRateModifier = 50,
            LaserIdleCoolMultiplier = 1.5f,
            EnemyBulletDamage = 15,
            EnemyContactDamage = 30,
            PrimaryUpgradeDuration = 8f,
            PrimaryUpgradeDamageMultiplier = 2f,
            SecondaryUpgradeDuration = 8f,
            SecondaryUpgradeDamageMultiplier = 2f,
            ShieldRepairAmount = 30,
            StructureRepairAmount = 20,
            RapidCoolDuration = 20f,
            DiagonalShotDuration = 15f,
            PowerShotDuration = 20f,
            TripleShotDuration = 15f
        }
    };

    /// Mapping preset → multiplicateur de régénération du bouclier.
    private static readonly Dictionary<DifficultyPreset, float> _shieldRegenPresets = new()
    {
        [DifficultyPreset.Easy] = 1.5f,
        [DifficultyPreset.Normal] = 1.0f,
        [DifficultyPreset.Hard] = 0.7f,
        [DifficultyPreset.Expert] = 0.5f
    };

    public static VideoConfig Video => _video;
    public static AudioConfig Audio => _audio;
    public static DifficultyConfig Difficulty => _difficulty;
    public static InputConfig Input => _input;

    /// <summary>
    /// Charge la configuration depuis le fichier XML.
    /// Crée des valeurs par défaut si le fichier est absent.
    /// </summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                using var stream = File.OpenRead(ConfigPath);
                var serializer = new XmlSerializer(typeof(AppConfigData), ShooterNamespace);
                var loaded = (AppConfigData)serializer.Deserialize(stream);
                if (loaded != null)
                {
                    if (loaded.Video != null) _video = loaded.Video;
                    if (loaded.Audio != null) _audio = loaded.Audio;
                    if (loaded.Difficulty != null)
                    {
                        _difficulty = loaded.Difficulty;
                        // Merge with preset baseline (handles missing fields in old files)
                        var baseline = _presets[_difficulty.Preset];
                        _difficulty.Preset = baseline.Preset;
                        _difficulty.SpawnRateModifier = loaded.Difficulty.SpawnRateModifier;
                        _difficulty.EnemyHpModifier = loaded.Difficulty.EnemyHpModifier;
                        _difficulty.EnemySpeedModifier = loaded.Difficulty.EnemySpeedModifier;
                        _difficulty.PlayerDamageMultiplier = loaded.Difficulty.PlayerDamageMultiplier;
                        _difficulty.ShieldStartEnergy = loaded.Difficulty.ShieldStartEnergy;
                        _difficulty.StructureHp = loaded.Difficulty.StructureHp;
                        _difficulty.LaserHeatRateModifier = baseline.LaserHeatRateModifier;
                        if (loaded.Difficulty.LaserHeatRateModifier != 0)
                            _difficulty.LaserHeatRateModifier = loaded.Difficulty.LaserHeatRateModifier;
                        _difficulty.LaserCoolingRateModifier = baseline.LaserCoolingRateModifier;
                        if (loaded.Difficulty.LaserCoolingRateModifier != 0)
                            _difficulty.LaserCoolingRateModifier = loaded.Difficulty.LaserCoolingRateModifier;
                        _difficulty.LaserIdleCoolMultiplier = baseline.LaserIdleCoolMultiplier;
                        if (Math.Abs(loaded.Difficulty.LaserIdleCoolMultiplier - 2.5f) > 0.01f)
                            _difficulty.LaserIdleCoolMultiplier = loaded.Difficulty.LaserIdleCoolMultiplier;
                        _difficulty.EnemyBulletDamage = baseline.EnemyBulletDamage;
                        if (loaded.Difficulty.EnemyBulletDamage != 10)
                            _difficulty.EnemyBulletDamage = loaded.Difficulty.EnemyBulletDamage;
                        _difficulty.EnemyContactDamage = baseline.EnemyContactDamage;
                        if (loaded.Difficulty.EnemyContactDamage != 20)
                            _difficulty.EnemyContactDamage = loaded.Difficulty.EnemyContactDamage;
                        _difficulty.PrimaryUpgradeDuration = baseline.PrimaryUpgradeDuration;
                        if (Math.Abs(loaded.Difficulty.PrimaryUpgradeDuration - 8f) > 0.001f)
                            _difficulty.PrimaryUpgradeDuration = loaded.Difficulty.PrimaryUpgradeDuration;
                        _difficulty.PrimaryUpgradeDamageMultiplier = baseline.PrimaryUpgradeDamageMultiplier;
                        if (Math.Abs(loaded.Difficulty.PrimaryUpgradeDamageMultiplier - 2f) > 0.001f)
                            _difficulty.PrimaryUpgradeDamageMultiplier = loaded.Difficulty.PrimaryUpgradeDamageMultiplier;
                        _difficulty.SecondaryUpgradeDuration = baseline.SecondaryUpgradeDuration;
                        if (Math.Abs(loaded.Difficulty.SecondaryUpgradeDuration - 8f) > 0.001f)
                            _difficulty.SecondaryUpgradeDuration = loaded.Difficulty.SecondaryUpgradeDuration;
                        _difficulty.SecondaryUpgradeDamageMultiplier = baseline.SecondaryUpgradeDamageMultiplier;
                        if (Math.Abs(loaded.Difficulty.SecondaryUpgradeDamageMultiplier - 2f) > 0.001f)
                            _difficulty.SecondaryUpgradeDamageMultiplier = loaded.Difficulty.SecondaryUpgradeDamageMultiplier;
                        _difficulty.ShieldRepairAmount = baseline.ShieldRepairAmount;
                        if (loaded.Difficulty.ShieldRepairAmount != 30)
                            _difficulty.ShieldRepairAmount = loaded.Difficulty.ShieldRepairAmount;
                        _difficulty.StructureRepairAmount = baseline.StructureRepairAmount;
                        if (loaded.Difficulty.StructureRepairAmount != 20)
                            _difficulty.StructureRepairAmount = loaded.Difficulty.StructureRepairAmount;
                        _difficulty.RapidCoolDuration = baseline.RapidCoolDuration;
                        if (Math.Abs(loaded.Difficulty.RapidCoolDuration - 20f) > 0.001f)
                            _difficulty.RapidCoolDuration = loaded.Difficulty.RapidCoolDuration;
                        _difficulty.DiagonalShotDuration = baseline.DiagonalShotDuration;
                        if (Math.Abs(loaded.Difficulty.DiagonalShotDuration - 15f) > 0.001f)
                            _difficulty.DiagonalShotDuration = loaded.Difficulty.DiagonalShotDuration;
                        _difficulty.PowerShotDuration = baseline.PowerShotDuration;
                        if (Math.Abs(loaded.Difficulty.PowerShotDuration - 20f) > 0.001f)
                            _difficulty.PowerShotDuration = loaded.Difficulty.PowerShotDuration;
                        _difficulty.TripleShotDuration = baseline.TripleShotDuration;
                        if (Math.Abs(loaded.Difficulty.TripleShotDuration - 15f) > 0.001f)
                            _difficulty.TripleShotDuration = loaded.Difficulty.TripleShotDuration;
                    }
                    if (loaded.Input != null)
                    {
                        _input = loaded.Input;
                        // Rebuild dictionary from serialized list
                        if (_input.BindingsList.Count == 0 && _input.Bindings.Count == 0)
                            _input = CreateDefaultBindings();
                    }
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppConfig] Erreur de chargement : {ex.Message}");
        }
        Reset();
    }

    /// <summary>
    /// Sauvegarde la configuration dans le fichier XML.
    /// </summary>
    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var data = new AppConfigData
            {
                Video = _video,
                Audio = _audio,
                Difficulty = _difficulty,
                Input = _input
            };
            var serializer = new XmlSerializer(typeof(AppConfigData), ShooterNamespace);
            using var writer = new StreamWriter(ConfigPath);
            var ns = new XmlSerializerNamespaces();
            ns.Add("", ShooterNamespace);
            serializer.Serialize(writer, data, ns);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppConfig] Erreur de sauvegarde : {ex.Message}");
        }
    }

    /// <summary>
    /// Réinitialise la configuration aux valeurs par défaut.
    /// </summary>
    public static void Reset()
    {
        _video = new VideoConfig();
        _audio = new AudioConfig();
        _difficulty = new DifficultyConfig();
        _input = CreateDefaultBindings();
        Save();
    }

    /// <summary>
    /// Applique le preset de difficulté et réinitialise les modificateurs fins.
    /// </summary>
    /// <param name="preset">Preset à appliquer.</param>
    public static void ApplyPreset(DifficultyPreset preset)
    {
        if (_presets.TryGetValue(preset, out var baseline))
        {
            _difficulty.Preset = preset;
            _difficulty.SpawnRateModifier = baseline.SpawnRateModifier;
            _difficulty.EnemyHpModifier = baseline.EnemyHpModifier;
            _difficulty.EnemySpeedModifier = baseline.EnemySpeedModifier;
            _difficulty.PlayerDamageMultiplier = baseline.PlayerDamageMultiplier;
            _difficulty.ShieldStartEnergy = baseline.ShieldStartEnergy;
            _difficulty.StructureHp = baseline.StructureHp;
            _difficulty.LaserHeatRateModifier = baseline.LaserHeatRateModifier;
            _difficulty.LaserCoolingRateModifier = baseline.LaserCoolingRateModifier;
            _difficulty.LaserIdleCoolMultiplier = baseline.LaserIdleCoolMultiplier;
            _difficulty.EnemyBulletDamage = baseline.EnemyBulletDamage;
            _difficulty.EnemyContactDamage = baseline.EnemyContactDamage;
            _difficulty.PrimaryUpgradeDuration = baseline.PrimaryUpgradeDuration;
            _difficulty.PrimaryUpgradeDamageMultiplier = baseline.PrimaryUpgradeDamageMultiplier;
            _difficulty.SecondaryUpgradeDuration = baseline.SecondaryUpgradeDuration;
            _difficulty.SecondaryUpgradeDamageMultiplier = baseline.SecondaryUpgradeDamageMultiplier;
            _difficulty.ShieldRepairAmount = baseline.ShieldRepairAmount;
            _difficulty.StructureRepairAmount = baseline.StructureRepairAmount;
            _difficulty.RapidCoolDuration = baseline.RapidCoolDuration;
            _difficulty.DiagonalShotDuration = baseline.DiagonalShotDuration;
            _difficulty.PowerShotDuration = baseline.PowerShotDuration;
            _difficulty.TripleShotDuration = baseline.TripleShotDuration;
            Save();
        }
    }

    /// <summary>
    /// Obtient les paramètres de jeu calculés en appliquant le preset
    /// et les modificateurs fins.
    /// </summary>
    public static GameplaySettings GetGameplaySettings()
    {
        float spawnMod = 1f + _difficulty.SpawnRateModifier / 100f;
        float hpMod = 1f + _difficulty.EnemyHpModifier / 100f;
        float speedMod = 1f + _difficulty.EnemySpeedModifier / 100f;
        float damageMod = _difficulty.PlayerDamageMultiplier;

        return new GameplaySettings
        {
            BaseSpawnInterval = 7.5f / spawnMod,
            MinSpawnInterval = 2.0f / spawnMod,
            WaveGrowthRate = 0.05f * spawnMod,
            EnemyHpMultiplier = hpMod,
            EnemySpeedMultiplier = speedMod,
            PlayerDamageMultiplier = damageMod,
            PlayerShieldStart = _difficulty.ShieldStartEnergy,
            PlayerStructureHp = _difficulty.StructureHp,
            ShieldRegenMultiplier = _shieldRegenPresets.TryGetValue(_difficulty.Preset, out float regen)
                ? regen : 1.0f,
            EnemyBulletDamage = _difficulty.EnemyBulletDamage,
            EnemyContactDamage = _difficulty.EnemyContactDamage
        };
    }

    /// <summary>
    /// Retourne les liaisons par défaut (clavier).
    /// </summary>
    private static InputConfig CreateDefaultBindings()
    {
        var config = new InputConfig
        {
            Deadzone = 0.25f,
        };
        config.SetBindings(new Dictionary<string, InputBinding>
        {
            ["MoveUp"] = new InputBinding { Type = "Key", Key = "Up" },
            ["MoveDown"] = new InputBinding { Type = "Key", Key = "Down" },
            ["MoveLeft"] = new InputBinding { Type = "Key", Key = "Left" },
            ["MoveRight"] = new InputBinding { Type = "Key", Key = "Right" },
            ["FirePrimary"] = new InputBinding { Type = "Key", Key = "Space" },
            ["FireSecondary"] = new InputBinding { Type = "Key", Key = "X" },
            ["FireSpecial"] = new InputBinding { Type = "Key", Key = "C" },
            ["MenuUp"] = new InputBinding { Type = "Key", Key = "Up" },
            ["MenuDown"] = new InputBinding { Type = "Key", Key = "Down" },
            ["MenuSelect"] = new InputBinding { Type = "Key", Key = "Enter" },
            ["MenuBack"] = new InputBinding { Type = "Key", Key = "Escape" },
            ["PauseGame"] = new InputBinding { Type = "Key", Key = "Escape" }
        });
        return config;
    }

    /// <summary>
    /// Données intermédiaires pour la sérialisation XML.
    /// </summary>
    [XmlRoot("config", Namespace = ShooterNamespace)]
    public class AppConfigData
    {
        [XmlElement("video")]
        public VideoConfig Video { get; set; } = new();

        [XmlElement("audio")]
        public AudioConfig Audio { get; set; } = new();

        [XmlElement("difficulty")]
        public DifficultyConfig Difficulty { get; set; } = new();

        [XmlElement("input")]
        public InputConfig Input { get; set; } = new();
    }
}

/// <summary>
/// Paramètres de jeu calculés à partir d'AppConfig.Difficulty.
/// Transmis à GameplayScreen au démarrage d'une partie.
/// </summary>
public class GameplaySettings
{
    public float BaseSpawnInterval { get; set; } = 1.5f;
    public float MinSpawnInterval { get; set; } = 0.4f;
    public float WaveGrowthRate { get; set; } = 0.05f;
    public float EnemyHpMultiplier { get; set; } = 1f;
    public float EnemySpeedMultiplier { get; set; } = 1f;
    public float PlayerDamageMultiplier { get; set; } = 1f;
    public float PlayerShieldStart { get; set; } = 100f;
    public int PlayerStructureHp { get; set; } = 100;

    /// <summary>
    /// Multiplicateur de régénération du bouclier.
    /// 1.5 = facile (rapide), 1.0 = normal, 0.7 = dur (lent), 0.5 = expert (très lent).
    /// </summary>
    public float ShieldRegenMultiplier { get; set; } = 1f;

    /// <summary>
    /// Dégâts causés par les tirs ennemis sur le bouclier/structure.
    /// </summary>
    public int EnemyBulletDamage { get; set; } = 10;

    /// <summary>
    /// Dégâts causés par le contact des ennemis avec le joueur.
    /// </summary>
    public int EnemyContactDamage { get; set; } = 20;
}
