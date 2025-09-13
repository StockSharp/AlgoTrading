//+------------------------------------------------------------------+
//|                                                   TypeString.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string h = "Hello";          // Hello
   string b = "Press \"OK\"";   // Press "OK"
   string z = "";               //
   string t = "New\nLine";      // New
   // Line
   string e = "\0Hidden text";  //
   string n = "123";            // 123, text (not an integer value)
   string m = "very long message "
              "can be presented " 
              "by parts";
   // equivalent:
   // string m = "very long message can be presented by parts"; 

   PRT(h);
   PRT(b);
   PRT(z);
   PRT(t);
   PRT(e);
   PRT(n);
   PRT(m);
}

//+------------------------------------------------------------------+
