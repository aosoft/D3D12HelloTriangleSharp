using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using D3D = SharpDX.Direct3D;
using DXGI = SharpDX.DXGI;
using D3D12 = SharpDX.Direct3D12;
using D3DCompiler = SharpDX.D3DCompiler;
using SharpDX.Mathematics;

namespace D3D12HelloTriangleSharp
{
    public sealed class D3D12HelloTriangle : IDisposable
    {
        private const int FrameCount = 2;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct Vertex
        {
            public Vector3 Position;
            public Vector4 Color;
        };

        // Pipeline objects.
        private D3D12.Viewport _viewport;
        private Rectangle _scissorRect;
        private DXGI.SwapChain3? _swapChain;
        private D3D12.Device? _device;
        private D3D12.Resource?[] _renderTargets = new D3D12.Resource?[FrameCount];
        private D3D12.CommandAllocator? _commandAllocator;
        private D3D12.CommandQueue? _commandQueue;
        private D3D12.RootSignature? _rootSignature;
        private D3D12.DescriptorHeap? _rtvHeap;
        private D3D12.PipelineState? _pipelineState;
        private D3D12.GraphicsCommandList? _commandList;
        private int _rtvDescriptorSize;

        // App resources.
        private D3D12.Resource? _vertexBuffer;
        private D3D12.VertexBufferView _vertexBufferView;

        // Synchronization objects.
        private int _frameIndex;
        private EventWaitHandle? _fenceEvent;
        private D3D12.Fence? _fence;
        private long _fenceValue;


