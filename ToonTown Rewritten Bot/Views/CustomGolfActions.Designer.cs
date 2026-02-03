namespace ToonTown_Rewritten_Bot.Views
{
    partial class CustomGolfActions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomGolfActions));
            updateSelectedActionItemBtn = new System.Windows.Forms.Button();
            actionItemsListBox = new System.Windows.Forms.ListBox();
            loadActionItemBtn = new System.Windows.Forms.Button();
            saveActionItemBtn = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            actionTimeTxtBox = new System.Windows.Forms.TextBox();
            comboBox1 = new System.Windows.Forms.ComboBox();
            removeItemBtn = new System.Windows.Forms.Button();
            addItemBtn = new System.Windows.Forms.Button();
            helpLabel = new System.Windows.Forms.Label();
            groupBoxPresets = new System.Windows.Forms.GroupBox();
            lblPresetsTitle = new System.Windows.Forms.Label();
            btnPreset50 = new System.Windows.Forms.Button();
            btnPreset100 = new System.Windows.Forms.Button();
            btnPreset150 = new System.Windows.Forms.Button();
            btnPreset200 = new System.Windows.Forms.Button();
            btnPreset1000 = new System.Windows.Forms.Button();
            btnPreset1500 = new System.Windows.Forms.Button();
            btnPreset2000 = new System.Windows.Forms.Button();
            btnPreset2500 = new System.Windows.Forms.Button();
            groupBoxPreview = new System.Windows.Forms.GroupBox();
            lblSequencePreview = new System.Windows.Forms.Label();
            groupBoxSummary = new System.Windows.Forms.GroupBox();
            lblSummary = new System.Windows.Forms.Label();
            lblDurationDisplay = new System.Windows.Forms.Label();
            groupBoxPresets.SuspendLayout();
            groupBoxPreview.SuspendLayout();
            groupBoxSummary.SuspendLayout();
            SuspendLayout();
            //
            // updateSelectedActionItemBtn
            //
            updateSelectedActionItemBtn.Enabled = false;
            updateSelectedActionItemBtn.Location = new System.Drawing.Point(248, 82);
            updateSelectedActionItemBtn.Name = "updateSelectedActionItemBtn";
            updateSelectedActionItemBtn.Size = new System.Drawing.Size(188, 28);
            updateSelectedActionItemBtn.TabIndex = 18;
            updateSelectedActionItemBtn.Text = "Update Selected Item";
            updateSelectedActionItemBtn.UseVisualStyleBackColor = true;
            updateSelectedActionItemBtn.Click += updateSelectedActionItemBtn_Click;
            //
            // actionItemsListBox
            //
            actionItemsListBox.FormattingEnabled = true;
            actionItemsListBox.ItemHeight = 15;
            actionItemsListBox.Location = new System.Drawing.Point(12, 12);
            actionItemsListBox.Name = "actionItemsListBox";
            actionItemsListBox.Size = new System.Drawing.Size(230, 244);
            actionItemsListBox.TabIndex = 17;
            actionItemsListBox.SelectedIndexChanged += actionItemsListBox_SelectedIndexChanged;
            //
            // loadActionItemBtn
            //
            loadActionItemBtn.Location = new System.Drawing.Point(12, 268);
            loadActionItemBtn.Name = "loadActionItemBtn";
            loadActionItemBtn.Size = new System.Drawing.Size(110, 34);
            loadActionItemBtn.TabIndex = 16;
            loadActionItemBtn.Text = "Load";
            loadActionItemBtn.UseVisualStyleBackColor = true;
            loadActionItemBtn.Click += loadActionItemBtn_Click;
            //
            // saveActionItemBtn
            //
            saveActionItemBtn.Location = new System.Drawing.Point(132, 268);
            saveActionItemBtn.Name = "saveActionItemBtn";
            saveActionItemBtn.Size = new System.Drawing.Size(110, 34);
            saveActionItemBtn.TabIndex = 15;
            saveActionItemBtn.Text = "Save";
            saveActionItemBtn.UseVisualStyleBackColor = true;
            saveActionItemBtn.Click += saveActionItemBtn_Click;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(248, 118);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(117, 15);
            label1.TabIndex = 14;
            label1.Text = "Duration (ms):";
            //
            // actionTimeTxtBox
            //
            actionTimeTxtBox.Enabled = false;
            actionTimeTxtBox.Location = new System.Drawing.Point(248, 136);
            actionTimeTxtBox.Name = "actionTimeTxtBox";
            actionTimeTxtBox.Size = new System.Drawing.Size(100, 23);
            actionTimeTxtBox.TabIndex = 13;
            actionTimeTxtBox.TextChanged += actionTimeTxtBox_TextChanged;
            //
            // lblDurationDisplay
            //
            lblDurationDisplay.AutoSize = true;
            lblDurationDisplay.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            lblDurationDisplay.ForeColor = System.Drawing.Color.DarkBlue;
            lblDurationDisplay.Location = new System.Drawing.Point(354, 139);
            lblDurationDisplay.Name = "lblDurationDisplay";
            lblDurationDisplay.Size = new System.Drawing.Size(80, 15);
            lblDurationDisplay.TabIndex = 25;
            lblDurationDisplay.Text = "";
            //
            // comboBox1
            //
            comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "SWING POWER", "TURN LEFT", "TURN RIGHT", "MOVE TO LEFT TEE SPOT", "MOVE TO RIGHT TEE SPOT", "DELAY TIME" });
            comboBox1.Location = new System.Drawing.Point(248, 14);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new System.Drawing.Size(188, 23);
            comboBox1.TabIndex = 12;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            //
            // removeItemBtn
            //
            removeItemBtn.Location = new System.Drawing.Point(345, 48);
            removeItemBtn.Name = "removeItemBtn";
            removeItemBtn.Size = new System.Drawing.Size(91, 28);
            removeItemBtn.TabIndex = 11;
            removeItemBtn.Text = "Remove";
            removeItemBtn.UseVisualStyleBackColor = true;
            removeItemBtn.Click += removeItemBtn_Click;
            //
            // addItemBtn
            //
            addItemBtn.Location = new System.Drawing.Point(248, 48);
            addItemBtn.Name = "addItemBtn";
            addItemBtn.Size = new System.Drawing.Size(91, 28);
            addItemBtn.TabIndex = 10;
            addItemBtn.Text = "Add";
            addItemBtn.UseVisualStyleBackColor = true;
            addItemBtn.Click += addItemBtn_Click;
            //
            // groupBoxPresets
            //
            groupBoxPresets.Controls.Add(lblPresetsTitle);
            groupBoxPresets.Controls.Add(btnPreset50);
            groupBoxPresets.Controls.Add(btnPreset100);
            groupBoxPresets.Controls.Add(btnPreset150);
            groupBoxPresets.Controls.Add(btnPreset200);
            groupBoxPresets.Controls.Add(btnPreset1000);
            groupBoxPresets.Controls.Add(btnPreset1500);
            groupBoxPresets.Controls.Add(btnPreset2000);
            groupBoxPresets.Controls.Add(btnPreset2500);
            groupBoxPresets.Location = new System.Drawing.Point(248, 165);
            groupBoxPresets.Name = "groupBoxPresets";
            groupBoxPresets.Size = new System.Drawing.Size(200, 95);
            groupBoxPresets.TabIndex = 20;
            groupBoxPresets.TabStop = false;
            groupBoxPresets.Text = "Quick Presets";
            //
            // lblPresetsTitle
            //
            lblPresetsTitle.AutoSize = true;
            lblPresetsTitle.Location = new System.Drawing.Point(8, 18);
            lblPresetsTitle.Name = "lblPresetsTitle";
            lblPresetsTitle.Size = new System.Drawing.Size(40, 15);
            lblPresetsTitle.TabIndex = 0;
            lblPresetsTitle.Text = "Turns:";
            //
            // btnPreset50
            //
            btnPreset50.Location = new System.Drawing.Point(8, 36);
            btnPreset50.Name = "btnPreset50";
            btnPreset50.Size = new System.Drawing.Size(45, 24);
            btnPreset50.TabIndex = 1;
            btnPreset50.Text = "50";
            btnPreset50.UseVisualStyleBackColor = true;
            btnPreset50.Click += btnPreset_Click;
            btnPreset50.Tag = "50";
            //
            // btnPreset100
            //
            btnPreset100.Location = new System.Drawing.Point(58, 36);
            btnPreset100.Name = "btnPreset100";
            btnPreset100.Size = new System.Drawing.Size(45, 24);
            btnPreset100.TabIndex = 2;
            btnPreset100.Text = "100";
            btnPreset100.UseVisualStyleBackColor = true;
            btnPreset100.Click += btnPreset_Click;
            btnPreset100.Tag = "100";
            //
            // btnPreset150
            //
            btnPreset150.Location = new System.Drawing.Point(108, 36);
            btnPreset150.Name = "btnPreset150";
            btnPreset150.Size = new System.Drawing.Size(45, 24);
            btnPreset150.TabIndex = 3;
            btnPreset150.Text = "150";
            btnPreset150.UseVisualStyleBackColor = true;
            btnPreset150.Click += btnPreset_Click;
            btnPreset150.Tag = "150";
            //
            // btnPreset200
            //
            btnPreset200.Location = new System.Drawing.Point(158, 36);
            btnPreset200.Name = "btnPreset200";
            btnPreset200.Size = new System.Drawing.Size(35, 24);
            btnPreset200.TabIndex = 4;
            btnPreset200.Text = "200";
            btnPreset200.UseVisualStyleBackColor = true;
            btnPreset200.Click += btnPreset_Click;
            btnPreset200.Tag = "200";
            //
            // btnPreset1000
            //
            btnPreset1000.Location = new System.Drawing.Point(8, 65);
            btnPreset1000.Name = "btnPreset1000";
            btnPreset1000.Size = new System.Drawing.Size(45, 24);
            btnPreset1000.TabIndex = 5;
            btnPreset1000.Text = "40%";
            btnPreset1000.UseVisualStyleBackColor = true;
            btnPreset1000.Click += btnPreset_Click;
            btnPreset1000.Tag = "1000";
            //
            // btnPreset1500
            //
            btnPreset1500.Location = new System.Drawing.Point(58, 65);
            btnPreset1500.Name = "btnPreset1500";
            btnPreset1500.Size = new System.Drawing.Size(45, 24);
            btnPreset1500.TabIndex = 6;
            btnPreset1500.Text = "60%";
            btnPreset1500.UseVisualStyleBackColor = true;
            btnPreset1500.Click += btnPreset_Click;
            btnPreset1500.Tag = "1500";
            //
            // btnPreset2000
            //
            btnPreset2000.Location = new System.Drawing.Point(108, 65);
            btnPreset2000.Name = "btnPreset2000";
            btnPreset2000.Size = new System.Drawing.Size(45, 24);
            btnPreset2000.TabIndex = 7;
            btnPreset2000.Text = "80%";
            btnPreset2000.UseVisualStyleBackColor = true;
            btnPreset2000.Click += btnPreset_Click;
            btnPreset2000.Tag = "2000";
            //
            // btnPreset2500
            //
            btnPreset2500.Location = new System.Drawing.Point(158, 65);
            btnPreset2500.Name = "btnPreset2500";
            btnPreset2500.Size = new System.Drawing.Size(35, 24);
            btnPreset2500.TabIndex = 8;
            btnPreset2500.Text = "100%";
            btnPreset2500.UseVisualStyleBackColor = true;
            btnPreset2500.Click += btnPreset_Click;
            btnPreset2500.Tag = "2500";
            //
            // helpLabel
            //
            helpLabel.Location = new System.Drawing.Point(455, 14);
            helpLabel.Name = "helpLabel";
            helpLabel.Size = new System.Drawing.Size(180, 85);
            helpLabel.TabIndex = 19;
            helpLabel.Text = "Select an action to see help.";
            helpLabel.ForeColor = System.Drawing.Color.DarkSlateGray;
            //
            // groupBoxPreview
            //
            groupBoxPreview.Controls.Add(lblSequencePreview);
            groupBoxPreview.Location = new System.Drawing.Point(12, 310);
            groupBoxPreview.Name = "groupBoxPreview";
            groupBoxPreview.Size = new System.Drawing.Size(436, 55);
            groupBoxPreview.TabIndex = 21;
            groupBoxPreview.TabStop = false;
            groupBoxPreview.Text = "Sequence Preview";
            //
            // lblSequencePreview
            //
            lblSequencePreview.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular);
            lblSequencePreview.Location = new System.Drawing.Point(8, 18);
            lblSequencePreview.Name = "lblSequencePreview";
            lblSequencePreview.Size = new System.Drawing.Size(420, 30);
            lblSequencePreview.TabIndex = 0;
            lblSequencePreview.Text = "(No actions)";
            lblSequencePreview.AutoEllipsis = true;
            //
            // groupBoxSummary
            //
            groupBoxSummary.Controls.Add(lblSummary);
            groupBoxSummary.Location = new System.Drawing.Point(455, 105);
            groupBoxSummary.Name = "groupBoxSummary";
            groupBoxSummary.Size = new System.Drawing.Size(180, 155);
            groupBoxSummary.TabIndex = 22;
            groupBoxSummary.TabStop = false;
            groupBoxSummary.Text = "Summary";
            //
            // lblSummary
            //
            lblSummary.Location = new System.Drawing.Point(8, 18);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new System.Drawing.Size(165, 130);
            lblSummary.TabIndex = 0;
            lblSummary.Text = "Actions: 0\nTotal Time: 0ms\nPower: -\nNet Turn: Center\nTee Position: Center";
            //
            // CustomGolfActions
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(645, 375);
            Controls.Add(groupBoxSummary);
            Controls.Add(groupBoxPreview);
            Controls.Add(groupBoxPresets);
            Controls.Add(lblDurationDisplay);
            Controls.Add(updateSelectedActionItemBtn);
            Controls.Add(actionItemsListBox);
            Controls.Add(loadActionItemBtn);
            Controls.Add(saveActionItemBtn);
            Controls.Add(label1);
            Controls.Add(actionTimeTxtBox);
            Controls.Add(comboBox1);
            Controls.Add(removeItemBtn);
            Controls.Add(addItemBtn);
            Controls.Add(helpLabel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "CustomGolfActions";
            Text = "Custom Golf Actions Manager";
            groupBoxPresets.ResumeLayout(false);
            groupBoxPresets.PerformLayout();
            groupBoxPreview.ResumeLayout(false);
            groupBoxSummary.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button updateSelectedActionItemBtn;
        private System.Windows.Forms.ListBox actionItemsListBox;
        private System.Windows.Forms.Button loadActionItemBtn;
        private System.Windows.Forms.Button saveActionItemBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox actionTimeTxtBox;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button removeItemBtn;
        private System.Windows.Forms.Button addItemBtn;
        private System.Windows.Forms.Label helpLabel;
        private System.Windows.Forms.GroupBox groupBoxPresets;
        private System.Windows.Forms.Label lblPresetsTitle;
        private System.Windows.Forms.Button btnPreset50;
        private System.Windows.Forms.Button btnPreset100;
        private System.Windows.Forms.Button btnPreset150;
        private System.Windows.Forms.Button btnPreset200;
        private System.Windows.Forms.Button btnPreset1000;
        private System.Windows.Forms.Button btnPreset1500;
        private System.Windows.Forms.Button btnPreset2000;
        private System.Windows.Forms.Button btnPreset2500;
        private System.Windows.Forms.GroupBox groupBoxPreview;
        private System.Windows.Forms.Label lblSequencePreview;
        private System.Windows.Forms.GroupBox groupBoxSummary;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.Label lblDurationDisplay;
    }
}
