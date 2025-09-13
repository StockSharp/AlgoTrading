//+------------------------------------------------------------------+
//|                                              JBrainTrend1Sig.mq5 |
//|                               Copyright © 2005, BrainTrading Inc |
//|                                      http://www.braintrading.com |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2005, BrainTrading Inc."
//---- link to the website of the author
#property link      "http://www.braintrading.com/"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//----two buffers are used for calculation of drawing of the indicator
#property indicator_buffers 2
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET 0 // The constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//---- DarkOrange color is used for the indicator bearish line
#property indicator_color1  DarkOrange
//---- thickness of line of the indicator 1 is equal to 4
#property indicator_width1  4
//---- displaying of the bullish label of the indicator
#property indicator_label1  "JBrain1 Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_ARROW
//---- blue color is used as the color of the bullish line of the indicator
#property indicator_color2  Blue
//---- thickness of the indicator line 2 is equal to 4
#property indicator_width2  4
//---- displaying of the bearish label of the indicator
#property indicator_label2 "JBrain1 Buy"

//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input int ATR_Period=7; //ATR period 
input int STO_Period=9; //stochastic period
input ENUM_MA_METHOD MA_Method = MODE_SMA; //smoothing method
input ENUM_STO_PRICE STO_Price = STO_LOWHIGH; //method of prices calculation
input int Length_=7; // depth of the JMA smoothing                   
input int Phase_=100; // parameter of the JMA smoothing,
                      //that changes within the range -100 ... +100,
//impacts the transitional process quality;
//+----------------------------------------------+

//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//---
double d,s;
int p,x1,x2,P_,min_rates_total,OldTrend;
//---- declaration of integer variables for the indicators handles
int ATR_Handle,STO_Handle,JO_Handle,JH_Handle,JL_Handle,JC_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- initialization of global variables 
   d=2.3;
   s=1.5;
   x1 = 53;
   x2 = 47;
   min_rates_total=int(MathMax(MathMax(ATR_Period,STO_Period),30)+2);
   
//---- getting handle of the ATR indicator
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)Print(" Failed to get handle of the ATR indicator");
   
//---- getting handle of the Stochastic indicator
   STO_Handle=iStochastic(NULL,0,STO_Period,STO_Period,1,MA_Method,STO_Price);
   if(STO_Handle==INVALID_HANDLE)Print(" Failed to get handle of the Stochastic indicator");
   
//---- getting handle of the JMA indicator
   JL_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,4,0,0);
   if(JL_Handle==INVALID_HANDLE)Print(" Failed to get handle of the JMA indicator");
   
//---- getting handle of the JMA indicator
   JC_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,1,0,0);
   if(JC_Handle==INVALID_HANDLE)Print(" Failed to get handle of the JMA indicator");
   
//---- getting handle of the JMA indicator
   JH_Handle=iCustom(NULL,0,"JMA",Length_,Phase_,3,0,0);
   if(JH_Handle==INVALID_HANDLE)Print(" Failed to get handle of the JMA indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- shifting the beginning of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);

//---- Setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- name for the data window and the label for sub-windows 
   string short_name="JBrainTrend1Sig";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(STO_Handle)<rates_total
      || BarsCalculated(JH_Handle)<rates_total
      || BarsCalculated(JL_Handle)<rates_total
      || BarsCalculated(JC_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar;
   double value2[],Range[],JH[],JL[],JC[],range,range2,val1,val2,val3;

//--- calculations of the necessary amount of data to be copied and
//----the limit starting number for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
        limit=rates_total-min_rates_total; // starting index for calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars    
   to_copy=limit+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(ATR_Handle,0,0,to_copy,Range)<=0) return(RESET);
   if(CopyBuffer(STO_Handle,0,0,to_copy,value2)<=0) return(RESET);
   if(CopyBuffer(JH_Handle,0,0,to_copy,JH)<=0) return(RESET);
   if(CopyBuffer(JL_Handle,0,0,to_copy,JL)<=0) return(RESET);
   if(CopyBuffer(JC_Handle,0,0,to_copy+2,JC)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(Range,true);
   ArraySetAsSeries(value2,true);
   ArraySetAsSeries(JH,true);
   ArraySetAsSeries(JL,true);
   ArraySetAsSeries(JC,true);


//---- restore values of the variables
   p=P_;

//---- main cycle of calculation of the indicator
   for(bar=limit; bar>=0; bar--)
     {
      //---- memorize values of the variables before running at the current bar
      if(rates_total!=prev_calculated && bar==0)
         P_=p;

      range=Range[bar]/d;
      range2=Range[bar]*s/4;
      val1 = 0.0;
      val2 = 0.0;
      SellBuffer[bar]=0.0;
      BuyBuffer[bar]=0.0;

      val3=MathAbs(NormalizeDouble(JC[bar],_Digits)-NormalizeDouble(JC[bar+2],_Digits));
      if(value2[bar] < x2 && val3 > range) p = 1;
      if(value2[bar] > x1 && val3 > range) p = 2;

      if(val3<=range) continue;

      if(value2[bar]<x2 && (p==1 || p==0))
        {
         if(OldTrend>0) SellBuffer[bar]=JH[bar]+range2;
         if(bar) OldTrend=-1;
        }
      if(value2[bar]>x1 && (p==2 || p==0))
        {
         if(OldTrend<0) BuyBuffer[bar]=JL[bar]-range2;
         if(bar) OldTrend=+1;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
