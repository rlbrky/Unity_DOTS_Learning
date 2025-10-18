using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class RandomWalkingAuthoring : MonoBehaviour
{
    public float3 targetPosition;
    public float3 originPosition;
    public float distanceMin;
    public float distanceMax;
    
    private class RandomWalkingBaker : Baker<RandomWalkingAuthoring>
    {
        public override void Bake(RandomWalkingAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RandomWalking
            {
                targetPosition = authoring.targetPosition,
                originPosition = authoring.originPosition,
                distanceMax = authoring.distanceMax,
                distanceMin = authoring.distanceMin,
                random = new Unity.Mathematics.Random(1),
            });
        }
    }
}

public struct RandomWalking : IComponentData
{
    public float3 targetPosition;
    public float3 originPosition;
    public float distanceMin;
    public float distanceMax;
    public Unity.Mathematics.Random random;
}