// =====================================================================
//  EncounterDatabase.cs
//  OWNER: Member 1 - Back-End (Data & Scenarios)
//
//  Everything the player READS lives here:
//    - the vague scenarios and their 3 cards (HANDA / PWEDE NA / SABLAY)
//    - the meme reactions and the lessons
//    - the 3 level maps (as text, easy to edit)
//    - the Barangay Assembly Report
//
//  OOP: Card is ABSTRACT. HandaCard, PwedeNaCard and SablayCard INHERIT
//  from it and each OVERRIDES GetPoints() and HeartChange (POLYMORPHISM).
// =====================================================================
using System.Text;

namespace HandaKaNaBa
{
    public enum Difficulty { Tsunami, Earthquake, Volcano }
    public enum FamilyType { Lola, Bunso, Bantay }
    public enum CardResult { Handa, PwedeNa, Sablay }

    // ------------------------------------------------------------------
    //  CARDS
    // ------------------------------------------------------------------
    public abstract class Card
    {
        public string Title { get; }      // the action, shown on the card front
        public string Hint { get; }       // short "thought bubble" under the title
        public string Reaction { get; }   // funny / meme line after picking
        public string Lesson { get; }     // the real safety lesson

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
        public abstract int HeartChange { get; }

        /// <summary>Points for picking this card. combo = current multiplier (1-3).</summary>
        public abstract int GetPoints(int combo, int secondsLeft);
    }

    public class HandaCard : Card
    {
        public HandaCard(string title, string hint, string reaction, string lesson)
            : base(title, hint, reaction, lesson) { }

        public override CardResult Result => CardResult.Handa;
        public override string Label => "HANDA!";
        public override Color Color => Color.FromArgb(52, 211, 153);
        public override int HeartChange => 0;
        public override int GetPoints(int combo, int secondsLeft) => 100 * combo + 10 * secondsLeft;
    }

    public class PwedeNaCard : Card
    {
        public PwedeNaCard(string title, string hint, string reaction, string lesson)
            : base(title, hint, reaction, lesson) { }

        public override CardResult Result => CardResult.PwedeNa;
        public override string Label => "PWEDE NA";
        public override Color Color => Color.FromArgb(253, 224, 71);
        public override int HeartChange => 0;
        public override int GetPoints(int combo, int secondsLeft) => 40;
    }

    public class SablayCard : Card
    {
        public SablayCard(string title, string hint, string reaction, string lesson)
            : base(title, hint, reaction, lesson) { }

        public override CardResult Result => CardResult.Sablay;
        public override string Label => "SABLAY!";
        public override Color Color => Color.FromArgb(244, 114, 182);
        public override int HeartChange => -1;
        public override int GetPoints(int combo, int secondsLeft) => 0;
    }

    // ------------------------------------------------------------------
    //  SCENARIO = one situation + 3 cards
    // ------------------------------------------------------------------
    public class Scenario
    {
        public Difficulty Disaster { get; }
        public FamilyType? For { get; }          // null = works for any family member
        public string Text { get; }              // may contain {name}
        public Card[] Cards { get; }

        public Scenario(Difficulty disaster, FamilyType? forWho, string text, Card handa, Card pwedeNa, Card sablay)
        {
            Disaster = disaster;
            For = forWho;
            Text = text;
            Cards = new[] { handa, pwedeNa, sablay };
        }

        private Scenario(Scenario original, Card[] shuffledCards)
        {
            Disaster = original.Disaster;
            For = original.For;
            Text = original.Text;
            Cards = shuffledCards;
        }

        public string TextFor(string name) => Text.Replace("{name}", name);

        /// <summary>A copy with the 3 cards in random order, so the answer is never always in the same spot.</summary>
        public Scenario Shuffled(Random rng) => new Scenario(this, Cards.OrderBy(_ => rng.Next()).ToArray());

        public Card BestCard => Cards.First(c => c.Result == CardResult.Handa);
    }

    // ------------------------------------------------------------------
    //  LEVEL DATA = one map, written as 12 strings of 12 characters
    //    .  floor            #  house / wall      ^  high ground (safe from the tsunami)
    //    E  evacuation center (exit)               P  player start
    //    L  Lola    B  Bunso    D  Bantay (dog)    G  Go-Bag (shield pickup)
    // ------------------------------------------------------------------
    public class LevelData
    {
        public Difficulty Disaster { get; }
        public string Title { get; }
        public string Tagline { get; }
        public string[] Rows { get; }

        public LevelData(Difficulty disaster, string title, string tagline, string[] rows)
        {
            Disaster = disaster;
            Title = title;
            Tagline = tagline;
            Rows = rows;
        }
    }

