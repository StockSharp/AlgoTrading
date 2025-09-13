//+------------------------------------------------------------------+
//| Ilan Dinamic 1.61 HT.mq5                                         |
//| Copyright 2014, Vasiliy Sokolov.                                 |
//| http://www.mql5.com                                              |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, developing Vasiliy Sokolov."
#property link "http://www.mql5.com"
#property version "1.60"

#include <Prototypes.mqh>
#include <Trade\Trade.mqh>
#include <TradeState.mqh>
#include <Environment.mqh>
#include <Indicators.mqh>
#include <TesterTable.mqh>
#include <DrawHedgePosition.mqh>

/*
               SECTION OF THE EA PARAMETERS
      (the parameter names match the names version
              Ilan Dinamic for MetaTrader 4)
*/
//+------------------------------------------------------------------+
//| Main section of the EA parameters.                               |
//+------------------------------------------------------------------+
input string PRIMARY_MM_PARAMETERS="";
//+------------------------------------------------------------------+
//| How much to multiply lot when placing the next tribe.            |
//| For example, if LotExponent = 1.4: the first lot is 0.1, the following: |
//| 0.16, 0.26, 0.43;                                                |
//+------------------------------------------------------------------+
input double LotExponent=1.4;
//+------------------------------------------------------------------+
//| The maximum number of simultaneously open orders.                |
//+------------------------------------------------------------------+
input int MaxTrades=10;
//+------------------------------------------------------------------+
//| True if you are using a dynamic price range.                     |
//+------------------------------------------------------------------+
input bool DynamicPips=true;
//+------------------------------------------------------------------+
//| Level of the price channel in pips by default (M15).             | 
//+------------------------------------------------------------------+
input int DefaultPips=120;
//+------------------------------------------------------------------+
//| The last number of bars within which is calculated               |
//| the price range is High-Low.                                     |
//+------------------------------------------------------------------+
input int Glubina=24;
//+------------------------------------------------------------------+
//| The factor involved in the calculation of the maximum range.     |
//+------------------------------------------------------------------+
input int DEL=3;
//+------------------------------------------------------------------+
//| Lot size for trading.                                            |
//+------------------------------------------------------------------+
input double Lots=0.1;
//+------------------------------------------------------------------+
//| How many digits after the decimal point in Lota to count.        |
//| 0 - normal lots (1.0), 1 - miniati (0.1), 2 - micro (0.01)       |
//+------------------------------------------------------------------+
input int lotdecimal=1;
//+------------------------------------------------------------------+
//| Upon reaching how many pips to close the deal.                   |
//+------------------------------------------------------------------+
input double TakeProfit=100.0;
//+------------------------------------------------------------------+
//| How much can vary the price, if the DC                           |
//| will ask for re-quotes (at the last moment a bit will change the price). |
//+------------------------------------------------------------------+
input ulong slippage=30;
//+------------------------------------------------------------------+
//| Unique identifier of the expert (magic number).                  |
//+------------------------------------------------------------------+
input int MagicNumber=2222;
//+------------------------------------------------------------------+
//| Section of parameters responsible for the entry into the market. |
//+------------------------------------------------------------------+
input string ENTRY_POSITION_PARAMETERS;
//+------------------------------------------------------------------+
//| RSI period.                                                      |
//+------------------------------------------------------------------+
input int RsiPeriod=14;
//+------------------------------------------------------------------+
//| The lower bound RSI.                                             |
//+------------------------------------------------------------------+
input double RsiMinimum=30.0;
//+------------------------------------------------------------------+
//| The upper bound of RSI.                                          |
//+------------------------------------------------------------------+
input double RsiMaximum=70.0;
//+------------------------------------------------------------------+
//| Section configuring KLASSICHESKIE stop-loss.                     |
//+------------------------------------------------------------------+
input string USING_CLASSIC_STOP="";
//+------------------------------------------------------------------+
//| True if you want to use the classical level                      |
//| Stop loss.                                                       | 
//+------------------------------------------------------------------+
input bool UseStopLoss=false;
//+------------------------------------------------------------------+
//| Level classic stop-loss.                                         |
//+------------------------------------------------------------------+
input double StopLoss=500.0;
//+------------------------------------------------------------------+
//| Section configuring the stop loss on equity.                     |
//+------------------------------------------------------------------+
input string USING_EQUITY_STOP="";
//+------------------------------------------------------------------+
//| True if you want to use stop on equity.                          |
//+------------------------------------------------------------------+
input bool UseEquityStop=false;
//+------------------------------------------------------------------+
//| The profit percentage of all open positions, equity current accounts. |
//| When privyshenii this interest is included trailing åquity.      |
//+------------------------------------------------------------------+
input double EquityPercent=1.0;
//+------------------------------------------------------------------+
//| % loss of the maximum achieved equity.                           |
//+------------------------------------------------------------------+
input double TotalEquityRisk=20.0;
//+------------------------------------------------------------------+
//| Section configuring a trailing stop. |
//+------------------------------------------------------------------+
input string USING_TRAILING_STOP="";
//+------------------------------------------------------------------+
//| True if you want to use a trailing stop.                         |
//+------------------------------------------------------------------+
input bool UseTrailingStop=false;
//+------------------------------------------------------------------+
//| Level weighted average profit of all positions in pips           |
//| when privyshenii which made the trawl.                           |
//+------------------------------------------------------------------+
input double TrailStart=100.0;
//+------------------------------------------------------------------+
//| Level in pips between the current price and the StopLoss, which must |
//| support.                                                         |
//+------------------------------------------------------------------+
input double TrailStop=100.0;
//+------------------------------------------------------------------+
//| Section configuring the stop on the CCI indicator.               |
//+------------------------------------------------------------------+
input string USING_CCI_STOP="";
//+------------------------------------------------------------------+
//| True if all positions should be closed when reaching             |
//| CCI level CCILevel.                                              |
//+------------------------------------------------------------------+
input bool UseCCIStop;
//+------------------------------------------------------------------+
//| Indicator period GCCI                                            |
//+------------------------------------------------------------------+
input int CCIPeriod=55;
//+------------------------------------------------------------------+
//| The absolute level of the CCI indicator, if exceeded,            |
//| all previously open positions are closed (the Counterpart of this|
//| parameter in MT4 is the variable 'Drop').                        |
//+------------------------------------------------------------------+
input double CCILevel=500;
//+------------------------------------------------------------------+
//| Section configuring the closing time.                            |
//+------------------------------------------------------------------+
input string USING_TIME_STOP="";
//+------------------------------------------------------------------+
//| True, if you use the closing time.                               |
//+------------------------------------------------------------------+
input bool UseCloseByTime=false;
//+------------------------------------------------------------------+
//| The time since opening the first position in hours, after        |
//| exceeding which will be closed all positions. This works,        |
//| if you use the closing time (UseCloseByTime = true).             |
//+------------------------------------------------------------------+
input int MaxTradeOpenHours=48;

