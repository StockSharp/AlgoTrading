//+------------------------------------------------------------------+
//|                                                   BreakOut15.mq4 |
//|                                                 Copyright © 2006 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Robert Hill"

// DISCLAIMER ***** IMPORTANT NOTE ***** READ BEFORE USING ***** 
// This expert advisor can open and close real positions and hence do real trades and lose real money.
// This is not a 'trading system' but a simple robot that places trades according to fixed rules.
// The author has no pretentions as to the profitability of this system and does not suggest the use
// of this EA other than for testing purposes in demo accounts.
// Use of this system is free - but u may not resell it - and is without any garantee as to its
// suitability for any purpose.
// By using this program you implicitly acknowledge that you understand what it does and agree that 
// the author bears no responsibility for any losses.
// Before using, please also check with your broker that his systems are adapted for the frequest trades
// associated with this expert.
//
// Added code for 3 level trailing stop to protect profits

#include <stdlib.mqh>
//----
int SignalTimeFrame=15;
//+---------------------------------------------------+
//|Money Management                                   |
//+---------------------------------------------------+
extern bool MoneyManagement=true;       // Change to false if you want to shutdown money management controls.
                                        // Lots = 1 will be in effect and only 1 lot will be open regardless of equity.
extern double TradeSizePercent=10;      // Change to whatever percent of equity you wish to risk.
extern double Lots=1;                   
double MaxLots=100;
extern double TakeProfit=0;             // number of ticks to take profit. normally is = grid size but u can override
extern double StopLoss=60;              // if u want to add a stop loss. normal grids dont use stop losses
extern bool UseTrailingStop=true;
extern int TrailingStopType=2;          // Type 1 moves stop immediately, Type 2 waits til value of TS is reached
extern double TrailingStop=45;          // Change to whatever number of pips you wish to trail your position with.
extern double TRStopLevel_1=20;         // Type 3  first level pip gain
extern double TrailingStop1=15;         // Move Stop to Breakeven
extern double TRStopLevel_2=30;         // Type 3 second level pip gain
extern double TrailingStop2=20;         // Move stop to lock is profit
extern double TRStopLevel_3=40;
extern double TrailingStop3=20;         // Move stop and trail from there
extern int FastMA_Mode=1;               //0=sma, 1=ema, 2=smma, 3=lwma, 4=LSMA
extern int FastMA_Period=  10;
extern int FastMA_Shift=0;
extern int FastMA_AppliedPrice=0;       // 0=close, 1=open, 2=high, 3=low, 4=median((h+l/2)), 5=typical((h+l+c)/3), 6=weighted((h+l+c+c)/4)
extern int SlowMA_Mode=1;               //0=sma, 1=ema, 2=smma, 3=lwma, 4=LSMA
extern int SlowMA_Period=  80;
extern int SlowMA_Shift=0;
extern int SlowMA_AppliedPrice=0;       // 0=close, 1=open, 2=high, 3=low, 4=median((h+l/2)), 5=typical((h+l+c)/3), 6=weighted((h+l+c+c)/4)
extern double BreakOutLevel=35;         // Start trade after breakout is reached
extern bool UseTimeLimit=true;
extern int StartHour =7;                // Start trades after time
extern int StopHour=16;                 // Stop trading after time
extern bool UseFridayCloseAll=false;
extern int FridayCloseHour=20;
//----
int SignalBar=1;                       // 1 for prior, 0 for current
double Margincutoff=800;               // Expert will stop trading if equity level decreases to that level.
int Slippage=10;                       // Possible fix for not getting filled or closed    
int    MagicNumber;                    // Magic number of the trades. must be unique to identify
string   setup;                        // identifies the expert
bool YesStop;
double lotMM;
int TradesInThisSymbol;
double LongTradeRate=0.0;
double ShortTradeRate=0.0;
bool Converted=true;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   setup="BreakOut15 " + Symbol() + "_" + func_TimeFrame_Val2String(func_TimeFrame_Const2Val(Period()));
   MagicNumber=3000 + func_Symbol2Val(Symbol())*100 + func_TimeFrame_Const2Val(Period());
