using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[UpdateBefore(typeof(FireProjectile))]
public class PlayerFireInput : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref FireProjectileData fire) =>
            {
                fire.isFiring = Input.GetAxisRaw("Fire1") > 0.1f;
            })
            .WithoutBurst()
            .Run(); // I'm not sure if Unity's input system is thread safe
    }
}