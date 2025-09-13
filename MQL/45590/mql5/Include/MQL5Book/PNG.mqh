//+------------------------------------------------------------------+
//|                                                          PNG.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/CRC32.mqh>

#define PNG_CTYPE_TRUECOLOR      2
#define PNG_CTYPE_TRUECOLORALPHA 6

namespace PNG
{

const uchar Signature[] = {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};

//+------------------------------------------------------------------+
//| Chunk type                                                       |
//| supported chuck types:                                           |
//| - 0x49484452 'IHDR'                                              |
//| - 0x49444154 'IDAT'                                              |
//| - 0x49454E44 'IEND' (crc=0xAE426082)                             |
//+------------------------------------------------------------------+
union ChunkType
{
   uint num;
   uchar bytes[4];
   ChunkType(uint n): num(MathSwap(n)) { }
};

//+------------------------------------------------------------------+
//| Base chunk                                                       |
//+------------------------------------------------------------------+
struct Chunk
{
   uint length;     // size of content in bytes
   ChunkType type;
   // followed by the following in descendants:
   // C content;    // uchar data[];
   // uint crc;     // CRC32 of type and content
   Chunk(const uint t, const uint l = 0): type(t), length(l) {}
   void write(const int h)
   {
      FileWriteInteger(h, MathSwap(length));
      FileWriteInteger(h, type.num);
   }
};

//+------------------------------------------------------------------+
//| IHDR chunk fields                                                |
//+------------------------------------------------------------------+
struct IHDRfields
{
   uint width, height;
   uchar depth;
   uchar ctype;
   uchar compression;
   uchar filter;
   uchar interlace;
};

//+------------------------------------------------------------------+
//| IHDR chunk bytes                                                 |
//+------------------------------------------------------------------+
union IHDRbytes
{
   IHDRfields ihdr;
   uchar bytes[13];
};

//+------------------------------------------------------------------+
//| IHDR chunk                                                       |
//+------------------------------------------------------------------+
struct IHDR: public Chunk
{
   IHDRbytes hb;

   IHDR(const uint w, const uint h, const uchar c = PNG_CTYPE_TRUECOLOR, const uchar d = 8) :
      Chunk(0x49484452, 13)
   {
      IHDRbytes hd = {{MathSwap(w), MathSwap(h), d, c, 0, 0, 0}};
      hb = hd;
   }
   
   uint size() const
   {
      return 13;
   }
   
   void write(const int h)
   {
      Chunk::write(h);
      FileWriteArray(h, hb.bytes);
      CRC32 crc32;
      const uint c0 = crc32.compute(type.bytes);
      const uint c1 = crc32.compute(hb.bytes, c0) ^ 0xFFFFFFFF;
      FileWriteInteger(h, MathSwap(c1));
   }
};

//+------------------------------------------------------------------+
//| IDAT chunk                                                       |
//+------------------------------------------------------------------+
struct IDAT: public Chunk
{
   uchar raw[];
   IDAT(uchar &data[]) : Chunk(0x49444154, ArraySize(data))
   {
      // deflated block begins with 2 bytes:
      // compression method and check bits,
      // then image data follows trailed by Adler32 checksum
      ArraySwap(data, raw);
   }
   uint size() const
   {
      return ArraySize(raw);
   }
   void write(const int h)
   {
      Chunk::write(h);
      FileWriteArray(h, raw);
      CRC32 crc32;
      const uint c0 = crc32.compute(type.bytes);
      const uint c1 = crc32.compute(raw, c0) ^ 0xFFFFFFFF;
      FileWriteInteger(h, MathSwap(c1));
   }
};

//+------------------------------------------------------------------+
//| IEND chunk                                                       |
//+------------------------------------------------------------------+
struct IEND: public Chunk
{
   IEND(): Chunk(0x49454E44) { }
   uint size() const
   {
      return 0;
   }
   void write(const int h)
   {
      Chunk::write(h);
      /*
      CRC32 crc32;
      const uint c0 = crc32.compute(type.bytes) ^ 0xFFFFFFFF;
      PrintFormat("%X", c0);
      */
      FileWriteInteger(h, MathSwap(0xAE426082)); // crc is constant here
   }
};

//+------------------------------------------------------------------+
//| Minimalistic PNG image                                           |
//+------------------------------------------------------------------+
struct Image
{
   PNG::IHDR ihdr;
   PNG::IDAT idat;
   PNG::IEND iend;

   Image(const int w, const int h, uchar &data[],
      const uchar c = PNG_CTYPE_TRUECOLOR, const uchar d = 8): ihdr(w, h, c, d), idat(data)
   {
   }
   
   void write(const int h)
   {
      FileWriteArray(h, Signature);
      ihdr.write(h);
      idat.write(h);
      iend.write(h);
   }
};

} // namespace
//+------------------------------------------------------------------+
