using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BulletMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach ((
                     RefRW<LocalTransform> localTransform,
                     RefRO<Bullet> bullet,
                     RefRO<Target> target,
                     Entity bulletEntity) 
                 in SystemAPI.Query<
                        RefRW<LocalTransform>,
                        RefRO<Bullet>,
                        RefRO<Target>>().WithEntityAccess())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                entityCommandBuffer.DestroyEntity(bulletEntity);
                continue;
            }
            
            // Calculate the direction bullet is going to move.
            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            // Get where to shoot the enemy
            ShootVictim targetShootVictim = SystemAPI.GetComponent<ShootVictim>(target.ValueRO.targetEntity);
            float3 targetPosition = targetLocalTransform.TransformPoint(targetShootVictim.hitLocalPosition);
            
            // There is a bug if the bullet is too fast and the destroy distance is too small our bullet can overshoot itself from the target and not get destroyed so to prevent that we make this calculation
            float distanceBeforeSq = math.distancesq(localTransform.ValueRO.Position, targetPosition);
            
            float3 movementDirection = targetPosition - localTransform.ValueRO.Position;
            movementDirection = math.normalize(movementDirection);

            localTransform.ValueRW.Position += movementDirection * bullet.ValueRO.speed * SystemAPI.Time.DeltaTime;

            float distanceAfterSq = math.distancesq(localTransform.ValueRO.Position, targetPosition);

            if (distanceAfterSq > distanceBeforeSq)
            {
                // We overshot the target
                localTransform.ValueRW.Position = targetPosition;
            }
            
            // Define a distance that is close enough to the target and when reached queue the bullet to be destroyed at the end of the frame
            float destroyDistanceSq = .2f;
            if (math.distancesq(localTransform.ValueRO.Position, targetPosition) < destroyDistanceSq)
            {
                // Bullet reached the target and now should damage it
                RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
                targetHealth.ValueRW.healthAmount -= bullet.ValueRO.damageAmount;
                targetHealth.ValueRW.onHealthChanged = true; // Fire off the health changed event, it gets reset automatically
                
                entityCommandBuffer.DestroyEntity(bulletEntity);
            }
        }
    }
}
