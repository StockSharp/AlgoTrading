//+------------------------------------------------------------------+
//|                                                      UseWPR2.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_separate_window
#property indicator_buffers 0
#property indicator_plots   0

#include <MQL5Book/PRTF.mqh>

// parameter to pass into subordinate indicator (0 is deliberately incorrect for IndWPR)
input int WPRPeriod = 0;

// global variable for subordinate indicator
int handle;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // use most simple way of creation with the name only, and the parameter
   handle = PRTF(iCustom(_Symbol, _Period, "IndWPR", WPRPeriod));
   // the next check should not be done here normally, because we need to wait
   // when the indicator is get executed and calculated (only for demo purpose)
   PRTF(BarsCalculated(handle));
   return handle == INVALID_HANDLE ? INIT_FAILED : INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   // wait until the subindicator is calculated for all bars
   if(PRTF(BarsCalculated(handle)) != PRTF(rates_total))
   {
      return prev_calculated;
   }
   
   // ... here will go normal processing relying on the handle
   
   return rates_total;
}
//+------------------------------------------------------------------+
/*
   example output 1 (for WPRPeriod = 0):

   iCustom(_Symbol,_Period,IndWPR,WPRPeriod)=10 / ok
   BarsCalculated(handle)=-1 / INDICATOR_DATA_NOT_FOUND(4806)
   Alert: Incorrect Period value (0). Should be 1 or larger
   BarsCalculated(handle)=0 / ok
   rates_total=20000 / ok
   
   example output 2:
   iCustom(_Symbol,_Period,IndWPR,WPRPeriod)=10 / ok
   BarsCalculated(handle)=-1 / INDICATOR_DATA_NOT_FOUND(4806)
   BarsCalculated(handle)=20000 / ok
   rates_total=20000 / ok

*/