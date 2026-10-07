using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Linq;
using System.ComponentModel;

namespace AutoEmailer
{
    public partial class Form1 : MaterialForm
    {
        private ProfileManager _profileManager;
        private BindingList<RptProfile> _profilesBinding;

        private MaterialTextBox txtInputFolder;
        private MaterialTextBox txtOutputFolder;
        private MaterialButton btnStart;
        private MaterialButton btnStop;
        private RichTextBox rtbLogs;
        
        private ListBox lstProfiles;
        private MaterialButton btnAddProfile;
        private MaterialButton btnDeleteProfile;
        
        private MaterialTextBox txtProfileId;
        private MaterialTextBox txtCC;
        private MaterialTextBox txtBCC;
        private MaterialTextBox txtHost;
        private MaterialTextBox txtPort;
        private MaterialTextBox txtUsername;
        private MaterialTextBox txtPassword;
        private MaterialSwitch chkUseSsl;
        private MaterialComboBox cmbCertMode;
        private MaterialTextBox txtThumbprint;
        private MaterialButton btnFetchCert;
        private MaterialCheckbox chkEnableEncryption;
        private MaterialTextBox txtFromName;
        private MaterialTextBox txtFromAddress;
        private TextBox rtbHtmlTemplate;
        
        private MaterialButton btnTestConnection;
        private MaterialButton btnSaveProfiles;
        private WebBrowser webPreview;
        private MaterialTextBox txtStaticAttachment;
        private MaterialButton btnBrowseStaticAttachment;
        
        // Log Dashboard Controls
        private MaterialComboBox cmbLogDates;
        private DataGridView dgvLogs;
        private MaterialTextBox txtFilterEmail;
        private MaterialTextBox txtFilterLevel;
        private MaterialButton btnLoadLogs;

        private Label lblLicenseStatus;
        private MaterialButton btnUploadLicense;

        private Label lblServiceStatus;
        private System.Windows.Forms.Timer _statusTimer;

        public Form1()
        {
            _profileManager = new ProfileManager();
            var loaded = _profileManager.LoadProfiles();
            if (!loaded.Any())
            {
                loaded.Add(new RptProfile { Id = "INV", SmtpConfig = new SmtpConfig { Host = "smtp.example.com", Port = 587 }, HtmlTemplate = "<h1>Invoice Attached</h1>" });
                _profileManager.SaveProfiles(loaded);
            }
            _profilesBinding = new BindingList<RptProfile>(loaded);

            InitializeComponent();
            this.Text = "HipoExchange";
            LoadSettings();

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);
        }

