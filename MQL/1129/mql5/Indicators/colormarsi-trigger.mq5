//+------------------------------------------------------------------+
//|                                           ColorMaRsi-Trigger.mq5 | 
//|                              Copyright © 2010, fx-system@mail.ru |
//|                                                fx-system@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, fx-system@mail.ru"
#property link      "fx-system@mail.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator cloud
#property indicator_color1 clrMagenta,clrRoyalBlue
//---- displaying the indicator label
#property indicator_label1  "MaRsi-Trigger"
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET  0 // the constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input uint nPeriodRsi=3;
input ENUM_APPLIED_PRICE nRSIPrice=PRICE_WEIGHTED;
input uint nPeriodRsiLong=13;
input ENUM_APPLIED_PRICE nRSIPriceLong=PRICE_MEDIAN;
input uint nPeriodMa=5;
input  ENUM_MA_METHOD nMAType=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPrice=PRICE_CLOSE;
input uint nPeriodMaLong=10;
input  ENUM_MA_METHOD nMATypeLong=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPriceLong=PRICE_CLOSE;
input int  Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+
//---- indicator buffers
double ExtMapBuffer[];
double ExtZerBuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of integer variables for the indicators handles
int MA_Handle,RSI_Handle,MAl_Handle,RSIl_Handle;
//+------------------------------------------------------------------+   
//| MaRsi-Trigger indicator initialization function                  | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- getting handle of the iRSI indicator
   RSI_Handle=iRSI(NULL,0,nPeriodRsi,nRSIPrice);
   if(RSI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iRSI indicator");

//---- getting handle of the iRSIl indicator
   RSIl_Handle=iRSI(NULL,0,nPeriodRsiLong,nRSIPriceLong);
   if(RSIl_Handle==INVALID_HANDLE) Print(" Failed to get handle of iRSIl indicator");

//---- getting handle of the iMA indicator
   MA_Handle=iMA(NULL,0,nPeriodMa,0,nMAType,nMAPrice);
   if(MA_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMA indicator");

//---- getting handle of the iMAl indicator
   MAl_Handle=iMA(NULL,0,nPeriodMaLong,0,nMATypeLong,nMAPriceLong);
   if(MAl_Handle==INVALID_HANDLE) Print(" Failed to get handle of iMAl indicator");

//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(MathMax(MathMax(nPeriodRsi,nPeriodRsiLong),nPeriodMa),nPeriodMaLong));

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,ExtZerBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(ExtZerBuffer,true);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,ExtMapBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(ExtMapBuffer,true);

//---- initializations of variable for indicator short name
   string shortname="MaRsi-Trigger()";
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| MaRsi-Trigger iteration function                                 | 
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
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(MAl_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(RSIl_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar;
   double res,MA_[],MAl_[],RSI_[],RSIl_[];

//--- calculations of the necessary amount of data to be copied and
//----the limit starting number for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
     {
      limit=rates_total-min_rates_total-1; // starting index for calculation of all bars
     }
   else
     {
      limit=rates_total-prev_calculated; // starting number for calculation of new bars
     }

   to_copy=limit+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA_)<=0) return(RESET);
   if(CopyBuffer(MAl_Handle,0,0,to_copy,MAl_)<=0) return(RESET);
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
   if(CopyBuffer(RSIl_Handle,0,0,to_copy,RSIl_)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(MA_,true);
   ArraySetAsSeries(MAl_,true);
   ArraySetAsSeries(RSI_,true);
   ArraySetAsSeries(RSIl_,true);

//---- main cycle of calculation of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      res=0;
      if(MA_[bar] > MAl_[bar]) res = +1;
      if(MA_[bar] < MAl_[bar]) res = -1;

      if(RSI_[bar] > RSIl_[bar]) res += 1;
      if(RSI_[bar] < RSIl_[bar]) res -= 1;
      
      ExtMapBuffer[bar]=MathMax(MathMin(1,res),-1);
      ExtZerBuffer[bar]=0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
