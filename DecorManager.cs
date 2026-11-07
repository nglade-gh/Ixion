using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Gère le plateau de décor style Xenon 2 :
/// - murs latéraux défilants (couche 2, devant),
/// - plafond fixe,
/// - couches de parallax derrière les entités (lueurs, astéroïdes, étoiles).
/// </summary>
public class DecorManager
{
    private readonly List<DecorElement> _elements = new();
    private readonly List<Star> _stars = new();
    private readonly Random _random;
    private readonly Vector2 _screen;
    private readonly DecorsConfig _config;

    private Texture2D _dotTexture;
    private readonly Dictionary<string, Texture2D> _textureCache = new();

    /// <summary>
    /// Zone de jeu accessible, définie par les murs latéraux.
    /// </summary>
    public Rectangle PlayArea { get; private set; }

    /// <summary>
    /// Largeur cumulée des murs latéraux.
    /// </summary>
    public float WallWidth { get; private set; }

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="DecorManager"/>.
    /// </summary>
    /// <param name="config">Configuration des décors.</param>
    /// <param name="screen">Dimensions de l'écran.</param>
    /// <param name="random">Générateur de nombres aléatoires.</param>
    public DecorManager(DecorsConfig config, Vector2 screen, Random random)
    {
        _config = config;
        _screen = screen;
        _random = random;
    }

    /// <summary>
    /// Construit toutes les couches de décor. Doit être appelé avec un <see cref="ContentManager"/> chargé.
    /// </summary>
    /// <param name="content">Gestionnaire de contenu.</param>
    /// <param name="device">Périphérique graphique.</param>
    public void Initialize(ContentManager content, GraphicsDevice device)
    {
        _dotTexture = Sprite.CreateDotTexture(device, 4, Color.White);

        foreach (var p in _config.Elements)
        {
            switch (p.Side)
            {
                case "left":
                    BuildWall(p, LoadTexture(content, p.Texture), flip: false);
                    break;
                case "right":
                    BuildWall(p, LoadTexture(content, p.Texture), flip: true);
                    break;
                case "top":
                    BuildTopCap(p, LoadTexture(content, p.Texture));
                    break;
                default:
                    BuildField(p, content);
                    break;
            }
        }

        BuildStars();

        PlayArea = new Rectangle(
            (int)WallWidth, 0,
            (int)(_screen.X - 2 * WallWidth), (int)_screen.Y);
    }

    /// <summary>
    /// Charge une texture en l'identifiant par sa clé de contenu.
    /// </summary>
    /// <param name="content">Gestionnaire de contenu.</param>
    /// <param name="assetPath">Chemin de la ressource.</param>
    /// <returns>Texture chargée.</returns>
    private Texture2D LoadTexture(ContentManager content, string assetPath)
    {
        string key = GameConfig.ContentKey(assetPath);
        if (!_textureCache.TryGetValue(key, out var tex))
        {
            tex = content.Load<Texture2D>(key);
            _textureCache[key] = tex;
        }
        return tex;
    }

    /// <summary>
    /// Construit un mur latéral de tuiles défilantes.
    /// </summary>
    /// <param name="p">Paramètres du décor.</param>
    /// <param name="tex">Texture des tuiles.</param>
    /// <param name="flip">Si true, retourne horizontalement (mur droit).</param>
    private void BuildWall(DecorParams p, Texture2D tex, bool flip)
    {
        float scale = p.Scale > 0f ? p.Scale : 0.28f;
        float tileW = tex.Width * scale;
        float tileH = tex.Height * scale;
        int count = (int)Math.Ceiling(_screen.Y / tileH) + 2;
        float span = count * tileH;
        float x = flip ? _screen.X - tileW / 2f : tileW / 2f;

        for (int i = 0; i < count; i++)
        {
            var element = new DecorElement(p, tex, new Vector2(x, i * tileH - tileH / 2f),
                scale, Color.White, _screen.Y, span, false, _random);
            element.Effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            _elements.Add(element);
        }
        WallWidth = Math.Max(WallWidth, tileW);
    }

