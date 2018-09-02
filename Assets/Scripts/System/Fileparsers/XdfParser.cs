namespace Assets.Fileparsers
{
    public class Xdf
    {
    }

    class XdfParser
    {
        public static Xdf ParseXdf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                var xdf = new Xdf();

                br.FindNext("WDFC");
                

                return xdf;
            }
        }
    }
}
