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
            if (!CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton))
            {
                await CoordinatesManager.ManualUpdateCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
                if (!CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton)) 
                    return;
            }

            //BringBotWindowToFront();
            //var confirmation = MessageBox.Show("Press OK when ready to begin!", "", MessageBoxButtons.OKCancel);
            var confirmation = MessageBox.Show(
            "Press OK when ready to begin!",
            "Begin Gardening",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.None,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.DefaultDesktopOnly);
            if (confirmation == DialogResult.Cancel) 
                return;

            await Task.Delay(2000);

            var (x, y) = CoordinatesManager.GetCoordsFromMap(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000);

            await CheckBeansAsync(GardeningCoordinatesEnum.RedJellybeanButton, cancellationToken);

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

            if (!CoordinatesManager.CheckCoordinates(location))
            {
                await CoordinatesManager.ManualUpdateCoordinates(location);
                if (!CoordinatesManager.CheckCoordinates(location)) return; // Ensure coordinates are set after update.
            }
            var (x, y) = CoordinatesManager.GetCoordsFromMap(location);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        private static async Task CheckBeansAsync(GardeningCoordinatesEnum location, CancellationToken cancellationToken)
        {
            int locationNumericalVal = Convert.ToInt32(location);
            if (locationNumericalVal <= 10)
            {
                if (!CoordinatesManager.CheckCoordinates(location))//if they're 0,0
                {
                    await CoordinatesManager.ManualUpdateCoordinates(location);
                    GardeningCoordinatesEnum nextLocation = (GardeningCoordinatesEnum)(locationNumericalVal + 1);
                    if (Enum.IsDefined(typeof(GardeningCoordinatesEnum), nextLocation))
                    {
                        await CheckBeansAsync(nextLocation, cancellationToken);
                    }
                }
                else
                {
                    GardeningCoordinatesEnum nextLocation = (GardeningCoordinatesEnum)(locationNumericalVal + 1);
                    if (Enum.IsDefined(typeof(GardeningCoordinatesEnum), nextLocation))
                    {
                        await CheckBeansAsync(nextLocation, cancellationToken);
                    }
                }
            }
        }

        private static async Task PressPlantButtonAsync(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.BluePlantButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(GardeningCoordinatesEnum.BluePlantButton);
                MoveCursor(x, y);
                DoMouseClick();
                Thread.Sleep(8000);
                await ClickOKAfterPlantAsync(cancellationToken);
                await WaterPlantAsync(cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(GardeningCoordinatesEnum.BluePlantButton);
                Thread.Sleep(2000);
                await PressPlantButtonAsync(cancellationToken);
            }
        }

        private static async Task ClickOKAfterPlantAsync(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.BlueOkButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(GardeningCoordinatesEnum.BlueOkButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(GardeningCoordinatesEnum.BlueOkButton);
                Thread.Sleep(2000);
                await ClickOKAfterPlantAsync(cancellationToken);
            }
        }

        public static async Task WaterPlantAsync(CancellationToken cancellationToken)
        {
            if (!CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.WateringCanButton))
            {
                await CoordinatesManager.ManualUpdateCoordinates(GardeningCoordinatesEnum.WateringCanButton);
                // Recheck coordinates after update
                if (!CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.WateringCanButton))
                {
                    throw new InvalidOperationException("Watering can button coordinates not set.");
                }
            }

            var (x, y) = CoordinatesManager.GetCoordsFromMap(GardeningCoordinatesEnum.WateringCanButton);

            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(4000, cancellationToken);

            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public static async Task RemovePlantAsync(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
                MessageBox.Show("Press OK when ready to begin!");
                Thread.Sleep(2000);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await SelectYESToRemoveAsync(cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton);//update the plant flower button coords
                await RemovePlantAsync(cancellationToken);
                Thread.Sleep(2000);
            }

        }

        private static async Task SelectYESToRemoveAsync(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(GardeningCoordinatesEnum.BlueYesButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(GardeningCoordinatesEnum.BlueYesButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(GardeningCoordinatesEnum.BlueYesButton);//update the plant flower button coords
                await SelectYESToRemoveAsync(cancellationToken);
                Thread.Sleep(2000);
            }
        }
    }
}
