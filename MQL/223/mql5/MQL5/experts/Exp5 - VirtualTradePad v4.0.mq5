//+------------------------------------------------------------------+
//|                                         Copyright 2009, Expforex |
//|                                        http://www.expforex.at.ua |
//+------------------------------------------------------------------+
#property copyright "2008-2010, Expforex"
#property link      "http://www.expforex.at.ua"
#property version   "4.0"

#property description "VirtualTradePad for MT5 јвторска€ разработка. —истема дл€ работы на рынке Forex одним нажатием кнопки. ƒл€ конкурса \"Ћучша€ панель управлени€\" "
/*
»стори€ версий: 

V1.0:
-¬ Ёксперте реалзиованы все скрипты по закрытию ордеров.
-¬ данной версии присутствуют такие кнопки:
--CloseALL
--CloseALLSTOP
--CloseBUY
--CloseBUYSTOP
--CloseSELL
--CloseSELLSTOP
--CloseLOSS
--ClosePROFIT
-ѕри нажатии кнопки, все позиции/ордера закроютс€/удал€тс€, по всем инструментам.
 
V2.0:
-ƒобавил кнопки открыти€ ордеров BUY SELL 
-ƒобавил параметры “ейкпрофит, —топлос, Ћот
-≈сли StopLosss или TakeProfits - указаны минимальны и неравны 0, то система автоматически устанавливает значени€ на минимальное рассто€ние.
-ƒобавил SymbolClose - булент на закрытие всех символов, либо же по текущему.
-ƒобавил возможность ставить отложенные ордера BUYSTOP b SELLSTOP
-ѕараметр OrderPricePip - рассто€ние дл€ отложки
-≈сли OrderPricePips неравно 0 и меньше минимального стопа, параметру присваиваетс€ минимальный стоп


V2.2:
-»справлены название кнопок на удаление отложенных ордеров
-ƒобавлено поле изменени€ Lots в реальном режиме.
--≈сли Lots - пытаетесь выставить меньше минимального - значение становитс€ минимально возможным на этом серевере
-ƒобавлено поле изменени€ TakeProfit в реальном режиме.
--≈сли TakeProfit - пытаетесь выставить меньше минимального - значение становитс€ минимально возможным на этом серевере
--≈сли TakeProfit больше 0 , то окно значени€ тейкпрофита становитс€ зеленым - т.е. включенным.
-ƒобавлено поле изменени€ StopLoss в реальном режиме.
--≈сли StopLoss - пытаетесь выставить меньше минимального - значение становитс€ минимально возможным на этом серевере
--≈сли StopLoss больше 0 , то окно значени€ тейкпрофита становитс€ зеленым - т.е. включенным.
-ƒобавлено поле изменени€ OrderPricePip в реальном режиме.
--≈сли OrderPricePip - пытаетесь выставить меньше минимального - значение становитс€ минимально возможным на этом серевере
--≈сли OrderPricePip больше 0 , то окно значени€ тейкпрофита становитс€ зеленым - т.е. включенным.

V2.3
-ƒобавил возможность пр€тать панель с помощью кнопки : ¬ключить/¬ыключить VisualTrade 
-ѕерестроил кнопки открыти€ ордеров под параметры открыти€.

v2.6 
-–азработка и доработка д€л конкурса на mql5.com
-ѕередвинул кнопки
-»справил ошибку открыти€ позиций
-»справил ошибку закрыти€ позиций еслибольше 1 а также удалении в пор€дке убывани€
-ƒобавил траллингстоп



v 3.4 
-ѕолностью помен€л дизайн
-ƒобавил график показани€ цен Ѕид и јск
-ƒобавил выбор валютной пары дл€ работы с ней
-”брал работу с отложенниками пока-что
-ƒобавил »нформацию по открытой позици дл€ выбранной пары
-ƒобавил проверку соединени€ с сервером
-ƒобавил последнее врем€ сервера
-ƒобавил панель управлени€ траллинга
-ѕри старте - позиции сразу провер€ютс€ и соответствующие кнопки соответсвенно раскрашиваютс€ 
-»справил все предствалени€ данных, чтобы не высвечивало Warningov
-ѕривел в более пон€тный вид весь код, сделал описание всех функций внутри кода
-ƒобавил 4 вкладки: 
Pos(–абота с позици€ыми)
: Ѕлок лоты
: Ѕлок —топлосс
: Ѕлок “ейкпрофит
: Ѕлок “раллингстоп
: Ѕлок открыти€ BUY SELL
: Ѕлок закрыти€ 
: Ѕлок Reverse - перевернуть позицию по паре
Ord(–абота с отложенниками)
: «начение ѕунктов от текуцщей цены
: 4 кнопки на 4 отложенника: BUYSTOP SELLSTOP BUYLIMIT SELLLIMIT
: —топлосс “ейкпрофит
: Ћот
:  нопка ”далить все отложенники

INFO(ѕоказывает информацию о выбранной валютной паре + показывает состо€ние баров по всем стандартным “‘ в виде стрелок)
: —пред
: —топ 
: ѕипс
: Ћќ“ ћин
: Ћќ“ ћах
: Ћќ“ шаг
: —воп длинный и короткий
: ѕанель движени€ валюты на разных “‘

OTHER(ƒобавил сюда полезные функции)
: «акрытие всех позиций при достижении определенного парамтера: ѕроцент от баланса / ѕункты / ¬алюта депозита
: √ридер дл€ существующей позиции, выставл€ет сетку отложенных ордеров от существующей позиции.

+ѕри нажатии на “‘ в вкладке INFO  - —боку открываетс€ график выбранной пары и “‘, при повторном нажатии график закрываетс€

Ind
: ¬кладка показывающа€ сигналы с 11 индикаторов, описанных в статье 20 торговых тактик http://www.mql5.com/ru/articles/130 

*/

//+------------------------------------------------------------------+
//| ¬нешние    переменные                                            |
//+------------------------------------------------------------------+
input string Symbol1="EURUSD";
input string Symbol2="EURGBP";
input string Symbol3="EURCHF";
input string Symbol4="USDCHF";
input string Symbol5="USDJPY";
input string Symbol6="GBPUSD";

//+------------------------------------------------------------------+
//| √лобальные переменные                                            |
//+------------------------------------------------------------------+

bool SymbolClose=true;
double lot=0.1;
int TakeProfits=0;
int StopLosss=0;
int OrderPricePips=20;
int Profit;
int Loss;
int TypeofClose;
int OrderPricePip=0;
int GRIDNUMBER=1;
int TakeProfit=0;
int StopLoss=0;
double lotss=0;
bool SymbolCloseS;
int OrderLevelStop;

int TFToInd;
ushort     TRAILING_STOP  =      0; // ”ровень трейлинг стопа
ushort     TRAILING_STEP  =      1; // Ўаг перемещени€ трейлинг стопа
ushort     SLIPPAGE=2; // ѕроскальзывание

int window=0;

//+------------------------------------------------------------------+
//| ќбъ€вим переменные дл€ хранени€ настроек индикаторов             |
//+------------------------------------------------------------------+
//--- input parameters Moving Average
int                periodma1=8;
int                periodma2=16;
ENUM_MA_METHOD     MAmethod=MODE_SMA;
ENUM_APPLIED_PRICE MAprice=PRICE_CLOSE;
//--- input parameters MACD
int                FastMACD=12;
int                SlowMACD=26;
int                MACDSMA=9;
ENUM_APPLIED_PRICE MACDprice=PRICE_CLOSE;
//--- input parameters Price Channel
int                PCPeriod=22;
//--- input parameters Adaptive Channel ADX
int                ADXPeriod=14;
//--- input parameters Stochastic Oscillator
int                SOPeriodK=5;
int                SOPeriodD=3;
int                SOslowing=3;
ENUM_MA_METHOD     SOmethod=MODE_SMA;
ENUM_STO_PRICE     SOpricefield=STO_LOWHIGH;
//--- input parameters RSI
int                RSIPeriod=14;
ENUM_APPLIED_PRICE RSIprice=PRICE_CLOSE;
//--- input parameters CCI
int                CCIPeriod=14;
ENUM_APPLIED_PRICE CCIprice=PRICE_TYPICAL;
//--- input parameters WPR
int                WPRPeriod=14;
//--- input parameters Bollinger Bands
int                BBPeriod=20;
double             BBdeviation=2.0;
ENUM_APPLIED_PRICE BBprice=PRICE_CLOSE;
//--- input parameters Standard Deviation Channel
int                SDCPeriod=14;
double             SDCdeviation=2.0;
ENUM_APPLIED_PRICE SDCprice=PRICE_CLOSE;
ENUM_MA_METHOD     SDCmethod=MODE_SMA;
//--- input parameters Price Channel 2
int                PC2Period=22;
//--- input parameters Envelopes
int                ENVPeriod=14;
double             ENVdeviation=0.1;
ENUM_APPLIED_PRICE ENVprice=PRICE_CLOSE;
ENUM_MA_METHOD     ENVmethod=MODE_SMA;
//--- input parameters Donchian Channels
int                DCPeriod=24;
int                DCExtremes=3;
int                DCMargins=-2;
//--- input parameters Silver-channels
int                SCPeriod=26;
double             SCSilvCh=38.2;
double             SCSkyCh=23.6;
double             SCFutCh=61.8;
//--- input parameters NRTR
int                NRTRPeriod   =  40;
double             NRTRK        =  2.0;
//--- input parameters Alligator
int                ALjawperiod=13;
int                ALteethperiod=8;
int                ALlipsperiod=5;
ENUM_MA_METHOD     ALmethod=MODE_SMMA;
ENUM_APPLIED_PRICE ALprice=PRICE_MEDIAN;
//--- input parameters AMA
int                AMAperiod=9;
int                AMAfastperiod=2;
int                AMAslowperiod=30;
ENUM_APPLIED_PRICE AMAprice=PRICE_CLOSE;
//--- input parameters Ichimoku Kinko Hyo
int                IKHtenkansen=9;
int                IKHkijunsen=26;
int                IKHsenkouspanb=52;

string buttonID="«акрыть все позиции";
string buttonID8="ќткрыть позицию Buy ";
string buttonID9="ќткрыть позицию Sell";


int trailing_level;      // ”ровень трейлинг стопа
int trailing_step;      // шаг трейлинг стопа
int slippage;           // проскальзывание


MqlTick tick;             // текуща€ котировка
MqlTick first_tick;       // перва€ котировка 
MqlTradeRequest request;  // параметры торгового запроса
MqlTradeResult result;    // результат торгового запроса


double order_open_price;  // цена открыти€ ордера
double spread;            // величина спрэда
ulong stop_level;         // минимальный отступ от цены дл€ установки стоп лосса / тейк профита
ulong order_type;         // тип ордера
ulong order_ticket;       // тикет ордера 

string  symToWork;
//+------------------------------------------------------------------+
//| Expert ¬”DEinitialization function                               |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)

  {
//ѕри деинициализации удал€ем все обьекты
   ObjectsDeleteAll(window,0);
   return;
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
//ѕрисваиваем по умолчанию - график на котором стоит ѕанель
   symToWork=Symbol();
//ѕрисваиваем значени€ дл€ “раллингстопа
   trailing_step  = TRAILING_STEP;
   slippage       = SLIPPAGE;
   if((_Digits==3) || (_Digits==5))
     {
      trailing_step  = trailing_step  * 10;
      slippage       = slippage      * 10;
     }

//+------------------------------------------------------------------+  
//–исуем кнопки
   SetParametrsandCreate();

//»нформаци€ по текущему инструменту   
   PosInfo(0);
//»нфомраци€ по серверу   
   SeverInfo(0);
//ѕроверка ¬сех символов на наличие позиций   
   SymINFOStart(Symbol1);
   SymINFOStart(Symbol2);
   SymINFOStart(Symbol3);
   SymINFOStart(Symbol4);
   SymINFOStart(Symbol5);
   SymINFOStart(Symbol6);

   return(0);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//ѕрисваиваем значени€ “екущей цены при обновлении тика  
   if(ObjectGetInteger(window,"Pos",OBJPROP_STATE)==true)SyMbolPriceRe(symToWork);
//ѕрисваиваем значени€ »нформации о символе  
   if(ObjectGetInteger(window,"INFO",OBJPROP_STATE)==true)InfoAboutSymbol();
//≈сли нажата кнопка “раллингстоп - траллим позицию
   if(ObjectGetInteger(window,"TrallingStop",OBJPROP_STATE)==true)WorkWithPositions(Symbol1);
   if(ObjectGetInteger(window,"TrallingStop",OBJPROP_STATE)==true)WorkWithPositions(Symbol2);
   if(ObjectGetInteger(window,"TrallingStop",OBJPROP_STATE)==true)WorkWithPositions(Symbol3);
   if(ObjectGetInteger(window,"TrallingStop",OBJPROP_STATE)==true)WorkWithPositions(Symbol4);
   if(ObjectGetInteger(window,"TrallingStop",OBJPROP_STATE)==true)WorkWithPositions(Symbol5);
   if(ObjectGetInteger(window,"TrallingStop",OBJPROP_STATE)==true)WorkWithPositions(Symbol6);
   if(ObjectGetInteger(window,"USEProfitCLOSE",OBJPROP_STATE)==true) startCloseBlock3();

   SymINFOStart(Symbol1);
   SymINFOStart(Symbol2);
   SymINFOStart(Symbol3);
   SymINFOStart(Symbol4);
   SymINFOStart(Symbol5);
   SymINFOStart(Symbol6);
// ќбновл€ем показани€ »ндикаторов
   if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true) VKL_IND(10,symToWork);

  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Expert CHART function                                            |
//+------------------------------------------------------------------+

