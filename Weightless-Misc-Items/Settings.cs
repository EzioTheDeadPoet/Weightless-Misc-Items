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
        [SynthesisDescription("This disables the healing effect from any item that isn't a potion, as they have 0 weight.")]
        [SynthesisTooltip("This disables the healing effect from any item that isn't a potion, as they have 0 weight.")]
        public bool WeightlessItemsOfferNoHealing = true;
    }
}
