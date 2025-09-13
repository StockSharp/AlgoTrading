//+----------------------------------------------------------------------------+
//|                                              ExpertCLOR_2MA&StATR_v01.mq4  |
//|                                                     Сергей Заикин as Vuki  |
//|                                                           f_kombi@mail.ru  |
//+----------------------------------------------------------------------------+
//|  Советник который:                                                         |
//|   1. Закрывает ордер по пересечению 2-х МА (5 и 7 по умолчанию);           |
//|   2. Автоматически двигает стоп по индикатору StopATR_auto;                |
//|   3. Переводит открытую позицию в безубыток при достижении заданного уровня|
//|                                                                            | 
//|   ВНИМАНИЕ!!! Советник только ЗАКРЫВАЕТ открытые ордера!!!                 |
//+----------------------------------------------------------------------------+
// Входные параметры:
// MA_CloseOnOff - включение (1) или выключение (0) режима закрытия ордера по пересечению МА  
// StATR_CloseOnOff - включение (1) или выключение (0) установки и изменения стопа по индикатору StopATR_auto
// MA_Fast_Pe - период быстрой МА
// MA_Fast_Ty - тип быстрой МА (0-SMA, 1-EMA, 2-SMMA(сглаженная), 3-LWMA(взвешенная))
// MA_Fast_Pr - цена быстрой МА (0-Close, 1-Open, 2-High, 3-Low, 4-HL/2, 5-HLC/3, 6-HLCC/4)
// MA_Slow_Pe - период медленной МА
// MA_Slow_Ty - тип медленной МА (0-SMA, 1-EMA, 2-SMMA(сглаженная), 3-LWMA(взвешенная))
// MA_Slow_Pr - цена медленной МА (0-Close, 1-Open, 2-High, 3-Low, 4-HL/2, 5-HLC/3, 6-HLCC/4)
// TimeFrame  - рабочий таймфрейм (1-М1, 5-М5, 15-М15б 60-Н1, 240-Н4)
// BezUb - уровень профита в пунктах, при котором ордер переводится в безубыток
// CountBarsForShift - параметр ин-ра StopATR_auto - расстояние в барах для вывода уровня стопа на экран  
// CountBarsForAverage - параметр ин-ра StopATR_auto - количество баров усреднения для рассчета стопа
// Target - параметр ин-ра StopATR_auto - кэффициент увеличения значения среднего бара для рассчета стопа
//-----
//  Индикатор StopATR_auto ОБЯЗАТЕЛЬНО должен быть в папке Indicators вашего МТ4
//  Индикатор StopATR_auto на график можно не вешать, его советник сам повесит
//-----
#property copyright "Сергей Заикин as Vuki"
#property link      "f_kombi@mail.ru"
//-----
  extern int      MA_CloseOnOff      =1;
  extern int      StATR_CloseOnOff   =1;
  extern int      MA_Fast_Pe         =5;
  extern int      MA_Fast_Ty         =MODE_EMA;
  extern int      MA_Fast_Pr         =PRICE_CLOSE;
  extern int      MA_Slow_Pe         =7;
  extern int      MA_Slow_Ty         =MODE_EMA;
  extern int      MA_Slow_Pr         =PRICE_OPEN;
  extern int      TimeFrame          =PERIOD_M5;
  extern int      BezUb              =15;
  extern int      CountBarsForShift  =7;
  extern int      CountBarsForAverage=12;
  extern double   Target             =2.0;
//----
  int Opened=0;
  int OpenedBuy=0;
  int   OpenedSell=0;
  int OpenedBuyTicket=0;
  int   OpenedSellTicket=0;
  int CloseBuy=0;
  int CloseSell=0;
  int Closed=0;
  double StopL;
//expert initialization function
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int init() 
  {
   return(0);
  }
//expert deinitialization function
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int deinit() 
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
   int cnt;
   bool errbool=true;
   int errint=0;
