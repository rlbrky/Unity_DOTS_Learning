using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }
    
    public event EventHandler OnSelectionAreaStart;
    public event EventHandler OnSelectionAreaEnd;
    
    private Vector2 selectionStartMousePosition;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectionStartMousePosition = Input.mousePosition;
            OnSelectionAreaStart?.Invoke(this, EventArgs.Empty);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 selectionEndMousePosition = Input.mousePosition;
            
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Find and deselect every selected unit
            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<Selected>().Build(entityManager);
            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Selected> selectedArray = entityQuery.ToComponentDataArray<Selected>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                entityManager.SetComponentEnabled<Selected>(entities[i], false);
                Selected selected = selectedArray[i];
                selected.onDeselected = true;
                entityManager.SetComponentData(entities[i], selected); // If you were to copy the array after setting them false here in this for loop than your next query won't be able to find them because of withall<selected> is there. This is a tricky error so be careful.
            }
            
            Rect selectionAreaRect = GetSelectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width + selectionAreaRect.height;
            float multipleSelectionSizeMin = 40f;
            bool isMultipleSelection = selectionAreaSize > multipleSelectionSizeMin;

            if (isMultipleSelection)
            {
                // Find friendly units
                entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, Unit>().WithPresent<Selected>().Build(entityManager);

            
                // Find and select which units are inside the selection area player specified with mouse
                entities = entityQuery.ToEntityArray(Allocator.Temp);
                NativeArray<LocalTransform> localTransformArray = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            
                for (int i = 0; i < localTransformArray.Length; i++)
                {
                    LocalTransform unitLocalTransform = localTransformArray[i];
                    Vector2 unitScreenPosition = Camera.main.WorldToScreenPoint(unitLocalTransform.Position);
                    if (selectionAreaRect.Contains(unitScreenPosition))
                    {
                        // Unit is inside the selection area
                        entityManager.SetComponentEnabled<Selected>(entities[i], true);
                        Selected selected = entityManager.GetComponentData<Selected>(entities[i]);
                        selected.onSelected = true;
                        entityManager.SetComponentData(entities[i], selected);
                    }
                }
            }
            else
            {
                // Single selection
                entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton)); // Alternative way of creating an entity query
                PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();
                CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
                
                UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                
                RaycastInput raycastInput = new RaycastInput
                {
                    Start = cameraRay.GetPoint(0f),
                    End = cameraRay.GetPoint(9999f),
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u, // Belongs to every layer, ~ is used to bit shift everything.
                        CollidesWith = 1u << GameAssets.UNITS_LAYER, // We start with 1 and carry it to the units layer by bit shifting.
                        GroupIndex = 0,
                    }
                };
                
                if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit hit))
                {
                    if (entityManager.HasComponent<Unit>(hit.Entity) &&  entityManager.HasComponent<Selected>(hit.Entity))
                    {
                        //Hit a unit
                        entityManager.SetComponentEnabled<Selected>(hit.Entity, true);
                        Selected selected = entityManager.GetComponentData<Selected>(hit.Entity);
                        selected.onSelected = true;
                        entityManager.SetComponentData(hit.Entity, selected);
                    }
                }
            }
            
            OnSelectionAreaEnd?.Invoke(this, EventArgs.Empty);
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();

            //Allocators are insanely important, you should be careful when using them.
            //Creating a persistent allocator requires you to destroy it so be careful when using that or you will cause memory leaks.
            //Temp allocator is disposed automatically at the end of frame.
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover, Selected>().Build(entityManager);
            
            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);
            NativeArray<UnitMover> unitMoverArray = entityQuery.ToComponentDataArray<UnitMover>(Allocator.Temp);
            
            // Get our specified positions and put our units in a formation
            NativeArray<float3> movePositionArray =
                GenerateMovePositionArrayOfTypeCircle(mouseWorldPosition, entities.Length);
            
            for (int i = 0; i < unitMoverArray.Length; i++)
            {
                UnitMover unitMover = unitMoverArray[i];
                unitMover.targetPosition = movePositionArray[i]; // Place the unit to the specific position that is set to them.
                // The above code works on the copy we made because we are working with structs so in order to actually change the data inside our component we need to find and change it like this.
                //entityManager.SetComponentData(entities[i], unitMover);
                //Another way to achieve the component data save would be
                unitMoverArray[i] = unitMover;
            }
            entityQuery.CopyFromComponentDataArray(unitMoverArray); //This will update all the data that matches the entity query we wrote above.
        }
    }

    public Rect GetSelectionAreaRect()
    {
        Vector2 selectionEndMousePosition = Input.mousePosition;
        
        Vector2 lowerLeftCorner = new Vector2(
            Mathf.Min(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Min(selectionStartMousePosition.y, selectionEndMousePosition.y)
            );
        Vector2 upperRightCorner = new Vector2(
            Mathf.Max(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Max(selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        return new Rect(
            lowerLeftCorner.x,
            lowerLeftCorner.y,
            upperRightCorner.x - lowerLeftCorner.x,
            upperRightCorner.y - lowerLeftCorner.y);
    }

    private NativeArray<float3> GenerateMovePositionArrayOfTypeCircle(float3 targetPosition, int positionCount)
    {
        NativeArray<float3> positionArray = new NativeArray<float3>(positionCount, Allocator.Temp);
        if (positionCount == 0)
        {
            return positionArray;
        }

        positionArray[0] = targetPosition;
        if (positionCount == 1)
        {
            return positionArray;
        }

        float ringSize = 2.2f;
        int ring = 0;
        int positionIndex = 1;

        // This loop will fill position array by creating rings after rings and getting positions around the ring, so first ring will have 3 positions, second one 5 and it goes like that.
        while (positionIndex < positionCount)
        {
            int ringPositionCount = 3 + ring * 2;
            for (int i = 0; i < ringPositionCount; i++)
            {
                float angle = i * (math.PI2 / ringPositionCount);
                // Get first vector that is to the right of the ring and then rotate it for other positions. This is going to give us positions around a ring and as the ring expands and gets bigger we will have more positions.
                float3 ringVector = math.rotate(quaternion.RotateY(angle), new float3(ringSize * (ring + 1), 0, 0));
                float3 ringPosition = targetPosition + ringVector;
                
                positionArray[positionIndex] = ringPosition;
                positionIndex++;

                if (positionIndex >= positionCount)
                    break;
            }
            ring++;
        }
        
        return positionArray;
    }
}
