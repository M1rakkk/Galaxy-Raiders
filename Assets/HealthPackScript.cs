using UnityEngine;

public class HealthPackScript : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public int healAmount = 25;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Вращаем вокруг оси Z, как и обычные бонусы, чтобы не было "диагонального" вращения
            rb.angularVelocity = Vector3.forward * rotationSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PLayerScript player = other.GetComponent<PLayerScript>();
            if (player != null)
            {
                player.Heal(healAmount);
                Destroy(gameObject);
            }
        }
        // Убрали проверку на GameBoundary, так как аптечка может создаваться внутри него
    }
}
