

//+------------------------------------------------------------------+
//|                                                 BW-wiseMan-1.mq5 |
//|                                          Copyright © 2005, wellx |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
//--- copyright
#property copyright "Copyright © 2005, wellx"
//--- a link to the website of the author
#property link      "http://www.metaquotes.net"
//--- indicator version
#property version   "1.00"
//--- drawing the indicator in the main window
#property indicator_chart_window 
//--- two buffers are used for calculating and drawing the indicator
#property indicator_buffers 2
//--- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Drawing parameters of the bearish indicator |
//+----------------------------------------------+
//--- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//--- pink is used for the color of the bearish indicator line
#property indicator_color1  clrMagenta
//--- indicator 1 line width is equal to 4
#property indicator_width1  4
//--- display of the indicator bullish label
#property indicator_label1  "BW-wiseMan-1 Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//--- drawing the indicator 2 as a symbol
#property indicator_type2   DRAW_ARROW
//---- light blue is used as the color of the bullish line of the indicator
#property indicator_color2  clrDodgerBlue
//---- indicator 2 line width is equal to 4
#property indicator_width2  4
//--- display of the bearish indicator label
#property indicator_label2 "BW-wiseMan-1 Buy"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0                // The constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint                    back=2;                       // Number of bars for analysis
input uint                    jaw_period=13;                // Period for calculating jaws
input uint                    jaw_shift=8;                  // Horizontal shift of jaws
input uint                    teeth_period=8;               // Teeth calculation period
input uint                    teeth_shift=5;                // Horizontal shift of teeth
input uint                    lips_period=5;                // Period for calculating lips
input uint                    lips_shift=3;                 // Horizontal shift of lips
input ENUM_MA_METHOD          ma_method=MODE_SMMA;          // Smoothing type
input ENUM_APPLIED_PRICE      applied_price=PRICE_MEDIAN;   // Price type or handle
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//--- declaration of integer variables for the indicators handles
int ALG_Handle,ATR_Handle;
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- initialization of global variables 
   min_rates_total=int(MathMax(lips_period,MathMax(jaw_period,teeth_period)));
//--- getting the handle of the iAlligator indicator
   ALG_Handle=iAlligator(NULL,0,jaw_period,jaw_shift,teeth_period,teeth_shift,lips_period,lips_shift,ma_method,applied_price);
   if(ALG_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the handle of iAlligator");
      return(INIT_FAILED);
     }
//---- Getting the handle of the ATR indicator
   ATR_Handle=iATR(NULL,0,15);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the ATR indicator");
      return(INIT_FAILED);
     }
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- shifting the start of drawing the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,119);
//--- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- shifting the starting point of calculation of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,119);
//--- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);
//--- setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- name for the data window and the label for sub-windows
   string short_name="BW-wiseMan-1";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---
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
   if(BarsCalculated(ALG_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- declarations of local variables 
   int to_copy,limit,bar;
   double JAW[],TEETH[],LIPS[],ATR[];
//--- calculations of the necessary amount of data to be copied
//--- and the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total; // starting index for calculating all bars
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for calculating new bars
     }
   to_copy=limit+1;
//--- copy newly appeared data in the arrays
   if(CopyBuffer(ALG_Handle,GATORJAW_LINE,0,to_copy,JAW)<=0) return(RESET);
   if(CopyBuffer(ALG_Handle,GATORTEETH_LINE,0,to_copy,TEETH)<=0) return(RESET);
   if(CopyBuffer(ALG_Handle,GATORLIPS_LINE,0,to_copy,LIPS)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- apply timeseries indexing to array elements  
   ArraySetAsSeries(JAW,true);
   ArraySetAsSeries(TEETH,true);
   ArraySetAsSeries(LIPS,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//--- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //--- sell signals
      if(low[bar]>LIPS[bar] && low[bar]>TEETH[bar] && low[bar]>JAW[bar] && close[bar]<(high[bar]+low[bar])/2)
        {
         bool contup=true;
         for(int i=1; i<=int(back); i++)
           {
            if(high[bar]<=high[bar+i])
              {
               contup=false;
               break;
              }
           }
         if(contup) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
        }
      //--- сигналы для покупок
      if(high[bar]<LIPS[bar] && high[bar]<TEETH[bar] && high[bar]<JAW[bar] && close[bar]>(high[bar]+low[bar])/2)
        {
         bool contup=true;
         for(int i=1; i<=int(back); i++)
           {
            if(low[bar]>=low[bar+i])
              {
               contup=false;
               break;
              }
           }
         if(contup) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+

   