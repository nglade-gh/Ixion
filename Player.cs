using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Vaisseau du joueur. Gère les déplacements, les tirs, les upgrades et la collecte de bonus.
/// </summary>
public class Player : Personnage
{
    private Rectangle _playArea;

    // Fire modes
    /// <summary>
    /// Cooldown du tir primaire, en secondes.
    /// </summary>
    public float CooldownPrimary { get; private set; }

    /// <summary>
    /// Cooldown du tir secondaire, en secondes.
    /// </summary>
    public float CooldownSecondary { get; private set; }

    /// <summary>
    /// Cooldown du tir spécial, en secondes.
    /// </summary>
    public float CooldownSpecial { get; private set; }

    // Upgrades
    /// <summary>
    /// Indique si le tir primaire est amélioré.
    /// </summary>
    public bool PrimaryUpgraded { get; private set; }

    /// <summary>
    /// Temps restant de l'upgrade primaire, en secondes.
    /// </summary>
    public float PrimaryUpgradeTimer { get; private set; }

    /// <summary>
    /// Munitions restantes pour l'upgrade primaire (0 = temps limité).
    /// </summary>
    public int PrimaryUpgradeAmmo { get; private set; }

    /// <summary>
    /// Durée totale de l'upgrade primaire, en secondes.
    /// </summary>
    public float PrimaryUpgradeDuration { get; private set; }

    /// <summary>
    /// Indique si le tir secondaire est amélioré.
    /// </summary>
    public bool SecondaryUpgraded { get; private set; }

    /// <summary>
    /// Temps restant de l'upgrade secondaire, en secondes.
    /// </summary>
    public float SecondaryUpgradeTimer { get; private set; }

    /// <summary>
    /// Munitions restantes pour l'upgrade secondaire (0 = temps limité).
    /// </summary>
    public int SecondaryUpgradeAmmo { get; private set; }

    /// <summary>
    /// Durée totale de l'upgrade secondaire, en secondes.
    /// </summary>
    public float SecondaryUpgradeDuration { get; private set; }

    /// <summary>
    /// Munitions disponibles pour le tir spécial.
    /// </summary>
    public int SpecialAmmo { get; set; }

    // Shield / structure
    /// <summary>
    /// Énergie du bouclier (0-100). Se régénère à 1% toutes les 2 secondes.
    /// </summary>
    public float ShieldEnergy { get; set; }

    /// <summary>
    /// Points de structure du vaisseau (100 max). Ne se régénèrent pas naturellement.
    /// </summary>
    public int StructurePoints => HP;

    /// <summary>
    /// Indique si le bouclier est actif.
    /// </summary>
    public bool HasShield => ShieldEnergy > 0.01f;

    /// <summary>
    /// Timer de régénération du bouclier.
    /// </summary>
    private float _shieldRegenTimer;

    /// <summary>
    /// Intervalle de régénération du bouclier, en secondes (1% toutes les 2s).
    /// </summary>
    private const float ShieldRegenInterval = 2f;

    /// <summary>
    /// Quantité de régénération par intervalle (1% de 100 = 1 point).
    /// </summary>
    private const float ShieldRegenAmount = 1f;

    /// <summary>
    /// Multiplicateur de régénération (dépend de la difficulté).
    /// </summary>
    public float ShieldRegenMultiplier { get; set; } = 1f;

    /// <summary>
    /// Animation/instance du bouclier visuel.
    /// </summary>
    public Shield Shield { get; set; }

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Player"/>.
    /// </summary>
    /// <param name="p">Paramètres du joueur.</param>
    /// <param name="texture">Texture du vaisseau.</param>
    /// <param name="position">Position initiale.</param>
    public Player(PlayerParams p, Texture2D texture, Vector2 position)
        : base(texture, position, new Vector2(p.Width, p.Height))
    {
        Scale = p.Scale;
        Speed = p.MaxSpeed;
        MaxHP = 100;
        HP = MaxHP;
        RegenRate = p.RegenRate;
        FireRate = p.FireRate;
        Lives = p.Lives;
        Score = p.InitialScore;
        Rotation = p.Rotation; // -90 from XML
        InvincibleDuration = p.InvincibilityDuration;
        _playArea = Rectangle.Empty;

        ShieldEnergy = 100f;
        _shieldRegenTimer = 0f;

        CooldownPrimary = 0f;
        CooldownSecondary = 0f;
        CooldownSpecial = 0f;
        PrimaryUpgraded = false;
        SecondaryUpgraded = false;
        SpecialAmmo = 0;
    }

