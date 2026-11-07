using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Particule d'explosion ou d'effet visuel.
/// </summary>
public class Particle : Sprite
{
    /// <summary>
    /// Vecteur vitesse de la particule.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Durée de vie restante, en secondes.
    /// </summary>
    public float Life { get; private set; }

    /// <summary>
    /// Durée de vie initiale.
    /// </summary>
    public float MaxLife { get; }

    private Color _baseColor;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Particle"/>.
    /// </summary>
    /// <param name="texture">Texture de la particule.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="velocity">Vecteur vitesse initial.</param>
    /// <param name="life">Durée de vie en secondes.</param>
    /// <param name="color">Couleur de base.</param>
    /// <param name="size">Taille initiale.</param>
    public Particle(Texture2D texture, Vector2 position, Vector2 velocity, float life, Color color, float size)
        : base(texture, position, new Vector2(size, size))
    {
        Velocity = velocity;
        MaxLife = Life = life;
        _baseColor = color;
        Origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
    }

    /// <summary>
    /// Met à jour la position, la vitesse et la durée de vie.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * dt;
        Velocity *= MathHelper.Clamp(1f - 1.8f * dt, 0f, 1f);
        Life -= dt;
        if (Life <= 0f)
            IsActive = false;
    }

    /// <summary>
    /// Dessine la particule avec fondu et redimensionnement selon sa durée de vie.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;
        float t = MathHelper.Clamp(Life / MaxLife, 0f, 1f);
        Color = Color.Lerp(Color.Transparent, _baseColor, t);
        Scale = 0.4f + 0.6f * t;
        base.Draw(spriteBatch);
    }
}
