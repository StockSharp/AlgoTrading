//+------------------------------------------------------------------+
//|                                                   ChartUnfix.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script unlockes [sub]windows with fixed height mode, so      |
//| they become sizeable by user (default mode).                     |
//| The script saves, analyses, modifies and re-applies tpl-file.    |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Periods.mqh>
#include <MQL5Book/TplFileFull.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string filename = _Symbol + "-" + PeriodToString(_Period) + "-unfix-rw";
   if(PRTF(ChartSaveTemplate(0, "/Files/" + filename)))
   {
      int handle = PRTF(FileOpen(filename + ".tpl",
         FILE_READ | FILE_WRITE | FILE_TXT | FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_UNICODE));
      
      // parse tpl-file into this container
      Container main(handle);
      main.read();
      
      // locate all windows with fixed height
      Container *results[];
      const int found = main.findAll("/chart/window/indicator[fixed_height>-1]", results);
      if(found > 0)
      {
         PrintFormat("Found %d elements", found);
         for(int i = 0; i < found; ++i)
         {
            Container *block = results[i];
            block.print();
            // 'fixed_height=-1' means freely sizeable window
            block.assign("fixed_height", "-1");
         }
         main.write();
         FileClose(handle);
         PRTF(ChartApplyTemplate(0, "/Files/" + filename));
      }
      else
      {
         Print("Fixed height windows not found");
      }
   }
}
//+------------------------------------------------------------------+
