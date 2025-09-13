//+------------------------------------------------------------------+
//|                                            DonchianScalperEA.mq4 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "2023, AlFa7961"
#property link      "https://www.mql5.com"
#property version   "2.02"
#property strict

#include  <Indicators.mqh>
#include  <OrderManagementFunctions.mqh>

#define VER "2.02"
#define EA_NAME "DonchianScalperEA"

//+------------------------------------------------------------------+
//| Expert inputs                                                    |
//+------------------------------------------------------------------+
input int MAGICMA                      = 3937;          // MAGICMA: Magic Number to recognize the chart orders
input int Slippage                     = 2;             // Slippage: Slippage Points admitted
input LotSizeCalcMode LotsCalculation  = ByFixedSize;   // LotsCalculation: How to compute the Lot size per trade
input double Lots                      = 0.01;          // Lots: Fixed Lots to assign to each order
input double RiskPerc                  = 1;             // RiskPerc: The percentage of the Balance to Risk if LotsCalculation != ByFixedSize
input double CrossAnchor               = 0;             // CrossAnchor: # of Points the price retrace from opposite level before open a new Order
input double StopLossPoints            = 80;            // StopLossPoints: # of Points from the opposite level to set the Stop Loss to (0 == level)
input double TakeProfitPoints          = 380;           // TakeProfitPoints: Fixed Take Profit Points if the order is filled and goes well
input TakeProfitType TargetTPMode      = CloseAtProfit; // TargetTPMode: Whether close as soon as the TP is reached or try to trail the StopLoss
input TrailingProfitType TrailingProfitMode = ByATR;    // TrailingProfitMode: Strategy to use for the trailing StopLoss if TargetTPMode != CloseAtProfit
input bool UseExternalIndicator        = true;          // UseExternalIndicator: Whether use external indicator or internal code for the D.Channels
input int Periods                      = 20;            // Periods: How many bars Donchian Channels should look back
/*input*/ int Extremes                 = 1;
/*input*/ int Margins                  = 1;
/*input*/ int Advance                  = 0;
/*input*/ int MaxBars                  = 100;
/*input*/ bool RunAtOpenBars           = true;
/*input*/ bool AllOrders               = false;

//+------------------------------------------------------------------+
//| Expert global variables                                          |
//+------------------------------------------------------------------+
datetime LastBarTime;
int max_bars;

//---- buffers
double ExtMapBuffer1[];
double ExtMapBuffer2[];
double ExtMapBuffer3[];

// Define a global variable to store the timer ID
int timerHandle;
// Define a global variable to store the last time the custom function was called
datetime lastFunctionCallTime = 0;
int functionIntervalSeconds = 300; // 5 minutes interval (adjust as needed)
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
// Set up a timer to call the custom function every 5 minutes (300 seconds)
   timerHandle = EventSetTimer(functionIntervalSeconds);
//---
   if(UseExternalIndicator)
     {
      max_bars = MaxBars;
      // Attach the "Donchian Channels" indicator
      double donchianChHandle = iCustom(NULL, PERIOD_CURRENT, "Donchian Channels.ex4", Periods, Extremes, Margins, Advance, max_bars, 0, 0);
      if(donchianChHandle == INVALID_HANDLE)
        {
         Print("Failed to attach indicator!");
         return(INIT_FAILED);
        }
     }
   else
     {
      max_bars = MaxBars;
     }
//---
   ArrayResize(ExtMapBuffer1, max_bars);
   ArrayResize(ExtMapBuffer2, max_bars);
   ArrayResize(ExtMapBuffer3, max_bars);

   ArrayFill(ExtMapBuffer1, 0, max_bars, 0);
   ArrayFill(ExtMapBuffer2, 0, max_bars, 0);
   ArrayFill(ExtMapBuffer3, 0, max_bars, 0);
