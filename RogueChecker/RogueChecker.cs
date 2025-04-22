using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using RogueChecker.Properties;

namespace RogueChecker;

public class RogueChecker : Form
{
	private delegate void SetTextCallback();

	private delegate void SetDCGridCallback(DataGridViewRow RowToAdd);

	private delegate void SetDCStatusCallback(string szStatus, bool IsError);

	private CommonUI uiData;

	private SendPacketProcessing sendPacket;

	private System.Threading.Timer timerRogueStart;

	private System.Threading.Timer timerDCQueryStatus;

	private static AutoResetEvent ServerReplied = new AutoResetEvent(initialState: false);

	private List<string> WellKnownServers;

	private int DotCount = 1;

	private int DHCPServersinAD;

	private IContainer components;

	private Button btn_OK;

	private Button btn_Close;

	private ErrorProvider ep_AuthIP;

	private TabControl Tab_Main;

	private TabPage tabProcessing;

	private TabPage tabConfiguration;

	private GroupBox groupBox4;

	private GroupBox groupBox2;

	private RadioButton rb_Interval;

	private RadioButton rb_OneTime;

	private Label label3;

	private NumericUpDown nud_Interval;

	private GroupBox groupBox1;

	private DataGridView dgv_AuthSrvrIP;

	private GroupBox groupBox3;

	private DataGridView dgv_RogueIP;

	private GroupBox groupBox5;

	private ComboBox cb_LogLevel;

	private Label label2;

	private Label label1;

	private Button button1;

	private TextBox txt_LogPath;

	private FolderBrowserDialog folderBrowserDialog1;

	private Label lbl_Status;

	private Label lbl_AuthoStatus;

	private CheckedListBox lb_InterfaceToBind;

	private NotifyIcon NI_Rogue;

	private DataGridViewTextBoxColumn DHCP_IP_ADDR;

	private DataGridViewTextBoxColumn DNCP_SERVER_Name;

	private BackgroundWorker ServersinDC;

	private DataGridViewCheckBoxColumn btnAdd;

	private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;

	private DataGridViewTextBoxColumn DHCP_CLIENT_IP;

	private DataGridViewTextBoxColumn DHCP_SERVER_GATEWAY;

	private DataGridViewTextBoxColumn DHCP_SERVER_RESPONSE_TIME;

	private LinkLabel linkLabel1;

	public RogueChecker()
	{
		uiData = CommonUI.CreateCommonInstance();
		WellKnownServers = new List<string>();
		sendPacket = new SendPacketProcessing();
		sendPacket.SendData += UpdateRogue;
		InitializeComponent();
	}

	private void RogueChecker_Load(object sender, EventArgs e)
	{
		txt_LogPath.Text = Directory.GetCurrentDirectory();
		cb_LogLevel.Items.Add("Critical");
		cb_LogLevel.Items.Add("Error");
		cb_LogLevel.Items.Add("warning");
		cb_LogLevel.Items.Add("Info");
		cb_LogLevel.Items.Add("Debug");
		cb_LogLevel.Items.Add("All");
		cb_LogLevel.SelectedIndex = 5;
		ServersinDC.DoWork += ShowADAuthorizedServers;
		ServersinDC.RunWorkerCompleted += ServersinDC_RunWorkerCompleted;
		ServersinDC.RunWorkerAsync();
		DisplayInterfaces(IPVersion.IPv4);
		string wellKnownServersPath = GetWellKnownServersPath();
		FileStream fileStream = new FileStream(wellKnownServersPath, FileMode.OpenOrCreate);
		fileStream.Close();
		loadFile(wellKnownServersPath);
	}

	private void ServersinDC_RunWorkerCompleted(object Sender, RunWorkerCompletedEventArgs e)
	{
	}

	private void UpdateDCQueryStatus(object StateInfo)
	{
	}

	private string GetWellKnownServersPath()
	{
		string currentDirectory = Directory.GetCurrentDirectory();
		return currentDirectory + "\\WellKnownServers.xml";
	}

	private void loadFile(string szFile)
	{
		try
		{
			XmlTextReader xmlTextReader = new XmlTextReader(szFile);
			bool flag = false;
			while (xmlTextReader.Read())
			{
				switch (xmlTextReader.NodeType)
				{
				case XmlNodeType.Element:
					if (xmlTextReader.Name.Equals("IP"))
					{
						flag = true;
					}
					break;
				case XmlNodeType.Text:
					if (flag)
					{
						string item = xmlTextReader.Value.Trim();
						WellKnownServers.Add(item);
					}
					break;
				case XmlNodeType.EndElement:
					flag = false;
					break;
				}
			}
			xmlTextReader.Close();
		}
		catch (XmlException)
		{
		}
	}

	private void RogueChecker_FormClosed(object sender, FormClosedEventArgs e)
	{
	}

