using System;
using System.Collections.Generic;
using UnityEngine;
using static GalacticScale.GS2;
using static GalacticScale.RomanNumbers;

namespace GalacticScale.Generators
{

    public partial class VanillaPlusPlusGenerator : iConfigurableGenerator
    {

        // ////////////////////////// HOME PLANET ////////////////////////// // 

        /// <summary>Method to manage home planet theme</summary>
        private void SetBirthPlanetTheme()
        {
            if (!preferences.GetBool("birthPlanetUnlock", true)) birthPlanet.Theme = "Mediterranean";
        }

        /// <summary>Method to set size of home planet</summary>
        private void SetBirthPlanetSize()
        {
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
        }

        /// <summary>Method to optionally add Silicon & Titanium veins to home planet</summary>
        private void EnsureBirthPlanetResources()
        {
            if (preferences.GetBool("birthPlanetSiTi", false))
            {
                birthPlanet.veinSettings = birthPlanet.GsTheme.VeinSettings.Clone();
                if (birthPlanet.veinSettings.Algorithm == "Vanilla")
                    birthPlanet.veinSettings.Algorithm = "GS2";
                birthPlanet.GsTheme.CustomGeneration = true;

                var s = GSVeinType.Generate(EVeinType.Silicium, 1, 10, 0.6f, 0.6f, 5, 10, false);
                var t = GSVeinType.Generate(EVeinType.Titanium, 1, 10, 0.6f, 0.6f, 5, 10, false);
                List<EVeinType> vts = new List<EVeinType>();
                foreach (var vt in birthPlanet.veinSettings.VeinTypes)
                {
                    vts.Add(vt.type);
                }

                if (!vts.Contains(EVeinType.Silicium)) birthPlanet.veinSettings.VeinTypes.Add(s);
                if (!vts.Contains(EVeinType.Titanium)) birthPlanet.veinSettings.VeinTypes.Add(t);
                foreach (var vt in birthPlanet.veinSettings.VeinTypes)
                {
                    if (vt.type == EVeinType.Silicium || vt.type == EVeinType.Titanium) vt.rare = false;
                }

                if (preferences.GetBool("noHomeworldRares", true))
                    birthPlanet.rareChance = 0;
                else
                    birthPlanet.rareChance = preferences.GetFloat("rareChance", 15) * 0.01f;

                Log("Ensured Silicon & Titanium deposits on the birth planet.");
            }
        }

        // ////////////////////////// HOME SYSTEM ////////////////////////// //

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

            if (!GSSettings.ThemeLibrary.ContainsKey("IceGiant")) { Themes.IceGiant.Process(); }
            SystemZones sz = new SystemZones(birthStar.luminosity, birthStar.Type, birthStar.Spectr);
            GSPlanet lastPlanet = birthStar.Planets[birthStar.PlanetCount - 1];
            float orbitalRadius = lastPlanet.OrbitRadius + Mathf.Max(lastPlanet.SystemRadius + 0.4f, Mathf.Min(sz.coldZoneEdge - sz.temperateZoneEdge, random.NextFloat(0.65f, 0.95f)));
            var orbitalPeriod = Utils.CalculateOrbitPeriod(orbitalRadius);
            string name = $"{birthStar.Name} - {roman[birthStar.Planets.Count + 1]}";
            var gasPlanet = birthStar.Planets.Add(new GSPlanet(name, "IceGiant", 80, orbitalRadius, random.NextFloat(-20f, 20f), orbitalPeriod, random.NextFloat(0f, 359f), 0f, 180f, 0f, -1f));
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
                    var tiPlanet = birthStar.Planets.Add(new GSPlanet("Black Swan", "AshenGelisol", GetPlanetSize(), orbitalRadius, 0f, 100000f, 0f, 0f, 360f, 0f, -1f));
                    tiPlanet.OrbitalPeriod = Utils.CalculateOrbitPeriodFromStarMass(tiPlanet.OrbitRadius, birthStar.mass);
                    return;
                }

                var p = birthPlanet;
                while (p == birthPlanet) { p = random.Item(birthStar.TelluricBodies); }
                p.Theme = "AshenGelisol";
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

        // //////////////// SUPER STARTING SYSTEM GENERATION /////////////// //

        /// <summary>Method that generates "dream" starting system (not balanced)</summary>
        /// <param name="star">Parent star</param>
        private void GenerateDreamStartingSystemPlanets(GSStar star)
        {
            int i = random.Next(0, 2);
            switch (i)
            {
                case 0:
                    GenerateDreamStartingSystemPlanets_v1(star);
                    break;
                case 1:
                    GenerateDreamStartingSystemPlanets_v2(star);
                    break;
                default:
                    GenerateDreamStartingSystemPlanets_v1(star);
                    break;
            }
        }

