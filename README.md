# SHOOTER

Shoot-up vertical 2D développé avec **MonoGame 3.8** en **C# / .NET 6.0**.
Le joueur contrôle un vaisseau spatial et doit survivre à des vagues
d'ennemis de plus en plus nombreuses et résistantes.

---

## Table des matières

1. [Architecture technique](#architecture-technique)
2. [Prérequis et installation](#prérequis-et-installation)
3. [Build et exécution](#build-et-exécution)
4. [Contrôles](#contrôles)
5. [Armes et système de combat](#armes-et-système-de-combat)
6. [Ennemis](#ennemis)
7. [Projets et tirs ennemis](#projets-et-tirs-ennemis)
8. [Bonus](#bonus)
9. [Difficulté et réglages](#difficulté-et-réglages)
10. [Configuration XML](#configuration-xml)
11. [Structure du dépôt](#structure-du-dépôt)
12. [Architecture logicielle](#architecture-logicielle)

---

## Architecture technique

| Élément | Détail |
|---|---|
| **Framework** | MonoGame 3.8 (DesktopGL) |
| **Langage** | C# / .NET 6.0 |
| **Fichier projet** | `Shooter.csproj` |
| **Pipeline d'actifs** | MonoGame Content Builder (`Content.mgcb`) |
| **Format de config** | XML avec schéma XSD (`shooter-config.xsd`) |
| **Résolutions supportées** | 600×800, 800×1000, 1024×768, 1280×720, 1920×1080 |

---

## Prérequis et installation

1. **.NET 6.0 SDK** — disponible sur [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/6.0)
2. **Git** — pour cloner le dépôt

```bash
git clone <url-du-dépôt>
cd Shooter
```

---

## Build et exécution

```bash
# Build uniquement
dotnet build

# Build + exécution
dotnet run

# Configuration Release
dotnet build -c Release
```

> **Remarque Docker :** dans un conteneur sans GPU, utilisez
> `SDL_VIDEODRIVER=offscreen dotnet run` pour exécuter en mode headless.

---

## Contrôles

| Action | Touche |
|---|---|
| **Se déplacer** | Flèches directionnelles |
| **Tir principal (laser)** | `Espace` |
| **Tir secondaire (plasma)** | `X` |
| **Tir spécial (missiles)** | `C` |
| **Pause / Menu** | `Échap` |

Les contrôles sont configurables via le menu *Réglages → Contrôles*.
La configuration est sauvegardée dans `Config/game.xml`.

---

## Armes et système de combat

Le joueur dispose de **trois armes** :

### Tir principal — Laser thermique

Tir rapide en continu. Le laser génère de la chaleur à chaque tir.

| Paramètre | Valeur de base |
|---|---|
| Fréquence de tir | 0,16 s |
| Dégâts par tir | 3 |
| Chaleur par tir | 5 |
| Chaleur maximale | 100 |
| Refroidissement de base | 20 / s |
| Seuil de surchauffe | 75 % de la chaleur max |

**Mécanique de surchauffe :** quand la température atteint 75 % du maximum,
la fréquence de tir est **divisée par 3** (tir ralenti). Quand la température
redescend sous 75 %, la fréquence retrouve sa valeur normale.

**Refroidissement au repos :** quand le joueur arrête de tirer, le refroidissement
est multiplié par le paramètre `LaserIdleCoolMultiplier` :

| Difficulté | Multiplicateur |
|---|---|
| Facile | 3,0× |
| Normal | 2,5× |
| Difficile | 2,0× |
| Expert | 1,5× |

Les sprites d'animation du laser (01.png – 66.png) défilent pour montrer
la progression de la température.

### Tir secondaire — Plasmagun

Tir plus lent mais plus puissant (dégâts : 12, vitesse : 500 px/s).
Soumis à un temps de recharge.

### Tir spécial — Missiles

Tirs massifs (dégâts : 25, vitesse : 400 px/s). Utilise une ammo limitée,
rechargeable via le bonus `SpecialAmmo` (+3) ou `MegaMissile` (+10).

### Upgrades de tir

Les bonus `PrimaryUpgrade` et `SecondaryUpgrade` améliorent temporairement
les tirs principaux et secondaires (durée configurable).

---

## Ennemis

Douze types d'ennemis, configurés dans `Content/config/enemies.xml` :

### Scout

| Propriété | Valeur |
|---|---|
| Taille | 46 px |
| PV | 15 |
| Vitesse | 150 px/s |
| Score | 15 |
| Motif de mouvement | Sinusoïdal (amplitude 140 px, fréquence 1,6 Hz) |
| Motif de tir | Simple (1 projectile) |

Petit ennemi rapide, se déplace en ondulation horizontale tout en descendant.

### Fighter

| Propriété | Valeur |
|---|---|
| Taille | 46 px |
| PV | 25 |
| Vitesse | 85 px/s |
| Score | 25 |
| Motif de mouvement | Horde (dérive en groupe) |
| Motif de tir | Burst (3 projectiles en éventail) |

Vaisseau de combat tirant en trio.

### Tank

| Propriété | Valeur |
|---|---|
| Taille | 88 px |
| PV | 120 |
| Vitesse | 35 px/s |
| Score | 60 |
| Motif de mouvement | Horde (faible amplitude) |
| Motif de tir | Burst (3 projectiles) |

Gros ennemi lent et résistant. Utilise une sous-rectangle d'un sprite-sheet.

### Elastic

| Propriété | Valeur |
|---|---|
| Taille | 44 px |
| PV | 20 |
| Vitesse | 120 px/s |
| Score | 35 |
| Motif de mouvement | Zigzag (large amplitude, haute fréquence) |
| Motif de tir | Simple |

Ennemi rapide avec des changements de direction brusques.

### Kamikaze

| Propriété | Valeur |
|---|---|
| Taille | 42 px |
| PV | 10 |
| Vitesse | 130 px/s |
| Score | 50 |
| Motif de mouvement | Charge directe |
| Motif de tir | Missile homing |

Ennemi suicidaire qui fonce vers le joueur et lance des missiles à tête chercheuse.

### EvilEye

| Propriété | Valeur |
|---|---|
| Taille | 64 px |
| PV | 45 |
| Vitesse | 70 px/s |
| Score | 40 |
| Motif de mouvement | Sinusoïdal |
| Motif de tir | Burst (3 projectiles) |

Œil flottant au mouvement gracieux mais au feu soutenu.

### Turret

| Propriété | Valeur |
|---|---|
| Taille | 72 px |
| PV | 90 |
| Vitesse | 25 px/s |
| Score | 80 |
| Motif de mouvement | Horde (très faible déplacement) |
| Motif de tir | Burst (3 projectiles) |

Tourence quasi-stationnée, dangereuse par son cadence de tir.

### SpikeBall

| Propriété | Valeur |
|---|---|
| Taille | 56 px |
| PV | 25 |
| Vitesse | 110 px/s |
| Score | 30 |
| Motif de mouvement | Zigzag |
| Motif de tir | Simple |

Boule épineuse rapide et erratique.

### Spaceship

| Propriété | Valeur |
|---|---|
| Taille | 70 px |
| PV | 60 |
| Vitesse | 60 px/s |
| Score | 55 |
| Motif de mouvement | Sinusoïdal |
| Motif de tir | Burst (3 projectiles) |

Vaisseau extraterrestre de taille moyenne.

### Swarm

| Propriété | Valeur |
|---|---|
| Taille | 28 px |
| PV | 5 |
| Vitesse | 160 px/s |
| Score | 8 |
| Motif de mouvement | Swar me (multi-sinus) |
| Motif de tir | Swarm (tirs diagonaux multiples) |

Petit, rapide et nombreux. Tire en dispersion diagonale.

### Spread

| Propriété | Valeur |
|---|---|
| Taille | 52 px |
| PV | 35 |
| Vitesse | 90 px/s |
| Score | 30 |
| Motif de mouvement | Zigzag |
| Motif de tir | Spread (3 projectiles en éventail large) |

Tire en éventail large, couvrant une zone plus grande.

### Spiral

| Propriété | Valeur |
|---|---|
| Taille | 56 px |
| PV | 40 |
| Vitesse | 75 px/s |
| Score | 35 |
| Motif de mouvement | Horde |
| Motif de tir | Spiral (3 projectiles rotatifs) |

Tire en rotation progressive, difficile à esquiver.

### Boss

| Propriété | Valeur |
|---|---|
| Taille | 120 px |
| PV | 250 |
| Vitesse | 30 px/s |
| Score | 200 |
| Motif de mouvement | Horde (faible déplacement) |
| Motif de tir | Burst (3 projectiles) |

Ennemi colossal, très résistant, servant de point culminant de difficulté.

---

## Projets et tirs ennemis

| Projectile | Vitesse | Dégâts | Spécial |
|---|---|---|---|
| **EnemyBullet** | 190 px/s | 12 | Tir simple |
| **EnemyHoming** | 150 px/s | 15 | Missile destructible, 2 PV, rate de virage 100°/s, rayon de recherche 220 px |
| **EnemyLaser** | 250 px/s | 8 | Tir destructible, 1 PV |
| **TurretLaser** | 200 px/s | 12 | Tir destructible, plus large, 1 PV |
| **SwarmBullet** | 280 px/s | 4 | Tir rapide, petit |
| **SwarmDiagLeft / DiagRight** | 260 px/s | 4 | Tirs diagonaux du Swarm |
| **EnergyBeam** | 180 px/s | 18 | Large rayon énergétique (boss) |
| **SpiralBullet** | 200 px/s | 6 | Tir rotatif |
| **TripleSpread** | 230 px/s | 7 | Trio en éventail |

---

## Bonus

Les ennemis vaincus lâchent un bonus (taille 3× leur taille originale).
11 types de bonus :

| Bonus | Couleur | Effet |
|---|---|---|
| **PrimaryUpgrade** | Cyan | Améliore le tir principal (durée configurable) |
| **SecondaryUpgrade** | Magenta | Améliore le tir secondaire (durée configurable) |
| **SpecialAmmo** | Jaune | +3 missiles spéciaux |
| **ShieldRepair** | Bleu | Répare 30 points de bouclier |
| **StructureRepair** | Vert | Répare 20 PV structure |
| **RapidCool** | Cyan clair | Triple le refroidissement laser pendant 20 s |
| **DiagonalShot** | Orange | Tir diagonal ajouté pendant 15 s |
| **PowerShot** | Rouge | Tir secondaire transformé en faisceau puissant pendant 20 s |
| **TripleShot** | Jaune clair | Tir principal en trio pendant 15 s |
| **MegaMissile** | Orange foncé | +10 missiles spéciaux |
| **ShieldBoost** | Bleu clair | +25 énergie de bouclier instantanée |

---

## Difficulté et réglages

Quatre presets prédéfinis, accessibles via le menu *Réglages → Difficulté* :

| Paramètre | Facile | Normal | Difficile | Expert |
|---|---|---|---|---|
| **Spawn Rate Modifier** | −50 % | 0 % | +40 % | +100 % |
| **Enemy HP Modifier** | −40 % | 0 % | +50 % | +100 % |
| **Enemy Speed Modifier** | −40 % | 0 % | +30 % | +60 % |
| **Player Damage Multiplier** | 1,5× | 1,0× | 0,8× | 0,5× |
| **Shield Start Energy** | 120 | 100 | 80 | 50 |
| **Structure HP** | 120 | 100 | 80 | 50 |
| **Shield Regen Multiplier** | 1,5× | 1,0× | 0,7× | 0,5× |
| **Laser Heat Rate** | −80 % | −66 % | −50 % | −33 % |
| **Laser Cooling Rate** | +200 % | +100 % | +75 % | +50 % |
| **Laser Idle Cool Multiplier** | 3,0× | 2,5× | 2,0× | 1,5× |

Chaque preset peut être affiné manuellement via les sliders (modificateurs de −100 % à +100 %).

### Audio

| Canal | Volume par défaut |
|---|---|
| Master | 70 % |
| Musique | 90 % |
| Effets sonores | 5 % |

### Vidéo

Résolution configurable parmi les 5 modes supportés (600×800 par défaut).
Mode plein écran activable.

---

## Configuration XML

Tous les fichiers XML utilisent le namespace
`https://www.timc.fr/nicolas-glade/dotnet/shooter` et sont validés
contre `Content/config/shooter-config.xsd`.

| Fichier | Contenu |
|---|---|
| `entities.xml` | Paramètres du joueur et ennemi de base (PV, vitesse, texture) |
| `enemies.xml` | 12 types d'ennemis avec mouvement, tir, stats |
| `bullets.xml` | 13 archétypes de projectiles (joueur + ennemis) |
| `bonuses.xml` | 11 types de bonus |
| `decors.xml` | Éléments de décor (murs, plafond, étoiles parallaxe) |
| `shooter-config.xsd` | Schéma XSD de validation |

Fichier de configuration utilisateur : `Config/game.xml` (vidéo, audio, difficulté, contrôles).

---

## Structure du dépôt

```
Shooter/
├── Shooter.csproj              # Projet C# (.NET 6.0, MonoGame 3.8)
├── Program.cs                  # Point d'entrée
├── README.md                   # Ce fichier
├── .gitignore
│
├── # --- Écrans ---
├── GameScreen.cs               # Classe de base pour tous les écrans
├── MenuScreen.cs               # Menu principal (Jouer, Réglages, Quitter)
├── GameplayScreen.cs           # Écran de gameplay (gestion principale)
├── SetupScreen.cs              # Menu de configuration (vidéo, audio, difficulté)
├── KeyBindingScreen.cs         # Remplacement des contrôles
│
├── # --- Entités ---
├── Sprite.cs                   # Classe de base 2D (position, texture, bounds)
├── Entite.cs                   # Entité de jeu avec PV, dégâts, score
├── Player.cs                   # Joueur (mouvement, 3 armes, bouclier, chaleur)
├── Enemy.cs                    # Ennemi (hérite de Creature)
├── Creature.cs                 # Entité animée (mouvement, tir)
├── Bullet.cs                   # Projectile (homing, destructible)
├── Bonus.cs                    # Bonus tombant
├── Shield.cs                   # Système de bouclier visuel
├── Explosion.cs                # Animation d'explosion
├── Particle.cs                 # Particules d'effet visuel
├── Star.cs                     # Étoile d'arrière-plan
├── DecorElement.cs             # Élément de décor statique/parallaxe
├── DecorManager.cs             # Gestionnaire de décor complet
├── Personnage.cs               # Classe intermédiaire personnage
│
├── # --- Services ---
├── ScreenManager.cs            # Gestionnaire d'écrans (pile, transitions)
├── InputService.cs             # Mappings d'entrées (clavier/manette)
├── InputManager.cs             # Lecture clavier + mode simulation
├── AudioService.cs             # Gestion musique et effets sonores
├── ResourceService.cs          # Chargement de ressources
├── EventService.cs             # Système d'événements
├── CameraService.cs            # Gestion caméra
├── GameConfig.cs               # Singleton de configuration XML
├── AppConfig.cs                # Configuration globale (XML)
│
├── # --- Contenu ---
├── Content/
│   ├── Content.mgcb            # Pipeline MonoGame (définit tous les assets)
│   ├── Roman15.spritefont      # Police de texte
│   │
│   ├── pics/                   # Sources images (250 fichiers .png/.jpg)
│   │   ├── ships/              # Vaisseaux joueur
│   │   ├── enemies/            # Ennemis de base
│   │   ├── bullets/            # Projectiles
│   │   ├── lasers/             # Lasers + sprites d'animation (01-66)
│   │   ├── shields/            # Boucliers + explosifs
│   │   ├── explosions/         # Frames d'explosion (8 par type × 8 types)
│   │   ├── space/              # Assets spatiaux complets
│   │   │   ├── ships/          # Vaisseaux ennemis
│   │   │   ├── turrets/        # Tourelles
│   │   │   ├── spikeballs/     # Boules épineuses
│   │   │   ├── rockwalls/      # Murs de roche (décor spatial)
│   │   │   ├── enemies/        # Ennemis spatiaux (EvilEye)
│   │   │   └── asteroids/      # Astéroïdes
│   │   ├── decors/             # Décor latéral (murs, coins, astéroïdes)
│   │   └── sprites/            # Sprites génériques
│   │
│   ├── sounds/                 # 8 fichiers audio .wav
│   │   ├── bgm_loop.wav        # Musique de fond
│   │   ├── bonus_pickup.wav    # Collecte de bonus
│   │   ├── explosion.wav       # Explosion
│   │   ├── laser_fire.wav      # Tir laser
│   │   ├── missile_launch.wav  # Lancement missile
│   │   ├── player_death.wav    # Mort du joueur
│   │   ├── shield_hit.wav      # Bouclier touché
│   │   └── wave_complete.wav   # Vague terminée
│   │
│   └── config/                 # Fichiers de configuration XML
│       ├── entities.xml
│       ├── enemies.xml
│       ├── bullets.xml
│       ├── bonuses.xml
│       ├── decors.xml
│       └── shooter-config.xsd
│
├── bin/                        # Généré par build (ignores Git)
├── obj/                        # Généré par build (ignores Git)
└── Config/                     # Config utilisateur (généré au premier lancement)
    └── game.xml
```

---

## Installation

### Prérequis

| Logiciel | Version minimale | Où le télécharger |
|---|---|---|
| .NET 6.0 SDK | 6.0.400+ | [dotnet.microsoft.com/download/dotnet/6.0](https://dotnet.microsoft.com/download/dotnet/6.0) |

> .NET 8.0+ fonctionne aussi, mais 6.0 est la version cible de ce projet.

---

### Sous Windows

1. **Installer .NET 6.0 SDK**
   - Télécharger le *Windows Installer (x64)* depuis le lien ci-dessus.
   - Exécuter `dotnet-sdk-6.0.x-win-x64.exe` et suivre l'assistant.
   - Ouvrir une nouvelle fenêtre d'Invite de commandes (CMD) ou PowerShell pour vérifier :
     ```cmd
     dotnet --version
     ```
     Doit afficher `6.x.x` ou supérieur.

2. **Cloner ou extraire le jeu**
   - Si vous clonez le dépôt :
     ```cmd
     git clone https://votre-url/mon-shooter.git
     cd mon-shooter
     ```
   - Si vous téléchargez une archive `.zip`, extraire le dossier `Shooter/` à l'endroit souhaité.

3. **Compiler le contenu et le code**
   - Ouvrir un terminal dans le dossier du projet :
     ```cmd
     cd Shooter
     dotnet build
     ```
   - Cette commande compile le code C#, construit les assets MonoGame (textures, sons, polices), et place le tout dans `bin/Release/net6.0/` (ou `bin/Debug/`).

4. **Lancer le jeu**
   ```cmd
   dotnet run --project Shooter.csproj
   ```

5. **Créer une distribution autonome (sans .NET installé)**
   ```cmd
   dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=false -p:TieredCompilation=false --self-contained
   ```
   - Les fichiers sont placés dans `bin/Release/net6.0/win-x64/publish/`.
   - Copier tout ce dossier sur la machine cible. Le jeu se lance en double-cliquant sur `Shooter.exe`.

---

### Sous Linux

1. **Installer .NET 6.0 SDK**

   **Debian / Ubuntu / Linux Mint :**
   ```bash
   sudo apt update
   sudo apt install -y apt-transport-https apt-utils
   sudo mkdir -pm 755 /etc/apt/keyrings
   wget -O- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /etc/apt/keyrings/packages.microsoft.gpg
   echo "deb [arch=amd64 sign=/etc/apt/keyrings/packages.microsoft.gpg] https://packages.microsoft.com/debian/12/prod stable main" | sudo tee /etc/apt/sources.list.d/microsoft.list > /dev/null
   sudo apt update
   sudo apt install -y dotnet-sdk-6.0
   ```

   **Fedora :**
   ```bash
   sudo dnf install dotnet-sdk-6.0
   ```

   **Arch Linux :**
   ```bash
   yay -S dotnet6-sdk
   ```

   Vérifier :
   ```bash
   dotnet --version
   ```

2. **Installer les dépendances système (bibliothèques graphiques)**
   ```bash
   # Debian / Ubuntu
   sudo apt install -y libsdl2-2.0-0 libasound2 libopenal1 libsndio7.0

   # Fedora
   sudo dnf install -y SDL2 alsa-lib libsndio-openal
   ```

3. **Compiler et lancer**
   ```bash
   cd Shooter
   dotnet build
   dotnet run --project Shooter.csproj
   ```

4. **Créer une distribution autonome**
   ```bash
   dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=false -p:TieredCompilation=false --self-contained
   ```
   - Les fichiers sont dans `bin/Release/net6.0/linux-x64/publish/`.
   - Distribuer le dossier complet.

   > Pour créer un **AppImage** : utiliser l'utilitaire [Publish-AppImage](https://github.com/kuiperzone/Publish-AppImage)
   > qui automatise le `dotnet publish` et l'emballage en `.AppImage`.

---

### Sous macOS

1. **Installer .NET 6.0 SDK**
   - Via Homebrew :
     ```bash
     brew install --cask dotnet-sdk
     ```
   - Ou depuis [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/6.0) (télécharger le `macOS ARM64 Installer` pour Apple Silicon ou `macOS x64 Installer` pour Intel).
   - Vérifier :
     ```bash
     dotnet --version
     ```

2. **Compiler et lancer**
   ```bash
   cd Shooter
   dotnet build
   dotnet run --project Shooter.csproj
   ```

3. **Créer une distribution autonome**

   **Pour Apple Silicon (M1/M2/M3) :**
   ```bash
   dotnet publish -c Release -r osx-arm64 -p:PublishReadyToRun=false -p:TieredCompilation=false --self-contained
   ```

   **Pour Intel (x64) :**
   ```bash
   dotnet publish -c Release -r osx-x64 -p:PublishReadyToRun=false -p:TieredCompilation=false --self-contained
   ```

   **Pour les deux (binaire universel) :**
   ```bash
   # Publier pour les deux architectures
   dotnet publish -c Release -r osx-arm64 -p:PublishReadyToRun=false -p:TieredCompilation=false --self-contained
   dotnet publish -c Release -r osx-x64 -p:PublishReadyToRun=false -p:TieredCompilation=false --self-contained

   # Fusionner les exécutables en un seul binaire universel
   lipo -create \
     bin/Release/net6.0/osx-arm64/publish/Shooter \
     bin/Release/net6.0/osx-x64/publish/Shooter \
     -output bin/Release/net6.0/universal/Shooter

   # Créer un .app bundle
   mkdir -p bin/Release/net6.0/Shooter.app/Contents/MacOS
   mkdir -p bin/Release/net6.0/Shooter.app/Contents/Resources/Content

   cp bin/Release/net6.0/universal/Shooter bin/Release/net6.0/Shooter.app/Contents/MacOS/
   cp -R bin/Release/net6.0/osx-arm64/publish/Content bin/Release/net6.0/Shooter.app/Contents/Resources/
   cp -R bin/Release/net6.0/osx-arm64/publish/runtimes bin/Release/net6.0/Shooter.app/Contents/Resources/

   cp bin/Release/net6.0/Shooter.app/Contents/MacOS/Shooter bin/Release/net6.0/Shooter.app/Contents/MacOS/MonogameGame
   chmod +x bin/Release/net6.0/Shooter.app/Contents/MacOS/Shooter
   chmod +x bin/Release/net6.0/Shooter.app/Contents/MacOS/MonogameGame
   ```

   > **Signature et notarization** (obligatoire pour les Mac récents si distribution publique) :
   > - Obtenir un *Apple Developer Program* membership.
   > - Signer : `codesign --force --deep --sign "Votre Certificate ID" Shooter.app`
   > - Notarizer : `xcrun notarytool submit Shooter.zip --wait --keychain-profile "MonProfil"`
   > - Stapler : `xcrun stapler staple Shooter.app`

---

## Installeur et distribution

MonoGame ne fournit pas d'outil d'installation intégré. La méthode recommandée par la
documentation officielle est un **archive (ZIP / tar.gz)** contenant tous les fichiers
d'un `dotnet publish --self-contained`. Voici les options disponibles selon la plateforme :

### Windows

| Méthode | Complexité | Résultat |
|---|---|---|
| **Archive ZIP** | Aucune | L'utilisateur extrait et double-clique sur `.exe`. |
| **NSIS (Nullsoft)** | Moyenne | Installateur avec wizard, raccourcis, désinstallateur. |
| **Inno Setup** | Moyenne | Similaire à NSIS, plus simple à apprendre. |
| **MSI / WiX** | Élevée | Installateur Windows officiel. Surdimensionné pour un petit jeu. |

**Recommandation** : une archive ZIP suffit pour un jeu de cette taille. Si vous souhaitez un installeur, **Inno Setup** est le compromis le plus simple (fichiers `.iss` declaratifs, prise en charge des raccourcis et de la désinstallation).

Exemple de script Inno Setup minimal :
```ini
[Setup]
AppName=Shooter
AppVersion=1.0
DefaultDirName={pf}\Shooter
OutputDir=.
OutputBaseFilename=Shooter-Setup

[Files]
Source: "bin\Release\net6.0\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{userdesktop}\Shooter"; Filename: "{app}\Shooter.exe"
Name: "{group}\Shooter"; Filename: "{app}\Shooter.exe"
```

### Linux

| Méthode | Complexité | Résultat |
|---|---|---|
| **Archive tar.gz** | Aucune | L'utilisateur extrait et lance le binaire. |
| **AppImage** | Faible-Moyenne | Binaire autonome avec icône de bureau, « copier-coller et lancer ». |
| **Flatpak** | Élevée | Plateforme de distribution standardisée (federation avec app stores). |
| **Paquet .deb / .rpm** | Moyenne | Installation via gestionnaire de paquets système. |

**Recommandation** : une archive `tar.gz` suffit pour les utilisateurs expérimentés. Pour le grand public, **AppImage** (via [Publish-AppImage](https://github.com/kuiperzone/Publish-AppImage)) est le plus simple : une commande génère un fichier `.AppImage` avec icône, lanceurs, etc.

### macOS

| Méthode | Complexité | Résultat |
|---|---|---|
| **Archive tar.gz** | Aucune | Binaires bruts, nécessite signature. |
| **Dossier .app** | Moyenne | Application native macOS avec icône et Info.plist. |
| **Disque .dmg** | Moyenne | Image disque avec raccourci vers /Applications. |
| **Notarization** | Élevée | Obligatoire pour distribution publique (Apple Developer ID requis). |

**Recommandation** : créer un `.app` bundle avec `lipo` (binaire universel), l'envoyer dans une image `.dmg`, et si distribution publique : le signer et le notarizer.

> **Note sur le PublishSingleFile** : bien qu'il soit tentant de regrouper tout en un seul exécutable
> (`PublishSingleFile=true`), la documentation MonoGame recommande explicitement de ne pas l'utiliser
> pour les jeux : cela augmente significativement le temps de démarrage et peut échouer sur des systèmes
> avec des permissions restreintes.

---

## Architecture logicielle

### Gestionnaire d'écrans (`ScreenManager`)

Le jeu utilise un système à **pile d'écrans** :

```
MenuScreen
    ├── GameplayScreen
    │   └── SetupScreen (push, depuis pause)
    └── KeyBindingScreen
```

Les transitions entre écrans utilisent un fondu noir animé (0,5 s par défaut).

### Configuration

- **`GameConfig`** : singleton chargé des fichiers `Content/config/*.xml`.
  Fournit des valeurs par défaut intégrées en cas de fichier manquant.
- **`AppConfig`** : singleton de la configuration utilisateur
  (`Config/game.xml`) — vidéo, audio, difficulté, contrôles.
- La sérialisation XML utilise un **namespace personnalisé**
  (`https://www.timc.fr/nicolas-glade/dotnet/shooter`).

### Système d'entrées

Deux couches d'abstraction :

1. **`InputService`** — service moderne avec mappage action-based
   (`MoveUp`, `FirePrimary`, etc.). Supporte clavier et manette.
2. **`InputManager`** — lecture directe du clavier avec modes
   `KeyPressed`, `KeyDown`, `KeyHold`, `KeyReleased`. Inclut un
   **mode simulation** pour l'exécution headless.

### Boucle de jeu (`GameplayScreen`)

Chaque frame :

1. **Mise à jour des entrées** — lecture clavier/manette
2. **Mise à jour du joueur** — position, armes, chaleur laser, bouclier
3. **Spawn d'ennemis** — intervalle décroissant avec les vagues
4. **Mise à jour des ennemis** — mouvement, tir, collision joueur
5. **Mise à jour des projectiles** — déplacement, homing, durée de vie
6. **Détection des collisions** — tirs vs ennemis, ennemis vs joueur
7. **Collecte de bonus** — effet immédiat sur le joueur
8. **Mise à jour des effets** — explosions, particules
9. **Rendu** — décor (parallaxe), entités, HUD (PV, bouclier, score, chaleur)

### Système de vague

La difficulté croît progressivement via un compteur de vague (`_wave`) :

- **Intervalle de spawn** : `baseInterval / spawnMod` (décroît avec les vagues)
- **Min spawn** : 2,0 s / spawnMod (plafond minimum)
- **Croissance de vague** : 0,05 × spawnMod par frame

---

## Développement

### Ajouter un ennemi

1. Ajouter une entrée dans `Content/config/enemies.xml` avec un nom unique,
   une texture, des statistiques et des motifs de mouvement/tir.
2. Ajouter un fichier image dans `pics/` (ou utiliser un sous-rectangle
   d'un sprite-sheet existant via `sourceRect`).
3. Ajouter une entrée dans `Content.mgcb` si la texture est nouvelle.
4. Reconstruire le contenu : `dotnet build`.

### Ajouter une arme

1. Déclarer le projectile dans `Content/config/bullets.xml`.
2. Implémenter la logique de tir dans `Player.cs` (cooldown, dégâts, spawn).
3. Ajouter le mapping d'entrée dans `AppConfig.CreateDefaultBindings()`.

### Modifier les paramètres par défaut

Les presets de difficulté sont définis dans `AppConfig.cs` (`_presets`).
Les valeurs par défaut des entités sont dans `GameConfig.DefaultEntities()`,
`GameConfig.DefaultEnemies()`, etc.
