//+------------------------------------------------------------------+
//|                                                 The_20s_v020.mq5 |
//|                                    Copyright © 2005, TraderSeven |
//|                                              TraderSeven@gmx.net |
//+------------------------------------------------------------------+
//--- Copyright
#property copyright "Copyright © 2005, TraderSeven"
//--- a link to the website of the author
#property link      "TraderSeven@gmx.net"
//--- indicator version
#property version   "1.10"
//--- drawing the indicator in the main window
#property indicator_chart_window 
//--- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//--- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Drawing parameters of the bearish indicator |
//+----------------------------------------------+
//--- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//--- Violet color is used as the indicator color
#property indicator_color1  clrViolet
//--- indicator 1 line width is equal to 4
#property indicator_width1  4
//--- display of the indicator bullish label
#property indicator_label1  "Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//--- drawing the indicator 2 as a symbol
#property indicator_type2   DRAW_ARROW
//--- DarkTurquoise color is used as the indicator color
#property indicator_color2  clrDarkTurquoise
//---- indicator 2 line width is equal to 4
#property indicator_width2  4
//--- display of the bearish indicator label
#property indicator_label2 "Buy"
//+----------------------------------------------+
//| declaration of constants                     |
//+----------------------------------------------+
#define RESET 0      // A constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| declaration of enumerations                  |
//+----------------------------------------------+
enum Mode
  {
   MODE_1,      // Option 1
   MODE_2       // Option 2
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input Mode Alg=MODE_1;    // Calculation algorithm
input uint Level=100;     // Trigger level in points
input double Ratio=0.2;   // Ratio 
input bool Direct=false;  // Signal direction 
//+----------------------------------------------+
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//---
double dLevel;
//--- declaration of integer variables for the indicators handles
int ATR_Handle;
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- initialization of global variables 
   int ATR_Period=15;
   min_rates_total=int(MathMax(5,ATR_Period));
//---- Getting the handle of the ATR indicator
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the ATR indicator");
      return(INIT_FAILED);
     }
   dLevel=Level*_Point;
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//--- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//--- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);
//--- setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- name for the data window and the label for sub-windows 
   string short_name="The_20s_v020";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- initialization end
   return(INIT_SUCCEEDED);
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
//--- checking if the number of bars is enough for the calculation
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- declarations of local variables 
   int to_copy,limit,bar;
   double LastBarsRange,Top20,Bottom20,ATR[];
//--- Calculations of the necessary number of copied data and limit starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
     {
      limit=rates_total-min_rates_total; // starting index for the calculation of all bars
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for the calculation of new bars
     }
   to_copy=limit+1;
//---- copy newly appeared data in the ATR[] array
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- apply timeseries indexing to array elements  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//--- main calculation loop of the indicator
   if(Direct)
      for(bar=limit; bar>=0 && !IsStopped(); bar--)
        {
         BuyBuffer[bar]=0.0;
         SellBuffer[bar]=0.0;
         //---
         LastBarsRange=high[bar+1]-low[bar+1];
         Top20=high[bar+1]-LastBarsRange*Ratio;
         Bottom20=low[bar+1]+LastBarsRange*Ratio;
         if(Alg==MODE_1)
           {
            if(open[bar+1]>=Top20 && close[bar+1]<=Bottom20 && low[bar]<=low[bar+1]-dLevel)
               BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
            else if(open[bar+1]<=Bottom20 && close[bar+1]>=Top20 && high[bar]>=high[bar+1]+dLevel)
                                 SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
           }
         //---
         if(Alg==MODE_2)
           {
            if((high[bar+4]-low[bar+4])>LastBarsRange && (high[bar+3]-low[bar+3])>LastBarsRange
               && (high[bar+2]-low[bar+2])>LastBarsRange && high[bar+2]>high[bar+1] && low[bar+2]<low[bar+1])
              {
               if(open[bar]<=Bottom20) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
               if(open[bar]>=Top20) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
              }
           }
        }

   if(!Direct)
      for(bar=limit; bar>=0 && !IsStopped(); bar--)
        {
         BuyBuffer[bar]=0.0;
         SellBuffer[bar]=0.0;
         //---
         LastBarsRange=high[bar+1]-low[bar+1];
         Top20=high[bar+1]-LastBarsRange*Ratio;
         Bottom20=low[bar+1]+LastBarsRange*Ratio;
         if(Alg==MODE_1)
           {
            if(open[bar+1]>=Top20 && close[bar+1]<=Bottom20 && low[bar]<=low[bar+1]-dLevel)
               SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
            else if(open[bar+1]<=Bottom20 && close[bar+1]>=Top20 && high[bar]>=high[bar+1]+dLevel)
                                 BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
           }
         //---
         if(Alg==MODE_2)
           {
            if((high[bar+4]-low[bar+4])>LastBarsRange && (high[bar+3]-low[bar+3])>LastBarsRange
               && (high[bar+2]-low[bar+2])>LastBarsRange && high[bar+2]>high[bar+1] && low[bar+2]<low[bar+1])
              {
               if(open[bar]<=Bottom20) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
               if(open[bar]>=Top20) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
              }
           }
        }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
