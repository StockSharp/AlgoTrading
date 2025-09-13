//
// Aussie Surfer Expert Advisor
//
// Created by Aussie Surfer Ltd
//
// Copyright 2023, Aussie Surfer Ltd.
//
// Risk Disclosure
//
// Futures and forex trading contains substantial risk and is not for every investor.
// An investor could potentially lose all or more than the initial investment.
// Risk capital is money that can be lost without jeopardizing ones’ financial security or life style.
// Only risk capital should be used for trading and only those with sufficient risk capital should consider trading.

#property copyright "Aussie Surfer Ltd."
#property version   "1.0"
#property strict

static input string _Properties_ = "------"; // --- Expert Properties ---
static input double Entry_Amount =     0.30; // Entry lots
input int    Stop_Loss    =       46; // Stop Loss   (pips)
input int    Take_Profit  =        0; // Take Profit (pips)

static input string ___0______   = "------"; // --- Bollinger Bands ---
input int    Ind0Param0   =        5; // Period
input double Ind0Param1   =     2.50; // Deviation

static input string ___1______   = "------"; // --- Alligator ---
input int    Ind1Param0   =       27; // Jaws period
input int    Ind1Param1   =       17; // Jaws shift
input int    Ind1Param2   =       21; // Teeth period
input int    Ind1Param3   =        0; // Teeth shift
input int    Ind1Param4   =        6; // Lips period
input int    Ind1Param5   =        4; // Lips shift

static input string _Settings___ = "------"; // --- Expert Settings ---
static input int    Magic_Number = 93389885; // Magic Number

#define TRADE_RETRY_COUNT   4
#define TRADE_RETRY_WAIT  100
#define OP_FLAT            -1
#define OP_BUY            ORDER_TYPE_BUY
#define OP_SELL           ORDER_TYPE_SELL

// Session time is set in seconds from 00:00
int  sessionSundayOpen          =     0; // 00:00
int  sessionSundayClose         = 86400; // 24:00
int  sessionMondayThursdayOpen  =     0; // 00:00
int  sessionMondayThursdayClose = 86400; // 24:00
int  sessionFridayOpen          =     0; // 00:00
int  sessionFridayClose         = 86400; // 24:00
bool sessionIgnoreSunday        = false;
bool sessionCloseAtSessionClose = false;
bool sessionCloseAtFridayClose  = false;

const double sigma = 0.000001;

double posType       = OP_FLAT;
ulong  posTicket     = 0;
double posLots       = 0;
double posStopLoss   = 0;
double posTakeProfit = 0;

