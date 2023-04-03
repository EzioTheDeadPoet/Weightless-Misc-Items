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
        [SynthesisSettingName("Weigthless items can't heal")]
        [SynthesisDescription("This disables the healing effect from any item that isn't a potion, and makes it impossible to brew healing potions yourself, you gotta buy them or find them.")]
        [SynthesisTooltip("This disables the healing effect from any item that isn't a potion, and makes it impossible to brew healing potions yourself, you gotta buy them or find them.")]
        public bool WeightlessItemsOfferNoHealing = false;
    }
}