    /// <summary>
    /// Construit un plafond fixe en haut de l'écran.
    /// </summary>
    /// <param name="p">Paramètres du décor.</param>
    /// <param name="tex">Texture du plafond.</param>
    private void BuildTopCap(DecorParams p, Texture2D tex)
    {
        float scale = p.Scale > 0f ? p.Scale : 0.5f;
        var element = new DecorElement(p, tex,
            new Vector2(_screen.X / 2f, -tex.Height * scale * 0.45f),
            scale, p.Color, _screen.Y, 0f, false, _random);
        _elements.Add(element);
    }

    /// <summary>
    /// Répartit aléatoirement des instances de décor dans le champ.
    /// </summary>
    /// <param name="p">Paramètres du décor.</param>
    /// <param name="content">Gestionnaire de contenu.</param>
    private void BuildField(DecorParams p, ContentManager content)
    {
        var textures = new List<Texture2D>();
        if (p.Textures != null && p.Textures.Count > 0)
            foreach (var path in p.Textures)
                textures.Add(LoadTexture(content, path));
        else if (!string.IsNullOrEmpty(p.Texture))
            textures.Add(LoadTexture(content, p.Texture));
        if (textures.Count == 0)
            return;

        int count = Math.Max(1, p.Count);
        for (int i = 0; i < count; i++)
        {
            Texture2D tex = textures[_random.Next(textures.Count)];
            float scale = p.MinScale > 0f && p.MaxScale > p.MinScale
                ? (float)(_random.NextDouble() * (p.MaxScale - p.MinScale) + p.MinScale)
                : (p.Scale > 0f ? p.Scale : 0.5f);

            float x = (float)(_random.NextDouble() * _screen.X);
            float y = (float)(_random.NextDouble() * _screen.Y * 1.5f) - _screen.Y * 0.25f;
            float recycleSpan = _screen.Y + tex.Height * scale + 80f;

            var element = new DecorElement(p, tex, new Vector2(x, y),
                scale, p.Color, _screen.Y, recycleSpan, true, _random);
            _elements.Add(element);
        }
    }

    /// <summary>
    /// Crée les étoiles de fond selon les paramètres <see cref="StarsParams"/>.
    /// </summary>
    private void BuildStars()
    {
        var s = _config.Stars;
        if (s == null)
            return;
        for (int i = 0; i < s.Count; i++)
        {
            float size = (float)(_random.NextDouble() * (s.MaxSize - s.MinSize) + s.MinSize);
            float speed = (float)(_random.NextDouble() * (s.MaxSpeed - s.MinSpeed) + s.MinSpeed);
            Color color = Color.Lerp(Color.Gray, Color.White, (float)_random.NextDouble());
            Vector2 pos = new Vector2(
                (float)(_random.NextDouble() * _screen.X),
                (float)(_random.NextDouble() * _screen.Y));
            _stars.Add(new Star(_dotTexture, pos, speed, size, color, _random));
        }
    }

    /// <summary>
    /// Met à jour tous les éléments de décor et les étoiles.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public void Update(GameTime gameTime)
    {
        foreach (var element in _elements)
            element.Update(gameTime);
        foreach (var star in _stars)
            star.Update(gameTime);
    }

    /// <summary>
    /// Dessine les couches arrière (0) et milieu (1), derrière les entités.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public void DrawBack(SpriteBatch spriteBatch)
    {
        foreach (var element in _elements)
            if (element.Layer <= 0)
                element.Draw(spriteBatch);
        foreach (var star in _stars)
            star.Draw(spriteBatch);
        foreach (var element in _elements)
            if (element.Layer == 1)
                element.Draw(spriteBatch);
    }

    /// <summary>
    /// Dessine la couche avant (2) — les murs latéraux — par-dessus les entités.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public void DrawFront(SpriteBatch spriteBatch)
    {
        foreach (var element in _elements)
            if (element.Layer >= 2)
                element.Draw(spriteBatch);
    }
}
