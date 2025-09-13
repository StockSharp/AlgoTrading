//+------------------------------------------------------------------+
//|                                                    GoodTimes.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Return new greeting on every call                                |
//+------------------------------------------------------------------+
string Greeting()
{
   static int counter = 0;
   static const string messages[3] =
   {
      "Good morning", "Good day", "Good evening"
   };
   // error demo: 'messages' - constant cannot be modified
   // messages[0] = "Good night";
   return messages[counter++ % 3];
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(Greeting(), ", ", Symbol());
   Print(Greeting(), ", ", Symbol());
   Print(Greeting(), ", ", Symbol());

   // Print(counter); // error: 'counter' - undeclared identifier
}

//+------------------------------------------------------------------+
