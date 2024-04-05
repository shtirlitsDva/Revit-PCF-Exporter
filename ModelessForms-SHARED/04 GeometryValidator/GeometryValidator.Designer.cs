namespace ModelessForms.GeometryValidator
{
    partial class GeometryValidatorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.Select = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.button_Update = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.comboBox_systemList = new System.Windows.Forms.ComboBox();
            this.tabControl1.SuspendLayout();
            this.Select.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.Select);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(827, 1304);
            this.tabControl1.TabIndex = 0;
            // 
            // Select
            // 
            this.Select.BackColor = System.Drawing.Color.Silver;
            this.Select.Controls.Add(this.tableLayoutPanel3);
            this.Select.Controls.Add(this.tableLayoutPanel1);
            this.Select.Location = new System.Drawing.Point(8, 39);
            this.Select.Margin = new System.Windows.Forms.Padding(4);
            this.Select.Name = "Select";
            this.Select.Padding = new System.Windows.Forms.Padding(4);
            this.Select.Size = new System.Drawing.Size(811, 1257);
            this.Select.TabIndex = 0;
            this.Select.Text = "Validate";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.Controls.Add(this.treeView1, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.button_Update, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.comboBox_systemList, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(4, 4);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 3;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(803, 1249);
            this.tableLayoutPanel3.TabIndex = 5;
            // 
            // treeView1
            // 
            this.treeView1.BackColor = System.Drawing.Color.Silver;
            this.treeView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.treeView1.Location = new System.Drawing.Point(4, 4);
            this.treeView1.Margin = new System.Windows.Forms.Padding(4);
            this.treeView1.Name = "treeView1";
            this.treeView1.PathSeparator = ".";
            this.treeView1.Size = new System.Drawing.Size(795, 1120);
            this.treeView1.TabIndex = 3;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // button_Update
            // 
            this.button_Update.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button_Update.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.button_Update.Location = new System.Drawing.Point(3, 1176);
            this.button_Update.Name = "button_Update";
            this.button_Update.Size = new System.Drawing.Size(797, 70);
            this.button_Update.TabIndex = 4;
            this.button_Update.Text = "UPDATE";
            this.button_Update.UseVisualStyleBackColor = true;
            this.button_Update.Click += new System.EventHandler(this.button_Update_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(807, 4);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1249F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(0, 1249);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(8, 39);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage2.Size = new System.Drawing.Size(811, 1257);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // comboBox_systemList
            // 
            this.comboBox_systemList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBox_systemList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_systemList.FormattingEnabled = true;
            this.comboBox_systemList.Location = new System.Drawing.Point(3, 1131);
            this.comboBox_systemList.Name = "comboBox_systemList";
            this.comboBox_systemList.Size = new System.Drawing.Size(797, 33);
            this.comboBox_systemList.TabIndex = 5;
            // 
            // GeometryValidatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 1304);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "GeometryValidatorForm";
            this.Text = "Validate geometry";
            this.tabControl1.ResumeLayout(false);
            this.Select.ResumeLayout(false);
            this.Select.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage Select;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button button_Update;
        private System.Windows.Forms.ComboBox comboBox_systemList;
    }
}