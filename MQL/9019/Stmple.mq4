//+------------------------------------------------------------------+
//|                                                       S!mple.mq4 |
//|                                         Version 1.2 / 02.07.2009 |
//|                                  Copyright © 2009, Mike Kaufmann |
//|                                            http://www.cractix.ch |
//|                                                  mike@cractix.ch |
//|                                                    ICQ# 38561935 |
//+------------------------------------------------------------------+
//|           dedicated to natalia, the best girlfriend in the world |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, cractix.ch"
#property link      "http://www.cractix.ch"

extern int    magicNum    = 10293847;
extern double lotSize     = 0.1;
extern string info1       = "number of simultaneous trades per currency";
extern int    numOrders   = 1;
extern string info2       = "stop loss in currency value, 0 = no stop loss";
extern double stopLoss    = 0;
extern string info3       = "take profit in currency value, 0 = no take profit";
extern double takeProfit  = 0;
extern string info4       = "margin for trend calculation in bars";
extern int    trendMargin = 10;
extern int    smaFast     = 50;
extern int    smaSlow     = 200;
extern bool   makeTrades  = false;

int    orders  = 0;
int    ordCur  = 0;
double bid     = 0;
double ask     = 0;
double sl      = 0;
double tp      = 0;
string comment = "";
int    lastAdj = 0;
int    timeFrame = PERIOD_M5;

string currencies[6];

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
   currencies[0] = "EURUSDm";
   currencies[1] = "USDCHFm";
   currencies[2] = "USDCADm";
   currencies[3] = "AUDUSDm";
   currencies[4] = "GBPUSDm";
   currencies[5] = "USDJPYm";
   
   return(0);
}

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
{
   return(0);
}

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{
   comment = "\n\nS!mple up and running on...\n\n";
   
   for(int i=0;i<ArraySize(currencies);i++)
   {
      comment = comment + currencies[i] + "\n";
      
      string smaSignal = smaSignal(i);
      string treSignal = trendSignal(currencies[i]);
      
      if(StringSubstr(smaSignal, 0, 3) == "BUY") {
         closeOpenOrders(currencies[i], OP_SELL);
         openBuyOrder(currencies[i]);
      }
      if(StringSubstr(smaSignal, 0, 4) == "SELL") {
         closeOpenOrders(currencies[i], OP_BUY);
         openSellOrder(currencies[i]);
      }
      
      comment = comment + "SMA-Signal: " + smaSignal + "\nTrend-Signal: " + treSignal + "\n";
      comment = comment + "\n";
   }
   
   Comment(comment);
   
   return(0);
}

//+------------------------------------------------------------------+
//| checkSignal                                                      |
//+------------------------------------------------------------------+
string smaSignal(int i)
{
   double smaFastCurr = getFastSMA(currencies[i], 0);
   double smaFastPrev = getFastSMA(currencies[i], 1);
   double smaFastHist = getFastSMA(currencies[i], 2);
   
   double smaSlowCurr = getSlowSMA(currencies[i], 0);
   double smaSlowPrev = getSlowSMA(currencies[i], 1);
   double smaSlowHist = getSlowSMA(currencies[i], 2);
   
   string diff = calcDiff(currencies[i], smaFastCurr, smaSlowCurr);
   
   if(smaFastHist < smaSlowHist && smaFastPrev > smaSlowPrev) {
      return("BUY, " + diff + " Pips difference");
   } else if(smaFastHist > smaSlowHist && smaFastPrev < smaSlowPrev) {
      return("SELL, " + diff + " Pips difference");
   } else {
      return("WAIT, " + diff + " Pips difference");
   }
}

//+------------------------------------------------------------------+
//| trendSignal                                                      |
//+------------------------------------------------------------------+
string trendSignal(string currency)
{
   double trendCurr = getSlowSMA(currency, 0);
   double trendPrev = getSlowSMA(currency, trendMargin);
   
   string diff = calcDiff(currency, trendCurr, trendPrev);
   
   if(trendPrev < trendCurr) {
      return("UP, " + diff + " Pips difference");
   } else if(trendPrev > trendCurr) {
      return("DOWN, " + diff + " Pips difference");
   } else {
      return("WAIT, " + diff + " Pips difference");
   }
}

//+------------------------------------------------------------------+
//| getFastSMA                                                       |
//+------------------------------------------------------------------+
double getFastSMA(string currency, int shift)
{
   double fastSMA = iMA(currency, timeFrame, smaFast, 0, MODE_LWMA, PRICE_CLOSE, shift);
   return(fastSMA);
}

//+------------------------------------------------------------------+
//| getSlowSMA                                                       |
//+------------------------------------------------------------------+
double getSlowSMA(string currency, int shift)
{
   double slowSMA = iMA(currency, timeFrame, smaSlow, 0, MODE_SMA, PRICE_CLOSE, shift);
   return(slowSMA);
}

