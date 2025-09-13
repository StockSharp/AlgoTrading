//+------------------------------------------------------------------+
//|                                                       Locker.mq4 |
//|                     Copyright © 2008, Демёхин Виталий Евгеньевич |
//|                                             vitalya_1983@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Демёхин Виталий Евгеньевич"
#property link      "vitalya_1983@list.ru"
extern double  NeedProfit  = 0.001,   //На сколько процентов Мы увеличиваем баланс?
               StepLot     = 0.2,   //Вторичные лоты
               Lot         = 0.5;   //Стартовый лот
extern int     Step        = 50;    //Шаги между локированием
extern bool    spasenie    = true;
string text = "Locker.mq4";
int  i,ticket1,ticket2;
double ChekPoint,Profit1,Profit2,HighBuy,LowSell;
bool mode_buy,mode_sell,Stop=false;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
  double buy_profit=0;
  double sell_profit=0;
//----
   for (i=OrdersTotal();i>=1;i--)                        //Сюда мы втыкае индикатора
      {
      OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() == OP_SELL && OrderSymbol () ==Symbol())  
         {
         sell_profit=sell_profit+OrderProfit();
         Lot=OrderLots();
         StepLot = NormalizeDouble (Lot/1.2,2);
         }
      if(OrderType() == OP_BUY && OrderSymbol () ==Symbol())  
         {
         buy_profit=buy_profit+OrderProfit();
         Lot=OrderLots();
         StepLot = NormalizeDouble (Lot/1.2,2);
         }
      if (sell_profit<buy_profit)
      mode_buy=true;
      if (sell_profit>buy_profit)
      mode_sell=true;
      }
      
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
  
//----
   if ((spasenie&&!Stop)||!spasenie)
      {
      int orders=0;
      bool Otkryt_orders = false;
      for (i=OrdersTotal();i>=1;i--)                        //Сюда мы втыкае индикатора
         {
         OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
         if((OrderType() == OP_SELL||OrderType() == OP_BUY) && OrderSymbol () ==Symbol())  
         Otkryt_orders = true;
         orders++;
         }
      if (!Otkryt_orders||mode_buy||mode_sell)
         {
         if (!Otkryt_orders&&(!mode_buy&&!mode_sell))
            {
            RefreshRates();
            OrderSend(Symbol(),OP_BUY,Lot,Ask,5,0,0,text,0,Blue);
            }
         if (mode_buy&&!mode_sell)
            {
            mode_buy = false;
            RefreshRates();
            OrderSend(Symbol(),OP_BUY,StepLot,Ask,5,0,0,text,0,Blue);
            }
         if (!mode_buy&&mode_sell)
            {
            mode_sell=false;
            RefreshRates();
            OrderSend(Symbol(),OP_SELL,StepLot,Bid,5,0,0,text,0,Blue);
            }
      
         ChekPoint = Ask;
         HighBuy=Ask;
         LowSell=Bid;
         }
      if (Otkryt_orders)
         {
         double CurrentProfit=0;                                        //Подсчитываем доходность серии
         for (i=OrdersTotal();i>=1;i--)
            {
            OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
            if((OrderType() == OP_SELL||OrderType() == OP_BUY) && OrderSymbol () ==Symbol())  
            CurrentProfit=CurrentProfit+OrderProfit();
            }
         if (orders>=8)                                                 //Если ордеров много, закрываем перекрытые ордера
            {
            for (i=OrdersTotal();i>=1;i--)
               {
               ticket1 = 0;
               Profit1 = 0;
               OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
               if(OrderType() == OP_SELL&& OrderSymbol () ==Symbol())  
                  {
                  ticket1 = OrderTicket();
                  Profit1 = OrderProfit();
                  }
               for (int n=OrdersTotal();n>=1;n--)
                  {
                  ticket2 = 0;
                  Profit2 = 0;
                  OrderSelect(n-1, SELECT_BY_POS, MODE_TRADES);
                  if(OrderType() == OP_BUY&& OrderSymbol () ==Symbol()&&(ticket1!=0&&ticket2!=0))
                     {
                     ticket2 = OrderTicket();
                     Profit2 = OrderProfit();
                     if (Profit1==Profit2*(-1))
                        {
                        RefreshRates();
                        OrderSelect(ticket1, SELECT_BY_TICKET);
                        OrderClose(OrderTicket() ,OrderLots(),Ask,5,Blue);
                        RefreshRates();
                        OrderSelect(ticket2, SELECT_BY_TICKET);
                        OrderClose(OrderTicket() ,OrderLots(),Bid,5,Blue);
                        break;
                        }
                     }
                  }
               }
            }
         if (CurrentProfit>=NeedProfit*AccountBalance())                   //Если наш профит у нас в кармане, закрываемся
            {
            for (i=OrdersTotal();i>=1;i--)
               {
               OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
               if(OrderType() == OP_SELL && OrderSymbol () ==Symbol())  
                  {
                  RefreshRates ();
                  OrderClose(OrderTicket() ,OrderLots(),Ask,5,Blue);
                  }
               if(OrderType() == OP_BUY && OrderSymbol () ==Symbol())  
                  {
                  RefreshRates ();
                  OrderClose(OrderTicket() ,OrderLots(),Bid,5,Red);
                  }
               }
            if (spasenie)
            Stop=false;
            }
         if (CurrentProfit<-1*NeedProfit*AccountBalance())                            //Если в минусе, ставим Локи. 
            {
            if (Ask>ChekPoint+(Step*Point)&&Ask>HighBuy)
               {
               Alert (ChekPoint+(Step*Point));
               RefreshRates();
               OrderSend(Symbol(),OP_BUY,StepLot,Ask,5,0,0,text,0,Blue);
               ChekPoint = Ask;
               HighBuy=Ask;
               }
            if (Bid<ChekPoint-(Step*Point)&&Bid<LowSell)
               {
               RefreshRates();
               OrderSend(Symbol(),OP_SELL,StepLot,Bid,5,0,0,text,0,Blue);
               ChekPoint = Bid;
               LowSell=Bid;
               }
            }
         }
      }
//----
   return(0);
  }
//+------------------------------------------------------------------+