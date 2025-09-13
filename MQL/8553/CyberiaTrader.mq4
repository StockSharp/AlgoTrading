#property copyright "Copyright c 2006, Cyberia Decisions"
#property link      "http://cyberia.org.ru"


#define DECISION_BUY 1
#define DECISION_SELL 0
#define DECISION_UNKNOWN -1

//---- Глобальные переменные
extern bool ExitMarket = false;
extern bool ShowSuitablePeriod = false;
extern bool ShowMarketInfo = false;
extern bool ShowAccountStatus = false;
extern bool ShowStat = false;
extern bool ShowDecision = false;
extern bool ShowDirection = false;
extern bool BlockSell = false;
extern bool BlockBuy = false;
extern bool ShowLots = false;
extern bool BlockStopLoss = false;
extern bool DisableShadowStopLoss = true;
extern bool DisableExitSell = false;
extern bool DisableExitBuy = false;
extern bool EnableMACD = false;
extern bool EnableMA = false;
extern bool EnableCyberiaLogic = true;
extern bool EnableLogicTrading = false;
extern bool EnableMoneyTrain = false;
extern bool EnableReverceDetector = false;
extern double ReverceIndex = 3;
extern double MoneyTrainLevel = 4;
extern int MACDLevel = 10;
extern bool AutoLots = True;
extern bool AutoDirection = True;
extern double ValuesPeriodCount = 23;
extern double ValuesPeriodCountMax = 23;
extern double SlipPage = 1; // Проскальзывание ставки
extern double Lots = 0.1; // Количество лотов
extern double StopLoss = 0;
extern double TakeProfit = 0;
extern double SymbolsCount = 1;
extern double Risk = 0.5;
extern double StopLossIndex = 1.1;
extern bool AutoStopLossIndex = true;
extern double StopLevel;
bool DisableSell = false;
bool DisableBuy = false;
bool ExitSell = false;
bool ExitBuy = false;
double Disperce = 0;
double DisperceMax = 0;
bool DisableSellPipsator = false;
bool DisableBuyPipsator = false;
//----
double ValuePeriod = 1; // Шаг периода в минутах
double ValuePeriodPrev = 1;
int FoundOpenedOrder = false;
bool DisablePipsator = false;
double BidPrev = 0;
double AskPrev = 0;
// Переменные для оценки качества моделирования
double BuyPossibilityQuality;
double SellPossibilityQuality;
double UndefinedPossibilityQuality;
//double BuyPossibilityQualityMid;
double PossibilityQuality;
double QualityMax = 0;
//----
double BuySucPossibilityQuality;
double SellSucPossibilityQuality;
double UndefinedSucPossibilityQuality;
double PossibilitySucQuality;
//----
double ModelingPeriod; // Период моделирования в минутах
double ModelingBars; // Количество шагов в периоде
//----
double Spread; // Спрэд
double Decision;
double DecisionValue;
double PrevDecisionValue;
//----
int ticket, total, cnt;
//----
double BuyPossibility;
double SellPossibility;
double UndefinedPossibility;
double BuyPossibilityPrev;
double SellPossibilityPrev;
double UndefinedPossibilityPrev;
//----
double BuySucPossibilityMid; // Средняя вероятность успешной покупки
double SellSucPossibilityMid; // Средняя вероятность успешной продажи
double UndefinedSucPossibilityMid; // Средняя успешная вероятность неопределенного состояния
//----
double SellSucPossibilityCount; // Количество вероятностей успешной продажи
double BuySucPossibilityCount; // Количество вероятностей успешной покупки
double UndefinedSucPossibilityCount; // Количество вероятностей неопределенного состояния
//----
double BuyPossibilityMid; // Средняя вероятность покупки
double SellPossibilityMid; // Средняя вероятность продажи
double UndefinedPossibilityMid; // Средняя вероятность неопределенного состояния
//----
double SellPossibilityCount; // Количество вероятностей продажи
double BuyPossibilityCount; // Количество вероятностей покупки
double UndefinedPossibilityCount; // Количество вероятностей неопределенного состояния
//----
// Переменные для хранения информация о рынке
double ModeLow;
double ModeHigh;
double ModeTime;
double ModeBid;
double ModeAsk;
double ModePoint;
double ModeDigits;
double ModeSpread;
double ModeStopLevel;
double ModeLotSize;
double ModeTickValue;
double ModeTickSize;
double ModeSwapLong;
double ModeSwapShort;
double ModeStarting;
double ModeExpiration;
double ModeTradeAllowed;
double ModeMinLot;
double ModeLotStep;
//+------------------------------------------------------------------+
//|Считываем информацию о рынке                                                                  |
//+------------------------------------------------------------------+
int GetMarketInfo()
  {
   // Считываем информацию о рынке
   ModeLow = MarketInfo(Symbol(), MODE_LOW);
   ModeHigh = MarketInfo(Symbol(), MODE_HIGH);
   ModeTime = MarketInfo(Symbol(), MODE_TIME);
   ModeBid = MarketInfo(Symbol(), MODE_BID);
   ModeAsk = MarketInfo(Symbol(), MODE_ASK);
   ModePoint = MarketInfo(Symbol(), MODE_POINT);
   ModeDigits = MarketInfo(Symbol(), MODE_DIGITS);
   ModeSpread = MarketInfo(Symbol(), MODE_SPREAD);
   ModeStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL);
   ModeLotSize = MarketInfo(Symbol(), MODE_LOTSIZE);
   ModeTickValue = MarketInfo(Symbol(), MODE_TICKVALUE);
   ModeTickSize = MarketInfo(Symbol(), MODE_TICKSIZE);
   ModeSwapLong = MarketInfo(Symbol(), MODE_SWAPLONG);
   ModeSwapShort = MarketInfo(Symbol(), MODE_SWAPSHORT);
   ModeStarting = MarketInfo(Symbol(), MODE_STARTING);
   ModeExpiration = MarketInfo(Symbol(), MODE_EXPIRATION);
   ModeTradeAllowed = MarketInfo(Symbol(), MODE_TRADEALLOWED);
   ModeMinLot = MarketInfo(Symbol(), MODE_MINLOT);
   ModeLotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
   // Выводим информацию о рынке
   if ( ShowMarketInfo == True )
     {
       Print("ModeLow:",ModeLow);
       Print("ModeHigh:",ModeHigh);
       Print("ModeTime:",ModeTime);
       Print("ModeBid:",ModeBid);
       Print("ModeAsk:",ModeAsk);
       Print("ModePoint:",ModePoint);
       Print("ModeDigits:",ModeDigits);
       Print("ModeSpread:",ModeSpread);
       Print("ModeStopLevel:",ModeStopLevel);
       Print("ModeLotSize:",ModeLotSize);
       Print("ModeTickValue:",ModeTickValue);
       Print("ModeTickSize:",ModeTickSize);
       Print("ModeSwapLong:",ModeSwapLong);
       Print("ModeSwapShort:",ModeSwapShort);
       Print("ModeStarting:",ModeStarting);
       Print("ModeExpiration:",ModeExpiration);
       Print("ModeTradeAllowed:",ModeTradeAllowed);
       Print("ModeMinLot:",ModeMinLot);
       Print("ModeLotStep:",ModeLotStep);
     }
   return (0);
  }