        private void InitializeComponent()
        {
            this.Text = "Hipodoc - Dynamic Email Engine";
            this.Size = new Size(1000, 700);

            var tabControl = new MaterialTabControl
            {
                Dock = DockStyle.Fill
            };
            
            // Link TabControl to the Drawer
            this.DrawerTabControl = tabControl;

            // --- TAB 1: DASHBOARD ---
            var tabDashboard = new TabPage("Dashboard");
            tabDashboard.BackColor = Color.White;

            var lblInput = new Label { Text = "Input Folder (X3 Output):", Location = new Point(20, 20), Width = 150 };
            txtInputFolder = new MaterialTextBox { Location = new Point(180, 10), Width = 400 };

            var lblOutput = new Label { Text = "Output/Archive Folder:", Location = new Point(20, 70), Width = 150 };
            txtOutputFolder = new MaterialTextBox { Location = new Point(180, 60), Width = 400 };

            btnStart = new MaterialButton { Text = "Start Engine", Location = new Point(20, 130), Width = 120 };
            btnStart.Click += BtnStart_Click;
            btnStop = new MaterialButton { Text = "Stop Engine", Location = new Point(160, 130), Width = 120 };
            btnStop.Click += BtnStop_Click;

            lblServiceStatus = new Label { Text = "SERVICE STATUS UNKNOWN", Location = new Point(300, 140), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };

            var lblLogs = new Label { Text = "Processing Logs:", Location = new Point(20, 190) };
            rtbLogs = new RichTextBox { Location = new Point(20, 220), Width = 700, Height = 350, ReadOnly = true, BackColor = Color.WhiteSmoke };

            tabDashboard.Controls.Add(lblInput); tabDashboard.Controls.Add(txtInputFolder);
            tabDashboard.Controls.Add(lblOutput); tabDashboard.Controls.Add(txtOutputFolder);
            tabDashboard.Controls.Add(btnStart); tabDashboard.Controls.Add(btnStop);
            tabDashboard.Controls.Add(lblServiceStatus);
            tabDashboard.Controls.Add(lblLogs); tabDashboard.Controls.Add(rtbLogs);

            _statusTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();

            // --- TAB 2: PROFILES ---
            var tabProfiles = new TabPage("RPT Profiles");
            tabProfiles.BackColor = Color.White;

            var lblProfilesInfo = new Label 
            { 
                Text = "Manage your dynamic RPT email profiles here. The 'Id' field matches the TYPE: tag in the PDF.", 
                Location = new Point(20, 10), 
                Width = 700 
            };

            var splitContainer = new SplitContainer
            {
                Location = new Point(20, 40),
                Width = 900,
                Height = 500,
                SplitterDistance = 200,
                IsSplitterFixed = false
            };

            lstProfiles = new ListBox { Dock = DockStyle.Fill, DataSource = _profilesBinding, DisplayMember = "Id", Font = new Font("Segoe UI", 11) };
            lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;
            
            var pnlListButtons = new Panel { Dock = DockStyle.Bottom, Height = 80 };
            btnAddProfile = new MaterialButton { Text = "Add Profile", Location = new Point(10, 5), Width = 180 };
            btnAddProfile.Click += BtnAddProfile_Click;

            btnDeleteProfile = new MaterialButton { Text = "Delete Profile", Location = new Point(10, 45), Width = 180 };
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            
            pnlListButtons.Controls.Add(btnAddProfile);
            pnlListButtons.Controls.Add(btnDeleteProfile);

            splitContainer.Panel1.Controls.Add(lstProfiles);
            splitContainer.Panel1.Controls.Add(pnlListButtons);

            var pnlDetails = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            
            // Clean 2-Column Grid Layout (Col 1: X=10, Col 2: X=350)
            // Priority 1: ID
            txtProfileId = new MaterialTextBox { Hint = "Profile ID", Location = new Point(10, 10), Width = 660 };
            
            // Priority 2: Sender Info
            txtFromName = new MaterialTextBox { Hint = "From Name", Location = new Point(10, 70), Width = 320 };
            txtFromAddress = new MaterialTextBox { Hint = "From Address", Location = new Point(350, 70), Width = 320 };
            
            // Priority 3: SMTP Server
            txtHost = new MaterialTextBox { Hint = "SMTP Host", Location = new Point(10, 130), Width = 320 };
            txtPort = new MaterialTextBox { Hint = "SMTP Port", Location = new Point(350, 130), Width = 320 };
            
            // Priority 4: SMTP Auth
            txtUsername = new MaterialTextBox { Hint = "SMTP Username", Location = new Point(10, 190), Width = 320 };
            txtPassword = new MaterialTextBox { Hint = "SMTP Password", Password = true, Location = new Point(350, 190), Width = 320 };
            
            // Priority 5: Security Toggles (Using Checkboxes with AutoSize=true and fixed widths to prevent clipping)
            chkUseSsl = new MaterialSwitch { Text = "Use SSL", Location = new Point(10, 250), AutoSize = false, Width = 150 };
            chkEnableEncryption = new MaterialCheckbox { Text = "Enable PDF Encryption", Location = new Point(350, 250), AutoSize = false, Width = 250 };
            
            // Priority 6: Extra Recipients
            // Certificate trust row
            cmbCertMode = new MaterialComboBox { Hint = "Certificate Trust", Location = new Point(10, 310), Width = 200 };
            cmbCertMode.Items.AddRange(new object[] { "System (Windows store)", "Pinned thumbprint", "Allow untrusted (insecure)" });
            cmbCertMode.SelectedIndex = 0;
            btnFetchCert = new MaterialButton { Text = "Fetch & Trust", Location = new Point(220, 315), AutoSize = true };
            btnFetchCert.Click += BtnFetchCert_Click;
            txtThumbprint = new MaterialTextBox { Hint = "Pinned SHA-256 thumbprint", Location = new Point(350, 310), Width = 320 };
            
            // Priority 6: Extra Recipients
            txtCC = new MaterialTextBox { Hint = "CC Emails (; separated)", Location = new Point(10, 370), Width = 320 };
            txtBCC = new MaterialTextBox { Hint = "BCC Emails (; separated)", Location = new Point(350, 370), Width = 320 };
            
            // Priority 7: Attachments
            txtStaticAttachment = new MaterialTextBox { Hint = "Static Attachment (PDF)", Location = new Point(10, 430), Width = 520 };
            btnBrowseStaticAttachment = new MaterialButton { Text = "Browse", Location = new Point(540, 430) };
            btnBrowseStaticAttachment.Click += (s, e) => {
                using (var ofd = new OpenFileDialog { Filter = "PDF Files|*.pdf", Title = "Select Static Attachment" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                        txtStaticAttachment.Text = ofd.FileName;
                }
            };

            // Priority 8: HTML Template
            var lblHtml = new Label { Text = "HTML Template:", Location = new Point(10, 490), AutoSize = true };
            rtbHtmlTemplate = new TextBox { Multiline = true, Location = new Point(10, 515), Width = 320, Height = 150, ScrollBars = ScrollBars.Vertical };
            
            var lblPreview = new Label { Text = "Preview:", Location = new Point(350, 490), AutoSize = true };
            webPreview = new WebBrowser { Location = new Point(350, 515), Width = 320, Height = 150 };
            
            rtbHtmlTemplate.TextChanged += (s, e) => {
                if (webPreview.Document != null)
                {
                    webPreview.Document.OpenNew(true);
                    webPreview.Document.Write(rtbHtmlTemplate.Text);
                }
                else
                {
                    webPreview.DocumentText = rtbHtmlTemplate.Text;
                }
            };
            
            pnlDetails.Controls.Add(txtProfileId);
            pnlDetails.Controls.Add(txtCC);
            pnlDetails.Controls.Add(txtBCC);
            pnlDetails.Controls.Add(txtHost);
            pnlDetails.Controls.Add(txtPort);
            pnlDetails.Controls.Add(txtUsername);
            pnlDetails.Controls.Add(txtPassword);
            pnlDetails.Controls.Add(chkUseSsl);
            pnlDetails.Controls.Add(cmbCertMode);
            pnlDetails.Controls.Add(btnFetchCert);
            pnlDetails.Controls.Add(txtThumbprint);
            pnlDetails.Controls.Add(chkEnableEncryption);
            pnlDetails.Controls.Add(txtFromName);
            pnlDetails.Controls.Add(txtFromAddress);
            pnlDetails.Controls.Add(txtStaticAttachment);
            pnlDetails.Controls.Add(btnBrowseStaticAttachment);
            pnlDetails.Controls.Add(lblHtml);
            pnlDetails.Controls.Add(rtbHtmlTemplate);
            pnlDetails.Controls.Add(lblPreview);
            pnlDetails.Controls.Add(webPreview);

            splitContainer.Panel2.Controls.Add(pnlDetails);

            btnTestConnection = new MaterialButton { Text = "Test SMTP Connection", Location = new Point(20, 560) };
            btnTestConnection.Click += BtnTestConnection_Click;

            btnSaveProfiles = new MaterialButton { Text = "Save Profiles", Location = new Point(260, 560) };
            btnSaveProfiles.Click += BtnSaveProfiles_Click;

            tabProfiles.Controls.Add(lblProfilesInfo);
            tabProfiles.Controls.Add(splitContainer);
            tabProfiles.Controls.Add(btnTestConnection);
            tabProfiles.Controls.Add(btnSaveProfiles);

            // --- TAB 3: LOG DASHBOARD ---
            var tabLogDashboard = new TabPage("Log Dashboard");
            tabLogDashboard.BackColor = Color.White;
            
            var lblSelectDate = new Label { Text = "Select Date:", Location = new Point(20, 20), AutoSize = true };
            cmbLogDates = new MaterialComboBox { Location = new Point(110, 10), Width = 200 };
            btnLoadLogs = new MaterialButton { Text = "Load Logs", Location = new Point(320, 15) };
            btnLoadLogs.Click += BtnLoadLogs_Click;
            
            var lblFilterEmail = new Label { Text = "Filter Message/Email:", Location = new Point(440, 20), AutoSize = true };
            txtFilterEmail = new MaterialTextBox { Location = new Point(580, 10), Width = 150 };
            txtFilterEmail.TextChanged += FilterLogs;
            
            var lblFilterLevel = new Label { Text = "Filter Level:", Location = new Point(740, 20), AutoSize = true };
            txtFilterLevel = new MaterialTextBox { Location = new Point(820, 10), Width = 100 };
            txtFilterLevel.TextChanged += FilterLogs;

            dgvLogs = new DataGridView 
            { 
                Location = new Point(20, 70), 
                Width = 920, 
                Height = 500,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };

            tabLogDashboard.Controls.Add(lblSelectDate);
            tabLogDashboard.Controls.Add(cmbLogDates);
            tabLogDashboard.Controls.Add(btnLoadLogs);
            tabLogDashboard.Controls.Add(lblFilterEmail);
            tabLogDashboard.Controls.Add(txtFilterEmail);
            tabLogDashboard.Controls.Add(lblFilterLevel);
            tabLogDashboard.Controls.Add(txtFilterLevel);
            tabLogDashboard.Controls.Add(dgvLogs);
            
            // --- TAB 4: LICENSING ---
            var tabLicensing = new TabPage("Licensing");
            tabLicensing.BackColor = Color.White;
            
            lblLicenseStatus = new Label { Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 16) };
            
            btnUploadLicense = new MaterialButton { Text = "Upload License File", Location = new Point(20, 80) };
            btnUploadLicense.Click += BtnUploadLicense_Click;
            
            tabLicensing.Controls.Add(lblLicenseStatus);
            tabLicensing.Controls.Add(btnUploadLicense);
            UpdateLicenseStatus();

            // Add tabs to control
            tabControl.TabPages.Add(tabDashboard);
            tabControl.TabPages.Add(tabProfiles);
            tabControl.TabPages.Add(tabLogDashboard);
            tabControl.TabPages.Add(tabLicensing);

            this.Controls.Add(tabControl);
            
            LoadLogDates();
        }

