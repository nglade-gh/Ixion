using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

// ---------------------------------------------------------------
// GameConfig
// ---------------------------------------------------------------

/// <summary>
/// Singleton chargeant toutes les configurations XML du jeu.
/// Fournit des valeurs par défaut robustes si un fichier est absent ou corrompu.
/// </summary>
public class GameConfig
{
    private static GameConfig _instance;

    /// <summary>
    /// Accès singleton au gestionnaire de configuration.
    /// </summary>
    public static GameConfig Instance => _instance ??= new GameConfig();

    /// <summary>
    /// Configuration des entités (joueur, ennemi de base).
    /// </summary>
    public EntitiesConfig Entities { get; private set; }

    /// <summary>
    /// Configuration des types d'ennemis.
    /// </summary>
    public EnemiesConfig Enemies { get; private set; }

    /// <summary>
    /// Configuration des éléments de décor.
    /// </summary>
    public DecorsConfig Decors { get; private set; }

    /// <summary>
    /// Configuration des projectiles.
    /// </summary>
    public BulletsConfig Bullets { get; private set; }

    /// <summary>
    /// Configuration des bonus.
    /// </summary>
    public BonusesConfig Bonuses { get; private set; }

    /// <summary>
    /// Configuration des sons (effets sonores et volumes).
    /// </summary>
    public SoundsConfig Sounds { get; private set; }

    /// <summary>
    /// Indique si la configuration a été chargée.
    /// </summary>
    public bool Loaded { get; private set; }

    private GameConfig() { }

    private static string ConfigDir => Path.Combine(AppContext.BaseDirectory, "Content", "config");

    /// <summary>
    /// Charge toutes les configurations XML si ce n'est pas déjà fait.
    /// </summary>
    public void LoadAll()
    {
        if (Loaded)
            return;
        Entities = Load<EntitiesConfig>("entities.xml") ?? DefaultEntities();
        Enemies = Load<EnemiesConfig>("enemies.xml") ?? DefaultEnemies();
        Decors = Load<DecorsConfig>("decors.xml") ?? DefaultDecors();
        Bullets = Load<BulletsConfig>("bullets.xml") ?? DefaultBullets();
        Bonuses = Load<BonusesConfig>("bonuses.xml") ?? DefaultBonuses();
        Sounds = Load<SoundsConfig>("sounds.xml");
        Loaded = true;
    }

    private const string ShooterNamespace = "https://www.timc.fr/nicolas-glade/dotnet/shooter";

