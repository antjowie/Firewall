using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

/**
 * We modify the move data structure of Moveable. This 
 * 
 */
[UpdateBefore(typeof(MoveableSystem))]
public class PlayerMoveInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref MoveableState input) =>
            {
                input.move.x = Input.GetAxisRaw("Horizontal");
                input.move.y = Input.GetAxisRaw("Vertical");
            })
            .WithoutBurst()
            .Run(); // I'm not sure if Unity's input system is thread safe
    }
}