//+------------------------------------------------------------------+
//|                               50 peHta6 tynnel MA medFIN.mq4.mq4 |
//|                                       Copyright © 2009, costy_   |
//|                                                 jena@deneg.net   |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, costy_"
#property link      "jena@deneg.net"
 
#define STUPID 0x60BE45
 
extern string     Lots_Desc         =  "Если 0 применяется динамический лот";
extern double     Lots              =  1;
 
extern string     RiskPercentage_Desc =  "Настройка для динамического лота -- % риска. Если 0 используется минимальный доступный размер лота, если Lots > 0 эта настройка игнорируется";
extern int        RiskPercentage    =  0;
 
extern int        Slippage          =  25;
 
extern string     Target_Desc       =  "Тейк профит, если 0 профит не выставляется 90-200";
extern int        Target            =  0 ;
 
extern string     Loss_Desc         =  "Стоп лосс, если 0 лосс не выставляется 30-80";
extern int        Loss              =  0;
 
extern string     MA_DESC           =  "Периоды МА ";
extern int        F55        = 55;
extern int        shift        = 13;
extern int        METHOD_MA=1;
extern int        timeframe =0;

extern string     Торгуемый_диапазон  =  "В часах(но -1час т.к. ma shift 1)";
extern int        окончание=20;
extern int        начало=8;

 
extern string     MaxOrders_Desc    =  "если 0 количество одновременно открытых позиций не ограничивается 1-3";
extern int        MaxOrders         =  1;
 
double LotsToBid;
string symbol;
  bool была_покупка?;
  bool была_продажа?;
 //---------------------------
  int k=1;

 // импортируем библиотеку функций для различных видов трейлинга
// пример вызова функций - см. ближе к концу кода
#import "TrailingAll.ex4"
   void TrailingByShadows(int ticket,int tmfrm,int bars_n, int indent,bool trlinloss);
   void TrailingByFractals(int ticket,int tmfrm,int frktl_bars,int indent,bool trlinloss);   
   void TrailingStairs(int ticket,int trldistance,int trlstep);   
   void TrailingUdavka(int ticket,int trl_dist_1,int level_1,int trl_dist_2,int level_2,int trl_dist_3);
   void TrailingByTime(int ticket,int interval,int trlstep,bool trlinloss);   
   void TrailingByATR(int ticket,int atr_timeframe,int atr1_period,int atr1_shift,int atr2_period,int atr2_shift,double coeff,bool trlinloss);
   void TrailingRatchetB(int ticket,int pf_level_1,int pf_level_2,int pf_level_3,int ls_level_1,int ls_level_2,int ls_level_3,bool trlinloss);  
   void TrailingByPriceChannel(int iTicket,int iBars_n,int iIndent);
   void TrailingByMA(int iTicket,int iTmFrme,int iMAPeriod,int iMAShift,int MAMethod,int iApplPrice,int iShift,int iIndent);
   void TrailingFiftyFifty(int iTicket,int iTmFrme,double dCoeff,bool bTrlinloss); 
   void KillLoss(int iTicket,double dSpeedCoeff);   
#import
 
 
  
 //--------------------------------------------------------------
// закрытие покупок
void CloseBuys(int MagicNumber, int Slippage)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_BUY)
      {
         if(OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, Blue))
         {
            i--;
         }
         RefreshRates();
      }
   }
}
 //-----------------------------------------------------
 
// закрытие продаж
void CloseSells(int MagicNumber, int Slippage)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_SELL)
      {
         if (OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, Red))
         {
            i--;
         }
         RefreshRates();
      }
   }
}
 //----------------------------------------------------
 
// подсчет кол-ва открытых позиций
int GetOrdersCount(int MagicNumber, int Type)
{
   int count = 0;
   
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == Type)
      {
         count++;
      }
   }
   
   return (count);
}
 //-------------------------------------------------------
 
// Вычисление динамического лота
double GetLotsToBid(int RiskPercentage)
{
   double margin = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
   double minLot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
   double step = MarketInfo(Symbol(), MODE_LOTSTEP);
   double account = AccountFreeMargin();
   
   double percentage = account*RiskPercentage/100;
   
   double lots = MathRound(percentage/margin/step)*step;
   
   if(lots < minLot)
   {
      lots = minLot;
   }
   
   if(lots > maxLot)
   {
      lots = maxLot;
   }
 
   return (lots);
}
 //----------------------------------------------------
 
