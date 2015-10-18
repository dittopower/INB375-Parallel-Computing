using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Media;
using System.Threading;
using System.Numerics;
using System.Xml;

namespace DigitalMusicParallelNogui
{
    class MainProgram
    {
        public static wavefile waveIn;
        public static timefreq stftRep;
        public static float[] pixelArray;
        public static musicNote[] sheetmusic;
        public static Complex[] compX;
        public enum pitchConv { C, Db, D, Eb, E, F, Gb, G, Ab, A, Bb, B };
        public static double bpm = 70;
        const int Num_threads = 8;

        static void Main(string[] args)
        {
            string filename = "..//..//Jupiter.wav";
            string xmlfile = "..//..//Jupiter.xml";
            Time timer = new Time();
            loadWave(filename);
            timer.next("setup");
            freqDomain();
            timer.next("freqDomain");
            sheetmusic = readXML(xmlfile);
            // timer.next("sheetmusic");
            onsetDetection();
            // timer.next("onsetDetection");
            //  timer.next("loadImage");
            //   timer.next("loadHistogram");
            //  timer.next("playBack");

            timer.end("Other stuff");
            Console.WriteLine("It is done!");
            Console.ReadKey();
        }

        // Reads in a .wav file
        public static void loadWave(string filename)
        {
            // Sound File
            FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
            if (file == null)
            {
                System.Console.Write("Failed to Open File!");
            }
            else
            {
                waveIn = new wavefile(file);
            }

        }

        // Transforms data into Time-Frequency representation

        public static void freqDomain()
        {
            Time timerf = new Time();
            stftRep = new timefreq(waveIn.wave, 2048);
            timerf.next("a");
            pixelArray = new float[stftRep.timeFreqData[0].Length * stftRep.wSamp / 2];
            timerf.next("b");
            for (int jj = 0; jj < stftRep.wSamp / 2; jj++)
            {
                for (int ii = 0; ii < stftRep.timeFreqData[0].Length; ii++)
                {
                    pixelArray[jj * stftRep.timeFreqData[0].Length + ii] = stftRep.timeFreqData[jj][ii];
                }
            }
            timerf.end("c");
        }

        // Onset Detection function - Determines Start and Finish times of a note and the frequency of the note over each duration.

