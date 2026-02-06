using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    public class TemplateVariantManagerForm : Form
    {
        private readonly string _elementName;
        private FlowLayoutPanel _galleryPanel;
        private Button _btnAddVariant;
        private Button _btnDeleteSelected;
        private Button _btnClose;
        private Label _statusLabel;
        private int _selectedIndex = -1;
        private readonly List<Image> _loadedImages = new List<Image>();

        public TemplateVariantManagerForm(string elementName)
        {
            _elementName = elementName;
            InitializeComponents();
            LoadGallery();
        }

        private void InitializeComponents()
        {
            Text = $"Manage Variants: {_elementName}";
            ClientSize = new Size(620, 340);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(450, 280);

            // Gallery panel (scrollable, fills most of the form)
            _galleryPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(5),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(10, 10),
                Size = new Size(460, 280)
            };
            Controls.Add(_galleryPanel);

            // Button panel on the right
            int btnX = 480;

            _btnAddVariant = new Button
            {
                Text = "Add Variant",
                Location = new Point(btnX, 10),
                Size = new Size(120, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnAddVariant.Click += BtnAddVariant_Click;
            Controls.Add(_btnAddVariant);

            _btnDeleteSelected = new Button
            {
                Text = "Delete Selected",
                Location = new Point(btnX, 50),
                Size = new Size(120, 30),
                ForeColor = Color.Red,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnDeleteSelected.Click += BtnDeleteSelected_Click;
            Controls.Add(_btnDeleteSelected);

            _btnClose = new Button
            {
                Text = "Close",
                Location = new Point(btnX, 90),
                Size = new Size(120, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            // Status label at bottom
            _statusLabel = new Label
            {
                AutoSize = false,
                Location = new Point(10, 295),
                Size = new Size(600, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.Gray
            };
            Controls.Add(_statusLabel);
        }

        private void LoadGallery()
        {
            // Dispose previous images
            foreach (var img in _loadedImages)
                img?.Dispose();
            _loadedImages.Clear();

            _galleryPanel.Controls.Clear();
            _selectedIndex = -1;
            _btnDeleteSelected.Enabled = false;

            var allPaths = UIElementManager.Instance.GetAllTemplatePaths(_elementName);

            for (int i = 0; i < allPaths.Count; i++)
            {
                int variantIndex = i; // capture for closure
                string path = allPaths[i];

                Image img;
                try
                {
                    // Load via MemoryStream to avoid locking the file
                    byte[] bytes = File.ReadAllBytes(path);
                    var ms = new MemoryStream(bytes);
                    img = Image.FromStream(ms);
                }
                catch
                {
                    continue;
                }
                _loadedImages.Add(img);

                string label = variantIndex == 0 ? "Primary" : $"Variant {variantIndex + 1}";

                var variantGroup = new GroupBox
                {
                    Text = label,
                    Size = new Size(140, 150),
                    Margin = new Padding(5),
                    Tag = variantIndex
                };

                var pb = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = img,
                    Dock = DockStyle.Fill,
                    Cursor = Cursors.Hand
                };

                pb.Click += (s, e) => SelectVariant(variantIndex, variantGroup);
                variantGroup.Click += (s, e) => SelectVariant(variantIndex, variantGroup);

                variantGroup.Controls.Add(pb);
                _galleryPanel.Controls.Add(variantGroup);
            }

            UpdateStatus(allPaths.Count);
        }

        private void SelectVariant(int index, GroupBox selectedGroup)
        {
            _selectedIndex = index;
            _btnDeleteSelected.Enabled = true;

            // Update visual selection
            foreach (Control ctrl in _galleryPanel.Controls)
            {
                if (ctrl is GroupBox gb)
                {
                    gb.BackColor = (int)gb.Tag == index ? Color.LightBlue : SystemColors.Control;
                }
            }

            // Update status with selected variant dimensions
            if (index >= 0 && index < _loadedImages.Count)
            {
                var img = _loadedImages[index];
                string variantLabel = index == 0 ? "Primary" : $"Variant {index + 1}";
                _statusLabel.Text = $"{_loadedImages.Count} variant{(_loadedImages.Count != 1 ? "s" : "")} | Selected: {variantLabel} ({img.Width}x{img.Height})";
            }
        }

        private void UpdateStatus(int count)
        {
            _statusLabel.Text = $"{count} variant{(count != 1 ? "s" : "")}";
        }

        private void BtnAddVariant_Click(object sender, EventArgs e)
        {
            bool captured = TemplateCaptureForm.CaptureVariant(_elementName);
            if (captured)
            {
                LoadGallery();
            }
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (_selectedIndex < 0)
                return;

            int variantCount = UIElementManager.Instance.GetVariantCount(_elementName);

            // Don't allow deleting the last variant without warning
            string variantLabel = _selectedIndex == 0 ? "Primary" : $"Variant {_selectedIndex + 1}";
            string message = variantCount == 1
                ? $"This is the only variant for '{_elementName}'. Deleting it will remove the template entirely.\n\nContinue?"
                : $"Delete {variantLabel}?\n\nRemaining variants will be renumbered automatically.";

            var result = MessageBox.Show(message, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;

            UIElementManager.Instance.DeleteTemplateVariant(_elementName, _selectedIndex);
            LoadGallery();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var img in _loadedImages)
                    img?.Dispose();
                _loadedImages.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