// покупка
void OpenBuy()
{
   double TP = 0;
   if (Target > 0)
   {
      TP = Bid + Target*Point;
   }
 
   double SL = 0;
   if (Loss > 0)
   {
      SL = Bid - Loss*Point;
   }
   
   if (Lots == 0) LotsToBid = GetLotsToBid(RiskPercentage);
    
   
   OrderSend(Symbol(), OP_BUY, LotsToBid, Ask, Slippage, SL, TP, NULL, STUPID, 0, Blue);
}
 //----------------------------------------------------
 
// продажа
void OpenSell()
{
   double TP = 0;
   if (Target > 0)
   {
      TP = Ask - Target*Point;
   }
 
   double SL = 0;
   if (Loss > 0)
   {
      SL = Ask + Loss*Point;
   }
   
   if (Lots == 0) LotsToBid = GetLotsToBid(RiskPercentage);
   
   OrderSend(Symbol(), OP_SELL, LotsToBid, Bid, Slippage,  SL, TP, NULL, STUPID, 0, Red);
}
 //------------------------------------------------------
 // проверка условий торговли и управление позициями
void Check()
{
  int X=1*k;
  int Y=13*k;
 
 //--------------------------------------------------------------
   double ma1         = iMA(symbol, timeframe, F55, 0, METHOD_MA, PRICE_MEDIAN,  shift);
   double ma0         = iMA(symbol, timeframe, F55, 0, METHOD_MA, PRICE_MEDIAN,  1);



    if(Hour()<окончание&&Hour()>начало){
              if(ma0>ma1&&была_покупка?<1 ){CheckBuy();была_покупка?=2;была_продажа?=0;
                        }                                 //цена находится ВЫШЕ канала buy

              if(ma0<ma1&&была_продажа?<1 ){CheckSell();была_продажа?=2;была_покупка?=0;
                       }    }                             //цена находится НИЖЕ канала sell
  
     

}
void PrintComments() {
}
   
 //--------------------------------------------------------------
 //--------------------------------------------------------------
   
void CheckBuy()
{
      if (GetOrdersCount(STUPID, OP_SELL) > 0)
      {
         CloseSells(STUPID, Slippage);
        
      }
      if (GetOrdersCount(STUPID, OP_BUY) < MaxOrders || MaxOrders == 0) 
      {
         OpenBuy();
      }}
void CheckSell()
{
      if (GetOrdersCount(STUPID, OP_BUY) > 0)
      {
         CloseBuys(STUPID, Slippage);
      }
      if (GetOrdersCount(STUPID, OP_SELL) < MaxOrders || MaxOrders == 0) 
      {
         OpenSell();
      }}
 //--------------------------------------------------------------
 //--------------------------------------------------------------
int init()
{

   LotsToBid = Lots;
   symbol = Symbol();
}
 //--------------------------------------------------------------
