using System;
using System.Collections.Generic;
using UnityEngine;
using static GalacticScale.GS2;
using static GalacticScale.RomanNumbers;

namespace GalacticScale.Generators
{
    /// <summary>Structure contatining data about 'climate' zones of the star system</summary>
    struct SystemZones
    {
        /// <summary>Constructor; generates zones based on star type and luminosity</summary>
        /// <param name="luminosity">Star's luminosity</param>
        /// <param name="starType">Star's type</param>
        /// <param name="spectreType">Star's spectral class</param>
        public SystemZones(float luminosity, EStarType starType, ESpectrType spectreType)
        {
            // remnant star types have a different logic for zones' generation
            if (spectreType == ESpectrType.X)
            {
                warmZoneEdge = Mathf.Max(Mathf.Sqrt(luminosity / 2.5f), 0.15f);
                temperateZoneEdge = Mathf.Max(Mathf.Sqrt(luminosity / 1.1f), 0.25f);
                coldZoneEdge = Mathf.Max(Mathf.Sqrt(luminosity / 0.53f), 0.45f);
                frozenZoneEdge = Mathf.Max(Mathf.Sqrt(luminosity / 0.3f), 0.7f);
                return;
            }

            // if this is a normal star, zones are dependent on luminosity
            // but we need to shift zones outwards for giant stars
            float multiplier = (starType == EStarType.GiantStar) ? 8.0f : 1.0f;
            warmZoneEdge = multiplier * Mathf.Sqrt(luminosity / 2.5f);
            temperateZoneEdge = multiplier * Mathf.Sqrt(luminosity / 1.1f);
            coldZoneEdge = multiplier * Mathf.Sqrt(luminosity / 0.53f);
            frozenZoneEdge = multiplier * Mathf.Sqrt(luminosity / 0.3f);
        }

        public float warmZoneEdge;
        public float temperateZoneEdge;
        public float coldZoneEdge;
        public float frozenZoneEdge;
    };

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

