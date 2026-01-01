using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TransferPiwigoToDigikam.Services;

namespace TransferPiwigoToDigikam
{
    public partial class Form1 : Form
    {
        private ImageTransferService _transferService;
        private bool _isTransferring;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select output directory for DigiKam collection";
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputDirectory.Text = folderDialog.SelectedPath;
                }
            }
        }

        private async void btnTransfer_Click(object sender, EventArgs e)
        {
            if (_isTransferring)
            {
                MessageBox.Show("Transfer is already in progress.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtPiwigoUrl.Text))
            {
                MessageBox.Show("Please enter the Piwigo URL.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPiwigoUrl.Focus();
                return;
            }

            // Validate URL format
            var piwigoUrl = txtPiwigoUrl.Text.Trim();
            if (!Uri.TryCreate(piwigoUrl, UriKind.Absolute, out Uri uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("Please enter a valid URL (e.g., https://your-site.com).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPiwigoUrl.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter your username.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter your password.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputDirectory.Text))
            {
                MessageBox.Show("Please select an output directory.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnBrowse.Focus();
                return;
            }

            // Validate output directory is accessible
            try
            {
                var testPath = Path.Combine(txtOutputDirectory.Text.Trim(), ".test");
                Directory.CreateDirectory(Path.GetDirectoryName(testPath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot access output directory:\n\n{ex.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnBrowse.Focus();
                return;
            }

            try
            {
                _isTransferring = true;
                btnTransfer.Enabled = false;
                progressBar.Value = 0;
                txtStatus.Clear();

                AddStatusMessage("Starting transfer...");
                AddStatusMessage($"Piwigo URL: {piwigoUrl}");
                AddStatusMessage($"Output Directory: {txtOutputDirectory.Text.Trim()}");
                AddStatusMessage("");

                _transferService = new ImageTransferService(
                    piwigoUrl,
                    txtUsername.Text.Trim(),
                    txtPassword.Text,
                    txtOutputDirectory.Text.Trim()
                );

                _transferService.ProgressChanged += TransferService_ProgressChanged;
                _transferService.StatusChanged += TransferService_StatusChanged;

                await Task.Run(() => _transferService.TransferAllImages());

                MessageBox.Show("Transfer completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException != null 
                    ? $"{ex.Message}\n\nDetails: {ex.InnerException.Message}"
                    : ex.Message;

                MessageBox.Show($"An error occurred during transfer:\n\n{errorMsg}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddStatusMessage($"ERROR: {errorMsg}");
            }
            finally
            {
                _isTransferring = false;
                btnTransfer.Enabled = true;

                if (_transferService != null)
                {
                    _transferService.ProgressChanged -= TransferService_ProgressChanged;
                    _transferService.StatusChanged -= TransferService_StatusChanged;
                }
            }
        }

        private void TransferService_ProgressChanged(object sender, TransferProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => TransferService_ProgressChanged(sender, e)));
                return;
            }

            if (e.TotalImages > 0)
            {
                progressBar.Maximum = e.TotalImages;
                progressBar.Value = Math.Min(e.ProcessedImages, e.TotalImages);
            }

            lblProgress.Text = $"Progress: {e.ProcessedImages} / {e.TotalImages} ({e.PercentComplete}%) - Success: {e.SuccessCount}, Failed: {e.FailureCount}";
        }

        private void TransferService_StatusChanged(object sender, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => TransferService_StatusChanged(sender, status)));
                return;
            }

            AddStatusMessage(status);
        }

        private void AddStatusMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtStatus.AppendText($"[{timestamp}] {message}\r\n");
            txtStatus.SelectionStart = txtStatus.Text.Length;
            txtStatus.ScrollToCaret();
        }
    }
}
