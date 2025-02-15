﻿using System;
using System.Drawing;
using NAudio.Wave;

namespace NAudio.WaveFormRenderer
{
    public class WaveFormRenderer
    {
        public Image Render(string selectedFile, WaveFormRendererSettings settings)
        {
            return Render(selectedFile, new MaxPeakProvider(), settings);
        }

        public Image Render(string selectedFile, IPeakProvider peakProvider, WaveFormRendererSettings settings)
        {
            return Render(selectedFile, peakProvider, settings, (a) => new AudioFileReader(a));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedFile"></param>
        /// <param name="peakProvider"></param>
        /// <param name="settings"></param>
        /// <param name="audioFileReader">Like AudioFileReader, it has to inherit both WaveStream and ISampleProvider</param>
        /// <returns></returns>
        public Image Render(string selectedFile, IPeakProvider peakProvider, WaveFormRendererSettings settings, Func<string, WaveStream> audioFileReader)
        {
            using (WaveStream reader = audioFileReader.Invoke(selectedFile))
            {
                int bytesPerSample = (reader.WaveFormat.BitsPerSample / 8);
                var samples = reader.Length / (bytesPerSample);
                var samplesPerPixel = (int)(samples / settings.Width);
                var stepSize = settings.PixelsPerPeak + settings.SpacerPixels;
                peakProvider.Init((ISampleProvider)reader, samplesPerPixel * stepSize);
                return Render(peakProvider, settings);
            }
        }

        public Image Render(WaveStream audioFileReader, IPeakProvider peakProvider, WaveFormRendererSettings settings)
        {
            using (audioFileReader)
            {
                int bytesPerSample = (audioFileReader.WaveFormat.BitsPerSample / 8);
                var samples = audioFileReader.Length / (bytesPerSample);
                var samplesPerPixel = (int)(samples / settings.Width);
                var stepSize = settings.PixelsPerPeak + settings.SpacerPixels;
                peakProvider.Init((ISampleProvider)audioFileReader, samplesPerPixel * stepSize);
                return Render(peakProvider, settings);
            }
        }

        private static Image Render(IPeakProvider peakProvider, WaveFormRendererSettings settings)
        {
            if (settings.DecibelScale)
                peakProvider = new DecibelPeakProvider(peakProvider, 48);

            var b = new Bitmap(settings.Width, settings.TopHeight + settings.BottomHeight);
            if (settings.BackgroundColor == Color.Transparent)
            {
                b.MakeTransparent();
            }
            using (var g = Graphics.FromImage(b))
            {
                g.FillRectangle(settings.BackgroundBrush, 0, 0, b.Width, b.Height);
                var midPoint = settings.TopHeight;

                int x = 0;
                var currentPeak = peakProvider.GetNextPeak();
                while (x < settings.Width)
                {
                    var nextPeak = peakProvider.GetNextPeak();

                    for (int n = 0; n < settings.PixelsPerPeak; n++)
                    {
                        var lineHeight = settings.TopHeight * currentPeak.Max;
                        g.DrawLine(settings.TopPeakPen, x, midPoint, x, midPoint - lineHeight);
                        lineHeight = settings.BottomHeight * currentPeak.Min;
                        g.DrawLine(settings.BottomPeakPen, x, midPoint, x, midPoint - lineHeight);
                        x++;
                    }

                    for (int n = 0; n < settings.SpacerPixels; n++)
                    {
                        // spacer bars are always the lower of the 
                        var max = Math.Min(currentPeak.Max, nextPeak.Max);
                        var min = Math.Max(currentPeak.Min, nextPeak.Min);

                        var lineHeight = settings.TopHeight * max;
                        g.DrawLine(settings.TopSpacerPen, x, midPoint, x, midPoint - lineHeight);
                        lineHeight = settings.BottomHeight * min;
                        g.DrawLine(settings.BottomSpacerPen, x, midPoint, x, midPoint - lineHeight);
                        x++;
                    }
                    currentPeak = nextPeak;
                }
            }
            return b;
        }


    }
}
