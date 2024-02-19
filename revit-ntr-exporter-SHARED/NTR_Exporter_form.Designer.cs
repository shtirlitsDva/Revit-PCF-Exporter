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

using mySettings = NTR_Exporter.Properties.Settings;
using Shared;

namespace NTR_Exporter
{
    public partial class NTR_Exporter_form : System.Windows.Forms.Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NTR_Exporter_form));
            this.Tabs = new System.Windows.Forms.TabControl();
            this.TabSetup = new System.Windows.Forms.TabPage();
            this.radioBox1 = new Shared.RadioBox();
            this.button9 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBox20 = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.radioBox3 = new Shared.RadioBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new Shared.RadioBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.radioButton14 = new System.Windows.Forms.RadioButton();
            this.radioButton13 = new System.Windows.Forms.RadioButton();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.textBox22 = new System.Windows.Forms.TextBox();
            this.textBox23 = new System.Windows.Forms.TextBox();
            this.textBox10 = new System.Windows.Forms.TextBox();
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
            this.Tabs.SuspendLayout();
            this.TabSetup.SuspendLayout();
            this.radioBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.radioBox3.SuspendLayout();
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
            this.Tabs.Size = new System.Drawing.Size(383, 563);
            this.Tabs.TabIndex = 0;
            // 
            // TabSetup
            // 
            this.TabSetup.Controls.Add(this.radioBox1);
            this.TabSetup.Controls.Add(this.groupBox2);
            this.TabSetup.Location = new System.Drawing.Point(4, 22);
            this.TabSetup.Name = "TabSetup";
            this.TabSetup.Padding = new System.Windows.Forms.Padding(3);
            this.TabSetup.Size = new System.Drawing.Size(375, 537);
            this.TabSetup.TabIndex = 0;
            this.TabSetup.Text = "Setup";
            this.TabSetup.UseVisualStyleBackColor = true;
            // 
            // radioBox1
            // 
            this.radioBox1.Controls.Add(this.button9);
            this.radioBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.radioBox1.Location = new System.Drawing.Point(3, 107);
            this.radioBox1.Name = "radioBox1";
            this.radioBox1.Size = new System.Drawing.Size(369, 74);
            this.radioBox1.TabIndex = 2;
            this.radioBox1.TabStop = false;
            this.radioBox1.Text = "Export undefined elements to EXCEL";
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(99, 19);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(162, 46);
            this.button9.TabIndex = 1;
            this.button9.Text = "Export undefined ELEMENTS to EXCEL";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.Button9_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBox20);
            this.groupBox2.Controls.Add(this.button4);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(369, 104);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "NTR configuration file";
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
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(99, 20);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(162, 46);
            this.button4.TabIndex = 2;
            this.button4.Text = "Select configuration file (EXCEL)";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.Button4_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.radioBox3);
            this.tabPage2.Controls.Add(this.groupBox3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(375, 537);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Config";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // radioBox3
            // 
            this.radioBox3.Controls.Add(this.checkBox1);
            this.radioBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.radioBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioBox3.Location = new System.Drawing.Point(3, 202);
            this.radioBox3.Name = "radioBox3";
            this.radioBox3.Size = new System.Drawing.Size(369, 68);
            this.radioBox3.TabIndex = 3;
            this.radioBox3.TabStop = false;
            this.radioBox3.Text = "Include";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = global::NTR_Exporter.Properties.Settings.Default.checkBox1Checked;
            this.checkBox1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::NTR_Exporter.Properties.Settings.Default, "checkBox1Checked", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox1.Location = new System.Drawing.Point(46, 28);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(94, 17);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "Steel structure";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.textBox3);
            this.groupBox3.Controls.Add(this.textBox9);
            this.groupBox3.Controls.Add(this.radioButton14);
            this.groupBox3.Controls.Add(this.radioButton13);
            this.groupBox3.Controls.Add(this.comboBox2);
            this.groupBox3.Controls.Add(this.textBox22);
            this.groupBox3.Controls.Add(this.textBox23);
            this.groupBox3.Controls.Add(this.textBox10);
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
            this.groupBox3.Size = new System.Drawing.Size(369, 199);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Scope";
            // 
            // textBox3
            // 
            this.textBox3.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::NTR_Exporter.Properties.Settings.Default, "textBox3DiameterLessThan", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox3.Location = new System.Drawing.Point(239, 151);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(57, 20);
            this.textBox3.TabIndex = 15;
            this.textBox3.Text = global::NTR_Exporter.Properties.Settings.Default.textBox3DiameterLessThan;
            this.textBox3.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            // 
            // textBox9
            // 
            this.textBox9.BackColor = System.Drawing.SystemColors.Window;
            this.textBox9.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox9.Location = new System.Drawing.Point(239, 132);
            this.textBox9.Name = "textBox9";
            this.textBox9.ReadOnly = true;
            this.textBox9.Size = new System.Drawing.Size(91, 13);
            this.textBox9.TabIndex = 14;
            this.textBox9.Text = "Nominal";
            // 
            // radioButton14
            // 
            this.radioButton14.AutoSize = true;
            this.radioButton14.Checked = global::NTR_Exporter.Properties.Settings.Default.radioButton14ExportSelection;
            this.radioButton14.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::NTR_Exporter.Properties.Settings.Default, "radioButton14ExportSelection", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton14.Location = new System.Drawing.Point(46, 107);
            this.radioButton14.Name = "radioButton14";
            this.radioButton14.Size = new System.Drawing.Size(69, 17);
            this.radioButton14.TabIndex = 13;
            this.radioButton14.Tag = "1";
            this.radioButton14.Text = "Selection";
            this.radioButton14.UseVisualStyleBackColor = true;
            this.radioButton14.CheckedChanged += new System.EventHandler(this.RadioButton14_CheckedChanged);
            // 
            // radioButton13
            // 
            this.radioButton13.AutoSize = true;
            this.radioButton13.Checked = global::NTR_Exporter.Properties.Settings.Default.radioButton13AllPipelinesSeparate;
            this.radioButton13.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::NTR_Exporter.Properties.Settings.Default, "radioButton13AllPipelinesSeparate", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton13.Location = new System.Drawing.Point(46, 62);
            this.radioButton13.Name = "radioButton13";
            this.radioButton13.Size = new System.Drawing.Size(148, 17);
            this.radioButton13.TabIndex = 12;
            this.radioButton13.Tag = "1";
            this.radioButton13.Text = "All pipelines, separate files";
            this.radioButton13.UseVisualStyleBackColor = true;
            this.radioButton13.CheckedChanged += new System.EventHandler(this.RadioButton13_CheckedChanged);
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(157, 84);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(205, 21);
            this.comboBox2.TabIndex = 11;
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.ComboBox2_SelectedIndexChanged);
            // 
            // textBox22
            // 
            this.textBox22.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::NTR_Exporter.Properties.Settings.Default, "textBox22DiameterLimit", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox22.Location = new System.Drawing.Point(84, 148);
            this.textBox22.Name = "textBox22";
            this.textBox22.Size = new System.Drawing.Size(57, 20);
            this.textBox22.TabIndex = 10;
            this.textBox22.Text = global::NTR_Exporter.Properties.Settings.Default.textBox22DiameterLimit;
            this.textBox22.TextChanged += new System.EventHandler(this.TextBox22_TextChanged);
            // 
            // textBox23
            // 
            this.textBox23.BackColor = System.Drawing.SystemColors.Window;
            this.textBox23.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox23.Location = new System.Drawing.Point(3, 170);
            this.textBox23.Name = "textBox23";
            this.textBox23.ReadOnly = true;
            this.textBox23.Size = new System.Drawing.Size(345, 13);
            this.textBox23.TabIndex = 7;
            this.textBox23.Text = "Elements below or equal to specified nominal size will not be exported.";
            // 
            // textBox10
            // 
            this.textBox10.BackColor = System.Drawing.SystemColors.Window;
            this.textBox10.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox10.Location = new System.Drawing.Point(143, 155);
            this.textBox10.Name = "textBox10";
            this.textBox10.ReadOnly = true;
            this.textBox10.Size = new System.Drawing.Size(91, 13);
            this.textBox10.TabIndex = 8;
            this.textBox10.Text = "<= DN <=";
            this.textBox10.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox24
            // 
            this.textBox24.BackColor = System.Drawing.SystemColors.Window;
            this.textBox24.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox24.Location = new System.Drawing.Point(84, 129);
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
            this.textBox21.Location = new System.Drawing.Point(3, 151);
            this.textBox21.Name = "textBox21";
            this.textBox21.ReadOnly = true;
            this.textBox21.Size = new System.Drawing.Size(75, 13);
            this.textBox21.TabIndex = 9;
            this.textBox21.Text = "Diameter limit:";
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = global::NTR_Exporter.Properties.Settings.Default.radioButton2SpecificPipeline;
            this.radioButton2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::NTR_Exporter.Properties.Settings.Default, "radioButton2SpecificPipeline", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton2.Location = new System.Drawing.Point(46, 85);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(105, 17);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Tag = "2";
            this.radioButton2.Text = "Specific pipeline:";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.RadioButton2_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = global::NTR_Exporter.Properties.Settings.Default.radioButton1AllPipelines;
            this.radioButton1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::NTR_Exporter.Properties.Settings.Default, "radioButton1AllPipelines", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.radioButton1.Location = new System.Drawing.Point(46, 39);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(120, 17);
            this.radioButton1.TabIndex = 1;
            this.radioButton1.TabStop = true;
            this.radioButton1.Tag = "1";
            this.radioButton1.Text = "All pipelines, one file";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.RadioButton1_CheckedChanged);
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
            this.tabPage3.Size = new System.Drawing.Size(375, 537);
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
            this.groupBox5.Text = "Export NTR data";
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(103, 19);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(162, 47);
            this.button6.TabIndex = 0;
            this.button6.Text = "Export NTR";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.Button6_Click);
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
            this.textBox5.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::NTR_Exporter.Properties.Settings.Default, "textBox5OutputPath", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBox5.Location = new System.Drawing.Point(3, 88);
            this.textBox5.Name = "textBox5";
            this.textBox5.ReadOnly = true;
            this.textBox5.Size = new System.Drawing.Size(363, 13);
            this.textBox5.TabIndex = 4;
            this.textBox5.Text = global::NTR_Exporter.Properties.Settings.Default.textBox5OutputPath;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(103, 19);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(162, 46);
            this.button5.TabIndex = 3;
            this.button5.Text = "Select output directory";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.Button5_Click);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.richTextBox1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(375, 537);
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
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.RichTextBox1_LinkClicked);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // openFileDialog2
            // 
            this.openFileDialog2.FileName = "openFileDialog1";
            // 
            // NTR_Exporter_form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(383, 563);
            this.Controls.Add(this.Tabs);
            this.Name = "NTR_Exporter_form";
            this.Text = "NTR Exporter";
            this.Tabs.ResumeLayout(false);
            this.TabSetup.ResumeLayout(false);
            this.radioBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.radioBox3.ResumeLayout(false);
            this.radioBox3.PerformLayout();
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
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
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
        private System.Windows.Forms.TextBox textBox20;
        private RadioBox radioBox1;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox textBox22;
        private System.Windows.Forms.TextBox textBox23;
        private System.Windows.Forms.TextBox textBox24;
        private System.Windows.Forms.TextBox textBox21;
        private RadioBox radioBox3;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.RadioButton radioButton14;
        private System.Windows.Forms.RadioButton radioButton13;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox9;
        private System.Windows.Forms.TextBox textBox10;
    }
}