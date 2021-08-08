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
            if (preferences.GetBool("birthPlanetSiTi"))
            {
                Warn("Setting SI/TI");
                birthPlanet.GsTheme.VeinSettings.Algorithm = "GS2";
                birthPlanet.GsTheme.CustomGeneration = true;
                birthPlanet.GsTheme.VeinSettings.VeinTypes.Add(GSVeinType.Generate(EVeinType.Silicium, 1, 10, 0.6f, 0.6f, 5, 10, false));
                birthPlanet.GsTheme.VeinSettings.VeinTypes.Add(GSVeinType.Generate(EVeinType.Titanium, 1, 10, 0.6f, 0.6f, 5, 10, false));
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
                p.Theme = "AshenGellisol";
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

    }
}