using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Élément de décor défilant (murs, rochers, lueurs).
/// Supporte le parallax par couches.
/// </summary>
public abstract class Decors : Sprite
{
    /// <summary>
    /// Vitesse de défilement vers le bas, en px/s.
    /// </summary>
    public float ScrollSpeed { get; set; }

    /// <summary>
    /// Couche de rendu : 0 = arrière-plan, 1 = milieu, 2 = premier plan.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Indique si l'élément défile (effet parallax).
    /// </summary>
    public bool IsParallax { get; set; }

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Decors"/>.
    /// </summary>
    /// <param name="texture">Texture du décor.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="size">Taille logique.</param>
    protected Decors(Texture2D texture, Vector2 position, Vector2 size)
        : base(texture, position, size)
    {
    }

    /// <summary>
    /// Met à jour la position si l'élément est en mode parallax.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsActive || !IsParallax)
            return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position.Y += ScrollSpeed * dt;
    }
}
