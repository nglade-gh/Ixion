using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Étoile de fond pour l'effet parallax.
/// </summary>
public class Star : Sprite
{
    /// <summary>
    /// Vitesse de défilement de l'étoile, en px/s.
    /// </summary>
    public float Speed { get; set; }

    private readonly Random _random;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Star"/>.
    /// </summary>
    /// <param name="texture">Texture de l'étoile.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="speed">Vitesse de défilement.</param>
    /// <param name="size">Taille en pixels.</param>
    /// <param name="color">Couleur de l'étoile.</param>
    /// <param name="random">Générateur aléatoire pour un léger scintillement.</param>
    public Star(Texture2D texture, Vector2 position, float speed, float size, Color color, Random random)
        : base(texture, position, new Vector2(size, size))
    {
        Speed = speed;
        Color = color;
        _random = random;
        Origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
    }

    /// <summary>
    /// Fait défiler l'étoile vers le bas et la recycle en haut de l'écran.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position.Y += Speed * dt;

        float screenW = ScreenManager._Instance._Dimensions.X;
        float screenH = ScreenManager._Instance._Dimensions.Y;
        if (Position.Y > screenH + 4f)
        {
            Position.Y = -4f;
            Position.X = (float)(_random.NextDouble() * screenW);
        }
    }
}
