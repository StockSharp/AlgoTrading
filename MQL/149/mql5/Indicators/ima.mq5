//+------------------------------------------------------------------+
//|                                                          ima.mq5 |
//|                                                      Vladimir M. |
//|                                                mikh.vl@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Vladimir M."
#property link      "mikh.vl@gmail.com"
#property version   "1.00"
#property indicator_separate_window
#property indicator_buffers 1
#property indicator_plots   1
#property indicator_type1   DRAW_LINE
#property indicator_color1  Red

input int MAPeriod=5; // Period

double imaBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
   SetIndexBuffer(0,imaBuffer,INDICATOR_DATA);
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,MAPeriod);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,const int prev_calculated,const int begin,const double &price[])
  {
   int start,j,i;
   double sum,ma;
   if(rates_total<MAPeriod-1)return(0);
   if(prev_calculated==0)start=MAPeriod-1;
   else start=prev_calculated-1;
   for(j=start;j<rates_total;j++)
     {
      sum=0.0;
      for(i=0;i<MAPeriod;i++)
         sum+=price[j-i];
      ma=sum/MAPeriod;
      imaBuffer[j]=(price[j]/ma-1);
     }
   return(rates_total);
  }
//+------------------------------------------------------------------+
