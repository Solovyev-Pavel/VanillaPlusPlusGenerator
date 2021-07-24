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

        /// <summary>Callback for setting default star count for the cluster</summary>
        /// <param name="o">Default star count</param>
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
            UI.Add("sizeBias", Options.Add(GSUI.Slider("Planet Size Bias", 0, 50, 100, "sizeBias", PlanetSizeBiasCallback)));

            UI.Add("chanceGas", Options.Add(GSUI.Slider("Chance Gas", 10, 20, 50, "chanceGas", GasChanceCallback)));
            UI.Add("chanceMoon", Options.Add(GSUI.Slider("Chance Moon", 10, 20, 80, "chanceMoon", MoonChanceCallback)));
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
            var maxSize = preferences.GetFloat("maxPlanetSize");
            if (maxSize == -1f) maxSize = 400;
            if (maxSize < o) { o = maxSize; }
            preferences.Set("minPlanetSize", Utils.ParsePlanetSize(o));
            UI["minPlanetSize"].Set(preferences.GetFloat("minPlanetSize"));
        }

        /// <summary>Callback for setting max planet size</summary>
        /// <param name="o">Max planet size value</param>
        private void MaxPlanetSizeCallback(Val o)
        {
            var minSize = preferences.GetFloat("minPlanetSize");
            if (minSize == -1f) minSize = 50;
            if (minSize > o) { o = minSize; }
            preferences.Set("maxPlanetSize", Utils.ParsePlanetSize(o));
            UI["maxPlanetSize"].Set(preferences.GetFloat("maxPlanetSize"));
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