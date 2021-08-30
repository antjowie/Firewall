using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/**
 * Takes care of moving among a sphere for us
 * The player modifies this component in its own system, and so do the enemies.
 * This makes it very easy to make an entity moveable (instead of implementing movement
 * calculation for each system)
 */

[GenerateAuthoringComponent]
public struct MoveAbilityData : IComponentData
{
    // The direction to move to along its local coordinate space
    public float2 move;
    public float degreesPerSecond;
}

[UpdateInGroup(typeof(TransformSystemGroup))]
public class MoveAbilitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = Time.DeltaTime;

        Entities
            .ForEach((ref Translation pos, ref Rotation rot, in MoveAbilityData move, in LocalToWorld ltw) =>
            {
                // This magic value is the radius of the sphere in the level
                var radius = 21f;

                /**
                 * Rotate the entity around its own center
                 * After that, since we only do movement on a sphere
                 * we just use the up vector to set the position
                 */
                {
                    var deltaRot2D = math.normalizesafe(move.move) * math.radians(move.degreesPerSecond) * dt;
                    var deltaRot = new float3(
                        deltaRot2D.y,
                        0,
                        -deltaRot2D.x
                        );
                    var newRot = math.mul(rot.Value, quaternion.Euler(deltaRot));
                    rot.Value = newRot;
                    pos.Value = math.rotate(rot.Value, math.up()) * radius;

                }
            })
            .ScheduleParallel();
    }
}
