// Copyright (c) 2013, Sarah Homsi 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;


namespace IR
{
    static class DocumentsReader
    {
        /// <summary>
        /// Builds an index of term frequencies in a file.
        /// </summary>
        /// <param name="filePath"> The path to the file to be indexed </param>
        /// <returns> A a hash-table where keys are the unique terms and values are their frequencies in the input file </returns>
        public static Hashtable BuildDocumentFrequencyIndex(string filePath)
        {
            Hashtable result = new Hashtable();
            // NOTES: WHAT IF THE FILE DOESN'T FIT IN MEMORY?
            string[] text = System.IO.File.ReadAllText(filePath).Split(null); // Split over null splits over any sequence of whitespace characters
            foreach (string word in text)
            {
                if (result.ContainsKey(word))
                    result[word] = (int)result[word] + 1;
                else
                    result.Add(word, 1);
            }
            return result;
        }
    }
}
