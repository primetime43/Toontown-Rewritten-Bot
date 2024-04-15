using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Views;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Services
{
    public class Gardening : CoreFunctionality
    {
        public async Task PlantFlowerAsync(string flowerCombo, CancellationToken cancellationToken)
        {
            // Assume ManualUpdateCoordinatesAsync is an async version of ManualUpdateCoordinates
            if (!CheckCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton))
            {
                await ManualUpdateCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
                if (!CheckCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton)) return;
            }

            var confirmation = MessageBox.Show("Press OK when ready to begin!", "", MessageBoxButtons.OKCancel);
            if (confirmation == DialogResult.Cancel) return;

            await Task.Delay(2000, cancellationToken);

            var (x, y) = GetCoordsFromMap(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);

            await CheckBeansAsync(GardeningCoordinatesEnum.RedJellybeanButton, cancellationToken);

            char[] beans = flowerCombo.ToCharArray();
            foreach (var bean in beans)
            {
                await SelectBeanAsync(bean, cancellationToken);
            }
            await PressPlantButtonAsync(cancellationToken);
            MessageBox.Show("Done!");
        }

        private async Task SelectBeanAsync(char beanType, CancellationToken cancellationToken)
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

            if (!CheckCoordinates(location))
            {
                await ManualUpdateCoordinates(location);
                if (!CheckCoordinates(location)) return; // Ensure coordinates are set after update.
            }
            var (x, y) = GetCoordsFromMap(location);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        private async Task CheckBeansAsync(GardeningCoordinatesEnum location, CancellationToken cancellationToken)
        {
            int locationNumericalVal = Convert.ToInt32(location);
            if (locationNumericalVal <= 10)
            {
                if (!CoreFunctionality.CheckCoordinates(location))//if they're 0,0
                {
                    await ManualUpdateCoordinates(location);
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

        private async Task PressPlantButtonAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates(GardeningCoordinatesEnum.BluePlantButton))
            {
                var (x, y) = GetCoordsFromMap(GardeningCoordinatesEnum.BluePlantButton);
                MoveCursor(x, y);
                DoMouseClick();
                Thread.Sleep(8000);
                await ClickOKAfterPlantAsync(cancellationToken);
                await WaterPlantAsync(cancellationToken);
            }
            else
            {
                await ManualUpdateCoordinates(GardeningCoordinatesEnum.BluePlantButton);
                Thread.Sleep(2000);
                await PressPlantButtonAsync(cancellationToken);
            }
        }

        private async Task ClickOKAfterPlantAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates(GardeningCoordinatesEnum.BlueOkButton))
            {
                var (x, y) = GetCoordsFromMap(GardeningCoordinatesEnum.BlueOkButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else
            {
                await ManualUpdateCoordinates(GardeningCoordinatesEnum.BlueOkButton);
                Thread.Sleep(2000);
                await ClickOKAfterPlantAsync(cancellationToken);
            }
        }

        public async Task WaterPlantAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates(GardeningCoordinatesEnum.WateringCanButton))
            {
                var (x, y) = GetCoordsFromMap(GardeningCoordinatesEnum.WateringCanButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(4000);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else
            {
                await ManualUpdateCoordinates(GardeningCoordinatesEnum.WateringCanButton);
                Thread.Sleep(2000);
                await WaterPlantAsync(cancellationToken);
            }
        }

        public async Task RemovePlantAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton))
            {
                var (x, y) = GetCoordsFromMap(GardeningCoordinatesEnum.PlantFlowerRemoveButton);
                MessageBox.Show("Press OK when ready to begin!");
                Thread.Sleep(2000);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await SelectYESToRemoveAsync(cancellationToken);
            }
            else
            {
                await ManualUpdateCoordinates(GardeningCoordinatesEnum.PlantFlowerRemoveButton);//update the plant flower button coords
                await RemovePlantAsync(cancellationToken);
                Thread.Sleep(2000);
            }

        }

        private async Task SelectYESToRemoveAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates(GardeningCoordinatesEnum.BlueYesButton))
            {
                var (x, y) = GetCoordsFromMap(GardeningCoordinatesEnum.BlueYesButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
            }
            else
            {
                await ManualUpdateCoordinates(GardeningCoordinatesEnum.BlueYesButton);//update the plant flower button coords
                await SelectYESToRemoveAsync(cancellationToken);
                Thread.Sleep(2000);
            }
        }
    }
}
