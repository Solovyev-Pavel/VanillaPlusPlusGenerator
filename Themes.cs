using System.Collections.Generic;
using UnityEngine;

namespace GalacticScale.Generators
{
    public partial class VanillaPlusPlusGenerator : iConfigurableGenerator
    {
        public static string[] baseKeys;

        /// <summary>Base planet theme setup</summary>
        public void SetupBaseThemes()
        {
            var newLibrary = new ThemeLibrary();
            foreach (var v in ThemeLibrary.Vanilla())
            {
                var clone = v.Value.Clone();
                newLibrary.Add(v.Key, clone);
            }

            baseKeys = new string[newLibrary.Keys.Count];
            newLibrary.Keys.CopyTo(baseKeys, 0);

            var smolLibrary = new ThemeLibrary();
            var keys = new string[newLibrary.Count + 1];
            newLibrary.Keys.CopyTo(keys, 0);
            for (var i = 0; i < newLibrary.Count; i++)
            {
                var key = keys[i];
                var theme = newLibrary[key];
                if (theme.PlanetType == EPlanetType.Ocean) theme.MinRadius = 50;
                if (theme.PlanetType != EPlanetType.Gas && theme.PlanetType != EPlanetType.Ocean)
                {
                    //For rocky worlds
                    var smolTheme = theme.Clone();
                    smolTheme.DisplayName = theme.DisplayName;
                    smolTheme.Name += "smol";
                    smolLibrary.Add(smolTheme.Name, smolTheme);
                    smolTheme.MaxRadius = 40;
                    theme.MinRadius = 50;
                    if (theme.PlanetType == EPlanetType.Vocano)
                    {
                        theme.TerrainSettings.BrightnessFix = true;
                        smolTheme.TerrainSettings.BrightnessFix = true;
                        theme.Init();
                        smolTheme.Init();
                    }

                    smolTheme.atmosphereMaterial.Params["_Intensity"] = 0f;
                }

                theme.CustomGeneration = true;
                theme.VeinSettings.Algorithm = "GS2";
                if (theme.Algo == 7)
                    theme.VeinSettings.Algorithm = "GS2W";
            }

            foreach (var s in smolLibrary)
                if (!newLibrary.ContainsKey(s.Key)) newLibrary.Add(s.Key, s.Value);
                else newLibrary[s.Key] = s.Value;
            GSSettings.ThemeLibrary = newLibrary;
        }

        /// <summary>Planet themes initialization</summary>
        public static void InitThemes()
        {
            GSSettings.ThemeLibrary.AddRange(GS2.externalThemes);
        }
    }
}