        static readonly string[] moonLetters = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };

        /// <summary>Method for generation of a star cluster</summary>
        /// <param name="starCount">Number of stars in the cluster</param>
        public void Generate(int starCount)
        {
            if (starCount < Config.MinStarCount) { starCount = Config.MinStarCount; }
            if (starCount > Config.MaxStarCount) { starCount = Config.MaxStarCount; }

            Log($"Start {GSSettings.Seed}");
            GSSettings.Reset(GSSettings.Seed);
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

            Log("Picking BirthPlanet");
            PickNewBirthPlanet();
            Log("Birthplanet Picked");
            if (!preferences.GetBool("birthPlanetUnlock", true)) birthPlanet.Theme = "Mediterranean";
            Log((birthPlanet != null).ToString());
            GSSettings.BirthPlanetName = birthPlanet.Name;
            Log("BirthPlanet Set");
            if (preferences.GetBool("birthPlanetSiTi")) AddSiTiToBirthPlanet();

            int iRequiredHomeworldSize = preferences.GetInt("birthPlanetSize", 200);
            if (iRequiredHomeworldSize != birthPlanet.Radius)
            {
                Log("Forcing BirthPlanet Size");
                birthPlanet.Radius = iRequiredHomeworldSize;
                birthPlanet.Scale = 1f;

                // if birth planet is moon, ensure parent body is bigger
                if (birthPlanetIsMoon && birthPlanetHost.Radius * birthPlanetHost.Scale <= birthPlanet.Radius)
                {
                    if (birthPlanetHost.GsTheme.ThemeType == EThemeType.Gas)
                    {
                        birthPlanetHost.Radius = Convert.ToInt32(birthPlanet.Radius / birthPlanetHost.Scale) + 10;
                    }
                    else
                    {
                        birthPlanetHost.Radius = birthPlanet.Radius + 50;
                    }
                }
            }

            // boost white and blue stars
            // actual in-game luminosity if roughly proportional to the cubic root of star.luminosity
            if (preferences.GetBool("luminosityBoost", false))
            {
                foreach (var star in GSSettings.Stars)
                {
                    if (star.Spectr == ESpectrType.F || star.Spectr == ESpectrType.A)
                    {
                        star.luminosity *= 1.953f; // 1.25^3
                    }
                    else if (star.Spectr == ESpectrType.B || star.Spectr == ESpectrType.O)
                    {
                        star.luminosity *= 3.375f; // 1.5^3
                    }
                }
            }

            EnsureProperStartingStar();
            EnsureBirthSystemHasTi();
            EnsureBirthSystemBodies();

            Log("End");
        }

        /// <summary>Ensure starting system has a proper star</summary>
        private void EnsureProperStartingStar()
        {
            if (birthStar.Spectr == ESpectrType.X)
            {
                birthStar.Spectr = ESpectrType.G;
                birthStar.Type = EStarType.MainSeqStar;
                birthStar.radius = StarDefaults.Radius(birthStar);
                birthStar.luminosity = StarDefaults.Luminosity(birthStar);
                birthStar.age = StarDefaults.Age(birthStar);
                birthStar.mass = StarDefaults.Mass(birthStar);
                birthStar.lifetime = StarDefaults.Luminosity(birthStar);
                birthStar.acDiscRadius = -1;
                birthStar.color = StarDefaults.Color(birthStar);
                birthStar.temperature = StarDefaults.Temperature(birthStar);
                birthStar.dysonRadius = StarDefaults.DysonRadius(birthStar);
                CreatePlanetOrbits(birthStar);
            }
        }

        /// <summary>Method to guarantee that at last one planet in the starting system has titanium veins</summary>
        private void EnsureBirthSystemHasTi()
        {
            if (!BirthSystemHasTi())
            {
                if (birthStar.TelluricBodyCount < 2)
                {
                    if (!GSSettings.ThemeLibrary.ContainsKey("AshenGelisol")) { Themes.AshenGelisol.Process(); }
                    SystemZones sz = new SystemZones(birthStar.luminosity, birthStar.Type, birthStar.Spectr);
                    GSPlanet lastPlanet = birthStar.Planets[birthStar.PlanetCount - 1];
                    float orbitalRadius = lastPlanet.OrbitRadius + Mathf.Max(lastPlanet.SystemRadius + 0.4f, Mathf.Min(sz.coldZoneEdge - sz.temperateZoneEdge, random.NextFloat(0.65f, 0.95f)));
                    var tiPlanet = birthStar.Planets.Add(new GSPlanet("Black Swan", "AshenGelisol", GetStarPlanetSize(birthStar), orbitalRadius, 0f, 100000f, 0f, 0f, 360f, 0f, -1f));
                    tiPlanet.OrbitalPeriod = Utils.CalculateOrbitPeriodFromStarMass(tiPlanet.OrbitRadius, birthStar.mass);
                    return;
                }

                var p = birthPlanet;
                while (p == birthPlanet) { p = random.Item(birthStar.TelluricBodies); }
                p.Theme = "AshenGellisol";
            }
        }
        
        /// <summary>Method to guarantee that home system always has a gas giant and at least one terrestrial planet that is not the starter one</summary>
        private void EnsureBirthSystemBodies()
        {
            // if we don't have a gas giant yet, create one
            for (int i = 0; i < birthStar.bodyCount; ++i)
            {
                if (birthStar.Bodies[i].GsTheme.ThemeType == EThemeType.Gas)
                {
                    return;
                }
            }

            if (random.NextPick(0.66))
            {
                if (!GSSettings.ThemeLibrary.ContainsKey("IceGiant")) { Themes.IceGiant.Process(); }
                SystemZones sz = new SystemZones(birthStar.luminosity, birthStar.Type, birthStar.Spectr);
                GSPlanet lastPlanet = birthStar.Planets[birthStar.PlanetCount - 1];
                float orbitalRadius = lastPlanet.OrbitRadius + Mathf.Max(lastPlanet.SystemRadius + 0.4f, Mathf.Min(sz.coldZoneEdge - sz.temperateZoneEdge, random.NextFloat(0.65f, 0.95f)));
                var orbitalPeriod = Utils.CalculateOrbitPeriod(orbitalRadius);
                var gasPlanet = birthStar.Planets.Add(new GSPlanet("Nibiru", "IceGiant", 80, orbitalRadius, random.NextFloat(-20f, 20f), orbitalPeriod, random.NextFloat(0f, 359f), 0f, 180f, 0f, -1f));
            }
        }

        /// <summary>Method to check that home system has at least one planet with titanium veins</summary>
        /// <returns>TRUE if home system has titanium veins</returns>
        private bool BirthSystemHasTi()
        {
            foreach (var p in birthStar.Bodies)
            {
                if (p.GsTheme.VeinSettings.VeinTypes.ContainsVein(EVeinType.Titanium)) { return true; }
            }
            return false;
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

        /// <summary>Method to determine how close stars in the cluster should be to each other</summary>
        /// <param name="density">Density level of stars in the cluster</param>
        public void SetGalaxyDensity(int density)
        {
            switch (density)
            {
                case 1:
                    minStepLength = 1.2f;
                    maxStepLength = 1.5f;
                    minDistance = 1.2f;
                    break;
                case 2:
                    minStepLength = 1.4f;
                    maxStepLength = 2f;
                    minDistance = 1.5f;
                    break;
                case 3:
                    minStepLength = 1.6f;
                    maxStepLength = 2.5f;
                    minDistance = 1.7f;
                    break;
                case 4:
                    minStepLength = 1.8f;
                    maxStepLength = 3f;
                    minDistance = 2f;
                    break;
                case 5:
                    minStepLength = 2f;
                    maxStepLength = 3.5f;
                    minDistance = 2.3f;
                    break;
                case 6:
                    minStepLength = 2.2f;
                    maxStepLength = 4.2f;
                    minDistance = 2.4f;
                    break;
                case 7:
                    minStepLength = 2.5f;
                    maxStepLength = 5.0f;
                    minDistance = 2.6f;
                    break;
                case 8:
                    minStepLength = 2.7f;
                    maxStepLength = 6.0f;
                    minDistance = 2.8f;
                    break;
                case 9:
                    minStepLength = 3.0f;
                    maxStepLength = 7.0f;
                    minDistance = 3.0f;
                    break;
                default:
                    minStepLength = 2f;
                    maxStepLength = 3.5f;
                    minDistance = 2.3f;
                    break;
            }

            GSSettings.GalaxyParams.minDistance = minDistance;
            GSSettings.GalaxyParams.minStepLength = minStepLength;
            GSSettings.GalaxyParams.maxStepLength = maxStepLength;
        }

        /// <summary>Method for deterimining the number of planetary bodies in a star's planetary system</summary>
        /// <param name="star">Parent star</param>
        /// <returns>Number of planets</returns>
        private int GetStarPlanetCount(GSStar star)
        {
            var min = GetMinPlanetCount();
            var max = GetMaxPlanetCount();
            var result = ClampedNormal(min, max, GetCountBias());
            return result;
        }

        /// <summary>Method for determining planetary object's size</summary>
        /// <param name="star">Parent star</param>
        /// <returns>Planetary object's size</returns>
        private int GetStarPlanetSize(GSStar star)
        {
            var min = GetMinPlanetSize();
            var max = GetMaxPlanetSize();
            var bias = GetSizeBias();
            int size = ClampedNormalSize(min, max, bias);
            size = Mathf.RoundToInt(size / 10f) * 10; // size step = 10
            return size;
        }

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

        /// <summary>Method for generating moon's size</summary>
        /// <param name="star">Parent star</param>
        /// <param name="hostRadius">Radius of parent planet</param>
        /// <param name="hostGas">TRUE if parent planet is a gas giant</param>
        /// <returns></returns>
        private int GetStarMoonSize(GSStar star, int hostRadius, bool hostGas)
        {
            int size = GetStarPlanetSize(star);
            int trueHostRadius = hostGas ? hostRadius * 10 : hostRadius;
            if (size > trueHostRadius)
            {
                size = trueHostRadius - 20;
            }
            if (preferences.GetBool("moonsAreSmall", false))
            {
                size /= 2;
            }
            return size;

            //if (hostGas) hostRadius *= 10;
            //var min = Utils.ParsePlanetSize(GetMinPlanetSize());
            //int max;
            //if (preferences.GetBool("moonsAreSmall", true))
            //{
            //    float divider = 2;
            //    if (hostGas) divider = 4;
            //    max = Utils.ParsePlanetSize(Mathf.RoundToInt(hostRadius / divider));
            //}
            //else
            //{
            //    max = Utils.ParsePlanetSize(hostRadius - 10);
            //}

            //if (max <= min) return min;
            ////float average = (max - min) * 0.5f + min;
            ////var range = max - min;
            ////var sd = (float)range * 0.25f;
            //var size = ClampedNormalSize(min, max, GetSizeBias());
            //return size;
        }

        /// <summary>Method for generating planetary objects of a star</summary>
        /// <param name="star">Parent star</param>
        private void GeneratePlanetsForStar(GSStar star)
        {
            star.Planets = new GSPlanets();
            var starBodyCount = GetStarPlanetCount(star);
            if (starBodyCount == 0) { return; }
            var moonChance = GetMoonChance();
            var moonCount = 0;

            var protos = new List<ProtoPlanet>();
            var moons = new List<ProtoPlanet>();
            
            protos.Add(new ProtoPlanet { gas = CalculateIsGasGiant(), radius = GetStarPlanetSize(star) });
            if (protos[0].gas) { protos[0].radius = random.Next(80, 161); }

            for (var i = 1; i < starBodyCount; i++)
            {
                if (random.NextPick(moonChance))
                {
                    moonCount++;
                }
                else
                {
                    var p = new ProtoPlanet { gas = CalculateIsGasGiant(), radius = GetStarPlanetSize(star) };
                    if (p.gas) { p.radius = random.Next(80, 161); }

                    protos.Add(p);
                }
            }

            for (var i = 0; i < moonCount; i++)   
            {
                var randomProto = random.Item(protos);
                var moon = new ProtoPlanet { gas = false, radius = GetStarMoonSize(star, randomProto.radius, randomProto.gas) };
                randomProto.moons.Add(moon);
                moons.Add(moon);
            }

            foreach (var proto in protos)
            {
                var planet = new GSPlanet(star.Name + "-Planet", null, proto.radius, -1, -1, -1, -1, -1, -1, -1, -1);
                if (proto.gas) planet.Scale = 10f;
                else planet.Scale = 1f;
                
                if (proto.moons.Count > 0) planet.Moons = new GSPlanets();
                foreach (var moon in proto.moons)
                {
                    var planetMoon = new GSPlanet(star.Name + "-Moon", null, moon.radius, -1, -1, -1, -1, -1, -1, -1, -1);
                    planetMoon.Scale = 1f;
                    planet.Moons.Add(planetMoon);
                }

                star.Planets.Add(planet);
            }

            CreatePlanetOrbits(star);
            SelectPlanetThemes(star);
            FudgeNumbersForPlanets(star);
        }

        /// <summary>Method for setting parameters for planetary objects</summary>
        /// <param name="star">Parent star</param>
        private void FudgeNumbersForPlanets(GSStar star)
        {
            foreach (var body in star.Bodies)
            {
                body.RotationPhase = random.Next(360);
                body.OrbitInclination = random.NextFloat(-20.0f, 20.0f);
                body.OrbitPhase = random.Next(360);
                body.Obliquity = random.NextFloat() * 20;
                body.RotationPeriod = random.Next(80, 3600);

                if (random.NextDouble() < 0.02)
                    body.OrbitalPeriod = -1 * body.OrbitalPeriod; // Clockwise Rotation

                if (body.OrbitRadius < 1f && random.NextFloat() < 0.5f)
                    body.RotationPeriod = body.OrbitalPeriod; // Tidal Lock
                else if (body.OrbitRadius < 1.5f && random.NextFloat() < 0.2f)
                    body.RotationPeriod = body.OrbitalPeriod / 2; // 1:2 Resonance
                else if (body.OrbitRadius < 2f && random.NextFloat() < 0.1f)
                    body.RotationPeriod = body.OrbitalPeriod / 4; // 1:4 Resonance
                if (random.NextDouble() < 0.05) // Crazy Obliquity
                    body.Obliquity = random.NextFloat(20f, 85f);

                // tidal-lock innermost planet if preference is set
                if (body == star.Planets[0] && preferences.GetBool("tidalLockInnerPlanets", false))
                {
                    body.RotationPeriod = body.OrbitalPeriod;
                }
            }
        }

        /// <summary>Method for setting themes for planetary objects</summary>
        /// <param name="star">Parent star</param>
        private void SelectPlanetThemes(GSStar star)
        {
            foreach (var planet in star.Planets)
            {
                var heat = CalculateThemeHeat(star, planet.OrbitRadius);
                var type = EThemeType.Planet;
                if (planet.Scale == 10f) { type = EThemeType.Gas; }
                planet.Theme = GSSettings.ThemeLibrary.Query(random, type, heat, planet.Radius);
                foreach (var body in planet.Bodies)
                {
                    if (body != planet) { body.Theme = GSSettings.ThemeLibrary.Query(random, EThemeType.Moon, heat, body.Radius); }
                }
            }
        }

        /// <summary>Method for generating orbits for star's planetary bodies</summary>
        /// <param name="star">Parent star</param>
        private void CreatePlanetOrbits(GSStar star)
        {
            // star's habitable zone
            SystemZones sz = new SystemZones(star.luminosity, star.Type, star.Spectr);
            // planets
            bool bInnerPlanetIsClose = random.NextPick(0.5);
            for (var planetIndex = 0; planetIndex < star.PlanetCount; planetIndex++)
            {
                var planet = star.Planets[planetIndex];
                planet.Name = $"{star.Name} - {roman[planetIndex + 1]}";

                // moons of the planet
                for (var moonIndex = 0; moonIndex < planet.MoonCount; moonIndex++)
                {
                    var moon = planet.Moons[moonIndex];
                    moon.Name = $"{star.Name} - {roman[planetIndex + 1]} - {moonLetters[moonIndex]}";

                    if (moonIndex == 0) { moon.OrbitRadius = planet.RadiusAU + GetMoonOrbit(); }
                    else { moon.OrbitRadius = planet.Moons[moonIndex - 1].OrbitRadius + GetMoonOrbit(); }

                    moon.OrbitalPeriod = Utils.CalculateOrbitPeriod(moon.OrbitRadius);
                }

                // if this is a normal star
                if (star.Type == EStarType.MainSeqStar)
                {
                    // innermost planet may be close to the star
                    if (planetIndex == 0)
                    {
                        if (bInnerPlanetIsClose)
                        {
                            planet.OrbitRadius = Mathf.Max(star.RadiusAU * 2.0f, random.NextFloat(sz.warmZoneEdge * 0.5f, sz.warmZoneEdge)) + planet.SystemRadius;
                        }
                        else
                        {
                            planet.OrbitRadius = Mathf.Max(star.RadiusAU * 3.0f, random.NextFloat(0.7f, 0.9f) * sz.temperateZoneEdge) + planet.SystemRadius;
                        }
                    }
                    // second planet has orbit gap variance depending on whether the innermost one is close to the star
                    else if (planetIndex == 1)
                    {
                        if (bInnerPlanetIsClose)
                        {
                            planet.OrbitRadius = star.Planets[0].OrbitRadius + Mathf.Max(planet.SystemRadius + star.Planets[0].SystemRadius + 0.25f, random.NextFloat(0.5f, 0.75f) * sz.temperateZoneEdge);
                        }
                        else
                        {
                            planet.OrbitRadius = star.Planets[0].OrbitRadius + Mathf.Max(planet.SystemRadius + star.Planets[0].SystemRadius + 0.25f, random.NextFloat(0.3f, 0.5f) * sz.temperateZoneEdge);
                        }
                    }
                    // third and later planets
                    else
                    {
                        planet.OrbitRadius = star.Planets[planetIndex - 1].OrbitRadius + Mathf.Max(planet.SystemRadius + star.Planets[planetIndex - 1].SystemRadius + 0.25f, random.NextFloat(0.3f, 0.5f) * sz.temperateZoneEdge);
                    }
                }
                // if this is a giant star
                else if (star.Type == EStarType.GiantStar)
                {
                    // innermost planet may be close to the star
                    if (planetIndex == 0)
                    {
                        if (bInnerPlanetIsClose)
                        {
                            planet.OrbitRadius = Mathf.Max(star.RadiusAU * 1.25f, random.NextFloat(sz.warmZoneEdge * 0.5f, sz.warmZoneEdge)) + planet.SystemRadius;
                        }
                        else
                        {
                            planet.OrbitRadius = Mathf.Max(star.RadiusAU * 3.0f, random.NextFloat(0.7f, 0.85f) * (sz.coldZoneEdge - sz.temperateZoneEdge) + star.RadiusAU * 0.25f) + planet.SystemRadius;
                        }
                    }
                    // second planet has orbit gap variance depending on whether the innermost one is close to the star
                    else if (planetIndex == 1)
                    {
                        if (bInnerPlanetIsClose)
                        {
                            planet.OrbitRadius = star.Planets[0].OrbitRadius + Mathf.Max(planet.SystemRadius + star.Planets[0].SystemRadius + 0.25f, random.NextFloat(0.75f, 0.9f) * (sz.coldZoneEdge - sz.temperateZoneEdge));
                        }
                        else
                        {
                            planet.OrbitRadius = star.Planets[0].OrbitRadius + Mathf.Max(planet.SystemRadius + star.Planets[0].SystemRadius + 0.25f, random.NextFloat(0.15f, 0.35f) * (sz.coldZoneEdge - sz.temperateZoneEdge));
                        }
                    }
                    // third and later planets
                    else
                    {
                        planet.OrbitRadius = star.Planets[planetIndex - 1].OrbitRadius + Mathf.Max(planet.SystemRadius + star.Planets[planetIndex - 1].SystemRadius + 0.25f, random.NextFloat(0.15f, 0.35f) * (sz.coldZoneEdge - sz.temperateZoneEdge));
                    }
                }
                // if this is a remnant star
                else
                {
                    // innermost planet may spawn close to the star
                    if (planetIndex == 0)
                    {
                        if (bInnerPlanetIsClose)
                        {
                            planet.OrbitRadius = random.NextFloat(0.25f, 0.45f);
                        }
                        else
                        {
                            planet.OrbitRadius = random.NextFloat(0.7f, 0.85f);
                        }
                    }
                    // other planets spawn random-ish distance away from previous one
                    else
                    {
                        planet.OrbitRadius = star.Planets[planetIndex - 1].OrbitRadius + Mathf.Max(random.NextFloat(0.35f, 0.5f), planet.SystemRadius + star.Planets[planetIndex - 1].SystemRadius + 0.2f);
                    }
                }

                planet.OrbitalPeriod = Utils.CalculateOrbitPeriod(planet.OrbitRadius);
            }
        }

        /// <summary>Method for determining orbit distance of a moon</summary>
        /// <returns>Orbit distance of a moon</returns>
        private float GetMoonOrbit()
        {
            return 0.04f + random.NextFloat(0f, 0.02f);
        }

        /// <summary>Method for adding silicon and titanium veins to the staring planet</summary>
        private void AddSiTiToBirthPlanet()
        {
            Warn("Setting SI/TI");
            birthPlanet.GsTheme.VeinSettings.Algorithm = "GS2";
            birthPlanet.GsTheme.CustomGeneration = true;
            birthPlanet.GsTheme.VeinSettings.VeinTypes.Add(GSVeinType.Generate(EVeinType.Silicium, 1, 10, 0.6f, 0.6f, 5, 10, false));
            birthPlanet.GsTheme.VeinSettings.VeinTypes.Add(GSVeinType.Generate( EVeinType.Titanium, 1, 10, 0.6f, 0.6f, 5, 10, false));
        }

        /// <summary>Method for picking a different starting planet in case original is not valid</summary>
        private void PickNewBirthPlanet()
        {
            if (GSSettings.StarCount == 0)
                Error("Cannot pick birth planet as there are 0 generated stars");

            var HabitablePlanets = GSSettings.Stars.HabitablePlanets;
            if (HabitablePlanets.Count == 1)
            {
                birthPlanet = HabitablePlanets[0];
                birthStar = GetGSStar(birthPlanet);
                if (IsPlanetOfStar(birthStar, birthPlanet))
                {
                    birthPlanetHost = null;
                    Log($"Selected only habitable planet {birthPlanet.Name} as planet of {birthStar.Name}");
                    return;
                }

                foreach (var planet in birthStar.Planets)
                {
                    foreach (var moon in planet.Moons)
                    {
                        if (moon == birthPlanet)
                        {
                            birthPlanetHost = planet;
                            Log($"Selected only habitable planet {birthPlanet.Name} as moon of {birthStar.Name}");
                            return;
                        }

                        if (IsMoonOfPlanet(moon, birthPlanet))
                        {
                            birthPlanetHost = moon;
                            Log($"Selected only habitable planet {birthPlanet.Name} as submoon of {birthStar.Name}");
                            return;
                        }
                    }
                }
            }

            if (HabitablePlanets.Count == 0)
            {
                Log("Generating new habitable planet by overwriting an existing one");
                var star = GSSettings.Stars.RandomStar;
                var index = 0;
                if (star.PlanetCount > 1) index = Mathf.RoundToInt((star.PlanetCount - 1) / 2);

                var planet = star.Planets[index];
                var themeNames = GSSettings.ThemeLibrary.Habitable;
                var themeName = themeNames[random.Next(themeNames.Count)];
                Log($"Setting Planet Theme to {themeName}");
                planet.Theme = themeName;
                birthPlanet = planet;
                birthPlanetIndex = index;
                birthStar = star;
                Log($"Selected {birthPlanet.Name}");
            }
            else if (HabitablePlanets.Count > 1)
            {
                Log("Selecting random habitable planet");
                birthPlanet = HabitablePlanets[random.Next(1, HabitablePlanets.Count - 1)];
                birthStar = GetGSStar(birthPlanet);
                for (var i = 0; i < birthStar.PlanetCount; i++)
                {
                    if (birthStar.Planets[i] == birthPlanet)
                    {
                        birthPlanetIsMoon = false;
                        birthPlanetIndex = i;
                        Log($"Selected {birthPlanet.Name} as birthPlanet (planet) index {i} of star {birthStar.Name}");
                        return;
                    }

                    for (var j = 0; j < birthStar.Planets[i].Moons.Count; j++)
                    {
                        if (birthStar.Planets[i].Moons[j] == birthPlanet)
                        {
                            birthPlanetIsMoon = true;
                            birthPlanetHost = birthStar.Planets[i];
                            birthPlanetIndex = j;
                            Log(
                                $"Selected {birthPlanet.Name} as birthPlanet (moon) index {j} of planet {birthPlanetHost.Name} ");
                            return;
                        }

                        for (var k = 0; k < birthStar.Planets[i].Moons[j].Moons.Count; k++)
                        {
                            if (birthStar.Planets[i].Moons[j].Moons[k] == birthPlanet)
                            {
                                birthPlanetIsMoon = true;
                                birthPlanetHost = birthStar.Planets[i].Moons[j];
                                birthPlanetIndex = k;
                                Log(
                                    $"Selected {birthPlanet.Name} as birthPlanet (sub moon) index {k} of moon {birthPlanetHost.Name} ");
                                return;
                            }
                        }
                    }
                }

                Error($"Selected {birthPlanet.Name} but failed to find a birthStar or host!");
            }
        }

        /// <summary>Method for determining if planetary object is a gas giant</summary>
        /// <param name="star">Parent star of a planetary object</param>
        /// <returns>TRUE if planetary object is a gas giant</returns>
        private bool CalculateIsGasGiant()
        {
            var gasChance = GetGasChanceGiant();
            return random.NextPick(gasChance);
        }

        /// <summary>Method for determining "temperature" class of a planetary object</summary>
        /// <param name="star">Parent star of a planetary object</param>
        /// <param name="OrbitRadius">Orbit radius for a planetary object</param>
        /// <returns>"Temperature" class of a planetary object</returns>
        public static EThemeHeat CalculateThemeHeat(GSStar star, float OrbitRadius)
        {
            SystemZones sz = new SystemZones(star.luminosity, star.Type, star.Spectr);
            if (OrbitRadius < sz.warmZoneEdge) return EThemeHeat.Hot;
            if (OrbitRadius < sz.temperateZoneEdge) return EThemeHeat.Warm;
            if (OrbitRadius < sz.coldZoneEdge) return EThemeHeat.Temperate;
            if (OrbitRadius < sz.frozenZoneEdge) return EThemeHeat.Cold;
            return EThemeHeat.Frozen;
        }

        private class ProtoPlanet
        {
            public bool gas;
            public readonly List<ProtoPlanet> moons = new List<ProtoPlanet>();
            public int radius;
        }
    }
}
