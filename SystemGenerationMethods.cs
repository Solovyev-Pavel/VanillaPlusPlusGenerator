using System;
using System.Collections.Generic;
using UnityEngine;
using static GalacticScale.RomanNumbers;
using static GalacticScale.GS2;

namespace GalacticScale.Generators
{

    public partial class VanillaPlusPlusGenerator : iConfigurableGenerator
    {

        // suffixes for moon names. since generator allows only 10 bodies per system max, no need to provide more letters than this
        static readonly string[] moonLetters = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
        // lists of possible companion stars for binary systems
        static readonly Dictionary<EStar, List<EStar>> companionTypes = new Dictionary<EStar, List<EStar>>
        {
            { EStar.M, new List<EStar> { EStar.M, EStar.WhiteDwarf } },
            { EStar.K, new List<EStar> { EStar.M, EStar.WhiteDwarf } },
            { EStar.G, new List<EStar> { EStar.M, EStar.K, EStar.WhiteDwarf } },
            { EStar.F, new List<EStar> { EStar.M, EStar.K, EStar.WhiteDwarf } },
            { EStar.A, new List<EStar> { EStar.M, EStar.NeutronStar, EStar.WhiteDwarf } },
            { EStar.B, new List<EStar> { EStar.M, EStar.K, EStar.F, EStar.NeutronStar, EStar.WhiteDwarf } },
            { EStar.O, new List<EStar> { EStar.M, EStar.K, EStar.F, EStar.A, EStar.NeutronStar, EStar.WhiteDwarf } },
            { EStar.RedGiant, new List<EStar> { EStar.M, EStar.K, EStar.F, EStar.A, EStar.BlackHole } },
            { EStar.YellowGiant, new List<EStar> { EStar.M, EStar.K, EStar.G, EStar.F } },
            { EStar.WhiteGiant, new List<EStar> { EStar.M, EStar.K, EStar.G, EStar.F, EStar.NeutronStar, EStar.WhiteDwarf, EStar.BlackHole } },
            { EStar.BlueGiant, new List<EStar> { EStar.M, EStar.K, EStar.F, EStar.A, EStar.NeutronStar, EStar.WhiteDwarf } },
            { EStar.NeutronStar, new List<EStar> { EStar.NeutronStar, EStar.WhiteDwarf } },
            { EStar.BlackHole, new List<EStar> { EStar.NeutronStar, EStar.BlackHole } }
        };

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
                float multiplier = (starType == EStarType.GiantStar) ? (6.25f - Mathf.Pow(luminosity, 0.25f)) : 1.0f;

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

        // //////////////////// STAR SYSTEM MODIFICATION /////////////////// //

