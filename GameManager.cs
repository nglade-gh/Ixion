using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Classe racine du jeu, dérivée de <see cref="Game"/>.
/// Configure la fenêtre, charge les services du moteur et orchestre la boucle principale.
/// </summary>
public class GameManager : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Services du moteur de jeu
    private ResourceService _resources;
    private AudioService _audio;
    private InputService _input;
    private EventService _events;
    private CameraService _camera;
    private ScreenManager _screenManager;

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="GameManager"/>.
    /// Charge la configuration et initialise les services du moteur.
    /// </summary>
    public GameManager()
    {
        AppConfig.Load();

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = AppConfig.Video.Width,
            PreferredBackBufferHeight = AppConfig.Video.Height,
            IsFullScreen = AppConfig.Video.Fullscreen
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Initialiser le gestionnaire d'écrans
        _screenManager = new ScreenManager();
        _screenManager._Game = this;
    }

    /// <summary>
    /// Accès au gestionnaire d'écrans pour les transitions extérieures.
    /// </summary>
    public ScreenManager ScreenManager => _screenManager;

    /// <summary>
    /// Accès au service de ressources.
    /// </summary>
    public ResourceService Resources => _resources;

    /// <summary>
    /// Accès au service audio.
    /// </summary>
    public AudioService Audio => _audio;

    /// <summary>
    /// Applaue les parametres d'affichage charges depuis la configuration.
    /// </summary>
    public void ApplyDisplaySettings()
    {
        _graphics.PreferredBackBufferWidth = AppConfig.Video.Width;
        _graphics.PreferredBackBufferHeight = AppConfig.Video.Height;
        _graphics.IsFullScreen = AppConfig.Video.Fullscreen;
        _graphics.ApplyChanges();
    }

    /// <summary>
    /// Applique les changements du <see cref="GraphicsDeviceManager"/> et initialise le moteur.
    /// </summary>
    protected override void Initialize()
    {
        _graphics.ApplyChanges();

        // Initialiser les services du moteur
        _input = InputService.Instance;
        _events = EventService.Instance;
        _camera = new CameraService();

        base.Initialize();
    }

    /// <summary>
    /// Charge le contenu initial et relie les services aux écrans.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Créer et connecter les services
        _resources = new ResourceService(Content);
        _audio = new AudioService(Content);
        _audio.ApplySettings();

        // Initialiser les bindings d'entrée
        _input.LoadBindings();

        // Connecter les services au ScreenManager
        _screenManager.InitializeServices(_resources, _audio, _input, _events, _camera);

        // Charger le contenu du premier écran
        _screenManager.LoadContent(Content, GraphicsDevice);
    }

    /// <summary>
    /// Décharge le contenu des écrans et des services.
    /// </summary>
    protected override void UnloadContent()
    {
        _screenManager.UnloadContent();
        _resources.Clear();
        _audio.StopMusic();
    }

    /// <summary>
    /// Met à jour l'état du jeu : entrées, écrans, services.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    protected override void Update(GameTime gameTime)
    {
        // Mettre à jour les services du moteur
        _input.Update();
        _audio.Update();

        // Mettre à jour InputManager (utilisé par les écrans legacy comme MenuScreen)
        InputManager.Update();

        // Bouton retour manette (dans les menus)
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            // On délègue à l'écran actuel — en gameplay cela ouvre la pause
        }

        // Mettre à jour l'écran courant
        _screenManager.Update(gameTime);

        base.Update(gameTime);
    }

    /// <summary>
    /// Effectue le rendu de la frame courante via le <see cref="ScreenManager"/>.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // Utiliser la matrice de caméra pour le SpriteBatch
        Matrix cameraTransform = _camera.GetTransformMatrix();
        _spriteBatch.Begin(transformMatrix: cameraTransform);
        _screenManager.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // ============================================================
    // Méthode de test headless : exécution manuelle de N frames
    // et capture du framebuffer OpenGL.
    // ============================================================

    private const int GL_BGRA = 0x80E1;
    private const int GL_UNSIGNED_BYTE = 0x1401;

    [System.Runtime.InteropServices.DllImport("libGL.so.1", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private static extern void glReadPixels(int x, int y, int width, int height,
        int format, int type, System.IntPtr pixels);

    /// <summary>
    /// Exécute le jeu en mode headless : initialise, charge le contenu,
    /// puis exécute un nombre limité de frames et capture le framebuffer.
    /// </summary>
    /// <param name="maxFrames">Nombre maximum de frames à exécuter (par défaut 10).</param>
    /// <param name="beforeUpdate">Callback appelé avant chaque Update. Permet de simuler des entrées par frame.</param>
    public void RunHeadless(int maxFrames = 10, Action<int> beforeUpdate = null)
    {
        // Initialiser le jeu (crée le GraphicsDevice et le contexte OpenGL)
        Initialize();

        Console.WriteLine($"=== Headless Runner - {maxFrames} frames ===");
        Console.WriteLine($"Back buffer: {GraphicsDevice.PresentationParameters.BackBufferWidth}x" +
            $"{GraphicsDevice.PresentationParameters.BackBufferHeight}");

        // Charger le contenu (textures, polices, sons)
        LoadContent();

        int frame = 0;
        while (frame < maxFrames)
        {
            // Callback de simulation (injecte les touches simulées avant Update)
            beforeUpdate?.Invoke(frame);

            // Mettre à jour le jeu
            Update(new GameTime(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)));
            // Dessiner
            Draw(new GameTime(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)));

            // Capturer le framebuffer
            CaptureFrame(frame);
            frame++;

            Console.WriteLine($"Frame {frame}/{maxFrames} done");
        }

        Console.WriteLine("=== Capture terminée ===");
        Console.WriteLine($"Image PPM enregistrée : /tmp/shooter_frame0.ppm");
    }

    /// <summary>
    /// Capture le contenu du back buffer OpenGL via glReadPixels.
    /// Sauvegarde au format PPM P6 (binaire).
    /// </summary>
    private unsafe void CaptureFrame(int frameNum)
    {
        try
        {
            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;

            // Allouer un buffer pour les pixels
            var buffer = new byte[width * height * 4];

            // Utiliser fixed pour obtenir un pointer pin sur le tableau
            fixed (byte* ptr = buffer)
            {
                // glReadPixels lit depuis le bas de l'écran
                glReadPixels(0, 0, width, height, GL_BGRA, GL_UNSIGNED_BYTE, new IntPtr(ptr));
            }

            // Vérifier que les données ne sont pas toutes noires
            int nonBlack = 0;
            for (int i = 0; i < buffer.Length; i += 4)
            {
                if (buffer[i] > 10 || buffer[i + 1] > 10 || buffer[i + 2] > 10)
                    nonBlack++;
            }
            float percentage = 100.0f * nonBlack / (width * height);
            Console.WriteLine($"  Frame {frameNum}: {nonBlack} pixels non-noirs sur {width * height} ({percentage:F1}%)");

            // Écrire en PPM binaire (P6) - lignes inversées (OpenGL: bas en haut, PPM: haut en bas)
            string output;
            if (frameNum == 0)
                output = "/tmp/shooter_frame0.ppm";
            else
                output = $"/tmp/shooter_frame{frameNum}.ppm";

            using var fs = System.IO.File.Create(output);
            using var bw = new System.IO.BinaryWriter(fs);

            // Header PPM P6
            bw.Write(System.Text.Encoding.ASCII.GetBytes("P6\n"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes($"{width} {height}\n"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("255\n"));

            // Lignes : glReadPixels avec y=0 lit depuis le bas
            // On veut écrire de haut en bas pour le PPM standard
            for (int y = 0; y < height; y++)
            {
                int srcY = height - 1 - y;
                int srcOffset = srcY * width * 4;
                bw.Write(buffer, srcOffset, width * 4);
            }

            Console.WriteLine($"  Sauvegardé : {output} ({(width * height * 3 + 20)} octets)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Frame {frameNum}: ERREUR capture: {ex.Message}");
        }
    }
}
