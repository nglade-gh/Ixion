using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Key";

    /// <summary>Touche ou bouton associé.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    /// <summary>Index manette (0-3) si type=Gamepad.</summary>
    [JsonPropertyName("gamepadIndex")]
    public int GamepadIndex { get; set; } = 0;
}

/// <summary>
/// Configuration audio (volumes 0.0-1.0).
/// </summary>
public class AudioConfig
{
    [JsonPropertyName("masterVolume")]
    public float MasterVolume { get; set; } = 0.7f;

    [JsonPropertyName("musicVolume")]
    public float MusicVolume { get; set; } = 0.9f;

    [JsonPropertyName("sfxVolume")]
    public float SfxVolume { get; set; } = 0.05f;

    [JsonPropertyName("muted")]
    public bool Muted { get; set; } = false;
}

/// <summary>
/// Configuration vidéo (résolution, fullscreen).
/// </summary>
public class VideoConfig
{
    [JsonPropertyName("width")]
    public int Width { get; set; } = 600;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 800;

    [JsonPropertyName("fullscreen")]
    public bool Fullscreen { get; set; } = false;

    /// <summary>Liste des résolutions supportées.</summary>
    [JsonIgnore]
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
    [JsonPropertyName("preset")]
    public DifficultyPreset Preset { get; set; } = DifficultyPreset.Normal;

    // Fine-tuning (-100 to +100 percent modifier)
    [JsonPropertyName("spawnRateModifier")]
    public int SpawnRateModifier { get; set; } = 0;

    [JsonPropertyName("enemyHpModifier")]
    public int EnemyHpModifier { get; set; } = 0;

    [JsonPropertyName("enemySpeedModifier")]
    public int EnemySpeedModifier { get; set; } = 0;

    [JsonPropertyName("playerDamageMultiplier")]
    public float PlayerDamageMultiplier { get; set; } = 1.0f;

    [JsonPropertyName("shieldStartEnergy")]
    public float ShieldStartEnergy { get; set; } = 100f;

    [JsonPropertyName("structureHp")]
    public int StructureHp { get; set; } = 100;

    [JsonPropertyName("difficultyLabel")]
    [JsonIgnore]
    public string DifficultyLabel
    {
        get
        {
            if (SpawnRateModifier != 0 || EnemyHpModifier != 0 ||
                EnemySpeedModifier != 0 || PlayerDamageMultiplier != 1.0f ||
                ShieldStartEnergy != 100f || StructureHp != 100)
                return $"{Preset:F0} (custom)";
            return Preset.ToString();
        }
    }
}

/// <summary>
/// Dictionnaire pour la désérialisation JSON des bindings.
/// key = action name, value = InputBinding.
/// </summary>
public class InputConfig
{
    [JsonPropertyName("bindings")]
    public Dictionary<string, InputBinding> Bindings { get; set; } = new();

    [JsonPropertyName("deadzone")]
    public float Deadzone { get; set; } = 0.25f;
}

/// <summary>
/// Configuration globale de l'application. Sérialisé en JSON
/// vers <c>Config/game.json</c>.
/// </summary>
public static class AppConfig
{
    private static string ConfigPath =>
        Path.Combine(AppContext.BaseDirectory, "Config", "game.json");

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
            StructureHp = 120
        },
        [DifficultyPreset.Normal] = new()
        {
            Preset = DifficultyPreset.Normal,
            SpawnRateModifier = 0,
            EnemyHpModifier = 0,
            EnemySpeedModifier = 0,
            PlayerDamageMultiplier = 1.0f,
            ShieldStartEnergy = 100f,
            StructureHp = 100
        },
        [DifficultyPreset.Hard] = new()
        {
            Preset = DifficultyPreset.Hard,
            SpawnRateModifier = 40,
            EnemyHpModifier = 50,
            EnemySpeedModifier = 30,
            PlayerDamageMultiplier = 0.8f,
            ShieldStartEnergy = 80f,
            StructureHp = 80
        },
        [DifficultyPreset.Expert] = new()
        {
            Preset = DifficultyPreset.Expert,
            SpawnRateModifier = 100,
            EnemyHpModifier = 100,
            EnemySpeedModifier = 60,
            PlayerDamageMultiplier = 0.5f,
            ShieldStartEnergy = 50f,
            StructureHp = 50
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
    /// Charge la configuration depuis le fichier JSON.
    /// Crée des valeurs par défaut si le fichier est absent.
    /// </summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<AppConfigData>(json);
                if (loaded != null)
                {
                    if (loaded.Video != null) _video = loaded.Video;
                    if (loaded.Audio != null) _audio = loaded.Audio;
                    if (loaded.Difficulty != null)
                    {
                        _difficulty = loaded.Difficulty;
                        // Merge with preset baseline
                        var baseline = _presets[loaded.Difficulty.Preset];
                        _difficulty.Preset = loaded.Difficulty.Preset;
                        _difficulty.SpawnRateModifier = loaded.Difficulty.SpawnRateModifier;
                        _difficulty.EnemyHpModifier = loaded.Difficulty.EnemyHpModifier;
                        _difficulty.EnemySpeedModifier = loaded.Difficulty.EnemySpeedModifier;
                        _difficulty.PlayerDamageMultiplier = loaded.Difficulty.PlayerDamageMultiplier;
                        _difficulty.ShieldStartEnergy = loaded.Difficulty.ShieldStartEnergy;
                        _difficulty.StructureHp = loaded.Difficulty.StructureHp;
                    }
                    if (loaded.Input != null) _input = loaded.Input;
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
    /// Sauvegarde la configuration dans le fichier JSON.
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
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            });
            File.WriteAllText(ConfigPath, json);
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
                ? regen : 1.0f
        };
    }

    /// <summary>
    /// Retourne les liaisons par défaut (clavier).
    /// </summary>
    private static InputConfig CreateDefaultBindings()
    {
        return new InputConfig
        {
            Deadzone = 0.25f,
            Bindings = new Dictionary<string, InputBinding>
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
            }
        };
    }

    /// <summary>
    /// Données intermédiaires pour la sérialisation JSON.
    /// </summary>
    private class AppConfigData
    {
        public VideoConfig Video { get; set; }
        public AudioConfig Audio { get; set; }
        public DifficultyConfig Difficulty { get; set; }
        public InputConfig Input { get; set; }
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
}
