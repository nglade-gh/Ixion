using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Gestionnaire centralisé des écrans de jeu.
/// Fournit les membres rétrocompatibles (_Content, _Dimensions, _graphicsDevice)
/// ET les nouveaux services du moteur (Resources, Audio, Input, Events, Camera).
/// </summary>
public class ScreenManager
{
    private static ScreenManager _instance;
    /// <summary>Accès singleton rétrocompatible.</summary>
    public static ScreenManager _Instance
    {
        get
        {
            if (_instance == null)
                _instance = new ScreenManager();
            return _instance;
        }
    }

    // Membres rétrocompatibles (utilisés par le code existant)
    [System.Xml.Serialization.XmlIgnore] public ContentManager _Content;
    [System.Xml.Serialization.XmlIgnore] public GraphicsDevice _graphicsDevice;
    [System.Xml.Serialization.XmlIgnore] public SpriteBatch _spriteBatch;
    [System.Xml.Serialization.XmlIgnore] public Game _Game;

    /// <summary>Dimensions de la zone de jeu en pixels.</summary>
    [System.Xml.Serialization.XmlElement("dimensions")] public Vector2 _Dimensions { init; get; } = new(600, 800);

    // Services du moteur (nouveaux)
    public ResourceService Resources { get; private set; }
    public AudioService Audio { get; private set; }
    public InputService Input { get; private set; }
    public EventService Events { get; private set; }
    public CameraService Camera { get; private set; }

    private GameScreen _currentGameScreen;
    private Stack<GameScreen> _screenStack = new();

    /// <summary>Accès au contenu pour les écrans existants.</summary>
    public ContentManager _content => _Content;

    // Transition d'écran
    private bool _IsTransitioning { get; set; }
    private float _transitionTimer;
    private float _transitionDuration;
    private GameScreen _oldScreen;

    /// <summary>Indique si une transition d'écran est en cours.</summary>
    public bool _IsTransitioningPublic => _IsTransitioning;

    /// <summary>
    /// Crée un nouveau gestionnaire d'écrans.
    /// </summary>
    public ScreenManager()
    {
        _IsTransitioning = false;
        _transitionTimer = 0f;
        _transitionDuration = 0.5f;
        _currentGameScreen = new MenuScreen();
    }

    /// <summary>
    /// Initialise le gestionnaire avec les services partagés.
    /// À appeler depuis GameManager.LoadContent().
    /// </summary>
    public void InitializeServices(ResourceService resources, AudioService audio, InputService input, EventService events, CameraService camera)
    {
        Resources = resources;
        Audio = audio;
        Input = input;
        Events = events;
        Camera = camera;
    }

    /// <summary>
    /// Charge le contenu de l'écran courant.
    /// </summary>
    /// <param name="Content">Gestionnaire de contenu racine.</param>
    public void LoadContent(ContentManager Content, GraphicsDevice graphicsDevice)
    {
        // S'assurer que le singleton pointe vers cette instance
        _instance = this;
        this._Content = new ContentManager(Content.ServiceProvider, "Content");
        _graphicsDevice = graphicsDevice;
        _currentGameScreen.LoadContent();
    }

    /// <summary>
    /// Décharge le contenu de l'écran courant.
    /// </summary>
    public void UnloadContent()
    {
        _currentGameScreen?.UnloadContent();
    }

    /// <summary>
    /// Délègue la mise à jour à l'écran courant.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public void Update(GameTime gameTime)
    {
        if (_IsTransitioning)
        {
            _transitionTimer += (float)gameTime.TotalGameTime.TotalSeconds;
            if (_transitionTimer >= _transitionDuration)
            {
                _oldScreen?.UnloadContent();
                _oldScreen = null;
                _IsTransitioning = false;
                _transitionTimer = 0f;
            }
            else
            {
                _oldScreen?.Update(gameTime);
                return;
            }
        }

        _currentGameScreen?.Update(gameTime);
    }

    /// <summary>
    /// Délègue le rendu à l'écran courant.
    /// </summary>
    /// <param name="spritebatch">Batch de rendu MonoGame.</param>
    public void Draw(SpriteBatch spritebatch)
    {
        _currentGameScreen?.Draw(spritebatch);

        if (_IsTransitioning && _oldScreen != null)
        {
            _oldScreen.Draw(spritebatch);

            float alpha = 1f - (_transitionTimer / _transitionDuration);
            DrawFadeOverlay(spritebatch, alpha);
        }
    }

    /// <summary>
    /// Change l'écran courant en déchargeant l'ancien et en chargeant le nouveau.
    /// </summary>
    /// <param name="newScreen">Nouvel écran à afficher.</param>
    public void ChangeScreen(GameScreen newScreen)
    {
        _currentGameScreen?.UnloadContent();
        _currentGameScreen = newScreen;
        _currentGameScreen.LoadContent();
    }

    /// <summary>
    /// Démarre une transition d'écran animée (fade).
    /// </summary>
    /// <param name="newScreen">Nouvel écran à afficher.</param>
    /// <param name="duration">Durée de la transition en secondes.</param>
    public void TransitionTo(GameScreen newScreen, float duration = 0.5f)
    {
        _oldScreen = _currentGameScreen;
        _currentGameScreen = newScreen;
        _currentGameScreen.LoadContent();
        _IsTransitioning = true;
        _transitionTimer = 0f;
        _transitionDuration = duration;
    }

    /// <summary>
    /// Pousse un nouvel écran sur la pile (empilement).
    /// L'écran précédent est sauvegardé pour un retour futur.
    /// </summary>
    public void PushScreen(GameScreen screen)
    {
        _screenStack.Push(_currentGameScreen);
        _currentGameScreen?.UnloadContent();
        _currentGameScreen = screen;
        _currentGameScreen.LoadContent();
    }

    /// <summary>
    /// Remplace l'écran courant par un nouveau (sans empilement).
    /// </summary>
    public void ReplaceScreen(GameScreen screen)
    {
        _currentGameScreen?.UnloadContent();
        _currentGameScreen = screen;
        _currentGameScreen.LoadContent();
    }

    /// <summary>
    /// Retourne à l'écran précédent (pop de la pile).
    /// </summary>
    public void PopScreen()
    {
        _currentGameScreen?.UnloadContent();
        if (_screenStack.Count > 0)
        {
            _currentGameScreen = _screenStack.Pop();
            if (_currentGameScreen != null)
            {
                _currentGameScreen.LoadContent();
                _currentGameScreen.OnResumeFromPause();
            }
        }
        else
        {
            // No screen to pop back to — fall back to menu
            _currentGameScreen = new MenuScreen();
            _currentGameScreen.LoadContent();
        }
    }

    /// <summary>
    /// Demande la fermeture de l'application.
    /// </summary>
    public void ExitGame()
    {
        _Game?.Exit();
    }

    /// <summary>
    /// Dessine un overlay noir avec une opacité donnée.
    /// </summary>
    private void DrawFadeOverlay(SpriteBatch spriteBatch, float alpha)
    {
        Color overlayColor = new(Color.Black.ToVector4() * alpha);
        Texture2D white = Resources?.Load<Texture2D>("pics/bullets/white-circle");
        if (white != null)
            spriteBatch.Draw(white, new Rectangle(0, 0, (int)_Dimensions.X, (int)_Dimensions.Y), overlayColor);
    }
}