//+------------------------------------------------------------------+
//| Расчет количества лотов                                          |
//+------------------------------------------------------------------+
int CyberiaLots()
  {
   GetMarketInfo();
   // Сумма счета
   double S;
   // Стоимость лота
   double L;
   // Количество лотов
   double k;
   // Стоимость одного пункта
   if( AutoLots == true )
     {
       if(SymbolsCount != OrdersTotal())
         {
           S = (AccountBalance()* Risk - AccountMargin()) * AccountLeverage() / 
                (SymbolsCount - OrdersTotal());
         }
       else
         {
           S = 0;
         }
       // Проверяем, является ли валюта по евро
       if(StringFind( Symbol(), "USD") == -1)
         {
           if(StringFind( Symbol(), "EUR") == -1)
             {
               S = 0;
             }
           else
             {
               S = S / iClose ("EURUSD", PERIOD_M1, 0);
               if(StringFind( Symbol(), "EUR") != 0)
                  {
                  S /= Bid;
                  }
             }
         }
       else
         {
           if(StringFind(Symbol(), "USD") != 0)
             {
               S /= Bid;
             }
         }
       S /= ModeLotSize;
       S -= ModeMinLot;
       S /= ModeLotStep;
       S = NormalizeDouble(S, 0);
       S *= ModeLotStep;
       S += ModeMinLot;
       Lots = S;
       if(ShowLots == True)
           Print ("Lots:", Lots);
     }
   return (0);
  }
//+------------------------------------------------------------------+
//|   Инициализируем советника                                       |
//+------------------------------------------------------------------+
int init()
  {
   AccountStatus();   
   GetMarketInfo();
   ModelingPeriod = ValuePeriod * ValuesPeriodCount; // Период моделирования в минутах
   if (ValuePeriod != 0 )
       ModelingBars = ModelingPeriod / ValuePeriod; // Количество шагов в периоде
   CalculateSpread();
   return(0);
  }
//+------------------------------------------------------------------+
//| Вычисляем фактическую величину спреда (возвращаемые функции      |
//| о рынке могут давать неверное фактическое значение спреда если   |
//| брокер варьирует величину спреда                                 |
//+------------------------------------------------------------------+
int CalculateSpread()
  {
   Spread = Ask - Bid;
   return (0);
  }
//+------------------------------------------------------------------+
//| Принимаем решение                                                |
//+------------------------------------------------------------------+
int CalculatePossibility (int shift)
  {
   DecisionValue = iClose( Symbol(), PERIOD_M1, ValuePeriod * shift) - 
                   iOpen( Symbol(), PERIOD_M1, ValuePeriod * shift);
   PrevDecisionValue = iClose( Symbol(), PERIOD_M1, ValuePeriod * (shift+1)) - 
                       iOpen( Symbol(), PERIOD_M1, ValuePeriod * (shift+1));
   SellPossibility = 0;
   BuyPossibility = 0;
   UndefinedPossibility = 0;
   if(DecisionValue != 0) // Если решение не определенно
     {
       if(DecisionValue > 0) // Если решение в пользу продажи
         {
           // Подозрение на вероятность продажи
           if(PrevDecisionValue < 0) // Подтверждение решения в пользу продажи
             {
               Decision = DECISION_SELL;
               BuyPossibility = 0;
               SellPossibility = DecisionValue;
               UndefinedPossibility = 0;
             }
           else  // Иначе решение не определено
             {
               Decision = DECISION_UNKNOWN;
               UndefinedPossibility = DecisionValue;
               BuyPossibility = 0;
               SellPossibility = 0;
             }
         }
       else // Если решение в пользу покупки
         {
           if(PrevDecisionValue > 0) // Подтверждение решения в пользу продажи
             {
               Decision = DECISION_BUY;
               SellPossibility = 0;
               UndefinedPossibility = 0;
               BuyPossibility = -1 * DecisionValue;
             }
           else  // Решение не определено
             {
               Decision = DECISION_UNKNOWN;
               UndefinedPossibility = -1 * DecisionValue;
               SellPossibility = 0;
               BuyPossibility = 0;
             }
         }
     }
   else
     {
       Decision = DECISION_UNKNOWN;
       UndefinedPossibility = 0;
       SellPossibility = 0;
       BuyPossibility = 0;
     }
   return (Decision);
  }
