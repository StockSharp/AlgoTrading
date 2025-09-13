//+------------------------------------------------------------------+
//|                                                  IndSubChart.mq5 |
//|                               Copyright (c) 2019-2021, Marketeer |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2019-2021, Marketeer"
#property link      "https://www.mql5.com/en/users/marketeer"
#property version   "1.1"
#property description "Display arbitrary symbol quotes (as line/candles/bars) in a subwindow, synchronized by time with current chart."

#property indicator_separate_window
#property indicator_buffers 4
#property indicator_plots   1

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/QuoteRefresh.mqh>

// inputs
input string SubSymbol = ""; // Symbol
input bool Exact = false;

// globals
string symbol;
ENUM_CHART_MODE mode = 0;

// Buffers: OHLC
double open[];
double high[];
double low[];
double close[];

//+------------------------------------------------------------------+
//| Single buffer setup                                              |
//+------------------------------------------------------------------+
void InitBuffer(const int index, double &buffer[],
   const ENUM_INDEXBUFFER_TYPE style = INDICATOR_DATA,
   const bool asSeries = false)
{
   SetIndexBuffer(index, buffer, style);
   ArraySetAsSeries(buffer, asSeries);
}

//+------------------------------------------------------------------+
//| Group of buffers setup                                           |
//+------------------------------------------------------------------+
string InitBuffers(const ENUM_CHART_MODE m)
{
   string title;
   if(m == CHART_LINE)
   {
      InitBuffer(0, close, INDICATOR_DATA, true);
      // hide all other buffers unneeded for line drawing
      InitBuffer(1, high, INDICATOR_CALCULATIONS, true);
      InitBuffer(2, low, INDICATOR_CALCULATIONS, true);
      InitBuffer(3, open, INDICATOR_CALCULATIONS, true);
      title = symbol + " Close";
   }
   else
   {
      InitBuffer(0, open, INDICATOR_DATA, true);
      InitBuffer(1, high, INDICATOR_DATA, true);
      InitBuffer(2, low, INDICATOR_DATA, true);
      InitBuffer(3, close, INDICATOR_DATA, true);
      title = "# Open;# High;# Low;# Close";
      StringReplace(title, "#", symbol);
   }
   return title;
}

//+------------------------------------------------------------------+
//| All-in-one plot setup                                            |
//+------------------------------------------------------------------+
void InitPlot(const int index, const string name, const int style,
   const int width = -1, const int colorx = -1,
   const double empty = EMPTY_VALUE)
{
  PlotIndexSetInteger(index, PLOT_DRAW_TYPE, style);
  PlotIndexSetString(index, PLOT_LABEL, name);
  PlotIndexSetDouble(index, PLOT_EMPTY_VALUE, empty);
  if(width != -1) PlotIndexSetInteger(index, PLOT_LINE_WIDTH, width);
  if(colorx != -1) PlotIndexSetInteger(index, PLOT_LINE_COLOR, colorx);
}

//+------------------------------------------------------------------+
//| Adjust plot colors according to the chart settings               |
//+------------------------------------------------------------------+
void SetPlotColors(const int index, const ENUM_CHART_MODE m)
{
   if(m == CHART_CANDLES)
   {
      PlotIndexSetInteger(index, PLOT_COLOR_INDEXES, 3);
      PlotIndexSetInteger(index, PLOT_LINE_COLOR, 0, (int)ChartGetInteger(0, CHART_COLOR_CHART_LINE));  // rectangle
      PlotIndexSetInteger(index, PLOT_LINE_COLOR, 1, (int)ChartGetInteger(0, CHART_COLOR_CANDLE_BULL)); // up
      PlotIndexSetInteger(index, PLOT_LINE_COLOR, 2, (int)ChartGetInteger(0, CHART_COLOR_CANDLE_BEAR)); // down
   }
   else
   {
      PlotIndexSetInteger(index, PLOT_COLOR_INDEXES, 1);
      PlotIndexSetInteger(index, PLOT_LINE_COLOR, (int)ChartGetInteger(0, CHART_COLOR_CHART_LINE));
   }
}

