//+------------------------------------------------------------------+
//|   e-News-Lucky$.mq4                                              |
//|   Lucky$ & KimIV                                                 |
//|   http://www.kimiv.ru                                            |
//|                                                                  |
//|   24.10.2005                                                     |
//|   Выставление ордеров в определённое время на пробой диапазона.  |
//|   Если ни один ордер не сработал, то модификация на каждом баре. |
//+------------------------------------------------------------------+
#property copyright "Lucky$ & KimIV"
#property link      "http://www.kimiv.ru"
//----
#define   MAGIC     20051024
//------- Внешние параметры советника --------------------------------
extern string _Parameters_Trade="----- Параметры торговли";
extern double Lots          =0.1;     // Размер торгуемого лота
extern int    StopLoss      =15;      // Размер фиксированного стопа
extern int    TakeProfit    =0;       // Размер фиксированного тэйка
extern string TimeSetOrders ="10:30"; // Время установки ордеров
extern string TimeDelOrders ="22:30"; // Время удаления ордеров
extern int    DistanceSet   =20;      // Расстояние от рынка
extern bool   UseTrailing   =True;    // Использовать трал
extern bool   ProfitTrailing=True;    // Тралить только профит
extern int    TrailingStop  =25;      // Фиксированный размер трала
extern int    TrailingStep  =5;       // Шаг трала
extern int    Slippage      =3;       // Проскальзывание цены
//----
extern string _Parameters_Expert="----- Параметры советника";
extern bool   UseOneAccount=False;        // Торговать только на одном счёте
extern int    NumberAccount=11111;        // Номер торгового счёта
extern string Name_Expert  ="e-News-Lucky$";
extern bool   UseSound     =True;         // Использовать звуковой сигнал
extern string NameFileSound="expert.wav"; // Наименование звукового файла
extern color  clOpenBuy    =LightBlue;    // Цвет открытия покупки
extern color  clOpenSell   =LightCoral;   // Цвет открытия продажи
extern color  clModifyBuy  =Aqua;         // Цвет модификации покупки
extern color  clModifySell =Tomato;       // Цвет модификации продажи
extern color  clCloseBuy   =Blue;         // Цвет закрытия покупки
extern color  clCloseSell  =Red;          // Цвет закрытия продажи
//---- Глобальные переменные советника -------------------------------
int prevBar;
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
  void deinit() 
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
  void start() 
  {
   if (TimeToStr(CurTime(), TIME_MINUTES)==TimeSetOrders) SetOrders();
   if (prevBar!=Bars && ExistOrder(1) && ExistOrder(2)) ModifyOrders();
   DeleteOppositeOrders();
   TrailingPositions();
     if (TimeToStr(CurTime(), TIME_MINUTES)==TimeDelOrders) 
     {
      DeleteAllOrders();
      CloseAllPositions();
     }
   prevBar=Bars;
  }
//+------------------------------------------------------------------+
//| Установка ордеров                                                |
//+------------------------------------------------------------------+
   void SetOrders() 
   {
   double ldStop=0, ldTake=0;
   int    spr=MarketInfo(Symbol(), MODE_SPREAD);
   double pAsk=Ask+(DistanceSet+spr)*Point;
   double pBid=Bid-DistanceSet*Point;
//----
     if (!ExistOrder(1)) 
     {
      if (StopLoss!=0) ldStop=pAsk-StopLoss*Point;
      if (TakeProfit!=0) ldTake=pAsk+TakeProfit*Point;
      SetOrder(OP_BUYSTOP, pAsk, ldStop, ldTake, 1);
     }
     if (!ExistOrder(2)) 
     {
      if (StopLoss!=0) ldStop=pBid+StopLoss*Point;
      if (TakeProfit!=0) ldTake=pBid-TakeProfit*Point;
      SetOrder(OP_SELLSTOP, pBid, ldStop, ldTake, 2);
     }
  }
//+------------------------------------------------------------------+
//| Модификация ордеров                                              |
//+------------------------------------------------------------------+
  void ModifyOrders() 
  {
   bool   fm;
   double ldStop=0, ldTake=0;
   int    spr=MarketInfo(Symbol(), MODE_SPREAD);
   double pAsk=Ask+(DistanceSet+spr)*Point;
   double pBid=Bid-DistanceSet*Point;
//----
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC+1) 
           {
            if (StopLoss!=0) ldStop=pAsk-StopLoss*Point;
            if (TakeProfit!=0) ldTake=pAsk+TakeProfit*Point;
            OrderModify(OrderTicket(), pAsk, ldStop, ldTake, 0, clModifyBuy);
           }
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC+2) 
           {
            if (StopLoss!=0) ldStop=pBid+StopLoss*Point;
            if (TakeProfit!=0) ldTake=pBid-TakeProfit*Point;
            OrderModify(OrderTicket(), pBid, ldStop, ldTake, 0, clModifySell);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Возвращает флаг существования ордера или позиции по номеру       |
//+------------------------------------------------------------------+
  bool ExistOrder(int mn) 
  {
   bool Exist=False;
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC+mn) 
           {
            Exist=True; break;
           }
        }
     }
   return(Exist);
  }
