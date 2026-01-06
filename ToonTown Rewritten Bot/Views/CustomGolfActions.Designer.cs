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
            loadActionItemBtn.Location = new System.Drawing.Point(12, 273);
            loadActionItemBtn.Name = "loadActionItemBtn";
            loadActionItemBtn.Size = new System.Drawing.Size(144, 34);
            loadActionItemBtn.TabIndex = 16;
            loadActionItemBtn.Text = "Load Action Item";
            loadActionItemBtn.UseVisualStyleBackColor = true;
            loadActionItemBtn.Click += loadActionItemBtn_Click;
            // 
            // saveActionItemBtn
            // 
            saveActionItemBtn.Location = new System.Drawing.Point(172, 273);
            saveActionItemBtn.Name = "saveActionItemBtn";
            saveActionItemBtn.Size = new System.Drawing.Size(144, 34);
            saveActionItemBtn.TabIndex = 15;
            saveActionItemBtn.Text = "Save Action Item";
            saveActionItemBtn.UseVisualStyleBackColor = true;
            saveActionItemBtn.Click += saveActionItemBtn_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(248, 160);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(117, 15);
            label1.TabIndex = 14;
            label1.Text = "Time for action (ms):";
            // 
            // actionTimeTxtBox
            // 
            actionTimeTxtBox.Enabled = false;
            actionTimeTxtBox.Location = new System.Drawing.Point(248, 178);
            actionTimeTxtBox.Name = "actionTimeTxtBox";
            actionTimeTxtBox.Size = new System.Drawing.Size(144, 23);
            actionTimeTxtBox.TabIndex = 13;
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
            removeItemBtn.Text = "Remove Item";
            removeItemBtn.UseVisualStyleBackColor = true;
            removeItemBtn.Click += removeItemBtn_Click;
            // 
            // addItemBtn
            // 
            addItemBtn.Location = new System.Drawing.Point(248, 48);
            addItemBtn.Name = "addItemBtn";
            addItemBtn.Size = new System.Drawing.Size(91, 28);
            addItemBtn.TabIndex = 10;
            addItemBtn.Text = "Add Item";
            addItemBtn.UseVisualStyleBackColor = true;
            addItemBtn.Click += addItemBtn_Click;
            //
            // helpLabel
            //
            helpLabel.Location = new System.Drawing.Point(248, 210);
            helpLabel.Name = "helpLabel";
            helpLabel.Size = new System.Drawing.Size(200, 100);
            helpLabel.TabIndex = 19;
            helpLabel.Text = "Select an action to see help.";
            helpLabel.ForeColor = System.Drawing.Color.DarkSlateGray;
            //
            // CustomGolfActions
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(453, 320);
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
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "CustomGolfActions";
            Text = "Custom Golf Actions Manager";
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
    }
}