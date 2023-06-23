using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentsCreator : MonoBehaviour {

    public RenderTexture _renderTexture;

    [SerializeField] private ComputeShader _agentComputeShader;

    private struct Agent {
        public Vector2 position;
        public Vector2 direction;
        public float speed;
        public Vector4 color;
        public int size;
    }

    private Agent[] _agents;

    private void Start () {
        _renderTexture = new RenderTexture(1920, 1080, 24);
        _renderTexture.enableRandomWrite = true;
        _renderTexture.Create();

        _agents = new Agent[16384];

        for (int i = 0; i < _agents.Length; i++) {
            _agents[i].position = new Vector2(Random.value * _renderTexture.width, Random.value * _renderTexture.height);
            _agents[i].direction = new Vector2(Random.value - .5f, Random.value - .5f).normalized;
            _agents[i].speed = Random.value * 50f;

            _agents[i].size = (int)(Random.value * 5f + 1);
            Color color = Random.ColorHSV();
            _agents[i].color = new Vector3(color.r, color.g, color.b);
        }

        int size = sizeof(float) * 9 + sizeof(int);
        ComputeBuffer computeBuffer = new ComputeBuffer(_agents.Length, size);
        computeBuffer.SetData(_agents);

        _agentComputeShader.SetBuffer(0, "agents", computeBuffer);

        _agentComputeShader.SetFloat("deltaTime", Time.deltaTime);
        _agentComputeShader.SetTexture(0, "Result", _renderTexture);
        _agentComputeShader.Dispatch(0, _agents.Length / 1024, 1, 1);

        computeBuffer.GetData(_agents);
        computeBuffer.Dispose();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(_renderTexture, destination);
    }

    private void Update() {
        int size = sizeof(float) * 9 + sizeof(int);
        ComputeBuffer computeBuffer = new ComputeBuffer(_agents.Length, size);
        computeBuffer.SetData(_agents);

        _agentComputeShader.SetBuffer(0, "agents", computeBuffer);
        _agentComputeShader.SetFloat("deltaTime", Time.deltaTime);

        _agentComputeShader.SetTexture(0, "Result", _renderTexture);
        _agentComputeShader.Dispatch(0, _agents.Length / 1024, 1, 1);

        computeBuffer.GetData(_agents);
        computeBuffer.Dispose();
    }

}
