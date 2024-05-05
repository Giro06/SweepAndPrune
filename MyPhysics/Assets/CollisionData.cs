using UnityEngine;

public class CollisionData
{
    public Physic2DObject object1;
    public Physic2DObject object2;

    public Vector2 object1HitNormal;
    public Vector2 object2HitNormal;
    public Vector2 hitPoint;

    public CollisionData(Physic2DObject obj1, Physic2DObject obj2, Vector2 obj1HitNormal, Vector2 obj2HitNormal, Vector2 hitPoint)
    {
        object1 = obj1;
        object2 = obj2;
        object1HitNormal = obj1HitNormal;
        object2HitNormal = obj2HitNormal;
        this.hitPoint = hitPoint;
    }
}