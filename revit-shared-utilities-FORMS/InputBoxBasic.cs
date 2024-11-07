using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Shared.Forms
{
    public partial class InputBoxBasic : Form
    {
        public string InputText;

        private string _label;
        private int minWidth = 180;

        public InputBoxBasic()
        {
            InitializeComponent();
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = textBox1.Text.Length;
        }
        public InputBoxBasic(string label) : this()
        {
            label1.AutoSize = true;
            _label = label;

            this.Shown += InputBoxBasic_Shown;
        }

        private void InputBoxBasic_Shown(object sender, EventArgs e)
        {
            label1.Text = _label;

            SuspendLayout();
            this.Height = 90;
            this.Width = Math.Max(minWidth, label1.Width + 80);
            ResumeLayout();

            //this.PerformLayout();
            //this.Refresh();
        }

        //private void textBox1_TextChanged(object sender, EventArgs e) => DistanceToKeep = textBox1.Text;

        private void InputBoxBasic_FormClosing(object sender, FormClosingEventArgs e)
        {
            InputText = textBox1.Text;
        }

        private void textBox1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) this.Close();
            if (e.KeyCode == Keys.Escape) { this.Close(); InputText = ""; }
        }
    }
}
