using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace Shooter;

// ---------------------------------------------------------------------------
// Serializable parameter classes loaded from Content/config/*.xml
// ---------------------------------------------------------------------------

/// <summary>
/// Helpers utilitaires pour les classes de paramètres XML.
/// </summary>
public static class XmlConfig
{
    /// <summary>
    /// Parse une couleur depuis une chaîne "r,g,b" ou "r,g,b,a".
    /// </summary>
    /// <param name="raw">Chaîne brute.</param>
    /// <param name="fallback">Couleur de repli.</param>
    /// <returns>Couleur parsée ou fallback.</returns>
    public static Color ParseColor(string raw, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;
        var parts = raw.Split(',');
        try
        {
            if (parts.Length == 3)
                return new Color(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]));
            if (parts.Length == 4)
                return new Color(byte.Parse(parts[0]), byte.Parse(parts[1]),
                                 byte.Parse(parts[2]), byte.Parse(parts[3]));
        }
        catch
        {
            // invalid color -> fallback
        }
        return fallback;
    }

    /// <summary>
    /// Parse un sous-rectangle "x,y,w,h" depuis une chaîne.
    /// </summary>
    /// <param name="raw">Chaîne brute.</param>
    /// <returns>Rectangle? ou null si invalide.</returns>
    public static Rectangle? ParseSourceRect(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var parts = raw.Split(',');
        if (parts.Length != 4)
            return null;
        if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) &&
            int.TryParse(parts[2], out int w) && int.TryParse(parts[3], out int h) &&
            w > 0 && h > 0)
            return new Rectangle(x, y, w, h);
        return null;
    }

    /// <summary>
    /// Parse un enum depuis une chaîne, insensible à la casse.
    /// </summary>
    /// <typeparam name="T">Type de l'enum.</typeparam>
    /// <param name="raw">Chaîne brute.</param>
    /// <param name="fallback">Valeur de repli.</param>
    /// <returns>Valeur parsée ou fallback.</returns>
    public static T ParseEnum<T>(string raw, T fallback) where T : struct
    {
        return Enum.TryParse<T>(raw, true, out var value) ? value : fallback;
    }
}

/// <summary>
/// Paramètres de base communs à toutes les entités du jeu.
/// </summary>
public class EntityParams
{
    /// <summary>
    /// Nom de l'entité.
    /// </summary>
    [XmlElement("name")] public string Name { get; set; } = "";

    /// <summary>
    /// Chemin de la texture (sans extension).
    /// </summary>
    [XmlElement("texture")] public string Texture { get; set; } = "";

    /// <summary>
    /// Échelle de rendu.
    /// </summary>
    [XmlElement("scale")] public float Scale { get; set; } = 1f;

    /// <summary>
    /// Points de vie maximum.
    /// </summary>
    [XmlElement("maxHp")] public int MaxHp { get; set; } = 1;

    /// <summary>
    /// Taux de régénération par seconde.
    /// </summary>
    [XmlElement("regenRate")] public float RegenRate { get; set; }

    /// <summary>
    /// Intervalle de base entre les tirs, en secondes.
    /// </summary>
    [XmlElement("fireRate")] public float FireRate { get; set; }
}

/// <summary>
/// Paramètres spécifiques au joueur.
/// </summary>
public class PlayerParams : EntityParams
{
    /// <summary>
    /// Largeur logique du vaisseau.
    /// </summary>
    [XmlElement("width")] public int Width { get; set; } = 34;

    /// <summary>
    /// Hauteur logique du vaisseau.
    /// </summary>
    [XmlElement("height")] public int Height { get; set; } = 48;

    /// <summary>
    /// Vitesse maximale en px/s.
    /// </summary>
    [XmlElement("maxSpeed")] public float MaxSpeed { get; set; } = 340f;

    /// <summary>
    /// Nombre de vies initial.
    /// </summary>
    [XmlElement("lives")] public int Lives { get; set; } = 3;

    /// <summary>
    /// Durée d'invincibilité après un hit, en secondes.
    /// </summary>
    [XmlElement("invincibilityDuration")] public float InvincibilityDuration { get; set; } = 2f;

    /// <summary>
    /// Score initial.
    /// </summary>
    [XmlElement("initialScore")] public int InitialScore { get; set; }

    /// <summary>
    /// Rotation initiale du sprite, en degrés.
    /// </summary>
    [XmlElement("rotation")] public float Rotation { get; set; }
}

/// <summary>
/// Paramètres de base d'un ennemi (éléments communs).
/// </summary>
public class EnemyParams : EntityParams
{
    /// <summary>
    /// Vitesse de déplacement, en px/s.
    /// </summary>
    [XmlElement("speed")] public float Speed { get; set; } = 70f;

    /// <summary>
    /// Points accordés à la destruction.
    /// </summary>
    [XmlElement("scoreValue")] public int ScoreValue { get; set; } = 10;
}

/// <summary>
/// Archetype d'ennemi concret. Chargé depuis enemies.xml.
/// </summary>
public class EnemyType : EnemyParams
{
    /// <summary>Logical size on screen, in pixels (assets downscaled by ~4-5x).</summary>
    [XmlElement("size")] public int Size { get; set; } = 46;
    [XmlElement("hp")] public int Hp { get; set; } = 1;
    [XmlElement("score")] public int Score { get; set; } = 10;
    [XmlElement("movePattern")] public string MovePatternRaw { get; set; } = "sine";
    [XmlElement("amplitude")] public float Amplitude { get; set; }
    [XmlElement("frequency")] public float Frequency { get; set; } = 1f;
    [XmlElement("bulletPattern")] public string BulletPatternRaw { get; set; } = "simple";
    [XmlElement("color")] public string ColorRaw { get; set; }
    [XmlElement("sourceRect")] public string SourceRectRaw { get; set; }

