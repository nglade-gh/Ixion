using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Classe abstraite intermédiaire entre <see cref="Sprite"/> et les entités mobiles.
/// Ajoute la physique (vitesse/accélération), les points de vie, la régénération et le tir.
/// </summary>
public abstract class Entite : Sprite
{
    /// <summary>
    /// Vitesse courante en px/s.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Accélération courante, remise à zéro chaque frame.
    /// </summary>
    public Vector2 Acceleration { get; set; }

    /// <summary>
    /// Vitesse maximale autorisée.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Points de vie courants.
    /// </summary>
    public int HP { get; set; }

    /// <summary>
    /// Points de vie maximum.
    /// </summary>
    public int MaxHP { get; set; }

    /// <summary>
    /// Points de vie regagnés par seconde (0 = pas de régénération).
    /// </summary>
    public float RegenRate { get; set; }

    /// <summary>
    /// Temps restant avant le prochain tir, en secondes.
    /// </summary>
    public float FireCooldown { get; set; }

    /// <summary>
    /// Intervalle de base entre deux tirs, en secondes.
    /// </summary>
    public float FireRate { get; set; }

    private float _regenAccumulator;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Entite"/>.
    /// </summary>
    /// <param name="texture">Texture de l'entité.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="size">Taille logique.</param>
    protected Entite(Texture2D texture, Vector2 position, Vector2 size)
        : base(texture, position, size)
    {
        HP = MaxHP;
    }

    /// <summary>
    /// Applique des dégâts à l'entité. Retourne true si l'entité meurt.
    /// </summary>
    /// <param name="amount">Montant des dégâts.</param>
    /// <returns>True si l'entité est tuée.</returns>
    public bool TakeDamage(int amount)
    {
        if (!IsActive)
            return false;
        HP -= amount;
        if (HP <= 0)
        {
            HP = 0;
            IsActive = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tente de déclencher un tir. Retourne true et arme le cooldown si autorisé.
    /// </summary>
    /// <returns>True si le tir est autorisé.</returns>
    public bool TryFire()
    {
        if (!IsActive || FireRate <= 0f || FireCooldown > 0f)
            return false;
        FireCooldown = FireRate;
        return true;
    }

    /// <summary>
    /// Met à jour la physique, le cooldown de tir et la régénération.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Velocity += Acceleration * dt;
        Acceleration = Vector2.Zero;
        Position += Velocity * dt;

        if (FireCooldown > 0f)
            FireCooldown -= dt;

        if (RegenRate > 0f && HP < MaxHP)
        {
            _regenAccumulator += RegenRate * dt;
            if (_regenAccumulator >= 1f)
            {
                int gained = (int)_regenAccumulator;
                _regenAccumulator -= gained;
                HP = Math.Min(MaxHP, HP + gained);
            }
        }
    }
}
