using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>Shrinks an object to nothing over the last part of its life, then destroys it.</summary>
    public sealed class ShrinkAndDestroy : MonoBehaviour
    {
        float life = 1f, age;
        Vector3 startScale;

        public void Begin(float seconds)
        {
            life = Mathf.Max(0.1f, seconds);
            startScale = transform.localScale;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.InverseLerp(life * 0.6f, life, age);
            transform.localScale = startScale * (1f - k);
            if (age >= life) Destroy(gameObject);
        }
    }
}
