using System;
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

        // /////////////////////////// METHODS /////////////////////////// //

        /// <summary>Initialization of UI and setup of default preferenses values</summary>
        public void Init()
        {
            AddUIElements();
            InitPreferences();
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

            Config.MinStarCount = 16;
            Config.MaxStarCount = 96;
            Config.DefaultStarCount = preferences.GetInt("defaultStarCount", 64); ;
        }

        /// <summary>Export of generator's preferences</summary>
        /// <returns>Generator's preferences</returns>
        public GSGenPreferences Export()
        {
            return preferences;
        }

        /// <summary>Generating initial preferences for the generator</summary>
        private void InitPreferences()
        {
            preferences.Set("galaxyDensity", 5);
            preferences.Set("defaultStarCount", 64);
            preferences.Set("startingSystemType", "Random");
            preferences.Set("birthPlanetSize", 200);
            preferences.Set("birthPlanetUnlock", false);
            preferences.Set("birthPlanetSiTi", false);
            preferences.Set("hugeGasGiants", false);
            preferences.Set("moreLikelyGasGiantMoons", false);
            preferences.Set("moonsAreSmall", true);
            preferences.Set("smallGasGiantMoons", false);
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
            List<string> moonsAreSmallOptions = new List<string>(){ "Disabled", "For Telluric Planets Only", "All Moons Are Small" };
            List<string> starTypeOptions = new List<string>() { "Random", "M-class", "K-class", "G-class", "F-class", "A-class",
                                                                "B-class", "O-class", "Red Giant", "Yellow Giant", "White Giant", "Blue Giant" };

            UI.Add("galaxyDensity", Options.Add(GSUI.Slider("Galaxy Density", 1, 5, 9, "galaxyDensity")));
            UI.Add("defaultStarCount", Options.Add(GSUI.Slider("Default StarCount", 16, 64, 96, "defaultStarCount", DefaultStarCountCallback)));

            UI.Add("startingSystemType", Options.Add(GSUI.Combobox("Starting System Star Type", starTypeOptions, StartingStarTypeCallback, InitializeStartingStarTypeComboBox)));
            UI.Add("birthPlanetSize", Options.Add(GSUI.PlanetSizeSlider("Starting Planet Size", 100, 200, 400, "birthPlanetSize")));
            UI.Add("birthPlanetUnlock", Options.Add(GSUI.Checkbox("Starting Planet Unlock", false, "birthPlanetUnlock")));
            UI.Add("birthPlanetSiTi", Options.Add(GSUI.Checkbox("Starting Planet Si/Ti", false, "birthPlanetSiTi")));

            Options.Add(GSUI.Spacer());
            Options.Add(GSUI.Separator());
            Options.Add(GSUI.Spacer());

            UI.Add("hugeGasGiants", Options.Add(GSUI.Checkbox("Variable Size Gas Giants", false, "hugeGasGiants")));
            UI.Add("moreLikelyGasGiantMoons", Options.Add(GSUI.Checkbox("Incread Chance of Gas Giant Moons", false, "moreLikelyGasGiantMoons")));
            UI.Add("moonsAreSmall", Options.Add(GSUI.Combobox("Moons Are Small", moonsAreSmallOptions, SmallMoonsCallback, InitializeSmallMoonsComboBox)));
            UI.Add("tidalLockInnerPlanets", Options.Add(GSUI.Checkbox("Tidal Lock Inner Planets", false, "tidalLockInnerPlanets")));
            UI.Add("luminosityBoost", Options.Add(GSUI.Checkbox("Boost Luminosity of Blue Stars", false, "luminosityBoost")));

            Options.Add(GSUI.Spacer());
            Options.Add(GSUI.Separator());
            Options.Add(GSUI.Spacer());

            // star types frequency settings group
            var starFreqOptions = new GSOptions();
            UI.Add("freqK", starFreqOptions.Add(GSUI.Slider("Freq. Type K", 0, 40, 100, "freqK")));
            UI.Add("freqM", starFreqOptions.Add(GSUI.Slider("Freq. Type M", 0, 50, 100, "freqM")));
            UI.Add("freqG", starFreqOptions.Add(GSUI.Slider("Freq. Type G", 0, 30, 100, "freqG")));
            UI.Add("freqF", starFreqOptions.Add(GSUI.Slider("Freq. Type F", 0, 25, 100, "freqF")));
            UI.Add("freqA", starFreqOptions.Add(GSUI.Slider("Freq. Type A", 0, 10, 100, "freqA")));
            UI.Add("freqB", starFreqOptions.Add(GSUI.Slider("Freq. Type B", 0, 4, 100, "freqB")));
            UI.Add("freqO", starFreqOptions.Add(GSUI.Slider("Freq. Type O", 0, 2, 100, "freqO")));
            UI.Add("freqBH", starFreqOptions.Add(GSUI.Slider("Freq. BlackHole", 0, 1, 100, "freqBH")));
            UI.Add("freqN", starFreqOptions.Add(GSUI.Slider("Freq. Neutron", 0, 1, 100, "freqN")));
            UI.Add("freqW", starFreqOptions.Add(GSUI.Slider("Freq. WhiteDwarf", 0, 2, 100, "freqW")));
            UI.Add("freqRG", starFreqOptions.Add(GSUI.Slider("Freq. Red Giant", 0, 1, 100, "freqRG")));
            UI.Add("freqYG", starFreqOptions.Add(GSUI.Slider("Freq. Yellow Giant", 0, 1, 100, "freqYG")));
            UI.Add("freqWG", starFreqOptions.Add(GSUI.Slider("Freq. White Giant", 0, 1, 100, "freqWG")));
            UI.Add("freqBG", starFreqOptions.Add(GSUI.Slider("Freq. Blue Giant", 0, 1, 100, "freqBG")));
            Options.Add(GSUI.Group("Star Types Frequencies", starFreqOptions, "Settings that determine frequencies of various star types in the cluster"));

            Options.Add(GSUI.Spacer());
            Options.Add(GSUI.Separator());
            Options.Add(GSUI.Spacer());

            UI.Add("minPlanetCount", Options.Add(GSUI.Slider("Min Planets/System", 1, 4, 10, "minPlanetCount", MinPlanetCountCallback)));
            UI.Add("maxPlanetCount", Options.Add(GSUI.Slider("Max Planets/System", 1, 6, 10, "maxPlanetCount", MaxPlanetCountCallback)));
            UI.Add("countBias", Options.Add(GSUI.Slider("Planet Count Bias", 0, 50, 100, "sizeBias", CountBiasCallback)));
            UI.Add("minPlanetSize", Options.Add(GSUI.PlanetSizeSlider("Min planet size", 100, 200, 400, "minPlanetSize", MinPlanetSizeCallback)));
            UI.Add("maxPlanetSize", Options.Add(GSUI.PlanetSizeSlider("Max planet size", 100, 200, 400, "maxPlanetSize", MaxPlanetSizeCallback)));
            UI.Add("sizeBias", Options.Add(GSUI.Slider("Planet Size Bias", 0, 50, 100, "sizeBias", PlanetSizeBiasCallback)));

            UI.Add("chanceGas", Options.Add(GSUI.Slider("Chance Gas", 10, 20, 50, "chanceGas", GasChanceCallback)));
            UI.Add("chanceMoon", Options.Add(GSUI.Slider("Chance Moon", 10, 20, 80, "chanceMoon", MoonChanceCallback)));
        }

        /// <summary>Callback for setting default star count for the cluster</summary>
        /// <param name="o">Default star count</param>
        private void DefaultStarCountCallback(Val o)
        {
            Config.DefaultStarCount = preferences.GetInt("defaultStarCount", 64);
        }

        /// <summary>Initializer for small moons combo box</summary>
        private void InitializeStartingStarTypeComboBox()
        {
            string startingStarType = preferences.Get("startingSystemType");
            if (startingStarType == "Random") { UI["startingSystemType"].Set(0); }
            else if (startingStarType == "M") { UI["startingSystemType"].Set(1); }
            else if (startingStarType == "K") { UI["startingSystemType"].Set(2); }
            else if (startingStarType == "G") { UI["startingSystemType"].Set(3); }
            else if (startingStarType == "F") { UI["startingSystemType"].Set(4); }
            else if (startingStarType == "A") { UI["startingSystemType"].Set(5); }
            else if (startingStarType == "B") { UI["startingSystemType"].Set(6); }
            else if (startingStarType == "O") { UI["startingSystemType"].Set(7); }
            else if (startingStarType == "RedGiant") { UI["startingSystemType"].Set(8); }
            else if (startingStarType == "YellowGiant") { UI["startingSystemType"].Set(9); }
            else if (startingStarType == "WhiteGiant") { UI["startingSystemType"].Set(10); }
            else if (startingStarType == "BlueGiant") { UI["startingSystemType"].Set(11); }
            else { UI["startingSystemType"].Set(3); }

        }

        /// <summary>Callback for changing starting star type</summary>
        /// <param name="o">Starting star type value</param>
        private void StartingStarTypeCallback(Val o)
        {
            int val = o;
            switch (val)
            {
                case 0: preferences.Set("startingSystemType", "Random"); break;
                case 1: preferences.Set("startingSystemType", "M"); break;
                case 2: preferences.Set("startingSystemType", "K"); break;
                case 3: preferences.Set("startingSystemType", "G"); break;
                case 4: preferences.Set("startingSystemType", "F"); break;
                case 5: preferences.Set("startingSystemType", "A"); break;
                case 6: preferences.Set("startingSystemType", "B"); break;
                case 7: preferences.Set("startingSystemType", "O"); break;
                case 8: preferences.Set("startingSystemType", "RedGiant"); break;
                case 9: preferences.Set("startingSystemType", "YellowGiant"); break;
                case 10: preferences.Set("startingSystemType", "WhiteGiant"); break;
                case 11: preferences.Set("startingSystemType", "BlueGiant"); break;
                default: preferences.Set("startingSystemType", "G"); break;
            }
        }

        /// <summary>Initializer for small moons combo box</summary>
        private void InitializeSmallMoonsComboBox()
        {
            bool bMoonsAreSmall = preferences.GetBool("moonsAreSmall", false);
            bool bGasGiantMoonsAreSmall = preferences.GetBool("smallGasGiantMoons", false);

            if (!bMoonsAreSmall)
            {
                UI["moonsAreSmall"].Set(0); // disabled
            }
            else if (!bGasGiantMoonsAreSmall)
            {
                UI["moonsAreSmall"].Set(1); // only terrestrial planets' moons are small
            }
            else
            {
                UI["moonsAreSmall"].Set(2); // all moons are small
            }
        }

        /// <summary>Callback for small moons property</summary>
        /// <param name="o">Small moons flag</param>
        private void SmallMoonsCallback(Val o)
        {
            int val = o;
            switch (val)
            {
                case 0: // disabled
                    {
                        preferences.Set("moonsAreSmall", false);
                        preferences.Set("smallGasGiantMoons", false);
                        break;
                    }
                case 1: // only terrestrial planets' moons are small
                    {
                        preferences.Set("moonsAreSmall", true);
                        preferences.Set("smallGasGiantMoons", false);
                        break;
                    }
                case 2: // all moons are small
                    {
                        preferences.Set("moonsAreSmall", true);
                        preferences.Set("smallGasGiantMoons", true);
                        break;
                    }
                default:
                    {
                        preferences.Set("moonsAreSmall", false);
                        preferences.Set("smallGasGiantMoons", false);
                        break;
                    }
            }
        }

        /// <summary>Callback for setting the chance for a planet to be a moon</summary>
        /// <param name="o">Moon chance</param>
        private void MoonChanceCallback(Val o)
        {
            //SetAllStarTypeOptions("chanceMoon", o);
            preferences.Set("chanceMoon", o);
        }

        /// <summary>Callback for setting the chance for a planet to be a gas giant</summary>
        /// <param name="o">Gas giant chance</param>
        private void GasChanceCallback(Val o)
        {
            preferences.Set("chanceGas", o);
        }

        /// <summary>Callback for setting min planet count</summary>
        /// <param name="o">Min planet count value</param>
        private void MinPlanetCountCallback(Val o)
        {
            var maxCount = preferences.GetInt("maxPlanetCount");
            if (maxCount == -1f) maxCount = 10;
            if (maxCount < o)
            {
                preferences.Set("minPlanetCount", maxCount);
            }
        }

        /// <summary>Callback for setting max planet count</summary>
        /// <param name="o">Max planet count value</param>
        private void MaxPlanetCountCallback(Val o)
        {
            var minCount = preferences.GetInt("minPlanetCount");
            if (minCount == -1f) minCount = 1;
            if (minCount > o)
            {
                preferences.Set("maxPlanetCount", minCount);
            }
        }

        /// <summary>Callback for setting planet count bias</summary>
        /// <param name="o">Planet count bias</param>
        private void CountBiasCallback(Val o)
        {
            preferences.Set("countBias", o);
        }

        /// <summary>Callback for setting min planet size</summary>
        /// <param name="o">Min planet size value</param>
        private void MinPlanetSizeCallback(Val o)
        {
            preferences.Set("minPlanetSize", Utils.ParsePlanetSize(o));
            var maxSize = preferences.GetFloat("maxPlanetSize", 200);
            if (maxSize < o)
            {
                UI["maxPlanetSize"].Set(o);
            }
        }

        /// <summary>Callback for setting max planet size</summary>
        /// <param name="o">Max planet size value</param>
        private void MaxPlanetSizeCallback(Val o)
        {
            preferences.Set("maxPlanetSize", Utils.ParsePlanetSize(o));
            var minSize = preferences.GetFloat("minPlanetSize", 200);
            if (minSize > o)
            {
                UI["minPlanetSize"].Set(o);
            }
        }

        /// <summary>Callback for planet size bias</summary>
        /// <param name="o">Planet size bias</param>
        private void PlanetSizeBiasCallback(Val o)
        {
            preferences.Set("sizeBias", o);
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
        private int GetPlanetSizeBias()
        {
            return preferences.GetInt("sizeBias", 50);
        }

        /// <summary>Method for getting a bias for star's planetary objects count</summary>
        /// <returns>Bias for planet count</returns>
        private int GetPlanetCountBias()
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