        public static void onsetDetection()
        {
            //Time timer = new Time();
            float[] HFC;
            int starts = 0;
            int stops = 0;
            Complex[] Y;
            double[] absY;
            List<int> lengths;
            List<int> noteStarts;
            List<int> noteStops;
            List<double> pitches;

            int ll;
            double pi = 3.14159265;
            Complex i = Complex.ImaginaryOne;

            noteStarts = new List<int>(100);
            noteStops = new List<int>(100);
            lengths = new List<int>(100);
            pitches = new List<double>(100);

            HFC = new float[stftRep.timeFreqData[0].Length];
            //timer.next("Onset setup");
            for (int jj = 0; jj < stftRep.timeFreqData[0].Length; jj++)
            {
                for (int ii = 0; ii < stftRep.wSamp / 2; ii++)
                {
                    HFC[jj] = HFC[jj] + (float)Math.Pow((double)stftRep.timeFreqData[ii][jj] * ii, 2);
                }

            }
            //timer.next("Onset loop 1");
            float maxi = HFC.Max();

            for (int jj = 0; jj < stftRep.timeFreqData[0].Length; jj++)
            {
                HFC[jj] = (float)Math.Pow((HFC[jj] / maxi), 2);
            }
            // timer.next("Onset loop 2");
            for (int jj = 0; jj < stftRep.timeFreqData[0].Length; jj++)
            {
                if (starts > stops)
                {
                    if (HFC[jj] < 0.001)
                    {
                        noteStops.Add(jj * ((stftRep.wSamp - 1) / 2));
                        stops = stops + 1;
                    }
                }
                else if (starts - stops == 0)
                {
                    if (HFC[jj] > 0.001)
                    {
                        noteStarts.Add(jj * ((stftRep.wSamp - 1) / 2));
                        starts = starts + 1;
                    }

                }
            }
            //timer.next("Onset loop 3");
            if (starts > stops)
            {
                noteStops.Add(waveIn.data.Length);
            }


            // DETERMINES START AND FINISH TIME OF NOTES BASED ON ONSET DETECTION       

            ///*
            // timer.restart();
            for (int ii = 0; ii < noteStops.Count; ii++)
            {
                lengths.Add(noteStops[ii] - noteStarts[ii]);
            }
            // timer.next("Onset loop 4");
            for (int mm = 0; mm < lengths.Count; mm++)
            {
                //Time timermm = new Time();
                int nearest = (int)Math.Pow(2, Math.Ceiling(Math.Log(lengths[mm], 2)));
                Core.twiddles = new Complex[nearest];
                for (ll = 0; ll < nearest; ll++)
                {
                    double a = 2 * pi * ll / (double)nearest;
                    Core.twiddles[ll] = Complex.Pow(Complex.Exp(-i), (float)a);
                }
                //timermm.next("Onset mm - ll");

                compX = new Complex[nearest];
                for (int kk = 0; kk < nearest; kk++)
                {
                    if (kk < lengths[mm] && (noteStarts[mm] + kk) < waveIn.wave.Length)
                    {
                        compX[kk] = waveIn.wave[noteStarts[mm] + kk];
                    }
                    else
                    {
                        compX[kk] = Complex.Zero;
                    }
                }
                // timermm.next("Onset mm - kk");

                Y = new Complex[nearest];

                Y = Core.fft(compX, nearest);
                //     timermm.next("Onset mm - Y = fft");

                absY = new double[nearest];

                double maximum = 0;
                int maxInd = 0;

                for (int jj = 0; jj < Y.Length; jj++)
                {
                    absY[jj] = Y[jj].Magnitude;
                    if (absY[jj] > maximum)
                    {
                        maximum = absY[jj];
                        maxInd = jj;
                    }
                }
                //   timermm.next("Onset mm - jj");

                for (int div = 6; div > 1; div--)
                {

                    if (maxInd > nearest / 2)
                    {
                        if (absY[(int)Math.Floor((double)(nearest - maxInd) / div)] / absY[(maxInd)] > 0.10)
                        {
                            maxInd = (nearest - maxInd) / div;
                        }
                    }
                    else
                    {
                        if (absY[(int)Math.Floor((double)maxInd / div)] / absY[(maxInd)] > 0.10)
                        {
                            maxInd = maxInd / div;
                        }
                    }
                }
                //   timermm.next("Onset mm - div");

                if (maxInd > nearest / 2)
                {
                    pitches.Add((nearest - maxInd) * waveIn.SampleRate / nearest);
                }
                else
                {
                    pitches.Add(maxInd * waveIn.SampleRate / nearest);
                }
                //   timermm.next("Onset mm - last");


            }
            // timer.next("onset mm loop");

            musicNote[] noteArray;
            noteArray = new musicNote[noteStarts.Count()];

            for (int ii = 0; ii < noteStarts.Count(); ii++)
            {
                noteArray[ii] = new musicNote(pitches[ii], lengths[ii]);
            }
            //   timer.next("onset loop 6");
            int[] sheetPitchArray = new int[sheetmusic.Length];
            int[] notePitchArray = new int[noteArray.Length];

            for (int ii = 0; ii < sheetmusic.Length; ii++)
            {
                sheetPitchArray[ii] = sheetmusic[ii].pitch % 12;
            }
            //   timer.next("onset loop 7");
            for (int jj = 0; jj < noteArray.Length; jj++)
            {
                notePitchArray[jj] = noteArray[jj].pitch % 12;
            }
            //  timer.next("onset loop 8");
            string[] alignedStrings = new string[2];

            alignedStrings = stringMatch(sheetPitchArray, notePitchArray);

            musicNote[] alignedStaffArray = new musicNote[alignedStrings[0].Length / 2];
            musicNote[] alignedNoteArray = new musicNote[alignedStrings[1].Length / 2];
            int staffCount = 0;
            int noteCount = 0;
            //   timer.next("onset stuff");
            for (int ii = 0; ii < alignedStrings[0].Length / 2; ii++)
            {

                if (alignedStrings[0][2 * ii] == ' ')
                {
                    alignedStaffArray[ii] = new musicNote(0, 0);
                }
                else
                {
                    alignedStaffArray[ii] = sheetmusic[staffCount];
                    staffCount++;
                }

                if (alignedStrings[1][2 * ii] == ' ')
                {
                    alignedNoteArray[ii] = new musicNote(0, 0);
                }
                else
                {
                    alignedNoteArray[ii] = noteArray[noteCount];
                    noteCount++;
                }
            }

        }//end onset

