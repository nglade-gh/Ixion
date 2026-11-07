using System;
using Microsoft.Xna.Framework;

namespace Shooter;

/// <summary>
/// Configuration du shake de caméra.
/// </summary>
public struct CameraShake
{
    /// <summary>Amplitude du shake (intensité).</summary>
    public float Intensity;
    /// <summary>Durée restante du shake.</summary>
    public float Duration;
    /// <summary>Fréquence du shake.</summary>
    public float Frequency;
}

/// <summary>
/// Service de gestion de la caméra.
/// Offre le zoom, le shake et le suivi d'une entité.
/// Les écrans utilisent ce service pour obtenir la matrice de transformation.
/// </summary>
public class CameraService
{
    /// <summary>Position actuelle de la caméra (origine du viewport).</summary>
    public Vector2 Position { get; private set; }

    /// <summary>Facteur de zoom (1.0 = normal).</summary>
    public float Zoom { get; private set; } = 1.0f;

    /// <summary>Rotation de la caméra en radians (pour effets de distorsion).</summary>
    public float Rotation { get; private set; }

    /// <summary>Entité suivie par la caméra (null = pas de suivi).</summary>
    private Sprite _followTarget;

    /// <summary>Décalage du suivi par rapport à la cible.</summary>
    private Vector2 _followOffset;

    /// <summary>Zone de jeu (pour limiter le suivi).</summary>
    private Vector2 _playArea;

    /// <summary>Shake en cours.</summary>
    private CameraShake _shake;

    /// <summary>Dernière position de la caméra (avant le shake).</summary>
    private Vector2 _lastCleanPosition;

    /// <summary>Générateur aléatoire.</summary>
    private readonly Random _random = new();

    /// <summary>
    /// Crée un nouveau service de caméra.
    /// </summary>
    public CameraService()
    {
        Position = Vector2.Zero;
        Zoom = 1.0f;
        Rotation = 0f;
        _followTarget = null;
        _followOffset = Vector2.Zero;
        _playArea = Vector2.Zero;
        _shake = new CameraShake { Intensity = 0f, Duration = 0f };
    }

    /// <summary>
    /// Définit la zone de jeu (pour le clamp du suivi).
    /// </summary>
    /// <param name="width">Largeur de la zone.</param>
    /// <param name="height">Hauteur de la zone.</param>
    public void SetPlayArea(float width, float height)
    {
        _playArea = new Vector2(width, height);
    }

    /// <summary>
    /// Active le suivi d'une entité.
    /// </summary>
    /// <param name="target">Entité à suivre.</param>
    /// <param name="offset">Décalage par rapport au centre de l'entité.</param>
    public void Follow(Sprite target, Vector2? offset = null)
    {
        _followTarget = target;
        _followOffset = offset ?? Vector2.Zero;
    }

    /// <summary>
    /// Désactive le suivi.
    /// </summary>
    public void StopFollowing()
    {
        _followTarget = null;
    }

    /// <summary>
    /// Applique un effet de shake.
    /// </summary>
    /// <param name="intensity">Intensité du tremblement.</param>
    /// <param name="duration">Durée en secondes.</param>
    /// <param name="frequency">Fréquence du tremblement.</param>
    public void Shake(float intensity = 3f, float duration = 0.3f, float frequency = 10f)
    {
        _shake = new CameraShake
        {
            Intensity = intensity,
            Duration = duration,
            Frequency = frequency
        };
    }

    /// <summary>
    /// Met à jour la caméra. À appeler chaque frame avant le rendu.
    /// </summary>
    /// <param name="viewportWidth">Largeur du viewport.</param>
    /// <param name="viewportHeight">Hauteur du viewport.</param>
    /// <param name="deltaTime">Delta temps depuis la dernière frame.</param>
    public void Update(int viewportWidth, int viewportHeight, float deltaTime)
    {
        // Suivi de cible
        if (_followTarget != null && _playArea.LengthSquared() > 0)
        {
            float targetX = _followTarget.Position.X + _followTarget.Size.X / 2f + _followOffset.X;
            float targetY = _followTarget.Position.Y + _followTarget.Size.Y / 2f + _followOffset.Y;

            // Clamp dans la zone de jeu
            targetX = MathHelper.Clamp(targetX, 0f, _playArea.X);
            targetY = MathHelper.Clamp(targetY, 0f, _playArea.Y);

            Position = new Vector2(targetX, targetY);
            _lastCleanPosition = Position;
        }

        // Application du shake
        if (_shake.Duration > 0f)
        {
            _shake.Duration -= deltaTime;
            float x = (_random.NextSingle() * 2f - 1f) * _shake.Intensity;
            float y = (_random.NextSingle() * 2f - 1f) * _shake.Intensity;
            Position = _lastCleanPosition + new Vector2(x, y);
        }
        else
        {
            _shake.Intensity = 0f;
        }

        // Clamp final de la caméra
        float clampedX = MathHelper.Clamp(Position.X, 0f, Math.Max(0, _playArea.X - viewportWidth / (int)Zoom));
        float clampedY = MathHelper.Clamp(Position.Y, 0f, Math.Max(0, _playArea.Y - viewportHeight / (int)Zoom));
        Position = new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// Change le facteur de zoom.
    /// </summary>
    /// <param name="zoom">Nouveau zoom (0.5 = moitié, 2.0 = double).</param>
    public void SetZoom(float zoom)
    {
        Zoom = MathHelper.Clamp(zoom, 0.25f, 4.0f);
    }

    /// <summary>
    /// Change la rotation de la caméra.
    /// </summary>
    /// <param name="rotation">Rotation en radians.</param>
    public void SetRotation(float rotation)
    {
        Rotation = rotation;
    }

    /// <summary>
    /// Retourne la matrice de transformation de la caméra
    /// à passer à SpriteBatch.Begin().
    /// </summary>
    /// <returns>Matrice de transformation.</returns>
    public Matrix GetTransformMatrix()
    {
        return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0f))
             * Matrix.CreateRotationZ(Rotation)
             * Matrix.CreateScale(Zoom, Zoom, 1f)
             * Matrix.CreateTranslation(new Vector3(0.5f, 0.5f, 0f));
    }

    /// <summary>
    /// Convertit une position du monde en position écran.
    /// </summary>
    /// <param name="worldPosition">Position dans le monde.</param>
    /// <returns>Position dans le viewport.</returns>
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        Matrix inv = Matrix.Invert(GetTransformMatrix());
        return Vector2.Transform(worldPosition, inv);
    }

    /// <summary>
    /// Convertit une position écran en position monde.
    /// </summary>
    /// <param name="screenPosition">Position dans le viewport.</param>
    /// <returns>Position dans le monde.</returns>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        Matrix inv = Matrix.Invert(GetTransformMatrix());
        return Vector2.Transform(screenPosition, inv);
    }
}
