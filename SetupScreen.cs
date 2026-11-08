using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Ecran de configuration generale. Point d'entree avec sous-menus video, audio et difficulte.
/// </summary>
public class SetupScreen : GameScreen
{
    private enum SubMenu { None, Video, Audio, Difficulty, KeyBindings }

    private SubMenu _activeMenu;
    private int _selectedIndex;
    private SpriteFont _font;
    private Texture2D _selectorTexture;
    private Texture2D _borderTexture;

    /// <summary>
    /// Indique si ce menu a été ouvert depuis le gameplay (pour utiliser PopScreen).
    /// </summary>
    private readonly bool _fromPause;

    /// <summary>
    /// Largeur du panneau de configuration.
    /// </summary>
    private const int PanelWidth = 480;

    /// <summary>
    /// Hauteur du panneau de configuration.
    /// </summary>
    private const int PanelHeight = 800;
    private bool _needsVideoApply;
    private bool _lastRight;
    private bool _lastLeft;

    /// <summary>
    /// Initialise le menu principal du setup depuis le menu principal.
    /// </summary>
    public SetupScreen() : this(fromPause: false) { }

    /// <summary>
    /// Initialise le menu principal du setup, avec option pour revenir via PopScreen.
    /// </summary>
    /// <param name="fromPause">Si true, PopScreen retourne au jeu au lieu du menu principal.</param>
    public SetupScreen(bool fromPause)
    {
        _activeMenu = SubMenu.None;
        _selectedIndex = 0;
        _fromPause = fromPause;
        _needsVideoApply = false;
        _lastRight = false;
        _lastLeft = false;
    }

    /// <summary>
    /// Charge les polices et textures du setup.
    /// </summary>
    public override void LoadContent()
    {
        base.LoadContent();
        _font = _content.Load<SpriteFont>("Roman15");
        _selectorTexture = Sprite.CreateArrowTexture(ScreenManager._Instance._graphicsDevice, 20, Color.Yellow);
        _borderTexture = Sprite.CreateDotTexture(ScreenManager._Instance._graphicsDevice, 1, Color.Gray);
    }

    /// <summary>
    /// Met a jour la navigation et les selections.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        if (InputManager.KeyPressed(Keys.Escape))
        {
            ApplyVideoIfChanged();
            // Si ouvert depuis le gameplay (pause), pop pour revenir
            if (_fromPause)
            {
                ScreenManager._Instance.PopScreen();
            }
            else
            {
                ScreenManager._Instance.ChangeScreen(new MenuScreen());
            }
            return;
        }

        if (_activeMenu != SubMenu.None)
        {
            UpdateSubmenu();
            return;
        }

