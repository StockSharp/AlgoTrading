//+------------------------------------------------------------------+
//|                                              TradeAlgorithms.mqh |
//|                               Copyright © 2013, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//| Торговые алгоритмы для брокеров предлагающих не нулевой спред!   |
//+------------------------------------------------------------------+ 
#property copyright "2013,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.21"
//+------------------------------------------------------------------+
//|  Перечисление для вариантов расчёта лота                         |
//+------------------------------------------------------------------+
enum MarginMode //Тип константы для переменной Margin_Mode торговых функций
  {
   FREEMARGIN=0,     //MM от свободных средств на счёте
   BALANCE,          //MM от баланса средств на счёте
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счёте
   LOSSBALANCE,      //MM по убыткам от баланса средств на счёте
   LOT               //Лот без изменения
  };
//+------------------------------------------------------------------+
//|  Алгоритм определения момента появления нового бара              |
//+------------------------------------------------------------------+  
class CIsNewBar
  {
   //----
public:
   //---- функция определения момента появления нового бара
   bool IsNewBar(string symbol,ENUM_TIMEFRAMES timeframe)
     {
      //---- получим время появления текущего бара
      datetime TNew=datetime(SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE));

      if(TNew!=m_TOld && TNew) // проверка на появление нового бара
        {
         m_TOld=TNew;
         return(true); // появился новый бар!
        }
      //----
      return(false); // новых баров пока нет!
     };

   //---- конструктор класса    
                     CIsNewBar(){m_TOld=-1;};

protected: datetime m_TOld;
   //---- 
  };
//+==================================================================+
//| Алгоритмы для торговых операций                                  |
//+==================================================================+

//+------------------------------------------------------------------+
//| Открываем длинную позицию                                        |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // флаг разрешения на сделку
 const string symbol,        // торговая пара сделки
 const datetime &TimeLevel,  // время, после которого будет осуществлена следущая сделка после текущей
 double Money_Management,    // MM
 int Margin_Mode,            // способ расчёта величины лота
 uint deviation,             // слиппаж
 int StopLoss,               // стоплосс в пунктах
 int Takeprofit              // тейкпрофит в пунктах
 )
//BuyPositionOpen(BUY_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!BUY_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;
//---- Проверка на истечение временного лимита для предыдущей сделки и полноты объёма
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Проверка на на наличие открытой позиции
   if(PositionSelect(symbol)) return(true);

//----
   double volume=BuyLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Неверный объём для структуры торгового запроса");
      return(false);
     }

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----   
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- Инициализация структуры торгового запроса MqlTradeRequest для открывания BUY позиции
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- Определение расстояния до стоплосса в единицах ценового графика
   if(StopLoss)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,int(digit));
     }
   else request.sl=0.0;

//---- Определение расстояния до тейкпрофита единицах ценового графика
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,int(digit));
     }
   else request.tp=0.0;

//----
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Открываем Buy позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Открываем BUY позицию и делаем проверку результата торгового запроса
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      BUY_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy позиция по ",symbol," открыта ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Открываем короткую позицию                                       |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // флаг разрешения на сделку
 const string symbol,        // торговая пара сделки
 const datetime &TimeLevel,  // время, после которого будет осуществлена следущая сделка после текущей
 double Money_Management,    // MM
 int Margin_Mode,            // способ расчёта величины лота
 uint deviation,             // слиппаж
 int StopLoss,               // стоплосс в пунктах
 int Takeprofit              // тейкпрофит в пунктах
 )
//SellPositionOpen(SELL_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!SELL_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;
//---- Проверка на истечение временного лимита для предыдущей сделки и полноты объёма
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Проверка на на наличие открытой позиции
   if(PositionSelect(symbol)) return(true);

//----
   double volume=SellLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Неверный объём для структуры торгового запроса");
      return(false);
     }

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- Инициализация структуры торгового запроса MqlTradeRequest для открывания SELL позиции
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- Определение расстояния до стоплосса в единицах ценового графика
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,int(digit));
     }
   else request.sl=0.0;

//---- Определение расстояния до тейкпрофита единицах ценового графика
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,int(digit));
     }
   else request.tp=0.0;
