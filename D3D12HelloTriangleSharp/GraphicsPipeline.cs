using System;
using SharpDX;
using D3D = SharpDX.Direct3D;
using D3D12 = SharpDX.Direct3D12;
using DXGI = SharpDX.DXGI;

namespace D3D12HelloTriangleSharp
{
    public sealed class GraphicsPipeline : IDisposable
    {
        public GraphicsPipeline(GraphicsDevice device, int width, int height, Assets assets)
        {
            var rootSignatureDesc =
                new D3D12.RootSignatureDescription(D3D12.RootSignatureFlags.AllowInputAssemblerInputLayout);
            using (var signature = rootSignatureDesc.Serialize())
            {
                RootSignature = device.Device.CreateRootSignature(signature);
            }
            
            var psoDesc = new D3D12.GraphicsPipelineStateDescription
            {
                RootSignature = RootSignature,
                VertexShader = assets.VertexShader,
                PixelShader = assets.PixelShader,
                StreamOutput = new D3D12.StreamOutputDescription(),
                BlendState = D3D12.BlendStateDescription.Default(),
                SampleMask = -1,
                RasterizerState = D3D12.RasterizerStateDescription.Default(),
                DepthStencilState = new D3D12.DepthStencilStateDescription
                {
                    IsDepthEnabled = false,
                    IsStencilEnabled = false,
                },
                InputLayout = assets.InputLayout,
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
            PipelineState = device.Device.CreateGraphicsPipelineState(psoDesc);

            var vertices = assets.GetVerticies((float)width / height);
            VertexBuffer = device.Device.CreateCommittedResource(new D3D12.HeapProperties(D3D12.HeapType.Upload),
                D3D12.HeapFlags.None,
                D3D12.ResourceDescription.Buffer(Utilities.SizeOf(vertices)),
                D3D12.ResourceStates.GenericRead);
            var vertexDataBegin = VertexBuffer.Map(0, new D3D12.Range
            {
                Begin = 0,
                End = 0
            });
            try
            {
                Utilities.Write(vertexDataBegin, vertices, 0, vertices.Length);
            }
            finally
            {
                VertexBuffer.Unmap(0);
            }

            VertexBufferView = new D3D12.VertexBufferView
            {
                BufferLocation = VertexBuffer.GPUVirtualAddress,
                SizeInBytes = Utilities.SizeOf(vertices),
                StrideInBytes = Utilities.SizeOf<Vertex>()
            };
        }
        
        public void Dispose()
        {
            
        }
        
        public D3D12.RootSignature RootSignature { get; }
        public D3D12.DescriptorHeap RtvHeap { get; }
        public int RtvDescriptorSize { get; }
        
        public D3D12.PipelineState PipelineState { get; }
        public D3D12.GraphicsCommandList CommandList { get; }
        public D3D12.Resource VertexBuffer { get; }
        public D3D12.VertexBufferView VertexBufferView { get; }

    }
}