//+------------------------------------------------------------------+
//| Вычисляем статистику вероятностей                                |
//+------------------------------------------------------------------+
int CalculatePossibilityStat()
  {
   int i;
   BuySucPossibilityCount = 0;
   SellSucPossibilityCount = 0;
   UndefinedSucPossibilityCount = 0;
//----
   BuyPossibilityCount = 0;
   SellPossibilityCount = 0;
   UndefinedPossibilityCount = 0;
   // Вычисляем средние значения вероятности
   BuySucPossibilityMid = 0;
   SellSucPossibilityMid = 0;
   UndefinedSucPossibilityMid = 0;
   BuyPossibilityQuality = 0;
   SellPossibilityQuality = 0;
   UndefinedPossibilityQuality = 0;
   PossibilityQuality = 0;
//----
   BuySucPossibilityQuality = 0;
   SellSucPossibilityQuality = 0;
   UndefinedSucPossibilityQuality = 0;
   PossibilitySucQuality = 0;
   for( i = 0 ; i < ModelingBars ; i ++ )
     {
       // Вычисляем решение для данного интервала
       CalculatePossibility (i);
       // Если решение для значения i - продавать         
       if(Decision == DECISION_SELL )
           SellPossibilityQuality ++;           
       // Если решение для значения i - покупать
       if(Decision == DECISION_BUY )
           BuyPossibilityQuality ++;           
       // Если решение для значения i - не определено
       if(Decision == DECISION_UNKNOWN )
           UndefinedPossibilityQuality ++;           
       // Те же оценки для успешных ситуаций                 
         //
       if((BuyPossibility > Spread) || (SellPossibility > Spread) || 
          (UndefinedPossibility > Spread))
         {
           if(Decision == DECISION_SELL)
               SellSucPossibilityQuality ++;                     
           if(Decision == DECISION_BUY)
               BuySucPossibilityQuality ++;
           if(Decision == DECISION_UNKNOWN )
               UndefinedSucPossibilityQuality ++;                   
         }  
       // Вычисляем средние вероятности событий
       // Вероятности покупки
       BuyPossibilityMid *= BuyPossibilityCount;
       BuyPossibilityCount ++;
       BuyPossibilityMid += BuyPossibility;
       if(BuyPossibilityCount != 0 )
           BuyPossibilityMid /= BuyPossibilityCount;
       else
           BuyPossibilityMid = 0;
       // Вероятности продажи
       SellPossibilityMid *= SellPossibilityCount;
       SellPossibilityCount ++;
       SellPossibilityMid += SellPossibility;
       if(SellPossibilityCount != 0 )
           SellPossibilityMid /= SellPossibilityCount;
       else
           SellPossibilityMid = 0;
       // Вероятности неопределенного состояния
       UndefinedPossibilityMid *= UndefinedPossibilityCount;
       UndefinedPossibilityCount ++;
       UndefinedPossibilityMid += UndefinedPossibility;
       if(UndefinedPossibilityCount != 0)
           UndefinedPossibilityMid /= UndefinedPossibilityCount;
       else
           UndefinedPossibilityMid = 0;
       // Вычисляем средние вероятности успешных событий
       if(BuyPossibility > Spread)
         {
           BuySucPossibilityMid *= BuySucPossibilityCount;
           BuySucPossibilityCount ++;
           BuySucPossibilityMid += BuyPossibility;
           if(BuySucPossibilityCount != 0)
               BuySucPossibilityMid /= BuySucPossibilityCount;
           else
               BuySucPossibilityMid = 0;
         }
       if(SellPossibility > Spread)
         {
           SellSucPossibilityMid *= SellSucPossibilityCount;
           SellSucPossibilityCount ++;                 
           SellSucPossibilityMid += SellPossibility;
           if (SellSucPossibilityCount != 0)
              SellSucPossibilityMid /= SellSucPossibilityCount;
              else
                 SellSucPossibilityMid = 0;
         }
       if(UndefinedPossibility > Spread)
         {
           UndefinedSucPossibilityMid *= UndefinedSucPossibilityCount;
           UndefinedSucPossibilityCount ++;                 
           UndefinedSucPossibilityMid += UndefinedPossibility;
           if(UndefinedSucPossibilityCount != 0)
               UndefinedSucPossibilityMid /= UndefinedSucPossibilityCount;
           else
               UndefinedSucPossibilityMid = 0;
         }
     }
   if((UndefinedPossibilityQuality + SellPossibilityQuality + BuyPossibilityQuality)!= 0)
       PossibilityQuality = (SellPossibilityQuality + BuyPossibilityQuality) / 
       (UndefinedPossibilityQuality + SellPossibilityQuality + BuyPossibilityQuality);
   else             
       PossibilityQuality = 0;
   // Качество для успешных ситуаций
   if((UndefinedSucPossibilityQuality + SellSucPossibilityQuality + 
      BuySucPossibilityQuality)!= 0)          
       PossibilitySucQuality = (SellSucPossibilityQuality + BuySucPossibilityQuality) / 
                                (UndefinedSucPossibilityQuality + SellSucPossibilityQuality + 
                                BuySucPossibilityQuality);
   else             
       PossibilitySucQuality = 0;
   return (0);
  }
//+------------------------------------------------------------------+
//| Показываем статистику                                            |
//+------------------------------------------------------------------+
int DisplayStat()
  {
   if(ShowStat == true)
     {
       Print ("SellPossibilityMid*SellPossibilityQuality:", SellPossibilityMid*SellPossibilityQuality);
       Print ("BuyPossibilityMid*BuyPossibilityQuality:", BuyPossibilityMid*BuyPossibilityQuality);
       Print ("UndefinedPossibilityMid*UndefinedPossibilityQuality:", UndefinedPossibilityMid*UndefinedPossibilityQuality);
       Print ("UndefinedSucPossibilityQuality:", UndefinedSucPossibilityQuality);
       Print ("SellSucPossibilityQuality:", SellSucPossibilityQuality);
       Print ("BuySucPossibilityQuality:", BuySucPossibilityQuality);
       Print ("UndefinedPossibilityQuality:", UndefinedPossibilityQuality);
       Print ("SellPossibilityQuality:", SellPossibilityQuality);
       Print ("BuyPossibilityQuality:", BuyPossibilityQuality);
       Print ("UndefinedSucPossibilityMid:", UndefinedSucPossibilityMid);
       Print ("SellSucPossibilityMid:", SellSucPossibilityMid);
       Print ("BuySucPossibilityMid:", BuySucPossibilityMid);
       Print ("UndefinedPossibilityMid:", UndefinedPossibilityMid);
       Print ("SellPossibilityMid:", SellPossibilityMid);
       Print ("BuyPossibilityMid:", BuyPossibilityMid);
     }
   return (0);
  }   // 
//+------------------------------------------------------------------+
//|  Анализируем состояние для принятия решения                      |
//+------------------------------------------------------------------+
int CyberiaDecision()
  {
// Вычисляем статистику периода
   CalculatePossibilityStat();
// Вычисляем вероятности совершения сделок
   CalculatePossibility(0);
   DisplayStat();
   return(Decision);     
  }
//+------------------------------------------------------------------+
//| Вычисляем направление движения рынка                             |
//+------------------------------------------------------------------+
int CalculateDirection()
  {
   DisableSellPipsator = false;
   DisableBuyPipsator = false;
   DisablePipsator = false;
   DisableSell = false;
   DisableBuy = false;
//----
   if(EnableCyberiaLogic == true)           
     {
       AskCyberiaLogic();
     }
   if(EnableMACD == true)
       AskMACD();
   if(EnableMA == true)
       AskMA();
   if(EnableReverceDetector == true)
       ReverceDetector();
   return (0);
  }
