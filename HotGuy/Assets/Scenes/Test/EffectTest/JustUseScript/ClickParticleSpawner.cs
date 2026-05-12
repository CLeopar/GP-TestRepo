using UnityEngine;

namespace MyImport.Project.Romain.Scene.Effect_Test
{
    public class ClickParticleSpawner : MonoBehaviour
    {
        [Header("Particle Settings")]
        public GameObject particlePrefab;
        public int count = 20;
        public float minSpeed = 200f;
        public float maxSpeed = 500f;
        public float lifetime = 3f;

        [Header("Color Settings")]
        [Tooltip("If true, ignores dominant color and uses manualColor instead.")]
        public bool useManualColor = false;
        public Color manualColor = Color.white;

        // How many sample pixels to read (higher = more accurate, slower)
        [Range(100, 2000)]
        public int colorSampleCount = 500;

        private Camera _cam;
        private Color _dominantColor = Color.white;
        private SpriteRenderer _targetSprite;

        void Start()
        {
            _cam = Camera.main;
            _targetSprite = GetComponent<SpriteRenderer>();
            SampleDominantColor();
        }

        // Call this any time the image changes at runtime
        public void SampleDominantColor()
        {
            if (useManualColor) return;
            if (_targetSprite == null || _targetSprite.sprite == null)
            {
                Debug.LogWarning("ClickParticleSpawner: No SpriteRenderer or Sprite found. Using white.");
                _dominantColor = Color.white;
                return;
            }

            _dominantColor = GetDominantColor(_targetSprite.sprite);
        }

        // Samples random pixels from the sprite texture and finds the most common color bucket
        Color GetDominantColor(Sprite sprite)
        {
            Texture2D tex = sprite.texture;

            // Sprites may use a texture atlas — only read pixels within the sprite rect
            int texX = Mathf.FloorToInt(sprite.rect.x);
            int texY = Mathf.FloorToInt(sprite.rect.y);
            int texW = Mathf.FloorToInt(sprite.rect.width);
            int texH = Mathf.FloorToInt(sprite.rect.height);

            // Texture must be readable — set Read/Write Enabled in Import Settings
            Color[] pixels;
            try
            {
                pixels = tex.GetPixels(texX, texY, texW, texH);
            }
            catch
            {
                Debug.LogWarning("ClickParticleSpawner: Texture is not readable. " +
                    "Enable Read/Write in the texture Import Settings. Falling back to white.");
                return Color.white;
            }

            // Bucket colors into a coarse grid to find the dominant hue
            // Each axis divided into 8 buckets = 512 total buckets
            int buckets = 8;
            float[,,] bucketWeights = new float[buckets, buckets, buckets];

            int step = Mathf.Max(1, pixels.Length / colorSampleCount);
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color c = pixels[i];
                if (c.a < 0.1f) continue; // skip transparent pixels

                int r = Mathf.Min(Mathf.FloorToInt(c.r * buckets), buckets - 1);
                int g = Mathf.Min(Mathf.FloorToInt(c.g * buckets), buckets - 1);
                int b = Mathf.Min(Mathf.FloorToInt(c.b * buckets), buckets - 1);
                bucketWeights[r, g, b] += 1f;
            }

            // Find the heaviest bucket
            float maxWeight = 0f;
            int br = 0, bg = 0, bb = 0;
            for (int r = 0; r < buckets; r++)
                for (int g = 0; g < buckets; g++)
                    for (int b = 0; b < buckets; b++)
                        if (bucketWeights[r, g, b] > maxWeight)
                        {
                            maxWeight = bucketWeights[r, g, b];
                            br = r; bg = g; bb = b;
                        }

            // Return the center of the winning bucket
            float inv = 1f / buckets;
            return new Color((br + 0.5f) * inv, (bg + 0.5f) * inv, (bb + 0.5f) * inv);
        }

        void OnMouseDown()
        {
            if (_cam == null) return;

            Vector3 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f;
            SpawnBurst(worldPos);
        }

        void SpawnBurst(Vector3 origin)
        {
            Color particleColor = useManualColor ? manualColor : _dominantColor;

            for (int i = 0; i < count; i++)
            {
                float baseAngle = (i / (float)count) * 360f * Mathf.Deg2Rad;
                float jitter = Random.Range(-0.5f, 0.5f) * (360f / count) * Mathf.Deg2Rad * 0.5f;
                float angle = baseAngle + jitter;

                // Clamp vertical component so particles don't fly too high
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                dir.y = Mathf.Clamp(dir.y, -1f, 0.4f); // max 40% upward force
                dir.Normalize();

                float speed = minSpeed + Random.Range(0f, maxSpeed - minSpeed);

                GameObject p = Instantiate(particlePrefab, origin, Quaternion.identity);
                BouncingParticle bp = p.GetComponent<BouncingParticle>();
                if (bp != null)
                    bp.Init(dir * speed, lifetime, particleColor);
            }
        }

        // Call this from other scripts when you swap the image at runtime
        public void OnImageChanged()
        {
            SampleDominantColor();
        }
    }
}