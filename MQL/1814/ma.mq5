//+------------------------------------------------------------------+
//|                                                           MA.mq5 |
//|                        Copyright 2013, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
//--- input parameters
input int      StopLoss=100;
input int      TakeProfit=100;
input int      MA_Period=57;
input int      MA_Period1=3;
input int      EA_Magic=12345;
input double   Lot=1.0;

//--- глобальные переменные
int ma1Handle;   // хэндл индикатора  Moving Average
int maHandle;    // хэндл индикатора Moving Average
double ma1Val[]; // динамические массивы для хранения численных значений Moving Average для каждого бара
double maVal[];  // динамический массив для хранения значений индикатора Moving Average для каждого бара
double p_close;  // переменная для хранения значения close бара
int STP,TKP;     // будут использованы для значений Stop Loss и Take Profit
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- Получить хэндл индикатора MA
   ma1Handle=iMA(_Symbol,_Period,MA_Period1,0,MODE_LWMA,PRICE_CLOSE);

//---Получить хэндл индикатора Moving Average
   maHandle=iMA(_Symbol,_Period,MA_Period,0,MODE_EMA,PRICE_CLOSE);
//--- Нужно проверить, не были ли возвращены значения Invalid Handle
   if(ma1Handle<0 || maHandle<0)
     {
      Alert("Ошибка при создании индикаторов - номер ошибки: ",GetLastError(),"!!");
      return(-1);
     }

//--- для работы с брокерами, использующими 5-ти значные котировки,
//--- умножаем на 10 значения SL и TP
   STP = StopLoss;
   TKP = TakeProfit;
   if(_Digits==5 || _Digits==3)
     {
      STP = STP*10;
      TKP = TKP*10;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- освобождаем хэндлы индикаторов
   IndicatorRelease(ma1Handle);

   IndicatorRelease(maHandle);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- достаточно ли количество баров для работы
   if(Bars(_Symbol,_Period)<60) // общее количество баров на графике меньше 60?
     {
      Alert("На графике меньше 60 баров, советник не будет работать!!");
      return;
     }

//--- Для сохранения значения времени бара мы используем static-переменную Old_Time.
//--- При каждом выполнении функции OnTick мы будем сравнивать время текущего бара с сохраненным временем.
//--- Если они не равны, это означает, что начал строится новый бар.

   static datetime Old_Time;
   datetime New_Time[1];
   bool IsNewBar=false;

//--- копируем время текущего бара в элемент New_Time[0]
   int copied=CopyTime(_Symbol,_Period,0,1,New_Time);
   if(copied>0) // ok, успешно скопировано
     {
      if(Old_Time!=New_Time[0]) // если старое время не равно
        {
         IsNewBar=true;   // новый бар
         if(MQL5InfoInteger(MQL5_DEBUGGING)) Print("Новый бар",New_Time[0],"старый бар",Old_Time);
         Old_Time=New_Time[0];   // сохраняем время бара
        }
     }
   else
     {
      Alert("Ошибка копирования времени, номер ошибки =",GetLastError());
      ResetLastError();
      return;
     }

//--- советник должен проверять условия совершения новой торговой операции только при новом баре
   if(IsNewBar==false)
     {
      return;
     }

//--- имеем ли мы достаточное количество баров на графике для работы
   int Mybars=Bars(_Symbol,_Period);
   if(Mybars<60) // если общее количество баров меньше 60
     {
      Alert("На графике менее 60 баров, советник работать не будет!!");
      return;
     }

//--- объявляем структуры, которые будут использоваться для торговли
   MqlTick latest_price;       // Будет использоваться для текущих котировок
   MqlTradeRequest mrequest;   // Будет использоваться для отсылки торговых запросов
   MqlTradeResult mresult;     // Будет использоваться для получения результатов выполнения торговых запросов
   MqlRates mrate[];           // Будет содержать цены, объемы и спред для каждого бара
   ZeroMemory(mrequest);
/*
     Установим индексацию в массивах котировок и индикаторов 
     как в таймсериях
*/
//--- массив котировок
   ArraySetAsSeries(mrate,true);

//--- массив значений индикатора MA
   ArraySetAsSeries(ma1Val,true);

//--- массив значений индикатора MA-8
   ArraySetAsSeries(maVal,true);

//--- Получить текущее значение котировки в структуру типа MqlTick
   if(!SymbolInfoTick(_Symbol,latest_price))
     {
      Alert("Ошибка получения последних котировок - ошибка:",GetLastError(),"!!");
      return;
     }

//--- Получить исторические данные последних 3-х баров
   if(CopyRates(_Symbol,_Period,0,3,mrate)<0)
     {
      Alert("Ошибка копирования исторических данных - ошибка:",GetLastError(),"!!");
      return;
     }

//--- копируем значения индикаторов из индикаторных буферов
   if(CopyBuffer(ma1Handle,0,0,3,ma1Val)<0)
     {
      Alert("Ошибка копирования буферов индикатора Moving Average - номер ошибки:",GetLastError(),"!!");
      return;
     }
   if(CopyBuffer(maHandle,0,0,3,maVal)<0)
     {
      Alert("Ошибка копирования буферов индикатора Moving Average - номер ошибки:",GetLastError());
      return;
     }
//--- есть ли открытые позиции?
   bool Buy_opened=false;  // переменные, в которых будет храниться информация 
   bool Sell_opened=false; // о наличии соответствующих открытых позиций

   if(PositionSelect(_Symbol)==true) // есть открытая позиция
     {
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         Buy_opened=true;  //это длинная позиция
        }
      else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         Sell_opened=true; // это короткая позиция
        }
     }

