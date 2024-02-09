namespace Shared.Tools
{
    partial class ValidationTypeSelector
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
            this.radioBox1 = new Shared.RadioBox();
            this.button_validate = new System.Windows.Forms.Button();
            this.comboBox_systemList = new System.Windows.Forms.ComboBox();
            this.radioButton_selectedSystem = new System.Windows.Forms.RadioButton();
            this.radioButton_allSystems = new System.Windows.Forms.RadioButton();
            this.radioBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // radioBox1
            // 
            this.radioBox1.Controls.Add(this.button_validate);
            this.radioBox1.Controls.Add(this.comboBox_systemList);
            this.radioBox1.Controls.Add(this.radioButton_selectedSystem);
            this.radioBox1.Controls.Add(this.radioButton_allSystems);
            this.radioBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radioBox1.Location = new System.Drawing.Point(0, 0);
            this.radioBox1.Name = "radioBox1";
            this.radioBox1.Size = new System.Drawing.Size(588, 180);
            this.radioBox1.TabIndex = 0;
            this.radioBox1.TabStop = false;
            // 
            // button_validate
            // 
            this.button_validate.Location = new System.Drawing.Point(13, 100);
            this.button_validate.Name = "button_validate";
            this.button_validate.Size = new System.Drawing.Size(563, 68);
            this.button_validate.TabIndex = 3;
            this.button_validate.Text = "VALIDATE";
            this.button_validate.UseVisualStyleBackColor = true;
            this.button_validate.Click += new System.EventHandler(this.button_validate_Click);
            // 
            // comboBox_systemList
            // 
            this.comboBox_systemList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_systemList.FormattingEnabled = true;
            this.comboBox_systemList.Location = new System.Drawing.Point(220, 54);
            this.comboBox_systemList.Name = "comboBox_systemList";
            this.comboBox_systemList.Size = new System.Drawing.Size(356, 33);
            this.comboBox_systemList.TabIndex = 2;
            // 
            // radioButton_selectedSystem
            // 
            this.radioButton_selectedSystem.AutoSize = true;
            this.radioButton_selectedSystem.Checked = global::Shared.Properties.Settings.Default.radioButton_selectedSystem;
            this.radioButton_selectedSystem.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Shared.Properties.Settings.Default, "radioButton_selectedSystem", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton_selectedSystem.Location = new System.Drawing.Point(13, 58);
            this.radioButton_selectedSystem.Name = "radioButton_selectedSystem";
            this.radioButton_selectedSystem.Size = new System.Drawing.Size(201, 29);
            this.radioButton_selectedSystem.TabIndex = 1;
            this.radioButton_selectedSystem.Text = "Selected system";
            this.radioButton_selectedSystem.UseVisualStyleBackColor = true;
            this.radioButton_selectedSystem.CheckedChanged += new System.EventHandler(this.radioButton_selectedSystem_CheckedChanged);
            // 
            // radioButton_allSystems
            // 
            this.radioButton_allSystems.AutoSize = true;
            this.radioButton_allSystems.Checked = global::Shared.Properties.Settings.Default.radioButton_allSystems;
            this.radioButton_allSystems.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Shared.Properties.Settings.Default, "radioButton_allSystems", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton_allSystems.Location = new System.Drawing.Point(13, 13);
            this.radioButton_allSystems.Name = "radioButton_allSystems";
            this.radioButton_allSystems.Size = new System.Drawing.Size(152, 29);
            this.radioButton_allSystems.TabIndex = 0;
            this.radioButton_allSystems.TabStop = true;
            this.radioButton_allSystems.Text = "All systems";
            this.radioButton_allSystems.UseVisualStyleBackColor = true;
            this.radioButton_allSystems.CheckedChanged += new System.EventHandler(this.radioButton_allSystems_CheckedChanged);
            // 
            // ValidationTypeSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(588, 180);
            this.Controls.Add(this.radioBox1);
            this.Name = "ValidationTypeSelector";
            this.Text = "Select validation mode";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ValidationTypeSelector_FormClosing);
            this.radioBox1.ResumeLayout(false);
            this.radioBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private RadioBox radioBox1;
        private System.Windows.Forms.ComboBox comboBox_systemList;
        private System.Windows.Forms.RadioButton radioButton_selectedSystem;
        private System.Windows.Forms.RadioButton radioButton_allSystems;
        private System.Windows.Forms.Button button_validate;
    }
}