//---- 
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------------+
//| LSMA - Least Squares Moving Average function calculation               |
//| LSMA_In_Color Indicator plots the end of the linear regression line    |
//| Modified to use any timeframe                                          |
//+------------------------------------------------------------------------+
double LSMA(int Rperiod,int prMode, int TimeFrame, int mshift)
  {
   int i;
   double sum, price;
   int length;
   double lengthvar;
   double tmp;
   double wt;
   //
   length=Rperiod;
   //
   sum=0;
   for(i=length; i>=1 ;i--)
     {
      lengthvar=length + 1;
      lengthvar/=3;
      tmp=0;
      switch(prMode)
        {
         case 0: price=iClose(NULL,TimeFrame,length-i+mshift);break;
         case 1: price=iOpen(NULL,TimeFrame,length-i+mshift);break;
         case 2: price=iHigh(NULL,TimeFrame,length-i+mshift);break;
         case 3: price=iLow(NULL,TimeFrame,length-i+mshift);break;
         case 4: price=(iHigh(NULL,TimeFrame,length-i+mshift) + iLow(NULL,TimeFrame,length-i+mshift))/2;break;
         case 5: price=(iHigh(NULL,TimeFrame,length-i+mshift) + iLow(NULL,TimeFrame,length-i+mshift) + iClose(NULL,TimeFrame,length-i+mshift))/3;break;
         case 6: price=(iHigh(NULL,TimeFrame,length-i+mshift) + iLow(NULL,TimeFrame,length-i+mshift) + iClose(NULL,TimeFrame,length-i+mshift) + iClose(NULL,TimeFrame,length-i+mshift))/4;break;
        }
      tmp =(i - lengthvar)*price;
      sum+=tmp;
     }
   wt=sum*6/(length*(length+1));
//----
   return(wt);
  }
//+------------------------------------------------------------------+
//| CheckExitCondition                                               |
//| Check if LSMAs cross down to exit BUY or up to exit SELL         |
//+------------------------------------------------------------------+
bool CheckExitCondition(string TradeType)
  {
   bool YesClose;
   double fMA, sMA;
   //
   YesClose=false;
   //
   if (UseFridayCloseAll && DayOfWeek()==5 && Hour()>=FridayCloseHour) return(true);
   if (FastMA_Mode==4)
     {
      fMA=LSMA(FastMA_Period,FastMA_AppliedPrice,SignalTimeFrame,SignalBar);
     }
   else
     {
      fMA=iMA(NULL, SignalTimeFrame, FastMA_Period, FastMA_Shift, FastMA_Mode, FastMA_AppliedPrice, SignalBar);
     }
   if (SlowMA_Mode==4)
     {
      sMA=LSMA(SlowMA_Period,SlowMA_AppliedPrice,SignalTimeFrame,SignalBar);
     }
   else
     {
      sMA=iMA(NULL, SignalTimeFrame, SlowMA_Period, SlowMA_Shift, SlowMA_Mode, SlowMA_AppliedPrice, SignalBar);
     }
   // Check for cross down
   if (TradeType=="BUY" && fMA < sMA)YesClose=true;
   // Check for cross up
   if (TradeType=="SELL" && fMA > sMA) YesClose=true;
//----
   return(YesClose);
  }
