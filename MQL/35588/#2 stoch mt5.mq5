#property copyright   "https://payhip.com/forexeas"
#property link        "https://payhip.com/forexeas"
#property description "Please Visit site \nmultiple different versions..."
#property version     "1.0"
#property strict

static input string StrategyProperties__ = "------------"; // ------ Expert Properties ------
static input double Entry_Amount = 0.01; // Entry lots
input int Stop_Loss   = 40; // Stop Loss (pips)
input int Take_Profit = 70; // Take Profit (pips)
static input string Ind0 = "------------";// ----- Stochastic Signal -----
input int Ind0Param0 = 40; // %K Period
input int Ind0Param1 = 10; // %D Period
input int Ind0Param2 = 10; // Slowing
static input string Ind1 = "------------";// ----- Stochastic -----
input int Ind1Param0 = 40; // %K Period
input int Ind1Param1 = 10; // %D Period
input int Ind1Param2 = 10; // Slowing
input int Ind1Param3 = 20; // Level
static input string Ind2 = "------------";// ----- Stochastic -----
input int Ind2Param0 = 40; // %K Period
input int Ind2Param1 = 10; // %D Period
input int Ind2Param2 = 10; // Slowing
input int Ind2Param3 = 75; // Level
static input string Ind3 = "------------";// ----- Accelerator Oscillator -----
input double Ind3Param0 = 0.0002; // Level
static input string Ind4 = "------------";// ----- Awesome Oscillator -----
input double Ind4Param0 = 0.0013; // Level

static input string ExpertSettings__ = "------------"; // ------ Expert Settings ------
static input int Magic_Number = 55248008; // Magic Number

#define TRADE_RETRY_COUNT 4
#define TRADE_RETRY_WAIT  100
#define OP_FLAT           -1
#define OP_BUY            ORDER_TYPE_BUY
#define OP_SELL           ORDER_TYPE_SELL

// Session time is set in seconds from 00:00
int sessionSundayOpen           = 0;     // 00:00
int sessionSundayClose          = 86400; // 24:00
int sessionMondayThursdayOpen   = 0;     // 00:00
int sessionMondayThursdayClose  = 86400; // 24:00
int sessionFridayOpen           = 0;     // 00:00
int sessionFridayClose          = 86400; // 24:00
bool sessionIgnoreSunday        = true;
bool sessionCloseAtSessionClose = true;
bool sessionCloseAtFridayClose  = true;

const double sigma=0.000001;

double posType       = OP_FLAT;
ulong  posTicket     = 0;
double posLots       = 0;
double posStopLoss   = 0;
double posTakeProfit = 0;

datetime barTime;
int      digits;
double   pip;
double   stopLevel;
bool     isTrailingStop=false;

ENUM_ORDER_TYPE_FILLING orderFillingType;

