//+------------------------------------------------------------------+
//|                                                     FileText.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/FileHandle.mqh>

const string texts[] =
{
   "MQL5Book/ansi1252.txt", // ANSI-1252
   "MQL5Book/unicode1.txt", // Unicode, BOM 0xFFFE
   "MQL5Book/unicode2.txt", // Unicode, no BOM
   "MQL5Book/unicode3.txt", // Unicode, BOM 0xFEFF
   "MQL5Book/utf8.txt"      // UTF-8
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("=====> UTF-8");
   for(int i = 0; i < ArraySize(texts); ++i)
   {
      FileHandle fh(FileOpen(texts[i], FILE_READ | FILE_TXT | FILE_ANSI, 0, CP_UTF8));
      Print(texts[i], " -> ", FileReadString(~fh));
   }

   Print("=====> Unicode");
   for(int i = 0; i < ArraySize(texts); ++i)
   {
      FileHandle fh(FileOpen(texts[i], FILE_READ | FILE_TXT | FILE_UNICODE));
      Print(texts[i], " -> ", FileReadString(~fh));
   }

   Print("=====> ANSI/1252");
   for(int i = 0; i < ArraySize(texts); ++i)
   {
      FileHandle fh(FileOpen(texts[i], FILE_READ | FILE_TXT | FILE_ANSI, 0, 1252));
      Print(texts[i], " -> ", FileReadString(~fh));
   }

   Print("=====> RAW");
   for(int i = 0; i < ArraySize(texts); ++i)
   {
      uchar bytes[];
      int n = (int)FileLoad(texts[i], bytes);
      ArrayResize(bytes, n); // shrink if necessary
      Print(texts[i]);
      ByteArrayPrint(bytes);
   }
}

//+------------------------------------------------------------------+
//| Output byte array in hex                                         |
//+------------------------------------------------------------------+
void ByteArrayPrint(const uchar &bytes[],
                    const int row = 16, const string separator = " | ",
                    const uint start = 0, const uint count = WHOLE_ARRAY)
{
   string hex = "";
   const int n = (int)MathCeil(MathLog10(ArraySize(bytes) + 1));
   for(uint i = start; i < MathMin(start + count, ArraySize(bytes)); ++i)
   {
      if(i % row == 0 || i == start)
      {
         if(hex != "") Print(hex);
         hex = StringFormat("[%0*d]", n, i) + " ";
      }
      hex += StringFormat("%02X", bytes[i]) + separator;
   }
   if(hex != "") Print(hex);
}
//+------------------------------------------------------------------+