//+------------------------------------------------------------------+
//| Возвращает флаг существования позиции по номеру                  |
//+------------------------------------------------------------------+
  bool ExistPosition(int mn) 
  {
   bool Exist=False;
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC+mn) 
           {
              if (OrderType()==OP_BUY || OrderType()==OP_SELL) 
              {
               Exist=True; break;
              }
           }
        }
     }
   return(Exist);
  }
//+------------------------------------------------------------------+
//| Установка ордера                                                 |
//| Параметры:                                                       |
//|   op     - операция                                              |
//|   pp     - цена                                                  |
//|   ldStop - уровень стоп                                          |
//|   ldTake - уровень тейк                                          |
//|   mn     - добавить к MAGIC                                      |
//+------------------------------------------------------------------+
  void SetOrder(int op, double pp, double ldStop, double ldTake, int mn) 
  {
   color  clOpen;
   string lsComm=GetCommentForOrder();
//----
   if (op==OP_BUYSTOP) clOpen=clOpenBuy;
   else clOpen=clOpenSell;
   OrderSend(Symbol(),op,Lots,pp,Slippage,ldStop,ldTake,lsComm,MAGIC+mn,0,clOpen);
   if (UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+
//| Генерирует и возвращает строку коментария для ордера или позиции |
//+------------------------------------------------------------------+
  string GetCommentForOrder() 
  {
   return(Name_Expert);
  }
//+------------------------------------------------------------------+
//| Удаление всех ордеров                                            |
//+------------------------------------------------------------------+
  void DeleteAllOrders() 
  {
   bool fd;
     for(int i=OrdersTotal()-1; i>=0; i--) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderMagicNumber()>MAGIC && OrderMagicNumber()<=MAGIC+2) 
           {
              if (OrderSymbol()==Symbol()) 
              {
                 if (OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP) 
                 {
                  fd=OrderDelete(OrderTicket());
                  if (fd && UseSound) PlaySound(NameFileSound);
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Закрытие всех позиций по рыночной цене                           |
//+------------------------------------------------------------------+
  void CloseAllPositions() 
  {
   bool fc;
     for(int i=OrdersTotal()-1; i>=0; i--) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderMagicNumber()>MAGIC && OrderMagicNumber()<=MAGIC+2) 
           {
              if (OrderSymbol()==Symbol()) 
              {
               fc=False;
                 if (OrderType()==OP_BUY) 
                 {
                  fc=OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, clCloseBuy);
                 }
                 if (OrderType()==OP_SELL) 
                 {
                  fc=OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, clCloseSell);
                 }
               if (fc && UseSound) PlaySound(NameFileSound);
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Удаление противоположных ордеров                                 |
//+------------------------------------------------------------------+
  void DeleteOppositeOrders() 
  {
   bool fd, fep1, fep2;
//----
   fep1=ExistPosition(1);
   fep2=ExistPosition(2);
//----
     for(int i=OrdersTotal()-1; i>=0; i--) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol()) 
           {
            fd=False;
              if (OrderType()==OP_BUYSTOP && OrderMagicNumber()==MAGIC+1) 
              {
               if (fep2) fd=OrderDelete(OrderTicket());
              }
              if (OrderType()==OP_SELLSTOP && OrderMagicNumber()==MAGIC+2) 
              {
               if (fep1) fd=OrderDelete(OrderTicket());
              }
            if (fd && UseSound) PlaySound(NameFileSound);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Сопровождение позиции простым тралом                             |
//+------------------------------------------------------------------+
  void TrailingPositions() 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderMagicNumber()>MAGIC && OrderMagicNumber()<=MAGIC+2) 
           {
              if (OrderSymbol()==Symbol()) 
              {
                 if (OrderType()==OP_BUY) 
                 {
                    if (!ProfitTrailing || (Bid-OrderOpenPrice())>TrailingStop*Point) 
                    {
                       if (OrderStopLoss()<Bid-(TrailingStop+TrailingStep-1)*Point) 
                       {
                        ModifyStopLoss(Bid-TrailingStop*Point, clModifyBuy);
                       }
                    }
                 }
                 if (OrderType()==OP_SELL) 
                 {
                    if (!ProfitTrailing || OrderOpenPrice()-Ask>TrailingStop*Point) 
                    {
                       if (OrderStopLoss()>Ask+(TrailingStop+TrailingStep-1)*Point || OrderStopLoss()==0) 
                       {
                        ModifyStopLoss(Ask+TrailingStop*Point, clModifySell);
                       }
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Перенос уровня StopLoss                                          |
//| Параметры:                                                       |
//|   ldStopLoss - уровень StopLoss                                  |
//|   clModify   - цвет модификации                                  |
//+------------------------------------------------------------------+
  void ModifyStopLoss(double ldStop, color clModify) 
  {
   bool   fm;
   double ldOpen=OrderOpenPrice();
   double ldTake=OrderTakeProfit();
//----
   fm=OrderModify(OrderTicket(), ldOpen, ldStop, ldTake, 0, clModify);
   if (fm && UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+

