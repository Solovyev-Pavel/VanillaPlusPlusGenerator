using System.Collections.Generic;
using UnityEngine;
using static GalacticScale.RomanNumbers;
using static GalacticScale.GS2;

namespace GalacticScale.Generators
{

    public partial class VanillaPlusPlusGenerator : iConfigurableGenerator
    {

        static readonly string[] moonLetters = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };

        /// <summary>Structure contatining data about 'climate' zones of the star system</summary>
        private struct SystemZones
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

        // //////////////////////////// METHODS //////////////////////////// //

        /// <summary>Method for generating planetary objects of a star</summary>
        /// <param name="star">Parent star</param>
        private void GeneratePlanetsForStar(GSStar star)
        {
            star.Planets = new GSPlanets();
            var starBodyCount = GetPlanetCount();
            if (starBodyCount == 0) { return; }
            var moonChance = GetMoonChance();
            bool bGasGiantMoons = preferences.GetBool("moreLikelyGasGiantMoons", false);

            var firstPlanet = CreateCelestialBody(star, null, GetPlanetIsGasGiant(), false);
            star.Planets.Add(firstPlanet);
            int prevPlanet = 0;

            for (int i = 1; i < starBodyCount; i++)
            {
                bool bPrevPlanetIsGasGiant = (star.Planets[prevPlanet].Scale == 10f);
                bool bAlreadyHasMoons = (star.Planets[prevPlanet].Moons.Count != 0);
                double dFinalMoonChance = moonChance;
                if (bPrevPlanetIsGasGiant && bGasGiantMoons && !bAlreadyHasMoons)
                {
                    if (dFinalMoonChance < 0.5) { dFinalMoonChance = 0.8; }
                    else { dFinalMoonChance = 1.0; }
                }

                if (random.NextPick(dFinalMoonChance))
                {
                    var moon = CreateCelestialBody(star, star.Planets[prevPlanet], false, true);
                    star.Planets[prevPlanet].Moons.Add(moon);
                }
                else
                {
                    var planet = CreateCelestialBody(star, null, GetPlanetIsGasGiant(), false);
                    star.Planets.Add(planet);
                    ++prevPlanet;
                }
            }

            CreatePlanetOrbits(star);
            SelectPlanetThemes(star);
            SetPlanetProperties(star);
        }

        /// <summary>Method to create a celestial body entity</summary>
        /// <param name="star">Host star of the celestial body</param>
        /// <param name="host">Host planet for moons</param>
        /// <param name="bGasGiant">Is celestial body a gas giant</param>
        /// <param name="bIsMoon">Is celestial body a moon</param>
        /// <returns>Celestial body entity</returns>
        private GSPlanet CreateCelestialBody(GSStar star, GSPlanet host, bool bGasGiant, bool bIsMoon)
        {
            int radius = bIsMoon ? GetMoonSize(star, host.Radius, (host.Scale == 10f)) : GetPlanetSize();
            if (bGasGiant) { radius = random.Next(80, 161); }

            string name = bIsMoon ? star.Name + "-Moon" : star.Name + "-Planet";

            var planet = new GSPlanet(name, null, radius, -1, -1, -1, -1, -1, -1, -1, -1);
            if (bGasGiant) { planet.Scale = 10f; }
            else { planet.Scale = 1f; }

            return planet;
        }

        // ///////////////////// PLANET COUNT & SIZES ////////////////////// //

        /// <summary>Method for deterimining the number of planetary bodies in a star's planetary system</summary>
        /// <returns>Number of planets</returns>
        private int GetPlanetCount()
        {
            var min = GetMinPlanetCount();
            var max = GetMaxPlanetCount();
            var result = ClampedNormal(min, max, GetPlanetCountBias());
            return result;
        }

        /// <summary>Method for determining planetary object's size</summary>
        /// <returns>Planetary object's size</returns>
        private int GetPlanetSize()
        {
            var min = GetMinPlanetSize();
            var max = GetMaxPlanetSize();
            var bias = GetPlanetSizeBias();
            int size = ClampedNormalSize(min, max, bias);
            size = Mathf.RoundToInt(size / 10f) * 10; // size step = 10
            return size;
        }

        /// <summary>Method for generating moon's size</summary>
        /// <param name="star">Parent star</param>
        /// <param name="hostRadius">Radius of parent planet</param>
        /// <param name="hostGas">TRUE if parent planet is a gas giant</param>
        /// <returns>Moon's size</returns>
        private int GetMoonSize(GSStar star, int hostRadius, bool hostGas)
        {
            int size = GetPlanetSize();
            int trueHostRadius = hostGas ? hostRadius * 10 : hostRadius;
            if (size > trueHostRadius)
            {
                size = trueHostRadius - 20;
            }

            bool bSmallMoons = preferences.GetBool("moonsAreSmall", false);
            bool bSmallGasGiantMoons = preferences.GetBool("smallGasGiantMoons", false);

            if (bSmallMoons && (!hostGas || bSmallGasGiantMoons))
            {
                size /= 2;
            }
            return size;
        }

        /// <summary>Method for determining if planetary object is a gas giant</summary>
        /// <returns>TRUE if planetary object is a gas giant</returns>
        private bool GetPlanetIsGasGiant()
        {
            var gasChance = GetGasChanceGiant();
            return random.NextPick(gasChance);
        }

        // ///////////////////////// PLANET ORBITS ///////////////////////// //

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
            return 0.05f + random.NextFloat(0f, 0.02f);
        }

        // ///////////////////////// PLANET THEMES ///////////////////////// //

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

        // /////////////////////// PLANET PROPERTIES /////////////////////// //

        /// <summary>Method for setting parameters for planetary objects</summary>
        /// <param name="star">Parent star</param>
        private void SetPlanetProperties(GSStar star)
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
        
        /// <summary>Method to apply luminosity boost to blue / white stars</summary>
        private void BoostBlueStarLuminosity()
        {
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
        }

    }
}