int start()
{

PrintComments();
   // Check for open new orders and close current ones
   Check();
   for (int i=0;i<OrdersTotal();i++)
         {
         if (!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) break;
         if (OrderMagicNumber()!=STUPID || OrderSymbol()!=Symbol()) continue;
         if ((OrderType()==OP_BUY) || (OrderType()==OP_SELL))
            {
            // !!! ПРИМЕР ВЫЗОВА ФУНКЦИЙ ТРЕЙЛИНГА !!!
            // !!! ПРИМЕР ВЫЗОВА ФУНКЦИЙ ТРЕЙЛИНГА !!!
            // !!! ПРИМЕР ВЫЗОВА ФУНКЦИЙ ТРЕЙЛИНГА !!!
            // среди возможных вариантов мы, допустим, выбрали трейлинг по фракталам. Трейлингуем по 
            // 5-барным фракталам на дневках, с отступом от экстремума в 3 п., в зоне лоссов не тралим
            //TrailingByFractals(OrderTicket(),5,10,8,false);
            // (как видим, достаточно предварительно выбрать ордер OrderSelect() и вызвать функцию, 
            // передав ей тикет позиции и определив необходимые параметры).
            // При желании Вы можете закоментировать данный вид трейлинга и подключить любой другой 
            // или даже "сконструировать" из них более или менее сложную конструкцию.    
//--------------------------------------------------------------


//--------------------------------------------------------------
//| 1) ТРЕЙЛИНГ ПО ФРАКТАЛАМ                                            |
//| TrailingByFractals(int ticket,int tmfrm,int frktl_bars,int indent,bool trlinloss)

//| Параметры:
//| ticket - уникальный порядковый номер ордера(выбранный перед вызовом функции с помощью OrderSelect());
//| tmfrm - таймфрейм, по барам которого осуществляется трейлинг (варианты - 1, 5, 15, 30, 60, 240, 1440, 10080, 43200);
//| bars_n - количество баров в составе фрактала (не меньше 3);
//| indent - отступ (пунктов) от экстремума последнего фрактала, на котором будет размещен стоплосс (не меньше 0);
//| trlinloss - указатель того, следует ли передвигать стоплосс на "лоссовом" участке, т.е. в интервале между 
//|    начальным стоплоссом и курсом открытия (true - тралим, false - трейлинг начинается только при условии, 
//|    что новый стоплосс "лучше" курса открытия, "в профите").

//TrailingByFractals(OrderTicket(),5,8,8,false);
            
//--------------------------------------------------------------
//| 2) ТРЕЙЛИНГ ПО ТЕНЯМ N СВЕЧЕЙ                                       |
//|  TrailingByShadows(int ticket,int tmfrm,int bars_n, int indent,bool trlinloss)

//| Параметры:
//| ticket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| tmfrm - таймфрейм, по барам которого осуществляется трейлинг (варианты - 1, 5, 15, 30, 60, 240, 1440, 10080, 43200);
//| bars_n - количество баров, которые используются для определения уровня стоплосса (не меньше 1);
//| indent - отступ (пунктов) от выбранного high/low, на котором будет размещен стоплосс (не меньше 0);
//| trlinloss - указатель того, следует ли передвигать стоплосс на "лоссовом" участке, т.е. 
//|   в интервале между начальным стоплоссом и курсом открытия (true - тралим, false - трейлинг начинается только при условии, 
//|   что новый стоплосс "лучше" курса открытия, "в профите").          
   
//TrailingByShadows(OrderTicket(),5,10,4,false);      

//--------------------------------------------------------------
//| 3) ТРЕЙЛИНГ СТАНДАРТНЫЙ-СТУПЕНЧАСТЫЙ   
//| TrailingStairs(int ticket,int trldistance,int trlstep)

//| Параметры:
//| ticket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| trldistance - расстояние от текущего курса (пунктов), на котором "тралим" (не меньше MarketInfo(Symbol(),MODE_STOPLEVEL));
//| trlstep - "шаг" изменения стоплосса (пунктов) (не меньше 1).

//TrailingStairs(OrderTicket(),30,15);

//--------------------------------------------------------------
//| 4) ТРЕЙЛИНГ СТАНДАРТНЫЙ-"УДАВКА" 
//| TrailingUdavka(int ticket,int trl_dist_1,int level_1,int trl_dist_2,int level_2,int trl_dist_3)

//| Параметры:
//| ticket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| trl_dist_1 - исходное расстояние трейлинга (пунктов) (не меньше MarketInfo(Symbol(),MODE_STOPLEVEL), 
//|            больше trl_dist_2 и trl_dist_3);
//| level_1 - уровень профита (пунктов), при достижении которого дистанция трейлинга будет сокращена 
//|            с trl_dist_1 до trl_dist_2 (меньше level_2; больше trl_dist_1);
//| trl dist_2 - расстояние трейлинга (пунктов) после достижения курсом уровня профита в level_1 пунктов 
//|           (не меньше MarketInfo(Symbol(),MODE_STOPLEVEL));
//| level_2 - уровень профита (пунктов), при достижении которого дистанция трейлинга будет сокращена 
//|            с trl_dist_2 до trl_dist_3 пунктов (больше trl_dist_1 и больше level_1);
//| trl dist_3 - расстояние трейлинга (пунктов) после достижения курсом уровня профита в level_2 пунктов (
//|            не меньше MarketInfo(Symbol(),MODE_STOPLEVEL)).

//TrailingUdavka(OrderTicket(),int trl_dist_1,int level_1,int trl_dist_2,int level_2,int trl_dist_3);

//--------------------------------------------------------------
//| 5) ТРЕЙЛИНГ ПО ВРЕМЕНИ 
//| TrailingByTime(int ticket,int interval,int trlstep,bool trlinloss) 
  
//| Параметры:
//| ticket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| interval - количество целых минут с момента открытия позиции, по истечению которых пытаемся переместить стоплосс 
//|           на шаг trlstep пунктов;
//| trlstep - шаг (пунктов), на который пытаемся перемещать стоплосс через каждые interval минут;
//| trlinloss - в данном случае если trlinloss==true, то тралим от стоплосса, иначе от курса открытия 
//|           (если стоплосс не был установлен, ==0, тралим всегда от курса открытия).
 
//TrailingByTime(OrderTicket(),30,10,true);

//--------------------------------------------------------------
//| 6) ТРЕЙЛИНГ ПО ATR (Average True Range, Средний истинный диапазон)
//| TrailingByATR(int ticket,int atr_timeframe,int atr1_period,int atr1_shift,int atr2_period,int atr2_shift,double coeff,bool trlinloss)
   
//| Параметры:
//| ticket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| atr_timeframe - таймфрейм, на котором рассчитываем значение ATR (варианты - 1, 5, 15, 30, 60, 240, 1440, 10080, 43200);
//| atr1_period - период первого ATR (больше 0; может быть равен atr2_period, но лучше отличен от последнего, почему - см. выше);
//| atr1_shift - для первого ATR сдвиг "окна", в котором рассчитывается значение ATR, относительно текущего бара на указанное 
//|              количество баров назад (неотрицательное целое число);
//| atr2_period - период второго ATR (больше 0);
//| atr2_shift - для второго ATR сдвиг "окна", в котором рассчитывается значение ATR, относительно текущего бара на указанное 
//|              количество баров назад (неотрицательное целое число);
//| coeff - стоплосс считаем как ATR*coeff, т.е. это коэффициент, определяющий, на расстоянии скольких ATR от текущего курса 
//|         следует разместить стоплосс;
//| trlinloss - указатель того, следует ли передвигать стоплосс на "лоссовом" участке, т.е. в интервале между начальным 
//|             стоплоссом и курсом открытия (true - тралим, false - трейлинг начинается только при условии, 
//|             что новый стоплосс "лучше" курса открытия, "в профите").   
   
//TrailingByATR(OrderTicket(),int atr_timeframe,int atr1_period,int atr1_shift,int atr2_period,int atr2_shift,double coeff,bool trlinloss);
   
//--------------------------------------------------------------
//| 7) ТРЕЙЛИНГ RATCHET БАРИШПОЛЬЦА
//| TrailingRatchetB(int ticket,int pf_level_1,int pf_level_2,int pf_level_3,int ls_level_1,int ls_level_2,int ls_level_3,bool trlinloss)   
   
//| Параметры:
//| ticket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| pf_level_1 - уровень профита (пунктов), при котором стоплосс переносим в безубыток + 1 пункт;
//| pf_level_2 - уровень профита (пунктов), при котором стоплосс переносим с +1 на расстояние pf_level_1 пунктов от курса открытия;
//| pf_level_3 - уровень профита (пунктов), при котором стоплосс переносим с pf_level_1 на pf_level_2 пунктов от курса открытия 
//|              (на этом действия функции заканчиваются);
//| ls_level_1 - расстояние от курса открытия в сторону "лосса", на котором будет установлен стоплосс при достижении профитом 
//|              позиции +1 (т.е. при +1 стоплосс будет поджат на ls_level_1);
//| ls_level_2 - расстояние от курса открытия в "лоссе", на котором будет установлен стоплосс при условии, что курс сначала 
//|              опускался ниже ls_level_1, а потом поднялся выше (т.е. имели лосс, но он начал уменьшаться - 
//|              не допустим его повторного увеличения);
//| ls_level_3 - расстояние от курса открытия "минусе", на котором будет установлен стоплосс при условии, что курс снижался 
//|              ниже ls_level_2, а потом поднялся выше;
//| trlinloss - указатель того, следует ли передвигать стоплосс на "лоссовом" участке, т.е. в интервале между начальным 
//|             стоплоссом и курсом открытия (true - тралим, false - трейлинг начинается только при условии, 
//|             что новый стоплосс "лучше" курса открытия, "в профите").   
   
//TrailingRatchetB(OrderTicket(),80,2000,1500,50,30,20,false);   
   
//--------------------------------------------------------------
//| 8) ТРЕЙЛИНГ ПО ЦЕНВОМУ КАНАЛУ    
//| TrailingByPriceChannel(int iTicket,int iBars_n,int iIndent)

//| Параметры:
//| iTicket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| iBars_n - период канала (кол-во баров, среди котрых ищем наибольший хай и наименьший лоу - верхнюю и нижнюю 
//|           границы канала соответственно);
//| iIndent - отступ (пунктов), с которым устанавливаем стоплосс от границы канала.

//TrailingByPriceChannel(OrderTicket(),16,5);
 
//--------------------------------------------------------------
//|  9) ТРЕЙЛИНГ ПО СКОЛЬЗЯЩЕМУ СРЕДНЕМУ
//|  TrailingByMA(OrderTicket(),int iTmFrme,int iMAPeriod,int iMAShift,int MAMethod,int iApplPrice,int iShift,int iIndent)  
   
//| Параметры:
//| iTicket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| iTmFrme - период чарта, на котором будет расчитываться мувинг; допустимые варианты ввода: 1 (M1), 5 (M5), 15 (M15), 30 (M30), 60 (H1), 240 (H4), 1440 (D), 10080 (W), 43200 (MN);
//| iMAPeriod - период усреднения для вычисления скользящего среднего;
//| iMAShift - сдвиг индикатора относительно ценового графика;
//| iMAMethod - метод усреднения; допустимые варианты ввода: 0 (MODE_SMA), 1 (MODE_EMA), 2 (MODE_SMMA), 3 (MODE_LWMA);
//| iApplPrice - используемая цена; варианты ввода: 0 (PRICE_CLOSE), 1 (PRICE_OPEN), 2 (PRICE_HIGH), 3 (PRICE_LOW), 4 (PRICE_MEDIAN), 5 (PRICE_TYPICAL), 6 (PRICE_WEIGHTED);
//| iShift - сдвиг относительно текущего бара на указанное количество периодов назад;
//| iIndent - отступ (пунктов) от значения среднего при размещении стоплосса.

//    Допустимые варианты ввода:   
//    iTmFrme:    1 (M1), 5 (M5), 15 (M15), 30 (M30), 60 (H1), 240 (H4), 1440 (D), 10080 (W), 43200 (MN);
//    iMAPeriod:  2-infinity, целые числа; 
//    iMAShift:   целые положительные или отрицательные числа, а также 0;
//    MAMethod:   0 (MODE_SMA), 1 (MODE_EMA), 2 (MODE_SMMA), 3 (MODE_LWMA);
//    iApplPrice: 0 (PRICE_CLOSE), 1 (PRICE_OPEN), 2 (PRICE_HIGH), 3 (PRICE_LOW), 4 (PRICE_MEDIAN), 5 (PRICE_TYPICAL), 6 (PRICE_WEIGHTED)
//    iShift:     0-Bars, целые числа;
//    iIndent:    0-infinity, целые числа;
  
//TrailingByMA(OrderTicket(),5,21,0,0,0,0,0);

//--------------------------------------------------------------
//|  10) ТРЕЙЛИНГ "ПОЛОВИНЯЩИЙ" 
//|  TrailingFiftyFifty(int iTicket,int iTmFrme,double dCoeff,bool bTrlinloss)   

//| Параметры:
//| iTicket - уникальный порядковый номер ордера (выбранный перед вызовом функции с помощью OrderSelect());
//| iTmFrme - период чарта, по барам которого будет осуществляться трейлинг; допустимые варианты ввода: 1 (M1), 5 (M5), 15 (M15), 30 (M30), 60 (H1), 240 (H4), 1440 (D), 10080 (W), 43200 (MN);

//| dCoeff - коэффициент, определяющий то, в сколько раз будет сокращено расстояние между курсом на момент закрытия бара 
//|          и текущим стоплоссом;
//| bTrlinloss - указатель того, следует ли осуществлять трейлинг на лоссовом участке.
       
//TrailingFiftyFifty(OrderTicket(),15,2,false)  ;

//--------------------------------------------------------------

 
            }
         }
   return(0);
}