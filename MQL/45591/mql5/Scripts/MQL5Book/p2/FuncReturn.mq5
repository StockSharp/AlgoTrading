//+------------------------------------------------------------------+
//|                                                   FuncReturn.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

/* ERRORS
int func2(void)
{
   if(IsStopped()) return; // function must return a value

}                          // not all control paths return a value

void dummy(void)
{
   return false;           // 'return' - 'void' function returns a value
}

const string &message()    // '&' - reference cannot used
{
   static const string s = "Hello";
   return s;
}
*/

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int func(void)
{
   if(IsStopped()) return 0;
   return 1;
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string number(int i)
{
   return i; // warning: implicit conversion from 'number' to 'string'
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(func());
}
//+------------------------------------------------------------------+
