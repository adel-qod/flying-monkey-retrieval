using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.IO;

namespace IR
{
    /// <summary>
    /// Represents each document's distance/consinal similarty
    /// </summary>
    class DocumentDistance : IComparable<DocumentDistance>
    {	
	    public string docName;
        public string docPath;
	    public double distance;

	    public DocumentDistance(String docPath)
	    {
            this.docPath = docPath;
            this.docName = Path.GetFileNameWithoutExtension(docPath);
	    }
    	
	    public int CompareTo(DocumentDistance that)
	    {
	       	if(this.distance < that.distance)
	    		return -1;
		    else if(this.distance == that.distance)
			    return 0;
		    else
			    return 1;
        }
    }

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
