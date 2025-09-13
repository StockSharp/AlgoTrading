//+------------------------------------------------------------------+
//|                                                Lloid Grid EA.mq4 |
//|                                   Copyright 2019, ea-builder.net |
//|                                        http://www.ea-builder.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2019, ea-builder.net"
#property link      "http://www.ea-builder.net"
#property version   "1.00"
#property strict


enum enum_VolumeType {Fixed_Size,Risk_Percentage,Risked_Amount,Used_Margin_Percentage,Kelly_Criterion};
enum enum_SLType {Fixed_Pips, ATR_Multiplier, Last_Swing_HighLow};
enum enum_TPType {TP_Fixed_Pips, RR_Ratio};

string Pairs="";
string Timeframes="";
string EAName="Ichimoku Price Action Strategy v1.0";
input string _Settings3=" --- STRATEGY PARAMETERS --- "; //.
input bool BuyMode=true; // Buy Mode
input bool SellMode=true; // Sell Mode


input ENUM_TIMEFRAMES TradingTF=PERIOD_H1; // Trading TF
input string BBIndiName="bb"; // BB Indi Name
extern int    Length=20;      // Bollinger Bands Period
extern int    Deviation=2;    // Deviation
extern double x=0;    // Tune
extern double MoneyRisk=1.00; // Offset Factor

input bool UseMACDFilter=true; // Use MACD Filter
input int MACDFastEMA=12; // MACD Fast EMA Period
input int MACDSlowEMA=26; // MACD Slow EMA Period
input int MACDSignalEMA=9; // MACD Signal Period


input bool WaitForCandleClose=false; // Wait For Candle Close Confirmation
input string ___Settings="";//.
input string __Settings2=" --- TRADE MANAGEMENT --- "; //.
int MultipleEntriesAtTheSameTime=1; // Multiple Entries At The Same Time
double DivideRiskBetweenEntries=true; // Split Risk Between Entries
input string StartTime="00:00"; // Start Time
input string EndTime="00:00";  // End Time
input bool CloseOrdersAtEndOfBar=false; // Close Positions At End Of Bar
input bool CloseOnReverse=true; // Close Positions On Opposite Signal
input enum_TPType TPType=TP_Fixed_Pips; // TP Type
string TakeProfits="20, 40"; // TP Pips (multiple orders)
string TakeProfitRatios="0.5, 1"; // TP Ratios (multiple orders)
input enum_SLType SLType=Fixed_Pips; // SL Type
input  double TakeProfit=20; // TP Pips
input  double TakeProfitRatio=2; // TP RR Ratio
input double StopLoss=50; // SL Pips  (0=Disabled)
input int BarsForSwingSL=10; // Bars For H/L Swing SL
input ENUM_TIMEFRAMES TFForSwingSL=PERIOD_M5; // TF For Swing SL
input double StopATRMultiplier=1; // SL ATR Multiplier
input ENUM_TIMEFRAMES ATRTFSL =PERIOD_M15; // SL ATR TF
input int ATRPeriod=28; // ATR Period
bool OpenAdditionalEntry=false; // Open Additional Entry
double TakeProfit2=20; // TP Pips 2
double TakeProfitRatio2=0.5; // TP Ratio 2
input double MoveToBreakEven=0; // Move To Break-Even Pips (0=Disabled)
input double TrailingStop=50; // Trailing-Stop Pips (0=Disabled)
input double TrailingStopTrigger=25; // Trailing-Stop Trigger Pips
input double TrailingStopStep=5; // Trailing-Stop Step
input double PartialTPPips=0; // Partial Close TP Pips (0=Disabled)
input double PartialCloseLotPercentage=50; // Partial Close Lot Percentage
input bool StealthSLandTP=false; // Stealth SL and TP
input int MaxOpenPositions=1; // Max Open Positions (per Symbol/EA)
int MaxSignalsPerBar=1; // Max Signals Per Bar
int MaxTradesPerBar=0; // Max Trades Per Bar
input string __Settings="";//.
input string _Settings=" --- MONEY MANAGEMENT --- "; //.
input enum_VolumeType VolumeType=Risk_Percentage; // Lot Calculation Method
input double FixedLotSize=0.1; //Fixed Lot-Size Value
input double RiskPercentage=1; // Risk Percentage Value
input double RiskedAmount=100; // USD Risked Amount Value
input double MarginPercentage=2; // Margin Percentage Value
input double LotMultiplierOnLoss=1; //  Lot Multiplier On Loss
input double LotAdderOnLoss=0.0; //  Lot Adder On Loss
ENUM_DAY_OF_WEEK StartDay=MONDAY;  // Start Day
ENUM_DAY_OF_WEEK EndDay=FRIDAY;  // End Day
input string _Settings6="";//.
input string _Settings7=" --- MARKET FILTERS --- ";//.
input double Slippage=3; // Max Slippage (0=Disabled)
input double MaxSpread=5; // Max Spread (0=Disabled)
input string _Settings8="";//.
input string _Settings9=" --- CUSTOMIZATION --- ";//.
input bool SaveTradeScreenshots=true; // Save Screenshots With Trades
input string TradeComment=""; // Trade Comment
input bool SoundAlert=false; // Sound Alert
input string SoundFile="alert2.wav"; // Sound File
input bool PopupAlert=false; // Pop-up Alefrt
input bool EmailAlert=true; // Email Alert
input bool PhoneAlert=false; // Phone Alert
input bool ShowDashBoard=true; // Show Dashboard
input ENUM_BASE_CORNER DashBoardCorner=CORNER_LEFT_UPPER; // DashBoard Corner
input color DashBoardBGColor=clrMidnightBlue; // Dashboard BackGround Color
input color DashBoardColor=clrGold; // Dashboard Text Color
color BearishColor=clrTomato; // Bearish Color
color BullishColor=clrLimeGreen; // Bullish Color
input int MagicNumber=321123; // Magic Number
bool ECN=true;