        private List<LogEntry> _currentLogs = new List<LogEntry>();

        private void LoadLogDates()
        {
            cmbLogDates.Items.Clear();
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, "log_*.json").Select(Path.GetFileNameWithoutExtension)
                                     .Select(f => f.Replace("log_", "")).OrderByDescending(d => d);
                foreach (var date in files)
                {
                    cmbLogDates.Items.Add(date);
                }
                if (cmbLogDates.Items.Count > 0)
                {
                    cmbLogDates.SelectedIndex = 0;
                }
            }
        }

        private void BtnLoadLogs_Click(object sender, EventArgs e)
        {
            if (cmbLogDates.SelectedItem == null) return;
            string dateStr = cmbLogDates.SelectedItem.ToString();
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", $"log_{dateStr}.json");
            
            if (File.Exists(logFile))
            {
                try
                {
                    string content = File.ReadAllText(logFile);
                    _currentLogs = System.Text.Json.JsonSerializer.Deserialize<List<LogEntry>>(content) ?? new List<LogEntry>();
                    DisplayLogs(_currentLogs);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading logs: {ex.Message}");
                }
            }
            else
            {
                _currentLogs.Clear();
                DisplayLogs(_currentLogs);
            }
        }

        private void FilterLogs(object sender, EventArgs e)
        {
            var filtered = _currentLogs.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(txtFilterEmail.Text))
            {
                filtered = filtered.Where(l => l.Message.Contains(txtFilterEmail.Text, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(txtFilterLevel.Text))
            {
                filtered = filtered.Where(l => l.Level.Contains(txtFilterLevel.Text, StringComparison.OrdinalIgnoreCase));
            }
            DisplayLogs(filtered.ToList());
        }

        private void DisplayLogs(List<LogEntry> logs)
        {
            dgvLogs.DataSource = null;
            dgvLogs.DataSource = logs;
        }

        private void LstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is RptProfile p)
            {
                txtProfileId.Text = p.Id;
                txtCC.Text = p.CC;
                txtBCC.Text = p.BCC;
                txtHost.Text = p.SmtpConfig.Host;
                txtPort.Text = p.SmtpConfig.Port.ToString();
                txtUsername.Text = p.SmtpConfig.Username;
                txtPassword.Text = p.SmtpConfig.Password;
                chkUseSsl.Checked = p.SmtpConfig.UseSsl;
                cmbCertMode.SelectedIndex = (int)p.SmtpConfig.CertTrustMode;
                txtThumbprint.Text = p.SmtpConfig.PinnedThumbprint ?? "";
                chkEnableEncryption.Checked = p.EnableEncryption;
                txtFromName.Text = p.SmtpConfig.FromName;
                txtFromAddress.Text = p.SmtpConfig.FromAddress;
                txtStaticAttachment.Text = p.StaticAttachmentPath;
                rtbHtmlTemplate.Text = p.HtmlTemplate;
            }
        }

        private void BtnAddProfile_Click(object sender, EventArgs e)
        {
            var newProfile = new RptProfile { Id = "NEW", SmtpConfig = new SmtpConfig() };
            _profilesBinding.Add(newProfile);
            lstProfiles.SelectedItem = newProfile;
        }

        private void BtnDeleteProfile_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is RptProfile p)
            {
                var confirmResult = MessageBox.Show($"Are you sure you want to delete profile {p.Id}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirmResult == DialogResult.Yes)
                {
                    _profilesBinding.Remove(p);
                    _profileManager.SaveProfiles(_profilesBinding.ToList());
                    MessageBox.Show("Profile deleted and changes saved.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private SmtpConfig BuildTrustConfigFromForm()
        {
            return new SmtpConfig
            {
                CertTrustMode = (CertTrustMode)Math.Max(0, cmbCertMode.SelectedIndex),
                PinnedThumbprint = txtThumbprint.Text
            };
        }

        private void BtnFetchCert_Click(object sender, EventArgs e)
        {
            try
            {
                int.TryParse(txtPort.Text, out int port);
                var info = SmtpCertValidator.FetchCertificate(txtHost.Text, port, chkUseSsl.Checked);
                var msg = $"Pin this {(info.IsCa ? "CA (root) certificate" : "server certificate")}?\n\n" +
                          $"Subject: {info.Subject}\nIssuer: {info.Issuer}\nExpires: {info.NotAfter:yyyy-MM-dd}\n" +
                          $"SHA-256: {info.Thumbprint}\n\nVerify this thumbprint with your IT team before trusting it.";
                if (MessageBox.Show(msg, "Trust Certificate", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    txtThumbprint.Text = info.Thumbprint;
                    cmbCertMode.SelectedIndex = (int)CertTrustMode.Pinned;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not fetch certificate: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    int.TryParse(txtPort.Text, out int port);
                    client.ServerCertificateValidationCallback = SmtpCertValidator.Create(BuildTrustConfigFromForm());
                    client.Connect(txtHost.Text, port == 0 ? 587 : port, chkUseSsl.Checked ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto);
                    if (!string.IsNullOrEmpty(txtUsername.Text) && !string.IsNullOrEmpty(txtPassword.Text))
                    {
                        client.Authenticate(txtUsername.Text, txtPassword.Text);
                    }
                    client.Disconnect(true);
                }
                MessageBox.Show("Connection successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSaveProfiles_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is RptProfile p)
            {
                p.Id = txtProfileId.Text;
                p.CC = txtCC.Text;
                p.BCC = txtBCC.Text;
                p.SmtpConfig.Host = txtHost.Text;
                if (int.TryParse(txtPort.Text, out int port)) p.SmtpConfig.Port = port;
                p.SmtpConfig.Username = txtUsername.Text;
                p.SmtpConfig.Password = txtPassword.Text;
                p.SmtpConfig.UseSsl = chkUseSsl.Checked;
                p.SmtpConfig.CertTrustMode = (CertTrustMode)Math.Max(0, cmbCertMode.SelectedIndex);
                p.SmtpConfig.PinnedThumbprint = txtThumbprint.Text;
                p.EnableEncryption = chkEnableEncryption.Checked;
                p.SmtpConfig.FromName = txtFromName.Text;
                p.SmtpConfig.FromAddress = txtFromAddress.Text;
                p.StaticAttachmentPath = txtStaticAttachment.Text;
                p.HtmlTemplate = rtbHtmlTemplate.Text;
                
                // Refresh listbox display
                _profilesBinding.ResetBindings();
            }

            _profileManager.SaveProfiles(_profilesBinding.ToList());
            MessageBox.Show("Profiles successfully saved to JSON database.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadSettings()
        {
            txtInputFolder.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input");
            txtOutputFolder.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                LicenseValidator.Validate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "License Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if service is offline. If so, spawn it in the background!
            var state = SystemStateManager.GetState();
            bool isOffline = true;
            if (DateTime.TryParse(state.LastHeartbeat, out DateTime hb))
            {
                if ((DateTime.Now - hb).TotalSeconds <= 15)
                {
                    isOffline = false;
                }
            }

            if (isOffline)
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo.FileName = Application.ExecutablePath;
                p.StartInfo.Arguments = "--service";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }

            SystemStateManager.SetCommand("START");
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            SystemStateManager.SetCommand("STOP");
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            var state = SystemStateManager.GetState();
            bool isOffline = true;
            if (DateTime.TryParse(state.LastHeartbeat, out DateTime hb))
            {
                if ((DateTime.Now - hb).TotalSeconds <= 15)
                {
                    isOffline = false;
                }
            }

            if (isOffline)
            {
                lblServiceStatus.Text = "SERVICE OFFLINE";
                lblServiceStatus.ForeColor = Color.Red;
            }
            else
            {
                lblServiceStatus.Text = $"SERVICE ONLINE - {state.CurrentStatus}";
                lblServiceStatus.ForeColor = Color.Green;
            }
        }

        private void SetInputsEnabled(bool enabled)
        {
            txtInputFolder.Enabled = enabled;
            txtOutputFolder.Enabled = enabled;
            lstProfiles.Enabled = enabled;
            btnAddProfile.Enabled = enabled;
            btnDeleteProfile.Enabled = enabled;
            btnTestConnection.Enabled = enabled;
            btnSaveProfiles.Enabled = enabled;
        }

        private void UpdateLicenseStatus()
        {
            try
            {
                LicenseValidator.Validate();
                lblLicenseStatus.Text = "License is VALID.";
                lblLicenseStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblLicenseStatus.Text = $"INVALID: {ex.Message}";
                lblLicenseStatus.ForeColor = Color.Red;
            }
        }

        private void BtnUploadLicense_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "License Files (*.key;*.lic;*.txt)|*.key;*.lic;*.txt", Title = "Select License File" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string target = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.key");
                    File.Copy(ofd.FileName, target, true);
                    UpdateLicenseStatus();
                }
            }
        }

        private void LogMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(LogMessage), msg);
                return;
            }
            rtbLogs.AppendText(msg + Environment.NewLine);
            rtbLogs.ScrollToCaret();
        }
    }
}
