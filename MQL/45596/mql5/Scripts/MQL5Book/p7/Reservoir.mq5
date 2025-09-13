//+------------------------------------------------------------------+
//|                                                    Reservoir.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Test script to store and restore user data to/from resource      |
//+------------------------------------------------------------------+
#include <MQL5Book/Reservoir.mqh>
#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string resource = "::reservoir";

   Reservoir res1;
   string message = "message1";     // custom string to write into resource
   PRTF(res1.packString(message));
   
   MqlTick tick1[1];                // simple struct to add
   SymbolInfoTick(_Symbol, tick1[0]);
   PRTF(res1.packArray(tick1));
   PRTF(res1.packNumber(DBL_MAX));  // double value
   
   /*
   // complex structures with strings and objects not supported,
   // compiler error: 'MqlParam' has objects and cannot be used as union member
   MqlParam param[1] = {{}};
   res1.packArray(param);
   */

   res1.submit(resource);           // commit our custom data into the resource
   res1.clear();                    // make the reservoir object empty

   string reply;                    // new variable to receive the message back
   MqlTick tick2[1];                // new struct to receive the tick back
   double result;                   // new variable to receive the number
   
   PRTF(res1.acquire(resource));    // attach the object to specified resource
   PRTF(res1.unpackString(reply));  // restore custom string
   PRTF(res1.unpackArray(tick2));   // restore simple struct
   PRTF(res1.unpackNumber(result)); // restore double value

   // output and compare restored data (string, struct, and number)
   PRTF(reply);
   PRTF(ArrayCompare(tick1, tick2));
   ArrayPrint(tick2);
   PRTF(result == DBL_MAX);

   // make sure the reservoir was read till the end   
   PRTF(res1.size());
   PRTF(res1.cursor());
   
   PrintFormat("Cleaning up local storage '%s'", resource);
   ResourceFree(resource);
}
//+------------------------------------------------------------------+
/*
   example output

   res1.packString(message)=4 / ok
   res1.packArray(tick1)=20 / ok
   res1.packNumber(DBL_MAX)=23 / ok
   res1.acquire(resource)=true / ok
   res1.unpackString(reply)=4 / ok
   res1.unpackArray(tick2)=20 / ok
   res1.unpackNumber(result)=23 / ok
   reply=message1 / ok
   ArrayCompare(tick1,tick2)=0 / ok
                    [time]   [bid]   [ask] [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.05.19 23:09:32 1.05867 1.05873 0.0000        0 1653001772050       6       0.00000
   result==DBL_MAX=true / ok
   res1.size()=23 / ok
   res1.cursor()=23 / ok
   Cleaning up local storage '::reservoir'

*/
//+------------------------------------------------------------------+
