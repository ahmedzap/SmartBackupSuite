using SmartBackupSuite.Models;
using SmartBackupSuite.Helpers;
using SmartBackupSuite.Services;
using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace SmartBackupSuite.Forms
{
    public partial class SettingsForm : Form
    {
        private AppSettings _settings;
        private bool _isDirty = false;

        // تعريف عناصر التحكم
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.CheckBox chkNotifications;
        private System.Windows.Forms.NumericUpDown numLogRetention;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.TextBox txtSmtpServer;
        private System.Windows.Forms.NumericUpDown numSmtpPort;
        private System.Windows.Forms.TextBox txtSmtpUsername;
        private System.Windows.Forms.TextBox txtSmtpPassword;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblAutoStart;
        private System.Windows.Forms.Label lblNotifications;
        private System.Windows.Forms.Label lblLogRetention;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.Label lblSmtpServer;
        private System.Windows.Forms.Label lblSmtpPort;
        private System.Windows.Forms.Label lblSmtpUsername;
        private System.Windows.Forms.Label lblSmtpPassword;
        private System.Windows.Forms.GroupBox grpGeneral;
        private System.Windows.Forms.GroupBox grpEmail;
        private System.Windows.Forms.GroupBox grpConnections;
        private System.Windows.Forms.DataGridView dgvConnections;
        private System.Windows.Forms.Button btnAddConnection;
        private System.Windows.Forms.Button btnEditConnection;
        private System.Windows.Forms.Button btnDeleteConnection;
        private System.Windows.Forms.FlowLayoutPanel flowPanel;
        private System.Windows.Forms.Panel panelButtons;

        // أزرار وعلامات جديدة للإيميل
        private System.Windows.Forms.Button btnTestEmail;
        private System.Windows.Forms.Label lblEmailStatus;
        private System.Windows.Forms.CheckBox chkEnableEmailNotifications;
        private System.Windows.Forms.Label lblEnableEmail;
        private System.Windows.Forms.ProgressBar progressBar;

        public SettingsForm()
        {
            InitializeComponent();
            ApplyCustomTheme();
            _settings = JsonFileHelper.ReadSettings();
            if (_settings.GeneralSettings == null)
                _settings.GeneralSettings = new GeneralSettings();
            LoadSettings();
            LoadConnections();
        }

        private void ApplyCustomTheme()
        {
            var slateDark = System.Drawing.Color.FromArgb(30, 41, 59);
            var slateText = System.Drawing.Color.FromArgb(15, 23, 42);
            var whiteColor = System.Drawing.Color.White;
            var lightGray = System.Drawing.Color.FromArgb(245, 247, 250);
            var borderColor = System.Drawing.Color.FromArgb(226, 232, 240);

            this.BackColor = lightGray;
            this.ForeColor = slateText;
            this.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            flowPanel.BackColor = lightGray;
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.TopDown;
            flowPanel.Padding = new Padding(20);
            flowPanel.AutoScroll = true;
            flowPanel.WrapContents = false;

            panelButtons.BackColor = whiteColor;
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Height = 70;
            panelButtons.Paint += (s, e) => {
                e.Graphics.DrawLine(new System.Drawing.Pen(borderColor), 0, 0, panelButtons.Width, 0);
            };

            // تخصيص GroupBoxes
            foreach (Control ctrl in flowPanel.Controls)
            {
                if (ctrl is GroupBox groupBox)
                {
                    groupBox.ForeColor = slateDark;
                    groupBox.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
                    groupBox.BackColor = whiteColor;
                    groupBox.RightToLeft = RightToLeft.Yes;
                    groupBox.AutoSize = false;
                    groupBox.Margin = new Padding(0, 0, 0, 15);
                    groupBox.Padding = new Padding(15, 30, 15, 15);

                    groupBox.Paint += (s, e) => {
                        using (var brush = new System.Drawing.SolidBrush(slateDark))
                        {
                            e.Graphics.FillRectangle(brush, 0, 0, groupBox.Width, 4);
                        }
                    };

                    foreach (Control child in groupBox.Controls)
                    {
                        if (child is Label label)
                        {
                            label.ForeColor = slateText;
                            label.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
                            label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
                            label.RightToLeft = RightToLeft.Yes;
                        }
                        else if (child is TextBox textBox)
                        {
                            textBox.BackColor = whiteColor;
                            textBox.ForeColor = slateText;
                            textBox.BorderStyle = BorderStyle.FixedSingle;
                            textBox.Font = new System.Drawing.Font("Segoe UI", 9.5F);
                            textBox.TextAlign = HorizontalAlignment.Right;
                            textBox.RightToLeft = RightToLeft.Yes;
                        }
                        else if (child is CheckBox checkBox)
                        {
                            checkBox.ForeColor = slateText;
                            checkBox.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                            checkBox.RightToLeft = RightToLeft.Yes;
                            checkBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
                            checkBox.BackColor = whiteColor;
                        }
                        else if (child is NumericUpDown numeric)
                        {
                            numeric.BackColor = whiteColor;
                            numeric.ForeColor = slateText;
                            numeric.Font = new System.Drawing.Font("Segoe UI", 9.5F);
                            numeric.RightToLeft = RightToLeft.Yes;
                            numeric.TextAlign = HorizontalAlignment.Right;
                            numeric.BorderStyle = BorderStyle.FixedSingle;
                        }
                        else if (child is Button button)
                        {
                            button.FlatStyle = FlatStyle.Flat;
                            button.FlatAppearance.BorderSize = 0;
                            button.Cursor = Cursors.Hand;
                            button.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                            button.RightToLeft = RightToLeft.Yes;
                            button.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                            button.Height = 34;
                        }
                    }
                }
            }

            // تخصيص DataGridView
            dgvConnections.BackgroundColor = whiteColor;
            dgvConnections.GridColor = borderColor;
            dgvConnections.ForeColor = slateText;
            dgvConnections.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            dgvConnections.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            dgvConnections.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            dgvConnections.ColumnHeadersDefaultCellStyle.BackColor = slateDark;
            dgvConnections.ColumnHeadersDefaultCellStyle.ForeColor = whiteColor;
            dgvConnections.RightToLeft = RightToLeft.Yes;
            dgvConnections.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvConnections.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvConnections.EnableHeadersVisualStyles = false;
            dgvConnections.BorderStyle = BorderStyle.None;
            dgvConnections.RowHeadersVisible = false;
            dgvConnections.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            dgvConnections.ColumnHeadersHeight = 38;
            dgvConnections.RowTemplate.Height = 30;

            // تخصيص الأزرار
            btnAddConnection.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            btnAddConnection.ForeColor = whiteColor;
            btnAddConnection.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(20, 160, 90);

            btnEditConnection.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            btnEditConnection.ForeColor = whiteColor;
            btnEditConnection.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(30, 80, 200);

            btnDeleteConnection.BackColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnDeleteConnection.ForeColor = whiteColor;
            btnDeleteConnection.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(180, 30, 30);

            btnSave.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            btnSave.ForeColor = whiteColor;
            btnSave.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(20, 160, 90);

            btnCancel.BackColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnCancel.ForeColor = whiteColor;
            btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(180, 30, 30);

            // تخصيص زر اختبار البريد
            btnTestEmail.BackColor = System.Drawing.Color.FromArgb(59, 130, 246);
            btnTestEmail.ForeColor = whiteColor;
            btnTestEmail.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(37, 99, 235);

            chkAutoStart.BackColor = whiteColor;
            chkNotifications.BackColor = whiteColor;
            chkEnableEmailNotifications.BackColor = whiteColor;
        }

        private void InitializeComponent()
        {
            flowPanel = new FlowLayoutPanel();
            grpConnections = new GroupBox();
            dgvConnections = new DataGridView();
            btnAddConnection = new Button();
            btnEditConnection = new Button();
            btnDeleteConnection = new Button();
            grpGeneral = new GroupBox();
            lblAutoStart = new Label();
            chkAutoStart = new CheckBox();
            lblNotifications = new Label();
            chkNotifications = new CheckBox();
            lblLogRetention = new Label();
            numLogRetention = new NumericUpDown();
            grpEmail = new GroupBox();
            lblEnableEmail = new Label();
            chkEnableEmailNotifications = new CheckBox();
            lblEmail = new Label();
            txtEmail = new TextBox();
            lblSmtpServer = new Label();
            txtSmtpServer = new TextBox();
            lblSmtpPort = new Label();
            numSmtpPort = new NumericUpDown();
            lblSmtpUsername = new Label();
            txtSmtpUsername = new TextBox();
            lblSmtpPassword = new Label();
            txtSmtpPassword = new TextBox();
            btnTestEmail = new Button();
            lblEmailStatus = new Label();
            progressBar = new ProgressBar();
            panelButtons = new Panel();
            btnSave = new Button();
            btnCancel = new Button();
            flowPanel.SuspendLayout();
            grpConnections.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConnections).BeginInit();
            grpGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numLogRetention).BeginInit();
            grpEmail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numSmtpPort).BeginInit();
            panelButtons.SuspendLayout();
            SuspendLayout();

            // 
            // flowPanel
            // 
            flowPanel.AutoScroll = true;
            flowPanel.BackColor = Color.FromArgb(245, 247, 250);
            flowPanel.Controls.Add(grpConnections);
            flowPanel.Controls.Add(grpGeneral);
            flowPanel.Controls.Add(grpEmail);
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.TopDown;
            flowPanel.Location = new Point(0, 0);
            flowPanel.Name = "flowPanel";
            flowPanel.Padding = new Padding(20);
            flowPanel.Size = new Size(850, 650);
            flowPanel.TabIndex = 0;
            flowPanel.WrapContents = false;

            // 
            // grpConnections
            // 
            grpConnections.BackColor = Color.White;
            grpConnections.Controls.Add(dgvConnections);
            grpConnections.Controls.Add(btnAddConnection);
            grpConnections.Controls.Add(btnEditConnection);
            grpConnections.Controls.Add(btnDeleteConnection);
            grpConnections.Location = new Point(20, 20);
            grpConnections.Margin = new Padding(0, 0, 0, 15);
            grpConnections.Name = "grpConnections";
            grpConnections.RightToLeft = RightToLeft.Yes;
            grpConnections.Size = new Size(790, 220);
            grpConnections.TabIndex = 0;
            grpConnections.TabStop = false;
            grpConnections.Text = "🔌 اتصالات SQL Server";

            // 
            // dgvConnections
            // 
            dgvConnections.AllowUserToAddRows = false;
            dgvConnections.AllowUserToDeleteRows = false;
            dgvConnections.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvConnections.BackgroundColor = Color.White;
            dgvConnections.Location = new Point(15, 35);
            dgvConnections.Name = "dgvConnections";
            dgvConnections.ReadOnly = true;
            dgvConnections.RightToLeft = RightToLeft.Yes;
            dgvConnections.RowHeadersVisible = false;
            dgvConnections.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvConnections.Size = new Size(760, 130);
            dgvConnections.TabIndex = 0;

            // 
            // btnAddConnection
            // 
            btnAddConnection.Location = new Point(645, 175);
            btnAddConnection.Name = "btnAddConnection";
            btnAddConnection.Size = new Size(130, 34);
            btnAddConnection.TabIndex = 1;
            btnAddConnection.Text = "➕ إضافة";
            btnAddConnection.Click += btnAddConnection_Click;

            // 
            // btnEditConnection
            // 
            btnEditConnection.Location = new Point(475, 175);
            btnEditConnection.Name = "btnEditConnection";
            btnEditConnection.Size = new Size(130, 34);
            btnEditConnection.TabIndex = 2;
            btnEditConnection.Text = "✏️ تعديل";
            btnEditConnection.Click += btnEditConnection_Click;

            // 
            // btnDeleteConnection
            // 
            btnDeleteConnection.Location = new Point(302, 175);
            btnDeleteConnection.Name = "btnDeleteConnection";
            btnDeleteConnection.Size = new Size(130, 34);
            btnDeleteConnection.TabIndex = 3;
            btnDeleteConnection.Text = "🗑️ حذف";
            btnDeleteConnection.Click += btnDeleteConnection_Click;

            // 
            // grpGeneral
            // 
            grpGeneral.BackColor = Color.White;
            grpGeneral.Controls.Add(lblAutoStart);
            grpGeneral.Controls.Add(chkAutoStart);
            grpGeneral.Controls.Add(lblNotifications);
            grpGeneral.Controls.Add(chkNotifications);
            grpGeneral.Controls.Add(lblLogRetention);
            grpGeneral.Controls.Add(numLogRetention);
            grpGeneral.Location = new Point(20, 255);
            grpGeneral.Margin = new Padding(0, 0, 0, 15);
            grpGeneral.Name = "grpGeneral";
            grpGeneral.RightToLeft = RightToLeft.Yes;
            grpGeneral.Size = new Size(790, 150);
            grpGeneral.TabIndex = 1;
            grpGeneral.TabStop = false;
            grpGeneral.Text = "⚙️ الإعدادات العامة";

            // 
            // lblAutoStart
            // 
            lblAutoStart.Location = new Point(578, 40);
            lblAutoStart.Name = "lblAutoStart";
            lblAutoStart.Size = new Size(130, 25);
            lblAutoStart.TabIndex = 0;
            lblAutoStart.Text = "تشغيل مع ويندوز:";
            lblAutoStart.TextAlign = ContentAlignment.MiddleRight;

            // 
            // chkAutoStart
            // 
            chkAutoStart.Location = new Point(527, 40);
            chkAutoStart.Name = "chkAutoStart";
            chkAutoStart.Size = new Size(20, 20);
            chkAutoStart.TabIndex = 1;
            chkAutoStart.CheckedChanged += chkAutoStart_CheckedChanged;

            // 
            // lblNotifications
            // 
            lblNotifications.Location = new Point(578, 65);
            lblNotifications.Name = "lblNotifications";
            lblNotifications.Size = new Size(130, 25);
            lblNotifications.TabIndex = 2;
            lblNotifications.Text = "إظهار الإشعارات:";
            lblNotifications.TextAlign = ContentAlignment.MiddleRight;

            // 
            // chkNotifications
            // 
            chkNotifications.Location = new Point(527, 70);
            chkNotifications.Name = "chkNotifications";
            chkNotifications.Size = new Size(20, 20);
            chkNotifications.TabIndex = 3;

            // 
            // lblLogRetention
            // 
            lblLogRetention.Location = new Point(578, 102);
            lblLogRetention.Name = "lblLogRetention";
            lblLogRetention.Size = new Size(150, 25);
            lblLogRetention.TabIndex = 4;
            lblLogRetention.Text = "أيام الاحتفاظ بالسجلات:";
            lblLogRetention.TextAlign = ContentAlignment.MiddleRight;

            // 
            // numLogRetention
            // 
            numLogRetention.Location = new Point(447, 105);
            numLogRetention.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            numLogRetention.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numLogRetention.Name = "numLogRetention";
            numLogRetention.Size = new Size(100, 23);
            numLogRetention.TabIndex = 5;
            numLogRetention.Value = new decimal(new int[] { 30, 0, 0, 0 });

            // 
            // grpEmail
            // 
            grpEmail.BackColor = Color.White;
            grpEmail.Controls.Add(lblEnableEmail);
            grpEmail.Controls.Add(chkEnableEmailNotifications);
            grpEmail.Controls.Add(lblEmail);
            grpEmail.Controls.Add(txtEmail);
            grpEmail.Controls.Add(lblSmtpServer);
            grpEmail.Controls.Add(txtSmtpServer);
            grpEmail.Controls.Add(lblSmtpPort);
            grpEmail.Controls.Add(numSmtpPort);
            grpEmail.Controls.Add(lblSmtpUsername);
            grpEmail.Controls.Add(txtSmtpUsername);
            grpEmail.Controls.Add(lblSmtpPassword);
            grpEmail.Controls.Add(txtSmtpPassword);
            grpEmail.Controls.Add(btnTestEmail);
            grpEmail.Controls.Add(lblEmailStatus);
            grpEmail.Controls.Add(progressBar);
            grpEmail.Location = new Point(20, 420);
            grpEmail.Margin = new Padding(0);
            grpEmail.Name = "grpEmail";
            grpEmail.RightToLeft = RightToLeft.Yes;
            grpEmail.Size = new Size(790, 300);
            grpEmail.TabIndex = 2;
            grpEmail.TabStop = false;
            grpEmail.Text = "📧 إشعارات البريد الإلكتروني";

            // 
            // lblEnableEmail
            // 
            lblEnableEmail.Location = new Point(598, 32);
            lblEnableEmail.Name = "lblEnableEmail";
            lblEnableEmail.Size = new Size(130, 25);
            lblEnableEmail.TabIndex = 10;
            lblEnableEmail.Text = "تفعيل الإشعارات:";
            lblEnableEmail.TextAlign = ContentAlignment.MiddleRight;

            // 
            // chkEnableEmailNotifications
            // 
            chkEnableEmailNotifications.Location = new Point(547, 35);
            chkEnableEmailNotifications.Name = "chkEnableEmailNotifications";
            chkEnableEmailNotifications.Size = new Size(20, 20);
            chkEnableEmailNotifications.TabIndex = 11;
            chkEnableEmailNotifications.CheckedChanged += chkEnableEmail_CheckedChanged;

            // 
            // lblEmail
            // 
            lblEmail.Location = new Point(598, 65);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(130, 25);
            lblEmail.TabIndex = 0;
            lblEmail.Text = "البريد الإلكتروني:";
            lblEmail.TextAlign = ContentAlignment.MiddleRight;

            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(175, 65);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(400, 23);
            txtEmail.TabIndex = 1;

            // 
            // lblSmtpServer
            // 
            lblSmtpServer.Location = new Point(598, 102);
            lblSmtpServer.Name = "lblSmtpServer";
            lblSmtpServer.Size = new Size(130, 25);
            lblSmtpServer.TabIndex = 2;
            lblSmtpServer.Text = "خادم SMTP:";
            lblSmtpServer.TextAlign = ContentAlignment.MiddleRight;

            // 
            // txtSmtpServer
            // 
            txtSmtpServer.Location = new Point(175, 102);
            txtSmtpServer.Name = "txtSmtpServer";
            txtSmtpServer.Size = new Size(400, 23);
            txtSmtpServer.TabIndex = 3;

            // 
            // lblSmtpPort
            // 
            lblSmtpPort.Location = new Point(598, 139);
            lblSmtpPort.Name = "lblSmtpPort";
            lblSmtpPort.Size = new Size(130, 25);
            lblSmtpPort.TabIndex = 4;
            lblSmtpPort.Text = "منفذ SMTP:";
            lblSmtpPort.TextAlign = ContentAlignment.MiddleRight;

            // 
            // numSmtpPort
            // 
            numSmtpPort.Location = new Point(475, 139);
            numSmtpPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numSmtpPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numSmtpPort.Name = "numSmtpPort";
            numSmtpPort.Size = new Size(100, 23);
            numSmtpPort.TabIndex = 5;
            numSmtpPort.Value = new decimal(new int[] { 587, 0, 0, 0 });

            // 
            // lblSmtpUsername
            // 
            lblSmtpUsername.Location = new Point(598, 176);
            lblSmtpUsername.Name = "lblSmtpUsername";
            lblSmtpUsername.Size = new Size(130, 25);
            lblSmtpUsername.TabIndex = 6;
            lblSmtpUsername.Text = "اسم المستخدم:";
            lblSmtpUsername.TextAlign = ContentAlignment.MiddleRight;

            // 
            // txtSmtpUsername
            // 
            txtSmtpUsername.Location = new Point(175, 176);
            txtSmtpUsername.Name = "txtSmtpUsername";
            txtSmtpUsername.Size = new Size(400, 23);
            txtSmtpUsername.TabIndex = 7;

            // 
            // lblSmtpPassword
            // 
            lblSmtpPassword.Location = new Point(598, 213);
            lblSmtpPassword.Name = "lblSmtpPassword";
            lblSmtpPassword.Size = new Size(130, 25);
            lblSmtpPassword.TabIndex = 8;
            lblSmtpPassword.Text = "كلمة المرور:";
            lblSmtpPassword.TextAlign = ContentAlignment.MiddleRight;

            // 
            // txtSmtpPassword
            // 
            txtSmtpPassword.Location = new Point(175, 213);
            txtSmtpPassword.Name = "txtSmtpPassword";
            txtSmtpPassword.Size = new Size(400, 23);
            txtSmtpPassword.TabIndex = 9;
            txtSmtpPassword.UseSystemPasswordChar = true;

            // 
            // btnTestEmail
            // 
            btnTestEmail.BackColor = Color.FromArgb(59, 130, 246);
            btnTestEmail.FlatStyle = FlatStyle.Flat;
            btnTestEmail.FlatAppearance.BorderSize = 0;
            btnTestEmail.Cursor = Cursors.Hand;
            btnTestEmail.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnTestEmail.ForeColor = Color.White;
            btnTestEmail.Location = new Point(20, 245);
            btnTestEmail.Name = "btnTestEmail";
            btnTestEmail.Size = new Size(150, 34);
            btnTestEmail.TabIndex = 12;
            btnTestEmail.Text = "📧 اختبار الإشعار";
            btnTestEmail.UseVisualStyleBackColor = false;
            btnTestEmail.Click += btnTestEmail_Click;

            // 
            // lblEmailStatus
            // 
            lblEmailStatus.AutoSize = true;
            lblEmailStatus.Font = new Font("Segoe UI", 9.5F);
            lblEmailStatus.ForeColor = Color.FromArgb(100, 116, 139);
            lblEmailStatus.Location = new Point(180, 250);
            lblEmailStatus.Name = "lblEmailStatus";
            lblEmailStatus.Size = new Size(120, 22);
            lblEmailStatus.TabIndex = 13;
            lblEmailStatus.Text = "⏳ في انتظار الاختبار";
            lblEmailStatus.TextAlign = ContentAlignment.MiddleRight;

            // 
            // progressBar
            // 
            progressBar.Location = new Point(360, 250);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(180, 20);
            progressBar.TabIndex = 14;
            progressBar.Visible = false;

            // 
            // panelButtons
            // 
            panelButtons.BackColor = Color.White;
            panelButtons.Controls.Add(btnSave);
            panelButtons.Controls.Add(btnCancel);
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Location = new Point(0, 650);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(850, 70);
            panelButtons.TabIndex = 1;

            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(16, 185, 129);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(620, 14);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(180, 42);
            btnSave.TabIndex = 0;
            btnSave.Text = "💾 حفظ";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;

            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(220, 38, 38);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(420, 14);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(180, 42);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "❌ إلغاء";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;

            // 
            // SettingsForm
            // 
            BackColor = Color.FromArgb(245, 247, 250);
            ClientSize = new Size(850, 720);
            Controls.Add(flowPanel);
            Controls.Add(panelButtons);
            MaximumSize = new Size(866, 759);
            MinimumSize = new Size(866, 759);
            Name = "SettingsForm";
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            StartPosition = FormStartPosition.CenterParent;
            Text = "⚙️ الإعدادات العامة";

            flowPanel.ResumeLayout(false);
            grpConnections.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvConnections).EndInit();
            grpGeneral.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numLogRetention).EndInit();
            grpEmail.ResumeLayout(false);
            grpEmail.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numSmtpPort).EndInit();
            panelButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            try
            {
                if (_settings.GeneralSettings == null)
                    _settings.GeneralSettings = new GeneralSettings();

                chkAutoStart.Checked = _settings.GeneralSettings.AutoStartWithWindows;
                chkNotifications.Checked = _settings.GeneralSettings.ShowNotifications;
                numLogRetention.Value = _settings.GeneralSettings.LogRetentionDays;

                // تحميل إعدادات البريد الإلكتروني
                chkEnableEmailNotifications.Checked = _settings.GeneralSettings.EnableEmailNotifications;
                txtEmail.Text = _settings.GeneralSettings.EmailNotifications ?? "";
                txtSmtpServer.Text = _settings.GeneralSettings.SmtpServer ?? "";
                numSmtpPort.Value = _settings.GeneralSettings.SmtpPort > 0 ? _settings.GeneralSettings.SmtpPort : 587;
                txtSmtpUsername.Text = _settings.GeneralSettings.SmtpUsername ?? "";

                if (!string.IsNullOrEmpty(_settings.GeneralSettings.SmtpPassword))
                {
                    try
                    {
                        txtSmtpPassword.Text = EncryptionHelper.Decrypt(_settings.GeneralSettings.SmtpPassword);
                    }
                    catch
                    {
                        txtSmtpPassword.Text = "";
                    }
                }

                UpdateEmailUI();
            }
            catch (Exception ex)
            {
                LogService.WriteLog("SettingsForm", "Error", $"خطأ في تحميل الإعدادات: {ex.Message}");
            }
        }

        private void LoadConnections()
        {
            try
            {
                dgvConnections.DataSource = null;
                dgvConnections.DataSource = _settings.ServerConnections;

                if (dgvConnections.Columns.Count > 0)
                {
                    if (dgvConnections.Columns.Contains("Id"))
                        dgvConnections.Columns["Id"].Visible = false;
                    if (dgvConnections.Columns.Contains("EncryptedPassword"))
                        dgvConnections.Columns["EncryptedPassword"].Visible = false;
                    if (dgvConnections.Columns.Contains("DisplayName"))
                        dgvConnections.Columns["DisplayName"].HeaderText = "اسم الاتصال";
                    if (dgvConnections.Columns.Contains("ServerName"))
                        dgvConnections.Columns["ServerName"].HeaderText = "اسم الخادم";
                    if (dgvConnections.Columns.Contains("AuthType"))
                        dgvConnections.Columns["AuthType"].HeaderText = "نوع المصادقة";
                    if (dgvConnections.Columns.Contains("Username"))
                        dgvConnections.Columns["Username"].HeaderText = "اسم المستخدم";
                    if (dgvConnections.Columns.Contains("IsActive"))
                        dgvConnections.Columns["IsActive"].HeaderText = "نشط";
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("SettingsForm", "Error", $"خطأ في تحميل الاتصالات: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (_settings.GeneralSettings == null)
                    _settings.GeneralSettings = new GeneralSettings();

                _settings.GeneralSettings.AutoStartWithWindows = chkAutoStart.Checked;
                _settings.GeneralSettings.ShowNotifications = chkNotifications.Checked;
                _settings.GeneralSettings.LogRetentionDays = (int)numLogRetention.Value;

                // حفظ إعدادات البريد الإلكتروني
                _settings.GeneralSettings.EnableEmailNotifications = chkEnableEmailNotifications.Checked;
                _settings.GeneralSettings.EmailNotifications = txtEmail.Text.Trim();
                _settings.GeneralSettings.SmtpServer = txtSmtpServer.Text.Trim();
                _settings.GeneralSettings.SmtpPort = (int)numSmtpPort.Value;
                _settings.GeneralSettings.SmtpUsername = txtSmtpUsername.Text.Trim();

                if (!string.IsNullOrEmpty(txtSmtpPassword.Text))
                {
                    _settings.GeneralSettings.SmtpPassword = EncryptionHelper.Encrypt(txtSmtpPassword.Text);
                }
                else if (string.IsNullOrEmpty(_settings.GeneralSettings.SmtpPassword))
                {
                    _settings.GeneralSettings.SmtpPassword = "";
                }

                JsonFileHelper.SaveSettings(_settings);
                LogService.WriteLog("SettingsForm", "Success", "✅ تم حفظ الإعدادات بنجاح");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("SettingsForm", "Error", $"خطأ في حفظ الإعدادات: {ex.Message}");
                throw;
            }
        }

        private void UpdateEmailUI()
        {
            bool enabled = chkEnableEmailNotifications.Checked;
            txtEmail.Enabled = enabled;
            txtSmtpServer.Enabled = enabled;
            numSmtpPort.Enabled = enabled;
            txtSmtpUsername.Enabled = enabled;
            txtSmtpPassword.Enabled = enabled;
            btnTestEmail.Enabled = enabled;

            if (!enabled)
            {
                lblEmailStatus.Text = "⏸️ الإشعارات غير مفعلة";
                lblEmailStatus.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            }
            else
            {
                lblEmailStatus.Text = "⏳ في انتظار الاختبار";
                lblEmailStatus.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            }
        }

        private void chkEnableEmail_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEmailUI();
            _isDirty = true;
        }

        private async void btnTestEmail_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateEmailSettings())
                    return;

                btnTestEmail.Enabled = false;
                btnTestEmail.Text = "⏳ جاري الإرسال...";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                lblEmailStatus.Text = "⏳ جاري الإرسال...";
                lblEmailStatus.ForeColor = System.Drawing.Color.FromArgb(59, 130, 246);

                bool success = await Task.Run(() => SendTestEmail());

                if (success)
                {
                    lblEmailStatus.Text = "✅ تم الإرسال بنجاح!";
                    lblEmailStatus.ForeColor = System.Drawing.Color.FromArgb(34, 197, 94);
                    MessageBox.Show(
                        "✅ تم إرسال بريد اختباري بنجاح!\n\n" +
                        "تأكد من صندوق الوارد الخاص بك (أو البريد غير المرغوب فيه).",
                        "نجاح",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    lblEmailStatus.Text = "❌ فشل الإرسال!";
                    lblEmailStatus.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
                }
            }
            catch (Exception ex)
            {
                lblEmailStatus.Text = "❌ خطأ!";
                lblEmailStatus.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
                MessageBox.Show($"❌ خطأ في إرسال البريد:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestEmail.Enabled = true;
                btnTestEmail.Text = "📧 اختبار الإشعار";
                progressBar.Visible = false;
                progressBar.Style = ProgressBarStyle.Blocks;
            }
        }

        private bool ValidateEmailSettings()
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("يرجى إدخال البريد الإلكتروني المستلم.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(txtEmail.Text.Trim());
            }
            catch
            {
                MessageBox.Show("صيغة البريد الإلكتروني غير صحيحة.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSmtpServer.Text))
            {
                MessageBox.Show("يرجى إدخال خادم SMTP.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSmtpServer.Focus();
                return false;
            }

            if ((int)numSmtpPort.Value <= 0 || (int)numSmtpPort.Value > 65535)
            {
                MessageBox.Show("المنفذ غير صحيح. يجب أن يكون بين 1 و 65535.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numSmtpPort.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSmtpUsername.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المستخدم (البريد الإلكتروني الكامل).", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSmtpUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSmtpPassword.Text))
            {
                MessageBox.Show("يرجى إدخال كلمة المرور.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSmtpPassword.Focus();
                return false;
            }

            return true;
        }

        private bool SendTestEmail()
        {
            try
            {
                using (var client = new SmtpClient(txtSmtpServer.Text.Trim(), (int)numSmtpPort.Value))
                {
                    client.EnableSsl = true;
                    client.Timeout = 30000;
                    client.UseDefaultCredentials = false;

                    if (!string.IsNullOrEmpty(txtSmtpUsername.Text) && !string.IsNullOrEmpty(txtSmtpPassword.Text))
                    {
                        client.Credentials = new NetworkCredential(txtSmtpUsername.Text.Trim(), txtSmtpPassword.Text);
                    }
                    else
                    {
                        client.Credentials = new NetworkCredential(txtSmtpUsername.Text.Trim(), "");
                    }

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(txtSmtpUsername.Text.Trim());
                        message.To.Add(txtEmail.Text.Trim());
                        message.Subject = "✅ اختبار إشعارات SmartBackupSuite";
                        message.Body = $@"مرحباً،

تم إرسال هذا البريد لاختبار إعدادات الإشعارات في SmartBackupSuite.

📅 التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
💻 الجهاز: {Environment.MachineName}
🔧 الإعدادات:
   • خادم SMTP: {txtSmtpServer.Text}
   • المنفذ: {numSmtpPort.Value}
   • المستخدم: {txtSmtpUsername.Text}

✅ إذا وصلتك هذه الرسالة، فالإعدادات صحيحة.

--
SmartBackupSuite
النسخ الاحتياطي الذكي لقواعد البيانات
";
                        message.IsBodyHtml = false;

                        client.Send(message);
                    }
                }

                LogService.WriteLog("SettingsForm", "Success", $"✅ تم إرسال بريد اختباري إلى {txtEmail.Text}");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                string errorMessage = GetSmtpErrorMessage(smtpEx);
                LogService.WriteLog("SettingsForm", "Error", $"فشل SMTP: {smtpEx.StatusCode} - {errorMessage}");
                throw new Exception($"فشل SMTP: {errorMessage}\n\n{(smtpEx.InnerException?.Message ?? "")}");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("SettingsForm", "Error", $"فشل إرسال البريد: {ex.Message}");
                throw;
            }
        }

        private string GetSmtpErrorMessage(SmtpException ex)
        {
            // التحقق من النص أولاً للحصول على رسالة أكثر دقة
            string message = ex.Message.ToLower();

            if (message.Contains("authentication") || message.Contains("credentials") ||
                message.Contains("login") || message.Contains("535") || message.Contains("5.7.8"))
            {
                return "فشل المصادقة. تحقق من:\n" +
                       "• اسم المستخدم (البريد الإلكتروني الكامل)\n" +
                       "• كلمة المرور (تأكد من صحتها)\n" +
                       "• إذا كنت تستخدم Gmail، تأكد من تفعيل 'تطبيقات أقل أماناً' أو استخدام كلمة مرور التطبيق";
            }

            if (message.Contains("timeout") || message.Contains("timed out"))
            {
                return "انتهت مهلة الاتصال. تحقق من:\n" +
                       "• اتصال الإنترنت\n" +
                       "• صحة خادم SMTP والمنفذ";
            }

            if (message.Contains("ssl") || message.Contains("secure"))
            {
                return "خطأ في الاتصال الآمن (SSL). تأكد من:\n" +
                       "• تفعيل SSL في الإعدادات\n" +
                       "• استخدام المنفذ الصحيح (Gmail: 587)";
            }

            // استخدام رمز الحالة - القيم المتوفرة فقط
            string statusCode = ex.StatusCode.ToString();

            if (statusCode == "MailboxBusy")
                return "صندوق البريد مشغول، حاول مرة أخرى.";
            else if (statusCode == "MailboxUnavailable")
                return "صندوق البريد غير متوفر.";
            else if (statusCode == "GeneralFailure")
                return "فشل عام في الاتصال بخادم البريد.";
            else if (statusCode == "ServiceNotAvailable")
                return "خدمة البريد غير متوفرة حالياً.";
            else if (statusCode == "SystemStatus")
                return "خطأ في النظام، حاول مرة أخرى.";
            else if (statusCode == "ExceededStorageAllocation")
                return "تجاوزت مساحة التخزين المخصصة.";
            else if (statusCode == "TransactionFailed")
                return "فشلت المعاملة.";
            else if (statusCode == "CommandUnrecognized")
                return "أمر غير معروف.";
            else if (statusCode == "SyntaxError")
                return "خطأ في الصياغة.";
            else if (statusCode == "CommandNotImplemented")
                return "الأمر غير مدعوم.";
            else if (statusCode == "BadCommandSequence")
                return "تسلسل أوامر خاطئ.";
            else if (statusCode == "MustIssueStartTlsFirst")
                return "يجب تنفيذ STARTTLS أولاً.";
            else if (statusCode == "InsufficientStorage")
                return "مساحة تخزين غير كافية.";
            else
                return ex.Message;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();
                MessageBox.Show("✅ تم حفظ الإعدادات بنجاح", "نجاح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في حفظ الإعدادات:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("هل تريد حفظ التغييرات قبل الإلغاء؟",
                    "تغييرات غير محفوظة",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveSettings();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void chkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (chkAutoStart.Checked)
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                    {
                        key.SetValue("SmartBackupSuite", Application.ExecutablePath);
                    }
                }
                else
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                    {
                        if (key.GetValue("SmartBackupSuite") != null)
                        {
                            key.DeleteValue("SmartBackupSuite", false);
                        }
                    }
                }
                _isDirty = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تعديل إعدادات بدء التشغيل:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnAddConnection_Click(object sender, EventArgs e)
        {
            using (var form = new ConnectionForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _settings.ServerConnections.Add(form.Connection);
                    JsonFileHelper.SaveSettings(_settings);
                    LoadConnections();
                    _isDirty = true;
                }
            }
        }

        private void btnEditConnection_Click(object sender, EventArgs e)
        {
            if (dgvConnections.SelectedRows.Count > 0)
            {
                var selectedConnection = dgvConnections.SelectedRows[0].DataBoundItem as ServerConnection;
                if (selectedConnection != null)
                {
                    using (var form = new ConnectionForm(selectedConnection))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            JsonFileHelper.SaveSettings(_settings);
                            LoadConnections();
                            _isDirty = true;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار اتصال للتعديل", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDeleteConnection_Click(object sender, EventArgs e)
        {
            if (dgvConnections.SelectedRows.Count > 0)
            {
                var selectedConnection = dgvConnections.SelectedRows[0].DataBoundItem as ServerConnection;
                if (selectedConnection != null)
                {
                    bool isUsed = _settings.BackupJobs.Any(j => j.ConnectionId == selectedConnection.Id);
                    if (isUsed)
                    {
                        MessageBox.Show("لا يمكن حذف هذا الاتصال لأنه مستخدم في مهمة موجودة", "تنبيه",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (MessageBox.Show($"هل أنت متأكد من حذف الاتصال '{selectedConnection.DisplayName}'؟",
                        "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _settings.ServerConnections.Remove(selectedConnection);
                        JsonFileHelper.SaveSettings(_settings);
                        LoadConnections();
                        _isDirty = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار اتصال للحذف", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}