        public D3D12HelloTriangle(IntPtr windowHandle, int width, int height, bool useWarpDevice)
        {
            _frameIndex = 0;
            _viewport = new()
            {
                TopLeftX = 0,
                TopLeftY = 0,
                Width = width,
                Height = height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            };

            _scissorRect = new()
            {
                Left = 0,
                Top = 0,
                Right = width,
                Bottom = height,
            };

            try
            {
                LoadPipeline(windowHandle, width, height, useWarpDevice);
                LoadAssets(width, height);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void LoadPipeline(IntPtr windowHandle, int width, int height, bool useWarpDevice)
        {
            bool debug = false;
#if DEBUG
            var debugController = D3D12.DebugInterface.Get();
            if (debugController != null)
            {
                debugController.EnableDebugLayer();
                debug = true;
            }
#endif
            using var factory2 = new DXGI.Factory2(debug);
            using var factory = factory2.QueryInterface<DXGI.Factory4>();
            if (useWarpDevice)
            {
                using var warpAdapter = factory.GetWarpAdapter();
                _device = new D3D12.Device(warpAdapter, D3D.FeatureLevel.Level_11_0);
            }
            else
            {
                using var hardwareAdapter = GetHardwareAdapter(factory);
                _device = new D3D12.Device(hardwareAdapter, D3D.FeatureLevel.Level_11_0);
            }

            var queueDesc = new D3D12.CommandQueueDescription
            {
                Type = D3D12.CommandListType.Direct,
                Priority = 0,
                Flags = D3D12.CommandQueueFlags.None,
                NodeMask = 0
            };
            _commandQueue = _device.CreateCommandQueue(queueDesc);

            var swapChainDesc = new DXGI.SwapChainDescription1
            {
                Width = width,
                Height = height,
                Format = DXGI.Format.R8G8B8A8_UNorm,
                Stereo = default,
                SampleDescription = new DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = DXGI.Usage.RenderTargetOutput,
                BufferCount = FrameCount,
                Scaling = DXGI.Scaling.Stretch,
                SwapEffect = DXGI.SwapEffect.FlipDiscard,
                AlphaMode = DXGI.AlphaMode.Unspecified,
                Flags = DXGI.SwapChainFlags.None
            };
            using var swapChain = new DXGI.SwapChain1(factory, _commandQueue, windowHandle, ref swapChainDesc);
            _swapChain = swapChain.QueryInterface<DXGI.SwapChain3>();
            factory.MakeWindowAssociation(windowHandle, DXGI.WindowAssociationFlags.IgnoreAltEnter);
            _frameIndex = _swapChain.CurrentBackBufferIndex;

            var rtvHeapDesc = new D3D12.DescriptorHeapDescription
            {
                Type = D3D12.DescriptorHeapType.RenderTargetView,
                DescriptorCount = FrameCount,
                Flags = D3D12.DescriptorHeapFlags.None,
                NodeMask = 0
            };
            _rtvHeap = _device.CreateDescriptorHeap(rtvHeapDesc);
            _rtvDescriptorSize =
                _device.GetDescriptorHandleIncrementSize(D3D12.DescriptorHeapType.RenderTargetView);

            var rtvHandle = _rtvHeap.CPUDescriptorHandleForHeapStart;
            for (int i = 0; i < FrameCount; i++)
            {
                _renderTargets[i] = _swapChain.GetBackBuffer<D3D12.Resource>(i);
                _device.CreateRenderTargetView(_renderTargets[i], null, rtvHandle);
                rtvHandle += _rtvDescriptorSize;
            }

            _commandAllocator = _device.CreateCommandAllocator(D3D12.CommandListType.Direct);
        }

        private void LoadAssets(int width, int height)
        {
            if (_device == null)
            {
                throw new InvalidOperationException();
            }

            var rootSignatureDesc =
                new D3D12.RootSignatureDescription(D3D12.RootSignatureFlags.AllowInputAssemblerInputLayout);
            using (var signature = rootSignatureDesc.Serialize())
            {
                _rootSignature = _device.CreateRootSignature(signature);
            }

            var compileFlags =
#if DEBUG
                D3DCompiler.ShaderFlags.Debug | D3DCompiler.ShaderFlags.SkipOptimization;
#else
                D3DCompiler.ShaderFlags.None;
#endif

            using var vertexShader = D3DCompiler.ShaderBytecode.Compile(Hlsl, "VSMain", "vs_5_0", compileFlags);
            using var pixelShader = D3DCompiler.ShaderBytecode.Compile(Hlsl, "PSMain", "ps_5_0", compileFlags);

            var psoDesc = new D3D12.GraphicsPipelineStateDescription
            {
                RootSignature = _rootSignature,
                VertexShader = vertexShader.Bytecode.Data,
                PixelShader = pixelShader.Bytecode.Data,
                BlendState = D3D12.BlendStateDescription.Default(),
                SampleMask = -1,
                RasterizerState = D3D12.RasterizerStateDescription.Default(),
                DepthStencilState = new D3D12.DepthStencilStateDescription
                {
                    IsDepthEnabled = false,
                    IsStencilEnabled = false,
                },
                InputLayout = new D3D12.InputElement[]
                {
                    new()
                    {
                        SemanticName = "POSITION",
                        SemanticIndex = 0,
                        Format = DXGI.Format.R32G32B32_Float,
                        Slot = 0,
                        AlignedByteOffset = 0,
                        Classification = D3D12.InputClassification.PerVertexData,
                        InstanceDataStepRate = 0
                    },
                    new()
                    {
                        SemanticName = "COLOR",
                        SemanticIndex = 0,
                        Format = DXGI.Format.R32G32B32_Float,
                        Slot = 0,
                        AlignedByteOffset = 12,
                        Classification = D3D12.InputClassification.PerVertexData,
                        InstanceDataStepRate = 0
                    },
                },
                IBStripCutValue = D3D12.IndexBufferStripCutValue.Disabled,
                PrimitiveTopologyType = D3D12.PrimitiveTopologyType.Triangle,
                RenderTargetCount = 1,
                DepthStencilFormat = DXGI.Format.D32_Float,
                SampleDescription = new DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },
                NodeMask = 0,
                Flags = D3D12.PipelineStateFlags.None
            };
            psoDesc.RenderTargetFormats[0] = DXGI.Format.R8G8B8A8_UNorm;

            _pipelineState = _device.CreateGraphicsPipelineState(psoDesc);
            _commandList =
                _device.CreateCommandList(0, D3D12.CommandListType.Direct, _commandAllocator, _pipelineState);
            _commandList.Close();

            var aspectRatio = (float)width / height;

            var triangleVertices = new Vertex[]
            {
                new()
                {
                    Position = new(0.0f, 0.25f * aspectRatio, 0.0f),
                    Color = new(1.0f, 0.0f, 0.0f, 1.0f)
                },
                new()
                {
                    Position = new(0.25f, -0.25f * aspectRatio, 0.0f),
                    Color = new(0.0f, 1.0f, 0.0f, 1.0f)
                },
                new()
                {
                    Position = new(-0.25f, -0.25f * aspectRatio, 0.0f),
                    Color = new(0.0f, 0.0f, 1.0f, 1.0f)
                },
            };

            var vertexBufferSize = Utilities.SizeOf(triangleVertices);

            _vertexBuffer = _device.CreateCommittedResource(new D3D12.HeapProperties(D3D12.HeapType.Upload),
                D3D12.HeapFlags.None,
                D3D12.ResourceDescription.Buffer(vertexBufferSize),
                D3D12.ResourceStates.GenericRead);
            var vertexDataBegin = _vertexBuffer.Map(0, new D3D12.Range
            {
                Begin = 0,
                End = 0
            });
            try
            {
                Utilities.Write(vertexDataBegin, triangleVertices, 0, triangleVertices.Length);
            }
            finally
            {
                _vertexBuffer.Unmap(0);
            }

            _vertexBufferView = new D3D12.VertexBufferView
            {
                BufferLocation = _vertexBuffer.GPUVirtualAddress,
                SizeInBytes = vertexBufferSize,
                StrideInBytes = Utilities.SizeOf<Vertex>()
            };

            _fence = _device.CreateFence(0, D3D12.FenceFlags.None);
            _fenceValue = 1;
            _fenceEvent = new AutoResetEvent(false);

            WaitForPreviousFrame();
        }

        public void Dispose()
        {
            foreach (var rt in _renderTargets)
            {
                rt?.Dispose();
            }

            _commandAllocator?.Dispose();
            _commandQueue?.Dispose();
            _rootSignature?.Dispose();
            _rtvHeap?.Dispose();
            _pipelineState?.Dispose();
            _commandList?.Dispose();
            _vertexBuffer?.Dispose();
            _fence?.Dispose();
            _fenceEvent?.Dispose();

            _swapChain?.Dispose();
            _device?.Dispose();
        }

        private void WaitForPreviousFrame()
        {
            if (_commandQueue == null || _fence == null || _fenceEvent == null || _swapChain == null)
            {
                return;
            }
            var fence = _fenceValue;
            _commandQueue.Signal(_fence, fence);
            _fenceValue++;
            if (_fence.CompletedValue < fence)
            {
                _fence.SetEventOnCompletion(fence, _fenceEvent.SafeWaitHandle.DangerousGetHandle());
                _fenceEvent.WaitOne();
            }

            _frameIndex = _swapChain.CurrentBackBufferIndex;
        }


        private static DXGI.Adapter1? GetHardwareAdapter(DXGI.Factory1 factory)
        {
            var count = factory.GetAdapterCount1();
            for (int i = 0; i < count; i++)
            {
                using var adapter = factory.GetAdapter1(i);
                var desc = adapter.Description1;
                if ((desc.Flags & DXGI.AdapterFlags.Software) != 0)
                {
                    continue;
                }

                using var device = new D3D12.Device(adapter, D3D.FeatureLevel.Level_11_0);
                return adapter.QueryInterface<DXGI.Adapter1>();
            }

            return null;
        }

        private static readonly string Hlsl = @"
struct PSInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

PSInput VSMain(float4 position : POSITION, float4 color : COLOR)
{
    PSInput result;

    result.position = position;
    result.color = color;

    return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return input.color;
}
";
    }
}