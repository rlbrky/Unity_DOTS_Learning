using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ShootAttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        
        foreach ((
                     RefRW<LocalTransform> localTransform,
                     RefRW<ShootAttack> shootAttack, 
                     RefRO<Target> target,
                     RefRW<UnitMover> unitMover) 
                 in SystemAPI.Query<
                     RefRW<LocalTransform>,
                     RefRW<ShootAttack>, 
                     RefRO<Target>,
                     RefRW<UnitMover>>().WithDisabled<MoveOverride>()) // This logic will only run if move override is disabled
                
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }
            
            // Ensure that unit moves in range before shooting
            LocalTransform targetTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);

            if (math.distance(localTransform.ValueRO.Position, targetTransform.Position) >
                shootAttack.ValueRO.attackDistance)
            {
                // Target is too far, move closer
                unitMover.ValueRW.targetPosition = targetTransform.Position;
                continue;
            }
            else
            {
                // Target is in range, stop moving and attack
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
            }
            
            // Make the unit face its target while shooting
            float3 aimDirection = targetTransform.Position - localTransform.ValueRO.Position;
            aimDirection =  math.normalize(aimDirection);
            
            quaternion targetRotation = quaternion.LookRotation(aimDirection, math.up());
            localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation, targetRotation, SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);
            
            shootAttack.ValueRW.timer -= SystemAPI.Time.DeltaTime;
            if (shootAttack.ValueRO.timer > 0f)
            {
                continue;
            }
            
            shootAttack.ValueRW.timer = shootAttack.ValueRO.timerMax;
            
            Entity bulletEntity = state.EntityManager.Instantiate(entitiesReferences.bulletPrefabEntity);
            // TransformPoint Converts the local position to global position
            float3 bulletSpawnWorldPosition = localTransform.ValueRO.TransformPoint(shootAttack.ValueRO.bulletSpawnLocalPosition);
            
            // Start the bullet from this entity's position with the offset we gave.
            SystemAPI.SetComponent(bulletEntity, LocalTransform.FromPosition(bulletSpawnWorldPosition)); 
            
            // Set bullets damage properly
            RefRW<Bullet> bulletBullet = SystemAPI.GetComponentRW<Bullet>(bulletEntity);
            bulletBullet.ValueRW.damageAmount = shootAttack.ValueRO.damageAmount;

            // Set bullets target from this units target
            RefRW<Target> bulletTarget = SystemAPI.GetComponentRW<Target>(bulletEntity);
            bulletTarget.ValueRW.targetEntity = target.ValueRO.targetEntity;

            shootAttack.ValueRW.onShoot.isTriggered = true; // Use an event to spawn our light for shooting
            shootAttack.ValueRW.onShoot.shootFromPosition = bulletSpawnWorldPosition;

            /*RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
            int damageAmount = 1;
            targetHealth.ValueRW.healthAmount -= damageAmount;*/
        }
    }
}
