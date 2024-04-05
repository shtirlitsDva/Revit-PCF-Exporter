namespace MEPUtils.SetMark
{
    partial class InputBoxBasic
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(54, 12);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(95, 20);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "200";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox1.Enter += new System.EventHandler(this.TextBox1_Enter);
            this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);
            this.textBox1.Leave += new System.EventHandler(this.TextBox1_Leave);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(54, 38);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(95, 20);
            this.textBox2.TabIndex = 1;
            this.textBox2.Text = "200";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox2.Enter += new System.EventHandler(this.TextBox2_Enter);
            this.textBox2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox2_KeyDown);
            this.textBox2.Leave += new System.EventHandler(this.TextBox2_Leave);
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(54, 64);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(95, 20);
            this.textBox3.TabIndex = 2;
            this.textBox3.Text = "200";
            this.textBox3.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox3.Enter += new System.EventHandler(this.TextBox3_Enter);
            this.textBox3.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox3_KeyDown);
            this.textBox3.Leave += new System.EventHandler(this.TextBox3_Leave);
            // 
            // textBox4
            // 
            this.textBox4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox4.Location = new System.Drawing.Point(12, 12);
            this.textBox4.Name = "textBox4";
            this.textBox4.ReadOnly = true;
            this.textBox4.Size = new System.Drawing.Size(36, 13);
            this.textBox4.TabIndex = 10;
            this.textBox4.TabStop = false;
            this.textBox4.Text = "Tag 1:";
            this.textBox4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox5
            // 
            this.textBox5.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox5.Location = new System.Drawing.Point(12, 41);
            this.textBox5.Name = "textBox5";
            this.textBox5.ReadOnly = true;
            this.textBox5.Size = new System.Drawing.Size(36, 13);
            this.textBox5.TabIndex = 11;
            this.textBox5.TabStop = false;
            this.textBox5.Text = "Tag 2:";
            this.textBox5.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox6
            // 
            this.textBox6.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox6.Location = new System.Drawing.Point(12, 67);
            this.textBox6.Name = "textBox6";
            this.textBox6.ReadOnly = true;
            this.textBox6.Size = new System.Drawing.Size(36, 13);
            this.textBox6.TabIndex = 12;
            this.textBox6.TabStop = false;
            this.textBox6.Text = "Tag 3:";
            this.textBox6.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // InputBoxBasic
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(171, 97);
            this.Controls.Add(this.textBox6);
            this.Controls.Add(this.textBox5);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Name = "InputBoxBasic";
            this.Text = "12";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.InputBoxBasic_FormClosing);
            this.Shown += new System.EventHandler(this.InputBoxBasic_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.TextBox textBox6;
    }
}