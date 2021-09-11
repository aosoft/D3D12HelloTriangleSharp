using System;
using System.Runtime.InteropServices;
using SharpDX;
using D3D12 = SharpDX.Direct3D12;

namespace D3D12HelloTriangleSharp
{
    public sealed class ShaderConstantBuffer : IDisposable
    {
        private BufferLayout _buffer;
        private readonly D3D12.Resource _constantBuffer;
        private readonly D3D12.DescriptorHeap _cbvHeap;

        private static readonly int _bufferSize = (Marshal.SizeOf<BufferLayout>() + 255) & ~255;
        
        public float Ratio
        {
            get => _buffer.Ratio;
            set => _buffer.Ratio = value;
        }

        public ShaderConstantBuffer(D3D12.Device device)
        {
            _constantBuffer = device.CreateCommittedResource(new D3D12.HeapProperties(D3D12.HeapType.Upload),
                D3D12.HeapFlags.None,
                D3D12.ResourceDescription.Buffer(_bufferSize),
                D3D12.ResourceStates.GenericRead);
            _cbvHeap = device.CreateDescriptorHeap(new D3D12.DescriptorHeapDescription
            {
                Type = D3D12.DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                DescriptorCount = 1,
                Flags = D3D12.DescriptorHeapFlags.None,
                NodeMask = 0
            });
            device.CreateConstantBufferView(new D3D12.ConstantBufferViewDescription
            {
                BufferLocation = 0,
                SizeInBytes = _bufferSize
            }, _cbvHeap.CPUDescriptorHandleForHeapStart);
        }

        public void Dispose()
        {
            _constantBuffer.Dispose();
            _cbvHeap.Dispose();
        }

        public void Update()
        {
            var mapped = _constantBuffer.Map(0, new D3D12.Range());
            try
            {
                Utilities.Write(mapped, ref _buffer);
            }
            finally
            {
                _constantBuffer.Unmap(0);
            }
        }

        public D3D12.CpuDescriptorHandle CbvCpuDescriptorHandle => _cbvHeap.CPUDescriptorHandleForHeapStart;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct BufferLayout
        {
            public float Ratio;
        }
    }
}