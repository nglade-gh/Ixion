using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Ecran de remappage des touches. Permet de selectionner une action et d'appuyer une nouvelle touche.
/// </summary>
public class KeyBindingScreen : GameScreen
{
    private readonly string[] _actions =
    {
        "Deplacer Haut",
        "Deplacer Bas",
        "Deplacer Gauche",
        "Deplacer Droite",
        "Tir Primaire",
        "Tir Secondaire",
        "Tir Special",
        "Menu Haut",
        "Menu Bas",
        "Menu Selectionner",
        "Menu Retour",
        "PauseGame",
        "Retour"
    };

    private readonly string[] _actionKeys =
    {
        "MoveUp", "MoveDown", "MoveLeft", "MoveRight",
        "FirePrimary", "FireSecondary", "FireSpecial",
        "MenuUp", "MenuDown", "MenuSelect", "MenuBack",
        "PauseGame",
        "Return", // "Retour" — pas de binding
        "" // placeholder for _actions.Length alignment
    };

    private SpriteFont _font;
    private Texture2D _selectorTexture;
    private Texture2D _borderTexture;

    private readonly bool _fromPause;
    private int _selectedIndex = 0;
    private bool _recording;
    private int _recordingFor; // index dans _actionKeys

    /// <summary>
    /// Initialise le menu de remappage depuis le menu principal.
    /// </summary>
    public KeyBindingScreen() : this(fromPause: false) { }

    /// <summary>
    /// Initialise le menu de remappage, avec option pour revenir via PopScreen.
    /// </summary>
    public KeyBindingScreen(bool fromPause)
    {
        _fromPause = fromPause;
    }

    /// <summary>
    /// Délai de protection en frames pour éviter de capturer la touche d'activation (Enter).
    /// </summary>
    private int _inputCooldown;

    public override void LoadContent()
    {
        base.LoadContent();
        _font = _content.Load<SpriteFont>("Roman15");
        _selectorTexture = Sprite.CreateArrowTexture(ScreenManager._Instance._graphicsDevice, 20, Color.Yellow);
        _borderTexture = Sprite.CreateDotTexture(ScreenManager._Instance._graphicsDevice, 1, Color.Gray);
    }

