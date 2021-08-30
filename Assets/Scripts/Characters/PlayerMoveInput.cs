using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class PlayerMoveInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref MoveAbilityData move, ref Rotation rot, ref Translation pos) =>
            {
                var inverseRot = math.inverse(rot.Value);

                var desMove = new float3(
                        Input.GetAxisRaw("Horizontal"),
                        0,
                        Input.GetAxisRaw("Vertical")
                    );

                //move.move.x = Input.GetAxisRaw("Horizontal");
                //move.move.y = Input.GetAxisRaw("Vertical");

                //Debug.DrawRay(pos.Value, desMove, Color.red, Time.DeltaTime);
                desMove = math.rotate(inverseRot, desMove);
                //Debug.DrawRay(pos.Value, desMove, Color.white, Time.DeltaTime);

                move.move.x = desMove.x;
                move.move.y = desMove.y;

                move.move.x = Input.GetAxisRaw("Horizontal");
                move.move.y = Input.GetAxisRaw("Vertical");
            })
            .WithoutBurst()
            .Run(); // I'm not sure if Unity's input system is thread safe
    }
}