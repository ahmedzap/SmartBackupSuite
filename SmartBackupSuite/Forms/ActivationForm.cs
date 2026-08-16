using SmartBackupSuite.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartBackupSuite.Forms
{
    public partial class ActivationForm : Form
    {
        private bool _isActivated = false;
        private LicenseHelper.LicenseData _licenseData;
        private bool _isDeveloper = false;

        public bool IsActivated => _isActivated;
        public LicenseHelper.LicenseData LicenseData => _licenseData;
        public bool IsDeveloper => _isDeveloper;

        public ActivationForm()
        {
            InitializeComponent();
            ApplyTheme();
            CheckForExistingLicense();
            this.KeyPreview = true;
            btnActivate.Enabled = true;
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActivationForm));
            lblTitle = new Label();
            lblSubTitle = new Label();
            lblInfo = new Label();
            txtLicenseKey = new TextBox();
            btnActivate = new Button();
            btnExit = new Button();
            btnRequestKey = new Button();
            lblStatus = new Label();
            panelHeader = new Panel();
            picIcon = new PictureBox();
            lblVersion = new Label();
            lblLicenseInfo = new Label();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(137, 16);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(277, 32);
            lblTitle.TabIndex = 1;
            lblTitle.Text = "🔐 Smart Backup Suite";
            // 
            // lblSubTitle
            // 
            lblSubTitle.AutoSize = true;
            lblSubTitle.Font = new Font("Segoe UI", 10F);
            lblSubTitle.ForeColor = Color.FromArgb(200, 240, 255);
            lblSubTitle.Location = new Point(137, 48);
            lblSubTitle.Name = "lblSubTitle";
            lblSubTitle.Size = new Size(197, 19);
            lblSubTitle.TabIndex = 2;
            lblSubTitle.Text = "نظام النسخ الاحتياطي الاحترافي";
            // 
            // lblInfo
            // 
            lblInfo.Font = new Font("Segoe UI", 10F);
            lblInfo.ForeColor = Color.FromArgb(40, 40, 40);
            lblInfo.Location = new Point(30, 110);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(490, 60);
            lblInfo.TabIndex = 0;
            lblInfo.Text = "📌 يرجى إدخال مفتاح التفعيل لتفعيل البرنامج\n\n💡 يمكنك الحصول على مفتاح التفعيل من المطور";
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtLicenseKey
            // 
            txtLicenseKey.BorderStyle = BorderStyle.FixedSingle;
            txtLicenseKey.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            txtLicenseKey.ForeColor = Color.FromArgb(0, 150, 180);
            txtLicenseKey.Location = new Point(30, 185);
            txtLicenseKey.MaxLength = 29;
            txtLicenseKey.Name = "txtLicenseKey";
            txtLicenseKey.Size = new Size(490, 29);
            txtLicenseKey.TabIndex = 1;
            txtLicenseKey.TextAlign = HorizontalAlignment.Center;
            txtLicenseKey.TextChanged += TxtLicenseKey_TextChanged;
            txtLicenseKey.KeyPress += TxtLicenseKey_KeyPress;
            // 
            // btnActivate
            // 
            btnActivate.BackColor = Color.FromArgb(46, 204, 113);
            btnActivate.FlatAppearance.BorderSize = 0;
            btnActivate.FlatStyle = FlatStyle.Flat;
            btnActivate.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnActivate.ForeColor = Color.White;
            btnActivate.Location = new Point(80, 300);
            btnActivate.Name = "btnActivate";
            btnActivate.Size = new Size(200, 45);
            btnActivate.TabIndex = 4;
            btnActivate.Text = "✅ تفعيل";
            btnActivate.UseVisualStyleBackColor = false;
            btnActivate.Click += BtnActivate_Click;
            // 
            // btnExit
            // 
            btnExit.BackColor = Color.FromArgb(200, 50, 50);
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnExit.ForeColor = Color.White;
            btnExit.Location = new Point(290, 300);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(200, 45);
            btnExit.TabIndex = 5;
            btnExit.Text = "❌ خروج";
            btnExit.UseVisualStyleBackColor = false;
            btnExit.Click += BtnExit_Click;
            // 
            // btnRequestKey
            // 
            btnRequestKey.BackColor = Color.Transparent;
            btnRequestKey.Cursor = Cursors.Hand;
            btnRequestKey.FlatAppearance.BorderSize = 0;
            btnRequestKey.FlatStyle = FlatStyle.Flat;
            btnRequestKey.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            btnRequestKey.ForeColor = Color.FromArgb(0, 150, 180);
            btnRequestKey.Location = new Point(200, 355);
            btnRequestKey.Name = "btnRequestKey";
            btnRequestKey.Size = new Size(150, 25);
            btnRequestKey.TabIndex = 6;
            btnRequestKey.Text = "🔗 طلب مفتاح تفعيل";
            btnRequestKey.UseVisualStyleBackColor = false;
            btnRequestKey.Click += BtnRequestKey_Click;
            // 
            // lblStatus
            // 
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(200, 50, 50);
            lblStatus.Location = new Point(30, 220);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(490, 25);
            lblStatus.TabIndex = 2;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(0, 150, 180);
            panelHeader.Controls.Add(picIcon);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSubTitle);
            panelHeader.Controls.Add(lblVersion);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(535, 80);
            panelHeader.TabIndex = 7;
            // 
            // picIcon
            // 
            picIcon.Image = Properties.Resources.logosbs;
            picIcon.Location = new Point(0, 0);
            picIcon.Name = "picIcon";
            picIcon.Size = new Size(131, 80);
            picIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            picIcon.TabIndex = 0;
            picIcon.TabStop = false;
            picIcon.Click += picIcon_Click;
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 8F);
            lblVersion.ForeColor = Color.FromArgb(200, 240, 255);
            lblVersion.Location = new Point(460, 52);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(69, 13);
            lblVersion.TabIndex = 3;
            lblVersion.Text = "الإصدار 1.2.0";
            // 
            // lblLicenseInfo
            // 
            lblLicenseInfo.Font = new Font("Segoe UI", 9F);
            lblLicenseInfo.ForeColor = Color.FromArgb(46, 204, 113);
            lblLicenseInfo.Location = new Point(30, 248);
            lblLicenseInfo.Name = "lblLicenseInfo";
            lblLicenseInfo.Size = new Size(490, 40);
            lblLicenseInfo.TabIndex = 3;
            lblLicenseInfo.TextAlign = ContentAlignment.MiddleCenter;
            lblLicenseInfo.Visible = false;
            // 
            // ActivationForm
            // 
            BackColor = Color.FromArgb(224, 247, 250);
            ClientSize = new Size(535, 412);
            Controls.Add(lblInfo);
            Controls.Add(txtLicenseKey);
            Controls.Add(lblStatus);
            Controls.Add(lblLicenseInfo);
            Controls.Add(btnActivate);
            Controls.Add(btnExit);
            Controls.Add(btnRequestKey);
            Controls.Add(panelHeader);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ActivationForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "🔐 تفعيل البرنامج - Smart Backup Suite";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void ApplyTheme()
        {
            this.btnActivate.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            this.btnActivate.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 130, 70);
            this.btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 30, 30);
            this.btnExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 20, 20);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.G))
            {
                this.Hide();
                using (var generatorForm = new LicenseGeneratorForm())
                {
                    generatorForm.ShowDialog();
                }
                this.Show();
                this.BringToFront();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CheckForExistingLicense()
        {
            try
            {
                var savedLicense = LicenseHelper.LoadLicenseFromFile();
                if (savedLicense != null)
                {
                    var result = LicenseHelper.ValidateLicenseWithDetails(savedLicense.LicenseKey);

                    if (result.IsValid)
                    {
                        _licenseData = result.LicenseData;
                        _isActivated = true;
                        _isDeveloper = result.IsDeveloper;

                        string typeName = _licenseData.LicenseType switch
                        {
                            "Trial" => "تجريبي",
                            "Monthly" => "شهري",
                            "Yearly" => "سنوي",
                            "Lifetime" => "دائم",
                            _ => _licenseData.LicenseType
                        };

                        if (_isDeveloper)
                        {
                            lblStatus.Text = "✅ مفتاح المطور - ترخيص دائم";
                            lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                            lblLicenseInfo.Text = "👤 المطور | 📋 دائم | 🚀 صلاحية غير محدودة";
                        }
                        else
                        {
                            lblStatus.Text = $"✅ الترخيص صالح - ينتهي في {result.ExpiryDate:yyyy-MM-dd}";
                            lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                            lblLicenseInfo.Text = $"👤 {_licenseData.CustomerName} | 📋 {typeName} | ";
                        }
                        lblLicenseInfo.Visible = true;

                        var timer = new System.Windows.Forms.Timer();
                        timer.Interval = 1500;
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        };
                        timer.Start();
                        return;
                    }
                    else
                    {
                        lblStatus.Text = $"⚠️ {result.Message}";
                        lblStatus.ForeColor = Color.FromArgb(200, 150, 0);
                        LicenseHelper.DeleteLicenseFile();
                    }
                }
                else
                {
                    lblStatus.Text = "📝 الرجاء إدخال مفتاح التفعيل";
                    lblStatus.ForeColor = Color.FromArgb(52, 152, 219);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"⚠️ خطأ: {ex.Message}";
                lblStatus.ForeColor = Color.FromArgb(200, 50, 50);
            }
        }

        private void TxtLicenseKey_TextChanged(object sender, EventArgs e)
        {
            string key = txtLicenseKey.Text.Replace("-", "").ToUpper();

            if (key.Length > 0)
            {
                string formattedKey = "";
                for (int i = 0; i < key.Length; i++)
                {
                    if (i > 0 && i % 5 == 0 && i < 25)
                        formattedKey += "-";
                    formattedKey += key[i];
                }

                if (txtLicenseKey.Text != formattedKey)
                {
                    int selectionStart = txtLicenseKey.SelectionStart;
                    txtLicenseKey.Text = formattedKey;
                    txtLicenseKey.SelectionStart = Math.Min(selectionStart + 1, txtLicenseKey.Text.Length);
                }
            }

            btnActivate.Enabled = !string.IsNullOrEmpty(txtLicenseKey.Text);

            txtLicenseKey.BackColor = !string.IsNullOrEmpty(txtLicenseKey.Text) ?
                Color.FromArgb(220, 255, 220) : Color.White;

            lblStatus.Text = "";
            lblLicenseInfo.Visible = false;
        }

        private void TxtLicenseKey_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsLetterOrDigit(e.KeyChar) && e.KeyChar != '-' && !char.IsControl(e.KeyChar))
                e.Handled = true;

            if (char.IsLetter(e.KeyChar))
                e.KeyChar = char.ToUpper(e.KeyChar);
        }

        private async void BtnActivate_Click(object sender, EventArgs e)
        {
            string licenseKey = txtLicenseKey.Text.Trim();

            if (string.IsNullOrEmpty(licenseKey))
            {
                lblStatus.Text = "⚠️ الرجاء إدخال مفتاح التفعيل";
                lblStatus.ForeColor = Color.FromArgb(200, 50, 50);
                return;
            }

            btnActivate.Enabled = false;
            btnActivate.Text = "⏳ جاري التحقق...";
            lblStatus.Text = "";
            lblLicenseInfo.Visible = false;

            try
            {
                var result = LicenseHelper.ValidateLicenseWithDetails(licenseKey);

                if (result.IsValid)
                {
                    _licenseData = result.LicenseData;
                    _isActivated = true;
                    _isDeveloper = result.IsDeveloper;

                    // ✅ حفظ الترخيص مع ربطه بالجهاز الحالي
                    var license = result.LicenseData;

                    // ✅ تحديث BoundMachineId إلى الجهاز الحالي (لتجنب خطأ "مقيد بجهاز آخر")
                    string currentMachineId = LicenseHelper.GetMachineId();
                    license.BoundMachineId = currentMachineId;

                    // ✅ حفظ الترخيص
                    LicenseHelper.SaveLicenseToFile(license);

                    string typeName = license.LicenseType switch
                    {
                        "Trial" => "تجريبي",
                        "Monthly" => "شهري",
                        "Yearly" => "سنوي",
                        "Lifetime" => "دائم",
                        _ => license.LicenseType
                    };

                    lblStatus.Text = $"✅ تم التفعيل بنجاح!";
                    lblStatus.ForeColor = Color.FromArgb(46, 204, 113);

                    lblLicenseInfo.Text =
                        $"👤 {license.CustomerName} | 📋 {typeName} | 📅 ينتهي في {license.ExpiryDate:yyyy-MM-dd}";
                    lblLicenseInfo.Visible = true;

                    await System.Threading.Tasks.Task.Delay(800);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblStatus.Text = $"❌ {result.Message}";
                    lblStatus.ForeColor = Color.FromArgb(200, 50, 50);
                    txtLicenseKey.BackColor = Color.FromArgb(255, 220, 220);

                    if (result.IsExpired)
                    {
                        lblStatus.Text += "\n💡 يرجى التواصل مع المطور لتجديد الترخيص";
                    }

                    btnActivate.Enabled = true;
                    btnActivate.Text = "✅ تفعيل";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ خطأ: {ex.Message}";
                lblStatus.ForeColor = Color.FromArgb(200, 50, 50);
                btnActivate.Enabled = true;
                btnActivate.Text = "✅ تفعيل";
            }
            finally
            {
                if (!_isActivated)
                {
                    btnActivate.Enabled = true;
                    btnActivate.Text = "✅ تفعيل";
                }
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "هل أنت متأكد من الخروج؟\n\nلن تتمكن من استخدام البرنامج بدون تفعيل.",
                "تأكيد الخروج",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void BtnRequestKey_Click(object sender, EventArgs e)
        {
            string machineId = LicenseHelper.GetMachineId();
            string message =
                "🔑 للحصول على مفتاح تفعيل:\n\n" +
                "📧 تواصل مع المطور عبر:\n" +
                "   - البريد الإلكتروني: sbsm.ye@gmail.com\n\n" +
                "📝 يرجى إرفاق المعرف الفريد للجهاز:\n" +
                $"   {machineId}\n\n" +
                "💡 سيتم إرسال مفتاح التفعيل إليك خلال 24 ساعة";

            MessageBox.Show(message, "طلب مفتاح التفعيل",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Label lblTitle, lblSubTitle, lblInfo, lblStatus, lblVersion, lblLicenseInfo;
        private TextBox txtLicenseKey;
        private Button btnActivate, btnExit, btnRequestKey;
        private Panel panelHeader;
        private PictureBox picIcon;

        private void picIcon_Click(object sender, EventArgs e)
        {

        }
    }
}