using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Bouclier animé autour du joueur. Gère l'animation normale (Shield_Bleu) et l'animation de blast (Blast_Blanc) lors des impacts.
/// </summary>
public class Shield
{
    private Texture2D[] _normalFrames;
    private Texture2D[] _blastFrames;
    private float _frameDuration;
    private float _blastDuration;
    private float _blastTimer;
    private float _animTimer;
    private int _currentFrame;
    private bool _isBlasting;
    private float _scale;

    // Flash rouge à l'impact : 1s rouge pur, puis transition progressive vers le bleu
    private float _flashTimer = 0f;
    private const float FlashDuration = 1f;
    private bool _isFlashing = false;

    // Transition vers le bleu après le flash rouge
    private float _transitionTimer = 0f;
    private const float TransitionDuration = 0.5f;
    private bool _isTransitioning = false;

    /// <summary>
    /// Multiplicateur de vitesse de régénération (dépend de la difficulté).
    /// 1.5 = facile (rapide), 1.0 = normal, 0.7 = dur (lent), 0.5 = expert (très lent).
    /// </summary>
    public float RegenMultiplier { get; set; } = 1f;

    /// <summary>
    /// Intensité lumineuse du shield (0.0 = éteint, 1.0 = pleine puissance).
    /// </summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>
    /// Indique si le shield est actif (intensité > 0).
    /// </summary>
    public bool IsActive => Intensity > 0.01f;

    /// <summary>
    /// Indique si le shield est en train d'afficher l'animation de blast.
    /// </summary>
    public bool IsBlasting => _isBlasting;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="Shield"/>.
    /// </summary>
    /// <param name="normalFrames">Frames d'animation normale du bouclier.</param>
    /// <param name="blastFrames">Frames d'animation de blast.</param>
    /// <param name="frameDuration">Durée de chaque frame normale en secondes.</param>
    /// <param name="blastDuration">Durée totale de l'animation de blast.</param>
    /// <param name="scale">Échelle d'affichage.</param>
    public Shield(Texture2D[] normalFrames, Texture2D[] blastFrames,
                  float frameDuration = 0.1f, float blastDuration = 0.35f, float scale = 1f)
    {
        _normalFrames = normalFrames;
        _blastFrames = blastFrames;
        _frameDuration = frameDuration;
        _blastDuration = blastDuration;
        _currentFrame = 0;
        _animTimer = 0f;
        _scale = scale;
    }

    /// <summary>
    /// Met à jour l'animation du shield et le timer de blast.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Gestion du flash rouge après impact
        if (_isFlashing)
        {
            _flashTimer -= dt;
            if (_flashTimer <= 0f)
            {
                _isFlashing = false;
                _isTransitioning = true;
                _transitionTimer = TransitionDuration;
            }
            // Le flash ne gère pas l'animation normale ici — Draw gère la couleur
            if (_normalFrames != null && _normalFrames.Length > 0)
            {
                _animTimer += dt;
                if (_animTimer >= _frameDuration)
                {
                    _animTimer -= _frameDuration;
                    _currentFrame = (_currentFrame + 1) % _normalFrames.Length;
                }
            }
            return;
        }

        // Transition rouge → bleu après le flash
        if (_isTransitioning)
        {
            _transitionTimer -= dt;
            if (_transitionTimer <= 0f)
            {
                _isTransitioning = false;
            }
            if (_normalFrames != null && _normalFrames.Length > 0)
            {
                _animTimer += dt;
                if (_animTimer >= _frameDuration)
                {
                    _animTimer -= _frameDuration;
                    _currentFrame = (_currentFrame + 1) % _normalFrames.Length;
                }
            }
            return;
        }

        if (_isBlasting)
        {
            _blastTimer -= dt;
            if (_blastTimer <= 0f)
            {
                _isBlasting = false;
                _blastTimer = 0f;
            }
            return;
        }

        if (_normalFrames == null || _normalFrames.Length == 0)
            return;

        _animTimer += dt;
        if (_animTimer >= _frameDuration)
        {
            _animTimer -= _frameDuration;
            _currentFrame = (_currentFrame + 1) % _normalFrames.Length;
        }
    }

    /// <summary>
    /// Déclenche le flash rouge + blast lors d'un impact.
    /// </summary>
    public void TriggerBlast()
    {
        _isBlasting = true;
        _blastTimer = _blastDuration;
        _currentFrame = 0;
        // Démarrer le séquence flash rouge → transition → bleu
        _isFlashing = true;
        _flashTimer = FlashDuration;
        _isTransitioning = false;
        _transitionTimer = TransitionDuration;
    }

    /// <summary>
    /// Dessine le shield centré sur la position donnée, avec l'intensité actuelle.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    /// <param name="center">Position centrale du shield.</param>
    public void Draw(SpriteBatch spriteBatch, Vector2 center)
    {
        if (!IsActive)
            return;

        float intensity = Math.Clamp(Intensity, 0f, 1f);
        if (_isBlasting && _blastFrames != null && _blastFrames.Length > 0)
        {
            int blastIndex = (int)((1f - _blastTimer / _blastDuration) * _blastFrames.Length);
            blastIndex = Math.Clamp(blastIndex, 0, _blastFrames.Length - 1);
            Texture2D blastFrame = _blastFrames[blastIndex];
            float blastAlpha = 0.5f + (1f - intensity) * 0.5f;
            spriteBatch.Draw(blastFrame, center, null, new Color(1f, 1f, 1f, blastAlpha), 0f,
                new Vector2(blastFrame.Width / 2f, blastFrame.Height / 2f), _scale, SpriteEffects.None, 0f);
            return;
        }

        if (_normalFrames == null || _normalFrames.Length == 0)
            return;

        Texture2D frame = _normalFrames[_currentFrame];

        // Déterminer la couleur en fonction du statut
        Color color;
        if (_isFlashing)
        {
            // Flash rouge pur pendant 1 seconde
            color = new Color(1f, 0f, 0f, 0.8f * intensity);
        }
        else if (_isTransitioning)
        {
            // Transition rouge → bleu
            float t = 1f - (_transitionTimer / TransitionDuration);
            float red = 1f - t;
            float blue = t;
            color = new Color(red, 0.1f, blue, 0.8f * intensity);
        }
        else
        {
            // Couleurs normales : bleu avec alpha proportionnel à l'intensité
            float alpha = 0.2f * intensity;
            color = new Color(0.3f, 0.6f, 1f, alpha);
        }

        spriteBatch.Draw(frame, center, null, color, 0f,
            new Vector2(frame.Width / 2f, frame.Height / 2f), _scale, SpriteEffects.None, 0f);
    }
}
