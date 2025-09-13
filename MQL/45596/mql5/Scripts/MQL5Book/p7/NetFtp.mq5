//+------------------------------------------------------------------+
//|                                                       NetFtp.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Periods.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string filename = _Symbol + "-" + PeriodToString() + "-"
      + (string)(ulong)TimeTradeServer() + ".png";
   PRTF(ChartScreenShot(0, filename, 300, 200));
   Print("Sending file: " + filename);
   PRTF(SendFTP(filename, "/upload")); // 0 or FTP_CONNECT_FAILED(4522), FTP_CHANGEDIR(4523), etc
}
//+------------------------------------------------------------------+
