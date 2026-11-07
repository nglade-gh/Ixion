using System;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Singleton responsable de la lecture des entrées clavier.
/// Fournit des méthodes de test d'état (pressé, relâché, maintenu).
/// Supporte le mode simulation pour l'exécution headless.
/// </summary>
public class InputManager
{
    // État clavier — statique pour accessibilité via singleton
    private static KeyboardState _currentKeyState;
    private static KeyboardState _previousKeyState;
    private static InputManager _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// Flag activé quand le mode simulation est en cours.
    /// </summary>
    public static bool SimulationMode { get; set; }

    /// <summary>
    /// File de simulation : chaque entrée est un tableau de touches et la durée en frames.
    /// </summary>
    private static readonly System.Collections.Generic.Queue<SimEntry> _simQueue = new();

    /// <summary>
    /// Compteur frames restant pour la touche simulée en cours.
    /// </summary>
    private static int _simFramesRemaining = 0;

    /// <summary>
    /// Touches simulées actuellement enfoncées.
    /// </summary>
    private static Keys[] _simKeysDown = new Keys[0];

    /// <summary>
    /// Structure pour stocker une entrée de simulation.
    /// </summary>
    private struct SimEntry
    {
        public Keys[] Keys;
        public int DurationFrames;
    }

    /// <summary>
    /// Accès singleton au gestionnaire d'entrées.
    /// </summary>
    public static InputManager _Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new InputManager();
                return _instance;
            }
        }
    }

    /// <summary>
    /// Initialise une nouvelle instance.
    /// </summary>
    public InputManager()
    {
        _currentKeyState = Keyboard.GetState();
        _previousKeyState = _currentKeyState;
    }

    /// <summary>
    /// Enfile une touche simulée qui restera enfoncée pendant le nombre de frames spécifié.
    /// </summary>
    /// <param name="key">Touche à simuler.</param>
    /// <param name="durationFrames">Durée en frames (par défaut 1 frame = un "clic").</param>
    public static void SimulateKey(Keys key, int durationFrames = 1)
    {
        _simQueue.Enqueue(new SimEntry { Keys = new Keys[] { key }, DurationFrames = durationFrames });
    }

    /// <summary>
    /// Enfile une séquence de touches simulées.
    /// Chaque paire (key, durationFrames) est appliquée en ordre.
    /// </summary>
    public static void SimulateSequence(params Keys[] keys)
    {
        foreach (var key in keys)
        {
            _simQueue.Enqueue(new SimEntry { Keys = new Keys[] { key }, DurationFrames = 1 });
        }
    }

    /// <summary>
    /// Met à jour les états du clavier (courant et précédent).
    /// En mode simulation, les touches enfilées sont appliquées ici.
    /// </summary>
    public static void Update()
    {
        _previousKeyState = _currentKeyState;

        if (SimulationMode && _simFramesRemaining <= 0 && _simQueue.Count > 0)
        {
            // Dépiler la prochaine touche simulée
            var entry = _simQueue.Dequeue();
            _simFramesRemaining = entry.DurationFrames;
            _simKeysDown = entry.Keys;
        }

        if (SimulationMode && _simFramesRemaining > 0)
        {
            // Construire un KeyboardState avec les touches simulées enfoncées
            _currentKeyState = new KeyboardState(_simKeysDown);
            _simFramesRemaining--;
            if (_simFramesRemaining == 0)
            {
                _simKeysDown = new Keys[0];
            }
        }
        else
        {
            _currentKeyState = Keyboard.GetState();
        }
    }

    /// <summary>
    /// Retourne true si l'une des touches spécifiées vient d'être pressée.
    /// </summary>
    /// <param name="keys">Touches à tester.</param>
    public static bool KeyPressed(params Keys[] keys)
    {
        foreach (Keys key in keys)
        {
            bool down = _currentKeyState.IsKeyDown(key);
            bool wasUp = _previousKeyState.IsKeyUp(key);
            if (down && wasUp)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Retourne true si l'une des touches spécifiées vient d'être relâchée.
    /// </summary>
    /// <param name="keys">Touches à tester.</param>
    public static bool KeyReleased(params Keys[] keys)
    {
        foreach (Keys key in keys)
        {
            if (_currentKeyState.IsKeyUp(key) && _previousKeyState.IsKeyDown(key))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Retourne true si l'une des touches spécifiées est maintenue enfoncée.
    /// </summary>
    /// <param name="keys">Touches à tester.</param>
    public static bool KeyHold(params Keys[] keys)
    {
        foreach (Keys key in keys)
        {
            if (_currentKeyState.IsKeyDown(key) && _previousKeyState.IsKeyDown(key))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Retourne true si l'une des touches spécifiées est enfoncée (état brut).
    /// </summary>
    /// <param name="keys">Touches à tester.</param>
    public static bool KeyDown(params Keys[] keys)
    {
        foreach (Keys key in keys)
        {
            if (_currentKeyState.IsKeyDown(key))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Retourne l'état actuel du clavier.
    /// </summary>
    public static KeyboardState GetCurrentState() => _currentKeyState;

    /// <summary>
    /// Retourne l'état précédent du clavier.
    /// </summary>
    public static KeyboardState GetPreviousState() => _previousKeyState;
}
