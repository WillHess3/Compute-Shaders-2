using UnityEngine;

public class FractalNoiseGenerator : MonoBehaviour {

    private Texture2D _displayTexture;

    [SerializeField] private ComputeShader _computeShader;

    [SerializeField] private int _scale;

    [SerializeField] [Range(1, 6)] private int _octaves;
    [SerializeField] [Range(0,1)] private float _persistance;
    [SerializeField] [Range(1, 6)] private float _lacunarity;

    [SerializeField] Vector2Int _dimensions;
    [SerializeField] private int _seed;

    [SerializeField] private bool _isDisplayingTexture;
    [SerializeField] private bool _isUsingGPU;

    private float[,,] _noiseValuesPerOctave;
    private float[,] _noiseValues;

    private void Start() {
        _displayTexture = new Texture2D(_dimensions.x, _dimensions.y);

        _noiseValues = new float[_dimensions.x, _dimensions.y];
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            GenerateFractalNoise();

            if (_isDisplayingTexture) {
                GenerateTexture();
            }
        }
    }

    private void GenerateFractalNoise() {
        _noiseValuesPerOctave = new float[_octaves, _dimensions.x, _dimensions.y];

        for (int k = 0; k < _octaves; k++) {
            int scale = _scale * Mathf.RoundToInt(Mathf.Pow(_lacunarity, k));

            PerlinNoise perlinNoise = new PerlinNoise(_seed, _dimensions.x, _dimensions.y, scale, _computeShader);
            if (_isUsingGPU) {
                perlinNoise.GenerateGPU();
            } else {
                perlinNoise.GenerateCPU();
            }

            float[,] perlinNoiseValues = perlinNoise.Noise;
            float maxNoiseValue = Mathf.Pow(_persistance, k);
            for (int j = 0; j < _dimensions.y; j++) {
                for (int i = 0; i < _dimensions.x; i++) {
                    _noiseValuesPerOctave[k, i, j] = perlinNoiseValues[i, j] * maxNoiseValue;
                    _noiseValues[i, j] = 0;
                }
            }
        }

        for (int j = 0; j < _dimensions.y; j++) {
            for (int i = 0; i < _dimensions.x; i++) {
                for (int k = 0; k < _octaves; k++) {
                    _noiseValues[i, j] += _noiseValuesPerOctave[k, i, j];
                }
            }
        }
    }

    private void GenerateTexture() {
        Color[] pixels = new Color[_dimensions.x * _dimensions.y];
        for (int j = 0; j < _dimensions.y; j++) {
            for (int i = 0; i < _dimensions.x; i++) {
                float value = _noiseValues[i, j];
                pixels[i + j * _dimensions.x] = new Color(value, value, value);
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
