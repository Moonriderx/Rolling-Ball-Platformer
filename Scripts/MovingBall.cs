using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Moonrider
{
    public class MovingBall : MonoBehaviour
    {

        Rigidbody body;

        [SerializeField, Range(0f, 100f)]
        float maxSpeed = 10f;

        [SerializeField, Range(0f, 100f)]
        float maxAcceleration = 10f, maxAirAcceleration = 1f;

        [SerializeField, Range(0f, 10f)]
        float jumpHeight = 2f;

        [SerializeField, Range(0, 5)]
        int maxAirJumps = 0;


        Vector3 velocity;
        Vector3 desiredVelocity;
        bool desiredJump;
        public bool onGround;
        int jumpPhase;


        // Start is called before the first frame update    
        void Start()
        {
            body = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            Vector2 playerInput; // we are reading the player input
            playerInput.x = Input.GetAxis("Horizontal");
            playerInput.y = Input.GetAxis("Vertical");
            playerInput = Vector2.ClampMagnitude(playerInput, 1f); // made the position inside the "circle" valid too.

            desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
            desiredJump |= Input.GetButtonDown("Jump");
        }

         

        private void FixedUpdate()
        {

            
            UpdateState();
            float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
            float maxSpeedChange = acceleration * Time.deltaTime;
            velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
            velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
            body.velocity = velocity;

            if (desiredJump)
            {
                desiredJump = false;
                Jump();
            }
            body.velocity = velocity;
            onGround = false;


        }

        private void UpdateState()
        {
            velocity = body.velocity;
            if (onGround)
            {
                jumpPhase = 0;
            }
        }

        void Jump()
        {
            
                if (onGround || jumpPhase < maxAirJumps)
                {
                    float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
                if (velocity.y > 0 )
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
                }
                    jumpPhase += 1;
                velocity.y += jumpSpeed;
                }
        }

        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision);
        }

        void EvaluateCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal; // The normal is the direction that the sphere should be pushed, which is directly away from the collision surface.
                onGround |= normal.y >= 0.9f;
            }
        }
    }

    }


