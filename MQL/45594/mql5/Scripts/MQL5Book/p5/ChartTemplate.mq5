//+------------------------------------------------------------------+
//|                                                ChartTemplate.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Periods.mqh>
#include <MQL5Book/TplFile.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // make sure Momentum(14) is not yet added, and add if not
   const int w = ChartWindowFind(0, "Momentum(14)");
   if(w == -1)
   {
      const int momentum = iMomentum(NULL, 0, 14, PRICE_TYPICAL);
      
      ChartIndicatorAdd(0, (int)ChartGetInteger(0, CHART_WINDOWS_TOTAL), momentum);
      
      // not necessary here, because the script will exit soon,
      // but it states explicitly our intention to not use this handle anymore
      IndicatorRelease(momentum);
   }
   
   ResetLastError();
   
   const string filename = _Symbol + "-" + PeriodToString(_Period) + "-momentum-rw";
   if(PRTF(ChartSaveTemplate(0, "/Files/" + filename)))
   {
      int handle = PRTF(FileOpen(filename + ".tpl",
         FILE_READ | FILE_WRITE | FILE_TXT | FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_UNICODE));
      // could use a second file for output
      // int writer = PRTF(FileOpen(filename + "w.tpl",
      //    FILE_WRITE | FILE_TXT | FILE_SHARE_READ | FILE_SHARE_WRITE));
      
      // parse tpl-file into this container
      Container main(handle);
      main.read();
      
      // locate indicator in a nested container
      Container *found = main.find("/chart/window/indicator[name=Momentum]");
      if(found)
      {
         found.print();
         // add visualization on monthly timeframe, if not yet present
         Container *period = found.find("*/period[period_type=3]");
         if(period == NULL)
         {
            period = found.add("period");
            period.assign("period_type", "3");
            period.assign("period_size", "1");
         }
         else
         {
            Print("Monthly period already exists");
         }
      }

      main.write(); // or main.write(writer);
      FileClose(handle);
      
      PRTF(ChartApplyTemplate(0, "/Files/" + filename));
   }
}
//+------------------------------------------------------------------+
