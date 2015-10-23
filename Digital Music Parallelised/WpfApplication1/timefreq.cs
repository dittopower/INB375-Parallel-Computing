using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading;

namespace WpfApplication1
{
    public class timefreq
    {
        public float[][] timeFreqData;
        public int wSamp;

        private Complex[] compX;
        private int nearest;
        private float[] xx;
        private float fftMax;
        private int N;
        private float[][] Y;
        private Complex[] xxx;

        public timefreq(float[] x, int windowSamp)
        {
            int ii;
            xx = x;
            double pi = 3.14159265;
            Complex i = Complex.ImaginaryOne;
            this.wSamp = windowSamp;
            Core.twiddles = new Complex[wSamp];
            Time timer = new Time();
            for (ii = 0; ii < wSamp; ii++)
            {
                double a = 2 * pi * ii / (double)wSamp;
                Core.twiddles[ii] = Complex.Pow(Complex.Exp(-i), (float)a);
            }
            //timer.next("timefreq - 1");
            timeFreqData = new float[wSamp / 2][];

            nearest = (int)Math.Ceiling((double)x.Length / (double)wSamp);
            nearest = nearest * wSamp;

            compX = new Complex[nearest];
            Thread[] mine = new Thread[MainWindow.Num_threads];
            for (int a = 0; a < MainWindow.Num_threads; a++)
            {
                mine[a] = new Thread(loop);
                mine[a].Start(a);
            }
            for (int a = 0; a < MainWindow.Num_threads; a++)
            {
                mine[a].Join();
            }
            //timer.next("timefreq - 2");

            int cols = 2 * nearest / wSamp;

            for (int jj = 0; jj < wSamp / 2; jj++)
            {
                timeFreqData[jj] = new float[cols];
            }
            timer.next("timefreq - 3");
            timeFreqData = stft(compX, wSamp);
            timer.end("timefreq - stft");
        }

        float[][] stft(Complex[] x, int wSamp)
        {
            int ii = 0;
            int jj = 0;
            int kk = 0;
            int ll = 0;
            N = x.Length;
            fftMax = 0;
            xxx = x;
            Thread[] mine = new Thread[MainWindow.Num_threads];

            Y = new float[wSamp / 2][];
            Time timer = new Time();
            for (ll = 0; ll < wSamp / 2; ll++)
            {
                Y[ll] = new float[2 * (int)Math.Floor((double)N / (double)wSamp)];
            }
            timer.next("timefreq@stft - 1");

            for (ll = 0; ll < MainWindow.Num_threads; ll++)
            {
                mine[ll] = new Thread(stft2);
                mine[ll].Start(ll);
            }
            for (ll = 0; ll < MainWindow.Num_threads; ll++)
            {
                mine[ll].Join();
            }
            //mytimer.end("stft - 2\tDONE");
            timer.next("timefreq@stft - 2");


            //Thread[] mine = new Thread[MainWindow.Num_threads];
            for (int a = 0; a < MainWindow.Num_threads; a++)
            {
                mine[a] = new Thread(stft3);
                mine[a].Start(a);
            }
            for (int a = 0; a < MainWindow.Num_threads; a++)
            {
                mine[a].Join();
            }
            timer.end("timefreq@stft - 3");
            return Y;
        }


        private void loop(object tid)
        {
            int id = (int)tid;
            int blocksize = (nearest + MainWindow.Num_threads - 1) / MainWindow.Num_threads;

            int lowerbound = id * blocksize;
            int upperbound = Math.Min(lowerbound + blocksize, nearest);

            for (int kk = lowerbound; kk < upperbound; kk++)
            {
                if (kk < xx.Length)
                {
                    compX[kk] = xx[kk];
                }
                else
                {
                    compX[kk] = Complex.Zero;
                }
            }
        }

        private void stft3(object tid)
        {
            int id = (int)tid;
            int blocksize = ((int)(2 * Math.Floor((double)N / (double)wSamp) - 1) + MainWindow.Num_threads - 1) / MainWindow.Num_threads;

            int lowerbound = id * blocksize;
            int upperbound = Math.Min(lowerbound + blocksize, (int)(2 * Math.Floor((double)N / (double)wSamp) - 1));

            for (int ii = lowerbound; ii < upperbound; ii++)
            {
                for (int kk = 0; kk < wSamp / 2; kk++)
                {
                    Y[kk][ii] /= fftMax;
                }
            }
        }

        private void stft2(object tid)
        {
            int id = (int)tid;
            int blocksize = ((int)(2 * Math.Floor((double)N / (double)wSamp) - 1) + MainWindow.Num_threads - 1) / MainWindow.Num_threads;

            int lowerbound = id * blocksize;
            int upperbound = Math.Min(lowerbound + blocksize, (int)(2 * Math.Floor((double)N / (double)wSamp) - 1));
            Complex[] temp = new Complex[wSamp];
            Complex[] tempFFT = new Complex[wSamp];

            for (int ii = lowerbound; ii < upperbound; ii++)
            {
                for (int jj = 0; jj < wSamp; jj++)
                {
                    temp[jj] = xxx[ii * (wSamp / 2) + jj];
                }

                tempFFT = Core.fft(temp, wSamp);

                for (int kk = 0; kk < wSamp / 2; kk++)
                {
                    Y[kk][ii] = (float)Complex.Abs(tempFFT[kk]);

                    if (Y[kk][ii] > fftMax)
                    {
                        fftMax = Y[kk][ii];
                    }
                }
            }
        }
    }
}
