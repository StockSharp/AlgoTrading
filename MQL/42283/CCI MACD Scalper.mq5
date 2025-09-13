//+------------------------------------------------------------------+
//|                                               CCI + MACD Scalper |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#include <Trade/Trade.mqh>
CTrade trade;

ulong tradeTicket = 0;

int ma34Handler;
int cciHandler;
int macdHandle;

double ma34Array[];
double cciArray[];
double macdArray[];
double macdSignalArray[];

datetime time0, prevBarTime;
bool timePassed = true;

input ulong magicNumber = 0; //Magic number
input double accountRisk = 2; //Account risk % per trade
input double riskReward = 1.5; //Risk to Reward ratio
input int emaPeriod = 34; //EMA period
input int cciPeriod = 50; //CCI MA period
input int minHour = 0; //Hours from which the EA will trade
input int maxHour = 24; //Hours to which the EA will trade
input double minimalStopLoss = 100; //Minimal stop loss in points required to open a trade
input bool useTrailingStop = false; //Use trailing stop
input double tslPoints = 100; //Trailing stop points

double orderSize = 0;
double profitsPassed = 0;
double unit = tslPoints * _Point;

//+------------------------------------------------------------------+
//| Initialization of indicators                                     |
//+------------------------------------------------------------------+
int OnInit()
  {
   trade.SetExpertMagicNumber(magicNumber);

   ma34Handler = iMA(_Symbol, _Period, emaPeriod, 0, MODE_EMA, PRICE_CLOSE);
   cciHandler = iCCI(_Symbol, _Period, cciPeriod, PRICE_CLOSE);
   macdHandle = iMACD(_Symbol, _Period, 12, 26, 9, PRICE_CLOSE);

   time0 = iTime(_Symbol, PERIOD_CURRENT, 0);
   prevBarTime = iTime(_Symbol, PERIOD_CURRENT, 0);

   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| OnTick method                                                    |
//+------------------------------------------------------------------+
void OnTick()
  {
//Check if the user want to use trailing stop
   if(useTrailingStop)
     {
      CheckTrailingStops();
     }

//Check for new bar
   time0 = iTime(_Symbol, PERIOD_CURRENT, 0);
   bool isNewBar = time0 != prevBarTime;

   MqlDateTime currentTime;
   MqlDateTime minTime;
   MqlDateTime maxTime;

   TimeToStruct(time0, currentTime);
   minTime.hour = minHour;
   maxTime.hour = maxHour;

   if(isNewBar && timePassed && currentTime.hour >= minTime.hour && currentTime.hour <= maxTime.hour)
     {

      //Check if there is an open order
      if(PositionSelectByTicket(tradeTicket) == false)
        {
         tradeTicket = 0;
        }

      CopyBuffer(ma34Handler, 0, 0, 2, ma34Array);
      CopyBuffer(cciHandler, 0, 0, 3, cciArray);
      CopyBuffer(macdHandle, 0, 0, 3, macdArray);
      CopyBuffer(macdHandle, 1, 0, 3, macdSignalArray);

      double ask = NormalizeDouble(SymbolInfoDouble(_Symbol, SYMBOL_ASK), _Digits);
      double bid = NormalizeDouble(SymbolInfoDouble(_Symbol, SYMBOL_BID), _Digits);
      double recentLow = iLow(_Symbol, _Period, iLowest(_Symbol, _Period, MODE_LOW, 5, 1));
      double recentHigh = iHigh(_Symbol, _Period, iHighest(_Symbol, _Period, MODE_HIGH, 5, 1));

      bool macdOversold = macdArray[2] < 0 && macdSignalArray[2] < 0 && macdArray[1] < 0 && macdSignalArray[1] < 0 && macdSignalArray[2] < macdArray[2] && macdSignalArray[1] > macdArray[1];
      bool macdOverbought = macdArray[2] > 0 && macdSignalArray[2] > 0 && macdArray[1] > 0 && macdSignalArray[1] > 0 && macdSignalArray[2] > macdArray[2] && macdSignalArray[1] < macdArray[1];

      if(tradeTicket <= 0 && ask > ma34Array[1] && cciArray[2] < 0 && cciArray[1] > 0 && macdOversold)
        {
         double balance = AccountInfoDouble(ACCOUNT_BALANCE);
         double risk = (accountRisk / 100) * balance;
         double stopLoss = NormalizeDouble(recentLow, _Digits);

         //We don't want the EA to open positions with a stop loss that's too small
         if(ask - stopLoss < minimalStopLoss * _Point)
           {
            return;
           }

         double takeProfit = NormalizeDouble(ask + riskReward * (ask - stopLoss), _Digits);

         //Calculate lots
         orderSize = CalcLots(MathAbs(ask - stopLoss) * _Point);
         double lotStep=SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
         orderSize = MathFloor(orderSize / lotStep) * lotStep;

         double maxVolume= SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_LIMIT);

         if(maxVolume > 0 && maxVolume - orderSize <= 0)
           {
            orderSize = maxVolume;
           }

         trade.Buy(orderSize, _Symbol, ask, stopLoss, takeProfit, NULL);

         //Set a timer so it doesn't open another trade too quickly and record the trade ticket
         tradeTicket = trade.ResultOrder();
         timePassed = false;
         EventSetTimer(PeriodSeconds(_Period) * 5);
        }
      else
         if(tradeTicket <= 0 && bid < ma34Array[1] && cciArray[2] > 0 && cciArray[1] < 0 && macdOverbought)
           {
            double balance = AccountInfoDouble(ACCOUNT_BALANCE);
            double risk = (accountRisk / 100) * balance;
            double stopLoss = NormalizeDouble(recentHigh, _Digits);

            if(stopLoss - bid < minimalStopLoss * _Point)
              {
               return;
              }

            double takeProfit = NormalizeDouble(bid + riskReward * (bid - stopLoss), _Digits);

            orderSize = CalcLots(MathAbs(bid - stopLoss) * _Point);
            double lotStep=SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
            orderSize = MathFloor(orderSize / lotStep) * lotStep;

            double maxVolume= SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_LIMIT);

            if(maxVolume > 0 && maxVolume - orderSize <= 0)
              {
               orderSize = maxVolume;
              }

            trade.Sell(orderSize, _Symbol, bid, stopLoss, takeProfit, NULL);

            tradeTicket = trade.ResultOrder();
            timePassed = false;
            EventSetTimer(PeriodSeconds(_Period) * 5);
           }
     }
  }