        /// <summary>Method that generates "dream" starting system (not balanced) -- variant 1</summary>
        /// <param name="star">Parent star</param>
        private void GenerateDreamStartingSystemPlanets_v1(GSStar star)
        {
            star.Planets = new GSPlanets();

            // planet #1 : lava or hot obsidian or molten oasis
            List<string> planet1_themes = new List<string>() { "Lava" };
            if (GS2.externalThemes.ContainsKey("HotObsidian")) { planet1_themes.Add("HotObsidian"); }
            if (GS2.externalThemes.ContainsKey("MoltenOasis")) { planet1_themes.Add("MoltenOasis"); }
            var planet1 = CreateCelestialBody(star, null, false, false);
            planet1.Theme = random.Item(planet1_themes);
            star.Planets.Add(planet1);

            // planet #2 : gas giant
            var planet2 = CreateCelestialBody(star, null, true, false);
            planet2.Theme = "GasGiant";
            // planet #2, moon #1 : sulfur sea or volcanic ash
            List<string> planet2_moon1_themes = new List<string>() { "VolcanicAsh" };
            if (GS2.externalThemes.ContainsKey("SulfurSea")) { planet2_moon1_themes.Add("SulfurSea"); }
            var planet2_moon1 = CreateCelestialBody(star, planet2, false, true);
            planet2_moon1.Theme = random.Item(planet2_moon1_themes);
            planet2.Moons.Add(planet2_moon1);
            // planet #2, moon #2 : <home planet>
            List<string> planet2_moon2_themes = new List<string>() { "OceanicJungle", "Sakura", "Prairie", "Mediterranean" };
            if (GS2.externalThemes.ContainsKey("Swamp")) { planet2_moon2_themes.Add("Swamp"); }
            if (GS2.externalThemes.ContainsKey("FloodedMesa")) { planet2_moon2_themes.Add("FloodedMesa"); }
            var planet2_moon2 = CreateCelestialBody(star, planet2, false, true);
            planet2_moon2.Theme = random.Item(planet2_moon2_themes);
            planet2.Moons.Add(planet2_moon2);
            // planet #2, moon #3 : gobi or arid desert or red stone
            List<string> planet2_moon3_themes = new List<string>() { "Gobi", "AridDesert", "RedStone", "Hurricane" };
            var planet2_moon3 = CreateCelestialBody(star, planet2, false, true);
            planet2_moon3.Theme = random.Item(planet2_moon3_themes);
            planet2.Moons.Add(planet2_moon3);
            star.Planets.Add(planet2);

            // planet #3 : ice giant
            var planet3 = CreateCelestialBody(star, null, true, false);
            planet3.Theme = "IceGiant";
            // planet #3, moon #1 : frozen forest or ice malusol or ice lake or glacial plates or ice gelisol or frozen comet or barren
            List<string> planet3_moon1_themes = new List<string>() { "IceLake", "IceGelisol", "Barren" };
            if (GS2.externalThemes.ContainsKey("FrozenForest")) { planet3_moon1_themes.Add("FrozenForest"); }
            if (GS2.externalThemes.ContainsKey("IceMalusol")) { planet3_moon1_themes.Add("IceMalusol"); }
            if (GS2.externalThemes.ContainsKey("GlacialPlates")) { planet3_moon1_themes.Add("GlacialPlates"); }
            if (GS2.externalThemes.ContainsKey("HydrogenOcean")) { planet3_moon1_themes.Add("HydrogenOcean"); }
            if (GS2.externalThemes.ContainsKey("DeuteriumOcean")) { planet3_moon1_themes.Add("DeuteriumOcean"); }
            var planet3_moon1 = CreateCelestialBody(star, planet3, false, true);
            planet3_moon1.Theme = random.Item(planet3_moon1_themes);
            planet3.Moons.Add(planet3_moon1);
            star.Planets.Add(planet3);

            CreatePlanetOrbits(star);
            SetPlanetProperties(star);

            birthPlanet = planet2_moon2;
            GSSettings.BirthPlanetName = birthPlanet.Name;
            birthPlanetHost = planet2;
            birthPlanetIsMoon = true;

            // ensure optical crystals
            if (!planet1.GsTheme.VeinSettings.VeinTypes.ContainsVein(EVeinType.Grat))
            {
                planet1.GsTheme.VeinSettings.Algorithm = "GS2";
                planet1.GsTheme.CustomGeneration = true;
                planet1.GsTheme.VeinSettings.VeinTypes.Add(GSVeinType.Generate(EVeinType.Grat, 2, 3, 0.6f, 0.6f, 5, 10, true));
            }
        }

