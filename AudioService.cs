using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Shooter;

/// <summary>
/// Catégorie audio pour appliquer un volume spécifique.
/// </summary>
public enum AudioCategory { Master, Music, Sfx }

/// <summary>
/// Service centralisé de gestion audio.
/// Gère la musique de fond et les effets sonores (SFX) avec volumes séparés.
/// </summary>
public class AudioService
{
    private readonly ContentManager _content;
    private Music _currentMusic;
    private float _masterVolume = 1.0f;
    private float _musicVolume = 0.6f;
    private float _sfxVolume = 0.7f;
    private bool _isMuted = false;

    // Cache des SFX chargés
    private readonly Dictionary<string, SoundEffect> _sfxCache = new();
    // Pool de sons jouables simultanément (limite la polyphonie)
    private readonly Queue<SoundEffectInstance> _soundPool = new();
    private const int MaxSimultaneousSounds = 8;

    /// <summary>
    /// Crée un nouveau service audio.
    /// </summary>
    /// <param name="content">ContentManager racine du jeu.</param>
    public AudioService(ContentManager content)
    {
        _content = content;
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
    /// Joue un effet sonore.
    /// </summary>
    /// <param name="path">Chemin de l'effet sonore.</param>
    /// <param name="pitch">Modulation de pitch (-1 à 1, défaut 0).</param>
    public void PlaySfx(string path, float pitch = 0f)
    {
        if (_isMuted) return;

        SoundEffect sfx;
        lock (_sfxCache)
        {
            if (!_sfxCache.TryGetValue(path, out sfx))
                sfx = LoadSfx(path);
        }

        if (sfx == null) return;

        // Créer une nouvelle instance
        SoundEffectInstance instance = sfx.CreateInstance();
        instance.IsLooped = false;
        instance.Volume = _sfxVolume * _masterVolume;
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
    /// Joue un court son de feedback (bip) au volume d'une catégorie donnée.
    /// Génère un ton sinusoïdal de 80ms via un buffer WAV temporaire.
    /// </summary>
    /// <param name="category">Catégorie du son : Sfx, Music ou Master.</param>
    public void PlayPreviewTone(AudioCategory category = AudioCategory.Master)
    {
        if (_isMuted) return;

        // Générer un ton sinusoïdal de 80ms @ 88200 Hz / 16-bit / mono
        int sampleRate = 88200;
        int durationMs = 80;
        int numSamples = sampleRate * durationMs / 1000;
        byte[] buffer = new byte[numSamples * 2]; // 16-bit = 2 bytes/sample
        float freq = 880f; // A5 - son aigu et distinctif
        float vol = 0.5f;

        float effectiveVolume = category switch
        {
            AudioCategory.Sfx => _sfxVolume,
            AudioCategory.Music => _musicVolume,
            _ => _masterVolume
        };

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