        // FFT function for Pitch Detection
        public static musicNote[] readXML(string filename)
        {

            List<string> stepList = new List<string>(100);
            List<int> octaveList = new List<int>(100);
            List<int> durationList = new List<int>(100);
            List<int> alterList = new List<int>(100);
            int noteCount = 0;
            bool sharp;
            musicNote[] scoreArray;

            FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
            if (file == null)
            {
                System.Console.Write("Failed to Open File!");
            }

            XmlTextReader reader = new XmlTextReader(filename);

            bool finished = false;

            while (finished == false)
            {
                sharp = false;
                while ((!reader.Name.Equals("note") || reader.NodeType == XmlNodeType.EndElement) && !finished)
                {
                    reader.Read();
                    if (reader.ReadState == ReadState.EndOfFile)
                    {
                        finished = true;
                    }
                }

                reader.Read();
                reader.Read();
                if (reader.Name.Equals("rest"))
                {
                }
                else if (reader.Name.Equals("pitch"))
                {

                    while (!reader.Name.Equals("step"))
                    {
                        reader.Read();
                    }
                    reader.Read();
                    stepList.Add(reader.Value);
                    while (!reader.Name.Equals("octave"))
                    {
                        if (reader.Name.Equals("alter") && reader.NodeType == XmlNodeType.Element)
                        {
                            reader.Read();
                            alterList.Add(int.Parse(reader.Value));
                            sharp = true;
                        }
                        reader.Read();
                    }
                    reader.Read();
                    if (!sharp)
                    {
                        alterList.Add(0);
                    }
                    sharp = false;
                    octaveList.Add(int.Parse(reader.Value));
                    while (!reader.Name.Equals("duration"))
                    {
                        reader.Read();
                    }
                    reader.Read();
                    durationList.Add(int.Parse(reader.Value));
                    //System.Console.Out.Write("Note ~ Pitch: " + stepList[noteCount] + alterList[noteCount] + " Octave: " + octaveList[noteCount] + " Duration: " + durationList[noteCount] + "\n");
                    noteCount++;

                }

            }

            scoreArray = new musicNote[noteCount];

            double c0 = 16.351625;

            for (int nn = 0; nn < noteCount; nn++)
            {
                int step = (int)Enum.Parse(typeof(pitchConv), stepList[nn]);

                double freq = c0 * Math.Pow(2, octaveList[nn]) * (Math.Pow(2, ((double)step + (double)alterList[nn]) / 12));
                scoreArray[nn] = new musicNote(freq, (double)durationList[nn] * 60 * waveIn.SampleRate / (4 * bpm));

            }

            return scoreArray;
        }

        public static string[] stringMatch(string A, string B)
        {
            // SETUP SIMILARITY MATRIX
            int[][] S = new int[12][];

            for (int i = 0; i < 12; i++)
            {
                S[i] = new int[12];
            }

            for (int i = 0; i < 12; i++)
            {
                for (int j = i; j < 12; j++)
                {
                    if (i == j)
                        S[i][j] = 10;
                    else
                        S[i][j] = -Math.Abs(i - j);
                }
            }

            //GAP PENALTY

            int d = -10;

            int[][] F = new int[A.Length + 1][];

            for (int i = 0; i < A.Length + 1; i++)
            {
                F[i] = new int[B.Length + 1];
            }

            for (int j = 0; j < B.Length + 1; j++)
            {
                F[0][j] = d * j;
            }

            for (int i = 0; i < A.Length + 1; i++)
            {
                F[i][0] = d * i;
            }

            for (int i = 1; i < A.Length + 1; i++)
            {
                for (int j = 1; j < B.Length + 1; j++)
                {
                    int Ai = (int)A[i - 1] - 65;//parseChar(A[i - 1]);
                    int Bj = (int)B[j - 1] - 65;// parseChar(B[j - 1]);

                    F[i][j] = Math.Max(Math.Max((F[i - 1][j - 1] + S[Ai][Bj]), (F[i][j - 1] + d)), (F[i - 1][j] + d));
                }
            }

            string AlignA = "";
            string AlignB = "";

            int ii = (A.Length);
            int jj = (B.Length);

            while (ii > 0 && jj > 0)
            {

                int Score = F[ii][jj];
                int ScoreDiag = F[ii - 1][jj - 1];
                int ScoreUp = F[ii][jj - 1];
                int ScoreLeft = F[ii - 1][jj];

                int Ai = (int)(A[ii - 1]) - 65;
                int Bj = (int)(B[jj - 1]) - 65;

                if (Score == ScoreDiag + S[Ai][Bj])
                {
                    AlignA = A[ii - 1] + AlignA;
                    AlignB = B[jj - 1] + AlignB;

                    ii = ii - 1;
                    jj = jj - 1;

                }

                else if (Score == ScoreUp + d)
                {
                    AlignA = "-" + AlignA;
                    AlignB = B[jj - 1] + AlignB;

                    jj = jj - 1;
                }

                else if (Score == ScoreLeft + d)
                {
                    AlignA = A[ii - 1] + AlignA;
                    AlignB = "-" + AlignB;

                    ii = ii - 1;

                }
            }

            while (ii > 0)
            {
                AlignA = A[ii - 1] + AlignA;
                AlignB = "-" + AlignB;

                ii = ii - 1;
            }

            while (jj > 0)
            {
                AlignA = "-" + AlignA;
                AlignB = B[jj - 1] + AlignB;

                jj = jj - 1;
            }

            System.Console.Out.Write("Original:   " + A + "\n");
            System.Console.Out.Write("New String: " + B + "\n\n");
            System.Console.Out.Write("Optimal Alignment: \n\n");
            System.Console.Out.Write(AlignA + "\n");
            System.Console.Out.Write(AlignB + "\n");

            string[] returnArray = new string[2];

            returnArray[0] = AlignA;
            returnArray[1] = AlignB;

            return returnArray;


        }