//+------------------------------------------------------------------+
//| Convert ENUM_CHART_MODE to ENUM_DRAW_TYPE                        |
//+------------------------------------------------------------------+
ENUM_DRAW_TYPE Mode2Style(const ENUM_CHART_MODE m)
{
   switch(m)
   {
      case CHART_CANDLES: return DRAW_CANDLES;
      case CHART_BARS: return DRAW_BARS;
      case CHART_LINE: return DRAW_LINE;
   }
   return DRAW_NONE;
}

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   Print("");
   symbol = SubSymbol;
   if(symbol == "") symbol = _Symbol;
   else
   {
      // ensure symbol is available in the market watch
      if(!SymbolSelect(symbol, true))
      {
         return INIT_PARAMETERS_INCORRECT;
      }
   }

   mode = (ENUM_CHART_MODE)ChartGetInteger(0, CHART_MODE);
   InitPlot(0, InitBuffers(mode), Mode2Style(mode));
   SetPlotColors(0, mode);

   IndicatorSetString(INDICATOR_SHORTNAME, "SubChart (" + symbol + ")");
   IndicatorSetInteger(INDICATOR_DIGITS, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Chart event handler function                                     |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &, const double &, const string &) // unused
{
   if(id == CHARTEVENT_CHART_CHANGE)
   {
      const ENUM_CHART_MODE newmode = (ENUM_CHART_MODE)ChartGetInteger(0, CHART_MODE);
      if(mode != newmode)
      {
         const ENUM_CHART_MODE oldmode = mode;
         mode = newmode;
         // change buffer mappings and their drawing types for the plot (ad hoc!)
         InitPlot(0, InitBuffers(mode), Mode2Style(mode));
         SetPlotColors(0, mode);
         if(oldmode == CHART_LINE || newmode == CHART_LINE)
         {
            // switching to or from CHART_LINE requires to refresh entire chart
            // including recalculation, because the number of buffers has changed
            Print("Refresh");
            ChartSetSymbolPeriod(0, _Symbol, _Period);
         }
         else
         {
            // while switching between candles and bars it's sufficient to draw
            // the same data in a different manner, because the 4 buffers are the same
            Print("Redraw");
            ChartRedraw();
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total, const int prev_calculated,
   const datetime &time[],
   const double &op[], const double &hi[], const double &lo[], const double &cl[],
   const long &[], const long &[], const int &[]) // unused
{
   // because we can monitor another symbol we need our own bar count
   // and custom prev_calculated variable
   static bool initialized;  // other symbol readiness flag
   static int lastAvailable; // other symbol bar count
   int _prev_calculated = prev_calculated;

   if(iBars(symbol, _Period) - lastAvailable > 1) // bar gap filled
   {
      _prev_calculated = 0;
      initialized = false;
      lastAvailable = 0;
   }

   if(_prev_calculated == 0)
   {
      ArrayInitialize(open, EMPTY_VALUE);
      ArrayInitialize(high, EMPTY_VALUE);
      ArrayInitialize(low, EMPTY_VALUE);
      ArrayInitialize(close, EMPTY_VALUE);
   }

   if(_Symbol != symbol)
   {
      if(!initialized)
      {
         Print("Host ", _Symbol, " ", rates_total, " bars up to ", (string)time[0]);
         Print("Updating ", symbol, " ", lastAvailable, " -> ", iBars(symbol, _Period),
            " / ", (iBars(symbol, _Period) > 0 ? (string)iTime(symbol, _Period, iBars(symbol, _Period) - 1) : "n/a"),
            "... Please wait");
         if(QuoteRefresh(symbol, _Period, time[0]))
         {
            Print("Done");
            initialized = true;
         }
         else
         {
            // OPTION:
            // consider calling EventSetTimer(timeout) to give the system more time
            // to build timeseries and call ChartSetSymbolPeriod in OnTimer handler
            ChartSetSymbolPeriod(0, _Symbol, _Period); // request for async update
            return 0;
         }
      }
     
      ArraySetAsSeries(time, true);
      for(int i = 0; i < MathMax(rates_total - _prev_calculated, 1); ++i)
      {
         int x = iBarShift(symbol, _Period, time[i], Exact);
         if(x != -1)
         {
            open[i] = iOpen(symbol, _Period, x);
            high[i] = iHigh(symbol, _Period, x);
            low[i] = iLow(symbol, _Period, x);
            close[i] = iClose(symbol, _Period, x);
         }
         else
         {
            open[i] = high[i] = low[i] = close[i] = EMPTY_VALUE;
         }
      }
   }
   else
   {
      ArraySetAsSeries(op, true);
      ArraySetAsSeries(hi, true);
      ArraySetAsSeries(lo, true);
      ArraySetAsSeries(cl, true);
      for(int i = 0; i < MathMax(rates_total - _prev_calculated, 1); ++i)
      {
         open[i] = op[i];
         high[i] = hi[i];
         low[i] = lo[i];
         close[i] = cl[i];
      }
   }
  
   lastAvailable = iBars(symbol, _Period);
  
   return rates_total;
}
//+------------------------------------------------------------------+