//----
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Открываем Sell позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Открываем SELL позицию и делаем проверку результата торгового запроса
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      SELL_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell позиция по ",symbol," открыта ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Открываем длинную позицию                                        |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // флаг разрешения на сделку
 const string symbol,        // торговая пара сделки
 const datetime &TimeLevel,  // время, после которого будет осуществлена следущая сделка после текущей
 double Money_Management,    // MM
 int Margin_Mode,            // способ расчёта величины лота
 uint deviation,             // слиппаж
 double dStopLoss,           // стоплосс в единицах ценового графика
 double dTakeprofit          // тейкпрофит в единицах ценового графика
 )
//BuyPositionOpen(BUY_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!BUY_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;
//---- Проверка на истечение временного лимита для предыдущей сделки и полноты объёма
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Проверка на на наличие открытой позиции
   if(PositionSelect(symbol)) return(true);

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- коррекция расстояний до стоплосса и тейкпрофита в единицах ценового графика
   if(!dStopCorrect(symbol,dStopLoss,dTakeprofit,PosType)) return(false);
   int StopLoss=int((Ask-dStopLoss)/point);
//----
   double volume=BuyLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Неверный объём для структуры торгового запроса");
      return(false);
     }

//---- Инициализация структуры торгового запроса MqlTradeRequest для открывания BUY позиции
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl=dStopLoss;
   request.tp=dTakeprofit;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Открываем Buy позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Открываем BUY позицию и делаем проверку результата торгового запроса
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      BUY_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy позиция по ",symbol," открыта ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Открываем короткую позицию                                       |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // флаг разрешения на сделку
 const string symbol,        // торговая пара сделки
 const datetime &TimeLevel,  // время, после которого будет осуществлена следущая сделка после текущей
 double Money_Management,    // MM
 int Margin_Mode,            // способ расчёта величины лота
 uint deviation,             // слиппаж
 double dStopLoss,           // стоплосс в единицах ценового графика
 double dTakeprofit          // тейкпрофит в единицах ценового графика
 )
//SellPositionOpen(SELL_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!SELL_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;
//---- Проверка на истечение временного лимита для предыдущей сделки и полноты объёма
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Проверка на на наличие открытой позиции
   if(PositionSelect(symbol)) return(true);

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- коррекция расстояний до стоплосса и тейкпрофита в единицах ценового графика
   if(!dStopCorrect(symbol,dStopLoss,dTakeprofit,PosType)) return(false);
   int StopLoss=int((dStopLoss-Bid)/point);
//----
   double volume=SellLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Неверный объём для структуры торгового запроса");
      return(false);
     }

//---- Инициализация структуры торгового запроса MqlTradeRequest для открывания SELL позиции
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): OrderCheck(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Открываем Sell позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Открываем SELL позицию и делаем проверку результата торгового запроса
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): OrderSend(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      SELL_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell позиция по ",symbol," открыта ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): OrderSend(): Невозможно совершить сделку!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Закрываем длинную позицию                                        |
//+------------------------------------------------------------------+
bool BuyPositionClose
(
 bool &Signal,         // флаг разрешения на сделку
 const string symbol,  // торговая пара сделки
 uint deviation        // слиппаж
 )
  {
//----
   if(!Signal) return(true);

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

//---- Проверка на наличие открытой BUY позиции
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_BUY) return(false);
     }
   else return(false);

   double MaxLot,volume,Bid;
//---- получение данных для расчёта    
   if(!PositionGetDouble(POSITION_VOLUME,volume)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- проверка лота на максимальное допустимое значение       
   if(volume>MaxLot) volume=MaxLot;

//---- Инициализация структуры торгового запроса MqlTradeRequest для закрывания BUY позиции
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }
//----     
   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Закрываем Buy позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Отправка приказа на закрывание позиции на торговый сервер
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Невозможно закрыть позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy позиция по ",symbol," закрыта ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Невозможно закрыть позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Закрываем короткую позицию                                       |
//+------------------------------------------------------------------+
bool SellPositionClose
(
 bool &Signal,         // флаг разрешения на сделку
 const string symbol,  // торговая пара сделки
 uint deviation        // слиппаж
 )
  {
//----
   if(!Signal) return(true);

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

//---- Проверка на наличие открытой SELL позиции
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_SELL)return(false);
     }
   else return(false);

   double MaxLot,volume,Ask;
