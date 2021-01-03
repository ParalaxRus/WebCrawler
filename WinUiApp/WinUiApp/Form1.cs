using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinUiApp
{
    public partial class Form1 : Form
    {
        public bool IsClosed { get; set; }

        public Form1()
        {
            InitializeComponent();
            this.IsClosed = false;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.IsClosed = true;
        }
    }
}
