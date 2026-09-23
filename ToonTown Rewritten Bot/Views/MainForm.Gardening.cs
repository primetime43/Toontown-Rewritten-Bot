using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        #region Gardening - Flower Planting

        private static readonly System.Collections.Generic.Dictionary<string, string> _plantComboDictionary = new System.Collections.Generic.Dictionary<string, string>
        {
            // 1 bean
            { "Laff-o-dil", "g" }, { "Dandy Pansy", "o" }, { "What-in Carnation", "i" },
            { "School Daisy", "y" }, { "Lily-of-the-Alley", "c" },
            // 2 beans
            { "Daffy Dill", "gc" }, { "Chim Pansy", "oc" }, { "Instant Carnation", "iy" },
            { "Lazy Daisy", "yr" }, { "Lily Pad", "cg" },
            // 3 beans
            { "Summer's Last Rose", "rrr" }, { "Potsen Pansy", "orr" }, { "Hybrid Carnation", "irr" },
            { "Midsummer Daisy", "yrg" }, { "Tiger Lily", "coo" },
            // 4 beans
            { "Corn Rose", "ryoy" }, { "Giraff-o-dil", "giyy" }, { "Marzi Pansy", "oyyr" },
            { "Freshasa Daisy", "yrco" }, { "Livered Lily", "cooi" },
            // 5 beans
            { "Time and a half-o-dil", "gibii" }, { "Onelip", "urbuu" }, { "Side Carnation", "irgbr" },
            { "Whoopsie Daisy", "yrooo" }, { "Chili Lily", "crrrr" },
            // 6 beans
            { "Tinted Rose", "rioroi" }, { "Smarty Pansy", "oiiobi" }, { "Twolip", "urrruu" },
            { "Upsy Daisy", "ybcubb" }, { "Silly Lily", "cruuuu" },
            // 7 beans
            { "Stinking Rose", "rcoiucc" }, { "Car Petunia", "bubucbb" }, { "Model Carnation", "iggggyg" },
            { "Crazy Daisy", "ygroggg" }, { "Indubitab Lily", "cucbcbb" },
            // 8 beans
            { "Istilla Rose", "rbuubbib" }, { "Threelip", "uyyuyouy" }, { "Platoonia", "biibroyy" },
            { "Hazy Dazy", "ybucurou" }, { "Dilly Lilly", "cbyycbyy" }
        };

        private void beanCountComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            flowerComboBox.Items.Clear();
            beanSequencePanel.Controls.Clear();

            int beanCount = beanCountComboBox.SelectedIndex + 1;
            string[][] flowersByCount = new string[][]
            {
                new[] { "Laff-o-dil", "Dandy Pansy", "What-in Carnation", "School Daisy", "Lily-of-the-Alley" },
                new[] { "Daffy Dill", "Chim Pansy", "Instant Carnation", "Lazy Daisy", "Lily Pad" },
                new[] { "Summer's Last Rose", "Potsen Pansy", "Hybrid Carnation", "Midsummer Daisy", "Tiger Lily" },
                new[] { "Corn Rose", "Giraff-o-dil", "Marzi Pansy", "Freshasa Daisy", "Livered Lily" },
                new[] { "Time and a half-o-dil", "Onelip", "Side Carnation", "Whoopsie Daisy", "Chili Lily" },
                new[] { "Tinted Rose", "Smarty Pansy", "Twolip", "Upsy Daisy", "Silly Lily" },
                new[] { "Stinking Rose", "Car Petunia", "Model Carnation", "Crazy Daisy", "Indubitab Lily" },
                new[] { "Istilla Rose", "Threelip", "Platoonia", "Hazy Dazy", "Dilly Lilly" }
            };

            if (beanCount >= 1 && beanCount <= 8)
            {
                flowerComboBox.Items.AddRange(flowersByCount[beanCount - 1]);
            }
        }

        private void flowerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = flowerComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected) || !_plantComboDictionary.ContainsKey(selected))
                return;

            UpdateBeanSequencePreview(_plantComboDictionary[selected]);
        }

        private void UpdateBeanSequencePreview(string beanCombo)
        {
            beanSequencePanel.Controls.Clear();

            for (int i = 0; i < beanCombo.Length; i++)
            {
                var panel = new Panel
                {
                    Size = new Size(18, 18),
                    Location = new Point(i * 22, 3),
                    BackColor = GetBeanColor(beanCombo[i])
                };
                beanSequencePanel.Controls.Add(panel);
            }
        }

        private static Color GetBeanColor(char bean)
        {
            switch (bean)
            {
                case 'r': return Color.Red;
                case 'g': return Color.Green;
                case 'o': return Color.Orange;
                case 'u': return Color.Purple;
                case 'b': return Color.Blue;
                case 'i': return Color.HotPink;
                case 'y': return Color.Gold;
                case 'c': return Color.Cyan;
                case 's': return Color.Silver;
                default: return Color.Gray;
            }
        }

        private async void plantFlowerBtn_Click(object sender, EventArgs e)
        {
            if (_gardeningTaskActive) return;
            string selectedFlower = flowerComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedFlower) || !_plantComboDictionary.ContainsKey(selectedFlower))
            {
                MessageBox.Show("Please select a bean count and flower first.", "No Flower Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                "Make sure you're at the flower bed before pressing OK!",
                "Ready to Plant?", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (confirmation != DialogResult.OK)
                return;

            string beanCombo = _plantComboDictionary[selectedFlower];
            // Read the spinner on the UI thread before going async — the planting service
            // applies this many waters at the end of the routine.
            int waterCount = (int)waterPlantNumericUpDown.Value;

            SetPlantStatus($"Planting {selectedFlower}...", UiColors.MutedText);
            SetGardeningTaskActive(true);

            try
            {
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                await Task.Run(() => Services.Gardening.PlantFlowerAsync(beanCombo, selectedFlower, waterCount, _cancellationTokenSource.Token));
                SetPlantStatus($"✓ {selectedFlower} planted ({DateTime.Now:HH:mm:ss})", UiColors.Success);
            }
            catch (OperationCanceledException)
            {
                SetPlantStatus($"⚠ Planting cancelled ({DateTime.Now:HH:mm:ss})", UiColors.Warning);
            }
            catch (Exception ex)
            {
                SetPlantStatus($"✗ Error ({DateTime.Now:HH:mm:ss})", UiColors.Danger);
                MessageBox.Show($"An error occurred: {ex.Message}", "Gardening Error", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
            finally { SetGardeningTaskActive(false); }
        }

        private void SetPlantStatus(string text, Color color)
        {
            plantStatusLabel.Text = text;
            plantStatusLabel.ForeColor = color;
            toolTip1.SetToolTip(plantStatusLabel, text);
        }

        private void stopPlantingBtn_Click(object sender, EventArgs e)
        {
            if (!_gardeningTaskActive || _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            SetPlantStatus("Stopping gardening...", UiColors.Warning);
            stopPlantingBtn.Enabled = false;
        }

        #endregion

        private async void waterPlantBtn_Click(object sender, EventArgs e)
        {
            if (_gardeningTaskActive || waterPlantNumericUpDown.Value == 0) return;
            SetGardeningTaskActive(true);
            SetPlantStatus("Watering plant...", UiColors.MutedText);
            try
            {
                // Ensure cancellation token source exists
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                await Services.Gardening.WaterPlantAsync((int)waterPlantNumericUpDown.Value, _cancellationTokenSource.Token);
                SetPlantStatus("Watering complete.", UiColors.Success);
            }
            catch (OperationCanceledException)
            {
                SetPlantStatus("Watering cancelled.", UiColors.Warning);
            }
            catch (Exception ex)
            {
                // General error handling
                SetPlantStatus("Watering failed.", UiColors.Danger);
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally { SetGardeningTaskActive(false); }
        }

        private async void removePlantBtn_Click(object sender, EventArgs e)
        {
            if (_gardeningTaskActive) return;
            SetGardeningTaskActive(true);
            SetPlantStatus("Removing plant...", UiColors.MutedText);
            try
            {
                // Ensure cancellation token source exists
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                await Services.Gardening.RemovePlantAsync(_cancellationTokenSource.Token);
                SetPlantStatus("Plant removed.", UiColors.Success);
            }
            catch (OperationCanceledException)
            {
                SetPlantStatus("Removing plant cancelled.", UiColors.Warning);
            }
            catch (Exception ex)
            {
                // General error handling
                SetPlantStatus("Removing plant failed.", UiColors.Danger);
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally { SetGardeningTaskActive(false); }
        }

        private void wizardCustomGardeningBtn_Click(object sender, EventArgs e)
        {
            string selected = customGardeningFilesComboBox.SelectedItem?.ToString();
            using (var form = new CustomGardeningWizardForm())
            {
                form.ShowDialog(this);
            }

            LoadCustomActions("Gardening", customGardeningFilesComboBox);
            if (selected != null) customGardeningFilesComboBox.SelectedItem = selected;
            UpdateGardeningControls();
        }

        private void editCustomGardeningBtn_Click(object sender, EventArgs e)
        {
            string selected = customGardeningFilesComboBox.SelectedItem?.ToString();
            if (selected == null) return;
            string folder = (string)CoreFunctionality.ManageCustomActionsFolder("Gardening", false);
            using (var form = new CustomGardeningActions(Path.Combine(folder, selected + ".json")))
            {
                form.ShowDialog(this);
            }

            LoadCustomActions("Gardening", customGardeningFilesComboBox);
            customGardeningFilesComboBox.SelectedItem = selected;
            UpdateGardeningControls();
        }

        private void calibrateGardeningBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Calibration Instructions:\n\n" +
                "1. Open a flower bed in Toontown (click on it) so the jellybean buttons are visible\n" +
                "2. Click OK to open the calibration overlay\n" +
                "3. Drag the green box to cover ALL jellybean buttons\n" +
                "4. The overlay will show detected beans with colored circles\n" +
                "5. Press ENTER to save when all beans are detected",
                "Jellybean Detection Calibration",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            using (var form = new GardeningCalibrationForm())
            {
                form.ShowDialog(this);
            }
        }

        private async void startCustomGardeningBtn_Click(object sender, EventArgs e)
        {
            if (_gardeningTaskActive) return;
            string selectedFileName = customGardeningFilesComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedFileName))
            {
                MessageBox.Show("Please select a gardening routine file.");
                return;
            }

            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Gardening", false);
            string filePath = Path.Combine(folderPath, selectedFileName + ".json");

            var result = Utilities.CustomGardeningActionFileManager.Load(filePath);
            if (!result.Success)
            {
                MessageBox.Show($"Failed to load file: {result.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var gardeningKeys = new Models.GardeningActionKeys();
            // Capture the post-plant water count from the UI now — we won't be on the UI
            // thread once we start awaiting.
            int plantWaterCount = (int)waterPlantNumericUpDown.Value;
            SetGardeningTaskActive(true);
            SetPlantStatus($"Running {selectedFileName}...", UiColors.MutedText);

            try
            {
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                CoreFunctionality.FocusTTRWindow();
                await Task.Delay(1000, _cancellationTokenSource.Token);

                foreach (var action in result.File.Actions)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    switch (action.Action)
                    {
                        case "WALK FORWARD":
                        case "WALK BACKWARD":
                        case "WALK LEFT":
                        case "WALK RIGHT":
                        case "TURN LEFT":
                        case "TURN RIGHT":
                            if (gardeningKeys.ActionKeyMap.TryGetValue(action.Action, out var keyCode))
                            {
                                // Translate the default control key into whatever the user has bound in TTR.
                                keyCode = Models.GameControls.Remap(keyCode);
                                WindowsInput.InputSimulator.SimulateKeyDown(keyCode);
                                try { await Task.Delay(action.Duration, _cancellationTokenSource.Token); }
                                finally { WindowsInput.InputSimulator.SimulateKeyUp(keyCode); }
                            }
                            break;

                        case "DELAY":
                            await Task.Delay(action.Duration, _cancellationTokenSource.Token);
                            break;

                        case "PLANT FLOWER":
                            if (!string.IsNullOrEmpty(action.BeanSequence))
                            {
                                string flowerName = !string.IsNullOrEmpty(action.FlowerName) ? action.FlowerName : "Custom Flower";
                                // Per-action WaterCount on PLANT FLOWER overrides the UI setting
                                // when authored explicitly; otherwise fall back to the user's spinner.
                                int plantWaters = action.WaterCount > 0 ? action.WaterCount : plantWaterCount;
                                await Services.Gardening.PlantFlowerAsync(action.BeanSequence, flowerName, plantWaters, _cancellationTokenSource.Token);
                            }
                            break;

                        case "WATER PLANT":
                            int waterCount = action.WaterCount > 0 ? action.WaterCount : 1;
                            await Services.Gardening.WaterPlantAsync(waterCount, _cancellationTokenSource.Token);
                            break;

                        case "REMOVE PLANT":
                            await Services.Gardening.RemovePlantAsync(_cancellationTokenSource.Token);
                            break;
                    }
                }

                SetPlantStatus($"Completed: {selectedFileName}", UiColors.Success);
            }
            catch (OperationCanceledException)
            {
                SetPlantStatus("Gardening routine cancelled.", UiColors.Warning);
            }
            catch (Exception ex)
            {
                SetPlantStatus("Gardening routine failed.", UiColors.Danger);
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetGardeningTaskActive(false);
                CoreFunctionality.BringBotWindowToFront();
            }
        }
    }
}
