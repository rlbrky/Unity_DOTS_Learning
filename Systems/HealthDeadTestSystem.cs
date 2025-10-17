using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthDeadTestSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        //EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
        
        foreach ((
                     RefRO<Health> health,
                     Entity entity)
                 in SystemAPI.Query<
                     RefRO<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.healthAmount <= 0)
            {
                // Entity died
                entityCommandBuffer.DestroyEntity(entity); // Queues the command for destroying the entity.
            }
        }
        
        //entityCommandBuffer.Playback(state.EntityManager); you don't need to manually call playback if you use the preset singleton entity buffers
    }
}
