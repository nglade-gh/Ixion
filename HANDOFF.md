# HANDOFF — MonoGame Shooter
**Date:** 2026-11-05  
**Projet:** Jeu spatial 2D / Shooter — MonoGame DesktopGL  
**État:** Compilé, prêt à tester en jeu

---

## 1. Objectif du projet
Développer un jeu de tir spatial 2D avec :
- Boucle de jeu complète (Update/Draw)
- Système de vague d'ennemis avec spawn progressif
- Tirs joueur (primaire, secondaire, spécial) et tirs ennemis
- Missiles homing destructibles par le joueur
- Bouclier animé avec séquence d'impact (flash rouge → transition bleu)
- Système de difficulté paramétrable (presets + fine-tuning)
- Menu principal → Setup (audio, difficulté) → Gameplay → Pause
- Assets chargés via MGCB (MonoGame Content Builder)

---

## 2. Architecture
### Structure des fichiers
```
Workspace: /workspace/Prog/dotnet/Programs/MonoGames/Shooter/
  - Shooter.csproj          → net6.0, MonoGame 3.8.1.303
  - Program.cs              → Entry point
  - GameManager.cs          → Main game class, handles screens
  - ScreenManager.cs        → Screen stack management
  - GameScreen.cs           → Abstract base class for screens
  - MenuScreen.cs           → Main menu (Start, Setup, Quit)
  - SetupScreen.cs          → Settings (Audio, Difficulty)
  - GameplayScreen.cs       → Core gameplay loop, collisions, HUD, spawns
  - AppConfig.cs            → Configuration (audio, video, difficulty presets + regen multiplier)
  - AudioService.cs         → Music + SFX with separate volume controls
  - Player.cs               → Player entity (movement, shooting, upgrades, shield, structure HP)
  - Enemy.cs                → Enemy entity (AI, shooting patterns)
  - Bullet.cs               → Projectile (straight, homing, destructible)
  - Shield.cs               → Shield visual + state (normal, blast, flash, transition)
  - Bonus.cs                → Collectible power-ups
  - Particle.cs             → Visual particles
  - Explosion.cs            → Explosion animation
  - Creature.cs             → Base class for moving entities
  - Entite.cs               → Base entity class (position, texture, bounds)
  - Personnage.cs           → Character base (velocity, health, fire rate)
  - Sprite.cs               → Base sprite (position, scale, rotation, draw)
  - Star.cs                 → Background stars
  - DecorManager.cs / DecorElement.cs → Scrolling background
  - GameConfig.cs / XmlConfig.cs → XML-based configuration loader
  - InputManager.cs / InputService.cs → Input handling
  - CameraService.cs        → Camera system
  - ResourceService.cs      → Resource management
  - EventService.cs         → Event system
```

### Architecture écran (state machine)
```
GameManager → ScreenManager → GameScreen stack
  MenuScreen (Start, Setup, Quit)
  SetupScreen (Audio submenu, Difficulty submenu)
  GameplayScreen (game loop, collisions, HUD)
    Pause overlay (Resume, Settings, Quit)
```

---

## 3. Décisions architecturales prises
1. **Audio indépendant** — `Music` class wrapper autour de `SoundEffectInstance` (pas `Song`) car DesktopGL ne supporte pas `MediaPlayer` avec WAV compilés MGCB.
2. **Configuration JSON** — `AppConfig` sérialisé en `Config/game.json` pour persistance audio, difficulté, inputs.
3. **Difficulté scalée** — Presets (Easy/Normal/Hard/Expert) + modificateurs fins (-100 à +100%). La difficulté affecte : spawn rate, ennemi HP, ennemi vitesse, dégâts joueur, énergie bouclier, PV structure, et régénération bouclier.
4. **Système de bouclier** — Le shield est un objet séparé avec animation de frames, gérant ses propres états visuels (normal, blast, flash rouge, transition bleu).
5. **HUD** — Barres d'énergie (Bleu=Shield, Rouge=Structure) en haut à droite, pas de texte "Vies".

---

## 4. Fichiers modifiés récemment et leurs rôles

