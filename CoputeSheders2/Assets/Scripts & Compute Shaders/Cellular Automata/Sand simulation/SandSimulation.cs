using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandSimulation : MonoBehaviour {

    private Texture2D _texture2D;
    private RenderTexture _renderTexture;

    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] private bool _isUsingGPU;

    [SerializeField][Range(0.0001f, 1)] private float _timeStep;
    [SerializeField] private Vector2Int _dimensions;
    [SerializeField] private int _drawRadius;
    [SerializeField] private CellState _drawingCellState;

    [SerializeField] private bool _isDrawing;
    [SerializeField] private bool _isDithering;
    private bool _isSimulateStarted;

    private enum CellState {
        Empty,
        Sand
    }

    private CellState[,] _cellStates;

    private struct Cell {
        public int stateIndex;
    }

    private Cell[] _cells;

    private void Start() {
        _texture2D = new Texture2D(_dimensions.x, _dimensions.y);

        _renderTexture = new RenderTexture(_dimensions.x, _dimensions.y, 24);
        _renderTexture.enableRandomWrite = true;
        _renderTexture.Create();

        _cellStates = new CellState[_dimensions.x, _dimensions.y];
        for (int j = 0; j < _dimensions.y; j++) {
            for (int i = 0; i < _dimensions.x; i++) {
                _cellStates[i, j] = CellState.Empty;
            }
        }

        _cells = new Cell[_dimensions.x * _dimensions.y];
    }

    private void Update() {
        if (_isDrawing) {
            DrawNewParticles();
            _isSimulateStarted = false;
            DrawTexture();
        } else {
            if (!_isSimulateStarted) {
                for (int j = 0; j < _dimensions.y; j++) {
                    for (int i = 0; i < _dimensions.x; i++) {
                        _cells[i + j * _dimensions.x].stateIndex = _cellStates[i, j] == CellState.Sand ? 1 : 0;
                    }
                }

                _isSimulateStarted = true;
                StartCoroutine(Tick());
            }
        }
    }

    private IEnumerator Tick() {
        if (!_isUsingGPU) {
            UpdateCells();
            DrawTexture();
        } else {
            DispatchComputeShader();
        }

        yield return new WaitForSeconds(_timeStep);

        if (!_isDrawing) {
            StartCoroutine(Tick());
        }
    }

    private void DrawNewParticles() {
        if (Input.GetMouseButtonDown(0)) {
            Vector2Int mousePosition = new Vector2Int((int)Input.mousePosition.x, (int)Input.mousePosition.y);
            for (int j = -_drawRadius; j < _drawRadius; j++) {
                for (int i = -_drawRadius; i < _drawRadius; i++) {
                    if (!IsGoodToDraw(i, j, mousePosition)) {
                        continue;
                    }

                    Vector2Int pixelCoordinate = new Vector2Int(mousePosition.x + i, mousePosition.y + j);
                    _cellStates[pixelCoordinate.x, pixelCoordinate.y] = _drawingCellState;
                }
            }
        }
    }

    private bool IsGoodToDraw(int x, int y, Vector2Int mousePosition) {
        bool isInCircle = x * x + y * y < _drawRadius * _drawRadius;
        bool isWithinXBoundsOfScreen = x + mousePosition.x >= 0 && x + mousePosition.x < _dimensions.x;
        bool isWithinYBoundsOfScreen = y + mousePosition.y >= 0 && y + mousePosition.y < _dimensions.y;
        bool isWithinDither = _isDithering ? (x + y + y * _dimensions.x) % 2 == 0 : true;

        return isInCircle && isWithinXBoundsOfScreen && isWithinYBoundsOfScreen && isWithinDither;
    }

    private void UpdateCells() {
        CellState[,] cellStates = new CellState[_cellStates.GetLength(0), _cellStates.GetLength(1)];

        for (int j = 0; j < _cellStates.GetLength(1); j++) {
            for (int i = 0; i < _cellStates.GetLength(0); i++) {
                switch (_cellStates[i, j]) {
                    case CellState.Empty:
                        cellStates[i, j] = CellState.Empty;
                        break;

                    case CellState.Sand:
                        if (j > 0) {
                            if (_cellStates[i, j - 1] == CellState.Empty) {
                                //nothing below
                                cellStates[i, j - 1] = CellState.Sand;
                                cellStates[i, j] = CellState.Empty;
                            } else {
                                //something below turn to the side
                                int offset = Mathf.CeilToInt(Random.value * 2) - 1;
                                if (i - 1 >= 0 && i + 1 < _cellStates.GetLength(0)) {
                                    if (_cellStates[i + offset, j - 1] == CellState.Empty) {
                                        cellStates[i + offset, j - 1] = CellState.Sand;
                                        cellStates[i, j] = CellState.Empty;
                                    } else if (_cellStates[i - offset, j - 1] == CellState.Empty) {
                                        cellStates[i - offset, j - 1] = CellState.Sand;
                                        cellStates[i, j] = CellState.Empty;
                                    } else {
                                        cellStates[i, j] = CellState.Sand;
                                    }
                                } else {
                                    //edge cases
                                    if (i == 0) {
                                        if (_cellStates[1, j - 1] == CellState.Empty) {
                                            cellStates[1, j - 1] = CellState.Sand;
                                            cellStates[i, j] = CellState.Empty;
                                        } else {
                                            cellStates[i, j] = CellState.Sand;
                                        }
                                    } else if (i == _cellStates.GetLength(0) - 1) {
                                        if (_cellStates[_cellStates.GetLength(0) - 2, j - 1] == CellState.Empty) {
                                            cellStates[_cellStates.GetLength(0) - 2, j - 1] = CellState.Sand;
                                            cellStates[i, j] = CellState.Empty;
                                        } else {
                                            cellStates[i, j] = CellState.Sand;
                                        }
                                    }
                                }
                            }
                        } else {
                            cellStates[i, j] = CellState.Sand;
                        }
                        break;
                }
            }
        }

        _cellStates = cellStates;
    }

    private void DrawTexture() {
        Color[] pixels = new Color[_dimensions.x * _dimensions.y];

        Color sandColor = Color.yellow;
        Color emptyColor = Color.white;

        for (int j = 0; j < _dimensions.y; j++) {
            for (int i = 0; i < _dimensions.x; i++) {
                switch (_cellStates[i, j]) {
                    case CellState.Empty:
                        pixels[i + j * _dimensions.x] = emptyColor;
                        break;

                    case CellState.Sand:
                        pixels[i + j * _dimensions.x] = sandColor;
                        break;
                }
            }
        }

        _texture2D.SetPixels(pixels);
        _texture2D.Apply();
    }

    private void DispatchComputeShader() {
        ComputeBuffer computeBuffer = new ComputeBuffer(_cells.Length, sizeof(int));
        computeBuffer.SetData(_cells);

        _computeShader.SetTexture(0, "Result", _renderTexture);
        _computeShader.SetBuffer(0, "Cells", computeBuffer);
        _computeShader.SetFloat("RandomNumber", Random.value);
        _computeShader.SetFloats("EmptyColor", new float[4] { 1, 1, 1, 1 });
        _computeShader.SetFloats("SandColor", new float[4] { 0, 0, 0, 1 });
        _computeShader.Dispatch(0, _dimensions.x / 8, _dimensions.y / 8, 1);

        computeBuffer.GetData(_cells);
        computeBuffer.Dispose();

        ComputeBuffer computeBuffer1 = new ComputeBuffer(_cells.Length, sizeof(int));
        computeBuffer1.SetData(_cells);

        _computeShader.SetBuffer(1, "Cells", computeBuffer1);
        _computeShader.SetTexture(1, "Result", _renderTexture);
        _computeShader.Dispatch(1, _dimensions.x / 8, _dimensions.y / 8, 1);

        computeBuffer1.GetData(_cells);
        computeBuffer1.Dispose();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (!_isUsingGPU || _isDrawing) {
            Graphics.Blit(_texture2D, destination);
        } else {
            Graphics.Blit(_renderTexture, destination);
        }
    }

}
