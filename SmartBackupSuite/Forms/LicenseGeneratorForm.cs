using SmartBackupSuite.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SmartBackupSuite.Forms
{
    public partial class LicenseGeneratorForm : Form
    {
        public LicenseGeneratorForm()
        {
            InitializeComponent();
            ApplyTheme();
            // ✅ جلب معرف الجهاز تلقائياً (قابل للتعديل)
            txtMachineId.Text = LicenseHelper.GetMachineId();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblDeveloperInfo = new Label();
            lblCustomerName = new Label();
            txtCustomerName = new TextBox();
            lblMachineId = new Label();
            txtMachineId = new TextBox();
            btnGetMachineId = new Button();
            lblLicenseType = new Label();
            cmbLicenseType = new ComboBox();
            lblDuration = new Label();
            numDuration = new NumericUpDown();
            btnGenerate = new Button();
            btnCopy = new Button();
            btnExport = new Button();
            btnClose = new Button();
            txtLicenseKey = new TextBox();
            lblLicenseInfo = new Label();
            panelResult = new Panel();
            lblMachineIdNote = new Label();
            ((System.ComponentModel.ISupportInitialize)numDuration).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 150, 180);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(600, 40);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "🔑 مولد مفاتيح الترخيص";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDeveloperInfo
            // 
            lblDeveloperInfo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDeveloperInfo.ForeColor = Color.FromArgb(155, 89, 182);
            lblDeveloperInfo.Location = new Point(20, 55);
            lblDeveloperInfo.Name = "lblDeveloperInfo";
            lblDeveloperInfo.Size = new Size(600, 30);
            lblDeveloperInfo.TabIndex = 1;
            // lblDeveloperInfo.Text = "🔐 مفتاح المطور: DEVEL-OPER-KEY20-24FUL-LVERS";
            lblDeveloperInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCustomerName
            // 
            lblCustomerName.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCustomerName.Location = new Point(30, 110);
            lblCustomerName.Name = "lblCustomerName";
            lblCustomerName.Size = new Size(130, 30);
            lblCustomerName.TabIndex = 2;
            lblCustomerName.Text = "👤 اسم العميل:";
            lblCustomerName.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtCustomerName
            // 
            txtCustomerName.Font = new Font("Segoe UI", 10F);
            txtCustomerName.Location = new Point(170, 107);
            txtCustomerName.Name = "txtCustomerName";
            txtCustomerName.Size = new Size(350, 25);
            txtCustomerName.TabIndex = 3;
            // 
            // lblMachineId
            // 
            lblMachineId.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblMachineId.Location = new Point(30, 150);
            lblMachineId.Name = "lblMachineId";
            lblMachineId.Size = new Size(130, 30);
            lblMachineId.TabIndex = 4;
            lblMachineId.Text = "🖥️ معرف الجهاز:";
            lblMachineId.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtMachineId
            // 
            txtMachineId.BackColor = Color.White;
            txtMachineId.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            txtMachineId.ForeColor = Color.FromArgb(0, 150, 180);
            txtMachineId.Location = new Point(170, 147);
            txtMachineId.Name = "txtMachineId";
            txtMachineId.Size = new Size(270, 25);
            txtMachineId.TabIndex = 5;
            // 
            // btnGetMachineId
            // 
            btnGetMachineId.BackColor = Color.FromArgb(52, 152, 219);
            btnGetMachineId.FlatStyle = FlatStyle.Flat;
            btnGetMachineId.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnGetMachineId.ForeColor = Color.White;
            btnGetMachineId.Location = new Point(450, 147);
            btnGetMachineId.Name = "btnGetMachineId";
            btnGetMachineId.Size = new Size(100, 27);
            btnGetMachineId.TabIndex = 6;
            btnGetMachineId.Text = "🔄 جلب المعرف";
            btnGetMachineId.UseVisualStyleBackColor = false;
            btnGetMachineId.Click += BtnGetMachineId_Click;
            // 
            // lblLicenseType
            // 
            lblLicenseType.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblLicenseType.Location = new Point(30, 215);
            lblLicenseType.Name = "lblLicenseType";
            lblLicenseType.Size = new Size(130, 30);
            lblLicenseType.TabIndex = 8;
            lblLicenseType.Text = "📋 نوع الترخيص:";
            lblLicenseType.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbLicenseType
            // 
            cmbLicenseType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLicenseType.Font = new Font("Segoe UI", 10F);
            cmbLicenseType.Items.AddRange(new object[] { "Trial", "Monthly", "Yearly", "Lifetime" });
            cmbLicenseType.Location = new Point(170, 212);
            cmbLicenseType.Name = "cmbLicenseType";
            cmbLicenseType.Size = new Size(150, 25);
            cmbLicenseType.TabIndex = 9;
            // 
            // lblDuration
            // 
            lblDuration.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDuration.Location = new Point(350, 215);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new Size(100, 30);
            lblDuration.TabIndex = 10;
            lblDuration.Text = "📅 المدة (أيام):";
            lblDuration.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numDuration
            // 
            numDuration.Font = new Font("Segoe UI", 10F);
            numDuration.Location = new Point(460, 212);
            numDuration.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            numDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDuration.Name = "numDuration";
            numDuration.Size = new Size(80, 25);
            numDuration.TabIndex = 11;
            numDuration.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // btnGenerate
            // 
            btnGenerate.BackColor = Color.FromArgb(46, 204, 113);
            btnGenerate.FlatStyle = FlatStyle.Flat;
            btnGenerate.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnGenerate.ForeColor = Color.White;
            btnGenerate.Location = new Point(220, 260);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(180, 45);
            btnGenerate.TabIndex = 12;
            btnGenerate.Text = "🔄 إنشاء مفتاح";
            btnGenerate.UseVisualStyleBackColor = false;
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // btnCopy
            // 
            btnCopy.BackColor = Color.FromArgb(52, 152, 219);
            btnCopy.FlatStyle = FlatStyle.Flat;
            btnCopy.ForeColor = Color.White;
            btnCopy.Location = new Point(200, 549);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(100, 30);
            btnCopy.TabIndex = 16;
            btnCopy.Text = "📋 نسخ";
            btnCopy.UseVisualStyleBackColor = false;
            btnCopy.Visible = false;
            btnCopy.Click += BtnCopy_Click;
            // 
            // btnExport
            // 
            btnExport.BackColor = Color.FromArgb(155, 89, 182);
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.ForeColor = Color.White;
            btnExport.Location = new Point(310, 549);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(100, 30);
            btnExport.TabIndex = 17;
            btnExport.Text = "💾 تصدير";
            btnExport.UseVisualStyleBackColor = false;
            btnExport.Visible = false;
            btnExport.Click += BtnExport_Click;
            // 
            // btnClose
            // 
            btnClose.BackColor = Color.FromArgb(200, 50, 50);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(420, 549);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(100, 30);
            btnClose.TabIndex = 18;
            btnClose.Text = "❌ إغلاق";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Visible = false;
            btnClose.Click += BtnClose_Click;
            // 
            // txtLicenseKey
            // 
            txtLicenseKey.BackColor = Color.White;
            txtLicenseKey.Font = new Font("Consolas", 14F, FontStyle.Bold);
            txtLicenseKey.ForeColor = Color.FromArgb(0, 150, 180);
            txtLicenseKey.Location = new Point(45, 340);
            txtLicenseKey.Name = "txtLicenseKey";
            txtLicenseKey.ReadOnly = true;
            txtLicenseKey.Size = new Size(550, 29);
            txtLicenseKey.TabIndex = 14;
            txtLicenseKey.TextAlign = HorizontalAlignment.Center;
            // 
            // lblLicenseInfo
            // 
            lblLicenseInfo.Font = new Font("Segoe UI", 9F);
            lblLicenseInfo.ForeColor = Color.FromArgb(40, 40, 40);
            lblLicenseInfo.Location = new Point(45, 380);
            lblLicenseInfo.Name = "lblLicenseInfo";
            lblLicenseInfo.Size = new Size(550, 45);
            lblLicenseInfo.TabIndex = 15;
            lblLicenseInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelResult
            // 
            panelResult.BackColor = Color.FromArgb(240, 240, 240);
            panelResult.BorderStyle = BorderStyle.FixedSingle;
            panelResult.Location = new Point(30, 380);
            panelResult.Name = "panelResult";
            panelResult.Size = new Size(580, 90);
            panelResult.TabIndex = 13;
            panelResult.Visible = false;
            // 
            // lblMachineIdNote
            // 
            lblMachineIdNote.Font = new Font("Segoe UI", 8.5F);
            lblMachineIdNote.ForeColor = Color.FromArgb(100, 100, 100);
            lblMachineIdNote.Location = new Point(170, 180);
            lblMachineIdNote.Name = "lblMachineIdNote";
            lblMachineIdNote.Size = new Size(380, 20);
            lblMachineIdNote.TabIndex = 7;
            lblMachineIdNote.Text = "💡 يمكنك ترك معرف الجهاز فارغاً لإنشاء مفتاح عام (غير مقيد بجهاز)";
            // 
            // LicenseGeneratorForm
            // 
            BackColor = Color.FromArgb(224, 247, 250);
            ClientSize = new Size(634, 591);
            Controls.Add(lblTitle);
            Controls.Add(lblDeveloperInfo);
            Controls.Add(lblCustomerName);
            Controls.Add(txtCustomerName);
            Controls.Add(lblMachineId);
            Controls.Add(txtMachineId);
            Controls.Add(btnGetMachineId);
            Controls.Add(lblMachineIdNote);
            Controls.Add(lblLicenseType);
            Controls.Add(cmbLicenseType);
            Controls.Add(lblDuration);
            Controls.Add(numDuration);
            Controls.Add(btnGenerate);
            Controls.Add(panelResult);
            Controls.Add(txtLicenseKey);
            Controls.Add(lblLicenseInfo);
            Controls.Add(btnCopy);
            Controls.Add(btnExport);
            Controls.Add(btnClose);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LicenseGeneratorForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "🔑 مولد مفاتيح الترخيص - Smart Backup Suite";
            ((System.ComponentModel.ISupportInitialize)numDuration).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void ApplyTheme()
        {
            this.btnGenerate.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            this.btnGenerate.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 130, 70);
            this.btnGetMachineId.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            this.btnGetMachineId.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 100, 150);
            this.btnCopy.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            this.btnCopy.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 100, 150);
            this.btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(142, 68, 173);
            this.btnExport.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 50, 150);
            this.btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 30, 30);
            this.btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 20, 20);
        }

        private void BtnGetMachineId_Click(object sender, EventArgs e)
        {
            try
            {
                string machineId = LicenseHelper.GetMachineId();
                txtMachineId.Text = machineId;
                MessageBox.Show($"✅ تم جلب معرف الجهاز:\n\n{machineId}",
                    "معرف الجهاز", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في جلب معرف الجهاز:\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                string customerName = txtCustomerName.Text.Trim();
                string machineId = txtMachineId.Text.Trim();

                if (string.IsNullOrEmpty(customerName))
                {
                    MessageBox.Show("⚠️ الرجاء إدخال اسم العميل",
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCustomerName.Focus();
                    return;
                }

                string licenseType = cmbLicenseType.SelectedItem?.ToString() ?? "Trial";
                int duration = (int)numDuration.Value;

                string licenseKey = LicenseHelper.GenerateLicenseKey(customerName, machineId, licenseType, duration);

                txtLicenseKey.Text = licenseKey;
                panelResult.Visible = true;
                btnCopy.Visible = true;
                btnExport.Visible = true;
                btnClose.Visible = true;

                string typeName = licenseType switch
                {
                    "Trial" => "تجريبي",
                    "Monthly" => "شهري",
                    "Yearly" => "سنوي",
                    "Lifetime" => "دائم",
                    _ => licenseType
                };

                DateTime expiryDate = licenseType switch
                {
                    "Trial" => DateTime.Now.AddDays(duration),
                    "Monthly" => DateTime.Now.AddMonths(1),
                    "Yearly" => DateTime.Now.AddYears(1),
                    "Lifetime" => DateTime.Now.AddYears(100),
                    _ => DateTime.Now.AddDays(duration)
                };

                string machineIdDisplay = string.IsNullOrEmpty(machineId) ? "غير مقيد" : machineId;

                lblLicenseInfo.Text =
                    $"👤 {customerName} | 🖥️ {machineIdDisplay}\n" +
                    $"📋 {typeName} | 📅 ينتهي في {expiryDate:yyyy-MM-dd}";

                LogLicenseGeneration(licenseKey, customerName, machineId, licenseType, expiryDate);

                MessageBox.Show($"✅ تم إنشاء مفتاح الترخيص بنجاح!\n\n" +
                    $"🔑 {licenseKey}\n\n" +
                    $"👤 {customerName}\n" +
                    $"🖥️ {machineIdDisplay}\n" +
                    $"📋 {typeName}\n" +
                    $"📅 ينتهي في {expiryDate:yyyy-MM-dd}",
                    "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في إنشاء المفتاح:\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtLicenseKey.Text))
            {
                Clipboard.SetText(txtLicenseKey.Text);
                MessageBox.Show("✅ تم نسخ المفتاح إلى الحافظة",
                    "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|All files (*.*)|*.*";
                    saveFileDialog.Title = "تصدير المفاتيح";
                    saveFileDialog.FileName = $"Licenses_{DateTime.Now:yyyyMMdd}";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string machineId = txtMachineId.Text.Trim();
                        string machineIdDisplay = string.IsNullOrEmpty(machineId) ? "غير مقيد" : machineId;

                        string exportData =
                            $"LicenseKey,CustomerName,MachineID,LicenseType,ExpiryDate\n" +
                            $"{txtLicenseKey.Text},{txtCustomerName.Text},{machineIdDisplay}," +
                            $"{cmbLicenseType.SelectedItem},{DateTime.Now.AddDays((int)numDuration.Value):yyyy-MM-dd}";

                        File.WriteAllText(saveFileDialog.FileName, exportData);

                        MessageBox.Show($"✅ تم التصدير بنجاح:\n{saveFileDialog.FileName}",
                            "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في التصدير: {ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LogLicenseGeneration(string key, string name, string machineId, string type, DateTime expiry)
        {
            try
            {
                string logPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Licenses",
                    "generated_licenses.log"
                );

                string directory = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                string machineIdDisplay = string.IsNullOrEmpty(machineId) ? "غير مقيد" : machineId;

                string logEntry =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"Key: {key} | " +
                    $"Customer: {name} | " +
                    $"MachineID: {machineIdDisplay} | " +
                    $"Type: {type} | " +
                    $"Expiry: {expiry:yyyy-MM-dd}";

                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { }
        }

        // عناصر التحكم
        private Label lblTitle, lblDeveloperInfo, lblCustomerName, lblMachineId, lblMachineIdNote;
        private Label lblLicenseType, lblDuration, lblLicenseInfo;
        private TextBox txtCustomerName, txtMachineId, txtLicenseKey;
        private ComboBox cmbLicenseType;
        private NumericUpDown numDuration;
        private Button btnGenerate, btnGetMachineId, btnCopy, btnExport, btnClose;
        private Panel panelResult;
    }
}