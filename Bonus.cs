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
    /// Texture 1x1 blanche pour dessiner le fond coloré semi-transparent.
    /// </summary>
    private static Texture2D _whitePixel;
    private static readonly object _pixelLock = new object();
    private static GraphicsDevice _device;

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
        Rotation = p.Rotation;
    }

    /// <summary>
    /// Initialise la texture 1x1 blanche partagée (appelée une fois au chargement).
    /// </summary>
    internal static void Initialize(GraphicsDevice device)
    {
        if (_whitePixel != null) return;
        lock (_pixelLock)
        {
            if (_whitePixel == null)
            {
                _device = device;
                _whitePixel = new Texture2D(device, 1, 1);
                _whitePixel.SetData(new[] { Color.White });
            }
        }
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

    /// <summary>
    /// Dessine le bonus avec un fond de bouclier coloré semi-transparent
    /// indiquant la catégorie du bonus.
    /// Munitions → fond rouge, structure/vie → fond vert.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive || _texture == null)
            return;

        Color border = DetermineBorderColor();

        // Dessiner le bonus par-dessus (en premier pour que le halo passe derrière)
        base.Draw(spriteBatch);

        // Dessiner un fond rectangulaire coloré autour du bonus
        float pad = 24f; // marge suffisante pour un halo visible (bonus 48px × 3 = 144px)
        float bw = Size.X * Scale + pad * 2f;
        float bh = Size.Y * Scale + pad * 2f;
        float alpha = 0.3f; // 30% d'opacité = halo transparent mais perceptible

        Vector2 center = new Vector2(Position.X, Position.Y);
        spriteBatch.Draw(_whitePixel, center, null,
            new Color(border.R, border.G, border.B, (byte)(alpha * 255)),
            0f, new Vector2(bw / 2f, bh / 2f),
            new Vector2(bw, bh), SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Retourne la couleur du fond selon le type de bonus.
    /// Munitions/ammo → rouge, structure/vie/shield → vert.
    /// </summary>
    private Color DetermineBorderColor()
    {
        string name = Params.Name;
        if (string.IsNullOrEmpty(name))
            return Color.White;

        string lower = name.ToLowerInvariant();

        // Munitions et upgrades d'armes → rouge
        if (lower.Contains("ammo") || lower.Contains("missile") ||
            lower.Contains("upgrade") || lower.Contains("power") ||
            lower.Contains("triple") || lower.Contains("diagonal"))
            return new Color(255, 50, 50); // rouge

        // Structure, shield, boost → vert
        if (lower.Contains("repair") || lower.Contains("shield") || lower.Contains("boost") ||
            lower.Contains("rapid"))
            return new Color(50, 200, 50); // vert

        // Défaut : blanc
        return Color.White;
    }
}