/*
               SECTION GLOBAL VARIABLES EXPERT
         (not to be confused with global variables!)
*/
CEnvironment Environment;              // The current environment expert.
CIndicators Indicators;                // Interface to the required indicators.
CTrade Trade;                          // Trading EA module.
CTradeState State;                     // The control Module of the trading state.
CTesterTable Tester;                   // Module testing table positions.
CDrawHedgePosition Draw;               // Renderer transactions on graphics (all prettiness here).
string EAName = "Ilan 1.6 Dinamic HT"; //Name of the expert.
int hist_trans;                        // Last saved number of historical transactions.
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   DrawIndicators();
//iRSI(Symbol(), PERIOD_CURRENT, 14, PRICE_CLOSE);
   Environment.SetMagic(MagicNumber);
   Trade.SetExpertMagicNumber(MagicNumber);
   Trade.SetDeviationInPoints(slippage);
   State.PrintSleeping(true);
   hist_trans=0;
//Draw.Enable(false);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   Draw.DeleteAutoTracing();
   DrawHistoryPos();
   Environment.Refresh();
   if(UseTrailingStop && !UseStopLoss && Environment.GetPositionsTotal()>0)
      TrailingAlls(TrailStart,TrailStop,Environment.AveragePrice());
   if(UseCCIStop)
      CloseByCCI();
   if(UseEquityStop)
      CloseByEquity();
   if(!Environment.NewBarDetected())return;
   if(UseCloseByTime)
      CloseByTime();
   bool isNewPosition=false;
   bool b1 = IsLastPositionLost();
   bool b2 = Environment.GetPositionsTotal() < MaxTrades;
   if(b1 && b2)
      isNewPosition=OpenAveragingLost();
   else if(Environment.GetPositionsTotal()==0)
      isNewPosition=OpenFirstPositionByRSI();
   if(isNewPosition)
      RecalcSLTP();
   Tester.PrintTable();
   DrawSLTP();
