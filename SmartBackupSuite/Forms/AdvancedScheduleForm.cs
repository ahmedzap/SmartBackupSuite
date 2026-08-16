using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SmartBackupSuite.Models;

namespace SmartBackupSuite.Forms
{
    public partial class AdvancedScheduleForm : Form
    {
        private Schedule _schedule;
        private Dictionary<int, List<string>> _dayTimes = new Dictionary<int, List<string>>();
        private readonly string[] _dayNames = { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" };

        // UI Arrays
        private Panel[] _dayPanels;
        private CheckBox[] _chkDays;
        private FlowLayoutPanel[] _timeFlowPanels;
        private TextBox[] _timeTextBoxes;
        private Button[] _addTimeButtons;
        private Button[] _removeTimeButtons;

        // Theme
        private static readonly Color PrimaryColor = Color.FromArgb(13, 148, 136);
        private static readonly Color PrimaryLight = Color.FromArgb(204, 251, 241);
        private static readonly Color SurfaceColor = Color.FromArgb(248, 250, 252);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Color TextPrimary = Color.FromArgb(30, 41, 59);
        private static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);
        private static readonly Color SuccessColor = Color.FromArgb(46, 204, 113);
        private static readonly Color ErrorColor = Color.FromArgb(239, 68, 68);

        public Schedule ScheduleData => _schedule;

        public AdvancedScheduleForm(Schedule schedule)
        {
            _schedule = schedule ?? new Schedule
            {
                CustomDaysOfWeek = new List<int>(),
                CustomDayTimes = new Dictionary<int, List<string>>()
            };

            // Initialize _dayTimes from existing schedule
            if (_schedule.CustomDaysOfWeek != null)
            {
                foreach (int day in _schedule.CustomDaysOfWeek)
                {
                    var times = _schedule.GetTimesForDay(day);
                    _dayTimes[day] = (times != null && times.Any())
                        ? new List<string>(times)
                        : new List<string> { "08:00" };
                }
            }

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "⏰ الجدولة المتقدمة";
            this.Size = new Size(720, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Padding = new Padding(20);

            // Main Layout
            TableLayoutPanel tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White
            };
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Header
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Content
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));     // Footer

            // Header
            Label lblHeader = new Label
            {
                Text = "⏰ إعداد الجدولة المخصصة",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            };

            Label lblSubHeader = new Label
            {
                Text = "اختر الأيام وأضف الأوقات المطلوبة لكل يوم",
                Font = new Font("Segoe UI", 10F),
                ForeColor = TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Fill, Height = 50, BackColor = Color.White };
            pnlHeader.Controls.Add(lblSubHeader);
            pnlHeader.Controls.Add(lblHeader);
            lblHeader.Location = new Point(0, 0);
            lblHeader.Size = new Size(pnlHeader.Width, 30);
            lblSubHeader.Location = new Point(0, 32);
            lblSubHeader.Size = new Size(pnlHeader.Width, 20);