//--- Скопируем текущую цену закрытия предыдущего бара (это бар 1)
   p_close=mrate[1].close;  // цена закрытия предыдущего бара

/*
    1. Проверка условий для покупки : MA-8 растет, 
    предыдущая цена закрытия бара больше MA-8, 
*/

//--- объявляем переменные типа boolean, они будут использоваться при проверке условий для покупки
   bool Buy_Condition_1=(maVal[0]>maVal[1]) && (maVal[1]>maVal[2]); // MA-8 растет
   bool Buy_Condition_2=(ma1Val[0]>ma1Val[1]) &&(ma1Val[1]>ma1Val[2]);
   bool Buy_Condition_3=(p_close>maVal[1]);         // предыдущая цена закрытия выше скользяшей средней MA-8
   bool Buy_Condition_4 =(maVal[0]>ma1Val[0]);

//--- собираем все вместе
   if(Buy_Condition_1 && Buy_Condition_2)
     {
      if(Buy_Condition_3 && Buy_Condition_4)
        {
         // есть ли в данный момент открытая позиция на покупку?
         if(Buy_opened)
           {
            Alert("Уже есть позиция на покупку!!!");
            return;    // не добавлять к открытой позиции на покупку
           }
         mrequest.action = TRADE_ACTION_DEAL;                                  // немедленное исполнение
         mrequest.price = NormalizeDouble(latest_price.ask,_Digits);           // последняя цена ask
         mrequest.sl = NormalizeDouble(latest_price.ask - STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.ask + TKP*_Point,_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                            // символ
         mrequest.volume = Lot;                                                // количество лотов для торговли
         mrequest.magic = EA_Magic;                                            // Magic Number
         mrequest.type = ORDER_TYPE_BUY;                                       // ордер на покупку
         mrequest.type_filling = ORDER_FILLING_FOK;                            // тип исполнения ордера - все или ничего
         mrequest.deviation=100;                                               // проскальзывание от текущей цены
         //--- отсылаем ордер
         if(OrderSend(mrequest,mresult))
         // анализируем код возврата торгового сервера
         if(mresult.retcode==10009 || mresult.retcode==10008) //запрос выполнен или ордер успешно помещен
           {
            Alert("Ордер Buy успешно помещен, тикет ордера #:",mresult.order,"!!");
           }
         else
           {
            Alert("Запрос на установку ордера Buy не выполнен - код ошибки:",GetLastError());
            return;
           }
        }
     }
/*
    2. Проверка условий для продажи : MA-8 падает, 
    предыдущая цена закрытия бара меньше MA-8
*/

//--- объявляем переменные типа boolean, они будут использоваться при проверке условий для продажи
   bool Sell_Condition_1 = (maVal[0]<maVal[1]) && (maVal[1]<maVal[2]);  // MA-8 падает
   bool Sell_Condition_2 = (p_close <maVal[1]);                         // предыдущая цена закрытия ниже MA-8
   bool Sell_Condition_3=(ma1Val[0]<ma1Val[1]) && (ma1Val[1]<ma1Val[2]);                         // текущее значение ADX value больше заданного (22)
   bool Sell_Condition_4=(maVal[0]<ma1Val[0]);                         // -DI больше, чем +DI

//--- собираем все вместе
   if(Sell_Condition_1 && Sell_Condition_2)
     {
      if(Sell_Condition_3 && Sell_Condition_4)
        {
         // есть ли в данный момент открытая позиция на продажу?
         if(Sell_opened)
           {
            Alert("Уже есть позиция на продажу!!!");
            return;    // не добавлять к открытой позиции на продажу
           }
         mrequest.action = TRADE_ACTION_DEAL;                                  // немедленное исполнение
         mrequest.price = NormalizeDouble(latest_price.bid,_Digits);           // последняя цена Bid
         mrequest.sl = NormalizeDouble(latest_price.bid + STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.bid - TKP*_Point,_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                            // символ
         mrequest.volume = Lot;                                                // количество лотов для торговли
         mrequest.magic = EA_Magic;                                            // Magic Number
         mrequest.type= ORDER_TYPE_SELL;                                       // ордер на продажу
         mrequest.type_filling = ORDER_FILLING_FOK;                            // тип исполнения ордера - все или ничего
         mrequest.deviation=100;                                               // проскальзывание от текущей цены
         //--- отсылаем ордер
         if(OrderSend(mrequest,mresult))
         // анализируем код возврата торгового сервера
         if(mresult.retcode==10009 || mresult.retcode==10008) //Request is completed or order placed
           {
            Alert("Ордер Sell успешно помещен, тикет ордера #:",mresult.order,"!!");
           }
         else
           {
            Alert("Запрос на установку ордера Sell не выполнен - код ошибки:",GetLastError());
            return;
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
