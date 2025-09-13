//+------------------------------------------------------------------+
//|                                                         MALR.mq5 |
//+------------------------------------------------------------------+
#property copyright "BECEMAL"
#property link      "http://www.becemal.ru/"
#property version   "1.44"
//---
#property indicator_chart_window
#property indicator_buffers 5
#property indicator_plots   5
//---
#property indicator_label1  "MALR"
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrGold
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//---
#property indicator_label2  "MALRH"
#property indicator_type2   DRAW_LINE
#property indicator_color2  clrLime
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1
//---
#property indicator_label3  "MALRL"
#property indicator_type3   DRAW_LINE
#property indicator_color3  clrRed
#property indicator_style3  STYLE_SOLID
#property indicator_width3  1
//---
#property indicator_label4  "MALRHH"
#property indicator_type4   DRAW_LINE
#property indicator_color4  clrForestGreen
#property indicator_style4  STYLE_SOLID
#property indicator_width4  1
//---
#property indicator_label5  "MALRLL"
#property indicator_type5   DRAW_LINE
#property indicator_color5  clrCrimson
#property indicator_style5  STYLE_SOLID
#property indicator_width5  1
//---
input int                  MAPeriod           = 120;
input int                  MAShift            = 0;
input ENUM_APPLIED_PRICE   AppliedPrice       = PRICE_CLOSE;
input double               ChannelReversal    = 0.8;
input double               ChannelBreakout    = 0.8;
//+------------------------------------------------------------------+
//| global parameters                                                |
//+------------------------------------------------------------------+
double FF[],FFL[],FFH[],FFLL[],FFHH[];
static int hSMA=INVALID_HANDLE,hLwMA=INVALID_HANDLE;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
   SetIndexBuffer(0,FF,INDICATOR_DATA);
   SetIndexBuffer(1,FFH,INDICATOR_DATA);
   SetIndexBuffer(2,FFL,INDICATOR_DATA);
   SetIndexBuffer(3,FFHH,INDICATOR_DATA);
   SetIndexBuffer(4,FFLL,INDICATOR_DATA);
//---
   hSMA=iMA(_Symbol,_Period,MAPeriod,MAShift,MODE_SMA,AppliedPrice);
   if(hSMA==INVALID_HANDLE)
     {
      Print("No SMA! Error # ",GetLastError());
      return(-1);
     }
   hLwMA=iMA(_Symbol,_Period,MAPeriod,MAShift,MODE_LWMA,AppliedPrice);
   if(hLwMA==INVALID_HANDLE)
     {
      Print("No LWMA! Error # ",GetLastError());
      return(-1);
     }
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
  {
   if(IsStopped()) return(0);
   int i,limit;
   if(prev_calculated>rates_total || prev_calculated<=0) limit=begin+MAPeriod+1;
   else limit=prev_calculated-1;
   if(limit<=0 || limit>=rates_total)
     {
      Print("No Data!");
      return(-1);
     }
//---
   for(i=limit; i<rates_total; i++)
     {
      double MA[1]={0.0};
      if(CopyBuffer(hSMA,0,rates_total-i,1,MA)<1) return(i-1);
      double tSMA=MA[0];
      if(tSMA==0.0) return(i-1);
      MA[0]=0.0;
      if(CopyBuffer(hLwMA,0,rates_total-i,1,MA)<1) return(i-1);
      double tLWMA=MA[0];
      if(tLWMA==0.0) return(i-1);
      FF[i]=3.0*tLWMA-2.0*tSMA;
     }
//---
   for(i=limit; i<rates_total; i++)
     {
      double StdDev=0.0;
      double tMA=FF[i];
      for(int j=0; j<MAPeriod; j++)
        {
         double t=price[i-j]-FF[i-j];
         StdDev+=t*t;
        }
      StdDev=MathSqrt(StdDev/MAPeriod);
      double tS=StdDev*ChannelReversal;
      double tS2=StdDev*(ChannelReversal+ChannelBreakout);
      FFL[i] = tMA-tS;
      FFH[i] = tMA+tS;
      FFLL[i] = tMA-tS2;
      FFHH[i] = tMA+tS2;
     }
//---
   return(rates_total);
  }
//+------------------------------------------------------------------+
