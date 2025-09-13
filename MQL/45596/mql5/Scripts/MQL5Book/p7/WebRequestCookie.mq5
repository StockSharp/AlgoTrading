//+------------------------------------------------------------------+
//|                                             WebRequestCookie.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Execute 2 successive HTTP-requests collecting and sending out cookies."
#property description "NB: Default 'Address' requires to allow 'www.mql5.com' in terminal settings - to use other addresses, change settings accordingly."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/HTTPRequest.mqh>

input string Address = "https://www.mql5.com";
input string Headers = "User-Agent: Mozilla/5.0 (Windows NT 10.0) Chrome/103.0.0.0"; // Headers (use '|' as separator, if many specified)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   uchar result[];
   string response;
   HTTPRequest http(Headers);
   
   for(int i = 0; i < 2; ++i)
   {
      if(http.GET(Address, result, response) > -1)
      {
         if(ArraySize(result) > 0)
         {
            PrintFormat("Got data: %d bytes", ArraySize(result));
            if(i == 0) // show start of the document only once
            {
               const string s = CharArrayToString(result, 0, 160, CP_UTF8);
               int j = -1, k = -1;
               while((j = StringFind(s, "\r\n", j + 1)) != -1) k = j;
               Print(StringSubstr(s, 0, k));
            }
         }
      }
   }
}

//+------------------------------------------------------------------+
