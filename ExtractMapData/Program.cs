using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExtractMapData
{
  class Program
  {
    public static StreamWriter _dynamicOutput = null;
    public static StreamWriter _clusteringOutput = null;

    static void ExportMaps(string directory)
    {
      string[] sDirs = Directory.GetDirectories(directory);

      for (int i = 0; i < sDirs.Length; i++)
      {
        ExportMaps(sDirs[i]);
      }

      string[] sFiles = Directory.GetFiles(directory);
      for (int i = 0; i < sFiles.Length; i++)
      {
        if ((Path.GetExtension(sFiles[i]) == ".sblwp") || (Path.GetExtension(sFiles[i]) == ".smubin"))
        {
          FileAnalytics.ParseFile(sFiles[i]);
        }
      }
    }

    static void Main(string[] args)
    {
      string sDir = ".";
      _dynamicOutput = new StreamWriter(sDir + "/Dynamic.tsv");
      _clusteringOutput = new StreamWriter(sDir + "/Clustered.tsv");
      _dynamicOutput.WriteLine("Name\tx\ty\tz");
      _clusteringOutput.WriteLine("Name\tx\ty\tz");

      if (!Directory.Exists(sDir + "/content"))
      {
        Console.WriteLine("Please place the executable in the folder with content, code and meta");
        Console.ReadLine();
        return;
      }

      if (!Directory.Exists(sDir + "/content/Map"))
      {
        Console.WriteLine("Map folder not found in Contents");
        Console.ReadLine();
        return;
      }

      ExportMaps(sDir + "/content/Map");

      _clusteringOutput.Flush();
      _clusteringOutput.Close();
      _dynamicOutput.Flush();
      _dynamicOutput.Close();
    }
  }
}
