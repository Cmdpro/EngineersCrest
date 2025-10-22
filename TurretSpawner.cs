using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EngineersCrest
{
    public class TurretSpawner : MonoBehaviour
    {
        ToolItem? tool;
        bool right;
        public void Awake()
        {
            if (!HeroController.instance)
            {
                return;
            }
            tool = Patches.GetWillThrowTool();
            right = HeroController.instance.cState.facingRight;
        }
        public void Start()
        {
            ToolItem? willThrowTool = tool;
            if (willThrowTool != null)
            {
                Turret? turret = Turret.GetTurretForTool(willThrowTool);
                Vector2 velocity = new UnityEngine.Vector2(UnityEngine.Random.Range(6f, 12f), 10f);
                Vector3 position = transform.position + new Vector3(0, 1f, 0f);
                if (!right)
                {
                    velocity.x *= -1;
                }
                if (turret != null)
                {
                    turret.transform.position = position;
                    turret.GetComponent<Rigidbody2D>().linearVelocity = velocity;
                }
                else
                {
                    turret = EngineersCrestPlugin.CreateTurret(position, velocity, willThrowTool);
                }
                turret.facingRight = right;
            }
            GameObject.Destroy(this.gameObject);
        }
    }
}