//+------------------------------------------------------------------+
//| Если вероятности превышают пороги инвертирования решения         |
//+------------------------------------------------------------------+
int ReverceDetector ()
  {
   if((BuyPossibility > BuyPossibilityMid * ReverceIndex && BuyPossibility != 0 && 
      BuyPossibilityMid != 0) ||(SellPossibility > SellPossibilityMid * ReverceIndex && 
      SellPossibility != 0 && SellPossibilityMid != 0))
     {
       if(DisableSell == true)
           DisableSell = false;
       else
           DisableSell = true;
       if(DisableBuy == true)
           DisableBuy = false;
       else
           DisableBuy = true;
       //----
       if(DisableSellPipsator == true)
           DisableSellPipsator = false;
       else
           DisableSellPipsator = true;
       if(DisableBuyPipsator == true)
           DisableBuyPipsator = false;
       else
           DisableBuyPipsator = true;
     }
   return (0);
  }
//+------------------------------------------------------------------+
//| Опрашиваем логику торговли CyberiaLogic(C)                       |
//+------------------------------------------------------------------+
int AskCyberiaLogic()
  {
   //Устанавливаем блокировки при падениях рынка
   /*DisableBuy = true;
   DisableSell = true;
   DisablePipsator = false;*/
   // Если рынок равномерно движется в заданном направлении
   if(ValuePeriod > ValuePeriodPrev)
     {
       if(SellPossibilityMid*SellPossibilityQuality > BuyPossibilityMid*BuyPossibilityQuality)
         {
           DisableSell = false;
           DisableBuy = true;
           DisableBuyPipsator = true;
           if(SellSucPossibilityMid*SellSucPossibilityQuality > 
              BuySucPossibilityMid*BuySucPossibilityQuality)
             {
               DisableSell = true;  
             }
         }
       if(SellPossibilityMid*SellPossibilityQuality < BuyPossibilityMid*BuyPossibilityQuality)
         {
           DisableSell = true;
           DisableBuy = false;
           DisableSellPipsator = true;
           if(SellSucPossibilityMid*SellSucPossibilityQuality < 
              BuySucPossibilityMid*BuySucPossibilityQuality)
             {
               DisableBuy = true;
             }
         }
     }
   // Если рынок меняет направление - никогда не торгуй против тренда!!!
   if(ValuePeriod < ValuePeriodPrev)
     {
      if(SellPossibilityMid*SellPossibilityQuality > BuyPossibilityMid*BuyPossibilityQuality)
         {
           DisableSell = true;
           DisableBuy = true;
         }
      if(SellPossibilityMid*SellPossibilityQuality < BuyPossibilityMid*BuyPossibilityQuality)
        {
          DisableSell = true;
          DisableBuy = true;
        }
     }
   // Если рынок горизонтальный
   if(SellPossibilityMid*SellPossibilityQuality == BuyPossibilityMid*BuyPossibilityQuality)
     {
       DisableSell = true;
       DisableBuy = true;
       DisablePipsator=false;
     }
   // Блокируем вероятность выхода из рынка
   if(SellPossibility > SellSucPossibilityMid * 2 && SellSucPossibilityMid > 0)
     {
       DisableSell = true;
       DisableSellPipsator = true;
     }
   // Блокируем вероятность выхода из рынка
   if(BuyPossibility > BuySucPossibilityMid * 2 && BuySucPossibilityMid > 0 )
     {
       DisableBuy = true;
       DisableBuyPipsator = true;
     }
   if(ShowDirection == true)
     {
       if(DisableSell == true )
         {
           Print("Продажа заблокирована:", SellPossibilityMid*SellPossibilityQuality);
         }
       else
         {
           Print ("Продажа разрешена:", SellPossibilityMid*SellPossibilityQuality);
         }
       //----
       if(DisableBuy == true )
         {
           Print ("Покупка заблокирована:", BuyPossibilityMid*BuyPossibilityQuality);
         }
       else
         {
           Print ("Покупка разрешена:", BuyPossibilityMid*BuyPossibilityQuality);
         }
     }
   if(ShowDecision == true)
     {
       if(Decision == DECISION_SELL)
           Print("Решение - продавать: ", DecisionValue);
       if(Decision == DECISION_BUY)
           Print("Решение - покупать: ", DecisionValue);
       if(Decision == DECISION_UNKNOWN)
           Print("Решение - неопределенность: ", DecisionValue);
     }
   return (0);
  }
//+------------------------------------------------------------------+
//| Опрашиваем индикатор MA                                          |
//+------------------------------------------------------------------+
int AskMA()
  {
   if(iMA(Symbol(), PERIOD_M1, ValuePeriod, 0 , MODE_EMA, PRICE_CLOSE, 0) > 
      iMA(Symbol(), PERIOD_M1, ValuePeriod, 0 , MODE_EMA, PRICE_CLOSE, 1))        
     {
       DisableSell = true;
       DisableSellPipsator = true;
     }
   if(iMA(Symbol(), PERIOD_M1, ValuePeriod, 0 , MODE_EMA, PRICE_CLOSE, 0) < 
      iMA(Symbol(), PERIOD_M1, ValuePeriod, 0 , MODE_EMA, PRICE_CLOSE, 1))        
     {
       DisableBuy = true;
       DisableBuyPipsator = true;
     }
   return (0);
  }
