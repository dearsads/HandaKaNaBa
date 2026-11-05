// EncounterDatabase.cs - Member 1 (Data & Scenarios)
// Dito nakalagay ang lahat ng cards, scenarios, maps at ang report sa dulo.
using System.Text;

namespace HandaKaNaBa
{
    public enum Difficulty { Tsunami, Lindol, ElNino }
    public enum FamilyType { Lola, Bunso, Bantay }
    public enum CardResult { Handa, PwedeNa, Sablay }

    // ABSTRACT parent class ng lahat ng cards
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
        public abstract int GetPoints(int combo, int secondsLeft);   // POLYMORPHISM
    }

    public class HandaCard : Card
    {
        public HandaCard(string t, string h, string r, string l) : base(t, h, r, l) { }
        public override CardResult Result => CardResult.Handa;
        public override string Label => "HANDA!";
        public override Color Color => Color.FromArgb(52, 211, 153);
        public override int GetPoints(int combo, int secondsLeft) => 100 * combo + 10 * secondsLeft;
    }

    public class PwedeNaCard : Card
    {
        public PwedeNaCard(string t, string h, string r, string l) : base(t, h, r, l) { }
        public override CardResult Result => CardResult.PwedeNa;
        public override string Label => "PWEDE NA";
        public override Color Color => Color.FromArgb(253, 224, 71);
        public override int GetPoints(int combo, int secondsLeft) => 40;
    }

    public class SablayCard : Card
    {
        public SablayCard(string t, string h, string r, string l) : base(t, h, r, l) { }
        public override CardResult Result => CardResult.Sablay;
        public override string Label => "SABLAY!";
        public override Color Color => Color.FromArgb(244, 114, 182);
        public override int GetPoints(int combo, int secondsLeft) => 0;
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

        public Card BestCard => Cards.First(c => c.Result == CardResult.Handa);
    }

    public class EncounterDatabase
    {
        private List<Scenario> scenarios = new List<Scenario>();
        private Random rng = new Random();
        public List<string> MissedLessons { get; } = new List<string>();

        public EncounterDatabase()
        {
            // ---------------- TSUNAMI ----------------
            Add(Difficulty.Tsunami, FamilyType.Lola, "Kakatapos lang ng lindol. Si Lola, nakatitig sa dagat: \"Apo, ang layo ng inatras ng tubig! Ang daming isda!\"",
                new HandaCard("Hilahin si Lola papunta sa burol", "Walang tanong-tanong.", "Nasa taas na kayo nang dumating ang alon.", "Kapag biglang umatras ang dagat, tumakbo agad sa mataas na lugar."),
                new PwedeNaCard("Hintayin ang announcement ng barangay", "Baka false alarm lang.", "Dumating ang text blast... kasabay ng alon.", "Babala na mismo ang pag-atras ng dagat. Huwag nang hintayin ang sirena."),
                new SablayCard("Pulutin ang mga isda, sayang!", "Libreng ulam for one week.", "Fresh catch nga. Ikaw ang nahuli ng alon.", "Ang pag-atras ng dagat ay babala ng tsunami, hindi biyaya."));

            Add(Difficulty.Tsunami, FamilyType.Bunso, "Umiiyak si Bunso sa covered court. May sumisigaw: \"Sa simbahan tayo, mas matibay!\" Nasa tabing-dagat ang simbahan.",
                new HandaCard("Sundan ang evacuation signs paakyat", "Yung berdeng arrow.", "Ligtas kayo sa eskwelahan sa taas ng burol.", "Sundin ang evacuation route papunta sa mataas na lugar, hindi ang tsismis."),
                new PwedeNaCard("Manatili muna sa covered court", "Matibay naman ang bubong.", "Matibay nga ang bubong. Inabot pa rin ng tubig ang sahig.", "Taas ng lupa ang mahalaga, hindi taas ng bubong."),
                new SablayCard("Sumama sa simbahan sa may dagat", "Mas maraming tao, mas safe?", "Nakapagdasal ka nga. Nakalangoy ka rin.", "Lumayo sa baybayin. Pinakadelikado ang malapit sa dagat."));

            Add(Difficulty.Tsunami, FamilyType.Bantay, "Nakatali si Bantay sa poste, tahol nang tahol. Naririnig mo na ang dagundong mula sa dagat.",
                new HandaCard("Kalagan si Bantay at sabay kayong tumakbo", "Dalawang segundo lang 'to.", "Mas mabilis pa si Bantay sa 'yo paakyat.", "Isama ang alaga sa paglikas, pero huwag nang magtagal."),
                new PwedeNaCard("Iwan muna, babalikan mamaya", "Marunong naman siyang lumangoy.", "Nakaligtas kayo. Pero hindi ka na niya pinapansin.", "Bahagi ng pamilya ang alaga. Isama siya sa evacuation plan."),
                new SablayCard("I-video muna ang paparating na alon", "Pang-viral 'to!", "Nag-viral nga: 'Lalaking tinangay habang nagvi-video.'", "Walang video na mas mahalaga sa buhay mo."));

            // ---------------- LINDOL ----------------
            Add(Difficulty.Lindol, FamilyType.Lola, "Lumilindol! Si Lola, nakakapit sa matangkad na aparador na puno ng plato. Umuuga na ito.",
                new HandaCard("Ilayo si Lola, sumilong sa ilalim ng mesa", "Yung matibay sa kusina.", "Bumagsak ang aparador. Kayo, ligtas sa ilalim ng mesa.", "Lumayo sa matataas na muwebles. Drop, Cover, Hold sa ilalim ng matibay na mesa."),
                new PwedeNaCard("Sabay ninyong hawakan ang aparador", "Kaya natin 'to!", "Hindi n'yo kinaya. Buti nakailag kayo.", "Huwag pigilan ang bumabagsak na muwebles. Iligtas ang sarili, hindi ang plato."),
                new SablayCard("Iligtas muna ang mga plato", "Pamana pa 'yan ni Lola!", "Nailigtas mo ang isang plato. Yung isa pa, sa ulo mo.", "Napapalitan ang gamit. Protektahan ang ulo at katawan."));

            Add(Difficulty.Lindol, FamilyType.Bunso, "Nasa 2nd floor si Bunso, katabi ng bintana. Nanginginig ang salamin at gusto niyang tumalon palabas.",
                new HandaCard("Pigilan siya, Drop-Cover-Hold sa tabi ng pader", "Malayo sa salamin.", "Natapos ang uga. Walang galos si Bunso.", "Huwag tumalon o tumakbo habang umuuga. Lumayo sa bintana."),
                new PwedeNaCard("Magtago sa likod ng kurtina", "Para hindi tamaan ng bubog.", "Nakatulong nang kaunti. May gasgas pa rin.", "Hindi sapat ang kurtina. Lumayo talaga sa salamin."),
                new SablayCard("Sabay kayong tumalon sa bintana", "Parang sa movies!", "Hindi ka si Spider-Man. Na-sprain ang paa mo.", "Mas delikado ang pagtalon mula sa mataas na palapag."));

            Add(Difficulty.Lindol, FamilyType.Bantay, "Tumigil na ang uga. Si Bantay, nagtatago sa ilalim ng nakalaylay na kable ng kuryente.",
                new HandaCard("Tawagin siya mula sa malayo gamit ang treat", "Huwag lalapit sa kable.", "Tumakbo si Bantay papunta sa 'yo. Good boy!", "Lumayo sa nahulog na kable at i-report ito sa barangay."),
                new PwedeNaCard("Hintayin siyang kusang lumabas", "Lalabas din 'yan pag gutom.", "Lumabas din siya pagkalipas ng isang oras.", "Ligtas ang hindi paglapit, pero gumawa ng paraan nang hindi hinahawakan ang kable."),
                new SablayCard("Itaas ang kable gamit ang kamay", "Mabilis lang naman.", "Bzzzt! Bagong hairstyle unlocked.", "Huwag hawakan ang nahulog na kable. Maaaring may kuryente pa ito."));

            // ---------------- EL NIÑO ----------------
            Add(Difficulty.ElNino, FamilyType.Lola, "Tanghaling tapat, 38°C. Si Lola, nagtatanim sa bakuran nang walang sumbrero at nahihilo na.",
                new HandaCard("Dalhin siya sa lilim at painumin ng tubig", "Paypayan mo rin.", "Bumalik ang kulay ni Lola. \"Salamat, apo.\"", "Iwasan ang init mula 10 AM hanggang 4 PM at uminom ng tubig para iwas heat stroke."),
                new PwedeNaCard("Bigyan siya ng malamig na softdrinks", "Pampalamig daw.", "Lumamig ang lalamunan. Nahihilo pa rin siya.", "Tubig ang kailangan ng katawan, hindi matamis na inumin."),
                new SablayCard("Sabihing tapusin muna ang tanim", "Konti na lang naman.", "Natapos ang tanim. Si Lola, natumba.", "Huwag pilitin ang matanda sa ilalim ng matinding init."));

            Add(Difficulty.ElNino, FamilyType.Bunso, "Dalawang buwan nang walang ulan. Si Bunso, naglalaro ng hose at pinapaagos ang tubig sa kalsada.",
                new HandaCard("Patayin ang hose, mag-timba na lang", "Tipid pero masaya pa rin.", "May tubig pa kayo hanggang Linggo.", "Magtipid ng tubig tuwing El Niño: timba imbes na hose."),
                new PwedeNaCard("Hayaan, basta 5 minutes lang", "Bata pa naman.", "Ang 5 minutes naging isang oras.", "Bawat litro ay mahalaga kapag tagtuyot."),
                new SablayCard("Sumali ka na rin, ang init eh!", "Water party!", "Masaya kayo. Wala nang tubig bukas.", "Ang tubig na sinayang ngayon ay kulang bukas."));

            Add(Difficulty.ElNino, FamilyType.Bantay, "Si Bantay, nakahiga sa mainit na semento, hinihingal at nakalabas ang dila.",
                new HandaCard("Ilipat sa lilim at bigyan ng malinis na tubig", "Basain din ang paa niya.", "Kumawag ang buntot ni Bantay. Good boy!", "Pati alaga ay nae-heat stroke. Bigyan ng lilim at tubig."),
                new PwedeNaCard("Buhusan ng isang baldeng yelo", "Para mabilis lumamig!", "Nagulat si Bantay at nanginig.", "Unti-unti ang pagpapalamig. Hindi maganda ang biglang yelo."),
                new SablayCard("Isama sa jogging para 'di tamad", "Exercise is life.", "Ikaw ang nagbuhat kay Bantay pauwi.", "Huwag pagurin ang alaga sa matinding init."));
        }

        private void Add(Difficulty d, FamilyType who, string text, Card handa, Card pwede, Card sablay)
        {
            scenarios.Add(new Scenario(d, who, text, handa, pwede, sablay));
        }

        // Kinukuha ang scenario at hinahalo ang cards para hindi laging nasa iisang pwesto ang sagot
        public Scenario Draw(Difficulty d, FamilyType who)
        {
            Scenario s = scenarios.First(x => x.Disaster == d && x.Who == who);
            Card[] shuffled = s.Cards.OrderBy(c => rng.Next()).ToArray();
            return new Scenario(s.Disaster, s.Who, s.Text, shuffled);
        }

        // index -1 = naubos ang oras
        public CardResult CheckAnswer(Scenario s, int index)
        {
            if (index < 0 || index >= s.Cards.Length) return CardResult.Sablay;
            return s.Cards[index].Result;
        }

        public void RecordMiss(string lesson)
        {
            if (!MissedLessons.Contains(lesson)) MissedLessons.Add(lesson);
        }

        public static string GetRank(int stars)
        {
            if (stars == 3) return "BARANGAY HERO";
            if (stars == 2) return "HANDA CAPTAIN";
            if (stars == 1) return "MEDYO HANDA";
            return "PRACTICE PA";
        }

        public string GetReport(Difficulty d, int stars)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RANGGO: " + GetRank(stars));
            sb.AppendLine();
            if (d == Difficulty.Tsunami) sb.AppendLine("Kapag umatras ang dagat, umakyat agad sa mataas na lugar.");
            else if (d == Difficulty.Lindol) sb.AppendLine("Drop, Cover, Hold habang umuuga. Asahan ang aftershocks.");
            else sb.AppendLine("Tuwing El Niño: magtipid ng tubig at iwasan ang tirik na araw.");
            sb.AppendLine();
            if (MissedLessons.Count == 0) sb.AppendLine("Perfect! Wala kang sablay.");
            else sb.AppendLine("MGA NATUTUNAN MO:");
            foreach (string lesson in MissedLessons) sb.AppendLine("  • " + lesson);
            return sb.ToString();
        }

        // Mapa: . sahig   # bahay   ^ mataas na lugar   E evacuation   P player   L B D pamilya
        public static string[] GetMap(Difficulty d)
        {
            if (d == Difficulty.Tsunami) return new[] {
                "P...#..^^..E", "..^^#.....^^", ".......##...", "L...^^......",
                "..#.^^..#..B", "..#.....#...", "...^^.......", "#..^^..##.^^",
                "....D.....^^", ".##.........", "....^^.#....", ".^^.^^.#..^^" };

            if (d == Difficulty.Lindol) return new[] {
                "P....#.....E", ".##..#..##..", ".#......#...", "....L.......",
                "###....###..", "......#...B.", ".#..#.#.....", ".#..#...##..",
                "....##......", "..D......#..", ".##......#..", "............" };

            return new[] {
                "...#....##.P", ".L.#........", "...#..##....", "......#...B.",
                ".##.......#.", "..#..##...#.", "..#...#.....", "......#.##..",
                ".D..........", "...##....##.", ".#.....#....", "E#.....#...." };
        }
    }
}