void OnChartEvent(const int id,

                  const long &lparam,

                  const double &dparam,

                  const string &sparam)

  {

// ¬ыбор ¬алютной пары
//+------------------------------------------------------------------+
//| –абота с валютной парой                                          |
//+------------------------------------------------------------------+

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==Symbol1)
     {
      symToWork=Symbol1;         ObjectSetInteger(window,Symbol1,OBJPROP_STATE,true);
      ObjectSetInteger(window,Symbol2,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol3,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol4,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol5,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol6,OBJPROP_STATE,false);
      SyMbolPriceRe(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true) VKL_IND(10,symToWork);
      ChartClose_A(symToWork);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==Symbol2)
     {
      symToWork=Symbol2;         ObjectSetInteger(window,Symbol2,OBJPROP_STATE,true);
      ObjectSetInteger(window,Symbol1,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol3,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol4,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol5,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol6,OBJPROP_STATE,false);
      SyMbolPriceRe(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      ChartClose_A(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true) VKL_IND(10,symToWork);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==Symbol3)
     {
      symToWork=Symbol3;         ObjectSetInteger(window,Symbol3,OBJPROP_STATE,true);
      ObjectSetInteger(window,Symbol2,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol1,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol4,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol5,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol6,OBJPROP_STATE,false);
      SyMbolPriceRe(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      ChartClose_A(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true) VKL_IND(10,symToWork);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==Symbol4)
     {
      symToWork=Symbol4;         ObjectSetInteger(window,Symbol4,OBJPROP_STATE,true);
      ObjectSetInteger(window,Symbol2,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol3,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol1,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol5,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol6,OBJPROP_STATE,false);
      SyMbolPriceRe(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      ChartClose_A(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true) VKL_IND(10,symToWork);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==Symbol5)
     {
      symToWork=Symbol5;         ObjectSetInteger(window,Symbol5,OBJPROP_STATE,true);
      ObjectSetInteger(window,Symbol2,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol3,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol4,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol1,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol6,OBJPROP_STATE,false);
      SyMbolPriceRe(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      ChartClose_A(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true) VKL_IND(10,symToWork);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==Symbol6)
     {
      symToWork=Symbol6;         ObjectSetInteger(window,Symbol6,OBJPROP_STATE,true);
      ObjectSetInteger(window,Symbol2,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol3,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol4,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol5,OBJPROP_STATE,false);
      ObjectSetInteger(window,Symbol1,OBJPROP_STATE,false);
      SyMbolPriceRe(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      ChartClose_A(symToWork);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true) VKL_IND(10,symToWork);
      ChartRedraw();
     }

//+------------------------------------------------------------------+
//| –абота с ¬кладками                                               |
//+------------------------------------------------------------------+
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Pos")
     {
      ObjectSetInteger(window,"Pos",OBJPROP_STATE,true);
      ObjectSetInteger(window,"Ord",OBJPROP_STATE,false);
      ObjectSetInteger(window,"INFO",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Func",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Ind",OBJPROP_STATE,false);
      VkladkaOrders(-500,-500);

      BG(0); // Ѕек√Ѕек√раунд
      Header();// Ўапка - вкладки
      body(0);  // “ело кнопок
      BODYLEVEL(-20,60);// “ело уровней
      InfoAboutSymbol(-2000);
      SymINFOStart(Symbol1);
      SymINFOStart(Symbol2);
      SymINFOStart(Symbol3);
      SymINFOStart(Symbol4);
      SymINFOStart(Symbol5);
      SymINFOStart(Symbol6);
      VKL_OTHERS(-500);
      VKL_IND(-5000);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Ord")
     {
      ObjectSetInteger(window,"Ord",OBJPROP_STATE,true);
      ObjectSetInteger(window,"Pos",OBJPROP_STATE,false);
      ObjectSetInteger(window,"INFO",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Func",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Ind",OBJPROP_STATE,false);
      priceCreate("BID",-500+97,65,"",15,Black);
      priceCreate("BID2",-500+137,60,"",20,Red);
      priceCreate("Ask",-500+177,65,"",15,Black);
      priceCreate("Ask2",-500+217,60,"",20,Green);
      priceCreate("BID3",-500+137,60,"",20,Red);
      priceCreate("Ask3",-500+217,60,"",20,Green);
      BG(-2000);
      body(-2000);  // “ело кнопок
      BODYLEVEL(-2000,60);// “ело уровней
      InfoAboutSymbol(-2000);
      Header();// Ўапка - вкладки
      VkladkaOrders(-20,80);
      PosInfo();
      SeverInfo(-1000);
      SymINFOStart(Symbol1);
      SymINFOStart(Symbol2);
      SymINFOStart(Symbol3);
      SymINFOStart(Symbol4);
      SymINFOStart(Symbol5);
      SymINFOStart(Symbol6);
      if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true)
        {
         if(OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
         ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
        }
      VKL_OTHERS(-500);
      VKL_IND(-5000);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="INFO")
     {
      ObjectSetInteger(window,"INFO",OBJPROP_STATE,true);
      ObjectSetInteger(window,"Pos",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Ord",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Func",OBJPROP_STATE,false);
      VkladkaOrders(-500,-500);
      ObjectSetInteger(window,"Ind",OBJPROP_STATE,false);

      priceCreate("BID",-500+97,65,"",15,Black);
      priceCreate("BID2",-500+137,60,"",20,Red);
      priceCreate("Ask",-500+177,65,"",15,Black);
      priceCreate("Ask2",-500+217,60,"",20,Green);
      priceCreate("BID3",-500+137,60,"",20,Red);
      priceCreate("Ask3",-500+217,60,"",20,Green);
      PosInfo(-1000);
      SeverInfo(-1000);
      BG(-2000);
      body(-2000);  // “ело кнопок
      BODYLEVEL(-2000,60);// “ело уровней
      InfoAboutSymbol();
      Header();// Ўапка - вкладки
      SymINFOStart(Symbol1);
      SymINFOStart(Symbol2);
      SymINFOStart(Symbol3);
      SymINFOStart(Symbol4);
      SymINFOStart(Symbol5);
      SymINFOStart(Symbol6);
      VKL_OTHERS(-500);
      VKL_IND(-5000);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Func")
     {
      ObjectSetInteger(window,"Func",OBJPROP_STATE,true);
      ObjectSetInteger(window,"Pos",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Ord",OBJPROP_STATE,false);
      ObjectSetInteger(window,"INFO",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Ind",OBJPROP_STATE,false);

      VkladkaOrders(-500,-500);

      priceCreate("BID",-500+97,65,"",15,Black);
      priceCreate("BID2",-500+137,60,"",20,Red);
      priceCreate("Ask",-500+177,65,"",15,Black);
      priceCreate("Ask2",-500+217,60,"",20,Green);
      priceCreate("BID3",-500+137,60,"",20,Red);
      priceCreate("Ask3",-500+217,60,"",20,Green);
      PosInfo(-1000);
      SeverInfo(-1000);
      BG(-2000);
      body(-2000);  // “ело кнопок
      BODYLEVEL(-2000,60);// “ело уровней
      InfoAboutSymbol(-2000);
      Header(-2000);// Ўапка - вкладки
      VKL_OTHERS(0);
      VKL_IND(-5000);

      ChartRedraw();
     }

//Ind

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Ind")
     {
      ObjectSetInteger(window,"Ind",OBJPROP_STATE,true);
      ObjectSetInteger(window,"Func",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Pos",OBJPROP_STATE,false);
      ObjectSetInteger(window,"Ord",OBJPROP_STATE,false);
      ObjectSetInteger(window,"INFO",OBJPROP_STATE,false);
      VkladkaOrders(-500,-500);

      priceCreate("BID",-500+97,65,"",15,Black);
      priceCreate("BID2",-500+137,60,"",20,Red);
      priceCreate("Ask",-500+177,65,"",15,Black);
      priceCreate("Ask2",-500+217,60,"",20,Green);
      priceCreate("BID3",-500+137,60,"",20,Red);
      priceCreate("Ask3",-500+217,60,"",20,Green);
      PosInfo(-1000);
      SeverInfo(-1000);
      BG(-2000);
      body(-2000);  // “ело кнопок
      BODYLEVEL(-2000,60);// “ело уровней
      InfoAboutSymbol(-2000);
      Header();// Ўапка - вкладки
      VKL_IND(10,symToWork);
      VKL_OTHERS(-2000);

      ChartRedraw();
     }

//+------------------------------------------------------------------+
//| –абота с «акрытием сделки                                        |
//+------------------------------------------------------------------+

//--- проверим событие на нажатие кнопки мышки и исполн€ем функцию, соотвтетсвующую данной кнопке, ѕосле исполнени€ - отжимаем кнопку

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==buttonID)
     {
      CloseAll(symToWork);         ObjectSetInteger(window,buttonID,OBJPROP_STATE,false);

      ChartRedraw();
     }
//+------------------------------------------------------------------+
//| –абота с открытием BUY                                           |
//+------------------------------------------------------------------+

//--- проверим событие на нажатие кнопки мышки и исполн€ем функцию, соотвтетсвующую данной кнопке, ѕосле исполнени€ - отжимаем кнопку

   if(id==CHARTEVENT_OBJECT_CLICK && sparam==buttonID8)
     {
      OpenBUY(symToWork);         ObjectSetInteger(window,buttonID8,OBJPROP_STATE,false);

      ChartRedraw();
     }
//+------------------------------------------------------------------+
//| –абота с открытием SELL                                          |
//+------------------------------------------------------------------+

//--- проверим событие на нажатие кнопки мышки и исполн€ем функцию, соотвтетсвующую данной кнопке, ѕосле исполнени€ - отжимаем кнопку
   if(id==CHARTEVENT_OBJECT_CLICK && sparam==buttonID9)
     {
      OpenSELL(symToWork);         ObjectSetInteger(window,buttonID9,OBJPROP_STATE,false);

      ChartRedraw();
     }

//REVERSE  

//+------------------------------------------------------------------+
//| –абота с открытием SELL                                          |
//+------------------------------------------------------------------+

//--- проверим событие на нажатие кнопки мышки и исполн€ем функцию, соотвтетсвующую данной кнопке, ѕосле исполнени€ - отжимаем кнопку
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="REVERSE")
     {
      REVERSE(symToWork);         ObjectSetInteger(window,"REVERSE",OBJPROP_STATE,false);

      ChartRedraw();
     }
//+------------------------------------------------------------------+
//| –абота с ћинимизацией                                            |
//+------------------------------------------------------------------+

//--- ≈сли нажата кнопка ћинимизации - —варачиваем ѕанель - ≈сли Ќажата в свернутом виде - разварачиваем панель
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Minimize2")
     {
      if(ObjectGetInteger(window,"Minimize2",OBJPROP_STATE)==true)
        {
         HideSetParametrsand();ButtonCreate("Minimize2",Black,LightCyan,2,200,13,70,">",10);
        }
      else  {SetParametrsandCreate();SyMbolPriceRe(symToWork);}

      ChartRedraw();
     }

//Minimize2    
//+------------------------------------------------------------------+
//| ћодификаци€                                                      |
//+------------------------------------------------------------------+

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Modify")
     {
      Modify(symToWork,StopLoss,TakeProfit);         ObjectSetInteger(window,"Modify",OBJPROP_STATE,false);

      ChartRedraw();

     }

//+------------------------------------------------------------------+
//| –абота с лотами                                                   |
//+------------------------------------------------------------------+




   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Ћот вверх")
     {
      lotss=lotss+0.01;
      ObjectSetString(window,"Lots",OBJPROP_TEXT,DoubleToString(lotss,2));

      ObjectSetInteger(window,"Ћот вверх",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Ћот вниз")
     {
      lotss=lotss-0.01;
      if(lotss<SymbolInfoDouble(symToWork,SYMBOL_VOLUME_MIN))lotss=SymbolInfoDouble(symToWork,SYMBOL_VOLUME_MIN);
      ObjectSetString(window,"Lots",OBJPROP_TEXT,DoubleToString(lotss,2));

      ObjectSetInteger(window,"Ћот вниз",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="Lots")
     {

      lotss=StringToDouble(ObjectGetString(window,"Lots",OBJPROP_TEXT));
      ChartRedraw();
     }

//+------------------------------------------------------------------+
//| –абота с Tralling                                              |
//+------------------------------------------------------------------+
//--- проверим событие на нажатие кнопки мышки и исполн€ем функцию, соотвтетсвующую данной кнопке
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="TrallingStop")
     {

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Tralling вверх")
     {
      trailing_level=trailing_level+1;
      if(trailing_level>0 && trailing_level<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))trailing_level=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
      ObjectSetString(window,"Tralling",OBJPROP_TEXT,DoubleToString(trailing_level,0));

      ObjectSetInteger(window,"Tralling вверх",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="Tralling вниз")
     {
      trailing_level=trailing_level-1;
      if(trailing_level<0)trailing_level=0;
      if(trailing_level>0 && trailing_level<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))trailing_level=0;

      ObjectSetString(window,"Tralling",OBJPROP_TEXT,DoubleToString(trailing_level,0));

      ObjectSetInteger(window,"Tralling вниз",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="Tralling")
     {

      trailing_level=(int)StringToInteger(ObjectGetString(window,"Tralling",OBJPROP_TEXT));
      ChartRedraw();
     }

   if(trailing_level!=0)ObjectSetInteger(window,"Tralling",OBJPROP_COLOR,Green);else ObjectSetInteger(window,"Tralling",OBJPROP_COLOR,Red);

//+------------------------------------------------------------------+
//| –абота с TakeProfit                                              |
//+------------------------------------------------------------------+



   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="TakeProfit вверх")
     {
      TakeProfit=TakeProfit+1;
      if(TakeProfit>0 && TakeProfit<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))TakeProfit=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
      ObjectSetString(window,"TakeProfit",OBJPROP_TEXT,DoubleToString(TakeProfit,0));

      ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="TakeProfit вниз")
     {
      TakeProfit=TakeProfit-1;
      if(TakeProfit<0)TakeProfit=0;
      if(TakeProfit>0 && TakeProfit<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))TakeProfit=0;

      ObjectSetString(window,"TakeProfit",OBJPROP_TEXT,DoubleToString(TakeProfit,0));

      ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="TakeProfit")
     {

      TakeProfit=(int)StringToInteger(ObjectGetString(window,"TakeProfit",OBJPROP_TEXT));
      ChartRedraw();
     }

   if(TakeProfit!=0)ObjectSetInteger(window,"TakeProfit",OBJPROP_COLOR,Green);else ObjectSetInteger(window,"TakeProfit",OBJPROP_COLOR,Red);

//+------------------------------------------------------------------+
//| –абота с StopLoss                                                |
//+------------------------------------------------------------------+



   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="StopLoss вверх")
     {
      StopLoss=StopLoss+1;
      if(StopLoss>0 && StopLoss<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))StopLoss=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);

      ObjectSetString(window,"StopLoss",OBJPROP_TEXT,DoubleToString(StopLoss,0));

      ObjectSetInteger(window,"StopLoss вверх",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="StopLoss вниз")
     {
      StopLoss=StopLoss-1;
      if(StopLoss<0)StopLoss=0;
      if(StopLoss>0 && StopLoss<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))StopLoss=0;

      ObjectSetString(window,"StopLoss",OBJPROP_TEXT,DoubleToString(StopLoss,0));

      ObjectSetInteger(window,"StopLoss вниз",OBJPROP_STATE,false);

      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="StopLoss")
     {

      StopLoss=(int)StringToInteger(ObjectGetString(window,"StopLoss",OBJPROP_TEXT));
      ChartRedraw();
     }
   if(StopLoss!=0)ObjectSetInteger(window,"StopLoss",OBJPROP_COLOR,Green);else ObjectSetInteger(window,"StopLoss",OBJPROP_COLOR,Red);

//+------------------------------------------------------------------+
//| –абота с OrderLevelStop                                          |
//+------------------------------------------------------------------+



   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="OrderLevelStop вверх")
     {
      OrderLevelStop=OrderLevelStop+1;
      if(OrderLevelStop>0 && OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);

      ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));

      ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="OrderLevelStop вниз")
     {
      OrderLevelStop=OrderLevelStop-1;
      if(OrderLevelStop<0)OrderLevelStop=0;
      if(OrderLevelStop>0 && OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderLevelStop=0;

      ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));

      ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_STATE,false);

      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="OrderLevelStop")
     {

      OrderLevelStop=(int)StringToInteger(ObjectGetString(window,"OrderLevelStop",OBJPROP_TEXT));
      ChartRedraw();
     }
   if(OrderLevelStop!=0)ObjectSetInteger(window,"OrderLevelStop",OBJPROP_COLOR,Green);else ObjectSetInteger(window,"OrderLevelStop",OBJPROP_COLOR,Red);

