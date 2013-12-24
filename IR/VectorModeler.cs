// Copyright (c) 2013, Adel Qodmani, Sarah Homsi
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.IO;

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
        // This thing here is VERY dangerous, it's the paths of the documents we're building the system upon; their order in this array is the order used to fill
        // the frequency matrix and is the order used to report the distances to the user; if any inconsistency happens here, we're basically .. well .. you know
        private string[] documentsPaths; 
        private Hashtable[] frequencyIndices; // Each array member is a document, each key is a term(string) and each value is its freq(int)
        private Hashtable uniqueTerms; // keys are unique terms of the documents and value is where the term is index in the frequencyMatrix
        private double[,] frequencyMatrix;
        private double[] maxFreqPerDoc;
        private double[] docCountPerTerm;
        private double[] IDF;
        private readonly int DocCount;
        private readonly int TotalTermsCount; // over all documents
        private DocumentDistance[] distances;

        /// <summary>
        /// Class used to get the closest matching document/s to a given query
        /// </summary>
        /// <param name="directoryPath"> The path of the directory where the documents to be queried are. </param>
        /// <exception cref="System.ArgumentNullException"> Thrown if you pass null to the constructor</exception>
        /// <exception cref="System.ArgumentException"> Thrown if the path passed is an empty string </exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"> Thrown if the path passed is corrupted or the directory doesn't exist </exception>
        /// <exception cref="System.Exception"> Thrown if the directory path passed contains no .txt files in it </exception>
        public VectorModeler(string directoryPath)
        {
            if (directoryPath == null)
                throw new ArgumentNullException("Parameter cannot be null!", "directoryPath");
            if (directoryPath.Length == 0)
                throw new ArgumentException("Parameter cannot be of length 0", "directoryPath");
            if(!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(String.Format("Cannot find Directory: {0}", directoryPath));
            documentsPaths = Directory.GetFiles(directoryPath, "*.txt");
            if (documentsPaths.Length == 0)
                throw new Exception("The directory path you enetered contains no .txt files!");
            Debug.WriteLine("Documents' names");
            foreach (var item in documentsPaths)
            {
                Debug.WriteLine(Path.GetFileName(item));
            }
            Debug.WriteLine("");
            this.InitFrequencyIndices(directoryPath);
            this.InitUniqueTerms();
            this.DocCount = this.frequencyIndices.Length;
            Debug.Assert(DocCount == documentsPaths.Length);
            this.TotalTermsCount = this.uniqueTerms.Count;
            this.InitFrequencyMatrix();
            distances = new DocumentDistance[DocCount];
            for (int i = 0; i < DocCount; i++)
            {
                distances[i] = new DocumentDistance(documentsPaths[i]);
            }
            // These are some heavy processing, consider using lazy initlization to speed the creation.
            this.InitMaxFrequencyPerDocument();
            this.NormalizeFrequencyMatrix();
            this.InitDocCountPerTerm();
            this.CalculateInverseDocumentFrequency();
            this.CalculateTfIdf();
        }

        /// <summary>
        /// Fills in the frequencyIndices with the term frequencies for each term in the documents
        /// </summary>
        /// <param name="directoryPath"> The directory path in which the documents are to be found </param>
        private void InitFrequencyIndices(string directoryPath)
        {
            frequencyIndices = new Hashtable[documentsPaths.Length];
            //filling frquencyIndices
            for (int i = 0; i < documentsPaths.Length; i++)
            {
                frequencyIndices[i] = DocumentsReader.BuildDocumentFrequencyIndex(documentsPaths[i]);
            }
            Debug.WriteLine("Frequency Indecies");
            for (int i = 0; i < DocCount; i++) // For each document
            {
                Debug.WriteLine("Document-{0}", i + 1);
                foreach (DictionaryEntry pair in frequencyIndices[i])
                {
                    Debug.WriteLine("Term: {0} => freq: {1}", pair.Key, pair.Value);
                }
            }
            Debug.WriteLine("");
        }

        /// <summary>
        /// Fills in the uniqueTerms hashtable where keys are unique terms and values are 
        /// </summary>
        private void InitUniqueTerms()
        {
            int index = 0;
            uniqueTerms = new Hashtable();
            //filling uniqueTerms table 
            foreach (Hashtable doc in frequencyIndices)
            {
                foreach (DictionaryEntry item in doc)
                {
                    if (!uniqueTerms.ContainsKey(item.Key))
                        uniqueTerms.Add(item.Key, index++);
                }
            }
            Debug.WriteLine("uniqueTerms count: {0} ", uniqueTerms.Count);
            foreach (DictionaryEntry pair in uniqueTerms)
            {
                Debug.WriteLine("Term: {0} => index: {1}", pair.Key, pair.Value);
            }
            Debug.WriteLine("");
        }

        /// <summary>
        /// Fills in the frequencyMatrix with frequencies of each word in the set of documents
        /// </summary>
        private void InitFrequencyMatrix()
        {
            frequencyMatrix = new double[TotalTermsCount, DocCount];
            //filling frequencyMatrix
            int index = 0;
            foreach (Hashtable doc in frequencyIndices)
            {
                foreach (DictionaryEntry item in uniqueTerms)
                {
                    if (doc.ContainsKey(item.Key))
                        frequencyMatrix[(int)item.Value, index] = Convert.ToDouble(doc[item.Key]);
                    else
                        frequencyMatrix[(int)item.Value, index] = 0;
                }
                index++;
            }
            Debug.WriteLine("Non-Normalized FreqMatrix");
            for (int i = 0; i < TotalTermsCount; i++)
            {
                for (int j = 0; j < DocCount; j++)
                {
                    Debug.Write(String.Format("{0:0.0##}", frequencyMatrix[i, j]));
                    Debug.Write("\t");
                }
                Debug.WriteLine("");
            }
            Debug.WriteLine("");
            #if false
            int index = 0;
            Hashtable uniqueTerms = new Hashtable();
            //filling word_index_lookup table 
            foreach (Hashtable doc in frequencyIndices)
            {
                foreach (DictionaryEntry item in doc)
                {
                    if (!uniqueTerms.ContainsKey(item.Key))
                        uniqueTerms.Add(item.Key, index++);
                }
            }
            frequencyMatrix = new double[uniqueTerms.Count, frequencyIndices.Length];// [terms as rows; documents as cols]
            index = -1;
            foreach (Hashtable doc in frequencyIndices)
            {
                index++;
                foreach (DictionaryEntry item in uniqueTerms)
                {
                    if (doc.ContainsKey(item.Key))
                        frequencyMatrix[(int)item.Value, index] = Convert.ToDouble(doc[item.Key]);
                    else
                        frequencyMatrix[(int)item.Value, index] = 0;
                }
            }
            #endif
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
                for (int j = 1; j < TotalTermsCount; j++)// For each word that can be in the document
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
                for (int j = 0; j < TotalTermsCount; j++)//For each word that can be in the document
                    frequencyMatrix[j, i] /= maxFreqPerDoc[i];
            }
            Debug.WriteLine("NormalizeFreqMatrix");
            for (int i = 0; i < TotalTermsCount; i++)
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
            docCountPerTerm = new double[TotalTermsCount];
            for (int i = 0; i < TotalTermsCount; i++) // For each term i
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
            IDF = new double[TotalTermsCount];
            for (int i = 0; i < TotalTermsCount; i++)
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
            for (int i = 0; i < TotalTermsCount; i++)
                for (int j = 0; j < DocCount; j++)
                    frequencyMatrix[i, j] *= IDF[i];
            Debug.WriteLine("TF-IDF Matrix");
            for (int i = 0; i < TotalTermsCount; i++)
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
        /// 
        /// </summary>
        /// <param name="query"> A pre-processed query </param>
        /// <returns> The frequency vectory of the query </returns>
        private double[] TextToQueryVector(string query)
        {
            double[] queryVector = new double[TotalTermsCount];
            string[] query_text = query.Split(null);
            // Discard any terms in the query that are not avaiable in our indices then do whatever you wanna do
            foreach (string word in query_text)
            {
                if (uniqueTerms.ContainsKey(word))
                    queryVector[(int)uniqueTerms[word]] += 1;
            }
            return queryVector;
        }

        /// <summary>
        /// Normalizes the query to be properly used with the documents in this system.
        /// </summary>
        /// <param name="queryVector"> The user query to be normalized </param>
        private void NormalizeQuery(double[] queryVector)
        {
            Debug.Assert(queryVector.Length == TotalTermsCount);
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
            Debug.Assert(normalizedQueryVector.Length == TotalTermsCount);
            for (int i = 0; i < normalizedQueryVector.Length; i++)
                normalizedQueryVector[i] *= IDF[i];
        }

        /// <summary>
        /// Uses the TF-IDF table to know the distance between the query and each document in the system.
        /// </summary>
        /// <param name="queryVector"> The frequency of each word in the query </param>
        private void CalculateDistances(double[] queryVector)
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
            double[] sums = new double[DocCount];
            for (int i = 0; i < DocCount; i++) // For each document i
            {
                for (int j = 0; j < TotalTermsCount; j++) // For each term j
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
        /// Uses the TF-IDF table to calculate the consinal similarity between the query and each document in the system.
        /// </summary>
        /// <remarks>
        /// Basically applying the formula for calculating the cosine between two vectors over each document and the query - look it up :P 
        /// Generally considered better than knowing the E-distance (according to a research done at Stanford .. not cited here)
        /// </remarks>
        /// <param name="queryVector"> The frequency of each word in the query </param>
        private void CalculateConsinalSimilarity(double[] queryVector)
        {
            this.NormalizeQuery(queryVector);
            this.CalculateQueuryTfId(queryVector);
            Debug.WriteLine("Query we have after TF-IDF");
            foreach (var item in queryVector)
                Debug.WriteLine(item);
            Debug.WriteLine("");
            double[] nominator = new double[DocCount];
            Debug.WriteLine("Printing the nominator for d1 and the query");
            for (int i = 0; i < nominator.Length; i++) // For each document
            {
                for (int j = 0; j < TotalTermsCount; j++) // For each term
                {
                    if (i == 0)
                    {
                        Debug.WriteLine("{0} * {1}", frequencyMatrix[j, i], queryVector[j]);
                    }
                    nominator[i] += queryVector[j] * frequencyMatrix[j, i];
                }
            }
            Debug.WriteLine("");
            double denominatorPartTwo = 0;
            foreach (var item in queryVector)
            {
                denominatorPartTwo += Math.Pow(item, 2);
            }
            denominatorPartTwo = Math.Sqrt(denominatorPartTwo);
            Debug.WriteLine("denominatorPartTwo = {0}", denominatorPartTwo);
            Debug.WriteLine("");
            double[] denominatorPartOne = new double[DocCount];
            for (int i = 0; i < denominatorPartOne.Length; i++) // For each document
            {
                for (int j = 0; j < TotalTermsCount; j++) // For each term
                {
                    denominatorPartOne[i] += Math.Pow(frequencyMatrix[j, i], 2);
                }
                denominatorPartOne[i] = Math.Sqrt(denominatorPartOne[i]);
            }
            for (int i = 0; i < DocCount; i++)
            {
                this.distances[i].distance = (nominator[i]) / ((denominatorPartOne[i]) * (denominatorPartTwo));
            }
        }

        /// <summary>
        /// Queries the documents to see what documents are most relevant to the user query
        /// </summary>
        /// <param name="query"> The users pre-processed query </param>
        /// <param name="threshold"> The amount of the relevant documents to return; by default it's 0 which means return all documents </param>
        /// <param name="useVectorDistance"> Set to true if you want to use VectorDistance, otherwise the system will use Cosine similarity </param>
        /// <returns> Array of DocumentDistances sorted where the first element is the most relevant and the last is the least relevant </returns>
        ///  <exception cref="System.ArgumentNullException"> Thrown if query is null</exception>
        /// <exception cref="System.ArgumentException"> Thrown if the query is an empty string </exception>
        public DocumentDistance[] GetRelevantDocuments(string query, int threshold = 0, bool useVectorDistance = false)
        {
            if (query == null)
                throw new ArgumentNullException("Parameter cannot be null!", "query");
            if (query.Length == 0)
                throw new ArgumentException("Parameter cannot be of length 0", "query");
            double[] queryVector = this.TextToQueryVector(query);
            if (useVectorDistance)
            {
                this.CalculateDistances(queryVector);
                Array.Sort(this.distances);
            }
            else
            {
                this.CalculateConsinalSimilarity(queryVector);
                Array.Sort(this.distances);
                // In CalculateConsinalSimilarity, the documents that are most relevant have a higher number and our sort is an ascending sort 
                // so we reverse the array after sorting to have the most relevant first
                Array.Reverse(this.distances);
            }
            return (threshold <= 0) ? this.distances : this.distances.Take(threshold).ToArray();
        }
    }
}