datetime barTime;
double   pip;
double   stopLevel;
bool     isTrailingStop=true;
ENUM_ORDER_TYPE_FILLING orderFillingType = ORDER_FILLING_FOK;
int indHandlers[1][12][2];

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   barTime        = Time(0);
   stopLevel      = (int) SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL);
   pip            = GetPipValue();
   isTrailingStop = isTrailingStop && Stop_Loss > 0;

   InitIndicators();

   return ValidateInit();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   datetime time = Time(0);
   if(time > barTime)
     {
      barTime = time;
      OnBar();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnBar()
  {
   UpdatePosition();

   if(posType != OP_FLAT && IsForceSessionClose())
     {
      ClosePosition();
      return;
     }

   if(IsOutOfSession())
      return;

   if(posType != OP_FLAT)
     {
      ManageClose();
      UpdatePosition();
     }

   if(posType != OP_FLAT && isTrailingStop)
     {
      double trailingStop=GetTrailingStopPrice();
      ManageTrailingStop(trailingStop);
      UpdatePosition();
     }

   int entrySignal = GetEntrySignal();

   if(posType == OP_FLAT && entrySignal != OP_FLAT)
     {
      OpenPosition(entrySignal);
      UpdatePosition();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UpdatePosition()
  {
   posType   = OP_FLAT;
   posTicket = 0;
   posLots   = 0;
   int posTotal = PositionsTotal();
   for(int posIndex = 0; posIndex < posTotal; posIndex++)
     {
      ulong ticket = PositionGetTicket(posIndex);
      if(PositionSelectByTicket(ticket) &&
         PositionGetString(POSITION_SYMBOL) == _Symbol &&
         PositionGetInteger(POSITION_MAGIC) == Magic_Number)
        {
         posType       = (int) PositionGetInteger(POSITION_TYPE);
         posTicket     = ticket;
         posLots       = NormalizeDouble(PositionGetDouble(POSITION_VOLUME),  2);
         posStopLoss   = NormalizeDouble(PositionGetDouble(POSITION_SL), _Digits);
         posTakeProfit = NormalizeDouble(PositionGetDouble(POSITION_TP), _Digits);
         break;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void InitIndicators()
  {
// Bollinger Bands (Close, 5, 2.50)
   indHandlers[0][0][0] = iBands(NULL, 0, Ind0Param0, 0, Ind0Param1, PRICE_CLOSE);
// Alligator (Smoothed, Median, 27, 17, 21, 0, 6, 4)
   indHandlers[0][1][0] = iAlligator(NULL, 0, Ind1Param0, Ind1Param1, Ind1Param2, Ind1Param3, Ind1Param4, Ind1Param5, MODE_SMMA, PRICE_MEDIAN);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GetEntrySignal()
  {
// Bollinger Bands (Close, 5, 2.50)
   double ind0buffer0[];
   CopyBuffer(indHandlers[0][0][0], 1, 1, 2, ind0buffer0);
   double ind0buffer1[];
   CopyBuffer(indHandlers[0][0][0], 2, 1, 2, ind0buffer1);
   double ind0upBand1 = ind0buffer0[1];
   double ind0dnBand1 = ind0buffer1[1];
   double ind0upBand2 = ind0buffer0[0];
   double ind0dnBand2 = ind0buffer1[0];
   bool   ind0long    = Open(0) < ind0dnBand1 - sigma && Open(1) > ind0dnBand2 + sigma;
   bool   ind0short   = Open(0) > ind0upBand1 + sigma && Open(1) < ind0upBand2 - sigma;

   bool canOpenLong  = ind0long;
   bool canOpenShort = ind0short;

   return canOpenLong  && !canOpenShort ? OP_BUY
          : canOpenShort && !canOpenLong  ? OP_SELL
          : OP_FLAT;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManageClose()
  {
// Alligator (Smoothed, Median, 27, 17, 21, 0, 6, 4)
   double ind1buffer0[];
   CopyBuffer(indHandlers[0][1][0], 0, 1, 2, ind1buffer0);
   double ind1buffer1[];
   CopyBuffer(indHandlers[0][1][0], 1, 1, 2, ind1buffer1);
   double ind1buffer2[];
   CopyBuffer(indHandlers[0][1][0], 2, 1, 2, ind1buffer2);
   double ind1val1  = ind1buffer1[1];
   double ind1val2  = ind1buffer1[0];
   bool   ind1long  = ind1val1 > ind1val2 + sigma;
   bool   ind1short = ind1val1 < ind1val2 - sigma;

   if((posType == OP_BUY  && ind1long) ||
      (posType == OP_SELL && ind1short))
      ClosePosition();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpenPosition(int command)
  {
   double stopLoss   = GetStopLossPrice(command);
   double takeProfit = GetTakeProfitPrice(command);
   ManageOrderSend(command, Entry_Amount, stopLoss, takeProfit, 0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ClosePosition()
  {
   int command = posType == OP_BUY ? OP_SELL : OP_BUY;
   ManageOrderSend(command, posLots, 0, 0, posTicket);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManageOrderSend(int command, double lots, double stopLoss, double takeProfit, ulong ticket)
  {
   for(int attempt = 0; attempt < TRADE_RETRY_COUNT; attempt++)
     {
      if(IsTradeContextFree())
        {
         MqlTradeRequest request;
         MqlTradeResult  result;
         ZeroMemory(request);
         ZeroMemory(result);

         request.action       = TRADE_ACTION_DEAL;
         request.symbol       = _Symbol;
         request.volume       = lots;
         request.type         = command == OP_BUY ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
         request.price        = command == OP_BUY ? Ask() : Bid();
         request.type_filling = orderFillingType;
         request.deviation    = 10;
         request.sl           = stopLoss;
         request.tp           = takeProfit;
         request.magic        = Magic_Number;
         request.position     = ticket;
         request.comment      = IntegerToString(Magic_Number);

         bool isOrderCheck = CheckOrder(request);
         bool isOrderSend  = false;

         if(isOrderCheck)
           {
            ResetLastError();
            isOrderSend = OrderSend(request, result);
           }

         if(isOrderCheck && isOrderSend && result.retcode == TRADE_RETCODE_DONE)
            return;
        }

      Sleep(TRADE_RETRY_WAIT);
      Print("Order Send retry no: " + IntegerToString(attempt + 2));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ModifyPosition(double stopLoss, double takeProfit, ulong ticket)
  {
   for(int attempt = 0; attempt < TRADE_RETRY_COUNT; attempt++)
     {
      if(IsTradeContextFree())
        {
         MqlTradeRequest request;
         MqlTradeResult  result;
         ZeroMemory(request);
         ZeroMemory(result);

         request.action   = TRADE_ACTION_SLTP;
         request.symbol   = _Symbol;
         request.sl       = stopLoss;
         request.tp       = takeProfit;
         request.magic    = Magic_Number;
         request.position = ticket;
         request.comment  = IntegerToString(Magic_Number);

         bool isOrderCheck = CheckOrder(request);
         bool isOrderSend  = false;

         if(isOrderCheck)
           {
            ResetLastError();
            isOrderSend = OrderSend(request, result);
           }

         if(isOrderCheck && isOrderSend && result.retcode == TRADE_RETCODE_DONE)
            return;
        }

      Sleep(TRADE_RETRY_WAIT);
      Print("Order Send retry no: " + IntegerToString(attempt + 2));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckOrder(MqlTradeRequest &request)
  {
   MqlTradeCheckResult check;
   ZeroMemory(check);
   ResetLastError();

   if(OrderCheck(request, check))
      return true;

   Print("Error with OrderCheck: " + check.comment);

   if(check.retcode == TRADE_RETCODE_INVALID_FILL)
     {
      switch(orderFillingType)
        {
         case ORDER_FILLING_FOK:
            Print("Filling mode changed to: ORDER_FILLING_IOC");
            orderFillingType = ORDER_FILLING_IOC;
            break;
         case ORDER_FILLING_IOC:
            Print("Filling mode changed to: ORDER_FILLING_RETURN");
            orderFillingType = ORDER_FILLING_RETURN;
            break;
         case ORDER_FILLING_RETURN:
            Print("Filling mode changed to: ORDER_FILLING_FOK");
            orderFillingType = ORDER_FILLING_FOK;
            break;
        }

      request.type_filling = orderFillingType;

      return CheckOrder(request);
     }

   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetStopLossPrice(int command)
  {
   if(Stop_Loss == 0)
      return 0;

   double delta    = MathMax(pip * Stop_Loss, _Point * stopLevel);
   double stopLoss = command == OP_BUY ? Bid() - delta : Ask() + delta;

   return NormalizeDouble(stopLoss, _Digits);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitPrice(int command)
  {
   if(Take_Profit == 0)
      return 0;

   double delta      = MathMax(pip * Take_Profit, _Point * stopLevel);
   double takeProfit = command == OP_BUY ? Bid() + delta : Ask() - delta;

   return NormalizeDouble(takeProfit, _Digits);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTrailingStopPrice()
  {
   double bid = Bid();
   double ask = Ask();
   double spread = ask - bid;
   double stopLevelPoints = _Point * stopLevel;
   double stopLossPoints  = pip * Stop_Loss;

   if(posType == OP_BUY)
     {
      double newStopLoss = High(1) - stopLossPoints;
      if(posStopLoss <= newStopLoss - pip)
         return newStopLoss < bid
                ? newStopLoss >= bid - stopLevelPoints
                ? bid - stopLevelPoints
                : newStopLoss
                : bid;
     }

   if(posType == OP_SELL)
     {
      double newStopLoss = Low(1) + spread + stopLossPoints;
      if(posStopLoss >= newStopLoss + pip)
         return newStopLoss > ask
                ? newStopLoss <= ask + stopLevelPoints
                ? ask + stopLevelPoints
                : newStopLoss
                : ask;
     }

   return posStopLoss;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManageTrailingStop(double trailingStop)
  {
   if((posType == OP_BUY  && MathAbs(trailingStop - Bid()) < _Point) ||
      (posType == OP_SELL && MathAbs(trailingStop - Ask()) < _Point))
     {
      ClosePosition();
      return;
     }

   if(MathAbs(trailingStop - posStopLoss) > _Point)
     {
      posStopLoss = NormalizeDouble(trailingStop, _Digits);
      ModifyPosition(posStopLoss, posTakeProfit, posTicket);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Bid()
  {
   return SymbolInfoDouble(_Symbol, SYMBOL_BID);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Ask()
  {
   return SymbolInfoDouble(_Symbol, SYMBOL_ASK);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime Time(int bar)
  {
   datetime buffer[];
   ArrayResize(buffer, 1);
   return CopyTime(_Symbol, _Period, bar, 1, buffer) == 1 ? buffer[0] : 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Open(int bar)
  {
   double buffer[];
   ArrayResize(buffer, 1);
   return CopyOpen(_Symbol, _Period, bar, 1, buffer) == 1 ? buffer[0] : 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double High(int bar)
  {
   double buffer[];
   ArrayResize(buffer, 1);
   return CopyHigh(_Symbol, _Period, bar, 1, buffer) == 1 ? buffer[0] : 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Low(int bar)
  {
   double buffer[];
   ArrayResize(buffer, 1);
   return CopyLow(_Symbol, _Period, bar, 1, buffer) == 1 ? buffer[0] : 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Close(int bar)
  {
   double buffer[];
   ArrayResize(buffer, 1);
   return CopyClose(_Symbol, _Period, bar, 1, buffer) == 1 ? buffer[0] : 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetPipValue()
  {
   return _Digits == 4 || _Digits == 5 ? 0.0001
          : _Digits == 2 || _Digits == 3 ? 0.01
          : _Digits == 1 ? 0.1 : 1;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsTradeAllowed()
  {
   return (bool) MQL5InfoInteger(MQL5_TRADE_ALLOWED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void RefreshRates()
  {
// Dummy function to make it compatible with MQL4
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int DayOfWeek()
  {
   MqlDateTime mqlTime;
   TimeToStruct(Time(0), mqlTime);
   return mqlTime.day_of_week;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsTradeContextFree()
  {
   if(IsTradeAllowed())
      return true;

   uint startWait = GetTickCount();
   Print("Trade context is busy! Waiting...");

   while(true)
     {
      if(IsStopped())
         return false;

      uint diff = GetTickCount() - startWait;
      if(diff > 30 * 1000)
        {
         Print("The waiting limit exceeded!");
         return false;
        }

      if(IsTradeAllowed())
        {
         RefreshRates();
         return true;
        }

      Sleep(TRADE_RETRY_WAIT);
     }

   return true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsOutOfSession()
  {
   int dayOfWeek    = DayOfWeek();
   int periodStart  = int(Time(0) % 86400);
   int periodLength = PeriodSeconds(_Period);
   int periodFix    = periodStart + (sessionCloseAtSessionClose ? periodLength : 0);
   int friBarFix    = periodStart + (sessionCloseAtFridayClose || sessionCloseAtSessionClose ? periodLength : 0);

   return dayOfWeek == 0 && sessionIgnoreSunday ? true
          : dayOfWeek == 0 ? periodStart < sessionSundayOpen         || periodFix > sessionSundayClose
          : dayOfWeek  < 5 ? periodStart < sessionMondayThursdayOpen || periodFix > sessionMondayThursdayClose
          : periodStart < sessionFridayOpen         || friBarFix > sessionFridayClose;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsForceSessionClose()
  {
   if(!sessionCloseAtFridayClose && !sessionCloseAtSessionClose)
      return false;

   int dayOfWeek = DayOfWeek();
   int periodEnd = int(Time(0) % 86400) + PeriodSeconds(_Period);

   return dayOfWeek == 0 && sessionCloseAtSessionClose ? periodEnd > sessionSundayClose
          : dayOfWeek  < 5 && sessionCloseAtSessionClose ? periodEnd > sessionMondayThursdayClose
          : dayOfWeek == 5 ? periodEnd > sessionFridayClose : false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
ENUM_INIT_RETCODE ValidateInit()
  {
   return INIT_SUCCEEDED;
  }
//+------------------------------------------------------------------+
/*STRATEGY MARKET FXOpen-ECN Live Server; GBPAUD; M15 */
/*STRATEGY CODE {"properties":{"entryLots":0.3,"tradeDirectionMode":0,"oppositeEntrySignal":0,"stopLoss":46,"takeProfit":100,"useStopLoss":true,"useTakeProfit":false,"isTrailingStop":true},"openFilters":[{"name":"Bollinger Bands","listIndexes":[4,3,0,0,0],"numValues":[5,2.5,0,0,0,0]}],"closeFilters":[{"name":"Alligator","listIndexes":[2,3,4,0,0],"numValues":[27,17,21,0,6,4]}]} */
