using UnityEngine;

public class Physic2DObject
{
    private Vector2 _position;
    private Vector2 _velocity;
    private Vector2 _accelaration;
    private float _radius;
    private float _bounciness;
    private Color _color;

    public Physic2DObject(Vector2 position, Vector2 velocity, Vector2 accelaration, float radius, float bounciness, Color particleColor)
    {
        _position = position;
        _velocity = velocity;
        _accelaration = accelaration;
        _radius = radius;
        _bounciness = bounciness;
        _color = particleColor;
    }

    public void UpdatePhysics(float deltaTime)
    {
        CalculateVelocity(deltaTime);
        CalculatePosition(deltaTime);
    }

    private void CalculatePosition(float deltaTime)
    {
        _position = _position + _velocity * deltaTime;
    }

    private void CalculateVelocity(float deltaTime)
    {
        _velocity = _velocity + _accelaration * deltaTime;
    }

    public void Reflect(Vector2 normal)
    {
        var dotValue = Vector2.Dot(_velocity, normal);

        if (dotValue > 0)
        {
            return;
        }

        _velocity = Vector2.Reflect(_velocity, normal) *   _bounciness;
    }

    public Vector2 GetPosition()
    {
        return _position;
    }

    public float GetRadius()
    {
        return _radius;
    }

    public Bound GetBounds()
    {
        var minX = _position.x - _radius;
        var maxX = _position.x + _radius;
        var minY = _position.y - _radius;
        var maxY = _position.y + _radius;
        return new Bound(minX, maxX, minY, maxY);
    }

    public void Draw()
    {
        Gizmos.color = _color;
        Gizmos.DrawSphere(_position, _radius);
    }
}