//+------------------------------------------------------------------+
//| CheckEntryCondition                                              |
//| Check if LSMAs cross up for BUY or down for SELL                 |
//+------------------------------------------------------------------+
bool CheckEntryCondition(string TradeType)
  {
   bool YesTrade;
   double fMA, sMA;
   //
   YesTrade=false;
   if (FastMA_Mode==4)
     {
      fMA=LSMA(FastMA_Period,FastMA_AppliedPrice,SignalTimeFrame,SignalBar);
     }
   else
     {
      fMA=iMA(NULL, SignalTimeFrame, FastMA_Period, FastMA_Shift, FastMA_Mode, FastMA_AppliedPrice, SignalBar);
     }
   if (SlowMA_Mode==4)
     {
      sMA=LSMA(SlowMA_Period,SlowMA_AppliedPrice,SignalTimeFrame,SignalBar);
     }
   else
     {
      sMA=iMA(NULL, SignalTimeFrame, SlowMA_Period, SlowMA_Shift, SlowMA_Mode, SlowMA_AppliedPrice, SignalBar);
     }
   // Check for cross up
   if (TradeType=="BUY" && fMA > sMA)  YesTrade=true;
   // Check for cross down
   if (TradeType=="SELL" && fMA < sMA)YesTrade=true;
//----
   return(YesTrade);
  }
//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   // Check for valid inputs
   if (Period()!=15)
     {
      Alert("15 Minute Chart Only.");
      return(0);
     }
   // Check if any pending Orders 
   Converted=true;
   if (LongTradeRate > 0.1) Converted=HandlePendingBuyOrder();
   if (ShortTradeRate > 0.1) Converted=HandlePendingSellOrder();
   if (!Converted) return(0);
   // Check if any open positions
   HandleOpenPositions();
   if (UseTimeLimit)
     {
      // trading from 7:00 to 13:00 GMT
      // trading from Start1 to Start2
      YesStop=true;
      if (Hour()>=StartHour && Hour() < StopHour) YesStop=false;
      //      Comment ("Trading has been stopped as requested - wrong time of day");
      if (YesStop) return(0);
     }
   TradesInThisSymbol=openPositions();
   //+------------------------------------------------------------------+
   //| Check if OK to make new trades                                   |
   //+------------------------------------------------------------------+
     if(AccountFreeMargin() < Margincutoff) 
     {
     return(0);
     }
   // Only allow 1 trade per Symbol
     if(TradesInThisSymbol > 0) 
     {
     return(0);
     }
   lotMM=GetLots();
   if(CheckEntryCondition("BUY") )
     {
      LongTradeRate=Ask + BreakOutLevel*Point;
      ShortTradeRate=0.0;
      return(0);
     }
   if (CheckEntryCondition("SELL"))
     {
      ShortTradeRate=Bid - BreakOutLevel * Point;
      LongTradeRate=0.0;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Open Buy                                                         |
//| Open a new trade using Buy                                       |
//| If Stop Loss or TakeProfit are used the values are calculated    |
//| for each trade                                                   |
//+------------------------------------------------------------------+
void OpenBuyOrder()
  {
   int err,ticket;
   double myStopLoss=0, myTakeProfit=0;
   //
   myStopLoss=0;
   if(StopLoss > 0)myStopLoss=Ask - StopLoss * Point ;
   myTakeProfit=0;
   if (TakeProfit>0) myTakeProfit=Ask + TakeProfit * Point;
   ticket=OrderSend(Symbol(),OP_BUY,lotMM,Ask,Slippage,myStopLoss,myTakeProfit,setup,MagicNumber,0,Green);
   if(ticket<=0)
     {
      err=GetLastError();
      Print("Error opening BUY order [" + setup + "]: (" + err + ") " + ErrorDescription(err));
     }
  }
//+------------------------------------------------------------------+
//| Open Sell                                                        |
//| Open a new trade using Sell                                      |
//| If Stop Loss or TakeProfit are used the values are calculated    |
//| for each trade                                                   |
//+------------------------------------------------------------------+
void OpenSellOrder()
  {
   int err, ticket;
   double myStopLoss=0, myTakeProfit=0;
   //
   myStopLoss=0;
   if(StopLoss > 0)myStopLoss=Bid + StopLoss * Point ;
   myTakeProfit=0;
   if (TakeProfit > 0) myTakeProfit=Bid - TakeProfit * Point;
   ticket=OrderSend(Symbol(),OP_SELL,lotMM,ShortTradeRate,Slippage,myStopLoss,myTakeProfit,setup,MagicNumber,0,Red);
   if(ticket<=0)
     {
      err=GetLastError();
      Print("Error opening Sell order [" + setup + "]: (" + err + ") " + ErrorDescription(err));
     }
  }
//+------------------------------------------------------------------------+
//| counts the number of open positions                                    |
//+------------------------------------------------------------------------+
int openPositions(  )
  {  int op =0;
   for(int i=OrdersTotal()-1;i>=0;i--)                                // scan all orders and positions...
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderMagicNumber()!=MagicNumber) continue;
      if(OrderSymbol()==Symbol() )
        {
         if(OrderType()==OP_BUY)op++;
         if(OrderType()==OP_SELL)op++;
        }
     }
   return(op);
  }
