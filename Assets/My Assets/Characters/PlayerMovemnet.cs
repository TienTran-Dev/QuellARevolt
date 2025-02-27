using System.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovemnet : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed;
    public float runSpeed;
    public float jumpPower;
    public float gravity;
    public float lookSpeed;
    public float lookXLimit;
    public float defaultHeight;
    public float crouchHeight;
    public float crouchSpeed;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX;
    private CharacterController characterController;
    private Animator animator;
  
   

    [SerializeField]
    private BoxCollider IsJumping;
    public bool canMove = true;
    private bool canJump = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        IsJumping = GetComponent<BoxCollider>();
        animator = GetComponent<Animator>();

    }

    private void OnTriggerEnter(Collider other)
    {


        if (other.gameObject.CompareTag("Ground"))
        {
            canJump = true;
        }

    }
    private void OnTriggerExit(Collider other)
    {


        if (other.gameObject.CompareTag("Ground"))
        {
            canJump = false;
        }

    }

    private void IsMoving()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        if (Input.GetKey(KeyCode.LeftShift) && canMove)
        {

            moveDirection.x = move.x * runSpeed;
            moveDirection.z = move.z * runSpeed;
            if (Input.GetKey(KeyCode.LeftShift) && moveX > 0 || moveZ > 0)
                animator.SetBool("IsFastRunning", true);
            animator.SetBool("IsWalkingBack", false);

        }
        else
        {
            moveDirection.x = move.x * walkSpeed;
            moveDirection.z = move.z * walkSpeed;
            if (moveX != 0f || moveZ != 0f)
            {
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsFastRunning", false);
                animator.SetBool("IsWalkingBack", false);

            }
            else
            {
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsFastRunning", false);
                animator.SetBool("IsWalkingBack", false);
            }

        }
        if (Input.GetKey(KeyCode.S))
        {
            float moveSpeed = 5f;

            if (Input.GetKey(KeyCode.S))
            {
                // Di chuyển lùi theo hướng ngược lại với transform.forward
                transform.position -= transform.forward * moveSpeed * Time.deltaTime;
                // Bật animation đi lùi
                animator.SetBool("IsWalkingBack", true);
                animator.SetBool("IsFastRunning", false);
            }
            else
            {
                
                animator.SetBool("IsWalkingBack", false);
            
                
            }

            //Quaternion toRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            //transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, lookSpeed * Time.deltaTime);
            //animator.SetBool("IsWalkingBack", true);
            //animator.SetBool("IsFastRunning", false);
            //animator.SetBool("IsWalking", false);

        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private bool IsCanJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canMove)
        {
            moveDirection.y = jumpPower; // Áp dụng lực nhảy

        }
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime; // Áp dụng trọng lực khi rơi
        }
        return canJump;
    }

    private void IsCanSitDown()
    {
        float speed;
        if (Input.GetKey(KeyCode.LeftControl) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;

        }
        else
        {
            characterController.height = defaultHeight;
            speed = walkSpeed;
            speed = runSpeed;

        }


    }

    private void IsCanLookAround()
    {
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
 
    void Update()
    {

        IsMoving();
        IsCanJump();
        IsCanLookAround();
        IsCanSitDown();
      

    }
}




