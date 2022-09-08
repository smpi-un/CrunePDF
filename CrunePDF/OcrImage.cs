// Copyright (c) Shimpei Uenoi. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CrunePDF
{
    internal class OcrImage
    {
        readonly string language;
        readonly double scoreThreshold;
        readonly double raitoThreshold;
        public OcrImage(string language, double scoreThreshold, double raitoThreshold)
        {
            this.language = language;
            this.scoreThreshold = scoreThreshold;
            this.raitoThreshold = raitoThreshold;
        }
        public async Task<List<int>> GetRotations(string path)
        {
            var result = new List<int>();
            using var pdfStream = File.OpenRead(path);
            using var winrtStream = pdfStream.AsRandomAccessStream();
            var doc = await PdfDocument.LoadFromStreamAsync(winrtStream);
            for (var i = 0u; i < doc.PageCount; i++)
            {
                using var page = doc.GetPage(i);
                using var memStream = new MemoryStream();
                await page.RenderToStreamAsync(memStream.AsRandomAccessStream());
                var decoder = await BitmapDecoder.CreateAsync(memStream.AsRandomAccessStream());
                var bitmap = await decoder.GetSoftwareBitmapAsync();
                var rot = await GetRotation(bitmap);
                result.Add(rot.GetValueOrDefault(0));
            }
            return result;
        }

        public async Task<int?> GetRotation(SoftwareBitmap bitmap)
        {
            var lang = new Windows.Globalization.Language(this.language);
            var engine = OcrEngine.TryCreateFromLanguage(lang);

            if (engine is null)
            {
                return null;
            }

            static async Task<int?> getScore(OcrEngine engine, SoftwareBitmap bmp, BitmapRotation rot)
            {
                var bmp2 = await SoftwareBitmapRotate(bmp, rot);
                var result = await engine.RecognizeAsync(bmp2);
                if (result is null)
                {
                    return null;
                }
                else
                {
                    return result.Lines.Count * result.Text.Length;
                }
            }

            var scores = new List<int?>() {
                await getScore(engine, bitmap, BitmapRotation.None),
                await getScore(engine, bitmap, BitmapRotation.Clockwise270Degrees),
                await getScore(engine, bitmap, BitmapRotation.Clockwise180Degrees),
                await getScore(engine, bitmap, BitmapRotation.Clockwise90Degrees),
            };
            if (scores.Any(x => !x.HasValue))
            {
                // Failed to rotate
                return null;
            }
            var scoresNotNull = scores.Select(x => x!.Value);
            // Console.WriteLine($"[{string.Join(",", scoresNotNull)}]");
            var maxScore = scoresNotNull.Max();
            var secondScore = scoresNotNull.OrderBy(x => x).ToArray()[scoresNotNull.Count() - 2];
            if (maxScore < this.scoreThreshold)
            {
                // Maximum value is below the threshold
                return null;
            }
            if (maxScore < this.raitoThreshold * secondScore)
            {
                // The largest value and the second largest value are close
                return null;
            }
            var maxScoreIndex = scoresNotNull.Select((v, i) => v == maxScore ? i : 0).Sum();
            return maxScoreIndex * 90;
        }
        async static Task<SoftwareBitmap> SoftwareBitmapRotate(SoftwareBitmap softwareBitmap, BitmapRotation rotation)
        {
            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            encoder.BitmapTransform.Rotation = rotation;
            await encoder.FlushAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var result = await decoder.GetSoftwareBitmapAsync(softwareBitmap.BitmapPixelFormat, BitmapAlphaMode.Premultiplied);
            return result;
        }
    }
}