//+------------------------------------------------------------------+
//| Reset the timePassed variable                                    |
//+------------------------------------------------------------------+
void OnTimer(void)
  {
   timePassed = true;
  }

//+------------------------------------------------------------------+
//| Calculate the order lot size                                     |
//+------------------------------------------------------------------+
double CalcLots(double slPoints)
  {
   double tickSize = SymbolInfoDouble(_Symbol, SYMBOL_TRADE_TICK_SIZE);
   double tickValue = SymbolInfoDouble(_Symbol, SYMBOL_TRADE_TICK_VALUE);
   double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);

   double moneyPerLotStep = slPoints / tickSize * tickValue * lotStep;
   double lots = MathFloor((accountRisk / 100) / moneyPerLotStep) * lotStep;

   lots = MathMin(lots, SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX));
   lots = MathMax(lots, SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN));

   return lots;
  }

//+------------------------------------------------------------------+
//| Check if we should move a trailing stop loss                     |
//+------------------------------------------------------------------+
void CheckTrailingStops()
  {
   if(PositionSelectByTicket(tradeTicket) == false)
     {
      tradeTicket = 0;
      profitsPassed = 0;
     }

   for(int i = 0; i < PositionsTotal(); i++)
     {
      ulong posTicket = PositionGetTicket(i);

      if(PositionGetString(POSITION_SYMBOL) == _Symbol)
        {
         double posOpen = PositionGetDouble(POSITION_PRICE_OPEN);
         double posSl = PositionGetDouble(POSITION_SL);

         double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
         double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);

         if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
           {

            double sl = posSl + unit;

            sl = NormalizeDouble(sl, SYMBOL_TRADE_TICK_SIZE);

            if(ask - unit > posSl)
              {
               if(trade.PositionModify(posTicket, ask - unit, 0))
                 {
                  //We only want to take patial profit on the first trailing stop move
                  if(profitsPassed == 0)
                    {
                     trade.PositionClosePartial(posTicket, orderSize / 2);
                    }

                  profitsPassed++;
                 }
              }
           }
         else
            if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
              {
               double sl = posSl - unit;

               sl = NormalizeDouble(sl, SYMBOL_TRADE_TICK_SIZE);

               if(bid + unit < posSl)
                 {
                  if(trade.PositionModify(posTicket, bid + unit, 0))
                    {
                     if(profitsPassed == 0)
                       {
                        trade.PositionClosePartial(posTicket, orderSize / 2);
                       }

                     profitsPassed++;
                    }
                 }
              }
        }
     }
  }
//+------------------------------------------------------------------+
