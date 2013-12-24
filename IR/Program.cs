using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.IO;

namespace IR
{
    class Program
    {
        static void Main(string[] args)
        {
            // Debug by default sends to the Output message (below ..) so redirect it to Console
            Debug.Listeners.Clear();
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            String path = @"C:\Users\Adel\Documents\Visual Studio 2010\Projects\IR\IR\Tests";
            VectorModeler mySystem = new VectorModeler(path);
            DocumentDistance[] docs = mySystem.GetRelevantDocuments("do i do", threshold: 0, useVectorDistance: true);
            for (int i = 0; i < docs.Length; i++)
            {
                Console.WriteLine(docs[i].docName);
                Console.WriteLine(String.Format("{0:0.0######}", docs[i].distance));
            }
        }
    }
}