//+------------------------------------------------------------------+
//| –абота с ќтложенниками                                           |
//+------------------------------------------------------------------+
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="BUYSTOP")
     {
      OpenBUYSTOP();         ObjectSetInteger(window,"BUYSTOP",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="SELLSTOP")
     {
      OpenSELLSTOP();         ObjectSetInteger(window,"SELLSTOP",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="BUYLIMIT")
     {
      OpenBUYLIMIT();         ObjectSetInteger(window,"BUYLIMIT",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="SELLLIMIT")
     {
      OpenSELLLIMIT();         ObjectSetInteger(window,"SELLLIMIT",OBJPROP_STATE,false);

      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="DELETE")
     {
      DeleteAllStops();         ObjectSetInteger(window,"DELETE",OBJPROP_STATE,false);

      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="OrderLevelStopGRID")
     {

      GRIDNUMBER=(int)StringToInteger(ObjectGetString(window,"OrderLevelStopGRID",OBJPROP_TEXT));
      ChartRedraw();
     }

//   ObjectSetString(window,"OrderLevelStopGRID",OBJPROP_TEXT,DoubleToString(GRIDNUMBER,0));

//+------------------------------------------------------------------+
//| –абота с ProfitClose                                           |
//+------------------------------------------------------------------+
/*int startCloseBlock3() 
TypeofClose
   ButtonCreate("TypeClose_DOLLAR",Black,Khaki,x+150,100,100,20,"$",10);
   ButtonCreate("TypeClose_PERCENT",Black,Khaki,x+150,120,100,20,"%",10);
   ObjectCreate(window,"USELossCLOSE_EDIT",OBJ_EDIT,0,100,100);
   ButtonCreate("USEProfitCLOSE",Black,Khaki,x+150,10,100,20,"USEProfitCLOSE",10);
   ObjectCreate(window,"USEProfitCLOSE_EDIT",OBJ_EDIT,0,100,100);
   int Profit;
int Loss;

*/

   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="USELossCLOSE_EDIT")
     {

      Loss=(int)StringToInteger(ObjectGetString(window,"USELossCLOSE_EDIT",OBJPROP_TEXT));
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_ENDEDIT && sparam=="USEProfitCLOSE_EDIT")
     {

      Profit=(int)StringToInteger(ObjectGetString(window,"USEProfitCLOSE_EDIT",OBJPROP_TEXT));
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="TypeClose_DOLLAR")
     {
      TypeofClose=1;         ObjectSetInteger(window,"TypeClose_PERCENT",OBJPROP_STATE,false);
      ObjectSetInteger(window,"TypeClose_POINT",OBJPROP_STATE,false);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="TypeClose_PERCENT")
     {
      TypeofClose=2;         ObjectSetInteger(window,"TypeClose_DOLLAR",OBJPROP_STATE,false);
      ObjectSetInteger(window,"TypeClose_POINT",OBJPROP_STATE,false);
      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="TypeClose_POINT")
     {
      TypeofClose=3;         ObjectSetInteger(window,"TypeClose_DOLLAR",OBJPROP_STATE,false);
      ObjectSetInteger(window,"TypeClose_PERCENT",OBJPROP_STATE,false);

      ChartRedraw();
     }

   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="USEProfitCLOSE")//ObjectSetInteger(window,"USEProfitCLOSE",OBJPROP_STATE,false);
     {
      startCloseBlock3();
      ChartRedraw();
     }

//+------------------------------------------------------------------+
//| –абота с „артами  INFO                                           |
//+------------------------------------------------------------------+




   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M1")
     {
      ChartOpen_A(0,1);TFToInd=1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M5")
     {
      ChartOpen_A(0,5);TFToInd=5;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M15")
     {
      ChartOpen_A(0,15);TFToInd=15;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M30")
     {
      ChartOpen_A(0,30);TFToInd=30;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="H1")
     {
      ChartOpen_A(0,PERIOD_H1);TFToInd=PERIOD_H1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="H4")
     {
      ChartOpen_A(0,PERIOD_H4);TFToInd=PERIOD_H4;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="D1")
     {
      ChartOpen_A(0,PERIOD_D1);TFToInd=PERIOD_D1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="W1")
     {
      ChartOpen_A(0,PERIOD_W1);TFToInd=PERIOD_W1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="MN")
     {
      ChartOpen_A(0,PERIOD_MN1);TFToInd=PERIOD_MN1;
      ChartRedraw();
     }



//+------------------------------------------------------------------+
//| –абота с „артами  Ind                                            |
//+------------------------------------------------------------------+




   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M1_IND")
     {
      ChartOpen_A(0,1);TFToInd=1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M5_IND")
     {
      ChartOpen_A(0,5);TFToInd=5;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M15_IND")
     {
      ChartOpen_A(0,15);TFToInd=15;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="M30_IND")
     {
      ChartOpen_A(0,30);TFToInd=30;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="H1_IND")
     {
      ChartOpen_A(0,PERIOD_H1);TFToInd=PERIOD_H1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="H4_IND")
     {
      ChartOpen_A(0,PERIOD_H4);TFToInd=PERIOD_H4;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="D1_IND")
     {
      ChartOpen_A(0,PERIOD_D1);TFToInd=PERIOD_D1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="W1_IND")
     {
      ChartOpen_A(0,PERIOD_W1);TFToInd=PERIOD_W1;
      ChartRedraw();
     }
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="MN_IND")
     {
      ChartOpen_A(0,PERIOD_MN1);TFToInd=PERIOD_MN1;
      ChartRedraw();
     }


//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+

  }  // END START 
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+::::::::::::::::::::::::::::::::::::::::::::::   ‘ункции эксперта     :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+

//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ѕереварачивает позицию                                         |
//+----------------------------------------------------------------------------+

void REVERSE(string sSymbol)
  {

   if(PositionSelect(sSymbol)==true)
     {

      double lotR=PositionGetDouble(POSITION_VOLUME);
      request.symbol = sSymbol;
      request.volume = PositionGetDouble( POSITION_VOLUME )*2;
      request.action=TRADE_ACTION_DEAL; // операци€ с рынка
      request.tp=0;
      request.sl=0;
      request.deviation=(ulong)((SymbolInfoDouble(sSymbol,SYMBOL_ASK)-SymbolInfoDouble(sSymbol,SYMBOL_BID))/SymbolInfoDouble(sSymbol,SYMBOL_POINT)); // по спреду 
                                                                                                                                                     // request.type_filling=ORDER_FILLING_CANCEL;
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         request.type=ORDER_TYPE_SELL;
         request.price=SymbolInfoDouble(sSymbol,SYMBOL_BID);
        }
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         request.type=ORDER_TYPE_BUY;
         request.price=SymbolInfoDouble(sSymbol,SYMBOL_ASK);

        }
      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");

     }

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : «акрывает позиции указанной валюты                             |
//+----------------------------------------------------------------------------+

void CloseAll(string symToWork2)
  {

   if(PositionSelect(symToWork2)==true)
     {
      ClosePosition(symToWork2);
     }

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : «акрывает выбранную позицию                                    |
//+----------------------------------------------------------------------------+
void ClosePosition(string sSymbol)
  {

   request.symbol = sSymbol;
   request.volume = PositionGetDouble( POSITION_VOLUME );
   request.action=TRADE_ACTION_DEAL; // операци€ с рынка
   request.tp=0;
   request.sl=0;
   request.deviation=(ulong)((SymbolInfoDouble(sSymbol,SYMBOL_ASK)-SymbolInfoDouble(sSymbol,SYMBOL_BID))/SymbolInfoDouble(sSymbol,SYMBOL_POINT)); // по спреду 
                                                                                                                                                  // request.type_filling=ORDER_FILLING_CANCEL;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
     {
      request.type=ORDER_TYPE_SELL;
      request.price=SymbolInfoDouble(sSymbol,SYMBOL_BID);
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
     {
      request.type=ORDER_TYPE_BUY;
      request.price=SymbolInfoDouble(sSymbol,SYMBOL_ASK);
     }

   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ќткрывает позицию Ѕай по выбранной ¬алюте                      |
//+----------------------------------------------------------------------------+  
void OpenBUY(string symToWork2)
  {

   request.symbol = symToWork2;
   request.volume = lotss;
   request.action=TRADE_ACTION_DEAL; // операци€ с рынка
   if(TakeProfit==0)request.tp=0;else request.tp=SymbolInfoDouble(symToWork2,SYMBOL_BID)+TakeProfit*SymbolInfoDouble(symToWork2,SYMBOL_POINT);
   if(StopLoss==0)request.sl=0;else request.sl=SymbolInfoDouble(symToWork2,SYMBOL_BID)-StopLoss*SymbolInfoDouble(symToWork2,SYMBOL_POINT);
   request.deviation=(ulong)((SymbolInfoDouble(symToWork2,SYMBOL_ASK)-SymbolInfoDouble(symToWork2,SYMBOL_BID))/SymbolInfoDouble(symToWork2,SYMBOL_POINT)); // по спреду 
                                                                                                                                                           // request.type_filling=ORDER_FILLING_CANCEL;
   request.type=ORDER_TYPE_BUY;
   request.price=SymbolInfoDouble(symToWork2,SYMBOL_ASK);
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ќткрывает позицию —елл по выбранной ¬алюте                                        |
//+----------------------------------------------------------------------------+  
void OpenSELL(string symToWork2)
  {

   request.symbol = symToWork2;
   request.volume = lotss;
   request.action=TRADE_ACTION_DEAL; // операци€ с рынка
   if(TakeProfit==0)request.tp=0;else request.tp=SymbolInfoDouble(symToWork2,SYMBOL_ASK)-TakeProfit*SymbolInfoDouble(symToWork2,SYMBOL_POINT);
   if(StopLoss==0)request.sl=0;else request.sl=SymbolInfoDouble(symToWork2,SYMBOL_ASK)+StopLoss*SymbolInfoDouble(symToWork2,SYMBOL_POINT);
   request.deviation=(ulong)((SymbolInfoDouble(symToWork2,SYMBOL_ASK)-SymbolInfoDouble(symToWork2,SYMBOL_BID))/SymbolInfoDouble(symToWork2,SYMBOL_POINT)); // по спреду 
                                                                                                                                                           //request.type_filling=ORDER_FILLING_CANCEL;
   request.type=ORDER_TYPE_SELL;
   request.price=SymbolInfoDouble(symToWork2,SYMBOL_BID);
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");

  }
//+------------------------------------------------------------------+

//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ћодифицирует позиции                                           |
//+----------------------------------------------------------------------------+ 

//+------------------------------------------------------------------+
void Modify(string symToWork2,int sl2,int tp2)
//+------------------------------------------------------------------+
  {
   for(int pos=0; pos<PositionsTotal(); pos++)
     {
      if(PositionSelect(symToWork2))
        {
         stop_level=SymbolInfoInteger(PositionGetString(POSITION_SYMBOL),SYMBOL_TRADE_STOPS_LEVEL);
         if(trailing_level<(int)stop_level) trailing_level=(int)stop_level;

         if((PositionGetString(POSITION_SYMBOL)!=symToWork2)) continue;

         order_open_price = PositionGetDouble(POSITION_PRICE_OPEN);
         order_type       = PositionGetInteger(POSITION_TYPE);
         request.order    = PositionGetInteger(POSITION_IDENTIFIER);

         if(order_type==POSITION_TYPE_BUY)
           {

            double sl,tp;

            if(sl2!=0) sl=SymbolInfoDouble(symToWork2,SYMBOL_BID)-sl2*SymbolInfoDouble(symToWork2,SYMBOL_POINT); else sl=0;
            if(tp2!=0) tp=SymbolInfoDouble(symToWork2,SYMBOL_ASK)+tp2*SymbolInfoDouble(symToWork2,SYMBOL_POINT); else tp=0;


            request.action=TRADE_ACTION_SLTP;
            request.symbol = symToWork2;
            request.sl     = sl;
            request.tp     = tp;
            request.deviation=slippage;

            OrderSend(request,result);

            continue;
           }// end POSITION_TYPE_BUY    

         else if(order_type==POSITION_TYPE_SELL)
           {

            double sl,tp;

            if(sl2!=0) sl=SymbolInfoDouble(symToWork2,SYMBOL_ASK)+sl2*SymbolInfoDouble(symToWork2,SYMBOL_POINT); else sl=0;
            if(tp2!=0) tp=SymbolInfoDouble(symToWork2,SYMBOL_BID)-tp2*SymbolInfoDouble(symToWork2,SYMBOL_POINT); else tp=0;
            request.action = TRADE_ACTION_SLTP;
            request.symbol = symToWork2;
            request.sl     = sl;
            request.tp     = tp;
            request.deviation=slippage;

            OrderSend(request,result);

           }// end POSITION_TYPE_SELL

        }// end if select                

     }// end for
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ¬ключает “раллингстоп                                          |
//+----------------------------------------------------------------------------+ 

//+------------------------------------------------------------------+
void WorkWithPositions(string symToWork2)
//+------------------------------------------------------------------+
  {

   for(int pos=0; pos<PositionsTotal(); pos++)
     {
      if(PositionSelect(symToWork2))
        {
         stop_level=SymbolInfoInteger(PositionGetString(POSITION_SYMBOL),SYMBOL_TRADE_STOPS_LEVEL);
         if(trailing_level<(int)stop_level) trailing_level=(int)stop_level;

         if((PositionGetString(POSITION_SYMBOL)!=symToWork2)) continue;

         order_open_price = PositionGetDouble(POSITION_PRICE_OPEN);
         order_type       = PositionGetInteger(POSITION_TYPE);
         request.order    = PositionGetInteger(POSITION_IDENTIFIER);

         if(order_type==POSITION_TYPE_BUY)
           {

            // “рейлинг стоп
            if((trailing_level==0))continue; // условие, при котором трейлинг стоп не работает

            double sl=PositionGetDouble(POSITION_SL);
            double profit=PositionGetDouble(POSITION_PROFIT);
            if(sl<0.0001)
              {
               sl=SymbolInfoDouble(symToWork2,SYMBOL_BID)-trailing_level*SymbolInfoDouble(symToWork2,SYMBOL_POINT);
               request.action=TRADE_ACTION_SLTP;
               request.symbol = symToWork2;
               request.sl     = sl;
               request.tp     = PositionGetDouble(POSITION_TP);
               request.deviation=slippage;

               OrderSend(request,result);

              }
            if((SymbolInfoDouble(symToWork2,SYMBOL_BID)>sl+(trailing_level+trailing_step)*SymbolInfoDouble(symToWork2,SYMBOL_POINT)))
               //if(tick.bid > sl + (trailing_level + trailing_step) * _Point)
              {
               request.action = TRADE_ACTION_SLTP;
               request.symbol = symToWork2;
               request.sl     = sl + trailing_step * SymbolInfoDouble(symToWork2,SYMBOL_POINT);
               request.tp     = PositionGetDouble(POSITION_TP);
               request.deviation=slippage;

               OrderSend(request,result);

              }
            continue;
           }// end POSITION_TYPE_BUY    

         else if(order_type==POSITION_TYPE_SELL)
           {

            //--- “рейлинг стоп
            if((trailing_level==0)) continue;

            double sl=PositionGetDouble(POSITION_SL);
            double profit=PositionGetDouble(POSITION_PROFIT);
            if(sl<0.0001)
              {
               sl=SymbolInfoDouble(symToWork2,SYMBOL_ASK)+trailing_level*SymbolInfoDouble(symToWork2,SYMBOL_POINT);
               request.action = TRADE_ACTION_SLTP;
               request.symbol = symToWork2;
               request.sl     = sl;
               request.tp     = PositionGetDouble(POSITION_TP);
               request.deviation=slippage;

               OrderSend(request,result);

              }
            if((SymbolInfoDouble(symToWork2,SYMBOL_ASK)<sl -(trailing_level+trailing_step)*SymbolInfoDouble(symToWork2,SYMBOL_POINT)))
              {
               request.action = TRADE_ACTION_SLTP;
               request.symbol = symToWork2;
               request.sl     = sl - trailing_step * SymbolInfoDouble(symToWork2,SYMBOL_POINT);
               request.tp     = PositionGetDouble(POSITION_TP);
               request.deviation=slippage;

               OrderSend(request,result);

              }

           }// end POSITION_TYPE_SELL

        }// end if select                

     }// end for
  }
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+::::::::::::::::::::::::::::::::::::::::::::::   —оздание объектов    :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
//+:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::+
// ==========================================================================================================================================================================================================
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// 
//                             ѕанель дл€ ¬кладки Pos  
// 
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// ==========================================================================================================================================================================================================


//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : –исует шапку(¬ыбор ¬алютных пар)                               |
//+----------------------------------------------------------------------------+  

void Header(int X=15,int Y=0)

  {

// Ўапка дл€ панели

   ButtonCreate(Symbol1,Black,OldLace,X,Y,80,20,Symbol1);
   ButtonCreate(Symbol2,Black,OldLace,X+80,Y,80,20,Symbol2);
   ButtonCreate(Symbol3,Black,OldLace,X+160,Y,80,20,Symbol3);

   ButtonCreate(Symbol4,Black,OldLace,X,Y+20,80,20,Symbol4);
   ButtonCreate(Symbol5,Black,OldLace,X+80,Y+20,80,20,Symbol5);
   ButtonCreate(Symbol6,Black,OldLace,X+160,Y+20,80,20,Symbol6);

  }
int ynew=30;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : –исуем “ело ѕанели                                             |
//+----------------------------------------------------------------------------+  

void body(int x)
  {

//
   ButtonCreate(buttonID8,Black,SkyBlue,x+183,ynew+90,60,20,"BUY",10);
   ButtonCreate(buttonID9,Black,Linen,x+103,ynew+90,60,20,"SELL",10);

   ButtonCreate("REVERSE",White,Green,x+135,ynew+45,70,17,"REVERSE",9);

   ButtonCreate(buttonID,Black,Khaki,x+23,ynew+90,60,20,"CLOSE",10);
   ButtonCreate("TrallingStop",Black,OldLace,x+15,ynew+160+ysdvig,74,20,"TRALLING",10);
   ButtonCreate("Modify",Black,OldLace,x+98,ynew+160+ysdvig,153,20,"MODIFY",10);

   ButtonCreate("Minimize2",Black,LightCyan,270,200,13,70,"<",10);

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : —оздает ¬кладки главных страниц                                |
//+----------------------------------------------------------------------------+  


void bodyVkladki(int x)
  {
   ButtonCreate("Pos",Black,LightCyan,x+8,270,40,15,"Pos",10);
   ButtonCreate("Ord",Black,Linen,x+53,270,40,15,"Ord",10);
   ButtonCreate("INFO",Black,Beige,x+98,270,40,15,"INFO",10);
   ButtonCreate("Ind",Black,Beige,x+145,270,40,15,"Ind",10);
   ButtonCreate("Func",Black,MistyRose,x+190,270,40,15,"Func",10);

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : —оздает √лавную подолжку под панель                            |
//+----------------------------------------------------------------------------+  


void BGMAIN(int x)
  {
//VirtPad_1.bmp
   ObjectCreate(window,"BG",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BG",OBJPROP_XDISTANCE,x+0);
   ObjectSetInteger(window,"BG",OBJPROP_YDISTANCE,0);
   ObjectSetInteger(window,"BG",OBJPROP_XSIZE,400);
   ObjectSetInteger(window,"BG",OBJPROP_YSIZE,200);
   ObjectSetString(window,"BG",OBJPROP_BMPFILE,"VirtPad_1.bmp");
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : —оздает картинки из файлов на графике                          |
//+----------------------------------------------------------------------------+  
   int ysdvig=25;

void BG(int x)
  {

   ObjectCreate(window,"BGBUY",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGBUY",OBJPROP_XDISTANCE,x+173);
   ObjectSetInteger(window,"BGBUY",OBJPROP_YDISTANCE,ynew+50);
   ObjectSetInteger(window,"BGBUY",OBJPROP_XSIZE,77);
   ObjectSetInteger(window,"BGBUY",OBJPROP_YSIZE,40);
   ObjectSetString(window,"BGBUY",OBJPROP_BMPFILE,"buy.bmp");

   ObjectCreate(window,"BGSELL",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGSELL",OBJPROP_XDISTANCE,x+93);
   ObjectSetInteger(window,"BGSELL",OBJPROP_YDISTANCE,ynew+50);
   ObjectSetInteger(window,"BGSELL",OBJPROP_XSIZE,77);
   ObjectSetInteger(window,"BGSELL",OBJPROP_YSIZE,40);
   ObjectSetString(window,"BGSELL",OBJPROP_BMPFILE,"sell.bmp");

   ObjectCreate(window,"BGClose",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGClose",OBJPROP_XDISTANCE,x+15);
   ObjectSetInteger(window,"BGClose",OBJPROP_YDISTANCE,ynew+50);
   ObjectSetInteger(window,"BGClose",OBJPROP_XSIZE,77);
   ObjectSetInteger(window,"BGClose",OBJPROP_YSIZE,40);
   ObjectSetString(window,"BGClose",OBJPROP_BMPFILE,"close.bmp");

   ObjectCreate(window,"BGTRAL",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGTRAL",OBJPROP_XDISTANCE,x+18-4);
   ObjectSetInteger(window,"BGTRAL",OBJPROP_YDISTANCE,ynew+114+ysdvig);
   ObjectSetInteger(window,"BGTRAL",OBJPROP_XSIZE,66);
   ObjectSetInteger(window,"BGTRAL",OBJPROP_YSIZE,19);
   ObjectSetString(window,"BGTRAL",OBJPROP_BMPFILE,"tral.bmp");

   ObjectCreate(window,"BGTP",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGTP",OBJPROP_XDISTANCE,x+95);
   ObjectSetInteger(window,"BGTP",OBJPROP_YDISTANCE,ynew+114+ysdvig);
   ObjectSetInteger(window,"BGTP",OBJPROP_XSIZE,66);
   ObjectSetInteger(window,"BGTP",OBJPROP_YSIZE,19);
   ObjectSetString(window,"BGTP",OBJPROP_BMPFILE,"tp.bmp");

   ObjectCreate(window,"BGSL",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGSL",OBJPROP_XDISTANCE,x+178);
   ObjectSetInteger(window,"BGSL",OBJPROP_YDISTANCE,ynew+114+ysdvig);
   ObjectSetInteger(window,"BGSL",OBJPROP_XSIZE,66);
   ObjectSetInteger(window,"BGSL",OBJPROP_YSIZE,19);
   ObjectSetString(window,"BGSL",OBJPROP_BMPFILE,"sl.bmp");
   
   
      ObjectCreate(window,"BGLOTS",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGLOTS",OBJPROP_XDISTANCE,x+25);
   ObjectSetInteger(window,"BGLOTS",OBJPROP_YDISTANCE,ynew+20+ysdvig);
   ObjectSetInteger(window,"BGLOTS",OBJPROP_XSIZE,77);
   ObjectSetInteger(window,"BGLOTS",OBJPROP_YSIZE,30);
   ObjectSetString(window,"BGLOTS",OBJPROP_BMPFILE,"lot.bmp");
  


  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание :ѕолучение бид аска по символу и отображени€ информации в квадратах на панели                      |
//+----------------------------------------------------------------------------+  
void SyMbolPriceRe(string Symboll)
  {

   int xxx=0;
   double Bid=SymbolInfoDouble(Symboll,SYMBOL_BID);
   double Ask=SymbolInfoDouble(Symboll,SYMBOL_ASK);

   string a1,a2,a3,b1,b2,b3;

   if(ObjectGetInteger(window,"Minimize2",OBJPROP_STATE)==true || ObjectGetInteger(window,"Func",OBJPROP_STATE)==true
      || ObjectGetInteger(window,"INFO",OBJPROP_STATE)==true || ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true)xxx=-500;
   if(ObjectGetInteger(window,"Minimize2",OBJPROP_STATE)==false  &&  ObjectGetInteger(window,"Func",OBJPROP_STATE)==false
      &&  ObjectGetInteger(window,"INFO",OBJPROP_STATE)==false && ObjectGetInteger(window,"Ind",OBJPROP_STATE)==false)xxx=0;

   if(ObjectGetInteger(window,"BID",OBJPROP_XDISTANCE)<0 && ObjectGetInteger(window,"Pos",OBJPROP_STATE)==false)return;

   if(SymbolInfoDouble(symToWork,SYMBOL_POINT)==0.00001 || SymbolInfoDouble(symToWork,SYMBOL_POINT)==0.0001)
     {
      b1=StringSubstr(DoubleToString(Bid,8),0,4);
      b2=StringSubstr(DoubleToString(Bid,8),4,2);
      b3=StringSubstr(DoubleToString(Bid,8),6,1);

      priceCreate("BID",xxx+97,ynew+65,b1,15,Black);
      priceCreate("BID2",xxx+137,ynew+62,b2,18,Yellow);
      priceCreate("BID3",xxx+163,ynew+62,b3,10,Black);

      a1=StringSubstr(DoubleToString(Ask,8),0,4);
      a2=StringSubstr(DoubleToString(Ask,8),4,2);
      a3=StringSubstr(DoubleToString(Ask,8),6,1);

      priceCreate("Ask",xxx+177,ynew+65,a1,15,Black);
      priceCreate("Ask2",xxx+217,ynew+62,a2,18,Yellow);
      priceCreate("Ask3",xxx+243,ynew+62,a3,10,Black);
     }

   if(SymbolInfoDouble(symToWork,SYMBOL_POINT)==0.001 || SymbolInfoDouble(symToWork,SYMBOL_POINT)==0.01)
     {
      b1=StringSubstr(DoubleToString(Bid,8),0,3);
      b2=StringSubstr(DoubleToString(Bid,8),3,2);
      b3=StringSubstr(DoubleToString(Bid,8),5,1);
      priceCreate("BID",xxx+105,ynew+65,b1,15,Black);
      priceCreate("BID2",xxx+137,ynew+62,b2,18,Yellow);
      priceCreate("BID3",xxx+163,ynew+62,b3,10,Black);

      a1=StringSubstr(DoubleToString(Ask,8),0,3);
      a2=StringSubstr(DoubleToString(Ask,8),3,2);
      a3=StringSubstr(DoubleToString(Ask,8),5,1);

      priceCreate("Ask",xxx+185,ynew+65,a1,15,Black);
      priceCreate("Ask2",xxx+217,ynew+62,a2,18,Yellow);
      priceCreate("Ask3",xxx+243,ynew+62,a3,10,Black);
     }
   PosInfo(xxx);
   SeverInfo(xxx);

   ChartRedraw();

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание :ƒень Ќедели                                                     |
//+----------------------------------------------------------------------------+  
int DayOfWeekMQL4()
  {
   MqlDateTime tm;
   TimeCurrent(tm);
   return(tm.day_of_week);
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание :ƒень Ќедели  выбранной даты                                     |
//+----------------------------------------------------------------------------+  
int TimeDayOfWeekMQL4(datetime date)
  {
   MqlDateTime tm;
   TimeToStruct(date,tm);
   return(tm.day_of_week);
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : »нформаци€ о подключении + ѕоследнее врем€ котировки           |
//+----------------------------------------------------------------------------+  
void SeverInfo(int x=0)
  {
  /*
   string con;
   if(TerminalInfoInteger(TERMINAL_CONNECTED)==true)con="Connect";else con="Disconnect";
   if(TimeDayOfWeekMQL4(TimeLocal())!=DayOfWeekMQL4())con="Market Close";
   string time=TimeToString(TimeCurrent(),TIME_MINUTES|TIME_SECONDS);

   ObjectCreate(window,"ServerTime",OBJ_EDIT,0,100,100);
   if(con=="Connect") ObjectSetInteger(window,"ServerTime",OBJPROP_COLOR,DarkGreen);
   if(con!="Connect") ObjectSetInteger(window,"ServerTime",OBJPROP_COLOR,DarkRed);
   ObjectSetInteger(window,"ServerTime",OBJPROP_BGCOLOR,WhiteSmoke);
   ObjectSetInteger(window,"ServerTime",OBJPROP_XDISTANCE,x+19);
   ObjectSetInteger(window,"ServerTime",OBJPROP_YDISTANCE,225);
   ObjectSetInteger(window,"ServerTime",OBJPROP_XSIZE,230);
   ObjectSetInteger(window,"ServerTime",OBJPROP_YSIZE,22);
   ObjectSetString(window,"ServerTime",OBJPROP_FONT,"Sans Serif");
   ObjectSetString(window,"ServerTime",OBJPROP_TEXT,con+"        "+time);
   ObjectSetInteger(window,"ServerTime",OBJPROP_FONTSIZE,14);
   ObjectSetInteger(window,"ServerTime",OBJPROP_READONLY,true); // иначе нажать на нее нельз€
*/
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : »нформаци€ о открытой позиции                                  |
//+----------------------------------------------------------------------------+  
void PosInfo(int x=0)
  {
   if(PositionSelect(symToWork)==true)
     {
      double lotinfo=PositionGetDouble(POSITION_VOLUME);
      int typeinfo=(int)PositionGetInteger(POSITION_TYPE);
      string typepos;
      if(typeinfo==POSITION_TYPE_SELL)typepos="SELL";
      if(typeinfo==POSITION_TYPE_BUY)typepos="BUY";
      if(typepos=="SELL") ObjectSetInteger(window,symToWork,OBJPROP_BGCOLOR,LightPink);
      if(typepos=="BUY") ObjectSetInteger(window,symToWork,OBJPROP_BGCOLOR,PaleGreen);


      double openinfo=PositionGetDouble(POSITION_PRICE_OPEN);
      double profinfo=PositionGetDouble(POSITION_PROFIT)+PositionGetDouble(POSITION_SWAP)+PositionGetDouble(POSITION_COMMISSION);
      ButtonCreateInfo("InfoPos",Navy,WhiteSmoke,x+15,45,240,24,typepos+" "+DoubleToString(lotinfo,2)+" at "+DoubleToString(openinfo,4)+"/ "+DoubleToString(profinfo,2),12);

     }

   if(PositionSelect(symToWork)==false)
     {
      ButtonCreateInfo("InfoPos",Navy,WhiteSmoke,x+15,45,240,24,"        NO POSITIONS",14);
      ObjectSetInteger(window,symToWork,OBJPROP_BGCOLOR,OldLace);
     }

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : —оздание кнопки —имвола                                        |
//+----------------------------------------------------------------------------+  
void ButtonCreateInfo(string name,color TextColor,color bgcolor,int Xdist,int Ydist,int Xsize,int Ysize,string Text,int FONTSIZE=12)
  {
   ObjectCreate(window,name,OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,name,OBJPROP_COLOR,TextColor);
   ObjectSetInteger(window,name,OBJPROP_BGCOLOR,bgcolor);
   ObjectSetInteger(window,name,OBJPROP_XDISTANCE,Xdist);
   ObjectSetInteger(window,name,OBJPROP_YDISTANCE,Ydist);
   ObjectSetInteger(window,name,OBJPROP_XSIZE,Xsize);
   ObjectSetInteger(window,name,OBJPROP_YSIZE,Ysize);
   ObjectSetString(window,name,OBJPROP_FONT,"Sans Serif");
   ObjectSetString(window,name,OBJPROP_TEXT,Text);
   ObjectSetInteger(window,name,OBJPROP_FONTSIZE,FONTSIZE);
   ObjectSetInteger(window,name,OBJPROP_READONLY,true); // иначе нажать на нее нельз€

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : —оздание ÷еновой метки                                         |
//+----------------------------------------------------------------------------+  
void priceCreate(string name,int Xd,int Yd,string text,int size,color cl,string tex="Arial")
  {
   ObjectCreate(window,name,OBJ_LABEL,0,100,100);
   ObjectSetInteger(window,name,OBJPROP_COLOR,cl);
   ObjectSetInteger(window,name,OBJPROP_XDISTANCE,Xd);
   ObjectSetInteger(window,name,OBJPROP_YDISTANCE,Yd);
   ObjectSetString(window,name,OBJPROP_FONT,tex);
   ObjectSetString(window,name,OBJPROP_TEXT,text);
   ObjectSetInteger(window,name,OBJPROP_FONTSIZE,size);

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание :ќбща€ функци€ —оздани€ панели                                   |
//+----------------------------------------------------------------------------+  
void SetParametrsandCreate()
  {
   BGMAIN(0); // Ѕек√Ѕек√раунд
   if(ObjectGetInteger(window,"Func",OBJPROP_STATE)==true)

      VKL_OTHERS(0);

   if(ObjectGetInteger(window,"Pos",OBJPROP_STATE)==true || 
      (ObjectGetInteger(window,"Pos",OBJPROP_STATE)==false
      && ObjectGetInteger(window,"Func",OBJPROP_STATE)==false
      && ObjectGetInteger(window,"INFO",OBJPROP_STATE)==false
      && ObjectGetInteger(window,"Ord",OBJPROP_STATE)==false
      && ObjectGetInteger(window,"Ind",OBJPROP_STATE)==false)

      )
     {

      BG(0); // Ѕек√Ѕек√раунд
      body(0);  // “ело кнопок
      BODYLEVEL(-20,60);// “ело уровней
      PosInfo();
      SeverInfo();

     }

   if(ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true
      )
     {
      VkladkaOrders(-20,60);
     }

   ButtonCreate("Minimize2",Black,LightCyan,270,200,13,70,"<",10);
   if(ObjectGetInteger(window,"Func",OBJPROP_STATE)==false)
     {
      Header();// Ўапка - вкладки
     }
   SymINFOStart(Symbol1);
   SymINFOStart(Symbol2);
   SymINFOStart(Symbol3);
   SymINFOStart(Symbol4);
   SymINFOStart(Symbol5);
   SymINFOStart(Symbol6);
   SyMbolPriceRe(symToWork);
   ChartOpen_A(0);
   bodyVkladki(0); // ¬кладки

   if(ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true)
     {
      VKL_IND();
     }


  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ћинимизаци€ панели                                             |
//+----------------------------------------------------------------------------+  
void HideSetParametrsand()
  {
   ChartOpen_A(-5000);
   priceCreate("BID",-500+97,65,"",15,Black);
   priceCreate("BID2",-500+137,60,"",20,Red);
   priceCreate("Ask",-500+177,65,"",15,Black);
   priceCreate("Ask2",-500+217,60,"",20,Green);
   priceCreate("Ask3",-500+217,60,"",20,Green);
   priceCreate("BID3",-500+137,60,"",20,Red);

   PosInfo(-1000);
   SeverInfo(-1000);
   BGMAIN(-2000);
   BG(-2000);
   Header(-2000);// Ўапка - вкладки
   body(-2000);  // “ело кнопок
   BODYLEVEL(-2000,60);// “ело уровней
   bodyVkladki(-2000); // ¬кладки
   InfoAboutSymbol(-2000);
   VkladkaOrders(-500,-500);
   VKL_OTHERS(-500);
   VKL_IND(-2000);
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : —оздание кнопок                                                |
//+----------------------------------------------------------------------------+  
void ButtonCreate(string name,color TextColor,color bgcolor,int Xdist,int Ydist,int Xsize,int Ysize,string Text,int FONTSIZE=12)
  {
   ObjectCreate(window,name,OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,name,OBJPROP_COLOR,TextColor);
   ObjectSetInteger(window,name,OBJPROP_BGCOLOR,bgcolor);
   ObjectSetInteger(window,name,OBJPROP_XDISTANCE,Xdist);
   ObjectSetInteger(window,name,OBJPROP_YDISTANCE,Ydist);
   ObjectSetInteger(window,name,OBJPROP_XSIZE,Xsize);
   ObjectSetInteger(window,name,OBJPROP_YSIZE,Ysize);
   ObjectSetString(window,name,OBJPROP_FONT,"Sans Serif");
   ObjectSetString(window,name,OBJPROP_TEXT,Text);
   ObjectSetInteger(window,name,OBJPROP_FONTSIZE,FONTSIZE);
   if(name==Symbol1)ObjectSetInteger(window,name,OBJPROP_SELECTABLE,true); // иначе нажать на нее нельз€
   if(name!=Symbol1)ObjectSetInteger(window,name,OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : —оздани€ обьектов управлени€ стопами                           |
//+----------------------------------------------------------------------------+  
void BODYLEVEL(int x,int y)
  {
   int a=30;
   window=0;
   int bb3=20;
   int xx=10;
   int xx2=130;
   int SizeY=20;
   color BG_CLOSEDELETE=Red;
   lotss=NormalizeDouble(lot,2);
   int xLOTS=10;
//

//+------------------------------------------------------------------+
//| ”величение/уменьшение ”ровн€ траллингстопа                       |
//+------------------------------------------------------------------+

   ObjectCreate(window,"Tralling",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"Tralling",OBJPROP_XDISTANCE,x+30+xLOTS-5);
   ObjectSetInteger(window,"Tralling",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvig);
   ObjectSetInteger(window,"Tralling",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"Tralling",OBJPROP_BGCOLOR,White);

   ObjectSetInteger(window,"Tralling",OBJPROP_XSIZE,58);
   ObjectSetInteger(window,"Tralling",OBJPROP_YSIZE,20);
   ObjectSetString(window,"Tralling",OBJPROP_TEXT,DoubleToString(trailing_level,0));
   ObjectSetInteger(window,"Tralling",OBJPROP_FONTSIZE,15);
   ObjectSetInteger(window,"Tralling",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"Tralling вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_XDISTANCE,x+80+xLOTS+4);
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvig);
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"Tralling вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"Tralling вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"Tralling вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"Tralling вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_XDISTANCE,x+80+xLOTS+4);
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_YDISTANCE,ynew+y+55+a+ysdvig);
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"Tralling вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"Tralling вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"Tralling вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//| ”величение/уменьшение лотов                                      |
//+------------------------------------------------------------------+


   ObjectCreate(window,"Lots",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"Lots",OBJPROP_XDISTANCE,x+30+xLOTS);
   ObjectSetInteger(window,"Lots",OBJPROP_YDISTANCE,ynew+y-25+a);
   ObjectSetInteger(window,"Lots",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"Lots",OBJPROP_BGCOLOR,Lavender);

   ObjectSetInteger(window,"Lots",OBJPROP_XSIZE,50);
   ObjectSetInteger(window,"Lots",OBJPROP_YSIZE,20);
   ObjectSetString(window,"Lots",OBJPROP_TEXT,DoubleToString(lotss,2));
   ObjectSetInteger(window,"Lots",OBJPROP_FONTSIZE,15);
   ObjectSetInteger(window,"Lots",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"Ћот вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_XDISTANCE,x+82+xLOTS);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_YDISTANCE,ynew+y-25+a);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"Ћот вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"Ћот вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"Ћот вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_XDISTANCE,x+82+xLOTS);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_YDISTANCE,ynew+y-15+a);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"Ћот вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"Ћот вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//| ”величение/уменьшение TakeProfit                                 |
//+------------------------------------------------------------------+
   int xTakeProfit=10;

   ObjectCreate(window,"TakeProfit",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_XDISTANCE,x+108+xTakeProfit);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvig);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_BGCOLOR,White);

   ObjectSetInteger(window,"TakeProfit",OBJPROP_XSIZE,55);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_YSIZE,20);
   ObjectSetString(window,"TakeProfit",OBJPROP_TEXT,DoubleToString(TakeProfit,0));
   ObjectSetInteger(window,"TakeProfit",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"TakeProfit вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_XDISTANCE,x+164+xTakeProfit);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvig);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"TakeProfit вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"TakeProfit вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"TakeProfit вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_XDISTANCE,x+164+xTakeProfit);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_YDISTANCE,ynew+y+55+a+ysdvig);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"TakeProfit вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"TakeProfit вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//| ”величение/уменьшение StopLoss                                   |
//+------------------------------------------------------------------+
   int xStopLoss=10;

   ObjectCreate(window,"StopLoss",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"StopLoss",OBJPROP_XDISTANCE,x+190+xStopLoss);
   ObjectSetInteger(window,"StopLoss",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvig);
   ObjectSetInteger(window,"StopLoss",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"StopLoss",OBJPROP_BGCOLOR,White);

   ObjectSetInteger(window,"StopLoss",OBJPROP_XSIZE,55);
   ObjectSetInteger(window,"StopLoss",OBJPROP_YSIZE,20);
   ObjectSetString(window,"StopLoss",OBJPROP_TEXT,DoubleToString(StopLoss,0));
   ObjectSetInteger(window,"StopLoss",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"StopLoss",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"StopLoss вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_XDISTANCE,x+246+xStopLoss);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvig);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"StopLoss вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"StopLoss вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"StopLoss вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_XDISTANCE,x+246+xStopLoss);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_YDISTANCE,ynew+y+55+a+ysdvig);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"StopLoss вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"StopLoss вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|   ѕроверка символа на наличие позиций                            |
//+------------------------------------------------------------------+
void SymINFOStart(string symb)
  {
   if(PositionSelect(symb)==true)
     {
      int typeinfo=(int)PositionGetInteger(POSITION_TYPE);
      string typepos;
      if(typeinfo==POSITION_TYPE_SELL)typepos="SELL";
      if(typeinfo==POSITION_TYPE_BUY)typepos="BUY";
      if(typepos=="SELL")   ObjectSetInteger(window,symb,OBJPROP_BGCOLOR,LightPink);
      if(typepos=="BUY")   ObjectSetInteger(window,symb,OBJPROP_BGCOLOR,PaleGreen);

     }
  }
// ==========================================================================================================================================================================================================
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// 
//                             ѕанель дл€ ¬кладки INFO  
// 
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// ==========================================================================================================================================================================================================

void InfoAboutSymbol(int x=0)
  {
   if(ObjectGetInteger(window,"Minimize2",OBJPROP_STATE)==true || ObjectGetInteger(window,"Func",OBJPROP_STATE)==true
      || ObjectGetInteger(window,"Ord",OBJPROP_STATE)==true || ObjectGetInteger(window,"Pos",OBJPROP_STATE)==true || ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true)x=-500;
   if(ObjectGetInteger(window,"Minimize2",OBJPROP_STATE)==false
      &&  ObjectGetInteger(window,"INFO",OBJPROP_STATE)==true)x=0;

   if(ObjectGetInteger(window,"INFO_Desc",OBJPROP_XDISTANCE)<0 && ObjectGetInteger(window,"INFO",OBJPROP_STATE)==false)return;

   int INFO_spread=(int)SymbolInfoInteger(symToWork,SYMBOL_SPREAD);
   int INFO_stops=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);

   double INFO_point=SymbolInfoDouble(symToWork,SYMBOL_POINT);
   double INFO_LotMin=SymbolInfoDouble(symToWork,SYMBOL_VOLUME_MIN);
   double INFO_LotMax=SymbolInfoDouble(symToWork,SYMBOL_VOLUME_MAX);
   double INFO_LotStep=SymbolInfoDouble(symToWork,SYMBOL_VOLUME_STEP);
   double INFO_SWAPLong=SymbolInfoDouble(symToWork,SYMBOL_SWAP_LONG);
   double INFO_SWAPshort=SymbolInfoDouble(symToWork,SYMBOL_SWAP_SHORT);

   string INFO_Bank=SymbolInfoString(symToWork,SYMBOL_BANK);
   string INFO_Desc=SymbolInfoString(symToWork,SYMBOL_DESCRIPTION);



   ObjectCreate(window,"RectLabelInfo",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"RectLabelInfo",OBJPROP_XDISTANCE,x+15);
   ObjectSetInteger(window,"RectLabelInfo",OBJPROP_YDISTANCE,50);
   ObjectSetInteger(window,"RectLabelInfo",OBJPROP_XSIZE,239);
   ObjectSetInteger(window,"RectLabelInfo",OBJPROP_YSIZE,150);
   ObjectSetInteger(window,"RectLabelInfo",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"RectLabelInfo",OBJPROP_BGCOLOR,WhiteSmoke);

   LabelCreate("INFO_Desc",Green,x+20,55,INFO_Desc);
   LabelCreate("INFO_spread",Green,x+20,72,"SPREAD",10); LabelCreate("INFO_spread"+"Var",Red,x+130,72,(string)INFO_spread,11);
   LabelCreate("INFO_stops",Green,x+20,85,"STOP",10); LabelCreate("INFO_stops"+"Var",Red,x+130,85,(string)INFO_stops,11);
   LabelCreate("INFO_point",Green,x+20,100,"POINT",10); LabelCreate("INFO_point"+"Var",Red,x+130,100,DoubleToString(INFO_point,5),11);
   LabelCreate("INFO_LotMin",Green,x+20,115,"LOT_MIN",10); LabelCreate("INFO_LotMin"+"Var",Red,x+130,115,DoubleToString(INFO_LotMin,2),11);
   LabelCreate("INFO_LotMax",Green,x+20,130,"LOT_MAX",10); LabelCreate("INFO_LotMax"+"Var",Red,x+130,130,DoubleToString(INFO_LotMax,2),11);
   LabelCreate("INFO_LotStep",Green,x+20,145,"LOT_STEP",10); LabelCreate("INFO_LotStep"+"Var",Red,x+130,145,DoubleToString(INFO_LotStep,2),11);
   LabelCreate("INFO_SWAPLong",Green,x+20,160,"SWAP_LONG",10); LabelCreate("INFO_SWAPLong"+"Var",Red,x+130,160,DoubleToString(INFO_SWAPLong,2),11);
   LabelCreate("INFO_SWAPshort",Green,x+20,175,"SWAP_SHORT",10); LabelCreate("INFO_SWAPshort"+"Var",Red,x+130,175,DoubleToString(INFO_SWAPshort,2),11);

   ObjectCreate(window,"TFARROW",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"TFARROW",OBJPROP_XDISTANCE,x+15);
   ObjectSetInteger(window,"TFARROW",OBJPROP_YDISTANCE,202);
   ObjectSetInteger(window,"TFARROW",OBJPROP_XSIZE,239);
   ObjectSetInteger(window,"TFARROW",OBJPROP_YSIZE,47);
   ObjectSetInteger(window,"TFARROW",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"TFARROW",OBJPROP_BGCOLOR,MintCream);

   LabelCreate("M1",Black,x+20,205,"M1",10);
   LabelCreate("M5",Black,x+45,205,"M5",10);
   LabelCreate("M15",Black,x+70,205,"M15",10);
   LabelCreate("M30",Black,x+100,205,"M30",10);
   LabelCreate("H1",Black,x+130,205,"H1",10);
   LabelCreate("H4",Black,x+155,205,"H4",10);
   LabelCreate("D1",Black,x+180,205,"D1",10);
   LabelCreate("W1",Black,x+205,205,"W1",10);
   LabelCreate("MN",Black,x+230,205,"MN",10);

   ArrowCreate("M1"+"ARROW",x+20,220,iOpenMQL4(symToWork,1,0),iCloseMQL4(symToWork,1,0));
   ArrowCreate("M5"+"ARROW",x+45,220,iOpenMQL4(symToWork,5,0),iCloseMQL4(symToWork,5,0));
   ArrowCreate("M15"+"ARROW",x+75,220,iOpenMQL4(symToWork,15,0),iCloseMQL4(symToWork,15,0));
   ArrowCreate("M30"+"ARROW",x+105,220,iOpenMQL4(symToWork,30,0),iCloseMQL4(symToWork,30,0));
   ArrowCreate("H1"+"ARROW",x+130,220,iOpenMQL4(symToWork,60,0),iCloseMQL4(symToWork,60,0));
   ArrowCreate("H4"+"ARROW",x+155,220,iOpenMQL4(symToWork,240,0),iCloseMQL4(symToWork,240,0));
   ArrowCreate("D1"+"ARROW",x+180,220,iOpenMQL4(symToWork,1440,0),iCloseMQL4(symToWork,1440,0));
   ArrowCreate("W1"+"ARROW",x+205,220,iOpenMQL4(symToWork,10080,0),iCloseMQL4(symToWork,10080,0));
   ArrowCreate("MN"+"ARROW",x+230,220,iOpenMQL4(symToWork,43200,0),iCloseMQL4(symToWork,43200,0));

  }
//+------------------------------------------------------------------+
//| ѕри нажатии на “‘ открываем „ј–“                                 |
//+------------------------------------------------------------------+
void ChartOpen_A(int x=0,int TF=0)
  {
   int TF2;
   if(TF==0)TF2=Period();else TF2=TF;

   if(ObjectFind(window,"CreateChart")<0 && (ObjectGetInteger(window,"INFO",OBJPROP_STATE)==true || ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true))
     {
      ObjectCreate(window,"CreateChart",OBJ_CHART,0,0,0);
      ObjectSetInteger(window,"CreateChart",OBJPROP_XDISTANCE,x+270);
      ObjectSetInteger(window,"CreateChart",OBJPROP_YDISTANCE,0);
      ObjectSetString(window,"CreateChart",OBJPROP_SYMBOL,symToWork);
      ObjectSetInteger(window,"CreateChart",OBJPROP_PERIOD,TF2);
     }
   else
     {

      if(ObjectGetInteger(window,"CreateChart",OBJPROP_PERIOD)==TF || ObjectGetInteger(window,"Minimize2",OBJPROP_STATE)==true) ChartCloseDel_A();
      else
        {
         if(TF!=0)
            if(ObjectGetInteger(window,"Minimize2",OBJPROP_STATE)==false
               && (ObjectGetInteger(window,"INFO",OBJPROP_STATE)==true || ObjectGetInteger(window,"Ind",OBJPROP_STATE)==true))
              {
               ObjectCreate(window,"CreateChart",OBJ_CHART,0,0,0);
               ObjectSetInteger(window,"CreateChart",OBJPROP_XDISTANCE,x+270);
               ObjectSetInteger(window,"CreateChart",OBJPROP_YDISTANCE,0);
               ObjectSetString(window,"CreateChart",OBJPROP_SYMBOL,symToWork);
               ObjectSetInteger(window,"CreateChart",OBJPROP_PERIOD,TF2);
              }
        }
     }
  }
//+------------------------------------------------------------------+
//| ѕри нажатии на “‘ закрываем „ј–“                                 |
//+------------------------------------------------------------------+
void ChartCloseDel_A()
  {
   ObjectDelete(window,"CreateChart");
  }
//+------------------------------------------------------------------+
//| ѕри нажатии на ѕару —мена валюты                                 |
//+------------------------------------------------------------------+
void ChartClose_A(string Symbols="")
  {
   ObjectSetString(window,"CreateChart",OBJPROP_SYMBOL,Symbols);
  }
//+------------------------------------------------------------------+
//| —оздаем объект дл€ »нформ ѕанели                                 |
//+------------------------------------------------------------------+
void LabelCreate(string name,color TextColor,int Xdist,int Ydist,string Text,int FONTSIZE=12)
  {
   ObjectCreate(window,name,OBJ_LABEL,0,100,100);
   ObjectSetInteger(window,name,OBJPROP_XDISTANCE,Xdist);
   ObjectSetInteger(window,name,OBJPROP_YDISTANCE,Ydist);
   ObjectSetInteger(window,name,OBJPROP_COLOR,TextColor);
   ObjectSetString(window,name,OBJPROP_TEXT,Text);
   ObjectSetInteger(window,name,OBJPROP_FONTSIZE,FONTSIZE);

  }
//+------------------------------------------------------------------+
//| –исуем —трелки дл€ ¬ывода информации о состо€нии движени€        |
//+------------------------------------------------------------------+
void ArrowCreate(string name,int Xdist,int Ydist,double open,double close,int FONTSIZE=12)
  {
   if(open<close)
     {
      if(ObjectGetString(window,name,OBJPROP_TEXT)!="Ё" || ObjectGetInteger(window,name,OBJPROP_XDISTANCE)!=Xdist)
        {
         ObjectCreate(window,name,OBJ_LABEL,0,0,0);

         ObjectSetInteger(window,name,OBJPROP_XDISTANCE,Xdist);
         ObjectSetInteger(window,name,OBJPROP_YDISTANCE,Ydist);
         ObjectSetInteger(window,name,OBJPROP_COLOR,Green);
         ObjectSetInteger(window,name,OBJPROP_FONTSIZE,FONTSIZE);
         ObjectSetString(window,name,OBJPROP_FONT,"WingDings");
         ObjectSetString(window,name,OBJPROP_TEXT,"Ё");
        }
     }
   if(open>close)
     {
      if(ObjectGetString(window,name,OBJPROP_TEXT)!="ё" || ObjectGetInteger(window,name,OBJPROP_XDISTANCE)!=Xdist)
        {
         ObjectCreate(window,name,OBJ_LABEL,0,0,0);
         ObjectSetInteger(window,name,OBJPROP_XDISTANCE,Xdist);
         ObjectSetInteger(window,name,OBJPROP_YDISTANCE,Ydist);
         ObjectSetInteger(window,name,OBJPROP_COLOR,Red);
         ObjectSetInteger(window,name,OBJPROP_FONTSIZE,FONTSIZE);
         ObjectSetString(window,name,OBJPROP_FONT,"WingDings");
         ObjectSetString(window,name,OBJPROP_TEXT,"ё");
        }
     }
   if(open==close)
     {
      if(ObjectGetString(window,name,OBJPROP_TEXT)!="у" || ObjectGetInteger(window,name,OBJPROP_XDISTANCE)!=Xdist)
        {
         ObjectCreate(window,name,OBJ_LABEL,0,0,0);
         ObjectSetInteger(window,name,OBJPROP_XDISTANCE,Xdist);
         ObjectSetInteger(window,name,OBJPROP_YDISTANCE,Ydist);
         ObjectSetInteger(window,name,OBJPROP_COLOR,Gray);
         ObjectSetInteger(window,name,OBJPROP_FONTSIZE,FONTSIZE);
         ObjectSetString(window,name,OBJPROP_FONT,"WingDings");
         ObjectSetString(window,name,OBJPROP_TEXT,"у");
        }
     }

  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| ¬озвращает значение цены открыти€ указанного параметром shift бара 
//| с соответствующего графика (symbol, timeframe).                  |
//+------------------------------------------------------------------+

double iOpenMQL4(string symbol,int tf,int index)
  {
   if(index<0) return(-1);
   double Arr[];
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   if(CopyOpen(symbol,timeframe,index,1,Arr)>0)
      return(Arr[0]);
   else return(-1);
  }
//+------------------------------------------------------------------+
//| ¬озвращает значение цены закрыти€ указанного параметром shift бара 
//| с соответствующего графика (symbol, timeframe).                  |
//+------------------------------------------------------------------+
double iCloseMQL4(string symbol,int tf,int index)
  {
   if(index<0) return(-1);
   double Arr[];
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   if(CopyClose(symbol,timeframe,index,1,Arr)>0)
      return(Arr[0]);
   else return(-1);
  }
//+------------------------------------------------------------------+
//| ¬озвращает “айм‘рейм по ћинутам                                  |
//+------------------------------------------------------------------+

ENUM_TIMEFRAMES TFMigrate(int tf)
  {
   switch(tf)
     {
      case 0: return(PERIOD_CURRENT);
      case 1: return(PERIOD_M1);
      case 5: return(PERIOD_M5);
      case 15: return(PERIOD_M15);
      case 30: return(PERIOD_M30);
      case 60: return(PERIOD_H1);
      case 240: return(PERIOD_H4);
      case 1440: return(PERIOD_D1);
      case 10080: return(PERIOD_W1);
      case 43200: return(PERIOD_MN1);

      case 2: return(PERIOD_M2);
      case 3: return(PERIOD_M3);
      case 4: return(PERIOD_M4);
      case 6: return(PERIOD_M6);
      case 10: return(PERIOD_M10);
      case 12: return(PERIOD_M12);
      case 16385: return(PERIOD_H1);
      case 16386: return(PERIOD_H2);
      case 16387: return(PERIOD_H3);
      case 16388: return(PERIOD_H4);
      case 16390: return(PERIOD_H6);
      case 16392: return(PERIOD_H8);
      case 16396: return(PERIOD_H12);
      case 16408: return(PERIOD_D1);
      case 32769: return(PERIOD_W1);
      case 49153: return(PERIOD_MN1);
      default: return(PERIOD_CURRENT);
     }
  }
//+------------------------------------------------------------------+
// ==========================================================================================================================================================================================================
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// 
//                             ѕанель дл€ ¬кладки Ord  
// 
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// ==========================================================================================================================================================================================================
int ysdvigORD=25;


void VkladkaOrders(int x,int y)

  {

//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
   ButtonCreate("DELETE",Black,Khaki,x+119,y+10,70,20,"DELETE",10);

   ButtonCreate("BUYSTOP",Black,LightBlue,x+35,y+10,80,20,"BUYSTOP",10);
   ButtonCreate("SELLSTOP",Black,LightPink,x+35,y+35,80,20,"SELLSTOP",10);
   ButtonCreate("BUYLIMIT",Black,LightBlue,x+193,y+10,80,20,"BUYLIMIT",10);
   ButtonCreate("SELLLIMIT",Black,LightPink,x+193,y+35,80,20,"SELLLIMIT",10);

//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+



   ObjectCreate(window,"BGLS",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGLS",OBJPROP_XDISTANCE,x+15+19);
   ObjectSetInteger(window,"BGLS",OBJPROP_YDISTANCE,y+ynew+34+ysdvigORD);
   ObjectSetInteger(window,"BGLS",OBJPROP_XSIZE,66);
   ObjectSetInteger(window,"BGLS",OBJPROP_YSIZE,19);
   ObjectSetString(window,"BGLS",OBJPROP_BMPFILE,"ls.bmp");

   ObjectCreate(window,"BGTP",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGTP",OBJPROP_XDISTANCE,x+95+20);
   ObjectSetInteger(window,"BGTP",OBJPROP_YDISTANCE,y+ynew+34+ysdvigORD);
   ObjectSetInteger(window,"BGTP",OBJPROP_XSIZE,66);
   ObjectSetInteger(window,"BGTP",OBJPROP_YSIZE,19);
   ObjectSetString(window,"BGTP",OBJPROP_BMPFILE,"tp.bmp");

   ObjectCreate(window,"BGSL",OBJ_BITMAP_LABEL,0,100,100);
   ObjectSetInteger(window,"BGSL",OBJPROP_XDISTANCE,x+178+20);
   ObjectSetInteger(window,"BGSL",OBJPROP_YDISTANCE,y+ynew+34+ysdvigORD);
   ObjectSetInteger(window,"BGSL",OBJPROP_XSIZE,66);
   ObjectSetInteger(window,"BGSL",OBJPROP_YSIZE,19);
   ObjectSetString(window,"BGSL",OBJPROP_BMPFILE,"sl.bmp");

//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+




   int a=10;
   window=0;
   int bb3=20;
   int xx=10;
   int xx2=130;
   int SizeY=20;
   color BG_CLOSEDELETE=Red;
   lotss=NormalizeDouble(lot,2);
   int xLOTS=10;

//
/*
   ObjectCreate(window,"OrdersGRIDER",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"OrdersGRIDER",OBJPROP_XDISTANCE,x+35);
   ObjectSetInteger(window,"OrdersGRIDER",OBJPROP_YDISTANCE,ynew+y+70+a+ysdvigORD);
   ObjectSetInteger(window,"OrdersGRIDER",OBJPROP_XSIZE,240);
   ObjectSetInteger(window,"OrdersGRIDER",OBJPROP_YSIZE,30);
   ObjectSetInteger(window,"OrdersGRIDER",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"OrdersGRIDER",OBJPROP_BGCOLOR,WhiteSmoke);
*/
   ObjectCreate(window,"OrderLevelStopGRID",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_XDISTANCE,x+119);
   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_YDISTANCE,120+ysdvigORD);
   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_BGCOLOR,White);

   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_XSIZE,69);
   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_YSIZE,20);
   ObjectSetString(window,"OrderLevelStopGRID",OBJPROP_TEXT,DoubleToString(GRIDNUMBER,0));
   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_FONTSIZE,15);
   ObjectSetInteger(window,"OrderLevelStopGRID",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

  // LabelCreate("OrderTextGRID",Green,x+50,ynew+y+75+a+ysdvigORD,"GRID amount ");

//+------------------------------------------------------------------+
//| ”величение/уменьшение ”ровн€ траллингстопа                       |
//+------------------------------------------------------------------+

   ObjectCreate(window,"OrderLevelStop",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_XDISTANCE,x+24+xLOTS);
   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvigORD);
   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_BGCOLOR,White);

   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_XSIZE,60);
   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_YSIZE,20);
   ObjectSetString(window,"OrderLevelStop",OBJPROP_TEXT,DoubleToString(OrderLevelStop,0));
   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_FONTSIZE,15);
   ObjectSetInteger(window,"OrderLevelStop",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"OrderLevelStop вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_XDISTANCE,x+84+xLOTS);
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvigORD);
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"OrderLevelStop вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"OrderLevelStop вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"OrderLevelStop вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"OrderLevelStop вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_XDISTANCE,x+84+xLOTS);
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_YDISTANCE,ynew+y+55+a+ysdvigORD);
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"OrderLevelStop вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"OrderLevelStop вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"OrderLevelStop вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//| ”величение/уменьшение лотов                                      |
//+------------------------------------------------------------------+


   ObjectCreate(window,"Lots",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"Lots",OBJPROP_XDISTANCE,x+110+xLOTS);
   ObjectSetInteger(window,"Lots",OBJPROP_YDISTANCE,ynew+y-5+a);
   ObjectSetInteger(window,"Lots",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"Lots",OBJPROP_BGCOLOR,Lavender);

   ObjectSetInteger(window,"Lots",OBJPROP_XSIZE,50);
   ObjectSetInteger(window,"Lots",OBJPROP_YSIZE,20);
   ObjectSetString(window,"Lots",OBJPROP_TEXT,DoubleToString(lotss,2));
   ObjectSetInteger(window,"Lots",OBJPROP_FONTSIZE,15);
   ObjectSetInteger(window,"Lots",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"Ћот вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_XDISTANCE,x+162+xLOTS);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_YDISTANCE,ynew+y-5+a);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"Ћот вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"Ћот вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"Ћот вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"Ћот вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_XDISTANCE,x+162+xLOTS);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_YDISTANCE,ynew+y+5+a);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"Ћот вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"Ћот вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"Ћот вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//| ”величение/уменьшение TakeProfit                                 |
//+------------------------------------------------------------------+
   int xTakeProfit=10;

   ObjectCreate(window,"TakeProfit",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_XDISTANCE,x+108+xTakeProfit);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvigORD);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_BGCOLOR,White);

   ObjectSetInteger(window,"TakeProfit",OBJPROP_XSIZE,55);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_YSIZE,20);
   ObjectSetString(window,"TakeProfit",OBJPROP_TEXT,DoubleToString(TakeProfit,0));
   ObjectSetInteger(window,"TakeProfit",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"TakeProfit",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"TakeProfit вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_XDISTANCE,x+164+xTakeProfit);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvigORD);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"TakeProfit вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"TakeProfit вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"TakeProfit вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"TakeProfit вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_XDISTANCE,x+164+xTakeProfit);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_YDISTANCE,ynew+y+55+a+ysdvigORD);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"TakeProfit вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"TakeProfit вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"TakeProfit вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//| ”величение/уменьшение StopLoss                                   |
//+------------------------------------------------------------------+
   int xStopLoss=10;

   ObjectCreate(window,"StopLoss",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"StopLoss",OBJPROP_XDISTANCE,x+190+xStopLoss);
   ObjectSetInteger(window,"StopLoss",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvigORD);
   ObjectSetInteger(window,"StopLoss",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"StopLoss",OBJPROP_BGCOLOR,White);

   ObjectSetInteger(window,"StopLoss",OBJPROP_XSIZE,55);
   ObjectSetInteger(window,"StopLoss",OBJPROP_YSIZE,20);
   ObjectSetString(window,"StopLoss",OBJPROP_TEXT,DoubleToString(StopLoss,0));
   ObjectSetInteger(window,"StopLoss",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"StopLoss",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"StopLoss вверх",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_BGCOLOR,Green);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_XDISTANCE,x+246+xStopLoss);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_YDISTANCE,ynew+y+45+a+ysdvigORD);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_YSIZE,10);
   ObjectSetString(window,"StopLoss вверх",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"StopLoss вверх",OBJPROP_TEXT,"+");
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"StopLoss вверх",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ObjectCreate(window,"StopLoss вниз",OBJ_BUTTON,0,100,100);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_COLOR,White);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_BGCOLOR,Red);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_XDISTANCE,x+246+xStopLoss);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_YDISTANCE,ynew+y+55+a+ysdvigORD);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_XSIZE,15);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_YSIZE,10);
   ObjectSetString(window,"StopLoss вниз",OBJPROP_FONT,"Arial");
   ObjectSetString(window,"StopLoss вниз",OBJPROP_TEXT,"-");
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_FONTSIZE,10);
   ObjectSetInteger(window,"StopLoss вниз",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+

//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ќткрывает позицию OpenSELLSTOP                                 |
//+----------------------------------------------------------------------------+  
void OpenSELLSTOP()
  {
   for(int i=1;i<=GRIDNUMBER;i++)

     {
      if(OrderLevelStop!=0&&OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderPricePip=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
      else OrderPricePip=OrderLevelStop;
      request.symbol = symToWork;
      request.volume = lotss;
      request.action=TRADE_ACTION_PENDING; // операци€ с рынка
      if(TakeProfit==0)request.tp=0;else request.tp=SymbolInfoDouble(symToWork,SYMBOL_BID)-OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT)-TakeProfit*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      if(StopLoss==0)request.sl=0;else request.sl=SymbolInfoDouble(symToWork,SYMBOL_BID)-OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT)+StopLoss*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.deviation=(ulong)((SymbolInfoDouble(symToWork,SYMBOL_ASK)-SymbolInfoDouble(symToWork,SYMBOL_BID))/SymbolInfoDouble(symToWork,SYMBOL_POINT)); // по спреду 
                                                                                                                                                           // request.type_filling=ORDER_FILLING_CANCEL;
      request.type=ORDER_TYPE_SELL_STOP;
      request.price=SymbolInfoDouble(symToWork,SYMBOL_BID)-OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.type_time=ORDER_TIME_GTC;

      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");

     }
  }
//+------------------------------------------------------------------+
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ќткрывает позицию OpenBUYSTOP                                  |
//+----------------------------------------------------------------------------+ 

void OpenBUYSTOP()
  {
   for(int i=1;i<=GRIDNUMBER;i++)

     {
      if(OrderLevelStop!=0&&OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderPricePip=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
      else OrderPricePip=OrderLevelStop;
      request.symbol= symToWork;
      request.volume= lotss;
      request.action=TRADE_ACTION_PENDING; // операци€ с рынка
      if(TakeProfit==0)request.tp=0;else request.tp=SymbolInfoDouble(symToWork,SYMBOL_ASK)+TakeProfit*SymbolInfoDouble(symToWork,SYMBOL_POINT)+OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      if(StopLoss==0)request.sl=0;else request.sl=SymbolInfoDouble(symToWork,SYMBOL_ASK)-StopLoss*SymbolInfoDouble(symToWork,SYMBOL_POINT)+OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.deviation=(ulong)((SymbolInfoDouble(symToWork,SYMBOL_ASK)-SymbolInfoDouble(symToWork,SYMBOL_BID))/SymbolInfoDouble(symToWork,SYMBOL_POINT)); // по спреду 
                                                                                                                                                           //request.type_filling=ORDER_FILLING_CANCEL;
      request.type=ORDER_TYPE_BUY_STOP;
      request.price=SymbolInfoDouble(symToWork,SYMBOL_ASK)+OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.type_time=ORDER_TIME_GTC;
      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");
     }
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ќткрывает позицию OpenSELLLIMT                                 |
//+----------------------------------------------------------------------------+  
void OpenSELLLIMIT()
  {
   for(int i=1;i<=GRIDNUMBER;i++)

     {
      if(OrderLevelStop!=0&&OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderPricePip=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
      else OrderPricePip=OrderLevelStop;
      request.symbol = symToWork;
      request.volume = lotss;
      request.action=TRADE_ACTION_PENDING; // операци€ с рынка
      if(TakeProfit==0)request.tp=0;else request.tp=SymbolInfoDouble(symToWork,SYMBOL_BID)+OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT)-TakeProfit*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      if(StopLoss==0)request.sl=0;else request.sl=SymbolInfoDouble(symToWork,SYMBOL_BID)+OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT)+StopLoss*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.deviation=(ulong)((SymbolInfoDouble(symToWork,SYMBOL_ASK)-SymbolInfoDouble(symToWork,SYMBOL_BID))/SymbolInfoDouble(symToWork,SYMBOL_POINT)); // по спреду 
                                                                                                                                                           // request.type_filling=ORDER_FILLING_CANCEL;
      request.type=ORDER_TYPE_SELL_LIMIT;
      request.price=SymbolInfoDouble(symToWork,SYMBOL_BID)+OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.type_time=ORDER_TIME_GTC;

      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");
     }
  }
//+----------------------------------------------------------------------------+
//|  јвтор    : ¬ладислав, Expforex  http://expforex.at.ua                     |
//+----------------------------------------------------------------------------+
//|  ќписание : ќткрывает позицию OpenBUYLIMIT                                  |
//+----------------------------------------------------------------------------+ 

void OpenBUYLIMIT()
  {
   for(int i=1;i<=GRIDNUMBER;i++)

     {
      if(OrderLevelStop!=0&&OrderLevelStop<SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL))OrderPricePip=(int)SymbolInfoInteger(symToWork,SYMBOL_TRADE_STOPS_LEVEL);
      else OrderPricePip=OrderLevelStop;
      request.symbol= symToWork;
      request.volume= lotss;
      request.action=TRADE_ACTION_PENDING; // операци€ с рынка
      if(TakeProfit==0)request.tp=0;else request.tp=SymbolInfoDouble(symToWork,SYMBOL_ASK)+TakeProfit*SymbolInfoDouble(symToWork,SYMBOL_POINT)-OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      if(StopLoss==0)request.sl=0;else request.sl=SymbolInfoDouble(symToWork,SYMBOL_ASK)-StopLoss*SymbolInfoDouble(symToWork,SYMBOL_POINT)-OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.deviation=(ulong)((SymbolInfoDouble(symToWork,SYMBOL_ASK)-SymbolInfoDouble(symToWork,SYMBOL_BID))/SymbolInfoDouble(symToWork,SYMBOL_POINT)); // по спреду 
                                                                                                                                                           //request.type_filling=ORDER_FILLING_CANCEL;
      request.type=ORDER_TYPE_BUY_LIMIT;
      request.price=SymbolInfoDouble(symToWork,SYMBOL_ASK)-OrderPricePip*i*SymbolInfoDouble(symToWork,SYMBOL_POINT);
      request.type_time=ORDER_TIME_GTC;
      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");
     }
  }
//+------------------------------------------------------------------+
//|  ”далим все отложки:                                             |
//+------------------------------------------------------------------+
void DeleteAllStops()
  {
   int pos=OrdersTotal(); // получим количество открытых позиций
   for(int ip=pos;ip>=0;ip--)
     {

      DeletePositionStops(ip);

     }

  }
//+------------------------------------------------------------------+
//|   ”далим отложенный ордер                                        |
//+------------------------------------------------------------------+
void DeletePositionStops(int ip)
  {

   request.action=TRADE_ACTION_REMOVE; // операци€ с рынка
   request.order=OrderGetTicket(ip);
   if(OrderGetString(ORDER_SYMBOL)!=symToWork)return;

   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");

  }
//+------------------------------------------------------------------+

// ==========================================================================================================================================================================================================
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// 
//                             ѕанель дл€ ¬кладки Func  
// 
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// ==========================================================================================================================================================================================================

void VKL_OTHERS(int x=0)
  {

   int y=10;
//+------------------------------------------------------------------+
//|   Ѕлок закрыти€ по прибыли и/или убытку                          |
//+------------------------------------------------------------------+

   ObjectCreate(window,"OthersPROF",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"OthersPROF",OBJPROP_XDISTANCE,x+6);
   ObjectSetInteger(window,"OthersPROF",OBJPROP_YDISTANCE,y+2);
   ObjectSetInteger(window,"OthersPROF",OBJPROP_XSIZE,258);
   ObjectSetInteger(window,"OthersPROF",OBJPROP_YSIZE,50);
   ObjectSetInteger(window,"OthersPROF",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"OthersPROF",OBJPROP_BGCOLOR,WhiteSmoke);

   ObjectCreate(window,"USEProfit",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"USEProfit",OBJPROP_XDISTANCE,x+10);
   ObjectSetInteger(window,"USEProfit",OBJPROP_YDISTANCE,y+5);
   ObjectSetInteger(window,"USEProfit",OBJPROP_COLOR,Black);
   ObjectSetInteger(window,"USEProfit",OBJPROP_BGCOLOR,White);
   ObjectSetInteger(window,"USEProfit",OBJPROP_XSIZE,250);
   ObjectSetInteger(window,"USEProfit",OBJPROP_YSIZE,17);
   ObjectSetString(window,"USEProfit",OBJPROP_TEXT,"CLOSE if PROFIT and/or LOSS");
   ObjectSetInteger(window,"USEProfit",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"USEProfit",OBJPROP_READONLY,true); // иначе нажать на нее нельз€

   ButtonCreate("USEProfitCLOSE",Black,Khaki,x+220,y+25,40,20,"USE",10);

//void LabelCreate(string name,color TextColor,int Xdist,int Ydist,string Text,int FONTSIZE=12)

   LabelCreate("ZNAK>",Green,x+15,y+25,">");

   ObjectCreate(window,"USEProfitCLOSE_EDIT",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_XDISTANCE,x+30);
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_YDISTANCE,y+25);
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_BGCOLOR,White);
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_XSIZE,55);
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_YSIZE,20);
   ObjectSetString(window,"USEProfitCLOSE_EDIT",OBJPROP_TEXT,DoubleToString(Profit,0));
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"USEProfitCLOSE_EDIT",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   LabelCreate("ZNAK<",Red,x+90,y+25,"<");

   ObjectCreate(window,"USELossCLOSE_EDIT",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_XDISTANCE,x+105);
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_YDISTANCE,y+25);
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_COLOR,Red);
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_BGCOLOR,White);
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_XSIZE,55);
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_YSIZE,20);
   ObjectSetString(window,"USELossCLOSE_EDIT",OBJPROP_TEXT,DoubleToString(Loss,0));
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"USELossCLOSE_EDIT",OBJPROP_SELECTABLE,false); // иначе нажать на нее нельз€

   ButtonCreate("TypeClose_DOLLAR",Black,MistyRose,x+160,y+25,20,20,"$",10);
   ButtonCreate("TypeClose_PERCENT",Black,OldLace,x+180,y+25,20,20,"%",10);
   ButtonCreate("TypeClose_POINT",Black,Honeydew,x+200,y+25,20,20,"P",10);