//---
   LastBarTime = TimeCurrent();
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Timer function to be called every 5 minutes                      |
//+------------------------------------------------------------------+
void OnTimer()
  {
// Call your custom function here that you want to execute every 5 minutes
   if(!IsTesting() && !RunAtOpenBars)
     {
      ExpertLogic("TIMER@TimerEvent");
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
// Check if a new bar has opened
   datetime currentBarTime = Time[0]; // Get the opening time of the current bar
   if(currentBarTime != LastBarTime)
     {
      LastBarTime = currentBarTime; // Update the last bar's opening time

      if(RunAtOpenBars)
        {
         ExpertLogic("TICK@OpenBars");
        }
     }

// Check if enough time has passed since the last function call
   datetime currentTime = TimeCurrent();
   if(currentTime - lastFunctionCallTime >= functionIntervalSeconds)
     {
      // Call your custom function here that you want to execute every 5 minutes
      if(IsTesting() && !RunAtOpenBars)
        {
         ExpertLogic("TIMER@TimerEvent");
        }

      // Check for profits
      if(TargetTPMode == CloseAtProfit && TakeProfitPoints > 0)
        {
         CheckForClose();
        }
      // Trail for profits
      else
        {
         switch(TrailingProfitMode)
           {
            case Instrumented:
               InstrumentedTrailingStop(MAGICMA, AllOrders, 40, 40);
               break;
            case ByMA:
               TrailingByMA(MAGICMA, AllOrders, PERIOD_CURRENT, Periods);
               break;
            case ByATR:
               TrailingByATR(MAGICMA, AllOrders, PERIOD_CURRENT, 5, 1, 36, 1, 1, false);
               break;
            case ByFractals:
               TrailingByFractals(MAGICMA, AllOrders, PERIOD_CURRENT, 5, 3, false);
               break;
           }
        }

      // Update the last function call time
      lastFunctionCallTime = currentTime;
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ExpertLogic(string callerMessage = "")
  {

   if(StringLen(callerMessage) > 0)
      Print(callerMessage);

// Calculate EMAs
   double EMA = NormPrice(iMA(NULL,PERIOD_CURRENT,Periods,0,MODE_EMA,PRICE_CLOSE,0));

// Calculate Donchian Channels
   if(UseExternalIndicator)
     {
      for(int i=0; i<=Periods; i++)
        {
         ExtMapBuffer1[i] = NormPrice(iCustom(NULL, PERIOD_CURRENT, "Donchian Channels.ex4", Periods, Extremes, Margins, Advance, max_bars, 0, i));
         ExtMapBuffer2[i] = NormPrice(iCustom(NULL, PERIOD_CURRENT, "Donchian Channels.ex4", Periods, Extremes, Margins, Advance, max_bars, 1, i));
         ExtMapBuffer3[i] = ExtMapBuffer1[i]+(ExtMapBuffer2[i] - ExtMapBuffer1[i])/2;
        }
     }
   else
     {
      DonchianChannels(PERIOD_CURRENT,ExtMapBuffer2,ExtMapBuffer1,ExtMapBuffer3,Periods,Extremes,Margins,Advance,max_bars);
     }

// Check if BUYSTOP order can be opened
   Count(max_bars, MAGICMA);
   double stopLoss = ExtMapBuffer1[0];
   if(StopLossPoints > 0)
      stopLoss = ExtMapBuffer1[0] + StopLossPoints * Point;

// Calculate the LotSize
   double lotSize = Lots;
   switch(LotsCalculation)
   {
      case ByFixedSize:
         lotSize = Lots;
         break;
      case ByAccountPercentageToRisk:
         lotSize = CalcLotByAccountPercentageToRisk(RiskPerc);
         break;
      case ByAmountToRisk:
         lotSize = CalcLotByAmountToRisk(RiskPerc, Lots);
         break;
      case ByPercentageToRisk:
         lotSize = CalcLotByPercentageToRisk(Lots, RiskPerc);
         break;
      case ByPerecentageToRiskAndStopLoss:
         lotSize = CalcLotByPerecentageToRiskAndStopLoss(RiskPerc, NormPrice(MathAbs(ExtMapBuffer2[0] - stopLoss)/Point));
         break;
      default:
         lotSize = Lots;
   }

   if(Buys == 0 && PendingBuys == 0)
     {
      if(GetBarIndexLastOrderClosed(MAGICMA) < 0 || GetBarIndexLastOrderClosed(MAGICMA) >= 3)
         for(int _bi=1; _bi<3; _bi++)
           {
            if(CrossesEMAFromAbove(ExtMapBuffer3[_bi] - CrossAnchor * Point, _bi) || CrossesEMAFromAbove(ExtMapBuffer1[_bi] - CrossAnchor * Point, _bi))
              {
               if(OpenOrder(lotSize,OP_BUYSTOP,ExtMapBuffer2[0],stopLoss,0.0,false,Slippage,EA_NAME+VER,MAGICMA,AllOrders))
                  break;
              }
           }
     }
   else
     {
      if(PendingBuys == 1 && PendingBuyTickets[0] >= 0 && PendingBuyOpenPrices[0] > 0 && PendingBuyOpenPrices[0] != ExtMapBuffer2[0])
        {
         if(ExtMapBuffer2[0] == ExtMapBuffer2[1] && ExtMapBuffer2[1] == ExtMapBuffer2[2])
           {
            if(OrderSelect(PendingBuyTickets[0],SELECT_BY_TICKET,MODE_TRADES))
              {
               Print("Modifying BUYSTOP Order :",PendingBuyOpenPrices[0]," --> ",ExtMapBuffer2[0]," - SL: ",stopLoss);
               // ModifyPendingOrder(PendingBuyTickets[0],ExtMapBuffer2[0],stopLoss);
               if(OrderDelete(OrderTicket(),clrBlue))
                  OpenOrder(lotSize,OP_BUYSTOP,ExtMapBuffer2[0],stopLoss,0.0,false,Slippage,EA_NAME+VER,MAGICMA,AllOrders);
              }
           }
        }
     }

// Check if SELLSTOP order can be opened
   Count(max_bars, MAGICMA);
   stopLoss = ExtMapBuffer2[0];
   if(StopLossPoints > 0)
      stopLoss = ExtMapBuffer2[0] - StopLossPoints * Point;

// Calculate the LotSize
   lotSize = Lots;
   switch(LotsCalculation)
   {
      case ByFixedSize:
         lotSize = Lots;
         break;
      case ByAccountPercentageToRisk:
         lotSize = CalcLotByAccountPercentageToRisk(RiskPerc);
         break;
      case ByAmountToRisk:
         lotSize = CalcLotByAmountToRisk(RiskPerc, Lots);
         break;
      case ByPercentageToRisk:
         lotSize = CalcLotByPercentageToRisk(Lots, RiskPerc);
         break;
      case ByPerecentageToRiskAndStopLoss:
         lotSize = CalcLotByPerecentageToRiskAndStopLoss(RiskPerc, NormPrice(MathAbs(stopLoss - ExtMapBuffer1[0])/Point));
         break;
      default:
         lotSize = Lots;
   }

   if(Sells == 0 && PendingSells == 0)
     {
      if(GetBarIndexLastOrderClosed(MAGICMA) < 0 || GetBarIndexLastOrderClosed(MAGICMA) >= 3)
         for(int _bi=1; _bi<3; _bi++)
           {
            if(CrossesEMAFromBelow(ExtMapBuffer3[_bi] + CrossAnchor * Point, _bi) || CrossesEMAFromBelow(ExtMapBuffer2[_bi] + CrossAnchor * Point, _bi))
              {
               if(OpenOrder(lotSize,OP_SELLSTOP,ExtMapBuffer1[0],stopLoss,0.0,false,Slippage,EA_NAME+VER,MAGICMA,AllOrders))
                  break;
              }
           }
     }
   else
     {
      if(PendingSells == 1 && PendingSellTickets[0] >= 0 && PendingSellOpenPrices[0] > 0 && PendingSellOpenPrices[0] != ExtMapBuffer1[0])
        {
         if(ExtMapBuffer1[0] == ExtMapBuffer1[1] && ExtMapBuffer1[1] == ExtMapBuffer1[2])
           {
            if(OrderSelect(PendingSellTickets[0],SELECT_BY_TICKET,MODE_TRADES))
              {
               Print("Modifying SELLSTOP Order :",PendingSellOpenPrices[0]," --> ",ExtMapBuffer1[0]," - SL: ",stopLoss);
               // ModifyPendingOrder(PendingSellTickets[0],ExtMapBuffer1[0],stopLoss);
               if(OrderDelete(OrderTicket(),clrRed))
                  OpenOrder(lotSize,OP_SELLSTOP,ExtMapBuffer1[0],stopLoss,0.0,false,Slippage,EA_NAME+VER,MAGICMA,AllOrders);
              }
           }
        }
     }

  }
//+------------------------------------------------------------------+
void CheckForClose()
  {
   RefreshRates();
   for(int i=OrdersTotal(); i>=0; i--)
     {

      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         continue;
        }

      if(OrderSymbol()==Symbol())
        {
         if(OrderMagicNumber()!=MAGICMA)
           {
            continue;
           }

         if(OrderType()==OP_BUY)
           {
            // Close at Profit
            if(TakeProfitPoints > 0 && (Bid + MarketInfo(Symbol(), MODE_SPREAD) * Point) - OrderOpenPrice() > TakeProfitPoints * Point)
              {
               if(OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,clrBlue))
                  continue;
              }
           }

         if(OrderType()==OP_SELL)
           {
            // Close at Profit
            if(TakeProfitPoints > 0 && OrderOpenPrice() - (Ask - MarketInfo(Symbol(), MODE_SPREAD) * Point) > TakeProfitPoints * Point)
              {
               if(OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,clrRed))
                  continue;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
