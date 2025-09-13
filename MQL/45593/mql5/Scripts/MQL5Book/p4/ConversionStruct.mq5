//+------------------------------------------------------------------+
//|                                             ConversionStruct.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Date and Time including milliseconds                             |
//+------------------------------------------------------------------+
struct DateTimeMsc
{
   MqlDateTime mdt;
   int msc;
   DateTimeMsc() : msc(0)
   {
      ZeroMemory(mdt);
   }
   DateTimeMsc(MqlDateTime &init, int m = 0) : msc(m)
   {
      mdt = init;
   }
};

//+------------------------------------------------------------------+
//| Convert datetime to MqlDateTime                                  |
//+------------------------------------------------------------------+
MqlDateTime TimeToStructInplace(datetime dt)
{
   static MqlDateTime m;
   if(!TimeToStruct(dt, m))
   {
      static MqlDateTime z = {};
      return z;
   }
   return m;
}

#define MDT(T) TimeToStructInplace(T)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   DateTimeMsc test(MDT(D'2021.01.01 10:10:15'), 123);
   uchar a[];
   Print(StructToCharArray(test, a));
   Print(ArraySize(a));
   ArrayPrint(a);
   /*
   outputs (array is reformatted):
   true
   36
   229   7   0   0
     1   0   0   0
     1   0   0   0
    10   0   0   0
    10   0   0   0
    15   0   0   0
     5   0   0   0
     0   0   0   0
   123   0   0   0
   */
   
   DateTimeMsc receiver;
   Print(CharArrayToStruct(receiver, a)); // true
   Print(StructToTime(receiver.mdt), "'", receiver.msc);
}
//+------------------------------------------------------------------+