//----
   CloseBuy=0; CloseSell=0;
   // проверка, есть ли открытые рыночные или отложенные ордера
   OpenedBuy=0;
   OpenedSell=0;
     for(cnt=0; cnt < OrdersTotal(); cnt++) 
     {
      errbool=OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(errbool!=true) Comment("Ошибка выбора ордера по позиции!","\n","\n");
      if(OrderType()==OP_BUY && OrderSymbol()==Symbol())       {OpenedBuy=1; OpenedBuyTicket=OrderTicket();}
      if(OrderType()==OP_SELL && OrderSymbol()==Symbol())      {OpenedSell=1; OpenedSellTicket=OrderTicket();}
     }
   //если есть рыночный бай, то изменяем стоплосс в бу и по стопатр
     if(OpenedBuy==1) 
     {
      StopL=iCustom(NULL,TimeFrame,"StopATR_auto",Blue,Brown,false,CountBarsForShift,CountBarsForAverage,Target,2,0);
      StopL=NormalizeDouble(StopL,Digits);
      errbool=OrderSelect(OpenedBuyTicket, SELECT_BY_TICKET, MODE_TRADES);
      if(errbool!=true) Comment("Ошибка выбора ордера по позиции!","\n","\n");
      // Перевод в безубыток ордера Бай      
        if(BezUb!=0 && (Bid - OrderOpenPrice())>=(BezUb*Point) && OrderStopLoss() < OrderOpenPrice()) 
        {
         errbool=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point,OrderTakeProfit(),0,White);
         errint=GetLastError();
         if(errbool!=true&&errint>1) Comment(errint," Ошибка модификации текущего ордера Бай по безубытку!","\n","\n");
         else Comment("Сработал перевод текущего ордера Бай в безубыток!\n");
         return(0);
        }
      // Модификация стопа по СтопАТР ордера Бай
        if(OrderStopLoss() < StopL && StATR_CloseOnOff==1) 
        {
         errbool=OrderModify(OrderTicket(),OrderOpenPrice(),StopL,OrderTakeProfit(),0,White);
         errint=GetLastError();
         if(errbool!=true&&errint>1) Comment(errint," Ошибка модификации текущего ордера Бай по СтопАТР!","\n","\n");
         return(0);
        }
     }
   //если есть рыночный селл, то изменяем стоплосс в бу и по стопатр
     if(OpenedSell==1) 
     {
      StopL=iCustom(NULL,TimeFrame,"StopATR_auto",Blue,Brown,false,CountBarsForShift,CountBarsForAverage,Target,1,0);
      StopL=NormalizeDouble(StopL,Digits);
      errbool=OrderSelect(OpenedSellTicket, SELECT_BY_TICKET, MODE_TRADES);
      if(errbool!=true) Comment("Ошибка выбора ордера по позиции!","\n","\n");
      // Перевод в безубыток ордера Селл      
        if(BezUb!=0 && (OrderOpenPrice() - Ask)>=(BezUb*Point) && OrderStopLoss() > OrderOpenPrice()) 
        {
         errbool=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point,OrderTakeProfit(),0,White);
         errint=GetLastError();
         if(errbool!=true&&errint>1) Comment(errint," Ошибка модификации текущего ордера Селл по безубытку!","\n","\n");
         else Comment("Сработал перевод текущего ордера Селл в безубыток!\n");
         return(0);
        }
      // Модификация стопа по СтопАТР ордера Селл
        if(StATR_CloseOnOff==1) 
        {
         //        Comment("здесь работает! StopL = ",StopL,"\n");
           if(OrderStopLoss()==0 || OrderStopLoss() > StopL) {
            errbool=OrderModify(OrderTicket(),OrderOpenPrice(),StopL,OrderTakeProfit(),0,White);
            errint=GetLastError();
            if(errbool!=true&&errint>1) Comment(errint," Ошибка модификации текущего ордера Селл по СтопАТР!","\n","\n");
            return(0);
           }
        }
     }
   if(MA_CloseOnOff==1) FunctionSignalClose();
   // Если есть сигнал на закрытие, закрываем
     if((OpenedBuy==1&&OpenedSell==0&&CloseBuy==1&&CloseSell==0)||(OpenedBuy==0&&OpenedSell==1&&CloseBuy==0&&CloseSell==1)) 
     {
        if(OpenedBuy==1) 
        {
         errbool=OrderSelect(OpenedBuyTicket, SELECT_BY_TICKET, MODE_TRADES);
         if(errbool!=true) Comment("Ошибка выбора ордера по позиции!","\n","\n");
        }
        if(OpenedSell==1) 
        {
         errbool=OrderSelect(OpenedSellTicket, SELECT_BY_TICKET, MODE_TRADES);
         if(errbool!=true) Comment("Ошибка выбора ордера по позиции!","\n","\n");
        }
      Closed=FunctionCloseTrade(OrderTicket(),OrderType(),OrderLots());
      if(Closed==1) OpenedBuy=0;
      if(Closed==2) OpenedSell=0;
      return(0);
     }
   return(0);
  }
// Функция рассчета сигналов Закрыть Бай, Закрыть Селл
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void FunctionSignalClose() 
  {
   double maFast1,maSlow1,maFast2,maSlow2;
   maFast1=iMA(NULL,TimeFrame,MA_Fast_Pe,0,MA_Fast_Ty,MA_Fast_Pr,1);
   maSlow1=iMA(NULL,TimeFrame,MA_Slow_Pe,0,MA_Slow_Ty,MA_Slow_Pr,1);
   maFast2=iMA(NULL,TimeFrame,MA_Fast_Pe,0,MA_Fast_Ty,MA_Fast_Pr,2);
   maSlow2=iMA(NULL,TimeFrame,MA_Slow_Pe,0,MA_Slow_Ty,MA_Slow_Pr,2);
     if(OpenedBuy==1) 
     {
      if(maFast1<=maSlow1 && maFast2 > maSlow2) {CloseBuy=1; CloseSell=0;}
     }
     if(OpenedSell==1) 
     {
      if(maFast1>=maSlow1 && maFast2 < maSlow2) {CloseSell=1; CloseBuy=0;}
     }
   return;
  }
// Функция закрытия позиций Бай или Селл. Возвращает 1 если закрыл Бай, 2 если закрыл Селл, 0 если ошибка
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int FunctionCloseTrade(int Tic, int Type, double Lot) 
  {
   int Ret;
   double PrCl;
   color ArCo;
   bool errbool;
//----
     if(Type==OP_BUY) 
     {
      PrCl=Bid;
      ArCo=Aqua;
      Ret=1;
     }
     if(Type==OP_SELL) 
     {
      PrCl=Ask;
      ArCo=Aqua;
      Ret=2;
     }
   errbool=OrderClose(Tic,Lot,PrCl,10,ArCo);
     if(errbool!=true) 
     {
      Comment("Ошибка ",GetLastError()," закрытия рыночного ордера!","\n","\n");
      return(0);
     }
   return(Ret);
  }
//+------------------------------------------------------------------+