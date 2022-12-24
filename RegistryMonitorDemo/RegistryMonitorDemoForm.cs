using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Data;
using RegistryUtils;
using Microsoft.Win32;

namespace RegistryMonitorDemo
{
	
	public class RegistryMonitorDemoForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonStart;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.TextBox textRegistryKey;

		private RegistryMonitor registryMonitor;
		private System.Windows.Forms.ComboBox comboBoxRegistryHives;

		
		private System.ComponentModel.Container components = null;

		public RegistryMonitorDemoForm()
		{
			
			InitializeComponent();

			comboBoxRegistryHives.DisplayMember = "Name";
			comboBoxRegistryHives.Items.Add(Registry.CurrentUser.Name);
			comboBoxRegistryHives.Items.Add(Registry.LocalMachine.Name);
			comboBoxRegistryHives.SelectedItem = Registry.CurrentUser.Name;
		}

		
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
				StopRegistryMonitor();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegistryMonitorDemoForm));
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxRegistryHives = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textRegistryKey = new System.Windows.Forms.TextBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
           
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "Роздiл реєстрів:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
          
            this.comboBoxRegistryHives.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRegistryHives.Location = new System.Drawing.Point(96, 9);
            this.comboBoxRegistryHives.Name = "comboBoxRegistryHives";
            this.comboBoxRegistryHives.Size = new System.Drawing.Size(208, 21);
            this.comboBoxRegistryHives.TabIndex = 1;
           
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Ключ реєстру:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label2.Click += new System.EventHandler(this.label2_Click);
           
            this.textRegistryKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textRegistryKey.Location = new System.Drawing.Point(88, 40);
            this.textRegistryKey.Name = "textRegistryKey";
            this.textRegistryKey.Size = new System.Drawing.Size(376, 20);
            this.textRegistryKey.TabIndex = 3;
           
            this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonStart.Location = new System.Drawing.Point(8, 80);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 4;
            this.buttonStart.Text = "Початок";
            this.buttonStart.Click += new System.EventHandler(this.OnButtonStartClick);
           
            this.buttonStop.Enabled = false;
            this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonStop.Location = new System.Drawing.Point(96, 80);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 5;
            this.buttonStop.Text = "Зупинка";
            this.buttonStop.Click += new System.EventHandler(this.OnButtonStopClick);
            
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.listBox1.IntegralHeight = false;
            this.listBox1.Location = new System.Drawing.Point(8, 112);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(456, 147);
            this.listBox1.TabIndex = 6;
            
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(480, 269);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.textRegistryKey);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxRegistryHives);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RegistryMonitorDemoForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "RegistryMonitor ";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		
		[STAThread]
		static void Main() 
		{
			
			Application.EnableVisualStyles();
			Application.DoEvents();

			Application.Run(new RegistryMonitorDemoForm());
		}

		private void OnButtonStartClick(object sender, EventArgs e)
		{
			string keyName = string.Format("{0}\\{1}", comboBoxRegistryHives.SelectedItem, textRegistryKey.Text);

			listBox1.Items.Add("Моніторінг \"" + keyName + "\" почався");
			registryMonitor = new RegistryMonitor(keyName);
            registryMonitor.RegChanged += new EventHandler(OnRegChanged);
            registryMonitor.Error += new System.IO.ErrorEventHandler(OnError);
            registryMonitor.Start();

			buttonStart.Enabled = false;
			buttonStop.Enabled = true;
		}

		private void OnButtonStopClick(object sender, EventArgs e)
		{
			StopRegistryMonitor();
		}

		private void StopRegistryMonitor()
		{
			if (registryMonitor != null)
			{
				registryMonitor.Stop();
				registryMonitor.RegChanged -= new EventHandler(OnRegChanged);
				registryMonitor.Error -= new System.IO.ErrorEventHandler(OnError);
				registryMonitor = null;

				buttonStart.Enabled = true;
				buttonStop.Enabled = false;

				listBox1.Items.Add("Моніторинг закінчився");
			}
		}

		private void OnRegChanged(object sender, EventArgs e)
		{
		    if (InvokeRequired)
		    {
		        BeginInvoke(new EventHandler(OnRegChanged), new object[] { sender, e});
		        return;
		    }
		    
			listBox1.Items.Add("Реєстр було змінено");
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
            if (InvokeRequired)
            {
                BeginInvoke(new ErrorEventHandler(OnError), new object[] { sender, e });
                return;
            }

            listBox1.Items.Add("ПОМИЛКА!: " + e.GetException().Message);
			StopRegistryMonitor();
		}

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
