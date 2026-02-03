namespace ToonTown_Rewritten_Bot.Views
{
    partial class CustomFishingActions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomFishingActions));
            addItemBtn = new System.Windows.Forms.Button();
            removeItemBtn = new System.Windows.Forms.Button();
            comboBox1 = new System.Windows.Forms.ComboBox();
            actionTimeTxtBox = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            saveActionItemBtn = new System.Windows.Forms.Button();
            loadActionItemBtn = new System.Windows.Forms.Button();
            actionItemsListBox = new System.Windows.Forms.ListBox();
            updateSelectedActionItemBtn = new System.Windows.Forms.Button();
            groupBoxRecorder = new System.Windows.Forms.GroupBox();
            btnStopRecording = new System.Windows.Forms.Button();
            btnStartRecording = new System.Windows.Forms.Button();
            lblRecordingStatus = new System.Windows.Forms.Label();
            btnAddSellFish = new System.Windows.Forms.Button();
            lblRecorderHelp = new System.Windows.Forms.Label();
            lblLivePreview = new System.Windows.Forms.Label();
            groupBoxPathPreview = new System.Windows.Forms.GroupBox();
            lblPathPreview = new System.Windows.Forms.Label();
            groupBoxCalibration = new System.Windows.Forms.GroupBox();
            btnCalibrateScanArea = new System.Windows.Forms.Button();
            btnCalibratePondColors = new System.Windows.Forms.Button();
            lblCalibrationStatus = new System.Windows.Forms.Label();
            groupBoxRecorder.SuspendLayout();
            groupBoxPathPreview.SuspendLayout();
            groupBoxCalibration.SuspendLayout();
            SuspendLayout();
            // 
            // addItemBtn
            // 
            addItemBtn.Location = new System.Drawing.Point(250, 49);
            addItemBtn.Name = "addItemBtn";
            addItemBtn.Size = new System.Drawing.Size(91, 28);
            addItemBtn.TabIndex = 1;
            addItemBtn.Text = "Add Item";
            addItemBtn.UseVisualStyleBackColor = true;
            addItemBtn.Click += addItemBtn_Click;
            // 
            // removeItemBtn
            // 
            removeItemBtn.Location = new System.Drawing.Point(347, 49);
            removeItemBtn.Name = "removeItemBtn";
            removeItemBtn.Size = new System.Drawing.Size(91, 28);
            removeItemBtn.TabIndex = 2;
            removeItemBtn.Text = "Remove Item";
            removeItemBtn.UseVisualStyleBackColor = true;
            removeItemBtn.Click += removeItemBtn_Click;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "WALK FORWARDS", "WALK BACKWARDS", "TURN LEFT", "TURN RIGHT", "TIME", "SELL FISH" });
            comboBox1.Location = new System.Drawing.Point(250, 15);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new System.Drawing.Size(188, 23);
            comboBox1.TabIndex = 3;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // actionTimeTxtBox
            // 
            actionTimeTxtBox.Enabled = false;
            actionTimeTxtBox.Location = new System.Drawing.Point(250, 179);
            actionTimeTxtBox.Name = "actionTimeTxtBox";
            actionTimeTxtBox.Size = new System.Drawing.Size(144, 23);
            actionTimeTxtBox.TabIndex = 4;
            actionTimeTxtBox.TextChanged += actionTimeTxtBox_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(250, 161);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(117, 15);
            label1.TabIndex = 5;
            label1.Text = "Time for action (ms):";
            // 
            // saveActionItemBtn
            // 
            saveActionItemBtn.Location = new System.Drawing.Point(174, 274);
            saveActionItemBtn.Name = "saveActionItemBtn";
            saveActionItemBtn.Size = new System.Drawing.Size(144, 34);
            saveActionItemBtn.TabIndex = 6;
            saveActionItemBtn.Text = "Save Action Item";
            saveActionItemBtn.UseVisualStyleBackColor = true;
            saveActionItemBtn.Click += saveActionItemBtn_Click;
            // 
            // loadActionItemBtn
            // 
            loadActionItemBtn.Location = new System.Drawing.Point(14, 274);
            loadActionItemBtn.Name = "loadActionItemBtn";
            loadActionItemBtn.Size = new System.Drawing.Size(144, 34);
            loadActionItemBtn.TabIndex = 7;
            loadActionItemBtn.Text = "Load Action Item";
            loadActionItemBtn.UseVisualStyleBackColor = true;
            loadActionItemBtn.Click += loadActionItemBtn_Click;
            // 
            // actionItemsListBox
            // 
            actionItemsListBox.FormattingEnabled = true;
            actionItemsListBox.ItemHeight = 15;
            actionItemsListBox.Location = new System.Drawing.Point(14, 13);
            actionItemsListBox.Name = "actionItemsListBox";
            actionItemsListBox.Size = new System.Drawing.Size(230, 244);
            actionItemsListBox.TabIndex = 8;
            actionItemsListBox.SelectedIndexChanged += actionItemsListBox_SelectedIndexChanged;
            //
            // updateSelectedActionItemBtn
            //
            updateSelectedActionItemBtn.Enabled = false;
            updateSelectedActionItemBtn.Location = new System.Drawing.Point(250, 83);
            updateSelectedActionItemBtn.Name = "updateSelectedActionItemBtn";
            updateSelectedActionItemBtn.Size = new System.Drawing.Size(188, 28);
            updateSelectedActionItemBtn.TabIndex = 9;
            updateSelectedActionItemBtn.Text = "Update Selected Item";
            updateSelectedActionItemBtn.UseVisualStyleBackColor = true;
            updateSelectedActionItemBtn.Click += updateSelectedActionItemBtn_Click;
            //
            // groupBoxRecorder
            //
            groupBoxRecorder.Controls.Add(lblRecorderHelp);
            groupBoxRecorder.Controls.Add(btnStartRecording);
            groupBoxRecorder.Controls.Add(btnStopRecording);
            groupBoxRecorder.Controls.Add(lblRecordingStatus);
            groupBoxRecorder.Controls.Add(btnAddSellFish);
            groupBoxRecorder.Controls.Add(lblLivePreview);
            groupBoxRecorder.Location = new System.Drawing.Point(14, 320);
            groupBoxRecorder.Name = "groupBoxRecorder";
            groupBoxRecorder.Size = new System.Drawing.Size(424, 150);
            groupBoxRecorder.TabIndex = 10;
            groupBoxRecorder.TabStop = false;
            groupBoxRecorder.Text = "Walk Path Recorder";
            //
            // lblRecorderHelp
            //
            lblRecorderHelp.Location = new System.Drawing.Point(10, 22);
            lblRecorderHelp.Name = "lblRecorderHelp";
            lblRecorderHelp.Size = new System.Drawing.Size(404, 32);
            lblRecorderHelp.TabIndex = 0;
            lblRecorderHelp.Text = "Click 'Start Recording', switch to TTR, walk your path using arrow keys. Press 'Add Sell' when at the bucket. Click 'Stop' when done.";
            //
            // btnStartRecording
            //
            btnStartRecording.BackColor = System.Drawing.Color.LightGreen;
            btnStartRecording.Location = new System.Drawing.Point(10, 60);
            btnStartRecording.Name = "btnStartRecording";
            btnStartRecording.Size = new System.Drawing.Size(120, 35);
            btnStartRecording.TabIndex = 1;
            btnStartRecording.Text = "Start Recording";
            btnStartRecording.UseVisualStyleBackColor = false;
            btnStartRecording.Click += btnStartRecording_Click;
            //
            // btnStopRecording
            //
            btnStopRecording.BackColor = System.Drawing.Color.LightCoral;
            btnStopRecording.Enabled = false;
            btnStopRecording.Location = new System.Drawing.Point(140, 60);
            btnStopRecording.Name = "btnStopRecording";
            btnStopRecording.Size = new System.Drawing.Size(120, 35);
            btnStopRecording.TabIndex = 2;
            btnStopRecording.Text = "Stop Recording";
            btnStopRecording.UseVisualStyleBackColor = false;
            btnStopRecording.Click += btnStopRecording_Click;
            //
            // btnAddSellFish
            //
            btnAddSellFish.Enabled = false;
            btnAddSellFish.Location = new System.Drawing.Point(270, 60);
            btnAddSellFish.Name = "btnAddSellFish";
            btnAddSellFish.Size = new System.Drawing.Size(140, 35);
            btnAddSellFish.TabIndex = 3;
            btnAddSellFish.Text = "Add Sell Fish Action";
            btnAddSellFish.UseVisualStyleBackColor = true;
            btnAddSellFish.Click += btnAddSellFish_Click;
            //
            // lblRecordingStatus
            //
            lblRecordingStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblRecordingStatus.ForeColor = System.Drawing.Color.Gray;
            lblRecordingStatus.Location = new System.Drawing.Point(10, 105);
            lblRecordingStatus.Name = "lblRecordingStatus";
            lblRecordingStatus.Size = new System.Drawing.Size(400, 25);
            lblRecordingStatus.TabIndex = 4;
            lblRecordingStatus.Text = "Status: Not recording";
            //
            // lblLivePreview
            //
            lblLivePreview.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular);
            lblLivePreview.ForeColor = System.Drawing.Color.DarkBlue;
            lblLivePreview.Location = new System.Drawing.Point(10, 130);
            lblLivePreview.Name = "lblLivePreview";
            lblLivePreview.Size = new System.Drawing.Size(400, 15);
            lblLivePreview.TabIndex = 5;
            lblLivePreview.Text = "";
            //
            // groupBoxPathPreview
            //
            groupBoxPathPreview.Controls.Add(lblPathPreview);
            groupBoxPathPreview.Location = new System.Drawing.Point(14, 475);
            groupBoxPathPreview.Name = "groupBoxPathPreview";
            groupBoxPathPreview.Size = new System.Drawing.Size(424, 60);
            groupBoxPathPreview.TabIndex = 11;
            groupBoxPathPreview.TabStop = false;
            groupBoxPathPreview.Text = "Path Preview";
            //
            // lblPathPreview
            //
            lblPathPreview.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular);
            lblPathPreview.Location = new System.Drawing.Point(10, 20);
            lblPathPreview.Name = "lblPathPreview";
            lblPathPreview.Size = new System.Drawing.Size(404, 32);
            lblPathPreview.TabIndex = 0;
            lblPathPreview.Text = "(No actions)";
            lblPathPreview.AutoEllipsis = true;
            //
            // groupBoxCalibration
            //
            groupBoxCalibration.Controls.Add(btnCalibrateScanArea);
            groupBoxCalibration.Controls.Add(btnCalibratePondColors);
            groupBoxCalibration.Controls.Add(lblCalibrationStatus);
            groupBoxCalibration.Location = new System.Drawing.Point(14, 540);
            groupBoxCalibration.Name = "groupBoxCalibration";
            groupBoxCalibration.Size = new System.Drawing.Size(424, 85);
            groupBoxCalibration.TabIndex = 12;
            groupBoxCalibration.TabStop = false;
            groupBoxCalibration.Text = "Calibration (Embedded in Action File)";
            //
            // btnCalibrateScanArea
            //
            btnCalibrateScanArea.Location = new System.Drawing.Point(10, 50);
            btnCalibrateScanArea.Name = "btnCalibrateScanArea";
            btnCalibrateScanArea.Size = new System.Drawing.Size(140, 28);
            btnCalibrateScanArea.TabIndex = 0;
            btnCalibrateScanArea.Text = "Calibrate Scan Area";
            btnCalibrateScanArea.UseVisualStyleBackColor = true;
            btnCalibrateScanArea.Click += btnCalibrateScanArea_Click;
            //
            // btnCalibratePondColors
            //
            btnCalibratePondColors.Location = new System.Drawing.Point(160, 50);
            btnCalibratePondColors.Name = "btnCalibratePondColors";
            btnCalibratePondColors.Size = new System.Drawing.Size(140, 28);
            btnCalibratePondColors.TabIndex = 1;
            btnCalibratePondColors.Text = "Calibrate Pond Colors";
            btnCalibratePondColors.UseVisualStyleBackColor = true;
            btnCalibratePondColors.Click += btnCalibratePondColors_Click;
            //
            // lblCalibrationStatus
            //
            lblCalibrationStatus.ForeColor = System.Drawing.Color.Gray;
            lblCalibrationStatus.Location = new System.Drawing.Point(10, 22);
            lblCalibrationStatus.Name = "lblCalibrationStatus";
            lblCalibrationStatus.Size = new System.Drawing.Size(400, 20);
            lblCalibrationStatus.TabIndex = 2;
            lblCalibrationStatus.Text = "No calibration data (will use global settings)";
            //
            // CustomFishingActions
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(453, 640);
            Controls.Add(groupBoxCalibration);
            Controls.Add(groupBoxPathPreview);
            Controls.Add(groupBoxRecorder);
            Controls.Add(updateSelectedActionItemBtn);
            Controls.Add(actionItemsListBox);
            Controls.Add(loadActionItemBtn);
            Controls.Add(saveActionItemBtn);
            Controls.Add(label1);
            Controls.Add(actionTimeTxtBox);
            Controls.Add(comboBox1);
            Controls.Add(removeItemBtn);
            Controls.Add(addItemBtn);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "CustomFishingActions";
            Text = "Custom Fishing Actions Manager";
            groupBoxCalibration.ResumeLayout(false);
            groupBoxPathPreview.ResumeLayout(false);
            groupBoxRecorder.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button addItemBtn;
        private System.Windows.Forms.Button removeItemBtn;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox actionTimeTxtBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button saveActionItemBtn;
        private System.Windows.Forms.Button loadActionItemBtn;
        private System.Windows.Forms.ListBox actionItemsListBox;
        private System.Windows.Forms.Button updateSelectedActionItemBtn;
        private System.Windows.Forms.GroupBox groupBoxRecorder;
        private System.Windows.Forms.Button btnStartRecording;
        private System.Windows.Forms.Button btnStopRecording;
        private System.Windows.Forms.Label lblRecordingStatus;
        private System.Windows.Forms.Button btnAddSellFish;
        private System.Windows.Forms.Label lblRecorderHelp;
        private System.Windows.Forms.Label lblLivePreview;
        private System.Windows.Forms.GroupBox groupBoxPathPreview;
        private System.Windows.Forms.Label lblPathPreview;
        private System.Windows.Forms.GroupBox groupBoxCalibration;
        private System.Windows.Forms.Button btnCalibrateScanArea;
        private System.Windows.Forms.Button btnCalibratePondColors;
        private System.Windows.Forms.Label lblCalibrationStatus;
    }
}