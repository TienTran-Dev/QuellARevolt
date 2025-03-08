using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private float m_AttackTime;
    private int m_AttackClick;
    // Attack
    [SerializeField]
    private float radius;
    [SerializeField]
    private Transform[] hitPoint;
    [SerializeField]
    private int dame;
    [SerializeField]
    private GameObject Effect;
    [SerializeField]
    private LayerMask targetPlayer;
    private Animator animatorAttack;
   
    private void Start()
    {
        animatorAttack =  GetComponent<Animator>();
    }
    private void Update()
    {
        Attack();
    }
    public void Attack()
    {


        if (Input.GetMouseButtonDown(0))
        {

            if (Time.time - m_AttackTime < 1.5f) // 1 giây để bấm đòn tiếp theo
            {
                m_AttackClick++; // Tăng combo bước tiếp theo

            }

                m_AttackTime = Time.time; // Cập nhật thời gian nhấn chuột

                // Kích hoạt attack animation theo bước combo
                if (m_AttackClick == 1)
                {
                    animatorAttack.SetTrigger("Attack");
                }
                else if (m_AttackClick == 2)
                {
                    animatorAttack.SetTrigger("Attack2");
                }
                else if (m_AttackClick == 3)
                {
                    animatorAttack.SetTrigger("Attack3");

                }
            
            else
            {
                m_AttackTime = 0f;
                m_AttackClick = 0; // Reset combo sau đòn thứ 3
                animatorAttack.SetBool("IsIdleAttack", true);
                StartCoroutine(idleNormal()); // Chờ 3 giây rồi tắt IsIdleAttack
            }
        }

        IEnumerator idleNormal()
        {
            yield return new WaitForSeconds(3f);
            animatorAttack.SetBool("IsIdleAttack", false);
        }

        foreach (Transform t in hitPoint)
        {
            Collider[] hit = Physics.OverlapSphere(t.position, radius, targetPlayer);// tạo hình cầu ảo để gây dame.

            if (hit.Length > 0)// đảm bảo có 1 đối tượng tác động.
            {
                hit[0].GetComponent<PlayerHealth>().TakeDamage(dame);// tạo list collider xem các gameobject có component health nhận dame.
                Instantiate(Effect.transform, hit[0].transform.position + new Vector3(0f, 1f, 0f), Quaternion.identity); // tạo bản sao effect.
            }
        }
    }

}