//---- получение данных для расчёта    
   if(!PositionGetDouble(POSITION_VOLUME,volume)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- проверка лота на максимальное допустимое значение       
   if(volume>MaxLot) volume=MaxLot;

//---- Инициализация структуры торгового запроса MqlTradeRequest для закрывания SELL позиции
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }
//----    
   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Закрываем Sell позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Отправка приказа на закрывание позиции на торговый сервер
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Невозможно закрыть позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Signal=false;
     }
   else
     {
      Print(__FUNCTION__,"(): Невозможно закрыть позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell позиция по ",symbol," закрыта ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Модифицируем длинную позицию                                     |
//+------------------------------------------------------------------+
bool BuyPositionModify
(
 bool &Modify_Signal,        // флаг разрешения модификации
 const string symbol,        // торговая пара сделки
 uint deviation,             // слиппаж
 int StopLoss,               // стоплосс в пунктах
 int Takeprofit              // тейкпрофит в пунктах
 )
//BuyPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;

//---- Проверка на на наличие открытой позиции
   if(!PositionSelect(symbol)) return(true);

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;

//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- Инициализация структуры торгового запроса MqlTradeRequest для открывания BUY позиции
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- Определение расстояния до стоплосса в единицах ценового графика
   if(StopLoss)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,int(digit));
      if(request.sl<PositionGetDouble(POSITION_SL)) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Определение расстояния до тейкпрофита единицах ценового графика
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,int(digit));
      if(request.tp<PositionGetDouble(POSITION_TP)) request.tp=PositionGetDouble(POSITION_TP);
     }
   else request.tp=PositionGetDouble(POSITION_TP);

//----   
   if(request.tp==PositionGetDouble(POSITION_TP) && request.sl==PositionGetDouble(POSITION_SL)) return(true);
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Модифицируем Buy позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Модифицируем BUY позицию и делаем проверку результата торгового запроса
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Невозможно модифицировать позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Modify_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy позиция по ",symbol," модифицирована ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Невозможно модифицировать позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Модифицируем короткую позицию                                    |
//+------------------------------------------------------------------+
bool SellPositionModify
(
 bool &Modify_Signal,        // флаг разрешения модификации
 const string symbol,        // торговая пара сделки
 uint deviation,             // слиппаж
 int StopLoss,               // стоплосс в пунктах
 int Takeprofit              // тейкпрофит в пунктах
 )
//SellPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;

//---- Проверка на на наличие открытой позиции
   if(!PositionSelect(symbol)) return(true);

//---- Объявление структур торгового запроса и результата торгового запроса
   MqlTradeRequest request;
   MqlTradeResult result;

//---- Объявление структуры результата проверки торгового запроса 
   MqlTradeCheckResult check;

//---- обнуление структур
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);
//----
   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- Инициализация структуры торгового запроса MqlTradeRequest для открывания BUY позиции
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- Определение расстояния до стоплосса в единицах ценового графика
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,int(digit));
      double laststop=PositionGetDouble(POSITION_SL);
      if(request.sl>laststop && laststop) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Определение расстояния до тейкпрофита единицах ценового графика
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,int(digit));
      double lasttake=PositionGetDouble(POSITION_TP);
      if(request.tp>lasttake && lasttake) request.tp=PositionGetDouble(POSITION_TP);
     }
   else request.tp=PositionGetDouble(POSITION_TP);

//----   
   if(request.tp==PositionGetDouble(POSITION_TP) && request.sl==PositionGetDouble(POSITION_SL)) return(true);
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Проверка торгового запроса на корректность
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Неверные данные для структуры торгового запроса!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Модифицируем Sell позицию по ",symbol," ============ >>>");
   Print(comment);

