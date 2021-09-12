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
            _renderer = new D3D12HelloTriangle(panel1.Handle, panel1.Width, panel1.Height, false);
        }

        protected override void OnClosed(EventArgs e)
        {
            _renderer?.OnDestroy();
            _renderer?.Dispose();
            base.OnClosed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _renderer?.OnRender();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (_renderer != null)
            {
                _renderer.Ratio = (float)trackBar1.Value / 100.0f;
                _renderer.OnRender();
                System.Threading.Thread.Sleep(20);
            }
        }
    }
}