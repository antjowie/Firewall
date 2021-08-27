using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[GenerateAuthoringComponent]
public struct CameraTag: IComponentData
{
}

[UpdateAfter(typeof(MoveableSystem))]
public class CameraSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Get the player
        var copyPos = new NativeArray<float3>(1, Allocator.TempJob);
        var fillCopyPos = Entities
            .ForEach((in PlayerTag tag, in Translation pos) =>
            {
                copyPos[0] = pos.Value;
            })
            .ScheduleParallel(Dependency);

        Dependency = Entities
            .ForEach((ref Translation pos, ref Rotation rot, in CameraTag camera, in LocalToWorld ltw) =>
            {
                pos.Value = copyPos[0] * 2f;

                var fwd = -math.normalize(copyPos[0]);
                
                var up = math.cross(fwd, math.normalize(ltw.Right));
                //Debug.DrawRay(pos.Value, ltw.Right);
                //Debug.DrawRay(pos.Value, fwd);
                rot.Value = quaternion.LookRotation(-math.normalize(copyPos[0]), up);
            })
            .WithDisposeOnCompletion(copyPos)
            .WithoutBurst()
            .ScheduleParallel(fillCopyPos);
    }
}