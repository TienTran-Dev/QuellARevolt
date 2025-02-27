using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    private int health;

    public void TakeDamage(int dame)
    {
        health -= dame;
        if (health < 0)
        {
            Destroy(this.gameObject);
        }
    }
}
