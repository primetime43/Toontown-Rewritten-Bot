using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private void devResetCoordinatesBtn_Click(object sender, EventArgs e)
        {
            CoordinatesManager.CreateFreshCoordinatesFile();
            MessageBox.Show("All coordinates reset!");
        }

        private void LoadCoordinatesIntoResetBox()
        {
            devCoordinatesComboBox.Items.Clear();
            var descriptions = CoordinateActions.GetAllDescriptions();
            devCoordinatesComboBox.Items.AddRange(descriptions.Values.ToArray());
        }

        private async void devUpdateCoordinateBtn_Click(object sender, EventArgs e)
        {
            string selectedDescription = devCoordinatesComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedDescription))
            {
                MessageBox.Show("Please select a valid item from the list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string keyToUpdate = CoordinateActions.GetKeyFromDescription(selectedDescription);
            if (keyToUpdate == null)
            {
                MessageBox.Show("No valid key found for the selected description.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                await _coordinatesManagerService.ManualUpdateCoordinates(keyToUpdate);
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show("Coordinates updated for " + selectedDescription);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to perform this action: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void aboutBtn_Click(object sender, EventArgs e)
        {
            AboutBox1 aboutBox = new AboutBox1();
            try
            {
                aboutBox.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void githubLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/primetime43/Toontown-Rewritten-Bot",
                UseShellExecute = true
            });
        }

        // Open Image Recognition Debug Window
        private void devOpenDebugBtn_Click(object sender, EventArgs e)
        {
            var debugForm = new ImageRecognitionDebugForm();
            debugForm.Show();
        }

        // Open Log Viewer Window
        private void devOpenLogViewerBtn_Click(object sender, EventArgs e)
        {
            var logViewer = new LogViewerForm();
            logViewer.Show();
        }

        // Download OCR data automatically
        private async void devDownloadOcrBtn_Click(object sender, EventArgs e)
        {
            // Check if already exists
            if (TessdataDownloader.LanguageDataExists())
            {
                MessageBox.Show(
                    "OCR data is already downloaded and ready to use!\n\n" +
                    "Click 'Open Debug Window' to test the OCR functionality.",
                    "OCR Data Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Download
            var button = sender as Button;
            if (button != null)
            {
                button.Enabled = false;
                button.Text = "Downloading...";
            }

            try
            {
                bool success = await TessdataDownloader.EnsureLanguageDataExistsAsync();

                if (success)
                {
                    MessageBox.Show(
                        "OCR data downloaded successfully!\n\n" +
                        "Click 'Open Debug Window' to test the OCR functionality.",
                        "Download Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to download OCR data.\n\n" +
                        "Please check your internet connection and try again.",
                        "Download Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (button != null)
                {
                    button.Enabled = true;
                    button.Text = "Download OCR Data";
                }
            }
        }

        // Template management methods
        private void LoadTemplateItemsComboBox()
        {
            comboBoxTemplateItems.Items.Clear();

            // Load from file-based TemplateDefinitionManager
            var definitions = TemplateDefinitionManager.Instance.GetAllDefinitions();
            foreach (var def in definitions)
            {
                comboBoxTemplateItems.Items.Add($"[{def.Category}] {def.Name}");
            }

            if (comboBoxTemplateItems.Items.Count > 0)
            {
                comboBoxTemplateItems.SelectedIndex = 0;
            }
        }

        private string GetSelectedTemplateName()
        {
            if (comboBoxTemplateItems.SelectedItem == null)
                return null;

            string selected = comboBoxTemplateItems.SelectedItem.ToString();
            // Extract name from "[Category] Name" format
            int bracketEnd = selected.IndexOf("] ");
            if (bracketEnd >= 0)
                return selected.Substring(bracketEnd + 2);
            return selected;
        }

        private void comboBoxTemplateItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItem = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(selectedItem))
                return;

            // Check if template exists and show variant count
            int variantCount = UIElementManager.Instance.GetVariantCount(selectedItem);

            if (variantCount > 0)
            {
                string variantText = variantCount == 1
                    ? "Template exists (1 variant)"
                    : $"Template exists ({variantCount} variants)";
                labelTemplateStatus.Text = variantText;
                labelTemplateStatus.ForeColor = Color.Green;
                btnViewTemplate.Enabled = true;
            }
            else
            {
                labelTemplateStatus.Text = $"No template - click 'Capture' to create";
                labelTemplateStatus.ForeColor = Color.Orange;
                btnViewTemplate.Enabled = false;
            }
        }

        private void btnCaptureTemplate_Click(object sender, EventArgs e)
        {
            string selectedItem = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool captured = false;

            if (UIElementManager.Instance.HasTemplate(selectedItem))
            {
                // Template already exists - ask user what to do
                var result = MessageBox.Show(
                    $"A template already exists for '{selectedItem}'.\n\n" +
                    "Yes = Replace existing primary template\n" +
                    "No = Add as a new variant\n" +
                    "Cancel = Do nothing",
                    "Template Exists",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    captured = TemplateCaptureForm.CaptureTemplate(selectedItem);
                }
                else if (result == DialogResult.No)
                {
                    captured = TemplateCaptureForm.CaptureVariant(selectedItem);
                }
                // Cancel = do nothing
            }
            else
            {
                captured = TemplateCaptureForm.CaptureTemplate(selectedItem);
            }

            if (captured)
            {
                int count = UIElementManager.Instance.GetVariantCount(selectedItem);
                MessageBox.Show($"Template captured successfully for: {selectedItem}\n({count} variant{(count != 1 ? "s" : "")} total)", "Template Captured", MessageBoxButtons.OK, MessageBoxIcon.Information);
                comboBoxTemplateItems_SelectedIndexChanged(sender, e);
            }
        }

        private void btnViewTemplate_Click(object sender, EventArgs e)
        {
            string selectedItem = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var allPaths = UIElementManager.Instance.GetAllTemplatePaths(selectedItem);

            if (allPaths.Count == 0)
            {
                MessageBox.Show($"No template found for: {selectedItem}\n\nClick 'Capture Template' to create one.", "Template Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var viewerForm = new Form())
                {
                    viewerForm.Text = $"Template: {selectedItem} ({allPaths.Count} variant{(allPaths.Count != 1 ? "s" : "")})";
                    viewerForm.StartPosition = FormStartPosition.CenterParent;
                    viewerForm.FormBorderStyle = FormBorderStyle.Sizable;
                    viewerForm.MinimumSize = new Size(300, 200);

                    var imagesToDispose = new System.Collections.Generic.List<Image>();

                    if (allPaths.Count == 1)
                    {
                        // Single variant - simple viewer
                        var img = Image.FromFile(allPaths[0]);
                        imagesToDispose.Add(img);

                        var pictureBox = new PictureBox
                        {
                            Dock = DockStyle.Fill,
                            SizeMode = PictureBoxSizeMode.Zoom,
                            Image = img
                        };
                        viewerForm.Controls.Add(pictureBox);

                        viewerForm.ClientSize = new Size(
                            Math.Max(200, Math.Min(img.Width + 20, 600)),
                            Math.Max(150, Math.Min(img.Height + 50, 400))
                        );
                    }
                    else
                    {
                        // Multiple variants - gallery view
                        var galleryPanel = new FlowLayoutPanel
                        {
                            Dock = DockStyle.Fill,
                            AutoScroll = true,
                            FlowDirection = FlowDirection.LeftToRight,
                            WrapContents = true,
                            Padding = new Padding(10)
                        };
                        viewerForm.Controls.Add(galleryPanel);

                        for (int i = 0; i < allPaths.Count; i++)
                        {
                            var img = Image.FromFile(allPaths[i]);
                            imagesToDispose.Add(img);

                            var variantGroup = new GroupBox
                            {
                                Text = i == 0 ? "Primary" : $"Variant {i + 1}",
                                Size = new Size(180, 160),
                                Margin = new Padding(5)
                            };

                            var pb = new PictureBox
                            {
                                Dock = DockStyle.Fill,
                                SizeMode = PictureBoxSizeMode.Zoom,
                                Image = img
                            };
                            variantGroup.Controls.Add(pb);
                            galleryPanel.Controls.Add(variantGroup);
                        }

                        viewerForm.ClientSize = new Size(
                            Math.Min(allPaths.Count * 200 + 30, 800),
                            230
                        );
                    }

                    var openFolderBtn = new Button
                    {
                        Text = "Open Folder",
                        Dock = DockStyle.Bottom,
                        Height = 30
                    };
                    openFolderBtn.Click += (s, args) =>
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{allPaths[0]}\"");
                    };
                    viewerForm.Controls.Add(openFolderBtn);

                    viewerForm.ShowDialog(this);

                    foreach (var img in imagesToDispose)
                        img?.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnManageVariants_Click(object sender, EventArgs e)
        {
            string selectedItem = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new TemplateVariantManagerForm(selectedItem))
            {
                form.ShowDialog(this);
            }

            // Refresh status after managing variants
            comboBoxTemplateItems_SelectedIndexChanged(sender, e);
        }

        private void btnAddTemplateItem_Click(object sender, EventArgs e)
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "Add New Template Item";
                inputForm.ClientSize = new Size(380, 180);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var lblName = new Label { Text = "Item Name:", Location = new Point(15, 15), AutoSize = true };
                var txtName = new TextBox { Location = new Point(15, 35), Size = new Size(350, 25) };

                var lblCategory = new Label { Text = "Category (select existing or type new):", Location = new Point(15, 70), AutoSize = true };
                var cmbCategory = new ComboBox
                {
                    Location = new Point(15, 90),
                    Size = new Size(350, 25),
                    DropDownStyle = ComboBoxStyle.DropDown
                };

                // Add existing categories as suggestions
                var categories = TemplateDefinitionManager.Instance.GetCategories();
                cmbCategory.Items.AddRange(categories.ToArray());
                cmbCategory.Text = categories.Count > 0 ? categories[0] : "Custom";

                var btnOk = new Button { Text = "Add", Location = new Point(205, 135), Size = new Size(75, 30), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Location = new Point(290, 135), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

                inputForm.Controls.AddRange(new Control[] { lblName, txtName, lblCategory, cmbCategory, btnOk, btnCancel });
                inputForm.AcceptButton = btnOk;
                inputForm.CancelButton = btnCancel;

                if (inputForm.ShowDialog(this) == DialogResult.OK)
                {
                    string name = txtName.Text.Trim();
                    string category = string.IsNullOrWhiteSpace(cmbCategory.Text) ? "Custom" : cmbCategory.Text.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        MessageBox.Show("Please enter a name for the template item.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (TemplateDefinitionManager.Instance.AddDefinition(name, category))
                    {
                        MessageBox.Show($"Added new template item: {name}\n\nYou can now capture a template for it.", "Item Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadTemplateItemsComboBox();

                        // Select the newly added item
                        for (int i = 0; i < comboBoxTemplateItems.Items.Count; i++)
                        {
                            if (comboBoxTemplateItems.Items[i].ToString().Contains(name))
                            {
                                comboBoxTemplateItems.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"An item with that name already exists.", "Duplicate Item", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void btnOpenTemplateDefinitions_Click(object sender, EventArgs e)
        {
            string filePath = TemplateDefinitionManager.Instance.GetDefinitionsFilePath();

            if (!System.IO.File.Exists(filePath))
            {
                MessageBox.Show("Definitions file not found. It will be created when you add the first item.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start("notepad.exe", filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEditTemplate_Click(object sender, EventArgs e)
        {
            string currentName = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(currentName))
            {
                MessageBox.Show("Please select a template to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var definition = TemplateDefinitionManager.Instance.GetDefinition(currentName);
            if (definition == null)
            {
                MessageBox.Show("Template definition not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dialog = new Form())
            {
                dialog.Text = "Edit Template";
                dialog.ClientSize = new Size(380, 180);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var nameLabel = new Label { Text = "Name:", Location = new Point(15, 20), AutoSize = true };
                var nameTextBox = new TextBox { Text = definition.Name, Location = new Point(80, 17), Size = new Size(280, 23) };

                var categoryLabel = new Label { Text = "Category:", Location = new Point(15, 55), AutoSize = true };
                var categoryComboBox = new ComboBox { Text = definition.Category, Location = new Point(80, 52), Size = new Size(280, 23), DropDownStyle = ComboBoxStyle.DropDown };

                // Add existing categories
                foreach (var cat in TemplateDefinitionManager.Instance.GetCategories())
                    categoryComboBox.Items.Add(cat);

                var saveBtn = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(185, 130), Size = new Size(80, 30) };
                var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(275, 130), Size = new Size(80, 30) };

                dialog.Controls.AddRange(new Control[] { nameLabel, nameTextBox, categoryLabel, categoryComboBox, saveBtn, cancelBtn });
                dialog.AcceptButton = saveBtn;
                dialog.CancelButton = cancelBtn;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string newName = nameTextBox.Text.Trim();
                    string newCategory = categoryComboBox.Text.Trim();

                    if (string.IsNullOrEmpty(newName))
                    {
                        MessageBox.Show("Name cannot be empty.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // If name changed, rename the template file too
                    if (!currentName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                    {
                        string oldPath = UIElementManager.Instance.GetTemplatePath(currentName);
                        string newPath = UIElementManager.Instance.GetTemplatePath(newName);

                        if (System.IO.File.Exists(oldPath) && !System.IO.File.Exists(newPath))
                        {
                            try
                            {
                                System.IO.File.Move(oldPath, newPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Failed to rename template file: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }

                    if (TemplateDefinitionManager.Instance.UpdateDefinition(currentName, newName, newCategory))
                    {
                        MessageBox.Show($"Updated template: {newName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadTemplateItemsComboBox();

                        // Re-select the renamed item (format is "[Category] Name")
                        for (int i = 0; i < comboBoxTemplateItems.Items.Count; i++)
                        {
                            if (comboBoxTemplateItems.Items[i].ToString().EndsWith("] " + newName))
                            {
                                comboBoxTemplateItems.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to update template. Name may already exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnDeleteTemplate_Click(object sender, EventArgs e)
        {
            string templateName = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(templateName))
            {
                MessageBox.Show("Please select a template to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var allPaths = UIElementManager.Instance.GetAllTemplatePaths(templateName);
            int variantCount = allPaths.Count;

            if (variantCount > 1)
            {
                // Multiple variants - let user pick which to delete
                using (var deleteForm = new Form())
                {
                    deleteForm.Text = "Delete Template Variants";
                    deleteForm.ClientSize = new Size(350, 250);
                    deleteForm.StartPosition = FormStartPosition.CenterParent;
                    deleteForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    deleteForm.MaximizeBox = false;
                    deleteForm.MinimizeBox = false;

                    var label = new Label
                    {
                        Text = $"'{templateName}' has {variantCount} variants.\nSelect which to delete:",
                        Location = new Point(15, 10),
                        AutoSize = true
                    };
                    deleteForm.Controls.Add(label);

                    var checkedListBox = new CheckedListBox
                    {
                        Location = new Point(15, 45),
                        Size = new Size(320, 120),
                        CheckOnClick = true
                    };

                    for (int i = 0; i < variantCount; i++)
                    {
                        string itemLabel = i == 0
                            ? $"Primary ({System.IO.Path.GetFileName(allPaths[i])})"
                            : $"Variant {i + 1} ({System.IO.Path.GetFileName(allPaths[i])})";
                        checkedListBox.Items.Add(itemLabel);
                    }
                    deleteForm.Controls.Add(checkedListBox);

                    var deleteAllBtn = new Button { Text = "Delete All", Location = new Point(15, 175), Size = new Size(90, 30) };
                    var deleteSelectedBtn = new Button { Text = "Delete Selected", Location = new Point(115, 175), Size = new Size(110, 30) };
                    var cancelBtn = new Button { Text = "Cancel", Location = new Point(255, 175), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };

                    deleteAllBtn.Click += (s, args) =>
                    {
                        for (int i = variantCount - 1; i >= 0; i--)
                            UIElementManager.Instance.DeleteTemplateVariant(templateName, i);
                        MessageBox.Show("All template variants deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        deleteForm.DialogResult = DialogResult.OK;
                        deleteForm.Close();
                    };

                    deleteSelectedBtn.Click += (s, args) =>
                    {
                        var indicesToDelete = new System.Collections.Generic.List<int>();
                        for (int i = 0; i < checkedListBox.Items.Count; i++)
                        {
                            if (checkedListBox.GetItemChecked(i))
                                indicesToDelete.Add(i);
                        }

                        if (indicesToDelete.Count == 0)
                        {
                            MessageBox.Show("Please check at least one variant to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Delete in reverse order to maintain correct indices
                        indicesToDelete.Sort();
                        indicesToDelete.Reverse();
                        foreach (int idx in indicesToDelete)
                            UIElementManager.Instance.DeleteTemplateVariant(templateName, idx);

                        MessageBox.Show($"Deleted {indicesToDelete.Count} variant(s).", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        deleteForm.DialogResult = DialogResult.OK;
                        deleteForm.Close();
                    };

                    deleteForm.Controls.AddRange(new Control[] { deleteAllBtn, deleteSelectedBtn, cancelBtn });
                    deleteForm.CancelButton = cancelBtn;

                    if (deleteForm.ShowDialog(this) == DialogResult.OK)
                    {
                        comboBoxTemplateItems_SelectedIndexChanged(sender, e);
                    }
                }
            }
            else
            {
                // Single variant or definition-only delete
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the template definition '{templateName}'?\n\nThis will also delete the template image file if it exists.",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // Delete the template file(s)
                    if (variantCount > 0)
                        UIElementManager.Instance.DeleteTemplateVariant(templateName, 0);

                    if (TemplateDefinitionManager.Instance.RemoveDefinition(templateName))
                    {
                        MessageBox.Show($"Deleted template definition: {templateName}", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadTemplateItemsComboBox();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete template definition.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
