//+------------------------------------------------------------------+
//|                                                  qiut symbol.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "tk28@op.pl"
#property link      "https://www.mql5.com"
#property version   "2.0"
#property strict
extern double TPforSymbol = 113;
extern double SLforSymbol = 100000000;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   SLforSymbol=(-1)*MathAbs(SLforSymbol);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(number_posiotions_for_symbol()==0)Sleep(5000);
   else
     {
      double result1=result();
      if(result1>TPforSymbol || result1<SLforSymbol)
        {
         close_all_for_symbol();
         Sleep(100);
         if(number_posiotions_for_symbol()>=1)close_all_for_symbol();
         Sleep(100);
         if(number_posiotions_for_symbol()>=1)close_all_for_symbol();
         Sleep(3000);
         if(number_posiotions_for_symbol()>=1)Alert("There are still not closed posiotions. Check the positons!!!");
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double result()
  {
   double wyni=0;
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)Print("OrderSelect returned the error of ",GetLastError());
      else
      if(OrderSymbol()==Symbol())
        {
         wyni=wyni+OrderProfit()-MathAbs(OrderSwap())-MathAbs(OrderCommission());
         Comment("Profit: "+DoubleToStr(wyni,2)+" to exit: "+DoubleToStr(wyni-TPforSymbol,2));
        }
     }
   return(wyni);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int close_all_for_symbol()
  {
   Print("Close all positions for symbol: "+Symbol());
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)Print("OrderSelect returned the error of ",GetLastError());
      else
      if(OrderSymbol()==Symbol())
        {
         int ot=OrderTicket();
         double ol=OrderLots();
         if(OrderType()==OP_SELL)//krotkie
           {
            for(int pz=0;pz<10;pz++)if(close(ot,-1,ol)==0)break;
           }
         else
         if(OrderType()==OP_BUY)//dlugie
           {
            for(int pz2=0;pz2<10;pz2++)if(close(ot,1,ol)==0)break;
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int close(int t,int dl,double ilo)
  {
     {
      string pozd="don't know";
      if(dl==1)pozd="long";else if(dl==-1)pozd="short";
      double close_price=0;
      RefreshRates();
      if(dl==1)close_price=Bid;else close_price=Ask;
      //int tc = t;
      //OrderSelect(t,SELECT_BY_TICKET);
      if(OrderClose(t,ilo,close_price,1,Red)==false)
        {
         Print("Close position faild: ",GetLastError());
         Print(" ticket: ",t," ilosc: ",ilo," cena_zamk: ",DoubleToStr(close_price,5));
         return(999);
           }else {
         Print("Closed position "+pozd+" with result: ",OrderProfit()-MathAbs(OrderSwap())-MathAbs(OrderCommission()));
         //---
         return(0);
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int number_posiotions_for_symbol()
  {
   int wynikx = 0;
   for( int i = 0; i < OrdersTotal(); i++ )
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)Print("OrderSelect returned the error of ",GetLastError());
      else
      if(OrderSymbol()==Symbol())
        {
         wynikx++;
        }
     }
   return(wynikx);
  }
//+------------------------------------------------------------------+