    public override void Update(GameTime gameTime)
    {
        // Decrement cooldown each frame
        if (_inputCooldown > 0) _inputCooldown--;

        // Escape: retour sans remapper
        if (InputManager.KeyPressed(Keys.Escape))
        {
            ExitScreen();
            return;
        }

        // Enregistrement en cours — attendre relâchement puis nouvelle pression
        if (_recording)
        {
            var ks = Keyboard.GetState();
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key == Keys.None) continue;
                // Détecter seulement si la touche vient d'être appuyée (pas déjà enfoncée)
                if (ks.IsKeyDown(key) && !InputManager.GetPreviousState().IsKeyDown(key))
                {
                    // Vérifier que la touche n'est pas déjà liée à une autre action
                    var conflict = false;
                    for (int i = 0; i < _actionKeys.Length; i++)
                    {
                        if (i == _recordingFor) continue;
                        var existing = AppConfig.Input.Bindings.GetValueOrDefault(_actionKeys[i]);
                        if (existing != null && existing.Type == "Key" && existing.Key == key.ToString())
                        {
                            conflict = true;
                            break;
                        }
                    }

                    if (!conflict)
                    {
                        AppConfig.Input.Bindings[_actionKeys[_recordingFor]] = new InputBinding { Type = "Key", Key = key.ToString() };
                        AppConfig.Save();

                        // Recharger les liaisons
                        var inputSvc = ScreenManager._Instance.Input;
                        if (inputSvc != null) inputSvc.LoadBindings();
                    }

                    _recording = false;
                    break;
                }
            }
            base.Update(gameTime);
            return;
        }

        // Décrémenter le cooldown avant de traiter les inputs normaux
        if (_inputCooldown > 0)
        {
            base.Update(gameTime);
            return;
        }

        // Navigation
        if (InputManager.KeyPressed(Keys.Up))
        {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = _actions.Length - 1;
        }
        if (InputManager.KeyPressed(Keys.Down))
        {
            _selectedIndex++;
            if (_selectedIndex >= _actions.Length) _selectedIndex = 0;
        }

        // Entrer: démarrer l'enregistrement (sauf pour "Retour")
        if (InputManager.KeyPressed(Keys.Enter))
        {
            if (_selectedIndex == _actions.Length - 1)
            {
                // Dernier item = Retour
                ExitScreen();
                return;
            }
            _recordingFor = _selectedIndex;
            _recording = true;
            _inputCooldown = 5; // Bloquer 5 frames pour laisser le temps de relâcher Enter
        }

        // Supprimer: reset to default
        if (InputManager.KeyPressed(Keys.Delete))
        {
            string defaultKey = GetDefaultKey(_actionKeys[_selectedIndex]);
            AppConfig.Input.Bindings[_actionKeys[_selectedIndex]] = new InputBinding { Type = "Key", Key = defaultKey };
            AppConfig.Save();
            var inputSvc = ScreenManager._Instance.Input;
            if (inputSvc != null) inputSvc.LoadBindings();
        }

        base.Update(gameTime);
    }

    private static string GetDefaultKey(string action)
    {
        return action switch
        {
            "MoveUp" => "Up",
            "MoveDown" => "Down",
            "MoveLeft" => "Left",
            "MoveRight" => "Right",
            "FirePrimary" => "Space",
            "FireSecondary" => "X",
            "FireSpecial" => "C",
            "MenuUp" => "Up",
            "MenuDown" => "Down",
            "MenuSelect" => "Enter",
            "MenuBack" => "Escape",
            "PauseGame" => "Escape",
            _ => "None"
        };
    }

    private string FormatKey(string keyString)
    {
        if (string.IsNullOrEmpty(keyString) || keyString == "None") return "???";
        if (keyString == "Space") return "ESPACE";
        if (keyString == "OemPeriod") return ".";
        return keyString;
    }

    /// <summary>
    /// Quitte l'écran de remappage. Si ouvert depuis pause, pop deux fois
    /// (KeyBindingScreen → SetupScreen → GameplayScreen).
    /// </summary>
    private void ExitScreen()
    {
        var sm = ScreenManager._Instance;
        try
        {
            sm.PopScreen();
            if (_fromPause)
            {
                sm.PopScreen();
            }
        }
        catch
        {
            // Fallback si la pile est vide
            sm.ChangeScreen(_fromPause ? new GameplayScreen() : new SetupScreen());
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var panelX = (ScreenManager._Instance._Dimensions.X - 480) / 2f;
        var panelY = (ScreenManager._Instance._Dimensions.Y - 500) / 2f;

        // Fond sombre
        spriteBatch.Draw(_borderTexture,
            new Rectangle((int)panelX, (int)panelY, 480, 500),
            new Color(30, 30, 40, 180));

        // Titre
        string title = _recording ? $"REMAP: {_actions[_recordingFor]}" : "REMAPPAGE TOUCHES";
        Color titleColor = _recording ? Color.Red : Color.White;
        spriteBatch.DrawString(_font, title,
            new Vector2(panelX + (480 - _font.MeasureString(title).X) / 2f, 20),
            titleColor);

        // Instructions si enregistrement
        if (_recording)
        {
            string instr = "Appuyez sur la nouvelle touche...";
            spriteBatch.DrawString(_font, instr,
                new Vector2(panelX + 20f, 45),
                Color.Orange);
        }

        // Liste des actions
        float startY = 90f;
        float spacing = 34f;

        for (int i = 0; i < _actions.Length; i++)
        {
            var binding = AppConfig.Input.Bindings.GetValueOrDefault(_actionKeys[i]);
            string keyStr = binding != null ? FormatKey(binding.Key) : "???";

            Vector2 pos = new Vector2(panelX + 30f, startY + i * spacing);
            string label = $"{_actions[i]}: {keyStr}";

            Color color = (i == _selectedIndex && !_recording) ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, label, pos, color);

            if (i == _selectedIndex && !_recording)
            {
                Vector2 selPos = new Vector2(pos.X - 24, pos.Y + 4);
                spriteBatch.Draw(_selectorTexture, selPos, Color.White);
            }
        }

        // Instructions bas (3 lignes empilees verticalement)
        string[] instLines = { "Haut/Bas : Naviguer", "Entree : Remapper", "Echap : Retour" };
        float instStartY = panelY + 460f; // 500 - 40 = bas du panneau
        for (int j = 0; j < instLines.Length; j++)
        {
            float instY = instStartY + j * 18f;
            if (instY + 18f > panelY + 500) continue; // ne pas dessiner hors du panneau
            Vector2 instPos = new Vector2(
                panelX + (480 - _font.MeasureString(instLines[j]).X) / 2f,
                instY);
            spriteBatch.DrawString(_font, instLines[j], instPos, Color.Gray);
        }
    }
}