//+------------------------------------------------------------------+
//| Опрашиваем индикатор MACD                                        |
//+------------------------------------------------------------------+
int AskMACD()
  {
   double DecisionIndex = 0;
   double SellIndex = 0;
   double BuyIndex = 0;
   double BuyVector = 0;
   double SellVector = 0;
   double BuyResult = 0;
   double SellResult = 0;
   DisablePipsator = false;
   DisableSellPipsator = false;
   DisableBuyPipsator = false;
   DisableBuy = false;
   DisableSell = false;
   DisableExitSell = false;
   DisableExitBuy = false;
   // Блокируем ошибки
   for(int i = 0 ; i < MACDLevel ; i ++)
     {
       if(iMACD(Symbol(), MathPow( 2, i) , 2, 4, 1, PRICE_CLOSE, MODE_MAIN, 0) < 
          iMACD(Symbol(), MathPow( 2, i), 2, 4, 1, PRICE_CLOSE, MODE_MAIN, 1) )
         {
           SellIndex += iMACD(Symbol(), MathPow( 2, i), 2, 4, 1, PRICE_CLOSE, MODE_MAIN, 0);
         }
       if(iMACD(Symbol(), MathPow( 2, i), 2, 4, 1, PRICE_CLOSE, MODE_MAIN, 0) > 
          iMACD(Symbol(), MathPow( 2, i), 2, 4, 1, PRICE_CLOSE, MODE_MAIN, 1) )
         {
           BuyIndex += iMACD(Symbol(), MathPow( 2, i), 2, 4, 1, PRICE_CLOSE, MODE_MAIN, 0);
         }

     }
   if(SellIndex> BuyIndex)
     {
       DisableBuy = true;
       DisableBuyPipsator = true;
     }
   if(SellIndex < BuyIndex)
     {
       DisableSell = true;
       DisableSellPipsator = true;
     }
   return (0);
  }
//+------------------------------------------------------------------------+
//| Ловим рыночные ГЭП - (включается непосредственно перед выходом новостей|
//+------------------------------------------------------------------------+
int MoneyTrain()
  {
   if(FoundOpenedOrder == False)
     {
       // Считаем дисперсию
       Disperce = (iHigh ( Symbol(), PERIOD_M1, 0) - iLow ( Symbol(), PERIOD_M1, 0));
       if(Decision == DECISION_SELL)
         {
           // *** Впрыгиваем в паровоз по направлению движения хаоса рынка ***
           if((iClose( Symbol(), PERIOD_M1, 0) - iClose( Symbol(), PERIOD_M1, ValuePeriod)) / 
               MoneyTrainLevel >= SellSucPossibilityMid && SellSucPossibilityMid != 0 && 
               EnableMoneyTrain == true)
             {
               ModeSpread = ModeSpread + 1;
               // Расчет стоп-лосс
               if((Bid - SellSucPossibilityMid*StopLossIndex- ModeSpread * Point) > 
                  (Bid - ModeStopLevel* ModePoint- ModeSpread * Point))
                 {
                   StopLoss = Bid - ModeStopLevel* ModePoint- ModeSpread * Point - Disperce;
                 }
               else
                 {
                   if(SellSucPossibilityMid != 0)
                       StopLoss = Bid - SellSucPossibilityMid*StopLossIndex- 
                       ModeSpread * Point - Disperce;
                   else
                       StopLoss = Bid - ModeStopLevel* ModePoint- ModeSpread * Point - Disperce;
                 }

               if(BlockBuy == true)
                 {
                   return(0);
                 }
               StopLevel = StopLoss;
               Print ("StopLevel:", StopLevel);
               // Блокировка стоплосов
               if(BlockStopLoss == true)
                   StopLoss = 0;                                                                            
               ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, SlipPage, StopLoss, 
                                  TakeProfit,"CyberiaTrader-AI-HB1",0,0,Blue);
               if(ticket > 0)
                 {
                   if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
                       Print("Открыт ордер на покупку: ",OrderOpenPrice());
                 }
               else
                 {
                   Print("Вход в рынок: Ошибка открытия ордера на покупку: ",GetLastError());
                   PrintErrorValues();
                 }
               return (0);
             }
         }              
       if(Decision == DECISION_BUY)
         {
           // *** Впрыгиваем в паровоз по направлению движения хаоса рынка ***
           if((iClose( Symbol(), PERIOD_M1, ValuePeriod) - iClose( Symbol(), PERIOD_M1, 0)) / 
               MoneyTrainLevel >= BuySucPossibilityMid && BuySucPossibilityMid != 0 && 
               EnableMoneyTrain == true)
             {
               ModeSpread = ModeSpread + 1;
               // Расчет стоп-лосс
               if((Ask + BuySucPossibilityMid*StopLossIndex+ ModeSpread* Point) < 
                  (Ask + ModeStopLevel* ModePoint+ ModeSpread * Point))
                 {
                   StopLoss = Ask + ModeStopLevel* ModePoint+ ModeSpread * Point+ Disperce;
                 }
               else
                 {
               if(BuySucPossibilityMid != 0)
                   StopLoss = Ask + BuySucPossibilityMid*StopLossIndex+ ModeSpread*Point + 
                              Disperce;
               else
                   StopLoss = Ask + ModeStopLevel* ModePoint+ ModeSpread * Point+ Disperce;
                 }
               // Если включена ручная блокировка продаж
               if(BlockSell == true)
                 {
                   return(0);
                 }
               StopLevel = StopLoss;
               Print ("StopLevel:", StopLevel);
               // Блокировка стоплосов
               if(BlockStopLoss == true)
                   StopLoss = 0;                                                                      
               ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, SlipPage, StopLoss, 
                                  TakeProfit, "CyberiaTrader-AI-HS1", 0, 0, Green);
               if(ticket > 0)
                 {
                   if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) 
                       Print("Открыт ордер на продажу: ", OrderOpenPrice());
                 }
               else
                 {
                   Print("Вход в рынок: Ошибка открытия ордера на продажу: ",GetLastError());
                   PrintErrorValues();
                 }
               return (0);
             }   
         }            
     }
   return (0);
  }