//+------------------------------------------------------------------+
//|   Ѕлок Custom1                                                    |
//+------------------------------------------------------------------+

   ObjectCreate(window,"OthersGRIDER",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"OthersGRIDER",OBJPROP_XDISTANCE,x+6);
   ObjectSetInteger(window,"OthersGRIDER",OBJPROP_YDISTANCE,y+59);
   ObjectSetInteger(window,"OthersGRIDER",OBJPROP_XSIZE,258);
   ObjectSetInteger(window,"OthersGRIDER",OBJPROP_YSIZE,50);
   ObjectSetInteger(window,"OthersGRIDER",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"OthersGRIDER",OBJPROP_BGCOLOR,WhiteSmoke);

   ObjectCreate(window,"Custom1",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"Custom1",OBJPROP_XDISTANCE,x+10);
   ObjectSetInteger(window,"Custom1",OBJPROP_YDISTANCE,y+62);
   ObjectSetInteger(window,"Custom1",OBJPROP_COLOR,Black);
   ObjectSetInteger(window,"Custom1",OBJPROP_BGCOLOR,White);
   ObjectSetInteger(window,"Custom1",OBJPROP_XSIZE,250);
   ObjectSetInteger(window,"Custom1",OBJPROP_YSIZE,17);
   ObjectSetString(window,"Custom1",OBJPROP_TEXT,"FREE CUSTOM BLOCK 1");
   ObjectSetInteger(window,"Custom1",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"Custom1",OBJPROP_READONLY,true); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//|   Ѕлок Custom2                                                    |
