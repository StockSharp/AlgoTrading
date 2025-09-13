//+------------------------------------------------------------------+
//|                                            IndFractalsZigZag.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 2
#property indicator_plots   1

// plot settings
#property indicator_type1   DRAW_ZIGZAG
#property indicator_color1  clrMediumOrchid
#property indicator_width1  2
#property indicator_label1  "ZigZag Up;ZigZag Down"

input int FractalOrder = 3;

// indicator buffers
double UpBuffer[];
double DownBuffer[];

// 10 pixels padding from extremums
const int ArrowShift = 10;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // indicator buffers mapping
   SetIndexBuffer(0, UpBuffer, INDICATOR_DATA);
   SetIndexBuffer(1, DownBuffer, INDICATOR_DATA);
   
   // empty value for drawings (can be omitted, as EMPTY_VALUE is default)
   PlotIndexSetDouble(0, PLOT_EMPTY_VALUE, EMPTY_VALUE);
   
   return FractalOrder > 0 ? INIT_SUCCEEDED : INIT_PARAMETERS_INCORRECT;
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
   if(prev_calculated == 0)
   {
      // initialize entire buffers on start
      ArrayInitialize(UpBuffer, EMPTY_VALUE);
      ArrayInitialize(DownBuffer, EMPTY_VALUE);
   }
   else
   {
      // initialize new bars
      for(int i = prev_calculated; i < rates_total; ++i)
      {
         UpBuffer[i] = EMPTY_VALUE;
         DownBuffer[i] = EMPTY_VALUE;
      }
   }
   
   // look throught all or new bars having FractalOrder bars in neighbourhood
   for(int i = fmax(prev_calculated - FractalOrder - 1, FractalOrder); i < rates_total - FractalOrder; ++i)
   {
      // check if this high is highest on neighbouring bars
      UpBuffer[i] = high[i];
      for(int j = 1; j <= FractalOrder; ++j)
      {
         if(high[i] <= high[i + j] || high[i] <= high[i - j])
         {
            UpBuffer[i] = EMPTY_VALUE;
            break;
         }
      }
      
      // check if this low is lowest on neighbouring bars
      DownBuffer[i] = low[i];
      for(int j = 1; j <= FractalOrder; ++j)
      {
         if(low[i] >= low[i + j] || low[i] >= low[i - j])
         {
            DownBuffer[i] = EMPTY_VALUE;
            break;
         }
      }
   }

   return rates_total;
}
//+------------------------------------------------------------------+
