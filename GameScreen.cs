using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Classe abstraite de base pour tous les écrans du jeu.
/// Définit le cycle de vie : chargement, mise à jour, rendu, déchargement.
/// Fournit un accès aux services du moteur (ressources, audio, événements, caméra).
/// Remplace le chargement local de ContentManager par l'utilisation du service centralisé.
/// </summary>
public abstract class GameScreen
{
    /// <summary>
    /// Accès aux services du moteur, partagés par tous les écrans.
    /// </summary>
    protected ScreenManager ScreenManager { get; private set; }

    /// <summary>
    /// Accès au service de ressources (cache centralisé de textures, polices, etc.).
    /// </summary>
    protected ResourceService Resources => ScreenManager?.Resources;

    /// <summary>
    /// Accès au service audio.
    /// </summary>
    protected AudioService Audio => ScreenManager?.Audio;

    /// <summary>
    /// Accès au service d'entrées.
    /// </summary>
    protected InputService Input => ScreenManager?.Input;

    /// <summary>
    /// Accès au service d'événements.
    /// </summary>
    protected EventService Events => ScreenManager?.Events;

    /// <summary>
    /// Accès au service de caméra.
    /// </summary>
    protected CameraService Camera => ScreenManager?.Camera;

    /// <summary>
    /// Accède au ContentManager pour le chargement de textures/polices.
    /// </summary>
    protected ContentManager _content => ScreenManager?._Content;

    /// <summary>
    /// Appelée quand cet écran est restauré via PopScreen().
    /// Permet de réinitialiser l'état interne (ex: dépauser le jeu).
    /// </summary>
    public virtual void OnResumeFromPause() { }

    /// <summary>
    /// Chemin vers le fichier XML de configuration de l'écran.
    /// </summary>
    public string _xmlPath;

    /// <summary>
    /// Crée un nouvel écran.
    /// </summary>
    protected GameScreen()
    {
        // Le ScreenManager sera assigné au premier LoadContent
    }

    /// <summary>
    /// Initialise les ressources de l'écran.
    /// Les services du moteur sont automatiquement liés au ScreenManager.
    /// </summary>
    public virtual void LoadContent()
    {
        ScreenManager = ScreenManager._Instance;
    }

    /// <summary>
    /// Décharge les ressources de l'écran.
    /// Les événements souscrits par l'écran doivent être désabonnés ici.
    /// </summary>
    public virtual void UnloadContent()
    {
        Events?.ClearAll();
    }

    /// <summary>
    /// Met à jour la logique de l'écran.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Effectue le rendu de l'écran.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public virtual void Draw(SpriteBatch spriteBatch) { }

    /// <summary>
    /// Méthode utilitaire d'affichage console (debug).
    /// </summary>
    public virtual void print() { }

    /// <summary>
    /// Demande une transition vers un nouvel écran.
    /// Utilise le système de transition animée (fade) du ScreenManager.
    /// </summary>
    /// <param name="newScreen">Nouvel écran à afficher.</param>
    /// <param name="duration">Durée de la transition en secondes.</param>
    protected void TransitionTo(GameScreen newScreen, float duration = 0.5f)
    {
        ScreenManager.TransitionTo(newScreen, duration);
    }

    /// <summary>
    /// Pousse un nouvel écran sur la pile (empilement sans remplacement).
    /// </summary>
    /// <param name="screen">Nouvel écran à empiler.</param>
    protected void PushScreen(GameScreen screen)
    {
        ScreenManager.PushScreen(screen);
    }

    /// <summary>
    /// Remplace l'écran courant par un nouveau.
    /// </summary>
    /// <param name="screen">Nouvel écran à afficher.</param>
    protected void ReplaceScreen(GameScreen screen)
    {
        ScreenManager.ReplaceScreen(screen);
    }

    /// <summary>
    /// Quitte l'application.
    /// </summary>
    protected void QuitGame()
    {
        ScreenManager.ExitGame();
    }

    /// <summary>
    /// Charge une texture depuis le cache de ressources.
    /// </summary>
    /// <param name="path">Chemin relatif dans Content.</param>
    protected Texture2D LoadTexture(string path)
    {
        return Resources?.Load<Texture2D>(path);
    }

    /// <summary>
    /// Charge un SpriteFont depuis le cache de ressources.
    /// </summary>
    /// <param name="path">Chemin relatif dans Content.</param>
    protected SpriteFont LoadFont(string path)
    {
        return Resources?.Load<SpriteFont>(path);
    }

    /// <summary>
    /// Charge un tableau de textures (pour les animations de frames).
    /// </summary>
    /// <param name="paths">Chemins relatifs des frames.</param>
    protected Texture2D[] LoadFrames(params string[] paths)
    {
        return Resources?.LoadFrames(paths);
    }

    /// <summary>
    /// Précharge des textures pour le prochain écran.
    /// </summary>
    /// <param name="paths">Chemin relatifs des textures à précharger.</param>
    protected void PreloadTextures(params string[] paths)
    {
        Resources?.Preload(paths);
    }
}