//+------------------------------------------------------------------+

   ObjectCreate(window,"OthersGRIDER2",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"OthersGRIDER2",OBJPROP_XDISTANCE,x+6);
   ObjectSetInteger(window,"OthersGRIDER2",OBJPROP_YDISTANCE,y+116);
   ObjectSetInteger(window,"OthersGRIDER2",OBJPROP_XSIZE,258);
   ObjectSetInteger(window,"OthersGRIDER2",OBJPROP_YSIZE,50);
   ObjectSetInteger(window,"OthersGRIDER2",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"OthersGRIDER2",OBJPROP_BGCOLOR,WhiteSmoke);

   ObjectCreate(window,"Custom2",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"Custom2",OBJPROP_XDISTANCE,x+10);
   ObjectSetInteger(window,"Custom2",OBJPROP_YDISTANCE,y+119);
   ObjectSetInteger(window,"Custom2",OBJPROP_COLOR,Black);
   ObjectSetInteger(window,"Custom2",OBJPROP_BGCOLOR,White);
   ObjectSetInteger(window,"Custom2",OBJPROP_XSIZE,250);
   ObjectSetInteger(window,"Custom2",OBJPROP_YSIZE,17);
   ObjectSetString(window,"Custom2",OBJPROP_TEXT,"FREE CUSTOM BLOCK 2");
   ObjectSetInteger(window,"Custom2",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"Custom2",OBJPROP_READONLY,true); // иначе нажать на нее нельз€