        /// <summary>Method to create a companion star (or two) within a given system</summary>
        /// <param name="star">Target parent star</param>
        private void CreateMultistarSystem(GSStar star)
        {
            if (star.genData.Get("hasBinary", new Val(false)).Bool(false) || star.genData.Get("binary", new Val(false)).Bool(false)) { return; }

            double chance = preferences.GetDouble("binaryStarChance", 25) * 0.01;
            bool bDreamStatringSystem = preferences.GetBool("dreamSystem", false);
            bool bIsBrightStar = IsBrightStar(star);

            // - brighter stars are more likely to have companion stars
            // - black holes and neutron stars, on the other hand, are far less likely to have a companion
            // - white dwarves will never have companions
            if (bIsBrightStar)
                chance = Math.Min(chance * 1.5, 1.0);
            else if (star.Type == EStarType.NeutronStar || star.Type == EStarType.BlackHole)
                chance *= 0.33;
            else if (star.Type == EStarType.WhiteDwarf)
                chance = 0;

            // dream start + bright star == very high chance of companion
            if (bDreamStatringSystem && star == birthStar)
                chance = Math.Min(chance + 0.5, 1.0);

            if (random.NextPick(chance))
            {
                (EStarType, ESpectrType) companionType = ConvertEStarValue(ChooseCompanionStarType(star));

                // dream start with a bright home star has a very high chance of having black hole or neutron star as companion
                if (bDreamStatringSystem && star == birthStar && random.NextPick(0.5))
                {
                    if (random.NextPick(0.5))
                        companionType = (EStarType.NeutronStar, ESpectrType.X);
                    else
                        companionType = (EStarType.BlackHole, ESpectrType.X);
                }

                var binaryCompanion = GSSettings.Stars.Add(new GSStar(random.Next(), star.Name + "-B", companionType.Item2, companionType.Item1, new GSPlanets()));
                binaryCompanion.genData.Add("binary", true);
                star.genData.Add("hasBinary", true);
                star.BinaryCompanion = binaryCompanion.Name;

                if (star.Type == EStarType.GiantStar)
                    binaryCompanion.radius = StarDefaults.Radius(binaryCompanion) * 1.5f;
                else
                    binaryCompanion.radius = StarDefaults.Radius(binaryCompanion) * 0.75f;

                binaryCompanion.Decorative = true;
                var binaryOffset = (star.RadiusLY + binaryCompanion.RadiusLY) * random.NextFloat(4.0f, 6.0f);
                if (star.Type == EStarType.GiantStar)
                    binaryOffset *= 0.66f;
                star.genData.Add("binaryOffset", binaryOffset);
                binaryCompanion.position = new VectorLF3(binaryOffset, 0, 0);
                star.luminosity += binaryCompanion.luminosity;
                binaryCompanion.luminosity = 0;

                if (random.NextPick(0.5) && binaryCompanion.Spectr != ESpectrType.X)
                {
                    var trinaryCompanion = GSSettings.Stars.Add(new GSStar(random.Next(), star.Name + "-C", companionType.Item2, companionType.Item1, new GSPlanets()));
                    trinaryCompanion.genData.Add("binary", true);
                    binaryCompanion.genData.Add("hasBinary", true);
                    binaryCompanion.BinaryCompanion = trinaryCompanion.Name;
                    trinaryCompanion.radius = binaryCompanion.radius;
                    trinaryCompanion.Decorative = true;

                    double angle = random.NextDouble(Math.PI / 2, 3 * Math.PI / 2);
                    double sin = Math.Sin(angle);
                    double cos = Math.Cos(angle);
                    var trinaryOffset = (star.RadiusLY + binaryCompanion.RadiusLY) * random.NextFloat(4.0f, 6.0f);
                    if (star.Type == EStarType.GiantStar)
                        trinaryOffset *= 0.66f;
                    binaryCompanion.genData.Add("binaryOffset", trinaryOffset);
                    trinaryCompanion.position = new VectorLF3(-binaryOffset + trinaryOffset * cos, 0, trinaryOffset * sin);

                    star.luminosity += trinaryCompanion.luminosity;
                    trinaryCompanion.luminosity = 0;
                }
            }
        }

        /// <summary>Method to pick the type of a companion star in a binary system</summary>
        /// <param name="star">Primary star</param>
        private EStar ChooseCompanionStarType(GSStar star)
        {
            // choosing a companion for a black hole
            if (star.Type == EStarType.BlackHole)
            {
                return random.Item(companionTypes[EStar.BlackHole]);
            }
            // choosing a companion for a neutron star
            else if (star.Type == EStarType.NeutronStar)
            {
                return random.Item(companionTypes[EStar.NeutronStar]);
            }
            // choosing a companion star for a binary with a normal star as primary
            else if (star.Type == EStarType.MainSeqStar)
            {
                switch (star.Spectr)
                {
                    case ESpectrType.M: return random.Item(companionTypes[EStar.M]);
                    case ESpectrType.K: return random.Item(companionTypes[EStar.K]);
                    case ESpectrType.G: return random.Item(companionTypes[EStar.G]);
                    case ESpectrType.F: return random.Item(companionTypes[EStar.F]);
                    case ESpectrType.A: return random.Item(companionTypes[EStar.A]);
                    case ESpectrType.B: return random.Item(companionTypes[EStar.B]);
                    case ESpectrType.O: return random.Item(companionTypes[EStar.O]);
                    default: return EStar.M;
                }
            }
            // choosing a companion star for a binary with a giant star as primary
            else if (star.Type == EStarType.GiantStar)
            {
                switch (star.Spectr)
                {
                    case ESpectrType.M:
                    case ESpectrType.K: return random.Item(companionTypes[EStar.RedGiant]);
                    case ESpectrType.G: return random.Item(companionTypes[EStar.YellowGiant]);
                    case ESpectrType.F:
                    case ESpectrType.A: return random.Item(companionTypes[EStar.WhiteGiant]);
                    case ESpectrType.B:
                    case ESpectrType.O: return random.Item(companionTypes[EStar.BlueGiant]);
                    default: return EStar.M;
                }
            }
            // fallback in case we somehow got there. M-types are available as companions to any kind of star
            else
            {
                return EStar.M;
            }
        }