	private void RogueChecker_Resize(object sender, EventArgs e)
	{
		if (FormWindowState.Minimized == base.WindowState)
		{
			Hide();
		}
	}

	private void btn_OK_Click(object sender, EventArgs e)
	{
		Cursor.Current = Cursors.WaitCursor;
		if (!AssignNICBindInfo())
		{
			return;
		}
		if (uiData.RogueServers != null && uiData.RogueServers.Count > 0)
		{
			uiData.RogueServers.Clear();
		}
		for (int i = 0; i < dgv_AuthSrvrIP.Rows.Count && i + 1 != dgv_AuthSrvrIP.Rows.Count; i++)
		{
			uiData.AddAuthorizedServer(dgv_AuthSrvrIP.Rows[i].Cells[0].Value.ToString());
		}
		if (rb_Interval.Checked)
		{
			try
			{
				Thread thread = new Thread(ScheduleDHCPDetectionInThread);
				int num = (int)nud_Interval.Value;
				thread.Start(num);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		try
		{
			Thread thread2 = new Thread(sendPacket.SendDHCPPacket);
			thread2.Start(ServerReplied);
			ServerReplied.WaitOne();
			ClearContents();
			UpdateStatus();
		}
		catch (Exception)
		{
		}
	}

	private void btn_Close_Click(object sender, EventArgs e)
	{
		if (sendPacket.RcvPacket != null)
		{
			sendPacket.RcvPacket.StopListener();
		}
		if (timerRogueStart != null)
		{
			timerRogueStart.Dispose();
		}
		UpdateWellKnownServers();
		Close();
	}

	private void UpdateWellKnownServers()
	{
		string wellKnownServersPath = GetWellKnownServersPath();
		FileStream fileStream = new FileStream(wellKnownServersPath, FileMode.Truncate);
		StreamWriter streamWriter = new StreamWriter(fileStream);
		streamWriter.WriteLine("<WellKnownServers>");
		foreach (string wellKnownServer in WellKnownServers)
		{
			streamWriter.WriteLine("<IP>" + wellKnownServer + "</IP>");
		}
		streamWriter.WriteLine("</WellKnownServers>");
		streamWriter.Close();
		fileStream.Close();
	}

	private void button1_Click(object sender, EventArgs e)
	{
		folderBrowserDialog1.SelectedPath = txt_LogPath.Text;
		if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
		{
			txt_LogPath.Text = folderBrowserDialog1.SelectedPath;
		}
	}

	private void rb_Interval_Click(object sender, EventArgs e)
	{
		nud_Interval.Enabled = true;
	}

	private void rb_OneTime_Click(object sender, EventArgs e)
	{
		nud_Interval.Enabled = false;
	}

	private void dgv_RogueIP_CellContentClick(object sender, DataGridViewCellEventArgs e)
	{
		_ = e.ColumnIndex;
	}

	private void dgv_RogueIP_CellValueChanged(object sender, DataGridViewCellEventArgs e)
	{
		if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
		{
			if ((bool)dgv_RogueIP[e.ColumnIndex, e.RowIndex].Value)
			{
				WellKnownServers.Add(dgv_RogueIP[1, e.RowIndex].Value.ToString());
				dgv_RogueIP.Rows[e.RowIndex].ErrorText = "";
			}
			else
			{
				WellKnownServers.Remove(dgv_RogueIP[1, e.RowIndex].Value.ToString());
				dgv_RogueIP.Rows[e.RowIndex].ErrorText = "Rogue Server";
			}
		}
	}

	private void dgv_RogueIP_CurrentCellDirtyStateChanged(object sender, EventArgs e)
	{
		dgv_RogueIP.CommitEdit(DataGridViewDataErrorContexts.Commit);
	}

	private void NI_Rogue_DoubleClick(object sender, EventArgs e)
	{
		Show();
		base.WindowState = FormWindowState.Normal;
	}

	private void UpdateStatus()
	{
		CleanDHCPRogueObjects();
		if (lbl_Status.InvokeRequired)
		{
			SetTextCallback method = UpdateStatus;
			BeginInvoke(method);
			return;
		}
		int rogueCount = GetRogueCount();
		if (rogueCount > 0)
		{
			lbl_Status.Visible = true;
			lbl_Status.ForeColor = Color.Red;
			lbl_Status.Text = " Number of Rogue Servers Detected: " + rogueCount;
			NI_Rogue.Icon = Resources.Warning;
			NI_Rogue.Text = lbl_Status.Text;
		}
		else
		{
			lbl_Status.Visible = true;
			lbl_Status.ForeColor = Color.Green;
			lbl_Status.Text = " NONE Rogue Servers Detected ";
			NI_Rogue.Icon = Resources.RogueDetect;
			NI_Rogue.Text = "Rogue Server";
		}
	}

	private int GetRogueCount()
	{
		int num = 0;
		if (uiData.RogueServers != null)
		{
			foreach (RogueData rogueServer in uiData.RogueServers)
			{
				if (!rogueServer.IsKnown)
				{
					num++;
				}
			}
		}
		return num;
	}

	private void CleanDHCPRogueObjects()
	{
		if (uiData.RogueServers == null)
		{
			return;
		}
		for (int num = uiData.RogueServers.Count - 1; num >= 0; num--)
		{
			if (string.IsNullOrEmpty(uiData.RogueServers[num].ServerIP))
			{
				uiData.RogueServers.RemoveAt(num);
			}
		}
	}

	private void ClearContents()
	{
		if (dgv_RogueIP.InvokeRequired)
		{
			SetTextCallback method = ClearContents;
			BeginInvoke(method);
		}
		else
		{
			dgv_RogueIP.Rows.Clear();
		}
	}

	private void UpdateRogue()
	{
		ClearContents();
		List<RogueData> rogueServers = uiData.RogueServers;
		if (rogueServers == null)
		{
			return;
		}
		foreach (RogueData item in rogueServers)
		{
			if (!string.IsNullOrEmpty(item.ServerIP))
			{
				DataGridViewRow dataGridViewRow = new DataGridViewRow();
				DataGridViewCheckBoxCell dataGridViewCheckBoxCell = new DataGridViewCheckBoxCell();
				dataGridViewCheckBoxCell.Value = (item.IsKnown = IsKnown(item.ServerIP));
				DataGridViewTextBoxCell dataGridViewTextBoxCell = new DataGridViewTextBoxCell();
				dataGridViewTextBoxCell.Value = item.ServerIP;
				DataGridViewTextBoxCell dataGridViewTextBoxCell2 = new DataGridViewTextBoxCell();
				dataGridViewTextBoxCell2.Value = item.ClientIP;
				DataGridViewTextBoxCell dataGridViewTextBoxCell3 = new DataGridViewTextBoxCell();
				dataGridViewTextBoxCell3.Value = item.GatewayIP;
				DataGridViewTextBoxCell dataGridViewTextBoxCell4 = new DataGridViewTextBoxCell();
				dataGridViewTextBoxCell4.Value = item.ResponseTime.Milliseconds.ToString();
				dataGridViewRow.Cells.Add(dataGridViewCheckBoxCell);
				dataGridViewRow.Cells.Add(dataGridViewTextBoxCell);
				dataGridViewRow.Cells.Add(dataGridViewTextBoxCell2);
				dataGridViewRow.Cells.Add(dataGridViewTextBoxCell3);
				dataGridViewRow.Cells.Add(dataGridViewTextBoxCell4);
				if (!(bool)dataGridViewCheckBoxCell.Value)
				{
					dataGridViewRow.ErrorText = "Rogue Server";
				}
				if (dgv_RogueIP.InvokeRequired)
				{
					SetTextCallback method = UpdateRogue;
					BeginInvoke(method);
				}
				else
				{
					dgv_RogueIP.Rows.Add(dataGridViewRow);
					dataGridViewRow.Cells[0].ReadOnly = false;
				}
			}
		}
		UpdateStatus();
	}

	private bool IsKnown(string IP)
	{
		foreach (string wellKnownServer in WellKnownServers)
		{
			if (wellKnownServer == IP)
			{
				return true;
			}
		}
		return false;
	}

	private void ScheduleDHCPDetectionInThread(object obj)
	{
		Cursor.Current = Cursors.WaitCursor;
		int num = (int)obj;
		AutoResetEvent autoResetEvent = new AutoResetEvent(initialState: false);
		TimerCallback callback = Scheduler;
		timerRogueStart = new System.Threading.Timer(callback, autoResetEvent, 0, num * 60 * 1000);
	}

	private void Scheduler(object StateInfo)
	{
		ClearContents();
		if (uiData.RogueServers != null && uiData.RogueServers.Count > 0)
		{
			uiData.RogueServers.Clear();
		}
		Thread thread = new Thread(sendPacket.SendDHCPPacket);
		thread.Start(ServerReplied);
		ServerReplied.WaitOne();
	}

	private void DisplayInterfaces(IPVersion IPv)
	{
		lb_InterfaceToBind.Items.Clear();
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		byte b = 0;
		NetworkInterface[] array = allNetworkInterfaces;
		foreach (NetworkInterface networkInterface in array)
		{
			IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
			UnicastIPAddressInformationCollection unicastAddresses = iPProperties.UnicastAddresses;
			if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback || networkInterface.OperationalStatus == OperationalStatus.Down)
			{
				continue;
			}
			foreach (UnicastIPAddressInformation item in unicastAddresses)
			{
				if (item.Address.AddressFamily == AddressFamily.InterNetworkV6 && IPv == IPVersion.IPv6)
				{
					lb_InterfaceToBind.Items.Add(item.Address.ToString());
				}
				else if (item.Address.AddressFamily == AddressFamily.InterNetwork && IPv == IPVersion.IPv4)
				{
					lb_InterfaceToBind.Items.Add(item.Address.ToString());
					lb_InterfaceToBind.SetItemChecked(b++, value: true);
				}
			}
		}
	}

	private void ShowADAuthorizedServers(object sender, DoWorkEventArgs e)
	{
		Cursor.Current = Cursors.WaitCursor;
		int num = 0;
		try
		{
			DirectoryEntry directoryEntry = new DirectoryEntry("LDAP://RootDSE");
			string text = directoryEntry.Properties["rootDomainNamingContext"].Value.ToString();
			DirectorySearcher directorySearcher = new DirectorySearcher();
			directorySearcher.SearchRoot = new DirectoryEntry("LDAP://cn=netservices, cn=services, cn=configuration, " + text);
			directorySearcher.Filter = "(objectClass=dHCPClass)";
			directorySearcher.SearchScope = SearchScope.OneLevel;
			directorySearcher.PropertiesToLoad.Insert(0, "dhcpServers");
			SearchResultCollection searchResultCollection = directorySearcher.FindAll();
			foreach (SearchResult item in searchResultCollection)
			{
				if (item.Properties.Count <= 1)
				{
					continue;
				}
				foreach (string propertyName in item.Properties.PropertyNames)
				{
					ResultPropertyValueCollection resultPropertyValueCollection = item.Properties[propertyName];
					if (propertyName == "dhcpservers")
					{
						string[] array = resultPropertyValueCollection[0].ToString().Split('$');
						if (array[1].Contains("rcn="))
						{
							string value = array[0].Remove(0, 1);
							string value2 = array[1].Remove(0, 4);
							DataGridViewRow dataGridViewRow = new DataGridViewRow();
							DataGridViewTextBoxCell dataGridViewTextBoxCell = new DataGridViewTextBoxCell();
							dataGridViewTextBoxCell.Value = value;
							dataGridViewRow.Cells.Add(dataGridViewTextBoxCell);
							DataGridViewTextBoxCell dataGridViewTextBoxCell2 = new DataGridViewTextBoxCell();
							dataGridViewTextBoxCell2.Value = value2;
							dataGridViewRow.Cells.Add(dataGridViewTextBoxCell2);
							UpdateDCServerGrid(dataGridViewRow);
							num++;
						}
					}
				}
			}
			Thread.Sleep(100);
			UpdateDCServerStatus("Number of Authorized servers found: " + num, IsError: false);
		}
		catch (Exception ex)
		{
			UpdateDCServerStatus(ex.Message, IsError: true);
		}
	}

	private void UpdateDCServerStatus(string szStatus, bool IsError)
	{
		if (lbl_AuthoStatus.InvokeRequired)
		{
			SetDCStatusCallback method = UpdateDCServerStatus;
			BeginInvoke(method, szStatus, IsError);
			return;
		}
		lbl_AuthoStatus.Visible = true;
		if (IsError)
		{
			lbl_AuthoStatus.ForeColor = Color.Red;
		}
		lbl_AuthoStatus.Text = szStatus;
		dgv_AuthSrvrIP.Refresh();
	}

	private void UpdateDCServerGrid(DataGridViewRow RowToAdd)
	{
		if (dgv_AuthSrvrIP.InvokeRequired)
		{
			SetDCGridCallback method = UpdateDCServerGrid;
			BeginInvoke(method, RowToAdd);
		}
		else
		{
			dgv_AuthSrvrIP.Rows.Add(RowToAdd);
			dgv_AuthSrvrIP.Refresh();
		}
	}

	private bool AssignNICBindInfo()
	{
		sendPacket.NICtoBind = new List<string>();
		CheckedListBox.CheckedItemCollection checkedItems = lb_InterfaceToBind.CheckedItems;
		if (checkedItems.Count <= 0)
		{
			MessageBox.Show("Atleast One NIC to be selected for this tool to work", "Rogue Detection", MessageBoxButtons.OK);
			return false;
		}
		for (int i = 0; i < checkedItems.Count; i++)
		{
			sendPacket.NICtoBind.Add(checkedItems[i].ToString());
		}
		return true;
	}

	private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		LicenseDlg licenseDlg = new LicenseDlg();
		licenseDlg.ShowDialog();
	}

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
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(global::RogueChecker.RogueChecker));
		this.btn_OK = new System.Windows.Forms.Button();
		this.btn_Close = new System.Windows.Forms.Button();
		this.ep_AuthIP = new System.Windows.Forms.ErrorProvider(this.components);
		this.Tab_Main = new System.Windows.Forms.TabControl();
		this.tabProcessing = new System.Windows.Forms.TabPage();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this.lbl_AuthoStatus = new System.Windows.Forms.Label();
		this.dgv_AuthSrvrIP = new System.Windows.Forms.DataGridView();
		this.DHCP_IP_ADDR = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.DNCP_SERVER_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.groupBox3 = new System.Windows.Forms.GroupBox();
		this.lbl_Status = new System.Windows.Forms.Label();
		this.dgv_RogueIP = new System.Windows.Forms.DataGridView();
		this.btnAdd = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.DHCP_CLIENT_IP = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.DHCP_SERVER_GATEWAY = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.DHCP_SERVER_RESPONSE_TIME = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabConfiguration = new System.Windows.Forms.TabPage();
		this.groupBox5 = new System.Windows.Forms.GroupBox();
		this.button1 = new System.Windows.Forms.Button();
		this.txt_LogPath = new System.Windows.Forms.TextBox();
		this.cb_LogLevel = new System.Windows.Forms.ComboBox();
		this.label2 = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.groupBox4 = new System.Windows.Forms.GroupBox();
		this.lb_InterfaceToBind = new System.Windows.Forms.CheckedListBox();
		this.groupBox2 = new System.Windows.Forms.GroupBox();
		this.rb_Interval = new System.Windows.Forms.RadioButton();
		this.rb_OneTime = new System.Windows.Forms.RadioButton();
		this.label3 = new System.Windows.Forms.Label();
		this.nud_Interval = new System.Windows.Forms.NumericUpDown();
		this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
		this.NI_Rogue = new System.Windows.Forms.NotifyIcon(this.components);
		this.ServersinDC = new System.ComponentModel.BackgroundWorker();
		this.linkLabel1 = new System.Windows.Forms.LinkLabel();
		((System.ComponentModel.ISupportInitialize)this.ep_AuthIP).BeginInit();
		this.Tab_Main.SuspendLayout();
		this.tabProcessing.SuspendLayout();
		this.groupBox1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dgv_AuthSrvrIP).BeginInit();
		this.groupBox3.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dgv_RogueIP).BeginInit();
		this.tabConfiguration.SuspendLayout();
		this.groupBox5.SuspendLayout();
		this.groupBox4.SuspendLayout();
		this.groupBox2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.nud_Interval).BeginInit();
		base.SuspendLayout();
		this.btn_OK.Location = new System.Drawing.Point(347, 166);
		this.btn_OK.Margin = new System.Windows.Forms.Padding(4);
		this.btn_OK.Name = "btn_OK";
		this.btn_OK.Size = new System.Drawing.Size(213, 28);
		this.btn_OK.TabIndex = 23;
		this.btn_OK.Text = "Detect Rogue Servers";
		this.btn_OK.UseVisualStyleBackColor = true;
		this.btn_OK.Click += new System.EventHandler(btn_OK_Click);
		this.btn_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btn_Close.Location = new System.Drawing.Point(533, 484);
		this.btn_Close.Margin = new System.Windows.Forms.Padding(4);
		this.btn_Close.Name = "btn_Close";
		this.btn_Close.Size = new System.Drawing.Size(100, 28);
		this.btn_Close.TabIndex = 19;
		this.btn_Close.Text = "Exit";
		this.btn_Close.UseVisualStyleBackColor = true;
		this.btn_Close.Click += new System.EventHandler(btn_Close_Click);
		this.ep_AuthIP.ContainerControl = this;
		this.Tab_Main.Controls.Add(this.tabProcessing);
		this.Tab_Main.Controls.Add(this.tabConfiguration);
		this.Tab_Main.Location = new System.Drawing.Point(27, 22);
		this.Tab_Main.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.Tab_Main.Name = "Tab_Main";
		this.Tab_Main.SelectedIndex = 0;
		this.Tab_Main.Size = new System.Drawing.Size(607, 455);
		this.Tab_Main.TabIndex = 26;
		this.tabProcessing.Controls.Add(this.groupBox1);
		this.tabProcessing.Controls.Add(this.groupBox3);
		this.tabProcessing.Location = new System.Drawing.Point(4, 25);
		this.tabProcessing.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.tabProcessing.Name = "tabProcessing";
		this.tabProcessing.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.tabProcessing.Size = new System.Drawing.Size(599, 426);
		this.tabProcessing.TabIndex = 0;
		this.tabProcessing.Text = "Processing";
		this.tabProcessing.UseVisualStyleBackColor = true;
		this.groupBox1.Controls.Add(this.lbl_AuthoStatus);
		this.groupBox1.Controls.Add(this.dgv_AuthSrvrIP);
		this.groupBox1.Location = new System.Drawing.Point(7, 217);
		this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox1.Size = new System.Drawing.Size(577, 203);
		this.groupBox1.TabIndex = 23;
		this.groupBox1.TabStop = false;
		this.groupBox1.Text = "AD Authorized Microsoft DHCP Servers";
		this.lbl_AuthoStatus.AutoSize = true;
		this.lbl_AuthoStatus.ForeColor = System.Drawing.Color.ForestGreen;
		this.lbl_AuthoStatus.Location = new System.Drawing.Point(17, 171);
		this.lbl_AuthoStatus.Name = "lbl_AuthoStatus";
		this.lbl_AuthoStatus.Size = new System.Drawing.Size(309, 17);
		this.lbl_AuthoStatus.TabIndex = 28;
		this.lbl_AuthoStatus.Text = "Updating DHCP Servers from Domain Controller";
		this.dgv_AuthSrvrIP.AllowUserToAddRows = false;
		this.dgv_AuthSrvrIP.AllowUserToDeleteRows = false;
		this.dgv_AuthSrvrIP.AllowUserToResizeColumns = false;
		this.dgv_AuthSrvrIP.AllowUserToResizeRows = false;
		this.dgv_AuthSrvrIP.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dgv_AuthSrvrIP.Columns.AddRange(this.DHCP_IP_ADDR, this.DNCP_SERVER_Name);
		this.dgv_AuthSrvrIP.Location = new System.Drawing.Point(11, 28);
		this.dgv_AuthSrvrIP.Margin = new System.Windows.Forms.Padding(4);
		this.dgv_AuthSrvrIP.Name = "dgv_AuthSrvrIP";
		this.dgv_AuthSrvrIP.RowTemplate.Height = 24;
		this.dgv_AuthSrvrIP.Size = new System.Drawing.Size(548, 129);
		this.dgv_AuthSrvrIP.TabIndex = 9;
		this.DHCP_IP_ADDR.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
		this.DHCP_IP_ADDR.HeaderText = "IPv4 Address";
		this.DHCP_IP_ADDR.Name = "DHCP_IP_ADDR";
		this.DHCP_IP_ADDR.ReadOnly = true;
		this.DHCP_IP_ADDR.Width = 116;
		this.DNCP_SERVER_Name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.DNCP_SERVER_Name.HeaderText = "Name";
		this.DNCP_SERVER_Name.Name = "DNCP_SERVER_Name";
		this.DNCP_SERVER_Name.ReadOnly = true;
		this.groupBox3.Controls.Add(this.lbl_Status);
		this.groupBox3.Controls.Add(this.dgv_RogueIP);
		this.groupBox3.Controls.Add(this.btn_OK);
		this.groupBox3.Location = new System.Drawing.Point(5, 7);
		this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox3.Name = "groupBox3";
		this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox3.Size = new System.Drawing.Size(579, 202);
		this.groupBox3.TabIndex = 24;
		this.groupBox3.TabStop = false;
		this.groupBox3.Text = "Discovered DHCP Servers in the subnet";
		this.lbl_Status.AutoSize = true;
		this.lbl_Status.ForeColor = System.Drawing.Color.Red;
		this.lbl_Status.Location = new System.Drawing.Point(19, 174);
		this.lbl_Status.Name = "lbl_Status";
		this.lbl_Status.Size = new System.Drawing.Size(68, 17);
		this.lbl_Status.TabIndex = 27;
		this.lbl_Status.Text = "lbl_status";
		this.lbl_Status.Visible = false;
		this.dgv_RogueIP.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dgv_RogueIP.Columns.AddRange(this.btnAdd, this.dataGridViewTextBoxColumn1, this.DHCP_CLIENT_IP, this.DHCP_SERVER_GATEWAY, this.DHCP_SERVER_RESPONSE_TIME);
		this.dgv_RogueIP.Location = new System.Drawing.Point(8, 25);
		this.dgv_RogueIP.Margin = new System.Windows.Forms.Padding(4);
		this.dgv_RogueIP.Name = "dgv_RogueIP";
		this.dgv_RogueIP.RowTemplate.Height = 24;
		this.dgv_RogueIP.Size = new System.Drawing.Size(552, 135);
		this.dgv_RogueIP.TabIndex = 9;
		this.dgv_RogueIP.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(dgv_RogueIP_CellValueChanged);
		this.dgv_RogueIP.CurrentCellDirtyStateChanged += new System.EventHandler(dgv_RogueIP_CurrentCellDirtyStateChanged);
		this.dgv_RogueIP.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(dgv_RogueIP_CellContentClick);
		this.btnAdd.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.btnAdd.FillWeight = 80f;
		this.btnAdd.HeaderText = "Valid DHCP Server";
		this.btnAdd.Name = "btnAdd";
		this.btnAdd.ReadOnly = true;
		this.btnAdd.Resizable = System.Windows.Forms.DataGridViewTriState.True;
		this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.dataGridViewTextBoxColumn1.FillWeight = 93.27411f;
		this.dataGridViewTextBoxColumn1.HeaderText = "Server IP";
		this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
		this.dataGridViewTextBoxColumn1.ReadOnly = true;
		this.DHCP_CLIENT_IP.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.DHCP_CLIENT_IP.FillWeight = 93.27411f;
		this.DHCP_CLIENT_IP.HeaderText = "Offered Client IP";
		this.DHCP_CLIENT_IP.Name = "DHCP_CLIENT_IP";
		this.DHCP_CLIENT_IP.ReadOnly = true;
		this.DHCP_SERVER_GATEWAY.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.DHCP_SERVER_GATEWAY.FillWeight = 93.27411f;
		this.DHCP_SERVER_GATEWAY.HeaderText = "Gateway Address";
		this.DHCP_SERVER_GATEWAY.Name = "DHCP_SERVER_GATEWAY";
		this.DHCP_SERVER_GATEWAY.ReadOnly = true;
		this.DHCP_SERVER_RESPONSE_TIME.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
		this.DHCP_SERVER_RESPONSE_TIME.FillWeight = 70f;
		this.DHCP_SERVER_RESPONSE_TIME.HeaderText = "Response Time (ms)";
		this.DHCP_SERVER_RESPONSE_TIME.Name = "DHCP_SERVER_RESPONSE_TIME";
		this.DHCP_SERVER_RESPONSE_TIME.ReadOnly = true;
		this.tabConfiguration.Controls.Add(this.groupBox5);
		this.tabConfiguration.Controls.Add(this.groupBox4);
		this.tabConfiguration.Controls.Add(this.groupBox2);
		this.tabConfiguration.Location = new System.Drawing.Point(4, 25);
		this.tabConfiguration.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.tabConfiguration.Name = "tabConfiguration";
		this.tabConfiguration.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.tabConfiguration.Size = new System.Drawing.Size(599, 426);
		this.tabConfiguration.TabIndex = 1;
		this.tabConfiguration.Text = "Configuration";
		this.tabConfiguration.UseVisualStyleBackColor = true;
		this.groupBox5.Controls.Add(this.button1);
		this.groupBox5.Controls.Add(this.txt_LogPath);
		this.groupBox5.Controls.Add(this.cb_LogLevel);
		this.groupBox5.Controls.Add(this.label2);
		this.groupBox5.Controls.Add(this.label1);
		this.groupBox5.Location = new System.Drawing.Point(40, 305);
		this.groupBox5.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.groupBox5.Name = "groupBox5";
		this.groupBox5.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.groupBox5.Size = new System.Drawing.Size(549, 98);
		this.groupBox5.TabIndex = 29;
		this.groupBox5.TabStop = false;
		this.groupBox5.Text = "Log Details";
		this.groupBox5.Visible = false;
		this.button1.Location = new System.Drawing.Point(392, 32);
		this.button1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(87, 25);
		this.button1.TabIndex = 4;
		this.button1.Text = "Browse ...";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Visible = false;
		this.button1.Click += new System.EventHandler(button1_Click);
		this.txt_LogPath.Location = new System.Drawing.Point(84, 34);
		this.txt_LogPath.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.txt_LogPath.Name = "txt_LogPath";
		this.txt_LogPath.Size = new System.Drawing.Size(281, 22);
		this.txt_LogPath.TabIndex = 3;
		this.txt_LogPath.Visible = false;
		this.cb_LogLevel.FormattingEnabled = true;
		this.cb_LogLevel.Location = new System.Drawing.Point(84, 64);
		this.cb_LogLevel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.cb_LogLevel.Name = "cb_LogLevel";
		this.cb_LogLevel.Size = new System.Drawing.Size(281, 24);
		this.cb_LogLevel.TabIndex = 2;
		this.cb_LogLevel.Visible = false;
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(21, 66);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(42, 17);
		this.label2.TabIndex = 1;
		this.label2.Text = "Level";
		this.label2.Visible = false;
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(19, 34);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(37, 17);
		this.label1.TabIndex = 0;
		this.label1.Text = "Path";
		this.label1.Visible = false;
		this.groupBox4.Controls.Add(this.lb_InterfaceToBind);
		this.groupBox4.Location = new System.Drawing.Point(40, 15);
		this.groupBox4.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.groupBox4.Name = "groupBox4";
		this.groupBox4.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.groupBox4.Size = new System.Drawing.Size(551, 146);
		this.groupBox4.TabIndex = 28;
		this.groupBox4.TabStop = false;
		this.groupBox4.Text = "Discover using the selected IPv4 interfaces";
		this.lb_InterfaceToBind.FormattingEnabled = true;
		this.lb_InterfaceToBind.Location = new System.Drawing.Point(19, 27);
		this.lb_InterfaceToBind.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		this.lb_InterfaceToBind.Name = "lb_InterfaceToBind";
		this.lb_InterfaceToBind.Size = new System.Drawing.Size(525, 89);
		this.lb_InterfaceToBind.TabIndex = 28;
		this.groupBox2.Controls.Add(this.rb_Interval);
		this.groupBox2.Controls.Add(this.rb_OneTime);
		this.groupBox2.Controls.Add(this.label3);
		this.groupBox2.Controls.Add(this.nud_Interval);
		this.groupBox2.Location = new System.Drawing.Point(40, 175);
		this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox2.Name = "groupBox2";
		this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox2.Size = new System.Drawing.Size(549, 103);
		this.groupBox2.TabIndex = 26;
		this.groupBox2.TabStop = false;
		this.groupBox2.Text = "Discovery scheduler";
		this.rb_Interval.AutoSize = true;
		this.rb_Interval.Location = new System.Drawing.Point(19, 62);
		this.rb_Interval.Margin = new System.Windows.Forms.Padding(4);
		this.rb_Interval.Name = "rb_Interval";
		this.rb_Interval.Size = new System.Drawing.Size(96, 21);
		this.rb_Interval.TabIndex = 15;
		this.rb_Interval.Text = "Frequency";
		this.rb_Interval.UseVisualStyleBackColor = true;
		this.rb_Interval.Click += new System.EventHandler(rb_Interval_Click);
		this.rb_OneTime.AutoSize = true;
		this.rb_OneTime.Checked = true;
		this.rb_OneTime.Location = new System.Drawing.Point(19, 33);
		this.rb_OneTime.Margin = new System.Windows.Forms.Padding(4);
		this.rb_OneTime.Name = "rb_OneTime";
		this.rb_OneTime.Size = new System.Drawing.Size(86, 21);
		this.rb_OneTime.TabIndex = 14;
		this.rb_OneTime.TabStop = true;
		this.rb_OneTime.Text = "One time";
		this.rb_OneTime.UseVisualStyleBackColor = true;
		this.rb_OneTime.Click += new System.EventHandler(rb_OneTime_Click);
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(219, 66);
		this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(57, 17);
		this.label3.TabIndex = 13;
		this.label3.Text = "Minutes";
		this.nud_Interval.Enabled = false;
		this.nud_Interval.Location = new System.Drawing.Point(123, 63);
		this.nud_Interval.Margin = new System.Windows.Forms.Padding(4);
		this.nud_Interval.Name = "nud_Interval";
		this.nud_Interval.Size = new System.Drawing.Size(81, 22);
		this.nud_Interval.TabIndex = 12;
		this.NI_Rogue.Icon = (System.Drawing.Icon)resources.GetObject("NI_Rogue.Icon");
		this.NI_Rogue.Text = "RogueDetect";
		this.NI_Rogue.Visible = true;
		this.NI_Rogue.DoubleClick += new System.EventHandler(NI_Rogue_DoubleClick);
		this.linkLabel1.AutoSize = true;
		this.linkLabel1.Location = new System.Drawing.Point(499, 9);
		this.linkLabel1.Name = "linkLabel1";
		this.linkLabel1.Size = new System.Drawing.Size(131, 17);
		this.linkLabel1.TabIndex = 27;
		this.linkLabel1.TabStop = true;
		this.linkLabel1.Text = "Terms && Conditions";
		this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linkLabel1_LinkClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 16f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(640, 519);
		base.Controls.Add(this.linkLabel1);
		base.Controls.Add(this.Tab_Main);
		base.Controls.Add(this.btn_Close);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
		base.Name = "RogueChecker";
		this.Text = "Subnet based Rogue Detection";
		base.Load += new System.EventHandler(RogueChecker_Load);
		base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(RogueChecker_FormClosed);
		base.Resize += new System.EventHandler(RogueChecker_Resize);
		((System.ComponentModel.ISupportInitialize)this.ep_AuthIP).EndInit();
		this.Tab_Main.ResumeLayout(false);
		this.tabProcessing.ResumeLayout(false);
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.dgv_AuthSrvrIP).EndInit();
		this.groupBox3.ResumeLayout(false);
		this.groupBox3.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.dgv_RogueIP).EndInit();
		this.tabConfiguration.ResumeLayout(false);
		this.groupBox5.ResumeLayout(false);
		this.groupBox5.PerformLayout();
		this.groupBox4.ResumeLayout(false);
		this.groupBox2.ResumeLayout(false);
		this.groupBox2.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.nud_Interval).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
