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
        private int birthPlanetIndex = -1;
        private bool birthPlanetIsMoon;
        private GSStar birthStar;
        private float maxStepLength = 3.5f;
        private float minDistance = 2f;
        private float minStepLength = 2.3f;
        private GS2.Random random;

        public string Name => "Vanilla++";
        public string Author => "innominata, NHunter";
        public string Description => "A galaxy generator trying to mimic vanilla, but with additional options";
        public string Version => "0.1";
        public string GUID => "space.customizing.generators.vanilla_plus_plus";

        // /////////////////////////// METHODS /////////////////////////// //

        /// <summary>Method for generation of a star cluster</summary>
        /// <param name="starCount">Number of stars in the cluster</param>
        public void Generate(int starCount)
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
            for (var i = 0; i < starCount; i++)
            {
                var starType = ChooseStarType();
                var star = new GSStar(random.Next(), SystemNames.GetName(i), starType.spectr, starType.type, new GSPlanets());

                GSSettings.Stars.Add(star);
                GeneratePlanetsForStar(star);
            }
            BoostBlueStarLuminosity();

            // manage birth planet
            Log("Picking BirthPlanet");
            PickNewBirthPlanet();
            Log("Birthplanet Picked");
            Log((birthPlanet != null).ToString());
            GSSettings.BirthPlanetName = birthPlanet.Name;
            Log("BirthPlanet Set");

            SetBirthPlanetTheme();
            SetBirthPlanetSize();
            EnsureBirthPlanetResources();

            EnsureProperStartingStar();
            EnsureBirthSystemBodies();
            EnsureBirthSystemHasTi();

            Log("End");
        }

        private void FixOrbitsForBirthPlanet(int newRadius)
        {
            var radiusDifference = newRadius - birthPlanet.Radius;
            var newRadiusAU = newRadius * 0.000025f;
            var auRadiusDifference = radiusDifference * 0.000025f;
            if (birthPlanet.MoonCount > 0)
                for (var i = 0; i < birthPlanet.MoonCount; i++)
                    if (birthPlanet.Moons[i].OrbitRadius + birthPlanet.Moons[i].SystemRadius > newRadiusAU)
                    {
                        birthPlanet.Moons.RemoveRange(0, i + 1);
                        Log($"Fixed birthplanet orbits by removing {i + 1} moons");
                        return;
                    }

            //Is the birthPlanet a moon?
            if (birthPlanetIsMoon)
            {
                //Can we solve this by removing sub moons?
                if (birthPlanet.MoonCount > 0)
                    for (var i = 0; i < birthPlanet.MoonCount; i++)
                        if (birthPlanet.Moons[i].OrbitRadius + birthPlanet.Moons[i].SystemRadius > newRadiusAU)
                        {
                            birthPlanet.Moons.RemoveRange(0, i + 1);
                            Log($"Fixed birthplanet orbits by removing {i + 1} sub moons");
                            return;
                        }

                //Can we solve this by removing host moons?
                if (birthPlanetHost.MoonCount > 1)
                {
                    var cumulativeSystemRadii = 0.0;
                    for (var i = birthPlanetIndex - 1; i > 0; i--)
                    {
                        // check in towards the host
                        cumulativeSystemRadii += birthPlanetHost.Moons[i].SystemRadius;
                        if (cumulativeSystemRadii > auRadiusDifference)
                        {
                            birthPlanetHost.Moons.RemoveRange(i, birthPlanetIndex - i);
                            birthPlanet.OrbitRadius -= auRadiusDifference;
                            Log($"Fixed birthplanet orbits by removing {birthPlanetIndex - i} host moons on inside");
                        }
                    }

                    cumulativeSystemRadii = 0.0;
                    for (var i = birthPlanetIndex + 1; i < birthPlanetHost.MoonCount; i++)
                    {
                        // check out away from the host
                        cumulativeSystemRadii += birthPlanetHost.Moons[i].SystemRadius;
                        if (cumulativeSystemRadii > auRadiusDifference)
                        {
                            birthPlanetHost.Moons.RemoveRange(birthPlanetIndex + 1, i - birthPlanetIndex);
                            birthPlanet.OrbitRadius -= auRadiusDifference;
                            Log($"Fixed birthplanet orbits by removing {i - birthPlanetIndex} host moons on outside");
                        }
                    }
                }

                //Can we solve this by making the host smaller?
                if (birthPlanetHost.Scale == 1f && birthPlanetHost.RadiusAU > auRadiusDifference)
                {
                    birthPlanetHost.Radius -= radiusDifference;
                    Log("Fixed birthplanet orbits by making host planet smaller");
                    return;
                }

                if (birthPlanetHost.Scale == 10f && birthPlanetHost.RadiusAU > auRadiusDifference)
                {
                    var reduction = Mathf.Max(Utils.ParsePlanetSize(radiusDifference / 10), 10);
                    birthPlanetHost.Radius -= reduction;
                    Warn("Fixed birthplanet orbits by making host planet smaller");
                    return;
                }
            }

            //Is the birthPlanet a planet?
            if (!birthPlanetIsMoon)
            {
                //Fix by moving all orbits out
                for (var i = birthPlanetIndex; i < birthStar.PlanetCount; i++)
                {
                    birthStar.Planets[i].OrbitRadius += 2 * auRadiusDifference;
                    birthPlanet.OrbitRadius -= auRadiusDifference;
                }

                Log(
                    $"Fixed birthplanet orbits by adding size difference to orbit radius for all planets at or above index {birthPlanetIndex}");
                return;
            }

            Error("Failed to adjust orbits for birthPlanet Increased Size");
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
