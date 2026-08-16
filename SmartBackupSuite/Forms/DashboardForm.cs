using SmartBackupSuite.Helpers;
using SmartBackupSuite.Models;
using SmartBackupSuite.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SmartBackupSuite.Forms
{
    public partial class DashboardForm : Form
    {
        #region 📋 المتغيرات

        private AppSettings _settings;
        private System.Windows.Forms.Timer refreshTimer;

        // عناصر التحكم الرئيسية
        private Panel panelHeader, panelTopStats, panelFilters, panelButtons;
        private Label lblTitle, lblSubtitle, lblRecentJobs, lblLastUpdate, lblFilter;
        private TableLayoutPanel tableLayoutStats;
        private List<Label> statValues = new List<Label>();
        private DataGridView dgvJobs;
        private TextBox txtSearch;
        private Button btnSearch, btnApplyFilter, btnResetFilter, btnRefresh, btnClose, btnViewLogs, btnCharts;
        private ComboBox cmbFilter;
        private DateTimePicker dtpFrom, dtpTo;

        #endregion

        #region 🚀 المُنشئ

        public DashboardForm()
        {
            InitializeComponent();
            LoadSettings();
            UpdateDashboard();
            StartAutoRefresh();
        }

        #endregion

        #region 🎨 تهيئة الواجهة

        private void InitializeComponent()
        {
            // ============================================================
            // إعدادات النموذج الرئيسي
            // ============================================================
            this.Text = "📊 Dashboard - لوحة التحكم المتقدمة";
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.RightToLeft = RightToLeft.Yes;
            this.FormClosing += (s, e) => refreshTimer?.Stop();

            // ============================================================
            // 1. Header (الرأس)
            // ============================================================
            this.panelHeader = new Panel();
            this.panelHeader.Dock = DockStyle.Top;
            this.panelHeader.Height = 80;
            this.panelHeader.BackColor = Color.FromArgb(30, 41, 59);
            this.panelHeader.Padding = new Padding(20, 10, 20, 10);

            this.lblTitle = new Label();
            this.lblTitle.Text = "📊 Dashboard - لوحة التحكم المتقدمة";
            this.lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(241, 245, 249);
            this.lblTitle.Dock = DockStyle.Top;
            this.lblTitle.Height = 40;

            this.lblSubtitle = new Label();
            this.lblSubtitle.Text = "مراقبة وإدارة جميع مهام النسخ الاحتياطي في مكان واحد";
            this.lblSubtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            this.lblSubtitle.Dock = DockStyle.Top;
            this.lblSubtitle.Height = 25;

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblSubtitle);

            // ============================================================
            // 2. البطاقات الإحصائية (Top Stats)
            // ============================================================
            this.panelTopStats = new Panel();
            this.panelTopStats.Dock = DockStyle.Top;
            this.panelTopStats.Height = 145;
            this.panelTopStats.BackColor = Color.FromArgb(245, 247, 250);
            this.panelTopStats.Padding = new Padding(12, 10, 12, 10);

            this.tableLayoutStats = new TableLayoutPanel();
            this.tableLayoutStats.Dock = DockStyle.Fill;
            this.tableLayoutStats.ColumnCount = 5;
            this.tableLayoutStats.RowCount = 1;
            this.tableLayoutStats.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            this.tableLayoutStats.BackColor = Color.Transparent;
            this.tableLayoutStats.Padding = new Padding(4);

            for (int i = 0; i < 5; i++)
                this.tableLayoutStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            this.tableLayoutStats.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            string[] icons = { "📋", "✅", "❌", "⏱️", "💾" };
            string[] titles = { "إجمالي المهام", "نسبة النجاح", "مهام فاشلة", "متوسط الوقت", "إجمالي الحجم" };
            Color[] colors = {
                Color.FromArgb(59, 130, 246),
                Color.FromArgb(16, 185, 129),
                Color.FromArgb(239, 68, 68),
                Color.FromArgb(245, 158, 11),
                Color.FromArgb(139, 92, 246)
            };

            for (int i = 0; i < 5; i++)
                this.tableLayoutStats.Controls.Add(CreateStatCard(icons[i], titles[i], "0", colors[i]), i, 0);

            this.panelTopStats.Controls.Add(this.tableLayoutStats);

            // ============================================================
            // 3. القسم السفلي (Filters + Table + Buttons)
            // ============================================================
            var panelBottom = new Panel();
            panelBottom.Dock = DockStyle.Fill;
            panelBottom.BackColor = Color.White;
            panelBottom.Padding = new Padding(10);

            // 3.1 عنوان الجدول
            this.lblRecentJobs = new Label();
            this.lblRecentJobs.Text = "📋 المهام";
            this.lblRecentJobs.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblRecentJobs.ForeColor = Color.FromArgb(44, 62, 80);
            this.lblRecentJobs.Dock = DockStyle.Top;
            this.lblRecentJobs.Height = 30;

            // 3.2 وقت آخر تحديث
            this.lblLastUpdate = new Label();
            this.lblLastUpdate.Text = "آخر تحديث: --:--";
            this.lblLastUpdate.Font = new Font("Segoe UI", 9F);
            this.lblLastUpdate.ForeColor = Color.FromArgb(149, 165, 166);
            this.lblLastUpdate.Dock = DockStyle.Top;
            this.lblLastUpdate.Height = 20;
            this.lblLastUpdate.TextAlign = ContentAlignment.MiddleRight;

            // 3.3 لوحة الفلاتر
            this.panelFilters = new Panel();
            this.panelFilters.Dock = DockStyle.Top;
            this.panelFilters.Height = 65;
            this.panelFilters.BackColor = Color.White;
            this.panelFilters.Padding = new Padding(5);
            this.panelFilters.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), 0, panelFilters.Height - 1, panelFilters.Width, panelFilters.Height - 1);
            };

            var flowFilters = new FlowLayoutPanel();
            flowFilters.Dock = DockStyle.Fill;
            flowFilters.FlowDirection = FlowDirection.LeftToRight;
            flowFilters.WrapContents = true;
            flowFilters.Padding = new Padding(5);

            // فلتر الحالة
            this.lblFilter = new Label();
            this.lblFilter.Text = "📊 الحالة:";
            this.lblFilter.Font = new Font("Segoe UI", 9F);
            this.lblFilter.AutoSize = true;

            this.cmbFilter = new ComboBox();
            this.cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbFilter.Size = new Size(120, 25);
            this.cmbFilter.Font = new Font("Segoe UI", 9F);
            this.cmbFilter.FlatStyle = FlatStyle.Flat;
            this.cmbFilter.BackColor = Color.White;
            this.cmbFilter.Items.AddRange(new object[] { "الكل", "✅ نجح", "❌ فشل", "⏳ قيد الانتظار" });
            this.cmbFilter.SelectedIndex = 0;
            this.cmbFilter.SelectedIndexChanged += (s, e) => ApplyFilters();

            // حقل البحث
            this.txtSearch = new TextBox();
            this.txtSearch.Size = new Size(150, 25);
            this.txtSearch.Font = new Font("Segoe UI", 10F);
            this.txtSearch.BorderStyle = BorderStyle.FixedSingle;
            this.txtSearch.BackColor = Color.White;
            this.txtSearch.ForeColor = Color.FromArgb(51, 65, 85);
            this.txtSearch.TextChanged += (s, e) => ApplyFilters();

            // زر البحث
            this.btnSearch = new Button();
            this.btnSearch.Text = "🔍 بحث";
            this.btnSearch.Size = new Size(75, 25);
            this.btnSearch.BackColor = Color.FromArgb(37, 99, 235);
            this.btnSearch.ForeColor = Color.White;
            this.btnSearch.FlatStyle = FlatStyle.Flat;
            this.btnSearch.FlatAppearance.BorderSize = 0;
            this.btnSearch.Cursor = Cursors.Hand;
            this.btnSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnSearch.Click += (s, e) => ApplyFilters();

            // فلتر التاريخ - من
            var lblFrom = new Label();
            lblFrom.Text = "📅 من:";
            lblFrom.Font = new Font("Segoe UI", 9F);
            lblFrom.AutoSize = true;

            this.dtpFrom = new DateTimePicker();
            this.dtpFrom.Format = DateTimePickerFormat.Short;
            this.dtpFrom.Size = new Size(100, 25);
            this.dtpFrom.Font = new Font("Segoe UI", 9F);
            this.dtpFrom.Value = DateTime.Now.AddDays(-30);
            this.dtpFrom.CalendarTitleBackColor = Color.FromArgb(30, 41, 59);
            this.dtpFrom.ValueChanged += (s, e) => ApplyFilters();

            // فلتر التاريخ - إلى
            var lblTo = new Label();
            lblTo.Text = "إلى:";
            lblTo.Font = new Font("Segoe UI", 9F);
            lblTo.AutoSize = true;

            this.dtpTo = new DateTimePicker();
            this.dtpTo.Format = DateTimePickerFormat.Short;
            this.dtpTo.Size = new Size(100, 25);
            this.dtpTo.Font = new Font("Segoe UI", 9F);
            this.dtpTo.Value = DateTime.Now;
            this.dtpTo.CalendarTitleBackColor = Color.FromArgb(30, 41, 59);
            this.dtpTo.ValueChanged += (s, e) => ApplyFilters();

            // زر تطبيق الفلتر
            this.btnApplyFilter = new Button();
            this.btnApplyFilter.Text = "🔍 تطبيق";
            this.btnApplyFilter.Size = new Size(75, 25);
            this.btnApplyFilter.BackColor = Color.FromArgb(46, 204, 113);
            this.btnApplyFilter.ForeColor = Color.White;
            this.btnApplyFilter.FlatStyle = FlatStyle.Flat;
            this.btnApplyFilter.FlatAppearance.BorderSize = 0;
            this.btnApplyFilter.Cursor = Cursors.Hand;
            this.btnApplyFilter.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnApplyFilter.Click += (s, e) => ApplyFilters();

            // زر إعادة تعيين الفلتر
            this.btnResetFilter = new Button();
            this.btnResetFilter.Text = "🔄 إعادة تعيين";
            this.btnResetFilter.Size = new Size(95, 25);
            this.btnResetFilter.BackColor = Color.FromArgb(52, 152, 219);
            this.btnResetFilter.ForeColor = Color.White;
            this.btnResetFilter.FlatStyle = FlatStyle.Flat;
            this.btnResetFilter.FlatAppearance.BorderSize = 0;
            this.btnResetFilter.Cursor = Cursors.Hand;
            this.btnResetFilter.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnResetFilter.Click += (s, e) =>
            {
                dtpFrom.Value = DateTime.Now.AddDays(-30);
                dtpTo.Value = DateTime.Now;
                cmbFilter.SelectedIndex = 0;
                txtSearch.Text = "";
                ApplyFilters();
            };

            // إضافة جميع عناصر الفلتر إلى FlowLayoutPanel
            flowFilters.Controls.Add(this.lblFilter);
            flowFilters.Controls.Add(this.cmbFilter);
            flowFilters.Controls.Add(this.txtSearch);
            flowFilters.Controls.Add(this.btnSearch);
            flowFilters.Controls.Add(lblFrom);
            flowFilters.Controls.Add(this.dtpFrom);
            flowFilters.Controls.Add(lblTo);
            flowFilters.Controls.Add(this.dtpTo);
            flowFilters.Controls.Add(this.btnApplyFilter);
            flowFilters.Controls.Add(this.btnResetFilter);

            this.panelFilters.Controls.Add(flowFilters);

            // 3.4 جدول المهام
            this.dgvJobs = new DataGridView();
            this.dgvJobs.Dock = DockStyle.Fill;
            this.dgvJobs.AllowUserToAddRows = false;
            this.dgvJobs.AllowUserToDeleteRows = false;
            this.dgvJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvJobs.BackgroundColor = Color.White;
            this.dgvJobs.ReadOnly = true;
            this.dgvJobs.RowHeadersVisible = false;
            this.dgvJobs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvJobs.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            this.dgvJobs.BorderStyle = BorderStyle.None;
            this.dgvJobs.Font = new Font("Segoe UI", 9F);
            this.dgvJobs.EnableHeadersVisualStyles = false;
            this.dgvJobs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            this.dgvJobs.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            this.dgvJobs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.dgvJobs.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dgvJobs.ColumnHeadersHeight = 40;
            this.dgvJobs.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            this.dgvJobs.RowTemplate.Height = 32;
            this.dgvJobs.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dgvJobs.DefaultCellStyle.Padding = new Padding(4);

            // 3.5 الأزرار السفلية
            this.panelButtons = new Panel();
            this.panelButtons.Dock = DockStyle.Bottom;
            this.panelButtons.Height = 55;
            this.panelButtons.BackColor = Color.FromArgb(248, 249, 250);
            this.panelButtons.Padding = new Padding(10, 8, 10, 8);

            var flowButtons = new FlowLayoutPanel();
            flowButtons.Dock = DockStyle.Fill;
            flowButtons.FlowDirection = FlowDirection.LeftToRight;
            flowButtons.WrapContents = false;
            flowButtons.Padding = new Padding(5);
            flowButtons.Margin = new Padding(0);

            // ✅ زر تحديث
            this.btnRefresh = new Button();
            this.btnRefresh.Text = "🔄 تحديث";
            this.btnRefresh.Size = new Size(110, 38);
            this.btnRefresh.BackColor = Color.FromArgb(37, 99, 235);
            this.btnRefresh.ForeColor = Color.White;
            this.btnRefresh.FlatStyle = FlatStyle.Flat;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.Cursor = Cursors.Hand;
            this.btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnRefresh.Click += (s, e) => UpdateDashboard();

            // ✅ زر المخططات
            this.btnCharts = new Button();
            this.btnCharts.Text = "📊 المخططات";
            this.btnCharts.Size = new Size(110, 38);
            this.btnCharts.BackColor = Color.FromArgb(124, 58, 237);
            this.btnCharts.ForeColor = Color.White;
            this.btnCharts.FlatStyle = FlatStyle.Flat;
            this.btnCharts.FlatAppearance.BorderSize = 0;
            this.btnCharts.Cursor = Cursors.Hand;
            this.btnCharts.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnCharts.Click += (s, e) => OpenChartsForm();

            // ✅ زر السجلات
            this.btnViewLogs = new Button();
            this.btnViewLogs.Text = "📋 السجلات";
            this.btnViewLogs.Size = new Size(110, 38);
            this.btnViewLogs.BackColor = Color.FromArgb(13, 148, 136);
            this.btnViewLogs.ForeColor = Color.White;
            this.btnViewLogs.FlatStyle = FlatStyle.Flat;
            this.btnViewLogs.FlatAppearance.BorderSize = 0;
            this.btnViewLogs.Cursor = Cursors.Hand;
            this.btnViewLogs.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnViewLogs.Click += BtnViewLogs_Click;

            // ✅ زر إغلاق
            this.btnClose = new Button();
            this.btnClose.Text = "❌ إغلاق";
            this.btnClose.Size = new Size(110, 38);
            this.btnClose.BackColor = Color.FromArgb(220, 38, 38);
            this.btnClose.ForeColor = Color.White;
            this.btnClose.FlatStyle = FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.Cursor = Cursors.Hand;
            this.btnClose.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnClose.Click += (s, e) => this.Close();

            flowButtons.Controls.Add(this.btnRefresh);
            flowButtons.Controls.Add(this.btnCharts);
            flowButtons.Controls.Add(this.btnViewLogs);
            flowButtons.Controls.Add(this.btnClose);

            this.panelButtons.Controls.Add(flowButtons);

            // إضافة جميع العناصر إلى panelBottom
            panelBottom.Controls.Add(this.dgvJobs);
            panelBottom.Controls.Add(this.panelButtons);
            panelBottom.Controls.Add(this.panelFilters);
            panelBottom.Controls.Add(this.lblLastUpdate);
            panelBottom.Controls.Add(this.lblRecentJobs);

            // ============================================================
            // 4. تجميع كل الأقسام في النموذج
            // ============================================================
            this.Controls.Add(panelBottom);
            this.Controls.Add(this.panelTopStats);
            this.Controls.Add(this.panelHeader);

            // ============================================================
            // 5. Timer للتحديث التلقائي
            // ============================================================
            this.refreshTimer = new System.Windows.Forms.Timer();
            this.refreshTimer.Interval = 30000;
            this.refreshTimer.Tick += (s, e) => UpdateDashboard();
        }

        #endregion

        #region 🎨 دوال إنشاء العناصر

        private Panel CreateStatCard(string icon, string title, string value, Color accentColor)
        {
            var card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = Color.White;
            card.Margin = new Padding(8);
            card.Padding = new Padding(0);

            card.Paint += (s, pe) => {
                using (var brush = new SolidBrush(accentColor))
                {
                    pe.Graphics.FillRectangle(brush, 12, 0, card.Width - 24, 4);
                }
                using (var pen = new Pen(Color.FromArgb(40, 0, 0, 0)))
                {
                    pe.Graphics.DrawLine(pen, 0, card.Height - 1, card.Width, card.Height - 1);
                }
            };

            var innerTable = new TableLayoutPanel();
            innerTable.Dock = DockStyle.Fill;
            innerTable.RowCount = 3;
            innerTable.ColumnCount = 1;
            innerTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            innerTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            innerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            innerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            innerTable.BackColor = Color.Transparent;
            innerTable.Margin = new Padding(0);
            innerTable.Padding = new Padding(6, 8, 6, 6);

            var iconLabel = new Label();
            iconLabel.Text = icon;
            iconLabel.Font = new Font("Segoe UI", 24F);
            iconLabel.ForeColor = accentColor;
            iconLabel.Dock = DockStyle.Fill;
            iconLabel.TextAlign = ContentAlignment.MiddleCenter;

            var titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            titleLabel.ForeColor = Color.FromArgb(100, 116, 139);
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.AutoSize = false;

            var valueLabel = new Label();
            valueLabel.Text = value;
            valueLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            valueLabel.ForeColor = Color.FromArgb(15, 23, 42);
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.TextAlign = ContentAlignment.MiddleCenter;
            valueLabel.AutoSize = false;
            valueLabel.Name = "valueLabel";

            innerTable.Controls.Add(iconLabel, 0, 0);
            innerTable.Controls.Add(titleLabel, 0, 1);
            innerTable.Controls.Add(valueLabel, 0, 2);

            card.Controls.Add(innerTable);

            statValues.Add(valueLabel);

            return card;
        }

        #endregion

        #region 📊 دوال البيانات الأساسية

        private void LoadSettings()
        {
            try
            {
                _settings = JsonFileHelper.ReadSettings();
                if (_settings == null) _settings = new AppSettings();
                if (_settings.BackupJobs == null) _settings.BackupJobs = new List<BackupJob>();
            }
            catch
            {
                _settings = new AppSettings();
                _settings.BackupJobs = new List<BackupJob>();
            }
        }

        private void StartAutoRefresh()
        {
            refreshTimer?.Start();
        }

        private void UpdateDashboard()
        {
            try
            {
                _settings = JsonFileHelper.ReadSettings();
                if (_settings.BackupJobs == null) _settings.BackupJobs = new List<BackupJob>();

                UpdateTopStats();
                ApplyFilters();

                lblLastUpdate.Text = $"آخر تحديث: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dashboard", "Error", $"خطأ في تحديث لوحة التحكم: {ex.Message}");
            }
        }

        private void UpdateStatCardValue(int index, string value)
        {
            try
            {
                if (statValues != null && index < statValues.Count)
                    statValues[index].Text = value;
            }
            catch { }
        }

        #endregion

        #region 📈 تحديث الإحصائيات العلوية

        private void UpdateTopStats()
        {
            try
            {
                var jobs = _settings.BackupJobs ?? new List<BackupJob>();

                int totalJobs = jobs.Count;
                int activeJobs = jobs.Count(j => j.IsActive);
                int successCount = jobs.Count(j => j.LastRunStatus == "Success");
                int failedCount = jobs.Count(j => j.LastRunStatus == "Failed");
                int totalExecuted = successCount + failedCount;
                double successRate = totalExecuted > 0 ? (double)successCount / totalExecuted * 100 : 0;

                var jobsWithTime = jobs.Where(j => j.LastRunTime > DateTime.MinValue).ToList();
                string avgTime = jobsWithTime.Any() ? $"{jobsWithTime.Average(j => (DateTime.Now - j.LastRunTime).TotalMinutes):F0} د" : "--";

                string totalSize = "0 MB";
                try
                {
                    string backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                    if (Directory.Exists(backupPath))
                    {
                        var files = Directory.GetFiles(backupPath, "*.zip");
                        long totalBytes = files.Sum(f => new FileInfo(f).Length);
                        totalSize = totalBytes > 0 ? $"{totalBytes / 1024 / 1024} MB" : "0 MB";
                    }
                }
                catch { }

                UpdateStatCardValue(0, $"{totalJobs} ({activeJobs} نشطة)");
                UpdateStatCardValue(1, $"{successRate:F0}%");
                UpdateStatCardValue(2, $"{failedCount}");
                UpdateStatCardValue(3, avgTime);
                UpdateStatCardValue(4, totalSize);
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dashboard", "Error", $"خطأ في تحديث الإحصائيات: {ex.Message}");
            }
        }

        #endregion

        #region 🔍 تطبيق الفلاتر

        private void ApplyFilters()
        {
            try
            {
                var jobs = _settings.BackupJobs ?? new List<BackupJob>();

                string filter = cmbFilter.SelectedItem?.ToString() ?? "الكل";
                var filteredJobs = jobs.AsEnumerable();

                if (filter != "الكل")
                {
                    string status = filter switch
                    {
                        "✅ نجح" => "Success",
                        "❌ فشل" => "Failed",
                        "⏳ قيد الانتظار" => "",
                        _ => ""
                    };
                    if (!string.IsNullOrEmpty(status))
                        filteredJobs = filteredJobs.Where(j => j.LastRunStatus == status);
                    else
                        filteredJobs = filteredJobs.Where(j => string.IsNullOrEmpty(j.LastRunStatus));
                }

                string fromDate = dtpFrom.Value.ToString("yyyy-MM-dd");
                string toDate = dtpTo.Value.ToString("yyyy-MM-dd");
                filteredJobs = filteredJobs.Where(j =>
                    j.LastRunTime >= DateTime.Parse(fromDate) &&
                    j.LastRunTime <= DateTime.Parse(toDate).AddDays(1)
                );

                string search = txtSearch.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(search))
                {
                    filteredJobs = filteredJobs.Where(j =>
                        j.JobName.ToLower().Contains(search) ||
                        (j.DatabaseNames != null && j.DatabaseNames.Any(db => db.ToLower().Contains(search)))
                    );
                }

                var recentJobs = filteredJobs
                    .OrderByDescending(j => j.LastRunTime)
                    .Take(100)
                    .Select(j => new
                    {
                        اسم_المهمة = j.JobName,
                        قاعدة_البيانات = string.Join(", ", j.DatabaseNames ?? new List<string>()),
                        نوع_النسخ = j.BackupType,
                        آخر_تنفيذ = j.LastRunTime > DateTime.MinValue ? j.LastRunTime.ToString("yyyy-MM-dd HH:mm") : "--",
                        الحالة = j.LastRunStatus == "Success" ? "✅ نجح" : j.LastRunStatus == "Failed" ? "❌ فشل" : "⏳ قيد الانتظار",
                        نشطة = j.IsActive ? "✅" : "❌",
                        الوجهة = _settings.Destinations?.FirstOrDefault(d => d.JobId == j.Id)?.DestinationType ?? "--"
                    })
                    .ToList();

                dgvJobs.DataSource = null;
                dgvJobs.DataSource = recentJobs;

                if (dgvJobs.Columns.Count > 0)
                {
                    foreach (DataGridViewColumn col in dgvJobs.Columns)
                    {
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        if (col.Name == "الحالة")
                            col.DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                    }
                }

                lblRecentJobs.Text = $"📋 المهام (المعروض: {recentJobs.Count} من {_settings.BackupJobs?.Count ?? 0})";
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dashboard", "Error", $"خطأ في تطبيق الفلاتر: {ex.Message}");
            }
        }

        #endregion

        #region 📊 فتح نافذة المخططات

        private void OpenChartsForm()
        {
            try
            {
                using (var chartsForm = new ChartsForm())
                {
                    chartsForm.StartPosition = FormStartPosition.CenterParent;
                    chartsForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح المخططات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 📋 عرض السجلات (نافذة منبثقة محسّنة)

        private void BtnViewLogs_Click(object sender, EventArgs e)
        {
            try
            {
                // ✅ استخدام مجلد Logs
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartBackupSuite",
                    "Logs"
                );

                if (!Directory.Exists(logPath))
                {
                    MessageBox.Show("لا توجد سجلات بعد.", "معلومات",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var logFiles = Directory.GetFiles(logPath, "*.log")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                if (logFiles.Count == 0)
                {
                    MessageBox.Show("لا توجد سجلات بعد.", "معلومات",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var allLogEntries = new List<LogEntry>();

                foreach (var file in logFiles)
                {
                    var lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var logEntry = ParseLogLine(line);
                            if (logEntry != null)
                                allLogEntries.Add(logEntry);
                        }
                    }
                }

                if (allLogEntries.Count == 0)
                {
                    MessageBox.Show("لا توجد سجلات متاحة.", "معلومات",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowLogsDialog(allLogEntries);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض السجلات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private LogEntry ParseLogLine(string line)
        {
            try
            {
                // ✅ تنسيق: 2026-08-12 09:12:11 [LEVEL] [SOURCE] MESSAGE
                if (string.IsNullOrWhiteSpace(line) || line.Length < 16) return null;

                // استخراج التاريخ والوقت
                string datePart = line.Substring(0, 10);
                string timePart = line.Substring(11, 8);

                // استخراج الحالة
                string status = GetStatusFromLine(line);
                if (status == "ℹ️ معلومات") return null;

                // استخراج اسم المهمة
                string jobName = ExtractJobName(line);
                string message = ExtractMessage(line);

                if (!string.IsNullOrEmpty(datePart))
                {
                    return new LogEntry
                    {
                        Date = datePart,
                        Time = timePart,
                        JobName = jobName,
                        Status = status,
                        Message = message
                    };
                }

                return null;
            }
            catch { return null; }
        }

        private string GetStatusFromLine(string line)
        {
            if (line.Contains("✅") || line.Contains("Success") || line.Contains("[SUCCESS]"))
                return "✅ نجح";
            else if (line.Contains("❌") || line.Contains("Failed") || line.Contains("[ERROR]") || line.Contains("Error"))
                return "❌ فشل";
            else if (line.Contains("[WARNING]") || line.Contains("Warning"))
                return "⚠️ تحذير";
            else
                return "ℹ️ معلومات";
        }

        private string ExtractJobName(string line)
        {
            // ✅ البحث عن اسم المهمة بين الأقواس المربعة الثانية
            int startIndex = line.IndexOf('[');
            int endIndex = -1;
            int bracketCount = 0;

            for (int i = startIndex + 1; i < line.Length; i++)
            {
                if (line[i] == '[') bracketCount++;
                if (line[i] == ']')
                {
                    if (bracketCount == 0)
                    {
                        endIndex = i;
                        break;
                    }
                    bracketCount--;
                }
            }

            if (startIndex >= 0 && endIndex > startIndex)
            {
                string between = line.Substring(startIndex + 1, endIndex - startIndex - 1);
                if (!between.Contains(":") && !between.Contains("-"))
                    return between;
            }

            // محاولة البحث عن Job:
            int jobIndex = line.IndexOf("Job:");
            if (jobIndex >= 0)
            {
                int start = jobIndex + 5;
                int end = line.IndexOf("|", start);
                if (end > start)
                    return line.Substring(start, end - start).Trim();
                return line.Substring(start).Trim();
            }

            return "";
        }

        private string ExtractMessage(string line)
        {
            // ✅ استخراج الرسالة بعد آخر علامة ]
            int lastBracket = line.LastIndexOf(']');
            if (lastBracket >= 0 && lastBracket < line.Length - 1)
                return line.Substring(lastBracket + 1).Trim();

            // محاولة البحث عن Message:
            int msgIndex = line.IndexOf("Message:");
            if (msgIndex >= 0)
                return line.Substring(msgIndex + 9).Trim();

            // محاولة البحث عن آخر |
            int lastPipe = line.LastIndexOf('|');
            if (lastPipe >= 0)
                return line.Substring(lastPipe + 1).Trim();

            return "";
        }

        private void ShowLogsDialog(List<LogEntry> allEntries)
        {
            var dgvLogs = new DataGridView();

            var logsForm = new Form();
            logsForm.Text = "📋 سجلات النسخ الاحتياطي";
            logsForm.Size = new Size(1200, 700);
            logsForm.StartPosition = FormStartPosition.CenterParent;
            logsForm.RightToLeft = RightToLeft.Yes;
            logsForm.MinimumSize = new Size(1000, 550);
            logsForm.BackColor = Color.FromArgb(248, 249, 250);

            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.RowCount = 2;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.ColumnCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // شريط التحكم العلوي
            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;
            topPanel.BackColor = Color.FromArgb(30, 41, 59);
            topPanel.Padding = new Padding(15, 10, 15, 10);

            var lblLogsTitle = new Label();
            int totalLogs = allEntries.Count;
            int successCount = allEntries.Count(e => e.Status == "✅ نجح");
            int failedCount = allEntries.Count(e => e.Status == "❌ فشل");
            int warningCount = allEntries.Count(e => e.Status == "⚠️ تحذير");

            lblLogsTitle.Text = $"📋 السجلات (إجمالي: {totalLogs} - نجاح: {successCount} | فشل: {failedCount} | تحذير: {warningCount})";
            lblLogsTitle.ForeColor = Color.FromArgb(241, 245, 249);
            lblLogsTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblLogsTitle.Location = new Point(10, 5);
            lblLogsTitle.AutoSize = true;

            // صف الفلاتر
            var filterRow = new Panel();
            filterRow.Dock = DockStyle.Bottom;
            filterRow.Height = 35;
            filterRow.BackColor = Color.Transparent;
            filterRow.Padding = new Padding(0, 2, 0, 2);

            var flowFilterRow = new FlowLayoutPanel();
            flowFilterRow.Dock = DockStyle.Fill;
            flowFilterRow.FlowDirection = FlowDirection.LeftToRight;
            flowFilterRow.WrapContents = false;
            flowFilterRow.Padding = new Padding(0);
            flowFilterRow.Margin = new Padding(0);

            // فلتر التاريخ - من
            var lblFrom = new Label();
            lblFrom.Text = "📅 من:";
            lblFrom.ForeColor = Color.White;
            lblFrom.Font = new Font("Segoe UI", 9F);
            lblFrom.AutoSize = true;
            lblFrom.Margin = new Padding(0, 5, 5, 5);

            var dtpFromLogs = new DateTimePicker();
            dtpFromLogs.Format = DateTimePickerFormat.Short;
            dtpFromLogs.Size = new Size(100, 23);
            dtpFromLogs.Font = new Font("Segoe UI", 9F);
            dtpFromLogs.Value = DateTime.Now.AddDays(-30);
            dtpFromLogs.Margin = new Padding(0, 2, 10, 2);

            // فلتر التاريخ - إلى
            var lblTo = new Label();
            lblTo.Text = "إلى:";
            lblTo.ForeColor = Color.White;
            lblTo.Font = new Font("Segoe UI", 9F);
            lblTo.AutoSize = true;
            lblTo.Margin = new Padding(0, 5, 5, 5);

            var dtpToLogs = new DateTimePicker();
            dtpToLogs.Format = DateTimePickerFormat.Short;
            dtpToLogs.Size = new Size(100, 23);
            dtpToLogs.Font = new Font("Segoe UI", 9F);
            dtpToLogs.Value = DateTime.Now;
            dtpToLogs.Margin = new Padding(0, 2, 10, 2);

            // فلتر الحالة
            var lblStatusFilter = new Label();
            lblStatusFilter.Text = "📊 الحالة:";
            lblStatusFilter.ForeColor = Color.White;
            lblStatusFilter.Font = new Font("Segoe UI", 9F);
            lblStatusFilter.AutoSize = true;
            lblStatusFilter.Margin = new Padding(0, 5, 5, 5);

            var cmbStatusFilter = new ComboBox();
            cmbStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatusFilter.Size = new Size(100, 23);
            cmbStatusFilter.Font = new Font("Segoe UI", 9F);
            cmbStatusFilter.Items.AddRange(new object[] { "الكل", "✅ نجح", "❌ فشل", "⚠️ تحذير" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.Margin = new Padding(0, 2, 10, 2);

            // حقل بحث
            var lblSearch = new Label();
            lblSearch.Text = "🔍 بحث:";
            lblSearch.ForeColor = Color.White;
            lblSearch.Font = new Font("Segoe UI", 9F);
            lblSearch.AutoSize = true;
            lblSearch.Margin = new Padding(0, 5, 5, 5);

            var txtLogSearch = new TextBox();
            txtLogSearch.Size = new Size(130, 23);
            txtLogSearch.Font = new Font("Segoe UI", 10F);
            txtLogSearch.Margin = new Padding(0, 2, 10, 2);
            txtLogSearch.PlaceholderText = "بحث...";

            // زر تطبيق الفلتر
            var btnApplyFilterLogs = new Button();
            btnApplyFilterLogs.Text = "🔍 تطبيق";
            btnApplyFilterLogs.Size = new Size(70, 25);
            btnApplyFilterLogs.BackColor = Color.FromArgb(46, 204, 113);
            btnApplyFilterLogs.ForeColor = Color.White;
            btnApplyFilterLogs.FlatStyle = FlatStyle.Flat;
            btnApplyFilterLogs.FlatAppearance.BorderSize = 0;
            btnApplyFilterLogs.Cursor = Cursors.Hand;
            btnApplyFilterLogs.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnApplyFilterLogs.Margin = new Padding(0, 2, 5, 2);

            // زر إعادة تعيين الفلتر
            var btnResetFilterLogs = new Button();
            btnResetFilterLogs.Text = "🔄 إعادة تعيين";
            btnResetFilterLogs.Size = new Size(90, 25);
            btnResetFilterLogs.BackColor = Color.FromArgb(52, 152, 219);
            btnResetFilterLogs.ForeColor = Color.White;
            btnResetFilterLogs.FlatStyle = FlatStyle.Flat;
            btnResetFilterLogs.FlatAppearance.BorderSize = 0;
            btnResetFilterLogs.Cursor = Cursors.Hand;
            btnResetFilterLogs.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnResetFilterLogs.Margin = new Padding(0, 2, 5, 2);

            // زر إغلاق
            var btnCloseLogs = new Button();
            btnCloseLogs.Text = "❌ إغلاق";
            btnCloseLogs.Size = new Size(80, 25);
            btnCloseLogs.BackColor = Color.FromArgb(231, 76, 60);
            btnCloseLogs.ForeColor = Color.White;
            btnCloseLogs.FlatStyle = FlatStyle.Flat;
            btnCloseLogs.FlatAppearance.BorderSize = 0;
            btnCloseLogs.Cursor = Cursors.Hand;
            btnCloseLogs.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnCloseLogs.Margin = new Padding(0, 2, 0, 2);

            // ربط الأحداث
            btnApplyFilterLogs.Click += (s, ev) =>
            {
                string fromDate = dtpFromLogs.Value.ToString("yyyy-MM-dd");
                string toDate = dtpToLogs.Value.ToString("yyyy-MM-dd");
                ApplyDateFilter(dgvLogs, allEntries, fromDate, toDate, lblLogsTitle, cmbStatusFilter, txtLogSearch);
            };

            btnResetFilterLogs.Click += (s, ev) =>
            {
                dtpFromLogs.Value = DateTime.Now.AddDays(-30);
                dtpToLogs.Value = DateTime.Now;
                cmbStatusFilter.SelectedIndex = 0;
                txtLogSearch.Text = "";
                ApplyDateFilter(dgvLogs, allEntries,
                    dtpFromLogs.Value.ToString("yyyy-MM-dd"),
                    dtpToLogs.Value.ToString("yyyy-MM-dd"),
                    lblLogsTitle, cmbStatusFilter, txtLogSearch);
            };

            cmbStatusFilter.SelectedIndexChanged += (s, ev) =>
            {
                string fromDate = dtpFromLogs.Value.ToString("yyyy-MM-dd");
                string toDate = dtpToLogs.Value.ToString("yyyy-MM-dd");
                ApplyDateFilter(dgvLogs, allEntries, fromDate, toDate, lblLogsTitle, cmbStatusFilter, txtLogSearch);
            };

            txtLogSearch.TextChanged += (s, ev) =>
            {
                string fromDate = dtpFromLogs.Value.ToString("yyyy-MM-dd");
                string toDate = dtpToLogs.Value.ToString("yyyy-MM-dd");
                ApplyDateFilter(dgvLogs, allEntries, fromDate, toDate, lblLogsTitle, cmbStatusFilter, txtLogSearch);
            };

            btnCloseLogs.Click += (s, ev) => logsForm.Close();

            // إضافة العناصر إلى filterRow
            flowFilterRow.Controls.Add(lblFrom);
            flowFilterRow.Controls.Add(dtpFromLogs);
            flowFilterRow.Controls.Add(lblTo);
            flowFilterRow.Controls.Add(dtpToLogs);
            flowFilterRow.Controls.Add(lblStatusFilter);
            flowFilterRow.Controls.Add(cmbStatusFilter);
            flowFilterRow.Controls.Add(lblSearch);
            flowFilterRow.Controls.Add(txtLogSearch);
            flowFilterRow.Controls.Add(btnApplyFilterLogs);
            flowFilterRow.Controls.Add(btnResetFilterLogs);
            flowFilterRow.Controls.Add(btnCloseLogs);

            filterRow.Controls.Add(flowFilterRow);

            topPanel.Controls.Add(filterRow);
            topPanel.Controls.Add(lblLogsTitle);

            // إعدادات جدول السجلات
            dgvLogs.Dock = DockStyle.Fill;
            dgvLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvLogs.BackgroundColor = Color.White;
            dgvLogs.ReadOnly = true;
            dgvLogs.RowHeadersVisible = false;
            dgvLogs.AllowUserToAddRows = false;
            dgvLogs.AllowUserToDeleteRows = false;
            dgvLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLogs.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgvLogs.BorderStyle = BorderStyle.None;
            dgvLogs.Font = new Font("Segoe UI", 9F);
            dgvLogs.EnableHeadersVisualStyles = false;
            dgvLogs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvLogs.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvLogs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvLogs.ColumnHeadersHeight = 38;
            dgvLogs.RowTemplate.Height = 30;
            dgvLogs.DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
            dgvLogs.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // إضافة الأعمدة
            dgvLogs.Columns.Add("Date", "📅 التاريخ");
            dgvLogs.Columns.Add("Time", "⏰ الوقت");
            dgvLogs.Columns.Add("JobName", "📌 المهمة");
            dgvLogs.Columns.Add("Status", "📊 الحالة");
            dgvLogs.Columns.Add("Message", "📝 الرسالة");

            dgvLogs.Columns["Date"].Width = 100;
            dgvLogs.Columns["Time"].Width = 70;
            dgvLogs.Columns["JobName"].Width = 120;
            dgvLogs.Columns["Status"].Width = 90;
            dgvLogs.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // تطبيق الفلتر الأولي
            ApplyDateFilter(dgvLogs, allEntries,
                DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"),
                DateTime.Now.ToString("yyyy-MM-dd"),
                lblLogsTitle, cmbStatusFilter, txtLogSearch);

            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(dgvLogs, 0, 1);

            logsForm.Controls.Add(mainPanel);
            logsForm.ShowDialog();
        }

        private void ApplyDateFilter(DataGridView dgvLogs, List<LogEntry> allEntries,
            string fromDate, string toDate, Label titleLabel, ComboBox cmbStatusFilter, TextBox txtLogSearch)
        {
            try
            {
                if (dgvLogs == null) return;

                dgvLogs.Rows.Clear();

                var filtered = allEntries.AsEnumerable();

                // فلتر التاريخ
                filtered = filtered.Where(l =>
                    string.Compare(l.Date, fromDate) >= 0 &&
                    string.Compare(l.Date, toDate) <= 0);

                // فلتر الحالة
                string selectedStatus = cmbStatusFilter.SelectedItem?.ToString() ?? "الكل";
                if (selectedStatus != "الكل")
                {
                    filtered = filtered.Where(l => l.Status == selectedStatus);
                }

                // البحث النصي
                string searchText = txtLogSearch.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = filtered.Where(l =>
                        l.JobName.ToLower().Contains(searchText) ||
                        l.Message.ToLower().Contains(searchText) ||
                        l.Date.Contains(searchText)
                    );
                }

                var result = filtered
                    .OrderByDescending(l => l.Date)
                    .ThenByDescending(l => l.Time)
                    .ToList();

                int totalDisplayed = result.Count;
                int totalSuccess = result.Count(l => l.Status == "✅ نجح");
                int totalFailed = result.Count(l => l.Status == "❌ فشل");
                int totalWarning = result.Count(l => l.Status == "⚠️ تحذير");

                titleLabel.Text = $"📋 السجلات (إجمالي: {totalDisplayed} - نجاح: {totalSuccess} | فشل: {totalFailed} | تحذير: {totalWarning})";

                foreach (var entry in result)
                {
                    int rowIndex = dgvLogs.Rows.Add(
                        entry.Date,
                        entry.Time,
                        entry.JobName,
                        entry.Status,
                        entry.Message
                    );

                    if (entry.Status == "✅ نجح")
                    {
                        dgvLogs.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(16, 185, 129);
                        dgvLogs.Rows[rowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                    else if (entry.Status == "❌ فشل")
                    {
                        dgvLogs.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(220, 38, 38);
                        dgvLogs.Rows[rowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                    else if (entry.Status == "⚠️ تحذير")
                    {
                        dgvLogs.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(245, 158, 11);
                        dgvLogs.Rows[rowIndex].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الفلتر: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 📦 كلاس السجلات

        private class LogEntry
        {
            public string Date { get; set; }
            public string Time { get; set; }
            public string JobName { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
        }

        #endregion

        #region 🧹 التخلص من الموارد

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer?.Stop();
                refreshTimer?.Dispose();

                panelHeader?.Dispose();
                panelTopStats?.Dispose();
                panelFilters?.Dispose();
                panelButtons?.Dispose();
                tableLayoutStats?.Dispose();
                dgvJobs?.Dispose();
                txtSearch?.Dispose();
                btnSearch?.Dispose();
                btnApplyFilter?.Dispose();
                btnResetFilter?.Dispose();
                btnRefresh?.Dispose();
                btnClose?.Dispose();
                btnViewLogs?.Dispose();
                btnCharts?.Dispose();
                cmbFilter?.Dispose();
                dtpFrom?.Dispose();
                dtpTo?.Dispose();
                lblTitle?.Dispose();
                lblSubtitle?.Dispose();
                lblRecentJobs?.Dispose();
                lblLastUpdate?.Dispose();
                lblFilter?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}