using System.Collections.Generic;
using System.Linq;

namespace GalacticScale.Generators
{
    public partial class VanillaPlusPlusGenerator : iConfigurableGenerator
    {
        private readonly GSGenPreferences preferences = new GSGenPreferences();
        private Dictionary<string, double> starFreq = new Dictionary<string, double>();
        private readonly Dictionary<string, GSOptionCallback> typeCallbacks = new Dictionary<string, GSOptionCallback>();

        // star types descriptions and letter identifiers
        private readonly string[] typeDesc = { "Type K", "Type M", "Type F", "Type G", "Type A", "Type B", "Type O", "White Dwarf", "Red Giant", "Yellow Giant", "White Giant", "Blue Giant", "Neutron Star", "Black Hole" };
        private readonly string[] typeLetter = {"K", "M", "F", "G", "A", "B", "O", "WD", "RG", "YG", "WG", "BG", "NS", "BH"};

        public bool DisableStarCountSlider => false;
        public Dictionary<string, GSUI> UI = new Dictionary<string, GSUI>();

        public GSGeneratorConfig Config { get; } = new GSGeneratorConfig();
        public GSOptions Options { get; } = new GSOptions();

        /// <summary>Initialization of UI and setup of default preferenses values</summary>
        public void Init()
        {
            AddUIElements();
            InitPreferences();
            Config.MinStarCount = 16;
            Config.MaxStarCount = 96;
        }

        /// <summary>Import of generator's preferences</summary>
        /// <param name="preferences">Generator's preferences</param>
        public void Import(GSGenPreferences preferences)
        {
            for (var i = 0; i < preferences.Count; i++)
            {
                var key = preferences.Keys.ElementAt(i);
                this.preferences.Set(key, preferences[key]);
            }

            Config.DefaultStarCount = preferences.GetInt("defaultStarCount", 64); ;
        }

        /// <summary>Export of generator's preferences</summary>
        /// <returns>Generator's preferences</returns>
        public GSGenPreferences Export()
        {
            return preferences;
        }

        private void DefaultStarCountCallback(Val o)
        {
            Config.DefaultStarCount = preferences.GetInt("defaultStarCount", 64);
        }

        /// <summary>Generating initial preferences for the generator</summary>
        private void InitPreferences()
        {
            preferences.Set("galaxyDensity", 5);
            preferences.Set("defaultStarCount", 64);
            preferences.Set("birthPlanetSize", 200);
            preferences.Set("birthPlanetUnlock", false);
            preferences.Set("birthPlanetSiTi", false);
            preferences.Set("moonsAreSmall", true);
            preferences.Set("tidalLockInnerPlanets", false);
            preferences.Set("luminosityBoost", false);
            preferences.Set("minPlanetCount", 1);
            preferences.Set("maxPlanetCount", 6);
            preferences.Set("minPlanetSize", 200);
            preferences.Set("maxPlanetSize", 200);
            preferences.Set("sizeBias", 50);
            preferences.Set("countBias", 50);
            preferences.Set("freqK", 40);
            preferences.Set("freqM", 50);
            preferences.Set("freqG", 30);
            preferences.Set("freqF", 25);
            preferences.Set("freqA", 10);
            preferences.Set("freqB", 4);
            preferences.Set("freqO", 2);
            preferences.Set("freqBH", 1);
            preferences.Set("freqN", 1);
            preferences.Set("freqW", 2);
            preferences.Set("freqRG", 1);
            preferences.Set("freqYG", 1);
            preferences.Set("freqWG", 1);
            preferences.Set("freqBG", 1);
        }

