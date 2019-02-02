namespace Assets.Scripts.System.Fileparsers
{
    public class Xdf
    {
    }

    internal class XdfParser
    {
        public static Xdf ParseXdf(string filename)
        {
            using (Bwd2Reader br = new Bwd2Reader(filename))
            {
                Xdf xdf = new Xdf();

                br.FindNext("WDFC");
                

                return xdf;
            }
        }
    }
}
