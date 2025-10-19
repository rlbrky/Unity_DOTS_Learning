using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct RandomWalkingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
                     RefRW<RandomWalking> randomWalking,
                     RefRW<UnitMover> unitMover,
                     RefRO<LocalTransform> localTransform)   
                 in SystemAPI.Query<
                     RefRW<RandomWalking>,
                     RefRW<UnitMover>,
                     RefRO<LocalTransform>>())
        {
            // This distance check should be the same logic as in UnitMoverSystem one because if
            // it's bigger than the other distance then this will never get executed.
            if (math.distancesq(localTransform.ValueRO.Position, randomWalking.ValueRO.targetPosition) <
                UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ)
            {
                // Target distance reached
                Random random = randomWalking.ValueRO.random; 
                // If we were to create a random here with the same seed everytime than that would get us same results everytime and our unit won't move after first movement is complete.
                // So to prevent that we saved the random to the IComponentData struct and used it from there
                // Also if you use the same seed everytime than that means every one of our units will go to the same position so we need to randomize our seed.
                
                float3 randomDirection = new float3(random.NextFloat(-1f, +1f), 0, random.NextFloat(-1f, +1f));
                randomDirection = math.normalize(randomDirection);
                
                randomWalking.ValueRW.targetPosition = randomWalking.ValueRO.originPosition + randomDirection *
                    random.NextFloat(randomWalking.ValueRO.distanceMin, randomWalking.ValueRO.distanceMax);

                randomWalking.ValueRW.random = random; // We don't want to work with the copy so we need to rewrite the actual random with our used one.
            }
            else
            {
                // We are too far from the target, move closer
                unitMover.ValueRW.targetPosition = randomWalking.ValueRO.targetPosition;
            }
        }   
    }
}
