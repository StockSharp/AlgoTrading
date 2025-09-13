//+------------------------------------------------------------------+
//|                                            Divergence_Expert.mq4 |
//|                                        Copyright 2015, Scriptong |
//|                                          http://advancetools.net |
//+------------------------------------------------------------------+
#property copyright "Scriptong"
#property link      "http://advancetools.net"
#property strict

#include <stderror.mqh>
#include <Divergence\Divergence_CalculateTradeType.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_YESNO
  {
   NO,                                                                                             // No / Нет
   YES                                                                                             // Yes / Да
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_ONOFF
  {
   OFF,                                                                                            // Off / Выкл.
   ON                                                                                              // On / Вкл.
  };

//---- input parameters
input string                   i_string1             = "Parameters of orders / Параметры ордеров"; // ======================
input double                   i_staticLots          = 0.1;                                        // Constant volume / Постоянный объем
input double                   i_dynamicLots         = 10.0;                                       // Dynamic volume, % of balance/ Динамический объем в % от баланса
input double                   i_tpToSLRatio         = 2.0;                                        // Ratio of the TP size to SL / Отношение размера TP к SL
input uint                     i_slOffset            = 1;                                          // Offset for Stop Loss, pts. / Отступ для Stop Loss, пп.

input string                   i_string2="Divergence parameters / Параметры дивергенции"; // ============================
input ENUM_INDICATOR_TYPE      i_indicatorType       = WILLIAM_BLAU;                               // Base indicator / Базовый индикатор
input int                      i_divergenceDepth     = 20;                                         // Depth of 2nd ref. point search / Глубина поиска 2ой оп. точки
input int                      i_barsPeriod1         = 8000;                                       // First calculate period / Первый период расчета
input int                      i_barsPeriod2         = 2;                                          // Second calculate period / Второй период расчета
input int                      i_barsPeriod3         = 1;                                          // Third calculate period / Третий период расчета
input ENUM_APPLIED_PRICE       i_indAppliedPrice     = PRICE_CLOSE;                                // Applied price of indicator / Цена расчета индикатора
input ENUM_MA_METHOD           i_indMAMethod         = MODE_EMA;                                   // MA calculate method / Метод расчета среднего
input int                      i_findExtInterval     = 10;                                         // Price ext. to indicator ext. / От экст. цены до экст. инд.
input ENUM_MARKET_APPLIED_PRICE i_marketAppliedPrice = MARKET_APPLIED_PRICE_CLOSE;                 // Applied price of market / Используемая рыночная цена
input string                   i_customName          = "Sentiment_Line";                           // The name of indicator / Имя индикатора
input int                      i_customBuffer        = 0;                                          // Index of data buffer / Индекс буфера для съема данных
input ENUM_CUSTOM_PARAM_CNT    i_customParamCnt      = PARAM_CNT_3;                                // Amount of ind. parameters / Кол-во параметров индикатора
input double                   i_customParam1        = 13.0;                                       // Value of the 1st parameter / Значение 1-ого параметра
input double                   i_customParam2        = 1.0;                                        // Value of the 2nd parameter / Значение 2-ого параметра
input double                   i_customParam3        = 0.0;                                        // Value of the 3rd parameter / Значение 3-ого параметра
input double                   i_customParam4        = 0.0;                                        // Value of the 4th parameter / Значение 4-ого параметра
input double                   i_customParam5        = 0.0;                                        // Value of the 5th parameter / Значение 5-ого параметра
input double                   i_customParam6        = 0.0;                                        // Value of the 6th parameter / Значение 6-ого параметра
input double                   i_customParam7        = 0.0;                                        // Value of the 7th parameter / Значение 7-ого параметра
input double                   i_customParam8        = 0.0;                                        // Value of the 8th parameter / Значение 8-ого параметра
input double                   i_customParam9        = 0.0;                                        // Value of the 9th parameter / Значение 9-ого параметра
input double                   i_customParam10       = 0.0;                                        // Value of the 10th parameter / Значение 10-ого параметра
input double                   i_customParam11       = 0.0;                                        // Value of the 11th parameter / Значение 11-ого параметра
input double                   i_customParam12       = 0.0;                                        // Value of the 12th parameter / Значение 12-ого параметра
input double                   i_customParam13       = 0.0;                                        // Value of the 13th parameter / Значение 13-ого параметра
input double                   i_customParam14       = 0.0;                                        // Value of the 14th parameter / Значение 14-ого параметра
input double                   i_customParam15       = 0.0;                                        // Value of the 15th parameter / Значение 15-ого параметра
input double                   i_customParam16       = 0.0;                                        // Value of the 16th parameter / Значение 16-ого параметра
input double                   i_customParam17       = 0.0;                                        // Value of the 17th parameter / Значение 17-ого параметра
input double                   i_customParam18       = 0.0;                                        // Value of the 18th parameter / Значение 18-ого параметра
input double                   i_customParam19       = 0.0;                                        // Value of the 19th parameter / Значение 19-ого параметра
input double                   i_customParam20       = 0.0;                                        // Value of the 20th parameter / Значение 20-ого параметра
input ENUM_ONOFF               i_useCoincidenceCharts = OFF;                                       // The coincidence of charts / Совпадение графиков
input ENUM_ONOFF               i_excludeOverlaps     = OFF;                                        // Exclude overlaps of lines / Исключить наложение линий
input ENUM_YESNO               i_useClassA           = YES;                                        // Use class A divergence / Использовать дивергенции класса А
input ENUM_YESNO               i_useClassB           = YES;                                        // Use class B divergence / Использовать дивергенции класса B
input ENUM_YESNO               i_useClassC           = YES;                                        // Use class C divergence / Использовать дивергенции класса C
input ENUM_YESNO               i_useHidden           = YES;                                        // Use hidden divergence / Использовать скрытые дивергенции

input string                   i_string3             = "Other parameters / Другие параметры";      // ======================
input int                      i_startBarsCnt        = 100;                                        // Start bars count / Количество баров на старте
input ENUM_YESNO               i_is5Digits           = YES;                                        // Use 5-digits / Использовать 5-изначные котировки
input ENUM_YESNO               i_isECN               = NO;                                         // Use ECN / Использовать ECN
input int                      i_slippage            = 3;                                          // Slippage / Отклонение от запрошенных цен
input string                   i_openOrderSound      = "ok.wav";                                   // Sound for open order / Звук при открытии ордера
input int                      i_magicNumber         = 3;                                          // ID of expert orders / ID ордеров эксперта

//--- Глобальные переменные эксперта
TradeParam           g_tradeParam;                                                                 // Структура для передачи значений торгового запроса
GetSymbolInfo       *g_symbolInfo;                                                                 // Класс сбора текущей рыночной информации
CTrade              *g_trade;                                                                      // Класс выполнения торговых операций
CCalculateTradeType *g_calcTradeType;                                                              // Класс расчета параметров торгового запроса
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Функция инициализации эксперта                                                                                                                                                           |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
int OnInit()
  {
   if(!TuningParameters()) // Корректировка значений настроечных параметров
      return INIT_FAILED;

   if(!InitializeClasses()) // Инициализация классов GetSymbolInfo, Trade и CalculateTradeType
      return INIT_FAILED;

   return INIT_SUCCEEDED;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Функция деинициализации эксперта                                                                                                                                                         |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   delete g_symbolInfo;
   delete g_trade;
   delete g_calcTradeType;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Проверка значений настроечных параметров                                                                                                                                                 |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool TuningParameters()
  {
   string name=WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");

   if(i_dynamicLots<0.01 || i_dynamicLots>99.99)
     {
      Alert(name,": величина динамического объема рассчитывается в процентах. Допустимые значения от 0.01 до 99.99. Эксперт отключен.");
      Alert(name,(isRussianLang)? ": величина динамического объема рассчитывается в процентах. Допустимые значения от 0.01 до 99.99. Эксперт отключен." :
            ": value of dynamic volume must be greater than 0.00 and less than 100.00. Expert is turned off.");
      return false;
     }

   if(i_barsPeriod1<1)
     {
      Alert(name,(isRussianLang)? ": первое количество баров для расчета показаний индикатора менее 1. Индикатор отключен." :
            ": the first amount of bars for calculate the indicator values is less then 1. The indicator is turned off.");
      return false;
     }

   if(i_barsPeriod2<1)
     {
      Alert(name,(isRussianLang)? ": второе количество баров для расчета показаний индикатора менее 1. Индикатор отключен." :
            ": the second amount of bars for calculate the indicator values is less then 1. The indicator is turned off.");
      return false;
     }

   if(i_barsPeriod3<1)
     {
      Alert(name,(isRussianLang)? ": третье количество баров для расчета показаний индикатора менее 1. Индикатор отключен." :
            ": the third amount of bars for calculate the indicator values is less then 1. The indicator is turned off.");
      return false;
     }

   if(i_findExtInterval<1)
     {
      Alert(name,(isRussianLang)? ": интервал поиска экстремума цены должен быть более нуля баров. Индикатор отключен." :
            ": the interval of search of price extremum must be greater than zero bars. The indicator is turned off.");
      return false;
     }

   return true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Инициализация всех необходимых классов                                                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool InitializeClasses()
  {
   uint pointMul=1;
   if(i_is5Digits==YES)
      pointMul=10;

   g_symbolInfo= new GetSymbolInfo(Symbol(),i_slippage * pointMul,i_isECN == YES);
   string name = WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");
   if(g_symbolInfo==NULL)
     {
      Alert(name,isRussianLang? ": невозможно инициализировать класс GetSymbolInfo. Эксперт выключен!" :
            ": could not to initialize the class GetSymbolInfo. Expert is turned off!");
      return false;
     }

   if(g_symbolInfo.GetPoint()==0)
     {
      Alert(name,isRussianLang? ": фатальная ошибка - размер пункта равен нулю. Эксперт выключен!" :
            ": fatal error - the size of one point is equal to zero. Expert is turned off!");
      return false;
     }

   if(g_symbolInfo.GetTickValue()==0)
     {
      Alert(name,isRussianLang? ": фатальная ошибка - стоимость тика равна нулю. Эксперт выключен!" :
            ": fatal error - the cost of one tick is equal to zero. Expert is turned off!");
      return false;
     }

   g_trade=new CTrade(i_openOrderSound);
   if(g_trade==NULL)
     {
      Alert(name,isRussianLang? ": невозможно инициализировать класс Trade. Эксперт выключен!" :
            ": could not to initialize the class Trade. Expert is turned off!");
      return false;
     }

   g_calcTradeType=new CCalculateTradeType(i_staticLots,i_dynamicLots,i_tpToSLRatio,i_slOffset*pointMul*g_symbolInfo.GetPoint(),i_magicNumber,i_indicatorType,i_divergenceDepth,
                                           i_barsPeriod1,i_barsPeriod2,i_barsPeriod3,i_indAppliedPrice,i_indMAMethod,i_findExtInterval,i_marketAppliedPrice,i_customName,
                                           i_customBuffer,i_customParamCnt,i_customParam1,i_customParam2,i_customParam3,i_customParam4,i_customParam5,i_customParam6,
                                           i_customParam7,i_customParam8,i_customParam9,i_customParam10,i_customParam11,i_customParam12,i_customParam13,i_customParam14,
                                           i_customParam15,i_customParam16,i_customParam17,i_customParam18,i_customParam19,i_customParam20,i_useCoincidenceCharts==ON,
                                           i_excludeOverlaps==ON,i_useClassA==YES,i_useClassB==YES,i_useClassC==YES,i_useHidden==YES);
   if(g_calcTradeType==NULL)
     {
      Alert(name,isRussianLang? ": невозможно инициализировать класс CalculateTradeType. Эксперт выключен!" :
            ": could not to initialize the class CalculateTradeType. Expert is turned off!");
      return false;
     }

   return true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Функция start эксперта                                                                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void OnTick()
  {
   if(Bars<i_startBarsCnt)
      return;

   static bool isStart=false;
   if(!isStart)
     {
      Alert("Запуск!");
      isStart=true;
     }

// Проведение торговых операций
   while(!IsStopped())
     {
      // Слежение за изменением рыночного окружения
      g_symbolInfo.RefreshInfo();

      if(g_symbolInfo.GetTickValue()==0 || g_symbolInfo.GetPoint()==0 || g_symbolInfo.GetTickSize()==0)
        {
         bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");
         Alert(WindowExpertName(),isRussianLang? ": фатальная ошибка терминала - величина пункта или тика равны нулю. Эксперт отключен." :
               ": fatal error - the size of one point or of one tick is equal to zero. Expert is turned off!");
         ExpertRemove();
         return;
        }

      ENUM_TRADE_TYPE tradeType=g_calcTradeType.GetTradeType(g_tradeParam,g_symbolInfo.GetAllSymbolInfo(),g_trade.GetTradeErrorState());

      switch(tradeType)
        {
         case TRADE_OPEN:     if(!g_trade.OpenOrder(g_symbolInfo,g_tradeParam))
            return;
            break;

         case TRADE_MODIFY:   if(!g_trade.ModifyOrder(g_symbolInfo,g_tradeParam))
            return;
            break;

         case TRADE_DESTROY:  if(!g_trade.DestroyOrder(g_symbolInfo,g_tradeParam))
            return;
            break;

         case TRADE_CLOSEBY:  if(!g_trade.CloseCounter(g_tradeParam))
            return;
            break;

         case TRADE_FATAL_ERROR:
            ExpertRemove();
            return;

         case TRADE_NONE:     return;
        }
     }
  }
//+------------------------------------------------------------------+
