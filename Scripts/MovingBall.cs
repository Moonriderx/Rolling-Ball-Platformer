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
        float maxAcceleration = 10f;

        [SerializeField, Range(0f, 10f)]
        float jumpHeight = 2f;

        Vector3 velocity;
        Vector3 desiredVelocity;
        bool desiredJump;
        public bool onGround;


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

            velocity = body.velocity;
            float maxSpeedChange = maxAcceleration * Time.deltaTime;
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

        void Jump()
        {
            if (onGround)
            {
                velocity.y += Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            onGround = true;
        }

        private void OnCollisionStay(Collision collision)
        {
            onGround = true;   
        }
    }

    }


