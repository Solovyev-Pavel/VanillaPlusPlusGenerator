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

        // //////////////////// STAR SYSTEM MODIFICATION /////////////////// //

        /// <summary>Method to create a companion star (or two) within a given system</summary>
        /// <param name="star">Target parent star</param>
        private void CreateMultistarSystem(GSStar star)
        {
            if (star.genData.Get("hasBinary", new Val(false)).Bool(false) || star.genData.Get("binary", new Val(false).Bool(false))) { return; }

            double chance = preferences.GetDouble("binaryStarChance", 25) * 0.01;
            bool bDreamStatringSystem = preferences.GetBool("dreamSystem", false);
            bool bIsBrightStar = (star.Spectr == ESpectrType.A || star.Spectr == ESpectrType.B || star.Spectr == ESpectrType.O);

            // black holes and neutron stars have far lower chance of having a companion, white dwarves never have one
            if (star.Type == EStarType.NeutronStar || star.Type == EStarType.BlackHole)
                chance *= 0.2;
            else if (star.Type == EStarType.WhiteDwarf)
                chance = 0;

            // dream start + bright star == very high chance of companion
            if (bDreamStatringSystem && bIsBrightStar)
                chance = Math.Max(chance + 0.5, 1.0);

            if (random.NextPick(chance))
            {
                (EStarType, ESpectrType) companionType = ConvertEStarValue(ChooseCompanionStarType(star));

                // dream start with a bright home star has a very high chance of having black hole or neutron star as companion
                if (bDreamStatringSystem && bIsBrightStar && random.NextPick(0.5))
                {
                    if (random.NextPick(0.5))
                        companionType = (EStarType.NeutronStar, ESpectrType.X);
                    else
                        companionType = (EStarType.BlackHole, ESpectrType.X);
                }

                var binary = GSSettings.Stars.Add(new GSStar(random.Next(), star.Name + "-B", companionType.Item2, companionType.Item1, new GSPlanets()));
                binary.genData.Add("binary", true);
                star.genData.Add("hasBinary", true);
                star.BinaryCompanion = binary.Name;
                binary.radius = StarDefaults.Radius(binary) * 0.75f;
                binary.Decorative = true;
                var offset = (star.RadiusLY + binary.RadiusLY) * random.NextFloat(4.0f, 6.0f);
                star.genData.Add("binaryOffset", offset);
                binary.position = new VectorLF3(offset, 0, 0);
                star.luminosity += binary.luminosity;
                binary.luminosity = 0;

                if (random.NextPick(0.5) && binary.Spectr != ESpectrType.X)
                {
                    var trinary = GSSettings.Stars.Add(new GSStar(random.Next(), star.Name + "-C", companionType.Item2, companionType.Item1, new GSPlanets()));
                    trinary.genData.Add("binary", true);
                    binary.genData.Add("hasBinary", true);
                    binary.BinaryCompanion = trinary.Name;
                    trinary.radius = binary.radius;
                    trinary.Decorative = true;

                    double angle = random.NextDouble(Math.PI / 2, 3 * Math.PI / 2);
                    double sin = Math.Sin(angle);
                    double cos = Math.Cos(angle);
                    binary.genData.Add("binaryOffset", 2* offset);
                    trinary.position = new VectorLF3(-offset + offset * cos, 0, offset * sin);

                    star.luminosity += trinary.luminosity;
                    trinary.luminosity = 0;
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
            EnsureBirthPlanetResources();
            EnsureBirthSystemBodies();
            EnsureBirthSystemHasTi();

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

                var index = Convert.ToInt32(Math.Floor((star.PlanetCount + 1) / 2.0));
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

        /// <summary>Method that generates "dream" starting system (not balanced)</summary>
        /// <param name="star">Parent star</param>
        private void GenerateDreamStartingSystemPlanets(GSStar star)
        {
            star.Planets = new GSPlanets();

            // planet #1 : lava or hot obsidian or molten oasis
            List<string> firstPlanetThemes = new List<string>() { "Lava" };
            if (GS2.externalThemes.ContainsKey("HotObsidian")) { firstPlanetThemes.Add("HotObsidian"); }
            if (GS2.externalThemes.ContainsKey("MoltenOasis")) { firstPlanetThemes.Add("MoltenOasis"); }
            var firstPlanet = CreateCelestialBody(star, null, false, false);
            firstPlanet.Theme = random.Item(firstPlanetThemes);
            star.Planets.Add(firstPlanet);

            // planet #2 : gas giant
            var secondPlanet = CreateCelestialBody(star, null, true, false);
            secondPlanet.Theme = "GasGiant";
            // planet #2, moon #1 : sulfur sea or volcanic ash
            List<string> firstMoonThemes = new List<string>() { "VolcanicAsh" };
            if (GS2.externalThemes.ContainsKey("SulfurSea")) { firstMoonThemes.Add("SulfurSea"); }
            var firstMoon = CreateCelestialBody(star, secondPlanet, false, true);
            firstMoon.Theme = random.Item(firstMoonThemes);
            secondPlanet.Moons.Add(firstMoon);
            // planet #2, moon #2 : <home planet>
            List<string> secondMoonThemes = new List<string>() { "OceanicJungle", "Sakura", "Prairie", "Mediterranean" };
            if (GS2.externalThemes.ContainsKey("Swamp")) { secondMoonThemes.Add("Swamp"); }
            if (GS2.externalThemes.ContainsKey("FloodedMesa")) { secondMoonThemes.Add("FloodedMesa"); }
            var secondMoon = CreateCelestialBody(star, secondPlanet, false, true);
            secondMoon.Theme = random.Item(secondMoonThemes);
            secondPlanet.Moons.Add(secondMoon);
            // planet #2, moon #3 : gobi or arid desert or red stone
            List<string> thirdMoonThemes = new List<string>() { "Gobi", "AridDesert", "RedStone", "Hurricane" };
            var thirdMoon = CreateCelestialBody(star, secondPlanet, false, true);
            thirdMoon.Theme = random.Item(thirdMoonThemes);
            secondPlanet.Moons.Add(thirdMoon);
            star.Planets.Add(secondPlanet);

            // planet #3 : ice giant
            var thirdPlanet = CreateCelestialBody(star, null, true, false);
            thirdPlanet.Theme = "IceGiant";
            // planet #3, moon #1 : frozen forest or ice malusol or ice lake or glacial plates or ice gelisol or frozen comet or barren
            List<string> fourthMoonThemes = new List<string>() { "IceLake", "IceGelisol", "Barren" };
            if (GS2.externalThemes.ContainsKey("FrozenForest")) { secondMoonThemes.Add("FrozenForest"); }
            if (GS2.externalThemes.ContainsKey("IceMalusol")) { secondMoonThemes.Add("IceMalusol"); }
            if (GS2.externalThemes.ContainsKey("GlacialPlates")) { secondMoonThemes.Add("GlacialPlates"); }
            if (GS2.externalThemes.ContainsKey("FrozenComet")) { secondMoonThemes.Add("FrozenComet"); }
            var fourthMoon = CreateCelestialBody(star, thirdPlanet, false, true);
            fourthMoon.Theme = random.Item(fourthMoonThemes);
            thirdPlanet.Moons.Add(fourthMoon);
            star.Planets.Add(thirdPlanet);

            CreatePlanetOrbits(star);
            SetPlanetProperties(star);

            birthPlanet = secondMoon;
            GSSettings.BirthPlanetName = birthPlanet.Name;
            birthPlanetHost = secondPlanet;
            birthPlanetIsMoon = true;

            // ensure optical crystals
            if (!firstPlanet.GsTheme.VeinSettings.VeinTypes.ContainsVein(EVeinType.Grat))
            {
                firstPlanet.GsTheme.VeinSettings.Algorithm = "GS2";
                firstPlanet.GsTheme.CustomGeneration = true;
                firstPlanet.GsTheme.VeinSettings.VeinTypes.Add(GSVeinType.Generate(EVeinType.Grat, 2, 3, 0.6f, 0.6f, 5, 10, true));
            }
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

                // rare resource chance
                float fRareChance = preferences.GetFloat("rareChance", 100) * 0.01f - 1;
                body.rareChance = fRareChance;
            }
        }

        // //////////////////////// VARIOUS METHODS //////////////////////// //

        /// <summary>Method to apply luminosity boost to blue / white stars</summary>
        private void BoostBlueStarLuminosity()
        {
            // actual in-game luminosity if roughly proportional to the cubic root of star.luminosity
            if (preferences.GetBool("luminosityBoost", false))
            {
                foreach (var star in GSSettings.Stars)
                {
                    switch (star.Spectr)
                    {
                        case ESpectrType.F: star.luminosity *= Mathf.Pow(1.2f, 3); break;
                        case ESpectrType.A: star.luminosity *= Mathf.Pow(1.35f, 3); break;
                        case ESpectrType.B: star.luminosity *= Mathf.Pow(1.5f, 3); break;
                        case ESpectrType.O: star.luminosity *= Mathf.Pow(1.65f, 3); break;
                    }
                }
            }
        }
    }
}