        /// <summary>Method for converting EStar enum value to (EStarType, ESpectrType) pair</summary>
        private (EStarType, ESpectrType) ConvertEStarValue(EStar type)
        {
            switch (type)
            {
                case EStar.M: return (EStarType.MainSeqStar, ESpectrType.M);
                case EStar.K: return (EStarType.MainSeqStar, ESpectrType.K);
                case EStar.G: return (EStarType.MainSeqStar, ESpectrType.G);
                case EStar.F: return (EStarType.MainSeqStar, ESpectrType.F);
                case EStar.A: return (EStarType.MainSeqStar, ESpectrType.A);
                case EStar.B: return (EStarType.MainSeqStar, ESpectrType.B);
                case EStar.O: return (EStarType.MainSeqStar, ESpectrType.O);
                case EStar.RedGiant: return (EStarType.GiantStar, ESpectrType.M);
                case EStar.YellowGiant: return (EStarType.GiantStar, ESpectrType.G);
                case EStar.WhiteGiant: return (EStarType.GiantStar, ESpectrType.A);
                case EStar.BlueGiant: return (EStarType.GiantStar, ESpectrType.O);
                case EStar.WhiteDwarf: return (EStarType.WhiteDwarf, ESpectrType.X);
                case EStar.NeutronStar: return (EStarType.NeutronStar, ESpectrType.X);
                case EStar.BlackHole: return (EStarType.BlackHole, ESpectrType.X);
                default: return (EStarType.MainSeqStar, ESpectrType.M);
            }
        }

        // ///////////////////// HOME SYSTEM GENERATION //////////////////// //