        /// <summary>Generate UI elements</summary>
        private void AddUIElements()
        {
            UI.Add("galaxyDensity", Options.Add(GSUI.Slider("Galaxy Density", 1, 5, 9, "galaxyDensity")));
            UI.Add("defaultStarCount", Options.Add(GSUI.Slider("Default StarCount", 16, 64, 96, "defaultStarCount", DefaultStarCountCallback)));
            UI.Add("birthPlanetSize", Options.Add(GSUI.PlanetSizeSlider("Starting Planet Size", 100, 200, 400, "birthPlanetSize")));
            UI.Add("birthPlanetUnlock", Options.Add(GSUI.Checkbox("Starting Planet Unlock", false, "birthPlanetUnlock")));
            UI.Add("birthPlanetSiTi", Options.Add(GSUI.Checkbox("Starting planet Si/Ti", false, "birthPlanetSiTi")));
            UI.Add("moonsAreSmall", Options.Add(GSUI.Checkbox("Moons Are Small", true, "moonsAreSmall")));
            UI.Add("tidalLockInnerPlanets", Options.Add(GSUI.Checkbox("Tidal Lock Inner Planets", false, "tidalLockInnerPlanets")));
            UI.Add("luminosityBoost", Options.Add(GSUI.Checkbox("Boost Luminosity of Blue Stars", false, "luminosityBoost")));

            UI.Add("freqK", Options.Add(GSUI.Slider("Freq. Type K", 0, 40, 100, "freqK")));
            UI.Add("freqM", Options.Add(GSUI.Slider("Freq. Type M", 0, 50, 100, "freqM")));
            UI.Add("freqG", Options.Add(GSUI.Slider("Freq. Type G", 0, 30, 100, "freqG")));
            UI.Add("freqF", Options.Add(GSUI.Slider("Freq. Type F", 0, 25, 100, "freqF")));
            UI.Add("freqA", Options.Add(GSUI.Slider("Freq. Type A", 0, 10, 100, "freqA")));
            UI.Add("freqB", Options.Add(GSUI.Slider("Freq. Type B", 0, 4, 100, "freqB")));
            UI.Add("freqO", Options.Add(GSUI.Slider("Freq. Type O", 0, 2, 100, "freqO")));
            UI.Add("freqBH", Options.Add(GSUI.Slider("Freq. BlackHole", 0, 1, 100, "freqBH")));
            UI.Add("freqN", Options.Add(GSUI.Slider("Freq. Neutron", 0, 1, 100, "freqN")));
            UI.Add("freqW", Options.Add(GSUI.Slider("Freq. WhiteDwarf", 0, 2, 100, "freqW")));
            UI.Add("freqRG", Options.Add(GSUI.Slider("Freq. Red Giant", 0, 1, 100, "freqRG")));
            UI.Add("freqYG", Options.Add(GSUI.Slider("Freq. Yellow Giant", 0, 1, 100, "freqYG")));
            UI.Add("freqWG", Options.Add(GSUI.Slider("Freq. White Giant", 0, 1, 100, "freqWG")));
            UI.Add("freqBG", Options.Add(GSUI.Slider("Freq. Blue Giant", 0, 1, 100, "freqBG")));

            UI.Add("minPlanetCount", Options.Add(GSUI.Slider("Min Planets/System", 1, 4, 10, "minPlanetCount", MinPlanetCountCallback)));
            UI.Add("maxPlanetCount", Options.Add(GSUI.Slider("Max Planets/System", 1, 6, 10, "maxPlanetCount", MaxPlanetCountCallback)));
            UI.Add("countBias", Options.Add(GSUI.Slider("Planet Count Bias", 0, 50, 100, "sizeBias", CountBiasCallback)));
            UI.Add("minPlanetSize", Options.Add(GSUI.PlanetSizeSlider("Min planet size", 50, 200, 200, "minPlanetSize", MinPlanetSizeCallback)));
            UI.Add("maxPlanetSize", Options.Add(GSUI.PlanetSizeSlider("Max planet size", 200, 400, 400, "maxPlanetSize", MaxPlanetSizeCallback)));
            UI.Add("sizeBias", Options.Add(GSUI.Slider("Planet Size Bias", 0, 50, 100, "sizeBias", SizeBiasCallback)));

            UI.Add("chanceGas", Options.Add(GSUI.Slider("Chance Gas", 10, 20, 50, "chanceGas", GasChanceCallback)));
            UI.Add("chanceMoon", Options.Add(GSUI.Slider("Chance Moon", 10, 20, 80, "chanceMoon", MoonChanceCallback)));
        }

        private void SizeBiasCallback(Val o)
        {
            //SetAllStarTypeOptions("sizeBias", o);
            preferences.Set("sizeBias", o);
        }

        private void CountBiasCallback(Val o)
        {
            //SetAllStarTypeOptions("countBias", o);
            preferences.Set("countBias", o);
        }

