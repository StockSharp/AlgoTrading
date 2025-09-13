//+------------------------------------------------------------------+
//|                                               WebRequestTest.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Request a web-page."
#property description "NB: Default 'Address' requires to allow 'httpbin.org' in terminal settings - to use other addresses, change settings accordingly."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/StringUtils.mqh>
#include <MQL5Book/URL.mqh>
#include <MQL5Book/HTTPHeader.mqh>

input string Method = "GET"; // Method (GET,POST,HEAD)
input string Address = "https://httpbin.org/headers";
input string Headers;
input int Timeout = 5000;
input bool DumpDataToFiles = true;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   uchar data[], result[];
   string response;
   
   int code = PRTF(WebRequest(Method, Address, Headers, Timeout, data, result, response));
   Print(response);
   if(code > -1)
   {
      if(ArraySize(result) > 0)
      {
         PrintFormat("Got data: %d bytes", ArraySize(result));
         if(DumpDataToFiles)
         {
            string parts[];
            URL::parse(Address, parts);
            
            const string filename = parts[URL_HOST] +
               (StringLen(parts[URL_PATH]) > 1 ? parts[URL_PATH] : "/_index_.htm");
            Print("Saving ", filename);
            PRTF(FileSave(filename, result));
         }
         else
         {
            Print(CharArrayToString(result, 0, 80, CP_UTF8));
         }
      }
   }
}
//+------------------------------------------------------------------+