//---- Модифицируем SELL позицию и делаем проверку результата торгового запроса
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Невозможно модифицировать позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Modify_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell позиция по ",symbol," модифицирована ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Невозможно модифицировать позицию!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| GetTimeLevelName() function                                      |
//+------------------------------------------------------------------+
string GetTimeLevelName(string symbol,ENUM_POSITION_TYPE trade_operation)
  {
//----
   string G_Name_;
//----  
   if(MQL5InfoInteger(MQL5_TESTING)
      || MQL5InfoInteger(MQL5_OPTIMIZATION)
      || MQL5InfoInteger(MQL5_DEBUGGING))
      StringConcatenate(G_Name_,"TimeLevel_",AccountInfoInteger(ACCOUNT_LOGIN),"_",symbol,"_",trade_operation,"_Test_");
   else StringConcatenate(G_Name_,"TimeLevel_",AccountInfoInteger(ACCOUNT_LOGIN),"_",symbol,"_",trade_operation);
//----
   return(G_Name_);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelCheck() function                                   |
//+------------------------------------------------------------------+
bool TradeTimeLevelCheck
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation,
 datetime TradeTimeLevel
 )
  {
//----
   if(TradeTimeLevel>0)
     {
      //---- Проверка на истечение временного лимита для предыдущей сделки 
      if(TimeCurrent()<GlobalVariableGet(GetTimeLevelName(symbol,trade_operation))) return(false);
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelSet() function                                     |
//+------------------------------------------------------------------+
void TradeTimeLevelSet
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation,
 datetime TradeTimeLevel
 )
  {
//----
   GlobalVariableSet(GetTimeLevelName(symbol,trade_operation),TradeTimeLevel);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelSet() function                                     |
//+------------------------------------------------------------------+
datetime TradeTimeLevelGet
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation
 )
  {
//----
   return(datetime(GlobalVariableGet(GetTimeLevelName(symbol,trade_operation))));
  }
//+------------------------------------------------------------------+
//| TimeLevelGlobalVariableDel() function                            |
//+------------------------------------------------------------------+
void TimeLevelGlobalVariableDel
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation
 )
  {
//----
   if(MQL5InfoInteger(MQL5_TESTING)
      || MQL5InfoInteger(MQL5_OPTIMIZATION)
      || MQL5InfoInteger(MQL5_DEBUGGING))
      GlobalVariableDel(GetTimeLevelName(symbol,trade_operation));
//----
  }
//+------------------------------------------------------------------+
//| GlobalVariableDel_() function                                    |
//+------------------------------------------------------------------+
void GlobalVariableDel_(string symbol)
  {
//----
   TimeLevelGlobalVariableDel(symbol,POSITION_TYPE_BUY);
   TimeLevelGlobalVariableDel(symbol,POSITION_TYPE_SELL);
//----
  }
//+------------------------------------------------------------------+
//| Расчёт размера лота для открывания лонга                         |  
//+------------------------------------------------------------------+
/*                                                                   |
 Внешняя  переменная Margin_Mode определяет способ расчёта  величины | 
 лота                                                                |
 0 - MM по свободным средствам на счёте                              |
 1 - MM по балансу средств на счёте                                  |
 2 - MM по убыткам от свободных средств на счёте                     |
 3 - MM по убыткам от баланса средств на счёте                       |
 по умолчанию - MM по свободным средствам на счёте                   |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 если Money_Management меньше нуля,  то торговая  функция в качестве | 
 величины  лота  использует  округлённую  до ближайшего стандартного |
 значения абсолютную величину Money_Management.                      |
*///                                                                 |
//+------------------------------------------------------------------+
double BuyLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 uint Slippage_
 )
// BuyLotCount_(string symbol, double Money_Management, int Margin_Mode, int STOPLOSS,Slippage_)
  {
//----
   double margin,Lot;

//---1+ РАСЧЁТ ВЕЛИЧИНЫ ЛОТА ДЛЯ ОТКРЫВАНИЯ ПОЗИЦИИ
   if(Money_Management<0) Lot=MathAbs(Money_Management);
   else
   switch(Margin_Mode)
     {
      //---- Расчёт лота от свободных средств на счёте
      case  0:
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
         break;

         //---- Расчёт лота от баланса средств на счёте
      case  1:
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
         break;

         //---- Расчёт лота по убыткам от свободных средств на счёте             
      case  2:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Неверный стоплосс!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price_open)) return(-1);

         //---- Определение расстояния до стоплосса в единицах ценового графика
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open-STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_BUY,symbol,1,price_open,price_close,profit)) return(-1);
         if(!profit) return(-1);

         //---- Расчёт потерь от свободных средств на счёте
         double Loss=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- Расчёт лота по убыткам от баланса средств на счёте
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Неверный стоплосс!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price_open)) return(-1);

         //---- Определение расстояния до стоплосса в единицах ценового графика
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open-STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_BUY,symbol,1,price_open,price_close,profit)) return(-1);

         if(!profit) return(-1);

         //---- Расчёт потерь от баланса средств на счёте
         double Loss=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- Расчёт лота без изменения
      case  4:
        {
         Lot=MathAbs(Money_Management);
         break;
        }

      //---- Расчёт лота от свободных средств на счёте по умолчанию
      default:
        {
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
        }
     }
//---1+    