    /// <summary>
    /// Définit la zone de déplacement autorisée du joueur.
    /// </summary>
    /// <param name="area">Rectangle de la zone de jeu.</param>
    public void SetPlayArea(Rectangle area) => _playArea = area;

    /// <summary>
    /// Met à jour le joueur : entrées, déplacement, timers d'upgrade, cooldowns.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        Vector2 input = Vector2.Zero;
        if (InputService.Instance.IsActionHeld("MoveLeft"))
            input.X -= 1f;
        if (InputService.Instance.IsActionHeld("MoveRight"))
            input.X += 1f;
        if (InputService.Instance.IsActionHeld("MoveUp"))
            input.Y -= 1f;
        if (InputService.Instance.IsActionHeld("MoveDown"))
            input.Y += 1f;
        if (input != Vector2.Zero)
            input.Normalize();

        Velocity = input * Speed;
        base.Update(gameTime);

        if (_playArea.Width > 0)
        {
            float hw = Size.X * Scale / 2f;
            float hh = Size.Y * Scale / 2f;
            Position.X = Math.Clamp(Position.X, _playArea.Left + hw, _playArea.Right - hw);
            Position.Y = Math.Clamp(Position.Y, _playArea.Top + hh, _playArea.Bottom - hh);
        }

        // Countdown upgrade timers
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (PrimaryUpgraded && PrimaryUpgradeDuration > 0f)
        {
            PrimaryUpgradeTimer -= dt;
            if (PrimaryUpgradeTimer <= 0f)
                ResetPrimaryUpgrade();
        }
        if (SecondaryUpgraded && SecondaryUpgradeDuration > 0f)
        {
            SecondaryUpgradeTimer -= dt;
            if (SecondaryUpgradeTimer <= 0f)
                ResetSecondaryUpgrade();
        }

        // Shield regeneration (1% every 2 seconds, scaled by difficulty)
        if (ShieldEnergy < 100f)
        {
            _shieldRegenTimer += dt;
            if (_shieldRegenTimer >= ShieldRegenInterval)
            {
                _shieldRegenTimer -= ShieldRegenInterval;
                ShieldEnergy = Math.Min(100f, ShieldEnergy + ShieldRegenAmount * ShieldRegenMultiplier);
            }
        }

        // Cooldowns
        if (CooldownPrimary > 0f) CooldownPrimary -= dt;
        if (CooldownSecondary > 0f) CooldownSecondary -= dt;
        if (CooldownSpecial > 0f) CooldownSpecial -= dt;

