using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleFlipByPosition : MonoBehaviour
{
    [SerializeField] private Transform center;
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private ParticleSystemRenderer psRenderer;
    private Camera cam;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
        cam = Camera.main;
    }

    void LateUpdate()
    {
        int count = ps.GetParticles(particles);

        // カメラとパーティクルの位置関係を計算
        Vector3 systemPos = ps.transform.position;
        Vector3 toParticle = (systemPos - center.position).normalized;
        Vector3 toCamera = (cam.transform.position - center.position).normalized;
        float angle = Vector3.SignedAngle(toParticle, toCamera, Vector3.up);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;

            // カメラ位置によって回転方向を制御
            if(Mathf.Abs(angle) >= 90f)
            {
                if (pos.x <= 0)
                    psRenderer.flip = new Vector3(1, 0, 0); // 左右反転
                else
                    psRenderer.flip = Vector3.zero;
            } else {
                if (pos.x <= 0)
                    psRenderer.flip = Vector3.zero;
                else
                    psRenderer.flip = new Vector3(1, 0, 0); // 左右反転
            }
        }

        ps.SetParticles(particles, count);
    }
}