using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExtractMapData
{
  class BY0002
  {
    private byte[] _data = null;
    private string _sector = "";
    private string[] _types = null;
    private string[] _values = null;

    public BY0002(string filename, string sector)
    {
      _data = File.ReadAllBytes(filename);
      _sector = sector;
    }

    public BY0002(byte[] data, string sector)
    {
      _sector = sector;
      _data = new byte[data.Length];
      Array.Copy(data, _data, data.Length);
    }

    public BY0002(Stream stream, string sector)
    {
      _sector = sector;
      long lTmp = stream.Position;
      stream.Position = 0;
      _data = new byte[stream.Length];
      stream.Read(_data, 0, _data.Length);
      stream.Position = lTmp;
    }

    public BY0002(Stream stream, bool revertPosition, long offset, int length, string sector)
    {
      _sector = sector;
      long lTmp = stream.Position;
      long lLength = length;

      stream.Position = offset;
      if (offset + lLength > stream.Length)
      {
        lLength = stream.Length - lLength;
      }
      _data = new byte[lLength];
      stream.Read(_data, 0, _data.Length);

      if (revertPosition)
      {
        stream.Position = lTmp;
      }
    }

    private short ReadShort(int offset)
    {
      return (short)((_data[offset + 1] << 0)
        | (_data[offset] << 8));
    }

    private int ReadInt(int offset)
    {
      return (_data[offset + 3] << 0)
        | (_data[offset + 2] << 8)
        | (_data[offset + 1] << 16)
        | (_data[offset + 0] << 24);
    }

    private uint ReadUInt(int offset)
    {
      byte[] bTmp = new byte[4];
      Array.Copy(_data, offset, bTmp, 0, 4);
      Array.Reverse(bTmp, 0, 4);
      return BitConverter.ToUInt32(bTmp, 0);
    }

    private float ReadFloat(int offset)
    {
      byte[] bTmp = new byte[4];
      Array.Copy(_data, offset, bTmp, 0, 4);
      Array.Reverse(bTmp, 0, 4);
      return BitConverter.ToSingle(bTmp, 0);
    }

    private string ReadNullterminatedString(int offset)
    {
      int iEnd = offset;
      while ((iEnd < _data.Length) &&
        !(_data[iEnd] == 0))
        iEnd++;
      return Encoding.ASCII.GetString(_data, offset, iEnd - offset);
    }

    private string[] ReadTypeC2(int offset)
    {
      int iNodeCount = ReadInt(offset);
      if ((iNodeCount & 0xC2000000) != 0xC2000000)
        return null;

      iNodeCount = iNodeCount & 0xffffff;
      string[] sStrings = new string[iNodeCount];
      for (int i = 0; i < iNodeCount; i++)
      {
        sStrings[i] = ReadNullterminatedString(ReadInt(offset + 4 + (i * 4)) + offset);
      }
      return sStrings;
    }

    private object[] ReadTypeC1(int offset)
    {
      int iNodeCount = ReadInt(offset);
      if ((iNodeCount & 0xC1000000) != 0xC1000000)
        return null;

      iNodeCount = iNodeCount & 0xffffff;
      if (iNodeCount > 0)
      {
        int iOffset = offset + 4;
        object[] oRet = new object[iNodeCount];
        for (int i = 0; i < iNodeCount; i++)
        {
          string sType = _types[ReadInt(iOffset) >> 8];
          switch (_data[iOffset + 3])
          {
            case 0xC0:
              oRet[i] = new Tuple<string, object>(sType, ReadTypeC0(ReadInt(iOffset + 4)));
              break;
            case 0xC1:
              oRet[i] = new Tuple<string, object>(sType, ReadTypeC1(ReadInt(iOffset + 4)));
              break;
            case 0xC2:
              oRet[i] = new Tuple<string, object>(sType, ReadTypeC2(ReadInt(iOffset + 4)));
              break;
            case 0xA0:
              oRet[i] = new Tuple<string, object>(sType, new Tuple<byte, int>(0xA0, ReadInt(iOffset + 4)));
              break;
            case 0xD0:
            case 0xD1:
            case 0xD2:
            case 0xD3:
              oRet[i] = new Tuple<string, object>(sType, new Tuple<byte, int>(0xA0, ReadInt(iOffset + 4)));
              break;
            default:
              break;
          }
          iOffset += 8;
        }
        return oRet;
      }
      return null;
    }

    private object[] ReadTypeC0(int offset)
    {
      int iNodeCount = ReadInt(offset);
      if ((iNodeCount & 0xC0000000) != 0xC0000000)
        return null;

      iNodeCount = iNodeCount & 0xffffff;

      int[] iOffsets = new int[iNodeCount];
      int iFinalNodes = 0;
      for (int i = 0; i < iNodeCount; i++)
      {
        int iType = _data[offset + 4 + i];
        iFinalNodes++;
        switch (iType)
        {
          case 0xC0:
          case 0xC1:
          case 0xC2:
          case 0xA0:
          case 0xD0:
          case 0xD1:
          case 0xD2:
          case 0xD3:
            break;
          default:
            iFinalNodes--;
            break;
        }
      }

      if (iFinalNodes > 0)
      {
        object[] oRet = new object[iFinalNodes];
        int iCountOffset = offset + 4 + iFinalNodes;
        iFinalNodes = 0;
        while ((iCountOffset & 0x3) != 0)
          iCountOffset++;
        for (int i = 0; i < iNodeCount; i++)
        {
          int iType = _data[offset + 4 + i];

          switch (iType)
          {
            case 0xC0:
              oRet[iFinalNodes] = ReadTypeC0(ReadInt(iCountOffset + (i * 4)));
              break;
            case 0xC1:
              oRet[iFinalNodes] = ReadTypeC1(ReadInt(iCountOffset + (i * 4)));
              break;
            case 0xC2:
              oRet[iFinalNodes] = ReadTypeC2(ReadInt(iCountOffset + (i * 4)));
              break;
            case 0xA0:
              oRet[i] = new Tuple<byte, int>(0xA0, ReadInt(iCountOffset + (i * 4)));
              break;
            case 0xD0:
            case 0xD1:
            case 0xD2:
            case 0xD3:
              oRet[i] = new Tuple<byte, int>(0xA0, ReadInt(iCountOffset + (i * 4)));
              break;
            default:
              iFinalNodes--;
              break;
          }
          iFinalNodes++;
        }
        return oRet;
      }

      return null;
    }

    private void Export(object part3)
    {
      if (part3 is object[])
      {
        object[] oa3 = (object[])part3;
        for (int i = 0; i < oa3.Length; i++)
        {
          if (oa3[i] is Tuple<string, object>)
          {
            Tuple<string, object> tpl1 = (Tuple<string, object>)oa3[i];
            if ((tpl1.Item1 == "Objs") && (tpl1.Item2 is object[]))
            {
              object[] oObjects = (object[])tpl1.Item2;
              for (int j = 0; j < oObjects.Length; j++)
              {
                if (oObjects[j] is object[])
                {
                  object[] oObject = (object[])oObjects[j];
                  string name = "";
                  float x = 0;
                  float y = 0;
                  float z = 0;
                  bool bHasName = false;
                  bool bHasCoordinates = false;
                  for (int k = 0; k < oObject.Length; k++)
                  {
                    if (oObject[k] is Tuple<string, object>)
                    {
                      Tuple<string, object> tProp = (Tuple<string, object>)oObject[k];
                      if (tProp.Item1 == "UnitConfigName")
                      {
                        if (tProp.Item2 is Tuple<byte, int>)
                        {
                          int iTmp = ((Tuple<byte, int>)tProp.Item2).Item2;
                          name = _values[iTmp];
                          bHasName = true;
                        }
                      }
                      else if (tProp.Item1 == "Translate")
                      {
                        if (tProp.Item2 is object[])
                        {
                          object[] oCoordinates = (object[])tProp.Item2;
                          if (oCoordinates.Length == 3)
                          {
                            byte[] bTmp = BitConverter.GetBytes(((Tuple<byte, int>)oCoordinates[0]).Item2);
                            x = BitConverter.ToSingle(bTmp, 0);
                            bTmp = BitConverter.GetBytes(((Tuple<byte, int>)oCoordinates[1]).Item2);
                            z = BitConverter.ToSingle(bTmp, 0);
                            bTmp = BitConverter.GetBytes(((Tuple<byte, int>)oCoordinates[2]).Item2);
                            y = BitConverter.ToSingle(bTmp, 0);
                            bHasCoordinates = true;
                          }
                        }
                      }
                    }
                  }
                  if (bHasCoordinates && bHasName)
                  {
                    Program._dynamicOutput.WriteLine(name + "\t" + x + "\t" + y + "\t" + z);
                  }
                }
              }
            }
          }
        }
      }
    }

    private object ReadOffset(int offset, int maximumoffset)
    {
      if (offset == 0) return null;
      int iLength = maximumoffset - offset;
      if (iLength == 0)
        return null;
      int iType = (ReadInt(offset) >> 24) & 0xFF;
      int iRemainder = (ReadInt(offset) & 0x00FFFFFF);
      switch (iType)
      {
        case 0xC0:
          return ReadTypeC0(offset);
        case 0xC1:
          return ReadTypeC1(offset);
        case 0xC2:
          return ReadTypeC2(offset);
        default:
          Console.WriteLine(Convert.ToString(iType, 16));
          break;
      }
      return null;
    }

    public bool Parse()
    {
      if (_data.Length < 0x10)
        return false;
      if (_data[0] != 0x42 || _data[1] != 0x59 || _data[2] != 0x00 || _data[3] != 0x02)
        return false;

      int offsetA = ReadInt(4);
      int offsetB = ReadInt(8);
      int offsetC = ReadInt(12);
      if (offsetA > _data.Length)
        return false;
      if (offsetB > _data.Length)
        return false;
      if (offsetC > _data.Length)
        return false;

      object oA1 = ReadOffset(offsetA, offsetB != 0 ? offsetB : (offsetC != 0 ? offsetC : _data.Length));
      if (oA1 != null)
      {
        if (oA1 is string[])
        {
          _types = (string[])oA1;
        }
        else
        {
          Console.WriteLine("Fail");
        }
      }
      object oA2 = ReadOffset(offsetB, offsetC != 0 ? offsetC : _data.Length);
      if (oA2 != null)
      {
        if (oA2 is string[])
        {
          _values = (string[])oA2;
        }
        else
        {
          Console.WriteLine("Fail");
        }
      }
      object oA3 = ReadOffset(offsetC, _data.Length);
      Export(oA3);
      return true;
    }

  }
}