//---- нормирование величины лота до ближайшего стандартного значения 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_BUY)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| Расчёт размера лота для открывания шорта                         |  
//+------------------------------------------------------------------+
/*                                                                   |
 Внешняя  переменная Margin_Mode определяет способ расчёта  величины | 
 лота                                                                |
 0 - MM по свободным средствам на счёте                              |
 1 - MM по балансу средств на счёте                                  |
 2 - MM по убыткам от свободных средств на счёте                     |
 3 - MM по убыткам от баланса средств на счёте                       |
 по умолчанию - MM по свободным средствам на счёте                   |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 если Money_Management меньше нуля,  то торговая  функция в качестве | 
 величины  лота  использует  округлённую  до ближайшего стандартного |
 значения абсолютную величину Money_Management.                      |
*///                                                                 |
//+------------------------------------------------------------------+
double SellLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 uint Slippage_
 )
// (string symbol, double Money_Management, int Margin_Mode, int STOPLOSS)
  {
//----
   double margin,Lot;

//---1+ РАСЧЁТ ВЕЛИЧИНЫ ЛОТА ДЛЯ ОТКРЫВАНИЯ ПОЗИЦИИ
   if(Money_Management<0) Lot=MathAbs(Money_Management);
   else
   switch(Margin_Mode)
     {
      //---- Расчёт лота от свободных средств на счёте
      case  0:
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
         break;

         //---- Расчёт лота от баланса средств на счёте
      case  1:
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
         break;

         //---- Расчёт лота по убыткам от свободных средств на счёте             
      case  2:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Неверный стоплосс!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_BID,price_open)) return(-1);

         //---- Определение расстояния до стоплосса в единицах ценового графика
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open+
                                            STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_SELL,symbol,1,price_open,price_close,profit)) return(-1);

         if(!profit) return(-1);

         //---- Расчёт потерь от свободных средств на счёте
         double Loss=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- Расчёт лота по убыткам от баланса средств на счёте
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Неверный стоплосс!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_BID,price_open)) return(-1);

         //---- Определение расстояния до стоплосса в единицах ценового графика
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open+STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_SELL,symbol,1,price_open,price_close,profit)) return(-1);
         if(!profit) return(-1);

         //---- Расчёт потерь от баланса средств на счёте
         double Loss=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- Расчёт лота без изменения
      case  4:
        {
         Lot=MathAbs(Money_Management);
         break;
        }

      //---- Расчёт лота от свободных средств на счёте по умолчанию
      default:
        {
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
        }
     }
//---1+ 

//---- нормирование величины лота до ближайшего стандартного значения 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_SELL)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| коррекция размера отложенного ордера до допустимого значения     |
//+------------------------------------------------------------------+
bool StopCorrect(string symbol,int &Stop)
  {
//----
   long Extrem_Stop;
   if(!SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL,Extrem_Stop)) return(false);
   if(Stop<Extrem_Stop) Stop=int(Extrem_Stop);
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| коррекция размера отложенного ордера до допустимого значения     |
//+------------------------------------------------------------------+
bool dStopCorrect
(
 string symbol,
 double &dStopLoss,
 double &dTakeprofit,
 ENUM_POSITION_TYPE trade_operation
 )
