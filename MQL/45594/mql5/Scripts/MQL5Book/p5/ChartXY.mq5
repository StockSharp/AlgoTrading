//+------------------------------------------------------------------+
//|                                                      ChartXY.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int w1 = PRTF(ChartWindowOnDropped());
   const datetime t1 = PRTF(ChartTimeOnDropped());
   const double p1 = PRTF(ChartPriceOnDropped());
   const int x1 = PRTF(ChartXOnDropped());
   const int y1 = PRTF(ChartYOnDropped());
   
   // convert XY to TP
   int w2;
   datetime t2;
   double p2;
   PRTF(ChartXYToTimePrice(0, x1, y1, w2, t2, p2));
   Print(w2, " ", p2, " ", t2);
   PRTF(w1 == w2 && t1 == t2 && p1 == p2);
   
   // convert TP to XY
   int x2, y2;
   PRTF(ChartTimePriceToXY(0, w1, t1, p1, x2, y2));
   Print(x2, " ", y2);
   PRTF(x1 == x2 && y1 == y2);

   // round trip conversions (both ways)
   int w3;
   datetime t3;
   double p3;
   PRTF(ChartXYToTimePrice(0, x2, y2, w3, t3, p3));
   Print(w3, " ", p3, " ", t3);
   PRTF(w1 == w3 && t1 == t3 && p1 == p3);

   int x3, y3;
   PRTF(ChartTimePriceToXY(0, w2, t2, p2, x3, y3));
   Print(x3, " ", y3);
   PRTF(x1 == x3 && y1 == y3);
}
//+------------------------------------------------------------------+
/*
   Example output

   ChartWindowOnDropped()=0 / ok
   ChartTimeOnDropped()=2021.11.22 18:00:00 / ok
   ChartPriceOnDropped()=1797.7 / ok
   ChartXOnDropped()=234 / ok
   ChartYOnDropped()=280 / ok
                                                  // convert XY to TP
   ChartXYToTimePrice(0,x1,y1,w2,t2,p2)=true / ok
   0 1797.16 2021.11.22 18:30:00                  // discrepancy
   w1==w2&&t1==t2&&p1==p2=false / ok
                                                  // convert TP to XY
   ChartTimePriceToXY(0,w1,t1,p1,x2,y2)=true / ok
   232 278                                        // discrepancy
   x1==x2&&y1==y2=false / ok
                                                  // round trip conversion
   ChartXYToTimePrice(0,x2,y2,w3,t3,p3)=true / ok
   0 1797.7 2021.11.22 18:00:00                   // match
   w1==w3&&t1==t3&&p1==p3=true / ok
   ChartTimePriceToXY(0,w2,t2,p2,x3,y3)=true / ok
   234 280                                        // match
   x1==x3&&y1==y3=true / ok

*/
//+------------------------------------------------------------------+
