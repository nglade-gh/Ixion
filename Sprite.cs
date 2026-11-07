using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Classe abstraite racine de tout objet dessiné.
/// Gère la texture, la position, la taille, l'échelle, la couleur, la rotation et l'origine de rendu.
/// </summary>
public abstract class Sprite
{
    protected Texture2D _texture;

    /// <summary>
    /// Sous-rectangle optionnel de la texture (sprite sheets). Null = texture entière.
    /// </summary>
    protected Rectangle? _sourceRect;

    /// <summary>
    /// Position centrale du sprite sur l'écran.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Taille logique sur l'écran, en pixels (avant <see cref="Scale"/>).
    /// </summary>
    public Vector2 Size { get; set; }

    /// <summary>
    /// Multiplicateur uniforme appliqué à la taille (utilisé pour réduire les assets).
    /// </summary>
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// Tinte de rendu appliquée à la texture.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Rotation en degrés (sens horaire sur l'écran).
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Point d'origine dans la texture source. Défaut : centre de la texture.
    /// </summary>
    public Vector2 Origin { get; set; }

    /// <summary>
    /// Indique si le sprite est actif et doit être rendu/mis à jour.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Sprite"/>.
    /// </summary>
    /// <param name="texture">Texture du sprite.</param>
    /// <param name="position">Position centrale initiale.</param>
    /// <param name="size">Taille logique.</param>
    protected Sprite(Texture2D texture, Vector2 position, Vector2 size)
    {
        _texture = texture;
        Position = position;
        Size = size;
        Origin = DefaultOrigin;
    }

    protected virtual Vector2 DefaultOrigin =>
        _sourceRect.HasValue
            ? new Vector2(_sourceRect.Value.X + _sourceRect.Value.Width / 2f,
                          _sourceRect.Value.Y + _sourceRect.Value.Height / 2f)
            : new Vector2(_texture.Width / 2f, _texture.Height / 2f);

    /// <summary>
    /// Rectangle englobant orienté (AABB) du sprite, centré sur <see cref="Position"/>.
    /// </summary>
    public Rectangle Bounds
    {
        get
        {
            float w = Size.X * Scale;
            float h = Size.Y * Scale;
            float r = MathHelper.ToRadians(Rotation);
            float c = Math.Abs((float)Math.Cos(r));
            float s = Math.Abs((float)Math.Sin(r));
            float bw = w * c + h * s;
            float bh = w * s + h * c;
            return new Rectangle(
                (int)(Position.X - bw / 2f),
                (int)(Position.Y - bh / 2f),
                Math.Max(1, (int)bw),
                Math.Max(1, (int)bh));
        }
    }

    /// <summary>
    /// Met à jour la logique du sprite. Aucune opération par défaut.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Dessine le sprite avec les paramètres courants.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive || _texture == null)
            return;

        Rectangle src = _sourceRect ?? _texture.Bounds;
        Vector2 drawScale = new Vector2(
            Size.X * Scale / src.Width,
            Size.Y * Scale / src.Height);

        spriteBatch.Draw(_texture, Position, src, Color,
            MathHelper.ToRadians(Rotation), Origin, drawScale, SpriteEffects.None, 0f);
    }

    // ---------- shared texture factories ----------

    /// <summary>
    /// Crée une texture de point radial (utilisée pour les étoiles et particules).
    /// </summary>
    /// <param name="graphicsDevice">Périphérique graphique.</param>
    /// <param name="size">Taille de la texture en pixels.</param>
    /// <param name="color">Couleur du point.</param>
    /// <returns>Texture 2D générée.</returns>
    public static Texture2D CreateDotTexture(GraphicsDevice graphicsDevice, int size = 4, Color color = default)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var data = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = MathHelper.Clamp(1f - dist / (size / 2f), 0f, 1f);
                data[y * size + x] = Color.Lerp(Color.Transparent, color, alpha);
            }
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// Rend du texte dans une texture procédurale.
    /// </summary>
    /// <param name="graphicsDevice">Périphérique graphique.</param>
    /// <param name="text">Texte à rendre.</param>
    /// <param name="font">Police de caractères.</param>
    /// <param name="color">Couleur du texte.</param>
    /// <returns>Texture 2D contenant le texte.</returns>
    public static Texture2D CreateTextTexture(GraphicsDevice graphicsDevice, string text, SpriteFont font, Color color)
    {
        Vector2 size = font.MeasureString(text);
        int w = Math.Max(1, (int)size.X);
        int h = Math.Max(1, (int)size.Y);
        var renderTarget = new RenderTarget2D(graphicsDevice, w, h);
        graphicsDevice.SetRenderTarget(renderTarget);
        graphicsDevice.Clear(Color.Transparent);
        using (var batch = new SpriteBatch(graphicsDevice))
        {
            batch.Begin();
            batch.DrawString(font, text, Vector2.Zero, color);
            batch.End();
        }
        graphicsDevice.SetRenderTarget(null);
        return renderTarget;
    }

    /// <summary>
    /// Crée une texture de flèche simple (utilisée pour le sélecteur du menu).
    /// </summary>
    /// <param name="graphicsDevice">Périphérique graphique.</param>
    /// <param name="size">Taille de la texture en pixels.</param>
    /// <param name="color">Couleur de la flèche.</param>
    /// <returns>Texture 2D générée.</returns>
    public static Texture2D CreateArrowTexture(GraphicsDevice graphicsDevice, int size = 12, Color color = default)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var data = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = x <= size - 1 - Math.Abs(x - (size - 1 - y) / 2f) * 2 && x > 0;
                data[y * size + x] = inside ? color : Color.Transparent;
            }
        texture.SetData(data);
        return texture;
    }
}
