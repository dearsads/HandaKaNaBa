// EncounterDatabase.cs - Member 1 (Data & Scenarios)
// Dito nakalagay ang lahat ng cards, scenarios, maps at ang report sa dulo.
using System.Text;

namespace HandaKaNaBa
{
    public enum Difficulty { Tsunami, Lindol, ElNino }
    public enum FamilyType { Lola, Bunso, Bantay }
    public enum CardResult { Handa, PwedeNa, Sablay }

    // ABSTRACT parent class ng lahat ng cards (Abstraction & Inheritance)
    public abstract class Card
    {
        public string Title { get; }
        public string Hint { get; }
        public string Reaction { get; }
        public string Lesson { get; }

        protected Card(string title, string hint, string reaction, string lesson)
        {
            Title = title;
            Hint = hint;
            Reaction = reaction;
            Lesson = lesson;
        }

        public abstract CardResult Result { get; }
        public abstract string Label { get; }
        public abstract Color Color { get; }

        // POLYMORPHISM: bawat klase ng card ay may sariling computation ng points
        public abstract int GetPoints(int combo, int secondsLeft);
    }

    public class HandaCard : Card
    {
        public HandaCard(string t, string h, string r, string l) : base(t, h, r, l) { }

        public override CardResult Result => CardResult.Handa;
        public override string Label => "HANDA!";
        public override Color Color => Color.FromArgb(52, 211, 153);

        public override int GetPoints(int combo, int secondsLeft)
        {
            return (100 * combo) + (10 * secondsLeft);
        }
    }

    public class PwedeNaCard : Card
    {
        public PwedeNaCard(string t, string h, string r, string l) : base(t, h, r, l) { }

        public override CardResult Result => CardResult.PwedeNa;
        public override string Label => "PWEDE NA";
        public override Color Color => Color.FromArgb(253, 224, 71);

        public override int GetPoints(int combo, int secondsLeft)
        {
            return 40;
        }
    }

    public class SablayCard : Card
    {
        public SablayCard(string t, string h, string r, string l) : base(t, h, r, l) { }

        public override CardResult Result => CardResult.Sablay;
        public override string Label => "SABLAY!";
        public override Color Color => Color.FromArgb(244, 114, 182);

        public override int GetPoints(int combo, int secondsLeft)
        {
            return 0;
        }
    }

    public class Scenario
    {
        public Difficulty Disaster { get; }
        public FamilyType Who { get; }
        public string Text { get; }
        public Card[] Cards { get; }

        public Scenario(Difficulty disaster, FamilyType who, string text, params Card[] cards)
        {
            Disaster = disaster;
            Who = who;
            Text = text;
            Cards = cards;
        }

        // Hinahanap ang tamang sagot (HANDA card) para sa lesson feedback
        public Card BestCard
        {
            get
            {
                for (int i = 0; i < Cards.Length; i++)
                {
                    if (Cards[i].Result == CardResult.Handa)
                        return Cards[i];
                }
                return Cards[0];
            }
        }
    }

    public class EncounterDatabase
    {
        private readonly List<Scenario> scenarios = new List<Scenario>();
        private readonly Random rng = new Random();

        public List<string> MissedLessons { get; } = new List<string>();