// dStopCorrect(symbol,dStopLoss,dTakeprofit,trade_operation)
  {
//----
   if(!dStopLoss && !dTakeprofit) return(true);

   if(dStopLoss<0)
     {
      Print(__FUNCTION__,"(): Отрицательное значение стоплосса!");
      return(false);
     }

   if(dTakeprofit<0)
     {
      Print(__FUNCTION__,"(): Отрицательное значение тейкпрофита!");
      return(false);
     }
//---- 
   int Stop;
   long digit;
   double point,dStop,ExtrStop,ExtrTake;

//---- получаем минимальное расстояние до отложенного ордера 
   Stop=0;
   if(!StopCorrect(symbol,Stop))return(false);
//----   
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(false);
   dStop=Stop*point;

//---- коррекция размера отложенного ордера для лонга
   if(trade_operation==POSITION_TYPE_BUY)
     {
      double Ask;
      if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(false);

      ExtrStop=NormalizeDouble(Ask-dStop,int(digit));
      ExtrTake=NormalizeDouble(Ask+dStop,int(digit));

      if(dStopLoss>ExtrStop && dStopLoss) dStopLoss=ExtrStop;
      if(dTakeprofit<ExtrTake && dTakeprofit) dTakeprofit=ExtrTake;
     }

//---- коррекция размера отложенного ордера для шорта
   if(trade_operation==POSITION_TYPE_SELL)
     {
      double Bid;
      if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(false);

      ExtrStop=NormalizeDouble(Bid+dStop,int(digit));
      ExtrTake=NormalizeDouble(Bid-dStop,int(digit));

      if(dStopLoss<ExtrStop && dStopLoss) dStopLoss=ExtrStop;
      if(dTakeprofit>ExtrTake && dTakeprofit) dTakeprofit=ExtrTake;
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| коррекция размера лота до ближайшего допустимого значения        |
//+------------------------------------------------------------------+
bool LotCorrect
(
 string symbol,
 double &Lot,
 ENUM_POSITION_TYPE trade_operation
 )
//LotCorrect(string symbol, double& Lot, ENUM_POSITION_TYPE trade_operation)
  {
//---- получение данных для расчёта   
   double Step,MaxLot,MinLot;
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,Step)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(false);

//---- нормирование величины лота до ближайшего стандартного значения 
   Lot=Step*MathFloor(Lot/Step);

//---- проверка лота на минимальное допустимое значение
   if(Lot<MinLot) Lot=MinLot;
//---- проверка лота на максимальное допустимое значение       
   if(Lot>MaxLot) Lot=MaxLot;

//---- проверка средств на достаточность
   if(!LotFreeMarginCorrect(symbol,Lot,trade_operation))return(false);
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ограничение размера лота возможностями депозита                  |
//+------------------------------------------------------------------+
bool LotFreeMarginCorrect
(
 string symbol,
 double &Lot,
 ENUM_POSITION_TYPE trade_operation
 )
//(string symbol, double& Lot, ENUM_POSITION_TYPE trade_operation)
  {
//---- проверка средств на достаточность
   double freemargin=AccountInfoDouble(ACCOUNT_FREEMARGIN);
   if(freemargin<=0) return(false);

//---- получение данных для расчёта   
   double Step,MaxLot,MinLot;
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,Step)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(false);

   double ExtremLot=GetLotForOpeningPos(symbol,trade_operation,freemargin);
//---- нормирование величины лота до ближайшего стандартного значения 
   ExtremLot=Step*MathFloor(ExtremLot/Step);

   if(ExtremLot<MinLot) return(false); // недостаточно денег даже на минимальный лот!
   if(Lot>ExtremLot) Lot=ExtremLot; // урезаем размер лота до того, что есть на депозите!
   if(Lot>MaxLot) Lot=MaxLot; // урезаем размер лота до масимально допустимого
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| расчёт размер лота для открывания позиции с маржой lot_margin    |
//+------------------------------------------------------------------+
double GetLotForOpeningPos(string symbol,ENUM_POSITION_TYPE direction,double lot_margin)
  {
//----
   double price=0.0,n_margin;
   if(direction==POSITION_TYPE_BUY)  if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price)) return(0);
   if(direction==POSITION_TYPE_SELL) if(!SymbolInfoDouble(symbol,SYMBOL_BID,price)) return(0);
   if(!price) return(NULL);

   if(!OrderCalcMargin(ENUM_ORDER_TYPE(direction),symbol,1,price,n_margin) || !n_margin) return(0);
   double lot=lot_margin/n_margin;

//---- получение торговых констант
   double LOTSTEP,MaxLot,MinLot;
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,LOTSTEP)) return(0);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(0);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(0);

//---- нормирование величины лота до ближайшего стандартного значения 
   lot=LOTSTEP*MathFloor(lot/LOTSTEP);

//---- проверка лота на минимальное допустимое значение
   if(lot<MinLot) lot=0;
//---- проверка лота на максимальное допустимое значение       
   if(lot>MaxLot) lot=MaxLot;
//----
   return(lot);
  }
