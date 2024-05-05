using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class My2DPhysics : MonoBehaviour
{
    [Header("Particle Settings")] public int startObjectCount;
    public float particleSpeed;
    public float particleMinSize;
    public float particleMaxSize;

    [Range(0, 1)] public float bounciness;
    public bool applyGravity;
    [Range(0, 20)] public float gravityAmount;
    public List<Color> particleColors;
    [Header("Collision Settings")] public CollisionDetectionAlgorithm collisionDetectionAlgorithm;
    [Header("Bound")] public float minXBound;
    public float maxXBound;
    public float minYBound;
    public float maxYBound;


    [Header("Debug Settings")] public bool showPossibleCollisionPairs;
    public Color showPossibleCollisionPairsColor;
    public bool showIntersactionBounds;
    public Color intersactionBoundsColor;


    [Header("UI")] public TMP_Text possibleCollisionCountText;
    public TMP_Text collisionCountText;
    public TMP_Text ballCountText;
    public TMP_Text gravityActiveText;
    public TMP_Text gravityAmountText;
    public TMP_Text bouncinessAmountText;
    public Slider gravitySlider;
    public Toggle gravityToggle;
    public Slider bouncinessSlider;

    private List<Physic2DObject> _physic2DObjects;
    private List<PossibleCollision> _possibleCollisions;
    private bool _isInitialize;
    private float _collisionCountResetTimer;
    private int _collisionCount;
    private float _spawnTimer;

    void Start()
    {
        CreateScene();
        gravitySlider.onValueChanged.AddListener((x) =>
        {
            gravityAmount = x;
            CreateScene();
        });
        gravityToggle.onValueChanged.AddListener((x) =>
        {
            applyGravity = x;
            CreateScene();
        });

        bouncinessSlider.onValueChanged.AddListener((x) =>
        {
            bounciness = x;
            CreateScene();
        });
    }

    void CreateScene()
    {
        _isInitialize = false;
        _physic2DObjects = new List<Physic2DObject>();

        for (int i = 0; i < startObjectCount; i++)
        {
            var randomPosition = new Vector2(Random.Range(minXBound, maxXBound), Random.Range(minYBound, maxYBound));
            CreateBall(randomPosition);
        }

        _isInitialize = true;
    }

    void CreateBall(Vector2 position)
    {
        var randomSpeed = new Vector2(Random.Range(-1f, 1f), Random.Range(-1, 1f)) * particleSpeed;
        var accelaration = applyGravity ? new Vector2(0, -gravityAmount) : Vector2.zero;
        var randomSize = Random.Range(particleMinSize, particleMaxSize);
        _physic2DObjects.Add(new Physic2DObject(position, randomSpeed, accelaration, randomSize, bounciness, particleColors[Random.Range(0, particleColors.Count)]));
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isInitialize)
        {
            return;
        }

        UpdateObjects();
        ChekcForWorldBoundCollision();
        CheckForCollision();
        UpdateUI();
        CheckInput();
    }

    private void CheckInput()
    {
        _spawnTimer += Time.deltaTime;

        if (Input.GetMouseButton(0) && _spawnTimer > 0.05f)
        {
            _spawnTimer = 0;
            Plane plane = new Plane(Vector3.back, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 worldPosition = ray.GetPoint(enter);
                worldPosition.z = 0;
                CreateBall(worldPosition);
            }
        }
    }

    private void UpdateUI()
    {
        _collisionCountResetTimer += Time.deltaTime;
        collisionCountText.text = "Collision count in second :" + _collisionCount;
        ballCountText.text = "Particle Count :" + _physic2DObjects.Count;

        if (_collisionCountResetTimer > 1)
        {
            _collisionCountResetTimer = 0;
            _collisionCount = 0;
        }

        gravityActiveText.text = "Gravity active :" + applyGravity.ToString();
        gravityAmountText.text = "Gravity force :" + gravityAmount.ToString("F1");
        bouncinessAmountText.text = "Bounciness :" + bounciness.ToString("F1");
    }

    private void UpdateObjects()
    {
        foreach (var physic2DObject in _physic2DObjects)
        {
            physic2DObject.UpdatePhysics(Time.deltaTime);
        }
    }

    private void CheckForCollision()
    {
        _possibleCollisions = BroadPhase();

        if (showPossibleCollisionPairs)
        {
            possibleCollisionCountText.text = "Possible Collision Count :" + _possibleCollisions.Count;
        }

        var collisionDatas = NarrowPhase(_possibleCollisions);
        _collisionCount += collisionDatas.Count;
        SolveCollisions(collisionDatas);
    }


    #region BroadPhase

    private List<PossibleCollision> BroadPhase()
    {
        List<PossibleCollision> possibleCollisions = new List<PossibleCollision>();
        switch (collisionDetectionAlgorithm)
        {
            case CollisionDetectionAlgorithm.None:
                break;
            case CollisionDetectionAlgorithm.CheckAllCombination:
                return CheckAllCombination();
                break;
            case CollisionDetectionAlgorithm.SweepAndPrune:
                return SweepAndPrune();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return possibleCollisions;
    }

    private List<PossibleCollision> CheckAllCombination()
    {
        var combinations = _physic2DObjects.SelectMany(x => _physic2DObjects, (x, y) => new PossibleCollision(x, y));
        return combinations.ToList();
    }

    private List<PossibleCollision> SweepAndPrune()
    {
        var possibleCollisions = new List<PossibleCollision>();
        var orderedPhysicsObjects = _physic2DObjects.OrderBy(x => x.GetBounds().minX).ToList();

        List<Physic2DObject> active = new List<Physic2DObject>(); // activeInterval

        active.Add(orderedPhysicsObjects.First());

        for (int i = 1; i < orderedPhysicsObjects.Count; i++) // CheckInterval intersect
        {
            var current = orderedPhysicsObjects[i];
            var currentBound = current.GetBounds();

            for (var index = active.Count - 1; index >= 0; index--)
            {
                var checkObject = active[index];
                var checkBound = checkObject.GetBounds();

                if (checkBound.maxX > currentBound.minX) // intersection 
                {
                    possibleCollisions.Add(new PossibleCollision(checkObject, current));
                }
                else
                {
                    active.Remove(checkObject);
                }
            }

            active.Add(current);
        }

        return possibleCollisions;
    }

    #endregion


    private List<CollisionData> NarrowPhase(List<PossibleCollision> possibleCollisions)
    {
        List<CollisionData> collisionDatas = new List<CollisionData>();

        foreach (var possibleCollision in possibleCollisions)
        {
            if (CheckPairCollision(possibleCollision, out CollisionData collisionData))
            {
                collisionDatas.Add(collisionData);
            }
        }

        return collisionDatas;
    }

    private void SolveCollisions(List<CollisionData> collisionDatas)
    {
        foreach (var collisionData in collisionDatas)
        {
            collisionData.object1.Reflect(collisionData.object1HitNormal);
            collisionData.object2.Reflect(collisionData.object2HitNormal);
        }
    }


    private bool CheckPairCollision(PossibleCollision possibleCollision, out CollisionData collisionData)
    {
        var obj1Position = possibleCollision.object1.GetPosition();
        var obj2Position = possibleCollision.object2.GetPosition();

        var distanceBetweenObjects = Vector2.Distance(obj1Position, obj2Position);
        var totalRadius = possibleCollision.object1.GetRadius() + possibleCollision.object2.GetRadius();
        if (distanceBetweenObjects <= totalRadius)
        {
            //There is a collision

            var obj1HitNormal = (obj1Position - obj2Position).normalized;
            var obj2HitNormal = (obj2Position - obj1Position).normalized;
            var hitPoint = obj1Position + obj1HitNormal * possibleCollision.object1.GetRadius();

            collisionData = new CollisionData(possibleCollision.object1, possibleCollision.object2, obj1HitNormal, obj2HitNormal, hitPoint);
            return true;
        }

        collisionData = null;
        return false;
    }

    private void ChekcForWorldBoundCollision()
    {
        foreach (var physic2DObject in _physic2DObjects)
        {
            var bounds = physic2DObject.GetBounds();

            if (bounds.minX < minXBound)
            {
                physic2DObject.Reflect(Vector2.right);
            }
            else if (bounds.maxX > maxXBound)
            {
                physic2DObject.Reflect(Vector2.left);
            }
            else if (bounds.minY < minYBound)
            {
                physic2DObject.Reflect(Vector2.up);
            }
            else if (bounds.maxY > maxYBound)
            {
                physic2DObject.Reflect(Vector2.down);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!_isInitialize)
        {
            return;
        }

        Gizmos.color = Color.white;

        foreach (var physic2DObject in _physic2DObjects)
        {
            physic2DObject.Draw();
        }

        //Draw bounds
        Gizmos.color = Color.red;
        var size = new Vector3(maxXBound - minXBound, maxYBound - minYBound, 0.1f);
        Gizmos.DrawWireCube(Vector3.zero, size);

        if (showPossibleCollisionPairs)
        {
            foreach (var possibleCollision in _possibleCollisions)
            {
                var position1 = possibleCollision.object1.GetPosition();
                var position2 = possibleCollision.object2.GetPosition();

                var bounds1 = possibleCollision.object1.GetBounds();
                var bounds2 = possibleCollision.object2.GetBounds();

                if (showIntersactionBounds)
                {
                    Gizmos.color = intersactionBoundsColor;
                    Gizmos.DrawLine(new Vector3(bounds1.minX, position1.y, 0), new Vector3(bounds1.minX, minYBound, 0));
                    Gizmos.DrawLine(new Vector3(bounds1.maxX, position1.y, 0), new Vector3(bounds1.maxX, minYBound, 0));


                    Gizmos.DrawLine(new Vector3(bounds2.minX, position2.y, 0), new Vector3(bounds2.minX, minYBound, 0));
                    Gizmos.DrawLine(new Vector3(bounds2.maxX, position2.y, 0), new Vector3(bounds2.maxX, minYBound, 0));
                }

                Gizmos.color = showPossibleCollisionPairsColor;
                Gizmos.DrawLine(position1, position2);
            }
        }

        Gizmos.color = Color.white;
    }
}