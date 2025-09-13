//+------------------------------------------------------------------+
//|                                              StmtDeclaration.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

// error: 'Init' - undeclared identifier
// int k = Init(-1);

//+------------------------------------------------------------------+
//| Initialization wrapper with printing                             |
//+------------------------------------------------------------------+
int Init(const int v)
{
   Print("Init: ", v);
   return v;
}

int k = Init(-1);
int m = Init(-2);

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(k);
   
   int i = Init(1);
   Print(i);

   // error: 'n' - undeclared identifier
   // Print(n);
   static int n = Init(0);

   // error: 'j' - undeclared identifier
   // Print(j);
   int j = Init(2);
   Print(j);

   Print(n);
   
   int p;
   i = p; // warning: possible use of uninitialized variable 'p'
}
//+------------------------------------------------------------------+
