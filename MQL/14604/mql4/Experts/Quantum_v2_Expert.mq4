//+------------------------------------------------------------------+
//|                                            Quantum_v2_Expert.mq4 |
//|                                        Copyright 2016, Scriptong |
//|                                          http://advancetools.net |
//+------------------------------------------------------------------+
#property copyright "Scriptong"
#property link      "http://advancetools.net"
#property version   "2.0"
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_YESNO
  {
   NO,                                                                                             // Нет
   YES                                                                                             // Да
  };

#include <Quantum\Quantum_CalculateTradeType_v2.mqh>

input string               i_string1               = "Orders / Ордера";                            // =================================================
input double               i_staticLots            = 0.1;                                          // Static volume / Фиксированный объем 
input double               i_dynamicLots           = 10.0;                                         // Dynamic volume, % / Динамический объем, % 
input uint                 i_tpSize                = 0;                                            // Size of TP, pts / Размер TP, пп.
input uint                 i_slShift               = 40;                                           // Offset from ext., pts. / Отступ от экстремума, пп.

input string               i_string2               = "Параметры Stochastic и Quantum";             // ======================
input uint                 i_periodK               = 100;                                          // %K period / Период %K
input uint                 i_periodD               = 100;                                          // %D period / Период %D
input uint                 i_slowing               = 3;                                            // Slowing / Запаздывание
input double               i_highLevel             = 80.0;                                         // Bottom of overbought zone / Низ зоны перекупленности
input double               i_lowLevel              = 20.0;                                         // Top of overselling zone / Верх зоны перепроданности
input double               i_highCloseLevel        = 90.0;                                         // Level for Buy close / Уровень для закрытия Buy
input double               i_lowCloseLevel         = 10.0;                                         // Level for Sell close / Уровень для закрытия Sell
input uint                 i_extremumRank          = 300;                                          // Rank of extremum / Ранг экстремума

input string               i_string3               = "Другие параметры";                           // ======================
input ENUM_YESNO           i_is5Digits           = YES;                                            // Use 5-digits / Использовать 5-изначные котировки
input ENUM_YESNO           i_isECN               = NO;                                             // Use ECN / Использовать ECN
input int                  i_slippage            = 3;                                              // Slippage / Отклонение от запрошенных цен
input string               i_openOrderSound      = "ok.wav";                                       // Sound for open order / Звук при открытии ордера
input int                  i_magicNumber         = 2353;                                           // ID of expert orders / ID ордеров эксперта

TradeParam           g_tradeParam;                                                                 // Структура для передачи значений торгового запроса
GetSymbolInfo       *g_symbolInfo;                                                                 // Класс сбора текущей рыночной информации
CTrade              *g_trade;                                                                      // Класс выполнения торговых операций
CalculateTradeType  *g_calcTradeType;                                                              // Класс расчета параметров торгового запроса
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Функция инициализации эксперта                                                                                                                                                           |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
int OnInit()
  {
// Инициализация торговой части советника
   if(!IsTunningParametersCorrect())
      return INIT_FAILED;

   if(!InitializeClasses()) // Инициализация классов GetSymbolInfo, Trade и CalculateTradeType
      return INIT_SUCCEEDED;

   return INIT_SUCCEEDED;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Проверка правильности значений настроечных параметров                                                                                                                                             |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool IsTunningParametersCorrect()
  {
   string name=WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");

   if(i_dynamicLots<0.01 || i_dynamicLots>99.99)
     {
      Alert(name,(isRussianLang)? ": величина динамического объема рассчитывается в процентах. Допустимые значения от 0.01 до 99.99. Эксперт отключен." :
            ": value of dynamic volume must be greater than 0.00 and less than 100.00. Expert is turned off.");
      return false;
     }

   if(i_periodK<1)
     {
      Alert(name,(isRussianLang)? ": период %К стохастика должен быть более 0. Эксперт отключен." :
            ": %K period of stochastic must be greater than zero. Expert is turned off.");
      return false;
     }

   if(i_periodD<1)
     {
      Alert(name,(isRussianLang)? ": период %D стохастика должен быть более 0. Эксперт отключен." :
            ": %D period of stochastic must be greater than zero. Expert is turned off.");
      return false;
     }

   if(i_slowing<1)
     {
      Alert(name,(isRussianLang)? ": запаздывание стохастика должен быть более 0. Эксперт отключен." :
            ": slowing of stochastic must be greater than zero. Expert is turned off.");
      return false;
     }

   if(i_extremumRank<1)
     {
      Alert(name,(isRussianLang)? ": ранг экстремума должен быть более 0. Эксперт отключен." :
            ": rank of extremum must be greater than zero. Expert is turned off.");
      return false;
     }

   return true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Функция деинициализации эксперта                                                                                                                                                         |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(g_symbolInfo)
     {
      delete g_symbolInfo;
      g_symbolInfo=NULL;
     }

   if(g_trade)
     {
      delete g_trade;
      g_trade=NULL;
     }

   if(g_calcTradeType)
     {
      delete g_calcTradeType;
      g_calcTradeType=NULL;
     }
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Инициализация всех необходимых классов                                                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool InitializeClasses()
  {
   uint pointMul=1;
   if(i_is5Digits==YES)
      pointMul=10;

   string name=WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");

   g_symbolInfo=new GetSymbolInfo(Symbol(),i_slippage*pointMul,i_isECN==YES);
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
      Alert(name,isRussianLang? ": невозможно инициализировать класс CTrade. Эксперт выключен!" :
            ": could not to initialize the class CTrade. Expert is turned off!");
      return false;
     }

   g_calcTradeType=new CalculateTradeType(i_staticLots,i_dynamicLots,i_tpSize*pointMul,i_slShift*pointMul,i_periodK,i_periodD,i_slowing,i_highLevel,i_lowLevel,
                                          i_highCloseLevel,i_lowCloseLevel,i_extremumRank,i_magicNumber);
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
   if(Bars<int(i_extremumRank))
      return;

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

      bool isContinue=true;
      switch(tradeType)
        {
         case TRADE_OPEN:     isContinue=g_trade.OpenOrder(g_symbolInfo,g_tradeParam);
         break;

         case TRADE_MODIFY:   isContinue=g_trade.ModifyOrder(g_symbolInfo,g_tradeParam);
         break;

         case TRADE_DESTROY:  isContinue=g_trade.DestroyOrder(g_symbolInfo,g_tradeParam);
         break;

         case TRADE_CLOSEBY:  isContinue=g_trade.CloseCounter(g_tradeParam);
         break;

         case TRADE_FATAL_ERROR:
            ExpertRemove();
            return;
        }

      if(tradeType==TRADE_NONE || !isContinue)
         break;
     }
  }
//+------------------------------------------------------------------+
