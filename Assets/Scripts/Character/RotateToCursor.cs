using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct RotateToCursorTag : IComponentData
{
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class RotateToCursorSystem : SystemBase
{
    protected override void OnUpdate()
    {
        /**
         * Calculate a position in the world to look at
         */
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = Physics.Raycast(ray, out RaycastHit hitInfo, Vector3.Magnitude(Camera.main.transform.position));
        float3 targetPos = hitInfo.point;
        if (!hit) targetPos = ray.origin + ray.direction * Vector3.Magnitude(Camera.main.transform.position);

        float dt = Time.DeltaTime;
        //Debug.DrawLine(ray.origin, targetPos, Color.red, dt);

        Entities
            .WithAll<RotateToCursorTag>()
            .ForEach((ref Rotation rot, in LocalToWorld ltw, in Parent parent) =>
            {
                var parentRot = EntityManager.GetComponentData<Rotation>(parent.Value).Value;
                parentRot = math.inverse(parentRot);

                // We calulate the direction to the target.
                // Since I'm rotating usting LookAt, I need to create a forward vector
                var up = math.normalize(ltw.Position);
                var toTarget = targetPos - ltw.Position;
                //Debug.DrawRay(ltw.Position, toTarget, Color.green, dt);

                // The forward vector must be projected on it's local xz plane.
                // To do this, I calculate the vertical component of toTarget 
                // by projecting it onto the up vector of the entity
                // I then add it to toTarget, resulting in a forward vector on the entities plane
                var verticalComponent = -up * math.dot(toTarget,up);
                //Debug.DrawRay(ltw.Position + toTarget, verticalComponent, Color.blue, dt);
                toTarget += verticalComponent;
                //Debug.DrawRay(ltw.Position, toTarget, Color.white, dt);

                rot.Value = quaternion.LookRotation(
                    math.normalize(toTarget),
                    up
                    );

                rot.Value = math.mul(parentRot, rot.Value);
            })
            .WithoutBurst()
            .Run();
    }
}