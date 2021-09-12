using System;

namespace D3D12HelloTriangleSharp
{
    public sealed class D3D12HelloTriangle : IDisposable
    {
        private const int FrameCount = 2;
        
        private GraphicsDevice _device;
        private GraphicsPipeline _pipeline;
        private Shader _shader = new Shader();
        private ShaderConstantBuffer _cb;
        private Fence _fence;

        private Display _display;
        
        public D3D12HelloTriangle(IntPtr windowHandle, int width, int height, bool useWarpDevice)
        {
            _device = new GraphicsDevice(useWarpDevice);
            _pipeline = new GraphicsPipeline(_device, width, height, _shader);
            _cb = new ShaderConstantBuffer(_device.Device);
            _fence = new Fence(_device);
            
            _display = new Display(_device, windowHandle, width, height, FrameCount);
        }
        
        public void Dispose()
        {
            _pipeline.Dispose();
            _device.Dispose();
            _cb.Dispose();
            _fence.Dispose();
        }

        public float Ratio
        {
            get => _cb.Ratio;
            set => _cb.Ratio = value;
        }

        public void OnRender()
        {
            var frameIndex = _display.SwapChain.CurrentBackBufferIndex;

            _cb.Update();
            _pipeline.PopulateCommandList(_device.CommandAllocator, _display.RenderTargets[frameIndex],
                _display.GetRtvCpuDescriptorHandle(frameIndex), _cb);
            _device.CommandQueue.ExecuteCommandList(_pipeline.CommandList);
            _display.SwapChain.Present(1, 0);
            _fence.WaitForPreviousFrame();
        }
        
        public void OnDestroy()
        {
            _fence.WaitForPreviousFrame();
            _fence.Dispose();
        }
    }
}