using UnityEngine;

public class PerlinNoiseGenerator : MonoBehaviour {

    private Texture2D _displayTexture;
    [SerializeField] private bool _isDisplayingTexture;

    [SerializeField] private ComputeShader _computeShader;

    [SerializeField] private int _scale;
    [SerializeField] private int _seed;

    private struct VectorOnGrid {
        public float randomNumber;
        public Vector2 vector2;
    }

    private PerlinNoise _perlinNoise;

    private void Start() {
        _displayTexture = new Texture2D(1024, 1024);

        _perlinNoise = new PerlinNoise(_seed, 1024, 1024, _scale, _computeShader);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            _perlinNoise.GenerateCPU();

            if (_isDisplayingTexture) { 
                GenerateTexture();
            }
        } else if (Input.GetMouseButtonDown(1)) {
            _perlinNoise.GenerateGPU();

            if (_isDisplayingTexture) {
                GenerateTexture();
            }
        }
    }

    private void GenerateTexture() {
        Color[] pixels = new Color[1024 * 1024];
        for (int j = 0; j < 1024; j++) {
            for (int i = 0; i < 1024; i++) {
                float value = _perlinNoise.Noise[i, j];
                pixels[i + j * 1024] = new Color(value, value, value);
            }
        }
        _displayTexture.SetPixels(pixels);
        _displayTexture.Apply();
    }

   
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (_isDisplayingTexture) {
            Graphics.Blit(_displayTexture, destination);
        } 
    }
}