        private void MoonChanceCallback(Val o)
        {
            //SetAllStarTypeOptions("chanceMoon", o);
            preferences.Set("chanceMoon", o);
        }

        private void GasChanceCallback(Val o)
        {
            //SetAllStarTypeOptions("chanceGas", o);
            preferences.Set("chanceGas", o);
        }

        private void MinPlanetCountCallback(Val o)
        {
            var maxCount = preferences.GetInt("maxPlanetCount");
            if (maxCount == -1f) maxCount = 10;
            if (maxCount < o)
            {
                //GS2.Warn("<");
                //o = maxCount;
                preferences.Set("minPlanetCount", maxCount);
                //UI["minPlanetCount"].Set(o);
            }

            //SetAllStarTypeOptions("minPlanetCount", o);
        }

        private void MaxPlanetCountCallback(Val o)
        {
            var minCount = preferences.GetInt("minPlanetCount");
            if (minCount == -1f) minCount = 1;
            if (minCount > o)
            {
                //GS2.Warn(">");
                //o = minCount;
                preferences.Set("maxPlanetCount", minCount);
                //UI["maxPlanetCount"].Set(o);
            }

            //SetAllStarTypeOptions("maxPlanetCount", o);
        }

        private void MinPlanetSizeCallback(Val o)
        {
            var maxSize = preferences.GetFloat("maxPlanetSize");
            if (maxSize == -1f) maxSize = 400;
            if (maxSize < o) o = maxSize;
            //if (preferences.GetBool("safeMode")) preferences.Set("minPlanetSize", SafePlanetSize(o));
            //else
                preferences.Set("minPlanetSize", Utils.ParsePlanetSize(o));
            UI["minPlanetSize"].Set(preferences.GetFloat("minPlanetSize"));
            //SetAllStarTypeMinSize(o);
        }

        private void MaxPlanetSizeCallback(Val o)
        {
            var minSize = preferences.GetFloat("minPlanetSize");
            if (minSize == -1f) minSize = 50;
            if (minSize > o) o = minSize;
            //if (preferences.GetBool("safeMode")) preferences.Set("maxPlanetSize", SafePlanetSize(o));
            //else
                preferences.Set("maxPlanetSize", Utils.ParsePlanetSize(o));
            UI["maxPlanetSize"].Set(preferences.GetFloat("maxPlanetSize"));
            //SetAllStarTypeMaxSize(o);
        }

        /// <summary>Generation for star frequences in the cluster</summary>
        /// <returns>dictionary "Type ID"-"Frequency"</returns>
        private Dictionary<string, double> CalculateFrequencies()
        {
            var StarFreqTupleArray = new (string type, double chance)[14];
            var fK = preferences.GetDouble("freqK", 40);
            var fM = preferences.GetDouble("freqM", 50);
            var fG = preferences.GetDouble("freqG", 30);
            var fF = preferences.GetDouble("freqF", 25);
            var fA = preferences.GetDouble("freqA", 10);
            var fB = preferences.GetDouble("freqB", 4);
            var fO = preferences.GetDouble("freqO", 2);
            var fBH = preferences.GetDouble("freqBH", 1);
            var fN = preferences.GetDouble("freqN", 1);
            var fW = preferences.GetDouble("freqW", 2);
            var fRG = preferences.GetDouble("freqRG", 1);
            var fYG = preferences.GetDouble("freqYG", 1);
            var fWG = preferences.GetDouble("freqWG", 1);
            var fBG = preferences.GetDouble("freqBG", 1);
            var total = fK + fM + fG + fF + fA + fB + fO + fBH + fN + fW + fRG + fYG + fWG + fBG;

            StarFreqTupleArray[0] = ("K", fK / total);
            StarFreqTupleArray[1] = ("M", fM / total);
            StarFreqTupleArray[2] = ("G", fG / total);
            StarFreqTupleArray[3] = ("F", fF / total);
            StarFreqTupleArray[4] = ("A", fA / total);
            StarFreqTupleArray[5] = ("B", fB / total);
            StarFreqTupleArray[6] = ("O", fO / total);
            StarFreqTupleArray[7] = ("BH", fBH / total);
            StarFreqTupleArray[8] = ("N", fN / total);
            StarFreqTupleArray[9] = ("W", fW / total);
            StarFreqTupleArray[10] = ("RG", fRG / total);
            StarFreqTupleArray[11] = ("YG", fYG / total);
            StarFreqTupleArray[12] = ("WG", fWG / total);
            StarFreqTupleArray[13] = ("BG", fBG / total);

            starFreq = new Dictionary<string, double>();
            starFreq.Add("K", fK / total);
            for (var i = 1; i < StarFreqTupleArray.Length; i++)
            {
                var element = StarFreqTupleArray[i];
                var previousElement = StarFreqTupleArray[i - 1];
                starFreq.Add(element.type, element.chance + previousElement.chance);
                StarFreqTupleArray[i].chance += previousElement.chance;
            }

            return starFreq;
        }

