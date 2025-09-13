//+------------------------------------------------------------------+
//|                                               UseDemoAllLoop.mq5 |
//|                                    Copyright (c) 2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
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
input bool ClearHandles = true;
input ENUM_DRAW_TYPE DrawType = DRAW_ARROW; // Drawing Type
input int DrawLineWidth = 1; // Drawing Line Width

const string IndicatorCustom = "LifeCycle";

// globals
BufferArray buffers(5);
int Handle = INVALID_HANDLE;

IndicatorType MainLoop[] =
{
   iCustom_,
   iAlligator_jawP_jawS_teethP_teethS_lipsP_lipsS_method_price,
   iAMA_period_fast_slow_shift_price,
   iBands_period_shift_deviation_price,
   iDEMA_period_shift_price,
   iEnvelopes_period_shift_method_price_deviation,
   iFractals_,
   iFrAMA_period_shift_price,
   iIchimoku_tenkan_kijun_senkou,
   iMA_period_shift_method_price,
   iSAR_step_maximum,
   iTEMA_period_shift_price,
   iVIDyA_momentum_smooth_shift_price,
};

const int N = ArraySize(MainLoop);
int Cursor = 0;
IndicatorType IndicatorSelector;

void OnTimer()
{
   if(Handle != INVALID_HANDLE && ClearHandles)
   {
      IndicatorRelease(Handle);
      /*
      // Handle is still 10, but it is not valid anymore
      // if uncomment this code fragment, we'll get the following error
      double data[1];
      const int n = CopyBuffer(Handle, 0, 0, 1, data);
      Print("Handle=", Handle, " CopyBuffer=", n, " Error=", _LastError);
      // Handle=10 CopyBuffer=-1 Error=4807 (ERR_INDICATOR_WRONG_HANDLE)
      */
   }

   IndicatorSelector = MainLoop[Cursor];
   Cursor = ++Cursor % N;
   
   // create the handle
   AutoIndicator indicator(IndicatorSelector,
      (IndicatorSelector == iCustom_ ? IndicatorCustom : ""), "");
   Handle = indicator.getHandle();
   if(Handle == INVALID_HANDLE)
   {
      Print(StringFormat("Can't create indicator: %s",
         _LastError ? E2S(_LastError) : "The name or number of parameters is incorrect"));
   }
   else
   {
      Print("Handle=", Handle);
   }
   
   buffers.empty(); // clean up for all buffers
   
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
      "(default-params)");
   ChartSetSymbolPeriod(0, NULL, 0); // request refreshing whole drawing
}

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   Comment("Wait 5 seconds to start looping through indicator set");
   EventSetTimer(5);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_SHORT_PARAM_LIST)
{
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
