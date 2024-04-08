using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot.Services
{
    public class Gardening : CoreFunctionality
    {
        public async Task PlantFlowerAsync(string flowerCombo, CancellationToken cancellationToken)
        {
            // Assume ManualUpdateCoordinatesAsync is an async version of ManualUpdateCoordinates
            if (!CheckCoordinates("1"))
            {
                await ManualUpdateCoordinates("1");
                if (!CheckCoordinates("1")) return;
            }

            var confirmation = MessageBox.Show("Press OK when ready to begin!", "", MessageBoxButtons.OKCancel);
            if (confirmation == DialogResult.Cancel) return;

            await Task.Delay(2000, cancellationToken);

            var (x, y) = GetCoordsFromMap("1");
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);

            await CheckBeansAsync("2", cancellationToken);

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
            string location = beanType switch
            {
                'r' => "2",  // Red Jellybean
                'g' => "3",  // Green Jellybean
                'o' => "4",  // Orange Jellybean
                'u' => "5",  // Purple Jellybean, assuming 'u' is for 'purple'
                'b' => "6",  // Blue Jellybean
                'i' => "7",  // Pink Jellybean, assuming 'i' is for 'pink'
                'y' => "8",  // Yellow Jellybean
                'c' => "9",  // Cyan Jellybean
                's' => "10", // Silver Jellybean
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

        private async Task CheckBeansAsync(string location, CancellationToken cancellationToken)
        {
            if (Convert.ToInt32(location) <= 10)
            {
                if (!CoreFunctionality.CheckCoordinates(location))//if they're 0,0
                {
                    await ManualUpdateCoordinates(location);
                    await CheckBeansAsync(Convert.ToString(Convert.ToInt32(location) + 1), cancellationToken);
                }
                else
                    await CheckBeansAsync(Convert.ToString(Convert.ToInt32(location) + 1), cancellationToken);
            }
        }

        private async Task PressPlantButtonAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates("11"))
            {
                var (x, y) = GetCoordsFromMap("11");
                MoveCursor(x, y);
                DoMouseClick();
                Thread.Sleep(8000);
                await ClickOKAfterPlantAsync(cancellationToken);
                await WaterPlantAsync(cancellationToken);
            }
            else
            {
                await ManualUpdateCoordinates("11");
                Thread.Sleep(2000);
                await PressPlantButtonAsync(cancellationToken);
            }
        }

        private async Task ClickOKAfterPlantAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates("12"))
            {
                var (x, y) = GetCoordsFromMap("12");
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else
            {
                await ManualUpdateCoordinates("12");
                Thread.Sleep(2000);
                await ClickOKAfterPlantAsync(cancellationToken);
            }
        }

        public async Task WaterPlantAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates("13"))
            {
                var (x, y) = GetCoordsFromMap("13");
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(4000);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else
            {
                await ManualUpdateCoordinates("13");
                Thread.Sleep(2000);
                await WaterPlantAsync(cancellationToken);
            }
        }

        public async Task RemovePlantAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates("1"))
            {
                var (x, y) = GetCoordsFromMap("1");
                MessageBox.Show("Press OK when ready to begin!");
                Thread.Sleep(2000);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await SelectYESToRemoveAsync(cancellationToken);
            }
            else
            {
                await ManualUpdateCoordinates("1");//update the plant flower button coords
                await RemovePlantAsync(cancellationToken);
                Thread.Sleep(2000);
            }

        }

        private async Task SelectYESToRemoveAsync(CancellationToken cancellationToken)
        {
            if (CoreFunctionality.CheckCoordinates("14"))
            {
                var (x, y) = GetCoordsFromMap("14");
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
            }
            else
            {
                await ManualUpdateCoordinates("14");//update the plant flower button coords
                await SelectYESToRemoveAsync(cancellationToken);
                Thread.Sleep(2000);
            }
        }
    }
}
