using System;
using SharpDX;
using D3D = SharpDX.Direct3D;
using DXGI = SharpDX.DXGI;
using D3D12 = SharpDX.Direct3D12;
using SharpDX.Mathematics;

namespace D3D12HelloTriangleSharp
{
    public sealed class D3D12HelloTriangle : IDisposable
    {
        private const int FrameCount = 2;
        
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
        private IntPtr _fenceEvent;
        private D3D12.Fence? _fence;
        private ulong _fenceValue;


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
        
        public void Dispose()
        {
            _swapChain?.Dispose();
            _device?.Dispose();
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

    }
}