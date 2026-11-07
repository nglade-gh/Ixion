using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Pattern de mouvement d'une créature.
/// </summary>
public enum MovePattern
{
    /// <summary>
    /// Oscillation horizontale sinusoïdale douce pendant la descente.
    /// </summary>
    Sine,
    /// <summary>
    /// Dérive douce en groupe, avec un léger mouvement de balancement.
    /// </summary>
    Horde,
    /// <summary>
    /// Descente puis plongeon direct vers le joueur.
    /// </summary>
    Charge,
    /// <summary>
    /// Changements de direction brusques gauche/droite pendant la descente.
    /// </summary>
    Zigzag,
    /// <summary>
    /// Mouvement chaotique avec multiples oscillations sinusoïdales à différentes fréquences.
    /// </summary>
    Swarm
}

/// <summary>
/// Pattern d'attaque d'une créature.
/// </summary>
public enum BulletPattern
{
    /// <summary>
    /// Un seul projectile tiré droit vers le bas.
    /// </summary>
    Simple,
    /// <summary>
    /// Barrage de plusieurs projectiles en éventail.
    /// </summary>
    Burst,
    /// <summary>
    /// projectile lent qui suit le joueur.
    /// </summary>
    Homing,
    /// <summary>
    /// Tirs rapides en dispersion (swarm).
    /// </summary>
    Swarm,
    /// <summary>
    /// Tir en éventail de 3 projectiles.
    /// </summary>
    Spread,
    /// <summary>
    /// Rotation spirale autour du joueur.
    /// </summary>
    Spiral
}

/// <summary>
/// Créature animée, mobile et capable de tirer (ennemis).
/// Définit les patterns de mouvement et d'attaque.
/// </summary>
public abstract class Creature : Entite
{
    /// <summary>
    /// Temps écoulé depuis l'apparition de la créature.
    /// </summary>
    public float MoveTimer { get; protected set; }

    /// <summary>
    /// Pattern de mouvement utilisé par la créature.
    /// </summary>
    public MovePattern MovePattern { get; set; }

    /// <summary>
    /// Amplitude des oscillations horizontales, en pixels.
    /// </summary>
    public float Amplitude { get; set; }

    /// <summary>
    /// Fréquence des oscillations (rad/s dans les termes sinus/cosinus).
    /// </summary>
    public float Frequency { get; set; } = 1f;

    /// <summary>
    /// Points accordés lorsque la créature est détruite.
    /// </summary>
    public int ScoreValue { get; set; }

    /// <summary>
    /// Pattern d'attaque à distance.
    /// </summary>
    public BulletPattern BulletPattern { get; set; } = BulletPattern.Simple;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Creature"/>.
    /// </summary>
    /// <param name="texture">Texture de la créature.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="size">Taille logique.</param>
    protected Creature(Texture2D texture, Vector2 position, Vector2 size)
        : base(texture, position, size)
    {
    }
}
