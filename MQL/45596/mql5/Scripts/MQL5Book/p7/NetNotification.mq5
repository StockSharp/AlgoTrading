//+------------------------------------------------------------------+
//|                                              NetNotification.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string message = MQLInfoString(MQL_PROGRAM_NAME)
      + " runs on " + AccountInfoString(ACCOUNT_SERVER)
      + " " + (string)AccountInfoInteger(ACCOUNT_LOGIN);
   Print("Sending notification: " + message);
   PRTF(SendNotification(NULL));    // INVALID_PARAMETER(4003)
   PRTF(SendNotification(message)); // NOTIFICATION_WRONG_SETTINGS(4517) or 0
}
//+------------------------------------------------------------------+
