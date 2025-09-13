//+------------------------------------------------------------------+
//|                                                    автотрейд.mq4 |
//|                      Copyright © 2006, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, народное :-)"
#property link      "scrivimi@mail.ru"
extern int уровень_ордеров=20;
extern int уровень_профита=-2;
extern int истечение_минут=20;
extern int безусловная_фиксация=30;
extern int стабилизация_пунктов=25;
extern double лотов=0.1;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   double pip=MarketInfo(Symbol(),MODE_TICKSIZE);
//----
   if (OrdersTotal()==0)
     {
      double buy= Ask+уровень_ордеров*pip;
      double sell= Bid-уровень_ордеров*pip;
      int ticket1=OrderSend(Symbol(),OP_BUYSTOP,лотов,buy,3,0,0,"buy",16384,CurTime()+истечение_минут*60,Green);
      int ticket2=OrderSend(Symbol(),OP_SELLSTOP,лотов,sell,3,0,0,"buy",16384,CurTime()+истечение_минут*60,Green);
     }
   if (OrdersTotal()>0)
     {
      for(int i=0;i<=OrdersTotal();i++)
        {
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
//----
         ticket1=OrderTicket();
         double profit1=OrderProfit();
         double price1=OrderOpenPrice();
         if(OrderType()==OP_BUY)
           {
            OrderSelect(i+1,SELECT_BY_POS,MODE_TRADES);
            ticket2=OrderTicket();
            if(OrderType()==OP_SELLSTOP)//---проверка закрытия по прибыли №1:
              {
               OrderModify(OrderTicket(),OrderOpenPrice(),0,0,0,CLR_NONE);
               if(profit1>уровень_профита&&MathAbs(Close[1]-Open[1])<=стабилизация_пунктов*pip)
                 {
                  OrderClose(ticket1,лотов,Bid,3,CLR_NONE);
                  OrderDelete(ticket2);
                 }
               if(MathAbs(Close[1]-Open[1])<=стабилизация_пунктов*pip&&MathAbs(Close[2]-Open[2])<=стабилизация_пунктов*pip)
                 {
                  OrderClose(ticket1,лотов,Bid,3,CLR_NONE);
                  OrderDelete(ticket2);
                 }
               if(profit1>=безусловная_фиксация)
                 {
                  OrderClose(ticket1,лотов,Bid,3,CLR_NONE);
                  OrderDelete(ticket2);
                 }
              }
            if(OrderType()==OP_SELL)
              {//---сразу закрытие обоих:
               OrderClose(ticket1,лотов,Bid,3,CLR_NONE);
               OrderClose(ticket2,лотов,Ask,3,CLR_NONE);
              }
           }
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
         if(OrderType()==OP_BUYSTOP)
           {
            OrderSelect(i+1,SELECT_BY_POS,MODE_TRADES);
            ticket2=OrderTicket();
            if(OrderType()==OP_SELL)//---проверка закрытия по прибыли №2:
              {
               OrderModify(ticket1,price1,0,0,0,CLR_NONE);
               double profit2=OrderProfit();
               if(profit2>уровень_профита&&MathAbs(Open[1]-Close[1])<=стабилизация_пунктов*pip)
                 {
                  OrderClose(ticket2,лотов,Bid,3,CLR_NONE);
                  OrderDelete(ticket1);
                 }
               if(MathAbs(Open[1]-Close[1])<=стабилизация_пунктов*pip&&MathAbs(Open[2]-Close[2])<=стабилизация_пунктов*pip)
                 {
                  OrderClose(ticket2,лотов,Bid,3,CLR_NONE);
                  OrderDelete(ticket1);
                 }
               if(profit2>=безусловная_фиксация)
                 {
                  OrderClose(ticket2,лотов,Bid,3,CLR_NONE);
                  OrderDelete(ticket1);
                 }
              }
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+