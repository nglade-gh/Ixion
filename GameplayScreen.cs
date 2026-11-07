using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

/// <summary>
/// Écran principal de jeu. Orchestre toutes les entités, gère les collisions, les spawns, les bonus et le HUD.
/// Tous les paramètres proviennent de <see cref="GameConfig"/> (Content/config/*.xml).
/// </summary>
public class GameplayScreen : GameScreen
{
    private SpriteFont _font;
    private Texture2D _bonusTexture;

    /// <summary>
    /// Vaisseau du joueur.
    /// </summary>
    private Player _player;

    /// <summary>
    /// Gestionnaire de décor.
    /// </summary>
    private DecorManager _decor;

    /// <summary>
    /// Service audio pour les effets sonores.
    /// </summary>
    private AudioService _audio;

    /// <summary>
    /// Liste des ennemis vivants.
    /// </summary>
    private List<Enemy> _enemies = new();

    /// <summary>
    /// Projectiles tirés par le joueur.
    /// </summary>
    private List<Bullet> _playerBullets = new();

    /// <summary>
    /// Projectiles tirés par les ennemis.
    /// </summary>
    private List<Bullet> _enemyBullets = new();

    /// <summary>
    /// Particules d'effet visuel.
    /// </summary>
    private List<Particle> _particles = new();

    /// <summary>
    /// Bonus tombants collectables.
    /// </summary>
    private List<Bonus> _bonuses = new();

    private Texture2D _playerTexture;
    private Texture2D _particleTexture;
    private Texture2D[] _shieldNormalFrames;
    private Texture2D[] _shieldBlastFrames;
    private Shield _shield;
    private readonly Dictionary<string, Texture2D> _enemyTextures = new();
    private readonly Dictionary<string, Texture2D> _bulletTextures = new();
    private readonly Dictionary<string, Texture2D[]> _explosionFrames = new();
    private readonly List<Explosion> _explosions = new();

    /// <summary>
    /// Parametres de jeu charges depuis la configuration (difficulte).
    /// </summary>
    private GameplaySettings _gameplaySettings;

    /// <summary>
    /// Timer d'apparition des ennemis.
    /// </summary>
    private float _enemySpawnTimer;

    /// <summary>
    /// Intervalle entre les spawns d'ennemis, en secondes.
    /// </summary>
    private float _enemySpawnInterval;

    /// <summary>
    /// Numéro de vague actuel (influence la difficulte).
    /// </summary>
    private float _wave = 1f;

    /// <summary>
    /// Indique si la partie est terminée.
    /// </summary>
    private bool _gameOver;

    /// <summary>
    /// Timer après le game over avant de pouvoir revenir au menu.
    /// </summary>
    private float _gameOverTimer;

    private bool _paused;

    /// <summary>
    /// Timer pour la gestion du menu pause.
    /// </summary>
    private float _pauseTimer;

    /// <summary>
    /// Index sélectionné dans le menu pause.
    /// </summary>
    private int _pauseSelected;

    /// <summary>
    /// Indique si LoadContent a déjà été appelé (pour éviter les réinitialisations sur PopScreen).
    /// </summary>
    private bool _contentLoaded;

    /// <summary>
    /// Appelée quand PopScreen restaure cet écran depuis un sous-menu.
    /// Rétablit le jeu en mode actif.
    /// </summary>
    public override void OnResumeFromPause()
    {
        _paused = false;
    }

    /// <summary>
    /// Générateur de nombres aléatoires.
    /// </summary>
    private Random _random;

