
#property copyright "Copyright © 2015, adam malik bin kasang"
#property link      "adamkasang@gmail.com"

extern double equity_percent_from_balances=1.2;

int start()
  {
   Comment("     ",AccountName(),"              ACCOUNT  ",AccountNumber(),"           FREE MARGIN  ",AccountFreeMargin(),"          EQUITY  ",AccountEquity(),"            BALANCE  ",AccountBalance());

   if(AccountEquity()>AccountBalance()*equity_percent_from_balances)
     {
      int total=OrdersTotal();

      for(int i=total-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS)==true)
           {
            int type=OrderType();

            bool result=false;

            switch(type)
              {
               case OP_BUY       : result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5,Red);
               break;
               case OP_SELL      : result=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5,Red);
              }

            if(result==false)
              {
               Sleep(0);
              }
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
