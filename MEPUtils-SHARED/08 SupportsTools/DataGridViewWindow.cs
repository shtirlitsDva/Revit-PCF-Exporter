using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MEPUtils.SupportTools
{
    public partial class DataGridViewWindow : Form
    {
        private DataTable CompareSupports;

        public DataGridViewWindow(DataTable compareSupports)
        {
            InitializeComponent();
            this.CompareSupports = compareSupports;
            dataGridView1.DataSource = CompareSupports;
        }
    }
}
