//+------------------------------------------------------------------+
//|                                           Test_CSpecialChart.mq5 |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

#include <SpecialChart.mqh>
#property script_show_inputs
//--- input parameters
input int      InpCount=100;
//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
  {
//--- 
   CSpecialChart chart;
   if(!chart.Create("Chart",50,10,600,400,COLOR_FORMAT_XRGB_NOALPHA))
      Print("Create() method returned false");
//--- setting the border colors and width
   chart.SetBackground(clrGray);
   chart.SetFrameColor(clrIvory);
   chart.SetFrameWidth(3);
//--- number of series on the chart
   chart.SetLines(5);
//--- data for charts drawing will be accepted into this array
   double data[];
//--- four series of random sequences
   for(int i=0;i<4;i++)
     {
      GetRandomArray(data);
      chart.AddSeria(data,true);
     }
//--- the fifth series for visual debugging
   ArrayResize(data,100);
//--- add a straight line
   for(int i=0;i<100;i++) data[i]=i;
   chart.AddSeria(data,false);
//--- drawing all
   chart.Update();
   for(int i=0;i<InpCount;i++)
     {
      Comment(i);
      Sleep(100);
     }
  }
//+------------------------------------------------------------------+
//| GetRandomArray                                                   |
//+------------------------------------------------------------------+
void GetRandomArray(double &randarr[])
  {
   int length=MathRand()/37767*50+50;
   ArrayResize(randarr,length);
   for(int t=0;t<length;t++)
     {
      randarr[t]=MathRand()*100;
      randarr[t]/=32767;
     }
  }
//+------------------------------------------------------------------+