    // ------------------------------------------------------------------
    //  THE DATABASE
    // ------------------------------------------------------------------
    public class EncounterDatabase
    {
        public const string TimeoutReaction = "Na-freeze ka! Walang napili, walang nangyari... at dumating ang sakuna.";
        public const string LostLesson = "Unahin ang pinakamalapit sa panganib. Mas maaga, mas ligtas.";

        private readonly List<Scenario> scenarios = new List<Scenario>();
        private readonly HashSet<Scenario> used = new HashSet<Scenario>();
        private readonly Random rng = new Random();

        /// <summary>Lessons from every card the player got wrong this run (shown in the report).</summary>
        public List<string> MissedLessons { get; } = new List<string>();

        public EncounterDatabase()
        {
            AddTsunamiScenarios();
            AddEarthquakeScenarios();
            AddVolcanoScenarios();
        }

        public static string NameOf(FamilyType who)
        {
            switch (who)
            {
                case FamilyType.Lola: return "Lola";
                case FamilyType.Bunso: return "Bunso";
                default: return "Bantay";
            }
        }

        public void ResetRun()
        {
            used.Clear();
            MissedLessons.Clear();
        }

        /// <summary>Picks a scenario for this disaster and family member, preferring ones not seen yet.</summary>
        public Scenario Draw(Difficulty disaster, FamilyType who)
        {
            List<Scenario> pool = scenarios.Where(s => s.Disaster == disaster && (s.For == who || s.For == null)).ToList();
            List<Scenario> fresh = pool.Where(s => !used.Contains(s)).ToList();

            Scenario pick = fresh.FirstOrDefault(s => s.For == who)
                         ?? fresh.FirstOrDefault()
                         ?? pool[rng.Next(pool.Count)];

            used.Add(pick);
            return pick.Shuffled(rng);
        }

        /// <summary>index = the card clicked (0-2). -1 means the panic timer ran out.</summary>
        public CardResult CheckAnswer(Scenario scenario, int index)
        {
            if (scenario == null || index < 0 || index >= scenario.Cards.Length)
                return CardResult.Sablay;
            return scenario.Cards[index].Result;
        }

        public void RecordMiss(string lesson)
        {
            if (!string.IsNullOrEmpty(lesson) && !MissedLessons.Contains(lesson))
                MissedLessons.Add(lesson);
        }

        // ---------------- Barangay Assembly Report ----------------

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

        public static string GetMoral(Difficulty disaster)
        {
            switch (disaster)
            {
                case Difficulty.Tsunami:
                    return "Kapag biglang umatras ang dagat, huwag nang hintayin ang sirena. Umakyat agad sa mataas na lugar.";
                case Difficulty.Earthquake:
                    return "Drop, Cover, Hold habang umuuga. Pagkatapos, lumabas nang maingat at asahan ang aftershocks.";
                default:
                    return "Lumikas agad kapag itinaas ang alert level. Iwasan ang abo, ang ilog at ang lambak.";
            }
        }

        public string GetBarangayAssemblyReport(Difficulty disaster, int stars)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RANGGO: " + GetRank(stars));
            sb.AppendLine();
            sb.AppendLine(GetMoral(disaster));
            sb.AppendLine();
            if (MissedLessons.Count == 0)
            {
                sb.AppendLine("Perfect! Wala kang sablay. Ligtas ang may alam!");
            }
            else
            {
                sb.AppendLine("MGA NATUTUNAN MO:");
                foreach (string lesson in MissedLessons)
                    sb.AppendLine("  • " + lesson);
            }
            return sb.ToString();
        }

        // ---------------- The 3 maps ----------------

        public static LevelData GetLevel(Difficulty disaster)
        {
            switch (disaster)
            {
                case Difficulty.Tsunami:
                    return new LevelData(disaster, "UMATRAS ANG DAGAT", "Tumayo sa berdeng burol (^) kapag dumating ang alon!", new[]
                    {
                        "P...#..^^..E",
                        "..^^#.....^^",
                        ".......##...",
                        "L...^^......",
                        "..#.^^..#..B",
                        "..#.....#...",
                        "...^^.......",
                        "#..^^..##.^^",
                        "....D.....^^",
                        ".##.....G...",
                        "....^^.#....",
                        ".^^.^^.#..^^",
                    });

                case Difficulty.Earthquake:
                    return new LevelData(disaster, "UGA SA UMAGA", "Lumalala ang bitak habang tumatagal. Bilisan!", new[]
                    {
                        "P....#.....E",
                        ".##..#..##..",
                        ".#......#...",
                        "....L.......",
                        "###....###..",
                        "......#...B.",
                        ".#..#.#.....",
                        ".#..#...##..",
                        "....##......",
                        "..D......#..",
                        ".##..G...#..",
                        "............",
                    });

                default:
                    return new LevelData(disaster, "ABO AT APOY", "Bantayan ang anino ng pagsabog. Lumayo sa gitna!", new[]
                    {
                        "...#....##.P",
                        ".L.#........",
                        "...#..##....",
                        "......#...B.",
                        ".##.......#.",
                        "..#..##...#.",
                        "..#...#.....",
                        "......#.##..",
                        ".D..........",
                        "...##..G.##.",
                        ".#.....#....",
                        "E#.....#....",
                    });
            }
        }

