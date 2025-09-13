//+------------------------------------------------------------------+
//|                                             UseDemoAllSimple.mq5 |
//|                                    Copyright (c) 2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//| This is a simplified version of UseDemoAll.mq5                   |
//+------------------------------------------------------------------+
#define BUF_NUM 5 // among built-in indicators this is max number

// drawing settings
#property indicator_chart_window
#property indicator_buffers BUF_NUM
#property indicator_plots   BUF_NUM

// includes
#include <MQL5Book/IndCommon.mqh>
#include <MQL5Book/AutoIndicator.mqh>
#include <MQL5Book/IndBufArray.mqh>
#include <MQL5Book/MqlError.mqh>

// inputs
input IndicatorType IndicatorSelector = iMA_period_shift_method_price; // Built-in Indicator Selector
input string IndicatorCustom = ""; // Custom Indicator Name
input string IndicatorParameters = "11,0,sma,close"; // Indicator Parameters (comma,separated,list)
input ENUM_DRAW_TYPE DrawType = DRAW_LINE; // Drawing Type
input int DrawLineWidth = 1; // Drawing Line Width

// globals
BufferArray buffers(5);
const string Title = "UseDemoAllIndicators";
int Handle;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // parse input data, prepare array of MqlParam and create the handle
   AutoIndicator indicator(IndicatorSelector, IndicatorCustom, IndicatorParameters);
   Handle = indicator.getHandle();
   if(Handle == INVALID_HANDLE)
   {
      Alert(StringFormat("Can't create indicator: %s",
         _LastError ? E2S(_LastError) : "The name or number of parameters is incorrect"));
      return INIT_FAILED;
   }
   
   IndicatorSetString(INDICATOR_SHORTNAME, Title);

   // prepare colors and text for plots setup
   static color defColors[BUF_NUM] = {clrBlue, clrGreen, clrRed, clrCyan, clrMagenta};
   const string s = indicator.getName();
   const int n = (IndicatorSelector != iCustom_) ? IND_BUFFERS(IndicatorSelector) : BUF_NUM;

   // setup all buffers/plots
   for(int i = 0; i < BUF_NUM; ++i)
   {
      PlotIndexSetString(i, PLOT_LABEL, s + "[" + (string)i + "]");
      PlotIndexSetInteger(i, PLOT_DRAW_TYPE, i < n ? DrawType : DRAW_NONE);
      PlotIndexSetInteger(i, PLOT_LINE_WIDTH, DrawLineWidth);
      PlotIndexSetInteger(i, PLOT_LINE_COLOR, defColors[i]);
      PlotIndexSetInteger(i, PLOT_SHOW_DATA, i < n);
   }
   
   Comment("DemoAll: ", (IndicatorSelector == iCustom_ ? IndicatorCustom : s),
      "(", IndicatorParameters, ")");
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_SHORT_PARAM_LIST)
{
   if(Handle == INVALID_HANDLE)
   {
      return 0;
   }

   // wait until the subindicator is calculated for all bars
   if(BarsCalculated(Handle) != rates_total)
   {
      return prev_calculated;
   }
   
   // get buffer count from built-in indicator or use maximum for custom (unknown)
   const int m = (IndicatorSelector != iCustom_) ? IND_BUFFERS(IndicatorSelector) : BUF_NUM;
   // copy data from subordinate indicator into our buffers
   for(int k = 0; k < m; ++k)
   {
      // fill our buffer with data from the handle,
      const int n = buffers[k].copy(Handle,
         k, 0, rates_total - prev_calculated + 1);
         
      if(_LastError != 0 && _LastError != 4007 && _LastError != 4806)
      {
         Comment("Error: ", _LastError);
      }
      
      // clean up on problems
      if(n < 0)
      {
         buffers[k].empty(EMPTY_VALUE, prev_calculated, rates_total - prev_calculated);
      }
   }
   
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization function                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Print(__FUNCSIG__);
   Comment("");
}
//+------------------------------------------------------------------+