//+------------------------------------------------------------------+
//| возврат символа с заданными валютами залога и котировки          |
//+------------------------------------------------------------------+
string GetSymbolByCurrencies(string margin_currency,string profit_currency)
  {
//---- переберем в цикле все символы, которые представлены в окне "Обзор рынка"
   int total=SymbolsTotal(true);
   for(int numb=0; numb<total; numb++)
     {
      //---- получим имя символа по номеру в списке "Обзор рынка"
      string symbolname=SymbolName(numb,true);

      //---- получим валюту залога
      string m_cur=SymbolInfoString(symbolname,SYMBOL_CURRENCY_MARGIN);

      //---- получим валюту котировки (в чем измеряется прибыль при изменении цены)
      string p_cur=SymbolInfoString(symbolname,SYMBOL_CURRENCY_PROFIT);

      //---- если символ подошел по обеим заданным валютам, вернем  имя символа
      if(m_cur==margin_currency && p_cur==profit_currency) return(symbolname);
     }
//----    
   return(NULL);
  }
//+------------------------------------------------------------------+
//| возврат стрингового результата торговой операции по его коду     |
//+------------------------------------------------------------------+
string ResultRetcodeDescription(int retcode)
  {
   string str;
//----
   switch(retcode)
     {
      case TRADE_RETCODE_REQUOTE: str="Реквота"; break;
      case TRADE_RETCODE_REJECT: str="Запрос отвергнут"; break;
      case TRADE_RETCODE_CANCEL: str="Запрос отменен трейдером"; break;
      case TRADE_RETCODE_PLACED: str="Ордер размещен"; break;
      case TRADE_RETCODE_DONE: str="Заявка выполнена"; break;
      case TRADE_RETCODE_DONE_PARTIAL: str="Заявка выполнена частично"; break;
      case TRADE_RETCODE_ERROR: str="Ошибка обработки запроса"; break;
      case TRADE_RETCODE_TIMEOUT: str="Запрос отменен по истечению времени";break;
      case TRADE_RETCODE_INVALID: str="Неправильный запрос"; break;
      case TRADE_RETCODE_INVALID_VOLUME: str="Неправильный объем в запросе"; break;
      case TRADE_RETCODE_INVALID_PRICE: str="Неправильная цена в запросе"; break;
      case TRADE_RETCODE_INVALID_STOPS: str="Неправильные стопы в запросе"; break;
      case TRADE_RETCODE_TRADE_DISABLED: str="Торговля запрещена"; break;
      case TRADE_RETCODE_MARKET_CLOSED: str="Рынок закрыт"; break;
      case TRADE_RETCODE_NO_MONEY: str="Нет достаточных денежных средств для выполнения запроса"; break;
      case TRADE_RETCODE_PRICE_CHANGED: str="Цены изменились"; break;
      case TRADE_RETCODE_PRICE_OFF: str="Отсутствуют котировки для обработки запроса"; break;
      case TRADE_RETCODE_INVALID_EXPIRATION: str="Неверная дата истечения ордера в запросе"; break;
      case TRADE_RETCODE_ORDER_CHANGED: str="Состояние ордера изменилось"; break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS: str="Слишком частые запросы"; break;
      case TRADE_RETCODE_NO_CHANGES: str="В запросе нет изменений"; break;
      case TRADE_RETCODE_SERVER_DISABLES_AT: str="Автотрейдинг запрещен сервером"; break;
      case TRADE_RETCODE_CLIENT_DISABLES_AT: str="Автотрейдинг запрещен клиентским терминалом"; break;
      case TRADE_RETCODE_LOCKED: str="Запрос заблокирован для обработки"; break;
      case TRADE_RETCODE_FROZEN: str="Ордер или позиция заморожены"; break;
      case TRADE_RETCODE_INVALID_FILL: str="Указан неподдерживаемый тип исполнения ордера по остатку "; break;
      case TRADE_RETCODE_CONNECTION: str="Нет соединения с торговым сервером"; break;
      case TRADE_RETCODE_ONLY_REAL: str="Операция разрешена только для реальных счетов"; break;
      case TRADE_RETCODE_LIMIT_ORDERS: str="Достигнут лимит на количество отложенных ордеров"; break;
      case TRADE_RETCODE_LIMIT_VOLUME: str="Достигнут лимит на объем ордеров и позиций для данного символа"; break;
      default: str="Неизвестный результат";
     }
//----
   return(str);
  }
