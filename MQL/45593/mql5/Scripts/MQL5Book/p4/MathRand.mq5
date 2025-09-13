//+------------------------------------------------------------------+
//|                                                     MathRand.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define LIMIT 1000 // number of trials
#define STATS 10   // number of backets

int stats[STATS] = {};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int bucket = 32767 / STATS;
   // reset generator
   MathSrand((int)TimeLocal());
   // repeat predefined number of trials
   for(int i = 0; i < LIMIT; ++i)
   {
      // get a random number and update distribution stats
      stats[MathRand() / bucket]++;
   }
   ArrayPrint(stats);
   
   /*
      outputs for 3 different runs (every time will show new resuls):
      
       96  93 117  76  98  88 104 124 113  91
      110  81 106  88 103  90 105 102 106 109
       89  98  98 107 114  90 101 106  93 104
   */
}
//+------------------------------------------------------------------+
