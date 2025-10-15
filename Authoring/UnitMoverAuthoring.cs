using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

//You can have one authoring component or have separate authoring scripts like this.
public class UnitMoverAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public float rotationSpeed;

    //The Baker class can be named whatever you want and doesn't have to be a nested class.
    //It does have to have the authoring class as its generic though not the IComponent one.
    public class Baker : Baker<UnitMoverAuthoring>
    {
        //With this default function that baker class gives us we can create and fill our entity with ease.
        public override void Bake(UnitMoverAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitMover
            {
                moveSpeed = authoring.moveSpeed,
                rotationSpeed = authoring.rotationSpeed,
            });
        }
    }
}

//This below is the DOTS component, in order to not have hundreds of scripts like this
//having just one script that both has the authoring and the DOTS components is much better.

public struct UnitMover : IComponentData
{
    public float moveSpeed;
    public float rotationSpeed;
    public float3 targetPosition;
}
