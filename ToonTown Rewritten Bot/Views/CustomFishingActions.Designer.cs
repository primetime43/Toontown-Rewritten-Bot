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
            // CustomFishingActions
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
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "CustomFishingActions";
            Text = "Custom Fishing Actions Manager";
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
    }
}