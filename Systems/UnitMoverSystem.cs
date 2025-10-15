using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct UnitMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //In order to cycle through the query we require a foreach because it returns a IEnumerable
        //Actual type is never written inside the query because while working with structs you deal with copies so doing it like this
        //wouldn't work and our transforms would never change LocalTransform localTransform in SystemAPI.Query<LocalTransform>()
        //Instead we have a helper class that transforms the struct to a reference type so that we access memory directly
        foreach ((
                     RefRW<LocalTransform> localTransform,
                     RefRO<UnitMover> unitMover,
                     RefRW<PhysicsVelocity> physicsVelocity) 
                 in SystemAPI.Query<
                     RefRW<LocalTransform>,
                     RefRO<UnitMover>,
                     RefRW<PhysicsVelocity>>())
        {
            //SystemAPI.Time.DeltaTime is essentially the same with UnityEngine Time.DeltaTime but is specifically designed for DOTS.
            
            float3 moveDirection = unitMover.ValueRO.targetPosition - localTransform.ValueRO.Position;
            moveDirection = math.normalize(moveDirection);
            //math.up() function returns float3(0, 1, 0) but its a constant and a safer option
            
            //Smooth rotation change for units
            localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRO.Rotation,
                                                quaternion.LookRotation(moveDirection, math.up()),
                                                SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);
            //localTransform.ValueRW.Rotation = quaternion.LookRotation(moveDirection, math.up());
            
            physicsVelocity.ValueRW.Linear = moveDirection * unitMover.ValueRO.moveSpeed; //Because we are setting velocity directly we don't need to apply delta time because that will be handled by physics system itself.
            physicsVelocity.ValueRW.Angular = float3.zero; //There can still be collisions that could move our units rotation so we take a measure for that.
            //localTransform.ValueRW.Position += moveDirection * moveSpeed.ValueRO.value * SystemAPI.Time.DeltaTime;
        }
    }
}
