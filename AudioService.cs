using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter;

// ---------------------------------------------------------------
// Sound definitions (serializable from sounds.xml)
// ---------------------------------------------------------------

/// <summary>
/// Une entrée individuelle de la configuration sonore.
/// </summary>
[XmlRoot("sound", Namespace = "https://www.timc.fr/nicolas-glade/dotnet/shooter", IsNullable = false)]
public class SoundEntry
{
    /// <summary>
    /// Nom unique de référence (utilisé dans le code pour jouer le son).
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Nom du fichier .wav (sans le chemin).
    /// </summary>
    [XmlAttribute("file")]
    public string File { get; set; } = "";

    /// <summary>
    /// Volume spécifique (0.0 à 1.0).
    /// </summary>
    [XmlAttribute("volume")]
    public float Volume { get; set; } = 0.5f;

    /// <summary>
    /// Catégorie audio (Sfx ou Music).
    /// </summary>
    [XmlAttribute("category")]
    public string Category { get; set; } = "Sfx";
}

/// <summary>
/// Conteneur de la configuration sonore (racine XML : <sounds>).
/// </summary>
[XmlRoot("sounds", Namespace = "https://www.timc.fr/nicolas-glade/dotnet/shooter", IsNullable = false)]
public class SoundsConfig
{
    [XmlElement("sound")]
    public List<SoundEntry> Sounds { get; set; } = new();

    public static implicit operator SoundsConfig(List<SoundEntry> s) => new() { Sounds = s };
}

/// <summary>
/// Service centralisé de gestion audio.
/// Gère la musique de fond et les effets sonores (SFX) avec volumes séparés.
/// Charge les volumes depuis sounds.xml si disponible.
/// </summary>
public class AudioService
{
    private readonly ContentManager _content;
    private readonly Dictionary<string, SoundEntry> _soundMap = new();
    private Music _currentMusic;
    private float _masterVolume = 1.0f;
    private float _musicVolume = 0.6f;
    private float _sfxVolume = 0.7f;
    private bool _isMuted = false;

    // Cache des SFX chargés
    private readonly Dictionary<string, SoundEffect> _sfxCache = new();
    // Pool de sons jouables simultanément (limite la polyphonie)
    private readonly Queue<SoundEffectInstance> _soundPool = new();
    private const int MaxSimultaneousSounds = 16;

    /// <summary>
    /// Crée un nouveau service audio.
    /// </summary>
    /// <param name="content">ContentManager racine du jeu.</param>
    public AudioService(ContentManager content)
    {
        _content = content;
    }

