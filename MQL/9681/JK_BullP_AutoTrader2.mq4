//+------------------------------------------------------------------+
//|                                          JK_BullP_AutoTrader.mq4 |
//|                                     Copyright © 2005, Johnny Kor |
//|                                                   autojk@mail.ru |
//|        On-Line Testing http://vesna.on-plus.ru/forex/stat/69740/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Johnny Kor"
#property link      "autojk@mail.ru"
//---- input parameters
extern double TakeProfit = 500;
extern double Lots = 8.5;
extern double StopLoss = 20;
extern int TrailingStop = 10;
extern double Patr = 1, Prange = 1, Kstop = 0.1, kts = 2, Vts = 2; 
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   double pos1pre, pos2cur, hzbul1, hzbul2, hzbear1, hzbear2;
   int cnt, ticket, total, TotalOpenOrders;
   pos1pre = iBullsPower(NULL, 0, 13, PRICE_CLOSE, 2);
   pos2cur = iBullsPower(NULL, 0, 13, PRICE_CLOSE, 1);
   total = OrdersTotal();
   int H, L;
   for(cnt = 0; cnt < 30; cnt++)
     {
       if(H == 0)
         {
           if(H < High[cnt]) 
               H = High[cnt];
         }
     }
   for(cnt = 0; cnt < 30; cnt++)
     {
       if(L == 0)
         {
           if(L > Low[cnt]) 
               L = Low[cnt];
         }
     }
   Comment("  BullsPower - ", pos1pre, "  BearsPower - ", pos2cur);
   int j;
   for(cnt = 0; cnt < total; cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderType() <= OP_SELL && // это открытая позиция? OP_BUY или OP_SELL 
          OrderSymbol() == Symbol())    // инструмент совпадает?
         {
           j = 2;
           if(OrderType() == OP_BUY)   // открыта длинная позиция
             {
               // проверим - может можно/нужно уже трейлинг стоп ставить?
               if(TrailingStop > 0)  // пользователь выставил в настройках трейлингстоп
                 {                 // значит мы идем его проверять
                   if(Bid- OrderOpenPrice() > Point*TrailingStop)
                     {
                       if(OrderStopLoss() < Bid - Point*TrailingStop)
                         {
                           OrderModify(OrderTicket(), OrderOpenPrice(), 
                                       Bid-Point*TrailingStop, 
                                       OrderTakeProfit(), 0, Red);
                           return(0);
                         }
                     }
                 }
             }
           else // иначе это короткая позиция
             {
               // проверим - может можно/нужно уже трейлинг стоп ставить?
               if(TrailingStop > 0)  // пользователь выставил в настройках трейлингстоп
                 {                 // значит мы идем его проверять
                   if((OrderOpenPrice() - Ask) > (Point*TrailingStop))
                     {
                       if(OrderStopLoss() == 0.0 || 
                          OrderStopLoss() > (Ask + Point*TrailingStop))
                         {
                           OrderModify(OrderTicket(), OrderOpenPrice(), 
                                       Ask + Point*TrailingStop, 
                                       OrderTakeProfit(), 0, Red);
                           return(0);
                         }
                     }
                 }
             }
         }
     }
   if(pos1pre > pos2cur && pos2cur > 0  && j < 2)
     {
       OrderSend(Symbol(), OP_SELL, Lots, Bid, 3, Bid + StopLoss*Point, 
                 Bid - TakeProfit*Point, NULL, 0, 0, Red);
     }   
   if(pos2cur < 0 && j < 1)
     {
       OrderSend(Symbol(), OP_BUY, Lots, Ask, 3, Ask - StopLoss*Point, 
                 Ask + TakeProfit*Point, NULL, 0, 0, Red);
     }
   return(0);
  }
//+------------------------------------------------------------------+