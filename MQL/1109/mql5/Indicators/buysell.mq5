//+------------------------------------------------------------------+
//|                                                      BuySell.mq5 |
//|                                          Copyright © 2008, bobik | 
//|                                             bobik@trah.guchka.eu | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, bobik"
#property link "bobik@trah.guchka.eu"
#property description "BuySell "
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//----two buffers are used for calculation of drawing of the indicator
#property indicator_buffers 4
//---- only two plots are used
#property indicator_plots   4
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//---- red color is used for the indicator
#property indicator_color1  Red
//---- indicator 1 width is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "Lower BuySell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_ARROW
//---- LightSeaGreen color is used for the indicator
#property indicator_color2  LightSeaGreen
//---- indicator 2 width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2 "Upper BuySell"
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 3 as a symbol
#property indicator_type3   DRAW_ARROW
//---- magenta color is used for the indicator
#property indicator_color3  DeepPink
//---- indicator 3 width is equal to 4
#property indicator_width3  4
//---- displaying of the bullish label of the indicator
#property indicator_label3  "BuySell Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 4 as a symbol
#property indicator_type4   DRAW_ARROW
//---- LightSeaGreen color is used for the indicator
#property indicator_color4  LightSeaGreen
//---- indicator 4 width is equal to 4
#property indicator_width4  4
//---- displaying of the bearish label of the indicator
#property indicator_label4 "BuySell Buy"
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // the constant for getting the command for the indicator recalculation back to the terminal

//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input uint MA_Period=14;                        //indicator period
input ENUM_MA_METHOD MA_Method=MODE_SMA;        //smoothing type
input ENUM_APPLIED_PRICE MA_Price=PRICE_CLOSE;  //price
input uint ATR_Period=60;                       //ATR period
//+----------------------------------------------+

//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double BuyBuffer[],SellBuffer[];
double UpBuffer[],DnBuffer[];
//---- declaration of integer variables for the indicators handles
int MA_Handle,ATR_Handle;
//---- declaration of the integer variables for the start of data calculation
int  min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- Initialization of variables    
   min_rates_total=int(MA_Period+ATR_Period);

//---- getting handle of the iMA indicator
   MA_Handle=iMA(NULL,0,MA_Period,0,MA_Method,MA_Price);
   if(MA_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMA indicator");
   
//---- getting handle of the ATR indicator
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)Print(" Failed to get handle of the ATR indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,158);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(UpBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- shifting the beginning of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,158);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(DnBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,SellBuffer,INDICATOR_DATA);
//---- shifting the beginning of drawing of the indicator 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,167);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(3,BuyBuffer,INDICATOR_DATA);
//---- shifting the beginning of drawing of the indicator 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(3,PLOT_ARROW,167);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- name for the data window and the label for sub-windows 
   string short_name="BuySell";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate
(const int rates_total,
 const int prev_calculated,
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
    || BarsCalculated(ATR_Handle)<rates_total
    || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int limit,to_copy,bar;
   double MA[],ATR[];

//--- calculations of the necessary amount of data to be copied and
//----the limit starting number for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
      limit=rates_total-min_rates_total-2; // starting index for calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars 
   to_copy=limit+2;

//---- copy newly appeared data in the SAR array
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(MA,true);
   ArraySetAsSeries(ATR,true);
   
//---- first indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- zero out the contents of the indicator buffers for calculation
      DnBuffer[bar]=0.0;
      UpBuffer[bar]=0.0;
           
      if (MA[bar]>MA[bar+1]) DnBuffer[bar]=MA[bar]-ATR[bar];
      if (MA[bar]<MA[bar+1]) UpBuffer[bar]=MA[bar]+ATR[bar];
     }
     
//---- recalculation of the starting index for calculation of all bars
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator     
     limit--;
   
//---- the second indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- zero out the contents of the indicator buffers for calculation
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
 
      if(UpBuffer[bar+1]&&DnBuffer[bar]) BuyBuffer [bar]=DnBuffer[bar];     
      if(DnBuffer[bar+1]&&UpBuffer[bar]) SellBuffer[bar]=UpBuffer[bar];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
