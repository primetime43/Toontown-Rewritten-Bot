namespace ToonTown_Rewritten_Bot.Views
{
    partial class CustomGardeningActions
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
            this.actionItemsListBox = new System.Windows.Forms.ListBox();
            this.cmbAction = new System.Windows.Forms.ComboBox();
            this.numDuration = new System.Windows.Forms.NumericUpDown();
            this.addItemBtn = new System.Windows.Forms.Button();
            this.removeItemBtn = new System.Windows.Forms.Button();
            this.updateSelectedActionItemBtn = new System.Windows.Forms.Button();
            this.loadActionItemBtn = new System.Windows.Forms.Button();
            this.saveActionItemBtn = new System.Windows.Forms.Button();
            this.lblDuration = new System.Windows.Forms.Label();
            this.groupBoxPresets = new System.Windows.Forms.GroupBox();
            this.cmbFlower = new System.Windows.Forms.ComboBox();
            this.lblFlower = new System.Windows.Forms.Label();
            this.numWaterCount = new System.Windows.Forms.NumericUpDown();
            this.lblWaterCount = new System.Windows.Forms.Label();
            this.lblHelp = new System.Windows.Forms.Label();
            this.groupBoxSequence = new System.Windows.Forms.GroupBox();
            this.lblSequencePreview = new System.Windows.Forms.Label();
            this.groupBoxSummary = new System.Windows.Forms.GroupBox();
            this.lblSummary = new System.Windows.Forms.Label();
            this.lblAction = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWaterCount)).BeginInit();
            this.groupBoxSequence.SuspendLayout();
            this.groupBoxSummary.SuspendLayout();
            this.SuspendLayout();
            //
            // actionItemsListBox
            //
            this.actionItemsListBox.FormattingEnabled = true;
            this.actionItemsListBox.ItemHeight = 16;
            this.actionItemsListBox.Location = new System.Drawing.Point(12, 12);
            this.actionItemsListBox.Name = "actionItemsListBox";
            this.actionItemsListBox.Size = new System.Drawing.Size(320, 180);
            this.actionItemsListBox.TabIndex = 0;
            this.actionItemsListBox.SelectedIndexChanged += new System.EventHandler(this.actionItemsListBox_SelectedIndexChanged);
            //
            // lblAction
            //
            this.lblAction.AutoSize = true;
            this.lblAction.Location = new System.Drawing.Point(350, 12);
            this.lblAction.Name = "lblAction";
            this.lblAction.Size = new System.Drawing.Size(50, 16);
            this.lblAction.TabIndex = 1;
            this.lblAction.Text = "Action:";
            //
            // cmbAction
            //
            this.cmbAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAction.FormattingEnabled = true;
            this.cmbAction.Location = new System.Drawing.Point(350, 31);
            this.cmbAction.Name = "cmbAction";
            this.cmbAction.Size = new System.Drawing.Size(180, 24);
            this.cmbAction.TabIndex = 2;
            this.cmbAction.SelectedIndexChanged += new System.EventHandler(this.cmbAction_SelectedIndexChanged);
            //
            // lblDuration
            //
            this.lblDuration.AutoSize = true;
            this.lblDuration.Location = new System.Drawing.Point(350, 62);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(92, 16);
            this.lblDuration.TabIndex = 3;
            this.lblDuration.Text = "Duration (ms):";
            //
            // numDuration
            //
            this.numDuration.Location = new System.Drawing.Point(350, 81);
            this.numDuration.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numDuration.Name = "numDuration";
            this.numDuration.Size = new System.Drawing.Size(100, 22);
            this.numDuration.TabIndex = 4;
            this.numDuration.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            //
            // lblFlower
            //
            this.lblFlower.AutoSize = true;
            this.lblFlower.Location = new System.Drawing.Point(350, 62);
            this.lblFlower.Name = "lblFlower";
            this.lblFlower.Size = new System.Drawing.Size(51, 16);
            this.lblFlower.TabIndex = 5;
            this.lblFlower.Text = "Flower:";
            this.lblFlower.Visible = false;
            //
            // cmbFlower
            //
            this.cmbFlower.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFlower.FormattingEnabled = true;
            this.cmbFlower.Location = new System.Drawing.Point(350, 81);
            this.cmbFlower.Name = "cmbFlower";
            this.cmbFlower.Size = new System.Drawing.Size(180, 24);
            this.cmbFlower.TabIndex = 6;
            this.cmbFlower.Visible = false;
            //
            // lblWaterCount
            //
            this.lblWaterCount.AutoSize = true;
            this.lblWaterCount.Location = new System.Drawing.Point(350, 62);
            this.lblWaterCount.Name = "lblWaterCount";
            this.lblWaterCount.Size = new System.Drawing.Size(87, 16);
            this.lblWaterCount.TabIndex = 7;
            this.lblWaterCount.Text = "Water Count:";
            this.lblWaterCount.Visible = false;
            //
            // numWaterCount
            //
            this.numWaterCount.Location = new System.Drawing.Point(350, 81);
            this.numWaterCount.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            this.numWaterCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numWaterCount.Name = "numWaterCount";
            this.numWaterCount.Size = new System.Drawing.Size(80, 22);
            this.numWaterCount.TabIndex = 8;
            this.numWaterCount.Value = new decimal(new int[] { 3, 0, 0, 0 });
            this.numWaterCount.Visible = false;
            //
            // groupBoxPresets
            //
            this.groupBoxPresets.Location = new System.Drawing.Point(350, 109);
            this.groupBoxPresets.Name = "groupBoxPresets";
            this.groupBoxPresets.Size = new System.Drawing.Size(260, 50);
            this.groupBoxPresets.TabIndex = 9;
            this.groupBoxPresets.TabStop = false;
            this.groupBoxPresets.Text = "Presets";
            //
            // addItemBtn
            //
            this.addItemBtn.Location = new System.Drawing.Point(540, 29);
            this.addItemBtn.Name = "addItemBtn";
            this.addItemBtn.Size = new System.Drawing.Size(70, 28);
            this.addItemBtn.TabIndex = 10;
            this.addItemBtn.Text = "Add";
            this.addItemBtn.UseVisualStyleBackColor = true;
            this.addItemBtn.Click += new System.EventHandler(this.addItemBtn_Click);
            //
            // removeItemBtn
            //
            this.removeItemBtn.Location = new System.Drawing.Point(12, 198);
            this.removeItemBtn.Name = "removeItemBtn";
            this.removeItemBtn.Size = new System.Drawing.Size(100, 28);
            this.removeItemBtn.TabIndex = 11;
            this.removeItemBtn.Text = "Remove";
            this.removeItemBtn.UseVisualStyleBackColor = true;
            this.removeItemBtn.Click += new System.EventHandler(this.removeItemBtn_Click);
            //
            // updateSelectedActionItemBtn
            //
            this.updateSelectedActionItemBtn.Enabled = false;
            this.updateSelectedActionItemBtn.Location = new System.Drawing.Point(118, 198);
            this.updateSelectedActionItemBtn.Name = "updateSelectedActionItemBtn";
            this.updateSelectedActionItemBtn.Size = new System.Drawing.Size(100, 28);
            this.updateSelectedActionItemBtn.TabIndex = 12;
            this.updateSelectedActionItemBtn.Text = "Update";
            this.updateSelectedActionItemBtn.UseVisualStyleBackColor = true;
            this.updateSelectedActionItemBtn.Click += new System.EventHandler(this.updateSelectedActionItemBtn_Click);
            //
            // loadActionItemBtn
            //
            this.loadActionItemBtn.Location = new System.Drawing.Point(232, 198);
            this.loadActionItemBtn.Name = "loadActionItemBtn";
            this.loadActionItemBtn.Size = new System.Drawing.Size(100, 28);
            this.loadActionItemBtn.TabIndex = 13;
            this.loadActionItemBtn.Text = "Load...";
            this.loadActionItemBtn.UseVisualStyleBackColor = true;
            this.loadActionItemBtn.Click += new System.EventHandler(this.loadActionItemBtn_Click);
            //
            // saveActionItemBtn
            //
            this.saveActionItemBtn.Location = new System.Drawing.Point(540, 63);
            this.saveActionItemBtn.Name = "saveActionItemBtn";
            this.saveActionItemBtn.Size = new System.Drawing.Size(70, 28);
            this.saveActionItemBtn.TabIndex = 14;
            this.saveActionItemBtn.Text = "Save...";
            this.saveActionItemBtn.UseVisualStyleBackColor = true;
            this.saveActionItemBtn.Click += new System.EventHandler(this.saveActionItemBtn_Click);
            //
            // lblHelp
            //
            this.lblHelp.Location = new System.Drawing.Point(350, 165);
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Size = new System.Drawing.Size(260, 85);
            this.lblHelp.TabIndex = 15;
            this.lblHelp.Text = "Select an action to see help.";
            this.lblHelp.ForeColor = System.Drawing.Color.DarkSlateGray;
            //
            // groupBoxSequence
            //
            this.groupBoxSequence.Controls.Add(this.lblSequencePreview);
            this.groupBoxSequence.Location = new System.Drawing.Point(12, 235);
            this.groupBoxSequence.Name = "groupBoxSequence";
            this.groupBoxSequence.Size = new System.Drawing.Size(430, 55);
            this.groupBoxSequence.TabIndex = 16;
            this.groupBoxSequence.TabStop = false;
            this.groupBoxSequence.Text = "Sequence Preview";
            //
            // lblSequencePreview
            //
            this.lblSequencePreview.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblSequencePreview.Location = new System.Drawing.Point(6, 18);
            this.lblSequencePreview.Name = "lblSequencePreview";
            this.lblSequencePreview.Size = new System.Drawing.Size(418, 32);
            this.lblSequencePreview.TabIndex = 0;
            this.lblSequencePreview.Text = "(No actions)";
            //
            // groupBoxSummary
            //
            this.groupBoxSummary.Controls.Add(this.lblSummary);
            this.groupBoxSummary.Location = new System.Drawing.Point(455, 235);
            this.groupBoxSummary.Name = "groupBoxSummary";
            this.groupBoxSummary.Size = new System.Drawing.Size(155, 115);
            this.groupBoxSummary.TabIndex = 17;
            this.groupBoxSummary.TabStop = false;
            this.groupBoxSummary.Text = "Summary";
            //
            // lblSummary
            //
            this.lblSummary.Location = new System.Drawing.Point(6, 18);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(143, 90);
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Text = "Actions: 0\r\nPlants: 0\r\nWaters: 0\r\nEst. Time: 0s";
            //
            // CustomGardeningActions
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(625, 360);
            this.Controls.Add(this.groupBoxSummary);
            this.Controls.Add(this.groupBoxSequence);
            this.Controls.Add(this.lblHelp);
            this.Controls.Add(this.saveActionItemBtn);
            this.Controls.Add(this.loadActionItemBtn);
            this.Controls.Add(this.updateSelectedActionItemBtn);
            this.Controls.Add(this.removeItemBtn);
            this.Controls.Add(this.addItemBtn);
            this.Controls.Add(this.groupBoxPresets);
            this.Controls.Add(this.numWaterCount);
            this.Controls.Add(this.lblWaterCount);
            this.Controls.Add(this.cmbFlower);
            this.Controls.Add(this.lblFlower);
            this.Controls.Add(this.numDuration);
            this.Controls.Add(this.lblDuration);
            this.Controls.Add(this.cmbAction);
            this.Controls.Add(this.lblAction);
            this.Controls.Add(this.actionItemsListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "CustomGardeningActions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Custom Gardening Actions";
            ((System.ComponentModel.ISupportInitialize)(this.numDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWaterCount)).EndInit();
            this.groupBoxSequence.ResumeLayout(false);
            this.groupBoxSummary.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox actionItemsListBox;
        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.ComboBox cmbAction;
        private System.Windows.Forms.Label lblDuration;
        private System.Windows.Forms.NumericUpDown numDuration;
        private System.Windows.Forms.Label lblFlower;
        private System.Windows.Forms.ComboBox cmbFlower;
        private System.Windows.Forms.Label lblWaterCount;
        private System.Windows.Forms.NumericUpDown numWaterCount;
        private System.Windows.Forms.GroupBox groupBoxPresets;
        private System.Windows.Forms.Button addItemBtn;
        private System.Windows.Forms.Button removeItemBtn;
        private System.Windows.Forms.Button updateSelectedActionItemBtn;
        private System.Windows.Forms.Button loadActionItemBtn;
        private System.Windows.Forms.Button saveActionItemBtn;
        private System.Windows.Forms.Label lblHelp;
        private System.Windows.Forms.GroupBox groupBoxSequence;
        private System.Windows.Forms.Label lblSequencePreview;
        private System.Windows.Forms.GroupBox groupBoxSummary;
        private System.Windows.Forms.Label lblSummary;
    }
}
