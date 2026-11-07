using System;
using System.Collections.Generic;

namespace Shooter;

/// <summary>
/// Types d'événements supportés par le système d'événements.
/// </summary>
public enum EventType
{
    /// <summary>Un ennemi a été détruit.</summary>
    EnemyDestroyed,
    /// <summary>Un bonus a été collecté par le joueur.</summary>
    BonusCollected,
    /// <summary>Le joueur a été touché.</summary>
    PlayerHit,
    /// <summary>Le joueur est mort (game over).</summary>
    PlayerDied,
    /// <summary>Un upgrade a été appliqué au joueur.</summary>
    PlayerUpgraded,
    /// <summary>Une nouvelle vague d'ennemis commence.</summary>
    WaveStarted,
    /// <summary>Le bouclier du joueur a été touché.</summary>
    ShieldHit,
    /// <summary>Un missile ennemi a été détruit.</summary>
    EnemyBulletDestroyed
}

/// <summary>
/// Conteneur d'événement portant des données associées.
/// </summary>
public class GameEvent
{
    /// <summary>Type de l'événement.</summary>
    public EventType Type { get; }

    /// <summary>Données associées à l'événement (clé-valeur).</summary>
    public Dictionary<string, object> Data { get; } = new();

    /// <summary>
    /// Crée un nouvel événement.
    /// </summary>
    /// <param name="type">Type de l'événement.</param>
    public GameEvent(EventType type)
    {
        Type = type;
    }

    /// <summary>
    /// Ajoute une donnée à l'événement.
    /// </summary>
    /// <param name="key">Clé de la donnée.</param>
    /// <param name="value">Valeur de la donnée.</param>
    public GameEvent AddData(string key, object value)
    {
        Data[key] = value;
        return this;
    }

    /// <summary>
    /// Récupère une donnée de l'événement.
    /// </summary>
    /// <param name="key">Clé de la donnée.</param>
    /// <typeparam name="T">Type attendu de la donnée.</typeparam>
    /// <returns>La donnée convertie, ou null si absente.</returns>
    public T GetData<T>(string key)
    {
        if (Data.TryGetValue(key, out var value) && value is T typed)
            return typed;
        return default;
    }
}

/// <summary>
/// Service de communication par événements (pub/sub).
/// Permet aux entités de communiquer sans couplage direct.
/// Ex: un ennemi mort publie un événement → le gameplay screen collecte un score,
/// et le bonus system drop un bonus.
/// </summary>
public class EventService
{
    private static EventService _instance;
    private readonly Dictionary<EventType, List<Action<GameEvent>>> _listeners = new();

    /// <summary>
    /// Accès singleton au service d'événements.
    /// </summary>
    public static EventService Instance
    {
        get
        {
            if (_instance == null)
                _instance = new EventService();
            return _instance;
        }
    }

    /// <summary>
    /// S'abonne à un type d'événement.
    /// </summary>
    /// <param name="type">Type d'événement à écouter.</param>
    /// <param name="handler">Délégué appelé quand l'événement est publié.</param>
    public void Subscribe(EventType type, Action<GameEvent> handler)
    {
        if (!_listeners.ContainsKey(type))
            _listeners[type] = new List<Action<GameEvent>>();
        _listeners[type].Add(handler);
    }

    /// <summary>
    /// Se désabonne d'un type d'événement.
    /// </summary>
    /// <param name="type">Type d'événement.</param>
    /// <param name="handler">Délégué à retirer.</param>
    public void Unsubscribe(EventType type, Action<GameEvent> handler)
    {
        if (_listeners.TryGetValue(type, out var handlers))
            handlers.Remove(handler);
    }

    /// <summary>
    /// Publie un événement. Tous les abonnés sont notifiés immédiatement.
    /// </summary>
    /// <param name="eventType">Type de l'événement.</param>
    /// <returns>L'événement publié (pour enchaîner AddData).</returns>
    public GameEvent Publish(EventType eventType)
    {
        var evt = new GameEvent(eventType);
        Publish(evt);
        return evt;
    }

    /// <summary>
    /// Publie un événement avec des données.
    /// </summary>
    /// <param name="evt">Événement à publier.</param>
    public void Publish(GameEvent evt)
    {
        if (_listeners.TryGetValue(evt.Type, out var handlers))
        {
            // Copie pour éviter les modifications concurrentes
            var snapshot = new List<Action<GameEvent>>(handlers);
            foreach (var handler in snapshot)
            {
                try
                {
                    handler(evt);
                }
                catch
                {
                    // Un handler ne doit jamais faire planter le système
                }
            }
        }
    }

    /// <summary>
    /// Publie un événement avec des données prédéfinies (ennemi détruit).
    /// </summary>
    /// <param name="scoreValue">Valeur de score rapportée.</param>
    /// <param name="enemyType">Type d'ennemi détruit.</param>
    public void PublishEnemyDestroyed(int scoreValue, string enemyType)
    {
        var evt = new GameEvent(EventType.EnemyDestroyed)
            .AddData("ScoreValue", scoreValue)
            .AddData("EnemyType", enemyType);
        Publish(evt);
    }

    /// <summary>
    /// Publie un événement avec des données prédéfinies (bonus collecté).
    /// </summary>
    /// <param name="bonusType">Type de bonus collecté.</param>
    public void PublishBonusCollected(string bonusType)
    {
        var evt = new GameEvent(EventType.BonusCollected)
            .AddData("BonusType", bonusType);
        Publish(evt);
    }

    /// <summary>
    /// Publie un événement avec des données prédéfinies (joueur touché).
    /// </summary>
    /// <param name="damage">Dégâts subis.</param>
    public void PublishPlayerHit(int damage)
    {
        var evt = new GameEvent(EventType.PlayerHit)
            .AddData("Damage", damage);
        Publish(evt);
    }

    /// <summary>
    /// Supprime tous les abonnés.
    /// À appeler lors de la décharge d'un écran.
    /// </summary>
    public void ClearAll()
    {
        _listeners.Clear();
    }
}
