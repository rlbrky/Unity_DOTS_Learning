using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct MeleeAttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        NativeList<RaycastHit> raycastHitList = new NativeList<RaycastHit>(Allocator.Temp);
        
        foreach ((
                     RefRO<LocalTransform> localTransform,
                     RefRW<MeleeAttack> meleeAttack,
                     RefRO<Target> target,
                     RefRW<UnitMover> unitMover)
                 in SystemAPI.Query<
                     RefRO<LocalTransform>,
                     RefRW<MeleeAttack>,
                     RefRO<Target>,
                     RefRW<UnitMover>>())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }
            
            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);
            
            float meleeAttackDistanceSq = 2f;
            bool isCloseEnoughToAttack = math.distancesq(localTransform.ValueRO.Position, targetLocalTransform.Position) < meleeAttackDistanceSq;
            
            bool isTouchingTarget = false;
            if (!isCloseEnoughToAttack)
            {
                float3 dirToTarget = targetLocalTransform.Position - localTransform.ValueRO.Position;
                dirToTarget = math.normalize(dirToTarget);
                float raycastOffsetForColliderSize = .4f;
                
                RaycastInput raycastInput = new RaycastInput
                {
                    Start = localTransform.ValueRO.Position,
                    End = localTransform.ValueRO.Position + dirToTarget * (meleeAttack.ValueRO.colliderSize + raycastOffsetForColliderSize),
                    Filter = CollisionFilter.Default, // Raycast belongs and hits to all layers.
                };
                // Check if melee attacking units are trying to hit something too big
                raycastHitList.Clear();
                if(collisionWorld.CastRay(raycastInput, ref raycastHitList))
                {
                    foreach (RaycastHit raycastHit in raycastHitList)
                    {
                        if (raycastHit.Entity == target.ValueRO.targetEntity)
                        {
                            // Raycast successfully hit our target so unit is actually close enough to hit
                            isTouchingTarget = true;
                            break;
                        }
                    }
                }
            }
            
            if (!isCloseEnoughToAttack && !isTouchingTarget)
            {
                // Target is too far to attack
                unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
            }
            else
            {
                // Target is in range
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position; // Stop moving

                meleeAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                if (meleeAttack.ValueRO.timer > 0)
                {
                    continue;
                }
                meleeAttack.ValueRW.timer = meleeAttack.ValueRO.timerMax;
                
                RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
                targetHealth.ValueRW.healthAmount -= meleeAttack.ValueRO.damageAmount;
                targetHealth.ValueRW.onHealthChanged = true; // Fire off the health changed event, it gets reset automatically
            }
        }
    }
}
