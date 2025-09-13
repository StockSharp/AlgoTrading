//+------------------------------------------------------------------+
//|                                            TradeReportWriter.mqh |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TradeReport.mqh>
#include <MQL5Book/AutoPtr.mqh>

#resource "TradeReportPage.htm" as string ReportPageTemplate
#resource "TradeReportTable.htm" as string ReportTableTemplate
#resource "TradeReportSVG.htm" as string SVGBoxTemplate

//+------------------------------------------------------------------+
//| Base abstract class to write trade stats into some storage       |
//+------------------------------------------------------------------+
class TradeReportWriter
{
protected:
   class DataHolder
   {
   public:
      double data[];                   // balance marks
      datetime when[];                 // time stamps for the marks
      string name;                     // description
      color clr;                       // visible color
      int width;                       // line width
      TradeReport::GenericStats stats; // trade statistics
   };
   
   AutoPtr<DataHolder> curves[];
   double lower, upper;
   datetime start, stop;

public:
   TradeReportWriter(): lower(DBL_MAX), upper(-DBL_MAX), start(0), stop(0) { }

   virtual bool addCurve(TradeReport::GenericStats &stats,
      double &data[], datetime &when[], const string name,
      const color clr = clrNONE, const int width = 1)
   {
      if(addCurve(data, when, name, clr, width))
      {
         curves[ArraySize(curves) - 1][].stats = stats;
         return true;
      }
      return false;
   }
   
   virtual bool addCurve(double &data[], datetime &when[], const string name,
      const color clr = clrNONE, const int width = 1)
   {
      if(ArraySize(data) == 0 || ArraySize(when) == 0) return false;
      if(ArraySize(data) != ArraySize(when)) return false;
      DataHolder *c = new DataHolder();
      if(!ArraySwap(data, c.data) || !ArraySwap(when, c.when))
      {
         delete c;
         return false;
      }

      const double max = c.data[ArrayMaximum(c.data)];
      const double min = c.data[ArrayMinimum(c.data)];
      
      lower = fmin(min, lower);
      upper = fmax(max, upper);
      if(start == 0) start = c.when[0];
      else if(c.when[0] != 0) start = fmin(c.when[0], start);
      stop = fmax(c.when[ArraySize(c.when) - 1], stop);
      
      c.name = name;
      c.clr = clr;
      c.width = width;
      ZeroMemory(c.stats); // no stats by default
      PUSH(curves, c);
      return true;
   }

   virtual void render() = 0;
};

//+------------------------------------------------------------------+
//| Concrete class to write trade stats into HTML-file               |
//+------------------------------------------------------------------+
class HTMLReportWriter: public TradeReportWriter
{
   int handle;
   int width, height;
   
public:
   HTMLReportWriter(const string name, const int w = 600, const int h = 400):
      width(w), height(h)
   {
      handle = FileOpen(name,
         FILE_WRITE | FILE_TXT | FILE_ANSI | FILE_REWRITE | FILE_SHARE_READ | FILE_SHARE_WRITE);
   }
   
   ~HTMLReportWriter()
   {
      if(handle != 0) FileClose(handle);
   }
   
   void close()
   {
      if(handle != 0) FileClose(handle);
      handle = 0;
   }

   virtual void render() override
   {
      string headerAndFooter[2];
      StringSplit(ReportPageTemplate, '~', headerAndFooter);
      FileWriteString(handle, headerAndFooter[0]);
      renderContent();
      FileWriteString(handle, headerAndFooter[1]);
   }
   
private:
   void renderContent()
   {
      renderSVG();
      renderTables();
   }
   