        public static string[] stringMatch(int[] A, int[] B)
        {
            // SETUP SIMILARITY MATRIX
            int[][] S = new int[12][];

            for (int i = 0; i < 12; i++)
            {
                S[i] = new int[12];
            }

            for (int i = 0; i < 12; i++)
            {
                for (int j = i; j < 12; j++)
                {
                    if (i == j)
                        S[i][j] = 10;
                    else if (Math.Abs(i - j) <= 6)
                        S[i][j] = -Math.Abs(i - j);
                    else
                        S[i][j] = Math.Abs(i - j) - 12;

                    S[j][i] = S[i][j];
                }
            }

            //GAP PENALTY

            int d = -20;

            int[][] F = new int[A.Length + 1][];

            for (int i = 0; i < A.Length + 1; i++)
            {
                F[i] = new int[B.Length + 1];
            }

            for (int j = 0; j < B.Length + 1; j++)
            {
                F[0][j] = d * j;
            }

            for (int i = 0; i < A.Length + 1; i++)
            {
                F[i][0] = d * i;
            }

            for (int i = 1; i < A.Length + 1; i++)
            {
                for (int j = 1; j < B.Length + 1; j++)
                {
                    int Ai = A[i - 1];
                    int Bj = B[j - 1];

                    F[i][j] = Math.Max(Math.Max((F[i - 1][j - 1] + S[Ai][Bj]), (F[i][j - 1] + d)), (F[i - 1][j] + d));
                }
            }

            string AlignA = "";
            string AlignB = "";

            int ii = (A.Length);
            int jj = (B.Length);

            while (ii > 0 && jj > 0)
            {

                int Score = F[ii][jj];
                int ScoreDiag = F[ii - 1][jj - 1];
                int ScoreUp = F[ii][jj - 1];
                int ScoreLeft = F[ii - 1][jj];

                int Ai = (A[ii - 1]);
                int Bj = (B[jj - 1]);

                if (Score == ScoreDiag + S[Ai][Bj])
                {
                    AlignA = Enum.GetName(typeof(musicNote.notePitch), (A[ii - 1])) + AlignA;
                    AlignB = Enum.GetName(typeof(musicNote.notePitch), (B[jj - 1])) + AlignB;

                    ii = ii - 1;
                    jj = jj - 1;

                }

                else if (Score == ScoreUp + d)
                {
                    AlignA = "  " + AlignA;
                    AlignB = Enum.GetName(typeof(musicNote.notePitch), (B[jj - 1])) + AlignB;

                    jj = jj - 1;
                }

                else if (Score == ScoreLeft + d)
                {
                    AlignA = Enum.GetName(typeof(musicNote.notePitch), (A[ii - 1])) + AlignA;
                    AlignB = "  " + AlignB;

                    ii = ii - 1;

                }
            }

            while (ii > 0)
            {
                AlignA = Enum.GetName(typeof(musicNote.notePitch), (A[ii - 1])) + AlignA;
                AlignB = "  " + AlignB;

                ii = ii - 1;
            }

            while (jj > 0)
            {
                AlignA = "  " + AlignA;
                AlignB = Enum.GetName(typeof(musicNote.notePitch), (B[jj - 1])) + AlignB;

                jj = jj - 1;
            }

            System.Console.Out.Write("\n\n----------------  String Matching ------------------\n\n");

            System.Console.Out.Write(AlignA + "\n");
            System.Console.Out.Write(AlignB + "\n");

            string[] returnArray = new string[2];

            returnArray[0] = AlignA;
            returnArray[1] = AlignB;

            return returnArray;
        }
    }
}
