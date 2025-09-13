//===================================================================//
//                                           CCIT3_Simple_v_2-01.mq4 ||
//                                 Copyright © 2012.08.19, Alexander ||
// ICQ: 609928564 | email: I-m-hungree@yandex.ru | skype:i_m_hungree ||
//===================================================================||
//        ------------------------------------------------           ||
//        information about the previous (modernized) code           ||
//        ------------------------------------------------           ||
//                                                                   ||
//                     		                       "Alextp., 2012 ã." ||
//                     		                       "atopunov@mail.ru" ||
//                     		                                    "1.1" ||
//                                                "CCI T3 Indicator" ||
//===================================================================\\
#property copyright "Copyright © 2012, Im_hungry"
#property link      "CCIT3_Simple_v_2-01"

#property indicator_separate_window
#property indicator_buffers	5
#property indicator_plots		2
//---
#property indicator_label1	   "CCI T3"
#property indicator_type1		DRAW_COLOR_HISTOGRAM
#property indicator_color1	   clrNONE, clrLime, clrRed
#property indicator_style1	   STYLE_SOLID
#property indicator_width1	   2
//---
#property indicator_label2	   "MA"
#property indicator_type2		DRAW_COLOR_LINE
#property indicator_color2	   clrNONE, clrYellow
#property indicator_style2	   STYLE_SOLID
#property indicator_width2	   1
//---
input int                           CCI_Period=170;
input ENUM_APPLIED_PRICE            CCI_Price_Type=PRICE_TYPICAL;
input int                           T3_Period=80;
input double                        Koeff_B=0.618;
input int                           Max_bars_calc=100000;
//+------------------------------------------------------------------+
//| global parameters                                                |
//+------------------------------------------------------------------+
double CCIBuff[],MCCIBuff[],MCCIColorBuff[],MABuff[],MAColorBuff[];
double e1,e2,e3,e4,e5,e6,b2,b3,c1,c2,c3,c4,w1,w2;
int cci_handler,n,start,flag_start;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
   SetIndexBuffer(0,MCCIBuff,INDICATOR_DATA);
   SetIndexBuffer(1,MCCIColorBuff,INDICATOR_COLOR_INDEX);
   SetIndexBuffer(2,MABuff,INDICATOR_DATA);
   SetIndexBuffer(3,MAColorBuff,INDICATOR_COLOR_INDEX);
   SetIndexBuffer(4,CCIBuff,INDICATOR_CALCULATIONS);
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
   IndicatorSetInteger(INDICATOR_DIGITS,Digits());
   IndicatorSetString(INDICATOR_SHORTNAME,"CCIT3_Simple (CCI_Period ="+string(CCI_Period)+", T3_Period ="+string(T3_Period)+")");
   cci_handler=iCCI(Symbol(),Period(),CCI_Period,CCI_Price_Type);
   if(cci_handler==INVALID_HANDLE)
     {
      Print("Error in loading CCIT3Simple_v_2-01 :",GetLastError());
      return(-1);
     }
   b2 = Koeff_B*Koeff_B;
   b3 = b2*Koeff_B;
   c1 = -b3;
   c2 = (3.0*(b2+b3));
   c3 = -3.0*(2.0*b2+Koeff_B+b3);
   c4 = (1.0+3.0*Koeff_B+b3+3.0*b2);
   n=T3_Period;
   if(n<1) n=1;
   else n=(n+1)/2;
   w1 = 2.0/(n+1.0);
   w2 = 1.0-w1;
   flag_start=0;
   ChartRedraw();
   return(0);
  }
//+------------------------------------------------------------------+
//| deinitialization function                                        |
//+------------------------------------------------------------------+
void OnDeinit(const int _reason)
  {
   if(cci_handler!=INVALID_HANDLE) IndicatorRelease(cci_handler);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,const int prev_calculated,
                const datetime &time[],const double &open[],
                const double &high[],const double &low[],
                const double &close[],const long &tick_volume[],
                const long &volume[],const int &spread[])
  {
   static datetime   last_bar=0;
   ArraySetAsSeries(CCIBuff,true);
   ArraySetAsSeries(MCCIBuff,true);
   ArraySetAsSeries(MABuff,true);
   ArraySetAsSeries(MAColorBuff,true);
   ArraySetAsSeries(MCCIColorBuff,true);
   ArraySetAsSeries(time,true);
   if(flag_start==0)
     {
      ArrayInitialize(MCCIBuff,0.0);
      ArrayInitialize(MABuff,0.0);
      if(Bars(Symbol(),NULL)-1<Max_bars_calc) start=Bars(Symbol(),NULL)-10;
      else start= Max_bars_calc-10;
      flag_start=1;
      if(CopyBuffer(cci_handler,0,0,start,CCIBuff)<=0) return(rates_total);
     }
   else
     {
      start=1;
      if(CopyBuffer(cci_handler,0,0,2,CCIBuff)<=0) return(rates_total);
     }
   if(time[0]>last_bar)
     {
      last_bar=time[0];
      for(int i=start; i>=0; i--)
        {
         if(CCIBuff[i]<1000)
           {
            e1 = w1*CCIBuff[i]+w2*e1;
            e2 = w1*e1+w2*e2;
            e3 = w1*e2+w2*e3;
            e4 = w1*e3+w2*e4;
            e5 = w1*e4+w2*e5;
            e6 = w1*e5+w2*e6;
            MCCIBuff[i]=NormalizeDouble((c1*e6+c2*e5+c3*e4+c4*e3),Digits());
            MABuff[i]=MCCIBuff[i];
           }
         else
           {
            MCCIBuff[i]=0.0;
            MABuff[i]=0.0;
           }
         MAColorBuff[i]=1;
         if(MCCIBuff[i]>0) MCCIColorBuff[i]=1;
         else
           {
            if(MCCIBuff[i]<0) MCCIColorBuff[i]=2;
            else MCCIColorBuff[i]=0;
           }
        }
     }
   return(rates_total);
  }
//+------------------------------------------------------------------+
