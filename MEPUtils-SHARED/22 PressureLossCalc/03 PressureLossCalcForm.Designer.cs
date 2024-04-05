namespace MEPUtils.PressureLossCalc
{
    partial class PressureLossCalcForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.textBox_LabelPowerInput = new System.Windows.Forms.TextBox();
            this.textBox_LabelTempSupply = new System.Windows.Forms.TextBox();
            this.textBox_LabelTempReturn = new System.Windows.Forms.TextBox();
            this.textBox_InputTempReturn = new System.Windows.Forms.TextBox();
            this.textBox_InputTempSupply = new System.Windows.Forms.TextBox();
            this.textBox_InputPower = new System.Windows.Forms.TextBox();
            this.textBox_DisplayPower = new System.Windows.Forms.TextBox();
            this.textBox_DisplayTempSupply = new System.Windows.Forms.TextBox();
            this.textBox_DisplayTempReturn = new System.Windows.Forms.TextBox();
            this.textBox_LabelFlow = new System.Windows.Forms.TextBox();
            this.textBox_DisplayFlow = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(309, 405);
            this.button1.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(200, 75);
            this.button1.TabIndex = 4;
            this.button1.Text = "Select folder where to export";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox_LabelPowerInput
            // 
            this.textBox_LabelPowerInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_LabelPowerInput.Location = new System.Drawing.Point(12, 12);
            this.textBox_LabelPowerInput.Name = "textBox_LabelPowerInput";
            this.textBox_LabelPowerInput.ReadOnly = true;
            this.textBox_LabelPowerInput.Size = new System.Drawing.Size(121, 24);
            this.textBox_LabelPowerInput.TabIndex = 5;
            this.textBox_LabelPowerInput.TabStop = false;
            this.textBox_LabelPowerInput.Text = "Power [kW]";
            // 
            // textBox_LabelTempSupply
            // 
            this.textBox_LabelTempSupply.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_LabelTempSupply.Location = new System.Drawing.Point(139, 12);
            this.textBox_LabelTempSupply.Name = "textBox_LabelTempSupply";
            this.textBox_LabelTempSupply.ReadOnly = true;
            this.textBox_LabelTempSupply.Size = new System.Drawing.Size(121, 24);
            this.textBox_LabelTempSupply.TabIndex = 5;
            this.textBox_LabelTempSupply.TabStop = false;
            this.textBox_LabelTempSupply.Text = "T frem [°]";
            // 
            // textBox_LabelTempReturn
            // 
            this.textBox_LabelTempReturn.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_LabelTempReturn.Location = new System.Drawing.Point(266, 12);
            this.textBox_LabelTempReturn.Name = "textBox_LabelTempReturn";
            this.textBox_LabelTempReturn.ReadOnly = true;
            this.textBox_LabelTempReturn.Size = new System.Drawing.Size(121, 24);
            this.textBox_LabelTempReturn.TabIndex = 5;
            this.textBox_LabelTempReturn.TabStop = false;
            this.textBox_LabelTempReturn.Text = "T retur [°]";
            // 
            // textBox_InputTempReturn
            // 
            this.textBox_InputTempReturn.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_InputTempReturn.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::MEPUtils.Properties.Settings.Default, "InputTempReturnText", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox_InputTempReturn.Location = new System.Drawing.Point(266, 52);
            this.textBox_InputTempReturn.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.textBox_InputTempReturn.Name = "textBox_InputTempReturn";
            this.textBox_InputTempReturn.Size = new System.Drawing.Size(113, 31);
            this.textBox_InputTempReturn.TabIndex = 3;
            this.textBox_InputTempReturn.Text = global::MEPUtils.Properties.Settings.Default.InputTempReturnText;
            this.textBox_InputTempReturn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox_InputTempReturn.TextChanged += new System.EventHandler(this.textBox_InputTempReturn_TextChanged);
            // 
            // textBox_InputTempSupply
            // 
            this.textBox_InputTempSupply.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_InputTempSupply.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::MEPUtils.Properties.Settings.Default, "InputTempSupplyText", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox_InputTempSupply.Location = new System.Drawing.Point(139, 52);
            this.textBox_InputTempSupply.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.textBox_InputTempSupply.Name = "textBox_InputTempSupply";
            this.textBox_InputTempSupply.Size = new System.Drawing.Size(113, 31);
            this.textBox_InputTempSupply.TabIndex = 2;
            this.textBox_InputTempSupply.Text = global::MEPUtils.Properties.Settings.Default.InputTempSupplyText;
            this.textBox_InputTempSupply.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox_InputTempSupply.TextChanged += new System.EventHandler(this.textBox_InputTempSupply_TextChanged);
            // 
            // textBox_InputPower
            // 
            this.textBox_InputPower.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_InputPower.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::MEPUtils.Properties.Settings.Default, "InputPowerText", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox_InputPower.Location = new System.Drawing.Point(12, 52);
            this.textBox_InputPower.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.textBox_InputPower.Name = "textBox_InputPower";
            this.textBox_InputPower.Size = new System.Drawing.Size(113, 31);
            this.textBox_InputPower.TabIndex = 1;
            this.textBox_InputPower.Text = global::MEPUtils.Properties.Settings.Default.InputPowerText;
            this.textBox_InputPower.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox_InputPower.TextChanged += new System.EventHandler(this.textBox_PowerInput_TextChanged);
            // 
            // textBox_DisplayPower
            // 
            this.textBox_DisplayPower.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_DisplayPower.Location = new System.Drawing.Point(12, 92);
            this.textBox_DisplayPower.Name = "textBox_DisplayPower";
            this.textBox_DisplayPower.ReadOnly = true;
            this.textBox_DisplayPower.Size = new System.Drawing.Size(113, 24);
            this.textBox_DisplayPower.TabIndex = 5;
            this.textBox_DisplayPower.TabStop = false;
            this.textBox_DisplayPower.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox_DisplayTempSupply
            // 
            this.textBox_DisplayTempSupply.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_DisplayTempSupply.Location = new System.Drawing.Point(139, 92);
            this.textBox_DisplayTempSupply.Name = "textBox_DisplayTempSupply";
            this.textBox_DisplayTempSupply.ReadOnly = true;
            this.textBox_DisplayTempSupply.Size = new System.Drawing.Size(113, 24);
            this.textBox_DisplayTempSupply.TabIndex = 5;
            this.textBox_DisplayTempSupply.TabStop = false;
            this.textBox_DisplayTempSupply.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox_DisplayTempReturn
            // 
            this.textBox_DisplayTempReturn.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_DisplayTempReturn.Location = new System.Drawing.Point(266, 92);
            this.textBox_DisplayTempReturn.Name = "textBox_DisplayTempReturn";
            this.textBox_DisplayTempReturn.ReadOnly = true;
            this.textBox_DisplayTempReturn.Size = new System.Drawing.Size(113, 24);
            this.textBox_DisplayTempReturn.TabIndex = 5;
            this.textBox_DisplayTempReturn.TabStop = false;
            this.textBox_DisplayTempReturn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox_LabelFlow
            // 
            this.textBox_LabelFlow.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_LabelFlow.Location = new System.Drawing.Point(396, 12);
            this.textBox_LabelFlow.Name = "textBox_LabelFlow";
            this.textBox_LabelFlow.ReadOnly = true;
            this.textBox_LabelFlow.Size = new System.Drawing.Size(121, 24);
            this.textBox_LabelFlow.TabIndex = 5;
            this.textBox_LabelFlow.TabStop = false;
            this.textBox_LabelFlow.Text = "Flow [m³/h]";
            // 
            // textBox_DisplayFlow
            // 
            this.textBox_DisplayFlow.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_DisplayFlow.Location = new System.Drawing.Point(396, 55);
            this.textBox_DisplayFlow.Name = "textBox_DisplayFlow";
            this.textBox_DisplayFlow.ReadOnly = true;
            this.textBox_DisplayFlow.Size = new System.Drawing.Size(113, 24);
            this.textBox_DisplayFlow.TabIndex = 5;
            this.textBox_DisplayFlow.TabStop = false;
            this.textBox_DisplayFlow.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // PressureLossCalcForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1168, 631);
            this.Controls.Add(this.textBox_LabelTempReturn);
            this.Controls.Add(this.textBox_LabelTempSupply);
            this.Controls.Add(this.textBox_InputTempReturn);
            this.Controls.Add(this.textBox_DisplayTempReturn);
            this.Controls.Add(this.textBox_DisplayTempSupply);
            this.Controls.Add(this.textBox_DisplayFlow);
            this.Controls.Add(this.textBox_DisplayPower);
            this.Controls.Add(this.textBox_LabelFlow);
            this.Controls.Add(this.textBox_LabelPowerInput);
            this.Controls.Add(this.textBox_InputTempSupply);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox_InputPower);
            this.Name = "PressureLossCalcForm";
            this.Text = "Pressure Loss Calc";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox_LabelPowerInput;
        private System.Windows.Forms.TextBox textBox_InputTempSupply;
        private System.Windows.Forms.TextBox textBox_LabelTempSupply;
        private System.Windows.Forms.TextBox textBox_InputTempReturn;
        private System.Windows.Forms.TextBox textBox_LabelTempReturn;
        private System.Windows.Forms.TextBox textBox_InputPower;
        private System.Windows.Forms.TextBox textBox_DisplayPower;
        private System.Windows.Forms.TextBox textBox_DisplayTempSupply;
        private System.Windows.Forms.TextBox textBox_DisplayTempReturn;
        private System.Windows.Forms.TextBox textBox_LabelFlow;
        private System.Windows.Forms.TextBox textBox_DisplayFlow;
    }
}