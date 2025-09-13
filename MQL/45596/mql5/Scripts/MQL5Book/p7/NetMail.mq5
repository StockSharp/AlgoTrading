//+------------------------------------------------------------------+
//|                                                      NetMail.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string message = "Hello from "
      + AccountInfoString(ACCOUNT_SERVER)
      + " " + (string)AccountInfoInteger(ACCOUNT_LOGIN);
   Print("Sending email: " + message);
   PRTF(SendMail(MQLInfoString(MQL_PROGRAM_NAME), message)); // MAIL_SEND_FAILED(4510) or 0
}
//+------------------------------------------------------------------+