        // ---------------- Scenarios ----------------
        // Each Add(...) = one scenario. Order of cards here does not matter:
        // they are shuffled every time they are shown.

        private void Add(Difficulty d, FamilyType? who, string text, Card handa, Card pwede, Card sablay)
        {
            scenarios.Add(new Scenario(d, who, text, handa, pwede, sablay));
        }

        private void AddTsunamiScenarios()
        {
            var d = Difficulty.Tsunami;

            Add(d, FamilyType.Lola,
                "Kakatapos lang ng lindol. Si Lola, nakatitig sa dagat: \"Apo, ang layo ng inatras ng tubig! Ang daming isda sa buhangin!\"",
                new HandaCard("Hilahin si Lola papunta sa burol", "Walang tanong-tanong.",
                    "Nasa taas na kayo nang dumating ang alon. Si Lola pa ang nauna.",
                    "Kapag biglang umatras ang dagat, tumakbo agad sa mataas na lugar. Babalik ito nang mas malakas."),
                new PwedeNaCard("Hintayin ang announcement ng barangay", "Baka false alarm lang.",
                    "Dumating ang text blast... kasabay ng alon. Muntik na!",
                    "Natural na babala ang pag-atras ng dagat. Hindi na kailangang hintayin ang sirena."),
                new SablayCard("Pulutin ang mga isda, sayang!", "Libreng ulam for one week.",
                    "Fresh catch nga. Ikaw ang nahuli ng alon.",
                    "Ang pag-atras ng dagat ay babala ng tsunami, hindi biyaya."));

            Add(d, FamilyType.Bunso,
                "Umiiyak si Bunso sa covered court. May sumisigaw: \"Sa simbahan tayo, mas matibay!\" Nasa tabing-dagat ang simbahan.",
                new HandaCard("Sundan ang evacuation signs paakyat", "Yung berdeng arrow.",
                    "Ligtas kayo sa eskwelahan sa taas ng burol.",
                    "Sundin ang evacuation route papunta sa mataas na lugar, hindi ang tsismis."),
                new PwedeNaCard("Manatili muna sa covered court", "Matibay naman ang bubong.",
                    "Matibay nga ang bubong. Inabot pa rin ng tubig ang sahig.",
                    "Hindi taas ng bubong ang mahalaga kundi taas ng lupa."),
                new SablayCard("Sumama sa simbahan sa may dagat", "Mas maraming tao, mas safe?",
                    "Nakapagdasal ka nga. Nakalangoy ka rin.",
                    "Lumayo sa baybayin. Pinakadelikado ang malapit sa dagat."));

            Add(d, FamilyType.Bantay,
                "Nakatali si Bantay sa poste sa labas, tahol nang tahol. Naririnig mo na ang dagundong mula sa dagat.",
                new HandaCard("Kalagan si Bantay at sabay kayong tumakbo", "Dalawang segundo lang 'to.",
                    "Mas mabilis pa si Bantay sa 'yo paakyat ng burol.",
                    "Isama ang alaga sa paglikas, pero huwag nang magtagal."),
                new PwedeNaCard("Iwan muna si Bantay, babalikan mamaya", "Marunong naman siyang lumangoy.",
                    "Nakaligtas kayong dalawa. Pero hindi ka na niya pinapansin.",
                    "Bahagi ng pamilya ang alaga. Isama siya sa evacuation plan."),
                new SablayCard("I-video muna ang paparating na alon", "Pang-viral 'to!",
                    "Nag-viral nga: 'Lalaking tinangay ng alon habang nagvi-video.'",
                    "Walang video na mas mahalaga sa buhay mo."));

            Add(d, null,
                "Tanong ni {name}: \"Ano'ng dadalhin natin?\" Tatlumpung segundo na lang bago kayo umalis.",
                new HandaCard("Go-bag lang, tara na", "Nakahanda na 'yon, 'di ba?",
                    "Nasa labas na kayo bago pa umabot ng 30.",
                    "Ihanda ang go-bag bago pa ang sakuna, para segundo lang ang pag-alis."),
                new PwedeNaCard("Mabilis na impake ng damit at pagkain", "Essentials lang.",
                    "Kumpleto nga ang gamit, pero huli kayong nakaalis.",
                    "Sa tsunami, oras ang pinakamahalagang dala."),
                new SablayCard("Isakay muna ang ref sa tricycle", "Bagong hulog pa 'yan!",
                    "Lumutang ang ref. Lumutang ka rin.",
                    "Mapapalitan ang gamit. Ang buhay, hindi."));
        }