    [XmlIgnore] public MovePattern MovePattern => XmlConfig.ParseEnum(MovePatternRaw, MovePattern.Sine);
    [XmlIgnore] public BulletPattern BulletPattern => XmlConfig.ParseEnum(BulletPatternRaw, BulletPattern.Simple);
    [XmlIgnore] public Color Color => XmlConfig.ParseColor(ColorRaw, Color.White);
    [XmlIgnore] public Rectangle? SourceRect => XmlConfig.ParseSourceRect(SourceRectRaw);
}

/// <summary>&lt;decorElement&gt; element of decors.xml: one background element type.</summary>
public class DecorParams
{
    [XmlElement("name")] public string Name { get; set; } = "";
    [XmlElement("texture")] public string Texture { get; set; } = "";
    [XmlArray("textures")] [XmlArrayItem("texture")] public List<string> Textures { get; set; }
    /// <summary>left | right | top | field</summary>
    [XmlElement("side")] public string Side { get; set; } = "field";
    [XmlElement("layer")] public int Layer { get; set; }
    [XmlElement("scrollSpeed")] public float ScrollSpeed { get; set; }
    [XmlElement("isParallax")] public int IsParallaxRaw { get; set; } = 1;
    [XmlElement("tile")] public int Tile { get; set; }
    [XmlElement("count")] public int Count { get; set; } = 1;
    [XmlElement("scale")] public float Scale { get; set; }
    [XmlElement("minScale")] public float MinScale { get; set; }
    [XmlElement("maxScale")] public float MaxScale { get; set; }
    [XmlElement("color")] public string ColorRaw { get; set; }

    [XmlIgnore] public bool IsParallax => IsParallaxRaw != 0;
    [XmlIgnore] public Color Color => XmlConfig.ParseColor(ColorRaw, Color.White);
}

/// <summary>&lt;stars&gt; element of decors.xml: background star field.</summary>
public class StarsParams
{
    [XmlElement("count")] public int Count { get; set; } = 60;
    [XmlElement("minSpeed")] public float MinSpeed { get; set; } = 20f;
    [XmlElement("maxSpeed")] public float MaxSpeed { get; set; } = 120f;
    [XmlElement("minSize")] public float MinSize { get; set; } = 1f;
    [XmlElement("maxSize")] public float MaxSize { get; set; } = 3f;
}

/// <summary>&lt;bullet&gt; element of bullets.xml: one projectile archetype.</summary>
public class BulletParams
{
    [XmlElement("name")] public string Name { get; set; } = "";
    [XmlElement("texture")] public string Texture { get; set; } = "";
    [XmlElement("sourceRect")] public string SourceRectRaw { get; set; }
    [XmlElement("width")] public int Width { get; set; } = 12;
    [XmlElement("height")] public int Height { get; set; } = 12;
    [XmlElement("speed")] public float Speed { get; set; } = 300f;
    [XmlElement("damage")] public int Damage { get; set; } = 10;
    [XmlElement("kind")] public string Kind { get; set; } = "straight";
    /// <summary>Turn rate in degrees/second toward the player (0 = no homing).</summary>
    [XmlElement("homing")] public float Homing { get; set; }
    [XmlElement("color")] public string ColorRaw { get; set; }
    [XmlElement("rotation")] public float Rotation { get; set; }

    [XmlIgnore] public Color Color => XmlConfig.ParseColor(ColorRaw, Color.White);
    [XmlIgnore] public Rectangle? SourceRect => XmlConfig.ParseSourceRect(SourceRectRaw);
    [XmlIgnore] public bool IsHoming => Homing > 0f;
    [XmlElement("searchRadius")] public float SearchRadius { get; set; }
    [XmlElement("hitPoints")] public int HitPoints { get; set; } = 1;
}

/// <summary>One bonus archetype dropped by enemies.</summary>
public class BonusParams
{
    [XmlElement("name")] public string Name { get; set; } = "";
    [XmlElement("texture")] public string Texture { get; set; } = "";
    [XmlElement("width")] public int Width { get; set; } = 16;
    [XmlElement("height")] public int Height { get; set; } = 16;
    [XmlElement("speed")] public float Speed { get; set; } = 80f;
    [XmlElement("color")] public string ColorRaw { get; set; }
    [XmlElement("amount")] public int Amount { get; set; } = 1;
    [XmlIgnore] public Color Color => XmlConfig.ParseColor(ColorRaw, Color.Yellow);
}

// ---------------------------------------------------------------------------
// Root documents
// ---------------------------------------------------------------------------

[XmlRoot("entities")]
public class EntitiesConfig
{
    [XmlElement("player")] public PlayerParams Player { get; set; }
    [XmlElement("enemy")] public EnemyParams Enemy { get; set; }
}

[XmlRoot("enemies")]
public class EnemiesConfig
{
    [XmlElement("enemyType")] public List<EnemyType> EnemyTypes { get; set; } = new();
}

[XmlRoot("decors")]
public class DecorsConfig
{
    [XmlElement("decorElement")] public List<DecorParams> Elements { get; set; } = new();
    [XmlElement("stars")] public StarsParams Stars { get; set; }
}

[XmlRoot("bullets")]
public class BulletsConfig
{
    [XmlElement("bullet")] public List<BulletParams> Bullets { get; set; } = new();
}

[XmlRoot("bonuses")]
public class BonusesConfig
{
    [XmlElement("bonus")] public List<BonusParams> Bonuses { get; set; } = new();
}