| Fichier | Rôle | Dernier changement |
|---------|------|-------------------|
| `AppConfig.cs` | Configuration globale, presets difficulté, `ShieldRegenMultiplier` | Réécrit proprement (405 lignes). Mapping presets → regen: Easy=1.5, Normal=1.0, Hard=0.7, Expert=0.5. |
| `AudioService.cs` | Gestion audio (Music + SFX + preview tone) | Ajout `AudioCategory` enum, `PlayPreviewTone(AudioCategory cat)` avec volume par catégorie. |
| `Player.cs` | Entité joueur | Ajout `ShieldRegenMultiplier`, appliqué dans la regen (`ShieldRegenAmount * ShieldRegenMultiplier`). |
| `GameplayScreen.cs` | Boucle principale, collisions, HUD | Collision ennemis ↔ tirs joueur : tous les tirs ennemis sont destructibles (pas seulement homing). Applique `ShieldRegenMultiplier` au joueur. |
| `SetupScreen.cs` | Menu paramètres | Suppression cadre fin gris → fond plein sombre (`Color(30,30,40,180)`). `PlayPreviewTone` reçoit catégorie audio. |
| `Shield.cs` | Bouclier visuel | États: normal, blast, flash rouge (1s), transition rouge→bleu (0.5s). Alpha proportionnel à intensité. |

---

## 5. Modifications réalisées et résultats

### 5.1 Volume Music/SFX sur le bip (CORRIGÉ ✅)
**Problème:** `PlayPreviewTone` appliquait toujours `_masterVolume`, ignorant `MusicVolume` et `SfxVolume`.
**Fix:**
- Ajout enum `AudioCategory { Master, Music, Sfx }` dans `AudioService.cs`.
- Signature changée: `PlayPreviewTone(AudioCategory category = AudioCategory.Master)`.
- Switch sur catégorie pour choisir le bon gain: `_sfxVolume`, `_musicVolume` ou `_masterVolume`.
- `SetupScreen.cs`: `AdjustAudioSetting` passe la catégorie correspondante (index 0→Master, 1→Music, 2→Sfx).
**Résultat:** Le bip respecte le volume de la piste ajustée. Build OK.

### 5.2 Régénération du bouclier (CORRIGÉ ✅)
**Problème:** `ShieldRegenMultiplier` était défini dans `AppConfig` mais non appliqué dans le calcul de regen.
**Fix:**
- `Player.cs`: nouvelle propriété publique `ShieldRegenMultiplier { get; set; } = 1f`.
- Formule de regen changée: `ShieldEnergy = Math.Min(100f, ShieldEnergy + ShieldRegenAmount * ShieldRegenMultiplier)`.
- `GameplayScreen.cs` `InitializeGame`: `_player.ShieldRegenMultiplier = _gameplaySettings.ShieldRegenMultiplier`.
**Résultat:** La regen est ×1.5 facile (rapide), ×1.0 normal, ×0.7 dur (lent), ×0.5 expert (très lent). Build OK.

### 5.3 Missiles homing destructibles (CORRIGÉ ✅)
**Problème:** Seuls les missiles homing étaient détruisibles (`!missile.IsHomingMissile`). Les balles droites ennemies n'étaient pas destructibles.
**Fix:** Dans `HandleCollisions()`, suppression de la condition `!missile.IsHomingMissile`. Toutes les balles ennemies (`_enemyBullets`) sont vérifiées contre les tirs joueur.
**Résultat:** Tirs joueur détruisent TOUS les tirs ennemis (homing et droits). Build OK.

### 5.4 Rectangle de bordure enlevé (CORRIGÉ ✅)
**Problème:** 4 bords fins gris dessinés avec `_borderTexture` étiré, créant un cadre visible.
**Fix:** Remplacement par un fond plein `_borderTexture` avec `Color(30, 30, 40, 180)` couvrant tout le panneau 480×500.
**Résultat:** Pas de bordure visible, fond sombre unifié. Build OK.

### 5.5 Bug scroll infini menu difficulté (DÉJÀ CORRIGÉ + précédent)
- Ajout `if (!rightPressed && !leftPressed) return;` en début de `AdjustDifficultySetting`.
- Le preset tourne uniquement sur appui (edge-triggered), pas sur hold.
- Borne fixée: indices 0-6, pas de wrap.

### 5.6 Bug de transparence bouclier (DÉJÀ CORRIGÉ + précédent)
- Alpha base: `0.2f * intensity` (80% transparent à plein, s'éteint à mesure qu'on prend des coups).
- Flash rouge pendant 1s à 80% alpha, puis transition 0.5s rouge→bleu.
- Intensity synced avec `ShieldEnergy / 100f` dans `Player.TakeDamage()`.