        private void AddEarthquakeScenarios()
        {
            var d = Difficulty.Earthquake;

            Add(d, FamilyType.Lola,
                "Lumilindol! Si Lola, nakakapit sa matangkad na aparador na puno ng plato. Umuuga na ito.",
                new HandaCard("Ilayo si Lola at sumilong sa ilalim ng mesa", "Yung matibay na mesa sa kusina.",
                    "Bumagsak ang aparador. Kayo, ligtas sa ilalim ng mesa.",
                    "Lumayo sa matataas na muwebles. Drop, Cover, Hold sa ilalim ng matibay na mesa."),
                new PwedeNaCard("Sabay ninyong hawakan ang aparador", "Kaya natin 'to!",
                    "Hindi n'yo kinaya. Buti nakailag kayo sa huling segundo.",
                    "Huwag pigilan ang bumabagsak na muwebles. Iligtas ang sarili, hindi ang plato."),
                new SablayCard("Iligtas muna ang mga mamahaling plato", "Pamana pa 'yan ni Lola!",
                    "Nailigtas mo ang isang plato. Yung isa pa, sa ulo mo.",
                    "Napapalitan ang gamit. Protektahan ang ulo at katawan."));

            Add(d, FamilyType.Bunso,
                "Nasa 2nd floor si Bunso, katabi ng bintana. Nanginginig ang salamin at gusto niyang tumalon palabas.",
                new HandaCard("Pigilan siya, Drop-Cover-Hold sa tabi ng pader", "Malayo sa salamin.",
                    "Natapos ang uga. Walang galos si Bunso.",
                    "Huwag tumalon o tumakbo habang umuuga. Lumayo sa bintana at salamin."),
                new PwedeNaCard("Isara ang kurtina at magtago sa likod", "Para hindi tamaan ng bubog.",
                    "Nakatulong nang kaunti ang kurtina. May gasgas pa rin.",
                    "Hindi sapat ang kurtina. Lumayo talaga sa salamin."),
                new SablayCard("Sabay kayong tumalon sa bintana", "Parang sa movies!",
                    "Hindi ka si Spider-Man. Na-sprain ang paa mo.",
                    "Mas delikado pa sa lindol ang pagtalon mula sa mataas na palapag."));

            Add(d, FamilyType.Bantay,
                "Tumigil na ang uga. Si Bantay, nagtatago sa ilalim ng nakalaylay na kable ng kuryente sa labas.",
                new HandaCard("Tawagin siya mula sa malayo gamit ang treat", "Huwag lalapit sa kable.",
                    "Tumakbo si Bantay papunta sa 'yo. Good boy!",
                    "Lumayo sa nahulog na kable ng kuryente at i-report ito sa barangay."),
                new PwedeNaCard("Hintayin siyang kusang lumabas", "Lalabas din 'yan pag gutom na.",
                    "Lumabas din siya pagkalipas ng isang oras. Ang tagal!",
                    "Ligtas ang hindi paglapit sa kable, pero gumawa ng paraan nang hindi ito hinahawakan."),
                new SablayCard("Itaas ang kable gamit ang kamay", "Mabilis lang naman.",
                    "Bzzzt! Bagong hairstyle unlocked.",
                    "Huwag hawakan ang nahulog na kable. Maaaring may kuryente pa ito."));

            Add(d, null,
                "Tapos na ang malakas na uga. Sabi ni {name}: \"Balik na tayo sa loob, tapos na naman.\"",
                new HandaCard("Manatili muna sa open space", "Malayo sa poste at pader.",
                    "Dumating ang aftershock. Ligtas kayo sa gitna ng field.",
                    "Asahan ang aftershocks. Manatili sa bukas na lugar hangga't hindi ligtas ang gusali."),
                new PwedeNaCard("Pumasok sandali para kumuha ng tubig", "Mabilis lang, promise.",
                    "Umuga ulit habang nasa loob ka. Nakalabas ka, pero kinabahan ka nang todo.",
                    "Bawat pagpasok sa gusaling niyanig ay panganib. Dalhin na ang kailangan bago lumabas."),
                new SablayCard("Bumalik sa loob at matulog na", "Puyat na tayo.",
                    "Ginising ka ng aftershock... at ng kisame.",
                    "Huwag bumalik sa loob hangga't hindi pa nasusuri ang gusali."));
        }

