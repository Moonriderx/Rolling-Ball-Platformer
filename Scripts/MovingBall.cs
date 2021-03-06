using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Moonrider
{
    public class MovingBall : MonoBehaviour
    {

        Rigidbody rigidBody;

        [SerializeField, Range(0f, 100f)]
        float maxSpeed = 10f;

        [SerializeField, Range(0f, 100f)]
        float maxAcceleration = 10f, maxAirAcceleration = 1f;

        [SerializeField, Range(0f, 10f)]
        float jumpHeight = 2f;

        [SerializeField, Range(0, 5)]
        int maxAirJumps = 0;

        [SerializeField, Range(0f, 90f)]
        float maxGroundAngle = 25f, maxStairsAngle = 50f;

        [SerializeField, Range(0f, 100f)]
        float maxSnapSpeed = 100f;

        [SerializeField, Min(0f)]
        float probeDistance = 1f;

        [SerializeField]
        LayerMask probeMask = -1, stairMask = -1;

        Vector3 velocity;
        Vector3 desiredVelocity;
        Vector3 contactNormal, steepNormal;
        bool desiredJump;
        int groundContactCount, steepContactCount;
        int stepsSinceLastGrounded, stepsSinceLastJump;
        bool OnGround => groundContactCount > 0;

        bool OnSteep => steepContactCount > 0;
        /* the line above is the same like ->
         bool OnGround {
		get {
			return groundContactCount > 0;
		}
        } 
         */


        int jumpPhase;
        float minGroundDotProduct, minStairsDotProduct;

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            OnValidate();
        }

        // Start is called before the first frame update    
        void Start()
        {
            //rigidBody = GetComponent<Rigidbody>();
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
            AdjustVelocity();


            if (desiredJump)
            {
                desiredJump = false;
                Jump();
            }
            rigidBody.velocity = velocity;
            ClearState();


        }

        private void UpdateState()
        {
            stepsSinceLastGrounded += 1;
            stepsSinceLastJump += 1;
            velocity = rigidBody.velocity;
            if (OnGround || SnapToGround() || CheckSteepContacts())
            {

                stepsSinceLastGrounded = 0;
                if (stepsSinceLastJump > 1)
                {
                    jumpPhase = 0;
                }
                
                if (groundContactCount > 1)
                {
                    contactNormal.Normalize();
                }
            }
            else
            {
                contactNormal = Vector3.up;
            }
        }

        void Jump()
        {
            Vector3 jumpDirection;
               if (OnGround)
            {
                jumpDirection = contactNormal;
            }
               else if (OnSteep)
            {
                jumpDirection = steepNormal;
                jumpPhase = 0;
            }
               else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
            {
                if (jumpPhase == 0)
                {
                    jumpPhase = 1;
                }
                jumpDirection = contactNormal;
            }
            else
            {
                return;
            }
            stepsSinceLastJump = 0;
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
                jumpDirection = (jumpDirection + Vector3.up).normalized;
                float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
                if (alignedSpeed > 0)
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
                }
                
                velocity += jumpDirection * jumpSpeed;
            
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
            float minDot = GetMinDot(collision.gameObject.layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal; // The normal is the direction that the sphere should be pushed, which is directly away from the collision surface.
                if (normal.y >= minDot)
                {
                    groundContactCount += 1;
                    contactNormal += normal;
                }
                else if (normal.y > -0.01f)
                {
                    steepContactCount += 1;
                    steepNormal += normal;
                }
            }
        }

        void OnValidate()
        {
            minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            minStairsDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        }

        Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - contactNormal * Vector3.Dot(vector, contactNormal);
        }

        void AdjustVelocity()
        {
            Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

            float currentX = Vector3.Dot(velocity, xAxis);
            float currentZ = Vector3.Dot(velocity, zAxis);

            float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
            float maxSpeedChange = acceleration * Time.deltaTime;

            float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

            velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

        }

        void ClearState()
        {
            groundContactCount = steepContactCount = 0;
            contactNormal = steepNormal = Vector3.zero;
        }

        bool SnapToGround()
        {
            if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
            {
                return false;
            }

            float speed = velocity.magnitude;
            if (speed > maxSnapSpeed)
            {
                return false;
            }

            if (!Physics.Raycast(rigidBody.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask))
            {
                return false;
            }

            if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
            {
                return false;
            }
            groundContactCount = 1;
            contactNormal = hit.normal;
            float dot = Vector3.Dot(velocity, hit.normal);
            if (dot > 0f)
            {
                velocity = (velocity - hit.normal * dot).normalized * speed;
            }
            return true;
        }


        float GetMinDot(int layer)
        {
            return (stairMask & (1 << layer)) == 0 ? minStairsDotProduct : minStairsDotProduct;

        }

        bool CheckSteepContacts()
        {
            if (steepContactCount > 1)
            {
                steepNormal.Normalize();
                if (steepNormal.y >= minGroundDotProduct)
                {
                    groundContactCount = 1;
                    contactNormal = steepNormal;
                    return true;
                }
            }
            return false;
        }

    }
}