//+------------------------------------------------------------------+
//|   Ѕлок Custom3                                                    |
//+------------------------------------------------------------------+

   ObjectCreate(window,"OthersGRIDER3",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"OthersGRIDER3",OBJPROP_XDISTANCE,x+6);
   ObjectSetInteger(window,"OthersGRIDER3",OBJPROP_YDISTANCE,y+173);
   ObjectSetInteger(window,"OthersGRIDER3",OBJPROP_XSIZE,258);
   ObjectSetInteger(window,"OthersGRIDER3",OBJPROP_YSIZE,50);
   ObjectSetInteger(window,"OthersGRIDER3",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"OthersGRIDER3",OBJPROP_BGCOLOR,WhiteSmoke);

   ObjectCreate(window,"Custom3",OBJ_EDIT,0,100,100);
   ObjectSetInteger(window,"Custom3",OBJPROP_XDISTANCE,x+10);
   ObjectSetInteger(window,"Custom3",OBJPROP_YDISTANCE,y+176);
   ObjectSetInteger(window,"Custom3",OBJPROP_COLOR,Black);
   ObjectSetInteger(window,"Custom3",OBJPROP_BGCOLOR,White);
   ObjectSetInteger(window,"Custom3",OBJPROP_XSIZE,250);
   ObjectSetInteger(window,"Custom3",OBJPROP_YSIZE,17);
   ObjectSetString(window,"Custom3",OBJPROP_TEXT,"FREE CUSTOM BLOCK 3");
   ObjectSetInteger(window,"Custom3",OBJPROP_FONTSIZE,13);
   ObjectSetInteger(window,"Custom3",OBJPROP_READONLY,true); // иначе нажать на нее нельз€

  }

//жжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжж
//жжжжжжжжжжжжжжжжжжж      ‘ункции закрыти€ по прибыли   жжжжжжжжжжжжжжжжжжжжжжжж
//жжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжжж

double price;
int    ordertype;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int startCloseBlock3()
  {
   double ask,bid,open,Prc2,point,buy_e=0,sell_e=0,Equity=0,_close=0;
   int Pips,buy_p=0,sell_p=0,flag=0;
   string com,mg;
   string  sy;
   sy=Symbol();
//-------------------------------смотрим открытые----------------------------------------------------------

   int pos=PositionsTotal(); // получим количество открытых позиций
   if(PositionsTotal()!=0)
     {
      for(int ip=pos;ip>=0;ip--)
        {

         int order_type2=(int)PositionGetInteger(POSITION_TYPE);
         double order_open_price2=PositionGetDouble(POSITION_PRICE_OPEN);
         double profit=PositionGetDouble(POSITION_PROFIT);

         if(order_type2==POSITION_TYPE_SELL)
           {
            point=SymbolInfoDouble(PositionGetString(POSITION_SYMBOL),SYMBOL_POINT);
            ask=MathRound(SymbolInfoDouble(PositionGetString(POSITION_SYMBOL),SYMBOL_ASK)/point);
            open=MathRound(order_open_price2/point);
            sell_e+=profit;
            sell_p+=(int)(open-ask);
           }
         if(order_type2==POSITION_TYPE_BUY)
           {
            point=SymbolInfoDouble(PositionGetString(POSITION_SYMBOL),SYMBOL_POINT);
            bid=MathRound(SymbolInfoDouble(PositionGetString(POSITION_SYMBOL),SYMBOL_ASK)/point);
            open=MathRound(order_open_price2/point);
            buy_e+=profit;
            buy_p+=(int)(bid-open);
           }
        }
      //-----------------------------------считаем--------------------------------------------------------------- 
      Pips=(buy_p+sell_p);
      Equity=(buy_e+sell_e);
      Prc2=NormalizeDouble((Equity*100)/AccountInfoDouble(ACCOUNT_BALANCE),2);
      //----------------------------------выбираем---------------------------------------------------------------
      switch(TypeofClose)
        {
         case 1: _close=Equity; com = "доллар"; break;
         case 2: _close=Prc2; com = "%баланс"; break;
         case 3: _close=Pips; com = "%пипсов"; break;
         default: com="Ќ≈ ” ј«јЌќ"; break;
        }
      //-------------------- омментарии--------------------------------------------------------------------------- 

      //---------------------------услови€ дл€ закрыти€--------------------------------------------------------------------- 
      if(_close>=Profit && Profit!=0){  flag=1; }
      if(_close<=Loss && Loss!=0){  flag=1; }
      //-----------------------------все позиции закрываем------------------------------------------------------------------
      if(flag>0)
        {
         CLOSEALLIFPROFIT();DELETEALLIFPROFIT();ObjectSetInteger(window,"USEProfitCLOSE",OBJPROP_STATE,false);Print("Close with -CLOSEIFPROFIT- #="+(string)_close);
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DELETEALLIFPROFIT()
  {
   int pos=OrdersTotal(); // получим количество открытых позиций
   for(int ip=pos;ip>=0;ip--)
     {

      request.action=TRADE_ACTION_REMOVE; // операци€ с рынка
      request.order=OrderGetTicket(ip);
      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");

     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CLOSEALLIFPROFIT()
  {

   int pos=PositionsTotal(); // получим количество открытых позиций
   for(int ip=pos;ip>=0;ip--)
     {
      string sSymbol=PositionGetSymbol(ip);
      if(PositionSelect(sSymbol)==true)
        {
         ClosePosition(sSymbol,ip);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ClosePosition(string sSymbol,int ip)
  {

   request.symbol = sSymbol;
   request.volume = PositionGetDouble( POSITION_VOLUME );
   request.action=TRADE_ACTION_DEAL; // операци€ с рынка
   request.tp=0;
   request.sl=0;
   request.deviation=(ulong)((SymbolInfoDouble(sSymbol,SYMBOL_ASK)-SymbolInfoDouble(sSymbol,SYMBOL_BID))/SymbolInfoDouble(sSymbol,SYMBOL_POINT)); // по спреду 
                                                                                                                                                  // request.type_filling=ORDER_FILLING_CANCEL;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
     {
      request.type=ORDER_TYPE_SELL;
      request.price=SymbolInfoDouble(sSymbol,SYMBOL_BID);
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
     {
      request.type=ORDER_TYPE_BUY;
      request.price=SymbolInfoDouble(sSymbol,SYMBOL_ASK);
     }

   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)PlaySound("ok.wav"); else PlaySound("stops.wav");

  }
//+----------------------------------------------------------------------

//

// ==========================================================================================================================================================================================================
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// 
//                             ѕанель дл€ ¬кладки Ind  
// 
// ∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆∆
// ==========================================================================================================================================================================================================

//+------------------------------------------------------------------+
//| ќбъ€вим переменные дл€ хранени€ сигналов индикаторов             |
//+------------------------------------------------------------------+
int SignalMA;
int SignalMACD;
int SignalPC;
int SignalACADX;
int SignalST;
int SignalRSI;
int SignalCCI;
int SignalWPR;
int SignalBB;
int SignalSDC;
int SignalPC2;
int SignalENV;
int SignalDC;
int SignalSC;
int SignalGC;
int SignalNRTR;
int SignalAL;
int SignalAMA;
int SignalAO;
int SignalICH;

//+------------------------------------------------------------------+
//| ќбъ€вим переменные дл€ хранени€ хэндлов индикаторов              |
//+------------------------------------------------------------------+
int h_ma1   = INVALID_HANDLE;
int h_ma2   = INVALID_HANDLE;
int h_macd  = INVALID_HANDLE;
int h_pc    = INVALID_HANDLE;
int h_pc2   = INVALID_HANDLE;
int h_acadx = INVALID_HANDLE;
int h_stoh  = INVALID_HANDLE;
int h_rsi   = INVALID_HANDLE;
int h_cci   = INVALID_HANDLE;
int h_wpr   = INVALID_HANDLE;
int h_bb    = INVALID_HANDLE;
int h_sdc   = INVALID_HANDLE;
int h_env   = INVALID_HANDLE;
int h_dc    = INVALID_HANDLE;
int h_sc    = INVALID_HANDLE;
int h_gc    = INVALID_HANDLE;
int h_nrtr  = INVALID_HANDLE;
int h_al    = INVALID_HANDLE;
int h_ama   = INVALID_HANDLE;
int h_ao    = INVALID_HANDLE;
int h_ich   = INVALID_HANDLE;
//+------------------------------------------------------------------+
//| ќбъ€вим необходимые массивы дл€ хранени€ данных индикаторов      |
//+------------------------------------------------------------------+
double ma1_buffer[];
double ma2_buffer[];
double macd1_buffer[];
double macd2_buffer[];
double pc1_buffer[];
double pc2_buffer[];
double pc1_buffer2[];
double pc2_buffer2[];
double acadx1_buffer[];
double acadx2_buffer[];
double stoh_buffer[];
double rsi_buffer[];
double cci_buffer[];
double wpr_buffer[];
double bb1_buffer[];
double bb2_buffer[];
double sdc1_buffer[];
double sdc2_buffer[];
double env1_buffer[];
double env2_buffer[];
double dc1_buffer[];
double dc2_buffer[];
double sc1_buffer[];
double sc2_buffer[];
double gc1_buffer[];
double gc2_buffer[];
double nrtr1_buffer[];
double nrtr2_buffer[];
double al1_buffer[];
double al2_buffer[];
double al3_buffer[];
double ama_buffer[];
double ao_buffer[];
double ich1_buffer[];
double ich2_buffer[];
double Close[];
//+------------------------------------------------------------------+
//| функции формировани€ сигналов                                    |
//|  0 - нет сигнала                                                 |
//|  1 - сигнал на покупку                                           |
//| -1 - сигнал на продажу                                           |
//+------------------------------------------------------------------+
int TradeSignal_01(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
//--- ноль означает отсутствие сигнала
   int sig=0;

//--- проверим хендлы индикаторов
   if(h_ma1==INVALID_HANDLE)//--- если хэндл невалидный
     {
      //--- создадим его снова                                                      
      h_ma1=iMA(symToWork,TFMigrate(TF),periodma1,0,MAmethod,MAprice);
      //--- выходим из функции
      return(0);
     }
   else //--- если хэндл валидный 
     {
      //--- копируем значени€ из индикатора в массив
      if(CopyBuffer(h_ma1,0,0,3,ma1_buffer)<3) //--- и если данных меньше требуемых
         //--- выходим из функции
         return(0);
      //--- зададим индексацию в массиве как таймсерию                                   
      if(!ArraySetAsSeries(ma1_buffer,true))
         //--- в случае ошибки индексации выходим из функции
         return(0);
     }

   if(h_ma2==INVALID_HANDLE)//--- если хэндл невалидный
     {
      //--- создадим его снова                                                      
      h_ma2=iMA(symToWork,TFMigrate(TF),periodma2,0,MAmethod,MAprice);
      //--- выходим из функции
      return(0);
     }
   else //--- если хэндл валидный 
     {
      //--- копируем значени€ из индикатора в массив
      if(CopyBuffer(h_ma2,0,0,2,ma2_buffer)<2) //--- и если данных меньше требуемых
         //--- выходим из функции
         return(0);
      //--- зададим индексацию в массиве как таймсерию                                   
      if(!ArraySetAsSeries(ma1_buffer,true))
         //--- в случае ошибки индексации выходим из функции
         return(0);
     }

//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(ma1_buffer[2]<ma2_buffer[1] && ma1_buffer[1]>ma2_buffer[1])
      sig=1;
   else if(ma1_buffer[2]>ma2_buffer[1] && ma1_buffer[1]<ma2_buffer[1])
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_02(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_macd==INVALID_HANDLE)
     {
      h_macd=iMACD(symToWork,TFMigrate(TF),FastMACD,SlowMACD,MACDSMA,MACDprice);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_macd,0,0,2,macd1_buffer)<2)
         return(0);
      if(CopyBuffer(h_macd,1,0,3,macd2_buffer)<3)
         return(0);
      if(!ArraySetAsSeries(macd1_buffer,true))
         return(0);
      if(!ArraySetAsSeries(macd2_buffer,true))
         return(0);
     }

//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(macd2_buffer[2]>macd1_buffer[1] && macd2_buffer[1]<macd1_buffer[1])
      sig=1;
   else if(macd2_buffer[2]<macd1_buffer[1] && macd2_buffer[1]>macd1_buffer[1])
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
int TradeSignal_05(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_stoh==INVALID_HANDLE)
     {
      h_stoh=iStochastic(symToWork,TFMigrate(TF),SOPeriodK,SOPeriodD,SOslowing,SOmethod,SOpricefield);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_stoh,0,0,3,stoh_buffer)<3)
         return(0);

      if(!ArraySetAsSeries(stoh_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(stoh_buffer[2]<20 && stoh_buffer[1]>20)
      sig=1;
   else if(stoh_buffer[2]>80 && stoh_buffer[1]<80)
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_06(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_rsi==INVALID_HANDLE)
     {
      h_rsi=iRSI(symToWork,TFMigrate(TF),RSIPeriod,RSIprice);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_rsi,0,0,3,rsi_buffer)<3)
         return(0);

      if(!ArraySetAsSeries(rsi_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(rsi_buffer[2]<30 && rsi_buffer[1]>30)
      sig=1;
   else if(rsi_buffer[2]>70 && rsi_buffer[1]<70)
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_07(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_cci==INVALID_HANDLE)
     {
      h_cci=iCCI(symToWork,TFMigrate(TF),CCIPeriod,CCIprice);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_cci,0,0,3,cci_buffer)<3)
         return(0);

      if(!ArraySetAsSeries(cci_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(cci_buffer[2]<-100 && cci_buffer[1]>-100)
      sig=1;
   else if(cci_buffer[2]>100 && cci_buffer[1]<100)
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_08(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_wpr==INVALID_HANDLE)
     {
      h_wpr=iWPR(symToWork,TFMigrate(TF),WPRPeriod);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_wpr,0,0,3,wpr_buffer)<3)
         return(0);

      if(!ArraySetAsSeries(wpr_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(wpr_buffer[2]<-80 && wpr_buffer[1]>-80)
      sig=1;
   else if(wpr_buffer[2]>-20 && wpr_buffer[1]<-20)
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_09(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_bb==INVALID_HANDLE)
     {
      h_bb=iBands(symToWork,TFMigrate(TF),BBPeriod,0,BBdeviation,BBprice);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_bb,1,0,2,bb1_buffer)<2)
         return(0);
      if(CopyBuffer(h_bb,2,0,2,bb2_buffer)<2)
         return(0);
      if(CopyClose(symToWork,TFMigrate(TF),0,3,Close)<3)
         return(0);
      if(!ArraySetAsSeries(bb1_buffer,true))
         return(0);
      if(!ArraySetAsSeries(bb2_buffer,true))
         return(0);
      if(!ArraySetAsSeries(Close,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(Close[2]<=bb2_buffer[1] && Close[1]>bb2_buffer[1])
      sig=1;
   else if(Close[2]>=bb1_buffer[1] && Close[1]<bb1_buffer[1])
                     sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
int TradeSignal_12(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_env==INVALID_HANDLE)
     {
      h_env=iEnvelopes(symToWork,TFMigrate(TF),ENVPeriod,0,ENVmethod,ENVprice,ENVdeviation);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_env,0,0,2,env1_buffer)<2)
         return(0);
      if(CopyBuffer(h_env,1,0,2,env2_buffer)<2)
         return(0);
      if(CopyClose(symToWork,TFMigrate(TF),0,3,Close)<3)
         return(0);
      if(!ArraySetAsSeries(env1_buffer,true))
         return(0);
      if(!ArraySetAsSeries(env2_buffer,true))
         return(0);
      if(!ArraySetAsSeries(Close,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(Close[2]<=env2_buffer[1] && Close[1]>env2_buffer[1])
      sig=1;
   else if(Close[2]>=env1_buffer[1] && Close[1]<env1_buffer[1])
                     sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int TradeSignal_17(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_al==INVALID_HANDLE)
     {
      h_al=iAlligator(symToWork,TFMigrate(TF),ALjawperiod,0,ALteethperiod,0,ALlipsperiod,0,ALmethod,ALprice);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_al,0,0,2,al1_buffer)<2)
         return(0);
      if(CopyBuffer(h_al,1,0,2,al2_buffer)<2)
         return(0);
      if(CopyBuffer(h_al,2,0,2,al3_buffer)<2)
         return(0);
      if(!ArraySetAsSeries(al1_buffer,true))
         return(0);
      if(!ArraySetAsSeries(al2_buffer,true))
         return(0);
      if(!ArraySetAsSeries(al3_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(al3_buffer[1]>al2_buffer[1] && al2_buffer[1]>al1_buffer[1])
      sig=1;
   else if(al3_buffer[1]<al2_buffer[1] && al2_buffer[1]<al1_buffer[1])
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_18(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_ama==INVALID_HANDLE)
     {
      h_ama=iAMA(symToWork,TFMigrate(TF),AMAperiod,AMAfastperiod,AMAslowperiod,0,AMAprice);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_ama,0,0,3,ama_buffer)<3)
         return(0);
      if(!ArraySetAsSeries(ama_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(ama_buffer[2]<ama_buffer[1])
      sig=1;
   else if(ama_buffer[2]>ama_buffer[1])
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_19(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_ao==INVALID_HANDLE)
     {
      h_ao=iAO(symToWork,TFMigrate(TF));
      return(0);
     }
   else
     {
      if(CopyBuffer(h_ao,1,0,20,ao_buffer)<20)
         return(0);
      if(!ArraySetAsSeries(ao_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(ao_buffer[1]==0)
      sig=1;
   else if(ao_buffer[1]==1)
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
int TradeSignal_20(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
   int sig=0;

   if(h_ich==INVALID_HANDLE)
     {
      h_ich=iIchimoku(symToWork,TFMigrate(TF),IKHtenkansen,IKHkijunsen,IKHsenkouspanb);
      return(0);
     }
   else
     {
      if(CopyBuffer(h_ich,0,0,2,ich1_buffer)<2)
         return(0);
      if(CopyBuffer(h_ich,1,0,2,ich2_buffer)<2)
         return(0);
      if(!ArraySetAsSeries(ich1_buffer,true))
         return(0);
      if(!ArraySetAsSeries(ich2_buffer,true))
         return(0);
     }
//--- проводим проверку услови€ и устанавливаем значение дл€ sig
   if(ich1_buffer[1]>ich2_buffer[1])
      sig=1;
   else if(ich1_buffer[1]<ich2_buffer[1])
      sig=-1;
   else sig=0;

//--- возвращаем торговый сигнал
   return(sig);
  }
//+------------------------------------------------------------------+
void IndCalc(string symToWork3="",int TF=0)
  {
   if(symToWork3=="")symToWork3=symToWork; if(TF==0)TF=TFToInd;
//--- создадим хэндлы индикаторов
   h_ma1=iMA(symToWork3,TFMigrate(TF),periodma1,0,MAmethod,MAprice);
   h_ma2=iMA(symToWork3,TFMigrate(TF),periodma2,0,MAmethod,MAprice);
   h_macd=iMACD(symToWork3,TFMigrate(TF),FastMACD,SlowMACD,MACDSMA,MACDprice);
   h_stoh=iStochastic(symToWork3,TFMigrate(TF),SOPeriodK,SOPeriodD,SOslowing,SOmethod,SOpricefield);
   h_rsi=iRSI(symToWork3,TFMigrate(TF),RSIPeriod,RSIprice);
   h_cci=iCCI(symToWork3,TFMigrate(TF),CCIPeriod,CCIprice);
   h_wpr=iWPR(symToWork3,TFMigrate(TF),WPRPeriod);
   h_bb=iBands(symToWork3,TFMigrate(TF),BBPeriod,0,BBdeviation,BBprice);
   h_env=iEnvelopes(symToWork3,TFMigrate(TF),ENVPeriod,0,ENVmethod,ENVprice,ENVdeviation);
   h_al=iAlligator(symToWork3,TFMigrate(TF),ALjawperiod,0,ALteethperiod,0,ALlipsperiod,0,ALmethod,ALprice);
   h_ama=iAMA(symToWork3,TFMigrate(TF),AMAperiod,AMAfastperiod,AMAslowperiod,0,AMAprice);
   h_ao=iAO(symToWork3,TFMigrate(TF));
   h_ich=iIchimoku(symToWork3,TFMigrate(TF),IKHtenkansen,IKHkijunsen,IKHsenkouspanb);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void VKL_IND(int x2=10,string symToWork3="",int TF=0)
  {
   IndCalc(symToWork3,TF);

   ObjectCreate(window,"IndBG",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"IndBG",OBJPROP_XDISTANCE,x2+5);
   ObjectSetInteger(window,"IndBG",OBJPROP_YDISTANCE,60);
   ObjectSetInteger(window,"IndBG",OBJPROP_XSIZE,240);
   ObjectSetInteger(window,"IndBG",OBJPROP_YSIZE,190);
   ObjectSetInteger(window,"IndBG",OBJPROP_BORDER_TYPE,1);
   ObjectSetInteger(window,"IndBG",OBJPROP_BGCOLOR,WhiteSmoke);

   ObjectCreate(window,"IndBG2",OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(window,"IndBG2",OBJPROP_XDISTANCE,x2+5);
   ObjectSetInteger(window,"IndBG2",OBJPROP_YDISTANCE,45);
   ObjectSetInteger(window,"IndBG2",OBJPROP_XSIZE,240);
   ObjectSetInteger(window,"IndBG2",OBJPROP_YSIZE,25);
   ObjectSetInteger(window,"IndBG2",OBJPROP_BORDER_TYPE,2);
   ObjectSetInteger(window,"IndBG2",OBJPROP_BGCOLOR,OldLace);

   LabelCreate("M1_IND",Black,x2+10,50,"M1",10);
   LabelCreate("M5_IND",Black,x2+35,50,"M5",10);
   LabelCreate("M15_IND",Black,x2+60,50,"M15",10);
   LabelCreate("M30_IND",Black,x2+90,50,"M30",10);
   LabelCreate("H1_IND",Black,x2+120,50,"H1",10);
   LabelCreate("H4_IND",Black,x2+145,50,"H4",10);
   LabelCreate("D1_IND",Black,x2+170,50,"D1",10);
   LabelCreate("W1_IND",Black,x2+195,50,"W1",10);
   LabelCreate("MN_IND",Black,x2+220,50,"MN",10);



//---присваеваем переменной значение сигнала
   SignalMA    = TradeSignal_01(symToWork3,TF);
   SignalMACD  = TradeSignal_02(symToWork3,TF);
   SignalST    = TradeSignal_05(symToWork3,TF);
   SignalRSI   = TradeSignal_06(symToWork3,TF);
   SignalCCI   = TradeSignal_07(symToWork3,TF);
   SignalWPR   = TradeSignal_08(symToWork3,TF);
   SignalBB    = TradeSignal_09(symToWork3,TF);
   SignalENV   = TradeSignal_12(symToWork3,TF);
   SignalAL    = TradeSignal_17(symToWork3,TF);
   SignalAMA   = TradeSignal_18(symToWork3,TF);
   SignalAO    = TradeSignal_19(symToWork3,TF);
   SignalICH   = TradeSignal_20(symToWork3,TF);

//--- рисуем графические объекты на графике в верхнем левом углу
   int size=((int)14);
   int i=0;
   int x=x2;
   int y=55;
   int fz=size-2;

   y+=size;
   SetLabel("arrow"+(string)i,arrow(SignalMA),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalMA));
   x+=size;
   SetLabel("label"+(string)i,"Moving Average",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalMACD),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalMACD));
   x+=size;
   SetLabel("label"+(string)i,"MACD",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalST),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalST));
   x+=size;
   SetLabel("label"+(string)i,"Stochastic Oscillator",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalRSI),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalRSI));
   x+=size;
   SetLabel("label"+(string)i,"RSI",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalCCI),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalCCI));
   x+=size;
   SetLabel("label"+(string)i,"CCI",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalWPR),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalWPR));
   x+=size;
   SetLabel("label"+(string)i,"WPR",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalBB),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalBB));
   x+=size;
   SetLabel("label"+(string)i,"Bollinger Bands",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalENV),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalENV));
   x+=size;
   SetLabel("label"+(string)i,"Envelopes",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalAL),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalAL));
   x+=size;
   SetLabel("label"+(string)i,"Alligator",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalAMA),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalAMA));
   x+=size;
   SetLabel("label"+(string)i,"AMA",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalAO),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalAO));
   x+=size;
   SetLabel("label"+(string)i,"Awesome oscillator",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);
   i++;y+=size;x=10;
   SetLabel("arrow"+(string)i,arrow(SignalICH),CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y+4,"Wingdings",fz-2,0,Colorarrow(SignalICH));
   x+=size;
   SetLabel("label"+(string)i,"Ichimoku Kinko Hyo",CORNER_RIGHT_UPPER,ANCHOR_RIGHT_UPPER,x2+x,y,"Arial",fz,0,BlueViolet);

  }
