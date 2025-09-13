//+------------------------------------------------------------------+
//|                                             AnchoredMomentum.mq5 | 
//|                              Copyright © 2010, Umnyashkin Victor | 
//|                                       http://www.metaquotes.net/ | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Umnyashkin Victor"
#property link "http://www.metaquotes.net/"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers is 4
#property indicator_buffers 4 
//---- only four plots are used
#property indicator_plots   4
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- use gray color for the indicator line
#property indicator_color1 clrGray
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "Momentum"

//+----------------------------------------------+
//| Bullish indicator drawing parameters         |
//+----------------------------------------------+
//---- drawing indicator as a symbol
#property indicator_type2   DRAW_ARROW
//---- color of the bullish indicator is light green
#property indicator_color2 clrSpringGreen
//---- the width of indicator line is 3
#property indicator_width2 3
//---- displaying of the bullish label of the indicator
#property indicator_label2 "Up_Signal"
//+----------------------------------------------+
//| Parameters of drawing a bearish indicator    |
//+----------------------------------------------+
//---- drawing indicator as a symbol
#property indicator_type3   DRAW_ARROW
//---- dark pink is used as the color of the bearish indicator
#property indicator_color3  clrDeepPink
//---- the width of indicator line is 3
#property indicator_width3 3
//---- displaying of the bearish label of the indicator
#property indicator_label3 "Dn_Signal"
//+----------------------------------------------+
//| Parameters of drawing a non-trend indicator  |
//+----------------------------------------------+
//---- drawing indicator as a symbol
#property indicator_type4   DRAW_ARROW
//---- gray color is used for the non-trend indicator
#property indicator_color4  clrGray
//---- the width of indicator line is 3
#property indicator_width4 3
//---- displaying of the non-trend label of the indicator
#property indicator_label4 "No_Signal"
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input uint MomPeriod=8; //SMA period 
input uint SmoothPeriod=6; //EMA period
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE;//applied price by which the indicator is calculated
input double UpLevel=+0.025; //upper breakthrough level
input double DnLevel=-0.025; //lower breakthrough level
input int Shift=0; //horizontal shift of the indicator in bars
//+-----------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double MomBuffer[];
double UpBuffer[];
double DnBuffer[];
double FlBuffer[];
//---- Declaration of integer variables for the indicator handles
int SMA_Handle,EMA_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+   
//| Momentum indicator initialization function                       | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- getting the SMA indicator handle
   SMA_Handle=iMA(NULL,0,MomPeriod,0,MODE_SMA,IPC);
   if(SMA_Handle==INVALID_HANDLE) Print(" Failed to get handle of SMA indicator");

//---- getting the SMA indicator handle
   EMA_Handle=iMA(NULL,0,MomPeriod,0,MODE_EMA,IPC);
   if(EMA_Handle==INVALID_HANDLE) Print(" Failed to get handle of EMA indicator");

//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MomPeriod);
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,MomBuffer,INDICATOR_DATA);
//---- horizontal shift of the indicator
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(MomBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,UpBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- selecting a symbol for drawing
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(UpBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,DnBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- selecting a symbol for drawing
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(DnBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(3,FlBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 3 drawing
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//--- create a label to display in DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"No Signal");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- selecting a symbol for drawing
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(FlBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"Momentum(",MomPeriod,", ",SmoothPeriod,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
   
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,3);
   
//---- the number of the indicator 2 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- values of the indicator horizontal levels   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,DnLevel);
//---- blue and magenta colors are used for horizontal levels lines  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrMagenta);
//---- short dot-dash is used for the horizontal level line  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//|  Momentum iteration function                                     | 
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
   if(BarsCalculated(SMA_Handle)<rates_total
      || BarsCalculated(EMA_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

//---- declaration of variables with a floating point  
   double res,Momentum,SMA[],EMA[];
//---- Declaration of integer variables and getting the bars already calculated
   int to_copy,limit,bar;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-1-min_rates_total; // starting index for the calculation of all bars
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for the calculation of new bars
     }

   to_copy=limit+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(SMA_Handle,0,0,to_copy,SMA)<=0) return(RESET);
   if(CopyBuffer(EMA_Handle,0,0,to_copy,EMA)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(SMA,true);
   ArraySetAsSeries(EMA,true);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      res=SMA[bar];
      if(res) Momentum=100*(EMA[bar]/SMA[bar]-1);
      else Momentum=EMPTY_VALUE;

      MomBuffer[bar]=Momentum;

      //---- initialization of cells of indicator buffers with zeros
      UpBuffer[bar]=EMPTY_VALUE;
      DnBuffer[bar]=EMPTY_VALUE;
      FlBuffer[bar]=EMPTY_VALUE;

      if(Momentum==EMPTY_VALUE) continue;
      
      //---- initialization of cells of the indicator buffers with obtained values 
      if(Momentum>UpLevel) UpBuffer[bar]=Momentum; //there is an ascending trend
      else if(Momentum<DnLevel) DnBuffer[bar]=Momentum; //there is a descending trend
      else FlBuffer[bar]=Momentum; //no trend
     }
//----     
   return(rates_total);
  }
//+X----------------------+ <<< The End >>> +-----------------------X+
