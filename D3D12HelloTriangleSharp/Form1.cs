using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace D3D12HelloTriangleSharp
{
    public partial class Form1 : Form
    {
        private D3D12HelloTriangle? _renderer;
        
        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _renderer = new D3D12HelloTriangle(Handle, Width, Height, false);
        }

        protected override void OnClosed(EventArgs e)
        {
            _renderer?.Dispose();
            base.OnClosed(e);
        }
    }
}