    /// <summary>
    /// Charge un fichier XML de configuration.
    /// </summary>
    /// <typeparam name="T">Type de la configuration.</typeparam>
    /// <param name="fileName">Nom du fichier XML.</param>
    /// <returns>Instance désérialisée, ou null en cas d'erreur.</returns>
    private static T Load<T>(string fileName) where T : class
    {
        string path = Path.Combine(ConfigDir, fileName);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[GameConfig] Fichier XML manquant : {path} — valeurs par défaut utilisées.");
            return null;
        }
        try
        {
            using var stream = File.OpenRead(path);
            return (T)new XmlSerializer(typeof(T), ShooterNamespace).Deserialize(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameConfig] Erreur de lecture de {path} : {ex.Message} — valeurs par défaut utilisées.");
            return null;
        }
    }

    /// <summary>
    /// Convertit un chemin de texture en clé de contenu (supprime l'extension .png).
    /// </summary>
    /// <param name="texturePath">Chemin de la texture.</param>
    /// <returns>Clé de contenu utilisable par <see cref="ContentManager"/>.</returns>
    public static string ContentKey(string texturePath)
    {
        if (string.IsNullOrEmpty(texturePath))
            return texturePath;
        return texturePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? texturePath.Substring(0, texturePath.Length - 4)
            : texturePath;
    }

    /// <summary>
    /// Recherche un projectile par nom.
    /// </summary>
    /// <param name="name">Nom du projectile.</param>
    /// <returns>Paramètres du projectile, ou le premier par défaut.</returns>
    public BulletParams GetBullet(string name)
    {
        foreach (var bullet in Bullets.Bullets)
            if (string.Equals(bullet.Name, name, StringComparison.OrdinalIgnoreCase))
                return bullet;
        return Bullets.Bullets[0];
    }

    /// <summary>
    /// Recherche un type d'ennemi par nom.
    /// </summary>
    /// <param name="name">Nom de l'ennemi.</param>
    /// <returns>Archetype de l'ennemi, ou le premier par défaut.</returns>
    public EnemyType GetEnemyType(string name)
    {
        foreach (var type in Enemies.EnemyTypes)
            if (string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase))
                return type;
        return Enemies.EnemyTypes[0];
    }

    /// <summary>
    /// Recherche un bonus par nom.
    /// </summary>
    /// <param name="name">Nom du bonus.</param>
    /// <returns>Paramètres du bonus, ou le premier par défaut.</returns>
    public BonusParams GetBonus(string name)
    {
        foreach (var b in Bonuses.Bonuses)
            if (string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase))
                return b;
        return Bonuses.Bonuses.Count > 0 ? Bonuses.Bonuses[0] : null;
    }

    // -----------------------------------------------------------------
    // Built-in fallbacks (mirror the XML defaults)
    // -----------------------------------------------------------------

    private static EntitiesConfig DefaultEntities() => new()
    {
        Player = new PlayerParams
        {
            Name = "Player",
            Texture = "pics/ships/ship1",
            Width = 34,
            Height = 48,
            MaxHp = 100,
            RegenRate = 4f,
            FireRate = 0.16f,
            MaxSpeed = 340f,
            Lives = 3,
            InvincibilityDuration = 2f
        },
        Enemy = new EnemyParams
        {
            Name = "Enemy",
            Texture = "pics/enemies/virus1",
            MaxHp = 30,
            FireRate = 1.5f,
            Speed = 70f,
            ScoreValue = 10
        }
    };

    private static EnemiesConfig DefaultEnemies() => new()
    {
        EnemyTypes = new List<EnemyType>
        {
            new()
            {
                Name = "Scout", Texture = "pics/enemies/virus1", Size = 46,
                Hp = 15, Speed = 150f, Score = 15, FireRate = 2.4f,
                MovePatternRaw = "sine", Amplitude = 140f, Frequency = 1.6f,
                BulletPatternRaw = "simple", ColorRaw = "255,230,160"
            },
            new()
            {
                Name = "Fighter", Texture = "pics/enemies/sniper-297661__180", Size = 46,
                Hp = 25, Speed = 85f, Score = 25, FireRate = 1.3f,
                MovePatternRaw = "horde", Amplitude = 70f, Frequency = 1.2f,
                BulletPatternRaw = "burst", ColorRaw = "255,255,255"
            },
            new()
            {
                Name = "Tank", Texture = "pics/enemies/virus_tiles", SourceRectRaw = "5,8,114,112",
                Size = 88, Hp = 120, Speed = 35f, Score = 60, FireRate = 2.0f,
                MovePatternRaw = "horde", Amplitude = 30f, Frequency = 0.4f,
                BulletPatternRaw = "burst", ColorRaw = "255,120,120"
            },
            new()
            {
                Name = "Elastic", Texture = "pics/enemies/virus_tiles", SourceRectRaw = "139,255,114,112",
                Size = 44, Hp = 20, Speed = 120f, Score = 35, FireRate = 1.8f,
                MovePatternRaw = "zigzag", Amplitude = 200f, Frequency = 2.2f,
                BulletPatternRaw = "simple", ColorRaw = "170,255,170"
            },
            new()
            {
                Name = "Kamikaze", Texture = "pics/enemies/virus_tiles", SourceRectRaw = "271,131,114,112",
                Size = 42, Hp = 10, Speed = 130f, Score = 50, FireRate = 2.5f,
                MovePatternRaw = "charge", BulletPatternRaw = "homing", ColorRaw = "255,90,60"
            },
            new()
            {
                Name = "EvilEye", Texture = "pics/space/enemies/EvilEye_1", Size = 64,
                Hp = 45, Speed = 70f, Score = 40, FireRate = 1.6f,
                MovePatternRaw = "sine", Amplitude = 160f, Frequency = 1.4f,
                BulletPatternRaw = "burst", ColorRaw = "120,220,120"
            },
            new()
            {
                Name = "Turret", Texture = "pics/space/turrets/Base_1", Size = 72,
                Hp = 90, Speed = 25f, Score = 80, FireRate = 2.2f,
                MovePatternRaw = "horde", Amplitude = 25f, Frequency = 0.5f,
                BulletPatternRaw = "burst", ColorRaw = "200,160,255"
            },
            new()
            {
                Name = "SpikeBall", Texture = "pics/space/spikeballs/Metal_Spike_Ball_1", Size = 56,
                Hp = 25, Speed = 110f, Score = 30, FireRate = 1.9f,
                MovePatternRaw = "zigzag", Amplitude = 180f, Frequency = 2.0f,
                BulletPatternRaw = "simple", ColorRaw = "255,180,120"
            },
            new()
            {
                Name = "Spaceship", Texture = "pics/space/ships/Spaceship_0", Size = 70,
                Hp = 60, Speed = 60f, Score = 55, FireRate = 1.5f,
                MovePatternRaw = "sine", Amplitude = 120f, Frequency = 1.0f,
                BulletPatternRaw = "burst", ColorRaw = "220,220,255"
            }
        }
    };

    private static DecorsConfig DefaultDecors() => new()
    {
        Elements = new List<DecorParams>
        {
            new()
            {
                Name = "LeftWall", Texture = "pics/decors/Left", Side = "left",
                Layer = 2, ScrollSpeed = 130f, IsParallaxRaw = 1, Scale = 0.28f
            },
            new()
            {
                Name = "RightWall", Texture = "pics/decors/Right", Side = "right",
                Layer = 2, ScrollSpeed = 130f, IsParallaxRaw = 1, Scale = 0.28f
            },
            new()
            {
                Name = "TopCap", Texture = "pics/decors/Top", Side = "top",
                Layer = 2, IsParallaxRaw = 0, Scale = 0.5f
            }
        },
        Stars = new StarsParams
        {
            Count = 90, MinSpeed = 20f, MaxSpeed = 120f, MinSize = 1f, MaxSize = 3f
        }
    };

    private static BulletsConfig DefaultBullets() => new()
    {
        Bullets = new List<BulletParams>
        {
            new()
            {
                Name = "PlayerBullet",
                Texture = "pics/bullets/white-down-arrow-icon-8",
                SourceRectRaw = "23,37,79,56",
                Width = 14, Height = 10, Speed = 700f, Damage = 3,
                ColorRaw = "160,230,255,255", Rotation = 180f, SearchRadius = 0f
            },
            new()
            {
                Name = "PlayerLaser",
                Texture = "pics/lasers/player_laser",
                Width = 8, Height = 24, Speed = 700f, Damage = 3,
                ColorRaw = "100,220,255,255", Rotation = 180f, SearchRadius = 0f
            },
            new()
            {
                Name = "PlayerPlasma",
                Texture = "pics/bullets/missile1",
                Width = 18, Height = 18, Speed = 500f, Damage = 12,
                ColorRaw = "255,140,200,255", Rotation = 180f, SearchRadius = 0f
            },
            new()
            {
                Name = "PlayerSpecial",
                Texture = "pics/bullets/missile1",
                Width = 28, Height = 28, Speed = 400f, Damage = 25,
                ColorRaw = "255,220,60,255", Rotation = 180f, SearchRadius = 0f
            },
            new()
            {
                Name = "EnemyBullet",
                Texture = "pics/bullets/missile1",
                Width = 44, Height = 12, Speed = 190f, Damage = 12,
                ColorRaw = "255,255,255,255", Rotation = 90f, SearchRadius = 0f
            },
            new()
            {
                Name = "EnemyHoming",
                Texture = "pics/bullets/missile1",
                Width = 44, Height = 12, Speed = 150f, Damage = 15,
                Homing = 100f, SearchRadius = 220f,
                ColorRaw = "255,200,160,255", Rotation = 90f
            },
            new()
            {
                Name = "EnemyLaser",
                Texture = "pics/lasers/enemy_laser",
                Width = 12, Height = 20, Speed = 250f, Damage = 8,
                HitPoints = 1,
                ColorRaw = "255,80,60,255", Rotation = 90f, SearchRadius = 0f
            },
            new()
            {
                Name = "TurretLaser",
                Texture = "pics/lasers/turret_laser",
                Width = 16, Height = 28, Speed = 200f, Damage = 12,
                HitPoints = 1,
                ColorRaw = "50,255,80,255", Rotation = 90f, SearchRadius = 0f
            }
        }
    };

    private static BonusesConfig DefaultBonuses() => new()
    {
        Bonuses = new List<BonusParams>
        {
            new()
            {
                Name = "PrimaryUpgrade",
                Texture = "pics/bullets/white-down-arrow-icon-8",
                Width = 16, Height = 16, Speed = 80f,
                ColorRaw = "0,255,255,255", Amount = 0
            },
            new()
            {
                Name = "SecondaryUpgrade",
                Texture = "pics/bullets/missile1",
                Width = 16, Height = 16, Speed = 80f,
                ColorRaw = "255,0,255,255", Amount = 0
            },
            new()
            {
                Name = "SpecialAmmo",
                Texture = "pics/bullets/missile1",
                Width = 16, Height = 16, Speed = 80f,
                ColorRaw = "255,255,0,255", Amount = 3
            },
            new()
            {
                Name = "ShieldRepair",
                Texture = "pics/bullets/white-down-arrow-icon-8",
                Width = 16, Height = 16, Speed = 80f,
                ColorRaw = "40,120,255,255", Amount = 30
            },
            new()
            {
                Name = "StructureRepair",
                Texture = "pics/bullets/missile1",
                Width = 16, Height = 16, Speed = 80f,
                ColorRaw = "80,200,80,255", Amount = 20
            }
        }
    };
}