        public EncounterDatabase()
        {
            // ---------------- TSUNAMI ----------------
            Add(Difficulty.Tsunami, FamilyType.Lola,
                "Biglang umatras ang tubig sa dalampasigan. Ano ang dapat mong gawin para kay Lola?",
                new HandaCard("Agad siyang dalhin sa mataas na lugar", "Mabilis na pagkilos.", "Ligtas kayo sa itaas bago dumating ang alon.", "Kapag biglang umatras ang dagat, tumakbo agad sa mataas na lugar."),
                new PwedeNaCard("Hintayin ang opisyal na babala ng barangay", "Suriin ang balita.", "Naunahan ng alon ang anunsyo.", "Babala na mismo ang pag-atras ng dagat. Huwag nang hintayin ang sirena."),
                new SablayCard("Mamulot ng mga isda sa tuyong sahig ng dagat", "Mag-ipon ng pagkain.", "Inanod kayo ng rumaragasang alon.", "Ang pag-atras ng dagat ay babala ng tsunami, hindi biyaya."));

            Add(Difficulty.Tsunami, FamilyType.Bunso,
                "May kumakalat na balita na sa mababang kapilya malapit sa baybayin magtago. Paano mo gagabayan si Bunso?",
                new HandaCard("Sundin ang mga berdeng tandang pangkaligtasan pataas", "Mag-evacuate.", "Nakarating kayo nang ligtas sa mataas na evacuation hill.", "Sundin ang evacuation route papunta sa mataas na lugar, hindi ang tsismis."),
                new PwedeNaCard("Manatili sa covered court malapit sa dagat", "Matibay ang bubong.", "Inabot pa rin ng baha ang ibabang bahagi.", "Taas ng lupa ang mahalaga, hindi taas ng bubong."),
                new SablayCard("Pumunta sa commercial area sa tabi ng dagat", "Bukas pa doon.", "Agad na sinalpok ng alon ang baybayin.", "Lumayo sa baybayin. Pinakadelikado ang malapit sa dagat."));

            Add(Difficulty.Tsunami, FamilyType.Bantay,
                "Nakatali si Bantay sa poste habang papalapit ang malakas na ugong ng alon. Ano ang iyong gagawin?",
                new HandaCard("Kalagan ang aso at sabay tumakbo paakyat", "Pakawalan siya.", "Nakasama mo ang iyong alaga sa paglikas.", "Isama ang alaga sa paglikas, pero huwag nang magtagal."),
                new PwedeNaCard("Iwan muna si Bantay para makaligtas kaagad", "Mabilis na desisyon.", "Ligtas ka nga, ngunit may bigat sa iyong dibdib.", "Bahagi ng pamilya ang alaga. Isama siya sa evacuation plan."),
                new SablayCard("Kunan ng video ang paparating na alon", "Mag-rekord.", "Tinangay ka ng rumaragasang tubig.", "Walang video na mas mahalaga sa buhay mo."));

            // ---------------- LINDOL ----------------
            Add(Difficulty.Lindol, FamilyType.Lola,
                "Lumindol habang ang isang mabigat na aparador ay malapit nang bumagsak kay Lola. Ano ang iyong gagawin?",
                new HandaCard("Sumilong sa ilalim ng matibay na mesa", "Drop, Cover, Hold.", "Ligtas kayo mula sa mga nahuhulog na gamit.", "Lumayo sa matataas na muwebles. Drop, Cover, Hold sa ilalim ng matibay na mesa."),
                new PwedeNaCard("Pigilan ninyong dalawa ang pagbagsak ng aparador", "Saluhin ito.", "Bumagsak ang kabinet at nasugatan kayo.", "Huwag pigilan ang bumabagsak na muwebles. Iligtas ang sarili, hindi ang plato."),
                new SablayCard("Unahing iligtas ang mga babasaging plato", "Mahalagang gamit.", "Tumaob ang aparador at natamaan ka.", "Napapalitan ang gamit. Protektahan ang ulo at katawan."));

            Add(Difficulty.Lindol, FamilyType.Bunso,
                "Nataranta si Bunso malapit sa malaking salaming bintana habang lumilindol. Paano mo siya aalalayan?",
                new HandaCard("Hatakin siya palayo at dumapa malapit sa pader", "Ligtas sa salamin.", "Walang sugat na natamo si Bunso.", "Huwag tumalon o tumakbo habang umuuga. Lumayo sa bintana."),
                new PwedeNaCard("Takpan ng kurtina ang kanyang mukha", "Panangga.", "Tumagos ang mga bubog sa tela.", "Hindi sapat ang kurtina. Lumayo talaga sa salamin."),
                new SablayCard("Utusang tumalon sa bintana para makalabas", "Tumalon na.", "Nagtamo siya ng malubhang pinsala sa katawan.", "Mas delikado ang pagtalon mula sa mataas na palapag."));

            Add(Difficulty.Lindol, FamilyType.Bantay,
                "Tumigil na ang lindol, ngunit may nakalaylay na live wire malapit kay Bantay. Ano ang iyong desisyon?",
                new HandaCard("Tawagin ang aso palayo gamit ang pagkain", "Ligtas na distansya.", "Lumayo si Bantay nang walang pinsala.", "Lumayo sa nahulog na kable at i-report ito sa barangay."),
                new PwedeNaCard("Hayaang kusa siyang lumayo sa kable", "Maghintay.", "Umalis siya makalipas ang ilang minuto.", "Ligtas ang hindi paglapit, pero gumawa ng paraan nang hindi hinahawakan ang kable."),
                new SablayCard("Hawakan at ilipat ang kable gamit ang kamay", "Ayusin ito.", "Nakuryente ka dahil may kuryente pa ito.", "Huwag hawakan ang nahulog na kable. Maaaring may kuryente pa ito."));

            // ---------------- EL NIÑO ----------------
            Add(Difficulty.ElNino, FamilyType.Lola,
                "Umiinit nang husto (42°C) at nahihilo na si Lola sa labas. Ano ang nararapat mong gawin?",
                new HandaCard("Dalhin siya sa lilim at painumin ng malinis na tubig", "Magpalamig.", "Bumuti ang pakiramdam at kalusugan ni Lola.", "Iwasan ang init mula 10 AM hanggang 4 PM at uminom ng tubig para iwas heat stroke."),
                new PwedeNaCard("Bigyan siya ng matamis na inuming malamig", "Pampalamig.", "Hindi natanggal ang pagkahilo ng matanda.", "Tubig ang kailangan ng katawan, hindi matamis na inumin."),
                new SablayCard("Hayaang tapusin ang kanyang gawain sa hardin", "Ituloy ang trabaho.", "Hinimatay si Lola dahil sa tindi ng init.", "Huwag pilitin ang matanda sa ilalim ng matinding init."));

            Add(Difficulty.ElNino, FamilyType.Bunso,
                "Kulang ang suplay ng tubig, ngunit ginagamit pa rin ni Bunso ang hose para maglaro. Paano mo siya aawayan?",
                new HandaCard("Isara ang gripo at gumamit ng timba at tabo", "Magtipid.", "May naiimbak pa kayong tubig para sa pamilya.", "Magtipid ng tubig tuwing El Niño: timba imbes na hose."),
                new PwedeNaCard("Hayaang maglaro ng ilang minuto pa", "Sandali na lang.", "Tuluyang naubos ang huling patak sa tangke.", "Bawat litro ay mahalaga kapag tagtuyot."),
                new SablayCard("Sumali sa pag-aaksaya ng tubig sa labas", "Makisaya.", "Wala na kayong magamit na tubig kinaumagahan.", "Ang tubig na sinayang ngayon ay kulang bukus."));

            Add(Difficulty.ElNino, FamilyType.Bantay,
                "Nakahandusay si Bantay at mabilis ang paghinga sa mainit na semento dahil sa tindi ng init. Ano ang gagawin mo?",
                new HandaCard("Ilipat siya sa lilim at painumin ng malinis na tubig", "Bigyan ng tubig.", "Unti-unting nanumbalik ang lakas ng aso.", "Pati alaga ay nae-heat stroke. Bigyan ng lilim at tubig."),
                new PwedeNaCard("Buhusan agad ng malamig na yelo ang katawan", "Palamigin agad.", "Nagulat ang aso at nakaranas ng pagkabigla.", "Unti-unti ang pagpapalamig. Hindi maganda ang biglang yelo."),
                new SablayCard("Ipagpatuloy ang pag-eehersisyo sa ilalim ng araw", "Magpapawis.", "Napaso ang mga paa ng aso sa mainit na semento.", "Huwag pagurin ang alaga sa matinding init."));
        }

