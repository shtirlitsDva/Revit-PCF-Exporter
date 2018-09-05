using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Shared;

namespace PCF_Exporter
{
    public partial class PCF_Exporter_form : System.Windows.Forms.Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PCF_Exporter_form));
            this.Tabs = new System.Windows.Forms.TabControl();
            this.TabSetup = new System.Windows.Forms.TabPage();
            this.radioBox1 = new RadioBox();
            this.button9 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.radioBox4 = new RadioBox();
            this.radioButton16 = new System.Windows.Forms.RadioButton();
            this.radioButton15 = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBox20 = new System.Windows.Forms.TextBox();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.button4 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.radioBox3 = new RadioBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.radioBox2 = new RadioBox();
            this.radioButton12 = new System.Windows.Forms.RadioButton();
            this.radioButton11 = new System.Windows.Forms.RadioButton();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.groupBox6 = new RadioBox();
            this.groupBox10 = new RadioBox();
            this.radioButton10 = new System.Windows.Forms.RadioButton();
            this.radioButton9 = new System.Windows.Forms.RadioButton();
            this.groupBox9 = new RadioBox();
            this.radioButton7 = new System.Windows.Forms.RadioButton();
            this.radioButton8 = new System.Windows.Forms.RadioButton();
            this.groupBox8 = new RadioBox();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.radioButton6 = new System.Windows.Forms.RadioButton();
            this.groupBox7 = new RadioBox();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new RadioBox();
            this.radioButton14 = new System.Windows.Forms.RadioButton();
            this.radioButton13 = new System.Windows.Forms.RadioButton();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.textBox22 = new System.Windows.Forms.TextBox();
            this.textBox23 = new System.Windows.Forms.TextBox();
            this.textBox24 = new System.Windows.Forms.TextBox();
            this.textBox21 = new System.Windows.Forms.TextBox();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.button6 = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.button5 = new System.Windows.Forms.Button();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
            this.button10 = new System.Windows.Forms.Button();
            this.Tabs.SuspendLayout();
            this.TabSetup.SuspendLayout();
            this.radioBox1.SuspendLayout();
            this.radioBox4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.radioBox3.SuspendLayout();
            this.radioBox2.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox10.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // Tabs
            // 
            this.Tabs.Controls.Add(this.TabSetup);
            this.Tabs.Controls.Add(this.tabPage2);
            this.Tabs.Controls.Add(this.tabPage3);
            this.Tabs.Controls.Add(this.tabPage4);
            this.Tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Tabs.Location = new System.Drawing.Point(0, 0);
            this.Tabs.Name = "Tabs";
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(383, 609);
            this.Tabs.TabIndex = 0;
            // 
            // TabSetup
            // 
            this.TabSetup.Controls.Add(this.radioBox1);
            this.TabSetup.Controls.Add(this.radioBox4);
            this.TabSetup.Controls.Add(this.groupBox2);
            this.TabSetup.Controls.Add(this.groupBox1);
            this.TabSetup.Location = new System.Drawing.Point(4, 22);
            this.TabSetup.Name = "TabSetup";
            this.TabSetup.Padding = new System.Windows.Forms.Padding(3);
            this.TabSetup.Size = new System.Drawing.Size(375, 583);
            this.TabSetup.TabIndex = 0;
            this.TabSetup.Text = "Setup";
            this.TabSetup.UseVisualStyleBackColor = true;
            // 
            // radioBox1
            // 
            this.radioBox1.Controls.Add(this.button9);
            this.radioBox1.Controls.Add(this.button8);
            this.radioBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.radioBox1.Location = new System.Drawing.Point(3, 399);
            this.radioBox1.Name = "radioBox1";
            this.radioBox1.Size = new System.Drawing.Size(369, 74);
            this.radioBox1.TabIndex = 6;
            this.radioBox1.TabStop = false;
            this.radioBox1.Text = "Create and export element schedule";
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(204, 20);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(162, 46);
            this.button9.TabIndex = 1;
            this.button9.Text = "Export schedule to EXCEL";
            this.button9.UseVisualStyleBackColor = true;
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(6, 20);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(162, 46);
            this.button8.TabIndex = 0;
            this.button8.Text = "Create schedules";
            this.button8.UseVisualStyleBackColor = true;
            // 
            // radioBox4
            // 
            this.radioBox4.Controls.Add(this.radioButton16);
            this.radioBox4.Controls.Add(this.radioButton15);
            this.radioBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.radioBox4.Location = new System.Drawing.Point(3, 353);
            this.radioBox4.Name = "radioBox4";
            this.radioBox4.Size = new System.Drawing.Size(369, 46);
            this.radioBox4.TabIndex = 5;
            this.radioBox4.TabStop = false;
            this.radioBox4.Text = "Write mode";
            // 
            // radioButton16
            // 
            this.radioButton16.AutoSize = true;
            this.radioButton16.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton16Append;
            this.radioButton16.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton16Append", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton16.Location = new System.Drawing.Point(251, 19);
            this.radioButton16.Name = "radioButton16";
            this.radioButton16.Size = new System.Drawing.Size(62, 17);
            this.radioButton16.TabIndex = 1;
            this.radioButton16.Text = "Append";
            this.radioButton16.UseVisualStyleBackColor = true;
            // 
            // radioButton15
            // 
            this.radioButton15.AutoSize = true;
            this.radioButton15.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton15Overwrite;
            this.radioButton15.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton15Overwrite", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton15.Location = new System.Drawing.Point(143, 19);
            this.radioButton15.Name = "radioButton15";
            this.radioButton15.Size = new System.Drawing.Size(70, 17);
            this.radioButton15.TabIndex = 0;
            this.radioButton15.TabStop = true;
            this.radioButton15.Text = "Overwrite";
            this.radioButton15.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.button10);
            this.groupBox2.Controls.Add(this.textBox20);
            this.groupBox2.Controls.Add(this.textBox7);
            this.groupBox2.Controls.Add(this.comboBox1);
            this.groupBox2.Controls.Add(this.button4);
            this.groupBox2.Controls.Add(this.button7);
            this.groupBox2.Controls.Add(this.button3);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(3, 135);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(369, 218);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "PCF parameter INITIALIZATION";
            // 
            // textBox20
            // 
            this.textBox20.BackColor = System.Drawing.SystemColors.Window;
            this.textBox20.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox20.Location = new System.Drawing.Point(3, 73);
            this.textBox20.Name = "textBox20";
            this.textBox20.ReadOnly = true;
            this.textBox20.Size = new System.Drawing.Size(363, 13);
            this.textBox20.TabIndex = 7;
            this.textBox20.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox7
            // 
            this.textBox7.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox7.Location = new System.Drawing.Point(71, 97);
            this.textBox7.Multiline = true;
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new System.Drawing.Size(220, 34);
            this.textBox7.TabIndex = 6;
            this.textBox7.Text = "Choose excel worksheet name:\r\n(select anew for each button below)";
            this.textBox7.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(99, 137);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(162, 21);
            this.comboBox1.TabIndex = 5;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(99, 20);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(162, 46);
            this.button4.TabIndex = 2;
            this.button4.Text = "Select parameter setup file (EXCEL)";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(204, 164);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(162, 46);
            this.button7.TabIndex = 2;
            this.button7.Text = "Populate PCF parameters PIPELINE";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(6, 164);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(162, 46);
            this.button3.TabIndex = 2;
            this.button3.Text = "Populate PCF parameters ELEMENT";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(369, 132);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "PCF parameter MANAGEMENT";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(99, 71);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(162, 46);
            this.button2.TabIndex = 1;
            this.button2.Text = "Delete PCF parameters";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(99, 19);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(162, 46);
            this.button1.TabIndex = 0;
            this.button1.Text = "Import PCF parameters";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.radioBox3);
            this.tabPage2.Controls.Add(this.radioBox2);
            this.tabPage2.Controls.Add(this.groupBox6);
            this.tabPage2.Controls.Add(this.groupBox3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(375, 583);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Config";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // radioBox3
            // 
            this.radioBox3.Controls.Add(this.checkBox2);
            this.radioBox3.Controls.Add(this.checkBox1);
            this.radioBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.radioBox3.Location = new System.Drawing.Point(3, 466);
            this.radioBox3.Name = "radioBox3";
            this.radioBox3.Size = new System.Drawing.Size(369, 68);
            this.radioBox3.TabIndex = 3;
            this.radioBox3.TabStop = false;
            this.radioBox3.Text = "Export to";
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = global::PCF_Functions.Properties.Settings.Default.checkBox2Checked;
            this.checkBox2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "checkBox2Checked", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox2.Location = new System.Drawing.Point(251, 20);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(68, 17);
            this.checkBox2.TabIndex = 1;
            this.checkBox2.Text = "Caesar II";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = global::PCF_Functions.Properties.Settings.Default.checkBox1Checked;
            this.checkBox1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "checkBox1Checked", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox1.Location = new System.Drawing.Point(143, 20);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(84, 17);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "Plant 3D Iso";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // radioBox2
            // 
            this.radioBox2.Controls.Add(this.radioButton12);
            this.radioBox2.Controls.Add(this.radioButton11);
            this.radioBox2.Controls.Add(this.textBox8);
            this.radioBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.radioBox2.Location = new System.Drawing.Point(3, 370);
            this.radioBox2.Name = "radioBox2";
            this.radioBox2.Size = new System.Drawing.Size(369, 96);
            this.radioBox2.TabIndex = 2;
            this.radioBox2.TabStop = false;
            this.radioBox2.Text = "Write wall thickness";
            // 
            // radioButton12
            // 
            this.radioButton12.AutoSize = true;
            this.radioButton12.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton12WallThkTrue;
            this.radioButton12.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton12WallThkTrue", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton12.Location = new System.Drawing.Point(143, 68);
            this.radioButton12.Name = "radioButton12";
            this.radioButton12.Size = new System.Drawing.Size(47, 17);
            this.radioButton12.TabIndex = 2;
            this.radioButton12.Text = "True";
            this.radioButton12.UseVisualStyleBackColor = true;
            this.radioButton12.CheckedChanged += new System.EventHandler(this.radioButton12_CheckedChanged);
            // 
            // radioButton11
            // 
            this.radioButton11.AutoSize = true;
            this.radioButton11.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton11WallThkFalse;
            this.radioButton11.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton11WallThkFalse", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton11.Location = new System.Drawing.Point(251, 68);
            this.radioButton11.Name = "radioButton11";
            this.radioButton11.Size = new System.Drawing.Size(50, 17);
            this.radioButton11.TabIndex = 1;
            this.radioButton11.TabStop = true;
            this.radioButton11.Text = "False";
            this.radioButton11.UseVisualStyleBackColor = true;
            this.radioButton11.CheckedChanged += new System.EventHandler(this.radioButton11_CheckedChanged);
            // 
            // textBox8
            // 
            this.textBox8.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox8.Location = new System.Drawing.Point(7, 20);
            this.textBox8.Multiline = true;
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(357, 41);
            this.textBox8.TabIndex = 0;
            this.textBox8.Text = "Experimental: Writes wall thickness for all components according to a schedule (c" +
    "urrently EN 10216-2 hardcoded). Ignores elements with not empty COMPONENT-ATTRIB" +
    "UTE4 parameter.";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.groupBox10);
            this.groupBox6.Controls.Add(this.groupBox9);
            this.groupBox6.Controls.Add(this.groupBox8);
            this.groupBox6.Controls.Add(this.groupBox7);
            this.groupBox6.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox6.Location = new System.Drawing.Point(3, 192);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(369, 178);
            this.groupBox6.TabIndex = 1;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "PCF File Header";
            // 
            // groupBox10
            // 
            this.groupBox10.Controls.Add(this.radioButton10);
            this.groupBox10.Controls.Add(this.radioButton9);
            this.groupBox10.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox10.Location = new System.Drawing.Point(3, 136);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(363, 40);
            this.groupBox10.TabIndex = 3;
            this.groupBox10.TabStop = false;
            this.groupBox10.Text = "UNITS-WEIGHT-LENGTH";
            // 
            // radioButton10
            // 
            this.radioButton10.AutoSize = true;
            this.radioButton10.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton10WeightLengthF;
            this.radioButton10.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton10WeightLengthF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton10.Location = new System.Drawing.Point(248, 17);
            this.radioButton10.Name = "radioButton10";
            this.radioButton10.Size = new System.Drawing.Size(52, 17);
            this.radioButton10.TabIndex = 0;
            this.radioButton10.Text = "FEET";
            this.radioButton10.UseVisualStyleBackColor = true;
            this.radioButton10.CheckedChanged += new System.EventHandler(this.radioButton10_CheckedChanged);
            // 
            // radioButton9
            // 
            this.radioButton9.AutoSize = true;
            this.radioButton9.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton9WeightLengthM;
            this.radioButton9.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton9WeightLengthM", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton9.Location = new System.Drawing.Point(140, 17);
            this.radioButton9.Name = "radioButton9";
            this.radioButton9.Size = new System.Drawing.Size(63, 17);
            this.radioButton9.TabIndex = 0;
            this.radioButton9.TabStop = true;
            this.radioButton9.Text = "METER";
            this.radioButton9.UseVisualStyleBackColor = true;
            this.radioButton9.CheckedChanged += new System.EventHandler(this.radioButton9_CheckedChanged);
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.radioButton7);
            this.groupBox9.Controls.Add(this.radioButton8);
            this.groupBox9.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox9.Location = new System.Drawing.Point(3, 96);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(363, 40);
            this.groupBox9.TabIndex = 2;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "UNITS-WEIGHT";
            // 
            // radioButton7
            // 
            this.radioButton7.AutoSize = true;
            this.radioButton7.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton7WeightKgs;
            this.radioButton7.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton7WeightKgs", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton7.Location = new System.Drawing.Point(140, 17);
            this.radioButton7.Name = "radioButton7";
            this.radioButton7.Size = new System.Drawing.Size(47, 17);
            this.radioButton7.TabIndex = 0;
            this.radioButton7.TabStop = true;
            this.radioButton7.Text = "KGS";
            this.radioButton7.UseVisualStyleBackColor = true;
            this.radioButton7.CheckedChanged += new System.EventHandler(this.radioButton7_CheckedChanged);
            // 
            // radioButton8
            // 
            this.radioButton8.AutoSize = true;
            this.radioButton8.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton8WeightLbs;
            this.radioButton8.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton8WeightLbs", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton8.Location = new System.Drawing.Point(248, 17);
            this.radioButton8.Name = "radioButton8";
            this.radioButton8.Size = new System.Drawing.Size(45, 17);
            this.radioButton8.TabIndex = 0;
            this.radioButton8.Text = "LBS";
            this.radioButton8.UseVisualStyleBackColor = true;
            this.radioButton8.CheckedChanged += new System.EventHandler(this.radioButton8_CheckedChanged);
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.radioButton5);
            this.groupBox8.Controls.Add(this.radioButton6);
            this.groupBox8.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox8.Location = new System.Drawing.Point(3, 56);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(363, 40);
            this.groupBox8.TabIndex = 1;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "UNITS-CO-ORDS";
            // 
            // radioButton5
            // 
            this.radioButton5.AutoSize = true;
            this.radioButton5.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton5CoordsMm;
            this.radioButton5.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton5CoordsMm", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton5.Location = new System.Drawing.Point(140, 17);
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.Size = new System.Drawing.Size(43, 17);
            this.radioButton5.TabIndex = 0;
            this.radioButton5.TabStop = true;
            this.radioButton5.Text = "MM";
            this.radioButton5.UseVisualStyleBackColor = true;
            this.radioButton5.CheckedChanged += new System.EventHandler(this.radioButton5_CheckedChanged);
            // 
            // radioButton6
            // 
            this.radioButton6.AutoSize = true;
            this.radioButton6.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton6CoordsInch;
            this.radioButton6.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton6CoordsInch", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton6.Location = new System.Drawing.Point(248, 17);
            this.radioButton6.Name = "radioButton6";
            this.radioButton6.Size = new System.Drawing.Size(51, 17);
            this.radioButton6.TabIndex = 0;
            this.radioButton6.Text = "INCH";
            this.radioButton6.UseVisualStyleBackColor = true;
            this.radioButton6.CheckedChanged += new System.EventHandler(this.radioButton6_CheckedChanged);
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.radioButton4);
            this.groupBox7.Controls.Add(this.radioButton3);
            this.groupBox7.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox7.Location = new System.Drawing.Point(3, 16);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(363, 40);
            this.groupBox7.TabIndex = 0;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "UNITS-BORE";
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton4BoreINCH;
            this.radioButton4.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton4BoreINCH", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton4.Location = new System.Drawing.Point(248, 19);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(51, 17);
            this.radioButton4.TabIndex = 0;
            this.radioButton4.Text = "INCH";
            this.radioButton4.UseVisualStyleBackColor = true;
            this.radioButton4.CheckedChanged += new System.EventHandler(this.radioButton4_CheckedChanged);
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton3BoreMM;
            this.radioButton3.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton3BoreMM", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton3.Location = new System.Drawing.Point(140, 19);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(43, 17);
            this.radioButton3.TabIndex = 0;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "MM";
            this.radioButton3.UseVisualStyleBackColor = true;
            this.radioButton3.CheckedChanged += new System.EventHandler(this.radioButton3_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radioButton14);
            this.groupBox3.Controls.Add(this.radioButton13);
            this.groupBox3.Controls.Add(this.comboBox2);
            this.groupBox3.Controls.Add(this.textBox22);
            this.groupBox3.Controls.Add(this.textBox23);
            this.groupBox3.Controls.Add(this.textBox24);
            this.groupBox3.Controls.Add(this.textBox21);
            this.groupBox3.Controls.Add(this.radioButton2);
            this.groupBox3.Controls.Add(this.radioButton1);
            this.groupBox3.Controls.Add(this.textBox4);
            this.groupBox3.Controls.Add(this.textBox2);
            this.groupBox3.Controls.Add(this.textBox1);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox3.Location = new System.Drawing.Point(3, 3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(369, 189);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Scope";
            // 
            // radioButton14
            // 
            this.radioButton14.AutoSize = true;
            this.radioButton14.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton14ExportSelection;
            this.radioButton14.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton14ExportSelection", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton14.Location = new System.Drawing.Point(46, 107);
            this.radioButton14.Name = "radioButton14";
            this.radioButton14.Size = new System.Drawing.Size(69, 17);
            this.radioButton14.TabIndex = 13;
            this.radioButton14.Tag = "1";
            this.radioButton14.Text = "Selection";
            this.radioButton14.UseVisualStyleBackColor = true;
            this.radioButton14.CheckedChanged += new System.EventHandler(this.radioButton14_CheckedChanged);
            // 
            // radioButton13
            // 
            this.radioButton13.AutoSize = true;
            this.radioButton13.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton13AllPipelinesSeparate;
            this.radioButton13.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton13AllPipelinesSeparate", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton13.Location = new System.Drawing.Point(46, 62);
            this.radioButton13.Name = "radioButton13";
            this.radioButton13.Size = new System.Drawing.Size(148, 17);
            this.radioButton13.TabIndex = 12;
            this.radioButton13.Tag = "1";
            this.radioButton13.Text = "All pipelines, separate files";
            this.radioButton13.UseVisualStyleBackColor = true;
            this.radioButton13.CheckedChanged += new System.EventHandler(this.radioButton13_CheckedChanged);
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(157, 84);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(205, 21);
            this.comboBox2.TabIndex = 11;
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // textBox22
            // 
            this.textBox22.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::PCF_Functions.Properties.Settings.Default, "textBox22DiameterLimit", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox22.Location = new System.Drawing.Point(84, 139);
            this.textBox22.Name = "textBox22";
            this.textBox22.Size = new System.Drawing.Size(57, 20);
            this.textBox22.TabIndex = 10;
            this.textBox22.Text = global::PCF_Functions.Properties.Settings.Default.textBox22DiameterLimit;
            this.textBox22.TextChanged += new System.EventHandler(this.textBox22_TextChanged);
            // 
            // textBox23
            // 
            this.textBox23.BackColor = System.Drawing.SystemColors.Window;
            this.textBox23.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox23.Location = new System.Drawing.Point(3, 161);
            this.textBox23.Name = "textBox23";
            this.textBox23.ReadOnly = true;
            this.textBox23.Size = new System.Drawing.Size(345, 13);
            this.textBox23.TabIndex = 7;
            this.textBox23.Text = "Elements below or equal to specified nominal size will not be exported.";
            // 
            // textBox24
            // 
            this.textBox24.BackColor = System.Drawing.SystemColors.Window;
            this.textBox24.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox24.Location = new System.Drawing.Point(158, 142);
            this.textBox24.Name = "textBox24";
            this.textBox24.ReadOnly = true;
            this.textBox24.Size = new System.Drawing.Size(91, 13);
            this.textBox24.TabIndex = 8;
            this.textBox24.Text = "Nominal";
            // 
            // textBox21
            // 
            this.textBox21.BackColor = System.Drawing.SystemColors.Window;
            this.textBox21.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox21.Location = new System.Drawing.Point(3, 142);
            this.textBox21.Name = "textBox21";
            this.textBox21.ReadOnly = true;
            this.textBox21.Size = new System.Drawing.Size(75, 13);
            this.textBox21.TabIndex = 9;
            this.textBox21.Text = "Diameter limit:";
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton2SpecificPipeline;
            this.radioButton2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton2SpecificPipeline", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton2.Location = new System.Drawing.Point(46, 85);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(105, 17);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Tag = "2";
            this.radioButton2.Text = "Specific pipeline:";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = global::PCF_Functions.Properties.Settings.Default.radioButton1AllPipelines;
            this.radioButton1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::PCF_Functions.Properties.Settings.Default, "radioButton1AllPipelines", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton1.Location = new System.Drawing.Point(46, 39);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(120, 17);
            this.radioButton1.TabIndex = 1;
            this.radioButton1.TabStop = true;
            this.radioButton1.Tag = "1";
            this.radioButton1.Text = "All pipelines, one file";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // textBox4
            // 
            this.textBox4.BackColor = System.Drawing.SystemColors.Window;
            this.textBox4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox4.Location = new System.Drawing.Point(259, 111);
            this.textBox4.Name = "textBox4";
            this.textBox4.ReadOnly = true;
            this.textBox4.Size = new System.Drawing.Size(103, 13);
            this.textBox4.TabIndex = 0;
            this.textBox4.Text = "System Abbreviation";
            this.textBox4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.Window;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.Location = new System.Drawing.Point(3, 41);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(36, 13);
            this.textBox2.TabIndex = 0;
            this.textBox2.Text = "Export:";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Window;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Location = new System.Drawing.Point(3, 19);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(360, 13);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "Pipelines are defined by System Abbreviation parameter in Revit.";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox5);
            this.tabPage3.Controls.Add(this.groupBox4);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(375, 583);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Run";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.button6);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox5.Location = new System.Drawing.Point(3, 110);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(369, 84);
            this.groupBox5.TabIndex = 1;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Export PCF data";
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(103, 19);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(162, 47);
            this.button6.TabIndex = 0;
            this.button6.Text = "Export PCF";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.textBox6);
            this.groupBox4.Controls.Add(this.textBox5);
            this.groupBox4.Controls.Add(this.button5);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox4.Location = new System.Drawing.Point(3, 3);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(369, 107);
            this.groupBox4.TabIndex = 0;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Output directory";
            // 
            // textBox6
            // 
            this.textBox6.BackColor = System.Drawing.SystemColors.Window;
            this.textBox6.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox6.Location = new System.Drawing.Point(6, 69);
            this.textBox6.Name = "textBox6";
            this.textBox6.ReadOnly = true;
            this.textBox6.Size = new System.Drawing.Size(205, 13);
            this.textBox6.TabIndex = 5;
            this.textBox6.Text = "Selected output path:";
            this.textBox6.Visible = false;
            // 
            // textBox5
            // 
            this.textBox5.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox5.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::PCF_Functions.Properties.Settings.Default, "textBox5OutputPath", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox5.Location = new System.Drawing.Point(3, 88);
            this.textBox5.Name = "textBox5";
            this.textBox5.ReadOnly = true;
            this.textBox5.Size = new System.Drawing.Size(363, 13);
            this.textBox5.TabIndex = 4;
            this.textBox5.Text = global::PCF_Functions.Properties.Settings.Default.textBox5OutputPath;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(103, 19);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(162, 46);
            this.button5.TabIndex = 3;
            this.button5.Text = "Select output directory";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.richTextBox1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(375, 583);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Help";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Location = new System.Drawing.Point(6, 6);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(361, 463);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBox1_LinkClicked);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // openFileDialog2
            // 
            this.openFileDialog2.FileName = "openFileDialog1";
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(267, 20);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(96, 46);
            this.button10.TabIndex = 8;
            this.button10.Text = "Export undefined elements";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // PCF_Exporter_form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(383, 609);
            this.Controls.Add(this.Tabs);
            this.Name = "PCF_Exporter_form";
            this.Text = "PCF_Exporter_form";
            this.Tabs.ResumeLayout(false);
            this.TabSetup.ResumeLayout(false);
            this.radioBox1.ResumeLayout(false);
            this.radioBox4.ResumeLayout(false);
            this.radioBox4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.radioBox3.ResumeLayout(false);
            this.radioBox3.PerformLayout();
            this.radioBox2.ResumeLayout(false);
            this.radioBox2.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox10.ResumeLayout(false);
            this.groupBox10.PerformLayout();
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.TabPage TabSetup;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ComboBox comboBox1;
        private RadioBox groupBox3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.OpenFileDialog openFileDialog2;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button button6;
        private RadioBox groupBox6;
        private RadioBox groupBox10;
        private System.Windows.Forms.RadioButton radioButton9;
        private RadioBox groupBox9;
        private System.Windows.Forms.RadioButton radioButton7;
        private System.Windows.Forms.RadioButton radioButton8;
        private RadioBox groupBox8;
        private System.Windows.Forms.RadioButton radioButton5;
        private System.Windows.Forms.RadioButton radioButton6;
        private RadioBox groupBox7;
        private System.Windows.Forms.RadioButton radioButton4;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.TextBox textBox7;
        private System.Windows.Forms.RadioButton radioButton10;
        private System.Windows.Forms.TextBox textBox20;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox textBox22;
        private System.Windows.Forms.TextBox textBox23;
        private System.Windows.Forms.TextBox textBox24;
        private System.Windows.Forms.TextBox textBox21;
        private RadioBox radioBox2;
        private System.Windows.Forms.RadioButton radioButton12;
        private System.Windows.Forms.RadioButton radioButton11;
        private System.Windows.Forms.TextBox textBox8;
        private RadioBox radioBox3;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.RadioButton radioButton14;
        private System.Windows.Forms.RadioButton radioButton13;
        private System.Windows.Forms.ComboBox comboBox2;
        private RadioBox radioBox1;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button8;
        private RadioBox radioBox4;
        private System.Windows.Forms.RadioButton radioButton16;
        private System.Windows.Forms.RadioButton radioButton15;
        private System.Windows.Forms.Button button10;
    }
}