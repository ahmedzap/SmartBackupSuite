using SmartBackupSuite.Helpers;
using SmartBackupSuite.Models;
using SmartBackupSuite.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SmartBackupSuite.Forms
{
    public partial class ChartsForm : Form
    {
        #region 📋 المتغيرات

        private AppSettings _settings;
        private System.Windows.Forms.Timer refreshTimer;
        private Panel panelCharts;
        private TableLayoutPanel chartsLayout;
        private ComboBox cmbFilterType;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnRefresh, btnClose, btnExport;

        // المخططات
        private Chart chartPie, chartBar, chartLine, chartJobPerformance;
        private Label lblLastUpdate;

        #endregion

        #region 🚀 المُنشئ

        public ChartsForm()
        {
            InitializeComponent();
            LoadSettings();
            LoadCharts();
            StartAutoRefresh();
        }

        #endregion

        #region 🎨 تهيئة الواجهة

        private void InitializeComponent()
        {
            // ============================================================
            // إعدادات النموذج الرئيسي
            // ============================================================
            this.Text = "📊 المخططات التفصيلية - Smart Backup Suite";
            this.Size = new Size(1400, 850);
            this.MinimumSize = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.RightToLeft = RightToLeft.Yes;
            this.WindowState = FormWindowState.Maximized;

            // ============================================================
            // 1. Header (الرأس)
            // ============================================================
            var panelHeader = new Panel();
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 80;
            panelHeader.BackColor = Color.FromArgb(30, 41, 59);
            panelHeader.Padding = new Padding(20, 10, 20, 10);

            var lblTitle = new Label();
            lblTitle.Text = "📊 المخططات التفصيلية";
            lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(241, 245, 249);
            lblTitle.Location = new Point(20, 10);
            lblTitle.Size = new Size(400, 35);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            var lblSubtitle = new Label();
            lblSubtitle.Text = "عرض تفصيلي لأداء المهام والنسخ الاحتياطي مع تحليلات متقدمة";
            lblSubtitle.Font = new Font("Segoe UI", 10F);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblSubtitle.Location = new Point(20, 45);
            lblSubtitle.Size = new Size(500, 25);
            lblSubtitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblLastUpdate = new Label();
            this.lblLastUpdate.Text = "🔄 آخر تحديث: --:--";
            this.lblLastUpdate.Font = new Font("Segoe UI", 9F);
            this.lblLastUpdate.ForeColor = Color.FromArgb(148, 163, 184);
            this.lblLastUpdate.Location = new Point(20, 70);
            this.lblLastUpdate.Size = new Size(200, 20);

            var panelControls = new Panel();
            panelControls.Location = new Point(650, 10);
            panelControls.Size = new Size(720, 60);
            panelControls.BackColor = Color.Transparent;

            var lblFilter = new Label();
            lblFilter.Text = "📊 التصنيف:";
            lblFilter.Font = new Font("Segoe UI", 9F);
            lblFilter.ForeColor = Color.FromArgb(148, 163, 184);
            lblFilter.Location = new Point(0, 5);
            lblFilter.Size = new Size(70, 25);
            lblFilter.TextAlign = ContentAlignment.MiddleRight;

            this.cmbFilterType = new ComboBox();
            this.cmbFilterType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbFilterType.Size = new Size(120, 25);
            this.cmbFilterType.Font = new Font("Segoe UI", 9F);
            this.cmbFilterType.FlatStyle = FlatStyle.Flat;
            this.cmbFilterType.BackColor = Color.White;
            this.cmbFilterType.Location = new Point(75, 3);
            this.cmbFilterType.Items.AddRange(new object[] { "الكل", "✅ نجح", "❌ فشل", "⏳ قيد الانتظار" });
            this.cmbFilterType.SelectedIndex = 0;
            this.cmbFilterType.SelectedIndexChanged += (s, e) => LoadCharts();

            var lblFrom = new Label();
            lblFrom.Text = "📅 من:";
            lblFrom.Font = new Font("Segoe UI", 9F);
            lblFrom.ForeColor = Color.FromArgb(148, 163, 184);
            lblFrom.Location = new Point(210, 5);
            lblFrom.Size = new Size(35, 25);
            lblFrom.TextAlign = ContentAlignment.MiddleRight;

            this.dtpFrom = new DateTimePicker();
            this.dtpFrom.Format = DateTimePickerFormat.Short;
            this.dtpFrom.Size = new Size(100, 25);
            this.dtpFrom.Font = new Font("Segoe UI", 9F);
            this.dtpFrom.Location = new Point(250, 3);
            this.dtpFrom.Value = DateTime.Now.AddDays(-30);
            this.dtpFrom.CalendarTitleBackColor = Color.FromArgb(30, 41, 59);
            this.dtpFrom.ValueChanged += (s, e) => LoadCharts();

            var lblTo = new Label();
            lblTo.Text = "إلى:";
            lblTo.Font = new Font("Segoe UI", 9F);
            lblTo.ForeColor = Color.FromArgb(148, 163, 184);
            lblTo.Location = new Point(360, 5);
            lblTo.Size = new Size(30, 25);
            lblTo.TextAlign = ContentAlignment.MiddleRight;

            this.dtpTo = new DateTimePicker();
            this.dtpTo.Format = DateTimePickerFormat.Short;
            this.dtpTo.Size = new Size(100, 25);
            this.dtpTo.Font = new Font("Segoe UI", 9F);
            this.dtpTo.Location = new Point(395, 3);
            this.dtpTo.Value = DateTime.Now;
            this.dtpTo.CalendarTitleBackColor = Color.FromArgb(30, 41, 59);
            this.dtpTo.ValueChanged += (s, e) => LoadCharts();

            this.btnRefresh = new Button();
            this.btnRefresh.Text = "🔄 تحديث";
            this.btnRefresh.Size = new Size(90, 28);
            this.btnRefresh.Location = new Point(510, 2);
            this.btnRefresh.BackColor = Color.FromArgb(37, 99, 235);
            this.btnRefresh.ForeColor = Color.White;
            this.btnRefresh.FlatStyle = FlatStyle.Flat;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.Cursor = Cursors.Hand;
            this.btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnRefresh.Click += (s, e) => LoadCharts();

            this.btnExport = new Button();
            this.btnExport.Text = "💾 تصدير";
            this.btnExport.Size = new Size(90, 28);
            this.btnExport.Location = new Point(605, 2);
            this.btnExport.BackColor = Color.FromArgb(16, 185, 129);
            this.btnExport.ForeColor = Color.White;
            this.btnExport.FlatStyle = FlatStyle.Flat;
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.Cursor = Cursors.Hand;
            this.btnExport.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnExport.Click += (s, e) => BtnExport_Click();

            var btnBack = new Button();
            btnBack.Text = "🔙 العودة";
            btnBack.Size = new Size(100, 28);
            btnBack.Location = new Point(700, 2);
            btnBack.BackColor = Color.FromArgb(220, 38, 38);
            btnBack.ForeColor = Color.White;
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Cursor = Cursors.Hand;
            btnBack.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnBack.Click += (s, e) => this.Close();

            panelControls.Controls.Add(lblFilter);
            panelControls.Controls.Add(this.cmbFilterType);
            panelControls.Controls.Add(lblFrom);
            panelControls.Controls.Add(this.dtpFrom);
            panelControls.Controls.Add(lblTo);
            panelControls.Controls.Add(this.dtpTo);
            panelControls.Controls.Add(this.btnRefresh);
            panelControls.Controls.Add(this.btnExport);
            panelControls.Controls.Add(btnBack);

            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSubtitle);
            panelHeader.Controls.Add(this.lblLastUpdate);
            panelHeader.Controls.Add(panelControls);

            // ============================================================
            // 2. منطقة المخططات
            // ============================================================
            this.panelCharts = new Panel();
            this.panelCharts.Dock = DockStyle.Fill;
            this.panelCharts.BackColor = Color.FromArgb(245, 247, 250);
            this.panelCharts.Padding = new Padding(15);
            this.panelCharts.AutoScroll = true;

            // ============================================================
            // 3. TableLayoutPanel للمخططات
            // ============================================================
            this.chartsLayout = new TableLayoutPanel();
            this.chartsLayout.AutoSize = true;
            this.chartsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.chartsLayout.ColumnCount = 2;
            this.chartsLayout.RowCount = 2;
            this.chartsLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            this.chartsLayout.BackColor = Color.Transparent;
            this.chartsLayout.Padding = new Padding(8);
            this.chartsLayout.MinimumSize = new Size(1050, 600);

            this.chartsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.chartsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.chartsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.chartsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            this.chartPie = CreateChart("📊 حالة المهام", "نسبة نجاح وفشل المهام", ChartType.Pie, Color.FromArgb(59, 130, 246));
            this.chartBar = CreateChart("📈 أداء المهام", "عدد المهام الناجحة والفاشلة في آخر 7 أيام", ChartType.Bar, Color.FromArgb(245, 158, 11));
            this.chartLine = CreateChart("📈 اتجاه النسخ الاحتياطي", "آخر 20 عملية نسخ احتياطي", ChartType.Line, Color.FromArgb(16, 185, 129));
            this.chartJobPerformance = CreateChart("📊 أداء كل مهمة", "عدد مرات النجاح والفشل لكل مهمة", ChartType.Column, Color.FromArgb(139, 92, 246));

            this.chartsLayout.Controls.Add(this.chartPie, 0, 0);
            this.chartsLayout.Controls.Add(this.chartBar, 1, 0);
            this.chartsLayout.Controls.Add(this.chartLine, 0, 1);
            this.chartsLayout.Controls.Add(this.chartJobPerformance, 1, 1);

            this.panelCharts.Controls.Add(this.chartsLayout);

            this.Controls.Add(this.panelCharts);
            this.Controls.Add(panelHeader);

            this.refreshTimer = new System.Windows.Forms.Timer();
            this.refreshTimer.Interval = 60000;
            this.refreshTimer.Tick += (s, e) => LoadCharts();
        }

        #endregion

        #region 🎨 دوال إنشاء المخططات

        private enum ChartType
        {
            Pie,
            Bar,
            Line,
            Column
        }

        private Chart CreateChart(string title, string subtitle, ChartType chartType, Color accentColor)
        {
            var chart = new Chart();
            chart.Dock = DockStyle.Fill;
            chart.BackColor = Color.White;
            chart.Padding = new Padding(10);
            chart.Margin = new Padding(8);
            chart.MinimumSize = new Size(400, 280);

            chart.Paint += (s, pe) => {
                using (var brush = new SolidBrush(accentColor))
                {
                    pe.Graphics.FillRectangle(brush, 12, 0, chart.Width - 24, 4);
                }
                using (var pen = new Pen(Color.FromArgb(40, 0, 0, 0)))
                {
                    pe.Graphics.DrawLine(pen, 0, chart.Height - 1, chart.Width, chart.Height - 1);
                }
            };

            var area = new ChartArea();
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 9);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 9);
            area.AxisY.Minimum = 0;
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisX.LabelStyle.ForeColor = Color.FromArgb(100, 116, 139);
            area.AxisY.LabelStyle.ForeColor = Color.FromArgb(100, 116, 139);
            area.InnerPlotPosition.Auto = true;
            area.Position.Auto = true;

            area.BackColor = Color.White;
            area.BorderColor = Color.FromArgb(226, 232, 240);
            area.BorderDashStyle = ChartDashStyle.Solid;
            chart.ChartAreas.Add(area);

            var legend = new Legend();
            legend.Font = new Font("Segoe UI", 9);
            legend.Docking = Docking.Bottom;
            legend.BackColor = Color.Transparent;
            legend.ForeColor = Color.FromArgb(51, 65, 85);
            chart.Legends.Add(legend);

            var titleObj = new Title(title);
            titleObj.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            titleObj.ForeColor = Color.FromArgb(15, 23, 42);
            titleObj.Docking = Docking.Top;
            titleObj.Alignment = ContentAlignment.MiddleLeft;
            chart.Titles.Add(titleObj);

            var subtitleObj = new Title(subtitle);
            subtitleObj.Font = new Font("Segoe UI", 9F);
            subtitleObj.ForeColor = Color.FromArgb(100, 116, 139);
            subtitleObj.Docking = Docking.Top;
            subtitleObj.Alignment = ContentAlignment.MiddleLeft;
            chart.Titles.Add(subtitleObj);

            return chart;
        }

        #endregion

        #region 📊 دوال تحميل البيانات

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

        private void LoadCharts()
        {
            try
            {
                _settings = JsonFileHelper.ReadSettings();
                if (_settings.BackupJobs == null) _settings.BackupJobs = new List<BackupJob>();

                var jobs = FilterJobs(_settings.BackupJobs);

                UpdatePieChart(jobs);
                UpdateBarChart(jobs);
                UpdateLineChart(jobs);
                UpdateJobPerformanceChart(jobs);

                lblLastUpdate.Text = $"🔄 آخر تحديث: {DateTime.Now:HH:mm:ss}";

                btnRefresh.Text = "✅ محدث";
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 2000;
                timer.Tick += (s, e) => { btnRefresh.Text = "🔄 تحديث"; timer.Stop(); };
                timer.Start();
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ChartsForm", "Error", $"خطأ في تحميل المخططات: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل المخططات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<BackupJob> FilterJobs(List<BackupJob> jobs)
        {
            var filtered = jobs.AsEnumerable();

            string filterType = cmbFilterType.SelectedItem?.ToString() ?? "الكل";
            if (filterType != "الكل")
            {
                string status = filterType switch
                {
                    "✅ نجح" => "Success",
                    "❌ فشل" => "Failed",
                    "⏳ قيد الانتظار" => "",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(status))
                    filtered = filtered.Where(j => j.LastRunStatus == status);
                else
                    filtered = filtered.Where(j => string.IsNullOrEmpty(j.LastRunStatus));
            }

            string fromDate = dtpFrom.Value.ToString("yyyy-MM-dd");
            string toDate = dtpTo.Value.ToString("yyyy-MM-dd");
            filtered = filtered.Where(j =>
                j.LastRunTime >= DateTime.Parse(fromDate) &&
                j.LastRunTime <= DateTime.Parse(toDate).AddDays(1)
            );

            return filtered.ToList();
        }

        #endregion

        #region 📈 تحديث المخططات

        private void UpdatePieChart(List<BackupJob> jobs)
        {
            chartPie.Series.Clear();

            int success = jobs.Count(j => j.LastRunStatus == "Success");
            int failed = jobs.Count(j => j.LastRunStatus == "Failed");
            int pending = jobs.Count(j => string.IsNullOrEmpty(j.LastRunStatus));

            var series = new Series("المهام")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                Label = "#PERCENT{P0}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                LabelForeColor = Color.White
            };

            if (success > 0)
            {
                series.Points.AddXY("✅ نجحت", success);
                series.Points[series.Points.Count - 1].Color = Color.FromArgb(16, 185, 129);
                series.Points[series.Points.Count - 1].LabelToolTip = $"عدد المهام الناجحة: {success}";
                series.Points[series.Points.Count - 1].BorderColor = Color.White;
                series.Points[series.Points.Count - 1].BorderWidth = 2;
            }
            if (failed > 0)
            {
                series.Points.AddXY("❌ فشلت", failed);
                series.Points[series.Points.Count - 1].Color = Color.FromArgb(220, 38, 38);
                series.Points[series.Points.Count - 1].LabelToolTip = $"عدد المهام الفاشلة: {failed}";
                series.Points[series.Points.Count - 1].BorderColor = Color.White;
                series.Points[series.Points.Count - 1].BorderWidth = 2;
            }
            if (pending > 0)
            {
                series.Points.AddXY("⏳ قيد الانتظار", pending);
                series.Points[series.Points.Count - 1].Color = Color.FromArgb(245, 158, 11);
                series.Points[series.Points.Count - 1].LabelToolTip = $"عدد المهام قيد الانتظار: {pending}";
                series.Points[series.Points.Count - 1].BorderColor = Color.White;
                series.Points[series.Points.Count - 1].BorderWidth = 2;
            }

            if (success == 0 && failed == 0 && pending == 0)
            {
                series.Points.AddXY("لا توجد بيانات", 1);
                series.Points[0].Color = Color.FromArgb(203, 213, 225);
                series.Points[0].BorderColor = Color.White;
                series.Points[0].BorderWidth = 2;
            }

            var total = success + failed + pending;
            chartPie.Titles.Add(new Title($"إجمالي: {total}", Docking.Top, new Font("Segoe UI", 11F, FontStyle.Bold), Color.FromArgb(15, 23, 42)));

            chartPie.Series.Add(series);
            chartPie.Invalidate();
        }

        private void UpdateBarChart(List<BackupJob> jobs)
        {
            chartBar.Series.Clear();
            chartBar.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 9);

            var successSeries = new Series("✅ نجح")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                Label = "#VAL",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Color = Color.FromArgb(16, 185, 129)
            };

            var failedSeries = new Series("❌ فشل")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                Label = "#VAL",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Color = Color.FromArgb(220, 38, 38)
            };

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-i).Date)
                .Reverse()
                .ToList();

            foreach (var day in last7Days)
            {
                string dayLabel = day.ToString("dd/MM");
                int successCount = jobs.Count(j => j.LastRunTime.Date == day && j.LastRunStatus == "Success");
                int failedCount = jobs.Count(j => j.LastRunTime.Date == day && j.LastRunStatus == "Failed");

                successSeries.Points.AddXY(dayLabel, successCount);
                failedSeries.Points.AddXY(dayLabel, failedCount);
            }

            chartBar.Series.Add(successSeries);
            chartBar.Series.Add(failedSeries);
            chartBar.Invalidate();
        }

        private void UpdateLineChart(List<BackupJob> jobs)
        {
            chartLine.Series.Clear();

            var successSeries = new Series("✅ نجح")
            {
                ChartType = SeriesChartType.Line,
                IsValueShownAsLabel = true,
                Label = "#VAL",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BorderWidth = 3,
                Color = Color.FromArgb(16, 185, 129),
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8,
                MarkerColor = Color.FromArgb(16, 185, 129)
            };

            var failedSeries = new Series("❌ فشل")
            {
                ChartType = SeriesChartType.Line,
                IsValueShownAsLabel = true,
                Label = "#VAL",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BorderWidth = 3,
                Color = Color.FromArgb(220, 38, 38),
                MarkerStyle = MarkerStyle.Diamond,
                MarkerSize = 8,
                MarkerColor = Color.FromArgb(220, 38, 38)
            };

            var recentJobs = jobs
                .Where(j => j.LastRunTime > DateTime.MinValue)
                .OrderBy(j => j.LastRunTime)
                .Take(20)
                .ToList();

            if (recentJobs.Count > 0)
            {
                foreach (var job in recentJobs)
                {
                    string timeLabel = job.LastRunTime.ToString("HH:mm");
                    if (job.LastRunStatus == "Success")
                        successSeries.Points.AddXY(timeLabel, 1);
                    else if (job.LastRunStatus == "Failed")
                        failedSeries.Points.AddXY(timeLabel, 1);
                    else
                    {
                        successSeries.Points.AddXY(timeLabel, 0);
                        failedSeries.Points.AddXY(timeLabel, 0);
                    }
                }
            }
            else
            {
                successSeries.Points.AddXY("لا توجد بيانات", 0);
                failedSeries.Points.AddXY("لا توجد بيانات", 0);
            }

            if (successSeries.Points.Count > 0)
                chartLine.Series.Add(successSeries);
            if (failedSeries.Points.Count > 0)
                chartLine.Series.Add(failedSeries);
            chartLine.Invalidate();
        }

        private void UpdateJobPerformanceChart(List<BackupJob> jobs)
        {
            chartJobPerformance.Series.Clear();
            chartJobPerformance.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 9);
            chartJobPerformance.ChartAreas[0].AxisX.Interval = 1;

            var successSeries = new Series("✅ نجح")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                Label = "#VAL",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Color = Color.FromArgb(16, 185, 129)
            };

            var failedSeries = new Series("❌ فشل")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                Label = "#VAL",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Color = Color.FromArgb(220, 38, 38)
            };

            // ✅ قراءة السجلات من مجلد Logs
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmartBackupSuite",
                "Logs"
            );

            if (Directory.Exists(logPath))
            {
                try
                {
                    var logFiles = Directory.GetFiles(logPath, "*.log");
                    var allLines = new List<string>();

                    foreach (var file in logFiles)
                    {
                        var lines = File.ReadAllLines(file);
                        allLines.AddRange(lines);
                    }

                    foreach (var job in jobs.Where(j => !string.IsNullOrEmpty(j.JobName)))
                    {
                        int successCount = 0;
                        int failedCount = 0;

                        foreach (var line in allLines)
                        {
                            if (line.Contains(job.JobName))
                            {
                                if (line.Contains("Success") || line.Contains("✅") || line.Contains("[SUCCESS]"))
                                    successCount++;
                                else if (line.Contains("Failed") || line.Contains("❌") || line.Contains("[ERROR]") || line.Contains("Error"))
                                    failedCount++;
                            }
                        }

                        string jobName = job.JobName.Length > 12 ? job.JobName.Substring(0, 12) + "..." : job.JobName;

                        successSeries.Points.AddXY(jobName, successCount);
                        failedSeries.Points.AddXY(jobName, failedCount);
                    }
                }
                catch { }
            }

            if (successSeries.Points.Count > 0)
                chartJobPerformance.Series.Add(successSeries);
            if (failedSeries.Points.Count > 0)
                chartJobPerformance.Series.Add(failedSeries);

            if (successSeries.Points.Count == 0 && failedSeries.Points.Count == 0)
            {
                var emptySeries = new Series("لا توجد بيانات")
                {
                    ChartType = SeriesChartType.Column,
                    Color = Color.FromArgb(203, 213, 225)
                };
                emptySeries.Points.AddXY("لا توجد مهام", 1);
                chartJobPerformance.Series.Add(emptySeries);
            }

            chartJobPerformance.Invalidate();
        }

        #endregion

        #region 💾 تصدير المخططات

        private void BtnExport_Click()
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All files (*.*)|*.*";
                    saveFileDialog.Title = "تصدير المخططات";
                    saveFileDialog.FileName = $"Charts_{DateTime.Now:yyyyMMdd_HHmmss}";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using (Bitmap bitmap = new Bitmap(panelCharts.Width, panelCharts.Height))
                        {
                            panelCharts.DrawToBitmap(bitmap, new Rectangle(0, 0, panelCharts.Width, panelCharts.Height));
                            bitmap.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        MessageBox.Show($"✅ تم تصدير المخططات بنجاح!\n{saveFileDialog.FileName}",
                            "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في تصدير المخططات: {ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 🧹 التخلص من الموارد

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer?.Stop();
                refreshTimer?.Dispose();

                chartPie?.Dispose();
                chartBar?.Dispose();
                chartLine?.Dispose();
                chartJobPerformance?.Dispose();
                panelCharts?.Dispose();
                chartsLayout?.Dispose();
                cmbFilterType?.Dispose();
                dtpFrom?.Dispose();
                dtpTo?.Dispose();
                btnRefresh?.Dispose();
                btnExport?.Dispose();
                lblLastUpdate?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}