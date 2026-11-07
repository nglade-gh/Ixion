using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Représente une entrée utilisateur récente (pressée, relâchée, tenue).
/// </summary>
public struct InputSnapshot
{
    /// <summary>Touches pressées cette frame (nouvelles).</summary>
    public readonly bool WasPressed => _pressed;
    /// <summary>Touches relâchées cette frame (nouvelles).</summary>
    public readonly bool WasReleased => _released;
    /// <summary>Touches actuellement maintenues.</summary>
    public readonly bool IsHeld => _held;

    private readonly bool _pressed;
    private readonly bool _released;
    private readonly bool _held;

    private InputSnapshot(bool pressed, bool released, bool held)
    {
        _pressed = pressed;
        _released = released;
        _held = held;
    }

    public static InputSnapshot FromKeyboard(KeyboardState current, KeyboardState previous)
    {
        bool pressed = false, released = false, held = false;
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (current.IsKeyDown(key) && !previous.IsKeyDown(key)) pressed = true;
            if (!current.IsKeyDown(key) && previous.IsKeyDown(key)) released = true;
            if (current.IsKeyDown(key)) held = true;
        }
        return new InputSnapshot(pressed, released, held);
    }
}

/// <summary>
/// Service de gestion des entrées avec mappings action-based.
/// Permet de mapper des actions logiques (déplacement, tir, menu)
/// sur des touches/clavier/souris/manette configurables.
/// </summary>
public class InputService
{
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;

    // Mappings actions → touches
    private readonly Dictionary<string, List<Keys>> _keyBindings = new();
    private readonly Dictionary<string, Buttons> _gamepadBindings = new();

    private static InputService _instance;

    /// <summary>
    /// Accès singleton au service d'entrées.
    /// </summary>
    public static InputService Instance
    {
        get
        {
            if (_instance == null)
                _instance = new InputService();
            return _instance;
        }
    }

    /// <summary>
    /// Mises à jour les états d'entrée. À appeler une fois par frame.
    /// </summary>
    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _currentKeyboard = Keyboard.GetState();
    }

    /// <summary>
    /// Vérifie si une action a été pressée cette frame.
    /// </summary>
    /// <param name="action">Nom de l'action (ex: "MoveUp", "FirePrimary").</param>
    /// <returns>Vrai si l'action est activée.</returns>
    public bool IsActionPressed(string action)
    {
        if (_keyBindings.TryGetValue(action, out var keys))
        {
            foreach (Keys key in keys)
            {
                if (_currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Vérifie si une action est maintenue.
    /// </summary>
    /// <param name="action">Nom de l'action.</param>
    /// <returns>Vrai si l'action est maintenue.</returns>
    public bool IsActionHeld(string action)
    {
        if (_keyBindings.TryGetValue(action, out var keys))
        {
            foreach (Keys key in keys)
            {
                if (_currentKeyboard.IsKeyDown(key))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Vérifie si une action a été relâchée cette frame.
    /// </summary>
    /// <param name="action">Nom de l'action.</param>
    /// <returns>Vrai si l'action est relâchée.</returns>
    public bool IsActionReleased(string action)
    {
        if (_keyBindings.TryGetValue(action, out var keys))
        {
            foreach (Keys key in keys)
            {
                if (!_currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyDown(key))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Enregistre une liaison clavier pour une action donnée.
    /// </summary>
    /// <param name="action">Nom de l'action.</param>
    /// <param name="key">Touche associée.</param>
    public void BindKey(string action, Keys key)
    {
        if (!_keyBindings.ContainsKey(action))
            _keyBindings[action] = new List<Keys>();
        _keyBindings[action].Clear();
        _keyBindings[action].Add(key);
    }

    /// <summary>
    /// Enregistre une liaison manette pour une action donnée.
    /// </summary>
    /// <param name="action">Nom de l'action.</param>
    /// <param name="button">Bouton manette associé.</param>
    public void BindGamepad(string action, Buttons button)
    {
        _gamepadBindings[action] = button;
    }

    /// <summary>
    /// Vérifie si un bouton de manette est pressé.
    /// </summary>
    /// <param name="action">Nom de l'action.</param>
    /// <returns>Vrai si le bouton manette est pressé.</returns>
    public bool IsGamepadPressed(string action)
    {
        if (_gamepadBindings.TryGetValue(action, out var button))
        {
            var currentState = GamePad.GetState(PlayerIndex.One);
            var previousState = GamePad.GetState(PlayerIndex.One);
            return currentState.Buttons.ToString().Contains(button.ToString()) &&
                   !previousState.Buttons.ToString().Contains(button.ToString());
        }
        return false;
    }

    /// <summary>
    /// Charge les liaisons depuis la configuration AppConfig.
    /// </summary>
    public void LoadBindings()
    {
        var bindings = AppConfig.Input.Bindings;
        foreach (var kvp in bindings)
        {
            var binding = kvp.Value;
            if (binding.Type == "Key")
            {
                if (Enum.TryParse<Keys>(binding.Key, out var key))
                    BindKey(kvp.Key, key);
            }
            else if (binding.Type == "Gamepad")
            {
                if (Enum.TryParse<Buttons>(binding.Key, out var button))
                    BindGamepad(kvp.Key, button);
            }
        }
    }

    /// <summary>
    /// Entre en mode d'enregistrement pour remapper une action.
    /// </summary>
    /// <param name="action">Action à remapper.</param>
    /// <returns>Vrai si le mode enregistrement est activé.</returns>
    public bool StartRecordingBinding(string action)
    {
        // Simple mode: attendre la prochaine touche pressée
        return true;
    }

    /// <summary>
    /// Enregistre la prochaine touche pressée comme nouvelle liaison.
    /// </summary>
    /// <param name="action">Action à remapper.</param>
    /// <param name="newKey">Nouvelle touche.</param>
    public void RecordBinding(string action, Keys newKey)
    {
        BindKey(action, newKey);
        AppConfig.Input.Bindings[action] = new InputBinding { Type = "Key", Key = newKey.ToString() };
        AppConfig.Save();
    }
}