        /// <summary>Method for generating the starting system</summary>
        private void GenerateStartingSystem()
        {
            Log("Generating staring system");
            // generate random starting star, exclude black holes, neutron stars and white dwarfs
            var starType = ChooseStarType();
            if (starType.spectr == ESpectrType.X)
            {
                starType.spectr = ESpectrType.G;
                starType.type = EStarType.MainSeqStar;
            }

            // user-preferred starting star type
            var requestedType = preferences.GetString("startingSystemType", "Random");
            if (requestedType == "M")
            {
                starType.spectr = ESpectrType.M;
                starType.type = EStarType.MainSeqStar;
            }
            else if (requestedType == "K")
            {
                starType.spectr = ESpectrType.K;
                starType.type = EStarType.MainSeqStar;
            }
            else if (requestedType == "G")
            {
                starType.spectr = ESpectrType.G;
                starType.type = EStarType.MainSeqStar;
            }
            else if (requestedType == "F")
            {
                starType.spectr = ESpectrType.F;
                starType.type = EStarType.MainSeqStar;
            }
            else if (requestedType == "A")
            {
                starType.spectr = ESpectrType.A;
                starType.type = EStarType.MainSeqStar;
            }
            else if (requestedType == "B")
            {
                starType.spectr = ESpectrType.B;
                starType.type = EStarType.MainSeqStar;
            }
            else if (requestedType == "O")
            {
                starType.spectr = ESpectrType.O;
                starType.type = EStarType.MainSeqStar;
            }
            else if (requestedType == "RedGiant")
            {
                starType.spectr = ESpectrType.K;
                starType.type = EStarType.GiantStar;
            }
            else if (requestedType == "YellowGiant")
            {
                starType.spectr = ESpectrType.G;
                starType.type = EStarType.GiantStar;
            }
            else if (requestedType == "WhiteGiant")
            {
                starType.spectr = ESpectrType.A;
                starType.type = EStarType.GiantStar;
            }
            else if (requestedType == "BlueGiant")
            {
                starType.spectr = ESpectrType.O;
                starType.type = EStarType.GiantStar;
            }
            else if (requestedType == "WhiteDwarf")
            {
                starType.spectr = ESpectrType.X;
                starType.type = EStarType.WhiteDwarf;
            }
            else if (requestedType == "NeutronStar")
            {
                starType.spectr = ESpectrType.X;
                starType.type = EStarType.NeutronStar;
            }
            else if (requestedType == "BlackHole")
            {
                starType.spectr = ESpectrType.X;
                starType.type = EStarType.BlackHole;
            }

            var star = new GSStar(random.Next(), SystemNames.GetName(0), starType.spectr, starType.type, new GSPlanets());
            GSSettings.Stars.Add(star);
            birthStar = star;

            bool bDreamStarterSystem = preferences.GetBool("dreamSystem", false);
            // normal starter system generation
            if (!bDreamStarterSystem)
            {
                int planetCount = GetPlanetCount();
                if (planetCount < 3) { planetCount = 3; }
                GeneratePlanetsForStar(birthStar, planetCount);
                // find a habitable planet in the system
                FindBirthPlanet(birthStar);
            }
            // "dream" starter system generation
            else
            {
                GenerateDreamStartingSystemPlanets(birthStar);
            }

            // ensure all bodies have proper orbital periods
            EnsureProperOrbitalPeriods(star);

            // adjust starting system
            SetBirthPlanetTheme();
            SetBirthPlanetSize();
            EnsureBirthSystemHasTi();
            EnsureBirthPlanetResources();
            EnsureBirthSystemBodies();

            Log($"Finished generating starting system. Starting system is {birthStar.Name}");
        }

        /// <summary>Method for determining staring planet. Or creating one if system has none</summary>
        /// <param name="star">Target star</param>
        private void FindBirthPlanet(GSStar star)
        {
            // look for a habitable planet or moon
            birthPlanet = null;
            foreach (var planet in birthStar.Planets)
            {
                if (planet.IsHabitable)
                {
                    birthPlanet = planet;
                    GSSettings.BirthPlanetName = birthPlanet.Name;
                    birthPlanetIsMoon = false;
                    birthPlanetHost = null;
                    return;
                }

                foreach (var moon in planet.Moons)
                {
                    if (moon.IsHabitable)
                    {
                        birthPlanet = moon;
                        GSSettings.BirthPlanetName = birthPlanet.Name;
                        birthPlanetIsMoon = true;
                        birthPlanetHost = planet;
                        return;
                    }
                }
            }

            // no habitable planets or moons in the system, we need to make one
            if (birthPlanet == null)
            {
                Log("Starting system generated with no habitable planets. Creating one by overwriting an existing planet.");

                var index = Convert.ToInt32(Math.Floor(star.Planets.Count / 2.0));
                var planet = star.Planets[index];

                // if this is a gas giant, try one of its moons
                if (planet.Scale == 10f)
                {
                    if (planet.Moons.Count > 0)
                    {
                        birthPlanet = random.Item(planet.Moons); ;
                        GSSettings.BirthPlanetName = birthPlanet.Name;
                        birthPlanetIsMoon = true;
                        birthPlanetHost = planet;
                    }
                    else
                    {
                        var moon = CreateCelestialBody(star, planet, true, true);
                        moon.Name = planet.Name + " - " + moonLetters[0];
                        moon.OrbitRadius = GetMoonOrbit();
                        moon.OrbitalPeriod = Utils.CalculateOrbitPeriod(moon.OrbitRadius);
                        moon.RotationPhase = random.Next(360);
                        moon.OrbitInclination = random.NextFloat(-20.0f, 20.0f);
                        moon.OrbitPhase = random.Next(360);
                        moon.Obliquity = random.NextFloat() * 20;
                        moon.RotationPeriod = random.Next(360, 1800);
                        planet.Moons.Add(moon);

                        birthPlanet = moon;
                        GSSettings.BirthPlanetName = birthPlanet.Name;
                        birthPlanetIsMoon = true;
                        birthPlanetHost = planet;
                    }
                }
                else
                {
                    birthPlanet = planet;
                    GSSettings.BirthPlanetName = birthPlanet.Name;
                    birthPlanetIsMoon = false;
                    birthPlanetHost = null;
                }

                var themeNames = GSSettings.ThemeLibrary.Habitable;
                var themeName = "Mediterranean";
                if (themeNames.Count > 0) { themeName = random.Item(themeNames); }
                birthPlanet.Theme = themeName;

                Log($"Staring planet is {birthPlanet.Name} of type {themeName}");
            }
        }