### 5.7 Barres d'énergie au lieu de vies (DÉJÀ CORRIGÉ + précédent)
- HUD affiche Shield (barre bleue 12px) et Vaisseau (barre rouge 12px) en haut à droite.
- La structure (rouge) ne diminue que quand le shield est vide (logique existante `TakeDamage` priorise shield).

---

## 6. Build et tests
- **Build:** `dotnet build Shooter.csproj -c Debug --nologo -v q -p:RestoreDotnetTools=false` → **succeeded** (0 erreur, 0 warning)
- **Cible:** net6.0, MonoGame.Framework.DesktopGL 3.8.1.303
- **Environnement:** `SDL_VIDEODRIVER=offscreen`, `SDL_VIDEO_OFFSCREEN_SIZE=640,480`, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`
- **Assets:** Content compilé via MGCB, syncrsync depuis `/workspace/Prog/dotnet/Assets/`

---

## 7. Tâches en cours / À tester
- **À tester en jeu:**
  1. Le bip audio change de volume pour Music et Sfx (pas seulement Master).
  2. Le shield se régénère lentement (plus lent en Hard/Expert).
  3. Les balles droites ennemies sont détruisibles par les tirs joueur.
  4. Plus de cadre/bordure visible dans SetupScreen.
- **Vérifications visuelles requises:**
  - Flash rouge → transition bleu fonctionne correctement après un impact.
  - Transparence du shield diminue progressivement avec les dégâts.

---

## 8. Problèmes connus / Points non résolus
- **Aucun problème bloquant en cours.** Build propre.
- **À vérifier:** Si le multiplicateur de spawn (`_enemySpawnInterval`) ne se superpose pas avec `AppConfig.GetGameplaySettings().BaseSpawnInterval`. Le `InitializeGame` initialise `_enemySpawnInterval = 1.5f` mais devrait utiliser `_gameplaySettings.BaseSpawnInterval`. Ce détail a été corrigé dans `LoadContent` mais pas dans `InitializeGame`. À vérifier.
- **Pas de tests unitaires** — le jeu se teste uniquement en runtime.
- **Pas de git commit** — les fichiers modifiés ne sont pas versionnés.

---

## 9. Conventions et contraintes
- **Language:** Français pour la communication, anglais pour le code.
- **Framework:** net6.0, MonoGame 3.8.1.303, DesktopGL.
- **Build command:** `SDL_VIDEODRIVER=offscreen SDL_VIDEO_OFFSCREEN_SIZE=640,480 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 /opt/data/home/.dotnet/dotnet build Shooter.csproj -c Debug --nologo -v q -p:RestoreDotnetTools=false`
- **Assets:** Compilés par MGCB, racine `/workspace/Prog/dotnet/Assets/pics/` et `sounds/`.
- **Audio:** Pas de `Song/MediaPlayer`, tout passe par `SoundEffect` + wrapper `Music`. Les sons sont générés en WAV 16-bit 88kHz dans le code.
- **Namespace:** `Shooter` pour tous les fichiers.
- **Asset root:** `/workspace/Prog/dotnet/Assets/` (syncrsync vers `Content/pics/`).

---

## 10. Instructions pour reprendre le travail

1. **Vérifier le build:**
   ```bash
   cd /workspace/Prog/dotnet/Programs/MonoGames/Shooter
   SDL_VIDEODRIVER=offscreen SDL_VIDEO_OFFSCREEN_SIZE=640,480 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 /opt/data/home/.dotnet/dotnet build Shooter.csproj -c Debug --nologo -v q -p:RestoreDotnetTools=false
   ```

2. **Vérifier le spawn interval dans InitializeGame:**
   - Ligne ~259 de `GameplayScreen.cs`: `_enemySpawnInterval = 1.5f;` devrait probablement être `_gameplaySettings.BaseSpawnInterval`.
   - `LoadContent` ligne 181 a déjà `_enemySpawnInterval = _gameplaySettings.BaseSpawnInterval;` mais `InitializeGame` l'écrase à 1.5f.

3. **Fichiers à lire avant de modifier:** Toujours lire le fichier complet (ou au moins la section ciblée) avant de patcher — le contexte compression peut avoir rendu des copies précédentes obsolètes.

4. **Build après chaque modification:** Toujours compiler et vérifier `Build succeeded` avant de claimer un changement.

5. **Conventions de patch:**
   - Utiliser `patch` plutôt que `write_file` pour les modifications ciblées.
   - `write_file` est utilisé quand le fichier est corrompu ou nécessite une réécriture totale.

---

*Fin du handoff.*
