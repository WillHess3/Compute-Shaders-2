using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoise {

    public float[,] Noise => _noise;

    private Vector2Int _dimensions;

    private int _scale;
    private int _seed;

    private float[,] _noise;

    private System.Random _random;

    private ComputeShader _computeShader;

    private struct VectorOnGrid {
        public float randomNumber;
        public Vector2 vector2;
    }

    public PerlinNoise(int seed, int width, int height, int scale, ComputeShader computeShader) {
        _dimensions = new Vector2Int(width, height);
        _scale = scale;
        _seed = seed;

        _noise = new float[width, height];

        _random = new System.Random(_seed);

        _computeShader = computeShader;
    }

    public void GenerateCPU() {
        _random = new System.Random(_seed);

        Vector2[,] gridOfVectors = GeneratePerlinNoiseVectorGrid();

        for (int j = 0; j < _dimensions.y; j++) {
            for (int i = 0; i < _dimensions.x; i++) {
                _noise[i, j] = SampleNoise(Mathf.Lerp(0, _scale, i / (float)_dimensions.x), Mathf.Lerp(0, _scale, j / (float)_dimensions.y), gridOfVectors);
            }
        }
    }

    private Vector2[,] GeneratePerlinNoiseVectorGrid() {
        Vector2[,] vectorGrid = new Vector2[_scale + 1, _scale + 1];
        for (int j = 0; j < vectorGrid.GetLength(0); j++) {
            for (int i = 0; i < vectorGrid.GetLength(1); i++) {
                Vector2 randomVector;
                if (i == _scale) {
                    randomVector = vectorGrid[0, j];
                } else if (j == _scale) {
                    randomVector = vectorGrid[i, 0];
                } else {
                    float angle = (float) _random.NextDouble() * Mathf.PI * 2;
                    randomVector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }

                vectorGrid[i, j] = randomVector;
            }
        }

        return vectorGrid;
    }

    private float SampleNoise(float x, float y, Vector2[,] gridOfVectors) {
        int bottomLeftXCoord = Mathf.FloorToInt(x);
        int bottomLeftYCoord = Mathf.FloorToInt(y);

        float xOffset = bottomLeftXCoord - x;
        float yOffset = bottomLeftYCoord - y;

        float[] dotProductValues = new float[4];

        for (int j = 0; j < 2; j++) {
            for (int i = 0; i < 2; i++) {
                Vector2 offsetVector = new Vector2(xOffset + i, yOffset + j);

                float dotProduct = Vector2.Dot(offsetVector, gridOfVectors[bottomLeftXCoord + i, bottomLeftYCoord + j]);
                dotProductValues[i + j * 2] = dotProduct;
            }
        }

        float horizontalLerpParameter = 3 * (xOffset * -1) * (xOffset * -1) - 2 * (xOffset * -1) * (xOffset * -1) * (xOffset * -1);
        float bottomLerpValue = Mathf.Lerp(dotProductValues[0], dotProductValues[1], horizontalLerpParameter);
        float topLerpValue = Mathf.Lerp(dotProductValues[2], dotProductValues[3], horizontalLerpParameter);

        float verticalLerpParameter = 3 * (yOffset * -1) * (yOffset * -1) - 2 * (yOffset * -1) * (yOffset * -1) * (yOffset * -1);
        float noiseValue = Mathf.Lerp(bottomLerpValue, topLerpValue, verticalLerpParameter);

        float scaledNoiseValue = (noiseValue + 0.707106f) / 1.41421f;
        return scaledNoiseValue;
    }

    public void GenerateGPU() {
        _random = new System.Random(_seed);

        VectorOnGrid[] vectorsOnGrid = new VectorOnGrid[(_scale + 1) * (_scale + 1)];

        for (int j = 0; j < _scale + 1; j++) {
            for (int i = 0; i < _scale + 1; i++) {
                float randomNumber;
                if (i == _scale) {
                    randomNumber = vectorsOnGrid[j * (_scale + 1)].randomNumber;
                } else if (j == _scale) {
                    randomNumber = vectorsOnGrid[i].randomNumber;
                } else {
                    randomNumber = (float)_random.NextDouble();
                }

                vectorsOnGrid[j * (_scale + 1) + i].randomNumber = randomNumber;
            }
        }

        int size = sizeof(float) * 3;
        ComputeBuffer vectorsOnGridBuffer = new ComputeBuffer((_scale + 1) * (_scale + 1), size);
        vectorsOnGridBuffer.SetData(vectorsOnGrid);

        _computeShader.SetBuffer(0, "VectorsOnGrid", vectorsOnGridBuffer);
        _computeShader.Dispatch(0, Mathf.RoundToInt(vectorsOnGrid.Length / Mathf.Sqrt(vectorsOnGrid.Length)), 1, 1);

        vectorsOnGridBuffer.GetData(vectorsOnGrid);

        float[] noiseValues = new float[_dimensions.x * _dimensions.y];
        ComputeBuffer noiseValuesBuffer = new ComputeBuffer(_dimensions.x * _dimensions.y, sizeof(float));
        noiseValuesBuffer.SetData(noiseValues);

        _computeShader.SetBuffer(1, "NoiseValues", noiseValuesBuffer);
        _computeShader.SetBuffer(1, "ReadOnlyVectorsOnGrid", vectorsOnGridBuffer);
        _computeShader.SetInt("gridWidth", _scale);
        _computeShader.SetInt("width", _dimensions.x);
        _computeShader.SetInt("height", _dimensions.y);
        _computeShader.Dispatch(1, _dimensions.x / 32, _dimensions.y / 32, 1);

        noiseValuesBuffer.GetData(noiseValues);

        for (int j = 0; j < _dimensions.y; j++) {
            for (int i = 0; i < _dimensions.x; i++) {
                _noise[i, j] = noiseValues[i + j * _dimensions.x];
            }
        }

        noiseValuesBuffer.Dispose();
        vectorsOnGridBuffer.Dispose();
    }
}