            // Days Content
            Panel pnlDays = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0, 5, 0, 5)
            };

            FlowLayoutPanel flpDays = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                WrapContents = false
            };

            _dayPanels = new Panel[7];
            _chkDays = new CheckBox[7];
            _timeFlowPanels = new FlowLayoutPanel[7];
            _timeTextBoxes = new TextBox[7];
            _addTimeButtons = new Button[7];
            _removeTimeButtons = new Button[7];

            for (int i = 0; i < 7; i++)
            {
                Panel dayPanel = CreateDayPanel(i);
                _dayPanels[i] = dayPanel;
                flpDays.Controls.Add(dayPanel);
            }

            pnlDays.Controls.Add(flpDays);

            // Footer Buttons
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0, 10, 0, 0)
            };

            Button btnOK = new Button
            {
                Text = "✅ حفظ الجدولة",
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                RightToLeft = RightToLeft.Yes
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.FlatAppearance.MouseOverBackColor = Color.FromArgb(15, 118, 110);
            btnOK.Click += BtnOK_Click;

            Button btnCancel = new Button
            {
                Text = "❌ إلغاء",
                Size = new Size(120, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0),
                RightToLeft = RightToLeft.Yes
            };
            btnCancel.FlatAppearance.BorderColor = BorderColor;
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            FlowLayoutPanel flpFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.Transparent,
                WrapContents = false
            };
            flpFooter.Controls.Add(btnCancel);
            flpFooter.Controls.Add(btnOK);
            pnlFooter.Controls.Add(flpFooter);

            tlpMain.Controls.Add(pnlHeader, 0, 0);
            tlpMain.Controls.Add(pnlDays, 0, 1);
            tlpMain.Controls.Add(pnlFooter, 0, 2);

            this.Controls.Add(tlpMain);

            // Initialize UI state
            LoadScheduleUI();
        }

        private Panel CreateDayPanel(int dayIndex)
        {
            bool isDaySelected = _dayTimes.ContainsKey(dayIndex) && _dayTimes[dayIndex] != null && _dayTimes[dayIndex].Any();

            Panel dayPanel = new Panel
            {
                Width = 640,
                Height = isDaySelected ? 110 : 50,
                BackColor = SurfaceColor,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 4, 0, 4),
                BorderStyle = BorderStyle.None,
                RightToLeft = RightToLeft.Yes
            };

            // Border paint
            dayPanel.Paint += (s, e) =>
            {
                Rectangle rect = new Rectangle(0, 0, dayPanel.Width - 1, dayPanel.Height - 1);
                using (Pen pen = new Pen(BorderColor, 1))
                    e.Graphics.DrawRectangle(pen, rect);
            };

            // Top row: Checkbox + controls
            FlowLayoutPanel flpTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                BackColor = Color.Transparent,
                WrapContents = false,
                Height = 36
            };

            CheckBox chkDay = new CheckBox
            {
                Text = _dayNames[dayIndex],
                Checked = isDaySelected,
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = TextPrimary,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(0, 4, 0, 4),
                Cursor = Cursors.Hand
            };

            TextBox txtTime = new TextBox
            {
                Text = "08:00",
                Width = 70,
                Height = 28,
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Margin = new Padding(8, 2, 4, 2),
                Enabled = isDaySelected
            };

            Button btnAdd = new Button
            {
                Text = "➕",
                Width = 36,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = SuccessColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = isDaySelected,
                Margin = new Padding(4, 2, 0, 2)
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            Button btnRemove = new Button
            {
                Text = "✖",
                Width = 36,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = ErrorColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = isDaySelected && _dayTimes.ContainsKey(dayIndex) && _dayTimes[dayIndex].Count > 1,
                Margin = new Padding(4, 2, 0, 2)
            };
            btnRemove.FlatAppearance.BorderSize = 0;

            flpTop.Controls.Add(chkDay);
            flpTop.Controls.Add(btnAdd);
            flpTop.Controls.Add(txtTime);
            flpTop.Controls.Add(btnRemove);

            // Times flow panel
            FlowLayoutPanel flpTimes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                BackColor = Color.Transparent,
                WrapContents = true,
                Padding = new Padding(0, 4, 0, 0),
                Visible = isDaySelected
            };

            dayPanel.Controls.Add(flpTimes);
            dayPanel.Controls.Add(flpTop);

            // Store references
            _chkDays[dayIndex] = chkDay;
            _timeFlowPanels[dayIndex] = flpTimes;
            _timeTextBoxes[dayIndex] = txtTime;
            _addTimeButtons[dayIndex] = btnAdd;
            _removeTimeButtons[dayIndex] = btnRemove;

            // Events
            int day = dayIndex;
            chkDay.CheckedChanged += (s, e) =>
            {
                if (chkDay.Checked)
                {
                    if (!_dayTimes.ContainsKey(day))
                        _dayTimes[day] = new List<string> { "08:00" };
                }
                else
                {
                    _dayTimes.Remove(day);
                }
                UpdateDayPanelState(day);
            };

            btnAdd.Click += (s, e) => AddTime(day);
            btnRemove.Click += (s, e) => RemoveTime(day);

            return dayPanel;
        }

        private void LoadScheduleUI()
        {
            for (int i = 0; i < 7; i++)
            {
                UpdateDayPanelState(i);
            }
        }

        private void UpdateDayPanelState(int dayIndex)
        {
            var dayPanel = _dayPanels[dayIndex];
            var chkDay = _chkDays[dayIndex];
            var flpTimes = _timeFlowPanels[dayIndex];
            var txtTime = _timeTextBoxes[dayIndex];
            var btnAdd = _addTimeButtons[dayIndex];
            var btnRemove = _removeTimeButtons[dayIndex];

            if (dayPanel == null || chkDay == null) return;

            bool isSelected = chkDay.Checked;

            // Update panel height
            dayPanel.Height = isSelected ? 110 : 50;

            // Update controls visibility/enablement
            flpTimes.Visible = isSelected;
            txtTime.Enabled = isSelected;
            btnAdd.Enabled = isSelected;
            btnRemove.Enabled = isSelected &&
                               _dayTimes.ContainsKey(dayIndex) &&
                               _dayTimes[dayIndex] != null &&
                               _dayTimes[dayIndex].Count > 1;

            // Update times display
            flpTimes.Controls.Clear();

            if (isSelected && _dayTimes.ContainsKey(dayIndex) && _dayTimes[dayIndex] != null)
            {
                foreach (var time in _dayTimes[dayIndex])
                {
                    Button timeTag = new Button
                    {
                        Text = $"⏰ {time}",
                        AutoSize = true,
                        Height = 28,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = PrimaryLight,
                        ForeColor = PrimaryColor,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        Padding = new Padding(8, 2, 8, 2),
                        Margin = new Padding(4, 2, 4, 2),
                        Tag = time
                    };
                    timeTag.FlatAppearance.BorderSize = 0;

                    int day = dayIndex;
                    string timeStr = time;
                    timeTag.Click += (s, e) =>
                    {
                        if (_dayTimes.ContainsKey(day) && _dayTimes[day].Count > 1)
                        {
                            _dayTimes[day].Remove(timeStr);
                            UpdateDayPanelState(day);
                        }
                        else
                        {
                            MessageBox.Show("يجب أن يكون هناك وقت واحد على الأقل!", "تنبيه",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    };

                    flpTimes.Controls.Add(timeTag);
                }
            }
        }

        private void AddTime(int dayIndex)
        {
            if (!_dayTimes.ContainsKey(dayIndex))
                _dayTimes[dayIndex] = new List<string>();

            var txtTime = _timeTextBoxes[dayIndex];
            if (txtTime == null) return;

            string time = txtTime.Text.Trim();

            // Validate time format HH:MM
            if (!System.Text.RegularExpressions.Regex.IsMatch(time, @"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$"))
            {
                MessageBox.Show("صيغة الوقت غير صحيحة. استخدم HH:MM (مثال: 08:00 أو 14:30)",
                    "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTime.Focus();
                return;
            }

            // Normalize to HH:MM format
            if (time.Length == 4 && time[1] == ':')
                time = "0" + time;

            if (_dayTimes[dayIndex].Contains(time))
            {
                MessageBox.Show("هذا الوقت مضاف بالفعل!", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _dayTimes[dayIndex].Add(time);
            _dayTimes[dayIndex].Sort();
            UpdateDayPanelState(dayIndex);
        }

        private void RemoveTime(int dayIndex)
        {
            if (!_dayTimes.ContainsKey(dayIndex) || _dayTimes[dayIndex].Count <= 1)
            {
                MessageBox.Show("يجب أن يكون هناك وقت واحد على الأقل!", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lastTime = _dayTimes[dayIndex].LastOrDefault();
            if (lastTime != null)
            {
                _dayTimes[dayIndex].Remove(lastTime);
                UpdateDayPanelState(dayIndex);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            var selectedDays = new List<int>();
            var dayTimes = new Dictionary<int, List<string>>();

            for (int i = 0; i < 7; i++)
            {
                if (_dayTimes.ContainsKey(i) && _dayTimes[i] != null && _dayTimes[i].Any())
                {
                    selectedDays.Add(i);
                    dayTimes[i] = new List<string>(_dayTimes[i]);
                }
            }

            if (selectedDays.Count == 0)
            {
                MessageBox.Show("يرجى تحديد يوم واحد على الأقل!", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate all times
            var allTimes = new HashSet<string>();
            foreach (var kvp in dayTimes)
            {
                foreach (var time in kvp.Value)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(time, @"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$"))
                    {
                        MessageBox.Show($"صيغة الوقت غير صحيحة: {time} في {_dayNames[kvp.Key]}",
                            "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    allTimes.Add(time);
                }
            }

            _schedule.CustomDaysOfWeek = selectedDays;
            _schedule.CustomDayTimes = dayTimes;
            _schedule.CustomTimes = allTimes.ToList();
            _schedule.CustomExecutionCountPerDay = allTimes.Count > 0 ? allTimes.Count : 1;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}