using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Views;
using static ToonTown_Rewritten_Bot.Models.Coordinates;
using static ToonTown_Rewritten_Bot.Utilities.ImageRecognition;

namespace ToonTown_Rewritten_Bot.Services
{
    public class Gardening : CoreFunctionality
    {
        public static async Task PlantFlowerAsync(string flowerCombo, CancellationToken cancellationToken)
        {
            var confirmation = MessageBox.Show(
            "Press OK when ready to begin!",
            "Begin Gardening",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.None,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.DefaultDesktopOnly);
            if (confirmation == DialogResult.Cancel)
                return;

            // Force game window to fullscreen/maximized before starting
            if (!ForceGameWindowFullscreen())
            {
                MessageBox.Show("Toontown Rewritten window not found. Please make sure the game is running.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await Task.Delay(2000);

            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000);

            char[] beans = flowerCombo.ToCharArray();
            foreach (var bean in beans)
            {
                await SelectBeanAsync(bean, cancellationToken);
            }
            await PressPlantButtonAsync(cancellationToken);

            MessageBox.Show(
            "Done!",
            "Gardening Complete",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.None,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.DefaultDesktopOnly);
        }

        private static async Task SelectBeanAsync(char beanType, CancellationToken cancellationToken)
        {
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

            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(location);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        private static async Task PressPlantButtonAsync(CancellationToken cancellationToken)
        {
            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(GardeningCoordinatesEnum.BluePlantButton);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(8000, cancellationToken);
            await ClickOKAfterPlantAsync(cancellationToken);
            await WaterPlantAsync(3, cancellationToken);
        }

        private static async Task ClickOKAfterPlantAsync(CancellationToken cancellationToken)
        {
            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(GardeningCoordinatesEnum.BlueOkButton);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public static async Task WaterPlantAsync(int waterPlantCount, CancellationToken cancellationToken)
        {
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

            // Force game window to fullscreen/maximized before starting
            if (!ForceGameWindowFullscreen())
            {
                MessageBox.Show("Toontown Rewritten window not found. Please make sure the game is running.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await Task.Delay(2000, cancellationToken);

            // Use image recognition to find button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
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
    }
}