        private void AddVolcanoScenarios()
        {
            var d = Difficulty.Volcano;

            Add(d, FamilyType.Lola,
                "Umuulan ng abo. Si Lola, nasa bakuran, nagwawalis ng abo habang umuubo-ubo.",
                new HandaCard("Ipasok siya at bigyan ng mask o basang tela", "Isara rin ang bintana.",
                    "Huminto ang ubo ni Lola. Malinis ang hangin sa loob.",
                    "Nakakasira ng baga ang abo ng bulkan. Manatili sa loob at magsuot ng mask."),
                new PwedeNaCard("Bigyan siya ng payong at hayaang tapusin", "Para hindi siya mainip.",
                    "Natapos ang walis. Hindi natapos ang ubo.",
                    "Hindi sapat ang payong. Ang hangin mismo ang delikado."),
                new SablayCard("Tulungan siyang magwalis para mabilis", "Teamwork!",
                    "Dalawa na kayong umuubo. Teamwork nga.",
                    "Bawasan ang paglabas habang may ashfall."));

            Add(d, FamilyType.Bunso,
                "Tumunog ang sirena. Ayaw umalis ni Bunso: \"Hindi pa naman sumasabog! Tingnan mo, ang ganda ng usok!\"",
                new HandaCard("Sumakay na sa unang biyahe palabas", "Habang maluwag pa ang daan.",
                    "Nakalabas kayo bago nagsiksikan ang lahat.",
                    "Lumikas agad kapag itinaas ang alert level. Hindi hinihintay ang pagsabog."),
                new PwedeNaCard("Mag-impake muna nang maayos", "Para kumpleto.",
                    "Kumpleto ang gamit, pero na-stuck kayo sa evacuation traffic.",
                    "Kapag may utos na lumikas, bawat minuto ay mahalaga."),
                new SablayCard("Umakyat sa bubong para sa better view", "Front row seat!",
                    "Ang ganda nga ng view. Hanggang tinamaan ka ng bato.",
                    "Hindi palabas ang bulkan. Lumayo, huwag lumapit."));

            Add(d, FamilyType.Bantay,
                "Si Bantay, umiinom sa ilog na kulay-abo na at amoy-asupre.",
                new HandaCard("Ilayo siya at painumin ng nakaimbak na tubig", "Yung nasa saradong lalagyan.",
                    "Masaya si Bantay sa malinis na tubig. Tail wag!",
                    "Kontaminado ng abo ang tubig sa labas. Gumamit ng nakaimbak at saradong tubig."),
                new PwedeNaCard("Pakuluan muna ang tubig ng ilog", "Pag pinakuluan, okay na?",
                    "Kumulo nga, pero lasang abo pa rin.",
                    "Hindi natatanggal ng pagpapakulo ang abo at kemikal."),
                new SablayCard("Hayaan lang, natural naman 'yan", "Aso naman 'yan, matibay.",
                    "Sumakit ang tiyan ni Bantay. Ikaw ang naglinis.",
                    "Huwag ipainom sa tao o alaga ang tubig na may abo."));

            Add(d, null,
                "Lumalakas ang dagundong. Tanong ni {name}: \"Saan tayo dadaan? Shortcut sa tabi ng ilog o yung highway?\"",
                new HandaCard("Highway, kahit malayo, palayo sa bulkan", "Yung official na evacuation route.",
                    "Mahaba ang biyahe, pero ligtas kayo.",
                    "Iwasan ang ilog at lambak. Doon dumadaloy ang lahar."),
                new PwedeNaCard("Magtanong muna sa mga kapitbahay", "Baka may alam sila.",
                    "Iba-iba ang sagot nila. Nasayang ang sampung minuto.",
                    "Alamin ang evacuation route bago pa ang sakuna."),
                new SablayCard("Shortcut sa tabi ng ilog, mas mabilis!", "Sabi ng Waze, 5 mins.",
                    "Mabilis nga. Pati ang lahar.",
                    "Daanan ng lahar ang mga ilog malapit sa bulkan. Huwag doon dumaan."));
        }
    }
}