        /// <summary>Method that generates "dream" starting system (not balanced) -- variant 2</summary>
        /// <param name="star">Parent star</param>
        private void GenerateDreamStartingSystemPlanets_v2(GSStar star)
        {
            star.Planets = new GSPlanets();

            // planet #1 : lava or hot obsidian or molten oasis
            List<string> planet1_themes = new List<string>() { "Lava" };
            if (GS2.externalThemes.ContainsKey("HotObsidian")) { planet1_themes.Add("HotObsidian"); }
            if (GS2.externalThemes.ContainsKey("MoltenOasis")) { planet1_themes.Add("MoltenOasis"); }
            var planet1 = CreateCelestialBody(star, null, false, false);
            planet1.Theme = random.Item(planet1_themes);
            // planet #1, moon #1 : sulfur sea or volcanic ash
            List<string> planet1_moon1_themes = new List<string>() { "VolcanicAsh" };
            if (GS2.externalThemes.ContainsKey("SulfurSea")) { planet1_moon1_themes.Add("SulfurSea"); }
            var planet1_moon1 = CreateCelestialBody(star, planet1, false, true);
            planet1_moon1.Theme = random.Item(planet1_moon1_themes);
            planet1.Moons.Add(planet1_moon1);
            star.Planets.Add(planet1);

            // planet #2 : gas giant
            var planet2 = CreateCelestialBody(star, null, true, false);
            planet2.Theme = "GasGiant";
            // planet #2, moon #1 : prairie or oceanic jungle or sakura
            List<string> planet2_moon1_themes = new List<string>() { "OceanicJungle", "Sakura", "Prairie" };
            var planet2_moon1 = CreateCelestialBody(star, planet2, false, true);
            planet2_moon1.Theme = random.Item(planet2_moon1_themes);
            planet2.Moons.Add(planet2_moon1);
            // planet #2, moon #2 : <home planet>
            List<string> planet2_moon2_themes = new List<string>() { "OceanicJungle", "Sakura", "Prairie", "Mediterranean" };
            if (GS2.externalThemes.ContainsKey("Swamp")) { planet2_moon2_themes.Add("Swamp"); }
            var planet2_moon2 = CreateCelestialBody(star, planet2, false, true);
            planet2_moon2.Theme = random.Item(planet2_moon2_themes);
            planet2.Moons.Add(planet2_moon2);
            // planet #2, moon #3 : ice lake or ice gelisol
            List<string> planet2_moon3_themes = new List<string>() { "IceLake", "IceGelisol" };
            var planet2_moon3 = CreateCelestialBody(star, planet2, false, true);
            planet2_moon3.Theme = random.Item(planet2_moon3_themes);
            planet2.Moons.Add(planet2_moon3);
            star.Planets.Add(planet2);

            // planet #3 : ice lake or ice gelisol
            List<string> planet3_themes = new List<string>() { "IceLake", "IceGelisol" };
            if (GS2.externalThemes.ContainsKey("FrozenForest")) { planet3_themes.Add("FrozenForest"); }
            if (GS2.externalThemes.ContainsKey("IceMalusol")) { planet3_themes.Add("IceMalusol"); }
            if (GS2.externalThemes.ContainsKey("GlacialPlates")) { planet3_themes.Add("GlacialPlates"); }
            if (GS2.externalThemes.ContainsKey("HydrogenOcean")) { planet3_themes.Add("HydrogenOcean"); }
            if (GS2.externalThemes.ContainsKey("DeuteriumOcean")) { planet3_themes.Add("DeuteriumOcean"); }
            var planet3 = CreateCelestialBody(star, null, false, false);
            planet3.Theme = random.Item(planet3_themes);
            star.Planets.Add(planet3);

            CreatePlanetOrbits(star);
            SetPlanetProperties(star);

            birthPlanet = planet2_moon2;
            GSSettings.BirthPlanetName = birthPlanet.Name;
            birthPlanetHost = planet2;
            birthPlanetIsMoon = true;

            // ensure optical crystals
            if (!planet1.GsTheme.VeinSettings.VeinTypes.ContainsVein(EVeinType.Grat))
            {
                planet1.GsTheme.VeinSettings.Algorithm = "GS2";
                planet1.GsTheme.CustomGeneration = true;
                planet1.GsTheme.VeinSettings.VeinTypes.Add(GSVeinType.Generate(EVeinType.Grat, 2, 3, 0.6f, 0.6f, 5, 10, true));
            }
        }
    }
}