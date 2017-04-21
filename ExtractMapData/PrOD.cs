using System;
using System.IO;
using System.Text;

namespace ExtractMapData
{
  class PrOD
  {
    private byte[] _data = null;
    private string[] _items = null;
    public ItemData[] Items = null;
    public class ItemData
    {
      public int Unknown0;
      public float coor1;
      public float coor2;
      public float coor3;
      public float coor4;
      public float coor5;
      public float coor6;
      public int Unknown28;
      public string name;

      internal ItemData(string name, int unk0, float c1, float c2, float c3, float c4, float c5, float c6, int unk28)
      {
        this.name = name;
        this.Unknown0 = unk0;
        coor1 = c1;
        coor2 = c2;
        coor3 = c3;
        coor4 = c4;
        coor5 = c5;
        coor6 = c6;
        Unknown28 = unk28;
      }
    }

    public PrOD(string filename)
    {
      _data = File.ReadAllBytes(filename);
    }

    public PrOD(byte[] data)
    {
      _data = new byte[data.Length];
      Array.Copy(data, _data, data.Length);
    }

    public PrOD(Stream stream)
    {
      long lTmp = stream.Position;
      stream.Position = 0;
      _data = new byte[stream.Length];
      stream.Read(_data, 0, _data.Length);
      stream.Position = lTmp;
    }

    public PrOD(Stream stream, bool revertPosition, long offset, int length)
    {
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

    public bool Parse()
    {
      if (_data.Length < 20) //Header required
      {
        _data = null;
        return false;
      }
      if (Encoding.ASCII.GetString(_data, 0, 4) != "PrOD")
      {
        _data = null;
        return false;
      }
      if (ReadInt(16) != _data.Length)
      {
        _data = null;
        return false;
      }
      if (ReadInt(4) != 0x1000000)
      {
        _data = null;
        return false;
      }
      if (ReadInt(8) != 1)
      {
        _data = null;
        return false;
      }

      int iStringOffset = ReadInt(12) + 12;
      iStringOffset += 4;
      _items = new string[ReadInt(20)];
      int iItemsOffset = 28;

      for (int i = 0; i < _items.Length; i++)
      {
        int iNumber = ReadInt(iItemsOffset + 8);
        iItemsOffset += 16;
        _items[i] = "";
        Items = new ItemData[iNumber];
        while (_data[iStringOffset] != 0)
        {
          _items[i] += (char)_data[iStringOffset];
          iStringOffset++;
        }
        for (int j = 0; j < iNumber; j++)
        {
          float x = ReadFloat(iItemsOffset + 4);
          float z = ReadFloat(iItemsOffset + 8);
          float y = ReadFloat(iItemsOffset + 12);
          //Rotation?
          float x1 = ReadFloat(iItemsOffset + 16);
          float z1 = ReadFloat(iItemsOffset + 20);
          float y1 = ReadFloat(iItemsOffset + 24);

          Items[j] = new ItemData(_items[i], ReadInt(iItemsOffset), x, z, y, x1, z1, y1, ReadInt(iItemsOffset + 28));
          iItemsOffset += (8 * 4);

          Program._clusteringOutput.WriteLine(_items[i] + "\t" + x + "\t" + y + "\t" + z);
        }
        if (i + 1 < _items.Length)
        {
          iStringOffset += 2;
          while ((iStringOffset & 0x3) != 0)
          {
            iStringOffset++;
          }
        }
      }


      _data = null;
      return true;
    }
  }
}
