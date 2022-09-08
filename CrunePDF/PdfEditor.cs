// Copyright (c) Shimpei Uenoi. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Collections.Generic;
using System.Linq;

namespace CrunePDF
{
    internal class PdfEditor
    {
        public static void RotatePdfPages(string path, IEnumerable<int> rotations, string dstPath)
        {
            using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
            using var saveDoc = new PdfDocument();
            var rotArr = rotations.ToArray();
            for (var i = 0; i < doc.PageCount; i++)
            {
                saveDoc.AddPage(doc.Pages[i], AnnotationCopyingType.DeepCopy);
                saveDoc.Pages[i].Rotate = doc.Pages[i].Rotate - (i < rotArr.Length ? rotArr[i] : 0);
            }
            saveDoc.Save(dstPath);

        }
    }
}