        UpdateMainMenu();
        base.Update(gameTime);
    }

    private void UpdateMainMenu()
    {
        if (InputManager.KeyPressed(Keys.Up))
            _selectedIndex = (_selectedIndex - 1 + _mainMenuItems.Count) % _mainMenuItems.Count;
        if (InputManager.KeyPressed(Keys.Down))
            _selectedIndex = (_selectedIndex + 1) % _mainMenuItems.Count;

        if (InputManager.KeyPressed(Keys.Enter))
        {
            if (_selectedIndex == 0)
            {
                _activeMenu = SubMenu.Audio;
                _selectedIndex = 0;
            }
            else if (_selectedIndex == 1)
            {
                _activeMenu = SubMenu.Difficulty;
                _selectedIndex = 0;
            }
            else if (_selectedIndex == 2)
            {
                // Ouvrir le menu de remapage en push (pour pouvoir pop)
                ScreenManager._Instance.PushScreen(new KeyBindingScreen(fromPause: _fromPause));
            }
            else if (_selectedIndex == 3)
            {
                // Retour — si ouvert depuis pause, pop; sinon menu principal
                if (_fromPause)
                {
                    ScreenManager._Instance.PopScreen();
                }
                else
                {
                    ScreenManager._Instance.ChangeScreen(new MenuScreen());
                }
            }
        }
    }

    private readonly List<string> _mainMenuItems = new()
    {
        "Reglages audio",
        "Difficulte",
        "Touches",
        "Retour"
    };

    private void UpdateSubmenu()
    {
        bool right = InputManager.KeyDown(Keys.Right);
        bool left = InputManager.KeyDown(Keys.Left);
        bool rightPressed = right && !_lastRight;
        bool leftPressed = left && !_lastLeft;
        _lastRight = right;
        _lastLeft = left;

        if (InputManager.KeyPressed(Keys.Up))
            _selectedIndex--;
        else if (InputManager.KeyPressed(Keys.Down))
            _selectedIndex++;

        if (_activeMenu == SubMenu.None) return;

        if (_activeMenu == SubMenu.Video)
        {
            if (_selectedIndex < 0) _selectedIndex = 3;
            if (_selectedIndex > 3) _selectedIndex = 0;
            if (_selectedIndex < 3)
                ApplyVideoSetting(_selectedIndex);
            else if (InputManager.KeyPressed(Keys.Enter))
                _activeMenu = SubMenu.None;
        }
        else if (_activeMenu == SubMenu.Audio)
        {
            if (_selectedIndex < 0) _selectedIndex = 3;
            if (_selectedIndex > 3) _selectedIndex = 0;
            // Ne modifier le son que sur appui directionnel + bip qu'une seule fois par modification
            if (_selectedIndex < 3 && (rightPressed || leftPressed))
            {
                AdjustAudioSetting(_selectedIndex, rightPressed, leftPressed);
            }
            else if (InputManager.KeyPressed(Keys.Enter))
            {
                _activeMenu = SubMenu.None;
            }
        }
        else if (_activeMenu == SubMenu.Difficulty)
        {
            // 21 items : indices 0-20 (Preset, Spawn, PV Ennemis, Vitesse Ennemis, Degats Joueur,
            // Structure, Chauffe Laser, Refroid. Repos, Deg. Bouclier Ennemi, Deg. Contact Ennemi,
            // Duree Upgrade Primaire, Multi Deg. Primaire, Duree Upgrade Secondaire,
            // Multi Deg. Secondaire, Reparation Bouclier, Reparation Structure,
            // Duree RapidCool, Duree DiagonalShot, Duree PowerShot, Duree TripleShot, Retour)
            const int difficultyItemCount = 21;
            // Pas de wrap pour éviter le défilement infini.
            if (_selectedIndex < 0) _selectedIndex = 0;
            if (_selectedIndex > difficultyItemCount - 1) _selectedIndex = difficultyItemCount - 1;
            if (_selectedIndex < difficultyItemCount - 1)
                AdjustDifficultySetting(_selectedIndex, rightPressed, leftPressed);
            else if (InputManager.KeyPressed(Keys.Enter))
            {
                _activeMenu = SubMenu.None;
            }
        }
    }

    private void ApplyVideoSetting(int index)
    {
        var resolutions = VideoConfig.SupportedResolutions;
        if (index == 0)
        {
            int cur = 0;
            for (int i = 0; i < resolutions.Count; i++)
                if (resolutions[i].W == AppConfig.Video.Width && resolutions[i].H == AppConfig.Video.Height)
                    cur = i;
            cur = (cur + 1) % resolutions.Count;
            AppConfig.Video.Width = resolutions[cur].W;
            AppConfig.Video.Height = resolutions[cur].H;
            _needsVideoApply = true;
        }
        else if (index == 1)
        {
            AppConfig.Video.Fullscreen = !AppConfig.Video.Fullscreen;
            _needsVideoApply = true;
        }
        else if (index == 2)
        {
            AppConfig.Reset();
            _needsVideoApply = true;
        }
        if (_needsVideoApply)
        {
            ApplyVideoIfChanged();
            AppConfig.Save();
        }
    }

    private void ApplyVideoIfChanged()
    {
        if (!_needsVideoApply) return;
        _needsVideoApply = false;
        var gm = ScreenManager._Instance._Game;
        if (gm is GameManager game)
        {
            game.ApplyDisplaySettings();
        }
    }

    private void AdjustAudioSetting(int index, bool rightPressed, bool leftPressed)
    {
        const float step = 0.05f;
        int inc = rightPressed ? 1 : (leftPressed ? -1 : 0);
        var audio = ScreenManager._Instance?.Audio;

        if (index == 0)
        {
            float v = Math.Clamp(AppConfig.Audio.MasterVolume + inc * step, 0f, 1f);
            AppConfig.Audio.MasterVolume = v;
            audio?.SetMasterVolume(v);
        }
        else if (index == 1)
        {
            float v = Math.Clamp(AppConfig.Audio.MusicVolume + inc * step, 0f, 1f);
            AppConfig.Audio.MusicVolume = v;
            audio?.SetMusicVolume(v);
        }
        else if (index == 2)
        {
            float v = Math.Clamp(AppConfig.Audio.SfxVolume + inc * step, 0f, 1f);
            AppConfig.Audio.SfxVolume = v;
            audio?.SetSfxVolume(v);
        }
        AppConfig.Save();

        // Feedback : bip court au volume master
        audio?.PlayPreviewTone();
    }

    private void AdjustDifficultySetting(int index, bool rightPressed, bool leftPressed)
    {
        // Ne faire quelque chose que si une direction a été appuyée (edge, pas state)
        if (!rightPressed && !leftPressed) return;

        var d = AppConfig.Difficulty;
        int inc = rightPressed ? 5 : (leftPressed ? -5 : 0);

        if (index == 0) // Preset
        {
            DifficultyPreset[] presets = { DifficultyPreset.Easy, DifficultyPreset.Normal, DifficultyPreset.Hard, DifficultyPreset.Expert };
            int curPreset = 0;
            for (int i = 0; i < presets.Length; i++)
                if (AppConfig.Difficulty.Preset == presets[i]) { curPreset = i; break; }
            curPreset = (curPreset + 1) % presets.Length;
            AppConfig.ApplyPreset(presets[curPreset]);
            d = AppConfig.Difficulty;
        }
        else if (index == 1)
        {
            d.SpawnRateModifier = Math.Clamp(d.SpawnRateModifier + inc, -100, 100);
        }
        else if (index == 2)
        {
            d.EnemyHpModifier = Math.Clamp(d.EnemyHpModifier + inc, -100, 100);
        }
        else if (index == 3)
        {
            d.EnemySpeedModifier = Math.Clamp(d.EnemySpeedModifier + inc, -100, 100);
        }
        else if (index == 4)
        {
            float dInc = rightPressed ? 0.05f : (leftPressed ? -0.05f : 0f);
            d.PlayerDamageMultiplier = Math.Clamp(d.PlayerDamageMultiplier + dInc, 0.1f, 3f);
        }
        else if (index == 6)
        {
            d.StructureHp = Math.Clamp(d.StructureHp + inc, 10, 200);
            d.ShieldStartEnergy = d.StructureHp;
        }
        else if (index == 7)
        {
            d.LaserHeatRateModifier = Math.Clamp(d.LaserHeatRateModifier + inc, -100, 100);
        }
        else if (index == 8)
        {
            d.LaserIdleCoolMultiplier = Math.Clamp(d.LaserIdleCoolMultiplier + inc * 0.1f, 1.0f, 5.0f);
        }
        else if (index == 9)
        {
            d.EnemyBulletDamage = Math.Clamp(d.EnemyBulletDamage + inc, 1, 100);
        }
        else if (index == 10)
        {
            d.EnemyContactDamage = Math.Clamp(d.EnemyContactDamage + inc, 1, 100);
        }
        else if (index == 11)
        {
            d.PrimaryUpgradeDuration = Math.Clamp(d.PrimaryUpgradeDuration + inc * 0.5f, 1f, 60f);
        }
        else if (index == 12)
        {
            d.PrimaryUpgradeDamageMultiplier = Math.Clamp(d.PrimaryUpgradeDamageMultiplier + 0.1f, 1f, 5f);
        }
        else if (index == 13)
        {
            d.SecondaryUpgradeDuration = Math.Clamp(d.SecondaryUpgradeDuration + inc * 0.5f, 1f, 60f);
        }
        else if (index == 14)
        {
            d.SecondaryUpgradeDamageMultiplier = Math.Clamp(d.SecondaryUpgradeDamageMultiplier + 0.1f, 1f, 5f);
        }
        else if (index == 15)
        {
            d.ShieldRepairAmount = Math.Clamp(d.ShieldRepairAmount + inc, 1, 100);
        }
        else if (index == 16)
        {
            d.StructureRepairAmount = Math.Clamp(d.StructureRepairAmount + inc, 1, 200);
        }
        else if (index == 17)
        {
            d.RapidCoolDuration = Math.Clamp(d.RapidCoolDuration + inc * 0.5f, 1f, 60f);
        }
        else if (index == 18)
        {
            d.DiagonalShotDuration = Math.Clamp(d.DiagonalShotDuration + inc * 0.5f, 1f, 60f);
        }
        else if (index == 19)
        {
            d.PowerShotDuration = Math.Clamp(d.PowerShotDuration + inc * 0.5f, 1f, 60f);
        }
        else if (index == 20)
        {
            d.TripleShotDuration = Math.Clamp(d.TripleShotDuration + inc * 0.5f, 1f, 60f);
        }
        AppConfig.Save();
    }

    /// <summary>
    /// Dessine le menu setup avec ses sous-menus.
    /// </summary>
    public override void Draw(SpriteBatch spriteBatch)
    {
        var panelX = (ScreenManager._Instance._Dimensions.X - 480) / 2f;
        var panelY = (ScreenManager._Instance._Dimensions.Y - 500) / 2f;

        // Fond léger du panneau
        spriteBatch.Draw(_borderTexture,
            new Rectangle((int)panelX, (int)panelY, PanelWidth, PanelHeight),
            new Color(30, 30, 40, 180));

        string title;
        string[] items;

        if (_activeMenu == SubMenu.None)
        {
            title = "PARAMETRES";
            items = _mainMenuItems.ToArray();
        }
        else if (_activeMenu == SubMenu.Video)
        {
            title = "VIDEO (désactivé)";
            items = new[]
            {
                "Changements non appliqués",
                "Retour"
            };
        }
        else if (_activeMenu == SubMenu.Audio)
        {
            title = "AUDIO";
            items = new[]
            {
                "Master : " + (AppConfig.Audio.MasterVolume * 100).ToString("F0") + "%",
                "Musique : " + (AppConfig.Audio.MusicVolume * 100).ToString("F0") + "%",
                "Sons : " + (AppConfig.Audio.SfxVolume * 100).ToString("F0") + "%",
                "Retour"
            };
        }
        else
        {
            var d = AppConfig.Difficulty;
            title = "DIFFICULTE";
            items = new[]
            {
                "Preset : " + d.Preset,
                "Spawn : " + d.SpawnRateModifier.ToString("+0;-0") + "%",
                "PV Ennemis : " + d.EnemyHpModifier.ToString("+0;-0") + "%",
                "Vitesse Ennemis : " + d.EnemySpeedModifier.ToString("+0;-0") + "%",
                "Degats Joueur : " + d.PlayerDamageMultiplier.ToString("F1") + "x",
                "Structure : " + d.StructureHp + " PV",
                "Chauffe Laser : " + d.LaserHeatRateModifier.ToString("+0;-0") + "%",
                "Refroid. Repos : " + d.LaserIdleCoolMultiplier.ToString("F1") + "x",
                "Deg. Bouclier Ennemi : " + d.EnemyBulletDamage,
                "Deg. Contact Ennemi : " + d.EnemyContactDamage,
                "Duree Upg. Primaire : " + d.PrimaryUpgradeDuration.ToString("F1") + "s",
                "Multi Deg. Primaire : " + d.PrimaryUpgradeDamageMultiplier.ToString("F1") + "x",
                "Duree Upg. Secondaire : " + d.SecondaryUpgradeDuration.ToString("F1") + "s",
                "Multi Deg. Secondaire : " + d.SecondaryUpgradeDamageMultiplier.ToString("F1") + "x",
                "Reparation Bouclier : " + d.ShieldRepairAmount + " PV",
                "Reparation Structure : " + d.StructureRepairAmount + " PV",
                "Duree RapidCool : " + d.RapidCoolDuration.ToString("F1") + "s",
                "Duree DiagonalShot : " + d.DiagonalShotDuration.ToString("F1") + "s",
                "Duree PowerShot : " + d.PowerShotDuration.ToString("F1") + "s",
                "Duree TripleShot : " + d.TripleShotDuration.ToString("F1") + "s",
                "Retour"
            };
        }

        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2(panelX + (480 - titleSize.X) / 2f, 20),
            Color.White);

        float startY = 70;
        float spacing = 36;

        for (int i = 0; i < items.Length; i++)
        {
            float textX = panelX + 30f;
            Vector2 pos = new Vector2(textX, startY + i * spacing);

            Color color = (i == _selectedIndex) ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, items[i], pos, color);

            if (i == _selectedIndex)
            {
                Vector2 selPos = new Vector2(pos.X - 24, pos.Y + 4);
                spriteBatch.Draw(_selectorTexture, selPos, Color.White);
            }
        }

        string instructions = "Haut/Bas : Navi | Entree : Valider | Echap : Retour";
        if (_activeMenu != SubMenu.None)
            instructions += " | +/- : Modifier";
        var instSize = _font.MeasureString(instructions);
        spriteBatch.DrawString(_font, instructions,
            new Vector2(panelX + (480 - instSize.X) / 2f, 485),
            Color.Gray);
    }
}
