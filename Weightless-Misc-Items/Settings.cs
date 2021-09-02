using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda.Synthesis.Settings;

namespace SlotsSlotsSlots
{
    public record Settings
    {   
        [SynthesisOrder]
        [SynthesisSettingName("Potion Weight")]
        [SynthesisDescription("This alters the weigth of any potion in the game, to set much slots 1 potion should take up.")]
        [SynthesisTooltip("This alters the weigth of any potion in the game, to set much slots 1 potion should take up.\nA value of 0.1 makes it, so 10 potions are needed to fill 1 slot.")]
        public float PotionSlotUse = 0.1f;
        [SynthesisOrder]
        [SynthesisSettingName("Scroll Weight")]
        [SynthesisDescription("This alters the weigth of any scroll in the game, to set much slots 1 scroll should take up.")]
        [SynthesisTooltip("This alters the weigth of any scroll in the game, to set much slots 1 scroll should take up.\nA value of 0.5 makes it, so 2 scrolls are needed to fill 1 slot.")]
        public float ScrollSlotUse = 0.5f;
        [SynthesisOrder]
        [SynthesisSettingName("Weigthless items can't heal")]
        [SynthesisDescription("This disables the healing effect from any item that isn't a potion, as they are excluded from the slot system.")]
        [SynthesisTooltip("This disables the healing effect from any item that isn't a potion, as they are excluded from the slot system.")]
        public bool WeightlessItemsOfferNoHealing = true;
    }
}
