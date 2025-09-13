//+------------------------------------------------------------------+
//|                                              ConversionColor.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| ARGB equivalent representation                                   |
//+------------------------------------------------------------------+
struct Argb
{
   uchar BB;
   uchar GG;
   uchar RR;
   uchar AA;
};

//+------------------------------------------------------------------+
//| Split ARGB value into components                                 |
//+------------------------------------------------------------------+
union ColorARGB
{
   uint value;
   uchar channels[4]; // 0 - BB, 1 - GG, 2 - RR, 3 - AA
   Argb split[1];
   ColorARGB(uint u) : value(u) { }
};

#define ARGBToColor(U) ((color)((((U) & 0xFF) << 16) | ((U) & 0xFF00) | (((U) >> 16) & 0xFF)))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRT(ColorToString(clrBlue));          // 0,0,255
   PRT(ColorToString(C'0,0,255', true)); // clrBlue
   PRT(ColorToString(C'0,0,250'));       // 0,0,250
   PRT(ColorToString(C'0,0,250', true)); // 0,0,250 (no such color name)
   PRT(ColorToString(0x34AB6821, true)); // 33,104,171 (0x21,0x68,0xAB)

   PRT(StringToColor("0,0,255")); // clrBlue
   PRT(StringToColor("clrBlue")); // clrBlue
   PRT(StringToColor("Blue"));    // clrBlack (no such color name)
   // excessive text is skipped
   PRT(StringToColor("255,255,255 more text"));      // clrWhite
   PRT(StringToColor("This is color: 128,128,128")); // clrGray

   uint u = ColorToARGB(clrBlue);
   PrintFormat("ARGB1=%X", u); // ARGB1=FF0000FF
   ColorARGB clr1(u);
   ArrayPrint(clr1.split);
   /*
       [BB] [GG] [RR] [AA]
   [0]  255    0    0  255
   */

   u = ColorToARGB(clrDeepSkyBlue, 0x40);
   PrintFormat("ARGB2=%X", u); // ARGB2=4000BFFF
   ColorARGB clr2(u);
   ArrayPrint(clr2.split);
   /*
       [BB] [GG] [RR] [AA]
   [0]  255  191    0   64
   */
   Print(ARGBToColor(u)); // clrDeepSkyBlue
}
//+------------------------------------------------------------------+