//+------------------------------------------------------------------+
//| Вход в рынок                                                     |
//+------------------------------------------------------------------+
int EnterMarket()
  {
// Если нет средств, выходим
   if(Lots == 0)
     {
       return (0);
     }
// Входим в рынок если нет команды выхода из рынка
   if(ExitMarket == False)
     {
       // ------- Если нет открытых ордеров - входим в рынок ------------
       if(FoundOpenedOrder == False)
         {
           // Считаем дисперсию
           Disperce = (iHigh(Symbol(), PERIOD_M1, 0) - iLow(Symbol(), PERIOD_M1, 0));
           if(Decision == DECISION_SELL)
             {
               // Если цена покупки больше средней величины покупки на моделируемом интервале
               if(SellPossibility >= SellSucPossibilityMid)
                 {
                   // Расчет стоп-лосс
                   if((Ask + BuySucPossibilityMid*StopLossIndex + ModeSpread * Point) < 
                      (Ask + ModeStopLevel* ModePoint+ ModeSpread * Point))
                     {
                       StopLoss = Ask + ModeStopLevel* ModePoint+ ModeSpread * Point + Disperce;
                     }
                   else
                     {
                       if(BuySucPossibilityMid != 0)
                           StopLoss = Ask + BuySucPossibilityMid*StopLossIndex + 
                                      ModeSpread * Point+ Disperce;
                       else
                           StopLoss = Ask + ModeStopLevel* ModePoint+ ModeSpread * Point + 
                                      Disperce;
                     }
                   // Если включена ручная блокировка продаж
                   if(DisableSell == true)
                     {
                       return(0);
                     }
                   if(BlockSell == true)
                     {
                       return(0);
                     }
                   StopLevel = StopLoss;
                   Print ("StopLevel:", StopLevel);
                   // Блокировка стоплосов
                   if(BlockStopLoss == true)
                       StopLoss = 0;                                                                      
                   ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, SlipPage, StopLoss, 
                            TakeProfit, "CyberiaTrader-AI-LS1", 0, 0, Green);
                   if(ticket > 0)
                     {
                       if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) 
                           Print("Открыт ордер на продажу: ",OrderOpenPrice());
                     }
                   else
                     {
                       Print("Вход в рынок: Ошибка открытия ордера на продажу: ",GetLastError());
                       PrintErrorValues();
                     }
                   // Сохраняем предыдущее значение периода
                   return (0);
                 }
             }
           if(Decision == DECISION_BUY)
             {
               // Если цена покупки больше средней величины покупки на моделируемом интервале
               if(BuyPossibility >= BuySucPossibilityMid)
                 {
                   // Расчет стоп-лосс
                   if((Bid - SellSucPossibilityMid*StopLossIndex- ModeSpread* Point) > 
                      (Bid - ModeStopLevel* ModePoint- ModeSpread* Point))
                     {
                       StopLoss = Bid - ModeStopLevel* ModePoint- ModeSpread* Point - Disperce;
                     }
                   else
                     {
                       if(SellSucPossibilityMid != 0)
                           StopLoss = Bid - SellSucPossibilityMid*StopLossIndex- 
                                      ModeSpread* Point- Disperce;
                       else
                           StopLoss = Bid - ModeStopLevel* ModePoint- ModeSpread* Point- 
                                      Disperce;
                     }
                   // Если включена ручная блокировка покупок
                   if(DisableBuy == true)
                     {
                       return(0);
                     }
                   if(BlockBuy == true)
                     {
                       return(0);
                     }
                   StopLevel = StopLoss;
                   Print("StopLevel:", StopLevel);
                   // Блокировка стоплосов
                   if(BlockStopLoss == true)
                       StopLoss = 0;                                                                      
                   ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, SlipPage, StopLoss, 
                            TakeProfit, "CyberiaTrader-AI-LB1", 0, 0, Blue);
                   if(ticket > 0)
                     {
                      if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) 
                          Print("Открыт ордер на покупку: ",OrderOpenPrice());
                     }
                   else
                     {
                       Print("Вход в рынок: Ошибка открытия ордера на покупку: ",GetLastError());
                       PrintErrorValues();
                     }
                   return (0);
                 }
             }
         }
// ---------------- Конец входа в рынок ----------------------        
     }     
   return (0);
  }   
//+------------------------------------------------------------------+
//| Поиск открытых ордеров                                           |
//+------------------------------------------------------------------+
int FindSymbolOrder()
  {
   FoundOpenedOrder = false;
   total = OrdersTotal();
   for(cnt = 0; cnt < total; cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       // Ищем ордер по нашей валюте
       if(OrderSymbol() == Symbol())
         {
           FoundOpenedOrder = True;
           break;
         }
       else
         {
           StopLevel = 0;
           StopLoss = 0;
         }
     }
   return (0);
  }
//+------------------------------------------------------------------+
//| Пипсатор на минутных интервалах                                  |
//+------------------------------------------------------------------+
int RunPipsator()
  {
   int i = 0;
   FindSymbolOrder();
   // Входим в рынок если нет команды выхода из рынка
   // Считаем дисперсию
   if(Lots == 0)
       return (0);
   Disperce = 0;
   if(ExitMarket == False)
     {
       // ---------- Если нет открытых ордеров - входим в рынок ----------
       if(FoundOpenedOrder == False)
         {
           Disperce = 0;
           DisperceMax = 0;
           // Считаем максимальную дисперсию
           for(i = 0 ; i < ValuePeriod ; i ++)
             {
               Disperce = (iHigh( Symbol(), PERIOD_M1, i + 1) - 
                           iLow( Symbol(), PERIOD_M1, i + 1));                                
               if(Disperce > DisperceMax)
                   DisperceMax = Disperce;                             
             }
           Disperce = DisperceMax  * StopLossIndex;
           if( Disperce == 0 )
             {
               Disperce = ModeStopLevel * Point;
             }
           for(i = 0 ; i < ValuePeriod ; i ++)
             {
               // Пипсатор минутного интервала по продаже
               if((Bid - iClose( Symbol(), PERIOD_M1, i + 1)) > 
                  SellSucPossibilityMid * (i + 1) && 
                  SellSucPossibilityMid != 0 && DisablePipsator == false && 
                  DisableSellPipsator == false)
                 {
                   // Расчет стоп-лосс
                   if((Ask + ModeSpread * Point + Disperce) < 
                      (Ask + ModeStopLevel* ModePoint + ModeSpread * Point))
                     {
                       StopLoss = Ask + ModeStopLevel* ModePoint+ ModeSpread * Point + Point;
                     }
                   else
                     {
                       if(BuySucPossibilityMid != 0)
                           StopLoss = Ask + ModeSpread * Point+ Disperce + Point;
                       else
                         StopLoss = Ask + ModeStopLevel* ModePoint+ ModeSpread * Point + Point;
                     }
                   // Если включена ручная блокировка продаж
                   if(BlockSell == true)
                     {
                       return(0);
                     }
                   // Если включена ручная блокировка продаж
                   if(DisableSell == true)
                     {
                       return(0);
                     }
                   StopLevel = StopLoss;
                   Print("StopLevel:", StopLevel);
                                      // Блокировка стоплосов
                   if(BlockStopLoss == true)
                       StopLoss = 0;
                   ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, SlipPage, StopLoss, 
                            TakeProfit, "CyberiaTrader-AI-PS1", 0, 0, Green);
                   if(ticket > 0)
                     {
                       if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) 
                           Print("Открыт ордер на продажу: ",OrderOpenPrice());
                     }
                   else
                     {
                       Print("Вход в рынок: Ошибка открытия ордера на продажу: ",GetLastError());
                       PrintErrorValues();
                     }
                   return (0);
                 }
               // Пипсатор минутного интервала по покупке
               if((iClose(Symbol(), PERIOD_M1, i + 1) - Bid) > BuySucPossibilityMid *(i + 1) && 
                   BuySucPossibilityMid != 0 && DisablePipsator == False && 
                   DisableBuyPipsator == false)
                 {
                   // Расчет стоп-лосс
                   if((Bid -  ModeSpread * Point - Disperce) > 
                      (Bid - ModeStopLevel* ModePoint- ModeSpread * Point))
                     {
                       StopLoss = Bid - ModeStopLevel* ModePoint- ModeSpread * Point - Point;
                     }
                   else
                     {
                       if(SellSucPossibilityMid != 0)
                           StopLoss = Bid - ModeSpread * Point- Disperce- Point;
                       else
                           StopLoss = Bid - ModeStopLevel* ModePoint- ModeSpread * Point - Point;
                     }
                   // Если включена ручная блокировка 
                   if(DisableBuy == true)
                     {
                       return(0);
                     }
                   if(BlockBuy == true)
                     {
                       return(0);
                     }
                   StopLevel = StopLoss;
                   Print("StopLevel:", StopLevel);
                   // Блокировка стоплосов
                   if(BlockStopLoss == true)
                       StopLoss = 0;                                                                            
                   ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, SlipPage, StopLoss, 
                            TakeProfit, "CyberiaTrader-AI-PB1", 0, 0, Blue);
                   if(ticket > 0)
                     {
                       if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) 
                           Print("Открыт ордер на покупку: ",OrderOpenPrice());
                     }
                   else
                     {
                       Print("Вход в рынок: Ошибка открытия ордера на покупку: ",GetLastError());
                       PrintErrorValues();
                     }
                   return (0);
                 }   
             }// Конец пипсаторного цикла           
         }
     }
   return (0);
  }
