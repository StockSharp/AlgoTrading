//+---------------------------------------------------------------------+
//|                                                Candles_Smoothed.mq5 |
//|                                Copyright © 2011,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Smoothed Candles"
//---- indicator version number
#property version   "1.00"
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator in the main window
#property indicator_chart_window 
//----five buffers are used for calculation of drawing of the indicator
#property indicator_buffers 5
//---- only one plot is used
#property indicator_plots   1
//---- color candlesticks are used for display
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  SlateBlue, Magenta
//---- displaying the indicator label
#property indicator_label1  "Smoothed Candles Open; Smoothed Candles High; Smoothed Candles Low; Smoothed Candles Close"
//+-----------------------------------+
//|  Averagings classes description   |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMAO,XMAL,XMAH,XMAC;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
/*enum Smooth_Method - the enumeration is declared in the SmoothAlgorithms.mqh file
  {
   MODE_SMA_,  // SMA
   MODE_EMA_,  // EMA
   MODE_SMMA_, // SMMA
   MODE_LWMA_, // LWMA
   MODE_JJMA,  // JJMA
   MODE_JurX,  // JurX
   MODE_ParMA, // ParMA
   MODE_T3,    // T3
   MODE_VIDYA, // VIDYA
   MODE_AMA,   // AMA
  }; */
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input Smooth_Method MA_SMethod=MODE_LWMA; // Smoothing method
input int MA_Length=30;                   // Smoothing depth                    
input int MA_Phase=100;                   // Smoothing parameter
                                          // that changes within the range -100 ... +100 for JJMA
                                          // for VIDIA it is a CMO period, for AMA it is a slow average period
//+----------------------------------------------+
//---- declaration of dynamic arrays that 
// will be used as indicator buffers
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- initialization of global variables 
   StartBars=XMAO.GetStartBars(MA_SMethod,MA_Length,MA_Phase)+1;

//---- setting up alerts for unacceptable values of external parameters
   XMAO.XMALengthCheck("Length", MA_Length);
   XMAO.XMAPhaseCheck("Phase", MA_Phase, MA_SMethod);

//---- set dynamic arrays as indicator buffers
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- set dynamic array as a color index buffer   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,StartBars);

//---- setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- name for the data window and the label for sub-windows 
   string short_name="Smoothed Candles";
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
//---- checking the number of bars to be enough for the calculation
   if(rates_total<StartBars) return(0);

//---- declaration of local variables 
   int first,bar;

//---- calculation of the 'first' starting number for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      first=0; // starting index for calculation of all bars
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- main loop of the indicator calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- four calls of the XMASeries function.
      ExtOpenBuffer [bar]=XMAO.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, open [bar], bar, false);
      ExtCloseBuffer[bar]=XMAC.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, close[bar], bar, false);
      ExtHighBuffer [bar]=XMAH.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, high [bar], bar, false);
      ExtLowBuffer  [bar]=XMAL.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, low  [bar], bar, false);

      if(bar<=StartBars) continue;

      //--- candlesticks coloring
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else                                       ExtColorBuffer[bar]=1.0;

     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+