bool AskForManualConfirmation=false; // Ask Manual Confirmation Before Opening Trades
int minTradesForKellyCriterion=20;
bool VirtualPendingOrders=false;
string DashKeys[8];
string DashValues[8];
string DashCount;
int dashRows=1;
datetime lastTradeHour=0;
string pairs[];
string s_timeframes[];
int timeframes[];
double takeProfits[];
double takeProfitRatios[];
string s_takeProfits[];
string s_takeProfitRatios[];
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   GlobalVariableSet("PClosedLastTime"+Symbol()+timeFrameToString(Period()),0);

   _MaxSpread=MaxSpread;
   _MaxSlippage=Slippage;
   _MaxTraderPerBar=MaxTradesPerBar;
   _ECN=ECN;
   _StealthPendingOrders=VirtualPendingOrders;
   _StealthSLandTP=StealthSLandTP && !IsOptimization();
   _AskForManualConfirmation=AskForManualConfirmation;
   TransformTime(StartTime,StartHour,StartMinute);
   TransformTime(EndTime,EndHour,EndMinute);
   InitAlertTypes(SoundAlert,SoundFile,PopupAlert,EmailAlert,PhoneAlert);

   if(IsTradeAllowed()==false)
      Alert("Please enable the setting \"Allow live trading\" under the Common tab in the Expert Properties (press F7)");
   if(IsExpertEnabled()==false)
      Alert("Expert advisors are disabled! Please enable the Expert Advisors button on the toolbar.");

   ChartSetInteger(0,CHART_SHOW_GRID,false);
   ChartSetInteger(0,CHART_MODE,1);

   if(WaitForCandleClose)
      masterCandleIdx=1;
   dashRows=ArraySize(DashKeys);

   double aux1,aux2;
   if(VolumeType==Kelly_Criterion && GetKellyCriterionFactors(Symbol(),MagicNumber,aux1,aux2)<minTradesForKellyCriterion)
     {
      Alert("Not enough trades in history to process the Kelly Criterion Risk Ratio (at least "+(string)minTradesForKellyCriterion+" trades necessary). The EA will use the Risk Percentage method until there are enough trades to make the proper calculations.");
     }

   if(StringLen(Pairs)>0)
     {
      StringSplit(Pairs,',',pairs);
      for(int i=0; i<ArraySize(pairs); i++)
        {
         pairs[i]=StringTrimLeft(StringTrimRight(pairs[i]));
        }
     }

   if(StringLen(Timeframes)>0)
     {
      StringSplit(Timeframes,',',s_timeframes);
      ArrayResize(timeframes,ArraySize(s_timeframes));
      for(int i=0; i<ArraySize(s_timeframes); i++)
        {
         s_timeframes[i]=StringTrimLeft(StringTrimRight(s_timeframes[i]));
         timeframes[i]=stringToTimeFrame(s_timeframes[i]);
        }
     }
   if(StringLen(TakeProfits)>0)
     {
      StringSplit(TakeProfits,',',s_takeProfits);
      ArrayResize(takeProfits,ArraySize(s_takeProfits));
      for(int i=0; i<ArraySize(s_takeProfits); i++)
        {
         s_takeProfits[i]=StringTrimLeft(StringTrimRight(s_takeProfits[i]));
         takeProfits[i]=stringToTimeFrame(s_takeProfits[i]);
        }
     }
   if(StringLen(TakeProfitRatios)>0)
     {
      StringSplit(TakeProfitRatios,',',s_takeProfitRatios);
      ArrayResize(takeProfitRatios,ArraySize(s_takeProfitRatios));
      for(int i=0; i<ArraySize(s_takeProfitRatios); i++)
        {
         s_takeProfitRatios[i]=StringTrimLeft(StringTrimRight(s_takeProfitRatios[i]));
         takeProfitRatios[i]=stringToTimeFrame(s_takeProfitRatios[i]);
        }
     }
   takeProfitRatios[0]=TakeProfitRatio;
   takeProfits[0]=TakeProfit;
   if(OpenAdditionalEntry)
     {
      MultipleEntriesAtTheSameTime=2;
      ArrayResize(takeProfitRatios,2);
      ArrayResize(takeProfits,2);
      takeProfitRatios[0]=TakeProfitRatio;
      takeProfitRatios[1]=TakeProfitRatio2;
      takeProfits[0]=TakeProfit;
      takeProfits[1]=TakeProfit2;
     }
   else
     {
      MultipleEntriesAtTheSameTime=1;
     }
   /* ChartSetInteger(0,CHART_COLOR_BACKGROUND,clrBlack);
    ChartSetInteger(0,CHART_COLOR_FOREGROUND,clrWhite);
    ChartSetInteger(0,CHART_COLOR_GRID,clrBlack);
    ChartSetInteger(0,CHART_COLOR_CHART_UP,clrAqua);
    ChartSetInteger(0,CHART_COLOR_CHART_DOWN,clrFireBrick);
    ChartSetInteger(0,CHART_COLOR_CANDLE_BULL,clrAqua);
    ChartSetInteger(0,CHART_COLOR_CANDLE_BEAR,clrTomato);
    ChartSetInteger(0,CHART_COLOR_GRID,clrYellow);
    ChartSetInteger(0,CHART_COLOR_VOLUME,clrForestGreen);
    ChartSetInteger(0,CHART_COLOR_ASK,clrRed);
    ChartSetInteger(0,CHART_COLOR_BID,clrRed);
    ChartSetInteger(0,CHART_SCALE,3);*/

   CheckOrders();

   MaxTradesPerBar=MultipleEntriesAtTheSameTime;
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int lastdeinitReason=-1;
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   if(reason!=3)
     {
      ObjectsDeleteAll(0,0,OBJ_BUTTON);
      RemoveDashboard();
     }
   lastdeinitReason=reason;
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   CheckOrders();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double atr(int bufferIdx)
  {
   double ma=iATR(Symbol(),ATRTFSL,ATRPeriod,masterCandleIdx);

   return ma;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckOrders()
  {
   bool canBuy=BuyMode;
   bool canSell=SellMode;
   int tf=Period();
   string pair=Symbol();



   double atr=atr(0);
   double pip=GetPip();

   static int signalsThisBar=0;

   if(IsNewCandle())
     {
      signalsThisBar=0;
     }

   int openCloseConditionIdx=4;
   DashKeys[openCloseConditionIdx-1]="Mode";
   DashValues[openCloseConditionIdx-1]="n/a";
   if(BuyMode && SellMode)
      DashValues[openCloseConditionIdx-1]="BUY and SELL";
   else
      if(BuyMode)
         DashValues[openCloseConditionIdx-1]="BUY";
      else
         if(SellMode)
            DashValues[openCloseConditionIdx-1]="SELL";


   int signalConditions=3;
   DashKeys[openCloseConditionIdx+1-1]="Time";
   DashValues[openCloseConditionIdx+1-1]=TimeToStr(TimeLocal());

   DashKeys[openCloseConditionIdx+2-1]="MACD Condition";
   DashValues[openCloseConditionIdx+2-1]="not used";
   if(UseMACDFilter)
     {
      double macdMain=iMACD(Symbol(),TradingTF,MACDFastEMA,MACDSlowEMA,MACDSignalEMA,0,MODE_MAIN,masterCandleIdx);
      double macdSignal=iMACD(Symbol(),TradingTF,MACDFastEMA,MACDSlowEMA,MACDSignalEMA,0,MODE_SIGNAL,masterCandleIdx);

      DashValues[openCloseConditionIdx+2-1]=Direction(macdMain>macdSignal,macdSignal>macdMain,canBuy,canSell);
     }
   static bool lastClosedOrderWasLost=false;
   static double nextOrderLots=0;


   if(CloseOrdersAtEndOfBar)
      CloseOrdersOlderThan(Time[0],MagicNumber);

   if((LotMultiplierOnLoss>1 || LotAdderOnLoss>0))
     {
      if(OrderJustClosed(Symbol(),MagicNumber,-1,true))
        {
         Print("NEW ORDER CLOSED");
         if(_selectedProfit>0)
           {
            lastClosedOrderWasLost=false;
            nextOrderLots=0;
            //   DashValues[openCloseConditionIdx+1]="Win - #"+IntegerToString(_selectedTicket)+": "+DoubleToStr(_selectedProfitPips,1)+" pips at "+TimeToStr(_selectedCloseTime);
            //   DashValues[openCloseConditionIdx+2]="reset lot-size";
            Print("Last order was closed in profit, reset multiplier and adder");
           }
         else
           {
            lastClosedOrderWasLost=true;
            nextOrderLots=_selectedVolume*LotMultiplierOnLoss+LotAdderOnLoss;
            if(VolumeType==Risk_Percentage)
              {
               nextOrderLots=_selectedRiskedPercentage*LotMultiplierOnLoss+LotAdderOnLoss;
               Print("last order's risk percentage was "+DoubleToStr(_selectedRiskedPercentage)+"%");
              }
            else
               if(VolumeType==Risked_Amount)
                 {
                  nextOrderLots=_selectedRiskedAmount*LotMultiplierOnLoss+LotAdderOnLoss;
                  Print("last order's risk in USD was $"+DoubleToStr(_selectedRiskedAmount,2));
                 }
            //     DashValues[openCloseConditionIdx+2]=DoubleToStr(nextOrderLots,1);
            //   DashValues[openCloseConditionIdx+1]="Loss - #"+IntegerToString(_selectedTicket)+": "+DoubleToStr(_selectedProfitPips,1)+" pips at "+TimeToStr(_selectedCloseTime);
            Print("Last order ("+DoubleToStr(_selectedVolume,2)+" lots) was  loss, next order will be "+DoubleToStr(nextOrderLots,2));
           }
        }
     }

   int sign=0;
   if(InInterval(TimeCurrent(),StartHour,StartMinute,EndHour,EndMinute))
     {
      // if inside daily trading interval, set the signal - sign=1 means BUY, sign=-1 means SELL
      if((canBuy) && CountOpenOrders(OP_BUY,Symbol(),MagicNumber)<MaxOpenPositions)
        {
         sign=1;
         TriggerAlerts("Bullish signal for "+pair+" on "+timeFrameToString(Period()));
        if (pair==Symbol())    SetSignalArrow(1,0,233,clrLimeGreen);
         DashValues[openCloseConditionIdx+signalConditions]="Long at "+TimeToStr(TimeCurrent());
        }
      if((canSell) && CountOpenOrders(OP_SELL,Symbol(),MagicNumber)<MaxOpenPositions)
        {
         sign=-1;
         TriggerAlerts("Bearish signal for "+pair+" on "+timeFrameToString(Period()));
           if (pair==Symbol()) SetSignalArrow(-1,0,234,clrTomato);
         DashValues[openCloseConditionIdx+signalConditions]="Short at "+TimeToStr(TimeCurrent());
        }
     }

   if(CloseOnReverse && CountOpenOrders(OP_SELL,Symbol(),MagicNumber)>0 && isBullish(DashValues[openCloseConditionIdx]))
     {
      Print("Bullish signal - close SELL orders");
      CloseSellOrders(pair,MagicNumber);
     }

   if(CloseOnReverse && CountOpenOrders(OP_BUY,Symbol(),MagicNumber)>0 && isBearish(DashValues[openCloseConditionIdx]))
     {
      Print("Bearish signal - close BUY orders");
      CloseBuyOrders(pair,MagicNumber);
     }

   if(sign!=0 && signalsThisBar<MaxSignalsPerBar)
     {
      signalsThisBar++;
      // if we've got a BUY (1) or SELL (-1) signal, calculate SL/TP/volumes and enter a market order
      double sl=0;
      double tp=0;
      double entryPrice=MarketInfo(pair,MODE_ASK);
      if(sign<0)
         entryPrice=MarketInfo(pair,MODE_BID);

      if(StopLoss>0)
         sl=entryPrice+(-1)*sign*StopLoss*pip;
      if(SLType==ATR_Multiplier)
        {
         sl=entryPrice+(-1)*sign*StopATRMultiplier*atr;
        }
      if(SLType==Last_Swing_HighLow)
        {
         sl=iHigh(Symbol(),TFForSwingSL,iHighest(Symbol(),TFForSwingSL,MODE_HIGH,BarsForSwingSL,masterCandleIdx));
         if(sign>0)
            sl=iLow(Symbol(),TFForSwingSL,iLowest(Symbol(),TFForSwingSL,MODE_LOW,BarsForSwingSL,masterCandleIdx));
        }

      double slDistance=MathAbs(entryPrice-sl);

      if(TakeProfit>0)
         tp=entryPrice+sign*TakeProfit*pip;
      if(TPType==RR_Ratio)
         tp=entryPrice+sign*TakeProfitRatio*slDistance;

      bool confirmed=ManuallyConfirmed(sign);
      if(confirmed)
        {
         slDistance=GetPip()*10;
         if(sl!=0)
           {
            slDistance=entryPrice-sl;
            if(sign<0)
               slDistance=sl-entryPrice;
           }
         double volume=CalculateLotSize(pair,MagicNumber,slDistance);
         if(MultipleEntriesAtTheSameTime>1 && DivideRiskBetweenEntries)
            volume=NormLots(volume/DivideRiskBetweenEntries);

         if(nextOrderLots>0)
            volume=CalculateLotSize(Symbol(),MagicNumber,slDistance,nextOrderLots);

         if(sign>0)
           {

            if(CountOpenOrders(OP_BUY,Symbol(),MagicNumber)>=MaxOpenPositions)
               Print("Too many BUY orders on "+Symbol()+", can't open another one");
            else
              {
               for(int i=0; i<MultipleEntriesAtTheSameTime; i++)
                 {
                  if(MultipleEntriesAtTheSameTime>=1)
                    {
                     if(TPType==TP_Fixed_Pips && ArraySize(takeProfits)>=i && takeProfits[i]>0)
                        tp=entryPrice+sign*takeProfits[i]*pip;
                     if(TPType==RR_Ratio && ArraySize(takeProfitRatios)>=i && takeProfitRatios[i]>0)
                        tp=entryPrice+sign*takeProfitRatios[i]*slDistance;
                    }

                  Print("Open order number "+(string)(i+1));
                  if(OpenBuy(pair,volume,sl,tp,TradeComment,MagicNumber))
                     PrintDash(dashRows-2);
                 }
              }

           }
         else
            if(sign<0)
              {

               if(CountOpenOrders(OP_SELL,Symbol(),MagicNumber)>=MaxOpenPositions)
                  Print("Too many SELL orders on "+Symbol()+", can't open another one");
               else
                  for(int i=0; i<MultipleEntriesAtTheSameTime; i++)
                    {

                     if(MultipleEntriesAtTheSameTime>=1)
                       {
                        if(TPType==TP_Fixed_Pips && ArraySize(takeProfits)>=i && takeProfits[i]>0)
                           tp=entryPrice+sign*takeProfits[i]*pip;
                        if(TPType==RR_Ratio && ArraySize(takeProfitRatios)>=i && takeProfitRatios[i]>0)
                           tp=entryPrice+sign*takeProfitRatios[i]*slDistance;
                       }

                     Print("Open order number "+(string)(i+1));
                     if(OpenSell(pair,volume,sl,tp,TradeComment,MagicNumber))
                        PrintDash(dashRows-2);
                    }

              }
        }
     }

   MoveToBE(pair,MagicNumber,MoveToBreakEven,1);
   TrailSL(pair,MagicNumber,TrailingStop,TrailingStopTrigger,TrailingStopStep);

   if(PartialTPPips>0)
     {
      for(int i=OrdersTotal()-1; i>=0; i--)
        {
         double currentSL=0;
         double currentTP=0;
         ResetLastError();
         if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
            return;
           }
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            if(OrderProfit()>0 && MathAbs(OrderClosePrice()-OrderOpenPrice())/GetPip()>=PartialTPPips)
              {
               Print("partially close order "+(string)OrderTicket());
               int close=OrderClose(OrderTicket(),NormLots(OrderLots()*PartialCloseLotPercentage/100),OrderClosePrice(),10);
              }
           }
        }
     }

   if(VirtualPendingOrders)
     {
      CheckVirtualPendingOrders();
     }
   CheckVirtualStops(pair,MagicNumber);

   DashKeys[0]=EAName;
   DashValues[0]=TimeToStr(TimeCurrent());
   DashKeys[1]="Current spread";
   DashValues[1]=DoubleToStr(MarketInfo(Symbol(),MODE_SPREAD)*(Point/GetPip(Symbol())),1)+" pips";
   DashKeys[2]="Lot-size";
   DashValues[2]="fixed, "+DoubleToStr(FixedLotSize,2)+ " lots";
   if(VolumeType==Used_Margin_Percentage)
      DashValues[2]="dynamic, "+DoubleToStr(MarginPercentage,1)+ "% margin used";
   if(VolumeType==Risk_Percentage)
      DashValues[2]="dynamic, risk="+DoubleToStr(RiskPercentage,1)+ "% of equity";
   if(VolumeType==Risked_Amount)
      DashValues[2]="dynamic, risk="+DoubleToStr(RiskedAmount,1)+ AccountCurrency();
   if(VolumeType==Kelly_Criterion)
      DashValues[2]="dyanmic, kelly criterion, risk="+DoubleToStr(RiskedAmount,1)+ AccountCurrency();

   if(ShowDashBoard)
     {
      DisplayDashboard(281,dashRows-1);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculateLotSize(string symbol,int magic,double slDistance,double valueOrPercentage=0)
  {
   slDistance=MathMax(slDistance,Point);

   double lots=FixedLotSize;
   if(VolumeType==Fixed_Size)
     {
      if(valueOrPercentage!=0)
         return valueOrPercentage;
      else
         return (lots);
     }

   if(VolumeType==Used_Margin_Percentage)
     {
      double marginFor1Lot=MarketInfo(symbol,MODE_MARGINREQUIRED);
      double lotsCanBuy=AccountEquity()/marginFor1Lot;
      if(valueOrPercentage!=0)
         lots=lotsCanBuy*valueOrPercentage/100;
      else
         lots=lotsCanBuy*MarginPercentage/100;
     }

   double pipValue=MarketInfo(symbol,MODE_TICKVALUE);
   double point=MarketInfo(symbol,MODE_POINT);
   double pip=GetPip(symbol);

   if(VolumeType==Kelly_Criterion)
     {
      double winFactor=0;
      double winLossRatio=0;
      int trades=GetKellyCriterionFactors(symbol,magic,winFactor,winLossRatio);

      double riskPercentage=RiskPercentage;
      if(valueOrPercentage!=0)
         riskPercentage=valueOrPercentage;
      if(trades>=minTradesForKellyCriterion)
        {
         double kellyRatio=winFactor-((1-winFactor)/winLossRatio);
         riskPercentage=kellyRatio*100;
        }

      lots=AccountEquity()*riskPercentage/100/((slDistance/point)*pipValue);
     }

   if(point==0)
     {
      Print("point=0 for "+symbol+"! - force exit");
      return 0;
     }
   if(pipValue==0)
     {
      Print("pipvalue=0 for "+symbol+"! - force exit");
      return 0;
     }

   if(VolumeType==Risk_Percentage)
     {
      lots=AccountEquity()*RiskPercentage/100/((slDistance/point)*pipValue);
      Print("sl distance="+DoubleToStr(slDistance/Point,1)+" points, risk="+(string)RiskPercentage+"%, lots="+DoubleToStr(lots,2));
      if(valueOrPercentage!=0)
        {
         lots=AccountEquity()*valueOrPercentage/100/((slDistance/point)*pipValue);
         Print("sl distance="+DoubleToStr(slDistance/Point,1)+" points, risk="+(string)valueOrPercentage+"%, lots="+DoubleToStr(lots,2));
        }
     }

   if(VolumeType==Risked_Amount)
     {
      lots=RiskedAmount/((slDistance/point)*pipValue);
      Print("sl distance="+DoubleToStr(slDistance/Point,1)+" points, risk="+(string)RiskedAmount+"USD, lots="+DoubleToStr(lots,2));
      if(valueOrPercentage!=0)
        {
         lots=valueOrPercentage/((slDistance/point)*pipValue);
         Print("sl distance="+DoubleToStr(slDistance/Point,1)+" points, risk="+(string)valueOrPercentage+"USD, lots="+DoubleToStr(lots,2));
        }
     }

   double lotStep=MarketInfo(symbol,MODE_LOTSTEP);
   int digits = 0;
   if(lotStep<= 0.01)
      digits=2;
   else
      if(lotStep<=0.1)
         digits=1;
   lots=NormalizeDouble(lots,digits);

   double minLots=MarketInfo(symbol,MODE_MINLOT);
   if(lots<minLots)
      lots=minLots;

   double maxLots=MarketInfo(symbol,MODE_MAXLOT);
   if(lots>maxLots)
      lots=maxLots;

   return (lots);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DisplayDashboard(int width=200,int currentProfitIdx=-1)
  {
   if(currentProfitIdx!=-1)
     {
      DashKeys[currentProfitIdx]="Running profit";
      DashValues[currentProfitIdx]="No order currently opened";
      int buys=CountOpenOrders(OP_BUY);
      int sells=CountOpenOrders(OP_SELL);
      if(buys+sells!=0)
        {
         string typesB="";
         string typesS="";
         if(buys==1)
            typesB=IntegerToString(buys)+" BUY";
         if(buys>1)
            typesB=IntegerToString(buys)+" BUYs";
         if(sells==1)
            typesS=IntegerToString(sells)+" SELL";
         if(sells>1)
            typesS=IntegerToString(sells)+" SELLs";
         if(buys!=0 && sells!=0)
            typesS=", "+typesS;
         DashValues[currentProfitIdx]="$"+DoubleToStr(CurrentProfit(Symbol(),MagicNumber,-1),2)+" ("+typesB+typesS+")";
        }
     }

   int fontSize=8;
   int xSign=1;
   int ySign=1;
   int DashBoardXOffset=10;
   int DashBoardYOffset=30;
   if(DashBoardCorner==CORNER_RIGHT_LOWER || DashBoardCorner==CORNER_RIGHT_UPPER)
     {
      DashBoardXOffset+=width;
      xSign=-1;
     }
   if(DashBoardCorner==CORNER_RIGHT_LOWER || DashBoardCorner==CORNER_LEFT_LOWER)
     {
      DashBoardYOffset+=9+ArraySize(DashKeys)*(fontSize+fontSize/2);
      ySign=-1;
     }

   string name="dashBack";
   if(ObjectFind(0,name)<0)
     {
      ObjectCreate(0,name,OBJ_RECTANGLE_LABEL,0,0,0);
     }
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,DashBoardXOffset);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,DashBoardYOffset);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,9+(int)(ArraySize(DashKeys)*(fontSize+fontSize/1.7)));
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,DashBoardBGColor);
   ObjectSetInteger(0,name,OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,name,OBJPROP_CORNER,DashBoardCorner);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_BACK,true);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,10);

   for(int i=0; i<ArraySize(DashKeys)+10; i++)
     {
      name="dashRow"+IntegerToString(i);
      if(i>=ArraySize(DashKeys))
        {
         ObjectDelete(0,name);
        }
      else
        {
         if(ObjectFind(0,name)<0)
           {
            ObjectCreate(0,name,OBJ_LABEL,0,0,0);
           }
         color clr=DashBoardColor;
         if(StringFind(DashValues[i],"Bullish")>=0 || StringFind(DashValues[i],"UP")>=0)
            clr=BullishColor;
         if(StringFind(DashValues[i],"Bearish")>=0 || StringFind(DashValues[i],"DOWN")>=0)
            clr=BearishColor;
         ObjectSetInteger(0,name,OBJPROP_XDISTANCE,DashBoardXOffset+xSign*5);
         ObjectSetInteger(0,name,OBJPROP_YDISTANCE,DashBoardYOffset+ySign*3+(int)(ySign*i*(fontSize+fontSize/1.7)));
         ObjectSetInteger(0,name,OBJPROP_BORDER_TYPE,BORDER_FLAT);
         ObjectSetInteger(0,name,OBJPROP_CORNER,DashBoardCorner);
         ObjectSetInteger(0,name,OBJPROP_COLOR,clr);
         ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
         ObjectSetInteger(0,name,OBJPROP_ZORDER,8);
         ObjectSetString(0,name,OBJPROP_TEXT,DashKeys[i]+": "+DashValues[i]);
         ObjectSetText(name,DashKeys[i]+": "+DashValues[i],fontSize);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void RemoveDashboard()
  {
   ObjectDelete(0,"dashBack");
   for(int i=0; i<ArraySize(DashKeys); i++)
     {
      ObjectDelete(0,"dashRow"+IntegerToString(i));
     }
   ObjectDelete(0,"closeAll");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddCloseAllTradesButton(int DashBoardX,int DashBoardY,int fontSize)
  {
   ObjectCreate(0,"closeAll",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"closeAll",OBJPROP_XDISTANCE,DashBoardX+5);
   ObjectSetInteger(0,"closeAll",OBJPROP_YDISTANCE,DashBoardY+13+ArraySize(DashKeys)*(fontSize+fontSize/2));
   ObjectSetInteger(0,"closeAll",OBJPROP_XSIZE,96);
   ObjectSetString(0,"closeAll",OBJPROP_TEXT,"Close all trades");
   ObjectSetInteger(0,"closeAll",OBJPROP_ZORDER,0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,// Event identifier
                  const long& lparam,   // Event parameter of long type
                  const double& dparam, // Event parameter of double type
                  const string& sparam) // Event parameter of string type
  {
   if(id==CHARTEVENT_OBJECT_CLICK && sparam=="closeAll")
     {
      CloseBuyOrders(Symbol(),MagicNumber);
      CloseSellOrders(Symbol(),MagicNumber);
      Sleep(100);
      ObjectSetInteger(0,"closeAll",OBJPROP_STATE,false);
     }

   if(id==CHARTEVENT_OBJECT_DRAG)
     {
      string labelName=sparam+"txt";
      if(ObjectFind(labelName)==0)
        {
         double newPrice=ObjectGetDouble(0,sparam,OBJPROP_PRICE1);
         ObjectSetDouble(0,labelName,OBJPROP_PRICE1,newPrice);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PrintDash(int rows=1)
  {
   for(int i=1; i<=rows; i++)
     {
      Print(DashKeys[i]+" "+DashValues[i]);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool isBullish(string value)
  {
   return StringFind(value,"Bullish")>=0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool isBearish(string value)
  {
   return StringFind(value,"Bearish")>=0;
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
// #define MacrosHello   "Hello, world!"
// #define MacrosYear    2010
int masterCandleIdx;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FractalUp(int bIdx,string symbol=NULL,int tf=0)
  {
   double fractalUp=iFractals(symbol,tf,MODE_UPPER,bIdx);
   return ok(fractalUp);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FractalDown(int bIdx,string symbol=NULL,int tf=0)
  {
   double fractalDown=iFractals(symbol,tf,MODE_LOWER,bIdx);
   return ok(fractalDown);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool tradingDay[7];
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void InitTradingDays(bool monday,bool tuesday,bool wednesday,bool thursday,bool friday)
  {
   tradingDay[1]=monday;
   tradingDay[2]=tuesday;
   tradingDay[3]=wednesday;
   tradingDay[4]=thursday;
   tradingDay[5]=friday;
   tradingDay[6]=false;
   tradingDay[0]=false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool TradingDay(int day)
  {
   return tradingDay[day];
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input int Debug=1;
bool _StealthSLandTP=false;
bool _StealthPendingOrders=false;
double _MaxSpread=0;
double _MaxSlippage=300;
int _MaxTraderPerBar=0;
string _VS_Prefix="vs ";
string _ObjPrefix="ea ";
bool _AskForManualConfirmation=false;
bool _ECN=true;
bool _DisplayVPOLabels=true;
bool _ShowArrows=true;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string SymbolSuffix(string symbol)
  {
   return (StringSubstr(symbol,6,0));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WaitTradeContext()
  {
   datetime tstart=TimeLocal();
//Print("enter WaitTradeContext");
   while(!IsTradeAllowed() && TimeLocal()-tstart<60)
     {
      Sleep(100);
     }
   if(!IsTradeAllowed())
     {
      Print("Trade not allowed");
     }
   else
     {
      //Print("Trade allowed!");
     }

   while(IsTradeContextBusy() && TimeLocal()-tstart<60)
     {
      Sleep(100);
     }
   if(IsTradeContextBusy())
     {
      Print("Trade context busy");
     }
   else
     {
      //Print("Trade context free!");
     }
//Print("leave WaitTradeContext");
  }
//+------------------------------------------------------------------+
string err_msg(int e)
//+------------------------------------------------------------------+
// Returns error message text for a given MQL4 error number
// Usage:   string s=err_msg(146) returns s="Error 0146:  Trade context is busy."
  {
   switch(e)
     {
      case 0:
         return("Error 0000:  No error returned.");
      case 1:
         return("Error 0001:  No error returned, but the result is unknown.");
      case 2:
         return("Error 0002:  Common error.");
      case 3:
         return("Error 0003:  Invalid trade parameters.");
      case 4:
         return("Error 0004:  Trade server is busy.");
      case 5:
         return("Error 0005:  Old version of the client terminal.");
      case 6:
         return("Error 0006:  No connection with trade server.");
      case 7:
         return("Error 0007:  Not enough rights.");
      case 8:
         return("Error 0008:  Too frequent requests.");
      case 9:
         return("Error 0009:  Malfunctional trade operation.");
      case 64:
         return("Error 0064:  Account disabled.");
      case 65:
         return("Error 0065:  Invalid account.");
      case 128:
         return("Error 0128:  Trade timeout.");
      case 129:
         return("Error 0129:  Invalid price.");
      case 130:
         return("Error 0130:  Invalid stops.");
      case 131:
         return("Error 0131:  Invalid trade volume.");
      case 132:
         return("Error 0132:  Market is closed.");
      case 133:
         return("Error 0133:  Trade is disabled.");
      case 134:
         return("Error 0134:  Not enough money.");
      case 135:
         return("Error 0135:  Price changed.");
      case 136:
         return("Error 0136:  Off quotes.");
      case 137:
         return("Error 0137:  Broker is busy.");
      case 138:
         return("Error 0138:  Requote.");
      case 139:
         return("Error 0139:  Order is locked.");
      case 140:
         return("Error 0140:  Long positions only allowed.");
      case 141:
         return("Error 0141:  Too many requests.");
      case 145:
         return("Error 0145:  Modification denied because order too close to market.");
      case 146:
         return("Error 0146:  Trade context is busy.");
      case 147:
         return("Error 0147:  Expirations are denied by broker.");
      case 148:
         return("Error 0148:  The amount of open and pending orders has reached the limit set by the broker.");
      case 149:
         return("Error 0149:  An attempt to open a position opposite to the existing one when hedging is disabled.");
      case 150:
         return("Error 0150:  An attempt to close a position contravening the FIFO rule.");
      case 4000:
         return("Error 4000:  No error.");
      case 4001:
         return("Error 4001:  Wrong function pointer.");
      case 4002:
         return("Error 4002:  Array index is out of range.");
      case 4003:
         return("Error 4003:  No memory for function call stack.");
      case 4004:
         return("Error 4004:  Recursive stack overflow.");
      case 4005:
         return("Error 4005:  Not enough stack for parameter.");
      case 4006:
         return("Error 4006:  No memory for parameter string.");
      case 4007:
         return("Error 4007:  No memory for temp string.");
      case 4008:
         return("Error 4008:  Not initialized string.");
      case 4009:
         return("Error 4009:  Not initialized string in array.");
      case 4010:
         return("Error 4010:  No memory for array string.");
      case 4011:
         return("Error 4011:  Too long string.");
      case 4012:
         return("Error 4012:  Remainder from zero divide.");
      case 4013:
         return("Error 4013:  Zero divide.");
      case 4014:
         return("Error 4014:  Unknown command.");
      case 4015:
         return("Error 4015:  Wrong jump (never generated error).");
      case 4016:
         return("Error 4016:  Not initialized array.");
      case 4017:
         return("Error 4017:  DLL calls are not allowed.");
      case 4018:
         return("Error 4018:  Cannot load library.");
      case 4019:
         return("Error 4019:  Cannot call function.");
      case 4020:
         return("Error 4020:  Expert function calls are not allowed.");
      case 4021:
         return("Error 4021:  Not enough memory for temp string returned from function.");
      case 4022:
         return("Error 4022:  System is busy (never generated error).");
      case 4050:
         return("Error 4050:  Invalid function parameters count.");
      case 4051:
         return("Error 4051:  Invalid function parameter value.");
      case 4052:
         return("Error 4052:  String function internal error.");
      case 4053:
         return("Error 4053:  Some array error.");
      case 4054:
         return("Error 4054:  Incorrect series array using.");
      case 4055:
         return("Error 4055:  Custom indicator error.");
      case 4056:
         return("Error 4056:  Arrays are incompatible.");
      case 4057:
         return("Error 4057:  Global variables processing error.");
      case 4058:
         return("Error 4058:  Global variable not found.");
      case 4059:
         return("Error 4059:  Function is not allowed in testing mode.");
      case 4060:
         return("Error 4060:  Function is not confirmed.");
      case 4061:
         return("Error 4061:  Send mail error.");
      case 4062:
         return("Error 4062:  String parameter expected.");
      case 4063:
         return("Error 4063:  Integer parameter expected.");
      case 4064:
         return("Error 4064:  Double parameter expected.");
      case 4065:
         return("Error 4065:  Array as parameter expected.");
      case 4066:
         return("Error 4066:  Requested history data in updating state.");
      case 4067:
         return("Error 4067:  Some error in trading function.");
      case 4099:
         return("Error 4099:  End of file.");
      case 4100:
         return("Error 4100:  Some file error.");
      case 4101:
         return("Error 4101:  Wrong file name.");
      case 4102:
         return("Error 4102:  Too many opened files.");
      case 4103:
         return("Error 4103:  Cannot open file.");
      case 4104:
         return("Error 4104:  Incompatible access to a file.");
      case 4105:
         return("Error 4105:  No order selected.");
      case 4106:
         return("Error 4106:  Unknown symbol.");
      case 4107:
         return("Error 4107:  Invalid price.");
      case 4108:
         return("Error 4108:  Invalid ticket.");
      case 4109:
         return("Error 4109:  Trade is not allowed. Enable checkbox 'Allow live trading' in the expert properties.");
      case 4110:
         return("Error 4110:  Longs are not allowed. Check the expert properties.");
      case 4111:
         return("Error 4111:  Shorts are not allowed. Check the expert properties.");
      case 4200:
         return("Error 4200:  Object exists already.");
      case 4201:
         return("Error 4201:  Unknown object property.");
      case 4202:
         return("Error 4202:  Object does not exist.");
      case 4203:
         return("Error 4203:  Unknown object type.");
      case 4204:
         return("Error 4204:  No object name.");
      case 4205:
         return("Error 4205:  Object coordinates error.");
      case 4206:
         return("Error 4206:  No specified subwindow.");
      case 4207:
         return("Error 4207:  Some error in object function.");
      //    case 9001:  return("Error 9001:  Cannot close entire order - insufficient volume previously open.");
      //    case 9002:  return("Error 9002:  Incorrect net position.");
      //    case 9003:  return("Error 9003:  Orders not completed correctly - details in log file.");
      default:
         return("Error " + IntegerToString(OrderTicket()) + ": ??? Unknown error.");
     }
   return("n/a");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum StopType {StopType_SL,StopType_TP};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddSpreadCheckFor(string symbol)
  {
   string s_spreadSumGV="Spread Sum "+symbol;
   string s_spreadCountGV="Spread Count "+symbol;

   ResetLastError();
   double sum=GlobalVariableGet(s_spreadSumGV);
   int count=(int)GlobalVariableGet(s_spreadCountGV);

   double pip=GetPip(symbol);
   double spread=NormalizeDouble(MarketInfo(symbol,MODE_SPREAD)*MarketInfo(symbol,MODE_POINT)/pip,2);

   Print(StringConcatenate("Add ",(string)spread," for ",symbol," sum="+(string)sum,", count=",(string)count));

   GlobalVariableSet(s_spreadSumGV,sum+spread);
   GlobalVariableSet(s_spreadCountGV,count+1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int martingaleSells;
int martingaleBuys;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenBuy(string pair,double lot,double sl,double tp,string comment,int magic,bool fifo=false,double martingaleMult=0,int maxMultiplications=0)
  {
   int ticket=0,tries=0;
   static datetime last=0;
   static int ordersThisBar=0;
   if(last!=Time[0] || ordersThisBar<_MaxTraderPerBar || _MaxTraderPerBar==0)
     {
      if(!IsTesting())
         ChartBringToTop();
      double SL=0,TP=0;
      if(sl>0)
         SL=sl;
      if(tp>0)
         TP=tp;

      if(martingaleMult>0)
        {
         int lastT=FindOrder(pair,magic,OP_BUY,-1,NULL,0,0,0,0,false,true);
         if(lastT>0 && _selectedProfit<0 && martingaleBuys<maxMultiplications)
           {
            lot=NormLots(_selectedVolume*martingaleMult);
            martingaleBuys++;
           }
         else
           {
            martingaleBuys=0;
           }
        }

      int tries_main=0;
      while(ticket<=0)
        {
         WaitTradeContext();
         RefreshRates();
         int digits=(int) MarketInfo(pair,MODE_DIGITS);
         double point=MarketInfo(pair,MODE_POINT);
         double price= MarketInfo(pair,MODE_ASK);
         double spread=MarketInfo(pair,MODE_SPREAD);
         double _pip=GetPip(pair);

         if(spread*(point/_pip)>_MaxSpread && _MaxSpread!=0)
           {
            Print("Order not opened because spread ("+(string)(spread*(point/_pip))+") is higher than max allowed spread ("+(string)_MaxSpread);
            return false;
           }

         if(!AccountFreeMarginCheck(Symbol(),OP_BUY,lot)>0)
           {
            Print("Order not opened because there's not enough money to buy "+DoubleToStr(lot,2)+" lots, try to lower the volume, ett="+(string)GetLastError());
            return false;
           }

         color arrowColor=clrLimeGreen;
         if(!_ShowArrows)
            arrowColor=clrNONE;
         if(SL!=0 && SL>MarketInfo(pair,MODE_BID)-MarketInfo(pair,MODE_STOPLEVEL)*point)
            SL=MarketInfo(pair,MODE_BID)-MarketInfo(pair,MODE_STOPLEVEL)*point;
         if(TP!=0 && TP<MarketInfo(pair,MODE_BID)+MarketInfo(pair,MODE_STOPLEVEL)*point)
            TP=MarketInfo(pair,MODE_BID)+MarketInfo(pair,MODE_STOPLEVEL)*point;

         if(_ECN)
           {
            ticket=OrderSend(pair,OP_BUY,lot,price,(int)(_MaxSlippage*_pip/point),0,0,comment,magic,Period(),arrowColor);
           }
         else
           {
            Print(price,NormalizeDouble(SL,digits),NormalizeDouble(TP,digits));
            ticket=OrderSend(pair,OP_BUY,lot,price,(int)(_MaxSlippage*_pip/point),NormalizeDouble(SL,digits),NormalizeDouble(TP,digits),comment,magic,Period(),arrowColor);
           }
         if(ticket<0)
            Print(err_msg(GetLastError()));
         if(ticket>0 && _ECN && (SL>0 || TP>0))
           {
            if(fifo && SL!=0)
               SL=FifoAllowedSLTP(SL,pair,OP_SELL,StopType_SL);
            if(fifo && TP!=0)
               TP=FifoAllowedSLTP(TP,pair,OP_SELL,StopType_TP);
            if(OrderSelect(ticket,SELECT_BY_TICKET))
              {
               if(_StealthSLandTP)
                 {
                  if(SL!=0)
                     SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(ticket),NormalizeDouble(SL,digits),clrRed,STYLE_DASHDOT,"SL for #"+IntegerToString(ticket),0,0);
                  if(TP!=0)
                     SetHorizontalLine(_VS_Prefix+"TP"+IntegerToString(ticket),NormalizeDouble(TP,digits),clrGreen,STYLE_DASHDOT,"TP for #"+IntegerToString(ticket),0,0);
                 }
               else
                 {
                  while(tries<10)
                    {
                     WaitTradeContext();
                     RefreshRates();
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(SL,digits),NormalizeDouble(TP,digits),0))
                        break;
                     tries++;
                    }
                 }
              }
           }
         tries_main++;
         if(tries_main>10)
            break;
        }

      if(ticket>0)
        {
         if(last!=Time[0])
            ordersThisBar=0;
         ordersThisBar++;
         last=Time[0];
         Print("Order Buy was opened");
         //  AddSpreadCheckFor(pair);
        }
     }
   return(ticket);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenSell(string pair,double lot,double sl,double tp,string comment,int magic,bool fifo=false,double martingaleMult=0,int maxMultiplications=0)
  {
   int ticket=0;
   static datetime last=0;
   static int ordersThisBar=0;
   if(last!=Time[0] || ordersThisBar<_MaxTraderPerBar || _MaxTraderPerBar==0)
     {
      if(!IsTesting())
         ChartBringToTop();
      int tries=0;
      double SL=0,TP=0;
      if(sl>0)
         SL=sl;
      if(tp>0)
         TP=tp;

      if(martingaleMult>0)
        {
         int lastT=FindOrder(pair,magic,OP_SELL,-1,NULL,0,0,0,0,false,true);
         if(lastT>0 && _selectedProfit<0)
           {
            lot=NormLots(_selectedVolume*martingaleMult);
            martingaleSells++;
           }
         else
           {
            martingaleSells=0;
           }
        }

      int tries_main=0;
      while(ticket<=0)
        {
         WaitTradeContext();
         RefreshRates();
         int digits=(int)MarketInfo(pair,MODE_DIGITS);
         double point=MarketInfo(pair,MODE_POINT);
         double price= MarketInfo(pair,MODE_BID);
         double spread=MarketInfo(pair,MODE_SPREAD);
         double _pip=GetPip(pair);

         if(spread*(point/_pip)>_MaxSpread && _MaxSpread!=0)
           {
            Print("Order not opened because spread ("+(string)(spread*(point/_pip))+") is higher than max allowed spread ("+(string)_MaxSpread);
            return false;
           }
         if(!AccountFreeMarginCheck(Symbol(),OP_SELL,lot)>0)
           {
            Print("Order not opened because there's not enough money, try to lower the volume, ett="+(string)GetLastError());
            return false;
           }

         color arrowColor=clrLimeGreen;
         if(!_ShowArrows)
            arrowColor=clrNONE;
         if(SL!=0 && SL<MarketInfo(pair,MODE_ASK)+MarketInfo(pair,MODE_STOPLEVEL)*point)
            SL=MarketInfo(pair,MODE_ASK)+MarketInfo(pair,MODE_STOPLEVEL)*point;
         if(TP!=0 && TP>MarketInfo(pair,MODE_ASK)-MarketInfo(pair,MODE_STOPLEVEL)*point)
            TP=MarketInfo(pair,MODE_ASK)-MarketInfo(pair,MODE_STOPLEVEL)*point;

         if(_ECN)
           {
            ticket=OrderSend(pair,OP_SELL,lot,price,(int)(_MaxSlippage*_pip/point),0,0,comment,magic,0,arrowColor);
           }
         else
           {
            ticket=OrderSend(pair,OP_SELL,lot,price,(int)(_MaxSlippage*_pip/point),NormalizeDouble(SL,digits),NormalizeDouble(TP,digits),comment,magic,0,arrowColor);
           }
         if(ticket<0)
            Print(err_msg(GetLastError()));
         if(ticket>0 && _ECN && (SL>0 || TP>0))
           {
            if(fifo && SL!=0)
               SL=FifoAllowedSLTP(SL,pair,OP_SELL,StopType_SL);
            if(fifo && TP!=0)
               TP=FifoAllowedSLTP(TP,pair,OP_SELL,StopType_TP);
            if(OrderSelect(ticket,SELECT_BY_TICKET))
              {
               if(_StealthSLandTP)
                 {
                  if(SL!=0)
                     SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(ticket),NormalizeDouble(SL,digits),clrRed,STYLE_DASHDOT,"SL for #"+IntegerToString(ticket),0,0);
                  if(TP!=0)
                     SetHorizontalLine(_VS_Prefix+"TP"+IntegerToString(ticket),NormalizeDouble(TP,digits),clrGreen,STYLE_DASHDOT,"TP for #"+IntegerToString(ticket),0,0);
                 }
               else
                 {
                  while(tries<10)
                    {
                     WaitTradeContext();
                     RefreshRates();
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(SL,digits),NormalizeDouble(TP,digits),0))
                        break;
                     tries++;
                    }
                 }
              }
           }
         tries_main++;
         if(tries_main>10)
            break;
        }

      if(ticket>0)
        {
         if(last!=Time[0])
            ordersThisBar=0;
         ordersThisBar++;
         last=Time[0];
         Print("Order Sell was opened");
         // AddSpreadCheckFor(pair);
        }
     }
   return (ticket);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string OscillatorDirection(double price,double buyLevel,double sellLevel)
  {
   if(price<buyLevel)
      return "Bullish";
   if(price>sellLevel)
      return "Bearish";
   return "n/a";
  }
//+----------------------------------------------------------------------+
//| Send command to the terminal to display the chart above all others.  |
//+----------------------------------------------------------------------+
bool ChartBringToTop(const long chart_ID=0)
  {
//--- reset the error value
   ResetLastError();
//--- show the chart on top of all others
   if(!ChartSetInteger(chart_ID,CHART_BRING_TO_TOP,0,true))
     {
      //--- display the error message in Experts journal
      Print(__FUNCTION__+", Error Code = ",GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CountOpenOrders(int type1=-1,string symbol=NULL,int magic=-1,int type2=-1,string comment="",double min=0,double max=0,double tp=0)
  {
   int total=0;
   for(int i=OrdersTotal()-1; i >= 0; i--)
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return 0;
        }
      if(OrderSymbol()==symbol || StringLen(symbol)==0)
         if(magic<0 || OrderMagicNumber()==magic)
            if(OrderType()==type1 || OrderType()==type2 || type1<0)
               if(StringLen(comment)==0 || StringFind(OrderComment(),comment)>=0)
                  if(min==0 || NormalizeDouble(OrderOpenPrice(),(int)MarketInfo(symbol,MODE_DIGITS))>=NormalizeDouble(min,(int)MarketInfo(symbol,MODE_DIGITS)))
                     if(max==0 || NormalizeDouble(OrderOpenPrice(),(int)MarketInfo(symbol,MODE_DIGITS))<=NormalizeDouble(max,(int)MarketInfo(symbol,MODE_DIGITS)))
                       {
                        double orderTP=OrderTakeProfit();
                        int digits=(int)MarketInfo(OrderSymbol(),MODE_DIGITS);
                        if(tp!=0 && _StealthSLandTP)
                          {
                           orderTP=ObjectGet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
                          }
                        if(tp==0 || NormalizeDouble(orderTP,digits)==NormalizeDouble(tp,digits))
                           total++;
                       }
     }
   return (total);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int FindOrder(string symbol=NULL,int magic=-1,int type1=-1,int type2=-1,string comment="",double min=0,double max=0,double tp=0,int inLastXSeconds=0,bool oldest=false,bool lookInHistory=false)
  {
   int ticket=-1;

   int total=0;
   for(int i=OrdersTotal()-1; i >= 0; i--)
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return 0;
        }
      if(OrderSymbol()==symbol || StringLen(symbol)==0)
         if(magic<0 || OrderMagicNumber()==magic)
            if(OrderType()==type1 || OrderType()==type2 || type1<0)
               if(StringLen(comment)==0 || StringFind(OrderComment(),comment)>=0)
                  if(min==0 || NormalizeDouble(OrderOpenPrice(),(int)MarketInfo(symbol,MODE_DIGITS))>=NormalizeDouble(min,(int)MarketInfo(symbol,MODE_DIGITS)))
                     if(max==0 || NormalizeDouble(OrderOpenPrice(),(int)MarketInfo(symbol,MODE_DIGITS))<=NormalizeDouble(max,(int)MarketInfo(symbol,MODE_DIGITS)))
                       {
                        if(inLastXSeconds==0 || TimeCurrent()-OrderOpenTime()<=inLastXSeconds)
                          {
                           double orderTP=OrderTakeProfit();
                           int digits=(int)MarketInfo(OrderSymbol(),MODE_DIGITS);
                           if(tp!=0 && _StealthSLandTP)
                             {
                              orderTP=ObjectGet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
                             }
                           if(tp==0 || NormalizeDouble(orderTP,digits)==NormalizeDouble(tp,digits))
                             {
                              if(oldest)
                                 ticket=OrderTicket();
                              else
                                 return OrderTicket();
                             }
                          }
                       }
     }

   if(_selectedTicket==0 && lookInHistory)
     {
      for(int i=OrdersHistoryTotal()-1; i>=0; i--)
        {
         ResetLastError();
         if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
           {
            Print("Failed to select history orders in "+__FUNCTION__+", error="+(string)GetLastError());
            return false;
           }
         if(OrderSymbol()==symbol)
            if(magic<0 || OrderMagicNumber()==magic)
               if(OrderType()==type1 || OrderType()==type2 || type1<0)
                  if(StringLen(comment)==0 || StringFind(OrderComment(),comment)>=0)
                     if(min==0 || OrderOpenPrice()>=min)
                        if(max==0 || OrderOpenPrice()<=max)
                          {
                           if(inLastXSeconds==0 || TimeCurrent()-OrderOpenTime()<=inLastXSeconds)
                             {
                              double orderTP=OrderTakeProfit();
                              int digits=(int)MarketInfo(OrderSymbol(),MODE_DIGITS);
                              if(tp!=0 && _StealthSLandTP)
                                {
                                 orderTP=ObjectGet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
                                }
                              if(tp==0 || NormalizeDouble(orderTP,digits)==NormalizeDouble(tp,digits))
                                {
                                 if(oldest)
                                    ticket=OrderTicket();
                                 else
                                    return OrderTicket();
                                }
                             }
                          }
        }
     }

   return ticket;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeleteLine(string name)
  {
   name=GetObjectName(name);
   ObjectDelete(0,name);
   ObjectDelete(0,name+"txt");
   ObjectDelete(0,name+"bottomtxt");
   ObjectDelete(0,name+"toptxt");
   ObjectDelete(0,name+"pricetxt");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CountVirtualPendings(string symbol=NULL,int magic=-1,int type=-1,double minPrice=0,double maxPrice=0,double tp=0,string comment=NULL)
  {
   int total=0;
   int digits=(int)MarketInfo(symbol,MODE_DIGITS);
   if(digits==0)
      digits=5;
   for(int i=0; i<GlobalVariablesTotal(); i++)
     {
      string data=GlobalVariableName(i);
      if(StringFind(data,"sl=")>=0 || StringFind(data,"tp=")>=0)
        {
         int _magic=(int)StringToInteger(GetInfoBitFromVPOString(data,"#"));
         int _type=GetOrderTypeFromVPOString(data);
         string _symbol=GetSymbolFromVPOString(data);
         double _price=(double)StringToDouble(GetInfoBitFromVPOString(data,"@"));
         double _tp=(double)StringToDouble(GetInfoBitFromVPOString(data,"tp="));
         string _comment=GetInfoBitFromVPOString(data,"c=");

         if((_symbol==symbol || symbol==NULL) && (_magic==magic || magic<0))
            if(_type==type || type<0)
               if(minPrice==0 || NormalizeDouble(_price,digits)>=NormalizeDouble(minPrice,digits))
                  if(maxPrice==0 || NormalizeDouble(_price,digits)<=NormalizeDouble(maxPrice,digits))
                     if(tp==0 || NormalizeDouble(_tp,digits)==NormalizeDouble(tp,digits))
                        if(StringFind(_comment,comment)>=0 || comment==NULL)
                           total++;
        }
     }
   return total;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool _UsePointInsteadOfPip=false;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetPip(string symbol=NULL)
  {

   if(symbol==NULL)
      symbol=Symbol();

#ifdef PipValue
   return  PipValue;
#endif

   double point=MarketInfo(symbol,MODE_POINT);

   if(_UsePointInsteadOfPip)
      return point;

   double _pip=point;
   if(point==0.00001 || point==0.001)
     {
      _pip=point*10;
     }
   else
     {
      _pip=point;
     }
   if(StringFind(symbol,"XAU")==0)
     {
      _pip=1;
     }

   if(StringFind(symbol,"XAG")==0)
     {
      _pip=0.1;
     }

   if(MarketInfo(symbol,MODE_BID)>1000)
     {
      return 1;
     }
   return _pip;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetHorizontalLine(string name,double price,color clr,int style,string label="",int width=1,int selected=0)
  {
   name=GetObjectName(name);
   if(ObjectFind(name)==0)
     {
      ObjectSet(name,OBJPROP_PRICE1,price);
      ObjectSet(name,OBJPROP_PRICE2,price);
      ObjectSet(name,OBJPROP_TIME1,iTime(NULL,0,Bars));
      ObjectSet(name,OBJPROP_TIME2,iTime(NULL,0,0));
     }
   else
     {
      ObjectCreate(name,OBJ_HLINE,0,iTime(NULL,0,Bars),price,iTime(NULL,0,0),price);
      ObjectSet(name,OBJPROP_RAY,false);
     }

   ObjectSet(name,OBJPROP_COLOR,clr);
   ObjectSet(name,OBJPROP_STYLE,style);
   ObjectSet(name,OBJPROP_WIDTH,width);
   ObjectSet(name,OBJPROP_SELECTED,selected);
   ObjectSet(name,OBJPROP_STYLE,style);

   if(StringLen(label)!=0 && (IsVisualMode() || !IsTesting()))
     {
      name=name+"pricetxt";
      datetime time=GetTimeForLabel(name,price);
      if(ObjectFind(name)!=0)
        {
         ObjectCreate(name,OBJ_TEXT,0,time,price);
        }
      else
        {
         ObjectSet(name,OBJPROP_PRICE1,price);
         ObjectSet(name,OBJPROP_TIME1,time);
        }
      ObjectSetText(name,label,8);
      ObjectSet(name,OBJPROP_COLOR,clr);
      ObjectSetInteger(0,name,OBJPROP_ANCHOR,ANCHOR_LEFT_LOWER);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime GetTimeForLabel(string labelName,double price)
  {
   int firstIdx=(int)ChartGetInteger(0,CHART_FIRST_VISIBLE_BAR);
   int candles=(int)ChartGetInteger(0,CHART_WIDTH_IN_BARS);
   datetime time=Time[MathMax(0,firstIdx-candles+8)];

   for(int i=0; i<ObjectsTotal(0,0,OBJ_TEXT); i++)
     {
      string name=ObjectName(0,i,0,OBJ_TEXT);
      if(name==labelName)
         break;
      if(price==ObjectGetDouble(0,name,OBJPROP_PRICE))
        {
         time+=(int)(Period()*60*StringLen(ObjectGetString(0,name,OBJPROP_TEXT))/4.5 *(MathPow(2,5-ChartGetInteger(0,CHART_SCALE))));
        }
     }

   return time;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetVerticalLine(string name,datetime time,color clr,int style,int width=1,string label=NULL,bool labelDown=true)
  {
   name=GetObjectName(name);
   if(ObjectFind(name)==0)
     {
      ObjectSet(name,OBJPROP_TIME1,time);
     }
   else
     {
      ObjectCreate(0,name,OBJ_VLINE,0,time,0);
      ObjectSet(name,OBJPROP_RAY,false);
     }

   ObjectSet(name,OBJPROP_COLOR,clr);
   ObjectSet(name,OBJPROP_STYLE,style);
   ObjectSet(name,OBJPROP_WIDTH,width);

   if(StringLen(label)!=0)
     {
      string labelName=name+"bottomtxt";

      int lowestIdx=iLowest(Symbol(),Period(),MODE_LOW,(int)ChartGetInteger(0,CHART_VISIBLE_BARS),0);
      double labelPrice=Low[lowestIdx];
      if(!labelDown)
        {
         int highestIdx=iHighest(Symbol(),Period(),MODE_HIGH,(int)ChartGetInteger(0,CHART_VISIBLE_BARS),0);
         labelPrice=High[highestIdx];
         labelName=name+"toptxt";
        }

      if(ObjectFind(labelName)!=0)
        {
         ObjectCreate(labelName,OBJ_TEXT,0,time,labelPrice);
        }
      else
        {
         ObjectSet(labelName,OBJPROP_PRICE1,labelPrice);
         ObjectSet(labelName,OBJPROP_TIME1,time);
        }
      ObjectSetText(labelName," "+label,8);
      ObjectSet(labelName,OBJPROP_COLOR,clr);
      ObjectSetInteger(0,labelName,OBJPROP_ANCHOR,ANCHOR_LEFT_LOWER);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UpdateLabels()
  {
   int total=ObjectsTotal(0,-1,OBJ_TEXT);
   int to=MathMax(0,total-20);
   for(int i=total-1; i>=to; i--)
     {
      string name=ObjectName(0,i,-1,OBJ_TEXT);
      if(StringFind(name,"bottomtxt")>=0)
        {

         int lowestIdx=iLowest(Symbol(),Period(),MODE_LOW,(int)ChartGetInteger(0,CHART_VISIBLE_BARS),0);
         double labelPrice=Low[lowestIdx];
         ObjectSetDouble(0,name,OBJPROP_PRICE,labelPrice);
        }
      if(StringFind(name,"toptxt")>=0)
        {
         int highestIdx=iHighest(Symbol(),Period(),MODE_HIGH,(int)ChartGetInteger(0,CHART_VISIBLE_BARS),0);
         double  labelPrice=High[highestIdx];
         ObjectSetDouble(0,name,OBJPROP_PRICE,labelPrice);
        }
      if(StringFind(name,"pricetxt")>=0)
        {
         double  labelPrice=ObjectGetDouble(0,StringSubstr(name,0,StringLen(name)-8),OBJPROP_PRICE1);
         ObjectSetDouble(0,name,OBJPROP_PRICE,labelPrice);
         ObjectSetInteger(0,name,OBJPROP_TIME1,GetTimeForLabel(name,labelPrice));
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetRectangle(string name,datetime time1,datetime time2,double price1,double price2,color clr)
  {
   if(ObjectFind(name)==0)
     {
      ObjectSet(name,OBJPROP_TIME1,time1);
      ObjectSet(name,OBJPROP_TIME2,time2);
      ObjectSet(name,OBJPROP_PRICE1,price1);
      ObjectSet(name,OBJPROP_PRICE2,price2);
     }
   else
     {
      ObjectCreate(0,name,OBJ_RECTANGLE,0,time1,price1,time2,price2);
     }

   ObjectSet(name,OBJPROP_COLOR,clr);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime lastOpenTime[8];
double lastOpenPrice[8];
double lastOpenTP[8];
double lastOpenSL[8];
double lastOpenVolume[8];
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int SetPendingOrder(string symbol,double level,int type,double SLpips,double TPpips,double volume,int magic,string comment,int expiration=0,bool newOrUpdate=false,bool usePoints=false,bool usePriceNotDistance=false)
  {

   double point=MarketInfo(symbol,MODE_POINT);
   double spread=MarketInfo(symbol,MODE_SPREAD);
   double pip=GetPip(symbol);

   if(point==0)
     {
      Print("point=0 for "+symbol+"! - force exit");
      return 0;
     }

   if(spread*(point/pip)>_MaxSpread && _MaxSpread!=0)
     {
      Print("Spread too big, didn't place pending");
      return -1;
     }

   if(usePoints)
      pip=point;

   RefreshRates();

   int digits=(int)MarketInfo(symbol,MODE_DIGITS);
   double minDistance=MarketInfo(symbol,MODE_STOPLEVEL)*point;
   double ask = MarketInfo(symbol,MODE_ASK);
   double bid = MarketInfo(symbol,MODE_BID);
   double sl = 0;
   double tp = 0;
   if(type==OP_SELLLIMIT)
     {
      if(level-bid<minDistance)
        {
         level=bid+minDistance;
        }
      if(SLpips!=0)
        {
         if(SLpips*pip<minDistance)
           {
            sl=level+minDistance;
           }
         else
           {
            sl=level+SLpips*pip;
           }
        }
      if(TPpips!=0)
        {
         if(TPpips*pip<minDistance)
           {
            tp=level-minDistance;
           }
         else
           {
            tp=level-TPpips*pip;
           }
        }
     }
   else
      if(type==OP_BUYSTOP)
        {
         if(level-ask<minDistance)
           {
            Print("adjust buy stop entry level (ask+minDistance), min distance="+(string)MarketInfo(symbol,MODE_STOPLEVEL));
            level=ask+minDistance;
           }
         if(SLpips!=0)
           {
            if(SLpips*pip<minDistance)
              {
               Print("adjust buy stop SL (ask+minDistance)");
               sl=level-minDistance;
              }
            else
              {
               sl=level-SLpips*pip;
              }
           }
         if(TPpips!=0)
           {
            if(TPpips*pip<minDistance)
              {
               Print("adjust buy stop TP (ask+minDistance)");
               tp=level+minDistance;
              }
            else
              {
               tp=level+TPpips*pip;
              }
           }
        }
      else
         if(type==OP_SELLSTOP)
           {
            if(bid-level<minDistance)
              {
               level=bid-minDistance;
              }
            if(SLpips!=0)
              {
               if(SLpips*pip<minDistance)
                 {
                  sl=level+minDistance;
                 }
               else
                 {
                  sl=level+SLpips*pip;
                 }
              }
            if(TPpips!=0)
              {
               if(TPpips*pip<minDistance)
                 {
                  tp=level-minDistance;
                 }
               else
                 {
                  tp=level-TPpips*pip;
                 }
              }
           }
         else
            if(type==OP_BUYLIMIT)
              {
               if(ask-level<minDistance)
                 {
                  level=ask-minDistance;
                 }
               if(SLpips!=0)
                 {
                  if(SLpips*pip<minDistance)
                    {
                     sl=level-minDistance;
                    }
                  else
                    {
                     sl=level-SLpips*pip;
                    }
                 }
               if(TPpips!=0)
                 {
                  if(TPpips*pip<minDistance)
                    {
                     tp=level+minDistance;
                    }
                  else
                    {
                     tp=level+TPpips*pip;
                    }
                 }
              }

   level=NormalizeDouble(level,digits);
   tp=NormalizeDouble(tp,digits);
   sl=NormalizeDouble(sl,digits);


   if(usePriceNotDistance)
     {
      sl=SLpips;
      tp=TPpips;
     }

   string vpo=symbol+",!"+(string)((int)type)+",#"+(string)magic+",@"+DoubleToString(level,digits)+",*"+DoubleToString(volume,2)+",sl="+(string)sl+",tp="+(string)tp+(string)tp+",c="+comment;

   bool newOrder=true;
   if(newOrUpdate)
     {
      if(_StealthPendingOrders)
        {
         for(int i=0; i<GlobalVariablesTotal(); i++)
           {
            string data=GlobalVariableName(i);
            if(StringFind(data,"sl=")>=0 || StringFind(data,"tp=")>=0)
              {
               int _magic=(int)StringToInteger(GetInfoBitFromVPOString(data,"#"));
               double _level=(double)StringToDouble(GetInfoBitFromVPOString(data,"@"));
               double _sl=(double)StringToDouble(GetInfoBitFromVPOString(data,"sl="));
               double _tp=(double)StringToDouble(GetInfoBitFromVPOString(data,"tp="));
               int _type=GetOrderTypeFromVPOString(data);
               string _symbol=GetSymbolFromVPOString(data);

               if(_type==type && _symbol==symbol && _magic==magic)
                 {
                  if(NormalizeDouble(sl,digits)!=_sl || NormalizeDouble(tp,digits)!=_tp || NormalizeDouble(level,digits)!=_level)
                    {

                     DeleteVirtualPending(data);
                     GlobalVariableSet(vpo,1);
                    }
                  newOrder=false;
                 }
              }
           }
        }
      else
        {
         for(int i=OrdersTotal()-1; i>=0; i--)
           {
            ResetLastError();
            if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
              {
               Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
               return -1;
              }
            if(OrderSymbol()==symbol && OrderMagicNumber()==magic)
              {
               if(OrderType()==type)
                 {
                  if(NormalizeDouble(sl,digits)!=NormalizeDouble(OrderStopLoss(),digits)
                     || NormalizeDouble(tp,digits)!=NormalizeDouble(OrderTakeProfit(),digits)
                     || NormalizeDouble(level,digits)!=NormalizeDouble(OrderOpenPrice(),digits))
                    {
                     ResetLastError();
                     if(!OrderModify(OrderTicket(),level,sl,tp,expiration))
                       {
                        Print("Failed to modify order in "+__FUNCTION__+", error="+err_msg(GetLastError()));
                        return OrderTicket();
                       }
                    }
                  newOrder=false;
                 }
              }
           }
        }
     }

   int ticket=0;
   if(newOrder)
     {

      lastOpenPrice[type]=level;;
      lastOpenTime[type]=Time[0];
      lastOpenVolume[type]=volume;

      color clr=clrGreen;
      if(type==OP_SELLSTOP || type==OP_SELLLIMIT)
         clr=clrRed;
      if(!_ShowArrows)
         clr=clrNONE;
      if(_StealthPendingOrders)
        {
         Print("setting virtual pending order ("+symbol+", "+TypeName(type)+", #"+(string)magic+", @"+(string)level+", *"+DoubleToString(volume,2)+", sl="+(string)sl+", tp="+(string)tp+")");
         int count=(int)GlobalVariableGet(vpo);
         Print("sl="+DoubleToStr(sl,5)+" tp="+DoubleToStr(tp,5));
         GlobalVariableSet(vpo,count+1);
         Print("count="+(string)(count+1));
        }
      else
        {
         ResetLastError();
         double _sl=sl;
         double _tp=tp;
         if(_StealthSLandTP)
           {
            _sl=0;
            _tp=0;
           }
         ticket=OrderSend(symbol,type,volume,level,(int)(_MaxSlippage*pip/point),_sl,_tp,comment,magic,expiration,clr);

         if(_StealthSLandTP)
           {
            if(sl!=0)
               SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(ticket),NormalizeDouble(sl,digits),clrRed,2,"SL for #"+IntegerToString(ticket));
            if(tp!=0)
               SetHorizontalLine(_VS_Prefix+"TP"+IntegerToString(ticket),NormalizeDouble(tp,digits),clrGreen,2,"TP for #"+IntegerToString(ticket));
           }

         if(ticket<=0)
           {
            Print("Failed to place pending order in "+__FUNCTION__+", error="+err_msg(GetLastError()));
           }
        }
     }

   return ticket;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string TypeName(int type)
  {
   switch(type)
     {
      case OP_BUYSTOP:
         return "Buy Stop";
      case OP_BUYLIMIT:
         return "Buy Limit";
      case OP_SELLSTOP:
         return "Sell Stop";
      case OP_SELLLIMIT:
         return "Sell Limit";
     }
   return "";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FirstOkOrDefault(double a,double b)
  {
   if(ok(a))
      return a;
   if(ok(b))
      return b;
   return 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailSL(string symbol,int magic,double trailStopPips,double trailStartAt,double trailingStep=0,double stopTrailingAt=0,double trailRatio=0,double trailStartAtRatio=0,double buyTS=0,double sellTS=0,int candlesOffset=0,double pipsOffset=0,bool fifo=false)
  {
   double bid=MarketInfo(symbol,MODE_BID);
   double ask=MarketInfo(symbol,MODE_ASK);
   int digits=(int)MarketInfo(symbol,MODE_DIGITS);
   double pip=GetPip(symbol);
   trailingStep*=GetPip();
   if(trailingStep==0)
      trailingStep=GetPip();

   if(trailStopPips>0 || buyTS>0 || sellTS>0)
     {
      for(int i=OrdersTotal()-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS))
           {
            int startBar=iBarShift(OrderSymbol(),Period(),OrderOpenTime());
            if(startBar<candlesOffset)
               continue;

            double sl_buy=NormalizeDouble(bid-trailStopPips*pip,digits);
            double sl_sell=NormalizeDouble(ask+trailStopPips*pip,digits);

            if(trailRatio>0)
              {
               sl_buy=OrderOpenPrice()+(bid-OrderOpenPrice())*trailRatio;
               sl_sell=OrderOpenPrice()-(OrderOpenPrice()-ask)*trailRatio;
              }

            if(buyTS>0)
               sl_buy=buyTS;
            if(sellTS>0)
               sl_sell=sellTS;

            double min_sl_buy=NormalizeDouble(bid-pipsOffset*pip,digits);
            double min_sl_sell=NormalizeDouble(ask+pipsOffset*pip,digits);

            sl_buy=MathMin(min_sl_buy,sl_buy);
            sl_sell=MathMax(min_sl_sell,sl_sell);

            if(fifo)
               sl_buy=FifoAllowedSLTP(sl_buy,OrderSymbol(),OrderType(),StopType_SL);
            int sel=OrderSelect(i,SELECT_BY_POS);
            if(fifo)
               sl_sell=FifoAllowedSLTP(sl_sell,OrderSymbol(),OrderType(),StopType_SL);
            sel=OrderSelect(i,SELECT_BY_POS);

            double currentSL=0;
            if(OrderSymbol()==symbol && OrderMagicNumber()==magic)
              {
               // GET CURRENT SL
               if(_StealthSLandTP)
                 {
                  if(ObjectFind(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()))==0)
                    {
                     currentSL=ObjectGet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
                    }
                 }
               else
                 {
                  currentSL=OrderStopLoss();
                 }
               double currentProfitRatio=0;
               if(OrderOpenPrice()-currentSL!=0 && currentSL!=0)
                  currentProfitRatio=(bid-OrderOpenPrice())/(OrderOpenPrice()-currentSL);

               if(OrderType()==OP_SELL)
                  if(currentSL-OrderOpenPrice()>0 && currentSL!=0)
                     currentProfitRatio=(OrderOpenPrice()-ask)/(currentSL-OrderOpenPrice());

               if(trailStartAtRatio!=0 && currentProfitRatio<trailStartAtRatio)
                  continue;

               if(OrderType()==OP_BUY && bid-OrderOpenPrice()>=trailStartAt*pip && (stopTrailingAt==0 || OrderStopLoss()-OrderOpenPrice()<=stopTrailingAt*pip))
                 {
                  if(currentSL<Point || (sl_buy>=currentSL+trailingStep))
                    {
                     if(_StealthSLandTP)
                       {
                        SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(OrderTicket()),sl_buy,clrRed,2,"SL for #"+IntegerToString(OrderTicket()));
                       }
                     else
                       {
                        Print("Trail SL for BUY");
                        if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl_buy,OrderTakeProfit(),0))
                          {
                           Print(err_msg(GetLastError()));
                          }
                       }
                    }
                 }
               else
                  if(OrderType()==OP_SELL && OrderOpenPrice()-ask>=trailStartAt*pip && (stopTrailingAt==0 || OrderOpenPrice()-OrderStopLoss()<=stopTrailingAt*pip))
                    {
                     if(currentSL<Point || (sl_sell<=currentSL-trailingStep))
                       {
                        if(_StealthSLandTP)
                          {
                           SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(OrderTicket()),sl_sell,clrRed,2,"SL for #"+IntegerToString(OrderTicket()));
                          }
                        else
                          {
                           Print("Trail SL for SELL");
                           if(!OrderModify(OrderTicket(),OrderOpenPrice(),sl_sell,OrderTakeProfit(),0))
                             {
                              Print(err_msg(GetLastError()));
                             }
                          }
                       }
                    }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FifoAllowedSLTP(double price,string symbol,int type,StopType stopType)
  {
   double lowestSL=0;
   double highestSL=0;
   double lowestTP=0;
   double highestTP=0;

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      double orderSL=OrderStopLoss();
      double orderTP=OrderTakeProfit();

      if(_StealthSLandTP)
        {
         if(ObjectFind(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()))==0)
           {
            orderSL=ObjectGet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
           }
         if(ObjectFind(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket()))==0)
           {
            orderTP=ObjectGet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
           }
        }

      if(OrderSymbol()==symbol && OrderType()==type)
        {
         if(lowestSL==0 || orderSL<lowestSL)
           {
            lowestSL=orderSL;
           }
         if(highestSL==0 || orderSL>highestSL)
           {
            highestSL=orderSL;
           }
         if(lowestTP==0 || orderTP<lowestTP)
           {
            lowestTP=orderTP;
           }
         if(highestTP==0 || orderTP>highestTP)
           {
            highestTP=orderTP;
           }
        }
     }

   double bestSLTP=price;

   if(stopType==StopType_SL)
     {
      if(type==OP_BUY)
        {
         if(highestSL!=0)
            bestSLTP=MathMin(highestSL,price);
        }

      if(type==OP_SELL)
        {
         if(lowestSL!=0)
            bestSLTP=MathMax(lowestSL,price);
        }
     }
   if(stopType==StopType_TP)
     {
      if(type==OP_BUY)
        {
         if(lowestTP!=0)
            bestSLTP=MathMax(lowestTP,price);
        }

      if(type==OP_SELL)
        {
         if(highestTP!=0)
            bestSLTP=MathMin(highestTP,price);
        }

      if(type==OP_SELL && stopType==StopType_TP)
         Comment(highestTP);
     }

   return bestSLTP;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void JumpTrailSL(string symbol,int magic,double moveJumpingStop,double jumpingStopPips,double jumpingStartAt)
  {
   double bid=MarketInfo(symbol,MODE_BID);
   double ask=MarketInfo(symbol,MODE_ASK);
   int digits=(int)MarketInfo(symbol,MODE_DIGITS);
   double pip=GetPip(symbol);
   if(jumpingStopPips>0)
     {
      for(int i=OrdersTotal()-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS))
           {
            double currentSL=0;
            if(OrderSymbol()==symbol && OrderMagicNumber()==magic)
              {
               if(_StealthSLandTP)
                 {
                  if(ObjectFind(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()))==0)
                    {
                     currentSL=ObjectGet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
                    }
                 }
               else
                 {
                  currentSL=OrderStopLoss();
                 }

               double currentProfitDistance=bid-OrderOpenPrice();
               if(OrderType()==OP_SELL)
                  currentProfitDistance=OrderOpenPrice()-ask;

               int jumpingStopIncrements=(int)MathFloor(currentProfitDistance/(moveJumpingStop*pip));
               double jumpingProfitDistance=jumpingStopIncrements*moveJumpingStop*pip;

               double sl_buy=0;
               double sl_sell=0;
               if(jumpingStopIncrements>0)
                 {
                  sl_buy=NormalizeDouble(OrderOpenPrice()+jumpingProfitDistance-jumpingStopPips*pip,digits);
                  sl_sell=NormalizeDouble(OrderOpenPrice()-jumpingProfitDistance+jumpingStopPips*pip,digits);
                 }

               sl_buy=MathMin(Bid-MinDistance(),sl_buy);
               sl_sell=MathMax(Ask+MinDistance(),sl_sell);

               if(OrderType()==OP_BUY && bid-OrderOpenPrice()>=jumpingStartAt*pip)
                 {
                  if(sl_buy>currentSL && currentSL!=0 && sl_buy>0)
                    {
                     if(_StealthSLandTP)
                       {
                        SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(OrderTicket()),NormalizeDouble(sl_buy,Digits),clrRed,2,"SL for #"+IntegerToString(OrderTicket()));
                       }
                     else
                       {
                        if(!OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(sl_buy,Digits),OrderTakeProfit(),OrderExpiration()))
                          {
                           Print(err_msg(GetLastError()));
                          }
                       }
                    }
                 }
               else
                  if(OrderType()==OP_SELL && OrderOpenPrice()-ask>=jumpingStartAt*pip)
                    {
                     if(sl_sell<currentSL && currentSL!=0 && sl_sell>0)
                       {
                        if(_StealthSLandTP)
                          {
                           SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(OrderTicket()),NormalizeDouble(sl_sell,Digits),clrRed,2,"SL for #"+IntegerToString(OrderTicket()));
                          }
                        else
                          {
                           if(!OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(sl_sell,Digits),OrderTakeProfit(),OrderExpiration()))
                             {
                              Print(err_msg(GetLastError()));
                             }
                          }
                       }
                    }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetInfoBitFromVPOString(string data,string key)
  {
   string bits[];
   int bitsCount=StringSplit(data,',',bits);
   for(int i=0; i<bitsCount; i++)
     {
      bits[i]=StringTrimLeft(StringTrimRight(bits[i]));
      int comma=StringFind(bits[i],",",0);
      if(comma>=0)
        {
         bits[i]=StringSubstr(bits[i],0,comma)+StringSubstr(bits[i],comma+1);
        }
      if(StringFind(bits[i],key)==0)
         return StringSubstr(bits[i],StringLen(key));
     }

   return "";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GetOrderTypeFromVPOString(string data)
  {
   string s_type=GetInfoBitFromVPOString(data,"!");

   if(StringLen(s_type)>0)
     {
      return (int)StringToInteger(s_type);
     }
   return -1;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetSymbolFromVPOString(string data)
  {
   string bits[];
   int bitsCount=StringSplit(data,',',bits);

   return bits[0];
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckEquityMM(string symbol="",int magic=-1,double maxProfit=0,double maxLoss=0,double maxProfitPercent=0,double maxLossPercent=0,bool usePips=false)
  {
   bool closeAll=false;

   double currentProfit=CurrentProfit(symbol,magic,-1,usePips);
   double currentProfitPercent=currentProfit/AccountBalance()*100;

   string scope="account";
   if(magic>=0)
      scope+=" with magic #="+(string)magic;
   if(StringLen(symbol)>0)
      scope=symbol;

   if(maxProfit!=0 && currentProfit>0 && currentProfit>maxProfit)
     {
      TriggerAlerts("Max Equity Profit exceeded ("+(string) currentProfit+") - close all orders on "+scope);
      closeAll=true;
     }
   if(maxLoss!=0 && currentProfit<0 && MathAbs(currentProfit)>maxLoss)
     {
      TriggerAlerts("Max Equity Loss exceeded ("+(string) currentProfit+") - close all orders on "+scope);
      closeAll=true;
     }
   if(maxProfitPercent!=0 && currentProfitPercent>0 && currentProfitPercent>maxProfitPercent)
     {
      TriggerAlerts("Max Equity Profit Percentage exceeded ("+(string) currentProfitPercent+") - close all orders on "+scope);
      closeAll=true;
     }
   if(maxLossPercent!=0 && currentProfitPercent<0 && MathAbs(currentProfitPercent)>maxLossPercent)
     {
      TriggerAlerts("Max Equity Loss Percentage exceeded ("+(string) currentProfitPercent+") - close all orders on "+scope);
      closeAll=true;
     }

   if(closeAll)
     {
      for(int i=0; i<=OrdersTotal()-1; i++)
        {
         ResetLastError();
         if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
            return true;
           }

         if(OrderSymbol()==symbol || StringLen(symbol)==0)
           {
            if(OrderMagicNumber()==magic || magic==0)
              {
               double pip=GetPip(OrderSymbol());
               double price=MarketInfo(OrderSymbol(),MODE_BID);
               if(OrderType()==OP_BUY)
                  price=MarketInfo(OrderSymbol(),MODE_BID);
               if(OrderType()==OP_SELL)
                  price=MarketInfo(OrderSymbol(),MODE_ASK);
               if(price!=0)
                 {
                  if(OrderType()!=OP_BUY && OrderType()!=OP_SELL)
                     OrderDelete(OrderTicket());

                  if(!OrderClose(OrderTicket(),OrderLots(),price,(int)(_MaxSlippage*pip/Point())))
                    {
                     Print("Failed to close order, try deleting it, in "+__FUNCTION__+", error="+(string)GetLastError());
                     return true;
                    }
                  else
                    {
                     i--;
                    }
                 }
              }
           }
        }
     }

   return closeAll;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckPipsMM(string symbol="",int magic=-1,double maxProfit=0,double maxLoss=0,double maxProfitPercent=0,double maxLossPercent=0)
  {
   bool closeAll=false;

   double currentProfit=CurrentProfit(symbol,magic,-1,true);

   string scope="account";
   if(magic>=0)
      scope+=" with magic #="+(string)magic;
   if(StringLen(symbol)>0)
      scope=symbol;

   if(maxProfit!=0 && currentProfit>0 && currentProfit>maxProfit)
     {
      TriggerAlerts("Max Profit Pips exceeded ("+(string) currentProfit+") - close all orders on "+scope);
      closeAll=true;
     }
   if(maxLoss!=0 && currentProfit<0 && MathAbs(currentProfit)>maxLoss)
     {
      TriggerAlerts("Max Loss Pips exceeded ("+(string) currentProfit+") - close all orders on "+scope);
      closeAll=true;
     }
   if(closeAll)
     {
      for(int i=OrdersTotal()-1; i>=0; i--)
        {
         ResetLastError();
         if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
            return true;
           }

         if(OrderSymbol()==symbol || StringLen(symbol)==0)
           {
            if(OrderMagicNumber()==magic || StringLen(symbol)==0)
              {
               double pip=GetPip(OrderSymbol());
               double price=0;
               if(OrderType()==OP_BUY)
                  price=MarketInfo(OrderSymbol(),MODE_BID);
               if(OrderType()==OP_SELL)
                  price=MarketInfo(OrderSymbol(),MODE_ASK);
               if(price!=0)
                 {
                  if(!OrderClose(OrderTicket(),OrderLots(),price,(int)(_MaxSlippage*pip/Point())))
                    {
                     Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
                     return true;
                    }
                 }
              }
           }
        }
     }

   return closeAll;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CurrentProfit(string symbol=NULL,int magic=-1,int type=-1,bool usePips=false,bool weighInLots=false)
  {
   double profit=0;
   double lots=0;
   int total=OrdersTotal();
   for(int b=total-1; b>=0; b--)
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderMagicNumber()==magic || magic<0)
           {
            if(OrderSymbol()==symbol || StringLen(symbol)==0)
              {
               if(OrderType()==type || type==-1)
                 {
                  if(usePips)
                    {
                     double multiplier=1;
                     if(weighInLots)
                        multiplier=OrderLots();
                     if(OrderType()==OP_BUY)
                       {
                        profit+=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice())/GetPip(OrderSymbol())*multiplier;
                        lots+=OrderLots();
                       }
                     if(OrderType()==OP_SELL)
                       {
                        profit+=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK))/GetPip(OrderSymbol())*multiplier;
                        lots+=OrderLots();
                       }
                    }
                  else
                    {
                     profit+=OrderProfit();
                    }
                 }
              }
           }
        }
     }
   return  NormalizeDouble(profit,2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CurrentLots(string symbol=NULL,int magic=-1,int type=-1)
  {
   double lots=0;
   int total=OrdersTotal();
   for(int b=total-1; b>=0; b--)
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderMagicNumber()==magic || magic<0)
           {
            if(OrderSymbol()==symbol || StringLen(symbol)==0)
              {
               if(OrderType()==type || type==-1)
                 {
                  if(OrderType()==OP_BUY)
                    {
                     lots+=OrderLots();
                    }
                  if(OrderType()==OP_SELL)
                    {
                     lots+=OrderLots();
                    }
                 }
              }
           }
        }
     }
   return  NormalizeDouble(lots,2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CurrentRisk(string symbol=NULL,int magic=-1,int type=-1)
  {
   double risk=0;
   int total=OrdersTotal();
   for(int b=total-1; b>=0; b--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderMagicNumber()==magic || magic<0)
           {
            if(OrderSymbol()==symbol || StringLen(symbol)==0)
              {
               if(OrderType()==type || type<0)
                 {
                  double sl=OrderStopLoss();
                  if(_StealthSLandTP)
                    {
                     sl=ObjectGet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
                    }

                  double diff=OrderOpenPrice()-sl;
                  if(OrderType()==OP_SELL)
                     diff=sl-OrderOpenPrice();

                  double pipValue=MarketInfo(symbol,MODE_TICKVALUE);
                  double point=MarketInfo(symbol,MODE_POINT);

                  risk+=OrderLots()*(diff/point)*pipValue;
                 }
              }
           }
        }
     }

   return  NormalizeDouble(risk,2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CurrentVirtualPendingsRisk(string symbol=NULL,int magic=-1,int type=-1)
  {
   double risk=0;
   for(int i=GlobalVariablesTotal()-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      string data=GlobalVariableName(i);

      if(StringFind(data,"sl=")>=0 || StringFind(data,"tp=")>=0)
        {
         double level=(double)StringToDouble(GetInfoBitFromVPOString(data,"@"));
         string _symbol=GetSymbolFromVPOString(data);
         int _type=GetOrderTypeFromVPOString(data);
         double pip=GetPip(symbol);
         int _magic=(int)StringToInteger(GetInfoBitFromVPOString(data,"#"));
         double sl=(double)StringToDouble(GetInfoBitFromVPOString(data,"sl="));
         double volume=NormalizeDouble(StringToDouble(GetInfoBitFromVPOString(data,"*")),2);
         int count=(int)GlobalVariableGet(data);

         if(_magic==magic || magic<0)
           {
            if(_symbol==symbol || StringLen(symbol)==0)
              {
               if(_type==type || type<0)
                 {
                  double diff=level-sl;
                  if(_type==OP_SELLSTOP || _type==OP_SELLLIMIT)
                     diff=sl-level;

                  double pipValue=MarketInfo(_symbol,MODE_TICKVALUE);
                  double point=MarketInfo(_symbol,MODE_POINT);

                  risk+=volume*(diff/point)*pipValue;
                 }
              }
           }
        }
     }
   return  NormalizeDouble(risk,2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CurrentProfitPips(string symbol=NULL,int magic=-1,int type=-1,bool usePoints=false)
  {
   double profitPips=0;
   double profitDistance=0;
   int total=OrdersTotal();
   for(int b=total-1; b>=0; b--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderMagicNumber()==magic || magic<0)
           {
            if(OrderSymbol()==symbol || StringLen(symbol)==0)
              {
               if(OrderType()==type || (type==-1 && (OrderType()==OP_BUY || OrderType()==OP_SELL)))
                 {
                  double pip=GetPip(OrderSymbol());
                  if(usePoints)
                     pip=MarketInfo(OrderSymbol(),MODE_POINT);
                  if(OrderType()==OP_BUY)
                    {
                     profitDistance+=(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice());
                    }
                  if(OrderType()==OP_SELL)
                    {
                     profitDistance+=(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK));
                    }

                  profitPips=profitDistance/pip;
                 }
              }
           }
        }
     }
   return  profitPips;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool OnceADayAtTime(int hour,int minute,datetime timeToCompare=0)
  {
   bool thatMoment=false;

   if(timeToCompare==0)
      timeToCompare=TimeCurrent();

   int currHour=TimeHour(timeToCompare);
   int currMinute=TimeMinute(timeToCompare);
   int currSeconds=TimeSeconds(timeToCompare);

   static bool firstTimeToday=true;
   if(currHour>hour ||
      (currHour==hour && currMinute>=minute) ||
      (currHour==hour && (currMinute==minute && currSeconds>=0)))
     {
      if(firstTimeToday)
        {
         thatMoment=true;
        }
      firstTimeToday=false;
     }
   else
     {
      firstTimeToday=true;
     }

   return thatMoment;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime GetNextDateByHourAndMin(int h,int _m,datetime dateToCompare)
  {
   datetime currTime=dateToCompare;
   datetime res=dateToCompare;

   if(TimeHour(currTime)>h || (TimeHour(currTime)==h && TimeMinute(currTime)>=_m))
     {
      res = res+24*60*60;
      res = res+(h-TimeHour(currTime))*60*60+(_m-TimeMinute(currTime))*60;
     }
   else
     {
      res=res+(h-TimeHour(currTime))*60*60+(_m-TimeMinute(currTime))*60;
     }
   return (res);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckVirtualStops(string symbol,int magic=-1,bool antiSpikeMode=false,bool useBidOnly=false,bool showTicket=true)
  {
   double bid=MarketInfo(symbol,MODE_BID);
   double ask=MarketInfo(symbol,MODE_ASK);
   if(useBidOnly)
      ask=bid;
   double pip=GetPip(symbol);
   if(IsOptimization())
      return;

   for(int i=0; i<=OrdersTotal()-1; i++)
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return;
        }
      if(OrderSymbol()==symbol)
        {
         if(magic<0 || OrderMagicNumber()==magic)
           {

            // Check if Virtual TP or SL have been hit, and if so, Close the orders
            double sl=ObjectGet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
            double tp=ObjectGet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
            datetime  time=Time[3];
            ObjectSet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket())+"pricetxt",OBJPROP_PRICE1,sl);
            ObjectSet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket())+"pricetxt",OBJPROP_PRICE1,tp);

            double pipValue=MarketInfo(symbol,MODE_TICKVALUE);
            double point=MarketInfo(symbol,MODE_POINT);

            double profit=OrderLots()*((MathAbs(sl-OrderOpenPrice())/point)*pipValue);
            double loss=OrderLots()*((MathAbs(tp-OrderOpenPrice())/point)*pipValue);
            ObjectSetString(0,_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket())+"pricetxt",OBJPROP_TEXT,"profit=$"+DoubleToString(loss,2));
            ObjectSetString(0,_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket())+"pricetxt",OBJPROP_TEXT,"loss=$"+DoubleToString(profit,2));
            //    ObjectSet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket())+"pricetxt",OBJPROP_TIME1,time);
            //    ObjectSet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket())+"pricetxt",OBJPROP_TIME1,time);

            double lastLow=0;
            double lastHigh=0;
            if(antiSpikeMode)
              {
               int startBar=iBarShift(OrderSymbol(),Period(),OrderOpenTime());
               if(startBar==0)
                  continue;

               lastLow=iLow(OrderSymbol(),Period(),1);
               lastHigh=iHigh(OrderSymbol(),Period(),1);
              }

            if(OrderType()==OP_BUY && ((bid<=sl && sl>0 && (lastLow<sl || lastLow==0)) || (bid>=tp && tp>0)))
              {
               ResetLastError();
               if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(symbol,MODE_BID),(int)(50*pip/Point())))
                 {
                  Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
                  return;
                 }
               else
                 {
                  i--;
                 }
               Print(OrderSymbol()+" #"+IntegerToString(OrderTicket())+" closed at "+DoubleToString(MarketInfo(symbol,MODE_BID),Digits));
              }

            if(OrderType()==OP_SELL && ((ask>=sl && sl>0 && (lastHigh>sl || lastHigh==0)) || (ask<=tp && tp>0)))
              {
               ResetLastError();
               if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(symbol,MODE_ASK),(int)(50*pip/Point())))
                 {
                  Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
                  return;
                 }
               else
                 {
                  i--;
                 }
               Print(OrderSymbol()+" #"+IntegerToString(OrderTicket())+" closed at "+DoubleToString(MarketInfo(symbol,MODE_ASK),Digits));
              }
           }
        }
     }

   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         Print("Failed to select history orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return;
        }
      if(OrderSymbol()==symbol)
        {
         if(magic<0 || OrderMagicNumber()==magic)
           {
            string ticket=IntegerToString(OrderTicket());

            if(ObjectFind(_ObjPrefix+_VS_Prefix+"SL"+ticket)<0 && ObjectFind(_ObjPrefix+_VS_Prefix+"TP"+ticket)<0)
               break;
            ObjectDelete(0,_ObjPrefix+_VS_Prefix+"SL"+ticket);
            ObjectDelete(0,_ObjPrefix+_VS_Prefix+"TP"+ticket);
            ObjectDelete(0,_ObjPrefix+_VS_Prefix+"SL"+ticket+"pricetxt");
            ObjectDelete(0,_ObjPrefix+_VS_Prefix+"TP"+ticket+"pricetxt");
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void MoveToBE(string symbol,int magicNumber,double pips,double lockPips,double minProfitPercentage=0,double minProfitRatio=0,bool fifo=false,int orderType=-1)
  {
   if(pips==0 && minProfitPercentage==0 && minProfitRatio==0)
      return;

   double bid=MarketInfo(symbol,MODE_BID);
   double ask=MarketInfo(symbol,MODE_ASK);
   double pip=GetPip(symbol);
   for(int i=OrdersTotal()-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      double currentSL=0;
      double currentTP=0;
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return;
        }
      if(OrderSymbol()==symbol && OrderMagicNumber()==magicNumber && (orderType==-1 || OrderType()==orderType))
        {
         if(_StealthSLandTP)
           {
            if(ObjectFind(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()))==0)
              {
               currentSL=ObjectGet(_ObjPrefix+_VS_Prefix+"SL"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
               currentTP=ObjectGet(_ObjPrefix+_VS_Prefix+"TP"+IntegerToString(OrderTicket()),OBJPROP_PRICE1);
              }
           }
         else
           {
            currentSL=OrderStopLoss();
            currentTP=OrderTakeProfit();
           }

         double currentProfitPerc=0;
         double currentProfitRatio=0;
         if(currentTP-OrderOpenPrice()!=0 && currentSL!=0 && currentTP!=0)
            currentProfitPerc=((bid-OrderOpenPrice())/(currentTP-OrderOpenPrice()))*100;
         if(OrderOpenPrice()-currentSL!=0 && currentSL!=0)
            currentProfitRatio=(bid-OrderOpenPrice())/(OrderOpenPrice()-currentSL);

         if(OrderType()==OP_SELL)
           {
            currentProfitPerc=0;
            currentProfitRatio=0;
            if(OrderOpenPrice()-currentTP!=0 && currentSL!=0 && currentTP!=0)
               currentProfitPerc=((OrderOpenPrice()-ask)/(OrderOpenPrice()-currentTP))*100;
            if(currentSL-OrderOpenPrice()>0 && currentSL!=0)
               currentProfitRatio=(OrderOpenPrice()-ask)/(currentSL-OrderOpenPrice());
           }

         bool moveToBE=false;

         double be=OrderOpenPrice()+lockPips*pip;
         be=MathMin(Bid-MinDistance(),be);
         if(OrderType()==OP_SELL)
           {
            be=OrderOpenPrice()-lockPips*pip;
            be=MathMax(Ask+MinDistance(),be);
           }

         int digits=(int)MarketInfo(OrderSymbol(),MODE_DIGITS);

         be=NormalizeDouble(be,digits);
         if(fifo)
            be=FifoAllowedSLTP(be,OrderSymbol(),OrderType(),StopType_SL);

         int sel=OrderSelect(i,SELECT_BY_POS);

         if(OrderType()==OP_BUY && bid-OrderOpenPrice()>=pips*pip && currentProfitPerc>=minProfitPercentage && currentProfitRatio>=minProfitRatio)
           {
            if(NormalizeDouble(currentSL,digits)<be || currentSL==0)
              {
               // Only move SL up for BUY
               moveToBE=true;
               Print("Move BUY order to BE");
              }
           }
         if(OrderType()==OP_SELL && OrderOpenPrice()-ask>=pips*pip && currentProfitPerc>=minProfitPercentage && currentProfitRatio>=minProfitRatio)
           {
            if(NormalizeDouble(currentSL,digits)>be || currentSL==0)
              {
               // Only move SL down for SELL
               moveToBE=true;
               Print("Moved SELL order to BE");
              }
           }
         if(moveToBE)
           {
            if(_StealthSLandTP)
              {
               SetHorizontalLine(_VS_Prefix+"SL"+IntegerToString(OrderTicket()),be,clrRed,2);
              }
            else
              {
               ResetLastError();
               if(!OrderModify(OrderTicket(),OrderOpenPrice(),be,OrderTakeProfit(),0))
                 {
                  Print("Failed to modify order in "+__FUNCTION__+", error="+(string)GetLastError());
                  return;
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string PrettyTime(string s_time)
  {
// without year
   return StringSubstr(s_time,5,0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeletePendingOrders(string symbol="",int magic=-1,int type=-1,double min=0,double max=0)
  {
   if(_StealthPendingOrders)
     {
      DeleteVirtualPendings(symbol,magic,type);
      return;
     }

   for(int i=OrdersTotal()-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return;
        }
      if((OrderSymbol()==symbol || StringLen(symbol)==0)
         && OrderType()!=OP_SELL && OrderType()!=OP_BUY)
        {
         if(OrderMagicNumber()==magic || magic<0)
           {
            if(OrderType()==type || type<0)
               if(min==0 || OrderOpenPrice()>=min)
                  if(max==0 || OrderOpenPrice()<=max)
                    {
                     ResetLastError();
                     if(!OrderDelete(OrderTicket()))
                       {
                        Print("Failed to delete pending order in "+__FUNCTION__+", error="+(string)GetLastError());
                        return;
                       }
                     else
                       {
                        if(_StealthPendingOrders)
                          {
                           ObjectDelete(0,_VS_Prefix+"SL"+IntegerToString(OrderTicket()));
                           ObjectDelete(0,_VS_Prefix+"TP"+IntegerToString(OrderTicket()));
                          }
                       }
                    }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseBuyOrders(string symbol=NULL,int magicNumber=-1,string comment="",double closePercentage=100)
  {
   int total=OrdersTotal();
   for(int b=0; b<=total-1; b++)
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==magicNumber || magicNumber==-1) && (OrderSymbol()==symbol || StringLen(symbol)==0)
            && (StringLen(comment)==0 || StringFind(OrderComment(),comment)>=0) && OrderType()==OP_BUY)
           {
            ResetLastError();
            color arrowColor=clrSteelBlue;
            if(!_ShowArrows)
               arrowColor=clrNONE;
            if(!OrderClose(OrderTicket(),OrderLots()*closePercentage/100,MarketInfo(OrderSymbol(),MODE_BID),10,arrowColor))
              {
               Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
               return;
              }
            else
              {
               total=OrdersTotal();
               b--;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseSellOrders(string symbol=NULL,int magicNumber=-1,string comment="",double closePercentage=100)
  {
   int total=OrdersTotal();
   int closed=0;
   for(int b=0; b<=total-1; b++)
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==magicNumber || magicNumber==-1) && (OrderSymbol()==symbol || StringLen(symbol)==0)
            && (StringLen(comment)==0 || StringFind(OrderComment(),comment)>=0) && OrderType()==OP_SELL)
           {
            ResetLastError();
            color arrowColor=clrSteelBlue;
            if(!_ShowArrows)
               arrowColor=clrNONE;
            if(!OrderClose(OrderTicket(),OrderLots()*closePercentage/100,MarketInfo(OrderSymbol(),MODE_ASK),10,arrowColor))
              {
               Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
               return;
              }
            else
              {
               total=OrdersTotal();
               b--;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double  CloseAllOrders(string symbol=NULL,int magicNumber=-1,int type=-1,string comment=NULL)
  {
   int total=OrdersTotal();
   double profit=0;
   for(int b=0; b<=total-1; b++)
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==magicNumber || magicNumber==-1) &&
            (OrderSymbol()==symbol || StringLen(symbol)==0) &&
            (OrderType()==type || type<0) &&
            (StringLen(comment)==0 || StringFind(OrderComment(),comment)>=0))
           {
            ResetLastError();
            color arrowColor=clrSteelBlue;
            if(!_ShowArrows)
               arrowColor=clrNONE;
            double closePrice=MarketInfo(OrderSymbol(),MODE_ASK);
            if(OrderType()==OP_BUY)
               closePrice=MarketInfo(OrderSymbol(),MODE_BID);

            if(OrderType()!=OP_BUY && OrderType()!=OP_SELL)
              {
               continue;
              }

            if(!OrderClose(OrderTicket(),OrderLots(),closePrice,10,arrowColor))
              {
               Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
               return 0;
              }
            else
              {
               total=OrdersTotal();
               b--;
               profit+=OrderProfit()+OrderCommission()+OrderSwap();
              }
           }
        }
     }

   return profit;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseProfitableOrders(string symbol=NULL,int magicNumber=-1,int type=-1)
  {
   int total=OrdersTotal();
   for(int b=0; b<=total-1; b++)
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES) && OrderProfit()>=0)
        {
         if((OrderMagicNumber()==magicNumber || magicNumber==-1) && (OrderSymbol()==symbol || StringLen(symbol)==0) && (OrderType()==type || type<0))
           {
            ResetLastError();
            color arrowColor=clrSteelBlue;
            if(!_ShowArrows)
               arrowColor=clrNONE;
            double closePrice=MarketInfo(OrderSymbol(),MODE_ASK);
            if(type==OP_BUY)
               closePrice=MarketInfo(OrderSymbol(),MODE_BID);
            if(!OrderClose(OrderTicket(),OrderLots(),closePrice,10,arrowColor))
              {
               Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
               return;
              }
            else
              {
               total=OrdersTotal();
               b--;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool allowBuy=true;
bool allowSell=true;
//+------------------------------------------------------------------+
//|                                                                  |
//+--------------------------------------------------------c----------+
bool CheckVirtualPendingOrders(string _symbol=NULL,int _type=-1,bool shortLabel=true,double belowLevel=0,double aboveLevel=0,string comment="",bool logs=false)
  {
   for(int i=GlobalVariablesTotal()-1; i>=0; i--)
     {
      string data=GlobalVariableName(i);

      if(StringFind(data,"sl=")>=0 || StringFind(data,"tp=")>=0)
        {
         double level=(double)StringToDouble(GetInfoBitFromVPOString(data,"@"));
         string symbol=GetSymbolFromVPOString(data);
         if(_symbol!=NULL && symbol!=_symbol)
            continue;

         int type=GetOrderTypeFromVPOString(data);
         if(type!=_type && _type>=0)
            continue;


         double pip=GetPip(symbol);
         int magic=(int)StringToInteger(GetInfoBitFromVPOString(data,"#"));
         double sl=(double)StringToDouble(GetInfoBitFromVPOString(data,"sl="));
         double tp=(double)StringToDouble(GetInfoBitFromVPOString(data,"tp="));
         double volume=NormalizeDouble(StringToDouble(GetInfoBitFromVPOString(data,"*")),2);
         int count=(int)GlobalVariableGet(data);

         double price=MarketInfo(symbol,MODE_ASK);
         if(type==OP_SELLSTOP || type==OP_SELLLIMIT)
            price=MarketInfo(symbol,MODE_BID);

         double priceDiff=price-level;
         if(type==OP_BUYLIMIT || type==OP_SELLSTOP)
            priceDiff=level-price;

         if(logs)
            Print("Checking virtual "+TypeName(type)+", currently at "+DoubleToStr((-1)*priceDiff/GetPip(_symbol),1)+" pips from market price");

         bool minMaxLimitCompliant=(price>=aboveLevel || aboveLevel==0) && (price<=belowLevel || belowLevel==0);

         if(priceDiff>=0 && // price crossed the line
            (priceDiff<=_MaxSlippage*pip || _MaxSlippage==0))
           {
            if(!minMaxLimitCompliant)
              {
               Print("Won't trigger virtual pending order at "+DoubleToStr(price,Digits)+", not below/above "+
                     DoubleToStr(belowLevel,Digits)+"/"+DoubleToStr(aboveLevel,Digits));
              }
            if(count==1)
               Print("Virtual "+TypeName(type)+" is triggered, open market order");
            else
               Print("Virtual "+TypeName(type)+"s"+" are triggered, open market orders");

            for(int iCount=0; iCount<count; iCount++)
              {
               // price just crossed level, open order
               if(type==OP_BUYSTOP || type==OP_BUYLIMIT)
                 {
                  DeleteVirtualPending(data);
                  if(allowBuy)
                    {
                     return  OpenBuy(symbol,volume,sl,tp,comment,magic)>0;
                    }
                  else
                    {
                     Print("Buy not allowed due to extra condition, V pending order was ignored");
                     return false;
                    }
                 }
               if(type==OP_SELLSTOP || type==OP_SELLLIMIT)
                 {
                  DeleteVirtualPending(data);
                  if(allowSell)
                    {
                     return  OpenSell(symbol,volume,sl,tp,comment,magic)>0;
                    }
                  else
                    {
                     Print("Sell not allowed due to extra condition, V pending order was ignored");
                     return false;
                    }
                 }
              }
            DeleteVirtualPending(data);
           }
         if(priceDiff<0)
           {
            // level not crossed yet, draw a line if on current symbol
            if(symbol==Symbol())
              {
               color clr=clrLimeGreen;
               if(type==OP_SELLSTOP || type==OP_SELLLIMIT)
                  clr=clrTomato;
               string label=TypeName(type)+" @"+(string)level+", "+(string)volume+" lots, sl="+(string)sl+", tp="+(string)tp;
               if(shortLabel)
                  label=DoubleToStr(volume,2)+" lots"; //  TypeName(type)+", "+
               if(count>1)
                  label=(string)count+" x "+label+" ("+DoubleToStr(volume,2)+" lots)";
               if(ObjectFind(0,_ObjPrefix+"vp @"+(string)level+" tp="+(string)tp+" sl="+(string)sl)<0)
                  SetHorizontalLine("vp @"+(string)level+" tp="+(string)tp+" sl="+(string)sl,level,clr,2,label);
              }
           }

         if(_MaxSlippage!=0 && priceDiff>_MaxSlippage*pip)
           {
            // missed the train, delete the pending
            Print(data+"missed the train, delete the pending");
            DeleteVirtualPending(data);
           }
        }
     }

   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CloseAllIfOneHitsTP(string symbol,int magic,int type=-1)
  {
   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         Print("Failed to select history orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return false;
        }
      if(OrderSymbol()==symbol && OrderMagicNumber()==magic)
        {
         if(OrderType()==type || type<0)
           {
            if(TimeCurrent()-OrderCloseTime()<5)
              {
               // recent
               if(MathAbs(OrderClosePrice()-OrderStopLoss())>MathAbs(OrderClosePrice()-OrderTakeProfit()))
                 {
                  if(type==OP_BUY || type==-1)
                     CloseBuyOrders(Symbol(),magic);
                  if(type==OP_SELL || type==-1)
                     CloseSellOrders(Symbol(),magic);
                  DeletePendingOrders(Symbol(),magic,type);
                  DeleteVirtualPendings(Symbol(),magic,type);
                  return true;
                 }
              }
            else
              {
               return false;
              }
           }
        }
     }
   return  false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string OrderInfo(string templateText)
  {
   string orderInfo=templateText;

   StringReplace(orderInfo,"[SYMBOL]",(string)_selectedSymbol);
   StringReplace(orderInfo,"[SL]",(string)_selectedSL);
   StringReplace(orderInfo,"[TP]",(string)_selectedTP);
   StringReplace(orderInfo,"[TICKET]",(string)_selectedTicket);
   StringReplace(orderInfo,"[OPEN PRICE]",(string)_selectedOpenPrice);
   StringReplace(orderInfo,"[OPEN PRICE]",(string)_selectedClosePrice);
   StringReplace(orderInfo,"[VOLUME]",(string)_selectedVolume);
   StringReplace(orderInfo,"[OPEN TIME]",(string)_selectedOpenTime);
   StringReplace(orderInfo,"[CLOSE TIME]",(string)_selectedCloseTime);
   StringReplace(orderInfo,"[TYPE]",_selectedOrderTypeAsString());
   double closePrice=_selectedClosePrice;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(_selectedType==OP_BUY)
     {
      if(closePrice==0)
         closePrice=MarketInfo(_selectedSymbol,MODE_BID);
      StringReplace(orderInfo,"[PROFIT PIPS]",DoubleToStr((closePrice-_selectedOpenPrice)/GetPip(_selectedSymbol),1));
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(_selectedType==OP_SELL)
     {
      if(closePrice==0)
         closePrice=MarketInfo(_selectedSymbol,MODE_ASK);
      StringReplace(orderInfo,"[PROFIT PIPS]",DoubleToStr((_selectedOpenPrice-closePrice)/GetPip(_selectedSymbol),1));
     }
   StringReplace(orderInfo,"[PROFIT USD]",(string)_selectedProfit);

   return orderInfo;
  }//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string _selectedOrderTypeAsString()
  {
   if(_selectedType==OP_BUY)
     {
      return "BUY";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(_selectedType==OP_SELL)
     {
      return "SELL";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(_selectedType==OP_BUYSTOP)
     {
      return "BUY STOP";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(_selectedType==OP_SELLSTOP)
     {
      return "SELL STOP";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(_selectedType==OP_BUYLIMIT)
     {
      return "BUY LIMIT";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(_selectedType==OP_SELLLIMIT)
     {
      return "SELL LIMIT";
     }

   return "Order (type="+string(_selectedType)+")";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int _selectedTicket;
int _selectedType;
int _selectedMagicNumber;
double _selectedOpenPrice;
double _selectedClosePrice;
double _selectedVolume;
double _selectedRiskedPercentage;
double _selectedRiskedAmount;
datetime _selectedOpenTime;
datetime _selectedCloseTime;
double _selectedProfit;
double _selectedProfitPips;
double _selectedSwap;
double _selectedCommission;
double _selectedSL;
double _selectedTP;
string _selectedSymbol;
string _selectedComment;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetSelectedOrderFields()
  {
   double tickSize=Point(); // MarketInfo(Symbol(),MODE_TICKSIZE);
   double tickValue=MarketInfo(Symbol(),MODE_TICKVALUE);
   _selectedSymbol=OrderSymbol();
   _selectedTicket=OrderTicket();
   _selectedType=OrderType();
   _selectedOpenPrice=OrderOpenPrice();
   _selectedVolume=OrderLots();
   _selectedOpenTime=OrderOpenTime();
   _selectedCloseTime=OrderCloseTime();
   _selectedProfit=OrderProfit();
   _selectedSL=OrderStopLoss();
   _selectedTP=OrderTakeProfit();
   _selectedProfitPips=(OrderProfit()/MathMax(MathAbs(OrderProfit()),0.0001))*MathAbs(OrderClosePrice()-OrderOpenPrice())/GetPip(OrderSymbol());
   _selectedRiskedAmount=(MathAbs(_selectedOpenPrice-_selectedSL)/tickSize)*tickValue*OrderLots();
   _selectedRiskedPercentage=_selectedRiskedAmount/AccountBalance()*100;
   _selectedSwap=OrderSwap();
   _selectedCommission=OrderCommission();
   _selectedClosePrice=OrderClosePrice();
   _selectedComment=OrderComment();
   _selectedMagicNumber=OrderMagicNumber();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool GetLastOpenedOrder(string symbol,int magicNumber,int type=-1,bool lookInHistory=true)
  {
   _selectedOpenTime=0;
   _selectedTicket=0;
   for(int i=OrdersTotal()-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return false;
        }
      if(OrderSymbol()==symbol && OrderMagicNumber()==magicNumber)
         if(OrderType()==type || type==-1)
            if(OrderOpenTime()>_selectedOpenTime || _selectedTicket==0)
              {
               SetSelectedOrderFields();
              }
     }

   if(_selectedTicket==0 && lookInHistory)
     {
      for(int i=OrdersHistoryTotal()-1; i>=0; i--)
        {
         ResetLastError();
         if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
           {
            Print("Failed to select history orders in "+__FUNCTION__+", error="+(string)GetLastError());
            return false;
           }
         if(OrderSymbol()==symbol && OrderMagicNumber()==magicNumber)
            if(OrderType()==type || type==-1)
               if(OrderOpenTime()>_selectedOpenTime || _selectedTicket==0)
                 {
                  SetSelectedOrderFields();
                  // break;
                 }
        }
     }
   return (_selectedTicket!=0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool PositionJustOpened(string symbol="",int magic=-1,int type=-1,bool useGlobalTimeStamp=false)
  {
   static int lastTicket=0;
   static datetime lastOpen=0;

   if(useGlobalTimeStamp)
      lastOpen=(int)GlobalVariableGet("PositionJustOpenedLastTime");

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS))
        {
         Print("Failed to select orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return false;
        }
      if(OrderSymbol()==symbol || StringLen(symbol)==0)
         if(magic==-1 || OrderMagicNumber()==magic)
           {
            if(OrderTicket()!=lastTicket && OrderOpenTime()>=lastOpen)
              {
               // recent
               if(type==OrderType() || (type==-1 && (OrderType()==OP_BUY || OrderType()==OP_SELL)))
                 {
                  lastTicket=OrderTicket();
                  lastOpen=OrderOpenTime();

                  if(useGlobalTimeStamp)
                     GlobalVariableSet("PositionJustOpenedLastTime",(int)lastOpen);

                  SetSelectedOrderFields();
                  return true;
                 }
              }
            else
              {
               return false;
              }
           }
     }
   return  false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool OrderJustClosed(string symbol="",int magic=-1,int type=-1,bool useGlobalTimeStamp=false,bool ignorePartialCloses=false)
  {
   static int lastTicket=0;
   static datetime lastClose=0;

   static int lastHistoryCount=0;
   if(OrdersHistoryTotal()<=lastHistoryCount)
      return false;
   lastHistoryCount=OrdersHistoryTotal();

   if(useGlobalTimeStamp)
      lastClose=(int)GlobalVariableGet("PClosedLastTime"+Symbol()+timeFrameToString(Period()));

   string lastOpenedComment="";
   int newTicket=0;
   if(ignorePartialCloses)
     {
      int sel=OrderSelect(OrdersTotal()-1,SELECT_BY_POS);
      lastOpenedComment=OrderComment();
      newTicket=OrderTicket();
     }

   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         Print("Failed to select history orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return false;
        }
      if(OrderSymbol()==symbol || StringLen(symbol)==0)
        {
         if(magic==-1 || OrderMagicNumber()==magic)
           {
            if(OrderTicket()!=lastTicket && OrderCloseTime()>=lastClose && OrderCloseTime()>=TimeCurrent()-Period()*60*200)
              {
               // recent
               if(type==OrderType() || (type==-1))
                 {
                  if(ignorePartialCloses && StringFind(lastOpenedComment,(string)OrderTicket())>=0)
                    {
                     int initialTicket=OrderTicket();
                     if(GlobalVariableCheck("partial"+(string)OrderTicket()))
                       {
                        initialTicket=(int)GlobalVariableGet("partial"+(string)OrderTicket());
                        GlobalVariableDel("partial"+(string)OrderTicket());
                       }
                     GlobalVariableSet("partial"+(string)newTicket,initialTicket);
                    }

                  lastTicket=OrderTicket();
                  lastClose=OrderCloseTime();

                  if(useGlobalTimeStamp)
                     GlobalVariableSet("PClosedLastTime"+Symbol()+timeFrameToString(Period()),(int)lastClose);

                  SetSelectedOrderFields();

                  return true;
                 }
              }
            else
              {
               return false;
              }
           }
        }
     }
   return  false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string OrderTypeToString(ENUM_ORDER_TYPE type)
  {
   switch(type)
     {
      case OP_BUY:
         return "Long";
      case OP_SELL:
         return "Short";
      case OP_BUYSTOP:
         return "BuyStop";
      case OP_SELLSTOP:
         return "SellStop";
      case OP_BUYLIMIT:
         return "BuyLimit";
      case OP_SELLLIMIT:
         return "SellLimit";
     }
   return "";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeleteVirtualPendings(string symbol=NULL,int magic=-1,int type=-1,string comment=NULL)
  {
   for(int i=GlobalVariablesTotal()-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      string data=GlobalVariableName(i);
      if(StringFind(data,"sl=")>=0 || StringFind(data,"tp=")>=0)
        {
         int _magic=(int)StringToInteger(GetInfoBitFromVPOString(data,"#"));
         int _type=GetOrderTypeFromVPOString(data);
         string _symbol=GetSymbolFromVPOString(data);
         string _comment=GetInfoBitFromVPOString(data,"c=");

         if((_symbol==symbol || symbol==NULL) && (_magic==magic || magic<1))
            if(_type==type || type<0)
               if(StringFind(_comment,comment)>=0 || comment==NULL)
                 {
                  DeleteVirtualPending(data);
                 }
        }
     }
   CheckVirtualPendingOrders();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeleteVirtualPending(string data)
  {
   Print("Delete "+data);
   double level=(double)StringToDouble(GetInfoBitFromVPOString(data,"@"));
   string symbol=GetSymbolFromVPOString(data);
   double sl=(double)StringToDouble(GetInfoBitFromVPOString(data,"sl="));
   double tp=(double)StringToDouble(GetInfoBitFromVPOString(data,"tp="));
   GlobalVariableDel(data);
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(symbol==Symbol())
     {
      DeleteLine("vp @"+(string)level+" tp="+(string)tp+" sl="+(string)sl);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseOrdersOlderThan(datetime time,int magic)
  {
   int total=OrdersTotal();
   for(int b=total-1; b>=0; b--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderMagicNumber()==magic && OrderSymbol()==Symbol())
           {
            if(OrderOpenTime()<time)
              {
               ResetLastError();
               if(OrderType()==OP_SELL)
                 {
                  if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_ASK),10,CLR_NONE))
                    {
                     Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
                     return;
                    }
                 }
               if(OrderType()==OP_BUY)
                 {
                  if(!OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),10,CLR_NONE))
                    {
                     Print("Failed to close order in "+__FUNCTION__+", error="+(string)GetLastError());
                     return;
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GetTFIdx(int tf)
  {
   for(int i=0; i<ArraySize(iTfTable); i++)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      if(iTfTable[i]==tf)
        {
         return i;
        }
     }

   return ArraySize(iTfTable);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string sTfTable[] = {"M1","M5","M15","M30","H1","H4","D1","W1","MN"};
int    iTfTable[] = {1,5,15,30,60,240,1440,10080,43200};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int stringToTimeFrame(string tfs)
  {
   StringToUpper(tfs);
   for(int i=ArraySize(iTfTable)-1; i>=0; i--)
      if(tfs==sTfTable[i] || tfs==""+(string)iTfTable[i])
         return(iTfTable[i]);
   return(Period());
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string timeFrameToString(int tf)
  {
   if(tf==0)
      tf=Period();
   for(int i=ArraySize(iTfTable)-1; i>=0; i--)
      if(tf==iTfTable[i])
         return(sTfTable[i]);
   return("");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetPipDistanceForValue(string symbol,double value,double lots)
  {
   double pipValue=MarketInfo(Symbol(),MODE_TICKVALUE);
   double point=MarketInfo(symbol,MODE_POINT);

   return ((value/lots)/pipValue)*point;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool _SoundAlert=false; // Sound Alert
string _SoundFile="alert2.wav"; // Sound File
bool _PopupAlert=false; // Pop-up Alert
bool _EmailAlert=true; // Email Alert
bool _PhoneAlert=false; // Phone Alert
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void InitAlertTypes(bool soundAlert,string soundFile,bool popupAlert,bool emailAlert,bool phoneAlert)
  {
   _SoundAlert=soundAlert;
   _SoundFile=soundFile;
   _EmailAlert=emailAlert;
   _PhoneAlert=phoneAlert;
   _PopupAlert=popupAlert;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime lastAlertTime[500];
string lastAlertType[500];
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TriggerAlerts(string type,string soundFile="",string uniqueType="",int minTimeDistance=0)
  {
   static bool alertsInit=false;

   if(StringLen(uniqueType)==0)
      uniqueType=type;

   if(!alertsInit)
     {
      alertsInit=true;
      for(int i=0; i<500; i++)
        {
         lastAlertType[i]="";
        }
     }

   datetime lastTime=GetLastAlertTime(uniqueType);

   if((lastTime<Time[0] || minTimeDistance!=0) && (TimeCurrent()-lastTime>=minTimeDistance*60))
     {
      SetLastAlertTime(uniqueType);
      // type=type+"\n, Time="+TimeToString(TimeCurrent(),TIME_MINUTES);
      Print(type);
      if(_SoundAlert)
        {
         if(StringLen(soundFile)==0)
            soundFile=_SoundFile;
         PlaySound(soundFile);
        }
      if(_PopupAlert)
        {
         Alert(type);
        }
      if(_EmailAlert)
        {
         SendMail(type,type);
        }
      if(_PhoneAlert)
        {
         SendNotification(type);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetLastAlertTime(string type)
  {
   int i=0;
   for(i=0; i<500; i++)
     {
      if(lastAlertType[i]==type || StringLen(lastAlertType[i])==0)
        {
         break;
        }
     }
   if(i==500)
      i=0;
   lastAlertTime[i]=TimeCurrent();

   lastAlertType[i]=type;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime GetLastAlertTime(string type)
  {
   for(int i=0; i<500; i++)
     {
      if(lastAlertType[i]==type)
         return (lastAlertTime[i]);
      if(lastAlertType[i]=="")
         break;
     }
   return (0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetTriangle(string name,datetime time1,double price1,datetime time2,double price2,datetime time3,double price3,color clr)
  {
   if(ObjectFind(name)==0)
     {
      ObjectSet(name,OBJPROP_TIME1,time1);
      ObjectSet(name,OBJPROP_TIME2,time2);
      ObjectSet(name,OBJPROP_TIME3,time3);
      ObjectSet(name,OBJPROP_PRICE1,price1);
      ObjectSet(name,OBJPROP_PRICE2,price2);
      ObjectSet(name,OBJPROP_PRICE3,price3);
     }
   else
     {
      ObjectCreate(0,name,OBJ_TRIANGLE,0,time1,price1,time2,price2,time3,price3);
     }

   ObjectSet(name,OBJPROP_COLOR,clr);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double RangeAvg(int startIdx,int bars=10)
  {
   double ravg=0;
   int lim=MathMin(startIdx+bars,Bars);
   for(int i=startIdx; i<lim; i++)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      ravg+=High[i]-Low[i];
     }
   ravg/=lim-startIdx;
   return (ravg);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetSignalArrow(int ssign,int i=0,int arrowCode=0,color clr=0,string name="signal",int size=1)
  {
   if(arrowCode==0 && ssign>0)
      arrowCode = 225;
   if(arrowCode==0 && ssign<0)
      arrowCode = 226;

   if(name=="signal")
      name+=(string)Time[i];
   if(ssign>0)
     {
      if(clr==0)
         clr=clrLimeGreen;
      SetArrow(name,Time[i],clr,arrowCode,Low[i]-ArrowPadding(),ANCHOR_TOP,size);
     }
   else
     {
      if(clr==0)
         clr=clrTomato;
      SetArrow(name,Time[i],clr,arrowCode,High[i]+ArrowPadding(),ANCHOR_BOTTOM,size);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double ArrowPadding()
  {
   static double   m_WinPriceMax= 0.0,m_WinPriceMin = 0.0;
   static long     m_PaneHeight = 0;
   static double Padding;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(m_WinPriceMax != WindowPriceMax() ||
      m_WinPriceMin != WindowPriceMin() ||
      m_PaneHeight!=ChartGetInteger(ChartID(),CHART_HEIGHT_IN_PIXELS,0))
     {
      m_WinPriceMax = WindowPriceMax();
      m_WinPriceMin = WindowPriceMin();
      m_PaneHeight=ChartGetInteger(ChartID(),CHART_HEIGHT_IN_PIXELS,0);
      if(m_PaneHeight>0)
        {
         Padding=NormalizeDouble(20.0 *(m_WinPriceMax-m_WinPriceMin)/m_PaneHeight,Digits);
        }
     }
   return Padding;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetArrow(string name,datetime time,color clr,int code,double price,int anchor,int size=1)
  {
   int bar=iBarShift(NULL,0,time);
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(ObjectFind(name)==0)
     {
      ObjectSet(name,OBJPROP_TIME1,time);
      ObjectSet(name,OBJPROP_PRICE1,price);
      ObjectSet(name,OBJPROP_COLOR,clr);
      ObjectSet(name,OBJPROP_ARROWCODE,code);
     }
   else
     {
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
      ObjectCreate(name,OBJ_ARROW,0,time,price);
      ObjectSet(name,OBJPROP_COLOR,clr);
      ObjectSet(name,OBJPROP_ARROWCODE,code);
      ObjectSet(name,OBJPROP_WIDTH,size);
      ObjectSet(name,OBJPROP_ANCHOR,anchor);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool ok(double indiValue,double min=0,double max=0)
  {
   if(indiValue!=EMPTY_VALUE && indiValue!=0)
     {
      if(min==0 || indiValue>=min)
        {
         if(max==0 || indiValue<=max)
           {
            return true;
           }
        }
     }

   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string OneSidedIndicatorStatus(double bullishValue,double bearishValue,double bullishValue2=0,double bearishValue2=0)
  {
   string status="n/a";
   if(ok(bullishValue) || ok(bullishValue2))
      status="Bullish";
   if(ok(bearishValue) || ok(bearishValue2))
      status="Bearish";

   return status;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string Direction(double ssign,double reference=0)
  {
   if(MathAbs(ssign)>=MathAbs(reference))
     {
      if(ssign>0)
         return "Bullish";
      if(ssign<0)
         return "Bearish";
     }
   return "n/a";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
color SignColor(double ssign,double reference=0)
  {
   if(MathAbs(ssign)>=MathAbs(reference))
     {
      if(ssign>0)
         return clrLimeGreen;
      if(ssign<0)
         return clrTomato;
     }
   return clrAliceBlue;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string Direction(bool conditionBuy,bool conditionSell,bool &canBuy,bool &canSell,string buyLabel="",string sellLabel="",string neutralLabel="")
  {
   if(!conditionBuy)
      canBuy=false;
   if(!conditionSell)
      canSell=false;

   if(conditionBuy && conditionSell)
      return "BOTH "+neutralLabel;
   if(conditionBuy)
      return "Bullish"+ buyLabel;
   if(conditionSell)
      return "Bearish"+ sellLabel;
   return "n/a "+neutralLabel;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string JoinedDirection(string direction1,string direction2,string direction3="none",string direction4="none",string direction5="none",string direction6="none")
  {
   string joined="none";
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(direction1!="none")
     {
      if(joined=="none")
         joined=NormalizeStatus(direction1);
      else
         if(DifferentStatus(joined,direction1))
            joined="n/a";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(direction2!="none")
     {
      if(joined=="none")
         joined=NormalizeStatus(direction2);
      else
         if(DifferentStatus(joined,direction2))
            joined="n/a";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(direction3!="none")
     {
      if(joined=="none")
         joined=NormalizeStatus(direction3);
      else
         if(DifferentStatus(joined,direction3))
            joined="n/a";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(direction4!="none")
     {
      if(joined=="none")
         joined=NormalizeStatus(direction4);
      else
         if(DifferentStatus(joined,direction4))
            joined="n/a";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(direction5!="none")
     {
      if(joined=="none")
         joined=NormalizeStatus(direction5);
      else
         if(DifferentStatus(joined,direction5))
            joined="n/a";
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(direction6!="none")
     {
      if(joined=="none")
         joined=NormalizeStatus(direction6);
      else
         if(DifferentStatus(joined,direction6))
            joined="n/a";
     }
   return joined;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool DifferentStatus(string status1,string status2)
  {
   return NormalizeStatus(status1)!=NormalizeStatus(status2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string NormalizeStatus(string status)
  {
   if(StringFind(status,"Bullish")>=0)
      status="Bullish";
   if(StringFind(status,"UP")>=0)
      status="Bullish";
   if(StringFind(status,"Bearish")>=0)
      status="Bearish";
   if(StringFind(status,"DOWN")>=0)
      status="Bearish";

   return status;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool NoTradeClosedInLastCandles(string symbol,int magic,int candles)
  {
   bool allGood=true;

   int total=OrdersHistoryTotal();
   datetime lastClosed=0;
   for(int i=total-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if(OrderSymbol()==symbol && OrderMagicNumber()==magic)
           {
            lastClosed=OrderCloseTime();
            break;
           }
        }
     }
   int lastClosedCandle=iBarShift(Symbol(),Period(),lastClosed);
   return lastClosed>candles;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NormLots(double lots)
  {
   double lotStep=MarketInfo(Symbol(),MODE_LOTSTEP);
   int digits = 0;
   if(lotStep<= 0.01)
      digits=2;
   else
      if(lotStep<=0.1)
         digits=1;
   lots=NormalizeDouble(lots,digits);

   double minLots=MarketInfo(Symbol(),MODE_MINLOT);
   if(lots<minLots)
      lots=minLots;

   double maxLots=MarketInfo(Symbol(),MODE_MAXLOT);
   if(lots>maxLots)
      lots=maxLots;

   return (lots);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool ManuallyConfirmed(int sign)
  {
   if(!_AskForManualConfirmation)
      return true;

   string s_type="BUY";
   if(sign<0)
      s_type="SELL";

   return Verify(WindowExpertName()+ " wants to open a "+s_type+" trade on "+Symbol()+", "+timeFrameToString(Period())+". Do you allow it?");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool Verify(string message,string subject="Manual confirmation required")
  {
   return (MessageBox(message,subject,MB_YESNO)==IDYES);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool InInterval(datetime time,int _beginHour,int _beginMinute,int _endHour,int _endMinute,int _beginSecond=0,int _endSecond=0)
  {
   if(_beginHour==0 && _beginMinute==0 && _endHour==0 && _endMinute==0)
     {
      return (true);
     }
   else
     {
      bool afterBeginTime=TimeHour(time)>_beginHour || (TimeHour(time)==_beginHour && TimeMinute(time)>=_beginMinute)
                          || (TimeHour(time)==_beginHour && TimeMinute(time)==_beginMinute && TimeSeconds(time)>=_beginSecond);
      bool beforeEndTime=(TimeHour(time)<_endHour || (TimeHour(time)==_endHour && TimeMinute(time)<=_endMinute) ||
                          (TimeSeconds(time)==_endHour && TimeMinute(time)==_endMinute && TimeSeconds(time)<=_endSecond) || (_endHour==0 && _endMinute==0));
      return (afterBeginTime && beforeEndTime) || ((afterBeginTime || beforeEndTime) && (_beginHour>_endHour));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool InHourInterval(datetime time,int startWeekDay,int endWeekDay,int _beginHour,int _beginMinute,int _endHour,int _endMinute)
  {
   if(_beginHour==0 && _beginMinute==0 && _endHour==0 && _endMinute==0)
     {
      return (true);
     }
   else
     {
      return((TimeDayOfWeek(time)>startWeekDay || (TimeDayOfWeek(time)==startWeekDay && (TimeHour(time)>_beginHour || (TimeHour(time)==_beginHour && TimeMinute(time)>=_beginMinute))))
             &&(TimeDayOfWeek(time)<endWeekDay || (TimeDayOfWeek(time)==endWeekDay && (TimeHour(time)<_endHour || (TimeHour(time)==_endHour && TimeMinute(time)<=_endMinute)))));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetTrendLine(string name,double price1,double price2,datetime time1,datetime time2,color clr,int style,int width=1,bool ray=true,string label="",int window=0,datetime labelTime=0,string labelName="")
  {
   name=GetObjectName(name);

   if(ObjectFind(name)==window)
     {
      ObjectSet(name,OBJPROP_PRICE1,price1);
      ObjectSet(name,OBJPROP_PRICE2,price2);
      ObjectSet(name,OBJPROP_TIME1,time1);
      ObjectSet(name,OBJPROP_TIME2,time2);
      ObjectSet(name,OBJPROP_COLOR,clr);
      if(ray==true)
        {
         ObjectSet(name,OBJPROP_RAY,true);
        }
      else
        {
         ObjectSet(name,OBJPROP_RAY,false);
        }
     }
   else
     {
      ObjectCreate(name,OBJ_TREND,window,time1,price1,time2,price2);
      if(ray==true)
        {
         ObjectSet(name,OBJPROP_RAY,true);
        }
      else
        {
         ObjectSet(name,OBJPROP_RAY,false);
        }
      ObjectSet(name,OBJPROP_COLOR,clr);
      ObjectSet(name,OBJPROP_STYLE,style);
      ObjectSet(name,OBJPROP_WIDTH,width);
     }
   if(StringLen(label)>0)
     {
      if(StringLen(labelName)==0)
         labelName=name+"txt";
      ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER;
      if(labelTime==0)
        {
         labelTime=time2;
         if(price2>price1)
            anchor=ANCHOR_LOWER;
        }
      if(ObjectFind(labelName)!=0)
        {
         ObjectCreate(labelName,OBJ_TEXT,window,labelTime,price2);
        }
      else
        {
         ObjectSet(labelName,OBJPROP_PRICE1,price2);
         ObjectSet(labelName,OBJPROP_TIME1,labelTime);
        }
      ObjectSetText(labelName,label,8);
      ObjectSet(labelName,OBJPROP_COLOR,clr);
      ObjectSet(labelName,OBJPROP_ANCHOR,anchor);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsNewCandle(int tf=0)
  {
   static datetime timeLastCandle=0;
   if(iTime(Symbol(),tf,0)==timeLastCandle)
      return (false);
   timeLastCandle=iTime(Symbol(),tf,0);
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsNewCandle2(int tf=0)
  {
   static datetime timeLastCandle=0;
   if(iTime(Symbol(),tf,0)==timeLastCandle)
      return (false);
   timeLastCandle=iTime(Symbol(),tf,0);
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GetKellyCriterionFactors(string symbol,int magic,double &winFactor,double &winLossRatio)
  {
   int losses=0;
   int wins=0;
   double winProfit=0;
   double lossProfit=0;

   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
     {
      ResetLastError();
      if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         Print("Failed to select history orders in "+__FUNCTION__+", error="+(string)GetLastError());
         return false;
        }
      if(OrderSymbol()==symbol && OrderMagicNumber()==magic)
        {
         if(OrderProfit()>0)
           {
            wins++;
            winProfit+=OrderProfit();
           }
         if(OrderProfit()<0)
           {
            losses++;
            lossProfit+=OrderProfit();
           }
        }
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(losses==0)
     {
      Print("0 losses, can't process win/loss ratio");
      return -1;
     }

   if(wins>0)
      winProfit/=wins;
   lossProfit/=losses;
   winFactor=(double)wins/(wins+losses);
   winLossRatio=winProfit/MathAbs(lossProfit);

   return wins+losses;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int StartHour=0; // Start Hour
int StartMinute=0; // Start Minute
int EndHour=0; // End Hour
int EndMinute=0; // End Minute
int CloseHour=0; //  Close Hour
int CloseMinute=0; //  Close Minute
int TradeHour;
int TradeMinute=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string Ordinal(int number)
  {
   if(number==1)
      return "1st";
   if(number==2)
      return "2nd";
   if(number==3)
      return "3rd";
   else
      return (string)number+"th";
  }
//+------------------------------------------------------------------+
//|                                 t                                 |
//+------------------------------------------------------------------+
void TransformTime(string time,int &hour,int &minute)
  {
   if(StringLen(time)!=5 || StringFind(time,":")!=2)
     {
      Alert("Start Time format must be like 'HH:MM'");
     }

   hour=(int)StringToInteger(StringSubstr(time,0,2));
   minute=(int)StringToInteger(StringSubstr(time,3,2));
  }
//+------------------------------------------------------------------+
//|                                 t                                 |
//+------------------------------------------------------------------+
void TransformTimeS(string time,int &hour,int &minute,int &second)
  {
   if(StringLen(time)!=8 || StringFind(time,":")!=2)
     {
      Alert("Start Time format must be like 'HH:MM'");
     }

   hour=(int)StringToInteger(StringSubstr(time,0,2));
   minute=(int)StringToInteger(StringSubstr(time,3,2));
   second=(int)StringToInteger(StringSubstr(time,6,2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string font="Ebrima";
int tableFontSize=8;
double xLeftMarginRatio=0.008;
double yTopMarginRatio=0.045;
double xFirstColumnSizeRatio=0.055;
double xColumnSizeRatio=0.052;
double xColumnSeparatorRatio=0.0006;
double yFirstRowSizeRatio=0.05;
double yRowSizeRatio=0.072;
double yRowSeparatorRatio=0.0025;
enum CellType {Radio_Cell,Text_Cell,Button_Cell};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct dashColumn
  {
   string            name;
   string            s_id;
   double            xSizeWindowPercentage;
   double            xOffsetWindowPercentage;
   CellType          cellType;
   string            tag;
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
dashColumn dashColumns[];
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddColumn(string headerID,string name,double xSizeWindowPercentage,CellType cellType,int subWindow=0,string tag="",color clr=clrDarkSeaGreen)
  {
   ArrayResize(dashColumns,ArraySize(dashColumns)+1);
   int idx=ArraySize(dashColumns)-1;
   dashColumns[idx].name=name;
   if(StringLen(name)==0)
      dashColumns[idx].name=headerID;
   dashColumns[idx].xSizeWindowPercentage=xSizeWindowPercentage;
   dashColumns[idx].cellType=cellType;
   dashColumns[idx].tag=tag;
   dashColumns[idx].s_id=headerID;

   for(int i=0; i<idx; i++)
     {
      dashColumns[idx].xOffsetWindowPercentage+=dashColumns[i].xSizeWindowPercentage+xColumnSeparatorRatio*100;
     }

   AddCell(dashColumns[idx].name,0,idx,clr,0,subWindow);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddCell(string text,int rowIdx,int columnIdx,color clr,double resizeYPercentage=0,int subWindow=0,int fontSize=0)
  {
   string name="row"+(string)rowIdx+"column"+(string)columnIdx;

   int chartXSize=(int)ChartGetInteger(0,CHART_WIDTH_IN_PIXELS,subWindow);
   int chartYSize=(int)ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS,subWindow);

   int columnSize=(int)(chartXSize*xColumnSizeRatio);
   if(columnIdx==0)
      columnSize=(int)(chartXSize*xFirstColumnSizeRatio); // first column
   if(columnIdx>=0 && columnIdx<ArraySize(dashColumns))
      columnSize=(int)(chartXSize*dashColumns[columnIdx].xSizeWindowPercentage/100);

   int rowSize=(int)(int)(chartYSize*yRowSizeRatio);
   if(rowIdx==0)
      rowSize=(int)(chartYSize*yFirstRowSizeRatio); // first row - HEADER

   int columnOffset=(int)(xLeftMarginRatio*chartXSize);
   if(columnIdx>=0 && columnIdx<ArraySize(dashColumns))
      columnOffset+=(int)(chartXSize*dashColumns[columnIdx].xOffsetWindowPercentage/100);
   else
      columnOffset+=(int)(xFirstColumnSizeRatio*chartXSize+chartXSize*xColumnSeparatorRatio+(columnIdx-1)*(chartXSize*xColumnSizeRatio+chartXSize*xColumnSeparatorRatio));

   int rowOffset=(int)(yTopMarginRatio*chartYSize);
   if(rowIdx!=0)
      rowOffset+=(int)(yFirstRowSizeRatio*chartYSize)+(int)(rowIdx-1)*rowSize+(int)(rowIdx*chartYSize*yRowSeparatorRatio);

   AddTextLabel(name,columnOffset,rowOffset,columnSize,rowSize,clr,text,CORNER_LEFT_UPPER,true,subWindow,fontSize);

// add margins
   if(columnIdx==0)
      AddBorder(name+"bl",columnOffset-(int)(xColumnSeparatorRatio*chartXSize*1),rowOffset,(int)(xColumnSeparatorRatio*chartXSize),rowSize+(int)(xColumnSeparatorRatio*chartXSize),clrWhiteSmoke,CORNER_LEFT_UPPER,subWindow);  // add left margin
   if(rowIdx==0)
      AddBorder(name+"bt",columnOffset,rowOffset-(int)(xColumnSeparatorRatio*chartXSize*1),columnSize+(int)(xColumnSeparatorRatio*chartXSize),(int)(xColumnSeparatorRatio*chartXSize),clrWhiteSmoke,CORNER_LEFT_UPPER,subWindow);  // add top margin
   AddBorder(name+"br",columnOffset+columnSize,rowOffset-(int)(xColumnSeparatorRatio*chartXSize*1),(int)(xColumnSeparatorRatio*chartXSize),rowSize+(int)(xColumnSeparatorRatio*chartXSize),clrWhiteSmoke,CORNER_LEFT_UPPER,subWindow);  // add right margin
   AddBorder(name+"bb",columnOffset-(int)(xColumnSeparatorRatio*chartXSize*1),rowOffset+rowSize,columnSize+(int)(xColumnSeparatorRatio*chartXSize)*2,(int)(xColumnSeparatorRatio*chartXSize),clrWhiteSmoke,CORNER_LEFT_UPPER,subWindow);  // add bottom margin
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddBorder(string name,int xDistance,int yDistance,int xSize,int ySize,color clr,ENUM_BASE_CORNER corner,int subWindow=0)
  {
   ObjectCreate(0,name,OBJ_RECTANGLE_LABEL,subWindow,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,clr);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,-99999);
   ObjectSetInteger(0,name,OBJPROP_BORDER_TYPE,BORDER_RAISED);
   ObjectSetInteger(0,name,OBJPROP_CORNER,corner);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_BACK,true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddTextLabel(string name,int xDistance,int yDistance,int xSize,int ySize,color clr,string text,ENUM_BASE_CORNER corner,bool trim=true,int subWindow=0,int fontSize=0)
  {
   if(StringLen(text)>63)
     {
      text=StringSetChar(text,61,'.');
      text=StringSetChar(text,62,'.');
      text=StringSetChar(text,63,'.');
      text=StringSubstr(text,0,64);
     }
   if(!trim)
      xSize=(int)(StringLen(text)*6.15);

   ObjectCreate(0,name,OBJ_RECTANGLE_LABEL,subWindow,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,clr);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,-99999);
   ObjectSetInteger(0,name,OBJPROP_BORDER_TYPE,BORDER_RAISED);
   ObjectSetInteger(0,name,OBJPROP_CORNER,corner);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_BACK,true);

   name=name+"txt";
   int ySign=1;
   if(corner==CORNER_LEFT_LOWER || corner==CORNER_RIGHT_LOWER)
      ySign=-1;
   ObjectCreate(0,name,OBJ_LABEL,subWindow,0,0);
   ObjectSetInteger(0,name,OBJPROP_ANCHOR,ANCHOR_LEFT);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance+5);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance+ySign*ySize/2);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_CORNER,corner);
   string trimmedText=text;
   if(trim)
     {
      trimmedText=StringSubstr(text,0,(int)(xSize/6));
      if(StringLen(trimmedText)<StringLen(text))
         trimmedText=StringSubstr(trimmedText,0,StringLen(trimmedText)-2)+"..";
     }

   if(fontSize==0)
      fontSize=tableFontSize;
   ObjectSetText(name,trimmedText,fontSize,font,clrWhiteSmoke);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetLabel(string name,int xDistance,int yDistance,int xSize,int ySize,color clr,string text,int fontSize=10,ENUM_BASE_CORNER corner=CORNER_LEFT_LOWER,ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER,bool trim=true)
  {
   name=GetObjectName(name);

   if(StringLen(text)>63)
     {
      text=StringSetChar(text,61,'.');
      text=StringSetChar(text,62,'.');
      text=StringSetChar(text,63,'.');
      text=StringSubstr(text,0,64);
     }
   if(!trim)
      xSize=(int)(StringLen(text)*6.15);

   int ySign=1;
   if(corner==CORNER_LEFT_LOWER || corner==CORNER_RIGHT_LOWER)
      ySign=-1;
   if(ObjectFind(0,name)<0)
      ObjectCreate(0,name,OBJ_LABEL,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_ANCHOR,ANCHOR_LEFT);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance+5);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance+ySign*ySize/2);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_CORNER,corner);
   ObjectSetInteger(0,name,OBJPROP_ANCHOR,anchor);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,333);
   string trimmedText=text;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(trim)
     {
      trimmedText=StringSubstr(text,0,(int)(xSize/6));
      if(StringLen(trimmedText)<StringLen(text))
         trimmedText=StringSubstr(trimmedText,0,StringLen(trimmedText)-2)+"..";
     }
   ObjectSetText(name,trimmedText,tableFontSize,font,clrWhiteSmoke);
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,fontSize);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double MinDistance(string symbol=NULL)
  {
   if(StringLen(symbol)==0 || symbol==NULL)
      symbol=Symbol();
   return MarketInfo(symbol,MODE_STOPLEVEL)*MarketInfo(symbol,MODE_POINT);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Spread(string symbol=NULL)
  {
   if(StringLen(symbol)==0 || symbol==NULL)
      symbol=Symbol();
   return MarketInfo(symbol,MODE_SPREAD)*MarketInfo(symbol,MODE_POINT);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetButton(string name,int xDistance,int yDistance,int xSize,int ySize,string text,color textColor,color bgColor,bool state=false,int fontSize=0,ENUM_BASE_CORNER  corner=CORNER_LEFT_UPPER,ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER,int window=0,string s_font="Ebrima")
  {
   name=GetObjectName(name);
   ObjectCreate(name,OBJ_BUTTON,window,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   if(StringLen(s_font)>0)
      ObjectSetString(0,name,OBJPROP_FONT,s_font);
   ObjectSetInteger(0,name,OBJPROP_COLOR,textColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,bgColor);
   ObjectSetInteger(0,name,OBJPROP_STATE,state);
   if(fontSize==0)
      fontSize=xSize/10;
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,fontSize);
   ObjectSetInteger(0,name,OBJPROP_CORNER,corner);
   ObjectSetInteger(0,name,OBJPROP_ANCHOR,anchor);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,5);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetRectangleLabel(string name,int xDistance,int yDistance,int xSize,int ySize,color clr,color bgColor=clrBlueViolet,
                       ENUM_BASE_CORNER  corner=CORNER_LEFT_UPPER,ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER,bool back=true,int window=0)
  {
   name=GetObjectName(name);
   ObjectCreate(name,OBJ_RECTANGLE_LABEL,window,0,0);
   ObjectSetInteger(0,name,OBJPROP_CORNER,corner);
   ObjectSetInteger(0,name,OBJPROP_ANCHOR,anchor);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetInteger(0,name,OBJPROP_COLOR,clr);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,bgColor);
   ObjectSetInteger(0,name,OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_BACK,back);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,10);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetObjectName(string name)
  {
   if(StringFind(name,_ObjPrefix)<0)
     {
      name=StringConcatenate(_ObjPrefix,name);
     }

   return name;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetInput(string name,int xDistance,int yDistance,int xSize,int ySize,string text,color textColor,color bgColor,bool state=false,string s_font="Ebrima")
  {
   name=GetObjectName(name);
   if(ObjectFind(0,name)<0)
      ObjectCreate(0,name,OBJ_EDIT,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   if(StringLen(s_font)>0)
      ObjectSetString(0,name,OBJPROP_FONT,s_font);
   ObjectSetInteger(0,name,OBJPROP_COLOR,textColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,bgColor);
   ObjectSetInteger(0,name,OBJPROP_STATE,state);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetNumberInput(string name,int xDistance,int yDistance,int xSize,int ySize,string text,color textColor,color bgColor,bool state=false,int window=0,string s_font="Ebrima")
  {
   int arrowsSize=32;

   name=GetObjectName(name);
   if(ObjectFind(0,name)<0)
      ObjectCreate(0,name,OBJ_EDIT,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize-arrowsSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   if(StringLen(s_font)>0)
      ObjectSetString(0,name,OBJPROP_FONT,s_font);
   ObjectSetInteger(0,name,OBJPROP_COLOR,textColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,bgColor);
   ObjectSetInteger(0,name,OBJPROP_STATE,state);

   string nameUp=name+" goUp";
   SetButton(nameUp,xDistance+xSize-arrowsSize,yDistance,arrowsSize,ySize/2,CharToStr(233),clrBlack,bgColor,false,7,0,0,window,"Wingdings");
   string nameDn=name+" goDn";
   SetButton(nameDn,xDistance+xSize-arrowsSize,yDistance+ySize/2,arrowsSize,ySize/2,CharToStr(234),clrBlack,bgColor,false,7,0,0,window,"Wingdings");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetCell(string name,int xDistance,int yDistance,int xSize,int ySize,string text,color textColor,color bgColor,int fontSize=0,int xOffset=0,int yOffset=0,string s_font="Ebrima",bool selectable=false)
  {
   name=GetObjectName(name);

   name=name+"bg";
   if(ObjectFind(0,name)<0)
      ObjectCreate(0,name,OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,selectable);
   ObjectSetInteger(0,name,OBJPROP_BACK,false);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,bgColor);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(StringLen(text)>0)
     {
      name=StringSubstr(name,0,StringLen(name)-2);
      if(fontSize==0)
         fontSize=(int)(ySize/1.75);

      if(ObjectFind(0,name)<0)
         ObjectCreate(0,name,OBJ_LABEL,0,0,0);
      if(xOffset==0)
         xOffset=(int)((double)xSize*12/(StringLen(text)*fontSize));
      if(yOffset==0)
         yOffset=(int)((double)(ySize-fontSize)/4);
      ObjectSetInteger(0,name,OBJPROP_XDISTANCE,xDistance+xOffset);
      ObjectSetInteger(0,name,OBJPROP_YDISTANCE,yDistance+yOffset);
      ObjectSetInteger(0,name,OBJPROP_XSIZE,xSize);
      ObjectSetInteger(0,name,OBJPROP_YSIZE,ySize);
      ObjectSetString(0,name,OBJPROP_TEXT,text);
      if(StringLen(s_font)>0)
         ObjectSetString(0,name,OBJPROP_FONT,s_font);
      ObjectSetInteger(0,name,OBJPROP_COLOR,textColor);
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,fontSize);
      ObjectSetInteger(0,name,OBJPROP_SELECTABLE,selectable);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Get(string key)
  {
   key+=StringConcatenate("",Symbol());
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   if(GlobalVariableCheck(key))
     {
      return GlobalVariableGet(key);
     }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   else
     {
      return -1;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime Set(string key,double value)
  {
   key+=StringConcatenate("",Symbol());
   datetime ret=GlobalVariableSet(key,value);

   return ret;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CurrentVersion()
  {
   double v=1.0;
   string name=WindowExpertName();
   for(int i=StringLen(name)-1; i>=0; i--)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      if(StringSubstr(name,i,1)=="v")
        {
         string vShort=StringSubstr(name,i+1,1);
         string vLong=StringSubstr(name,i+1,3);
         v=StringToDouble(vLong);
         if(v==0)
            v=StringToDouble(vLong);
         break;
        }
     }
   return "v"+DoubleToStr(v,1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
color disabledColor=clrSlateGray;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SelectRadioButton(string selected)
  {
   ObjectSetInteger(0,selected,OBJPROP_STATE,true);

   int pos=StringFind(selected," ");
   string radioGroup=StringSubstr(selected,0,pos);

   for(int i=0; i<ObjectsTotal(0,0,OBJ_BUTTON); i++)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      string name=ObjectName(0,i,0,OBJ_BUTTON);
      if(StringFind(name,radioGroup)>=0 && name!=selected)
        {
         ObjectSetInteger(0,name,OBJPROP_STATE,false);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckActionLines(int magic=-1)
  {
   static double lastBid=0;
   static double lastAsk=0;

   if(lastBid!=0 && lastAsk!=0)
     {
      for(int i=0; i<ObjectsTotal(0,0,OBJ_HLINE); i++)
        {
         string name=ObjectName(0,i,0,OBJ_HLINE);
         double price=ObjectGetDouble(0,name,OBJPROP_PRICE1);
         if(StringFind(name,"buy")>=0 || StringFind(name,"sell")>=0)
           {
            double lastRefPrice=0;
            double refPrice=0;

            if(StringFind(name,"buy")>=0)
              {
               lastRefPrice=lastBid;
               refPrice=Bid;
              }
            if(StringFind(name,"sell")>=0)
              {
               lastRefPrice=lastAsk;
               refPrice=Ask;
              }

            double orderPrice=StringToDouble(GetInfoBitFromVPOString(name,"p="));
            int orderTicket=FindOrder(Symbol(),magic,-1,-1,NULL,orderPrice-5*Point,orderPrice+5*Point,0);

            if(orderTicket<0)
              {
               ObjectDelete(0,name);
               ObjectDelete(0,name+"txt");
               continue;
              }

            double ssign=refPrice-price;
            double lastSign=lastRefPrice-price;

            if(ssign*lastSign<=0)
              {
               // LINE touched or crossed

               color clr=(color)ObjectGetInteger(0,name,OBJPROP_COLOR);

               if(clr==clrTomato || clr==clrLimeGreen)
                 {
                  Print("crossed "+name);
                  double volume=StringToDouble(GetInfoBitFromVPOString(name,"v="));

                  Print("trying to close "+DoubleToStr(volume,2)+" lots for ticket #"+(string)orderTicket);

                  int sel=OrderSelect(orderTicket,SELECT_BY_TICKET,MODE_TRADES);

                  double close=OrderClose(orderTicket,volume,refPrice,(int)(50*GetPip(Symbol())/Point()));

                  if(close)
                    {
                     ObjectSetInteger(0,name,OBJPROP_COLOR,clrGray);
                    }
                 }
              }
           }
        }
     }

   lastBid=Bid;
   lastAsk=Ask;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CrossedAbove(double fast,double slow,double prev_fast,double prev_slow,double minDistance=0)
  {
   return fast>slow && prev_fast<=prev_slow &&  fast-slow>=minDistance;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CrossedBelow(double fast,double slow,double prev_fast,double prev_slow,double minDistance=0)
  {
   return fast<slow && prev_fast>=prev_slow &&  slow-fast>=minDistance;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetText(string name,double price,datetime time,color clr,string text,int fontSize=10,int window=0,ENUM_BASE_CORNER corner=CORNER_LEFT_LOWER,ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER)
  {
   name=GetObjectName(name);
   ObjectCreate(name,OBJ_TEXT,window,0,0);
   ObjectSetInteger(window,name,OBJPROP_ANCHOR,ANCHOR_LEFT);

   ObjectSet(name,OBJPROP_PRICE1,price);

   ObjectSet(name,OBJPROP_SELECTABLE,false);
   ObjectSet(name,OBJPROP_TIME1,time);
   ObjectSetInteger(window,name,OBJPROP_CORNER,corner);
   ObjectSetInteger(window,name,OBJPROP_ANCHOR,anchor);
   ObjectSetInteger(window,name,OBJPROP_ZORDER,0);
   ObjectSetText(name,text,fontSize,NULL,clr);
   ObjectSetInteger(window,name,OBJPROP_FONTSIZE,fontSize);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int Correlated(string pair1,string pair2)
  {
   if(StringSubstr(pair1,0,3)==StringSubstr(pair2,0,3))
      return 1;
   if(StringSubstr(pair1,3,3)==StringSubstr(pair2,3,3))
      return 1;
   if(StringSubstr(pair1,0,3)==StringSubstr(pair2,3,3))
      return -1;
   if(StringSubstr(pair1,3,3)==StringSubstr(pair2,0,3))
      return -1;
   return 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double SafeMin(double value1,double value2)
  {
   if(!ok(value1) && ok(value2))
      return value2;
   if(!ok(value2) && ok(value1))
      return value1;
   return MathMin(value1,value2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double SafeMax(double value1,double value2)
  {
   if(!ok(value1) && ok(value2))
      return value2;
   if(!ok(value2) && ok(value1))
      return value1;
   return MathMax(value1,value2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double SafeDivide(double divideThis,double byThat)
  {
   if(byThat==0)
      return divideThis/0.0000001;
   else
      return divideThis/byThat;
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckLicense(int lockedAccountNumber,int lockedYear,int lockedMonth,int lockedDayOfMonth)
  {
   if(AccountNumber()!=lockedAccountNumber && lockedAccountNumber!=0)
      return false;
   if(TimeYear(TimeCurrent())>lockedYear || (TimeYear(TimeCurrent())==lockedYear && TimeMonth(TimeCurrent())>lockedMonth)
      ||(TimeYear(TimeCurrent())==lockedYear && TimeMonth(TimeCurrent())==lockedMonth && TimeDay(TimeCurrent())>lockedDayOfMonth))
      return false;
   return true;
  }