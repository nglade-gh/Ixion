using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Projectile. Peut être droit, sinusoïdal (oscillation horizontale),
/// à tête chercheuse (homing) ou destructible (missiles ennemis).
/// </summary>
public class Bullet : Sprite
{
    /// <summary>
    /// Vecteur vitesse du projectile.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Dégâts infligés à la cible.
    /// </summary>
    public int Damage { get; }

    /// <summary>
    /// Taux de virage vers la cible, en degrés par seconde (0 = pas de homing).
    /// </summary>
    public float HomingTurnRate { get; }

    /// <summary>
    /// Cible poursuivie par le missile homing.
    /// </summary>
    public Entite Target { get; }

    /// <summary>
    /// Rayon de recherche avant activation du homing.
    /// </summary>
    public float SearchRadius { get; }

    /// <summary>
    /// Délai avant activation du homing, en secondes.
    /// </summary>
    public float HomingDelay { get; }

    /// <summary>
    /// Indique si le projectile est un missile homing destructible par le joueur.
    /// </summary>
    public bool IsHomingMissile { get; }

    /// <summary>
    /// Points de vie restants du projectile (les missiles ennemis ont 2 HP).
    /// </summary>
    public int CurrentHitPoints { get; private set; }

    private float _life = 8f;
    private float _homingTimer;

    // Sinusoidal movement (kind="sinusoidal")
    private bool _isSinusoidal;
    private float _sinusoidAmplitude;
    private float _sinusoidFrequency;
    private float _spawnX;
    private float _baseFallSpeed;
    private float _baseRotation;
    private float _age;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Bullet"/>.
    /// </summary>
    /// <param name="p">Paramètres du projectile.</param>
    /// <param name="texture">Texture du projectile.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="velocity">Vecteur vitesse initial.</param>
    /// <param name="target">Cible optionnelle pour le homing.</param>
    public Bullet(BulletParams p, Texture2D texture, Vector2 position, Vector2 velocity, Entite target = null)
        : base(texture, position, new Vector2(p.Width, p.Height))
    {
        _sourceRect = p.SourceRect;
        Origin = DefaultOrigin;
        Velocity = velocity;
        Damage = p.Damage;
        HomingTurnRate = p.Homing;
        Target = target;
        SearchRadius = p.SearchRadius;
        HomingDelay = 0.5f; // homing missiles activate after a short delay
        IsHomingMissile = false;
        CurrentHitPoints = p.HitPoints;
        Color = p.Color;
        Rotation = p.Rotation;
        _isSinusoidal = p.IsSinusoidal;
        _sinusoidAmplitude = p.SinusoidAmplitude;
        _sinusoidFrequency = p.SinusoidFrequency;
        _spawnX = position.X;
        _baseRotation = p.Rotation;
        _age = 0f;
        // Vitesse de chute initiale (le tir sinusoïdal descend droit, en Y).
        _baseFallSpeed = velocity.Y;
        if (_baseFallSpeed < 1f)
            _baseFallSpeed = velocity.Length();
    }

    /// <summary>Constructor for destructible homing missiles fired by enemies.</summary>
    public Bullet(BulletParams p, Texture2D texture, Vector2 position, Vector2 velocity, Entite target, bool isHomingMissile)
        : this(p, texture, position, velocity, target)
    {
        IsHomingMissile = isHomingMissile;
        if (IsHomingMissile && CurrentHitPoints <= 1)
            CurrentHitPoints = 2;
        HomingDelay = 0.5f;
    }

    /// <summary>
    /// Applique des dégâts au projectile. Retourne true si le projectile est détruit.
    /// </summary>
    /// <param name="amount">Montant de dégâts.</param>
    /// <returns>True si détruit, false sinon.</returns>
    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || !IsActive)
            return false;

        CurrentHitPoints -= amount;
        if (CurrentHitPoints <= 0)
        {
            IsActive = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Met à jour le projectile : homing, déplacement, durée de vie.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        bool canHome = HomingTurnRate > 0f && Target != null && Target.IsActive;
        if (canHome)
        {
            _homingTimer += dt;
            if (_homingTimer >= HomingDelay)
            {
                float dist = Vector2.Distance(Position, Target.Position);
                if (SearchRadius <= 0f || dist <= SearchRadius)
                {
                    Vector2 toTarget = Target.Position - Position;
                    float targetAngle = (float)Math.Atan2(toTarget.Y, toTarget.X);
                    float currentAngle = (float)Math.Atan2(Velocity.Y, Velocity.X);
                    float diff = targetAngle - currentAngle;
                    while (diff > MathHelper.Pi)
                        diff -= MathHelper.TwoPi;
                    while (diff < -MathHelper.Pi)
                        diff += MathHelper.TwoPi;

                    float maxTurn = MathHelper.ToRadians(HomingTurnRate) * dt;
                    float newAngle = currentAngle + MathHelper.Clamp(diff, -maxTurn, maxTurn);
                    float speed = Velocity.Length();
                    Velocity = new Vector2((float)Math.Cos(newAngle), (float)Math.Sin(newAngle)) * speed;
                    Rotation = MathHelper.ToDegrees(newAngle);
                }
            }
        }

        if (_isSinusoidal)
        {
            // Sinusoidal : descente droite + oscillation horizontale (sinusoïde).
            _age += dt;
            float dxdt = _sinusoidAmplitude * _sinusoidFrequency * (float)Math.Cos(_sinusoidFrequency * _age);
            Position.X = _spawnX + _sinusoidAmplitude * (float)Math.Sin(_sinusoidFrequency * _age);
            Position.Y += _baseFallSpeed * dt;
            // Incline légèrement le sprite dans le sens du mouvement horizontal.
            Rotation = _baseRotation + MathHelper.ToDegrees((float)Math.Atan2(dxdt, _baseFallSpeed));
        }
        else
        {
            Position += Velocity * dt;
        }
        _life -= dt;

        float margin = 40f;
        float screenW = ScreenManager._Instance._Dimensions.X;
        float screenH = ScreenManager._Instance._Dimensions.Y;
        if (_life <= 0f ||
            Position.X < -margin || Position.X > screenW + margin ||
            Position.Y < -margin || Position.Y > screenH + margin)
            IsActive = false;
    }
}
