namespace ToonTown_Rewritten_Bot.Views
{
    partial class LogViewerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip = new System.Windows.Forms.ToolTip(components);
            toolPanel = new System.Windows.Forms.FlowLayoutPanel();
            _clearBtn = new System.Windows.Forms.Button();
            _openFileBtn = new System.Windows.Forms.Button();
            _openFolderBtn = new System.Windows.Forms.Button();
            _copyAllBtn = new System.Windows.Forms.Button();
            _logTextBox = new System.Windows.Forms.RichTextBox();
            toolPanel.SuspendLayout();
            SuspendLayout();
            //
            // toolPanel
            //
            toolPanel.Controls.Add(_clearBtn);
            toolPanel.Controls.Add(_openFileBtn);
            toolPanel.Controls.Add(_openFolderBtn);
            toolPanel.Controls.Add(_copyAllBtn);
            toolPanel.Dock = System.Windows.Forms.DockStyle.Top;
            toolPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            toolPanel.Location = new System.Drawing.Point(0, 0);
            toolPanel.Name = "toolPanel";
            toolPanel.Padding = new System.Windows.Forms.Padding(5, 5, 5, 0);
            toolPanel.Size = new System.Drawing.Size(884, 35);
            toolPanel.TabIndex = 0;
            //
            // _clearBtn
            //
            _clearBtn.Name = "_clearBtn";
            _clearBtn.Size = new System.Drawing.Size(55, 23);
            _clearBtn.TabIndex = 0;
            _clearBtn.Text = "Clear";
            _clearBtn.UseVisualStyleBackColor = true;
            _clearBtn.Click += clearBtn_Click;
            toolTip.SetToolTip(_clearBtn, "Clear all entries from this viewer (does not affect the log file)");
            //
            // _openFileBtn
            //
            _openFileBtn.Name = "_openFileBtn";
            _openFileBtn.Size = new System.Drawing.Size(90, 23);
            _openFileBtn.TabIndex = 1;
            _openFileBtn.Text = "Open Log File";
            _openFileBtn.UseVisualStyleBackColor = true;
            _openFileBtn.Click += openFileBtn_Click;
            toolTip.SetToolTip(_openFileBtn, "Open today's log file in your default text editor");
            //
            // _openFolderBtn
            //
            _openFolderBtn.Name = "_openFolderBtn";
            _openFolderBtn.Size = new System.Drawing.Size(90, 23);
            _openFolderBtn.TabIndex = 2;
            _openFolderBtn.Text = "Open Folder";
            _openFolderBtn.UseVisualStyleBackColor = true;
            _openFolderBtn.Click += openFolderBtn_Click;
            toolTip.SetToolTip(_openFolderBtn, "Open the Logs folder in File Explorer to view previous log files");
            //
            // _copyAllBtn
            //
            _copyAllBtn.Name = "_copyAllBtn";
            _copyAllBtn.Size = new System.Drawing.Size(65, 23);
            _copyAllBtn.TabIndex = 3;
            _copyAllBtn.Text = "Copy All";
            _copyAllBtn.UseVisualStyleBackColor = true;
            _copyAllBtn.Click += copyAllBtn_Click;
            toolTip.SetToolTip(_copyAllBtn, "Copy all visible log entries to the clipboard");
            //
            // _logTextBox
            //
            _logTextBox.BackColor = System.Drawing.Color.White;
            _logTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            _logTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            _logTextBox.Location = new System.Drawing.Point(0, 35);
            _logTextBox.Name = "_logTextBox";
            _logTextBox.ReadOnly = true;
            _logTextBox.Size = new System.Drawing.Size(884, 476);
            _logTextBox.TabIndex = 1;
            _logTextBox.Text = "";
            _logTextBox.WordWrap = false;
            //
            // LogViewerForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(884, 511);
            Controls.Add(_logTextBox);
            Controls.Add(toolPanel);
            MinimumSize = new System.Drawing.Size(600, 300);
            Name = "LogViewerForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Log Viewer";
            toolPanel.ResumeLayout(false);
            toolPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.FlowLayoutPanel toolPanel;
        private System.Windows.Forms.Button _clearBtn;
        private System.Windows.Forms.Button _openFileBtn;
        private System.Windows.Forms.Button _openFolderBtn;
        private System.Windows.Forms.Button _copyAllBtn;
        private System.Windows.Forms.RichTextBox _logTextBox;
    }
}
