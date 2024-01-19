namespace GoPlayProj.Utils
{
    /// <summary>
    /// 正态分布随机数生成器
    /// </summary>
    public class RandBoxMuller
    {
        private const double TOW_PI = Math.PI * 2f;
        private double z1;
        private bool generate;
	
        public double Rand(double mu, double sigma)
        {
            generate = !generate;

            if (!generate)
                return z1 * sigma + mu;

            double u1, u2;
            do
            {
                u1 = Utils.Rand.Range(0f, 1f);
                u2 = Utils.Rand.Range(0f, 1f);
            }
            while ( u1 <= double.Epsilon );

            double z0;
            z0 = Math.Sqrt(-2.0f * Math.Log(u1)) * Math.Cos(TOW_PI * u2);
            z1 = Math.Sqrt(-2.0f * Math.Log(u1)) * Math.Sin(TOW_PI * u2);
            return z0 * sigma + mu;
        }
    }
}