using System.Linq;
using System.Collections.Generic;

namespace GalacticScale.Generators
{

    public partial class VanillaPlusPlusGenerator : iConfigurableGenerator
    {
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

        /// <summary>Generation for star frequences in the cluster</summary>
        /// <returns>dictionary "Type ID"-"Frequency"</returns>
        private Dictionary<string, double> CalculateFrequencies()
        {
            var StarFreqTupleArray = new (string type, double chance)[14];
            var fK = preferences.GetDouble("freqK", 40);
            var fM = preferences.GetDouble("freqM", 50);
            var fG = preferences.GetDouble("freqG", 30);
            var fF = preferences.GetDouble("freqF", 25);
            var fA = preferences.GetDouble("freqA", 10);
            var fB = preferences.GetDouble("freqB", 4);
            var fO = preferences.GetDouble("freqO", 2);
            var fBH = preferences.GetDouble("freqBH", 1);
            var fN = preferences.GetDouble("freqN", 1);
            var fW = preferences.GetDouble("freqW", 2);
            var fRG = preferences.GetDouble("freqRG", 1);
            var fYG = preferences.GetDouble("freqYG", 1);
            var fWG = preferences.GetDouble("freqWG", 1);
            var fBG = preferences.GetDouble("freqBG", 1);
            var total = fK + fM + fG + fF + fA + fB + fO + fBH + fN + fW + fRG + fYG + fWG + fBG;

            StarFreqTupleArray[0] = ("K", fK / total);
            StarFreqTupleArray[1] = ("M", fM / total);
            StarFreqTupleArray[2] = ("G", fG / total);
            StarFreqTupleArray[3] = ("F", fF / total);
            StarFreqTupleArray[4] = ("A", fA / total);
            StarFreqTupleArray[5] = ("B", fB / total);
            StarFreqTupleArray[6] = ("O", fO / total);
            StarFreqTupleArray[7] = ("BH", fBH / total);
            StarFreqTupleArray[8] = ("N", fN / total);
            StarFreqTupleArray[9] = ("W", fW / total);
            StarFreqTupleArray[10] = ("RG", fRG / total);
            StarFreqTupleArray[11] = ("YG", fYG / total);
            StarFreqTupleArray[12] = ("WG", fWG / total);
            StarFreqTupleArray[13] = ("BG", fBG / total);

            starFreq = new Dictionary<string, double>();
            starFreq.Add("K", fK / total);
            for (var i = 1; i < StarFreqTupleArray.Length; i++)
            {
                var element = StarFreqTupleArray[i];
                var previousElement = StarFreqTupleArray[i - 1];
                starFreq.Add(element.type, element.chance + previousElement.chance);
                StarFreqTupleArray[i].chance += previousElement.chance;
            }

            return starFreq;
        }

        /// <summary>Method for randomly chosing star type</summary>
        /// <returns>pair "Star type"-"Star spectral class"</returns>
        private (EStarType type, ESpectrType spectr) ChooseStarType()
        {
            var choice = random.NextDouble();
            var starType = "";
            for (var i = 0; i < starFreq.Count; i++)
                if (choice < starFreq.ElementAt(i).Value)
                {
                    starType = starFreq.ElementAt(i).Key;
                    break;
                }

            switch (starType)
            {
                case "K": return (EStarType.MainSeqStar, ESpectrType.K);
                case "M": return (EStarType.MainSeqStar, ESpectrType.M);
                case "G": return (EStarType.MainSeqStar, ESpectrType.G);
                case "F": return (EStarType.MainSeqStar, ESpectrType.F);
                case "A": return (EStarType.MainSeqStar, ESpectrType.A);
                case "B": return (EStarType.MainSeqStar, ESpectrType.B);
                case "O": return (EStarType.MainSeqStar, ESpectrType.O);
                case "BH": return (EStarType.BlackHole, ESpectrType.X);
                case "N": return (EStarType.NeutronStar, ESpectrType.X);
                case "W": return (EStarType.WhiteDwarf, ESpectrType.X);
                case "RG": return (EStarType.GiantStar, ESpectrType.M);
                case "YG": return (EStarType.GiantStar, ESpectrType.G);
                case "WG": return (EStarType.GiantStar, ESpectrType.A);
                default: return (EStarType.GiantStar, ESpectrType.B);
            }
        }

    }
}