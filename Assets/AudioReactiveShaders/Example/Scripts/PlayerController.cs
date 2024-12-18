using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AudioReactiveShader
{
    public class PlayerController : MonoBehaviour
    {

        Rigidbody playerRigidbody;

        public Transform camTransform;
        public float horizontalLookSensitivity;
        public float verticalLookSensitivity;
        public float smoothCameraTime;
        public Vector2 lookMinMax;
        public float walkSpeed;

        Quaternion playerTargetRotation;
        Quaternion camTargetRotation;

        Vector3 moveInput;
        Vector3 moveDirection;


        void Start()
        {
            playerRigidbody = gameObject.GetComponent<Rigidbody>();

            playerTargetRotation = transform.localRotation;
            camTargetRotation = camTransform.localRotation;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            Movement();
        }

        void LateUpdate()
        {
            LookRotation();
        }

        void LookRotation()
        {
            playerTargetRotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * horizontalLookSensitivity, 0f);
            camTargetRotation *= Quaternion.Euler(-Input.GetAxis("Mouse Y") * verticalLookSensitivity, 0f, 0f);
            camTargetRotation = ClampRotationAroundXAxis(camTargetRotation);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, playerTargetRotation, smoothCameraTime * Time.deltaTime);
            camTransform.localRotation = Quaternion.Slerp(camTransform.localRotation, camTargetRotation, smoothCameraTime * Time.deltaTime);

        }

        void Movement()
        {
            float moveSpeed = walkSpeed;
            moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveInput);
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1);

            Vector3 newVelocity;

            if (moveInput.magnitude > 0.05)
            {
                newVelocity = new Vector3((moveSpeed * moveDirection.x), playerRigidbody.linearVelocity.y, (moveSpeed * moveDirection.z));
            }
            else
            {
                newVelocity = new Vector3(0, playerRigidbody.linearVelocity.y, 0);
            }

            float yVelocity = playerRigidbody.linearVelocity.y;
            newVelocity.y = 0;
            newVelocity = Vector3.ClampMagnitude(newVelocity, moveSpeed);
            newVelocity.y = yVelocity;

            playerRigidbody.linearVelocity = newVelocity;
        }


        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, lookMinMax.x, lookMinMax.y);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }

    }
}