//---
  }
//+------------------------------------------------------------------+
//| Returns true, if the lesion is on the last position exceeds      |
//| a certain limit in points. Returns false if not                  |
//| case. The marginal loss is calculated as a price range for       |
//| the last 'Glubina' bars multiplied by DEL.                       |
//| The algorithm of calculation, see special functions GetDynamicPips(). |
//| RESULT                                                           |
//| True if the amount of the loss last position, exceeds            |
//| critical loss.                                                   |
//+------------------------------------------------------------------+
bool IsLastPositionLost(void)
  {
//---
   if(Environment.LastPositionId()==0)
     {
      if(Environment.GetPositionsTotal()>0)
         Environment.Refresh();
      if(Environment.LastPositionId()==0)
         return false;
     }
   if(!TransactionSelect(Environment.LastPositionId(),SELECT_BY_TICKET,MODE_TRADES))
     {
      printf("Last position #"+(string)Environment.LastPositionId()+" not selected.");
      return false;
     }
   int pips=(int)MathRound(HedgePositionGetDouble(HEDGE_POSITION_PROFIT_POINTS)/Point());
   int needPips=GetDynamicPips();
   if(pips<0)
     {
      if(MathAbs(pips)>=GetDynamicPips())
         return true;
     }
   return false;
//---
  }
