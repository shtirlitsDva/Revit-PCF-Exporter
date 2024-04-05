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
using stgs = MEPUtils.Properties.Settings;

namespace MEPUtils.SetMark
{
    public partial class InputBoxBasic : Form
    {
        public string Tag1Value;
        public string Tag2Value;
        public string Tag3Value;

        public InputBoxBasic()
        {
            InitializeComponent();
        }

        private void InputBoxBasic_Shown(object sender, EventArgs e)
        {
            textBox1.Text = Tag1Value;
            textBox2.Text = Tag2Value;
            textBox3.Text = Tag3Value;
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = textBox1.Text.Length;
        }

        private void InputBoxBasic_FormClosing(object sender, FormClosingEventArgs e)
        {
            Tag1Value = textBox1.Text;
            Tag2Value = textBox2.Text;
            Tag3Value = textBox3.Text;
        }

        private void textBox1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) this.Close();
        }

        private void TextBox2_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) this.Close();
        }

        private void TextBox3_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) this.Close();
        }

        private void TextBox1_Enter(object sender, EventArgs e)
        {
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = textBox1.Text.Length;
        }

        private void TextBox1_Leave(object sender, EventArgs e)
        {
            textBox1.SelectionLength = 0;
        }

        private void TextBox2_Enter(object sender, EventArgs e)
        {
            textBox2.SelectionStart = 0;
            textBox2.SelectionLength = textBox2.Text.Length;
        }

        private void TextBox2_Leave(object sender, EventArgs e)
        {
            textBox2.SelectionLength = 0;
        }

        private void TextBox3_Enter(object sender, EventArgs e)
        {
            textBox3.SelectionStart = 0;
            textBox3.SelectionLength = textBox3.Text.Length;
        }

        private void TextBox3_Leave(object sender, EventArgs e)
        {
            textBox3.SelectionLength = 0;
        }

       
    }
}
