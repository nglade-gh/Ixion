using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter;

/// <summary>
/// Service centralisé de gestion des ressources.
/// Met en cache les textures, polices et autres contenus chargés depuis le ContentManager.
/// Évite les rechargements multiples et offre un chargement prédictif par type d'écran.
/// </summary>
public class ResourceService
{
    private readonly ContentManager _content;
    private readonly Dictionary<string, object> _cache = new();

    /// <summary>
    /// Crée un nouveau service de ressources.
    /// </summary>
    /// <param name="content">ContentManager racine du jeu.</param>
    public ResourceService(ContentManager content)
    {
        _content = content;
    }

    /// <summary>
    /// Charge une ressource et la met en cache.
    /// Les ressources déjà chargées sont retournées directement depuis le cache.
    /// </summary>
    /// <typeparam name="T">Type de la ressource (Texture2D, SpriteFont, etc.).</typeparam>
    /// <param name="key">Clé unique de la ressource (chemin relatif au répertoire Content).</param>
    /// <returns>La ressource chargée.</returns>
    public T Load<T>(string key) where T : notnull
    {
        if (_cache.TryGetValue(key, out var cached))
            return (T)cached;

        var resource = _content.Load<T>(key);
        _cache[key] = resource;
        return resource;
    }

    /// <summary>
    /// Charge un tableau de textures depuis une liste de chemins.
    /// Chaque texture est mise en cache individuellement.
    /// </summary>
    /// <param name="keys">Chemins relatifs des textures à charger.</param>
    /// <returns>Tableau de textures chargées.</returns>
    public Texture2D[] LoadFrames(params string[] keys)
    {
        var result = new Texture2D[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            result[i] = Load<Texture2D>(keys[i]);
        return result;
    }

    /// <summary>
    /// Précharge un ensemble de ressources anticipées pour le prochain écran.
    /// </summary>
    /// <param name="keys">Clés des ressources à précharger.</param>
    public void Preload(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!_cache.ContainsKey(key))
            {
                try
                {
                    _cache[key] = _content.Load<Texture2D>(key);
                }
                catch
                {
                    // Ignore les erreurs de préchargement (ressource optionnelle)
                }
            }
        }
    }

    /// <summary>
    /// Vérifie si une ressource est déjà en cache.
    /// </summary>
    /// <param name="key">Clé de la ressource.</param>
    /// <returns>Vrai si la ressource est en cache.</returns>
    public bool IsCached(string key) => _cache.ContainsKey(key);

    /// <summary>
    /// Décharge toutes les ressources en cache.
    /// À appeler lors de la fermeture propre du jeu.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
}
