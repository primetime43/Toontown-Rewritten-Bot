using System;
using System.Threading;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    class Gardening
    {
        private static int x, y;
        public static void plantFlower(String flowerCombo)
        {
            DialogResult confirmation;
            //check if plant button is (0,0). True means continue, not (0,0)
            if (BotFunctions.checkCoordinates("1"))
            {
                confirmation = MessageBox.Show("Press OK when ready to begin!","", MessageBoxButtons.OKCancel);
                if (confirmation.Equals(DialogResult.Cancel))
                    return;
                Thread.Sleep(2000);
                getCoords("1");
                BotFunctions.MoveCursor(x,y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
                checkBeans("2");

                char[] beans = flowerCombo.ToCharArray();
                selectBeans(flowerCombo, beans);
                pressPlantButton();
                Thread.Sleep(1500);
                MessageBox.Show("Done!");
            }
            else
            {
                BotFunctions.updateCoordinates("1");//update the plant flower button coords
                plantFlower(flowerCombo);
                Thread.Sleep(2000);
            }
        }

        private static void selectBeans(String flowerCombo, char[] beans)
        {
            for (int i = 0; i < flowerCombo.Length; i++)
            {
                switch (beans[i])
                {
                    case 'r':
                        getCoords("2");
                        break;
                    case 'g':
                        getCoords("3");
                        break;
                    case 'o':
                        getCoords("4");
                        break;
                    case 'u':
                        getCoords("5");
                        break;
                    case 'b':
                        getCoords("6");
                        break;
                    case 'i':
                        getCoords("7");
                        break;
                    case 'y':
                        getCoords("8");
                        break;
                    case 'c':
                        getCoords("9");
                        break;
                    case 's':
                        getCoords("10");
                        break;
                }
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
        }

        private static void checkBeans(String location)
        {
            if (Convert.ToInt32(location) <= 10)
            {
                if (!BotFunctions.checkCoordinates(location))//if they're 0,0
                {
                    BotFunctions.updateCoordinates(location);
                    checkBeans(Convert.ToString(Convert.ToInt32(location) + 1));
                }
                else
                    checkBeans(Convert.ToString(Convert.ToInt32(location) + 1));
            }
        }

        private static void pressPlantButton()
        {
            if (BotFunctions.checkCoordinates("11"))
            {
                getCoords("11");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(8000);
                clickOKAfterPlant();
                waterPlant();
            }
            else
            {
                BotFunctions.updateCoordinates("11");
                Thread.Sleep(2000);
                pressPlantButton();
            }
        }

        private static void clickOKAfterPlant()
        {
            if (BotFunctions.checkCoordinates("12"))
            {
                getCoords("12");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else
            {
                BotFunctions.updateCoordinates("12");
                Thread.Sleep(2000);
                clickOKAfterPlant();
            }
        }

        public static void waterPlant()
        {
            if (BotFunctions.checkCoordinates("13"))
            {
                getCoords("13");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(4000);
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else
            {
                BotFunctions.updateCoordinates("13");
                Thread.Sleep(2000);
                waterPlant();
            }
        }

        public static void removePlant()
        {
            if (BotFunctions.checkCoordinates("1"))
            {
                getCoords("1");
                MessageBox.Show("Press OK when ready to begin!");
                Thread.Sleep(2000);
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                selectYESToRemove();
            }
            else
            {
                BotFunctions.updateCoordinates("1");//update the plant flower button coords
                removePlant();
                Thread.Sleep(2000);
            }

        }

        private static void selectYESToRemove()
        {
            if (BotFunctions.checkCoordinates("14"))
            {
                getCoords("14");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
            }
            else
            {
                BotFunctions.updateCoordinates("14");//update the plant flower button coords
                selectYESToRemove();
                Thread.Sleep(2000);
            }
        }

        private static void getCoords(String item)
        {
            int[] coordinates = BotFunctions.getCoordinates(item);
            x = coordinates[0];
            y = coordinates[1];
        }
    }
}
