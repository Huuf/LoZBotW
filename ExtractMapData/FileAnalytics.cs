using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractMapData
{
  static class FileAnalytics
  {
    public static void ParseFile(string basename, Stream sr)
    {
      string sID = null;
      if (sr.Length >= 4)
      {
        byte[] bHeader = new byte[4];
        sr.Read(bHeader, 0, 4);
        for (int i = 0; i < 4; i++)
        {
          if (bHeader[i] < 0x20 || bHeader[i] >= 0x7F)
          {
            sID += "0x" + Convert.ToString(bHeader[i], 16).PadLeft(2, '0');
          }
          else
          {
            sID += (char)bHeader[i];
          }
        }
      }

      switch (sID)
      {
        case "BY0x000x02":
          {
            string[] sTmpArray = basename.Split('\\');
            BY0002 by = new BY0002(sr, ""); //sTmpArray[6]
            if (by.Parse())
            {
            }
          }
          break;
        case "PrOD":
          {
            PrOD prod = new PrOD(sr);
            if (prod.Parse())
            {
            }
          }
          break;
        case "Yaz0":
          sr.Position = 0;
          Yaz0 yaz = new Yaz0(sr);
          if (yaz.Parse() && yaz.decompressed != null)
          {
            bool bValidAscii = true;
            for (int i = 0; bValidAscii && i < 4; i++)
            {
              if (yaz.decompressed[i] < 0x20 || yaz.decompressed[i] >= 0x7F)
              {
                bValidAscii = false;
              }
            }
            ParseFile(basename, new MemoryStream(yaz.decompressed));
            return;
          }
          string sAdd2 = "YAZ0 Not long enough";
          return;

        default:
          return;
      }
    }

    public static void ParseFile(string filename)
    {
      StreamReader sr = new StreamReader(filename);
      ParseFile(filename, sr.BaseStream);
      sr.Close();
    }
  }
}