        private void Add(Difficulty d, FamilyType who, string text, Card handa, Card pwede, Card sablay)
        {
            scenarios.Add(new Scenario(d, who, text, handa, pwede, sablay));
        }

        // Kinukuha ang scenario at hinahalo ang cards (Fisher-Yates shuffle)
        public Scenario Draw(Difficulty d, FamilyType who)
        {
            Scenario found = scenarios[0];
            for (int i = 0; i < scenarios.Count; i++)
            {
                if (scenarios[i].Disaster == d && scenarios[i].Who == who)
                {
                    found = scenarios[i];
                    break;
                }
            }

            // Copy at shuffle ang cards array para hindi nakikita sa iisang pwesto ang sagot
            Card[] shuffled = new Card[found.Cards.Length];
            for (int i = 0; i < found.Cards.Length; i++)
            {
                shuffled[i] = found.Cards[i];
            }

            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                Card temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            return new Scenario(found.Disaster, found.Who, found.Text, shuffled);
        }

        // index -1 = naubos ang oras
        public CardResult CheckAnswer(Scenario s, int index)
        {
            if (index < 0 || index >= s.Cards.Length)
                return CardResult.Sablay;

            return s.Cards[index].Result;
        }

        public void RecordMiss(string lesson)
        {
            if (!MissedLessons.Contains(lesson))
                MissedLessons.Add(lesson);
        }

