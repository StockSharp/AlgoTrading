//+------------------------------------------------------------------+
//|                                                     MathSwap.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Integer value to byte array                                      |
//+------------------------------------------------------------------+
template<typename T>
union ByteOverlay
{
   T value;
   uchar bytes[sizeof(T)];
   ByteOverlay(const T v) : value(v) { }
   void operator=(const T v) { value = v; }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const uint ui = 0x12345678;
   const ulong ul = 0x0123456789ABCDEF;

   ByteOverlay<uint> bo(ui);
   ArrayPrint(bo.bytes); // 120  86  52  18 <==> 0x78 0x56 0x34 0x12
   bo = MathSwap(ui);
   ArrayPrint(bo.bytes); //  18  52  86 120 <==> 0x12 0x34 0x56 0x78

   PrintFormat("%I32X -> %I32X", ui, MathSwap(ui));
   PrintFormat("%I64X -> %I64X", ul, MathSwap(ul));
   /*
   12345678 -> 78563412
   123456789ABCDEF -> EFCDAB8967452301
   */
}
//+------------------------------------------------------------------+
