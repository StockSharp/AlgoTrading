//+---------------------------------------------------------------------+
//|                                                            TRVI.mq5 |
//|                    Copyright © 2010,   VladMsk, contact@mqlsoft.com |
//|                                             http://www.becemal.ru// |
//+---------------------------------------------------------------------+
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+

//---- author of the indicator
#property copyright "Copyright © 2010, VladMsk, contact@mqlsoft.com"
//---- author of the indicator
#property link      "http://www.becemal.ru/"
//---- indicator description
#property description "True RVI"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//| TRVI indicator drawing parameters            |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- DeepPink color is used as the color of the bullish line of the indicator
#property indicator_color1  clrDeepPink
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "TRVI"
//+----------------------------------------------+
//| Signal indicator drawing parameters          |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- DodgerBlue color is used for the indicator bearish line
#property indicator_color2  clrDodgerBlue
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 2
#property indicator_width2  2
//---- displaying of the bearish label of the indicator
#property indicator_label2  "Signal"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal

//+----------------------------------------------+
//|  INDICATOR INPUT PARAMETERS                  |
//+----------------------------------------------+
input uint TRVIPeriod=10; //TRVI period
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //volume
input int Shift=0; // horizontal shift of the indicator in bars
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double TRVIBuffer[];
double SignalBuffer[];
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| calculation                                                      |
//+------------------------------------------------------------------+
double CountVal(const double &B[],const double &A[],const long &V[],const long &tV[],ENUM_APPLIED_VOLUME Type,int ii)
  {
//----
   if(VolumeType==VOLUME_TICK)     
        return(tV[ii]*(A[ii]-B[ii])+8*tV[ii-1]*(A[ii-1]-B[ii-1])+8*tV[ii-2]*(A[ii-2]-B[ii-2])+tV[ii-3]*(A[ii-3]-B[ii-3]));
   else return(V[ii]*(A[ii]-B[ii])+8*V[ii-1]*(A[ii-1]-B[ii-1])+8*V[ii-2]*(A[ii-2]-B[ii-2])+V[ii-3]*(A[ii-3]-B[ii-3]));
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(TRVIPeriod+8);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,TRVIBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,SignalBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"TRVI(",TRVIPeriod,")");
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
   if(rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int first,bar,Norm,rrr,kkk;
   double dNum,dDeNum;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=int(TRVIPeriod+4); // starting number for calculation of all bars
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- main indicator calculation loop
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      dNum=0.0;
      dDeNum=0.0;
      for(kkk=0; kkk<int(TRVIPeriod); kkk++)
        {
         rrr=bar-kkk;
         Norm=int(TRVIPeriod)-kkk+1;
         dNum+=Norm*CountVal(open,close,volume,tick_volume,VolumeType,rrr);
         dDeNum+=Norm*CountVal(low,high,volume,tick_volume,VolumeType,rrr);
        }

      if(dDeNum!=0.0) TRVIBuffer[bar]=dNum/dDeNum;
      else  TRVIBuffer[bar]=dNum;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first+=4;

//---- main loop of the signal line calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
      SignalBuffer[bar]=(4*TRVIBuffer[bar]+3*TRVIBuffer[bar-1]+2*TRVIBuffer[bar-2]+TRVIBuffer[bar-3])/10;

//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
