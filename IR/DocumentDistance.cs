using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.IO;

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