//+------------------------------------------------------------------+
//| Handle pending Buy order                                         |
//+------------------------------------------------------------------+
int HandlePendingBuyOrder()
  {
   // Check if still a valid breakout
   if (LongTradeRate > 0.1)
     {
      if(CheckExitCondition("BUY")) LongTradeRate=0.0;
      if(Hour() >=StopHour) LongTradeRate=0.0;
     }

   if (LongTradeRate > 0.1 && Ask>=LongTradeRate)
     {
      OpenBuyOrder();
      LongTradeRate=0.0;
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//| Handle pending Sell order                                        |
//+------------------------------------------------------------------+
int HandlePendingSellOrder()
  {
   // Check if still a valid breakout
   if (ShortTradeRate > 0.1)
     {
      if(CheckExitCondition("SELL")) ShortTradeRate=0.0;
      if(Hour() >=StopHour) ShortTradeRate=0.0;
     }
   if (ShortTradeRate > 0.1 && Bid<=ShortTradeRate)
     {
      OpenSellOrder();
      ShortTradeRate=0.0;
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|  Close Open Position Controls                                    |
//|  Try to close position 3 times                                   |
//+------------------------------------------------------------------+
void CloseOrder(int ticket,double numLots,double close_price)
  {
   int CloseCnt, err;
   // try to close 3 Times
   CloseCnt=0;
   while(CloseCnt < 3)
     {
      if (OrderClose(ticket,numLots,close_price,Slippage,Violet))
        {
         CloseCnt=3;
        }
      else
        {
         err=GetLastError();
         Print(CloseCnt," Error closing order : (", err , ") " + ErrorDescription(err));
         if (err > 0) CloseCnt++;
        }
     }
  }
//+------------------------------------------------------------------+
//|  Modify Open Position Controls                                   |
//|  Try to modify position 3 times                                  |
//+------------------------------------------------------------------+
void ModifyOrder(int ord_ticket,double op, double price,double tp)
  {
   int CloseCnt, err;
   //
   CloseCnt=0;
   while(CloseCnt < 3)
     {
      if (OrderModify(ord_ticket,op,price,tp,0,Aqua))
        {
         CloseCnt=3;
        }
      else
        {
         err=GetLastError();
         Print(CloseCnt," Error modifying order : (", err , ") " + ErrorDescription(err));
         if (err>0) CloseCnt++;
        }
     }
  }
//+------------------------------------------------------------------+
//| HandleTrailingStop                                               |
//| Type 1 moves the stoploss without delay.                         |
//| Type 2 waits for price to move the amount of the trailStop       |
//| before moving stop loss then moves like type 1                   |
//| Type 3 uses up to 3 levels for trailing stop                     |
//|      Level 1 Move stop to 1st level                              |
//|      Level 2 Move stop to 2nd level                              |
//|      Level 3 Trail like type 1 by fixed amount other than 1      |
//| Possible future types                                            |
//| Type 4 uses 2 for 1, every 2 pip move moves stop 1 pip           |
//| Type 5 uses 3 for 1, every 3 pip move moves stop 1 pip           |
//+------------------------------------------------------------------+
int HandleTrailingStop(string type, int ticket, double op, double os, double tp)
  {
   double pt, TS=0;
   double bos,bop,opa,osa;
   //
   if (type=="BUY")
     {
      switch(TrailingStopType)
        {
         case 1: pt=Point*StopLoss;
            if(Bid-os > pt) ModifyOrder(ticket,op,Bid-pt,tp);
            break;
         case 2: pt=Point*TrailingStop;
            if(Bid-op > pt && os < Bid - pt) ModifyOrder(ticket,op,Bid - pt,tp);
            break;
         case 3: if (Bid - op > TRStopLevel_1 * Point)
              {
               TS=op + TRStopLevel_1*Point - TrailingStop1 * Point;
               if (os < TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (Bid - op > TRStopLevel_2 * Point)
              {
               TS=op + TRStopLevel_2*Point - TrailingStop2 * Point;
               if (os < TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (Bid - op > TRStopLevel_3 * Point)
              {
               //                   TS = op + TRStopLevel_3 * Point - TrailingStop3*Point;
               TS=Bid  - TrailingStop3*Point;
               if (os < TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            break;
        }
      return(0);
     }
   if (type== "SELL")
     {
      switch(TrailingStopType)
        {
         case 1: pt=Point*StopLoss;
            if(os - Ask > pt) ModifyOrder(ticket,op,Ask+pt,tp);
            break;
         case 2: pt=Point*TrailingStop;
            if(op - Ask > pt && os > Ask+pt) ModifyOrder(ticket,op,Ask+pt,tp);
            break;
         case 3: if (op - Ask > TRStopLevel_1 * Point)
              {
               TS=op - TRStopLevel_1 * Point + TrailingStop1 * Point;
               if (os > TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (op - Ask > TRStopLevel_2 * Point)
              {
               TS=op - TRStopLevel_2 * Point + TrailingStop2 * Point;
               if (os > TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (op - Ask > TRStopLevel_3 * Point)
              {
               //                  TS = op - TRStopLevel_3 * Point + TrailingStop3 * Point;               
               TS=Ask + TrailingStop3 * Point;
               if (os > TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            break;
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Handle Open Positions                                            |
//| Check if any open positions need to be closed or modified        |
//+------------------------------------------------------------------+
int HandleOpenPositions()
  {
   int cnt;
   bool YesClose;
   double pt;
//----
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
      OrderSelect (cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()!=Symbol()) continue;
      if(OrderMagicNumber()!=MagicNumber)  continue;
      if(OrderType()==OP_BUY)
        {
         if (CheckExitCondition("BUY"))
           {
            CloseOrder(OrderTicket(),OrderLots(),Bid);
           }
         else
           {
            if (UseTrailingStop)
              {
               HandleTrailingStop("BUY",OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit());
              }
           }
        }
      if(OrderType()==OP_SELL)
        {
         if (CheckExitCondition("SELL"))
           {
            CloseOrder(OrderTicket(),OrderLots(),Ask);
           }
         else
           {
            if(UseTrailingStop)
              {
               HandleTrailingStop("SELL",OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit());
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Get number of lots for this trade                                |
//+------------------------------------------------------------------+
double GetLots()
  {
   double lot;
//----
   lot=Lots;
   if(MoneyManagement)
     {
      lot=LotsOptimized();
     }
   if (lot>=1.0) lot=MathFloor(lot); else lot=1.0;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
//---- select lot size
   lot=NormalizeDouble(MathFloor(AccountFreeMargin()*TradeSizePercent/10000)/10,1);
   // Check if mini or standard Account
   if (lot < 1.0) lot=1.0;
   if (lot > MaxLots) lot=MaxLots;
//----
   return(lot);
  }
//+------------------------------------------------------------------+
//| Time frame interval appropriation  function                      |
//+------------------------------------------------------------------+
  int func_TimeFrame_Const2Val(int Constant)
  {
     switch(Constant) 
     {
         case 1:  // M1
            return(1);
         case 5:  // M5
            return(2);
         case 15:
            return(3);
         case 30:
            return(4);
         case 60:
            return(5);
         case 240:
            return(6);
         case 1440:
            return(7);
         case 10080:
            return(8);
         case 43200:
            return(9);
        }
     }
         //+------------------------------------------------------------------+
         //| Time frame string appropriation  function                        |
         //+------------------------------------------------------------------+
           string func_TimeFrame_Val2String(int Value)
           {
              switch(Value) 
              {
                  case 1:  // M1
                     return("PERIOD_M1");
                  case 2:  // M1
                     return("PERIOD_M5");
                  case 3:
                     return("PERIOD_M15");
                  case 4:
                     return("PERIOD_M30");
                  case 5:
                     return("PERIOD_H1");
                  case 6:
                     return("PERIOD_H4");
                  case 7:
                     return("PERIOD_D1");
                  case 8:
                     return("PERIOD_W1");
                  case 9:
                     return("PERIOD_MN1");
                  default:
                     return("undefined " + Value);
                 }
              }
                  //+------------------------------------------------------------------+
                  //|                                                                  |
                  //+------------------------------------------------------------------+
                    int func_Symbol2Val(string symbol) 
                    {
                     string mySymbol=StringSubstr(symbol,0,6);
                     if(mySymbol=="AUDCAD") return(1);
                     if(mySymbol=="AUDJPY") return(2);
                     if(mySymbol=="AUDNZD") return(3);
                     if(mySymbol=="AUDUSD") return(4);
                     if(mySymbol=="CHFJPY") return(5);
                     if(mySymbol=="EURAUD") return(6);
                     if(mySymbol=="EURCAD") return(7);
                     if(mySymbol=="EURCHF") return(8);
                     if(mySymbol=="EURGBP") return(9);
                     if(mySymbol=="EURJPY") return(10);
                     if(mySymbol=="EURUSD") return(11);
                     if(mySymbol=="GBPCHF") return(12);
                     if(mySymbol=="GBPJPY") return(13);
                     if(mySymbol=="GBPUSD") return(14);
                     if(mySymbol=="NZDUSD") return(15);
                     if(mySymbol=="USDCAD") return(16);
                     if(mySymbol=="USDCHF") return(17);
                     if(mySymbol=="USDJPY") return(18);
                     return(19);
                    }
                  //+------------------------------------------------------------------+
                  //| return error description                                         |
                  //+------------------------------------------------------------------+
                  string ErrorDescription(int error_code)
                    {
                     string error_string;
                  //----
                     switch(error_code)
                       {
                        //---- codes returned from trade server
                        case 0:
                        case 1:   error_string="no error";                                                  break;
                        case 2:   error_string="common error";                                              break;
                        case 3:   error_string="invalid trade parameters";                                  break;
                        case 4:   error_string="trade server is busy";                                      break;
                        case 5:   error_string="old version of the client terminal";                        break;
                        case 6:   error_string="no connection with trade server";                           break;
                        case 7:   error_string="not enough rights";                                         break;
                        case 8:   error_string="too frequent requests";                                     break;
                        case 9:   error_string="malfunctional trade operation";                             break;
                        case 64:  error_string="account disabled";                                          break;
                        case 65:  error_string="invalid account";                                           break;
                        case 128: error_string="trade timeout";                                             break;
                        case 129: error_string="invalid price";                                             break;
                        case 130: error_string="invalid stops";                                             break;
                        case 131: error_string="invalid trade volume";                                      break;
                        case 132: error_string="market is closed";                                          break;
                        case 133: error_string="trade is disabled";                                         break;
                        case 134: error_string="not enough money";                                          break;
                        case 135: error_string="price changed";                                             break;
                        case 136: error_string="off quotes";                                                break;
                        case 137: error_string="broker is busy";                                            break;
                        case 138: error_string="requote";                                                   break;
                        case 139: error_string="order is locked";                                           break;
                        case 140: error_string="long positions only allowed";                               break;
                        case 141: error_string="too many requests";                                         break;
                        case 145: error_string="modification denied because order too close to market";     break;
                        case 146: error_string="trade context is busy";                                     break;
                        //---- mql4 errors
                        case 4000: error_string="no error";                                                 break;
                        case 4001: error_string="wrong function pointer";                                   break;
                        case 4002: error_string="array index is out of range";                              break;
                        case 4003: error_string="no memory for function call stack";                        break;
                        case 4004: error_string="recursive stack overflow";                                 break;
                        case 4005: error_string="not enough stack for parameter";                           break;
                        case 4006: error_string="no memory for parameter string";                           break;
                        case 4007: error_string="no memory for temp string";                                break;
                        case 4008: error_string="not initialized string";                                   break;
                        case 4009: error_string="not initialized string in array";                          break;
                        case 4010: error_string="no memory for array\' string";                             break;
                        case 4011: error_string="too long string";                                          break;
                        case 4012: error_string="remainder from zero divide";                               break;
                        case 4013: error_string="zero divide";                                              break;
                        case 4014: error_string="unknown command";                                          break;
                        case 4015: error_string="wrong jump (never generated error)";                       break;
                        case 4016: error_string="not initialized array";                                    break;
                        case 4017: error_string="dll calls are not allowed";                                break;
                        case 4018: error_string="cannot load library";                                      break;
                        case 4019: error_string="cannot call function";                                     break;
                        case 4020: error_string="expert function calls are not allowed";                    break;
                        case 4021: error_string="not enough memory for temp string returned from function"; break;
                        case 4022: error_string="system is busy (never generated error)";                   break;
                        case 4050: error_string="invalid function parameters count";                        break;
                        case 4051: error_string="invalid function parameter value";                         break;
                        case 4052: error_string="string function internal error";                           break;
                        case 4053: error_string="some array error";                                         break;
                        case 4054: error_string="incorrect series array using";                             break;
                        case 4055: error_string="custom indicator error";                                   break;
                        case 4056: error_string="arrays are incompatible";                                  break;
                        case 4057: error_string="global variables processing error";                        break;
                        case 4058: error_string="global variable not found";                                break;
                        case 4059: error_string="function is not allowed in testing mode";                  break;
                        case 4060: error_string="function is not confirmed";                                break;
                        case 4061: error_string="send mail error";                                          break;
                        case 4062: error_string="string parameter expected";                                break;
                        case 4063: error_string="integer parameter expected";                               break;
                        case 4064: error_string="double parameter expected";                                break;
                        case 4065: error_string="array as parameter expected";                              break;
                        case 4066: error_string="requested history data in update state";                   break;
                        case 4099: error_string="end of file";                                              break;
                        case 4100: error_string="some file error";                                          break;
                        case 4101: error_string="wrong file name";                                          break;
                        case 4102: error_string="too many opened files";                                    break;
                        case 4103: error_string="cannot open file";                                         break;
                        case 4104: error_string="incompatible access to a file";                            break;
                        case 4105: error_string="no order selected";                                        break;
                        case 4106: error_string="unknown symbol";                                           break;
                        case 4107: error_string="invalid price parameter for trade function";               break;
                        case 4108: error_string="invalid ticket";                                           break;
                        case 4109: error_string="trade is not allowed";                                     break;
                        case 4110: error_string="longs are not allowed";                                    break;
                        case 4111: error_string="shorts are not allowed";                                   break;
                        case 4200: error_string="object is already exist";                                  break;
                        case 4201: error_string="unknown object property";                                  break;
                        case 4202: error_string="object is not exist";                                      break;
                        case 4203: error_string="unknown object type";                                      break;
                        case 4204: error_string="no object name";                                           break;
                        case 4205: error_string="object coordinates error";                                 break;
                        case 4206: error_string="no specified subwindow";                                   break;
                        default:   error_string="unknown error";
                       }
                     //----
                     return(error_string);
                    }
//+------------------------------------------------------------------+