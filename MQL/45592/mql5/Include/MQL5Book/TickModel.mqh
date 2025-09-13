//+------------------------------------------------------------------+
//|                                                    TickModel.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/MqlError.mqh>

//+------------------------------------------------------------------+
//| Tick modelling options                                           |
//+------------------------------------------------------------------+
enum TICK_MODEL
{
   TICK_MODEL_UNKNOWN = -1,    /*Unknown (any)*/    // unknown/not detected yet
   TICK_MODEL_REAL = 0,        /*Real ticks*/       // best quality
   TICK_MODEL_GENERATED = 1,   /*Generated ticks*/  // good quality
   TICK_MODEL_OHLC_M1 = 2,     /*OHLC M1*/          // acceptable quality and fast
   TICK_MODEL_OPEN_PRICES = 3, /*Open prices*/      // worst quality, but super fast (take care of accuracy manually)
   TICK_MODEL_MATH_CALC = 4,   /*Math calculations*/// no ticks (undetectable)
};

//+------------------------------------------------------------------+
//| Tick model detector                                              |
//| NB: TICK_MODEL_OPEN_PRICES reported at 1-st tick can be          |
//| refined to TICK_MODEL_OHLC_M1 on the 2-nd tick                   |
//| All other models are reported persistently from the 1-st tick    |
//+------------------------------------------------------------------+
TICK_MODEL getTickModel()
{
   static TICK_MODEL model = TICK_MODEL_UNKNOWN;
   
   if(model == TICK_MODEL_UNKNOWN)
   {
      MqlTick ticks[];
      const int n = CopyTicks(_Symbol, ticks, COPY_TICKS_ALL, 0, 10);
      if(n == -1)
      {
         switch(_LastError)
         {
         case ERR_NOT_ENOUGH_MEMORY:    // emulated ticks
            model = TICK_MODEL_GENERATED;
            break;
            
         case ERR_FUNCTION_NOT_ALLOWED: // open or OHLC
            if(TimeCurrent() != iTime(_Symbol, _Period, 0))
            {
               model = TICK_MODEL_OHLC_M1;
            }
            else if(model == TICK_MODEL_UNKNOWN)
            {
               model = TICK_MODEL_OPEN_PRICES;
            }
            break;
         }
         
         Print(E2S(_LastError));
      }
      else
      {
         model = TICK_MODEL_REAL;
      }
   }
   else if(model == TICK_MODEL_OPEN_PRICES)
   {
      if(TimeCurrent() != iTime(_Symbol, _Period, 0))
      {
         model = TICK_MODEL_OHLC_M1;
      }
   }
   return model;
}
//+------------------------------------------------------------------+
