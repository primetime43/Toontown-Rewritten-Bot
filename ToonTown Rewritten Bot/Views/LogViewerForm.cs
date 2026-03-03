using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    public partial class LogViewerForm : Form
    {
        private LogLevel _filterLevel = LogLevel.Debug;
        private string _filterCategory = "(All)";

        public LogViewerForm()
        {
            InitializeComponent();

            // Set the global log level combo to match current Logger setting
            _logLevelCombo.SelectedIndex = (int)Logger.Instance.MinimumLevel;

            _levelFilter.SelectedIndex = 0;
            _categoryFilter.SelectedIndex = 0;

            Logger.Instance.LogEntryWritten += OnLogEntryWritten;
            this.FormClosing += (s, e) => Logger.Instance.LogEntryWritten -= OnLogEntryWritten;
        }

        private void logLevelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var newLevel = (LogLevel)_logLevelCombo.SelectedIndex;
            Logger.Instance.MinimumLevel = newLevel;

            // Persist to user preferences
            UserPreferences.Instance.LogLevel = newLevel.ToString();
            UserPreferences.Instance.Save();
        }

        private void levelFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            _filterLevel = (LogLevel)_levelFilter.SelectedIndex;
        }

        private void categoryFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            _filterCategory = _categoryFilter.SelectedItem?.ToString() ?? "(All)";
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            _logTextBox.Clear();
        }

        private void openFileBtn_Click(object sender, EventArgs e)
        {
            string path = Logger.Instance.GetCurrentLogFilePath();
            if (System.IO.File.Exists(path))
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("No log file exists yet for today.", "No Log File", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void openFolderBtn_Click(object sender, EventArgs e)
        {
            string logDir = Logger.Instance.LogDirectory;
            if (System.IO.Directory.Exists(logDir))
            {
                Process.Start(new ProcessStartInfo { FileName = logDir, UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("The Logs folder does not exist yet.", "No Logs Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void copyAllBtn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_logTextBox.Text))
                Clipboard.SetText(_logTextBox.Text);
        }

        private void OnLogEntryWritten(LogEntry entry)
        {
            if (this.IsDisposed || !this.IsHandleCreated)
                return;

            if (entry.Level < _filterLevel)
                return;

            if (_filterCategory != "(All)" && !string.Equals(entry.Category, _filterCategory, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => AppendEntry(entry)));
                }
                else
                {
                    AppendEntry(entry);
                }
            }
            catch
            {
                // Form may be closing
            }
        }

        private void AppendEntry(LogEntry entry)
        {
            if (_logTextBox.IsDisposed)
                return;

            Color color = entry.Level switch
            {
                LogLevel.Debug => Color.Gray,
                LogLevel.Info => Color.Black,
                LogLevel.Warning => Color.DarkOrange,
                LogLevel.Error => Color.Red,
                _ => Color.Black
            };

            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
            _logTextBox.SelectionColor = color;
            _logTextBox.AppendText(entry.ToString() + Environment.NewLine);

            // Auto-scroll to bottom
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.ScrollToCaret();

            // Limit buffer to ~5000 lines to prevent memory bloat
            if (_logTextBox.Lines.Length > 5500)
            {
                var lines = _logTextBox.Lines;
                var trimmed = lines.Skip(lines.Length - 5000).ToArray();
                _logTextBox.Lines = trimmed;
                _logTextBox.SelectionStart = _logTextBox.TextLength;
                _logTextBox.ScrollToCaret();
            }
        }
    }
}