//+------------------------------------------------------------------+
//| calcDiff                                                         |
//+------------------------------------------------------------------+
string calcDiff(string currency, double smaFast, double smaSlow)
{
   double diff    = 0;
   int    multi   = 0;
   int    digits  = MarketInfo(currency,MODE_DIGITS);
   
   if(digits == 5) {
      multi = 100000;
   }
   if(digits == 4) {
      multi = 10000;
   }
   if(digits == 3) {
      multi = 1000;
   }
   if(digits == 2) {
      multi = 100;
   }
   
   if(smaFast >= smaSlow) {
      diff = smaFast-smaSlow;
   }
   if(smaFast < smaSlow) {
      diff = smaSlow-smaFast;
   }
   
   return(DoubleToStr((diff*multi),1));
}

//+------------------------------------------------------------------+
//| openBuyOrder                                                     |
//+------------------------------------------------------------------+
void openBuyOrder(string currency)
{
   if(makeTrades == true) {
      orders = OrdersTotal();
      ordCur = 0;
      
      ask = MarketInfo(currency,MODE_ASK);
      bid = MarketInfo(currency,MODE_BID);
      
      for(int o=0;o<orders;o++) {
         if(OrderSelect(o, SELECT_BY_POS)==true) {
            if(OrderSymbol() == currency) {
               ordCur++;
            }
         }
      }
      
      for(int i=ordCur;i<numOrders;i++) {
         if(AccountFreeMarginCheck(currency,OP_SELL,lotSize) >= (AccountBalance()/5)) {
            if(stopLoss != 0) {
               int stopLossPips = calcStopLossPips(currency);
               sl = (bid-stopLossPips*MarketInfo(currency,MODE_POINT));
            } else {
               sl = stopLoss;
            }
            if(takeProfit != 0) {
               int takeProfitPips = calcTakeProfitPips(currency);
               tp = (bid+takeProfitPips*MarketInfo(currency,MODE_POINT));
            } else {
               tp = takeProfit;
            }
            OrderSend(currency,OP_BUY,lotSize,ask,3,0,tp,"S!mple",magicNum,0,Green);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| openSellOrder                                                    |
//+------------------------------------------------------------------+
void openSellOrder(string currency)
{
   if(makeTrades == true) {
      orders = OrdersTotal();
      ordCur = 0;
      
      ask = MarketInfo(currency,MODE_ASK);
      bid = MarketInfo(currency,MODE_BID);
      
      for(int o=0;o<orders;o++) {
         if(OrderSelect(o, SELECT_BY_POS)==true) {
            if(OrderSymbol() == currency) {
               ordCur++;
            }
         }
      }
      
      for(int i=ordCur;i<numOrders;i++) {
         if(AccountFreeMarginCheck(currency,OP_SELL,lotSize) >= (AccountBalance()/5)) {
            if(stopLoss != 0) {
               int stopLossPips = calcStopLossPips(currency);
               sl = (ask+stopLossPips*MarketInfo(currency,MODE_POINT));
            } else {
               sl = stopLoss;
            }
            if(takeProfit != 0) {
               int takeProfitPips = calcTakeProfitPips(currency);
               tp = (ask-takeProfitPips*MarketInfo(currency,MODE_POINT));
            } else {
               tp = takeProfit;
            }
            OrderSend(currency,OP_SELL,lotSize,bid,3,sl,tp,"S!mple",magicNum,0,Red);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| closeOpenOrders                                                  |
//+------------------------------------------------------------------+
void closeOpenOrders(string currency, int orderType)
{
   orders = OrdersTotal();
   for(int i=0;i<orders;i++) {
      if(OrderSelect(i, SELECT_BY_POS)==true) {
         if(OrderType() == orderType && OrderSymbol() == currency && orderType == OP_SELL) {
            ask = MarketInfo(currency,MODE_ASK);
            OrderClose(OrderTicket(),OrderLots(),ask,3,Red);
         }
         if(OrderType() == orderType && OrderSymbol() == currency && orderType == OP_BUY) {
            bid = MarketInfo(currency,MODE_BID);
            OrderClose(OrderTicket(),OrderLots(),bid,3,Green);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| calcStopLossPips                                                 |
//+------------------------------------------------------------------+
int calcStopLossPips(string currency)
{
   int calc = calcCurrencyPips(currency, stopLoss);
   return(calc);
}

//+------------------------------------------------------------------+
//| calcTakeProfitPips                                               |
//+------------------------------------------------------------------+
int calcTakeProfitPips(string currency)
{
   int calc = calcCurrencyPips(currency, takeProfit);
   return(calc);
}


//+------------------------------------------------------------------+
//| calcCurrencyPips                                                 |
//+------------------------------------------------------------------+
int calcCurrencyPips(string currency, int diff)
{
   return(MathFloor(diff / (MarketInfo(currency,MODE_TICKVALUE) * lotSize)));
}

//+------------------------------------------------------------------+