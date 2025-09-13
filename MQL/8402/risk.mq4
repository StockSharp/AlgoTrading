//+------------------------------------------------------------------+
//|                                                 Виктор Чеботарёв |
//|                                    http://www.chebotariov.co.ua/ |
//+------------------------------------------------------------------+
#property copyright "Виктор Чеботарёв"
#property link      "http://www.chebotariov.co.ua/"
extern double risk = 10;
   
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  
   int trade, cnt, cnt2, ticket;
   int total=OrdersTotal();
   double OpenLots=0;

//----
    for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()==OP_SELL || OrderType()==OP_BUY)
        {
         OpenLots += OrderLots();
        }
     }
//----

      
   double Lots=NormalizeDouble(AccountBalance()/1000,1);
   double raznica = MathMod(Lots,MarketInfo(Symbol(),MODE_LOTSTEP));
   if(raznica>0.1){Lots-=raznica;}

   double Lots2=NormalizeDouble(Lots*risk/100.0,1);
   if(raznica>0.1){Lots2-=raznica;}
   

   double Lots3=MathAbs(Lots2)-MathAbs(OpenLots);
   if(Lots3<0 && Lots3>(raznica*(-1))){Lots3=0;}
   
   int i,ks;
   double op,ou;
   for(i=0;i<HistoryTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if(OrderType()==OP_BUY || OrderType()==OP_SELL)
           {
            if(OrderProfit()>0)
              {
               op+=OrderProfit();
              }
               else
              {
               ou+=OrderProfit();
              }
            op+=OrderSwap();
            ks++;
           }
        }
     }
   double totalprofit=NormalizeDouble(op+ou,2);
   double totalprofitpersent=NormalizeDouble(100/(AccountBalance()-totalprofit)*totalprofit,2);
   
   Comment("Доступно: ",Lots,"   Разрешено: ",Lots2,"   Используется: ",OpenLots,"   Можно добавить: ",Lots3,"\n","Риск: ",risk,"%   Прибыль: ",AccountProfit()," (",NormalizeDouble(100/AccountBalance()*AccountProfit(),2),"%)   Общая прибыль: ",totalprofit," (",totalprofitpersent,"%)");
   
   return(0);
  }
//+------------------------------------------------------------------+