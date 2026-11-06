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
        public string Topic { get; }     // short title, reused in the final report
        public string Flavor { get; }    // scene-setting line, shown in the encounter AND the report
        public string Text { get; }
        public Card[] Cards { get; }

        public Scenario(Difficulty disaster, FamilyType who, string topic, string flavor, string text, params Card[] cards)
        {
            Disaster = disaster;
            Who = who;
            Topic = topic;
            Flavor = flavor;
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

    // One answered encounter. The end-of-level report is built from these.
    public class Decision
    {
        public FamilyType Who { get; }
        public string Topic { get; }
        public string Flavor { get; }
        public string Chosen { get; }    // title of the card the player picked
        public string Outcome { get; }   // HANDA! / PWEDE NA / SABLAY!

        public Decision(FamilyType who, string topic, string flavor, string chosen, string outcome)
        {
            Who = who;
            Topic = topic;
            Flavor = flavor;
            Chosen = chosen;
            Outcome = outcome;
        }
    }

    public class EncounterDatabase
    {
        private readonly List<Scenario> scenarios = new List<Scenario>();
        private readonly Random rng = new Random();

        public List<string> MissedLessons { get; } = new List<string>();
        public List<Decision> Decisions { get; } = new List<Decision>();

        public EncounterDatabase()
        {
            // ---------------- TSUNAMI ----------------
            Add(Difficulty.Tsunami, FamilyType.Lola,
                "Pag-atras ng dagat",
                "Tumahimik ang mga ibon. Amoy-asin at putik ang hangin.",
                "Biglang umatras ang tubig sa dalampasigan. Ano ang dapat mong gawin para kay Lola?",
                new HandaCard("Agad siyang dalhin sa mataas na lugar", "Mabilis na pagkilos.", "Ligtas kayo sa itaas bago dumating ang alon.", "Kapag biglang umatras ang dagat, tumakbo agad sa mataas na lugar."),
                new PwedeNaCard("Hintayin ang opisyal na babala ng barangay", "Suriin ang balita.", "Naunahan ng alon ang anunsyo.", "Babala na mismo ang pag-atras ng dagat. Huwag nang hintayin ang sirena."),
                new SablayCard("Mamulot ng mga isda sa tuyong sahig ng dagat", "Mag-ipon ng pagkain.", "Inanod kayo ng rumaragasang alon.", "Ang pag-atras ng dagat ay babala ng tsunami, hindi biyaya."));

            Add(Difficulty.Tsunami, FamilyType.Bunso,
                "Tsismis sa baybayin",
                "Sunod-sunod ang text. Magkakasalungat ang balita.",
                "May kumakalat na balita na sa mababang kapilya malapit sa baybayin magtago. Paano mo gagabayan si Bunso?",
                new HandaCard("Sundin ang mga berdeng tandang pangkaligtasan pataas", "Mag-evacuate.", "Nakarating kayo nang ligtas sa mataas na evacuation hill.", "Sundin ang evacuation route papunta sa mataas na lugar, hindi ang tsismis."),
                new PwedeNaCard("Manatili sa covered court malapit sa dagat", "Matibay ang bubong.", "Inabot pa rin ng baha ang ibabang bahagi.", "Taas ng lupa ang mahalaga, hindi taas ng bubong."),
                new SablayCard("Pumunta sa commercial area sa tabi ng dagat", "Bukas pa doon.", "Agad na sinalpok ng alon ang baybayin.", "Lumayo sa baybayin. Pinakadelikado ang malapit sa dagat."));

            Add(Difficulty.Tsunami, FamilyType.Bantay,
                "Ugong ng alon",
                "Kahol nang kahol si Bantay. Lumalakas ang ugong ng dagat.",
                "Nakatali si Bantay sa poste habang papalapit ang malakas na ugong ng alon. Ano ang iyong gagawin?",
                new HandaCard("Kalagan ang aso at sabay tumakbo paakyat", "Pakawalan siya.", "Nakasama mo ang iyong alaga sa paglikas.", "Isama ang alaga sa paglikas, pero huwag nang magtagal."),
                new PwedeNaCard("Iwan muna si Bantay para makaligtas kaagad", "Mabilis na desisyon.", "Ligtas ka nga, ngunit may bigat sa iyong dibdib.", "Bahagi ng pamilya ang alaga. Isama siya sa evacuation plan."),
                new SablayCard("Kunan ng video ang paparating na alon", "Mag-rekord.", "Tinangay ka ng rumaragasang tubig.", "Walang video na mas mahalaga sa buhay mo."));

            // ---------------- LINDOL ----------------
            Add(Difficulty.Lindol, FamilyType.Lola,
                "Bumabagsak na aparador",
                "Kumakalampag ang mga plato. Umuuga ang buong bahay.",
                "Lumindol habang ang isang mabigat na aparador ay malapit nang bumagsak kay Lola. Ano ang iyong gagawin?",
                new HandaCard("Sumilong sa ilalim ng matibay na mesa", "Drop, Cover, Hold.", "Ligtas kayo mula sa mga nahuhulog na gamit.", "Lumayo sa matataas na muwebles. Drop, Cover, Hold sa ilalim ng matibay na mesa."),
                new PwedeNaCard("Pigilan ninyong dalawa ang pagbagsak ng aparador", "Saluhin ito.", "Bumagsak ang kabinet at nasugatan kayo.", "Huwag pigilan ang bumabagsak na muwebles. Iligtas ang sarili, hindi ang plato."),
                new SablayCard("Unahing iligtas ang mga babasaging plato", "Mahalagang gamit.", "Tumaob ang aparador at natamaan ka.", "Napapalitan ang gamit. Protektahan ang ulo at katawan."));

            Add(Difficulty.Lindol, FamilyType.Bunso,
                "Salaming bintana",
                "Nanginginig ang mga bintana. Umiiyak si Bunso sa takot.",
                "Nataranta si Bunso malapit sa malaking salaming bintana habang lumilindol. Paano mo siya aalalayan?",
                new HandaCard("Hatakin siya palayo at dumapa malapit sa pader", "Ligtas sa salamin.", "Walang sugat na natamo si Bunso.", "Huwag tumalon o tumakbo habang umuuga. Lumayo sa bintana."),
                new PwedeNaCard("Takpan ng kurtina ang kanyang mukha", "Panangga.", "Tumagos ang mga bubog sa tela.", "Hindi sapat ang kurtina. Lumayo talaga sa salamin."),
                new SablayCard("Utusang tumalon sa bintana para makalabas", "Tumalon na.", "Nagtamo siya ng malubhang pinsala sa katawan.", "Mas delikado ang pagtalon mula sa mataas na palapag."));

            Add(Difficulty.Lindol, FamilyType.Bantay,
                "Nakalaylay na kable",
                "Tumigil na ang uga. Kumikislap ang kable sa poste.",
                "Tumigil na ang lindol, ngunit may nakalaylay na live wire malapit kay Bantay. Ano ang iyong desisyon?",
                new HandaCard("Tawagin ang aso palayo gamit ang pagkain", "Ligtas na distansya.", "Lumayo si Bantay nang walang pinsala.", "Lumayo sa nahulog na kable at i-report ito sa barangay."),
                new PwedeNaCard("Hayaang kusa siyang lumayo sa kable", "Maghintay.", "Umalis siya makalipas ang ilang minuto.", "Ligtas ang hindi paglapit, pero gumawa ng paraan nang hindi hinahawakan ang kable."),
                new SablayCard("Hawakan at ilipat ang kable gamit ang kamay", "Ayusin ito.", "Nakuryente ka dahil may kuryente pa ito.", "Huwag hawakan ang nahulog na kable. Maaaring may kuryente pa ito."));

            // ---------------- EL NIÑO ----------------
            Add(Difficulty.ElNino, FamilyType.Lola,
                "Matinding init",
                "Tirik ang araw. Walang hangin at pumuputok ang lupa.",
                "Umiinit nang husto (42°C) at nahihilo na si Lola sa labas. Ano ang nararapat mong gawin?",
                new HandaCard("Dalhin siya sa lilim at painumin ng malinis na tubig", "Magpalamig.", "Bumuti ang pakiramdam at kalusugan ni Lola.", "Iwasan ang init mula 10 AM hanggang 4 PM at uminom ng tubig para iwas heat stroke."),
                new PwedeNaCard("Bigyan siya ng matamis na inuming malamig", "Pampalamig.", "Hindi natanggal ang pagkahilo ng matanda.", "Tubig ang kailangan ng katawan, hindi matamis na inumin."),
                new SablayCard("Hayaang tapusin ang kanyang gawain sa hardin", "Ituloy ang trabaho.", "Hinimatay si Lola dahil sa tindi ng init.", "Huwag pilitin ang matanda sa ilalim ng matinding init."));

            Add(Difficulty.ElNino, FamilyType.Bunso,
                "Kulang na tubig",
                "Humihingal ang gripo. Halos wala nang laman ang tangke.",
                "Kulang ang suplay ng tubig, ngunit ginagamit pa rin ni Bunso ang hose para maglaro. Paano mo siya aawayan?",
                new HandaCard("Isara ang gripo at gumamit ng timba at tabo", "Magtipid.", "May naiimbak pa kayong tubig para sa pamilya.", "Magtipid ng tubig tuwing El Niño: timba imbes na hose."),
                new PwedeNaCard("Hayaang maglaro ng ilang minuto pa", "Sandali na lang.", "Tuluyang naubos ang huling patak sa tangke.", "Bawat litro ay mahalaga kapag tagtuyot."),
                new SablayCard("Sumali sa pag-aaksaya ng tubig sa labas", "Makisaya.", "Wala na kayong magamit na tubig kinaumagahan.", "Ang tubig na sinayang ngayon ay kulang bukus."));

            Add(Difficulty.ElNino, FamilyType.Bantay,
                "Heat stroke ni Bantay",
                "Umuusok sa init ang semento. Hingal na hingal si Bantay.",
                "Nakahandusay si Bantay at mabilis ang paghinga sa mainit na semento dahil sa tindi ng init. Ano ang gagawin mo?",
                new HandaCard("Ilipat siya sa lilim at painumin ng malinis na tubig", "Bigyan ng tubig.", "Unti-unting nanumbalik ang lakas ng aso.", "Pati alaga ay nae-heat stroke. Bigyan ng lilim at tubig."),
                new PwedeNaCard("Buhusan agad ng malamig na yelo ang katawan", "Palamigin agad.", "Nagulat ang aso at nakaranas ng pagkabigla.", "Unti-unti ang pagpapalamig. Hindi maganda ang biglang yelo."),
                new SablayCard("Ipagpatuloy ang pag-eehersisyo sa ilalim ng araw", "Magpapawis.", "Napaso ang mga paa ng aso sa mainit na semento.", "Huwag pagurin ang alaga sa matinding init."));
        }

        private void Add(Difficulty d, FamilyType who, string topic, string flavor, string text, Card handa, Card pwede, Card sablay)
        {
            scenarios.Add(new Scenario(d, who, topic, flavor, text, handa, pwede, sablay));
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

            return new Scenario(found.Disaster, found.Who, found.Topic, found.Flavor, found.Text, shuffled);
        }

        // index -1 = naubos ang oras
        public CardResult CheckAnswer(Scenario s, int index)
        {
            if (index < 0 || index >= s.Cards.Length)
                return CardResult.Sablay;

            return s.Cards[index].Result;
        }

        // Called by GameEngine after every answer so the report can retell the run.
        public void RecordDecision(Scenario s, int index)
        {
            bool answered = index >= 0 && index < s.Cards.Length;
            Decisions.Add(new Decision(
                s.Who,
                s.Topic,
                s.Flavor,
                answered ? s.Cards[index].Title : "(naubos ang oras)",
                answered ? s.Cards[index].Label : "SABLAY!"));
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

        // The one-line lesson for each disaster. The menu shows it on hover and the report opens with it.
        public static string GetTip(Difficulty d)
        {
            if (d == Difficulty.Tsunami)
                return "Kapag umatras ang dagat, umakyat agad sa mataas na lugar.";
            if (d == Difficulty.Lindol)
                return "Drop, Cover, Hold habang umuuga. Asahan ang aftershocks.";
            return "Tuwing El Niño: magtipid ng tubig at iwasan ang tirik na araw.";
        }

        // The barangay captain's closing remark: one line for every disaster and star count.
        public static string GetVerdict(Difficulty d, int stars)
        {
            string[][] lines =
            {
                new[]   // Tsunami (0 to 3 stars)
                {
                    "Kapitan: Maaga ang tunay na handa. Ulitin natin ang drill sa baybayin.",
                    "Kapitan: Nakaligtas kayo, pero balikan natin ang plano sa paglikas.",
                    "Kapitan: Mahusay ang paglikas. Kaunting bilis na lang at perpekto na.",
                    "Kapitan: Walang naiwan sa baybayin. Taas-noo ang buong barangay!"
                },
                new[]   // Lindol
                {
                    "Kapitan: Hindi pa huli ang lahat. Balikan natin ang earthquake drill.",
                    "Kapitan: Buti at nakalabas kayo. Pag-aralan ulit ang evacuation plan.",
                    "Kapitan: Maayos ang kilos mo. Aralin pa ang panganib pagkatapos ng lindol.",
                    "Kapitan: Kalmado ang lahat habang umuuga. Panalo ang Drop, Cover, Hold!"
                },
                new[]   // El Niño
                {
                    "Kapitan: Mahaba pa ang tag-init. Maghanda tayo nang mas maaga.",
                    "Kapitan: Nakayanan ninyo ang init, pero huwag nang magsayang ng tubig.",
                    "Kapitan: Mahusay ang pagtitipid at pag-iingat sa init.",
                    "Kapitan: Buo ang tubig, buo ang pamilya. Ikaw ang huwaran ng baryo!"
                }
            };

            return lines[(int)d][Math.Max(0, Math.Min(3, stars))];
        }

        // The Barangay Assembly Report: rank, lesson, captain's remark, a log of every
        // encounter (with the same scene line the player saw), then the lessons missed.
        public string GetReport(Difficulty d, int stars)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RANGGO: " + GetRank(stars));
            sb.AppendLine();
            sb.AppendLine(GetTip(d));
            sb.AppendLine();
            sb.AppendLine(GetVerdict(d, stars));
            sb.AppendLine();

            if (Decisions.Count > 0)
            {
                sb.AppendLine("TALA NG PAGSAGIP:");
                foreach (Decision x in Decisions)
                {
                    sb.AppendLine(x.Who.ToString().ToUpper() + " - " + x.Topic + " [" + x.Outcome + "]");
                    sb.AppendLine("  \"" + x.Flavor + "\"");
                    sb.AppendLine("  Pinili: " + x.Chosen);
                }
                sb.AppendLine();
            }

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