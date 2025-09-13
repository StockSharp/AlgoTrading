//+------------------------------------------------------------------+
//|                                                   StringInit.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTE(A) Print(#A, "=", (A) ? "true" : "false:" + (string)GetLastError())

//+------------------------------------------------------------------+
//| Helper function to show the given string and its metrics         |
//+------------------------------------------------------------------+
void StrOut(const string &s)
{
   Print("'", s, "' [", StringLen(s), "] ", StringBufferLen(s));
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string s = "message";
   StrOut(s);
   PRTE(StringReserve(s, 100)); // ok, but got larger capacity: 260
   StrOut(s);
   PRTE(StringReserve(s, 500)); // ok, buffer is expanded to 500
   StrOut(s);
   PRTE(StringSetLength(s, 4)); // ok: string is shrinking
   StrOut(s);
   s += "age";
   PRTE(StringReserve(s, 100)); // ok: but buffer is still 500
   StrOut(s);
   PRTE(StringSetLength(s, 8)); // no: string expansion not available
   StrOut(s);                   //     via StringSetLength
   PRTE(StringInit(s, 8, '$')); // ok: string is expanded and filled
   StrOut(s);                   //     buffer is the same
   PRTE(StringFill(s, 0));      // ok: string is collapsed, because it's
   StrOut(s);                   //     filled with 0, buffer is intact
   PRTE(StringInit(s, 0));      // ok: string is zeroed, including buffer
   // s = NULL;                 // equivalent of above
   StrOut(s);

   /*
      output:
   
   'message' [7] 0
   StringReserve(s,100)=true
   'message' [7] 260
   StringReserve(s,500)=true
   'message' [7] 500
   StringSetLength(s,4)=true
   'mess' [4] 500
   StringReserve(s,10)=true
   'message' [7] 500
   StringSetLength(s,8)=false:5035
   'message' [7] 500
   StringInit(s,8,'$')=true
   '$$$$$$$$' [8] 500
   StringFill(s,0)=true
   '' [0] 500
   StringInit(s,0)=true
   '' [0] 0
   
   */
}
//+------------------------------------------------------------------+