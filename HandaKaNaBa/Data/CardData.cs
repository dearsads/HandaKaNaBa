// Owner: Back End (Members 1 & 2)
// All card content lives here as lists in code (no database or file needed).
using HandaKaNaBa.Models;
using D = HandaKaNaBa.Models.DisasterType;

namespace HandaKaNaBa.Data
{
    public static class CardData
    {
        /// <summary>The 6 disaster cards.</summary>
        public static List<DisasterCard> Disasters()
        {
            return new List<DisasterCard>
            {
                new DisasterCard(D.Flood,      "🌊", "Baha na hanggang tuhod sa kalye niyo!"),
                new DisasterCard(D.Fire,       "🔥", "May sunog sa kabilang bahay at kumakalat na!"),
                new DisasterCard(D.Typhoon,    "🌀", "Signal No. 4 na! Malakas ang hangin at ulan."),
                new DisasterCard(D.Earthquake, "🏚️", "Lumilindol! Umuuga ang buong bahay."),
                new DisasterCard(D.Landslide,  "⛰️", "Tatlong araw nang umuulan at may bitak na ang lupa sa bundok."),
                new DisasterCard(D.Drought,    "☀️", "El Niño! Dalawang buwan nang walang ulan."),
            };
        }

        /// <summary>The main deck: 24 specific cards (4 per disaster) + 6 general cards = 30 cards.</summary>
        public static List<ResponseCard> ResponseCards()
        {
            return new List<ResponseCard>
            {
                // Flood
                new SpecificCard("Sandbags", "🧱", "Block water from entering the house.", D.Flood),
                new SpecificCard("High Ground Evacuation", "⬆️", "Move to higher ground before the water rises.", D.Flood),
                new SpecificCard("Turn Off Electricity", "🔌", "Switch off the main breaker to avoid electrocution.", D.Flood),
                new SpecificCard("Avoid Floodwater", "🚫", "Do not walk or swim in floodwater.", D.Flood),

                // Fire
                new SpecificCard("Fire Extinguisher", "🧯", "Put out small fires early (Pull, Aim, Squeeze, Sweep).", D.Fire),
                new SpecificCard("Stop-Drop-Roll", "🤸", "If your clothes catch fire, stop, drop and roll.", D.Fire),
                new SpecificCard("Use Fire Exit", "🚪", "Leave through the fire exit. Never use the elevator.", D.Fire),
                new SpecificCard("Cover Nose with Wet Cloth", "😷", "Stay low and cover your nose to avoid smoke.", D.Fire),

                // Typhoon
                new SpecificCard("Board Up Windows", "🪟", "Protect windows from flying debris.", D.Typhoon),
                new SpecificCard("Go to Evacuation Center", "🏫", "Follow the barangay and go to the evacuation center.", D.Typhoon),
                new SpecificCard("Secure the Roof", "🏠", "Tie down the roof so the wind cannot lift it.", D.Typhoon),
                new SpecificCard("Stock Canned Goods", "🥫", "Prepare food for at least 3 days.", D.Typhoon),

                // Earthquake
                new SpecificCard("Drop-Cover-Hold", "🙇", "Drop to the ground, cover your head, hold on.", D.Earthquake),
                new SpecificCard("Hide Under Sturdy Table", "🪑", "Shelter under a strong table until shaking stops.", D.Earthquake),
                new SpecificCard("Go to Open Space", "🌳", "After shaking, move away from buildings and poles.", D.Earthquake),
                new SpecificCard("Stay Away from Glass", "🪞", "Keep away from windows and glass that can shatter.", D.Earthquake),

                // Landslide
                new SpecificCard("Evacuate Slope Area", "🏃", "Leave homes on or below steep slopes.", D.Landslide),
                new SpecificCard("Heed Early Warning", "📢", "Listen to PAGASA and barangay warnings.", D.Landslide),
                new SpecificCard("Stay Away from Slide Path", "↔️", "Move sideways, away from the path of the slide.", D.Landslide),
                new SpecificCard("Check Ground Cracks", "🔍", "New cracks in the ground are a warning sign.", D.Landslide),

                // Drought
                new SpecificCard("Store Water", "🪣", "Keep clean water in covered containers.", D.Drought),
                new SpecificCard("Conserve Water", "🚿", "Use a pail, not a hose. Fix leaks.", D.Drought),
                new SpecificCard("Harvest Rainwater", "🌧️", "Collect rain for washing and plants.", D.Drought),
                new SpecificCard("Plant Drought-Resistant Crops", "🌵", "Grow crops that need less water.", D.Drought),

                // General (Partial for any disaster)
                new GeneralCard("Go-Bag", "🎒", "Ready bag with essentials. Helps, but not enough alone."),
                new GeneralCard("First Aid Kit", "🩹", "Treats injuries, but does not stop the disaster."),
                new GeneralCard("Flashlight", "🔦", "Useful when the power is out."),
                new GeneralCard("Emergency Radio", "📻", "Keeps you updated on warnings."),
                new GeneralCard("Drinking Water", "💧", "Always needed, but not a full solution."),
                new GeneralCard("Call Hotline", "📞", "Call 911 for help."),
            };
        }

