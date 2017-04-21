using System;
using System.IO;
using System.Text;

namespace ExtractMapData
{
  class Yaz0
  {
    private Stream stream;
    public byte[] decompressed;

    public Yaz0(Stream stream)
    {
      this.stream = stream;
    }

    private bool Decompress(byte[] data)
    {
      int iSrc = 0;
      int iDst = 0;
      int iSrcEnd = data.Length;
      int iDstEnd = decompressed.Length;
      byte bCode = 0;
      int iCodeLength = 0;

      while ((iSrc < iSrcEnd) && (iDst < iDstEnd)) {
        if (iCodeLength == 0) {
          bCode = data[iSrc++];
          iCodeLength = 7;
        }
        else {
          iCodeLength--;
        }

        if ((bCode & 0x80) != 0) {
          decompressed[iDst++] = data[iSrc++];
        }
        else {
          byte b1 = data[iSrc++];
          byte b2 = data[iSrc++];
          int iCopySrc = iDst - (((b1 & 0x0F) << 8) | b2) - 1;
          int n = b1 >> 4;
          if (n == 0) {
            n = data[iSrc++] + 0x12;
          }
          else {
            n += 2;
          }

          if (iCopySrc < 0) return false;

          if (iDst + n > iDstEnd) {
            while (iDst < iDstEnd) {
              decompressed[iDst++] = decompressed[iCopySrc++];
            }
            return false;
          }

          while (n-- > 0) {
            decompressed[iDst++] = decompressed[iCopySrc++];
          }
        }
        bCode <<= 1;
      }

      return true;
    }

    public bool Parse()
    {
      try
      {
        byte[] bHeader = new byte[4];
        stream.Read(bHeader, 0, 4);
        if (ASCIIEncoding.ASCII.GetString(bHeader) != "Yaz0")
        {
          return false;
        }
        byte[] bSize = new byte[4];
        stream.Read(bSize, 0, 4);
        Array.Reverse(bSize);
        uint uDecompressedSize = BitConverter.ToUInt32(bSize, 0);
        decompressed = new byte[uDecompressedSize];
        byte[] bPadding = new byte[8];
        stream.Read(bPadding, 0, 8);
        byte[] bCompressed = new byte[stream.Length - 4 - 4 - 8];
        stream.Read(bCompressed, 0, bCompressed.Length);
        return Decompress(bCompressed);
      }
      catch
      {
        return false;
      }
    }
  }
}
