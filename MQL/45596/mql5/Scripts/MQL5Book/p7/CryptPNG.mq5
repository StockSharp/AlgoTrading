//+------------------------------------------------------------------+
//|                                                     CryptPNG.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#resource "\\Images\\euro.bmp"

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/PNG.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   uchar null[];      // empty key
   uchar result[];    // receiving 'packed' array
   uint data[];       // source pixels
   uchar bytes[];     // source bytes
   int width, height;
   PRTF(ResourceReadImage("::Images\\euro.bmp", data, width, height));
   
   ArrayResize(bytes, ArraySize(data) * 3 + width);
   ArrayInitialize(bytes, 0);
   int j = 0;
   for(int i = 0; i < ArraySize(data); ++i)
   {
      if(i % width == 0) bytes[j++] = 0; // prepend 0 filter-type byte to each scanline
      const uint c = data[i];
      // bytes[j++] = (uchar)((c >> 24) & 0xFF); // alpha, for PNG_CTYPE_TRUECOLORALPHA (ARGB)
      bytes[j++] = (uchar)((c >> 16) & 0xFF);
      bytes[j++] = (uchar)((c >> 8) & 0xFF);
      bytes[j++] = (uchar)(c & 0xFF);
   }
   
   PRTF(CryptEncode(CRYPT_ARCH_ZIP, bytes, null, result));
   
   int h = PRTF(FileOpen("my.png", FILE_BIN | FILE_WRITE));
   
   PNG::Image image(width, height, result); // by default PNG_CTYPE_TRUECOLOR (RGB)
   image.write(h);
   
   FileClose(h);
}
//+------------------------------------------------------------------+
