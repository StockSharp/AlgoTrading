//+------------------------------------------------------------------+
//|                                                     TypeChar.mq5 |
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
   char a1 = 'a';  // ok, a1 = 97, English letter 'a' code
   char a2 = 97;   // ok, a2 = 'a' as well
   char b = '£';   // warning: truncation of constant value, b = -93
   uchar c = '£';  // ok, c = 163, pound symbol code
   short d = '£';  // ok

   short z = '\0';    // ok, 0
   short t = '\t';    // ok, 9
   short s1 = '\x5c'; // ok, backslash code 92
   short s2 = '\\';   // ok, backslash as is, code 92 as well
   short s3 = '\0134';// ok, backslash code in octal form

   PRT(a1);
   PRT(a2);
   PRT(b);
   PRT(c);
   PRT(z);
   PRT(t);
   PRT(s1);
   PRT(s2);
   PRT(s3);
}

//+------------------------------------------------------------------+