    /// <summary>
    /// Charge toutes les textures et initialise le décor et le joueur.
    /// </summary>
    public override void LoadContent()
    {
        try
        {
            Console.WriteLine("  [GameplayScreen.LoadContent] DEBUT");
            base.LoadContent();
            Console.WriteLine("  [GameplayScreen.LoadContent] base.LoadContent OK");
            _random = new Random();
            GameConfig.Instance.LoadAll();
            Console.WriteLine("  [GameplayScreen.LoadContent] LoadAll OK");

            _font = _content.Load<SpriteFont>("Roman15");
            Console.WriteLine("  [GameplayScreen.LoadContent] Font OK");

            // Connecter le service audio
            _audio = ScreenManager._Instance.Audio;
            Console.WriteLine("  [GameplayScreen.LoadContent] Audio connecté");

            var cfg = GameConfig.Instance;
            _playerTexture = _content.Load<Texture2D>(GameConfig.ContentKey(cfg.Entities.Player.Texture));
            Console.WriteLine("  [GameplayScreen.LoadContent] Player tex OK");

            // Load shield frames
            _shieldNormalFrames = LoadFrames("pics/shields/Shield_Bleu");
            _shieldBlastFrames = LoadFrames("pics/shields/Blast_Blanc");
            Console.WriteLine($"  [GameplayScreen.LoadContent] Shields: {_shieldNormalFrames.Length} + {_shieldBlastFrames.Length} frames");
            _shield = new Shield(_shieldNormalFrames, _shieldBlastFrames, 0.1f, 0.35f, 2.0f);

            // Load explosion frames
            string[] explosionTypes = {
                "PetiteExplosion_Missile", "Explosion_Vaisseau_Petit",
                "Explosion_Vaisseau_Gros", "GrosseExplosion_Virus",
                "ExplosionMoyenne_Virus", "Explosion_Sol",
                "PetiteExplosion_Decor", "Fumee_Residuelle"
            };
            foreach (var type in explosionTypes)
                _explosionFrames[type] = LoadFrames($"pics/explosions/{type}");
            Console.WriteLine($"  [GameplayScreen.LoadContent] Explosions: {_explosionFrames.Count} types");

            foreach (var type in cfg.Enemies.EnemyTypes)
                LoadTextureOnce(_enemyTextures, type.Texture);
            foreach (var bullet in cfg.Bullets.Bullets)
                LoadTextureOnce(_bulletTextures, bullet.Texture);
            Console.WriteLine($"  [GameplayScreen.LoadContent] Enemies: {_enemyTextures.Count} Bullets: {_bulletTextures.Count}");

            // Bonus texture fallback
            if (cfg.Bonuses.Bonuses.Count > 0)
            {
                string btex = cfg.Bonuses.Bonuses[0].Texture;
                _bonusTexture = LoadTextureOnce(_bulletTextures, btex);
            }

            _particleTexture = Sprite.CreateDotTexture(ScreenManager._Instance._graphicsDevice, 4, Color.White);
            _decor = new DecorManager(cfg.Decors, ScreenManager._Instance._Dimensions, _random);
            _decor.Initialize(_content, ScreenManager._Instance._graphicsDevice);
            Console.WriteLine("  [GameplayScreen.LoadContent] Decor OK");

            // Apply gameplay settings from AppConfig
            _gameplaySettings = AppConfig.GetGameplaySettings();
            _enemySpawnInterval = _gameplaySettings.BaseSpawnInterval;

            // Only initialize game state on first load, not on PopScreen restore
            if (!_contentLoaded)
            {
                InitializeGame();
                _contentLoaded = true;
            }
            Console.WriteLine("  [GameplayScreen.LoadContent] COMPLETE OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [GameplayScreen.LoadContent] ERREUR: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"    Stack: {ex.StackTrace?.Split('\n')[0]}");
            throw;
        }
    }

    private Texture2D LoadTextureOnce(Dictionary<string, Texture2D> cache, string path)
    {
        string key = GameConfig.ContentKey(path);
        if (string.IsNullOrEmpty(key) || cache.ContainsKey(key))
            return cache[key];
        cache[key] = _content.Load<Texture2D>(key);
        return cache[key];
    }

    /// <summary>
    /// Charge une série de frames d'animation (1.png, 2.png, ...).
    /// </summary>
    /// <param name="basePath">Chemin de base sans extension ni numéro.</param>
    /// <returns>Tableau de textures chargées.</returns>
    private Texture2D[] LoadFrames(string basePath)
    {
        var frames = new List<Texture2D>();
        for (int i = 1; i <= 8; i++)
        {
            string frameKey = $"{basePath}/{i}";
            try
            {
                frames.Add(_content.Load<Texture2D>(frameKey));
            }
            catch
            {
                break;
            }
        }
        return frames.ToArray();
    }

    /// <summary>
    /// Réinitialise toutes les listes d'entités et replace le joueur.
    /// </summary>
    private void InitializeGame()
    {
        var cfg = GameConfig.Instance;
        Vector2 spawn = new Vector2(
            ScreenManager._Instance._Dimensions.X / 2f,
            ScreenManager._Instance._Dimensions.Y - 100f);

        _player = new Player(cfg.Entities.Player, _playerTexture, spawn);
        _player.Shield = _shield;
        _player.SetPlayArea(_decor.PlayArea);
        _shield.Intensity = 1f;

        // Apply difficulty settings
        _player.MaxHP = _gameplaySettings.PlayerStructureHp;
        _player.HP = _gameplaySettings.PlayerStructureHp;
        _player.ShieldEnergy = _gameplaySettings.PlayerShieldStart;
        _player.ShieldRegenMultiplier = _gameplaySettings.ShieldRegenMultiplier;

        // Démarrer la musique de fond au début d'une partie
        if (_audio != null)
        {
            _audio.PlayMusic("sounds/bgm_loop");
        }

        _enemies.Clear();
        _playerBullets.Clear();
        _enemyBullets.Clear();
        _particles.Clear();
        _bonuses.Clear();
        _explosions.Clear();
        _enemySpawnTimer = 0f;
        _enemySpawnInterval = 1.5f;
        _wave = 1f;
        _gameOver = false;
        _gameOverTimer = 0f;
    }

    /// <summary>
    /// Boucle de jeu : mise à jour du décor, du joueur, des tirs, ennemis, collisions, spawns et HUD.
    /// </summary>
    /// <param name="gameTime">Temps écoulé depuis la dernière frame.</param>
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Toggle pause on PauseGame action
        if (InputService.Instance.IsActionPressed("PauseGame"))
        {
            _paused = !_paused;
            if (_paused)
            {
                _pauseSelected = 0;
                _pauseTimer = 0f;
            }
            return;
        }

        _decor.Update(gameTime);

        if (_gameOver)
        {
            _gameOverTimer += dt;
            if (InputService.Instance.IsActionPressed("MenuSelect") && _gameOverTimer > 1.5f)
                ScreenManager._Instance.ChangeScreen(new MenuScreen());
            return;
        }

        if (_paused)
        {
            // On ne fait que gérer l'input en pause; le dessin est dans Draw()
            UpdatePauseMenu();
            return;
        }

        _player.Update(gameTime);

        // Player firing
        if (InputService.Instance.IsActionHeld("FirePrimary") && _player.TryFirePrimary())
        {
            BulletParams bp = GameConfig.Instance.GetBullet("PlayerLaser");
            if (_player.PrimaryUpgraded)
            {
                bp = new BulletParams
                {
                    Name = bp.Name,
                    Texture = bp.Texture,
                    Width = bp.Width,
                    Height = bp.Height,
                    Speed = bp.Speed,
                    Damage = bp.Damage * 2,
                    Kind = bp.Kind,
                    Homing = bp.Homing,
                    SearchRadius = bp.SearchRadius,
                    ColorRaw = "255,80,120,255",
                    Rotation = bp.Rotation,
                    SourceRectRaw = bp.SourceRectRaw
                };
            }
            Vector2 pos = new Vector2(
                _player.Position.X,
                _player.Position.Y - _player.Size.Y * _player.Scale / 2f);
            _playerBullets.Add(new Bullet(bp, _bulletTextures[GameConfig.ContentKey(bp.Texture)],
                pos, new Vector2(0f, -bp.Speed), null));
            if (_audio != null) _audio.PlaySfx("sounds/laser_fire");
        }

        if (InputService.Instance.IsActionHeld("FireSecondary") && _player.TryFireSecondary())
        {
            BulletParams bp = GameConfig.Instance.GetBullet("PlayerPlasma");
            if (_player.SecondaryUpgraded)
            {
                bp = new BulletParams
                {
                    Name = bp.Name,
                    Texture = bp.Texture,
                    Width = bp.Width,
                    Height = bp.Height,
                    Speed = bp.Speed,
                    Damage = bp.Damage * 2,
                    Kind = bp.Kind,
                    Homing = bp.Homing,
                    SearchRadius = bp.SearchRadius,
                    ColorRaw = "255,0,255,255",
                    Rotation = bp.Rotation,
                    SourceRectRaw = bp.SourceRectRaw
                };
            }
            Vector2 pos = new Vector2(
                _player.Position.X,
                _player.Position.Y - _player.Size.Y * _player.Scale / 2f);
            _playerBullets.Add(new Bullet(bp, _bulletTextures[GameConfig.ContentKey(bp.Texture)],
                pos, new Vector2(0f, -bp.Speed), null));
        }

        if (InputService.Instance.IsActionHeld("FireSpecial") && _player.TryFireSpecial())
        {
            BulletParams bp = GameConfig.Instance.GetBullet("PlayerSpecial");
            Vector2 pos = new Vector2(
                _player.Position.X,
                _player.Position.Y - _player.Size.Y * _player.Scale / 2f);
            _playerBullets.Add(new Bullet(bp, _bulletTextures[GameConfig.ContentKey(bp.Texture)],
                pos, new Vector2(0f, -bp.Speed), null));
        }

        // Bullets
        foreach (var bullet in _playerBullets)
            bullet.Update(gameTime);
        _playerBullets.RemoveAll(b => !b.IsActive);

        foreach (var bullet in _enemyBullets)
            bullet.Update(gameTime);
        _enemyBullets.RemoveAll(b => !b.IsActive);

        // Enemies
        foreach (var enemy in _enemies)
        {
            enemy.TargetPosition = _player.Position;
            enemy.Update(gameTime);
            if (enemy.TryFire())
                FireEnemy(enemy);
        }
        _enemies.RemoveAll(e => !e.IsActive);

        // Bonuses
        foreach (var bonus in _bonuses)
            bonus.Update(gameTime);
        _bonuses.RemoveAll(b => !b.IsActive);

        // Particles
        foreach (var particle in _particles)
            particle.Update(gameTime);
        _particles.RemoveAll(p => !p.IsActive);

        // Explosions
        foreach (var explosion in _explosions)
            explosion.Update(gameTime);
        _explosions.RemoveAll(e => !e.IsActive);

        // Spawning
        _enemySpawnTimer += dt;
        if (_enemySpawnTimer >= _enemySpawnInterval)
        {
            _enemySpawnTimer = 0f;
            SpawnEnemy();
        }

        HandleCollisions();
        HandleBonusCollection();

        // Difficulty ramps up over time
        _wave += dt * _gameplaySettings.WaveGrowthRate;
        _enemySpawnInterval = Math.Max(_gameplaySettings.MinSpawnInterval, _enemySpawnInterval - dt * 0.1f);

        base.Update(gameTime);
    }