        /// <summary>The 12 Meme cards (2 per disaster). Kept out of the main deck.</summary>
        public static List<MemeCard> MemeCards()
        {
            return new List<MemeCard>
            {
                new MemeCard("Mag-order ng kayak sa Shopee", "🛶", D.Flood,
                    "\"Estimated delivery: 7-10 days. Lunod ka na bago dumating.\"",
                    "Evacuate to high ground right away. Do not wait for gear."),
                new MemeCard("Mag-swimming sa baha, libre naman", "🏊", D.Flood,
                    "\"Libreng leptospirosis, walang bayad!\"",
                    "Floodwater carries disease. Avoid wading, and wear boots if you must."),

                new MemeCard("Post muna sa FB: \"Pray for us\"", "📱", D.Fire,
                    "\"50 likes, abo na ang bahay.\"",
                    "Get out first and call 911 or the BFP. Post later."),
                new MemeCard("Buhusan ng tubig ang nag-aapoy na saksakan", "💦", D.Fire,
                    "\"Bzzzt! Plot twist: ikaw ang na-kuryente.\"",
                    "Never use water on electrical fires. Cut the power and use an extinguisher."),

                new MemeCard("Umakyat sa bubong para i-picture ang mata ng bagyo", "📸", D.Typhoon,
                    "\"Lumipad ka kasama ng yero.\"",
                    "Stay indoors and away from windows, or go to the evacuation center."),
                new MemeCard("Mag-panic buy ng 20 sachet ng 3-in-1 kape lang", "☕", D.Typhoon,
                    "\"Gising ka nga buong bagyo, gutom naman.\"",
                    "Stock food and water for at least 3 days, not just one item."),

                new MemeCard("Mag-TikTok live habang lumilindol", "🎥", D.Earthquake,
                    "\"Viral ka nga, pero sa balita.\"",
                    "Drop, Cover, Hold until the shaking stops."),
                new MemeCard("Tumakbo palabas habang umuuga", "🥷", D.Earthquake,
                    "\"Ninja moves, pero tinamaan ng paso ni Lola.\"",
                    "Running during shaking causes injuries from falling objects. Drop, Cover, Hold first."),

                new MemeCard("Balikan ang charger sa bahay", "🔋", D.Landslide,
                    "\"100% battery, 0% bahay.\"",
                    "Once you evacuate, do not go back until officials say it is safe."),
                new MemeCard("Mag-picnic sa gilid ng bundok pagkatapos ng ulan", "🧺", D.Landslide,
                    "\"Sumama ang bundok sa picnic.\"",
                    "Stay away from slopes after heavy rain. That is when landslides happen."),

                new MemeCard("Mag-car wash araw-araw para fresh", "🚗", D.Drought,
                    "\"Fresh ang kotse, uhaw ang barangay.\"",
                    "Conserve water. Use a pail instead of a hose."),
                new MemeCard("Diligan ang kalsada para lumamig", "🚰", D.Drought,
                    "\"Lumamig nga, isang minuto lang.\"",
                    "Save water for drinking, cooking and crops."),
            };
        }
    }
}
