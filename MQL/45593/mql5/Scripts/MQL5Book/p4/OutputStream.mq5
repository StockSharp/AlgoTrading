//+------------------------------------------------------------------+
//|                                                 OutputStream.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/OutputStream.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   OutputStream os(5, ',');
   
   bool b = true;
   datetime dt = TimeCurrent();
   color clr = C'127,128,129';
   int array[] = {100, 0, -100};

   os << M_PI << "text" << clrBlue << b << array << dt << clr << '@' << os;
   
   /*
      example output
      
      3.14159,text,clrBlue,true
      [100,0,-100]
      2021.09.07 17:38,clr7F8081,@
   */
}
//+------------------------------------------------------------------+
