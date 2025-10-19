using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthbarSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Vector3 cameraForward = Vector3.zero;
        if (Camera.main != null)
        {
            cameraForward = Camera.main.transform.forward;
        }
        
        foreach((
                    RefRW<LocalTransform> localTransform, 
                    RefRO<HealthBar> healthBar) 
                in SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<HealthBar>>())
        {
            LocalTransform parentLocalTransform = SystemAPI.GetComponent<LocalTransform>(healthBar.ValueRO.healthEntity);
            // Make sure health bar faces camera if its visible
            if (localTransform.ValueRO.Scale == 1f)
            {
                // InverseTransformRotation is used to convert global rotation to local rotation which is exactly what we want in this situation
                localTransform.ValueRW.Rotation = parentLocalTransform.InverseTransformRotation(quaternion.LookRotation(cameraForward, math.up()));    
            }
            
            Health health = SystemAPI.GetComponent<Health>(healthBar.ValueRO.healthEntity);

            if (!health.onHealthChanged)
            {
                continue;
            }
            
            // This is to have a nice value for our visual
            float healthNormalized = (float)health.healthAmount / health.healthAmountMax;

            // Hide the visual of healthbar if the unit has full health
            if (healthNormalized == 1f)
            {
                localTransform.ValueRW.Scale = 0f;
            }
            else
            {
                localTransform.ValueRW.Scale = 1f; // Show the health bar if the units health dropped down
            }
            
            RefRW<PostTransformMatrix> barVisualPostTransformMatrix =
                SystemAPI.GetComponentRW<PostTransformMatrix>(healthBar.ValueRO.barVisualEntity);
            barVisualPostTransformMatrix.ValueRW.Value = float4x4.Scale(healthNormalized, 1, 1);

            // This one's scale is uniform which means the healthbar will scale on xyz everytime, which is not what we want. We want to be able to scale on x only.
            //RefRW<LocalTransform> barVisualLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(healthBar.ValueRO.barVisualEntity);
            //barVisualLocalTransform.ValueRW.Scale = healthNormalized;
        }
    }
}