        public static string GetRank(int stars)
        {
            switch (stars)
            {
                case 3: return "BARANGAY HERO";
                case 2: return "HANDA CAPTAIN";
                case 1: return "MEDYO HANDA";
                default: return "PRACTICE PA";
            }
        }

        public string GetReport(Difficulty d, int stars)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RANGGO: " + GetRank(stars));
            sb.AppendLine();

            if (d == Difficulty.Tsunami)
                sb.AppendLine("Kapag umatras ang dagat, umakyat agad sa mataas na lugar.");
            else if (d == Difficulty.Lindol)
                sb.AppendLine("Drop, Cover, Hold habang umuuga. Asahan ang aftershocks.");
            else
                sb.AppendLine("Tuwing El Niño: magtipid ng tubig at iwasan ang tirik na araw.");

            sb.AppendLine();

            if (MissedLessons.Count == 0)
            {
                sb.AppendLine("Perfect! Wala kang sablay.");
            }
            else
            {
                sb.AppendLine("MGA NATUTUNAN MO:");
                foreach (string lesson in MissedLessons)
                {
                    sb.AppendLine("  • " + lesson);
                }
            }

            return sb.ToString();
        }

        // Mapa: . sahig   # bahay   ^ mataas na lugar   E evacuation   P player   L B D pamilya
        public static string[] GetMap(Difficulty d)
        {
            if (d == Difficulty.Tsunami)
            {
                return new[]
                {
                    "P...#..^^..E", "..^^#.....^^", ".......##...", "L...^^......",
                    "..#.^^..#..B", "..#.....#...", "...^^.......", "#..^^..##.^^",
                    "....D.....^^", ".##.........", "....^^.#....", ".^^.^^.#..^^"
                };
            }

            if (d == Difficulty.Lindol)
            {
                return new[]
                {
                    "P....#.....E", ".##..#..##..", ".#......#...", "....L.......",
                    "###....###..", "......#...B.", ".#..#.#.....", ".#..#...##..",
                    "....##......", "..D......#..", ".##......#..", "............"
                };
            }

            return new[]
            {
                "...#....##.P", ".L.#........", "...#..##....", "......#...B.",
                ".##.......#.", "..#..##...#.", "..#...#.....", "......#.##..",
                ".D..........", "...##....##.", ".#.....#....", "E#.....#...."
            };
        }
    }
}