//+------------------------------------------------------------------+
//| Tipu_EA.mq4 |
//+-----------------------------------------------------------------------------------------------+

#property copyright "Copyright 2021, Kaleem Haider"
#property link      "https://www.mql5.com/en/users/kaleem.haider/seller"
#property version   "1.00"
#property strict

//dependencies
#import "stdlib.ex4"
string ErrorDescription(int e);
//#import "urlmon.dll"
//int URLDownloadToFileW(int pCaller, string szURL, string szFileName, int dwReserved, int Callback);
#import

#define PipsPoint Get_PipsPoint()
#define PipsValue Get_PipsValue()
//+------------------------------------------------------------------+
//| Structure for Tipu Signals                                       |
//+------------------------------------------------------------------+
struct STRC_Tipu {
   bool              buy, sell, range;
};
//+------------------------------------------------------------------+
//| Structure for Order Price
//+------------------------------------------------------------------+
struct STRUC_orderPrice {
   double            buy, sell;
   void              reset() {
      buy = sell = 0.0;
   };
};
//+------------------------------------------------------------------+
//| Structure for Order Lots
//+------------------------------------------------------------------+
struct STRUC_tradeLots {
   double            buyLots, sellLots;
   void              reset() {
      buyLots = sellLots = 0.0;
   };
};
//+------------------------------------------------------------------+
//| used in #define PipsPoint
//+------------------------------------------------------------------+
double Get_PipsPoint() {
   double PP = (_Digits == 5 || _Digits == 3) ? _Point * 10 : _Point;
   return (PP);
}
//+------------------------------------------------------------------+
//| used in #defind PipsValue
//+------------------------------------------------------------------+
double Get_PipsValue() {
   double PV = (MarketInfo(_Symbol, MODE_TICKVALUE) * PipsPoint) / MarketInfo(_Symbol, MODE_TICKSIZE);
   return(PV);
}

//inputs
input bool     bTimeFilter                = true;     //Use Time Filter?
input int     iStartHour1                 = 10;     //Zone1: Start Hour
input int     iEndHour1                   = 18;   //Zone1: End Hour
input int     iStartHour2                 = 1;    //Zone2: Start Hour
input int     iEndHour2                   = 6;    //Zone2: End Hour
input string            sMACDSettings = "---MACD Settings--"; //MACD Settings
input int               FastEMA     =  12;   // Fast EMA Period
input int               SlowEMA     =  26;   // Slow EMA Period
input int               SignalSMA = 9; // Signal SMA Period
input int iShift = 1;              //MACD Shift (use 0 for current candle, 1 for previous)
input ENUM_APPLIED_PRICE eAppliedPrice = PRICE_CLOSE; //Applied Price
input string            sSignalType = "---Signal Types--"; //Signal Types
input bool              bReversals = false;             //Zero Line Cross
input bool              bMainSignal = true;            //Main Signal Cross
input string sTradeSettings = "---Trade Settings--"; //Trade Settings
input bool  bHedge = false;        //Hedging Trades (only if Broker allow)
input bool  bRevSignal = true;       //Close Trade on Reverse Signal
input bool bTP = true;                 //TP?
input double dTP = 20;                 //TP pips
input bool bSL = false;                 //SL?
input double dRisk = 50;               //SL Risk per Trade pips
input double dlots = 0.01;             //Lots per order
input double dMaxLots = 0.05;          //Max Lots Position
input bool  bTrailSL = true;           //Trail SL?
input bool bRiskFree = true;           //Risk Free Trade?
input double dTrailSL = 10;            //Trail Profit pips
input double dTrailCh = 5;             //Trail Cushion between current price and SL
input int iMagic = 420;                //iMagic Number

