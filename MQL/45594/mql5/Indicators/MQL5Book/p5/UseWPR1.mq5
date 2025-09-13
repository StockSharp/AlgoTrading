//+------------------------------------------------------------------+
//|                                                      UseWPR1.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_separate_window
#property indicator_buffers 0
#property indicator_plots   0

#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // all handles are local variables since they are not used in this intro

   // absolute path: since indicator examples for this book is stored at
   // MQL5/Indicators/MQL5Book/p5, the complete pathname is as follows:
   int handle1 = PRTF(iCustom(_Symbol, _Period, "/Indicators/MQL5Book/p5/IndWPR"));
   
   // relative path: consists of the single name, searched in current folder
   // the caller indicator and the callee indicator are in the same folder
   int handle2 = PRTF(iCustom(_Symbol, _Period, "IndWPR"));

   // relative path: consists of the book subfolders and the name,
   // will find proper indicator in the context of MQL5/Indicators
   int handle3 = PRTF(iCustom(_Symbol, _Period, "MQL5Book/p5/IndWPR"));
   
   // wrong path: no such indicator in current folder (runtime error 4802)
   int handle4 = PRTF(iCustom(_Symbol, _Period, "IndWPR NonExistent"));

   // wrong path: backslashes are not escaped, every '\' should be '\\'
   // 4 compiler warnings in a row: unrecognized character escape sequence
   int handle5 = PRTF(iCustom(_Symbol, _Period, "\Indicators\MQL5Book\p5\IndWPR"));

   // this is a twin of handle2, check how their values are the same
   int handle6 = PRTF(iCustom(_Symbol, _Period, "IndWPR"));
   
   return INIT_SUCCEEDED;
}
//+------------------------------------------------------------------+
/*
   output:
   
   iCustom(_Symbol,_Period,/Indicators/MQL5Book/p5/IndWPR)=10 / ok
   iCustom(_Symbol,_Period,IndWPR)=11 / ok
   iCustom(_Symbol,_Period,MQL5Book/p5/IndWPR)=12 / ok
   cannot load custom indicator 'IndWPR NonExistent' [4802]
   iCustom(_Symbol,_Period,IndWPR NonExistent)=-1 / INDICATOR_CANNOT_CREATE(4802)
   iCustom(_Symbol,_Period,\Indicators\MQL5Book\p5\IndWPR)=13 / ok
   iCustom(_Symbol,_Period,IndWPR)=11 / ok
*/

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   return rates_total;
}
//+------------------------------------------------------------------+
