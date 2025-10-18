using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct ZombieSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach((
                    RefRO<LocalTransform> localTransform,
                    RefRW<ZombieSpawner> zombieSpawner)
                in SystemAPI.Query<
                    RefRO<LocalTransform>,
                    RefRW<ZombieSpawner>>())
        {
            zombieSpawner.ValueRW.timer -= SystemAPI.Time.DeltaTime;
            // Skip if timer hasn't finished yet
            if (zombieSpawner.ValueRW.timer > 0f)
            {
                continue;
            }
            zombieSpawner.ValueRW.timer =  zombieSpawner.ValueRO.timerMax;

            // Instantiate and assign zombies position
            Entity zombieEntity = state.EntityManager.Instantiate(entitiesReferences.zombiePrefabEntity);
            SystemAPI.SetComponent(zombieEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));
         
            entityCommandBuffer.AddComponent(zombieEntity, new RandomWalking
            {
                originPosition = localTransform.ValueRO.Position, // Unit's spawn position is the origin
                targetPosition = localTransform.ValueRO.Position, // First walking point is its own start loc. so it will start moving next frame
                distanceMin = zombieSpawner.ValueRO.randomWalkingDistanceMin,
                distanceMax = zombieSpawner.ValueRO.randomWalkingDistanceMax,
                random = new Unity.Mathematics.Random((uint)zombieEntity.Index), // The entity index is unique for every entity so it makes a perfect candidate for our random seed.
            });
        }
    }
}
