//+------------------------------------------------------------------+
//|                                                 MBKAsctrend3.mq5 |
//|                                    Copyright © 2007, Matt Kennel | 
//|                                       http://www.metatrader.org/ | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, Matt Kennel"
#property link "http://www.metatrader.org/"
#property description "MBKAsctrend3"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- two buffers are used for calculation of drawing of the indicator
#property indicator_buffers 2
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Bearish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//---- magenta color is used as the color of the bearish indicator line
#property indicator_color1  Magenta
//---- indicator 1 line width is equal to 4
#property indicator_width1  4
//---- bullish indicator label display
#property indicator_label1  "MBKAsctrend3 Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_ARROW
//---- blue color is used for the indicator bullish line
#property indicator_color2  DodgerBlue
//---- indicator 2 line width is equal to 4
#property indicator_width2  4
//---- bearish indicator label display
#property indicator_label2 "MBKAsctrend3 Buy"

//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+ 
input uint WPRLength1=9;   //WPR1 period
input uint WPRLength2=33;  //WPR2 period
input uint WPRLength3=77;  //WPR3 period
input int  Swing=3;        //swing
input int  AverSwing=-5;   //average swing
input double W1=1.0;       //WPR1 indicator weight
input double W2=3.0;       //WPR2 indicator weight
input double W3=1.0;       //WPR3 indicator weight
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//----
double w1,w2,w3;
//---- Declaration of integer variables for the indicator handles
int WPR1_Handle,WPR2_Handle,WPR3_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total,SSP,UpLevel,DnLevel,Up1Level,Dn1Level;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- initialization of global variables   
   double sumweights=W1+W2+W3;
   w1=W1/sumweights;
   w2=W2/sumweights;
   w3=W3/sumweights;
   SSP=10;
   UpLevel=67+Swing;
   DnLevel=33-Swing;
   Up1Level=50-AverSwing;
   Dn1Level=50+AverSwing;
   min_rates_total=int(MathMax(MathMax(MathMax(WPRLength1,WPRLength2),WPRLength3),SSP));

//---- getting handle of the iWPR 1 indicator
   WPR1_Handle=iWPR(NULL,0,WPRLength1);
   if(WPR1_Handle==INVALID_HANDLE)Print(" Failed to get handle of the iWPR 1 indicator");

//---- getting handle of the iWPR 2 indicator
   WPR2_Handle=iWPR(NULL,0,WPRLength2);
   if(WPR2_Handle==INVALID_HANDLE)Print(" Failed to get handle of the iWPR 2 indicator");

//---- getting handle of the iWPR 3 indicator
   WPR3_Handle=iWPR(NULL,0,WPRLength3);
   if(WPR3_Handle==INVALID_HANDLE)Print(" Failed to get handle of the iWPR 3 indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"MBKAsctrend3 Sell");
//---- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(SellBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- Create label to display in DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"MBKAsctrend3 Buy");
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(BuyBuffer,true);

//---- Setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- name for the data window and the label for sub-windows 
   string short_name="MBKAsctrend3";
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
//---- checking for the sufficiency of bars for the calculation
   if(BarsCalculated(WPR1_Handle)<rates_total
      || BarsCalculated(WPR2_Handle)<rates_total
      || BarsCalculated(WPR3_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of local variables 
   int limit,bar,to_copy,trend;
   double WPR1[],WPR2[],WPR3[];
   double wprvalue,wprlong,Range;
   static int oldtrend;

//--- calculations of the necessary amount of data to be copied and
//the limit starting index for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1; // starting index for calculation of all bars
      oldtrend=0;
     }
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars
   to_copy=limit+1;

//---- indexing elements in arrays as time series  
   ArraySetAsSeries(WPR1,true);
   ArraySetAsSeries(WPR2,true);
   ArraySetAsSeries(WPR3,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//--- copy newly appeared data in the array  
   if(CopyBuffer(WPR1_Handle,0,0,to_copy,WPR1)<=0) return(RESET);
   if(CopyBuffer(WPR2_Handle,0,0,to_copy,WPR2)<=0) return(RESET);
   if(CopyBuffer(WPR3_Handle,0,0,to_copy,WPR3)<=0) return(RESET);
   
//---- indicators values recalculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      WPR1[bar]=100+WPR1[bar];
      WPR2[bar]=100+WPR2[bar];
      WPR3[bar]=100+WPR3[bar];
     }

//---- main loop of the indicator calculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      trend=0;
      wprvalue=w1*WPR1[bar]+w2*WPR2[bar]+w3*WPR3[bar];
      wprlong=WPR3[bar];
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(wprvalue < DnLevel && wprlong <= Dn1Level) trend=-1;
      if(wprvalue > UpLevel && wprlong >= Up1Level) trend=+1;

      if(oldtrend && trend!=oldtrend && trend>0)
        {
         Range=0.0;
         for(int iii=bar; iii<=bar+SSP; iii++) Range+=MathAbs(high[iii]-low[iii]);
         Range/=SSP+1;
         BuyBuffer[bar]=low[bar]-Range*0.8;
        }

      if(oldtrend && trend!=oldtrend && trend<0)
        {
         Range=0.0;
         for(int iii=bar; iii<=bar+SSP; iii++) Range+=MathAbs(high[iii]-low[iii]);
         Range/=SSP+1;
         SellBuffer[bar]=high[bar]+Range*0.8;
        }

      if(bar && trend) oldtrend=trend;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
