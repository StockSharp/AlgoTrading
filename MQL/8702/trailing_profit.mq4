//+------------------------------------------------------------------+
//|                                                         трал.mq4 |
//|                     Copyright © 2008, Демёхин Виталий Евгеньевич |
//|                                             vitalya_1983@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Демёхин Виталий Евгеньевич"
#property link      "vitalya_1983@list.ru"

extern double percent_of_profit =33;
extern double minimum_profit = 1000;
double profit,profit_off,result;
bool trail_enable=false,close_start=false;
int init()
   {
   profit_off=0;
   result = 0;
   Comment ("");
   return(0);
   }
   
int start()
   {
   while (IsExpertEnabled())
      {
      Sleep (50);
      RefreshRates(); 
//----
      if (close_start)
         {
         for (int i=OrdersTotal();i>=1;i--)
            {
            OrderSelect (i-1,SELECT_BY_POS,MODE_TRADES);
               {
               if (OrderType()==OP_BUY)
                  {
                  double price = MarketInfo (OrderSymbol(),MODE_BID);
                  OrderClose (OrderTicket(),OrderLots(),price,3,0);
                  result = OrderProfit()+result;
                  }
               if (OrderType()==OP_SELL)
                  {
                  price = MarketInfo (OrderSymbol(),MODE_ASK);
                  OrderClose (OrderTicket(),OrderLots(),price,3,0);
                  result = OrderProfit()+result;
                  }
               }
            }
         }
      for (i=OrdersTotal();i>=1;i--)
         {
         OrderSelect(i-1,SELECT_BY_POS,MODE_TRADES);
         profit = OrderProfit()+OrderSwap()+profit;
         }
      if (close_start&&OrdersTotal()==0)
         {
         trail_enable=false;
         close_start= false;
         Alert ("Советник закрыл позиции с результатом ", result);
         profit_off=0;
         result = 0;
         }
      if (!trail_enable&&OrdersTotal()!=0)
         {
         Comment ("Режим трала выключен." ,"\n","Советник начнет сопровождать ордера при росте прибыли до ",minimum_profit,
                  " текущая прибыль ", profit);
         }
      if ((profit_off==0&&minimum_profit < profit)||(profit_off!=0&&profit_off<profit-profit*(percent_of_profit/100)))
         {
         trail_enable=true;
         profit_off=profit-profit*(percent_of_profit/100);
         Comment("Режим трала включен." ,"\n","Советник закроет ордера при падении прибыли до ",profit_off,
                  " максимальная прибыль ", profit);
         }
      if (trail_enable&&profit_off>profit)
         {
         close_start=true;
         }
      profit =0;
      }
//----
   return(0);
   }
//+------------------------------------------------------------------+