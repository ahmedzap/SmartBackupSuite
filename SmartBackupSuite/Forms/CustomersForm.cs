using SmartBackupSuite.Helpers;
using SmartBackupSuite.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace SmartBackupSuite.Forms
{
    public partial class CustomersForm : Form
    {
        private List<CustomerLicense> _customers = new List<CustomerLicense>();
        private string _licensesPath;
        private System.ComponentModel.IContainer components = null;

        // عناصر التحكم
        private Label lblTitle;
        private Panel panelHeader;
        private DataGridView dgvCustomers;
        private Panel panelFooter;
        private Button btnRefresh;
        private Button btnCopyKey;
        private Button btnDeleteCustomer;
        private Button btnClose;
        private Label lblTotal;
        private TextBox txtSearch;
        private Label lblSearch;

        public CustomersForm()
        {
            InitializeComponent();
            ApplyTheme();
            _licensesPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Licenses"
            );
            LoadCustomers();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.panelHeader = new Panel();
            this.dgvCustomers = new DataGridView();
            this.panelFooter = new Panel();
            this.btnRefresh = new Button();
            this.btnCopyKey = new Button();
            this.btnDeleteCustomer = new Button();
            this.btnClose = new Button();
            this.lblTotal = new Label();
            this.txtSearch = new TextBox();
            this.lblSearch = new Label();

            // ============================================================
            // CustomersForm
            // ============================================================
            this.Text = "👥 إدارة العملاء - Smart Backup Suite";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(900, 550);
            this.MinimumSize = new Size(900, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(224, 247, 250);
            this.KeyPreview = true;

            // ============================================================
            // panelHeader
            // ============================================================
            this.panelHeader.BackColor = Color.FromArgb(0, 150, 180);
            this.panelHeader.Dock = DockStyle.Top;
            this.panelHeader.Height = 80;
            this.panelHeader.Padding = new Padding(20, 0, 20, 0);

            // lblTitle
            this.lblTitle.Text = "👥 إدارة العملاء";
            this.lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Location = new Point(20, 20);
            this.lblTitle.Size = new Size(250, 35);
            this.lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            // lblTotal
            this.lblTotal.Text = "📊 إجمالي العملاء: 0";
            this.lblTotal.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblTotal.ForeColor = Color.FromArgb(200, 240, 255);
            this.lblTotal.Location = new Point(20, 55);
            this.lblTotal.Size = new Size(200, 20);
            this.lblTotal.TextAlign = ContentAlignment.MiddleLeft;

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblTotal);

            // ============================================================
            // dgvCustomers
            // ============================================================
            this.dgvCustomers.AllowUserToAddRows = false;
            this.dgvCustomers.AllowUserToDeleteRows = false;
            this.dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvCustomers.BackgroundColor = Color.White;
            this.dgvCustomers.BorderStyle = BorderStyle.None;
            this.dgvCustomers.ColumnHeadersHeight = 40;
            this.dgvCustomers.Dock = DockStyle.Fill;
            this.dgvCustomers.Location = new Point(0, 80);
            this.dgvCustomers.Name = "dgvCustomers";
            this.dgvCustomers.ReadOnly = true;
            this.dgvCustomers.RowHeadersVisible = false;
            this.dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvCustomers.Size = new Size(900, 380);
            this.dgvCustomers.TabIndex = 0;
            this.dgvCustomers.CellContentClick += new DataGridViewCellEventHandler(this.dgvCustomers_CellContentClick);

            // ============================================================
            // panelFooter
            // ============================================================
            this.panelFooter.BackColor = Color.FromArgb(236, 240, 241);
            this.panelFooter.Dock = DockStyle.Bottom;
            this.panelFooter.Height = 60;
            this.panelFooter.Padding = new Padding(10);

            // btnRefresh
            this.btnRefresh.Text = "🔄 تحديث";
            this.btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnRefresh.BackColor = Color.FromArgb(52, 152, 219);
            this.btnRefresh.ForeColor = Color.White;
            this.btnRefresh.FlatStyle = FlatStyle.Flat;
            this.btnRefresh.Size = new Size(100, 40);
            this.btnRefresh.Location = new Point(10, 10);
            this.btnRefresh.Click += new EventHandler(this.BtnRefresh_Click);

            // btnCopyKey
            this.btnCopyKey.Text = "📋 نسخ المفتاح";
            this.btnCopyKey.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnCopyKey.BackColor = Color.FromArgb(46, 204, 113);
            this.btnCopyKey.ForeColor = Color.White;
            this.btnCopyKey.FlatStyle = FlatStyle.Flat;
            this.btnCopyKey.Size = new Size(120, 40);
            this.btnCopyKey.Location = new Point(120, 10);
            this.btnCopyKey.Click += new EventHandler(this.BtnCopyKey_Click);

            // btnDeleteCustomer
            this.btnDeleteCustomer.Text = "🗑️ حذف";
            this.btnDeleteCustomer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnDeleteCustomer.BackColor = Color.FromArgb(231, 76, 60);
            this.btnDeleteCustomer.ForeColor = Color.White;
            this.btnDeleteCustomer.FlatStyle = FlatStyle.Flat;
            this.btnDeleteCustomer.Size = new Size(80, 40);
            this.btnDeleteCustomer.Location = new Point(250, 10);
            this.btnDeleteCustomer.Click += new EventHandler(this.BtnDeleteCustomer_Click);

            // btnClose
            this.btnClose.Text = "❌ إغلاق";
            this.btnClose.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnClose.BackColor = Color.FromArgb(149, 165, 166);
            this.btnClose.ForeColor = Color.White;
            this.btnClose.FlatStyle = FlatStyle.Flat;
            this.btnClose.Size = new Size(100, 40);
            this.btnClose.Location = new Point(790, 10);
            this.btnClose.Click += new EventHandler(this.BtnClose_Click);

            // lblSearch
            this.lblSearch.Text = "🔍 بحث:";
            this.lblSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblSearch.ForeColor = Color.FromArgb(40, 40, 40);
            this.lblSearch.Location = new Point(420, 20);
            this.lblSearch.Size = new Size(50, 20);
            this.lblSearch.TextAlign = ContentAlignment.MiddleLeft;

            this.txtSearch.Font = new Font("Segoe UI", 10F);
            this.txtSearch.Location = new Point(475, 16);
            this.txtSearch.Size = new Size(200, 27);
            this.txtSearch.TextChanged += new EventHandler(this.TxtSearch_TextChanged);

            this.panelFooter.Controls.Add(this.btnRefresh);
            this.panelFooter.Controls.Add(this.btnCopyKey);
            this.panelFooter.Controls.Add(this.btnDeleteCustomer);
            this.panelFooter.Controls.Add(this.btnClose);
            this.panelFooter.Controls.Add(this.lblSearch);
            this.panelFooter.Controls.Add(this.txtSearch);

            // ============================================================
            // إضافة العناصر إلى النموذج
            // ============================================================
            this.Controls.Add(this.dgvCustomers);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelFooter);
        }

        private void ApplyTheme()
        {
            // تخصيص الأزرار
            this.btnRefresh.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            this.btnRefresh.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 100, 150);

            this.btnCopyKey.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            this.btnCopyKey.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 130, 70);

            this.btnDeleteCustomer.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 30, 30);
            this.btnDeleteCustomer.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 20, 20);

            this.btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(130, 140, 140);
            this.btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(110, 120, 120);

            // تخصيص DataGridView
            this.dgvCustomers.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            this.dgvCustomers.DefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
            this.dgvCustomers.DefaultCellStyle.BackColor = Color.White;
            this.dgvCustomers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(178, 235, 242);
            this.dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 150, 180);
            this.dgvCustomers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            this.dgvCustomers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            this.dgvCustomers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            // تخصيص خلفية DataGridView
            this.dgvCustomers.RowTemplate.Height = 35;
            this.dgvCustomers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);

            // ✅ إضافة زر "نسخ" في DataGridView
            if (!this.dgvCustomers.Columns.Contains("btnCopy"))
            {
                DataGridViewButtonColumn btnCopy = new DataGridViewButtonColumn();
                btnCopy.Name = "btnCopy";
                btnCopy.HeaderText = "نسخ";
                btnCopy.Text = "📋 نسخ";
                btnCopy.UseColumnTextForButtonValue = true;
                btnCopy.Width = 80;
                this.dgvCustomers.Columns.Add(btnCopy);
            }
        }

        private void LoadCustomers(string searchText = "")
        {
            try
            {
                _customers.Clear();

                if (!Directory.Exists(_licensesPath))
                {
                    Directory.CreateDirectory(_licensesPath);
                    UpdateGrid();
                    return;
                }

                var files = Directory.GetFiles(_licensesPath, "*.json")
                    .Where(f => !Path.GetFileName(f).Contains("generated_licenses"))
                    .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var customer = JsonSerializer.Deserialize<CustomerLicense>(json);
                        if (customer != null)
                        {
                            // ✅ البحث حسب النص
                            if (!string.IsNullOrEmpty(searchText))
                            {
                                string searchLower = searchText.ToLower();
                                string customerName = customer.CustomerName?.ToLower() ?? "";
                                string licenseKey = customer.LicenseKey?.ToLower() ?? "";
                                string machineId = customer.MachineId?.ToLower() ?? "";

                                if (customerName.Contains(searchLower) ||
                                    licenseKey.Contains(searchLower) ||
                                    machineId.Contains(searchLower))
                                {
                                    _customers.Add(customer);
                                }
                            }
                            else
                            {
                                _customers.Add(customer);
                            }
                        }
                    }
                    catch { }
                }

                // ✅ ترتيب حسب تاريخ الإنشاء (الأحدث أولاً)
                _customers = _customers.OrderByDescending(c => c.ActivationDate).ToList();

                UpdateGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateGrid()
        {
            try
            {
                dgvCustomers.DataSource = null;
                dgvCustomers.DataSource = _customers;

                if (dgvCustomers.Columns.Count > 0)
                {
                    // ✅ إخفاء الأعمدة غير الضرورية
                    if (dgvCustomers.Columns.Contains("LicenseKey"))
                        dgvCustomers.Columns["LicenseKey"].Visible = false;
                    if (dgvCustomers.Columns.Contains("Signature"))
                        dgvCustomers.Columns["Signature"].Visible = false;
                    if (dgvCustomers.Columns.Contains("IsActive"))
                        dgvCustomers.Columns["IsActive"].Visible = false;
                    if (dgvCustomers.Columns.Contains("IsDeveloper"))
                        dgvCustomers.Columns["IsDeveloper"].Visible = false;
                    if (dgvCustomers.Columns.Contains("LicenseKeyFormatted"))
                        dgvCustomers.Columns["LicenseKeyFormatted"].Visible = false;

                    // ✅ إعادة تسمية الأعمدة
                    if (dgvCustomers.Columns.Contains("CustomerName"))
                        dgvCustomers.Columns["CustomerName"].HeaderText = "👤 اسم العميل";
                    if (dgvCustomers.Columns.Contains("MachineId"))
                        dgvCustomers.Columns["MachineId"].HeaderText = "🖥️ معرف الجهاز";
                    if (dgvCustomers.Columns.Contains("LicenseType"))
                        dgvCustomers.Columns["LicenseType"].HeaderText = "📋 نوع الترخيص";
                    if (dgvCustomers.Columns.Contains("ActivationDate"))
                        dgvCustomers.Columns["ActivationDate"].HeaderText = "📅 تاريخ التفعيل";
                    if (dgvCustomers.Columns.Contains("ExpiryDate"))
                        dgvCustomers.Columns["ExpiryDate"].HeaderText = "📅 تاريخ الانتهاء";
                    if (dgvCustomers.Columns.Contains("BoundMachineId"))
                        dgvCustomers.Columns["BoundMachineId"].HeaderText = "🔒 مقيد بجهاز";

                    // ✅ تنسيق التواريخ
                    if (dgvCustomers.Columns.Contains("ActivationDate"))
                    {
                        dgvCustomers.Columns["ActivationDate"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                    }
                    if (dgvCustomers.Columns.Contains("ExpiryDate"))
                    {
                        dgvCustomers.Columns["ExpiryDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
                    }

                    // ✅ تحويل نوع الترخيص إلى نص عربي
                    if (dgvCustomers.Columns.Contains("LicenseType"))
                    {
                        dgvCustomers.Columns["LicenseType"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }

                // ✅ تحديث العدد الإجمالي
                lblTotal.Text = $"📊 إجمالي العملاء: {_customers.Count}";
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Customers", "Error", $"خطأ في تحديث القائمة: {ex.Message}");
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadCustomers(txtSearch.Text.Trim());
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            LoadCustomers();
        }

        private void BtnCopyKey_Click(object sender, EventArgs e)
        {
            CopySelectedKey();
        }

        private void dgvCustomers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // ✅ عند الضغط على زر "نسخ" في العمود
            if (e.RowIndex >= 0 && dgvCustomers.Columns[e.ColumnIndex].Name == "btnCopy")
            {
                var customer = dgvCustomers.Rows[e.RowIndex].DataBoundItem as CustomerLicense;
                if (customer != null && !string.IsNullOrEmpty(customer.LicenseKey))
                {
                    string formattedKey = FormatLicenseKey(customer.LicenseKey);
                    Clipboard.SetText(formattedKey);
                    MessageBox.Show($"✅ تم نسخ مفتاح الترخيص:\n\n{formattedKey}",
                        "نسخ المفتاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void CopySelectedKey()
        {
            if (dgvCustomers.SelectedRows.Count > 0)
            {
                var customer = dgvCustomers.SelectedRows[0].DataBoundItem as CustomerLicense;
                if (customer != null && !string.IsNullOrEmpty(customer.LicenseKey))
                {
                    string formattedKey = FormatLicenseKey(customer.LicenseKey);
                    Clipboard.SetText(formattedKey);
                    MessageBox.Show($"✅ تم نسخ مفتاح الترخيص:\n\n{formattedKey}",
                        "نسخ المفتاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("⚠️ الرجاء اختيار عميل لنسخ مفتاحه", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string FormatLicenseKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (key.Length == 25)
            {
                string formatted = "";
                for (int i = 0; i < key.Length; i += 5)
                {
                    if (i > 0) formatted += "-";
                    formatted += key.Substring(i, Math.Min(5, key.Length - i));
                }
                return formatted;
            }
            return key;
        }

        private void BtnDeleteCustomer_Click(object sender, EventArgs e)
        {
            if (dgvCustomers.SelectedRows.Count > 0)
            {
                var customer = dgvCustomers.SelectedRows[0].DataBoundItem as CustomerLicense;
                if (customer != null)
                {
                    if (MessageBox.Show($"هل أنت متأكد من حذف عميل '{customer.CustomerName}'؟\nسيتم حذف ملف الترخيص بالكامل.",
                        "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try
                        {
                            string filePath = Path.Combine(_licensesPath, $"{customer.LicenseKey}.json");
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                                LoadCustomers(txtSearch.Text.Trim());
                                MessageBox.Show($"✅ تم حذف عميل '{customer.CustomerName}' بنجاح",
                                    "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"❌ خطأ في حذف العميل: {ex.Message}", "خطأ",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("⚠️ الرجاء اختيار عميل للحذف", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // ✅ Ctrl+F للبحث السريع
            if (keyData == (Keys.Control | Keys.F))
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
                return true;
            }
            // ✅ ESC لإغلاق النموذج
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
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

    // ============================================================
    // ✅ هيكل بيانات العميل (مطابق لملف JSON)
    // ============================================================
    public class CustomerLicense
    {
        public string LicenseKey { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string MachineId { get; set; } = "";
        public string LicenseType { get; set; } = "Trial";
        public DateTime ActivationDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);
        public string BoundMachineId { get; set; } = "";
        public string Signature { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public bool IsDeveloper { get; set; } = false;
    }
}