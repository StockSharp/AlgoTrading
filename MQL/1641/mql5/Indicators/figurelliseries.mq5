//+------------------------------------------------------------------+
//|                                              FigurelliSeries.mq5 |
//|                              Copyright © 2010, Rogerio Figurelli |
//|                                      figurelli@quantafinance.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Rogerio Figurelli"
#property link      "http://www.quantafinance.com"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- one buffer is used for calculation and drawing of the indicator
#property indicator_buffers 1
//---- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator as a histogram
#property indicator_type1   DRAW_HISTOGRAM
//---- DarkOrchid color is used as the color of the bullish line of the indicator
#property indicator_color1  clrDarkOrchid
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- indicator 1 line width is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "FigurelliSeries"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//|  declaration of constants         |
//+-----------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint StartPeriod=6;  // initial period
input uint Step=6;         // periods calculation step
input uint Total=36;       // number of Moving Averages
input  ENUM_MA_METHOD   MAType=MODE_EMA; // Moving Averages smoothing type
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE; // price timeseries of Moving Averages
input int Shift=0;         // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer[];
//---- Declaration of integer variables for the indicator handles
int MA_Handle[];
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of data calculation starting point
   min_rates_total=int(StartPeriod+Step*(Total-1));

//---- memory allocation for array  
   if(ArrayResize(MA_Handle,Total)<int(Total)) Print("Failed to distribute the memory for MA_Handle[]array");

//---- getting the iMA indicator handles
   for(int count=0; count<int(Total); count++)
     {
      MA_Handle[count]=iMA(NULL,0,StartPeriod+Step*count,0,MAType,MAPrice);
      if(MA_Handle[count]==INVALID_HANDLE) Print(" Failed to get the iMA indicator handle");
     }

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(IndBuffer,true);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"FigurelliSeries");
//--- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
                const double& low[],      // price array of minimums of price for the calculation of indicator
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking for the sufficiency of the number of bars for the calculation
   if(rates_total<min_rates_total) return(RESET);
   for(int count=0; count<int(Total); count++) if(BarsCalculated(MA_Handle[count])<rates_total) return(RESET);

//---- declaration of local variables 
   int limit,bar;
   double MA[1];

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-1-min_rates_total; // starting index for the calculation of all bars
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

//---- indexing elements in arrays as in timeseries  
   ArraySetAsSeries(close,true);

//---- main cycle of calculation of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double tot_Ask=0;
      double tot_Bid=0;

      for(int count=0; count<int(Total); count++)
        {
         //---- copy newly appeared data into the arrays
         if(CopyBuffer(MA_Handle[count],0,bar,1,MA)<=0) return(RESET);

         if(close[bar]<MA[0]) tot_Ask++;
         if(close[bar]>MA[0]) tot_Bid++;
        }

      IndBuffer[bar]=tot_Bid-tot_Ask;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
