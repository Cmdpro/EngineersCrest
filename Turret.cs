using GlobalEnums;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EngineersCrest
{
    public class Turret : MonoBehaviour
    {
        public static List<Turret> turrets = new List<Turret>();
        public ToolItem tool;
        public bool facingRight;
        public float cooldown;
        public int shot;
        public int health = 8;
        public float invincibility;
        public Rigidbody2D rb;
        public AudioSource hitSound;
        public int GetMaxShots()
        {
            if (ToolItemManager.GetToolByName("Quick Sling").IsEquipped)
            {
                return 2;
            }
            return 1;
        }
        public Turret SetTool(ToolItem tool)
        {
            this.tool = tool;
            return this;
        }
        public void Die()
        {
            hitSound.transform.parent = transform.parent;
            hitSound.gameObject.AddComponent<OneTimeAudio>();
            Destroy(this.gameObject);
        }
        public Turret Hurt(int damage)
        {
            if (invincibility > 0)
            {
                return this;
            }
            this.health -= damage;
            invincibility = 0.75f;
            hitSound.pitch = UnityEngine.Random.Range(0.75f, 1.5f);
            hitSound.Play();

            if (health <= 0)
            {
                Die();
            }
            return this;
        }
        public Turret Hurt(int damage, CollisionSide side)
        {
            if (invincibility > 0)
            {
                return this;
            }
            Vector2 velocity = new Vector2(4, 8);
            switch (side)
            {
                case CollisionSide.left:
                    break;
                case CollisionSide.right:
                    velocity.x *= -1;
                    break;
                case CollisionSide.top:
                    velocity.y = velocity.x * -1;
                    velocity.x = 0;
                    break;
                case CollisionSide.bottom:
                    velocity.y = velocity.x;
                    velocity.x = 0;
                    break;
            }
            rb.linearVelocity = velocity;
            return Hurt(damage);
        }
        public Turret Heal(int heal)
        {
            this.health += heal;
            return this;
        }
        public static Turret? GetTurretForTool(ToolItem tool)
        {
            foreach (Turret i in turrets)
            {
                if (i.tool == tool)
                {
                    return i;
                }
            }
            return null;
        }
        public void Shoot()
        {
            if (EngineersCrestPlugin.TOOL_BLACKLIST.Contains(tool.name))
            {
                return;
            }
            EngineersCrestPlugin.ThrowToolFrom(tool, transform.position, facingRight);
            if (EngineersCrestPlugin.TOOL_TURRET_CODE_ADDITION.ContainsKey(tool.name))
            {
                EngineersCrestPlugin.TOOL_TURRET_CODE_ADDITION[tool.name].Invoke(tool, this);
            }
        }
        public void ResetCooldown()
        {
            if (shot >= GetMaxShots())
            {
                cooldown = 0.2f + (tool.Usage.ThrowCooldown * 3f);
                shot = 1;
            } else
            {
                shot++;
                cooldown = tool.Usage.ThrowCooldown;
            }
        }
        public void Start()
        {
            ResetCooldown();
            rb = GetComponent<Rigidbody2D>();
            hitSound = transform.Find("HitSound").GetComponent<AudioSource>();
        }
        public void Awake()
        {
            turrets.Add(this);
        }
        public void OnDestroy()
        {
            turrets.Remove(this);
        }
        public void FixedUpdate()
        {
            if (cooldown > 0)
            {
                cooldown -= Time.fixedDeltaTime;
            }
            if (invincibility > 0)
            {
                invincibility -= Time.fixedDeltaTime;
            }
            if (cooldown <= 0)
            {
                Shoot();
                ResetCooldown();
            }
            if (HeroController.instance.playerData.atBench)
            {
                GameObject.Destroy(this.gameObject);
            }
        }
    }
}
