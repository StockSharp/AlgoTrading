//+------------------------------------------------------------------+
//|                                                 ATR_Trailing.mq5 | 
//|                             Copyright ฉ 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright ฉ 2012, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Indicator of trailingstop moving line"

//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 2
#property indicator_buffers 2
//---- Only 2 graphical plots are used
#property indicator_plots   2

//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET 0 // the constant for getting the command for the indicator recalculation back to the terminal

//+--------------------------------------------+
//|  Indicator levels drawing parameters       |
//+--------------------------------------------+
//---- drawing the levels as lines
#property indicator_type1   DRAW_LINE
#property indicator_type2   DRAW_LINE
//---- the width of indicator line is 3
#property indicator_width1 3
#property indicator_width2 3
//---- selection of levels colors
#property indicator_color1  LightGreen
#property indicator_color2  Magenta
//---- display levels labels
#property indicator_label1  "StopLoss Sell"
#property indicator_label2  "StopLoss Buy"

//+-----------------------------------+
//|  Input parameters of the indicator|
//+-----------------------------------+
input int Period_ATR=14;//ภาR period
input double Sell_Factor=2.0;
input double Buy_Factor=2.0;
//+-----------------------------------+

//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double ExtLineBuffer1[],ExtLineBuffer2[];

//----Declaration of variables for storing the indicators handles
int ATR_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+   
//| SL_ATR indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=Period_ATR;

//---- getting handle of the ATR indicator
   ATR_Handle=iATR(NULL,0,Period_ATR);
   if(ATR_Handle==INVALID_HANDLE)Print(" Failed to get handle of the ATR indicator");

//---- set dynamic arrays as indicator buffers
   SetIndexBuffer(0,ExtLineBuffer1,INDICATOR_DATA);
   SetIndexBuffer(1,ExtLineBuffer2,INDICATOR_DATA);

//---- set the position, from which the Bollinger Bands drawing starts
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);

//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);

//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(ExtLineBuffer1,true);
   ArraySetAsSeries(ExtLineBuffer2,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"ATR_Trailing(",Period_ATR,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| SL_ATR iteration function                                        | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar;
   double Range[],ATR;

//---- Calculate the "limit" starting number for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
     {
      limit=rates_total-min_rates_total; // starting index for calculation of all bars
      for(bar=rates_total-1; bar>limit && !IsStopped(); bar--)
        {
         ExtLineBuffer1[bar]=0.0;
         ExtLineBuffer2[bar]=0.0;
        }

     }
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars

//---- calculation of the necessary amount of data to be copied
   to_copy=limit+1;

//---- copy newly appeared data in the Range[] arrays
   if(CopyBuffer(ATR_Handle,0,0,to_copy,Range)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(Range,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- main cycle of calculation of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ATR=Range[bar];
      ExtLineBuffer1[bar]=NormalizeDouble(low[bar]+ATR*Sell_Factor,_Digits);
      ExtLineBuffer2[bar]=NormalizeDouble(high[bar]-ATR*Buy_Factor,_Digits);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
