using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter your username.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter your password.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputDirectory.Text))
            {
                MessageBox.Show("Please select an output directory.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _isTransferring = true;
                btnTransfer.Enabled = false;
                progressBar.Value = 0;
                txtStatus.Clear();

                _transferService = new ImageTransferService(
                    txtPiwigoUrl.Text.Trim(),
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
                MessageBox.Show($"An error occurred during transfer:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AddStatusMessage($"ERROR: {ex.Message}");
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

            progressBar.Maximum = e.TotalImages;
            progressBar.Value = e.ProcessedImages;

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
