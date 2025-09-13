//+------------------------------------------------------------------+
//|                                                 StringModify.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "='", (A), "'")

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string text = "  \tAbCdE F1  ";
   PRT(StringToLower(text));   // 'true'
   PRT(text);                  // '  \tabcde f1  '
   PRT(StringToUpper(text));   // 'true'
   PRT(text);                  // '  \tABCDE F1  '
   PRT(StringTrimLeft(text));  // '3'
   PRT(text);                  // 'ABCDE F1  '
   PRT(StringTrimRight(text)); // '2'
   PRT(text);                  // 'ABCDE F1'
   PRT(StringTrimRight(text)); // '0'  (no more whitespace to remove)
   PRT(text);                  // 'ABCDE F1'
                               //       ↑
                               //       └internal blank remains
   
   string russian = "Русский Текст";
   PRT(StringToUpper(russian));  // 'true'
   PRT(russian);                 // 'РУССКИЙ ТЕКСТ'
   
   string german = "straßenführung";
   PRT(StringToUpper(german));   // 'true'
   PRT(german);                  // 'STRAßENFÜHRUNG'
}
//+------------------------------------------------------------------+