        // Shield animation
        Shield?.Update(gameTime);
    }

    /// <summary>
    /// Tente de déclencher le tir primaire (30/s).
    /// </summary>
    /// <returns>True si le tir est autorisé.</returns>
    public bool TryFirePrimary()
    {
        if (CooldownPrimary > 0f) return false;
        CooldownPrimary = PrimaryUpgraded ? 0.033f : 0.1f; // 10/sec base, 30/sec when upgraded
        return true;
    }

    /// <summary>
    /// Tente de déclencher le tir secondaire (2/s, dégâts doublés si upgrade).
    /// </summary>
    /// <returns>True si le tir est autorisé.</returns>
    public bool TryFireSecondary()
    {
        float rate = SecondaryUpgraded ? 0.25f : 0.5f; // faster when upgraded
        if (CooldownSecondary > 0f) return false;
        if (SecondaryUpgraded && SecondaryUpgradeAmmo <= 0 && SecondaryUpgradeDuration > 0f)
        {
            ResetSecondaryUpgrade();
            return false;
        }
        CooldownSecondary = rate;
        if (SecondaryUpgraded && SecondaryUpgradeAmmo > 0)
            SecondaryUpgradeAmmo--;
        return true;
    }

    /// <summary>
    /// Tente de déclencher le tir spécial (consomme une munition).
    /// </summary>
    /// <returns>True si le tir est autorisé.</returns>
    public bool TryFireSpecial()
    {
        if (CooldownSpecial > 0f) return false;
        if (SpecialAmmo <= 0) return false;
        CooldownSpecial = 2f;
        SpecialAmmo--;
        return true;
    }

    /// <summary>
    /// Applique un upgrade au tir primaire (temps limité et/ou munitions limitées).
    /// </summary>
    /// <param name="duration">Durée de l'upgrade en secondes.</param>
    /// <param name="ammo">Nombre de munitions (0 = temps seulement).</param>
    public void ApplyPrimaryUpgrade(float duration, int ammo)
    {
        PrimaryUpgraded = true;
        PrimaryUpgradeDuration = duration;
        PrimaryUpgradeTimer = duration;
        PrimaryUpgradeAmmo = ammo;
    }

    /// <summary>
    /// Applique un upgrade au tir secondaire (temps limité et/ou munitions limitées).
    /// </summary>
    /// <param name="duration">Durée de l'upgrade en secondes.</param>
    /// <param name="ammo">Nombre de munitions (0 = temps seulement).</param>
    public void ApplySecondaryUpgrade(float duration, int ammo)
    {
        SecondaryUpgraded = true;
        SecondaryUpgradeDuration = duration;
        SecondaryUpgradeTimer = duration;
        SecondaryUpgradeAmmo = ammo;
    }

    /// <summary>
    /// Ajoute des munitions pour le tir spécial.
    /// </summary>
    /// <param name="amount">Nombre de munitions à ajouter.</param>
    public void AddSpecialAmmo(int amount) => SpecialAmmo += amount;

    /// <summary>
    /// Répare le bouclier de la quantité donnée.
    /// </summary>
    /// <param name="amount">Quantité de réparation.</param>
    public void RepairShield(float amount)
    {
        ShieldEnergy = Math.Min(100f, ShieldEnergy + Math.Max(0f, amount));
    }

    /// <summary>
    /// Répare la structure du vaisseau de la quantité donnée.
    /// </summary>
    /// <param name="amount">Quantité de réparation.</param>
    public void RepairStructure(int amount)
    {
        HP = Math.Min(MaxHP, HP + Math.Max(0, amount));
    }

    /// <summary>
    /// Applique des dégâts au joueur. Le bouclier absorbe les dégâts en priorité.
    /// </summary>
    /// <param name="amount">Montant de dégâts.</param>
    /// <returns>True si le joueur a été tué (structure à 0), false sinon.</returns>
    public new bool TakeDamage(int amount)
    {
        if (amount <= 0) return false;

        if (ShieldEnergy > 0f)
        {
            // Shield absorbs damage first
            ShieldEnergy = Math.Max(0f, ShieldEnergy - 10f);
            // Sync visual intensity with energy level
            Shield?.TriggerBlast();
            if (Shield != null)
                Shield.Intensity = ShieldEnergy / 100f;
            return false;
        }
        else
        {
            // Damage structure
            HP -= amount;
            if (HP <= 0)
            {
                HP = 0;
                IsActive = false;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Réinitialise l'upgrade du tir primaire.
    /// </summary>
    private void ResetPrimaryUpgrade()
    {
        PrimaryUpgraded = false;
        PrimaryUpgradeTimer = 0f;
        PrimaryUpgradeAmmo = 0;
        PrimaryUpgradeDuration = 0f;
    }

    /// <summary>
    /// Réinitialise l'upgrade du tir secondaire.
    /// </summary>
    private void ResetSecondaryUpgrade()
    {
        SecondaryUpgraded = false;
        SecondaryUpgradeTimer = 0f;
        SecondaryUpgradeAmmo = 0;
        SecondaryUpgradeDuration = 0f;
    }

    /// <summary>
    /// Dessine le vaisseau du joueur avec un effet de clignotement pendant l'invincibilité.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;
        Color = Invincible
            ? new Color((byte)255, (byte)255, (byte)255, (byte)(140 + 115 * Math.Sin(InvincibleTimer * 25f)))
            : Color.White;
        base.Draw(spriteBatch);
    }
}