        /// <summary>Method for randomly chosing star type</summary>
        /// <returns>pair "Star type"-"Star spectral class"</returns>
        private (EStarType type, ESpectrType spectr) ChooseStarType()
        {
            var choice = random.NextDouble();
            var starType = "";
            for (var i = 0; i < starFreq.Count; i++)
                if (choice < starFreq.ElementAt(i).Value)
                {
                    starType = starFreq.ElementAt(i).Key;
                    break;
                }

            switch (starType)
            {
                case "K": return (EStarType.MainSeqStar, ESpectrType.K);
                case "M": return (EStarType.MainSeqStar, ESpectrType.M);
                case "G": return (EStarType.MainSeqStar, ESpectrType.G);
                case "F": return (EStarType.MainSeqStar, ESpectrType.F);
                case "A": return (EStarType.MainSeqStar, ESpectrType.A);
                case "B": return (EStarType.MainSeqStar, ESpectrType.B);
                case "O": return (EStarType.MainSeqStar, ESpectrType.O);
                case "BH": return (EStarType.BlackHole, ESpectrType.X);
                case "N": return (EStarType.NeutronStar, ESpectrType.X);
                case "W": return (EStarType.WhiteDwarf, ESpectrType.X);
                case "RG": return (EStarType.GiantStar, ESpectrType.M);
                case "YG": return (EStarType.GiantStar, ESpectrType.G);
                case "WG": return (EStarType.GiantStar, ESpectrType.A);
                default: return (EStarType.GiantStar, ESpectrType.B);
            }
        }

        /// <summary>Method for getting the maximal number of planets a star can have</summary>
        /// <returns>Maximal number of planets a star can have</returns>
        private int GetMaxPlanetCount()
        {
            return preferences.GetInt("maxPlanetCount");
        }

        /// <summary>Method for getting the minimal number of planets a star can have</summary>
        /// <returns>Minimal number of planets a star can have</returns>
        private int GetMinPlanetCount()
        {
            return preferences.GetInt("minPlanetCount");
        }

        /// <summary>Method for getting the maximal size a planet can have</summary>
        /// <returns>Maximal size for a planet</returns>
        private int GetMaxPlanetSize()
        {
            return preferences.GetInt("maxPlanetSize");
        }

        /// <summary>Method for getting the minimal size a planet can have</summary>
        /// <returns>Minimal size for a planet</returns>
        private int GetMinPlanetSize()
        {
            return preferences.GetInt("minPlanetSize");
        }

        /// <summary>Method for getting a bias for planet size</summary>
        /// <returns></returns>
        private int GetSizeBias()
        {
            return preferences.GetInt("sizeBias", 50);
        }

        /// <summary>Method for getting a bias for star's planetary objects count</summary>
        /// <returns>Bias for planet count</returns>
        private int GetCountBias()
        {
            return preferences.GetInt("countBias", 50);
        }

        /// <summary>Method for getting a chance of a planet orbiting a star being a moon</summary>
        /// <returns>Chance of a planet being a moon</returns>
        private double GetMoonChance()
        {
            return preferences.GetInt("chanceMoon", 20) / 100.0;
        }

        /// <summary>Method for getting a chance of a planet orbiting a star being a gas giant</summary>
        /// <returns>Chance of a planet being a gas giant</returns>
        private double GetGasChanceGiant()
        {
            return preferences.GetInt("chanceGas", 20) / 100.0;
        }
    }
}