STRC_Tipu st_M15, st_H1;
STRUC_orderPrice stIniSL, stTrailSL;
string sDisplay, sIndicator;
static datetime lasttime;
//+------------------------------------------------------------------+
//| On Tick
//+------------------------------------------------------------------+
void OnTick(void) {
//check if you can trade or not, this will stop loading indicator calculation on every tick, may slow the EA
   if(lasttime == iTime(_Symbol, _Period, iShift)) return;
   else lasttime = iTime(_Symbol, _Period, iShift);
//check if you can trade or not, this will stop loading indicator calculation on every tick, may slow the EA

   if(Bars < 100) {
      Print("bars less than 100");
      return;
   }
   if(dTP < 10) {
      Print("TakeProfit less than 10");
      return;
   }
   if(AccountFreeMargin() < (1000 * dlots)) {
      Print("We have no money. Free Margin = ", AccountFreeMargin());
      return;
   }

//--------------------------------------------------
//Step#1 - Define Trading Signal - Tipu Trading
//--------------------------------------------------
//Time Filter
   if(bTimeFilter && TimeFilter(iStartHour1, iEndHour1, iStartHour2, iEndHour2) == "") return;

   double dhandleMain0 = iMACD(_Symbol, _Period, FastEMA, SlowEMA, SignalSMA, eAppliedPrice, MODE_MAIN, iShift);
   double dhandleMain1 = iMACD(_Symbol, _Period, FastEMA, SlowEMA, SignalSMA, eAppliedPrice, MODE_MAIN, iShift + 1);
   double dhandleSignal0 = iMACD(_Symbol, _Period, FastEMA, SlowEMA, SignalSMA, eAppliedPrice, MODE_SIGNAL, iShift);
   double dhandleSignal1 = iMACD(_Symbol, _Period, FastEMA, SlowEMA, SignalSMA, eAppliedPrice, MODE_SIGNAL, iShift + 1);

//iCustom(_Symbol,_Period,sIndicator,"",2,FastEMA,SlowEMA,SignalSMA,eAppliedPrice,"",bRevSignal,bMainSignal,0,5,clrNONE,clrNONE,"",false,clrNONE,clrNONE,clrNONE,clrNONE,"",false,"tmacd","","",1,false,false,false,7,1);
   st_M15.buy = (bReversals && dhandleMain0 > 0 && dhandleMain1 < 0) || (bMainSignal && (dhandleMain0 - dhandleSignal0) > 0 && (dhandleMain1 - dhandleSignal1) < 0);
   st_M15.sell = (bReversals && dhandleMain0 < 0 && dhandleMain1 > 0) || (bMainSignal && (dhandleMain0 - dhandleSignal0) < 0 && (dhandleMain1 - dhandleSignal1) > 0);

//this is a preferred way of defining the signals
   bool bBuy = st_M15.buy; // && st_H1.buy && !st_M15.range;
   bool bSell = st_M15.sell; // && st_H1.sell && !st_M15.range;

//--------------------------------------------------
//Step#2 - Enter Market First Trade
//--------------------------------------------------
//Buy Order
   if(bBuy) {
      //if stop loss acceptable open trade
      if(!bSL) {
         if(bRevSignal) Order_CloseAll(_Symbol, OP_SELL, iMagic);
         //allow only one trade at a time when there is no position
         double dcheck = GetPosition_Total_Lots(_Symbol, OP_BUY, iMagic);

         //if(GetPosition_Total_Lots(_Symbol,OP_BUY,iMagic)==0 && (bHedge || (!bHedge && GetPosition_Total_Lots(_Symbol,OP_SELL,iMagic)==0)))
         if(GetPosition_Total_Lots(_Symbol, OP_BUY, iMagic) < dMaxLots && (bHedge || (!bHedge && GetPosition_Total_Lots(_Symbol, OP_SELL, iMagic) == 0)))
            Order_Trade(_Symbol, OP_BUY, dlots, dRisk, bTP ? dTP : 0, "Tipu_EA" + (string)iMagic, iMagic, clrBlue);
      }
   }
//Sell Order
   if(bSell) {
      //if stop loss acceptable open trade
      if(!bSL) {
         if(bRevSignal) Order_CloseAll(_Symbol, OP_BUY, iMagic);
         //allow only one trade at a time when there is no position
         if(GetPosition_Total_Lots(_Symbol, OP_SELL, iMagic) < dMaxLots && (bHedge || (!bHedge && GetPosition_Total_Lots(_Symbol, OP_BUY, iMagic) == 0)))
            Order_Trade(_Symbol, OP_SELL, dlots, dRisk, bTP ? dTP : 0, "Tipu_EA" + (string)iMagic, iMagic, clrRed);
      }
   }
//--------------------------------------------------
//Step#3 - Manage Trade - this can be adding more position or trailing stop
//--------------------------------------------------
   if(bTrailSL && (GetPosition_Total_Lots(_Symbol, OP_BUY, iMagic) != 0 || GetPosition_Total_Lots(_Symbol, OP_SELL, iMagic) != 0))
      Order_Trail(false, dTrailSL, dTrailCh, iMagic, clrPurple);
   if(bRiskFree)
      Order_RiskFree(bHedge, dTrailSL, dlots, dMaxLots, iMagic, clrPurple);

   return;
}
//+------------------------------------------------------------------+
//| Order Trade
//+------------------------------------------------------------------+
int Order_Trade(string Symbol, int Type, double Lots, double SL, double TP, string Comment, int Magic, color Color = clrBlue) {
   int ocheck = true;
   double odPrice = 0.0, odSL = 0.0, odTP = 0.0;
   int ilasterror = 0;

   switch(Type) {
//Buy Order
   case OP_BUY:
      odPrice = NormalizeDouble(SymbolInfoDouble(Symbol, SYMBOL_ASK), _Digits);
      odSL = (SL > 0) ? (odPrice - SL * PipsPoint) : 0.0;
      odTP = (TP > 0) ? (odPrice + TP * PipsPoint) : 0.0;
      if(Lots != 0.0)
         if(OrderSend(Symbol, Type, Lots, odPrice, 3, odSL, odTP, Comment, Magic, 0, Color) < 0) {
            ilasterror = GetLastError();
            Print("Unable to send Buy Order " + Comment + " Magic Number: " + IntegerToString(Magic, 0) + "Price :" + DoubleToString(odPrice, _Digits)
                  + " SL: " + DoubleToString(odSL, _Digits) + " TP: " + DoubleToString(odTP, _Digits)
                  + IntegerToString(Magic) + " err#:" + (string)ilasterror + ": " + ErrorDescription(ilasterror));
            ocheck = ilasterror;
         }
      break;
//Sell Order
   case OP_SELL:
      odPrice = NormalizeDouble(SymbolInfoDouble(Symbol, SYMBOL_BID), _Digits);
      odSL = (SL > 0) ? (odPrice + SL * PipsPoint) : 0.0;
      odTP = (TP > 0) ? (odPrice - TP * PipsPoint) : 0.0;
      if(Lots != 0.0)
         if(OrderSend(Symbol, Type, Lots, odPrice, 3, odSL, odTP, Comment, Magic, 0, Color) < 0) {
            ilasterror = GetLastError();
            Print("Unable to send Sell Order " + Comment + " Magic Number: " + IntegerToString(Magic, 0) + "Price :" + DoubleToString(odPrice, _Digits)
                  + " SL: " + DoubleToString(odSL, _Digits) + " TP: " + DoubleToString(odTP, _Digits)
                  + IntegerToString(Magic) + " err#:" + (string)ilasterror + ": " + ErrorDescription(ilasterror));
            ocheck = ilasterror;
         }
      break;
   }
   return(ocheck);
}
//+------------------------------------------------------------------+
//| Close all Trade
//+------------------------------------------------------------------+
bool Order_CloseAll(string Symbol, int Type, int Magic) {

   int itotalOrders = OrdersTotal();
   int icounter = 0;

   if(GetPosition_Total_Lots(Symbol, Type, Magic) == 0) return(true);

//double dClosePrice;
   for(int d = itotalOrders - 1; d >= 0; d--) {
      if(!OrderSelect(d, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderSymbol() == Symbol && OrderType() == Type && OrderMagicNumber() == Magic)
         if(IsTradeAllowed() && OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 3, clrBlue))
            //Print("LastError = ", GetLastError());
            icounter++;
   }
   if(icounter) return(true);
   else return(false);
}
//+------------------------------------------------------------------+
//| Order change sl - not used here
//+------------------------------------------------------------------+
bool Order_ChangeSL(double NewSL, string Symbol, int Type, int Magic, color Color) {

   int itotalOrders = OrdersTotal();
   int icounter = 0;

   for(int d = itotalOrders - 1; d >= 0; d--) {
      if(!OrderSelect(d, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderSymbol() == Symbol && OrderMagicNumber() == Magic) {
         if(Type == OrderType() && OrderType() == OP_BUY && NewSL < SymbolInfoDouble(_Symbol, SYMBOL_BID) && NewSL > OrderStopLoss()) {
            if(!OrderModify(OrderTicket(), OrderOpenPrice(), NewSL, OrderTakeProfit(), OrderExpiration(), Color))
               Print("Cannot Modify Order# " + (string)OrderTicket() + " Type OP_BUY New SL " + DoubleToString(NewSL, _Digits) + " " + ErrorDescription(GetLastError()));
            else icounter++;
         }
         if(Type == OrderType() && OrderType() == OP_SELL && NewSL > SymbolInfoDouble(_Symbol, SYMBOL_ASK) && (NewSL < OrderStopLoss() || OrderStopLoss() == 0)) { //no zero check
            if(!OrderModify(OrderTicket(), OrderOpenPrice(), NewSL, OrderTakeProfit(), OrderExpiration(), Color))
               Print("Cannot Modify Order# " + (string)OrderTicket() + " Type OP_BUY New SL " + DoubleToString(NewSL, _Digits) + " " + ErrorDescription(GetLastError()));
            else icounter++;
         }
      }
   }
   if(icounter) return(true);
   else return(false);
}
//+------------------------------------------------------------------+
// Trail Stop in pips increments instead of ticks
// Trail Custion between current price and SL                                                                  |
//+------------------------------------------------------------------+
bool Order_Trail(bool bAggressive, double TrailStop, double TrailCushion, int Magic, color Color) {
   bool ocheck = false;
   double oSL = 0.0;
   double newSL = 0.0;
   double oProfit = 0.0;
   int oTSL = 0;

//Loop through orders
   for(int cnt = OrdersTotal() - 1; cnt >= 0; cnt--) {
      if(!OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) continue;

      int iOrderType          = OrderType();
      string sOrderSymbol     = OrderSymbol();
      double dOrderStopLoss   = OrderStopLoss();
      double dOrderTakeProfit = OrderTakeProfit();
      double dOrderOpenPrice  = OrderOpenPrice();
      double dOrderPnL        = OrderProfit() + OrderSwap() + OrderCommission();
      int iOrderTicket        = OrderTicket();
      int iOrderMagic         = OrderMagicNumber();

      //modify buy order trailing stops
      if((iOrderType == OP_BUY) && (sOrderSymbol == _Symbol)) {
         oSL = (dOrderStopLoss - dOrderOpenPrice) / PipsPoint * (dOrderStopLoss >= 0); //calculated in pips
         oProfit = MathMax(0, (SymbolInfoDouble(_Symbol, SYMBOL_BID) - dOrderOpenPrice) / PipsPoint - TrailCushion);
         oTSL = int((oProfit - oSL) / TrailStop);
         newSL = (dOrderStopLoss + double(oTSL - 1) * TrailStop * PipsPoint);
         newSL *= (((oProfit) >= (oSL + (double(oTSL) * TrailStop))) && newSL >= dOrderStopLoss && newSL >= dOrderOpenPrice + TrailStop * PipsPoint);
         if((newSL > 0 && newSL != dOrderStopLoss) && (bAggressive || (!bAggressive && dOrderPnL > 0.0))) {
            if(!OrderModify(iOrderTicket, dOrderOpenPrice, newSL, dOrderTakeProfit, 0, Color))
               Print("Cannot Modify Order# " + (string)iOrderTicket + " Type " + (string)iOrderType + " New SL " + DoubleToString(newSL, _Digits) + " " + ErrorDescription(GetLastError()));
            else ocheck = true;
         }
      }

      //modify sell order trailing stops
      if((iOrderType == OP_SELL) && (sOrderSymbol == _Symbol)) {
         oSL = ((dOrderOpenPrice - dOrderStopLoss) / PipsPoint) * (dOrderStopLoss != 0);
         oProfit = MathMax(0, (dOrderOpenPrice - SymbolInfoDouble(_Symbol, SYMBOL_ASK)) / PipsPoint - TrailCushion);
         oTSL = int((oProfit - oSL) / TrailStop);
         //Print("oTSL:"+(string)oTSL);
         newSL = ((dOrderStopLoss == 0 ? dOrderOpenPrice : dOrderStopLoss) - double(oTSL - 1) * TrailStop * PipsPoint);
         //Print("Sell NewSL:"+(string)newSL);
         newSL *= (oProfit >= (oSL + (double(oTSL) * TrailStop)) && newSL <= (dOrderStopLoss == 0 ? dOrderOpenPrice : dOrderStopLoss) && newSL <= dOrderOpenPrice - TrailStop * PipsPoint);
         //Print("Sell NewSL:"+(string)newSL);
         if((newSL > 0 && newSL != dOrderStopLoss) && (bAggressive || (!bAggressive && dOrderPnL > 0.0))) {
            if(!OrderModify(iOrderTicket, dOrderOpenPrice, newSL, dOrderTakeProfit, 0, Color))
               Print("Cannot Modify Order# " + (string)iOrderTicket + " Type " + (string)iOrderType + " New SL " + DoubleToString(newSL, _Digits) + " " + ErrorDescription(GetLastError()));
            else ocheck = true;
         }
      }
   }
   return(ocheck);
}
//+------------------------------------------------------------------+
//| Lock Pips for all the orders
//| this is a bonus function, not used in the OnTick()
//+------------------------------------------------------------------+
bool Order_Lock(double LockPips, int Magic, color Color) {
   double oSL = 0.0;
   double oSLD = 0.0;
   double newSL = 0.0;
   double oProfit = 0.0;
   int oTSL = 0;
   int icounter = 0;

   for(int cnt = OrdersTotal() - 1; cnt >= 0; cnt--) {
      if(!OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) continue;

      int iOrderType          = OrderType();
      string sOrderSymbol     = OrderSymbol();
      double dOrderStopLoss   = OrderStopLoss();
      double dOrderTakeProfit = OrderTakeProfit();
      double dOrderOpenPrice  = OrderOpenPrice();
      int iOrderTicket        = OrderTicket();
      int iOrderMagic         = OrderMagicNumber();

      if((iOrderType == OP_BUY) && (sOrderSymbol == _Symbol) && (iOrderMagic == Magic)) {
         oSL = (dOrderStopLoss - dOrderOpenPrice) / PipsPoint;    //calculated in pips
         oSLD = dOrderStopLoss;
         oProfit = MathMax(0, (SymbolInfoDouble(_Symbol, SYMBOL_BID) - dOrderOpenPrice) / PipsPoint); //calculated in pips
         if(LockPips != 0.0 && oProfit >= LockPips && oSL < 0) {
            newSL = dOrderOpenPrice + LockPips * PipsPoint;
            if(!OrderModify(iOrderTicket, dOrderOpenPrice, newSL, dOrderTakeProfit, 0, Color))
               Print("Order Modify Error: " + ErrorDescription(GetLastError()) + " newSL: " + DoubleToString(newSL, _Digits));
            else icounter++;
         }
      }
      if((iOrderType == OP_SELL) && (sOrderSymbol == _Symbol) && (iOrderMagic == Magic)) {
         oSL = (dOrderOpenPrice - dOrderStopLoss) / PipsPoint;    //calculated in pips
         oSLD = dOrderStopLoss;
         oProfit = MathMax(0, (dOrderOpenPrice - SymbolInfoDouble(_Symbol, SYMBOL_ASK)) / PipsPoint); //calculated in pips
         //Print("open price: " + (string)dOrderOpenPrice + " lock profit : " + (string)oProfit);
         if(LockPips != 0.0 && oProfit >= LockPips && oSL < 0) {
            newSL = dOrderOpenPrice - LockPips * PipsPoint;
            if(!OrderModify(iOrderTicket, dOrderOpenPrice, newSL, dOrderTakeProfit, 0, Color))
               Print("Order Modify Error: " + ErrorDescription(GetLastError()) + " newSL: " + DoubleToString(newSL, _Digits));
            else icounter++;
         }
      }
   }
   if(icounter > 0) return(true);
   else return (false);
}
//+------------------------------------------------------------------+
//| Enter a Risk Free Trade, this is the pyaramiding function with no trail stop loss until total positions == MaxLots
//+------------------------------------------------------------------+
int Order_RiskFree(bool Hedge, double RiskFreeLock, double IncLots, double MaxLots, int Magic, color Color) {

   double dSL = 0.0, dPrice = 0.0, dLots = 0.0, dProfit = 0;
   int icheck = 0;
   STRUC_tradeLots tradeLots, positionLots;
   STRUC_orderPrice oOpen, oPips, oRisk;

//Get lots to trade
   tradeLots.buyLots  = Order_GetLots(Hedge, OP_BUY, IncLots, MaxLots, Magic);
   tradeLots.sellLots = Order_GetLots(Hedge, OP_SELL, IncLots, MaxLots, Magic);

//Get position lots
   positionLots.buyLots = MathAbs(GetPosition_Total_Lots(_Symbol, OP_BUY, Magic));
   positionLots.sellLots = MathAbs(GetPosition_Total_Lots(_Symbol, OP_SELL, Magic));

//Calculate breakeven point for total OP_BUY orders
   oOpen.buy = (GetPosition_Total_Opn(_Symbol, OP_BUY, Magic) + SymbolInfoDouble(_Symbol, SYMBOL_ASK) * tradeLots.buyLots);
   oOpen.buy /= (positionLots.buyLots + tradeLots.buyLots) == 0 ? 1 : (positionLots.buyLots + tradeLots.buyLots);
   oOpen.sell = (GetPosition_Total_Opn(_Symbol, OP_SELL, Magic) + SymbolInfoDouble(_Symbol, SYMBOL_BID) * tradeLots.sellLots);
   oOpen.sell /= (positionLots.sellLots + tradeLots.sellLots) == 0 ? 1 : (positionLots.sellLots + tradeLots.sellLots);

//how much pips will you lose if the trade close agains you.  this assumes all the trades have stop loss
   oRisk.buy = GetPosition_Total_Risk(_Symbol, OP_BUY, Magic);
   oRisk.sell = GetPosition_Total_Risk(_Symbol, OP_SELL, Magic);

//pyramid + trail stop
   if(SymbolInfoDouble(_Symbol, SYMBOL_ASK) > (oOpen.buy + RiskFreeLock * PipsPoint) && positionLots.buyLots != 0) {
      //Pyramid
      if(tradeLots.buyLots > 0) {
         Order_ChangeSL(SymbolInfoDouble(_Symbol, SYMBOL_ASK) - RiskFreeLock * PipsPoint, _Symbol, OP_BUY, Magic, clrPurple);
         Order_Trade(_Symbol, OP_BUY, tradeLots.buyLots, (SymbolInfoDouble(_Symbol, SYMBOL_ASK) - oOpen.buy) / PipsPoint, (bTP ? dTP : 0), "Tipu EA Risk Free Trade", Magic, clrPurple);
      }
      //Trail stop by increments of RiskFreeLock Pips
      if(positionLots.buyLots == MaxLots)
         Order_ChangeSL(SymbolInfoDouble(_Symbol, SYMBOL_ASK) - RiskFreeLock * PipsPoint, _Symbol, OP_BUY, Magic, clrPurple);
   }
   if(SymbolInfoDouble(_Symbol, SYMBOL_BID) < (oOpen.sell - RiskFreeLock * PipsPoint) && positionLots.sellLots != 0) {
      //pyramid
      if(tradeLots.sellLots > 0) {
         Order_ChangeSL(SymbolInfoDouble(_Symbol, SYMBOL_BID) + RiskFreeLock * PipsPoint, _Symbol, OP_SELL, Magic, clrPurple);
         Order_Trade(_Symbol, OP_SELL, tradeLots.sellLots, (oOpen.sell - SymbolInfoDouble(_Symbol, SYMBOL_BID)) / PipsPoint, (bTP ? dTP : 0), "Tipu EA Risk Free Trade", Magic, clrPurple);
      }
      //Trail stop by increments of RiskFreeLock Pips
      if(positionLots.sellLots == MaxLots)
         Order_ChangeSL(SymbolInfoDouble(_Symbol, SYMBOL_BID) + RiskFreeLock * PipsPoint, _Symbol, OP_SELL, Magic, clrPurple);
   }

   return (icheck);
}
//+------------------------------------------------------------------+
//| Get lots used in risk free trade
//+------------------------------------------------------------------+
double Order_GetLots(bool Hedge, int oType, double IncLots, double MaxLots, int Magic) {
   double dLots = 0.0;

   if(Hedge)
      switch(oType) {
      case OP_BUY:
         dLots = MathMax(MathMin(MaxLots - GetPosition_Total_Lots(_Symbol, oType, Magic), IncLots), 0.0);
         break;
      case OP_SELL:
         dLots = MathMax(MathMin(MaxLots - GetPosition_Total_Lots(_Symbol, oType, Magic), IncLots), 0.0);
         break;
      } else
      switch(oType) {
      case OP_BUY:
         dLots = (GetPosition_Total_Lots(_Symbol, OP_SELL, Magic) != 0) ? 0.0 : MathMax(MathMin(MaxLots - GetPosition_Total_Lots(_Symbol, oType, Magic), IncLots), 0.0);
         break;
      case OP_SELL:
         dLots = (GetPosition_Total_Lots(_Symbol, OP_BUY, Magic) != 0) ? 0.0 : MathMax(MathMin(MaxLots - GetPosition_Total_Lots(_Symbol, oType, Magic), IncLots), 0.0);
         break;
      }

   return (dLots);
}
//+------------------------------------------------------------------+
//| Get Total Position Lots + Buy - Sell
//+------------------------------------------------------------------+
double GetPosition_Total_Lots(string Symbol, int Type, int Magic) {
   double LotsCount = 0;
   for(int i = OrdersTotal() - 1; i >= 0; i--) {
      if(!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderSymbol() == Symbol && OrderMagicNumber() == Magic) {
         if(OrderType() == OP_BUY &&  Type == OP_BUY) LotsCount += OrderLots();
         if(OrderType() == OP_SELL && Type == OP_SELL) LotsCount += OrderLots();
      }
   }
   return(LotsCount);
}
//+------------------------------------------------------------------+
//| Get Position Total profit in pips
//+------------------------------------------------------------------+
double GetPosition_Total_AvPips(string sSymbol, int iType, int Magic) {
   double oPips = 0.0;
   double Lots = 0.0;
   for(int i = OrdersTotal() - 1; i >= 0; i--) {
      if(!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderSymbol() == sSymbol && OrderType() == iType && OrderMagicNumber() == Magic) {
         if(iType == OP_BUY) oPips +=  (SymbolInfoDouble(_Symbol, SYMBOL_BID) - OrderOpenPrice()) / PipsPoint * OrderLots();
         if(iType == OP_SELL) oPips += (OrderOpenPrice() - SymbolInfoDouble(_Symbol, SYMBOL_ASK)) / PipsPoint * OrderLots();
         //LotsPrice = LotsPrice + OrderLots()*OrderOpenPrice();
      }
   }
   if(GetPosition_Total_Lots(sSymbol, iType, Magic) != 0.0)
      oPips /= MathAbs(GetPosition_Total_Lots(sSymbol, iType, Magic));
   return (oPips);
}
//+------------------------------------------------------------------+
//| Get Total Average Open price for buy/sell orders
//+------------------------------------------------------------------+
double GetPosition_Total_AvOpn(string sSymbol, int iType, int Magic) {
   double oOpen = 0.0;
   double Lots = 0.0;
   for(int i = OrdersTotal() - 1; i >= 0; i--) {
      if(!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderSymbol() == sSymbol && OrderType() == iType && OrderMagicNumber() == Magic)
         oOpen += OrderOpenPrice() * OrderLots();
   }
   if(GetPosition_Total_Lots(sSymbol, iType, Magic) != 0.0)
      oOpen /= MathAbs(GetPosition_Total_Lots(sSymbol, iType, Magic));
   return (oOpen);
}
//+------------------------------------------------------------------+
//| Get Total Cost OrderOpen*OrderLots
//+------------------------------------------------------------------+
double GetPosition_Total_Opn(string sSymbol, int iType, int Magic) {
   double oOpen = 0.0;
   double Lots = 0.0;
   for(int i = OrdersTotal() - 1; i >= 0; i--) {
      if(!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderSymbol() == sSymbol && OrderType() == iType && OrderMagicNumber() == Magic)
         oOpen += (OrderOpenPrice() * OrderLots());
   }
   return (oOpen);
}
//+------------------------------------------------------------------+
//| Get Position Total Stop Loss this is how much pips you will loose if the trade goes against you
//+------------------------------------------------------------------+
double GetPosition_Total_Risk(string sSymbol, int iType, int Magic) {
   double oRisk = 0.0;
   double Lots = 0.0;
   for(int i = OrdersTotal() - 1; i >= 0; i--) {
      if(!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderSymbol() == sSymbol && OrderType() == iType && OrderMagicNumber() == Magic)
         oRisk += OrderStopLoss() * OrderLots();
   }
   if(GetPosition_Total_Lots(sSymbol, iType, Magic) != 0.0)
      oRisk /= MathAbs(GetPosition_Total_Lots(sSymbol, iType, Magic));
   return (oRisk);
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string TimeFilter(int startHour1, int endHour1, int startHour2, int endHour2) {
//bool test,
   bool test1, test2;
   string zone    = "";
   string zone1   = "";
   string zone2   = "";

//int hour = TimeHour(TimeUSD());
//int minute = TimeMinute(TimeUSD());

   int hour = TimeHour(TimeCurrent());
   int minute = TimeMinute(TimeCurrent());

   if(startHour1 > endHour1)
      test1 = ((hour >= startHour1) || (hour <= endHour1));
   else test1 = !(hour < startHour1 || hour > endHour1);

   if(startHour2 > endHour2)
      test2 = ((hour >= startHour2) || (hour <= endHour2));
   else test2 = !(hour < startHour2 || hour > endHour2);

   if(test1) zone1 = "#1 " + IntegerToString(startHour1) + "h->" + IntegerToString(endHour1) + "h";
   if(test2) zone2 = "#2 " + IntegerToString(startHour2) + "h->" + IntegerToString(endHour2) + "h";

   zone = zone1 + zone2;

//test = test1 || test2 || test3;

   return (zone);
}
//+------------------------------------------------------------------+
