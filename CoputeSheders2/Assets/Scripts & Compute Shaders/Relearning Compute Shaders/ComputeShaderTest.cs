using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CoputeShaderTest : MonoBehaviour {

    [SerializeField] ComputeShader _computeShader;

    private float[] _squareRoots;

    private void Start() {
        _squareRoots = new float[262144];
    }

    private float NewtonsMethodAproximation(float number, float guess) {
        return guess - (guess * guess - number) / (2 * guess);
    }

    private void SquareRootComputeShader() {
        ComputeBuffer computeBuffer = new ComputeBuffer(_squareRoots.Length, sizeof(float));
        computeBuffer.SetData(_squareRoots);

        _computeShader.SetBuffer(1, "squareRoots", computeBuffer);
        _computeShader.Dispatch(1, _squareRoots.Length / 1024, 1, 1);

        computeBuffer.GetData(_squareRoots);

        computeBuffer.Dispose();

        Debug.Log("Done");
        //StartCoroutine(LogSquareRoots());
    }

    private void SquareRootCPU() {
        for (int i = 0; i < _squareRoots.Length; i++) {
            
            float aproximation = 0;
            for (int j = 0; j < 100; j++) {
                aproximation = NewtonsMethodAproximation(i, i + 1);
                aproximation = NewtonsMethodAproximation(i, aproximation);
                aproximation = NewtonsMethodAproximation(i, aproximation);
                aproximation = NewtonsMethodAproximation(i, aproximation);
                aproximation = NewtonsMethodAproximation(i, aproximation);
                aproximation = NewtonsMethodAproximation(i, aproximation);
                aproximation = NewtonsMethodAproximation(i, aproximation);
            }

            _squareRoots[i] = aproximation;
        }

        Debug.Log("Done");
        //StartCoroutine(LogSquareRoots());
    }

    private IEnumerator LogSquareRoots() {
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < _squareRoots.Length; i++) {
            Debug.Log(i + " --> " + _squareRoots[i]);
        }
    }

    private void Update(){
        if (Input.GetMouseButtonDown(0)) {
            SquareRootCPU();
        }

        if (Input.GetMouseButtonDown(1)) {
            SquareRootComputeShader();
        }
    }
}
