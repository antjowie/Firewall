using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[UpdateBefore(typeof(MoveableSystem))]
public class PlayerMoveInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref MoveableState move) =>
            {
                move.move.x = Input.GetAxisRaw("Horizontal");
                move.move.y = Input.GetAxisRaw("Vertical");
            })
            .WithoutBurst()
            .Run(); // I'm not sure if Unity's input system is thread safe
    }
}