//+------------------------------------------------------------------+
//| Calculates the dynamic level of loss the last position in        |
//| pips above which opens a new Srednyaya                           |
//| position.                                                        |
//| RESULT                                                           |
//| The dynamic level loss position in points.                       | 
//+------------------------------------------------------------------+
int GetDynamicPips(void)
  {
//---
   double high= Indicators.iHighest(Symbol(),PERIOD_CURRENT,Glubina,1);
   double low = Indicators.iLowest(Symbol(),PERIOD_CURRENT,Glubina,1);
   double pips = MathRound((high - low)/DEL/Point());
   int PipStep = (int)pips;
   if(pips < DefaultPips/DEL)PipStep = (int)MathRound(DefaultPips/DEL);
   if(pips > DefaultPips*DEL)PipStep = (int)MathRound(DefaultPips*DEL);
   return PipStep;
//---
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Opens averaging a loss position in the direction of the open     |
//| positions.                                                       |
//| RESULT                                                           |
//| True if the position was opened, and false otherwise.            |
//+------------------------------------------------------------------+
bool OpenAveragingLost()
  {
//---
   string comment=EAName+"-"+(string)(Environment.GetPositionsTotal()+1)+"-"+(string)GetDynamicPips();
   bool res_op=false;
   State.RememberDeals();
   if(Environment.CurrentDirection()==DIRECTION_LONG)
      res_op=Trade.Buy(GetLot(),Symbol(),0.0,0.0,0.0,comment);
   else if(Environment.CurrentDirection()==DIRECTION_SHORT)
      res_op=Trade.Sell(GetLot(),Symbol(),0.0,0.0,0.0,comment);
   else return false;
   if(!res_op)
     {
      printf("OrderSend Error:"+Trade.ResultRetcodeDescription());
      return false;
     }
   else if(!State.ChangedState())
     {
      printf("Error! State not changed.");
      return false;
     }
   if(TransactionSelect(TransactionsTotal()-1))
      Draw.DrawEntryPrice();
   return true;
//---
  }
//+------------------------------------------------------------------+
//| Opens the first position depending on the RSI and                |
//| direction of last bars. Returns true if was                      |
//| open position.                                                   |
//| RESULT                                                           |
//| True if the position was opened, and false otherwise.            |
//+------------------------------------------------------------------+
bool OpenFirstPositionByRSI()
  {
//--- 
   if(Environment.CurrentDirection() != DIRECTION_UNDEFINED)return false;
   double rsi= Indicators.iRSI(Symbol(),PERIOD_CURRENT,RsiPeriod,PRICE_CLOSE,1);
   bool isUp = Indicators.iClose(Symbol(),PERIOD_CURRENT,1)>
              Indicators.iClose(Symbol(),PERIOD_CURRENT,2);
   bool isDown= !isUp;
   bool isBuy = rsi>RsiMinimum && isUp;
   bool isSell = rsi < RsiMaximum && isDown;
   bool res_op = false;
   string comment=EAName+"-"+(string)(Environment.GetPositionsTotal()+1)+"-"+(string)GetDynamicPips();
   State.RememberDeals();
   if(isBuy)
      res_op=Trade.Buy(Lots,Symbol(),0.0,0.0,0.0,comment);
   else if(isSell)
      res_op=Trade.Sell(Lots,Symbol(),0.0,0.0,0.0,comment);
   else return false;
   if(!res_op)
     {
      printf("Error:"+Trade.ResultRetcodeDescription());
      return false;
     }
   else if(!State.ChangedState())
     {
      printf("Trade state not changed.");
      return false;
     }
   if(TransactionSelect(TransactionsTotal()-1))
      Draw.DrawEntryPrice();
   return true;
//---
  }
//+------------------------------------------------------------------+
//| Recalculates and sets the SL/TP for all                          |
//| open positions.                                                  |
//+------------------------------------------------------------------+
void RecalcSLTP()
  {
//--- 
   Environment.Refresh();
   if(TransactionsTotal()==0)
      printf("Position not find");
   FOREACH_POSITION
     {
      if(!TransactionSelect(i))continue;
      if(!Environment.IsMainPosition())continue;
      IF_FROZEN continue;
      double curr_sl = HedgePositionGetDouble(HEDGE_POSITION_SL);
      double curr_tp = HedgePositionGetDouble(HEDGE_POSITION_TP);
      HedgeTradeRequest request;
      request.action = REQUEST_MODIFY_SLTP;
      bool is_modify = false;
      IF_LONG
        {
         double tp = Environment.AveragePrice() + TakeProfit * Point();
         double sl = Environment.AveragePrice() - StopLoss * Point();
         if(UseStopLoss && !Environment.DoubleEquals(curr_sl,sl) && !UseTrailingStop)
            is_modify=request.sl=sl;
         if(!Environment.DoubleEquals(curr_tp,tp))
            is_modify=request.tp=tp;
        }
      IF_SHORT
        {
         double tp = Environment.AveragePrice() - TakeProfit * Point();
         double sl = Environment.AveragePrice() + StopLoss * Point();
         if(UseStopLoss && !Environment.DoubleEquals(curr_sl,sl) && !UseTrailingStop)
            is_modify=request.sl=sl;
         if(!Environment.DoubleEquals(curr_tp,tp))
            is_modify=request.tp=tp;
        }
      if(is_modify && !SendTradeRequest(request))
        {
         printf("Failed modify SL/TP. Reason: "+Trade.ResultRetcodeDescription());
         return;
        }
     }
//---
  }
//+------------------------------------------------------------------+
//| Function trawls protective stop divergent positions              |
//| after the price.                                                 |
//| INPUT PARAMETERS                                                 |
//| needProfit - Level srednevzveshanny profit of all positions      |
//| in points, below which the trawl is not done;                    |
//| stop Level in points between the current price and the StopLoss, |
//| which must be supported.                                         |
//| AvgPrice - Weighted average price of all positions.              |
//+------------------------------------------------------------------+
void TrailingAlls(double needProfit,double stop,double AvgPrice)
  {
//---
   double profit=0; // the Total profit in points of all open positions.
   double currStopLoss=0.0; // Current stop loss.
   double stopLevel=0.0; // the New stop loss level.
   double point=SymbolInfoDouble(Symbol(),SYMBOL_POINT);
   double ask = SymbolInfoDouble(Symbol(), SYMBOL_ASK);
   double bid = SymbolInfoDouble(Symbol(), SYMBOL_BID);
   if(Environment.DoubleEquals(stop, 0.0))return;
   FOREACH_POSITION
     {
      // Block the choice of our own positions.
      if(!TransactionSelect(i))continue;
      if(!Environment.IsMainPosition())continue;
      IF_FROZEN continue;
      // Trailing for long positions.
      IF_LONG
        {
         //Not Talim, if the total profit of all positions less than 10 (pType) points.
         profit=(int)MathRound((bid-AvgPrice)/point);
         if(profit<needProfit)continue;
         //Tralin stop after the price is at a distance of 100(stop) points.
         currStopLoss=HedgePositionGetDouble(HEDGE_POSITION_SL);
         stopLevel=bid-stop*point;
         bool stopNull=Environment.DoubleEquals(currStopLoss,0.0);
         if(stopNull || (!stopNull && stopLevel>currStopLoss))
            ModifyStop(stopLevel);
        }
      // The trawl for short positions.
      IF_SHORT
        {
         profit=(int)MathRound((AvgPrice-ask)/point);
         if(profit<needProfit)continue;
         currStopLoss=HedgePositionGetDouble(HEDGE_POSITION_SL);
         stopLevel=ask+stop*point;
         bool stopNull=Environment.DoubleEquals(currStopLoss,0.0);
         if(stopNull || (!stopNull && stopLevel>currStopLoss))
            ModifyStop(stopLevel);
        }
     }
//---
  }
//+------------------------------------------------------------------+
//| Modifies the level StopLoss'and the current selected position.   |
//| Position should be pre-selected.                                 |
//| INPUT PARAMETERS                                                 |
//| newStopLevel - New price level for stop-loss.                    |
//+------------------------------------------------------------------+
void ModifyStop(double newStopLevel)
  {
//---
   HedgeTradeRequest request;
   request.action=REQUEST_MODIFY_SLTP;
   request.sl = newStopLevel;
   request.tp = HedgePositionGetDouble(HEDGE_POSITION_TP);
   int total=TransactionsTotal();
   if(!SendTradeRequest(request))
      printf("Modify SL failed. Reason: "+(string)GetHedgeError());
//---
  }
//+------------------------------------------------------------------+
//| Checks the level Drop of the CCI indicator and out of the existing |
//| position, if this level is exceeded the size of the 'Drop'       |
//+------------------------------------------------------------------+ 
void CloseByCCI()
  {
//---
   bool closeAll=false;
   double cci=Indicators.iCCI(Symbol(),PERIOD_CURRENT,CCIPeriod,PRICE_CLOSE,1);
   ENUM_DIRECTION_TYPE dir = Environment.CurrentDirection();
   if(cci> CCILevel && dir == DIRECTION_SHORT)
      closeAll=true;
   else if(cci<(-CCILevel) && dir==DIRECTION_LONG)
      closeAll=true;
   if(closeAll)
     {
      printf("Closed All due to CCI");
      CloseAllPositions("Exit by CCI ("+DoubleToString(cci,1)+")");
     }

//---
  }
//+------------------------------------------------------------------+
//| Out of all available positions.                                  |
//+------------------------------------------------------------------+
void CloseAllPositions(string comment="")
  {
//--- 
   int hist_total=TransactionsTotal(MODE_HISTORY);
   string s="CLOSE ALL ";
   FOREACH_POSITION
     {
      if(!TransactionSelect(i))continue;
      if(!Environment.IsMainPosition())continue;
      IF_FROZEN continue;
      HedgeTradeRequest request;
      request.action=REQUEST_CLOSE_POSITION;
      request.exit_comment=comment;
      if(!SendTradeRequest(request))
        {
         ENUM_HEDGE_ERR err=GetHedgeError();
         printf("close Failed. Reason: "+EnumToString(err));
        }
     }
   if(TransactionsTotal(MODE_HISTORY)!=hist_total)
     {
      if(!TransactionSelect(TransactionsTotal(MODE_HISTORY),SELECT_BY_POS,MODE_HISTORY))
         return;
      Draw.DrawExitPrice();
     }
//---
  }
//+------------------------------------------------------------------+
//| Closes all positions, if the retention time of the first of them |
//| exceeds MaxTradeOpenHours.                                       |
//+------------------------------------------------------------------+
void CloseByTime()
  {
//---
   if(Environment.FirstPosOpen() == 0)return;
   double bars[];
   int hours = CopyClose(Symbol(), PERIOD_H1, Environment.FirstPosOpen(), TimeCurrent(), bars);
   if(hours >= MaxTradeOpenHours)
     {
      printf("Close all position by time limit.");
      CloseAllPositions("Close by Time ("+(string)hours+"h)");
     }
//---
  }
//+------------------------------------------------------------------+
//| Closes all positions, if the total loss exceeds 20%              |
//| (set variable TotalEquityRisk) maximum achieved                  |
//| the profit positions.                                            |
//+------------------------------------------------------------------+
void CloseByEquity()
  {
//--- 
   double profit=Environment.CurrentProfit();
   double hequity=Environment.GetHighEquity();
   double perDeposit=AccountInfoDouble(ACCOUNT_EQUITY)/100.0*EquityPercent;
   if(profit<0.0 && hequity>perDeposit && MathAbs(profit)>TotalEquityRisk/100.0*hequity)
     {
      string comment="Profit"+(string)profit+" - "+(string)Environment.GetHighEquity();
      printf("Close all by equity stop");
      CloseAllPositions("Close by Equity stop");
     }
//---
  }
//+------------------------------------------------------------------+
//| Returns a lot that you need to open the next position.           |
//+------------------------------------------------------------------+
double GetLot()
  {
//---
   return NormalizeDouble(Lots * MathPow(LotExponent, Environment.GetPositionsTotal()+1), lotdecimal);
//---
  }
//+------------------------------------------------------------------+
//| Displays the SL/TP on the chart.                                 |
//+------------------------------------------------------------------+
void DrawSLTP()
  {
   FOREACH_POSITION
     {
      if(!TransactionSelect(i))continue;
      if(!Environment.IsMainPosition())continue;
      Draw.DrawSLTP();
      // Because the TP/SL Levels for all positions in this strategy are the same, we leave
      // at the first rendering of any of the levels.
      break;
     }
  }
//+------------------------------------------------------------------+
//| Renders a new position on the chart.                             |
//+------------------------------------------------------------------+
void DrawHistoryPos()
{
   for(;hist_trans < TransactionsTotal(MODE_HISTORY); hist_trans++)
   {
      if(!TransactionSelect(hist_trans, SELECT_BY_POS, MODE_HISTORY))continue;
      if(!Environment.IsMainPosition())continue;
      Draw.DrawExitPrice();
      Draw.DrawHistoryLine();
   }
}

//+------------------------------------------------------------------+
//| Torbay used indicators on the chart.                             |
//+------------------------------------------------------------------+
void DrawIndicators()
{
//---
   int handle = iRSI(Symbol(), PERIOD_CURRENT, RsiPeriod, PRICE_CLOSE);
   if(handle != INVALID_HANDLE)
   {
      int total = (int)ChartGetInteger(ChartID(), CHART_WINDOWS_TOTAL);
      if(!ChartIndicatorAdd(ChartID(), total, handle))
         printf("Failed create RSI. Reason: " + (string)GetLastError());
   }
   if(UseCCIStop)
   {
      int h_cci = iCCI(Symbol(), PERIOD_CURRENT, CCIPeriod, PRICE_CLOSE);
      int total = (int)ChartGetInteger(ChartID(), CHART_WINDOWS_TOTAL);
      if(!ChartIndicatorAdd(ChartID(), total, h_cci))
         printf("Failed create CCI. Reason: " + (string)GetLastError());
   }
   
//---
}
//+------------------------------------------------------------------+
