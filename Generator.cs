using System;
using System.Collections.Generic;
using UnityEngine;
using static GalacticScale.GS2;

namespace GalacticScale.Generators
{

    public partial class VanillaPlusPlusGenerator : iConfigurableGenerator
    {
        private GSPlanet birthPlanet;
        private GSPlanet birthPlanetHost;
        //private int birthPlanetIndex = -1;
        private bool birthPlanetIsMoon;
        private GSStar birthStar;
        private float maxStepLength = 3.5f;
        private float minDistance = 2f;
        private float minStepLength = 2.3f;
        private GS2.Random random;

        public string Name => "Vanilla++";
        public string Author => "innominata, NHunter";
        public string Description => "A galaxy generator trying to mimic vanilla, but with additional options";
        public string Version => "0.0.8";
        public string GUID => "space.customizing.generators.vanilla_plus_plus";

        // /////////////////////////// METHODS /////////////////////////// //

        /// <summary>Method for generation of a star cluster</summary>
        /// <param name="starCount">Number of stars in the cluster</param>
        /// <param name="birthStar">Interface parameter, not used. Denotes player-selected birth star</param>
        public void Generate(int starCount, StarData birthStar = null)
        {
            if (starCount < Config.MinStarCount) { starCount = Config.MinStarCount; }
            if (starCount > Config.MaxStarCount) { starCount = Config.MaxStarCount; }

            Log($"Start {GSSettings.Seed}");
            GSSettings.Reset(GSSettings.Seed);
            
            // generate galaxy
            SetupBaseThemes();
            InitThemes();
            GSSettings.GalaxyParams.graphDistance = 32;
            GSSettings.GalaxyParams.graphMaxStars = 512;
            SetGalaxyDensity(preferences.GetInt("galaxyDensity", 5));
            random = new GS2.Random(GSSettings.Seed);
            CalculateFrequencies();

            Log("Generating Stars");

            GenerateStartingSystem();
            CreateMultistarSystem(this.birthStar);
            for (var i = 1; i < starCount; i++)
            {
                var starType = ChooseStarType();
                var star = new GSStar(random.Next(), SystemNames.GetName(i), starType.spectr, starType.type, new GSPlanets());

                GSSettings.Stars.Add(star);
                CreateMultistarSystem(star);
                GeneratePlanetsForStar(star);
                EnsureProperOrbitalPeriods(star);
            }

            BoostBlueStarLuminosity();
            foreach (var star in GSSettings.Stars)
            {
                if (star.Type == EStarType.BlackHole)
                {
                    star.radius *= 0.33f;
                }
            }
            Log("End");
        }

        /// <summary>Method for generating clamped value with bias</summary>
        /// <param name="min">Minimal value</param>
        /// <param name="max">Maximal value</param>
        /// <param name="bias">Value bias</param>
        /// <returns>Biased clamped value between min and max</returns>
        private int ClampedNormal(int min, int max, int bias)
        {
            var range = max - min;
            var average = bias / 100f * range + min;
            var sdHigh = (max - average) / 3;
            var sdLow = (average - min) / 3;
            var sd = Math.Max(sdLow, sdHigh);
            var rResult = Mathf.RoundToInt(random.Normal(average, sd));
            var result = Mathf.Clamp(rResult, min, max);
            //Warn($"ClampedNormal min:{min} max:{max} bias:{bias} range:{range} average:{average} sdHigh:{sdHigh} sdLow:{sdLow} sd:{sd} fResult:{fResult} result:{result}");
            return result;
        }

        /// <summary>Method for generating clamped planet size with bias</summary>
        /// <param name="min">Minimal planet size</param>
        /// <param name="max">Maximal planet size</param>
        /// <param name="bias">Planet size bias</param>
        /// <returns>Biased clamped planet between min and max</returns>
        private int ClampedNormalSize(int min, int max, int bias)
        {
            var range = max - min;
            var average = bias / 100f * range + min;
            var sdHigh = (max - average) / 3;
            var sdLow = (average - min) / 3;
            var sd = Math.Max(sdLow, sdHigh);
            var fResult = random.Normal(average, sd);
            var result = Mathf.Clamp(Utils.ParsePlanetSize(fResult), min, max);
            //Warn($"ClampedNormal min:{min} max:{max} bias:{bias} range:{range} average:{average} sdHigh:{sdHigh} sdLow:{sdLow} sd:{sd} fResult:{fResult} result:{result}");
            return result;
        }
   }
}