//+------------------------------------------------------------------+
//| Выход из рынка                                                   |
//+------------------------------------------------------------------+
int ExitMarket ()
  {
   //FindSymbolOrder();
   // -------------------- Обработка открытых ордеров ----------------
   if(FoundOpenedOrder == True) // Если есть открытый ордер по этой валюте
     {
       if(OrderType()==OP_BUY) // Если найденный ордер на приобретение валюты
         {
           // Закрытие ордера, если он достиг уровня стоп-лосс
           if(Bid <= StopLevel && DisableShadowStopLoss == false && StopLevel != 0)
             {
               OrderClose(OrderTicket(),OrderLots(),Bid ,SlipPage,Violet); // Закрываем ордер
               return(0);
             }
           if(DisableExitBuy == true)
               return (0);
           // Закрытие пипсатора
           if((OrderOpenPrice() < Bid))
             {
               // закрываем ордер
               OrderClose(OrderTicket(), OrderLots(), Bid , SlipPage, Violet); // Закрываем ордер
               return(0);
             }
           // Не выходим из рынка, если имеем хаос, работающий на прибыль
           if((iClose( Symbol(), PERIOD_M1, 0) - iClose( Symbol(), PERIOD_M1, 1)) >= 
               SellSucPossibilityMid * 4 && SellSucPossibilityMid > 0)
               return(0);

           // Закрытие ордера по превышению вероятности успешной продажи
           if((OrderOpenPrice() < Bid) && (Bid - OrderOpenPrice() >= 
              SellSucPossibilityMid) && (SellSucPossibilityMid > 0) )
             {
               // закрываем ордер
               OrderClose(OrderTicket(), OrderLots(), Bid , SlipPage, Violet); // Закрываем ордер
               return(0);
             }
           // Закрытие ордера по превышению вероятности успешной покупки
           if((OrderOpenPrice() < Bid) && (Bid - OrderOpenPrice() >= 
              BuySucPossibilityMid) && (BuySucPossibilityMid > 0) )
             {
               // закрываем ордер
               OrderClose(OrderTicket(), OrderLots(), Bid , SlipPage, Violet); // Закрываем ордер
               return(0);
             }

           if(Decision == DECISION_SELL)
             {
               // Если цена продажи больше цены покупки - значит имеем прибыль (с учетом слипа ордера)
               //if ( OrderOpenPrice() < Bid - SlipPage * Point )
               if( OrderOpenPrice() < Bid)
                 {
                   // Если цена покупки больше средней величины покупки на моделируемом интервале
                   if(SellPossibility >= SellPossibilityMid - Point)
                     {
                       OrderClose(OrderTicket(), OrderLots(), Bid , SlipPage, Violet); // Закрываем ордер
                       return(0);
                     }
                 }
             }
           // Закрытие ордера по индикатору неопределенного сстояния
           if((OrderOpenPrice() < Bid) && (Bid - OrderOpenPrice() >= UndefinedPossibilityMid) )
             {
               // закрываем ордер
               OrderClose(OrderTicket(), OrderLots(), Bid , SlipPage, Violet); // Закрываем ордер
               return(0);
             }

           //Проверяем можно ли его продать в настоящий момент
           if(Decision == DECISION_BUY)
             {
               return(0);
             }
         }
       if(OrderType() == OP_SELL) // Если найденный ордер на приобретение валюты
         {
           // Закрытие ордера, если он достиг уровня стоп-лосс
           if(Ask >= StopLevel && DisableShadowStopLoss == false && StopLevel != 0)
             {
               OrderClose(OrderTicket(), OrderLots(), Ask , SlipPage, Violet); // Закрываем ордер
               return(0);
             }
           if(DisableExitSell == true)
               return (0);
           // Закрытие пипсатора
           if((OrderOpenPrice() > Ask))
             {
               OrderClose(OrderTicket(), OrderLots(), Ask, SlipPage, Violet); // Закрываем ордер
               return(0);
             }


           // Не выходим из рынка, если имеем хаос, работающий на прибыль
           if((iClose( Symbol(), PERIOD_M1, 1) - iClose( Symbol(), PERIOD_M1, 0)) >= BuySucPossibilityMid * 4 && BuySucPossibilityMid > 0)
            return (0);
           // Закрытие ордера по факту превыщения вероятности успешной покупки
           if((OrderOpenPrice() > Ask) && (OrderOpenPrice() - Ask) >= 
               BuySucPossibilityMid && BuySucPossibilityMid > 0)
             {
               // Закрываем ордер
               OrderClose(OrderTicket(), OrderLots(), Ask, SlipPage, Violet); // Закрываем ордер
               return(0);
             }

           // Закрытие ордера по факту превыщения вероятности успешной продажи
           if((OrderOpenPrice() > Ask) && (OrderOpenPrice() - Ask) >= 
              SellSucPossibilityMid && SellSucPossibilityMid > 0)
             {
               // Закрываем ордер
               OrderClose(OrderTicket(), OrderLots(), Ask, SlipPage, Violet); // Закрываем ордер
               return(0);
             }

           if (Decision == DECISION_BUY )
             {
               // Если цена покупки больше цены продажи - значит имеем прибыль (с учетом слипа ордера)
               if(OrderOpenPrice() > Ask)
                 {
                   // Если цена покупки больше средней величины покупки на моделируемом интервале
                   if(BuyPossibility >= BuyPossibilityMid - Point)
                     {
                       OrderClose(OrderTicket(), OrderLots(), Ask, SlipPage, Violet); // Закрываем ордер
                       return(0);
                     }
                 }

             }
           // Закрытие ордера по индикатору неопределенного сстояния
           if((OrderOpenPrice() > Ask) && (OrderOpenPrice() - Ask) >= UndefinedPossibilityMid)
             {
               // Закрываем ордер
               OrderClose(OrderTicket(), OrderLots(), Ask, SlipPage, Violet); // Закрываем ордер
               return(0);
             }
           //Проверяем можно ли его продать в настоящий момент
           if(Decision == DECISION_SELL)
             {
               return (0);
             }

         }
     }
 // --------------------- Конец обработки открытых ордеров -----------
 //  ValuePeriodPrev = ValuePeriod;
   return (0);
  }   
