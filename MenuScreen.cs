using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Écran de menu principal. Permet de lancer une partie ou de quitter.
/// </summary>
public class MenuScreen : GameScreen
{
    private List<MenuItem> _menuItems;
    private int _selectedIndex;
    private Texture2D _titleTexture;
    private Texture2D _selectorTexture;
    private float _selectorOffset;
    private SpriteFont _font;

    /// <summary>
    /// Initialise le menu avec les entrées par défaut.
    /// </summary>
    public MenuScreen()
    {
        _menuItems = new List<MenuItem>
        {
            new MenuItem { _text = "Jouer", _action = "play" },
            new MenuItem { _text = "Reglages", _action = "setup" },
            new MenuItem { _text = "Quitter", _action = "quit" }
        };
        _selectedIndex = 0;
    }

    /// <summary>
    /// Charge les polices et textures procédurales du menu.
    /// </summary>
    public override void LoadContent()
    {
        base.LoadContent();
        _font = _content.Load<SpriteFont>("Roman15");

        // Generate title texture
        _titleTexture = Sprite.CreateTextTexture(ScreenManager._Instance._graphicsDevice, "SHOOTER", _font, Color.Cyan);

        // Generate selector (a small triangle/arrow)
        _selectorTexture = Sprite.CreateArrowTexture(ScreenManager._Instance._graphicsDevice, 16, Color.Yellow);
    }

    /// <summary>
    /// Décharge les ressources du menu.
    /// </summary>
    public override void UnloadContent()
    {
        base.UnloadContent();
    }

    /// <summary>
    /// Gère la navigation dans le menu et l'animation du sélecteur.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        if (InputManager.KeyPressed(Keys.Up))
        {
            _selectedIndex--;
            if (_selectedIndex < 0)
                _selectedIndex = _menuItems.Count - 1;
        }
        if (InputManager.KeyPressed(Keys.Down))
        {
            _selectedIndex++;
            if (_selectedIndex >= _menuItems.Count)
                _selectedIndex = 0;
        }

        if (InputManager.KeyPressed(Keys.Enter))
        {
            var action = _menuItems[_selectedIndex]._action;
            if (action == "play")
            {
                ScreenManager._Instance.ChangeScreen(new GameplayScreen());
            }
            else if (action == "setup")
            {
                ScreenManager._Instance.ChangeScreen(new SetupScreen());
            }
            else if (action == "quit")
            {
                ScreenManager._Instance.ExitGame();
            }
        }

        // Animate selector
        _selectorOffset = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5.0) * 4.0f;

        base.Update(gameTime);
    }

    /// <summary>
    /// Affiche le titre, les items du menu et les instructions.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw title
        Vector2 titlePos = new Vector2(
            (ScreenManager._Instance._Dimensions.X - _titleTexture.Width) / 2,
            150
        );
        spriteBatch.Draw(_titleTexture, titlePos, Color.White);

        // Draw menu items
        float startY = 300;
        float spacing = 50;
        for (int i = 0; i < _menuItems.Count; i++)
        {
            Vector2 textSize = _font.MeasureString(_menuItems[i]._text);
            Vector2 pos = new Vector2(
                (ScreenManager._Instance._Dimensions.X - textSize.X) / 2,
                startY + i * spacing
            );

            Color color = (i == _selectedIndex) ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, _menuItems[i]._text, pos, color);

            if (i == _selectedIndex)
            {
                Vector2 selectorPos = new Vector2(
                    pos.X - _selectorTexture.Width - 10 + _selectorOffset,
                    pos.Y + (textSize.Y - _selectorTexture.Height) / 2
                );
                spriteBatch.Draw(_selectorTexture, selectorPos, Color.White);
            }
        }

        // Instructions bas (3 lignes empilees)
        string[] instLines = { "Fleches : Naviguer", "Entree : Valider" };
        Vector2 firstLineSize = _font.MeasureString(instLines[0]);
        Vector2 instBaseY = new Vector2(
            (ScreenManager._Instance._Dimensions.X - firstLineSize.X) / 2,
            ScreenManager._Instance._Dimensions.Y - 60
        );
        foreach (var line in instLines)
        {
            Vector2 pos = new Vector2(instBaseY.X, instBaseY.Y);
            spriteBatch.DrawString(_font, line, pos, Color.Gray);
            instBaseY.Y += 14f;
        }
    }
}

public class MenuItem
{
    /// <summary>
    /// Texte affiché dans le menu.
    /// </summary>
    public string _text;

    /// <summary>
    /// Action associée à l'item (ex : "play", "quit").
    /// </summary>
    public string _action;
}
