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
            logLevelLabel = new System.Windows.Forms.Label();
            _logLevelCombo = new System.Windows.Forms.ComboBox();
            separator1 = new System.Windows.Forms.Label();
            levelLabel = new System.Windows.Forms.Label();
            _levelFilter = new System.Windows.Forms.ComboBox();
            catLabel = new System.Windows.Forms.Label();
            _categoryFilter = new System.Windows.Forms.ComboBox();
            _clearBtn = new System.Windows.Forms.Button();
            _openFileBtn = new System.Windows.Forms.Button();
            _copyAllBtn = new System.Windows.Forms.Button();
            _logTextBox = new System.Windows.Forms.RichTextBox();
            toolPanel.SuspendLayout();
            SuspendLayout();
            //
            // toolPanel
            //
            toolPanel.Controls.Add(logLevelLabel);
            toolPanel.Controls.Add(_logLevelCombo);
            toolPanel.Controls.Add(separator1);
            toolPanel.Controls.Add(levelLabel);
            toolPanel.Controls.Add(_levelFilter);
            toolPanel.Controls.Add(catLabel);
            toolPanel.Controls.Add(_categoryFilter);
            toolPanel.Controls.Add(_clearBtn);
            toolPanel.Controls.Add(_openFileBtn);
            toolPanel.Controls.Add(_copyAllBtn);
            toolPanel.Dock = System.Windows.Forms.DockStyle.Top;
            toolPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            toolPanel.Location = new System.Drawing.Point(0, 0);
            toolPanel.Name = "toolPanel";
            toolPanel.Padding = new System.Windows.Forms.Padding(5, 5, 5, 0);
            toolPanel.Size = new System.Drawing.Size(884, 35);
            toolPanel.TabIndex = 0;
            //
            // logLevelLabel
            //
            logLevelLabel.AutoSize = true;
            logLevelLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            logLevelLabel.Margin = new System.Windows.Forms.Padding(0, 5, 3, 0);
            logLevelLabel.Name = "logLevelLabel";
            logLevelLabel.Size = new System.Drawing.Size(62, 13);
            logLevelLabel.TabIndex = 10;
            logLevelLabel.Text = "Log Level:";
            //
            // _logLevelCombo
            //
            _logLevelCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _logLevelCombo.Items.AddRange(new object[] { "Debug", "Info", "Warning", "Error" });
            _logLevelCombo.Name = "_logLevelCombo";
            _logLevelCombo.Size = new System.Drawing.Size(80, 23);
            _logLevelCombo.TabIndex = 11;
            _logLevelCombo.SelectedIndexChanged += logLevelCombo_SelectedIndexChanged;
            toolTip.SetToolTip(_logLevelCombo, "Controls which messages are logged to file and displayed. Saved across sessions.");
            //
            // separator1
            //
            separator1.AutoSize = true;
            separator1.ForeColor = System.Drawing.Color.LightGray;
            separator1.Margin = new System.Windows.Forms.Padding(6, 5, 6, 0);
            separator1.Name = "separator1";
            separator1.Size = new System.Drawing.Size(10, 15);
            separator1.TabIndex = 12;
            separator1.Text = "|";
            //
            // levelLabel
            //
            levelLabel.AutoSize = true;
            levelLabel.Margin = new System.Windows.Forms.Padding(0, 5, 3, 0);
            levelLabel.Name = "levelLabel";
            levelLabel.Size = new System.Drawing.Size(36, 15);
            levelLabel.TabIndex = 0;
            levelLabel.Text = "Filter:";
            //
            // _levelFilter
            //
            _levelFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _levelFilter.Items.AddRange(new object[] { "Debug", "Info", "Warning", "Error" });
            _levelFilter.Name = "_levelFilter";
            _levelFilter.Size = new System.Drawing.Size(80, 23);
            _levelFilter.TabIndex = 1;
            _levelFilter.SelectedIndexChanged += levelFilter_SelectedIndexChanged;
            toolTip.SetToolTip(_levelFilter, "Filter which messages are shown in this viewer (does not affect the log file)");
            //
            // catLabel
            //
            catLabel.AutoSize = true;
            catLabel.Margin = new System.Windows.Forms.Padding(6, 5, 3, 0);
            catLabel.Name = "catLabel";
            catLabel.Size = new System.Drawing.Size(58, 15);
            catLabel.TabIndex = 2;
            catLabel.Text = "Category:";
            //
            // _categoryFilter
            //
            _categoryFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _categoryFilter.Items.AddRange(new object[] { "(All)", "Input", "Coordinates", "TemplateMatch", "Fishing", "FishDetect", "Doodle" });
            _categoryFilter.Name = "_categoryFilter";
            _categoryFilter.Size = new System.Drawing.Size(120, 23);
            _categoryFilter.TabIndex = 3;
            _categoryFilter.SelectedIndexChanged += categoryFilter_SelectedIndexChanged;
            toolTip.SetToolTip(_categoryFilter, "Filter by category: Input, Coordinates, TemplateMatch, Fishing, FishDetect, or Doodle");
            //
            // _clearBtn
            //
            _clearBtn.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            _clearBtn.Name = "_clearBtn";
            _clearBtn.Size = new System.Drawing.Size(55, 23);
            _clearBtn.TabIndex = 4;
            _clearBtn.Text = "Clear";
            _clearBtn.UseVisualStyleBackColor = true;
            _clearBtn.Click += clearBtn_Click;
            toolTip.SetToolTip(_clearBtn, "Clear all entries from this viewer (does not affect the log file)");
            //
            // _openFileBtn
            //
            _openFileBtn.Name = "_openFileBtn";
            _openFileBtn.Size = new System.Drawing.Size(90, 23);
            _openFileBtn.TabIndex = 5;
            _openFileBtn.Text = "Open Log File";
            _openFileBtn.UseVisualStyleBackColor = true;
            _openFileBtn.Click += openFileBtn_Click;
            toolTip.SetToolTip(_openFileBtn, "Open today's log file in your default text editor");
            //
            // _copyAllBtn
            //
            _copyAllBtn.Name = "_copyAllBtn";
            _copyAllBtn.Size = new System.Drawing.Size(65, 23);
            _copyAllBtn.TabIndex = 6;
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
        private System.Windows.Forms.Label logLevelLabel;
        private System.Windows.Forms.ComboBox _logLevelCombo;
        private System.Windows.Forms.Label separator1;
        private System.Windows.Forms.Label levelLabel;
        private System.Windows.Forms.ComboBox _levelFilter;
        private System.Windows.Forms.Label catLabel;
        private System.Windows.Forms.ComboBox _categoryFilter;
        private System.Windows.Forms.Button _clearBtn;
        private System.Windows.Forms.Button _openFileBtn;
        private System.Windows.Forms.Button _copyAllBtn;
        private System.Windows.Forms.RichTextBox _logTextBox;
    }
}