    // -----------------------------------------------------------------
    // Spawning
    // -----------------------------------------------------------------

    /// <summary>
    /// Fait apparaître un ennemi (ou une horde) selon les paramètres XML.
    /// </summary>
    private void SpawnEnemy()
    {
        var types = GameConfig.Instance.Enemies.EnemyTypes;
        var type = types[_random.Next(types.Count)];

        // Hordes: groups of Scouts or Fighters
        if ((string.Equals(type.Name, "Scout", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(type.Name, "Fighter", StringComparison.OrdinalIgnoreCase)) &&
            _random.NextDouble() < 0.35)
        {
            int count = 3 + _random.Next(3);
            float spacing = 70f;
            float areaLeft = _decor.PlayArea.Left + 30f;
            float areaWidth = _decor.PlayArea.Width - 60f - (count - 1) * spacing;
            float startX = areaLeft + (float)(_random.NextDouble() * Math.Max(0f, areaWidth));
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = new Vector2(startX + i * spacing, -40f - (i % 2) * 30f);
                _enemies.Add(CreateEnemy(type, pos));
            }
            return;
        }

        float x = (float)(_random.NextDouble() * (_decor.PlayArea.Width - 60f)) + _decor.PlayArea.Left + 30f;
        _enemies.Add(CreateEnemy(type, new Vector2(x, -40f)));
    }