int ind0handler;
int ind1handler;
int ind2handler;
int ind3handler;
int ind4handler;

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   barTime          = Time(0);
   digits           = (int) SymbolInfoInteger(_Symbol, SYMBOL_DIGITS);
   pip              = GetPipValue(digits);
   stopLevel        = (int) SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL);
   orderFillingType = GetOrderFillingType();
   isTrailingStop   = isTrailingStop && Stop_Loss > 0;

   ind0handler = iStochastic(NULL,0,Ind0Param0,Ind0Param1,Ind0Param2,MODE_SMA,STO_LOWHIGH);
   ind1handler = iStochastic(NULL,0,Ind1Param0,Ind1Param1,Ind1Param2,MODE_SMA,0);
   ind2handler = iStochastic(NULL,0,Ind2Param0,Ind2Param1,Ind2Param2,MODE_SMA,0);
   ind3handler = iAC(NULL,0);
   ind4handler = iAO(NULL,0);

   const ENUM_INIT_RETCODE initRetcode = ValidateInit();

   return (initRetcode);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   datetime time=Time(0);
   if(time>barTime)
     {
      barTime=time;
      OnBar();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnBar()
  {
   UpdatePosition();

   if(posType!=OP_FLAT && IsForceSessionClose())
     {
      ClosePosition();
      return;
     }

   if(IsOutOfSession())
      return;

   if(posType!=OP_FLAT)
     {
      ManageClose();
      UpdatePosition();
     }

   if(posType!=OP_FLAT && isTrailingStop)
     {
      double trailingStop=GetTrailingStop();
      ManageTrailingStop(trailingStop);
      UpdatePosition();
     }

   if(posType==OP_FLAT)
     {
      ManageOpen();
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
   int posTotal=PositionsTotal();
   for(int posIndex=0;posIndex<posTotal;posIndex++)
     {
      const ulong ticket=PositionGetTicket(posIndex);
      if(PositionSelectByTicket(ticket) &&
         PositionGetString(POSITION_SYMBOL)==_Symbol &&
         PositionGetInteger(POSITION_MAGIC)==Magic_Number)
        {
         posType       = (int) PositionGetInteger(POSITION_TYPE);
         posLots       = NormalizeDouble(PositionGetDouble(POSITION_VOLUME), 2);
         posTicket     = ticket;
         posStopLoss   = NormalizeDouble(PositionGetDouble(POSITION_SL), digits);
         posTakeProfit = NormalizeDouble(PositionGetDouble(POSITION_TP), digits);
         break;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManageOpen()
  {
   double ind0buffer0[]; CopyBuffer(ind0handler,MAIN_LINE,1,2,ind0buffer0);
   double ind0buffer1[]; CopyBuffer(ind0handler,SIGNAL_LINE,1,2,ind0buffer1);
   double ind0val1 = ind0buffer0[1];
   double ind0val2 = ind0buffer1[1];
   bool ind0long  = ind0val1 > ind0val2 + sigma;
   bool ind0short = ind0val1 < ind0val2 - sigma;

   double ind1buffer[]; CopyBuffer(ind1handler,MAIN_LINE,1,3,ind1buffer);
   double ind1val1 = ind1buffer[2];
   bool ind1long  = ind1val1 > Ind1Param3 + sigma;
   bool ind1short = ind1val1 < 100 - Ind1Param3 - sigma;

   double ind2buffer[]; CopyBuffer(ind2handler,MAIN_LINE,1,3,ind2buffer);
   double ind2val1 = ind2buffer[2];
   bool ind2long  = ind2val1 < Ind2Param3 - sigma;
   bool ind2short = ind2val1 > 100 - Ind2Param3 + sigma;

   double ind3buffer[]; CopyBuffer(ind3handler,0,1,3,ind3buffer);
   double ind3val1 = ind3buffer[2];
   double ind3val2 = ind3buffer[1];
   bool ind3long  = ind3val1 > Ind3Param0 + sigma && ind3val2 < Ind3Param0 - sigma;
   bool ind3short = ind3val1 < -Ind3Param0 - sigma && ind3val2 > -Ind3Param0 + sigma;

   const bool canOpenLong  = ind0long && ind1long && ind2long && ind3long;
   const bool canOpenShort = ind0short && ind1short && ind2short && ind3short;

   if(canOpenLong && canOpenShort) return;

   if(canOpenLong)
      OpenPosition(OP_BUY);
   else if(canOpenShort)
      OpenPosition(OP_SELL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManageClose()
  {
   double ind4buffer[]; CopyBuffer(ind4handler,0,1,3,ind4buffer);
   double ind4val1 = ind4buffer[2];
   double ind4val2 = ind4buffer[1];
   bool ind4long  = ind4val1 < Ind4Param0 - sigma && ind4val2 > Ind4Param0 + sigma;
   bool ind4short = ind4val1 > -Ind4Param0 + sigma && ind4val2 < -Ind4Param0 - sigma;

   if(posType==OP_BUY && ind4long)
      ClosePosition();
   else if(posType==OP_SELL && ind4short)
      ClosePosition();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpenPosition(int command)
  {
   const double stopLoss   = GetStopLossPrice(command);
   const double takeProfit = GetTakeProfitPrice(command);
   ManageOrderSend(command,Entry_Amount,stopLoss,takeProfit,0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ClosePosition()
  {
   const int command=posType==OP_BUY ? OP_SELL : OP_BUY;
   ManageOrderSend(command,posLots,0,0,posTicket);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManageOrderSend(int command,double lots,double stopLoss,double takeProfit,ulong ticket)
  {
   for(int attempt=0; attempt<TRADE_RETRY_COUNT; attempt++)
     {
      if(IsTradeContextFree())
        {
         ResetLastError();
         MqlTick         tick;    SymbolInfoTick(_Symbol,tick);
         MqlTradeRequest request; ZeroMemory(request);
         MqlTradeResult  result;  ZeroMemory(result);

         request.action       = TRADE_ACTION_DEAL;
         request.symbol       = _Symbol;
         request.volume       = lots;
         request.type         = command==OP_BUY ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
         request.price        = command==OP_BUY ? tick.ask : tick.bid;
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
            isOrderSend=OrderSend(request,result);
           }

         if(isOrderCheck && isOrderSend && result.retcode==TRADE_RETCODE_DONE)
            return;
        }
      Sleep(TRADE_RETRY_WAIT);
      Print("Order Send retry no: "+IntegerToString(attempt+2));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ModifyPosition(double stopLoss,double takeProfit,ulong ticket)
  {
   for(int attempt=0; attempt<TRADE_RETRY_COUNT; attempt++)
     {
      if(IsTradeContextFree())
        {
         ResetLastError();
         MqlTick         tick;    SymbolInfoTick(_Symbol,tick);
         MqlTradeRequest request; ZeroMemory(request);
         MqlTradeResult  result;  ZeroMemory(result);

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
            isOrderSend=OrderSend(request,result);
           }

         if(isOrderCheck && isOrderSend && result.retcode==TRADE_RETCODE_DONE)
            return;
        }
      Sleep(TRADE_RETRY_WAIT);
      Print("Order Send retry no: "+IntegerToString(attempt+2));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckOrder(MqlTradeRequest &request)
  {
   MqlTradeCheckResult check; ZeroMemory(check);
   const bool isOrderCheck=OrderCheck(request,check);
   if(isOrderCheck) return (true);


   if(check.retcode==TRADE_RETCODE_INVALID_FILL)
     {
      switch(orderFillingType)
        {
         case  ORDER_FILLING_FOK:
            orderFillingType=ORDER_FILLING_IOC;
            break;
         case  ORDER_FILLING_IOC:
            orderFillingType=ORDER_FILLING_RETURN;
            break;
         case  ORDER_FILLING_RETURN:
            orderFillingType=ORDER_FILLING_FOK;
            break;
        }

      request.type_filling=orderFillingType;

      const bool isNewCheck=CheckOrder(request);

      return (isNewCheck);
     }

   Print("Error with OrderCheck: "+check.comment);
   return (false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetStopLossPrice(int command)
  {
   if(Stop_Loss==0) return (0);

   MqlTick tick; SymbolInfoTick(_Symbol,tick);
   const double delta    = MathMax(pip*Stop_Loss, _Point*stopLevel);
   const double price    = command==OP_BUY ? tick.bid : tick.ask;
   const double stopLoss = command==OP_BUY ? price-delta : price+delta;
   const double normalizedStopLoss = NormalizeDouble(stopLoss, _Digits);

   return (normalizedStopLoss);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTrailingStop()
  {
   MqlTick tick; SymbolInfoTick(_Symbol,tick);
   const double stopLevelPoints = _Point*stopLevel;
   const double stopLossPoints  = pip*Stop_Loss;

   if(posType==OP_BUY)
     {
      const double stopLossPrice=High(1)-stopLossPoints;
      if(posStopLoss<stopLossPrice-pip)
        {
         if(stopLossPrice<tick.bid)
           {
            const double fixedStopLossPrice = (stopLossPrice>=tick.bid-stopLevelPoints)
                                              ? tick.bid - stopLevelPoints
                                              : stopLossPrice;

            return (fixedStopLossPrice);
           }
         else
           {
            return (tick.bid);
           }
        }
     }

   else if(posType==OP_SELL)
     {
      const double stopLossPrice=Low(1)+stopLossPoints;
      if(posStopLoss>stopLossPrice+pip)
        {
         if(stopLossPrice>tick.ask)
           {
            if(stopLossPrice<=tick.ask+stopLevelPoints)
               return (tick.ask + stopLevelPoints);
            else
               return (stopLossPrice);
           }
         else
           {
            return (tick.ask);
           }
        }
     }

   return (posStopLoss);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManageTrailingStop(double trailingStop)
  {
   MqlTick tick; SymbolInfoTick(_Symbol,tick);

   if(posType==OP_BUY && MathAbs(trailingStop-tick.bid)<_Point)
     {
      ClosePosition();
     }

   else if(posType==OP_SELL && MathAbs(trailingStop-tick.ask)<_Point)
     {
      ClosePosition();
     }

   else if(MathAbs(trailingStop-posStopLoss)>_Point)
     {
      posStopLoss=NormalizeDouble(trailingStop,digits);
      ModifyPosition(posStopLoss,posTakeProfit,posTicket);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitPrice(int command)
  {
   if(Take_Profit==0) return (0);

   MqlTick tick; SymbolInfoTick(_Symbol,tick);
   const double delta      = MathMax(pip*Take_Profit, _Point*stopLevel);
   const double price      = command==OP_BUY ? tick.bid : tick.ask;
   const double takeProfit = command==OP_BUY ? price+delta : price-delta;
   const double normalizedTakeProfit = NormalizeDouble(takeProfit, _Digits);

   return (normalizedTakeProfit);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime Time(int bar)
  {
   datetime buffer[]; ArrayResize(buffer,1);
   const int result=CopyTime(_Symbol,_Period,bar,1,buffer);
   return (result==1 ? buffer[0] : 0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Open(int bar)
  {
   double buffer[]; ArrayResize(buffer,1);
   const int result=CopyOpen(_Symbol,_Period,bar,1,buffer);
   return (result==1 ? buffer[0] : 0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double High(int bar)
  {
   double buffer[]; ArrayResize(buffer,1);
   const int result=CopyHigh(_Symbol,_Period,bar,1,buffer);
   return (result==1 ? buffer[0] : 0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Low(int bar)
  {
   double buffer[]; ArrayResize(buffer,1);
   const int result=CopyLow(_Symbol,_Period,bar,1,buffer);
   return (result==1 ? buffer[0] : 0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Close(int bar)
  {
   double buffer[]; ArrayResize(buffer,1);
   const int result=CopyClose(_Symbol,_Period,bar,1,buffer);
   return (result==1 ? buffer[0] : 0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetPipValue(int digit)
  {
   if(digit==4 || digit==5)
      return (0.0001);
   if(digit==2 || digit==3)
      return (0.01);
   if(digit==1)
      return (0.1);
   return (1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsTradeContextFree()
  {
   if(MQL5InfoInteger(MQL5_TRADE_ALLOWED)) return (true);

   uint startWait=GetTickCount();
   Print("Trade context is busy! Waiting...");

   while(true)
     {
      if(IsStopped()) return (false);

      uint diff=GetTickCount()-startWait;
      if(diff>30*1000)
        {
         Print("The waiting limit exceeded!");
         return (false);
        }

      if(MQL5InfoInteger(MQL5_TRADE_ALLOWED)) return (true);

      Sleep(TRADE_RETRY_WAIT);
     }

   return (true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsOutOfSession()
  {
   MqlDateTime time0; TimeToStruct(Time(0),time0);
   const int weekDay           = time0.day_of_week;
   const long timeFromMidnight = Time(0)%86400;
   const int periodLength      = PeriodSeconds(_Period);

   if(weekDay==0)
     {
      if(sessionIgnoreSunday) return (true);

      const int lastBarFix = sessionCloseAtSessionClose ? periodLength : 0;
      const bool skipTrade = timeFromMidnight<sessionSundayOpen ||
                             timeFromMidnight+lastBarFix>sessionSundayClose;

      return (skipTrade);
     }

   if(weekDay<5)
     {
      const int lastBarFix = sessionCloseAtSessionClose ? periodLength : 0;
      const bool skipTrade = timeFromMidnight<sessionMondayThursdayOpen ||
                             timeFromMidnight+lastBarFix>sessionMondayThursdayClose;

      return (skipTrade);
     }

   const int lastBarFix=sessionCloseAtFridayClose || sessionCloseAtSessionClose ? periodLength : 0;
   const bool skipTrade=timeFromMidnight<sessionFridayOpen || timeFromMidnight+lastBarFix>sessionFridayClose;

   return (skipTrade);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsForceSessionClose()
  {
   if(!sessionCloseAtFridayClose && !sessionCloseAtSessionClose) return (false);

   MqlDateTime time0; TimeToStruct(Time(0),time0);
   const int weekDay           = time0.day_of_week;
   const long timeFromMidnight = Time(0)%86400;
   const int periodLength      = PeriodSeconds(_Period);

   bool forceExit=false;
   if(weekDay==0 && sessionCloseAtSessionClose)
     {
      forceExit=timeFromMidnight+periodLength>sessionSundayClose;
     }
   else if(weekDay<5 && sessionCloseAtSessionClose)
     {
      forceExit=timeFromMidnight+periodLength>sessionMondayThursdayClose;
     }
   else if(weekDay==5)
     {
      forceExit=timeFromMidnight+periodLength>sessionFridayClose;
     }

   return (forceExit);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
ENUM_ORDER_TYPE_FILLING GetOrderFillingType()
  {
   const int oftIndex=(int) SymbolInfoInteger(_Symbol,SYMBOL_FILLING_MODE);
   const ENUM_ORDER_TYPE_FILLING fillType=(ENUM_ORDER_TYPE_FILLING)(oftIndex>0 ? oftIndex-1 : oftIndex);

   return (fillType);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
ENUM_INIT_RETCODE ValidateInit()
  {
   return (INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
/*STRATEGY MARKET Premium Data; EURUSD; H4 */
/*STRATEGY CODE {"properties":{"entryLots":0.01,"tradeDirectionMode":0,"stopLoss":40,"takeProfit":70,"useStopLoss":true,"useTakeProfit":true,"isTrailingStop":false},"openFilters":[{"name":"Stochastic Signal","listIndexes":[2,0,0,0,0],"numValues":[40,10,10,0,0,0]},{"name":"Stochastic","listIndexes":[2,0,0,0,0],"numValues":[40,10,10,20,0,0]},{"name":"Stochastic","listIndexes":[3,0,0,0,0],"numValues":[40,10,10,75,0,0]},{"name":"Accelerator Oscillator","listIndexes":[4,0,0,0,0],"numValues":[0.0002,0,0,0,0,0]}],"closeFilters":[{"name":"Awesome Oscillator","listIndexes":[5,0,0,0,0],"numValues":[0.0013,0,0,0,0,0]}]} */
