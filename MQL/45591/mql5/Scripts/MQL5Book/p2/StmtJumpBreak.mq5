//+------------------------------------------------------------------+
//|                                                StmtJumpBreak.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int a[10][10] = {0};

   for(int i = 0; i < 10; ++i)
   {
      for(int j = 0; j < 10; ++j)
      {
         if(j > i)
            break;
         a[i][j] = (i + 1) * (j + 1);
      }
   }

   ArrayPrint(a);

   string s = "Hello, " + Symbol();
   ushort d = 0;
   const int n = StringLen(s);

   for(int i = 0; i < n && d == 0; ++i)
   {
      for(int j = i + 1; j < n; ++j)
      {
         if(s[i] == s[j])
         {
            d = s[i];
            break;
         }
      }
   }

   PrintFormat("Duplicate: %c", d);

   int count = 0;
   while(++count < 100)
   {
      Comment(GetTickCount());
      Sleep(1000);
      if(IsStopped())
      {
         Print("Terminated by user");
         break;
      }
   }
   Comment("");
}
//+------------------------------------------------------------------+