    /// <summary>
    /// Cree une instance d'ennemi a partir de son archetype.
    /// Les multiplicateurs de difficulte sont appliques ici.
    /// </summary>
    /// <param name="type">Archetype de l'ennemi.</param>
    /// <param name="position">Position de spawn.</param>
    /// <returns>Ennemi cree.</returns>
    private Enemy CreateEnemy(EnemyType type, Vector2 position)
    {
        Texture2D tex = _enemyTextures[GameConfig.ContentKey(type.Texture)];
        Enemy enemy = new Enemy(type, tex, position, type.SourceRect, _wave);
        // Apply difficulty multipliers
        enemy.HP = (int)(enemy.HP * _gameplaySettings.EnemyHpMultiplier);
        enemy.MaxHP = enemy.HP;
        enemy.Speed *= _gameplaySettings.EnemySpeedMultiplier;
        return enemy;
    }

    /// <summary>
    /// Fait apparaître un bonus à la position donnée (8% de chance).
    /// </summary>
    /// <param name="position">Position de spawn du bonus.</param>
    private void SpawnBonus(Vector2 position)
    {
        if (GameConfig.Instance.Bonuses.Bonuses.Count == 0) return;
        if (_random.NextDouble() > 0.08f) return;

        var bonusParams = GameConfig.Instance.Bonuses.Bonuses[_random.Next(GameConfig.Instance.Bonuses.Bonuses.Count)];
        Texture2D tex = _bonusTexture;
        if (!string.IsNullOrEmpty(bonusParams.Texture) && _bulletTextures.ContainsKey(GameConfig.ContentKey(bonusParams.Texture)))
            tex = _bulletTextures[GameConfig.ContentKey(bonusParams.Texture)];

        var bonus = new Bonus(bonusParams, tex, position);
        _bonuses.Add(bonus);
    }

