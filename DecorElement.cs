using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Instance concrète d'un élément de décor (mur, rocher, lueur).
/// Gère le recyclage en haut de l'écran et les effets de rendu.
/// </summary>
public class DecorElement : Decors
{
    /// <summary>
    /// Paramètres de configuration de l'élément de décor.
    /// </summary>
    public DecorParams Params { get; }

    /// <summary>
    /// Effets de rendu à appliquer (ex : retournement horizontal).
    /// </summary>
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;

    private readonly float _screenHeight;
    private readonly float _recycleSpan;
    private readonly bool _randomizeX;
    private readonly Random _random;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="DecorElement"/>.
    /// </summary>
    /// <param name="p">Paramètres du décor.</param>
    /// <param name="texture">Texture de l'élément.</param>
    /// <param name="position">Position initiale.</param>
    /// <param name="scale">Échelle de rendu.</param>
    /// <param name="color">Tinte de rendu.</param>
    /// <param name="screenHeight">Hauteur de l'écran en pixels.</param>
    /// <param name="recycleSpan">Distance de recyclage vertical.</param>
    /// <param name="randomizeX">Si true, la position X est randomisée au recyclage.</param>
    /// <param name="random">Générateur de nombres aléatoires.</param>
    public DecorElement(DecorParams p, Texture2D texture, Vector2 position, float scale, Color color,
                        float screenHeight, float recycleSpan, bool randomizeX, Random random)
        : base(texture, position, new Vector2(texture.Width, texture.Height))
    {
        Params = p;
        Scale = scale;
        Color = color;
        ScrollSpeed = p.ScrollSpeed;
        Layer = p.Layer;
        IsParallax = p.IsParallax;
        _screenHeight = screenHeight;
        _recycleSpan = recycleSpan;
        _randomizeX = randomizeX;
        _random = random;
    }

    /// <summary>
    /// Met à jour la position et recycle l'élément en haut de l'écran si nécessaire.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!IsParallax || _recycleSpan <= 0f)
            return;

        float h = Size.Y * Scale;
        if (Position.Y - h / 2f > _screenHeight + 4f)
        {
            Position.Y -= _recycleSpan;
            if (_randomizeX)
                Position.X = (float)(_random.NextDouble() * ScreenManager._Instance._Dimensions.X);
        }
    }

    /// <summary>
    /// Dessine l'élément de décor avec ses effets de rendu.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;
        Rectangle src = _sourceRect ?? _texture.Bounds;
        Vector2 drawScale = new Vector2(
            Size.X * Scale / src.Width,
            Size.Y * Scale / src.Height);
        spriteBatch.Draw(_texture, Position, src, Color,
            MathHelper.ToRadians(Rotation), Origin, drawScale, Effects, 0f);
    }
}
