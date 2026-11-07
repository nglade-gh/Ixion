using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Explosion animée à partir d'une série de sprites. Joue la séquence puis disparaît.
/// </summary>
public class Explosion : Sprite
{
    /// <summary>
    /// Frames de l'animation.
    /// </summary>
    public Texture2D[] Frames { get; }

    /// <summary>
    /// Durée de chaque frame en secondes.
    /// </summary>
    public float FrameDuration { get; }

    /// <summary>
    /// Durée totale de l'animation.
    /// </summary>
    public float TotalDuration { get; }

    /// <summary>
    /// Temps écoulé depuis le début.
    /// </summary>
    public float Elapsed { get; private set; }

    /// <summary>
    /// Index de la frame courante.
    /// </summary>
    public int CurrentFrame { get; private set; }

    /// <summary>
    /// Indique si l'animation est terminée.
    /// </summary>
    public bool IsFinished => CurrentFrame >= Frames.Length;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Explosion"/>.
    /// </summary>
    /// <param name="frames">Frames de l'animation.</param>
    /// <param name="position">Position de l'explosion.</param>
    /// <param name="frameDuration">Durée de chaque frame en secondes.</param>
    /// <param name="scale">Échelle d'affichage.</param>
    public Explosion(Texture2D[] frames, Vector2 position, float frameDuration = 0.05f, float scale = 1f)
        : base(frames.Length > 0 ? frames[0] : null, position, Vector2.Zero)
    {
        Frames = frames;
        FrameDuration = frameDuration;
        TotalDuration = frames.Length * frameDuration;
        Scale = scale;
        Origin = new Vector2(frames[0].Width / 2f, frames[0].Height / 2f);
    }

    /// <summary>
    /// Met à jour l'animation de l'explosion.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsActive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Elapsed += dt;
        CurrentFrame = (int)(Elapsed / FrameDuration);
        if (CurrentFrame >= Frames.Length)
            IsActive = false;
    }

    /// <summary>
    /// Dessine la frame courante de l'explosion.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive || Frames == null || Frames.Length == 0) return;
        int frame = Math.Clamp(CurrentFrame, 0, Frames.Length - 1);
        spriteBatch.Draw(Frames[frame], Position, null, Color, Rotation, Origin, Scale, SpriteEffects.None, 0f);
    }
}
