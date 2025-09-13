//+--------------------------------------------------------+
//|                  e-Smart_Tralling                      |
//+--------------------------------------------------------+

extern   bool     UseOneAccount = true;
extern   bool     UseCloseOneThird = true;
extern   int      LevelProfit1 = 20;
extern   int      LevelMoving1 = 1;
extern   int      LevelProfit2 = 35;
extern   int      LevelMoving2 = 10;
extern   int      LevelProfit3 = 55;
extern   int      LevelMoving3 = 30;
extern   int      TrailingStop = 30;
extern   int      TrailingStep = 5;
extern   int      Slippage = 2;
extern   bool     ShowComment = true;
extern   bool     UseSound = true;
string   var_132 = "expert.wav";

//+------------------------------------------------------------------+

void deinit()
{
Comment("");
}

//+------------------------------------------------------------------+

void start()
{
double point;
int    digits;
string msg = "";

for (int i = 0; i < OrdersTotal(); i++)
   {
   if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      {
      if (UseOneAccount || (OrderSymbol() == Symbol()))
         {
         ThreeLevelSystemOfOutput();
         digits = MarketInfo(OrderSymbol(),MODE_DIGITS);
         point = MarketInfo(OrderSymbol(),MODE_POINT);
         msg = msg + OrderSymbol() + "  Цена: " + DoubleToStr(OrderOpenPrice(),digits) + "  SL = " + DoubleToStr(OrderStopLoss(),digits) + " (" + StopLossInPoint() + ")\n";
         }
      }
   }

if (ShowComment) Comment(msg);
}

//+------------------------------------------------------------------+

void ThreeLevelSystemOfOutput()
{
int profit = ProfitPosition();
int sl = StopLossInPoint();
int spread = MarketInfo(OrderSymbol(),MODE_SPREAD);

if ((profit > LevelProfit1) && (profit <= LevelProfit2) && (sl < LevelMoving1))
   {
   ModifyStopLossInPoint(LevelMoving1);
   if (UseCloseOneThird) CloseOneThird();
   }
if ((profit > LevelProfit2) && (profit <= LevelProfit3) && (sl < LevelMoving2)) ModifyStopLossInPoint(LevelMoving2);
if ((profit > LevelProfit3) && (sl < LevelMoving3)) ModifyStopLossInPoint(LevelMoving3);
if (profit > LevelMoving3 + TrailingStop + TrailingStep) TrailingPositions();
}

//+------------------------------------------------------------------+

void CloseOneThird()
{
bool   result = false;
double lots = MathCeil(OrderLots() / 3.0 * 10.0) / 10.0;

if (lots > 0.0)
   {
   if (OrderType() == OP_BUY)
      {
      result = OrderClose(OrderTicket(),lots,Bid,Slippage,CLR_NONE);
      }
   if (OrderType() == OP_SELL)
      {
      result = OrderClose(OrderTicket(),lots,Ask,Slippage,CLR_NONE);
      }
   if (result && UseSound) PlaySound(var_132);
   }
}

//+------------------------------------------------------------------+

void TrailingPositions()
{
double bid;
double ask;
double point = MarketInfo(OrderSymbol(),MODE_POINT);

if (OrderType() == OP_BUY)
   {
   bid = MarketInfo(OrderSymbol(),MODE_BID);
   if (bid - OrderOpenPrice() > TrailingStop * point)
      {
      if (OrderStopLoss() < bid - (TrailingStop + TrailingStep - 1) * point)
         {
         ModifyStopLoss(bid - TrailingStop * point);
         return;
         }
      }
   }

if (OrderType() == OP_SELL)
   {
   ask = MarketInfo(OrderSymbol(),MODE_ASK);
   if (OrderOpenPrice() - ask > TrailingStop * point)
      {
      if ((OrderStopLoss() > ask + (TrailingStop + TrailingStep - 1) * point) || (OrderStopLoss() == 0))
         {
         ModifyStopLoss(ask + TrailingStop * point);
         }
      }
   }
}

//+------------------------------------------------------------------+

void ModifyStopLoss(double sl)
{
bool result = OrderModify(OrderTicket(),OrderOpenPrice(),sl,OrderTakeProfit(),0,CLR_NONE);
if (result && UseSound) PlaySound(var_132);
}

//+------------------------------------------------------------------+

void ModifyStopLossInPoint(int stoploss)
{
bool   result;
double sl = 0;
double point = MarketInfo(OrderSymbol(),MODE_POINT);

if (OrderType() == OP_BUY) sl = OrderOpenPrice() + stoploss * point;
if (OrderType() == OP_SELL) sl = OrderOpenPrice() - stoploss * point;

result = OrderModify(OrderTicket(),OrderOpenPrice(),sl,OrderTakeProfit(),0,CLR_NONE);
if (result && UseSound) PlaySound(var_132);
}

//+------------------------------------------------------------------+

int ProfitPosition()
{
double bid;
double ask;
double point = MarketInfo(OrderSymbol(),MODE_POINT);
double profit = 0;

if (OrderType() == OP_BUY)
   {
   bid = MarketInfo(OrderSymbol(),MODE_BID);
   profit = (bid - OrderOpenPrice()) / point;
   }
if (OrderType() == OP_SELL)
   {
   ask = MarketInfo(OrderSymbol(),MODE_ASK);
   profit = (OrderOpenPrice() - ask) / point;
   }
return(MathRound(profit));
}

//+------------------------------------------------------------------+

int StopLossInPoint()
{
double point = MarketInfo(OrderSymbol(),MODE_POINT);
double sl = 0;

if (OrderType() == OP_BUY) sl = (OrderStopLoss() - OrderOpenPrice()) / point;
if (OrderType() == OP_SELL) sl = (OrderOpenPrice() - OrderStopLoss()) / point;
if (OrderStopLoss() == 0.0) sl = -OrderOpenPrice() / point;
return(MathRound(sl));
}