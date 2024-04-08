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
            listBox1 = new System.Windows.Forms.ListBox();
            button1 = new System.Windows.Forms.Button();
            button2 = new System.Windows.Forms.Button();
            comboBox1 = new System.Windows.Forms.ComboBox();
            textBox1 = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            button3 = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 15;
            listBox1.Location = new System.Drawing.Point(19, 15);
            listBox1.Name = "listBox1";
            listBox1.Size = new System.Drawing.Size(225, 244);
            listBox1.TabIndex = 0;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(250, 49);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(91, 28);
            button1.TabIndex = 1;
            button1.Text = "Add Item";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new System.Drawing.Point(250, 83);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(91, 28);
            button2.TabIndex = 2;
            button2.Text = "Remove Item";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "WALK FORWARDS", "WALK BACKWARDS", "TURN LEFT", "TURN RIGHT", "TIME" });
            comboBox1.Location = new System.Drawing.Point(250, 15);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new System.Drawing.Size(144, 23);
            comboBox1.TabIndex = 3;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // textBox1
            // 
            textBox1.Enabled = false;
            textBox1.Location = new System.Drawing.Point(250, 139);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(144, 23);
            textBox1.TabIndex = 4;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(250, 121);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(144, 15);
            label1.TabIndex = 5;
            label1.Text = "Time for action (seconds):";
            // 
            // button3
            // 
            button3.Location = new System.Drawing.Point(250, 213);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(134, 46);
            button3.TabIndex = 6;
            button3.Text = "Save Action Item";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // CustomFishingActions
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(410, 275);
            Controls.Add(button3);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(comboBox1);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(listBox1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Name = "CustomFishingActions";
            Text = "CustomFishingActions";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button3;
    }
}