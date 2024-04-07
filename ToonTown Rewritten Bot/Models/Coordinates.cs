using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToonTown_Rewritten_Bot.Models
{
    public class Coordinates
    {
        public enum GardeningCoordinatesEnum
        {
            PlantFlowerRemoveButton = 1,
            RedJellybeanButton,
            GreenJellybeanButton,
            OrangeJellybeanButton,
            PurpleJellybeanButton,
            BlueJellybeanButton,
            PinkJellybeanButton,
            YellowJellybeanButton,
            CyanJellybeanButton,
            SilverJellybeanButton,
            BluePlantButton,
            BlueOkButton,
            WateringCanButton,
            BlueYesButton
        }

        public enum FishingCoordinatesEnum
        {
            RedFishingButton = 15,
            ExitFishingButton,
            BlueSellAllButton
        }

        public enum DoodleTrainingCoordinatesEnum
        {
            FeedDoodleButton = 18,
            ScratchDoodleButton,
            GreenSpeedChatButton,
            PetsTabInSpeedChat,
            TricksTabInSpeedChat,
            JumpTrickOptionInSpeedChat,
            BegTrickOptionInSpeedChat,
            PlayDeadTrickOptionInSpeedChat,
            RolloverTrickOptionInSpeedChat,
            BackflipTrickOptionInSpeedChat,
            DanceTrickOptionInSpeedChat,
            SpeakTrickOptionInSpeedChat
        }
    }
}