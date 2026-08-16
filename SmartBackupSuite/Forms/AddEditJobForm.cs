using Microsoft.Data.SqlClient;
using SmartBackupSuite.Helpers;
using SmartBackupSuite.Models;
using SmartBackupSuite.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartBackupSuite.Forms
{
    public partial class AddEditJobForm : Form
    {
        private BackupJob _job;
        private Schedule _schedule;
        private Destination _destination;
        private AppSettings _settings;
        private bool _isEditMode;
        private System.ComponentModel.IContainer components = null;

        private int _currentStep = 1;
        private Panel[] _stepCircles;
        private Label[] _stepLabels;
        private Panel[] _stepLines;
        private Panel[] _stepPanels;

        private bool _isLoadingDatabases = false;
        private List<string> _allDatabases = new List<string>();
        private HashSet<string> _checkedDatabases = new HashSet<string>();

        private Label lblFormTitle;
        private Label lblFormSubtitle;

        private TextBox txtJobName;
        private ComboBox cmbConnection;
        private ComboBox cmbBackupType;
        private NumericUpDown numRetentionCount;
        private CheckBox chkCompression;
        private CheckBox chkEncryption;
        private CheckBox chkActive;
        private CheckBox chkKeepLocalCopy;
        private CheckBox chkExecuteMissed;
        private Label lblConnectionStatus;

        private CheckedListBox chklstDatabases;
        private Button btnLoadDatabases;
        private Button btnSelectAllDB;
        private Button btnDeselectAllDB;
        private TextBox txtSearchDB;
        private Label lblDBCount;
        private Label lblLoadingStatus;

        private ComboBox cmbScheduleType;
        private DateTimePicker dtpStartTime;
        private NumericUpDown numIntervalMinutes;
        private FlowLayoutPanel flpDaysOfWeek;
        private CheckBox[] chkDays;
        private Label lblIntervalUnit;
        private Panel pnlCustomWrapper;
        private Label lblScheduleSummary;
        private Button btnAdvancedSchedule;
        private NumericUpDown numMaxMissed;
        private Panel pnlMissedOptions;
        private Panel pnlDaysWrapper;
        private CheckBox chkCatchUpMissed;

        private FlowLayoutPanel flpDestCards;
        private Panel destCardLocal, destCardNetwork, destCardGoogle, destCardDropbox, destCardOneDrive, destCardS3, destCardAzure;
        private Panel pnlActiveDestFields;
        private Label lblDestFieldTitle;

        private Panel pnlLocalFields, pnlNetworkFields, pnlGoogleFields, pnlS3Fields, pnlAzureFields, pnlCloudAuthFields;
        private TextBox txtLocalPath;
        private Button btnBrowseLocal;
        private TextBox txtNetworkPath;
        private Button btnBrowseNetwork;
        private TextBox txtGoogleDriveFolder;
        private TextBox txtS3Bucket, txtS3AccessKey, txtS3SecretKey, txtS3Region;
        private TextBox txtAzureContainer, txtAzureConnectionString;
        private Button btnShowInstructions;
        private Button btnClearAllTokens;

        private Button btnPrev;
        private Button btnNext;
        private Button btnSave;
        private Button btnCancel;

        private static readonly Color PrimaryColor = Color.FromArgb(13, 148, 136);
        private static readonly Color PrimaryDark = Color.FromArgb(15, 118, 110);
        private static readonly Color PrimaryLight = Color.FromArgb(240, 253, 250);
        private static readonly Color TextPrimary = Color.FromArgb(30, 41, 59);
        private static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Color SurfaceColor = Color.FromArgb(248, 250, 252);
        private static readonly Color ErrorColor = Color.FromArgb(239, 68, 68);
        private static readonly Color SuccessColor = Color.FromArgb(34, 197, 94);
        private static readonly Color StepInactiveColor = Color.FromArgb(226, 232, 240);

        public BackupJob Job => _job;
        public Schedule Schedule => _schedule;
        public Destination Destination => _destination;

        public AddEditJobForm(BackupJob job = null, Schedule schedule = null, Destination destination = null)
        {
            InitializeComponent();
            ApplyModernTheme();

            try
            {
                _settings = JsonFileHelper.ReadSettings() ?? new AppSettings { ServerConnections = new List<ServerConnection>() };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في قراءة الإعدادات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _settings = new AppSettings { ServerConnections = new List<ServerConnection>() };
            }

            _isEditMode = job != null;

            if (_isEditMode)
            {
                _job = job;
                _schedule = schedule ?? new Schedule { JobId = job.Id };
                _destination = destination ?? new Destination { JobId = job.Id };
            }
            else
            {
                _job = new BackupJob();
                _schedule = new Schedule { JobId = _job.Id };
                _destination = new Destination { JobId = _job.Id };
            }

            LoadConnections();
            SetupEventHandlers();
            UpdateScheduleUI();

            this.Text = _isEditMode ? "تعديل مهمة النسخ الاحتياطي" : "إضافة مهمة جديدة";
            lblFormTitle.Text = this.Text;
            ShowStep(1);

            // ✅ الحل: تأجيل تحميل بيانات التعديل إلى حدث Load لضمان إنشاء Window Handle
            if (_isEditMode)
            {
                this.Load += AddEditJobForm_Load;
            }
        }

        private async void AddEditJobForm_Load(object sender, EventArgs e)
        {
            LoadJobData();

            // تحميل قواعد البيانات بعد التأكد من إنشاء الـ Handle
            if (_job.DatabaseNames != null && _job.DatabaseNames.Count > 0)
            {
                try
                {
                    await LoadDatabasesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تحميل قواعد البيانات: {ex.Message}", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ApplyModernTheme()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.ForeColor = TextPrimary;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(920, 750);
            this.MinimumSize = new Size(880, 700);
            this.Padding = new Padding(0);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
        }

        private void InitializeComponent()
        {
            TableLayoutPanel tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(24, 20, 24, 16)
            };
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 115F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));

            Panel pnlHeader = CreateHeaderPanel();
            Panel pnlSteps = CreateStepsPanel();
            Panel cardMain = CreateContentCard();
            Panel pnlFooter = CreateFooterPanel();

            tlpMain.Controls.Add(pnlHeader, 0, 0);
            tlpMain.Controls.Add(pnlSteps, 0, 1);
            tlpMain.Controls.Add(cardMain, 0, 2);
            tlpMain.Controls.Add(pnlFooter, 0, 3);
            this.Controls.Add(tlpMain);
        }

        private Panel CreateHeaderPanel()
        {
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                Height = 75
            };

            lblFormTitle = new Label
            {
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = PrimaryDark,
                AutoSize = true,
                Location = new Point(0, 0),
                RightToLeft = RightToLeft.Yes,
                Height = 45
            };

            lblFormSubtitle = new Label
            {
                Font = new Font("Segoe UI", 11F),
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(0, 40),
                Text = "قم بإعداد مهمة النسخ الاحتياطي بخطوات منظمة وسهلة",
                RightToLeft = RightToLeft.Yes,
                Height = 30
            };

            pnlHeader.Controls.Add(lblFormTitle);
            pnlHeader.Controls.Add(lblFormSubtitle);
            return pnlHeader;
        }

        private Panel CreateStepsPanel()
        {
            Panel pnlSteps = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 16),
                Height = 115,
                BackColor = Color.Transparent
            };

            TableLayoutPanel tlpSteps = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 0, 5)
            };

            for (int i = 0; i < 7; i++)
            {
                if (i % 2 == 0)
                    tlpSteps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 4));
                else
                    tlpSteps.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            }

            _stepCircles = new Panel[4];
            _stepLabels = new Label[4];
            _stepLines = new Panel[3];
            string[] stepNames = { "عام", "قواعد البيانات", "الجدولة", "الوجهة" };
            string[] stepIcons = { "⚙️", "🗄️", "⏰", "☁️" };

            for (int i = 0; i < 4; i++)
            {
                Panel wrapper = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };

                TableLayoutPanel verticalLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0)
                };
                verticalLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
                verticalLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

                Panel circle = new Panel
                {
                    Size = new Size(48, 48),
                    BackColor = i == 0 ? PrimaryColor : Color.White,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.None
                };

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(2, 2, 43, 43);
                    circle.Region = new Region(path);
                }

                Label num = new Label
                {
                    Text = stepIcons[i],
                    ForeColor = i == 0 ? Color.White : TextSecondary,
                    Font = new Font("Segoe UI", 18F),
                    AutoSize = false,
                    Size = new Size(48, 48),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                circle.Controls.Add(num);

                Panel circleContainer = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };
                circleContainer.Controls.Add(circle);

                circle.Location = new Point(
                    (circleContainer.Width - 48) / 2,
                    (circleContainer.Height - 48) / 2
                );

                circleContainer.Resize += (s, e) =>
                {
                    circle.Location = new Point(
                        Math.Max(0, (circleContainer.Width - 48) / 2),
                        Math.Max(0, (circleContainer.Height - 48) / 2)
                    );
                };

                verticalLayout.Controls.Add(circleContainer, 0, 0);

                Label name = new Label
                {
                    Text = stepNames[i],
                    ForeColor = i == 0 ? PrimaryDark : TextSecondary,
                    Font = new Font("Segoe UI", 11F, i == 0 ? FontStyle.Bold : FontStyle.Regular),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                verticalLayout.Controls.Add(name, 0, 1);

                wrapper.Controls.Add(verticalLayout);

                int stepNum = i + 1;
                circle.Click += (s, e) => ShowStep(stepNum);
                num.Click += (s, e) => ShowStep(stepNum);
                name.Click += (s, e) => ShowStep(stepNum);
                wrapper.Click += (s, e) => ShowStep(stepNum);

                _stepCircles[i] = circle;
                _stepLabels[i] = name;
                tlpSteps.Controls.Add(wrapper, i * 2, 0);

                if (i < 3)
                {
                    Panel line = new Panel
                    {
                        Dock = DockStyle.Fill,
                        Height = 4,
                        BackColor = BorderColor,
                        Margin = new Padding(0, 22, 0, 0)
                    };
                    _stepLines[i] = line;
                    tlpSteps.Controls.Add(line, i * 2 + 1, 0);
                }
            }
            pnlSteps.Controls.Add(tlpSteps);
            return pnlSteps;
        }

        private Panel CreateContentCard()
        {
            Panel cardMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(2),
                Margin = new Padding(0, 0, 0, 12)
            };

            cardMain.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var bounds = new Rectangle(0, 0, cardMain.Width - 1, cardMain.Height - 1);
                using (var path = new GraphicsPath())
                {
                    path.AddRoundedRectangle(bounds, 16);
                    using (var pen = new Pen(BorderColor, 1))
                        g.DrawPath(pen, path);
                }
            };

            Panel cardContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24, 20, 24, 20),
                AutoScroll = true
            };

            _stepPanels = new Panel[4];
            _stepPanels[0] = CreateStep1Panel();
            _stepPanels[1] = CreateStep2Panel();
            _stepPanels[2] = CreateStep3Panel();
            _stepPanels[3] = CreateStep4Panel();

            foreach (var p in _stepPanels)
            {
                p.Visible = false;
                p.Dock = DockStyle.Fill;
                cardContent.Controls.Add(p);
            }

            cardMain.Controls.Add(cardContent);
            return cardMain;
        }

        private Panel CreateStep1Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoSize = true };

            TableLayoutPanel tlpGeneral = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 5,
                BackColor = Color.White,
                AutoSize = true
            };
            tlpGeneral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
            tlpGeneral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            tlpGeneral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
            tlpGeneral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            tlpGeneral.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tlpGeneral.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tlpGeneral.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tlpGeneral.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tlpGeneral.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));

            txtJobName = CreateStyledTextBox();
            txtJobName.PlaceholderText = "مثال: نسخ يومي للإنتاج";

            cmbConnection = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Margin = new Padding(4, 8, 4, 8),
                Font = new Font("Segoe UI", 10F)
            };

            cmbBackupType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Margin = new Padding(4, 8, 4, 8),
                Font = new Font("Segoe UI", 10F)
            };
            cmbBackupType.Items.AddRange(new object[] { "Full", "Differential", "Log" });
            cmbBackupType.SelectedIndex = 0;

            numRetentionCount = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 365,
                Value = 7,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(4, 8, 4, 8),
                Font = new Font("Segoe UI", 10F)
            };

            lblConnectionStatus = new Label
            {
                Text = "⚠️ أضف اتصالاً من الإعدادات",
                ForeColor = ErrorColor,
                AutoSize = true,
                Visible = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                Margin = new Padding(4, 8, 4, 8)
            };

            Panel pnlChecks = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Margin = new Padding(0, 8, 0, 0),
                Padding = new Padding(12, 8, 12, 8),
                Height = 48
            };

            FlowLayoutPanel flpChecks = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            chkCompression = CreateStyledCheckBox("ضغط النسخة", true);
            chkEncryption = CreateStyledCheckBox("تشفير النسخة", false);
            chkActive = CreateStyledCheckBox("المهمة نشطة", true);
            chkExecuteMissed = CreateStyledCheckBox("🔄 تنفيذ المهام الفائتة", true);
            chkKeepLocalCopy = CreateStyledCheckBox("💾 الاحتفاظ بنسخة محلية", true);
            chkKeepLocalCopy.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            chkKeepLocalCopy.ForeColor = PrimaryDark;

            flpChecks.Controls.AddRange(new Control[] { chkKeepLocalCopy, chkExecuteMissed, chkActive, chkEncryption, chkCompression });
            pnlChecks.Controls.Add(flpChecks);

            tlpGeneral.Controls.Add(CreateStyledLabel("اسم المهمة *", true), 2, 0);
            tlpGeneral.Controls.Add(txtJobName, 3, 0);
            tlpGeneral.Controls.Add(CreateStyledLabel("الاتصال *", true), 0, 0);
            tlpGeneral.Controls.Add(cmbConnection, 1, 0);
            tlpGeneral.Controls.Add(CreateStyledLabel("نوع النسخ"), 2, 1);
            tlpGeneral.Controls.Add(cmbBackupType, 3, 1);
            tlpGeneral.Controls.Add(CreateStyledLabel("عدد النسخ المحتفظ بها"), 0, 1);
            tlpGeneral.Controls.Add(numRetentionCount, 1, 1);
            tlpGeneral.Controls.Add(lblConnectionStatus, 1, 2);
            tlpGeneral.SetColumnSpan(pnlChecks, 4);
            tlpGeneral.Controls.Add(pnlChecks, 0, 3);

            panel.Controls.Add(tlpGeneral);
            return panel;
        }

        private Panel CreateStep2Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoSize = true };

            FlowLayoutPanel flpTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 8),
                WrapContents = false,
                Height = 48
            };

            btnLoadDatabases = CreatePrimaryButton("🔄 تحميل القواعد");
            btnLoadDatabases.AutoSize = true;
            btnLoadDatabases.Height = 36;
            btnLoadDatabases.Width = 160;
            btnLoadDatabases.Margin = new Padding(0, 0, 0, 0);

            btnSelectAllDB = CreateSecondaryButton("☑️ تحديد الكل");
            btnSelectAllDB.AutoSize = true;
            btnSelectAllDB.Height = 36;
            btnSelectAllDB.Margin = new Padding(8, 0, 8, 0);

            btnDeselectAllDB = CreateSecondaryButton("⬜ إلغاء التحديد");
            btnDeselectAllDB.AutoSize = true;
            btnDeselectAllDB.Height = 36;
            btnDeselectAllDB.Margin = new Padding(0, 0, 8, 0);

            txtSearchDB = new TextBox
            {
                Width = 250,
                Height = 32,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(0, 2, 8, 0)
            };
            txtSearchDB.PlaceholderText = "🔍 بحث في قواعد البيانات...";

            flpTop.Controls.Add(btnLoadDatabases);
            flpTop.Controls.Add(btnSelectAllDB);
            flpTop.Controls.Add(btnDeselectAllDB);
            flpTop.Controls.Add(txtSearchDB);

            chklstDatabases = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true,
                IntegralHeight = false,
                RightToLeft = RightToLeft.Yes,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0)
            };

            lblLoadingStatus = new Label
            {
                Text = "",
                ForeColor = TextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                RightToLeft = RightToLeft.Yes,
                Visible = false,
                Dock = DockStyle.Bottom,
                Height = 24
            };

            lblDBCount = new Label
            {
                Text = "0 قاعدة محددة",
                ForeColor = TextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                RightToLeft = RightToLeft.Yes,
                Dock = DockStyle.Bottom,
                Height = 24
            };

            panel.Controls.Add(chklstDatabases);
            panel.Controls.Add(lblDBCount);
            panel.Controls.Add(lblLoadingStatus);
            panel.Controls.Add(flpTop);
            return panel;
        }

        private Panel CreateStep3Panel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            FlowLayoutPanel flpMain = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                BackColor = Color.White,
                WrapContents = false,
                Width = panel.Width - 32
            };

            FlowLayoutPanel flpRow1 = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12),
                Width = flpMain.Width
            };

            cmbScheduleType = new ComboBox
            {
                Width = 180,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 8, 0)
            };
            cmbScheduleType.Items.AddRange(new object[] { "مرة واحدة", "يومي", "أسبوعي", "شهري", "فترة زمنية", "مخصص" });
            cmbScheduleType.SelectedIndex = 1;

            dtpStartTime = new DateTimePicker
            {
                Width = 140,
                Height = 32,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = new DateTime(2024, 1, 1, 2, 0, 0),
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 8, 0)
            };

            flpRow1.Controls.Add(CreateStyledLabel("وقت البدء:"));
            flpRow1.Controls.Add(dtpStartTime);
            flpRow1.Controls.Add(CreateStyledLabel("نوع الجدولة:"));
            flpRow1.Controls.Add(cmbScheduleType);

            FlowLayoutPanel flpInterval = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12),
                Visible = false
            };
            flpInterval.Tag = "IntervalRow";

            numIntervalMinutes = new NumericUpDown
            {
                Width = 100,
                Height = 32,
                Minimum = 1,
                Maximum = 1440,
                Value = 60,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 8, 0)
            };

            lblIntervalUnit = new Label
            {
                Text = "دقيقة",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 6, 8, 0)
            };

            flpInterval.Controls.Add(lblIntervalUnit);
            flpInterval.Controls.Add(numIntervalMinutes);
            flpInterval.Controls.Add(CreateStyledLabel("الفترة الزمنية:"));

            pnlMissedOptions = new Panel
            {
                AutoSize = true,
                BackColor = SurfaceColor,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 12),
                Visible = false,
                BorderStyle = BorderStyle.None,
                Width = flpMain.Width - 16
            };

            FlowLayoutPanel flpMissed = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                BackColor = Color.Transparent,
                WrapContents = true
            };

            chkCatchUpMissed = new CheckBox
            {
                Text = "🔄 تنفيذ المهام الفائتة عند تشغيل الجهاز",
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = TextPrimary,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(8, 4, 4, 4)
            };

            Label lblMaxMissed = new Label
            {
                Text = "الحد الأقصى للفوات:",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(8, 6, 16, 0)
            };

            numMaxMissed = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = 5,
                Width = 60,
                Height = 28,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 2, 4, 0)
            };

            flpMissed.Controls.Add(chkCatchUpMissed);
            flpMissed.Controls.Add(lblMaxMissed);
            flpMissed.Controls.Add(numMaxMissed);
            pnlMissedOptions.Controls.Add(flpMissed);

            pnlDaysWrapper = new Panel
            {
                AutoSize = true,
                BackColor = SurfaceColor,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 12),
                Visible = false,
                BorderStyle = BorderStyle.None,
                Width = flpMain.Width - 16
            };

            flpDaysOfWeek = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                BackColor = Color.Transparent,
                WrapContents = true
            };

            string[] dayNames = { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" };
            chkDays = new CheckBox[7];
            for (int i = 0; i < 7; i++)
            {
                chkDays[i] = new CheckBox
                {
                    Text = dayNames[i],
                    AutoSize = true,
                    Margin = new Padding(6, 4, 6, 4),
                    Checked = i == 0,
                    RightToLeft = RightToLeft.Yes,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = TextPrimary
                };
                flpDaysOfWeek.Controls.Add(chkDays[i]);
            }
            pnlDaysWrapper.Controls.Add(flpDaysOfWeek);

            pnlCustomWrapper = new Panel
            {
                AutoSize = true,
                BackColor = SurfaceColor,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 12),
                Visible = false,
                BorderStyle = BorderStyle.None,
                Width = flpMain.Width - 16
            };

            btnAdvancedSchedule = new Button
            {
                Text = "⚡ فتح الجدولة المتقدمة (أيام وأوقات مختلفة)",
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Height = 40,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(12, 0, 12, 0)
            };
            btnAdvancedSchedule.FlatAppearance.BorderSize = 0;
            btnAdvancedSchedule.FlatAppearance.MouseOverBackColor = Color.FromArgb(142, 68, 173);
            btnAdvancedSchedule.FlatAppearance.MouseDownBackColor = Color.FromArgb(106, 27, 154);
            btnAdvancedSchedule.Click += BtnAdvancedSchedule_Click;

            lblScheduleSummary = new Label
            {
                Text = "📋 لم يتم تحديد جدولة مخصصة بعد",
                AutoSize = true,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                Margin = new Padding(0, 4, 0, 0),
                RightToLeft = RightToLeft.Yes,
                MaximumSize = new Size(pnlCustomWrapper.Width - 24, 0)
            };

            pnlCustomWrapper.Controls.Add(lblScheduleSummary);
            pnlCustomWrapper.Controls.Add(btnAdvancedSchedule);

            flpMain.Controls.Add(flpRow1);
            flpMain.Controls.Add(flpInterval);
            flpMain.Controls.Add(pnlMissedOptions);
            flpMain.Controls.Add(pnlDaysWrapper);
            flpMain.Controls.Add(pnlCustomWrapper);

            panel.Controls.Add(flpMain);
            return panel;
        }

        private Panel CreateStep4Panel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoSize = true };

            TableLayoutPanel tlpDest = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White,
                AutoSize = true
            };
            tlpDest.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpDest.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpDest.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            flpDestCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(4)
            };

            destCardLocal = CreateDestCard("💻", "محلي", "حفظ على هذا الجهاز", true);
            destCardNetwork = CreateDestCard("🌐", "شبكي", "مسار شبكة مشترك");
            destCardGoogle = CreateDestCard("☁️", "Google Drive", "مصادقة OAuth تلقائية");
            destCardDropbox = CreateDestCard("📦", "Dropbox", "مصادقة OAuth تلقائية");
            destCardOneDrive = CreateDestCard("🔄", "OneDrive", "مصادقة OAuth تلقائية");
            destCardS3 = CreateDestCard("🪣", "AWS S3", "مفاتيح الوصول");
            destCardAzure = CreateDestCard("🔷", "Azure Blob", "Connection String");
            flpDestCards.Controls.AddRange(new Control[] { destCardLocal, destCardNetwork, destCardGoogle, destCardDropbox, destCardOneDrive, destCardS3, destCardAzure });

            pnlActiveDestFields = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = SurfaceColor,
                Padding = new Padding(20),
                Margin = new Padding(0, 4, 0, 12),
                AutoSize = true,
                Visible = true
            };

            lblDestFieldTitle = new Label
            {
                Text = "إعدادات الوجهة:",
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = PrimaryDark,
                Margin = new Padding(0, 0, 0, 12),
                Dock = DockStyle.Top,
                RightToLeft = RightToLeft.Yes,
                Height = 28
            };
            pnlActiveDestFields.Controls.Add(lblDestFieldTitle);

            pnlLocalFields = CreateLocalFieldsPanel();
            pnlNetworkFields = CreateNetworkFieldsPanel();
            pnlGoogleFields = CreateGoogleFieldsPanel();
            pnlS3Fields = CreateS3FieldsPanel();
            pnlAzureFields = CreateAzureFieldsPanel();
            pnlCloudAuthFields = CreateCloudAuthPanel();

            pnlActiveDestFields.Controls.Add(pnlLocalFields);
            pnlActiveDestFields.Controls.Add(pnlNetworkFields);
            pnlActiveDestFields.Controls.Add(pnlGoogleFields);
            pnlActiveDestFields.Controls.Add(pnlS3Fields);
            pnlActiveDestFields.Controls.Add(pnlAzureFields);
            pnlActiveDestFields.Controls.Add(pnlCloudAuthFields);

            FlowLayoutPanel flpDestActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0)
            };

            btnShowInstructions = CreateSecondaryButton("📖 تعليمات المصادقة");
            btnShowInstructions.Height = 36;

            btnClearAllTokens = new Button
            {
                Text = "🗑️ إعادة المصادقة (الكل)",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(185, 28, 28),
                Height = 36,
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(4),
                RightToLeft = RightToLeft.Yes,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(12, 0, 12, 0)
            };
            btnClearAllTokens.FlatAppearance.BorderSize = 0;

            flpDestActions.Controls.Add(btnClearAllTokens);
            flpDestActions.Controls.Add(btnShowInstructions);

            tlpDest.Controls.Add(flpDestCards, 0, 0);
            tlpDest.Controls.Add(pnlActiveDestFields, 0, 1);
            tlpDest.Controls.Add(flpDestActions, 0, 2);

            panel.Controls.Add(tlpDest);
            return panel;
        }

        private Panel CreateLocalFieldsPanel()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, BackColor = Color.Transparent, AutoSize = true, Visible = true };
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            txtLocalPath = CreateStyledTextBox();
            txtLocalPath.Text = "Backups";

            btnBrowseLocal = CreateSecondaryButton("📁 استعراض...");
            btnBrowseLocal.Dock = DockStyle.Fill;
            btnBrowseLocal.Margin = new Padding(8, 4, 0, 4);

            tlp.Controls.Add(CreateStyledLabel("المسار المحلي:"), 0, 0);
            tlp.Controls.Add(txtLocalPath, 1, 0);
            tlp.Controls.Add(btnBrowseLocal, 2, 0);
            pnl.Controls.Add(tlp);
            return pnl;
        }

        private Panel CreateNetworkFieldsPanel()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, BackColor = Color.Transparent, AutoSize = true, Visible = false };
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            txtNetworkPath = CreateStyledTextBox();
            txtNetworkPath.Text = @"\\server\share\Backups";
            txtNetworkPath.PlaceholderText = @"\\المسار\الشبكي\للمجلد";

            btnBrowseNetwork = CreateSecondaryButton("📁 استعراض...");
            btnBrowseNetwork.Dock = DockStyle.Fill;
            btnBrowseNetwork.Margin = new Padding(8, 4, 0, 4);

            tlp.Controls.Add(CreateStyledLabel("مسار الشبكة:"), 0, 0);
            tlp.Controls.Add(txtNetworkPath, 1, 0);
            tlp.Controls.Add(btnBrowseNetwork, 2, 0);
            pnl.Controls.Add(tlp);
            return pnl;
        }

        private Panel CreateGoogleFieldsPanel()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, BackColor = Color.Transparent, AutoSize = true, Visible = false };
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));

            txtGoogleDriveFolder = CreateStyledTextBox();
            txtGoogleDriveFolder.PlaceholderText = "معرف المجلد في Google Drive (اختياري)";

            tlp.Controls.Add(CreateStyledLabel("معرف المجلد:"), 0, 0);
            tlp.Controls.Add(txtGoogleDriveFolder, 1, 0);
            pnl.Controls.Add(tlp);
            return pnl;
        }

        private Panel CreateS3FieldsPanel()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, BackColor = Color.Transparent, AutoSize = true, Visible = false };
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 3,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            txtS3Bucket = CreateStyledTextBox();
            txtS3Bucket.PlaceholderText = "اسم الـ Bucket";

            txtS3AccessKey = CreateStyledTextBox();
            txtS3AccessKey.PlaceholderText = "Access Key ID";

            txtS3SecretKey = CreateStyledTextBox();
            txtS3SecretKey.PlaceholderText = "Secret Access Key";
            txtS3SecretKey.UseSystemPasswordChar = true;

            txtS3Region = CreateStyledTextBox();
            txtS3Region.Text = "us-east-1";

            tlp.Controls.Add(CreateStyledLabel("Bucket:"), 2, 0);
            tlp.Controls.Add(txtS3Bucket, 3, 0);
            tlp.Controls.Add(CreateStyledLabel("Access Key:"), 0, 1);
            tlp.Controls.Add(txtS3AccessKey, 1, 1);
            tlp.Controls.Add(CreateStyledLabel("Secret Key:"), 2, 1);
            tlp.Controls.Add(txtS3SecretKey, 3, 1);
            tlp.Controls.Add(CreateStyledLabel("المنطقة (Region):"), 0, 2);
            tlp.Controls.Add(txtS3Region, 1, 2);

            pnl.Controls.Add(tlp);
            return pnl;
        }

        private Panel CreateAzureFieldsPanel()
        {
            Panel pnl = new Panel { Dock = DockStyle.Top, BackColor = Color.Transparent, AutoSize = true, Visible = false };
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            txtAzureContainer = CreateStyledTextBox();
            txtAzureContainer.PlaceholderText = "اسم الـ Container";

            txtAzureConnectionString = CreateStyledTextBox();
            txtAzureConnectionString.PlaceholderText = "Azure Storage Connection String";

            tlp.Controls.Add(CreateStyledLabel("اسم Container:"), 0, 0);
            tlp.Controls.Add(txtAzureContainer, 1, 0);
            tlp.Controls.Add(CreateStyledLabel("Connection String:"), 0, 1);
            tlp.Controls.Add(txtAzureConnectionString, 1, 1);

            pnl.Controls.Add(tlp);
            return pnl;
        }

        private Panel CreateCloudAuthPanel()
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(254, 252, 232),
                AutoSize = true,
                Visible = false,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 8, 0, 0)
            };

            Label lblInfo = new Label
            {
                Text = "سيتم فتح نافذة المتصفح لتسجيل الدخول في المرة الأولى فقط. سيتم حفظ التوكن تلقائياً للاستخدامات اللاحقة.",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(161, 98, 7),
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = true,
                RightToLeft = RightToLeft.Yes
            };
            pnl.Controls.Add(lblInfo);
            return pnl;
        }

        private Panel CreateFooterPanel()
        {
            Panel pnlFooter = new Panel { Dock = DockStyle.Fill, Height = 56, Margin = new Padding(0, 4, 0, 0) };

            FlowLayoutPanel flpFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };

            btnCancel = new Button
            {
                Text = "❌ إلغاء",
                Size = new Size(130, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                RightToLeft = RightToLeft.Yes
            };
            btnCancel.FlatAppearance.BorderColor = BorderColor;
            btnCancel.FlatAppearance.BorderSize = 1;

            btnSave = new Button
            {
                Text = "💾 حفظ المهمة",
                Size = new Size(180, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                RightToLeft = RightToLeft.Yes,
                Visible = false
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor = PrimaryDark;
            btnSave.FlatAppearance.MouseDownBackColor = Color.FromArgb(17, 94, 89);

            btnNext = new Button
            {
                Text = "التالي ←",
                Size = new Size(140, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                RightToLeft = RightToLeft.Yes
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.MouseOverBackColor = PrimaryDark;

            btnPrev = new Button
            {
                Text = "→ السابق",
                Size = new Size(140, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                RightToLeft = RightToLeft.Yes,
                Visible = false
            };
            btnPrev.FlatAppearance.BorderColor = BorderColor;
            btnPrev.FlatAppearance.BorderSize = 1;

            flpFooter.Controls.Add(btnSave);
            flpFooter.Controls.Add(btnNext);
            flpFooter.Controls.Add(btnPrev);
            flpFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(flpFooter);
            return pnlFooter;
        }

        private TextBox CreateStyledTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(4, 8, 4, 8),
                RightToLeft = RightToLeft.Yes,
                Height = 32
            };
        }

        private Label CreateStyledLabel(string text, bool required = false)
        {
            return new Label
            {
                Text = text + (required ? " *" : ""),
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F, required ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = required ? TextPrimary : TextSecondary,
                Margin = new Padding(4, 8, 4, 8),
                RightToLeft = RightToLeft.Yes
            };
        }

        private CheckBox CreateStyledCheckBox(string text, bool initialChecked)
        {
            return new CheckBox
            {
                Text = text,
                Checked = initialChecked,
                AutoSize = true,
                Margin = new Padding(10, 6, 10, 6),
                RightToLeft = RightToLeft.Yes,
                Font = new Font("Segoe UI", 10F),
                ForeColor = TextPrimary,
                Cursor = Cursors.Hand
            };
        }

        private Button CreatePrimaryButton(string text)
        {
            Button btn = new Button
            {
                Text = text,
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4),
                RightToLeft = RightToLeft.Yes,
                Height = 36
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = PrimaryDark;
            return btn;
        }

        private Button CreateSecondaryButton(string text)
        {
            Button btn = new Button
            {
                Text = text,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Margin = new Padding(4),
                RightToLeft = RightToLeft.Yes,
                Height = 36
            };
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private Panel CreateDestCard(string icon, string title, string subtitle, bool selected = false)
        {
            Panel card = new Panel
            {
                Size = new Size(140, 108),
                Margin = new Padding(6),
                Cursor = Cursors.Hand,
                BackColor = selected ? PrimaryLight : Color.White,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(8),
                Tag = title
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var bounds = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (var path = new GraphicsPath())
                {
                    path.AddRoundedRectangle(bounds, 10);
                    bool isSel = card.BackColor == PrimaryLight;
                    using (var pen = new Pen(isSel ? PrimaryColor : BorderColor, isSel ? 2 : 1))
                        g.DrawPath(pen, path);

                    if (isSel)
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(30, PrimaryColor)))
                            g.FillPath(brush, path);
                    }
                }
            };

            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 22F),
                AutoSize = true,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 40,
                RightToLeft = RightToLeft.Yes
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = selected ? PrimaryDark : TextPrimary,
                AutoSize = true,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            };

            Label lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TextSecondary,
                AutoSize = true,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            };

            card.Controls.Add(lblSub);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblIcon);

            EventHandler clickHandler = (s, e) => SelectDestinationCard(card, title);
            card.Click += clickHandler;
            foreach (Control c in card.Controls) c.Click += clickHandler;

            return card;
        }

        private void ShowStep(int step)
        {
            if (step < 1 || step > 4) return;
            _currentStep = step;

            for (int i = 0; i < 4; i++)
            {
                bool isActive = i + 1 == step;
                bool isCompleted = i + 1 < step;

                _stepPanels[i].Visible = isActive;
                _stepCircles[i].BackColor = isActive || isCompleted ? PrimaryColor : Color.White;
                _stepCircles[i].Controls[0].ForeColor = isActive || isCompleted ? Color.White : TextSecondary;
                _stepLabels[i].ForeColor = isActive ? PrimaryDark : (isCompleted ? PrimaryColor : TextSecondary);
                _stepLabels[i].Font = new Font("Segoe UI", 11F, isActive ? FontStyle.Bold : FontStyle.Regular);

                if (i < 3)
                    _stepLines[i].BackColor = isCompleted ? PrimaryColor : BorderColor;
            }

            btnPrev.Visible = step > 1;
            btnNext.Visible = step < 4;
            btnSave.Visible = step == 4;

            string[] stepDescriptions = {
                "أدخل المعلومات الأساسية للمهمة",
                "اختر قواعد البيانات المراد نسخها",
                "حدد مواعيد التنفيذ والتكرار",
                "اختر مكان حفظ النسخ الاحتياطي"
            };
            lblFormSubtitle.Text = $"الخطوة {step} من 4: {stepDescriptions[step - 1]}";
        }

        private void SelectDestinationCard(Panel selectedCard, string destType)
        {
            foreach (Control c in flpDestCards.Controls)
            {
                if (c is Panel p)
                {
                    p.BackColor = Color.White;
                    if (p.Controls.Count > 1)
                        p.Controls[1].ForeColor = TextPrimary;
                    p.Invalidate();
                }
            }

            selectedCard.BackColor = PrimaryLight;
            if (selectedCard.Controls.Count > 1)
                selectedCard.Controls[1].ForeColor = PrimaryDark;
            selectedCard.Invalidate();

            pnlLocalFields.Visible = false;
            pnlNetworkFields.Visible = false;
            pnlGoogleFields.Visible = false;
            pnlS3Fields.Visible = false;
            pnlAzureFields.Visible = false;
            pnlCloudAuthFields.Visible = false;
            btnClearAllTokens.Visible = false;

            switch (destType)
            {
                case "محلي":
                    lblDestFieldTitle.Text = "إعدادات الحفظ المحلي";
                    pnlLocalFields.Visible = true;
                    break;

                case "شبكي":
                    lblDestFieldTitle.Text = "إعدادات الحفظ على الشبكة";
                    pnlNetworkFields.Visible = true;
                    break;

                case "Google Drive":
                    lblDestFieldTitle.Text = "إعدادات Google Drive";
                    pnlGoogleFields.Visible = true;
                    pnlCloudAuthFields.Visible = true;
                    btnClearAllTokens.Visible = true;
                    break;

                case "Dropbox":
                    lblDestFieldTitle.Text = "إعدادات Dropbox";
                    pnlCloudAuthFields.Visible = true;
                    btnClearAllTokens.Visible = true;
                    break;

                case "OneDrive":
                    lblDestFieldTitle.Text = "إعدادات OneDrive";
                    pnlCloudAuthFields.Visible = true;
                    btnClearAllTokens.Visible = true;
                    break;

                case "AWS S3":
                    lblDestFieldTitle.Text = "إعدادات AWS S3";
                    pnlS3Fields.Visible = true;
                    break;

                case "Azure Blob":
                    lblDestFieldTitle.Text = "إعدادات Azure Blob Storage";
                    pnlAzureFields.Visible = true;
                    break;
            }
        }

        private void SetupEventHandlers()
        {
            btnLoadDatabases.Click += async (s, e) => await LoadDatabasesAsync();
            btnSelectAllDB.Click += (s, e) => {
                for (int i = 0; i < chklstDatabases.Items.Count; i++)
                    chklstDatabases.SetItemChecked(i, true);
                UpdateDBCount();
            };
            btnDeselectAllDB.Click += (s, e) => {
                for (int i = 0; i < chklstDatabases.Items.Count; i++)
                    chklstDatabases.SetItemChecked(i, false);
                UpdateDBCount();
            };

            chklstDatabases.ItemCheck += (s, e) => {
                string item = chklstDatabases.Items[e.Index].ToString();
                if (e.NewValue == CheckState.Checked)
                    _checkedDatabases.Add(item);
                else
                    _checkedDatabases.Remove(item);
                if (this.IsHandleCreated)
                    BeginInvoke(new Action(UpdateDBCount));
                else
                    UpdateDBCount();
            };

            txtSearchDB.TextChanged += (s, e) => FilterDatabases();

            cmbScheduleType.SelectedIndexChanged += (s, e) => UpdateScheduleUI();
            btnAdvancedSchedule.Click += BtnAdvancedSchedule_Click;

            btnNext.Click += (s, e) => ShowStep(_currentStep + 1);
            btnPrev.Click += (s, e) => ShowStep(_currentStep - 1);
            btnSave.Click += btnSave_Click;
            btnCancel.Click += btnCancel_Click;
            btnShowInstructions.Click += BtnShowInstructions_Click;
            btnClearAllTokens.Click += btnClearAllTokens_Click;

            btnBrowseLocal.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog { Description = "اختر مجلد حفظ النسخ الاحتياطي" })
                    if (fbd.ShowDialog() == DialogResult.OK) txtLocalPath.Text = fbd.SelectedPath;
            };

            btnBrowseNetwork.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog { Description = "اختر مجلد الشبكة لحفظ النسخ الاحتياطي" })
                    if (fbd.ShowDialog() == DialogResult.OK) txtNetworkPath.Text = fbd.SelectedPath;
            };
        }

        private void BtnAdvancedSchedule_Click(object sender, EventArgs e)
        {
            if (_schedule == null)
                _schedule = new Schedule { JobId = _job?.Id ?? Guid.NewGuid().ToString() };

            if (_schedule.CustomDaysOfWeek == null)
                _schedule.CustomDaysOfWeek = new List<int>();

            if (_schedule.CustomDayTimes == null)
                _schedule.CustomDayTimes = new Dictionary<int, List<string>>();

            if (_schedule.CustomDaysOfWeek.Count == 0 && chkDays != null)
            {
                for (int i = 0; i < chkDays.Length; i++)
                {
                    if (chkDays[i].Checked)
                    {
                        _schedule.CustomDaysOfWeek.Add(i);
                    }
                }
            }

            using (var scheduleForm = new AdvancedScheduleForm(_schedule))
            {
                if (scheduleForm.ShowDialog() == DialogResult.OK)
                {
                    _schedule = scheduleForm.ScheduleData;
                    _schedule.ScheduleType = "Custom";
                    cmbScheduleType.SelectedItem = "مخصص";

                    UpdateScheduleUI();

                    if (chkDays != null)
                    {
                        for (int i = 0; i < chkDays.Length; i++)
                        {
                            chkDays[i].Checked = _schedule.CustomDaysOfWeek.Contains(i);
                        }
                    }

                    UpdateScheduleSummary();

                    MessageBox.Show("✅ تم تحديث الجدولة المخصصة بنجاح!", "نجاح",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateScheduleUI()
        {
            string type = cmbScheduleType.SelectedItem?.ToString() ?? "يومي";
            bool isWeekly = type == "أسبوعي";
            bool isInterval = type == "فترة زمنية";
            bool isCustom = type == "مخصص";
            bool isMissedVisible = type != "مرة واحدة";

            _stepPanels[2].SuspendLayout();

            dtpStartTime.Visible = !isCustom;

            foreach (Control c in _stepPanels[2].Controls[0].Controls)
            {
                if (c is FlowLayoutPanel flp && flp.Tag?.ToString() == "IntervalRow")
                {
                    flp.Visible = isInterval;
                    break;
                }
            }
            numIntervalMinutes.Visible = isInterval;
            lblIntervalUnit.Visible = isInterval;

            if (pnlMissedOptions != null)
                pnlMissedOptions.Visible = isMissedVisible;

            if (pnlDaysWrapper != null)
                pnlDaysWrapper.Visible = isWeekly;

            if (pnlCustomWrapper != null)
                pnlCustomWrapper.Visible = isCustom;

            if (isCustom)
            {
                UpdateScheduleSummary();
            }
            else
            {
                string scheduleDisplay = type switch
                {
                    "مرة واحدة" => $"مرة واحدة في {dtpStartTime.Value:HH:mm}",
                    "يومي" => $"يومياً في {dtpStartTime.Value:HH:mm}",
                    "أسبوعي" => "أسبوعياً في يوم محدد",
                    "شهري" => "شهرياً في يوم محدد",
                    "فترة زمنية" => $"كل {numIntervalMinutes.Value} دقيقة (بدءاً من {dtpStartTime.Value:HH:mm})",
                    _ => type
                };

                lblScheduleSummary.Text = $"📋 الجدولة: {scheduleDisplay}";
                lblScheduleSummary.ForeColor = TextSecondary;
                lblScheduleSummary.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            }

            _stepPanels[2].ResumeLayout(true);
            _stepPanels[2].PerformLayout();
        }

        private void UpdateScheduleSummary()
        {
            if (_schedule.ScheduleType != "Custom" && _schedule.ScheduleType != "مخصص")
            {
                lblScheduleSummary.Text = "📋 اختر 'مخصص' من قائمة الجدولة ثم اضغط على زر الجدولة المتقدمة";
                lblScheduleSummary.ForeColor = TextSecondary;
                lblScheduleSummary.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                return;
            }

            if (_schedule.CustomDaysOfWeek == null || _schedule.CustomDaysOfWeek.Count == 0)
            {
                lblScheduleSummary.Text = "📋 لم يتم تحديد أيام - استخدم الجدولة المتقدمة";
                lblScheduleSummary.ForeColor = TextSecondary;
                lblScheduleSummary.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                return;
            }

            string summary = "📋 الجدولة المخصصة:\n";
            var days = _schedule.CustomDaysOfWeek.OrderBy(d => d).ToList();
            string[] dayNames = { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" };

            int dayCount = 0;
            foreach (int day in days)
            {
                var times = _schedule.GetTimesForDay(day);
                if (times != null && times.Any())
                {
                    summary += $"  • {dayNames[day]}: {string.Join(", ", times)}\n";
                    dayCount++;
                }
                else
                {
                    summary += $"  • {dayNames[day]}: (لا توجد أوقات محددة)\n";
                }
            }

            if (dayCount == 0)
            {
                summary = "📋 لم يتم تحديد أوقات لأي يوم - استخدم الجدولة المتقدمة";
                lblScheduleSummary.ForeColor = TextSecondary;
                lblScheduleSummary.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            }
            else
            {
                lblScheduleSummary.ForeColor = PrimaryDark;
                lblScheduleSummary.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }

            lblScheduleSummary.Text = summary;
            lblScheduleSummary.TextAlign = ContentAlignment.MiddleRight;
        }

        private async Task LoadDatabasesAsync()
        {
            if (_isLoadingDatabases) return;

            try
            {
                _isLoadingDatabases = true;
                btnLoadDatabases.Enabled = false;
                btnLoadDatabases.Text = "⏳ جاري التحميل...";
                lblLoadingStatus.Visible = true;
                lblLoadingStatus.Text = "⏳ جاري الاتصال بقاعدة البيانات...";

                var conn = cmbConnection.SelectedItem as ServerConnection;
                if (conn == null)
                {
                    MessageBox.Show("اختر اتصالاً أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string cs = BuildConnectionString(conn);
                if (string.IsNullOrEmpty(cs))
                {
                    MessageBox.Show("خطأ في سلسلة الاتصال", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                chklstDatabases.Items.Clear();
                _allDatabases.Clear();
                _checkedDatabases.Clear();

                var databases = await Task.Run(() => GetDatabaseList(cs));

                if (databases == null || databases.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على قواعد بيانات (باستثناء system databases)", "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var previouslyChecked = _isEditMode && _job.DatabaseNames != null
                    ? new HashSet<string>(_job.DatabaseNames)
                    : new HashSet<string>();

                foreach (var db in databases)
                {
                    bool isChecked = previouslyChecked.Contains(db);
                    if (isChecked) _checkedDatabases.Add(db);
                    _allDatabases.Add(db);
                    chklstDatabases.Items.Add(db, isChecked);
                }

                UpdateDBCount();
                MessageBox.Show($"تم تحميل {chklstDatabases.Items.Count} قاعدة بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"خطأ في الاتصال بقاعدة البيانات:\n{ex.Message}\n\nتأكد من:\n- صحة بيانات الاتصال\n- أن الخادم يعمل\n- أن لديك صلاحيات الوصول", "خطأ في الاتصال", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ غير متوقع:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isLoadingDatabases = false;
                btnLoadDatabases.Enabled = true;
                btnLoadDatabases.Text = "🔄 تحميل القواعد";
                lblLoadingStatus.Visible = false;
                lblLoadingStatus.Text = "";
            }
        }

        private List<string> GetDatabaseList(string connectionString)
        {
            var databases = new List<string>();
            var excludeList = new HashSet<string> { "master", "tempdb", "model", "msdb", "distribution", "ReportServer", "ReportServerTempDB" };

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = connection.GetSchema("Databases");

                    foreach (DataRow row in dt.Rows)
                    {
                        string name = row["database_name"]?.ToString();
                        if (!string.IsNullOrEmpty(name) && !excludeList.Contains(name.ToLower()))
                        {
                            databases.Add(name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("AddEditJobForm", "Error", $"خطأ في الحصول على قواعد البيانات: {ex.Message}");
                throw;
            }

            return databases;
        }

        private void FilterDatabases()
        {
            string filter = txtSearchDB.Text.ToLower().Trim();
            chklstDatabases.Items.Clear();

            foreach (var item in _allDatabases)
            {
                if (string.IsNullOrEmpty(filter) || item.ToLower().Contains(filter))
                {
                    chklstDatabases.Items.Add(item, _checkedDatabases.Contains(item));
                }
            }
            UpdateDBCount();
        }

        private void UpdateDBCount()
        {
            int count = chklstDatabases.CheckedItems.Count;
            int total = chklstDatabases.Items.Count;
            lblDBCount.Text = $"{count} من {total} قاعدة محددة";
            lblDBCount.ForeColor = count > 0 ? SuccessColor : ErrorColor;
            lblDBCount.Font = new Font("Segoe UI", 9.5F, count > 0 ? FontStyle.Regular : FontStyle.Bold);
        }

        private void LoadConnections()
        {
            try
            {
                if (_settings.ServerConnections == null || _settings.ServerConnections.Count == 0)
                {
                    lblConnectionStatus.Visible = true;
                    cmbConnection.DataSource = null;
                    cmbConnection.Items.Clear();
                    cmbConnection.Items.Add("لا توجد اتصالات - أضف من الإعدادات");
                    cmbConnection.SelectedIndex = 0;
                    return;
                }

                lblConnectionStatus.Visible = false;
                cmbConnection.DataSource = _settings.ServerConnections;
                cmbConnection.DisplayMember = "DisplayName";
                cmbConnection.ValueMember = "Id";

                if (_isEditMode && !string.IsNullOrEmpty(_job.ConnectionId))
                {
                    try { cmbConnection.SelectedValue = _job.ConnectionId; }
                    catch { cmbConnection.SelectedIndex = 0; }
                }
                else
                    cmbConnection.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الاتصالات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildConnectionString(ServerConnection c)
        {
            try
            {
                if (c.AuthType == "Windows")
                    return $"Server={c.ServerName};Integrated Security=True;TrustServerCertificate=True;Connection Timeout=30;";
                return $"Server={c.ServerName};User Id={c.Username};Password={EncryptionHelper.Decrypt(c.EncryptedPassword)};TrustServerCertificate=True;Connection Timeout=30;";
            }
            catch { return null; }
        }

        private void LoadJobData()
        {
            try
            {
                txtJobName.Text = _job.JobName;
                chkExecuteMissed.Checked = _job.ExecuteMissedJobs;

                if (!string.IsNullOrEmpty(_job.ConnectionId))
                {
                    try { cmbConnection.SelectedValue = _job.ConnectionId; }
                    catch { }
                }

                cmbBackupType.SelectedItem = _job.BackupType;
                chkCompression.Checked = _job.Compression;
                chkEncryption.Checked = _job.Encryption;
                numRetentionCount.Value = _job.RetentionCount;
                chkActive.Checked = _job.IsActive;
                chkKeepLocalCopy.Checked = _job.KeepLocalCopy;

                string scheduleType = _schedule.ScheduleType switch
                {
                    "Once" => "مرة واحدة",
                    "Daily" => "يومي",
                    "Weekly" => "أسبوعي",
                    "Monthly" => "شهري",
                    "Interval" => "فترة زمنية",
                    "Custom" => "مخصص",
                    _ => "يومي"
                };
                cmbScheduleType.SelectedItem = scheduleType;
                dtpStartTime.Value = _schedule.StartTime;
                numIntervalMinutes.Value = _schedule.IntervalMinutes > 0 ? _schedule.IntervalMinutes : 60;

                if (numMaxMissed != null)
                    numMaxMissed.Value = _schedule.MaxMissedExecutions;

                if (chkDays != null)
                {
                    for (int i = 0; i < chkDays.Length; i++)
                        chkDays[i].Checked = false;

                    if (_schedule.DayOfWeek >= 0 && _schedule.DayOfWeek <= 6)
                    {
                        chkDays[_schedule.DayOfWeek].Checked = true;
                    }

                    if (_schedule.ScheduleType == "Custom" && _schedule.CustomDaysOfWeek != null)
                    {
                        foreach (int day in _schedule.CustomDaysOfWeek)
                        {
                            if (day >= 0 && day <= 6)
                                chkDays[day].Checked = true;
                        }
                    }
                }

                if (_schedule.ScheduleType == "Custom")
                {
                    UpdateScheduleSummary();
                }

                string destType = _destination.DestinationType switch
                {
                    "Local" => "محلي",
                    "Network" => "شبكي",
                    "GoogleDrive" => "Google Drive",
                    "Dropbox" => "Dropbox",
                    "OneDrive" => "OneDrive",
                    "S3" => "AWS S3",
                    "Azure" => "Azure Blob",
                    _ => "محلي"
                };

                foreach (Control c in flpDestCards.Controls)
                {
                    if (c is Panel p && p.Controls.Count > 1 && p.Controls[1].Text == destType)
                    {
                        SelectDestinationCard(p, destType);
                        break;
                    }
                }

                txtLocalPath.Text = _destination.LocalPath ?? "Backups";
                txtNetworkPath.Text = _destination.NetworkPath ?? @"\\server\share\Backups";
                txtGoogleDriveFolder.Text = _destination.GoogleDriveFolderId ?? "";
                txtS3Bucket.Text = _destination.S3BucketName ?? "";
                txtS3AccessKey.Text = _destination.S3AccessKey ?? "";
                txtS3SecretKey.Text = _destination.S3SecretKey ?? "";
                txtS3Region.Text = _destination.S3Region ?? "us-east-1";
                txtAzureContainer.Text = _destination.AzureContainerName ?? "";
                txtAzureConnectionString.Text = _destination.AzureConnectionString ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateForm()) return;

                _job.JobName = txtJobName.Text.Trim();
                _job.ConnectionId = ((ServerConnection)cmbConnection.SelectedItem)?.Id ?? "";
                _job.DatabaseNames = chklstDatabases.CheckedItems.Cast<string>().ToList();
                _job.BackupType = cmbBackupType.SelectedItem?.ToString() ?? "Full";
                _job.Compression = chkCompression.Checked;
                _job.Encryption = chkEncryption.Checked;
                _job.RetentionCount = (int)numRetentionCount.Value;
                _job.IsActive = chkActive.Checked;
                _job.KeepLocalCopy = chkKeepLocalCopy.Checked;
                _job.ExecuteMissedJobs = chkExecuteMissed.Checked;

                SaveScheduleData();
                SaveDestinationData();

                var settings = JsonFileHelper.ReadSettings();
                if (_isEditMode)
                {
                    settings.BackupJobs.RemoveAll(j => j.Id == _job.Id);
                    settings.Schedules.RemoveAll(s => s.JobId == _job.Id);
                    settings.Destinations.RemoveAll(d => d.JobId == _job.Id);
                }
                settings.BackupJobs.Add(_job);
                settings.Schedules.Add(_schedule);
                settings.Destinations.Add(_destination);
                JsonFileHelper.SaveSettings(settings);

                MessageBox.Show($"تم حفظ المهمة '{_job.JobName}' بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveScheduleData()
        {
            string st = cmbScheduleType.SelectedItem?.ToString() ?? "يومي";
            _schedule.ScheduleType = st switch
            {
                "مرة واحدة" => "Once",
                "يومي" => "Daily",
                "أسبوعي" => "Weekly",
                "شهري" => "Monthly",
                "فترة زمنية" => "Interval",
                "مخصص" => "Custom",
                _ => "Daily"
            };
            _schedule.StartTime = dtpStartTime.Value;
            _schedule.IntervalMinutes = (int)numIntervalMinutes.Value;

            if (numMaxMissed != null)
                _schedule.MaxMissedExecutions = (int)numMaxMissed.Value;

            if (chkDays != null)
            {
                if (_schedule.ScheduleType != "Custom")
                {
                    for (int i = 0; i < chkDays.Length; i++)
                    {
                        if (chkDays[i].Checked)
                        {
                            _schedule.DayOfWeek = i;
                            break;
                        }
                    }
                }
            }

            if (_schedule.ScheduleType == "Custom")
            {
                var customDays = new List<int>();
                if (chkDays != null)
                {
                    for (int i = 0; i < chkDays.Length; i++)
                    {
                        if (chkDays[i].Checked)
                            customDays.Add(i);
                    }
                }
                _schedule.CustomDaysOfWeek = customDays.Any() ? customDays : new List<int> { 0 };

                if (_schedule.CustomDayTimes == null || _schedule.CustomDayTimes.Count == 0)
                {
                    _schedule.CustomDayTimes = new Dictionary<int, List<string>>();
                    var defaultTimes = new List<string> { "08:00" };
                    foreach (int day in _schedule.CustomDaysOfWeek)
                    {
                        _schedule.CustomDayTimes[day] = new List<string>(defaultTimes);
                    }
                }

                var allTimes = new HashSet<string>();
                foreach (var dayTimes in _schedule.CustomDayTimes.Values)
                {
                    foreach (var time in dayTimes)
                        allTimes.Add(time);
                }
                _schedule.CustomTimes = allTimes.Any() ? allTimes.ToList() : new List<string> { "08:00" };
                _schedule.CustomExecutionCountPerDay = _schedule.CustomTimes.Count > 0 ? _schedule.CustomTimes.Count : 1;
            }
        }

        private void SaveDestinationData()
        {
            string destTitle = GetSelectedDestinationTitle();
            _destination.DestinationType = destTitle switch
            {
                "محلي" => "Local",
                "شبكي" => "Network",
                "Google Drive" => "GoogleDrive",
                "Dropbox" => "Dropbox",
                "OneDrive" => "OneDrive",
                "AWS S3" => "S3",
                "Azure Blob" => "Azure",
                _ => "Local"
            };

            _destination.LocalPath = pnlLocalFields.Visible ? txtLocalPath.Text.Trim() : "Backups";
            _destination.NetworkPath = pnlNetworkFields.Visible ? txtNetworkPath.Text.Trim() : @"\\server\share\Backups";
            _destination.GoogleDriveFolderId = txtGoogleDriveFolder.Text.Trim();
            _destination.S3BucketName = txtS3Bucket.Text.Trim();
            _destination.S3AccessKey = txtS3AccessKey.Text.Trim();
            _destination.S3SecretKey = txtS3SecretKey.Text.Trim();
            _destination.S3Region = txtS3Region.Text.Trim();
            _destination.AzureContainerName = txtAzureContainer.Text.Trim();
            _destination.AzureConnectionString = txtAzureConnectionString.Text.Trim();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtJobName.Text))
            {
                txtJobName.Focus();
                MessageBox.Show("أدخل اسم المهمة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (chklstDatabases.CheckedItems.Count == 0)
            {
                MessageBox.Show("اختر قاعدة بيانات واحدة على الأقل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbConnection.SelectedItem == null || cmbConnection.SelectedIndex < 0)
            {
                MessageBox.Show("اختر اتصالاً صالحاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string destTitle = GetSelectedDestinationTitle();
            if (string.IsNullOrEmpty(destTitle))
            {
                MessageBox.Show("اختر وجهة لحفظ النسخ الاحتياطي", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private string GetSelectedDestinationTitle()
        {
            foreach (Control c in flpDestCards.Controls)
            {
                if (c is Panel p && p.BackColor == PrimaryLight && p.Controls.Count > 1)
                {
                    return p.Controls[1].Text;
                }
            }
            return "";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("هل أنت متأكد من إلغاء العملية؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void BtnShowInstructions_Click(object sender, EventArgs e)
        {
            string destTitle = GetSelectedDestinationTitle();

            string message = destTitle switch
            {
                "Google Drive" => @"Google Drive - تفعيل تلقائي

في المرة الأولى فقط:

1. ستفتح نافذة متصفح لتسجيل الدخول إلى Google
2. سجل الدخول ووافق على الصلاحيات
3. سيتم حفظ التوكين تلقائياً

سيتم رفع الملفات تلقائياً في المرات التالية!",
                "Dropbox" => @"Dropbox - تفعيل تلقائي

في المرة الأولى فقط:

1. ستفتح نافذة متصفح لتسجيل الدخول إلى Dropbox
2. سجل الدخول ووافق على الصلاحيات
3. سيتم حفظ Refresh Token تلقائياً

سيتم رفع الملفات تلقائياً في المرات التالية!",
                "OneDrive" => @"OneDrive - تفعيل تلقائي

في المرة الأولى فقط:

1. ستفتح نافذة متصفح لتسجيل الدخول إلى Microsoft
2. سجل الدخول ووافق على الصلاحيات
3. سيتم حفظ التوكين تلقائياً

سيتم رفع الملفات تلقائياً في المرات التالية!",
                _ => "لا توجد تعليمات خاصة لهذه الخدمة."
            };

            MessageBox.Show(message, "تعليمات الخدمة السحابية", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClearAllTokens_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("هل أنت متأكد من حذف جميع توكينات المصادقة؟\n\nسيُطلب منك تسجيل الدخول مرة أخرى إلى:\n• Google Drive\n• Dropbox\n• OneDrive",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                CloudUploadService.ClearAllTokens();
                MessageBox.Show("تم حذف جميع التوكينات بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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

    public static class GraphicsExtensions
    {
        public static void AddRoundedRectangle(this GraphicsPath path, Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
        }
    }
}