    /// <summary>
    /// Charge la carte des sons depuis GameConfig.Sounds.
    /// Doit être appelé après le chargement de la configuration.
    /// </summary>
    public void LoadSoundConfig()
    {
        var cfg = GameConfig.Instance;
        if (cfg.Sounds != null && cfg.Sounds.Sounds != null)
        {
            foreach (var entry in cfg.Sounds.Sounds)
            {
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    _soundMap[entry.Name] = entry;
                    Console.WriteLine($"  [AudioService] Son configuré: {entry.Name} (fichier={entry.File}, volume={entry.Volume}, category={entry.Category})");
                }
            }
            Console.WriteLine($"  [AudioService] {cfg.Sounds.Sounds.Count} sons chargés depuis sounds.xml");
        }
        else
        {
            Console.WriteLine("[AudioService] Aucun son configuré dans sounds.xml");
        }
    }

    /// <summary>
    /// Récupère le volume configuré pour un son donné.
    /// Retourne 0.5 par défaut si aucune configuration n'est trouvée.
    /// </summary>
    private float GetSoundVolume(string name)
    {
        if (_soundMap.TryGetValue(name, out var entry))
            return Math.Clamp(entry.Volume, 0f, 1f);
        return 0.5f; // volume par défaut
    }

    /// <summary>
    /// Récupère la catégorie configurée pour un son donné.
    /// </summary>
    private string GetSoundCategory(string name)
    {
        if (_soundMap.TryGetValue(name, out var entry))
            return entry.Category ?? "Sfx";
        return "Sfx";
    }

    /// <summary>
    /// Initialise le volume à partir de la configuration.
    /// </summary>
    public void ApplySettings()
    {
        _masterVolume = AppConfig.Audio.MasterVolume;
        _musicVolume = AppConfig.Audio.MusicVolume;
        _sfxVolume = AppConfig.Audio.SfxVolume;
        _isMuted = AppConfig.Audio.Muted;
        UpdateVolumes();
    }

    /// <summary>
    /// Charge un effet sonore en cache.
    /// </summary>
    /// <param name="path">Chemin relatif dans le répertoire Content.</param>
    /// <returns>L'effet sonore chargé.</returns>
    public SoundEffect LoadSfx(string path)
    {
        if (!_sfxCache.ContainsKey(path))
            _sfxCache[path] = _content.Load<SoundEffect>(path);
        return _sfxCache[path];
    }

    /// <summary>
    /// Joue un effet sonore avec la configuration du fichier sounds.xml.
    /// Le nom du son est utilisé pour lookup le volume et la catégorie dans _soundMap.
    /// </summary>
    /// <param name="name">Nom du son configuré dans sounds.xml.</param>
    /// <param name="pitch">Modulation de pitch (-1 à 1, défaut 0).</param>
    public void PlaySfx(string name, float pitch = 0f)
    {
        if (_isMuted) return;

        // Déterminer le chemin du fichier et le volume
        // MGCB compile les sons sans l'extension, donc on enlève l'extension du chemin
        string filePath;
        float volume;
        if (_soundMap.TryGetValue(name, out var entry))
        {
            // Enlever l'extension du nom de fichier pour la clé de contenu MGCB
            string fileName = System.IO.Path.GetFileNameWithoutExtension(entry.File);
            filePath = $"sounds/{fileName}";
            volume = entry.Volume;
        }
        else
        {
            // Fallback: utiliser le nom comme chemin directement
            filePath = System.IO.Path.GetFileNameWithoutExtension(name);
            volume = 0.5f; // volume par défaut
        }

        SoundEffect sfx;
        lock (_sfxCache)
        {
            if (!_sfxCache.TryGetValue(filePath, out sfx))
            {
                try
                {
                    sfx = LoadSfx(filePath);
                }
                catch
                {
                    Console.WriteLine($"[AudioService] Son non trouvé: {filePath}");
                    return;
                }
            }
        }

        if (sfx == null) return;

        // Créer une nouvelle instance
        SoundEffectInstance instance = sfx.CreateInstance();
        instance.IsLooped = false;
        
        // Déterminer la catégorie
        string category = "Sfx";
        if (_soundMap.TryGetValue(name, out var entry2))
            category = entry2.Category ?? "Sfx";

        // Calculer le volume final
        float finalVolume = volume;
        if (category == "Music")
            finalVolume *= _musicVolume;
        else // Sfx
            finalVolume *= _sfxVolume;
        
        finalVolume *= (_isMuted ? 0f : _masterVolume);

        if (finalVolume < 0.01f) return;

        instance.Volume = finalVolume;
        instance.Pitch = pitch;
        instance.Play();
    }

    /// <summary>
    /// Joue un effet sonore avec le volume spécifié (ignore les paramètres globaux).
    /// </summary>
    /// <param name="path">Chemin de l'effet sonore.</param>
    /// <param name="volume">Volume absolu (0 à 1).</param>
    /// <param name="pitch">Modulation de pitch.</param>
    public void PlaySfxAtVolume(string path, float volume, float pitch = 0f)
    {
        SoundEffect sfx;
        lock (_sfxCache)
        {
            if (!_sfxCache.TryGetValue(path, out sfx))
                sfx = LoadSfx(path);
        }
        if (sfx == null) return;

        float finalVolume = volume * _sfxVolume * (_isMuted ? 0f : _masterVolume);
        if (finalVolume < 0.01f) return;

        SoundEffectInstance instance = sfx.CreateInstance();
        instance.Volume = finalVolume;
        instance.Pitch = pitch;
        instance.Play();
    }

    /// <summary>
    /// Charge et lance la musique de fond.
    /// La musique se répète en boucle.
    /// </summary>
    /// <param name="path">Chemin du fichier musique dans Content.</param>
    public void PlayMusic(string path)
    {
        // Arrêter la musique en cours
        if (_currentMusic != null)
            _currentMusic.Stop();

        _currentMusic = new Music(_content, path);
        _currentMusic.Volume = _musicVolume * _masterVolume;
        _currentMusic.Play();
    }

    /// <summary>
    /// Arrête la musique en cours.
    /// </summary>
    public void StopMusic()
    {
        if (_currentMusic != null)
        {
            _currentMusic.Stop();
            _currentMusic = null;
        }
    }

    /// <summary>
    /// Met en sourdine ou remet le son.
    /// </summary>
    /// <param name="muted">Vrai pour couper le son.</param>
    public void SetMuted(bool muted)
    {
        _isMuted = muted;
        AppConfig.Audio.Muted = muted;
        AppConfig.Save();
        UpdateVolumes();
    }

    /// <summary>
    /// Modifie le volume master.
    /// </summary>
    /// <param name="volume">Volume (0.0 à 1.0).</param>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = MathHelper.Clamp(volume, 0f, 1f);
        AppConfig.Audio.MasterVolume = _masterVolume;
        AppConfig.Save();
        UpdateVolumes();
    }

    /// <summary>
    /// Modifie le volume de la musique.
    /// </summary>
    /// <param name="volume">Volume (0.0 à 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = MathHelper.Clamp(volume, 0f, 1f);
        AppConfig.Audio.MusicVolume = _musicVolume;
        AppConfig.Save();
        UpdateVolumes();
    }

    /// <summary>
    /// Modifie le volume des effets sonores.
    /// </summary>
    /// <param name="volume">Volume (0.0 à 1.0).</param>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = MathHelper.Clamp(volume, 0f, 1f);
        AppConfig.Audio.SfxVolume = _sfxVolume;
        AppConfig.Save();
    }

    /// <summary>
    /// Met à jour les volumes de tous les lecteurs.
    /// </summary>
    private void UpdateVolumes()
    {
        if (_currentMusic != null)
            _currentMusic.Volume = _musicVolume * (_isMuted ? 0f : _masterVolume);
    }

    /// <summary>
    /// Met à jour le service (nettoyage du pool).
    /// </summary>
    public void Update()
    {
        // Nettoyage discret du pool (les instances expirées ne sont pas remises automatiquement)
    }

    /// <summary>
    /// Joue un court son de feedback (bip) au volume master.
    /// Génère un ton sinusoïdal de 80ms via un buffer WAV temporaire.
    /// </summary>
    public void PlayPreviewTone()
    {
        if (_isMuted) return;

        // Générer un ton sinusoïdal de 80ms @ 88200 Hz / 16-bit / mono
        int sampleRate = 88200;
        int durationMs = 80;
        int numSamples = sampleRate * durationMs / 1000;
        byte[] buffer = new byte[numSamples * 2]; // 16-bit = 2 bytes/sample
        float freq = 880f; // A5 - son aigu et distinctif
        float vol = 0.5f;
        float effectiveVolume = _masterVolume;

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Enveloppe ADSR simple : attack 2ms, decay to sustain 40ms, hold 20ms, release 18ms
            float envelope = 1f;
            float attackTime = 0.002f;
            float holdTime = 0.06f;
            float releaseTime = 0.08f;
            if (t < attackTime)
                envelope = t / attackTime;
            else if (t < holdTime)
                envelope = 1f;
            else if (t < holdTime + releaseTime)
                envelope = 1f - (t - holdTime) / releaseTime;
            else
                envelope = 0f;

            float sample = (float)Math.Sin(2f * Math.PI * freq * t);
            short val = (short)(Math.Clamp(sample * envelope * vol * 32767f, -32768f, 32767f));
            buffer[i * 2] = (byte)(val & 0xFF);
            buffer[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
        }

        // Créer le WAV avec en-tête RIFF
        using (var ms = new System.IO.MemoryStream())
        {
            using (var bw = new System.IO.BinaryWriter(ms))
            {
                // RIFF header
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write((uint)(36 + buffer.Length));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                // fmt chunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write((uint)16);
                bw.Write((ushort)1); // PCM
                bw.Write((ushort)1); // mono
                bw.Write((uint)sampleRate);
                bw.Write((uint)(sampleRate * 2)); // byte rate
                bw.Write((ushort)2); // block align
                bw.Write((ushort)16); // bits per sample
                // data chunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write((uint)buffer.Length);
                bw.Write(buffer);
            }
            var sfx = SoundEffect.FromStream(new System.IO.MemoryStream(ms.ToArray()));
            SoundEffectInstance instance = sfx.CreateInstance();
            instance.IsLooped = false;
            instance.Volume = effectiveVolume;
            instance.Pitch = 0.3f;
            instance.Play();
        }
    }

    /// <summary>
    /// Réapplique les volumes courants (master, musique) sur la musique en cours de lecture.
    /// Nécessaire car le volume est appliqué à la création mais pas mis à jour en cours de lecture.
    /// </summary>
    public void ReapplyAllVolumes()
    {
        if (_currentMusic != null)
            _currentMusic.Volume = _musicVolume * (_isMuted ? 0f : _masterVolume);
    }
}