//+--------------------------------------------------------------------------+
//| Сохраняем значения ставок и периода моделирования для следующей итеррации|
//+--------------------------------------------------------------------------+
int SaveStat()
  {
   BidPrev = Bid;
   AskPrev = Ask;
   ValuePeriodPrev = ValuePeriod;
   return (0);
  }
//+------------------------------------------------------------------+
//| Трейдинг                                                         |
//+------------------------------------------------------------------+
int Trade ()
  {
   // Начинаем торговать
   // Ищем открытые ордера
   FindSymbolOrder();
   CalculateDirection();
   AutoStopLossIndex();
//---- Если открытых ордеров по симаолу нет, возможен вход в рынок
//---- Внимание - важен именно этот порядок рассмотрения технологий входа в рынок (MoneyTrain, LogicTrading, Pipsator)
   if(FoundOpenedOrder == false)
     {
       if(EnableMoneyTrain == true)
           MoneyTrain();
       if(EnableLogicTrading == true)
           EnterMarket();
       if(DisablePipsator == false)
           RunPipsator();           
     }
   else
     {
       ExitMarket();
     }
//---- Конец обработки входа/выхода из рынка
   return(0);
  }
//+------------------------------------------------------------------+
//| Выводить в логах статус счета                                    |
//+------------------------------------------------------------------+
int AccountStatus()
  {
   if(ShowAccountStatus == True )
     {
       Print ("AccountBalance:", AccountBalance());
       Print ("AccountCompany:", AccountCompany());
       Print ("AccountCredit:", AccountCredit());
       Print ("AccountCurrency:", AccountCurrency());
       Print ("AccountEquity:", AccountEquity());
       Print ("AccountFreeMargin:", AccountFreeMargin());
       Print ("AccountLeverage:", AccountLeverage());
       Print ("AccountMargin:", AccountMargin());
       Print ("AccountName:", AccountName());
       Print ("AccountNumber:", AccountNumber());
       Print ("AccountProfit:", AccountProfit());
     }    
   return ( 0 );
  }
//+------------------------------------------------------------------+
//| Самая важная функция - выбор периода моделирования               |
//+------------------------------------------------------------------+
int FindSuitablePeriod()
  {
   double SuitablePeriodQuality = -1 *ValuesPeriodCountMax*ValuesPeriodCountMax;
   double SuitablePeriod = 0;
   int i; // Переменная для анализа периодов
// Количество анализируемых периодов. i - размер периода
   for(i = 0 ; i < ValuesPeriodCountMax ; i ++ )
     {
       ValuePeriod = i + 1;
      // Значение подобрано опытным путем и как ни странно оно совпало с числом в теории эллиота
       ValuesPeriodCount = ValuePeriod * 5; 
       init();           
       CalculatePossibilityStat ();
       if(PossibilitySucQuality > SuitablePeriodQuality)
         {
           SuitablePeriodQuality = PossibilitySucQuality;
           //Print ("PossibilitySucQuality:", PossibilitySucQuality:);
           SuitablePeriod = i + 1;
         }
     }
   ValuePeriod = SuitablePeriod;
   init();
   // Выводить период моделирования
   if(ShowSuitablePeriod == True)
     {
       Print("Период моделирования:", SuitablePeriod, " минут с вероятностью:", 
       SuitablePeriodQuality );
     }
   return(SuitablePeriod);
  }
//+------------------------------------------------------------------+
//|Автоматическая установка уровня стоп-лосс                         |
//+------------------------------------------------------------------+
int AutoStopLossIndex()
  {
   if(AutoStopLossIndex == true)
     {
       StopLossIndex = ModeSpread;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|Вывод ошибок при входе в рынок                                    |
//+------------------------------------------------------------------+
int PrintErrorValues()
  {
   Print("ErrorValues:Symbol=", Symbol(),",Lots=",Lots, ",Bid=", Bid, ",Ask=", Ask,
         ",SlipPage=", SlipPage, "StopLoss=",StopLoss,",TakeProfit=", TakeProfit);
   return (0);
  }   
//+------------------------------------------------------------------+
//| expert start function (Трейдинг)                                 |
//+------------------------------------------------------------------+
int start()
  {
   GetMarketInfo();
   CyberiaLots();
   CalculateSpread();
   FindSuitablePeriod();
   CyberiaDecision();
   Trade();
   SaveStat();
   return(0);
  }


