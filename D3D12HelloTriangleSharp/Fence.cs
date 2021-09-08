using System;
using System.Threading;
using D3D12 = SharpDX.Direct3D12;

namespace D3D12HelloTriangleSharp
{
    public sealed class Fence : IDisposable
    {
        private D3D12.CommandQueue _commandQueue;
        private EventWaitHandle _fenceEvent;
        private D3D12.Fence _fence;
        private long _fenceValue;

        public Fence(D3D12.Device device, D3D12.CommandQueue commandQueue)
        {
            _commandQueue = commandQueue;
            _fence = device.CreateFence(0, D3D12.FenceFlags.None);
            _fenceValue = 1;
            _fenceEvent = new AutoResetEvent(false);
        }
        
        public void Dispose()
        {
            _fence.Dispose();
            _fenceEvent.Dispose();
        }
        
        public void WaitForPreviousFrame()
        {
            var fence = _fenceValue;
            _commandQueue.Signal(_fence, fence);
            _fenceValue++;
            if (_fence.CompletedValue < fence)
            {
                _fence.SetEventOnCompletion(fence, _fenceEvent.SafeWaitHandle.DangerousGetHandle());
                _fenceEvent.WaitOne();
            }
        }
    }
}