//+------------------------------------------------------------------+
//|                                                HistoryLoader.mqh |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Загрузка истории для мультивалютного эксперта                    |
//+------------------------------------------------------------------+
int LoadHistory(datetime StartDate,           // стартовая дата для подгрузки истории
                string LoadedSymbol,          // символ запрашиваемых исторических данных
                ENUM_TIMEFRAMES LoadedPeriod) // таймфрейм запрашиваемых исторических данных
  {
//----+ 
//Print(__FUNCTION__, ": Start load ", LoadedSymbol+ " , " + EnumToString(LoadedPeriod) + " from ", StartDate);
   int res=CheckLoadHistory(LoadedSymbol,LoadedPeriod,StartDate);
   switch(res)
     {
      case -1 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Unknown symbol ", LoadedSymbol);               break;
      case -2 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Requested bars more than max bars in chart "); break;
      case -3 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Program was stopped ");                        break;
      case -4 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Indicator shouldn't load its own data ");      break;
      case -5 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Load failed ");                                break;
      case  0 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded OK ");  */                           break;
      case  1 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded previously ");  */                   break;
      case  2 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded previously and built ");  */         break;
      default : { /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Unknown result "); */}
     }
/* 
   if (res > 0)
    {   
     bars = Bars(LoadedSymbol, LoadedPeriod);
     Print(__FUNCTION__, "(", LoadedSymbol, " ", GetPeriodName(LoadedPeriod), "): First date ", first_date, " - ", bars, " bars");
    }
   */
//----+
   return(res);
  }
//+------------------------------------------------------------------+
//|  проверка истории для подгрузки                                  |
//+------------------------------------------------------------------+
int CheckLoadHistory(string symbol,ENUM_TIMEFRAMES period,datetime start_date)
  {
//----+
   datetime first_date=0;
   datetime times[100];
//--- check symbol & period
   if(symbol == NULL || symbol == "") symbol = Symbol();
   if(period == PERIOD_CURRENT)     period = Period();
//--- check if symbol is selected in the MarketWatch
   if(!SymbolInfoInteger(symbol,SYMBOL_SELECT))
     {
      if(GetLastError()==ERR_MARKET_UNKNOWN_SYMBOL) return(-1);
      if(!SymbolSelect(symbol,true)) Print(__FUNCTION__,"(): Не удалось добавить символ ",symbol," в окно MarketWatch!!!");
     }
//--- check if data is present
   SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date);
   if(first_date>0 && first_date<=start_date) return(1);
//--- don't ask for load of its own data if it is an indicator
   if(MQL5InfoInteger(MQL5_PROGRAM_TYPE)==PROGRAM_INDICATOR && Period()==period && Symbol()==symbol)
      return(-4);
//--- second attempt
   if(SeriesInfoInteger(symbol,PERIOD_M1,SERIES_TERMINAL_FIRSTDATE,first_date))
     {
      //--- there is loaded data to build timeseries
      if(first_date>0)
        {
         //--- force timeseries build
         CopyTime(symbol,period,first_date+PeriodSeconds(period),1,times);
         //--- check date
         if(SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date))
            if(first_date>0 && first_date<=start_date) return(2);
        }
     }
//--- max bars in chart from terminal options
   int max_bars=TerminalInfoInteger(TERMINAL_MAXBARS);
//--- load symbol history info
   datetime first_server_date=0;
   while(!SeriesInfoInteger(symbol,PERIOD_M1,SERIES_SERVER_FIRSTDATE,first_server_date) && !IsStopped())
      Sleep(5);
//--- fix start date for loading
   if(first_server_date>start_date) start_date=first_server_date;
   if(first_date>0 && first_date<first_server_date)
      Print(__FUNCTION__,"(): Warning: first server date ",first_server_date," for ",symbol,
            " does not match to first series date ",first_date);
//--- load data step by step
   int fail_cnt=0;
   while(!IsStopped())
     {
      //--- wait for timeseries build
      while(!SeriesInfoInteger(symbol,period,SERIES_SYNCHRONIZED) && !IsStopped())
         Sleep(5);
      //--- ask for built bars
      int bars=Bars(symbol,period);
      if(bars>0)
        {
         if(bars>=max_bars) return(-2);
         //--- ask for first date
         if(SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date))
            if(first_date>0 && first_date<=start_date) return(0);
        }
      //--- copying of next part forces data loading
      int copied=CopyTime(symbol,period,bars,100,times);
      if(copied>0)
        {
         //--- check for data
         if(times[0]<=start_date) return(0);
         if(bars+copied>=max_bars) return(-2);
         fail_cnt=0;
        }
      else
        {
         //--- no more than 100 failed attempts
         fail_cnt++;
         if(fail_cnt>=100) return(-5);
         Sleep(10);
        }
     }
//----+ stopped
   return(-3);
  }
//+------------------------------------------------------------------+
