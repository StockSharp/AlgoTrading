//+------------------------------------------------------------------+
//|                                                   RSI_Expert.mq4 |
//|                                                             Joni |
//|                                                  JoniH88@mail.ru |
//+------------------------------------------------------------------+
#property copyright     "Joni"
#property link          "JoniH88@mail.ru"
#property version       "1.00"
#property description   "JoniH88@mail.ru"
#property strict
//---
input double   lot       = 0.01;       //Лот
input int      SL        = 0;          //Stop Loss (в пунктах)
input int      TP        = 0;          //Take Profit (в пунктах)
input int      stepTrall = 0;          //Трейлинг стоп (в пунктах)
//---
input int      periodRSI    = 14;      //Период RSI
input double   levelUpRSI   = 70.0;    //Верхний уровень RSI
input double   levelDownRSI = 30.0;    //Нижний уровень RSI
//---
input int      Slippage = 3;           //Проскальзывание (в пунктах)
input int      magic    = 19;
//---
datetime gBar;
int      gSlippage;
double   gPoint;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(Digits==5) gPoint=0.0001;
   else
     {
      if(Digits==3) gPoint=0.01;
      else gPoint=Point;
     }
   gSlippage=(int)NormalizeDouble(Slippage*gPoint/Point,0);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(gBar!=Time[0])
     {
      gBar=Time[0];
      trade(1);
      trall(stepTrall);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void trade(int _numBar)
  {
   int _typeTrade=typeTrade();
   if(_typeTrade==1){   openOrder(OP_BUY,lot,SL,TP); closeAllSell();}
   if(_typeTrade==-1){  openOrder(OP_SELL,lot,SL,TP); closeAllBuy();}
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int typeTrade()
  {
   double rsi_1 = iRSI(Symbol(), PERIOD_CURRENT, periodRSI, PRICE_CLOSE, 1);
   double rsi_2 = iRSI(Symbol(), PERIOD_CURRENT, periodRSI, PRICE_CLOSE, 2);
   if(rsi_1 > levelDownRSI && rsi_2 < levelDownRSI)   return 1;
   if(rsi_1 < levelUpRSI && rsi_2 > levelUpRSI)       return -1;
   return 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void trall(int step)
  {
   if(MarketInfo(Symbol(), MODE_STOPLEVEL) > step * gPoint / Point) return;
   int ordersTotal=OrdersTotal();
   for(int i=ordersTotal-1; i>=0; i --)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic)
           {
            setSl(step);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void setSl(int sl)
  {
   double _sl=0.0;
   if(OrderType()==OP_BUY)
     {
      _sl=ND(Bid -(double)sl*gPoint);
      if(_sl <= OrderStopLoss()) return;
     }
   if(OrderType()==OP_SELL)
     {
      _sl=ND(Ask+(double)sl*gPoint);
      //Print("sl = ", sl, "  ordersl = ", OrderStopLoss());
      if(OrderStopLoss() != 0.0 && _sl >= OrderStopLoss()) return;
     }
   bool f=OrderModify(OrderTicket(),OrderOpenPrice(),_sl,OrderTakeProfit(),0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeAllBuy()
  {
   int i;
   int ordersTotal=OrdersTotal();
   for(i=ordersTotal-1; i>=0; i --)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_BUY)
           {
            if(OrderStopLoss()==0 || OrderStopLoss()-OrderOpenPrice()<0) closeOpder(OP_BUY);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeAllSell()
  {
   int i;
   int ordersTotal=OrdersTotal();
   for(i=ordersTotal-1; i>=0; i --)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_SELL)
           {
            if(OrderStopLoss()==0 || OrderOpenPrice()-OrderStopLoss()<0) closeOpder(OP_SELL);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int openOrder(int cmd,double lots,int _stop_loss,int _take_profit)
  {
   double sl = 0;
   double tp = 0;
   int t=0;
   int error=0;
   RefreshRates();
   ResetLastError();
   while(true)
     {
      if(cmd==OP_SELL)
        {
         if(_stop_loss==0) sl=0;
         else sl=NormalizeDouble(Ask+_stop_loss*gPoint,Digits);
         if(_take_profit==0) tp=0;
         else tp=NormalizeDouble(Ask-_take_profit*gPoint,Digits);
         t=OrderSend(Symbol(),OP_SELL,lots,NormalizeDouble(Bid,Digits),gSlippage,sl,tp,NULL,magic,0,Red);
        }
      if(cmd==OP_BUY)
        {
         if(_stop_loss==0) sl=0;
         else sl=NormalizeDouble(Bid-_stop_loss*gPoint,Digits);
         if(_take_profit==0) tp=0;
         else tp=NormalizeDouble(Bid+_take_profit*gPoint,Digits);
         t=OrderSend(Symbol(),OP_BUY,lots,NormalizeDouble(Ask,Digits),gSlippage,sl,tp,NULL,magic,0,Blue);
        }
      //---
      if(t>0) break;
      error=GetLastError();
      switch(error)
        {
         case 135: Print("Цена изменилась. Пробую ещё ...");
         RefreshRates();
         continue;
         case 136: Print("Нет цен. Жду новый тик ...");
         while(RefreshRates()==false)
            Sleep(1);
         continue;
         case 146: Print("Подсистема торговли занята. Пробую ещё ...");
         Sleep(500);
         RefreshRates();
         continue;
         case 138: Print("Цена устарела. Пробую ещё ...");
         Sleep(500);
         RefreshRates();
         continue;
         case 129: Print("Неправильная цена при попытке открыть ордер. Пробую ещё ...");
         Sleep(5000);
         RefreshRates();
         continue;
        }
      switch(error) // Критические ошибки
        {
         case 2 : Print("Общая ошибка.");
         break;
         case 5 : Print("Старая версия клиентского терминала.");
         break;
         case 64: Print("Счет заблокирован.");
         break;
         case 133:Print("Торговля запрещена");
         break;
         case 130:Print("Слишком маленький СЛ или ТП");
         break;
         case 134:Print("Не хватает средств");
         break;
         default: Print("Возникла ошибка: ",error);// Другие варианты   
        }
      break;                                    // Выход из цикла
     }
   return t;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeAll()
  {
   int i;
   int ordersTotal=OrdersTotal();
   if(ordersTotal>0)
     {
      for(i=ordersTotal-1; i>=0; i --)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_SELL)
              {
               if(OrderStopLoss()==0 || OrderOpenPrice()-OrderStopLoss()<0) closeOpder(OP_SELL);
              }
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_BUY)
              {
               if(OrderStopLoss()==0 || OrderStopLoss()-OrderOpenPrice()<0) closeOpder(OP_BUY);
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeOpder(int cmd)
  {
   bool er=false;
   int error=0;
   RefreshRates();
   ResetLastError();
   while(true)
     {
      if(cmd==OP_BUY)
        {
         er=OrderClose(OrderTicket(),OrderLots(),Bid,gSlippage,Blue);
        }
      if(cmd==OP_SELL)
        {
         er=OrderClose(OrderTicket(),OrderLots(),Ask,gSlippage,Red);
        }
      if(er == true) break;
      error = GetLastError();
      switch(error) // Преодолимые ошибки
        {
         case 135: Print("Цена изменилась. Пробую ещё ...");
         RefreshRates();
         continue;
         case 136: Print("Нет цен. Жду новый тик ...");
         while(RefreshRates()==false)
            Sleep(1);
         continue;
         case 146: Print("Подсистема торговли занята. Пробую ещё ...");
         Sleep(500);
         RefreshRates();
         continue;
         case 129: Print("Неправильная цена при попытке закрыть ордер. Пробую ещё...");
         Sleep(5000);
         RefreshRates();
         continue;
        }
      switch(error)
        {
         case 2 : Print("Общая ошибка.");
         break;
         case 5 : Print("Старая версия клиентского терминала.");
         break;
         case 64: Print("Счет заблокирован.");
         break;
         case 133: Print("Торговля запрещена");
         break;
         default: Print("Возникла ошибка: ",error);
        }
      break;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double ND(double value)
  {
   return NormalizeDouble(value, Digits);
  }
//+------------------------------------------------------------------+
