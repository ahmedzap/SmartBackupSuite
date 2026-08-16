using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using SmartBackupSuite.Helpers;
using SmartBackupSuite.Models;
using SmartBackupSuite.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SmartBackupSuite.Forms
{
    public partial class ConnectionForm : Form
    {
        private ServerConnection _connection;
        private bool _isEditMode;
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.ComboBox cmbAuthType;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDiscoverServers;
        private System.Windows.Forms.Label lblServerName;
        private System.Windows.Forms.Label lblAuthType;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.CheckBox chkIsActive;

        // ✅ عناصر جديدة لعرض المثيلات
        private System.Windows.Forms.Label lblInstances;
        private System.Windows.Forms.ListBox lstInstances;
        private System.Windows.Forms.Button btnRefreshInstances;
        private System.Windows.Forms.Label lblInstanceInfo;

        // ✅ قائمة المثيلات المكتشفة
        private List<string> _discoveredInstances = new List<string>();

        // ✅ اسم الجهاز المحلي
        private string _machineName = Environment.MachineName;

        public ServerConnection Connection => _connection;

        public ConnectionForm(ServerConnection connection = null)
        {
            InitializeComponent();
            ApplyCustomTheme();
            _isEditMode = connection != null;

            if (_isEditMode)
            {
                _connection = connection;
                LoadConnectionData();
            }
            else
            {
                _connection = new ServerConnection();
            }

            // ✅ تحميل المثيلات تلقائياً عند فتح النموذج
            DiscoverInstances();
        }

        private void ApplyCustomTheme()
        {
            var cyanLight = System.Drawing.Color.FromArgb(224, 247, 250);
            var cyanDark = System.Drawing.Color.FromArgb(0, 150, 180);
            var goldColor = System.Drawing.Color.FromArgb(212, 175, 55);
            var darkGold = System.Drawing.Color.FromArgb(184, 134, 11);
            var whiteColor = System.Drawing.Color.White;
            var darkText = System.Drawing.Color.FromArgb(20, 20, 20);

            this.BackColor = cyanLight;
            this.ForeColor = darkText;
            this.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // تخصيص زر اكتشاف الخوادم
            btnDiscoverServers.BackColor = System.Drawing.Color.FromArgb(155, 89, 182);
            btnDiscoverServers.ForeColor = whiteColor;
            btnDiscoverServers.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(142, 68, 173);
            btnDiscoverServers.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(120, 50, 150);

            // تخصيص زر تحديث المثيلات
            btnRefreshInstances.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            btnRefreshInstances.ForeColor = whiteColor;
            btnRefreshInstances.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            btnRefreshInstances.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(30, 100, 150);

            // تخصيص زر اختبار الاتصال
            btnTestConnection.BackColor = cyanDark;
            btnTestConnection.ForeColor = whiteColor;
            btnTestConnection.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(0, 120, 150);
            btnTestConnection.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 90, 110);

            // تخصيص زر الحفظ
            btnSave.BackColor = goldColor;
            btnSave.ForeColor = darkText;
            btnSave.FlatAppearance.MouseOverBackColor = darkGold;
            btnSave.FlatAppearance.MouseDownBackColor = darkGold;

            // تخصيص زر الإلغاء
            btnCancel.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
            btnCancel.ForeColor = whiteColor;
            btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(180, 30, 30);
            btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(160, 20, 20);

            // تخصيص عناصر القائمة
            lblInstances.ForeColor = cyanDark;
            lblInstances.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);

            lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            lblInstanceInfo.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular);

            chkIsActive.BackColor = cyanLight;
            chkIsActive.FlatStyle = FlatStyle.Standard;
        }

        private void InitializeComponent()
        {
            txtServerName = new TextBox();
            cmbAuthType = new ComboBox();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            btnTestConnection = new Button();
            btnSave = new Button();
            btnCancel = new Button();
            btnDiscoverServers = new Button();
            lblServerName = new Label();
            lblAuthType = new Label();
            lblUsername = new Label();
            lblPassword = new Label();
            chkIsActive = new CheckBox();
            lblInstances = new Label();
            lstInstances = new ListBox();
            btnRefreshInstances = new Button();
            lblInstanceInfo = new Label();
            SuspendLayout();
            // 
            // txtServerName
            // 
            txtServerName.BackColor = Color.White;
            txtServerName.BorderStyle = BorderStyle.FixedSingle;
            txtServerName.Font = new Font("Segoe UI", 9.5F);
            txtServerName.ForeColor = Color.FromArgb(20, 20, 20);
            txtServerName.Location = new Point(130, 19);
            txtServerName.Name = "txtServerName";
            txtServerName.RightToLeft = RightToLeft.Yes;
            txtServerName.Size = new Size(300, 24);
            txtServerName.TabIndex = 1;
            txtServerName.TextAlign = HorizontalAlignment.Right;
            // 
            // cmbAuthType
            // 
            cmbAuthType.BackColor = Color.White;
            cmbAuthType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAuthType.FlatStyle = FlatStyle.Flat;
            cmbAuthType.ForeColor = Color.FromArgb(20, 20, 20);
            cmbAuthType.Items.AddRange(new object[] { "Windows", "SQL" });
            cmbAuthType.Location = new Point(130, 54);
            cmbAuthType.Name = "cmbAuthType";
            cmbAuthType.RightToLeft = RightToLeft.Yes;
            cmbAuthType.Size = new Size(150, 23);
            cmbAuthType.TabIndex = 3;
            cmbAuthType.SelectedIndexChanged += cmbAuthType_SelectedIndexChanged;
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.White;
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.Font = new Font("Segoe UI", 9.5F);
            txtUsername.ForeColor = Color.FromArgb(20, 20, 20);
            txtUsername.Location = new Point(130, 89);
            txtUsername.Name = "txtUsername";
            txtUsername.RightToLeft = RightToLeft.Yes;
            txtUsername.Size = new Size(300, 24);
            txtUsername.TabIndex = 5;
            txtUsername.TextAlign = HorizontalAlignment.Right;
            // 
            // txtPassword
            // 
            txtPassword.BackColor = Color.White;
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.Font = new Font("Segoe UI", 9.5F);
            txtPassword.ForeColor = Color.FromArgb(20, 20, 20);
            txtPassword.Location = new Point(130, 124);
            txtPassword.Name = "txtPassword";
            txtPassword.RightToLeft = RightToLeft.Yes;
            txtPassword.Size = new Size(300, 24);
            txtPassword.TabIndex = 7;
            txtPassword.TextAlign = HorizontalAlignment.Right;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // btnTestConnection
            // 
            btnTestConnection.BackColor = Color.FromArgb(0, 150, 180);
            btnTestConnection.FlatAppearance.BorderSize = 0;
            btnTestConnection.FlatStyle = FlatStyle.Flat;
            btnTestConnection.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnTestConnection.ForeColor = Color.White;
            btnTestConnection.Location = new Point(242, 320);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.RightToLeft = RightToLeft.Yes;
            btnTestConnection.Size = new Size(130, 35);
            btnTestConnection.TabIndex = 9;
            btnTestConnection.Text = "🔌 اختبار الاتصال";
            btnTestConnection.TextAlign = ContentAlignment.MiddleRight;
            btnTestConnection.UseVisualStyleBackColor = false;
            btnTestConnection.Click += btnTestConnection_Click;
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(212, 175, 55);
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSave.ForeColor = Color.FromArgb(20, 20, 20);
            btnSave.Location = new Point(89, 320);
            btnSave.Name = "btnSave";
            btnSave.RightToLeft = RightToLeft.Yes;
            btnSave.Size = new Size(130, 35);
            btnSave.TabIndex = 10;
            btnSave.Text = "💾 حفظ";
            btnSave.TextAlign = ContentAlignment.MiddleRight;
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(200, 50, 50);
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(410, 320);
            btnCancel.Name = "btnCancel";
            btnCancel.RightToLeft = RightToLeft.Yes;
            btnCancel.Size = new Size(130, 35);
            btnCancel.TabIndex = 11;
            btnCancel.Text = "❌ إلغاء";
            btnCancel.TextAlign = ContentAlignment.MiddleRight;
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnDiscoverServers
            // 
            btnDiscoverServers.BackColor = Color.FromArgb(155, 89, 182);
            btnDiscoverServers.FlatAppearance.BorderSize = 0;
            btnDiscoverServers.FlatStyle = FlatStyle.Flat;
            btnDiscoverServers.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnDiscoverServers.ForeColor = Color.White;
            btnDiscoverServers.Location = new Point(440, 18);
            btnDiscoverServers.Name = "btnDiscoverServers";
            btnDiscoverServers.RightToLeft = RightToLeft.Yes;
            btnDiscoverServers.Size = new Size(100, 25);
            btnDiscoverServers.TabIndex = 2;
            btnDiscoverServers.Text = "🔄 اكتشاف";
            btnDiscoverServers.TextAlign = ContentAlignment.MiddleRight;
            btnDiscoverServers.UseVisualStyleBackColor = false;
            btnDiscoverServers.Click += btnDiscoverServers_Click;
            // 
            // lblServerName
            // 
            lblServerName.AutoSize = true;
            lblServerName.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblServerName.ForeColor = Color.FromArgb(0, 150, 180);
            lblServerName.Location = new Point(15, 22);
            lblServerName.Name = "lblServerName";
            lblServerName.RightToLeft = RightToLeft.Yes;
            lblServerName.Size = new Size(71, 17);
            lblServerName.TabIndex = 0;
            lblServerName.Text = "اسم الخادم:";
            lblServerName.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblAuthType
            // 
            lblAuthType.AutoSize = true;
            lblAuthType.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblAuthType.ForeColor = Color.FromArgb(0, 150, 180);
            lblAuthType.Location = new Point(15, 57);
            lblAuthType.Name = "lblAuthType";
            lblAuthType.RightToLeft = RightToLeft.Yes;
            lblAuthType.Size = new Size(87, 17);
            lblAuthType.TabIndex = 2;
            lblAuthType.Text = "نوع المصادقة:";
            lblAuthType.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblUsername.ForeColor = Color.FromArgb(0, 150, 180);
            lblUsername.Location = new Point(15, 92);
            lblUsername.Name = "lblUsername";
            lblUsername.RightToLeft = RightToLeft.Yes;
            lblUsername.Size = new Size(91, 17);
            lblUsername.TabIndex = 4;
            lblUsername.Text = "اسم المستخدم:";
            lblUsername.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblPassword.ForeColor = Color.FromArgb(0, 150, 180);
            lblPassword.Location = new Point(15, 127);
            lblPassword.Name = "lblPassword";
            lblPassword.RightToLeft = RightToLeft.Yes;
            lblPassword.Size = new Size(73, 17);
            lblPassword.TabIndex = 6;
            lblPassword.Text = "كلمة المرور:";
            lblPassword.TextAlign = ContentAlignment.MiddleRight;
            // 
            // chkIsActive
            // 
            chkIsActive.AutoSize = true;
            chkIsActive.BackColor = Color.FromArgb(224, 247, 250);
            chkIsActive.Checked = true;
            chkIsActive.CheckState = CheckState.Checked;
            chkIsActive.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            chkIsActive.ForeColor = Color.FromArgb(0, 150, 180);
            chkIsActive.Location = new Point(130, 155);
            chkIsActive.Name = "chkIsActive";
            chkIsActive.RightToLeft = RightToLeft.Yes;
            chkIsActive.Size = new Size(89, 21);
            chkIsActive.TabIndex = 8;
            chkIsActive.Text = "اتصال نشط";
            chkIsActive.TextAlign = ContentAlignment.MiddleRight;
            chkIsActive.UseVisualStyleBackColor = false;
            // 
            // lblInstances
            // 
            lblInstances.AutoSize = true;
            lblInstances.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblInstances.ForeColor = Color.FromArgb(0, 150, 180);
            lblInstances.Location = new Point(15, 195);
            lblInstances.Name = "lblInstances";
            lblInstances.RightToLeft = RightToLeft.Yes;
            lblInstances.Size = new Size(109, 17);
            lblInstances.TabIndex = 12;
            lblInstances.Text = "المثيلات المكتشفة:";
            lblInstances.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lstInstances
            // 
            lstInstances.BackColor = Color.White;
            lstInstances.BorderStyle = BorderStyle.FixedSingle;
            lstInstances.Font = new Font("Segoe UI", 9.5F);
            lstInstances.ForeColor = Color.FromArgb(20, 20, 20);
            lstInstances.Location = new Point(130, 193);
            lstInstances.Name = "lstInstances";
            lstInstances.RightToLeft = RightToLeft.Yes;
            lstInstances.Size = new Size(300, 87);
            lstInstances.TabIndex = 13;
            lstInstances.SelectedIndexChanged += lstInstances_SelectedIndexChanged;
            lstInstances.DoubleClick += lstInstances_DoubleClick;
            // 
            // btnRefreshInstances
            // 
            btnRefreshInstances.BackColor = Color.FromArgb(52, 152, 219);
            btnRefreshInstances.FlatAppearance.BorderSize = 0;
            btnRefreshInstances.FlatStyle = FlatStyle.Flat;
            btnRefreshInstances.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnRefreshInstances.ForeColor = Color.White;
            btnRefreshInstances.Location = new Point(440, 193);
            btnRefreshInstances.Name = "btnRefreshInstances";
            btnRefreshInstances.RightToLeft = RightToLeft.Yes;
            btnRefreshInstances.Size = new Size(100, 25);
            btnRefreshInstances.TabIndex = 14;
            btnRefreshInstances.Text = "🔄 تحديث";
            btnRefreshInstances.TextAlign = ContentAlignment.MiddleRight;
            btnRefreshInstances.UseVisualStyleBackColor = false;
            btnRefreshInstances.Click += btnRefreshInstances_Click;
            // 
            // lblInstanceInfo
            // 
            lblInstanceInfo.AutoSize = true;
            lblInstanceInfo.Font = new Font("Segoe UI", 8.5F);
            lblInstanceInfo.ForeColor = Color.FromArgb(100, 100, 100);
            lblInstanceInfo.Location = new Point(130, 295);
            lblInstanceInfo.Name = "lblInstanceInfo";
            lblInstanceInfo.RightToLeft = RightToLeft.Yes;
            lblInstanceInfo.Size = new Size(155, 15);
            lblInstanceInfo.TabIndex = 15;
            lblInstanceInfo.Text = "💡 انقر مرتين على مثيل لاختياره";
            lblInstanceInfo.TextAlign = ContentAlignment.MiddleRight;
            // 
            // ConnectionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 247, 250);
            ClientSize = new Size(560, 380);
            Controls.Add(lblInstanceInfo);
            Controls.Add(btnRefreshInstances);
            Controls.Add(lstInstances);
            Controls.Add(lblInstances);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(btnTestConnection);
            Controls.Add(chkIsActive);
            Controls.Add(txtPassword);
            Controls.Add(txtUsername);
            Controls.Add(cmbAuthType);
            Controls.Add(btnDiscoverServers);
            Controls.Add(txtServerName);
            Controls.Add(lblPassword);
            Controls.Add(lblUsername);
            Controls.Add(lblAuthType);
            Controls.Add(lblServerName);
            Font = new Font("Segoe UI", 9F);
            Name = "ConnectionForm";
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            StartPosition = FormStartPosition.CenterParent;
            Text = "🔌 إدارة اتصال SQL Server";
            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadConnectionData()
        {
            txtServerName.Text = _connection.ServerName;
            cmbAuthType.SelectedItem = _connection.AuthType;
            txtUsername.Text = _connection.Username;
            chkIsActive.Checked = _connection.IsActive;

            if (!string.IsNullOrEmpty(_connection.EncryptedPassword))
            {
                txtPassword.Text = EncryptionHelper.Decrypt(_connection.EncryptedPassword);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isSQLAuth = cmbAuthType.SelectedItem?.ToString() == "SQL";
            txtUsername.Enabled = isSQLAuth;
            txtPassword.Enabled = isSQLAuth;
            lblUsername.Enabled = isSQLAuth;
            lblPassword.Enabled = isSQLAuth;

            if (!isSQLAuth)
            {
                txtUsername.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                txtPassword.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                txtUsername.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);
                txtPassword.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);
            }
            else
            {
                txtUsername.BackColor = System.Drawing.Color.White;
                txtPassword.BackColor = System.Drawing.Color.White;
                txtUsername.ForeColor = System.Drawing.Color.FromArgb(20, 20, 20);
                txtPassword.ForeColor = System.Drawing.Color.FromArgb(20, 20, 20);
            }
        }

        // ============================================================
        // ✅ دالة تحويل localhost إلى اسم الجهاز
        // ============================================================
        private string ReplaceLocalhostWithMachineName(string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
                return serverName;

            // ✅ استبدال localhost باسم الجهاز
            if (serverName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return _machineName;

            // ✅ استبدال localhost\Instance باسم الجهاز\Instance
            if (serverName.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase))
            {
                string instance = serverName.Substring("localhost\\".Length);
                return $"{_machineName}\\{instance}";
            }

            // ✅ استبدال .\Instance باسم الجهاز\Instance
            if (serverName.StartsWith(".\\", StringComparison.OrdinalIgnoreCase))
            {
                string instance = serverName.Substring(".\\".Length);
                return $"{_machineName}\\{instance}";
            }

            // ✅ استبدال (local)\Instance باسم الجهاز\Instance
            if (serverName.StartsWith("(local)\\", StringComparison.OrdinalIgnoreCase))
            {
                string instance = serverName.Substring("(local)\\".Length);
                return $"{_machineName}\\{instance}";
            }

            // ✅ استبدال . باسم الجهاز
            if (serverName.Equals(".", StringComparison.OrdinalIgnoreCase))
                return _machineName;

            // ✅ استبدال (local) باسم الجهاز
            if (serverName.Equals("(local)", StringComparison.OrdinalIgnoreCase))
                return _machineName;

            return serverName;
        }

        // ============================================================
        // ✅ دالة اكتشاف جميع المثيلات
        // ============================================================
        private async void DiscoverInstances()
        {
            try
            {
                btnRefreshInstances.Enabled = false;
                btnDiscoverServers.Enabled = false;
                btnRefreshInstances.Text = "⏳ جاري البحث...";
                btnRefreshInstances.BackColor = System.Drawing.Color.FromArgb(255, 193, 7);

                lstInstances.Items.Clear();
                _discoveredInstances.Clear();

                lstInstances.Items.Add("⏳ جاري اكتشاف المثيلات...");

                var instances = await System.Threading.Tasks.Task.Run(() =>
                {
                    var instanceList = new List<string>();

                    try
                    {
                        // ✅ 1. اكتشاف المثيلات عبر SqlDataSourceEnumerator
                        DataTable serversTable = SqlDataSourceEnumerator.Instance.GetDataSources();

                        if (serversTable != null && serversTable.Rows.Count > 0)
                        {
                            foreach (DataRow row in serversTable.Rows)
                            {
                                string serverName = row["ServerName"]?.ToString() ?? "";
                                string instanceName = row["InstanceName"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(serverName))
                                {
                                    string fullName;
                                    if (!string.IsNullOrEmpty(instanceName))
                                    {
                                        fullName = $"{serverName}\\{instanceName}";
                                    }
                                    else
                                    {
                                        fullName = serverName;
                                    }

                                    // ✅ استبدال localhost باسم الجهاز الفعلي
                                    fullName = ReplaceLocalhostWithMachineName(fullName);
                                    instanceList.Add(fullName);
                                }
                            }
                        }

                        // ✅ 2. إضافة المثيلات المحلية المعروفة
                        if (instanceList.Count == 0)
                        {
                            string[] localInstances = GetLocalInstances();
                            instanceList.AddRange(localInstances);
                        }

                        // ✅ 3. إضافة خوادم شائعة (مع استبدال localhost)
                        string[] commonServers = new[]
                        {
                            _machineName,
                            $"{_machineName}\\SQLEXPRESS",
                            $"{_machineName}\\MSSQLSERVER",
                            "localhost",
                            ".",
                            "(local)",
                            "localhost\\SQLEXPRESS",
                            ".\\SQLEXPRESS",
                            "(local)\\SQLEXPRESS"
                        };

                        foreach (string server in commonServers)
                        {
                            string convertedServer = ReplaceLocalhostWithMachineName(server);
                            if (!instanceList.Contains(convertedServer) && !string.IsNullOrEmpty(convertedServer))
                            {
                                instanceList.Add(convertedServer);
                            }
                        }

                        // ✅ 4. ترتيب القائمة
                        instanceList = instanceList
                            .Distinct()
                            .OrderBy(s => s)
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteLog("Connection", "Warning", $"فشل اكتشاف المثيلات: {ex.Message}");
                        instanceList.AddRange(new[]
                        {
                            _machineName,
                            $"{_machineName}\\SQLEXPRESS",
                            "localhost",
                            ".",
                            "(local)"
                        });
                    }

                    return instanceList;
                });

                // ✅ تحديث القائمة
                lstInstances.Items.Clear();
                _discoveredInstances = instances;

                if (instances.Count > 0)
                {
                    foreach (string instance in instances)
                    {
                        lstInstances.Items.Add(instance);
                    }
                    lstInstances.SelectedIndex = 0;
                    lblInstanceInfo.Text = $"✅ تم العثور على {instances.Count} مثيل";
                    lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                }
                else
                {
                    lstInstances.Items.Add("⚠️ لم يتم العثور على مثيلات");
                    lblInstanceInfo.Text = "⚠️ لم يتم العثور على مثيلات SQL Server";
                    lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(200, 150, 0);
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Connection", "Error", $"خطأ في اكتشاف المثيلات: {ex.Message}");
                lstInstances.Items.Clear();
                lstInstances.Items.Add("❌ خطأ في الاكتشاف");
                lblInstanceInfo.Text = $"❌ خطأ: {ex.Message}";
                lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(200, 50, 50);
            }
            finally
            {
                btnRefreshInstances.Enabled = true;
                btnDiscoverServers.Enabled = true;
                btnRefreshInstances.Text = "🔄 تحديث";
                btnRefreshInstances.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            }
        }

        // ✅ دالة الحصول على المثيلات المحلية (مع استبدال localhost)
        private string[] GetLocalInstances()
        {
            var instances = new List<string>();

            try
            {
                var services = System.ServiceProcess.ServiceController.GetServices();
                foreach (var service in services)
                {
                    try
                    {
                        if (service.ServiceName.StartsWith("MSSQL$"))
                        {
                            string instanceName = service.ServiceName.Replace("MSSQL$", "");
                            if (!string.IsNullOrEmpty(instanceName))
                            {
                                // ✅ استخدام اسم الجهاز بدلاً من localhost
                                instances.Add($"{_machineName}\\{instanceName}");
                            }
                        }
                        else if (service.ServiceName == "MSSQLSERVER")
                        {
                            // ✅ استخدام اسم الجهاز بدلاً من localhost
                            instances.Add(_machineName);
                        }
                    }
                    catch { /* تجاهل الأخطاء الفردية */ }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Connection", "Warning", $"فشل الحصول على المثيلات المحلية: {ex.Message}");
            }

            return instances.ToArray();
        }

        // ============================================================
        // ✅ زر اكتشاف خوادم الشبكة
        // ============================================================
        private async void btnDiscoverServers_Click(object sender, EventArgs e)
        {
            await DiscoverNetworkServers();
        }

        private async Task DiscoverNetworkServers()
        {
            try
            {
                btnDiscoverServers.Enabled = false;
                btnDiscoverServers.Text = "⏳ جاري البحث...";
                btnDiscoverServers.BackColor = System.Drawing.Color.FromArgb(255, 193, 7);

                var servers = await System.Threading.Tasks.Task.Run(() =>
                {
                    var serverList = new List<string>();

                    try
                    {
                        DataTable serversTable = SqlDataSourceEnumerator.Instance.GetDataSources();

                        if (serversTable != null && serversTable.Rows.Count > 0)
                        {
                            foreach (DataRow row in serversTable.Rows)
                            {
                                string serverName = row["ServerName"]?.ToString() ?? "";
                                string instanceName = row["InstanceName"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(serverName))
                                {
                                    string fullName;
                                    if (!string.IsNullOrEmpty(instanceName))
                                    {
                                        fullName = $"{serverName}\\{instanceName}";
                                    }
                                    else
                                    {
                                        fullName = serverName;
                                    }

                                    // ✅ استبدال localhost باسم الجهاز الفعلي
                                    fullName = ReplaceLocalhostWithMachineName(fullName);
                                    serverList.Add(fullName);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteLog("Connection", "Warning", $"فشل اكتشاف خوادم الشبكة: {ex.Message}");
                    }

                    return serverList;
                });

                if (servers.Count > 0)
                {
                    foreach (string server in servers)
                    {
                        if (!_discoveredInstances.Contains(server))
                        {
                            _discoveredInstances.Add(server);
                            lstInstances.Items.Add(server);
                        }
                    }

                    lblInstanceInfo.Text = $"✅ تم تحديث القائمة - {_discoveredInstances.Count} مثيل";
                    lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                }
                else
                {
                    MessageBox.Show(
                        "⚠️ لم يتم العثور على خوادم إضافية في الشبكة.",
                        "اكتشاف الخوادم",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في اكتشاف الخوادم:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDiscoverServers.Enabled = true;
                btnDiscoverServers.Text = "🔄 اكتشاف";
                btnDiscoverServers.BackColor = System.Drawing.Color.FromArgb(155, 89, 182);
            }
        }

        // ============================================================
        // ✅ زر تحديث المثيلات
        // ============================================================
        private void btnRefreshInstances_Click(object sender, EventArgs e)
        {
            DiscoverInstances();
        }

        // ============================================================
        // ✅ عند اختيار مثيل من القائمة
        // ============================================================
        private void lstInstances_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstInstances.SelectedItem != null)
            {
                string selected = lstInstances.SelectedItem.ToString();
                if (!selected.StartsWith("⏳") && !selected.StartsWith("⚠️") && !selected.StartsWith("❌"))
                {
                    txtServerName.Text = selected;
                    lblInstanceInfo.Text = $"✅ تم اختيار: {selected}";
                    lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                }
            }
        }

        // ============================================================
        // ✅ عند الضغط مرتين على مثيل
        // ============================================================
        private async void lstInstances_DoubleClick(object sender, EventArgs e)
        {
            if (lstInstances.SelectedItem != null)
            {
                string selected = lstInstances.SelectedItem.ToString();
                if (!selected.StartsWith("⏳") && !selected.StartsWith("⚠️") && !selected.StartsWith("❌"))
                {
                    txtServerName.Text = selected;
                    lblInstanceInfo.Text = $"⏳ جاري اختبار الاتصال بـ {selected}...";
                    lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(255, 193, 7);

                    await TestConnectionAsync(selected);
                }
            }
        }

        private async Task TestConnectionAsync(string serverName)
        {
            try
            {
                string connectionString = $"Server={serverName};Integrated Security=True;TrustServerCertificate=True;Connection Timeout=5;";

                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    lblInstanceInfo.Text = $"✅ تم الاتصال بـ {serverName} بنجاح!";
                    lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
                }
            }
            catch (Exception ex)
            {
                lblInstanceInfo.Text = $"❌ فشل الاتصال بـ {serverName}: {ex.Message}";
                lblInstanceInfo.ForeColor = System.Drawing.Color.FromArgb(200, 50, 50);
            }
        }

        private void cmbAuthType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                btnTestConnection.Enabled = false;
                btnTestConnection.Text = "⏳ جاري الاختبار...";
                btnTestConnection.BackColor = System.Drawing.Color.FromArgb(255, 193, 7);

                string connectionString = BuildConnectionString();
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    MessageBox.Show("✅ تم الاتصال بنجاح!", "نجاح",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل الاتصال:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestConnection.Enabled = true;
                btnTestConnection.Text = "🔌 اختبار الاتصال";
                btnTestConnection.BackColor = System.Drawing.Color.FromArgb(0, 150, 180);
            }
        }

        private string BuildConnectionString()
        {
            if (cmbAuthType.SelectedItem?.ToString() == "Windows")
            {
                return $"Server={txtServerName.Text};Integrated Security=True;TrustServerCertificate=True;Connection Timeout=10;";
            }
            else
            {
                return $"Server={txtServerName.Text};User Id={txtUsername.Text};Password={txtPassword.Text};TrustServerCertificate=True;Connection Timeout=10;";
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtServerName.Text))
                {
                    MessageBox.Show("الرجاء إدخال اسم الخادم", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtServerName.Focus();
                    return;
                }

                if (cmbAuthType.SelectedItem?.ToString() == "SQL")
                {
                    if (string.IsNullOrWhiteSpace(txtUsername.Text))
                    {
                        MessageBox.Show("الرجاء إدخال اسم المستخدم", "تنبيه",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtUsername.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtPassword.Text))
                    {
                        MessageBox.Show("الرجاء إدخال كلمة المرور", "تنبيه",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPassword.Focus();
                        return;
                    }
                }

                _connection.ServerName = txtServerName.Text.Trim();
                _connection.AuthType = cmbAuthType.SelectedItem?.ToString() ?? "Windows";
                _connection.Username = txtUsername.Text.Trim();
                _connection.IsActive = chkIsActive.Checked;

                if (!string.IsNullOrEmpty(txtPassword.Text))
                {
                    _connection.EncryptedPassword = EncryptionHelper.Encrypt(txtPassword.Text);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}