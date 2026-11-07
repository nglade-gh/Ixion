using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Ennemi concret. Définit le comportement de déplacement et d'attaque
/// selon son <see cref="EnemyType"/> chargé depuis la configuration XML.
/// </summary>
public class Enemy : Creature
{
    private const float ChargeDelay = 1.1f;

    private readonly float _phase;
    private float _zigzagDir = 1f;

    /// <summary>
    /// Cible actuelle du pattern de mouvement <see cref="MovePattern.Charge"/>.
    /// </summary>
    public Vector2 TargetPosition { get; set; }

    /// <summary>
    /// Couleur des particules d'explosion lors de la destruction.
    /// </summary>
    public Color ExplosionColor { get; set; } = Color.Orange;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Enemy"/>.
    /// </summary>
    /// <param name="type">Archetype de l'ennemi.</param>
    /// <param name="texture">Texture de l'ennemi.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="sourceRect">Sous-rectangle optionnel de la texture.</param>
    /// <param name="wave">Numéro de vague (influence la vitesse).</param>
    public Enemy(EnemyType type, Texture2D texture, Vector2 position, Rectangle? sourceRect, float wave)
        : base(texture, position, new Vector2(type.Size, type.Size))
    {
        _sourceRect = sourceRect;
        Origin = DefaultOrigin;
        Scale = type.Scale;
        MaxHP = type.Hp;
        HP = MaxHP;
        Speed = type.Speed * (1f + wave * 0.05f);
        FireRate = 5f / type.FireRate;
        ScoreValue = type.Score;
        MovePattern = type.MovePattern;
        Amplitude = type.Amplitude;
        Frequency = type.Frequency;
        BulletPattern = type.BulletPattern;
        ExplosionColor = type.Color;
        _phase = position.X * 0.017f; // desyncs instances of the same pattern
        Velocity = new Vector2(0f, Speed);
    }

    /// <summary>
    /// Met à jour le mouvement selon le pattern et supprime l'ennemi hors écran.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MoveTimer += dt;

        switch (MovePattern)
        {
            case MovePattern.Sine:
                Velocity = new Vector2(
                    (float)Math.Cos(MoveTimer * Frequency + _phase) * Amplitude * Frequency,
                    Speed);
                break;

            case MovePattern.Horde:
                Velocity = new Vector2(
                    (float)Math.Sin(MoveTimer * Frequency + _phase) * Amplitude * 0.35f * Frequency,
                    Speed);
                break;

            case MovePattern.Zigzag:
                _zigzagDir = Math.Sign(Math.Sin(MoveTimer * MathHelper.TwoPi / 0.9f));
                if (_zigzagDir == 0f)
                    _zigzagDir = 1f;
                Velocity = new Vector2(_zigzagDir * Speed * 0.7f, Speed * 0.6f);
                break;

            case MovePattern.Charge:
                if (MoveTimer < ChargeDelay)
                {
                    Velocity = new Vector2(0f, Speed);
                }
                else
                {
                    Vector2 toTarget = TargetPosition - Position;
                    Vector2 desired = Vector2.Normalize(toTarget) * Speed * 2.1f;
                    Velocity = Vector2.Lerp(Velocity, desired, MathHelper.Clamp(3f * dt, 0f, 1f));
                }
                break;

            case MovePattern.Swarm:
                // Chaotic, jittery movement: multiple sine waves at different frequencies/scales
                float swarmX1 = (float)(Math.Cos(MoveTimer * Frequency * 1.5f + _phase) * Amplitude);
                float swarmX2 = (float)(Math.Sin(MoveTimer * Frequency * 2.7f + _phase * 0.7) * Amplitude * 0.5f);
                float swarmX3 = (float)(Math.Cos(MoveTimer * Frequency * 0.8f + _phase * 1.3f) * Amplitude * 0.3f);
                Velocity = new Vector2((swarmX1 + swarmX2 + swarmX3) * 0.8f, Speed * 1.2f);
                break;
        }

        base.Update(gameTime);

        // Remove when it leaves the screen (bottom or sides)
        float screenW = ScreenManager._Instance._Dimensions.X;
        float screenH = ScreenManager._Instance._Dimensions.Y;
        if (Position.Y > screenH + 80f || Position.X < -80f || Position.X > screenW + 80f)
            IsActive = false;
    }
}
