using UnityEngine;

public class SpinnerRotator : MonoBehaviour
{
    [Tooltip("Мин. скорость (°/с)")]
    public float minSpeed = 100f;
    [Tooltip("Макс. скорость (°/с)")]
    public float maxSpeed = 300f;
    [Tooltip("Период пульсации (сек)")]
    public float period = 2f;

    void Update()
    {
        // 0→1→0 за период
        float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / period) + 1f) * 0.5f;
        float currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);
        transform.Rotate(0f, 0f, currentSpeed * Time.deltaTime);
    }
}