/// <summary>
/// Wrapper léger autour de SoundEffect pour la gestion de la musique en boucle.
/// Utilise SoundEffectInstance au lieu de Song/MediaPlayer car DesktopGL ne supporte
/// pas MediaPlayer avec les fichiers WAV compilés par MGCB.
/// </summary>
public class Music
{
    private SoundEffect _sfx;
    private SoundEffectInstance _instance;
    private float _volume;

    /// <summary>
    /// Volume de la musique (0.0 à 1.0).
    /// </summary>
    public float Volume 
    { 
        get => _volume;
        set 
        { 
            _volume = value;
            if (_instance != null)
                _instance.Volume = _volume;
        } 
    }

    /// <summary>
    /// Indique si la musique est en cours de lecture.
    /// </summary>
    public bool IsPlaying => _instance != null && _instance.State == SoundState.Playing;

    /// <summary>
    /// Crée un nouveau lecteur de musique.
    /// </summary>
    /// <param name="content">ContentManager.</param>
    /// <param name="path">Chemin du fichier son dans Content.</param>
    public Music(ContentManager content, string path)
    {
        _sfx = content.Load<SoundEffect>(path);
        _instance = _sfx.CreateInstance();
        _instance.IsLooped = true;
        _volume = 1.0f;
    }

    /// <summary>
    /// Lance la musique.
    /// </summary>
    public void Play()
    {
        if (_instance.State == SoundState.Playing)
            return;
        _instance.Play();
    }

    /// <summary>
    /// Arrête la musique.
    /// </summary>
    public void Stop()
    {
        if (_instance != null && _instance.State == SoundState.Playing)
            _instance.Stop();
    }

    /// <summary>
    /// Libère les ressources.
    /// </summary>
    public void Dispose()
    {
        Stop();
        _instance?.Dispose();
    }
}