        // //////////////////////////// METHODS //////////////////////////// //

        /// <summary>Method for generating planetary objects of a star</summary>
        /// <param name="star">Parent star</param>
        private void GeneratePlanetsForStar(GSStar star)
        {
            int planetCount = GetPlanetCount(); // always 1 or more
            GeneratePlanetsForStar(star, planetCount);
        }

        /// <summary>Method for generating planetary objects of a star</summary>
        /// <param name="star">Parent star</param>
        /// <param name="planetCount">Number of planetary objects around the star</param>
        private void GeneratePlanetsForStar(GSStar star, int planetCount)
        {
            star.Planets = new GSPlanets();
            var moonChance = GetMoonChance();
            bool bGasGiantMoons = preferences.GetBool("moreLikelyGasGiantMoons", false);

            var firstPlanet = CreateCelestialBody(star, null, GetPlanetIsGasGiant(), false);
            star.Planets.Add(firstPlanet);
            int prevPlanetIndex = 0;

            for (int i = 1; i < planetCount; i++)
            {
                bool bPrevPlanetIsGasGiant = (star.Planets[prevPlanetIndex].Scale == 10f);
                bool bAlreadyHasMoons = (star.Planets[prevPlanetIndex].Moons.Count > 0);
                double dFinalMoonChance = moonChance;
                if (bPrevPlanetIsGasGiant && bGasGiantMoons && !bAlreadyHasMoons)
                {
                    if (dFinalMoonChance < 0.5) { dFinalMoonChance = 0.8; }
                    else { dFinalMoonChance = 1.0; }
                }

                if (random.NextPick(dFinalMoonChance))
                {
                    var moon = CreateCelestialBody(star, star.Planets[prevPlanetIndex], false, true);
                    star.Planets[prevPlanetIndex].Moons.Add(moon);
                }
                else
                {
                    var planet = CreateCelestialBody(star, null, GetPlanetIsGasGiant(), false);
                    star.Planets.Add(planet);
                    ++prevPlanetIndex;
                }
            }

            // ensure that there is at least one telluric body in the system
            int iTelluricCount = 0;
            for (int i = 0; i < star.Planets.Count; ++i)
            {
                if (star.Planets[i].Scale != 10f)
                {
                    ++iTelluricCount;
                }
                if (star.Planets[i].Moons.Count > 0)
                {
                    iTelluricCount += star.Planets[i].Moons.Count; // all moons in Vanilla++ are telluric
                }
            }
            // if normal generation didn't spawn any telluric bodies, add one as a moon to the first gas giant
            if (iTelluricCount == 0)
            {
                var moon = CreateCelestialBody(star, star.Planets[0], false, true);
                star.Planets[0].Moons.Add(moon);
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
            int radius = bIsMoon ? GetMoonSize(host.Radius, (host.Scale == 10f)) : GetPlanetSize();
            if (bGasGiant)
            {
                bool bHugeGasGiants = preferences.GetBool("hugeGasGiants", false);
                if (bHugeGasGiants)
                    radius = random.Next(20, 41) * 4; // 800 + 40N final size
                else
                    radius = 80; // 800 always
            }

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
            FloatPair planetCount = preferences.GetFloatFloat("planetCount");
            var min = Math.Max(Convert.ToInt32(planetCount.low), 1);
            var max = Math.Max(Convert.ToInt32(planetCount.high), 1);
            var result = ClampedNormal(min, max, GetPlanetCountBias());
            return result;
        }

        /// <summary>Method for determining planetary object's size</summary>
        /// <returns>Planetary object's size</returns>
        private int GetPlanetSize()
        {
            FloatPair planetSize = preferences.GetFloatFloat("planetSize");
            var min = Convert.ToInt32(planetSize.low);
            var max = Convert.ToInt32(planetSize.high);
            var bias = GetPlanetSizeBias();
            int size = ClampedNormalSize(min, max, bias);
            size = Mathf.RoundToInt(size / 10f) * 10; // size step = 10
            return size;
        }

        /// <summary>Method for generating moon's size</summary>
        /// <param name="hostRadius">Radius of parent planet</param>
        /// <param name="hostGas">TRUE if parent planet is a gas giant</param>
        /// <returns>Moon's size</returns>
        private int GetMoonSize(int hostRadius, bool hostGas)
        {
            int size = GetPlanetSize();
            int trueHostRadius = hostGas ? hostRadius * 10 : hostRadius;
            if (size > trueHostRadius)
            {
                size = trueHostRadius;
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
            // offset in case of binary system
            float fBinaryOffset = star.genData.Get("binaryOffset", new Val(0f)).Float(0f) * 60f; // 1LY = 60 AU

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
                            planet.OrbitRadius = Mathf.Max(fBinaryOffset * 2.0f, star.RadiusAU * 2.0f, random.NextFloat(sz.warmZoneEdge * 0.5f, sz.warmZoneEdge)) + planet.SystemRadius;
                        }
                        else
                        {
                            planet.OrbitRadius = Mathf.Max(fBinaryOffset * 2.0f, star.RadiusAU * 3.0f, random.NextFloat(0.7f, 0.9f) * sz.temperateZoneEdge) + planet.SystemRadius;
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
                            planet.OrbitRadius = Mathf.Max(fBinaryOffset * 2.0f, star.RadiusAU * 1.25f, random.NextFloat(sz.warmZoneEdge * 0.5f, sz.warmZoneEdge)) + planet.SystemRadius;
                        }
                        else
                        {
                            planet.OrbitRadius = Mathf.Max(fBinaryOffset * 2.0f, star.RadiusAU * 3.0f, random.NextFloat(0.7f, 0.85f) * (sz.coldZoneEdge - sz.temperateZoneEdge) + star.RadiusAU * 0.25f) + planet.SystemRadius;
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
                            planet.OrbitRadius = Mathf.Max(fBinaryOffset * 2.0f, random.NextFloat(0.25f, 0.45f));
                        }
                        else
                        {
                            planet.OrbitRadius = Mathf.Max(fBinaryOffset * 2.0f, random.NextFloat(0.7f, 0.85f));
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
            return 0.06f + random.NextFloat(0f, 0.02f);
        }

        /// <summary>Method to ensure all planets/moons of a star have proper orbital periods</summary>
        /// <param name="star">Target star</param>
        private void EnsureProperOrbitalPeriods(GSStar star)
        {
            foreach (var planet in star.Planets)
            {
                if (planet.OrbitalPeriod == 1000f)
                    planet.OrbitalPeriod = Utils.CalculateOrbitPeriod(planet.OrbitRadius);

                foreach (var moon in planet.Moons)
                {
                    if (moon.OrbitalPeriod == 1000f)
                        moon.OrbitalPeriod = Utils.CalculateOrbitPeriod(moon.OrbitRadius);
                }
            }
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
                body.OrbitLongitude = random.NextFloat(0f, 359.5f);
                body.OrbitPhase = random.Next(360);
                body.Obliquity = random.NextFloat() * 20;
                body.RotationPeriod = random.Next(80, 3600);

                if (body.OrbitRadius < 1f && random.NextDouble() < 0.15) // Tidal Lock
                {
                    //Log($"object {body.Name} is tidally locked!");
                    body.RotationPeriod = body.OrbitalPeriod;
                }
                else if (random.NextDouble() < 0.15) // 1:2 Resonance
                {
                    //Log($"object {body.Name} has 1:2 orbital resonanse!");
                    body.RotationPeriod = Convert.ToInt32(body.OrbitalPeriod / 2);
                    body.OrbitalPeriod = body.RotationPeriod * 2;
                }
                else if (random.NextDouble() < 0.15) // 1:4 Resonance
                {
                    //Log($"object {body.Name} has 1:4 orbital resonanse!");
                    body.RotationPeriod = Convert.ToInt32(body.OrbitalPeriod / 4);
                    body.OrbitalPeriod = body.RotationPeriod * 4;
                }
                else if (random.NextDouble() < 0.15) // Reverse Rotation
                {
                    //Log($"object {body.Name} has reverse rotation!");
                    body.RotationPeriod = -1 * Mathf.Abs(body.RotationPeriod);
                }

                if (random.NextFloat() < 0.1f) // Crazy Obliquity
                    body.Obliquity = random.NextFloat(20f, 90f);

                // tidal-lock innermost planet if preference is set
                if (body == star.Planets[0] && preferences.GetBool("tidalLockInnerPlanets", false))
                {
                    body.RotationPeriod = body.OrbitalPeriod;
                }

                // rare resource chance
                if (body != birthPlanet)
                    body.rareChance = preferences.GetFloat("rareChance", 15) * 0.01f;
            }
        }

        /// <summary>Method for setting more 'realistic' solar power levels for planets</summary>
        private void SetPlanetSolarPowerLevels()
        {
            foreach (var star in GSSettings.Stars)
            {
                SystemZones sz = new SystemZones(star.luminosity, star.Type, star.Spectr);
                float baseDist = (sz.temperateZoneEdge + sz.coldZoneEdge) / 2f;

                foreach (var planet in star.Planets)
                {
                    float planetSolarLevel = Mathf.Pow((baseDist / planet.OrbitRadius), 1.75f);
                    planetSolarLevel = Mathf.Max(Mathf.Min(5f, planetSolarLevel), 0.33f);

                    if (planet.Scale != 10f) // gas giants don't need solar power values
                        planet.Luminosity = planetSolarLevel;

                    foreach (var moon in planet.Moons)
                        moon.Luminosity = planetSolarLevel;
                }
            }
        }

        // //////////////////////// VARIOUS METHODS //////////////////////// //

        /// <summary>Utility method for determining whether a star is bright enough to get good boost</summary>
        /// <param name="star">Target star</param>
        bool IsBrightStar(GSStar star)
        {
            if (star.luminosity < 1) { return false; } // also cuts away companion stars since they have lum = 0
            switch (star.Spectr)
            {
                case ESpectrType.O:
                case ESpectrType.B:
                case ESpectrType.A:
                case ESpectrType.F: return true;
                default: return false;
            }
        }

        /// <summary>Method to apply luminosity boost to blue / white stars</summary>
        // formulas have been adjusted using : https://www.desmos.com/calculator/owamhrm8e6
        private void BoostBlueStarLuminosity()
        {
            bool bLuminosityBoost = preferences.GetBool("luminosityBoost", false);
            bool bExponentialBoost = preferences.GetBool("luminosityExponentialBoost", false);
            float coeff = 1.0f;

            if (!bLuminosityBoost && !bExponentialBoost) { return; }

            if (bExponentialBoost)
                coeff = preferences.GetFloat("exponenitalBoostCoefficient", 1f);
            else if (bLuminosityBoost)
                coeff = preferences.GetFloat("linearBoostCoefficient", 1f);

            // iterate through stars to see which ones need a boost
            foreach (var star in GSSettings.Stars)
            {
                if (IsBrightStar(star))
                {
                    // actual in-game luminosity if roughly proportional to the cubic root of star.luminosity
                    float dysonLuminosity = Mathf.Pow(star.luminosity, 0.33f);

                    if (bExponentialBoost)
                        dysonLuminosity = Mathf.Pow(dysonLuminosity, coeff);
                    else if (bLuminosityBoost)
                        dysonLuminosity = (dysonLuminosity - 1) * coeff + 1;

                    star.luminosity = Mathf.Pow(dysonLuminosity, 3);
                }
            }

        }
    }
}
