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
    /// TO BE FILLED LATER
    /// </summary>
    class DocumentDistance : IComparable<DocumentDistance>
    {	
	    public String name;
	    public double distance;

	    public DocumentDistance(String name)
	    {
	    	this.name = name;
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
        /// <summary>
        /// Just a helper to test DocumentsReader's code to avoid cluttering the main function
        /// </summary>
        static void TestDocumentsReaderCode(string path)
        {
            Hashtable[] files_indexes = DocumentsReader.BuildDocumentsFrequencyIndices(path);
            for (int i = 0; i < files_indexes.Length; i++ )
            {
                Console.WriteLine("--------- d{0}----------", i+1); // +1 because documents are named starting from 1 not 0
                foreach (DictionaryEntry item in files_indexes[i])
                {
                    Console.WriteLine("{0} : {1}", item.Key, item.Value);
                }
            }
        }

        static void Main(string[] args)
        {
            // Debug by default sends to the Output message (below ..) so redirect it to Console
            Debug.Listeners.Clear();
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            String path = @"C:\Users\Adel\Documents\Visual Studio 2010\Projects\IR\IR";
            Hashtable[] filesIndices = DocumentsReader.BuildDocumentsFrequencyIndices(path);
            VectorModeler mySystem = new VectorModeler(filesIndices);
            double[] q = {0,2,0,0,0,0,1,0,0,0,0,0,0,0}; // Term frequencies of the query
            Debug.WriteLine("Length of the query array: {0}\n",q.Length);
            mySystem.CalculateDistances(q);
            Array.Sort(mySystem.distances);
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine(mySystem.distances[i].name);
                Console.WriteLine(String.Format("{0:0.0######}", mySystem.distances[i].distance));
            }
            TestDocumentsReaderCode(path);
        }
    }
}