   void renderTable(const int k, TradeReport::GenericStats &stats,
      const string name, const color clr)
   {
      string table = ReportTableTemplate;
      StringReplace(table, "%COLOR%", colorToString(k, clr));
      StringReplace(table, "%NAME%", name);
      
      StringReplace(table, "%PROFITS%", DoubleToString(stats.profits, 2));
      StringReplace(table, "%PF%", DoubleToString(stats.pf, 2));
      StringReplace(table, "%LOSSES%", DoubleToString(stats.losses, 2));
      StringReplace(table, "%SHARPE%", DoubleToString(stats.sharpe, 2));
      StringReplace(table, "%NET%", DoubleToString(stats.profits + stats.losses, 2));
      StringReplace(table, "%RF%", DoubleToString(stats.recovery, 2));
      StringReplace(table, "%DEPOSIT%", DoubleToString(stats.series_start, 2));
      StringReplace(table, "%DEALS%", (string)stats.deals);
      
      StringReplace(table, "%BUYWIN%", (string)stats.buy_wins);
      StringReplace(table, "%BUYLOSS%", (string)(stats.buy_trades - stats.buy_wins));
      StringReplace(table, "%BUY%", (string)stats.buy_trades);
      StringReplace(table, "%SELLWIN%", (string)stats.sell_wins);
      StringReplace(table, "%SELLLOSS%", (string)(stats.trades - stats.buy_trades - stats.sell_wins));
      StringReplace(table, "%SELL%", (string)(stats.trades - stats.buy_trades));
      StringReplace(table, "%WIN%", (string)stats.wins);
      StringReplace(table, "%LOSS%", (string)(stats.trades - stats.wins));
      StringReplace(table, "%TOTAL%", (string)stats.trades);
      
      StringReplace(table, "%DD%", DoubleToString(stats.series_dd, 2));
      StringReplace(table, "%DD_PCT%", DoubleToString(stats.series_dd_percent, 2));
      StringReplace(table, "%DD_REL%", DoubleToString(stats.series_dd_relative, 2));
      StringReplace(table, "%DD_REL_PCT%", DoubleToString(stats.series_dd_relative_percent, 2));
      const double depo_dd = -fmin(stats.series_start - stats.series_min, 0);
      StringReplace(table, "%DD_DEPO%", DoubleToString(depo_dd, 2));
      StringReplace(table, "%DD_DEPO_PCT%", DoubleToString(depo_dd * 100 / stats.series_start, 2));
      
      StringReplace(table, "%MAX_PROFIT%", DoubleToString(stats.max_profit, 2));
      StringReplace(table, "%AVERAGE%", DoubleToString(stats.average_trade, 2));
      StringReplace(table, "%MAX_LOSS%", DoubleToString(stats.max_loss, 2));

      FileWriteString(handle, table);
   }
   
   void renderTables()
   {
      for(int i = 0; i < ArraySize(curves); ++i)      
      {
         renderTable(i, curves[i][].stats, curves[i][].name, curves[i][].clr);
      }
   }
   
   static string colorToString(const int k, const color c)
   {
      #define NUM_COLORS 6
      static const color autoColor[NUM_COLORS] =
         {clrRed, clrLimeGreen, clrBlue, clrMagenta, clrDodgerBlue, clrOrange};
      string s = ColorToString(c == clrNONE ? autoColor[k % NUM_COLORS] : c, true);
      StringReplace(s, "clr", "");
      StringToLower(s);
      return s;
      #undef NUM_COLORS
   }
   
   void renderCurve(const int k, const double &data[], const datetime &when[],
      const string name, const color c, const int w)
   {
      const string polyline = StringFormat(
         "<polyline stroke=\"%s\" stroke-width=\"%dpx\" fill=\"none\" points=\"",
         colorToString(k, c), w);
      FileWriteString(handle, polyline);
      const int n = ArraySize(data);
      for(int i = 0; i < n; ++i)
      {
         const int y = height - (int)((data[i] - lower) / fmax(upper - lower, 1) * height);
         const int x = (int)((when[i] - start) * width / fmax(stop - start, 1));
         FileWriteString(handle, (string)x + ", " + (string)y + " ");
      }
      const string polylineend = "\"/>\n";
      FileWriteString(handle, polylineend);
      
      const string legend = "<text x=\"%d\" y=\"%d\" fill=\"%s\" class=\"legend\">%s</text>";
      FileWriteString(handle, StringFormat(legend, 10, 20 + k * 10, colorToString(k, c), name));
   }
   
   void renderSVG()
   {
      string headerAndFooter[2];
      if(StringSplit(SVGBoxTemplate, '~', headerAndFooter) != 2) return;
      StringReplace(headerAndFooter[0], "%WIDTH%", (string)width);
      StringReplace(headerAndFooter[0], "%HEIGHT%", (string)height);
      FileWriteString(handle, headerAndFooter[0]);

      for(int i = 0; i < ArraySize(curves); ++i)      
      {
         renderCurve(i, curves[i][].data, curves[i][].when,
            curves[i][].name, curves[i][].clr, curves[i][].width);
      }
      
      FileWriteString(handle, headerAndFooter[1]);
   }
};
//+------------------------------------------------------------------+
