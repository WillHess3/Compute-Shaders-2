using UnityEngine;
using System.Collections;

public class GameOfLife : MonoBehaviour {

    private RenderTexture _renderTexture;

    [SerializeField] private ComputeShader _computeShader;

    [SerializeField] [Min(float.Epsilon)] private float _timeInBetweenSteps;

    private struct Cell {
        public int isActive;
    }

    private Cell[] _cells;

    private void Start () {
        _renderTexture = new RenderTexture(1920, 1080, 24);
        _renderTexture.enableRandomWrite = true;
        _renderTexture.Create();

        _cells = new Cell[_renderTexture.width * _renderTexture.height];

        for (int i = 0; i < _cells.Length; i++) {
            _cells[i].isActive = Random.value < .333f ? 1 : 0;
        }

        DispatchComputeShader();
        StartCoroutine(NextStep());
    }

    private void DispatchComputeShader() {
        int size = sizeof(int);
        ComputeBuffer computeBufferReferenceCells = new ComputeBuffer(_cells.Length, size);
        computeBufferReferenceCells.SetData(_cells);

        Cell[] cellsThatGetChanged = _cells;
        ComputeBuffer computeBufferChangableCells = new ComputeBuffer(cellsThatGetChanged.Length, size);
        computeBufferChangableCells.SetData(cellsThatGetChanged);

        _computeShader.SetBuffer(0, "cellsReference", computeBufferReferenceCells);
        _computeShader.SetBuffer(0, "cellsThatGetChanged", computeBufferChangableCells);
        _computeShader.SetTexture(0, "Result", _renderTexture);
        _computeShader.Dispatch(0, _renderTexture.width / 16, _renderTexture.height / 16, 1);

        computeBufferReferenceCells.GetData(_cells);
        computeBufferChangableCells.GetData(cellsThatGetChanged);

        _cells = cellsThatGetChanged;

        computeBufferReferenceCells.Dispose();
        computeBufferChangableCells.Dispose();

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(_renderTexture, destination);
    }

    private IEnumerator NextStep() {
        yield return new WaitForSeconds(_timeInBetweenSteps);
        DispatchComputeShader();

        StartCoroutine(NextStep());
    }

}
