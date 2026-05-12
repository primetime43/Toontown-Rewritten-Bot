using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Services
{
    public class Gardening : CoreFunctionality
    {
        private static GardeningOverlayForm _overlay;

        public static async Task PlantFlowerAsync(string flowerCombo, string flowerName, int waterCount, CancellationToken cancellationToken)
        {
            if (waterCount < 0)
                waterCount = 0;

            int beanCount = flowerCombo.Length;
            // Total steps: beans + plant button + OK + waterings
            int totalSteps = beanCount + 1 + 1 + waterCount;
            int step = 0;

            try
            {
                ShowOverlay();
                InvokeOverlay(() =>
                {
                    _overlay?.SetFlowerInfo(flowerName, flowerCombo);
                    _overlay?.SetStatus("Running");
                });

                // Check if game window is available and focus it
                if (!EnsureGameWindowReadyWithMessage())
                {
                    CloseOverlay();
                    return;
                }
                FocusTTRWindow();

                string firstBeanName = GardeningOverlayForm.GetBeanName(flowerCombo[0]);
                InvokeOverlay(() => _overlay?.UpdateAction("Opening planting menu", $"Selecting {firstBeanName} Bean", 0, totalSteps, 2000));
                await Task.Delay(2000, cancellationToken);

                // Hide overlay so template capture prompts can show if needed
                var (x, y) = await FindElementWithOverlayPause(GardeningCoordinatesEnum.PlantFlowerButton);
                MoveCursor(x, y);
                DoMouseClick();

                InvokeOverlay(() => _overlay?.UpdateAction("Waiting for planting UI", $"Selecting {firstBeanName} Bean", 0, totalSteps, 2000));
                await Task.Delay(2000, cancellationToken);

                char[] beans = flowerCombo.ToCharArray();
                for (int i = 0; i < beans.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    step++;

                    string currentBeanName = GardeningOverlayForm.GetBeanName(beans[i]);
                    string nextActionText;
                    if (i + 1 < beans.Length)
                        nextActionText = $"Selecting {GardeningOverlayForm.GetBeanName(beans[i + 1])} Bean";
                    else
                        nextActionText = "Clicking Plant";

                    int beanIndex = i;
                    int currentStep = step;
                    InvokeOverlay(() =>
                    {
                        _overlay?.SetCurrentBean(beanIndex);
                        _overlay?.UpdateAction($"Selecting {currentBeanName} Bean", nextActionText, currentStep, totalSteps, 2000);
                    });

                    await SelectBeanAsync(beans[i], cancellationToken);
                }

                // Plant button
                step++;
                int plantStep = step;
                InvokeOverlay(() =>
                {
                    _overlay?.SetCurrentBean(beans.Length); // Past all beans
                    _overlay?.UpdateAction("Clicking Plant", "Waiting for confirmation", plantStep, totalSteps, 8000);
                });

                var (px, py) = await FindElementWithOverlayPause(GardeningCoordinatesEnum.BluePlantButton);
                MoveCursor(px, py);
                DoMouseClick();
                await Task.Delay(8000, cancellationToken);

                // OK button
                step++;
                int okStep = step;
                string okNextText = waterCount > 0 ? $"Watering (1/{waterCount})" : "Done";
                InvokeOverlay(() => _overlay?.UpdateAction("Confirming plant", okNextText, okStep, totalSteps, 2000));

                var (ox, oy) = await FindElementWithOverlayPause(GardeningCoordinatesEnum.BlueOkButton);
                MoveCursor(ox, oy);
                DoMouseClick();
                await Task.Delay(2000, cancellationToken);

                // Watering — skip the button lookup entirely if the user chose 0 waters
                if (waterCount > 0)
                {
                    var (wx, wy) = await FindElementWithOverlayPause(GardeningCoordinatesEnum.WateringCanButton);
                    for (int i = 0; i < waterCount; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        step++;

                        int waterNum = i + 1;
                        string nextWaterText = waterNum < waterCount
                            ? $"Watering ({waterNum + 1}/{waterCount})"
                            : "Done";
                        int waterStep = step;
                        InvokeOverlay(() => _overlay?.UpdateAction($"Watering ({waterNum}/{waterCount})", nextWaterText, waterStep, totalSteps, 4000));

                        MoveCursor(wx, wy);
                        DoMouseClick();
                        await Task.Delay(4000, cancellationToken);
                    }
                }

                InvokeOverlay(() => _overlay?.SetStatus("Completed"));

                MessageBox.Show(
                    "Done!",
                    "Gardening Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
            catch (OperationCanceledException)
            {
                InvokeOverlay(() => _overlay?.SetStatus("Cancelled"));
                throw;
            }
            catch
            {
                InvokeOverlay(() => _overlay?.SetStatus("Cancelled"));
                throw;
            }
            finally
            {
                CloseOverlay();
            }
        }

        /// <summary>
        /// Temporarily hides the overlay before image recognition so template capture
        /// prompts can display on top. Re-shows the overlay afterward.
        /// </summary>
        private static async Task<(int x, int y)> FindElementWithOverlayPause(GardeningCoordinatesEnum element)
        {
            HideOverlay();
            try
            {
                return await CoordinatesManager.GetCoordsWithImageRecAsync(element);
            }
            finally
            {
                ShowOverlay();
            }
        }

        private static async Task SelectBeanAsync(char beanType, CancellationToken cancellationToken)
        {
            // Use template-based image recognition to find the jellybean button
            GardeningCoordinatesEnum location = beanType switch
            {
                'r' => GardeningCoordinatesEnum.RedJellybeanButton,
                'g' => GardeningCoordinatesEnum.GreenJellybeanButton,
                'o' => GardeningCoordinatesEnum.OrangeJellybeanButton,
                'u' => GardeningCoordinatesEnum.PurpleJellybeanButton,
                'b' => GardeningCoordinatesEnum.BlueJellybeanButton,
                'i' => GardeningCoordinatesEnum.PinkJellybeanButton,
                'y' => GardeningCoordinatesEnum.YellowJellybeanButton,
                'c' => GardeningCoordinatesEnum.CyanJellybeanButton,
                's' => GardeningCoordinatesEnum.SilverJellybeanButton,
                _ => throw new ArgumentException("Invalid bean type", nameof(beanType)),
            };

            // Hide overlay so template capture prompts can show if needed
            var (x, y) = await FindElementWithOverlayPause(location);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public static async Task WaterPlantAsync(int waterPlantCount, CancellationToken cancellationToken)
        {
            if (waterPlantCount <= 0)
                return;

            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(GardeningCoordinatesEnum.WateringCanButton);

            for (int i = 0; i < waterPlantCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(4000, cancellationToken);
            }
        }

        public static async Task RemovePlantAsync(CancellationToken cancellationToken)
        {
            MessageBox.Show("Press OK when ready to begin!");

            // Check if game window is available and focus it
            if (!EnsureGameWindowReadyWithMessage())
                return;
            FocusTTRWindow();

            await Task.Delay(2000, cancellationToken);

            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(GardeningCoordinatesEnum.RemovePlantButton);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();

            // Wait for the confirmation dialog to appear
            await Task.Delay(2000, cancellationToken);

            await SelectYESToRemoveAsync(cancellationToken);
        }

        private static async Task SelectYESToRemoveAsync(CancellationToken cancellationToken)
        {
            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(GardeningCoordinatesEnum.BlueYesButton);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(1000, cancellationToken);
        }

        /// <summary>
        /// Shows the gardening overlay. Thread-safe.
        /// </summary>
        public static void ShowOverlay()
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(ShowOverlayInternal));
                    return;
                }
            }
            ShowOverlayInternal();
        }

        private static void ShowOverlayInternal()
        {
            if (_overlay == null || _overlay.IsDisposed)
            {
                _overlay = new GardeningOverlayForm();
                _overlay.Show();
            }
            else if (!_overlay.Visible)
            {
                _overlay.Show();
            }
        }

        /// <summary>
        /// Hides the gardening overlay without disposing it, so the next ShowOverlay
        /// brings it back with all state intact. Used around template lookups to keep
        /// the overlay out of any template capture prompts. Thread-safe.
        /// </summary>
        public static void HideOverlay()
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(HideOverlayInternal));
                    return;
                }
            }
            HideOverlayInternal();
        }

        private static void HideOverlayInternal()
        {
            if (_overlay != null && !_overlay.IsDisposed && _overlay.Visible)
            {
                _overlay.Hide();
            }
        }

        /// <summary>
        /// Closes and disposes the gardening overlay. Use at the end of a planting
        /// session — for in-session hiding, prefer HideOverlay so state is preserved.
        /// Thread-safe.
        /// </summary>
        public static void CloseOverlay()
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(CloseOverlayInternal));
                    return;
                }
            }
            CloseOverlayInternal();
        }

        private static void CloseOverlayInternal()
        {
            if (_overlay != null && !_overlay.IsDisposed)
            {
                _overlay.Close();
                _overlay.Dispose();
                _overlay = null;
            }
        }

        /// <summary>
        /// Invokes an action on the overlay's UI thread.
        /// </summary>
        private static void InvokeOverlay(Action action)
        {
            try
            {
                if (_overlay != null && !_overlay.IsDisposed)
                {
                    if (_overlay.InvokeRequired)
                        _overlay.BeginInvoke(action);
                    else
                        action();
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        public static bool IsOverlayVisible => _overlay != null && !_overlay.IsDisposed && _overlay.Visible;
    }
}
