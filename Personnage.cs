using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Classe abstraite pour un personnage contrôlé (joueur).
/// Ajoute le score, les vies et une invincibilité temporaire après avoir été touché.
/// </summary>
public abstract class Personnage : Entite
{
    public int Score { get; set; }

    /// <summary>
    /// Score accumulé par le personnage.
    /// </summary>
    public int Lives { get; set; }

    /// <summary>
    /// Temps d'invincibilité restant, en secondes.
    /// </summary>
    public float InvincibleTimer { get; set; }

    /// <summary>
    /// Durée d'invincibilité après avoir été touché, en secondes.
    /// </summary>
    public float InvincibleDuration { get; set; } = 2f;

    /// <summary>
    /// Indique si le personnage est actuellement invincible.
    /// </summary>
    public bool Invincible => InvincibleTimer > 0f;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Personnage"/>.
    /// </summary>
    /// <param name="texture">Texture du personnage.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="size">Taille logique.</param>
    protected Personnage(Texture2D texture, Vector2 position, Vector2 size)
        : base(texture, position, size)
    {
    }

    /// <summary>
    /// Consomme une vie et accorde une invincibilité temporaire.
    /// Ignoré si le personnage est déjà invincible ou inactif.
    /// </summary>
    public void TakeHit()
    {
        if (!IsActive || Invincible)
            return;
        Lives--;
        InvincibleTimer = InvincibleDuration;
        if (Lives <= 0)
        {
            Lives = 0;
            IsActive = false;
        }
    }

    /// <summary>
    /// Met à jour le timer d'invincibilité.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (InvincibleTimer > 0f)
            InvincibleTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }
}
