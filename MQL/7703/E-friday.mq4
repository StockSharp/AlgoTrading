//+------------------------------------------------------------------+
//|                                                     e-Friday.mq4 |
//|                                           Ким Игорь В. aka KimIV |
//|                                              http://www.kimiv.ru |
//|                                                                  |
//| 08.10.2005  Эффект пятницы                                       |
//+------------------------------------------------------------------+
#property copyright "Ким Игорь В. aka KimIV"
#property link      "http://www.kimiv.ru"
//-------
#define   MAGIC     20051008
//------- Внешние параметры советника --------------------------------
extern string _Parameters_Trade="----- Параметры торговли";
extern double Lots           =0.1;    // Размер торгуемого лота
extern int    StopLoss       =75;     // Размер фиксированного стопа
extern int    TakeProfit     =0;      // Размер фиксированного тэйка
extern int    HourOpenPos    =7;      // Время открытия позиции
extern bool   UseClosePos    =True;   // Использовать закрытие позиции
extern int    HourClosePos   =19;     // Время закрытия позиции
extern bool   UseTrailing    =True;   // Использовать трал
extern bool   ProfitTrailing =True;   // Тралить только профит
extern int    TrailingStop   =60;     // Фиксированный размер трала
extern int    TrailingStep   =5;      // Шаг трала
extern int    Slippage       =3;      // Проскальзывание цены
//----
extern string _Parameters_Expert="----- Параметры советника";
extern bool   UseOneAccount=False;        // Торговать только на одном счёте
extern int    NumberAccount=11111;        // Номер торгового счёта
extern string Name_Expert  ="e-Friday.mq4";
extern bool   UseSound     =True;         // Использовать звуковой сигнал
extern string NameFileSound="expert.wav"; // Наименование звукового файла
extern color  clOpenBuy    =LightBlue;    // Цвет открытия покупки
extern color  clOpenSell   =LightCoral;   // Цвет открытия продажи
extern color  clModifyBuy  =Aqua;         // Цвет модификации покупки
extern color  clModifySell =Tomato;       // Цвет модификации продажи
extern color  clCloseBuy   =Blue;         // Цвет закрытия покупки
extern color  clCloseSell  =Red;          // Цвет закрытия продажи
//---- Глобальные переменные советника -------------------------------
//------- Подключение внешних модулей --------------------------------
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
     if (UseOneAccount && AccountNumber()!=NumberAccount) 
     {
      if (!IsTesting()) Comment("Торговля на счёте: "+AccountNumber()+" ЗАПРЕЩЕНА!");
      return;
     }
      else if (!IsTesting()) Comment("");
      if (DayOfWeek()!=5 || Hour()<HourOpenPos || Hour()>HourClosePos) 
      {
      if (!IsTesting()) Comment("Время торговли ещё не наступило!");
      return;
     }
      else if (!IsTesting()) Comment("");
   if (Hour()==HourOpenPos) OpenPosition();
   if (Hour()>=HourClosePos && UseClosePos) CloseAllPositions();
   if (UseTrailing) TrailingPositions();
  }
//+------------------------------------------------------------------+
//| Установка ордеров                                                |
//+------------------------------------------------------------------+
  void OpenPosition() 
  {
   double ldStop=0, ldTake=0;
   double Op1=iOpen (NULL, PERIOD_D1, 1);
   double Cl1=iClose(NULL, PERIOD_D1, 1);
//----
     if (!ExistPosition()) 
     {
        if (Op1>Cl1) 
        {
         if (StopLoss!=0) ldStop=Ask-StopLoss*Point;
         if (TakeProfit!=0) ldTake=Ask+TakeProfit*Point;
         SetOrder(OP_BUY, Ask, ldStop, ldTake);
        }
        if (Op1<Cl1) 
        {
         if (StopLoss!=0) ldStop=Bid+StopLoss*Point;
         if (TakeProfit!=0) ldTake=Bid-TakeProfit*Point;
         SetOrder(OP_SELL, Bid, ldStop, ldTake);
        }
     }
  }
//+------------------------------------------------------------------+
//| Возвращает флаг существования позиции                            |
//+------------------------------------------------------------------+
  bool ExistPosition() 
  {
   bool Exist=False;
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
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
//+------------------------------------------------------------------+
  void SetOrder(int op, double pp, double ldStop, double ldTake) 
  {
   color  clOpen;
   string lsComm=GetCommentForOrder();
//----
   if (op==OP_BUYLIMIT || op==OP_BUYSTOP) clOpen=clOpenBuy;
   else clOpen=clOpenSell;
   //  Lots=MathCeil(AccountFreeMargin()/10000*10)/10;
   OrderSend(Symbol(),op,Lots,pp,Slippage,ldStop,ldTake,lsComm,MAGIC,0,clOpen);
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
//| Закрытие всех позиций по рыночной цене                           |
//+------------------------------------------------------------------+
  void CloseAllPositions() 
  {
   bool fc;
     for(int i=OrdersTotal()-1; i>=0; i--) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
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
//+------------------------------------------------------------------+
//| Сопровождение позиции простым тралом                             |
//+------------------------------------------------------------------+
  void TrailingPositions() 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
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

