//+------------------------------------------------------------------+
//|                                               StmtExpression.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

// errors:
// 'Print' - unexpected token, probably type is missing?
// 'Hello, ' - declaration without type
// 'Hello, ' - comma expected
// 'Symbol' - declaration without type
// '(' - comma expected
// ')' - semicolon expected
// ')' - expressions are not allowed on a global scope
//
// Print("Hello, ", Symbol());

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int i = 1, j = 2;
   
   i + j; // warning: expression has no effect
   
   Print("Hello, ", Symbol()); // ok: prints greeting in the log
}
//+------------------------------------------------------------------+
