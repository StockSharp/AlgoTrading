//+------------------------------------------------------------------+
//|                                         ADX_Cross_Hull_Style.mq5 | 
//|                                             Copyright © 2005,  . |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, ."
#property link      ""
#property description "METRO"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Parameters of drawing the bullish indicator |
//+----------------------------------------------+
//---- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//---- LightSeaGreen color is used for the indicator
#property indicator_color1  clrLightSeaGreen
//---- indicator 1 width is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "Lower ADX_Cross_Hull_Style"
//+----------------------------------------------+
//|  Bearish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_ARROW
//---- red color is used for the indicator
#property indicator_color2  clrRed
//---- indicator 2 width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2 "Upper ADX_Cross_Hull_Style"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint ADXPeriod=14;//indicator period
input int Shift=0; //horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double UpBuffer[],DnBuffer[];
//---- Declaration of integer variables for the indicator handles
int ADX1_Handle,ADX2_Handle,ATR_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   int AtrPeriod=10;
   min_rates_total=int(MathMax(ADXPeriod+1,AtrPeriod));
   
//---- getting the ADX1 indicator handle
   ADX1_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX1_Handle==INVALID_HANDLE) Print(" Failed to get handle of the ADX1 indicator");

//---- getting the ADX2 indicator handle
   ADX2_Handle=iADX(NULL,0,int(MathFloor(ADXPeriod/2.0)));
   if(ADX2_Handle==INVALID_HANDLE) Print(" Failed to get handle of the ADX2 indicator");

//---- getting the ATR indicator handle
   ATR_Handle=iATR(NULL,0,AtrPeriod);
   if(ATR_Handle==INVALID_HANDLE)Print(" Failed to get handle of the ATR indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(UpBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- shifting the indicator 3 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(DnBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"ADX_Cross_Hull_Style(",ADXPeriod,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of maximums of price for the calculation of indicator
                const double& low[],      // price array of price lows for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(ADX1_Handle)<rates_total
      || BarsCalculated(ADX2_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int limit,to_copy,bar;
   double pADX1[],pADX2[],mADX1[],mADX2[],ATR[];
   double b4plusdi,nowplusdi,b4minusdi,nowminusdi;
//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(pADX1,true);
   ArraySetAsSeries(pADX2,true);
   ArraySetAsSeries(mADX1,true);
   ArraySetAsSeries(mADX2,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      limit=rates_total-1-min_rates_total; // starting index for the calculation of all bars
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars
//----
   to_copy=limit+2;

//--- copy newly appeared data in the array
   if(CopyBuffer(ADX1_Handle,PLUSDI_LINE,0,to_copy,pADX1)<=0) return(RESET);
   if(CopyBuffer(ADX2_Handle,PLUSDI_LINE,0,to_copy,pADX2)<=0) return(RESET);
   if(CopyBuffer(ADX1_Handle,MINUSDI_LINE,0,to_copy,mADX1)<=0) return(RESET);
   if(CopyBuffer(ADX2_Handle,MINUSDI_LINE,0,to_copy,mADX2)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy-1,ATR)<=0) return(RESET);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      UpBuffer[bar]=0.0;
      DnBuffer[bar]=0.0;

      b4plusdi=pADX2[bar+1]*2-pADX1[bar+1];
      nowplusdi=pADX2[bar]*2-pADX1[bar];
      b4minusdi=mADX2[bar+1]*2-mADX1[bar+1];
      nowminusdi=mADX2[bar]*2-mADX1[bar];

      if(b4plusdi < b4minusdi && nowplusdi > nowminusdi) UpBuffer[bar]=low[bar]-0.25*ATR[bar];
      if(b4plusdi > b4minusdi && nowplusdi < nowminusdi) DnBuffer[bar]=high[bar]+0.25*ATR[bar];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