//+---------------------------------------------------------------------------------------------------+
//| ‘ункци€ дл€ создани€ текстового объекта Label                                                     |
//|    ѕараметры:                                                                                     |                                                          
//|    nm - наименование объекта                                                                      |                                               
//|    tx - текст                                                                                     |
//|    cn - угол графика дл€ прив€зки графического объекта                                            |
//|         CORNER_LEFT_UPPER - ÷ентр координат в левом верхнем углу графика                          |
//|         CORNER_LEFT_LOWER - ÷ентр координат в левом нижнем углу графика                           |
//|         CORNER_RIGHT_LOWER - ÷ентр координат в правом нижнем углу графика                         |
//|         CORNER_RIGHT_UPPER - ÷ентр координат в правом верхнем углу графика                        |
//|    cr - положение точки прив€зки графического объекта                                             |
//|         ANCHOR_LEFT_UPPER - “очка прив€зки в левом верхнем углу                                   |
//|         ANCHOR_LEFT - “очка прив€зки слева по центру                                              |
//|         ANCHOR_LEFT_LOWER - “очка прив€зки в левом нижнем углу                                    |
//|         ANCHOR_LOWER - “очка прив€зки снизу по центру                                             |
//|         ANCHOR_RIGHT_LOWER - “очка прив€зки в правом нижнем углу                                  |
//|         ANCHOR_RIGHT - “очка прив€зки справа по центру                                            |
//|         ANCHOR_RIGHT_UPPER - “очка прив€зки в правом верхнем углу                                 |
//|         ANCHOR_UPPER - “очка прив€зки сверху по центру                                            |
//|         ANCHOR_CENTER - “очка прив€зки строго по центру объекта                                   |
//|    xd - координата X в пикселах                                                                   |                                           
//|    yd - координата Y в пикселах                                                                   |
//|    fn - наименование шрифта                                                                       |                    
//|    fs - размер шрифта в пикселах                                                                  | 
//|    yg - угол наклона текста в градусах. со знаком минус по часовой, со знаком плюс против часовой |                                                                    
//|    ct - цвет текста                                                                               |
//+---------------------------------------------------------------------------------------------------+
void SetLabel(string nm,string tx,ENUM_BASE_CORNER cn,ENUM_ANCHOR_POINT cr,int xd,int yd,string fn,int fs,double yg,color ct)export
  {
   if(fs<1)fs=1;
   cn=CORNER_LEFT_UPPER;
   cr=ANCHOR_LEFT_UPPER;
   if(ObjectFind(0,nm)<0)ObjectCreate(0,nm,OBJ_LABEL,0,0,0);  //--- создадим объект Label
   ObjectSetString (0,nm,OBJPROP_TEXT,tx);                    //--- установим текст дл€ объекта Label 
   ObjectSetInteger(0,nm,OBJPROP_CORNER,cn);                  //--- установим прив€зку к углу графика              
   ObjectSetInteger(0,nm,OBJPROP_ANCHOR,cr);                  //--- установим положение точки прив€зки графического объекта
   ObjectSetInteger(0,nm,OBJPROP_XDISTANCE,xd);               //--- установим координату X
   ObjectSetInteger(0,nm,OBJPROP_YDISTANCE,yd);               //--- установим координату Y
   ObjectSetString (0,nm,OBJPROP_FONT,fn);                    //--- установим шрифт надписи
   ObjectSetInteger(0,nm,OBJPROP_FONTSIZE,fs);                //--- установим размер шрифта    
   ObjectSetDouble (0,nm,OBJPROP_ANGLE,yg);                   //--- установим угол наклона
   ObjectSetInteger(0,nm,OBJPROP_COLOR,ct);                   //--- зададим цвет текста
   ObjectSetInteger(0,nm,OBJPROP_SELECTABLE,false);           //--- запретим выделение объекта мышкой   
  }
//+------------------------------------------------------------------+
//| функци€ дл€ определени€ типа стрелки по коду шрифта  Wingdings   |
//+------------------------------------------------------------------+
string arrow(int sig)export
  {
   switch(sig)
     {
      case  0: return(CharToString(251));
      case  1: return(CharToString(233));
      case -1: return(CharToString(234));
     }
   return((string)0);
  }
//+------------------------------------------------------------------+
//| функци€ дл€ определени€ цвета стрелки                            |
//+------------------------------------------------------------------+
color Colorarrow(int sig)export
  {
   switch(sig)
     {
      case -1: return(Red);
      case  0:  return(MediumAquamarine);
      case  1:  return(Blue);
     }
   return(0);
  }
//+------------------------------------------------------------------+
