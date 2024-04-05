using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MEPUtils.SelectByGuid
{
    public partial class InputBox : Form
    {
        public bool Execute = false;
        public string GUID = string.Empty;

        public InputBox()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Execute = true;
            GUID = textBox1.Text;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Execute = false;
            this.Close();
        }
    }
}
