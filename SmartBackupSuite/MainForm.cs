using SmartBackupSuite.Forms;
using SmartBackupSuite.Helpers;
using SmartBackupSuite.Models;
using SmartBackupSuite.Services;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SmartBackupSuite
{
    public partial class MainForm : Form
    {
        private AppSettings _settings;
        private ScheduleService _scheduleService;
        private NotifyIcon _notifyIcon;
        private System.ComponentModel.IContainer components = null;
        private bool _isTrayInitialized = false;
        private bool _isExiting = false;
        private bool _isInitialized = false;
        private bool _isTrayDisposed = false;

        // الألوان
        private static readonly Color PrimaryDark = Color.FromArgb(15, 23, 42);
        private static readonly Color Primary = Color.FromArgb(30, 41, 59);
        private static readonly Color Accent = Color.FromArgb(59, 130, 246);
        private static readonly Color Success = Color.FromArgb(34, 197, 94);
        private static readonly Color Warning = Color.FromArgb(250, 204, 21);
        private static readonly Color Danger = Color.FromArgb(239, 68, 68);
        private static readonly Color Surface = Color.FromArgb(248, 250, 252);
        private static readonly Color TextPrimary = Color.FromArgb(241, 245, 249);
        private static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);
        private static readonly Color BorderLight = Color.FromArgb(226, 232, 240);

        private DataGridView dgvJobs;
        private Button btnAddJob;
        private Button btnEditJob;
        private Button btnDeleteJob;
        private Button btnViewLogs;
        private Button btnSettings;
        private Button btnDashboard;
        private Label lblActiveJobs;
        private Label lblLastBackup;
        private Label lblLastBackupStatus;
        private Panel panelHeader;
        private Panel panelFooter;
        private Panel panelContent;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblStatusIndicator;

        public MainForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.DoubleBuffered = true;

            LoadSettings();
            InitializeSystemTray();
            UpdateUI();
            StartScheduler();
            _isInitialized = true;

            this.Shown += (s, e) =>
            {
                EnsureTrayIconVisible();
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.G))
            {
                try
                {
                    using (var generatorForm = new LicenseGeneratorForm())
                    {
                        generatorForm.ShowDialog();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    ShowStyledMessageBox($"خطأ في فتح مولد المفاتيح: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.L))
            {
                try
                {
                    using (var customersForm = new CustomersForm())
                    {
                        customersForm.ShowDialog();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    ShowStyledMessageBox($"خطأ في فتح صفحة العملاء: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private DialogResult ShowStyledMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(text, caption, buttons, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

            dgvJobs = new DataGridView();

            btnAddJob = CreateStyledButton("➕ إضافة مهمة", Success, new Size(140, 40), btnAddJob_Click);
            btnEditJob = CreateStyledButton("✏️ تعديل مهمة", Accent, new Size(140, 40), btnEditJob_Click);
            btnDeleteJob = CreateStyledButton("🗑️ حذف مهمة", Danger, new Size(140, 40), btnDeleteJob_Click);
            btnViewLogs = CreateStyledButton("📋 السجلات", Color.FromArgb(13, 148, 136), new Size(140, 40), btnViewLogs_Click);
            btnDashboard = CreateStyledButton("📊 لوحة التحكم", Color.FromArgb(124, 58, 237), new Size(140, 40), BtnDashboard_Click);
            btnSettings = CreateStyledButton("⚙️ الإعدادات", Color.FromArgb(100, 116, 139), new Size(140, 40), btnSettings_Click);

            lblActiveJobs = new Label();
            lblLastBackup = new Label();
            lblLastBackupStatus = new Label();
            lblTitle = new Label();
            lblSubtitle = new Label();
            lblStatusIndicator = new Label();
            panelHeader = new Panel();
            panelFooter = new Panel();
            panelContent = new Panel();

            ((System.ComponentModel.ISupportInitialize)dgvJobs).BeginInit();
            panelHeader.SuspendLayout();
            panelFooter.SuspendLayout();
            panelContent.SuspendLayout();
            SuspendLayout();

            // ==================== panelHeader ====================
            panelHeader.BackColor = PrimaryDark;
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 100;
            panelHeader.Padding = new Padding(25, 15, 25, 15);

            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = TextPrimary;
            lblTitle.Location = new Point(25, 15);
            lblTitle.Text = "💾 Smart Backup Suite";

            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 10F);
            lblSubtitle.ForeColor = TextSecondary;
            lblSubtitle.Location = new Point(25, 48);
            lblSubtitle.Text = "نظام إدارة النسخ الاحتياطي المتكامل";

            lblStatusIndicator.AutoSize = true;
            lblStatusIndicator.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStatusIndicator.ForeColor = Success;
            lblStatusIndicator.Location = new Point(25, 72);
            lblStatusIndicator.Text = "● النظام يعمل";

            lblActiveJobs.AutoSize = true;
            lblActiveJobs.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblActiveJobs.ForeColor = TextPrimary;
            lblActiveJobs.Location = new Point(700, 20);
            lblActiveJobs.Text = "📊 المهام النشطة: 0";

            lblLastBackup.AutoSize = true;
            lblLastBackup.Font = new Font("Segoe UI", 9F);
            lblLastBackup.ForeColor = TextSecondary;
            lblLastBackup.Location = new Point(700, 48);
            lblLastBackup.Text = "آخر نسخ: --";

            lblLastBackupStatus.AutoSize = true;
            lblLastBackupStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLastBackupStatus.ForeColor = TextSecondary;
            lblLastBackupStatus.Location = new Point(700, 68);
            lblLastBackupStatus.Text = "الحالة: --";

            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSubtitle);
            panelHeader.Controls.Add(lblStatusIndicator);
            panelHeader.Controls.Add(lblActiveJobs);
            panelHeader.Controls.Add(lblLastBackup);
            panelHeader.Controls.Add(lblLastBackupStatus);

            // ==================== panelContent ====================
            panelContent.BackColor = Surface;
            panelContent.Dock = DockStyle.Fill;
            panelContent.Padding = new Padding(20);

            // ==================== dgvJobs ====================
            dgvJobs.AllowUserToAddRows = false;
            dgvJobs.AllowUserToDeleteRows = false;
            dgvJobs.AllowUserToResizeRows = false;
            dgvJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvJobs.BackgroundColor = Color.White;
            dgvJobs.BorderStyle = BorderStyle.None;
            dgvJobs.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvJobs.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvJobs.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            dgvJobs.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            dgvJobs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvJobs.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvJobs.ColumnHeadersDefaultCellStyle.Padding = new Padding(0, 10, 0, 10);
            dgvJobs.ColumnHeadersHeight = 45;
            dgvJobs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvJobs.DefaultCellStyle.BackColor = Color.White;
            dgvJobs.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgvJobs.DefaultCellStyle.ForeColor = PrimaryDark;
            dgvJobs.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvJobs.DefaultCellStyle.Padding = new Padding(5);
            dgvJobs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dgvJobs.DefaultCellStyle.SelectionForeColor = Accent;
            dgvJobs.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvJobs.EnableHeadersVisualStyles = false;
            dgvJobs.GridColor = BorderLight;
            dgvJobs.Dock = DockStyle.Fill;
            dgvJobs.MultiSelect = false;
            dgvJobs.ReadOnly = true;
            dgvJobs.RowHeadersVisible = false;
            dgvJobs.RowTemplate.Height = 40;
            dgvJobs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvJobs.ScrollBars = ScrollBars.Vertical;

            // ✅ إضافة عمود زر التنفيذ الفوري
            DataGridViewButtonColumn btnExecuteColumn = new DataGridViewButtonColumn();
            btnExecuteColumn.Name = "Execute";
            btnExecuteColumn.HeaderText = "⚡ تنفيذ";
            btnExecuteColumn.Text = "▶️ تنفيذ";
            btnExecuteColumn.UseColumnTextForButtonValue = true;
            btnExecuteColumn.Width = 80;
            btnExecuteColumn.DefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnExecuteColumn.DefaultCellStyle.ForeColor = Color.White;
            btnExecuteColumn.DefaultCellStyle.BackColor = Color.FromArgb(34, 197, 94);
            btnExecuteColumn.FlatStyle = FlatStyle.Flat;
            dgvJobs.Columns.Add(btnExecuteColumn);

            // ✅ حدث النقر على زر التنفيذ
            dgvJobs.CellClick += DgvJobs_CellClick;

            dgvJobs.Paint += (s, e) => {
                Rectangle rect = new Rectangle(0, 0, dgvJobs.Width - 1, dgvJobs.Height - 1);
                using (Pen pen = new Pen(BorderLight, 1))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            // ✅ تنسيق عمود التنفيذ
            dgvJobs.CellFormatting += DgvJobs_CellFormatting;

            panelContent.Controls.Add(dgvJobs);

            // ==================== panelFooter ====================
            panelFooter.BackColor = Color.White;
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 80;
            panelFooter.Padding = new Padding(20, 15, 20, 15);

            int buttonY = 18;
            int startX = 20;
            int spacing = 155;

            btnAddJob.Location = new Point(startX, buttonY);
            btnEditJob.Location = new Point(startX + spacing, buttonY);
            btnDeleteJob.Location = new Point(startX + spacing * 2, buttonY);
            btnViewLogs.Location = new Point(startX + spacing * 3, buttonY);
            btnDashboard.Location = new Point(startX + spacing * 4, buttonY);
            btnSettings.Location = new Point(startX + spacing * 5, buttonY);

            panelFooter.Controls.Add(btnAddJob);
            panelFooter.Controls.Add(btnEditJob);
            panelFooter.Controls.Add(btnDeleteJob);
            panelFooter.Controls.Add(btnViewLogs);
            panelFooter.Controls.Add(btnDashboard);
            panelFooter.Controls.Add(btnSettings);

            panelFooter.Paint += (s, e) => {
                using (Pen pen = new Pen(BorderLight, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
                }
            };

            // ==================== MainForm ====================
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Surface;
            ClientSize = new Size(1024, 640);
            Controls.Add(panelContent);
            Controls.Add(panelHeader);
            Controls.Add(panelFooter);
            Font = new Font("Segoe UI", 9F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(900, 500);
            Name = "MainForm";
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Smart Backup Suite - إدارة النسخ الاحتياطي";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;

            ((System.ComponentModel.ISupportInitialize)dgvJobs).EndInit();
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelFooter.ResumeLayout(false);
            panelContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        // ==================== تنسيق عمود التنفيذ ====================
        private void DgvJobs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvJobs.Columns[e.ColumnIndex].Name == "Execute" && e.RowIndex >= 0)
            {
                var job = dgvJobs.Rows[e.RowIndex].DataBoundItem as BackupJob;
                if (job != null)
                {
                    e.Value = job.IsActive ? "▶️ تنفيذ" : "⏸️ غير نشط";
                }
            }
        }

        // ==================== حدث النقر على زر التنفيذ ====================
        private async void DgvJobs_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // ✅ التأكد من أن العمود هو عمود التنفيذ
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            if (dgvJobs.Columns[e.ColumnIndex].Name != "Execute") return;

            var job = dgvJobs.Rows[e.RowIndex].DataBoundItem as BackupJob;
            if (job == null) return;

            // ✅ التحقق من أن المهمة نشطة
            if (!job.IsActive)
            {
                ShowStyledMessageBox(
                    "⚠️ هذه المهمة غير نشطة.\n" +
                    "يرجى تفعيل المهمة أولاً من خلال التعديل.",
                    "تنبيه",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // ✅ تأكيد التنفيذ
            var confirmResult = ShowStyledMessageBox(
                $"هل تريد تنفيذ المهمة '{job.JobName}' الآن؟",
                "تأكيد التنفيذ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmResult != DialogResult.Yes) return;

            // ✅ تنفيذ المهمة
            await ExecuteSingleJob(job);
        }

        // ==================== تنفيذ مهمة واحدة ====================
        private async System.Threading.Tasks.Task ExecuteSingleJob(BackupJob job)
        {
            try
            {
                // ✅ تغيير مؤشر الماوس إلى انتظار
                this.Cursor = Cursors.WaitCursor;

                // ✅ عرض إشعار بدء التنفيذ
                ShowTrayNotification(
                    "▶️ تنفيذ المهمة",
                    $"جاري تنفيذ المهمة: {job.JobName}",
                    ToolTipIcon.Info
                );

                // ✅ الحصول على بيانات الاتصال والوجهة
                var connection = _settings.ServerConnections.FirstOrDefault(c => c.Id == job.ConnectionId);
                var destination = _settings.Destinations.FirstOrDefault(d => d.JobId == job.Id);

                if (connection == null)
                {
                    ShowStyledMessageBox(
                        "❌ لا يوجد اتصال محدد لهذه المهمة.\n" +
                        "يرجى التحقق من إعدادات المهمة.",
                        "خطأ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                if (destination == null)
                {
                    ShowStyledMessageBox(
                        "❌ لا توجد وجهة محددة لهذه المهمة.\n" +
                        "يرجى التحقق من إعدادات المهمة.",
                        "خطأ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                // ✅ تنفيذ النسخ الاحتياطي
                var backupService = new BackupService();
                bool success = await backupService.ExecuteBackup(job, connection, destination);

                // ✅ تحديث حالة المهمة
                job.LastRunTime = DateTime.Now;
                job.LastRunStatus = success ? "Success" : "Failed";

                // ✅ حفظ التغييرات
                var settings = JsonFileHelper.ReadSettings();
                var index = settings.BackupJobs.FindIndex(j => j.Id == job.Id);
                if (index >= 0)
                {
                    settings.BackupJobs[index] = job;
                    JsonFileHelper.SaveSettings(settings);
                    _settings = settings;
                }

                // ✅ تحديث واجهة المستخدم
                UpdateUI();

                // ✅ عرض إشعار النتيجة
                if (success)
                {
                    ShowTrayNotification(
                        "✅ اكتمل التنفيذ",
                        $"تم تنفيذ المهمة '{job.JobName}' بنجاح",
                        ToolTipIcon.Info,
                        5000
                    );
                    ShowStyledMessageBox(
                        $"✅ تم تنفيذ المهمة '{job.JobName}' بنجاح!",
                        "نجاح",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    ShowTrayNotification(
                        "❌ فشل التنفيذ",
                        $"فشل تنفيذ المهمة '{job.JobName}'",
                        ToolTipIcon.Error,
                        5000
                    );
                    ShowStyledMessageBox(
                        $"❌ فشل تنفيذ المهمة '{job.JobName}'.\n" +
                        "يرجى التحقق من السجلات لمعرفة السبب.",
                        "فشل",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("MainForm", "Error", $"خطأ في تنفيذ المهمة {job.JobName}: {ex.Message}");
                ShowStyledMessageBox(
                    $"❌ خطأ في تنفيذ المهمة:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                ShowTrayNotification(
                    "❌ خطأ",
                    $"حدث خطأ أثناء تنفيذ المهمة: {ex.Message}",
                    ToolTipIcon.Error
                );
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private Button CreateStyledButton(string text, Color backColor, Size size, EventHandler clickHandler)
        {
            Button btn = new Button();
            btn.BackColor = backColor;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.ForeColor = Color.White;
            btn.Size = size;
            btn.Text = text;
            btn.UseVisualStyleBackColor = false;

            if (clickHandler != null)
            {
                btn.Click += clickHandler;
            }

            btn.MouseEnter += (s, e) => {
                btn.BackColor = ControlPaint.Light(backColor, 0.15f);
            };
            btn.MouseLeave += (s, e) => {
                btn.BackColor = backColor;
            };

            btn.Region = new Region(CreateRoundedRectangle(btn.ClientRectangle, 8));

            return btn;
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arcRect, 180, 90);
            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90);
            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);
            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void BtnDashboard_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dashboard = new DashboardForm())
                {
                    dashboard.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ShowStyledMessageBox($"خطأ في فتح لوحة التحكم: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== SYSTEM TRAY ====================

        private void InitializeSystemTray()
        {
            try
            {
                if (_isTrayInitialized || _isTrayDisposed) return;

                Icon appIcon = GetApplicationIcon();

                _notifyIcon = new NotifyIcon
                {
                    Icon = appIcon,
                    Text = "🔄 Smart Backup Suite - يعمل في الخلفية",
                    Visible = true,
                    Tag = "SmartBackupSuite"
                };

                ContextMenuStrip contextMenu = new ContextMenuStrip();
                contextMenu.Renderer = new ToolStripProfessionalRenderer(new CustomColorTable());
                contextMenu.Font = new Font("Segoe UI", 9.5F);
                contextMenu.ShowImageMargin = true;
                contextMenu.RightToLeft = RightToLeft.Yes;

                contextMenu.Items.Add("🔄 إظهار التطبيق", null, (s, e) => ShowMainWindow());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("▶️ تنفيذ جميع المهام الآن", null, (s, e) => ExecuteAllJobsNow());
                contextMenu.Items.Add("📊 لوحة التحكم", null, (s, e) => BtnDashboard_Click(s, e));
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("📋 عرض السجلات", null, (s, e) => btnViewLogs_Click(s, e));
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("❌ خروج", null, (s, e) => ExitApplication());
                {
                    ForeColor = Danger;
                }
                ;

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
                _notifyIcon.MouseClick += OnNotifyIconMouseClick;
                _notifyIcon.BalloonTipClicked += (s, e) => ShowMainWindow();

                _isTrayInitialized = true;
                _isTrayDisposed = false;

                ShowTrayNotification(
                    "🔄 Smart Backup Suite",
                    "✅ تم تشغيل التطبيق بنجاح في الخلفية",
                    ToolTipIcon.Info,
                    3000
                );

                LogService.WriteLog("System", "Info", "✅ تم تهيئة علبة النظام بنجاح");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"❌ خطأ في تهيئة علبة النظام: {ex.Message}");
                ShowStyledMessageBox(
                    $"خطأ في تهيئة علبة النظام:\n{ex.Message}\n\n" +
                    "قد لا تظهر أيقونة التطبيق في شريط المهام.",
                    "تحذير",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private Icon GetApplicationIcon()
        {
            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                    return appIcon;
            }
            catch { }

            return SystemIcons.Application;
        }

        private void OnNotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        }

        private bool IsTrayIconValid()
        {
            if (_notifyIcon == null || _isTrayDisposed)
                return false;

            try
            {
                var text = _notifyIcon.Text;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void EnsureTrayIconVisible()
        {
            try
            {
                if (!IsTrayIconValid())
                {
                    _isTrayInitialized = false;
                    _isTrayDisposed = false;
                    InitializeSystemTray();
                    return;
                }

                _notifyIcon.Visible = false;
                _notifyIcon.Visible = true;
                _notifyIcon.Text = "🔄 Smart Backup Suite - يعمل في الخلفية";

                LogService.WriteLog("System", "Info", "✅ تم تأكيد ظهور أيقونة علبة النظام");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"❌ خطأ في تأكيد ظهور الأيقونة: {ex.Message}");
            }
        }

        private void ShowTrayNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
        {
            try
            {
                if (IsTrayIconValid())
                {
                    _notifyIcon.ShowBalloonTip(timeout, title, message, icon);
                }
            }
            catch { }
        }

        private void ShowMainWindow()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(ShowMainWindow));
                    return;
                }

                this.Visible = true;
                this.ShowInTaskbar = true;

                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }

                this.BringToFront();
                this.Activate();

                if (IsTrayIconValid())
                {
                    _notifyIcon.Text = "🔄 Smart Backup Suite - نشط";
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"❌ خطأ في إظهار النافذة: {ex.Message}");
            }
        }

        private void HideToTray()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(HideToTray));
                    return;
                }

                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
                this.ShowInTaskbar = false;

                if (IsTrayIconValid())
                {
                    _notifyIcon.Text = "🔄 Smart Backup Suite - يعمل في الخلفية";
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"❌ خطأ في إخفاء النافذة: {ex.Message}");
            }
        }

        private void ExitApplication()
        {
            try
            {
                var result = MessageBox.Show(
                    "هل أنت متأكد من إغلاق التطبيق؟\n\n" +
                    "سيتم إيقاف جميع عمليات النسخ الاحتياطي الجارية.",
                    "تأكيد الخروج",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2
                );

                if (result != DialogResult.Yes)
                    return;

                _isExiting = true;

                DisposeTrayIcon();

                _scheduleService?.Stop();
                _isTrayInitialized = false;

                Application.Exit();
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"❌ خطأ في الخروج: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private void DisposeTrayIcon()
        {
            try
            {
                if (_notifyIcon != null && !_isTrayDisposed)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                    _isTrayDisposed = true;
                }
            }
            catch { }
        }

        // ==================== SCHEDULER ====================

        private void StartScheduler()
        {
            try
            {
                _scheduleService = new ScheduleService();
                _scheduleService.JobExecuted += (s, msg) =>
                {
                    if (_settings?.GeneralSettings?.ShowNotifications == true)
                    {
                        ShowTrayNotification("✅ تنفيذ النسخ الاحتياطي", msg, ToolTipIcon.Info);
                    }
                    SafeInvoke(() => UpdateUI());
                };
                _scheduleService.Start();
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"خطأ في بدء الجدولة: {ex.Message}");
            }
        }

        private void SafeInvoke(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }

        // ==================== UI UPDATE ====================

        private void UpdateUI()
        {
            try
            {
                if (_settings?.BackupJobs == null) return;

                if (dgvJobs != null && !dgvJobs.IsDisposed)
                {
                    var jobsList = _settings.BackupJobs.ToList();

                    dgvJobs.SuspendLayout();
                    dgvJobs.DataSource = null;
                    dgvJobs.DataSource = jobsList;

                    if (dgvJobs.Columns.Count > 0)
                    {
                        ConfigureGridColumns();
                        ApplyRowStyles();
                    }
                    dgvJobs.ResumeLayout();
                }

                UpdateStatusLabels();
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"خطأ في تحديث الواجهة: {ex.Message}");
            }
        }

        private void ConfigureGridColumns()
        {
            try
            {
                if (dgvJobs.Columns.Contains("Id"))
                    dgvJobs.Columns["Id"].Visible = false;
                if (dgvJobs.Columns.Contains("ConnectionId"))
                    dgvJobs.Columns["ConnectionId"].Visible = false;

                // ✅ إعادة ترتيب الأعمدة بحيث يكون زر التنفيذ في النهاية
                if (dgvJobs.Columns.Contains("Execute"))
                {
                    dgvJobs.Columns["Execute"].DisplayIndex = dgvJobs.Columns.Count - 1;
                }

                SetColumnHeader("JobName", "اسم المهمة", 150);
                SetColumnHeader("DatabaseNames", "قواعد البيانات", 160);
                SetColumnHeader("BackupType", "نوع النسخ", 90);
                SetColumnHeader("Compression", "ضغط", 70);
                SetColumnHeader("Encryption", "تشفير", 70);
                SetColumnHeader("RetentionCount", "عدد النسخ", 80);
                SetColumnHeader("IsActive", "الحالة", 70);
                SetColumnHeader("LastRunTime", "آخر تنفيذ", 120);
                SetColumnHeader("LastRunStatus", "النتيجة", 90);

                if (dgvJobs.Columns.Contains("IsActive"))
                {
                    dgvJobs.Columns["IsActive"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
            catch { }
        }

        private void SetColumnHeader(string columnName, string headerText, int width)
        {
            if (dgvJobs.Columns.Contains(columnName))
            {
                dgvJobs.Columns[columnName].HeaderText = headerText;
                dgvJobs.Columns[columnName].MinimumWidth = width;
            }
        }

        private void ApplyRowStyles()
        {
            foreach (DataGridViewRow row in dgvJobs.Rows)
            {
                if (row.DataBoundItem is BackupJob job)
                {
                    if (!job.IsActive)
                    {
                        row.DefaultCellStyle.ForeColor = TextSecondary;
                        row.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
                    }

                    if (dgvJobs.Columns.Contains("LastRunStatus") && row.Cells["LastRunStatus"].Value != null)
                    {
                        string status = row.Cells["LastRunStatus"].Value.ToString();
                        if (status == "Success")
                            row.Cells["LastRunStatus"].Style.ForeColor = Success;
                        else if (status == "Failed")
                            row.Cells["LastRunStatus"].Style.ForeColor = Danger;
                    }

                    // ✅ تنسيق زر التنفيذ
                    if (dgvJobs.Columns.Contains("Execute"))
                    {
                        var executeCell = row.Cells["Execute"];
                        if (job.IsActive)
                        {
                            executeCell.Style.BackColor = Color.FromArgb(34, 197, 94);
                            executeCell.Style.ForeColor = Color.White;
                            executeCell.Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                        }
                        else
                        {
                            executeCell.Style.BackColor = Color.FromArgb(156, 163, 175);
                            executeCell.Style.ForeColor = Color.White;
                            executeCell.Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                        }
                    }
                }
            }
        }

        private void UpdateStatusLabels()
        {
            if (lblActiveJobs != null && !lblActiveJobs.IsDisposed)
            {
                int activeJobs = _settings.BackupJobs.Count(j => j.IsActive);
                lblActiveJobs.Text = $"📊 المهام النشطة: {activeJobs}";
            }

            if (lblLastBackup != null && !lblLastBackup.IsDisposed && lblLastBackupStatus != null)
            {
                var lastJob = _settings.BackupJobs
                    .Where(j => j.LastRunTime > DateTime.MinValue)
                    .OrderByDescending(j => j.LastRunTime)
                    .FirstOrDefault();

                if (lastJob != null)
                {
                    lblLastBackup.Text = $"⏱️ آخر نسخ: {lastJob.LastRunTime:yyyy-MM-dd HH:mm}";
                    bool isSuccess = lastJob.LastRunStatus == "Success";
                    lblLastBackupStatus.Text = $"📌 الحالة: {(isSuccess ? "✅ نجح" : "❌ فشل")}";
                    lblLastBackupStatus.ForeColor = isSuccess ? Success : Danger;
                }
                else
                {
                    lblLastBackup.Text = "⏱️ آخر نسخ: --";
                    lblLastBackupStatus.Text = "📌 الحالة: --";
                    lblLastBackupStatus.ForeColor = TextSecondary;
                }
            }
        }

        // ==================== EXECUTE JOBS ====================

        private async void ExecuteAllJobsNow()
        {
            try
            {
                ShowTrayNotification("▶️ تنفيذ المهام", "جاري تنفيذ جميع المهام النشطة...", ToolTipIcon.Info);

                int executedCount = 0;
                int successCount = 0;

                foreach (var job in _settings.BackupJobs.Where(j => j.IsActive))
                {
                    var connection = _settings.ServerConnections.FirstOrDefault(c => c.Id == job.ConnectionId);
                    var destination = _settings.Destinations.FirstOrDefault(d => d.JobId == job.Id);

                    if (connection != null && destination != null)
                    {
                        var backupService = new BackupService();
                        bool success = await backupService.ExecuteBackup(job, connection, destination);

                        job.LastRunTime = DateTime.Now;
                        job.LastRunStatus = success ? "Success" : "Failed";
                        JsonFileHelper.SaveSettings(_settings);

                        executedCount++;
                        if (success) successCount++;
                    }
                }

                UpdateUI();
                ShowTrayNotification(
                    "✅ اكتمل التنفيذ",
                    $"تم تنفيذ {executedCount} مهمة\nنجحت {successCount} مهمة",
                    ToolTipIcon.Info
                );
            }
            catch (Exception ex)
            {
                ShowTrayNotification("❌ خطأ", $"حدث خطأ: {ex.Message}", ToolTipIcon.Error);
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void btnAddJob_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new AddEditJobForm())
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        _settings.BackupJobs.Add(form.Job);
                        _settings.Schedules.Add(form.Schedule);
                        _settings.Destinations.Add(form.Destination);
                        JsonFileHelper.SaveSettings(_settings);
                        UpdateUI();
                        ShowTrayNotification("✅ تم الإضافة", $"تم إضافة المهمة: {form.Job.JobName}", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStyledMessageBox($"خطأ في إضافة المهمة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEditJob_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvJobs.SelectedRows.Count > 0)
                {
                    var selectedJob = dgvJobs.SelectedRows[0].DataBoundItem as BackupJob;
                    if (selectedJob != null)
                    {
                        var schedule = _settings.Schedules.FirstOrDefault(s => s.JobId == selectedJob.Id);
                        var destination = _settings.Destinations.FirstOrDefault(d => d.JobId == selectedJob.Id);

                        using (var form = new AddEditJobForm(selectedJob, schedule, destination))
                        {
                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                JsonFileHelper.SaveSettings(_settings);
                                UpdateUI();
                                ShowTrayNotification("✅ تم التعديل", $"تم تعديل المهمة: {selectedJob.JobName}", ToolTipIcon.Info);
                            }
                        }
                    }
                }
                else
                {
                    ShowStyledMessageBox("الرجاء اختيار مهمة للتعديل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowStyledMessageBox($"خطأ في تعديل المهمة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteJob_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvJobs.SelectedRows.Count > 0)
                {
                    var selectedJob = dgvJobs.SelectedRows[0].DataBoundItem as BackupJob;
                    if (selectedJob != null)
                    {
                        var result = ShowStyledMessageBox(
                            $"هل أنت متأكد من حذف المهمة '{selectedJob.JobName}'؟\nسيتم حذف جميع البيانات المرتبطة بها.",
                            "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            _settings.BackupJobs.Remove(selectedJob);
                            _settings.Schedules.RemoveAll(s => s.JobId == selectedJob.Id);
                            _settings.Destinations.RemoveAll(d => d.JobId == selectedJob.Id);
                            JsonFileHelper.SaveSettings(_settings);
                            UpdateUI();
                            ShowTrayNotification("✅ تم الحذف", $"تم حذف المهمة: {selectedJob.JobName}", ToolTipIcon.Info);
                        }
                    }
                }
                else
                {
                    ShowStyledMessageBox("الرجاء اختيار مهمة للحذف", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowStyledMessageBox($"خطأ في حذف المهمة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartBackupSuite", "Logs");

                if (Directory.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logPath);
                }
                else
                {
                    ShowStyledMessageBox("لا توجد سجلات بعد", "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowStyledMessageBox($"خطأ في فتح السجلات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new SettingsForm())
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        LoadSettings();
                        UpdateUI();
                        ShowTrayNotification("✅ تم الحفظ", "تم حفظ الإعدادات العامة", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStyledMessageBox($"خطأ في فتح الإعدادات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== FORM EVENTS ====================

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                if (_settings?.GeneralSettings != null)
                {
                    LogService.CleanOldLogs(_settings.GeneralSettings.LogRetentionDays);
                }

                int jobCount = _settings?.BackupJobs?.Count ?? 0;
                EnsureTrayIconVisible();

                ShowTrayNotification(
                    "🔄 Smart Backup Suite",
                    $"تم التشغيل بنجاح\n{jobCount} مهمة محملة",
                    ToolTipIcon.Info,
                    4000
                );
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Error", $"خطأ في التحميل: {ex.Message}");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_isExiting)
            {
                e.Cancel = true;
                HideToTray();
                ShowTrayNotification(
                    "🔄 Smart Backup Suite",
                    "التطبيق يعمل في الخلفية\nانقر نقراً مزدوجاً للإظهار",
                    ToolTipIcon.Info,
                    2000
                );
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && !_isExiting)
            {
                HideToTray();
                ShowTrayNotification(
                    "🔄 Smart Backup Suite",
                    "تم تصغير التطبيق إلى علبة النظام",
                    ToolTipIcon.Info,
                    1500
                );
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            EnsureTrayIconVisible();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _scheduleService?.Stop();
            DisposeTrayIcon();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                DisposeTrayIcon();
                _scheduleService?.Stop();
            }
            base.Dispose(disposing);
        }

        // ==================== HELPER ====================

        private void LoadSettings()
        {
            try
            {
                _settings = JsonFileHelper.ReadSettings() ?? new AppSettings();

                _settings.ServerConnections ??= new System.Collections.Generic.List<ServerConnection>();
                _settings.BackupJobs ??= new System.Collections.Generic.List<BackupJob>();
                _settings.Schedules ??= new System.Collections.Generic.List<Schedule>();
                _settings.Destinations ??= new System.Collections.Generic.List<Destination>();
                _settings.GeneralSettings ??= new GeneralSettings();
            }
            catch (Exception ex)
            {
                ShowStyledMessageBox($"خطأ في تحميل الإعدادات: {ex.Message}\nسيتم استخدام الإعدادات الافتراضية.",
                    "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _settings = new AppSettings();
            }
        }

        private class CustomColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder => BorderLight;
            public override Color MenuItemBorder => Accent;
            public override Color MenuItemSelected => Color.FromArgb(239, 246, 255);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(239, 246, 255);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(239, 246, 255);
            public override Color ToolStripDropDownBackground => Color.White;
        }
    }
}