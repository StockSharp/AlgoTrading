//+------------------------------------------------------------------+
//|                                                  ReInitChart.mq5 |
//|                             Copyright (c) 2009, Vladimir Gomonov |
//|                                            MetaDriver@rambler.ru |
//+------------------------------------------------------------------+
#property copyright "(c) 2009, Vladimir Gomonov     mail:  MetaDriver@rambler.ru"
#property link      "MetaDriver@rambler.ru"
#property version   "1.00"
#property description "The Expert Advisor adds a button "
#property description "to the bottom right corner of each chart."
#property description "After press of the button press it reinitializes"
#property description "all charts of the chart symbol."
#property description "The chart reinitialization is performed"
#property description "by temporary change of the chart timeframe."

//--- input parameters
input string   SampleText="Recalculate";      // Button text
input color    SampleTextColor=NavajoWhite;   // Text color
input color    SampleBackColor=SlateGray;     // Button background

#include "ReinitClass.mqh"

string ButtonName="ButtonReDraw";

cChartReInit cr;
//+------------------------------------------------------------------+
//| Expert Advisor initializaton function                            |
//+------------------------------------------------------------------+
int OnInit()
  {
   cr.Init(ButtonName,SampleText,SampleTextColor,SampleBackColor);
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert Advisor deinitializaton function                          |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   cr.Deinit();
  }
//+------------------------------------------------------------------+
//| OnTimer event handler                                            |
//+------------------------------------------------------------------+
void OnTimer()
  {
   cr.Run();
  }
//+------------------------------------------------------------------+
