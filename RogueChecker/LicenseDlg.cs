using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using RogueChecker.Properties;

namespace RogueChecker;

public class LicenseDlg : Form
{
	private IContainer components;

	private RichTextBox richTextBox1;

	private Button button1;

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.richTextBox1 = new System.Windows.Forms.RichTextBox();
		this.button1 = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.richTextBox1.BackColor = System.Drawing.SystemColors.Control;
		this.richTextBox1.Location = new System.Drawing.Point(12, 12);
		this.richTextBox1.Name = "richTextBox1";
		this.richTextBox1.ReadOnly = true;
		this.richTextBox1.Size = new System.Drawing.Size(644, 312);
		this.richTextBox1.TabIndex = 0;
		this.richTextBox1.Text = "";
		this.button1.Location = new System.Drawing.Point(285, 330);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(99, 24);
		this.button1.TabIndex = 1;
		this.button1.Text = "OK";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(button1_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 16f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(668, 361);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.richTextBox1);
		base.Name = "LicenseDlg";
		this.Text = "Terms and Conditions";
		base.ResumeLayout(false);
	}

	public LicenseDlg()
	{
		InitializeComponent();
		richTextBox1.Rtf = Resources.license;
	}

	private void button1_Click(object sender, EventArgs e)
	{
		Close();
	}
}
