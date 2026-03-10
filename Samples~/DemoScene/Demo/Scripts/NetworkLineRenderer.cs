using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Caskev.NetcodeForXRInteractionToolkitSamples.DemoScene
{
    public class NetworkLineRenderer : NetworkBehaviour
    {
        private struct Vector3Array : INetworkSerializable
        {
            public Vector3[] points;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                if (serializer.IsWriter)
                {
                    FastBufferWriter buffer = serializer.GetFastBufferWriter();
                    buffer.WriteValueSafe(points.Length);
                    for (int i = 0; i < points.Length; i++)
                    {
                        buffer.WriteValueSafe(points[i]);
                    }
                }
                if (serializer.IsReader)
                {
                    FastBufferReader buffer = serializer.GetFastBufferReader();
                    buffer.ReadValueSafe(out int length);
                    points = new Vector3[length];
                    for (int i = 0; i < length; i++)
                    {
                        buffer.ReadValueSafe(out Vector3 vector3);
                        points[i] = vector3;
                    }
                }
            }
        }
        private struct LineColorGradient : INetworkSerializable
        {
            public GradientMode mode;
            public ColorSpace colorSpace;
            public GradientAlphaKey[] alphas;
            public GradientColorKey[] colors;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                if (serializer.IsWriter)
                {
                    FastBufferWriter buffer = serializer.GetFastBufferWriter();
                    buffer.WriteValueSafe((byte)mode);
                    buffer.WriteValueSafe((byte)(((int)colorSpace) + 1));
                    buffer.WriteValueSafe((byte)alphas.Length);
                    for (int i = 0; i < alphas.Length; i++)
                    {
                        buffer.WriteValueSafe(alphas[i].time);
                        buffer.WriteValueSafe(alphas[i].alpha);
                    }
                    buffer.WriteValueSafe((byte)colors.Length);
                    for (int i = 0; i < colors.Length; i++)
                    {
                        buffer.WriteValueSafe(colors[i].time);
                        buffer.WriteValueSafe(colors[i].color);
                    }
                }

                if (serializer.IsReader)
                {
                    FastBufferReader buffer = serializer.GetFastBufferReader();
                    buffer.ReadValueSafe(out byte modeByte);
                    mode = (GradientMode)modeByte;
                    buffer.ReadValueSafe(out byte colorSpaceByte);
                    colorSpace = (ColorSpace)(((int)colorSpaceByte) - 1);
                    buffer.ReadValueSafe(out byte alphasLength);
                    alphas = new GradientAlphaKey[alphasLength];
                    for (int i = 0; i < alphas.Length; i++)
                    {
                        buffer.ReadValueSafe(out float time);
                        buffer.ReadValueSafe(out float alpha);
                        alphas[i] = new GradientAlphaKey(alpha, time);
                    }
                    buffer.ReadValueSafe(out byte colorsLength);
                    colors = new GradientColorKey[colorsLength];
                    for (int i = 0; i < colors.Length; i++)
                    {
                        buffer.ReadValueSafe(out float time);
                        buffer.ReadValueSafe(out Color color);
                        colors[i] = new GradientColorKey(color, time);
                    }
                }
            }
            public Gradient ToGradient()
            {
                return new Gradient() { mode = mode, colorSpace = colorSpace, alphaKeys = alphas == null ? new GradientAlphaKey[0] : alphas.Select(x => x).ToArray(), colorKeys = colors == null ? new GradientColorKey[0] : colors.Select(x => x).ToArray() };
            }
            public static LineColorGradient FromGradient(Gradient gradient)
            {
                return new LineColorGradient() { mode = gradient.mode, colorSpace = gradient.colorSpace, alphas = gradient.alphaKeys == null ? new GradientAlphaKey[0] : gradient.alphaKeys.Select(x => x).ToArray(), colors = gradient.colorKeys == null ? new GradientColorKey[0] : gradient.colorKeys.Select(x => x).ToArray() };
            }
        }
        [SerializeField] private LineRenderer _lineRenderer;
        private LineRenderer _sourceLineRenderer;
        private NetworkVariable<bool> _enabled = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> _widthMultiplier = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<LineColorGradient> _colorGradient = new NetworkVariable<LineColorGradient>(new LineColorGradient() { alphas = new GradientAlphaKey[0], colors = new GradientColorKey[0] }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3Array> _points = new NetworkVariable<Vector3Array>(new Vector3Array() { points = new Vector3[0] }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public LineRenderer SourceLineRenderer { get => _sourceLineRenderer; set => _sourceLineRenderer = value; }

        private void Awake()
        {
            if (IsOwner)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) { return; }

            if (_sourceLineRenderer.enabled != _enabled.Value)
            {
                _enabled.Value = _sourceLineRenderer.enabled;
            }

            if (_sourceLineRenderer.widthMultiplier != _widthMultiplier.Value)
            {
                _widthMultiplier.Value = _sourceLineRenderer.widthMultiplier;
            }

            if (!CompareGradients(_sourceLineRenderer.colorGradient, _colorGradient.Value.ToGradient()))
            {
                _colorGradient.Value = LineColorGradient.FromGradient(_sourceLineRenderer.colorGradient);
            }

            Vector3[] positions = new Vector3[_sourceLineRenderer.positionCount];
            _sourceLineRenderer.GetPositions(positions);
            if (positions.Length != _points.Value.points.Length || !CompareVector3Arrays(positions, _points.Value.points))
            {
                _points.Value = new Vector3Array() { points = positions };
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
            {
                _enabled.OnValueChanged += OnEnableChanged;
                _widthMultiplier.OnValueChanged += OnWidthMultiplierChanged;
                _points.OnValueChanged += OnPointsUpdated;
                _colorGradient.OnValueChanged += OnColorGradientUpdated;
            }
        }

        private void OnColorGradientUpdated(LineColorGradient previousValue, LineColorGradient newValue)
        {
            _lineRenderer.colorGradient = newValue.ToGradient();
        }

        private void OnWidthMultiplierChanged(float previousValue, float newValue)
        {
            _lineRenderer.widthMultiplier = newValue;
        }

        private void OnEnableChanged(bool previousValue, bool newValue)
        {
            _lineRenderer.enabled = newValue;
        }

        private void OnPointsUpdated(Vector3Array previousValue, Vector3Array newValue)
        {
            _lineRenderer.positionCount = newValue.points.Length;
            _lineRenderer.SetPositions(newValue.points);
        }




        private bool CompareVector3Arrays(Vector3[] arrayA, Vector3[] arrayB)
        {
            for (int i = 0; i < arrayA.Length; i++)
            {
                if (arrayA[i] != arrayB[i])
                {
                    return false;
                }
            }
            return true;
        }
        private bool CompareGradients(Gradient gradientA, Gradient gradientB)
        {
            if (gradientA.mode != gradientB.mode || gradientA.colorSpace != gradientB.colorSpace)
            {
                return false;
            }

            if (gradientA.alphaKeys.Length != gradientB.alphaKeys.Length || gradientA.colorKeys.Length != gradientB.colorKeys.Length)
            {
                return false;
            }

            for (int i = 0; i < gradientA.alphaKeys.Length; i++)
            {
                if (!Mathf.Approximately(gradientA.alphaKeys[i].time, gradientB.alphaKeys[i].time) || !Mathf.Approximately(gradientA.alphaKeys[i].alpha, gradientB.alphaKeys[i].alpha))
                {
                    return false;
                }
            }

            for (int i = 0; i < gradientA.colorKeys.Length; i++)
            {
                if (!Mathf.Approximately(gradientA.colorKeys[i].time, gradientB.colorKeys[i].time) || !CompareColors(gradientA.colorKeys[i].color, gradientB.colorKeys[i].color))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareColors(Color colorA, Color colorB)
        {
            return Mathf.Approximately(colorA.a, colorB.a) && Mathf.Approximately(colorA.r, colorB.r) && Mathf.Approximately(colorA.g, colorB.g) && Mathf.Approximately(colorA.b, colorB.b);
        }
    }
}
