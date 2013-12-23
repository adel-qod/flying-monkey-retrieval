// Copyright (c) 2013, Adel Qodmani
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace IR
{
    /// <summary>
    /// <para>
    /// Allows the user to get the distance from a query to a set of documents using the vector-distance model.
    /// </para>
    /// <para>
    /// Bear in mind that this class is meant to be immutable in terms of what documents it queries; if you want your query to work over a different
    /// set of documents, create a new instance.
    /// </para>
    /// </summary>
    class VectorModeler
    {
        private double[,] frequencyMatrix;
        private double[] maxFreqPerDoc;
        private double[] docCountPerTerm;
        private double[] IDF;
        private readonly int DocCount;
        private readonly int MaxTermCount; // over all documents

        /// <summary>
        /// Class used to get the closest matching document/s to a given query
        /// </summary>
        /// <param name="frequencyIndices"> Array of hash-tables for each document where keys are the unqiue terms and values are their frequencies for a given file </param>
        public VectorModeler(Hashtable[] frequencyIndices)
        {
            DocCount = frequencyIndices.Length; // Each document has a member in the frequencyIndices array
            MaxTermCount = countUniqueTerms(frequencyIndices);
            this.InitFrequencyMatrix();
            this.InitMaxFrequencyPerDocument();
            this.NormalizeFrequencyMatrix();
            this.InitDocCountPerTerm();
            this.CalculateInverseDocumentFrequency();
            this.CalculateTfIdf();
        }

        public DocumentDistance[] distances;

        /// <summary>
        /// Counts the unique terms in all the documents given
        /// </summary>
        /// <param name="freqIndices"> Array of hash-tables for each document where keys are the unqiue terms and values are their frequencies for a given file </param>
        /// <returns> Count of unique terms over all the documents </returns>
        private int countUniqueTerms(Hashtable[] freqIndices)
        {
            int count = 0;
            Hashtable uniqueTerms = new Hashtable();
            Debug.WriteLine("Unique terms");
            foreach (var table in freqIndices)
            {
                foreach (var term in table.Keys)
                {
                    if (!uniqueTerms.ContainsKey(term))
                    {
                        uniqueTerms.Add(term, true);
                        Debug.WriteLine(term);
                        count++;
                    }
                }
            }
            Debug.WriteLine("");
            Debug.WriteLine("UniqueTermCount = {0}", count);
            Debug.WriteLine("");
            return count;
        }

        private void InitFrequencyMatrix()
        {
            /* NEEDS REFACTORING SO IT WON'T BE HARD_CODED */
            frequencyMatrix = new double[,] {
            {4, 2, 0, 0},
			{2, 0, 3, 3},
			{2, 0, 0, 0},
			{2, 2, 2, 2},
			{0, 1, 0, 0},
			{0, 1, 0, 0},
			{0, 2, 2, 0},
			{0, 2, 1, 0},
			{0, 1, 0, 0},
			{0, 0, 1, 0},
			{0, 0, 1, 0},
			{0, 0, 0, 3},
			{0, 0, 0, 2},
			{0, 0, 0, 2}
		    };// End array init
        }

        /// <summary>
        /// Calculates the max frequency per document and stores it in the maxFreqPerDoc
        /// </summary>
        private void InitMaxFrequencyPerDocument()
        {
            maxFreqPerDoc = new double[DocCount];
            double tmpMax;
            // Get the max freq per doc
            for (int i = 0; i < DocCount; i++)// For each document i
            {
                tmpMax = frequencyMatrix[0, i];
                for (int j = 1; j < MaxTermCount; j++)// For each word that can be in the document
                {
                    if (frequencyMatrix[j, i] > tmpMax)
                        tmpMax = frequencyMatrix[j, i];
                }
                maxFreqPerDoc[i] = tmpMax;
            }
        }

        /// <summary>
        /// <para>Normalizes the frequency matrix so that values in it are in it are between 0 and 1 (inclusive).</para>
        /// <para>It uses the maximum term frequency per document to do so</para>
        /// </summary>
        private void NormalizeFrequencyMatrix()
        {
            for (int i = 0; i < DocCount; i++)// For each document i 
            {
                for (int j = 0; j < MaxTermCount; j++)//For each word that can be in the document
                    frequencyMatrix[j, i] /= maxFreqPerDoc[i];
            }

            Debug.WriteLine("NormalizeFreqMatrix");
            for (int i = 0; i < MaxTermCount; i++)
            {
                for (int j = 0; j < DocCount; j++)
                {
                    Debug.Write(String.Format("{0:0.0##}", frequencyMatrix[i, j]));
                    Debug.Write("\t");
                }
                Debug.WriteLine("");
            }
            Debug.WriteLine("");
        }

        /// <summary>
        /// Fills the docCountPerTerm array with the count of how many documents contained a given term i
        /// </summary>
        private void InitDocCountPerTerm()
        {
            docCountPerTerm = new double[MaxTermCount];
            for (int i = 0; i < MaxTermCount; i++) // For each term i
            {
                for (int j = 0; j < DocCount; j++) // For each document
                {
                    if (frequencyMatrix[i, j] != 0)
                        docCountPerTerm[i]++;
                }
            }
        }

        /// <summary>
        /// Builds the IDF array which contains the Log10(docCount/docCountPerTerm) for each term
        /// It's used in building the TF-IDF table
        /// </summary>
        private void CalculateInverseDocumentFrequency()
        {
            IDF = new double[MaxTermCount];
            for (int i = 0; i < MaxTermCount; i++)
                IDF[i] = Math.Log10(DocCount / docCountPerTerm[i]);
            Debug.WriteLine("Printing the IDF");
            foreach (var item in IDF)
            {
                Debug.WriteLine(String.Format("{0:0.0##}", item));
            }
            Debug.WriteLine("");
        }

        /// <summary>
        /// Builds the tf-idf table using the IDF table and writes it over the frequency matrix
        /// </summary>
        private void CalculateTfIdf()
        {
            for (int i = 0; i < MaxTermCount; i++)
                for (int j = 0; j < DocCount; j++)
                    frequencyMatrix[i, j] *= IDF[i];
            Debug.WriteLine("TF-IDF Matrix");
            for (int i = 0; i < MaxTermCount; i++)
            {
                for (int j = 0; j < DocCount; j++)
                {
                    Debug.Write(String.Format("{0:0.0##}", frequencyMatrix[i, j]));
                    Debug.Write("\t");
                }
                Debug.WriteLine("");
            }
            Debug.WriteLine("");
        }

        /// <summary>
        /// Uses the TF-IDF table to know the distance between the query and each document in the system.
        /// </summary>
        /// <param name="queryVector"> The words frequency of each word in the query </param>
        public void CalculateDistances(double[] queryVector)
        {
            // To calc distance for a query; (1) nomralize it (2)get its tf-idf and (3) calc the distance
            Debug.WriteLine("Query we have now");
            foreach (var item in queryVector)
                Debug.WriteLine(item);
            Debug.WriteLine("");
            this.NormalizeQuery(queryVector);
            Debug.WriteLine("Query we have after normalization");
            foreach (var item in queryVector)
                Debug.WriteLine(item);
            Debug.WriteLine("");
            this.CalculateQueuryTfId(queryVector);
            Debug.WriteLine("Query we have after TF-IDF");
            foreach (var item in queryVector)
                Debug.WriteLine(item);
            Debug.WriteLine("");
            distances = new DocumentDistance[DocCount];
            for (int i = 0; i < DocCount; i++)
            {
                distances[i] = new DocumentDistance("Document[" + (i + 1).ToString() + "]");
            }
            double[] sums = new double[DocCount];
            for (int i = 0; i < DocCount; i++) // For each document i
            {
                for (int j = 0; j < MaxTermCount; j++) // For each term j
                {
                    sums[i] += Math.Pow(queryVector[j] - frequencyMatrix[j, i], 2);
                }
            }
            for (int i = 0; i < DocCount; i++)
            {
                distances[i].distance = Math.Sqrt(sums[i]);
            }
        }

        /// <summary>
        /// Normalizes the query to be properly used with the documents in this system.
        /// </summary>
        /// <param name="queryVector"> The user query to be normalized </param>
        private void NormalizeQuery(double[] queryVector)
        {
            Debug.Assert(queryVector.Length == MaxTermCount);
            // queury is normalized by dividing each frequency in it by the max frequency in the query
            double max = queryVector[0];
            foreach (var item in queryVector)
            {
                if (item > max)
                    max = item;
            }
            for (int i = 0; i < queryVector.Length; i++)
                queryVector[i] /= max;
        }

        /// <summary>
        /// Uses the IDF in the class to build the TF-IDF of the given query
        /// </summary>
        /// <param name="normalizedQueryVector">The normalized user query to be TF-IDFed</param>
        private void CalculateQueuryTfId(double[] normalizedQueryVector)
        {
            Debug.Assert(normalizedQueryVector.Length == MaxTermCount);
            for (int i = 0; i < normalizedQueryVector.Length; i++)
                normalizedQueryVector[i] *= IDF[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"> A pre-processed query </param>
        /// <returns> The frequency vectory of the query </returns>
        private double[] TextToQueryVectory(string query)
        {
            double[] queryVector = new double[MaxTermCount];

            return null;
        }
    }
}
