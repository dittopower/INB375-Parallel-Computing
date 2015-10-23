using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading;

namespace DigitalMusicParallelNogui
{
    class Core
    {
        public static Complex[] twiddles;

        public static Complex[] fft(Complex[] x, int L)
        {
         //   MainWindow.ttt.start();
            int ii = 0;
            int kk = 0;
            int N = x.Length;

            Complex[] Y = new Complex[N];

            if (N == 1)
            {
                Y[0] = x[0];
            }
            else
            {

                Complex[] E = new Complex[N / 2];
                Complex[] O = new Complex[N / 2];
                if (N == 2)
                {
                    E[0] = x[0];
                    O[0] = x[1];
                }
                else
                {
                    Complex[] even = new Complex[N / 2];
                    Complex[] odd = new Complex[N / 2];
                    // timeff.next("fft things");
                    for (ii = 0; ii < N; ii++)
                    {

                        if (ii % 2 == 0)
                        {
                            even[ii / 2] = x[ii];
                        }
                        if (ii % 2 == 1)
                        {
                            odd[(ii - 1) / 2] = x[ii];
                        }
                    }
                    
                    E = fft(even, L);
                    O = fft(odd, L);
                }

                for (kk = 0; kk < N; kk++)
                {
                    Y[kk] = E[(kk % (N / 2))] + O[(kk % (N / 2))] * twiddles [kk * (L / N)];
                }
            }
            
        //    MainWindow.ttt.pause();
            return Y;
        }
    }
}