    // -----------------------------------------------------------------
    // Enemy attacks
    // -----------------------------------------------------------------

    /// <summary>
    /// Fait tirer un ennemi selon son pattern d'attaque.
    /// </summary>
    /// <param name="enemy">Ennemi qui tire.</param>
    private void FireEnemy(Enemy enemy)
    {
        var cfg = GameConfig.Instance;
        Vector2 muzzle = new Vector2(
            enemy.Position.X,
            enemy.Position.Y + enemy.Size.Y * enemy.Scale / 2f);

        switch (enemy.BulletPattern)
        {
            case BulletPattern.Simple:
                // Les tourelles et gros ennemis tirent des lasers
                if (enemy.Speed < 50f || enemy.Size.X > 50f)
                {
                    SpawnEnemyBullet(cfg.GetBullet("EnemyLaser"), muzzle, new Vector2(0f, 1f));
                }
                else
                {
                    SpawnEnemyBullet(cfg.GetBullet("EnemyBullet"), muzzle, new Vector2(0f, 1f));
                }
                break;
            case BulletPattern.Burst:
                string burstBullet = (_random.Next(2) == 0) ? "EnemyLaser" : "EnemyBullet";
                foreach (float angle in new[] { -0.35f, 0f, 0.35f })
                {
                    Vector2 dir = new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle));
                    SpawnEnemyBullet(cfg.GetBullet(burstBullet), muzzle, dir);
                }
                break;
            case BulletPattern.Homing:
                Vector2 toPlayer = _player.Position - muzzle;
                SpawnEnemyBullet(cfg.GetBullet("EnemyHoming"), muzzle, Vector2.Normalize(toPlayer), _player);
                if (_audio != null) _audio.PlaySfx("sounds/missile_launch");
                break;
        }
    }

    /// <summary>
    /// Crée un projectile ennemi et l'ajoute à la liste.
    /// </summary>
    /// <param name="p">Paramètres du projectile.</param>
    /// <param name="position">Position de spawn.</param>
    /// <param name="direction">Direction normalisée.</param>
    /// <param name="target">Cible optionnelle pour homing.</param>
    private void SpawnEnemyBullet(BulletParams p, Vector2 position, Vector2 direction, Player target = null)
    {
        var tex = _bulletTextures[GameConfig.ContentKey(p.Texture)];
        bool isHomingMissile = target != null && p.Homing > 0f;
        _enemyBullets.Add(new Bullet(p, tex, position, direction * p.Speed, target, isHomingMissile));
    }

    // -----------------------------------------------------------------
    // Collisions
    // -----------------------------------------------------------------

    /// <summary>
    /// Gère toutes les collisions : tirs joueur vs ennemis, tirs joueur vs missiles homing, tirs ennemis vs joueur, ennemi vs joueur.
    /// </summary>
    private void HandleCollisions()
    {
        // Player bullets vs enemies
        foreach (var bullet in _playerBullets)
        {
            if (!bullet.IsActive) continue;
            Rectangle bulletBox = Shrink(bullet.Bounds, 0.8f);
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsActive) continue;
                if (bulletBox.Intersects(Shrink(enemy.Bounds, 0.75f)))
                {
                    bullet.IsActive = false;
                    if (enemy.TakeDamage(bullet.Damage))
                    {
                        _player.Score += enemy.ScoreValue;
                        SpawnExplosion(enemy.Position, "Explosion_Vaisseau_Petit", 1f);
                        SpawnBonus(enemy.Position);
                        if (_audio != null) _audio.PlaySfx("sounds/explosion");
                    }
                    else
                    {
                        SpawnExplosion(bullet.Position, "PetiteExplosion_Decor", 0.6f);
                        if (_audio != null) _audio.PlaySfx("sounds/explosion");
                    }
                    break;
                }
            }
        }

        // Player bullets vs enemy bullets (all enemy bullets are destructible)
        foreach (var bullet in _playerBullets)
        {
            if (!bullet.IsActive) continue;
            foreach (var missile in _enemyBullets)
            {
                if (!missile.IsActive) continue;
                // Both homing and straight bullets are destructible
                if (bullet.Bounds.Intersects(Shrink(missile.Bounds, 0.7f)))
                {
                    bullet.IsActive = false;
                    if (missile.TakeDamage(bullet.Damage))
                    {
                        SpawnExplosion(missile.Position, "PetiteExplosion_Missile", 1f);
                        _player.Score += 5;
                        if (_audio != null) _audio.PlaySfx("sounds/explosion");
                    }
                    break;
                }
            }
        }

        if (!_player.IsActive || _player.Invincible)
            return;

        Rectangle playerBox = Shrink(_player.Bounds, 0.7f);

        foreach (var bullet in _enemyBullets)
        {
            if (!bullet.IsActive) continue;
            if (bullet.Bounds.Intersects(playerBox))
            {
                bullet.IsActive = false;
                _player.TakeDamage(10);
                if (_audio != null) _audio.PlaySfx("sounds/shield_hit");
                if (!_player.IsActive)
                {
                    _gameOver = true;
                    _gameOverTimer = 0f;
                    if (_audio != null) _audio.PlaySfx("sounds/player_death");
                }
                break;
            }
        }

        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive) continue;
            if (enemy.Bounds.Intersects(playerBox))
            {
                enemy.IsActive = false;
                SpawnExplosion(enemy.Position, "Explosion_Vaisseau_Gros", 1f);
                _player.TakeDamage(20);
                if (_audio != null) _audio.PlaySfx("sounds/explosion");
                if (!_player.IsActive)
                {
                    _gameOver = true;
                    _gameOverTimer = 0f;
                    if (_audio != null) _audio.PlaySfx("sounds/player_death");
                }
                break;
            }
        }
    }

    /// <summary>
    /// Vérifie si le joueur touche un bonus et applique son effet.
    /// </summary>
    private void HandleBonusCollection()
    {
        if (!_player.IsActive) return;
        Rectangle playerBox = Shrink(_player.Bounds, 0.7f);
        foreach (var bonus in _bonuses)
        {
            if (!bonus.IsActive) continue;
            if (bonus.Bounds.Intersects(playerBox))
            {
                bonus.IsActive = false;
                ApplyBonus(bonus);
                if (_audio != null) _audio.PlaySfx("sounds/bonus_pickup");
            }
        }
    }

    /// <summary>
    /// Applique l'effet d'un bonus au joueur selon son type.
    /// </summary>
    /// <param name="bonus">Bonus collecté.</param>
    private void ApplyBonus(Bonus bonus)
    {
        var p = bonus.Params;
        if (string.Equals(p.Name, "PrimaryUpgrade", StringComparison.OrdinalIgnoreCase))
        {
            _player.ApplyPrimaryUpgrade(8f, 0);
        }
        else if (string.Equals(p.Name, "SecondaryUpgrade", StringComparison.OrdinalIgnoreCase))
        {
            _player.ApplySecondaryUpgrade(8f, 0);
        }
        else if (string.Equals(p.Name, "SpecialAmmo", StringComparison.OrdinalIgnoreCase))
        {
            _player.AddSpecialAmmo(p.Amount > 0 ? p.Amount : 3);
        }
        else if (string.Equals(p.Name, "ShieldRepair", StringComparison.OrdinalIgnoreCase))
        {
            _player.RepairShield(p.Amount);
        }
        else if (string.Equals(p.Name, "StructureRepair", StringComparison.OrdinalIgnoreCase))
        {
            _player.RepairStructure(p.Amount);
        }
    }

    /// <summary>
    /// Réduit un rectangle selon un facteur pour les collisions.
    /// </summary>
    /// <param name="r">Rectangle source.</param>
    /// <param name="factor">Facteur de réduction (0.0–1.0).</param>
    /// <returns>Rectangle réduit.</returns>
    private static Rectangle Shrink(Rectangle r, float factor)
    {
        int w = Math.Max(1, (int)(r.Width * factor));
        int h = Math.Max(1, (int)(r.Height * factor));
        return new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h);
    }

    /// <summary>
    /// Crée une explosion animée à la position donnée.
    /// </summary>
    /// <param name="position">Centre de l'explosion.</param>
    /// <param name="explosionType">Type d'explosion (nom du répertoire).</param>
    /// <param name="scale">Échelle d'affichage.</param>
    private void SpawnExplosion(Vector2 position, string explosionType, float scale = 1f)
    {
        if (!_explosionFrames.ContainsKey(explosionType) || _explosionFrames[explosionType].Length == 0)
            return;
        var explosion = new Explosion(_explosionFrames[explosionType], position, 0.05f, scale);
        _explosions.Add(explosion);
    }

    /// <summary>
    /// Crée une explosion animée avec une couleur et un nombre de particules (compatibilité).
    /// </summary>
    /// <param name="position">Centre de l'explosion.</param>
    /// <param name="color">Couleur (inutilisée, conservée pour compatibilité).</param>
    /// <param name="count">Nombre de particules (inutilisé).</param>
    private void SpawnExplosion(Vector2 position, Color color, int count)
    {
        string type = "PetiteExplosion_Decor";
        if (color == Color.OrangeRed) type = "PetiteExplosion_Missile";
        else if (color == Color.Red) type = "Explosion_Vaisseau_Petit";
        SpawnExplosion(position, type, 1f);
    }

    // -----------------------------------------------------------------
    // Rendering
    // -----------------------------------------------------------------

    /// <summary>
    /// Effectue le rendu complet de la frame : décor, entités, HUD, game over.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        _decor.DrawBack(spriteBatch);

        foreach (var enemy in _enemies)
            enemy.Draw(spriteBatch);
        _player.Draw(spriteBatch);
        if (_player.IsActive)
            _shield?.Draw(spriteBatch, _player.Position);
        foreach (var bullet in _playerBullets)
            bullet.Draw(spriteBatch);
        foreach (var bullet in _enemyBullets)
            bullet.Draw(spriteBatch);
        foreach (var bonus in _bonuses)
            bonus.Draw(spriteBatch);
        foreach (var particle in _particles)
            particle.Draw(spriteBatch);
        foreach (var explosion in _explosions)
            explosion.Draw(spriteBatch);

        _decor.DrawFront(spriteBatch);

        DrawHUD(spriteBatch);
        if (_gameOver)
            DrawGameOver(spriteBatch);
        if (_paused)
            DrawPauseMenu(spriteBatch);
    }

    /// <summary>
    /// Dessine l'interface utilisateur (score, vies, vague, barre de PV, upgrades).
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    private void DrawHUD(SpriteBatch spriteBatch)
    {
        // Shield bar (blue) - top right
        float barWidth = 150f;
        float barHeight = 12f;
        float shieldRatio = _player.ShieldEnergy / 100f;
        var shieldBarRect = new Rectangle(
            (int)(ScreenManager._Instance._Dimensions.X - barWidth - 10), 10,
            (int)(barWidth * Math.Clamp(shieldRatio, 0f, 1f)), (int)barHeight);
        spriteBatch.DrawString(_font, "Shield",
            new Vector2((int)(ScreenManager._Instance._Dimensions.X - barWidth - 10 - 50), 10),
            Color.Cyan);
        if (shieldBarRect.Width > 0)
            spriteBatch.Draw(_particleTexture, shieldBarRect, new Color(40, 120, 255));

        // Structure bar (red) - below shield
        if (_player.MaxHP > 0)
        {
            float structureRatio = (float)_player.HP / _player.MaxHP;
            var structBarRect = new Rectangle(
                (int)ScreenManager._Instance._Dimensions.X - (int)barWidth - 10, 30,
                (int)(barWidth * Math.Clamp(structureRatio, 0f, 1f)), (int)barHeight);
            spriteBatch.DrawString(_font, "Vaisseau",
                new Vector2((int)(ScreenManager._Instance._Dimensions.X - barWidth - 10 - 58), 30),
                Color.Red);
            if (structBarRect.Width > 0)
                spriteBatch.Draw(_particleTexture, structBarRect, new Color(220, 50, 50));
        }

        // Special ammo
        if (_player.SpecialAmmo > 0)
        {
            spriteBatch.DrawString(_font, "Special: " + _player.SpecialAmmo,
                new Vector2(10, 50), Color.Gold);
        }

        // Score (bottom-left area)
        spriteBatch.DrawString(_font, "Score: " + _player.Score,
            new Vector2(10, 50 + (_player.SpecialAmmo > 0 ? 20 : 0)), Color.Yellow);

        // Upgrade timers
        int hudY = 70;
        if (_player.PrimaryUpgraded)
        {
            spriteBatch.DrawString(_font, "Upgrade Prim: " + Math.Max(0, (int)_player.PrimaryUpgradeTimer) + "s",
                new Vector2(10, hudY), Color.Cyan);
            hudY += 20;
        }
        if (_player.SecondaryUpgraded)
        {
            spriteBatch.DrawString(_font, "Upgrade Sec: " + Math.Max(0, (int)_player.SecondaryUpgradeTimer) + "s",
                new Vector2(10, hudY), Color.Magenta);
            hudY += 20;
        }
    }

    /// <summary>
    /// Affiche l'écran de fin de partie et le score final.
    /// </summary>
    /// <param name="spriteBatch">Batch de rendu MonoGame.</param>
    private void DrawGameOver(SpriteBatch spriteBatch)
    {
        string text = "GAME OVER";
        string scoreText = "Score final : " + _player.Score;
        string restartText = "Appuyez sur Entree pour revenir au menu";

        Vector2 textSize = _font.MeasureString(text);
        Vector2 scoreSize = _font.MeasureString(scoreText);
        Vector2 restartSize = _font.MeasureString(restartText);

        spriteBatch.DrawString(_font, text,
            new Vector2((ScreenManager._Instance._Dimensions.X - textSize.X) / 2, 250), Color.Red);
        spriteBatch.DrawString(_font, scoreText,
            new Vector2((ScreenManager._Instance._Dimensions.X - scoreSize.X) / 2, 300), Color.White);
        spriteBatch.DrawString(_font, restartText,
            new Vector2((ScreenManager._Instance._Dimensions.X - restartSize.X) / 2, 350), Color.Gray);
    }

    // -----------------------------------------------------------------
    // Pause Menu
    // -----------------------------------------------------------------

    private void UpdatePauseMenu()
    {
        if (InputService.Instance.IsActionPressed("MenuUp"))
        {
            _pauseSelected--;
            if (_pauseSelected < 0) _pauseSelected = 2;
        }
        if (InputService.Instance.IsActionPressed("MenuDown"))
        {
            _pauseSelected++;
            if (_pauseSelected > 2) _pauseSelected = 0;
        }
        if (InputService.Instance.IsActionPressed("MenuSelect"))
        {
            if (_pauseSelected == 0)
            {
                // Reprendre
                _paused = false;
            }
            else if (_pauseSelected == 1)
            {
                // Reglages — ouvrir SetupScreen (push pour pouvoir pop, avec fromPause=true)
                ScreenManager._Instance.PushScreen(new SetupScreen(fromPause: true));
            }
            else if (_pauseSelected == 2)
            {
                // Retour au menu principal — on ne sauvegarde pas (la partie est perdue, c'est un choix)
                ScreenManager._Instance.ChangeScreen(new MenuScreen());
            }
        }
    }

    private void DrawPauseMenu(SpriteBatch sb)
    {
        // Overlay sombre semi-transparent
        Texture2D overlay = _bulletTextures.GetValueOrDefault("pics/bullets/white-circle", null);
        if (overlay == null)
            overlay = _particleTexture;
        sb.Draw(overlay,
            new Rectangle(0, 0, (int)ScreenManager._Instance._Dimensions.X, (int)ScreenManager._Instance._Dimensions.Y),
            new Color(0, 0, 0, 160));

        // Titre PAUSE
        string title = "PAUSE";
        Vector2 titleSize = _font.MeasureString(title);
        sb.DrawString(_font, title,
            new Vector2((ScreenManager._Instance._Dimensions.X - titleSize.X) / 2, 200), Color.White);

        // Items
        string[] items = { "Reprendre", "Reglages", "Retour au menu principal" };
        float startY = 300;
        float spacing = 50;

        for (int i = 0; i < items.Length; i++)
        {
            Vector2 textSize = _font.MeasureString(items[i]);
            Vector2 pos = new Vector2(
                (ScreenManager._Instance._Dimensions.X - textSize.X) / 2,
                startY + i * spacing);

            Color color = (i == _pauseSelected) ? Color.Yellow : Color.White;
            sb.DrawString(_font, items[i], pos, color);

            if (i == _pauseSelected)
            {
                // Flèche > dessinée en texte à la place de _selectorTexture
                sb.DrawString(_font, ">", new Vector2(pos.X - 25, pos.Y + (textSize.Y - 20) / 2), Color.Yellow);
            }
        }

        // Instructions
        string instructions = "Fleches : Naviguer | Entree : Valider | Echap : Reprendre";
        Vector2 instSize = _font.MeasureString(instructions);
        sb.DrawString(_font, instructions,
            new Vector2((ScreenManager._Instance._Dimensions.X - instSize.X) / 2,
            ScreenManager._Instance._Dimensions.Y - 40), Color.Gray);
    }
}
