using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Bonus tombant depuis un ennemi détruit. Collectable par le joueur.
/// </summary>
public class Bonus : Sprite
{
    /// <summary>
    /// Paramètres de configuration du bonus.
    /// </summary>
    public BonusParams Params { get; }

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Bonus"/>.
    /// </summary>
    /// <param name="p">Paramètres du bonus.</param>
    /// <param name="texture">Texture du bonus.</param>
    /// <param name="position">Position initiale.</param>
    public Bonus(BonusParams p, Texture2D texture, Vector2 position)
        : base(texture, position, new Vector2(p.Width, p.Height))
    {
        Params = p;
        Scale = 1f;
        Color = p.Color;
    }

    /// <summary>
    /// Fait descendre le bonus et le désactive quand il sort de l'écran.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsActive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position = new Vector2(Position.X, Position.Y + Params.Speed * dt);

        float screenH = ScreenManager._Instance._Dimensions.Y;
        if (Position.Y > screenH + 40f)
            IsActive = false;
    }
}
