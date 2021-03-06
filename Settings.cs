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

            Config.enableStarSelector = false;
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
            Config.DefaultStarCount = preferences.GetInt("defaultStarCount", 64);
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
            preferences.Set("binaryStarChance", 25);
            preferences.Set("startingSystemType", "Random");
            preferences.Set("birthPlanetSize", 200);
            preferences.Set("birthPlanetUnlock", false);
            preferences.Set("birthPlanetSiTi", false);
            preferences.Set("noHomeworldRares", true);
            preferences.Set("hugeGasGiants", false);
            preferences.Set("moreLikelyGasGiantMoons", false);
            preferences.Set("moonsAreSmall", true);
            preferences.Set("smallGasGiantMoons", false);
            preferences.Set("tidalLockInnerPlanets", false);
            preferences.Set("luminosityBoost", false);
            preferences.Set("luminosityExponentialBoost", false);
            preferences.Set("realisticSolarPowerLevels", false);
            preferences.Set($"planetCount", new FloatPair(1, 6));
            preferences.Set($"planetSize", new FloatPair(200, 200));
            preferences.Set("sizeBias", 50);
            preferences.Set("countBias", 50);
            preferences.Set("freqM", 50);
            preferences.Set("freqK", 40);
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
            preferences.Set("chanceGas", 25);
            preferences.Set("chanceMoon", 25);
            preferences.Set("rareChance", 15);

            preferences.Set("linearBoostCoefficient", 2f);
            preferences.Set("exponenitalBoostCoefficient", 2f);

            preferences.Set("dreamSystem", false);
        }

        /// <summary>Generate UI elements</summary>
        private void AddUIElements()
        {
            List<string> lumBoostOptions = new List<string>() { "None", "Linear", "Exponential" };
            List<string> moonsAreSmallOptions = new List<string>(){ "Disabled", "For Telluric Planets Only", "All Moons Are Small" };
            List<string> starTypeOptions = new List<string>() { "Random", "M-class", "K-class", "G-class", "F-class", "A-class",
                                                                "B-class", "O-class", "Red Giant", "Yellow Giant", "White Giant", "Blue Giant" };

            UI.Add("galaxyDensity", Options.Add(GSUI.Slider("Galaxy Density", 1, 5, 9, "galaxyDensity")));
            UI.Add("defaultStarCount", Options.Add(GSUI.Slider("Default StarCount", 16, 64, 96, "defaultStarCount", DefaultStarCountCallback)));
            UI.Add("binaryStarChance", Options.Add(GSUI.Slider("Binary and Trinary Star Chance", 0, 25, 50, "binaryStarChance")));

            Options.Add(GSUI.Spacer());
            Options.Add(GSUI.Separator());
            Options.Add(GSUI.Spacer());

            UI.Add("startingSystemType", Options.Add(GSUI.Combobox("Starting System Star Type", starTypeOptions, StartingStarTypeCallback, InitializeStartingStarTypeComboBox)));
            UI.Add("birthPlanetSize", Options.Add(GSUI.PlanetSizeSlider("Starting Planet Size", 100, 200, 400, "birthPlanetSize")));
            UI.Add("birthPlanetUnlock", Options.Add(GSUI.Checkbox("Starting Planet Unlock", false, "birthPlanetUnlock")));
            UI.Add("birthPlanetSiTi", Options.Add(GSUI.Checkbox("Starting Planet Si/Ti", false, "birthPlanetSiTi")));
            UI.Add("noHomeworldRares", Options.Add(GSUI.Checkbox("Disable Rares on Starting Planet", true, "noHomeworldRares")));

            Options.Add(GSUI.Spacer());
            Options.Add(GSUI.Separator());
            Options.Add(GSUI.Spacer());

            UI.Add("hugeGasGiants", Options.Add(GSUI.Checkbox("Variable Size Gas Giants", false, "hugeGasGiants")));
            UI.Add("moreLikelyGasGiantMoons", Options.Add(GSUI.Checkbox("Higher Chance of Gas Giant Moons", false, "moreLikelyGasGiantMoons")));
            UI.Add("moonsAreSmall", Options.Add(GSUI.Combobox("Moons Are Small", moonsAreSmallOptions, SmallMoonsCallback, InitializeSmallMoonsComboBox)));
            UI.Add("tidalLockInnerPlanets", Options.Add(GSUI.Checkbox("Tidal Lock Inner Planets", false, "tidalLockInnerPlanets")));
 
            UI.Add("luminosityBoost", Options.Add(GSUI.Combobox("Blue Stars Luminosity Boost", lumBoostOptions, LuminosityBoostCallback, InitializeLuminosityBoostCombobox)));
            UI.Add("linearBoostCoeff", Options.Add(GSUI.Slider("Coefficient", 1f, 2f, 5f, 0.25f, "linearBoostCoefficient")));
            UI.Add("exponentialBoostCoeff", Options.Add(GSUI.Slider("Coefficient", 1f, 2f, 3f, 0.25f, "exponenitalBoostCoefficient")));

            UI.Add("solarPowerLevels", Options.Add(GSUI.Checkbox("Realistic Solar Power Levels", false, "realisticSolarPowerLevels")));

            Options.Add(GSUI.Spacer());
            Options.Add(GSUI.Separator());
            Options.Add(GSUI.Spacer());

            // star types frequency settings group
            var starFreqOptions = new GSOptions();
            UI.Add("freqM", starFreqOptions.Add(GSUI.Slider("Freq. Type M", 0, 50, 100, "freqM")));
            UI.Add("freqK", starFreqOptions.Add(GSUI.Slider("Freq. Type K", 0, 40, 100, "freqK")));
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

            UI.Add("planetCount", Options.Add(GSUI.RangeSlider("Number of Planets per System", 1, 2, 6, 10, 1, "planetCount")));
            UI.Add("countBias", Options.Add(GSUI.Slider("Planet Count Bias", 0, 50, 100, "countBias")));
            UI.Add("planetSize", Options.Add(GSUI.RangeSlider("Planet Size Range", 100, 200, 200, 400, 20, "planetSize")));
            UI.Add("sizeBias", Options.Add(GSUI.Slider("Planet Size Bias", 0, 50, 100, "sizeBias")));

            UI.Add("chanceGas", Options.Add(GSUI.Slider("Chance Gas", 10, 20, 50, "chanceGas")));
            UI.Add("chanceMoon", Options.Add(GSUI.Slider("Chance Moon", 10, 20, 80, "chanceMoon")));
            UI.Add("rareChance", Options.Add(GSUI.Slider("Rare Resource Vein Chance %", 0, 10, 100, 5, "rareChance")));
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

        /// <summary>Initializer for blue stars luminosity boost combo box</summary>
        private void InitializeLuminosityBoostCombobox()
        {
            bool bLuminosityBoost = preferences.GetBool("luminosityBoost", false);
            bool bExponentialBoost = preferences.GetBool("luminosityExponentialBoost", false);

            if (!bLuminosityBoost)
            {
                UI["luminosityBoost"].Set(0); // none
                UI["linearBoostCoeff"].RectTransform.gameObject.SetActive(false);
                UI["exponentialBoostCoeff"].RectTransform.gameObject.SetActive(false);
            }
            else if (!bExponentialBoost)
            {
                UI["luminosityBoost"].Set(1); // linear
                UI["linearBoostCoeff"].RectTransform.gameObject.SetActive(true);
                UI["exponentialBoostCoeff"].RectTransform.gameObject.SetActive(false);
            }
            else
            {
                UI["luminosityBoost"].Set(2); // exponential
                UI["linearBoostCoeff"].RectTransform.gameObject.SetActive(false);
                UI["exponentialBoostCoeff"].RectTransform.gameObject.SetActive(true);
            }
        }

        /// <summary>Callback for blue stars luminosity boost property</summary>
        /// <param name="o">Blue stars luminosity boost flag</param>
        private void LuminosityBoostCallback(Val o)
        {
            int val = o;
            switch (val)
            {
                case 0: // no boost
                    {
                        preferences.Set("luminosityBoost", false);
                        preferences.Set("luminosityExponentialBoost", false);
                        UI["linearBoostCoeff"].RectTransform.gameObject.SetActive(false);
                        UI["exponentialBoostCoeff"].RectTransform.gameObject.SetActive(false);
                        break;
                    }
                case 1: // linear boost
                    {
                        preferences.Set("luminosityBoost", true);
                        preferences.Set("luminosityExponentialBoost", false);
                        UI["linearBoostCoeff"].RectTransform.gameObject.SetActive(true);
                        UI["exponentialBoostCoeff"].RectTransform.gameObject.SetActive(false);
                        break;
                    }
                case 2: // exponential boost
                    {
                        preferences.Set("luminosityBoost", true);
                        preferences.Set("luminosityExponentialBoost", true);
                        UI["linearBoostCoeff"].RectTransform.gameObject.SetActive(false);
                        UI["exponentialBoostCoeff"].RectTransform.gameObject.SetActive(true);
                        break;
                    }
                default:
                    {
                        preferences.Set("luminosityBoost", false);
                        preferences.Set("luminosityExponentialBoost", false);
                        UI["linearBoostCoeff"].RectTransform.gameObject.SetActive(false);
                        UI["exponentialBoostCoeff"].RectTransform.gameObject.SetActive(false);
                        break;
                    }
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