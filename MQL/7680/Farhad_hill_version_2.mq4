//+---------------------------------------------------------------------------+
//|     Farhad.mq4                                                            |
//|     Copyright © 2006, Farhad Farshad                                      |
//|     http://www.rahbord-investment.com                                     |
//|     http://farhadfarshad.Com                                              |
//|     This EA is optimized to work on                                       |
//|     GBP/JPY ... if you want the optimized                                 |
//|     EA s for any currency pair please                                     |
//|     mail me at: info@farhadfarshad.Com                                    |
//|     This is the first version of this EA. If                              |
//|     you want the second edition (Farhad2.mq4)                             |
//|     with considerably better performance mail me.                         |
//|     Enjoy a better automatic investment:) with at least 70% a month.      |
//|                                                                           |
//| Modified by Robert Hill to allow optimization for different currency pairs|
//+---------------------------------------------------------------------------+
#property copyright "Copyright © 2006, Farhad Farshad"
#property link      "http://www.rahbord-investment.com"
#include <stdlib.mqh>
//----
extern bool AccountIsMini=false;       // Change to true if trading mini account
extern bool MoneyManagement=false;     // Change to false to shutdown money management controls.
//----                                 // Lots = 1 will be in effect and only 1 lot will be open regardless of equity.
extern double TradeSizePercent=5;      // Change to whatever percent of equity you wish to risk.
extern double Lots        =1;          // you can change the lot but be aware of margin. Its better to trade with 1/4 of your capital. 
extern double MaxLots=100;
//+---------------------------------------------------+
//|Money Management                                   |
//+---------------------------------------------------+
//extern double stopLoss     = 0;      // do not use s/l at all. Take it easy man. I'll guarantee your profit :)
extern double StopLoss=0;              // Maximum pips willing to lose per position.
extern double TrailingStop=15;         // Change to whatever number of pips you wish to trail your position with.
extern bool UseTrailingStop=true;
extern int TrailingStopType=2;         // Type 1 moves stop immediately, Type 2 waits til value of TS is reached
extern double FirstMove=20;            // Type 3  first level pip gain
extern double TrailingStop1=20;        // Move Stop to Breakeven
extern double SecondMove=30;           // Type 3 second level pip gain
extern double TrailingStop2=20;        // Move stop to lock is profit
extern double ThirdMove=40;            // type 3 third level pip gain
extern double TrailingStop3=20;        // Move stop and trail from there
extern int TakeProfit=20;              // Maximum profit level achieved. recomended  no more than 20
extern double MarginCutoff=300;        // Expert will stop trading if equity level decreases to that level.
extern int Slippage=3;                 // Possible fix for not getting closed Could be higher with some brokers    
//+---------------------------------------------------+
//|Indicator Variables                                |
//| Change these to try your own system               |
//| or add more if you like                           |
//+---------------------------------------------------+
// Mode : 0=sma, 1=ema, 2=smma, 3=lwma, 4=LSMA
// Price : 0=close, 1=open, 2=high, 3=low, 4=median((h+l/2)), 5=typical((h+l+c)/3), 6=weighted((h+l+c+c)/4)
// All settings are the defaults from Farhad version 1
extern bool UseMACD=true;
extern int MACD_Price=1;
extern bool UseMA_Cross=false;
extern int MA_SlowPeriod=21;
extern int MA_FastPeriod=2;
extern int MA_Shift=1;
extern int MA_Mode=2;
extern int MA_Price=5;
extern bool UseMomentum=true;
extern bool UsePSAR=true;
extern int MomentumPeriod=14;
extern int MomentumPrice=1;
extern double MomentumHigh=100;
extern double MomentumLow=100;
extern bool UseStochLevel=true;
extern int Stoch_Mode=0;
extern int StochPrice=0;   // 0, 1
extern double StochHigh=60;
extern double StochLow=35;
extern bool UseStochCross=false;
extern int SignalCandle=0;
extern int SignalTimeFrame=0;
//----
double macdHistCurrent, macdHistPrevious, macdSignalCurrent, macdSignalPrevious, highCurrent, lowCurrent;
double stochHistCurrent, stochHistPrevious, stochSignalCurrent, stochSignalPrevious;
double sarCurrent, sarPrevious,  momCurrent, momPrevious;
double maLongCurrent, maShortCurrent, maLongPrevious, maShortPrevious;
//----
int cnt, ticket;
int MagicNumber;        // Magic EA identifier. Allows for several co-existing EA with different input values
string ExpertName;      // To "easy read" which EA place an specific order and remember me forever :)
double lotMM;
int TradesInThisSymbol;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
  int init() 
  {
   MagicNumber=3000 + func_Symbol2Val(Symbol())*100 + func_TimeFrame_Const2Val(Period());
   ExpertName="Farhad3Hill - Magic: " + MagicNumber + " : " + Symbol() + "_" + func_TimeFrame_Val2String(func_TimeFrame_Const2Val(Period()));
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
//| Check if any rules are met for close of trade                    |
//| This EA closes trades by hitting StopLoss or TrailingStop        |
//| No Exit rules so always return false                             |
//+------------------------------------------------------------------+
bool CheckExitCondition(string TradeType)
  {
   bool YesClose;
   //
   YesClose=false;
/*
   calculateIndicators();                      // Calculate indicators' value   
   // Check for BUY, SELL, and CLOSE signal
   isBuying  = (sarCurrent<=Ask && sarPrevious>sarCurrent && momCurrent<MomentumLow && macdHistCurrent<macdSignalCurrent && stochHistCurrent<StochLow);
   isSelling = (sarCurrent>=Bid && sarPrevious<sarCurrent && momCurrent>MomentumHigh && macdHistCurrent>macdSignalCurrent && stochHistCurrent>StochHigh);
    // Check for cross down
   if (TradeType == "BUY" && isSelling) YesClose = true;
    // Check for cross up
   if (TradeType == "SELL" && isBuying) YesClose = true;

*/
   return(YesClose);
  }
//+------------------------------------------------------------------+
//| CheckEntryCondition                                              |
//| Check if rules are met for Buy trade                             |
//+------------------------------------------------------------------+
bool CheckEntryConditionBUY()
  {
   // calculateIndicators();                      // Calculate indicators' value
   // Moved code here for faster running
   // Check each rule for false for faster return

   // commented out what is not used

   //void calculateIndicators() {    // Calculate indicators' value   

   if (UseMACD)
     {
      // Check MACD Rules
      macdHistCurrent    =iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_MAIN,SignalCandle);
      //   macdHistPrevious    = iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_MAIN,SignalCandle+1);   
      macdSignalCurrent  =iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_SIGNAL,SignalCandle);
      //   macdSignalPrevious  = iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_SIGNAL,SignalCandle+1);
      if (macdHistCurrent>=macdSignalCurrent) return(false);
     }
   // Check Stochastic rules
   if (UseStochLevel)
     {
      stochHistCurrent   =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_MAIN,SignalCandle);
      if (stochHistCurrent>=StochLow) return(false);
     }
   if (UseStochCross)
     {
      stochHistCurrent   =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_MAIN,SignalCandle);
      if (stochHistCurrent>=StochLow) return(false);
      stochSignalCurrent =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_SIGNAL,SignalCandle);
      stochHistPrevious  =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_MAIN,SignalCandle+1);
      stochSignalPrevious=iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_SIGNAL,SignalCandle+1);
      if (stochSignalCurrent>=stochHistCurrent) return(false);
      if (stochSignalPrevious<=stochHistPrevious) return(false);
     }
   if (UsePSAR)
     {
      // Check Paraboloc SAR rules

      sarCurrent         =iSAR(NULL,SignalTimeFrame,0.02,0.2,SignalCandle);           // Parabolic Sar Current
      if (sarCurrent>Ask) return(false);
      sarPrevious        =iSAR(NULL,SignalTimeFrame,0.02,0.2,SignalCandle+1);         //Parabolic Sar Previous
      if (sarPrevious<=sarCurrent) return(false);
     }
   if (UseMomentum)
     {
      // Check Momentum rules
      momCurrent         =iMomentum(NULL,SignalTimeFrame,MomentumPeriod,PRICE_OPEN,SignalCandle); // Momentum Current
      //   momPrevious         = iMomentum(NULL,SignalTimeFrame,MomentumPeriod,PRICE_OPEN,SignalCandle+1); // Momentum Previous
      if(momCurrent>=MomentumLow) return(false);
     }
   //   highCurrent         = iHigh(NULL,SignalTimeFrame,SignalCandle);     //High price Current
   //   lowCurrent          = iLow(NULL,SignalTimeFrame,SignalCandle);      //Low Price Current
   if (UseMA_Cross)
     {
      if (MA_Mode==4)
        {
         maLongCurrent=LSMA(MA_SlowPeriod,MA_Price,SignalTimeFrame,SignalCandle);
         //        maLongPrevious = LSMA(MA_SlowPeriod,MA_Price,SignalTimeFrame,SignalCandle+1);
         maShortCurrent=LSMA(MA_FastPeriod,MA_Price,SignalTimeFrame,SignalCandle);
         //        maShortPrevious = LSMA(MA_FastPeriod,MA_Price,SignalTimeFrame,SignalCandle+1);
        }
      else
        {
         maLongCurrent      =iMA(NULL,SignalTimeFrame,MA_SlowPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle);
         //        maLongPrevious      = iMA(NULL,SignalTimeFrame,MA_SlowPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle+1); 
         maShortCurrent     =iMA(NULL,SignalTimeFrame,MA_FastPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle);
         //        maShortPrevious     = iMA(NULL,SignalTimeFrame,MA_FastPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle+1); 
        }
      // Check if MA Cross and separation is showing a sell so do not take a buy
      if (maShortCurrent<=maLongCurrent) return(false);
     }
   // }
   // Check for BUY, SELL, and CLOSE signal
   //   isBuying  = (sarCurrent<=Ask && sarPrevious>sarCurrent && momCurrent<MomentumLow && macdHistCurrent<macdSignalCurrent && stochHistCurrent<StochLow);
   // If we get this far all rules are met
   return(true);
  }
//+------------------------------------------------------------------+
//| CheckEntryCondition                                              |
//| Check if rules are met for open of trade                         |
//+------------------------------------------------------------------+
bool CheckEntryConditionSELL()
  {
   // calculateIndicators();                      // Calculate indicators' value
   // Moved code here for faster running
   // Check each rule for false for faster return
   // commented out what is not used
   // void calculateIndicators() {    // Calculate indicators' value   
   if (UseMACD)
     {
      // Check MACD Rules
      macdHistCurrent    =iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_MAIN,SignalCandle);
      //   macdHistPrevious    = iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_MAIN,SignalCandle+1);   
      macdSignalCurrent  =iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_SIGNAL,SignalCandle);
      //   macdSignalPrevious  = iMACD(NULL,SignalTimeFrame,12,26,9,MACD_Price,MODE_SIGNAL,SignalCandle+1); 
      if (macdHistCurrent<=macdSignalCurrent) return(false);
     }
   // Check Stochastic rules
   if (UseStochLevel)
     {
      stochHistCurrent   =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_MAIN,SignalCandle);
      if (stochHistCurrent<=StochHigh) return(false);
     }
   if (UseStochCross)
     {
      stochHistCurrent   =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_MAIN,SignalCandle);
      stochHistPrevious  =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_MAIN,SignalCandle+1);
      stochSignalCurrent =iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_SIGNAL,SignalCandle);
      stochSignalPrevious=iStochastic(NULL,SignalTimeFrame,5,3,3,Stoch_Mode,StochPrice,MODE_SIGNAL,SignalCandle+1);
      if (stochHistCurrent<=StochHigh) return(false);
      if (stochSignalCurrent<=stochHistCurrent) return(false);
      if (stochSignalPrevious>=stochHistPrevious) return(false);
     }
   if (UsePSAR)
     {
      // Check Paraboloc SAR rules
      sarCurrent         =iSAR(NULL,SignalTimeFrame,0.02,0.2,SignalCandle);           // Parabolic Sar Current
      if (sarCurrent< Bid) return(false);
      sarPrevious        =iSAR(NULL,SignalTimeFrame,0.02,0.2,SignalCandle+1);         //Parabolic Sar Previous
      if (sarPrevious>=sarCurrent) return(false);
     }
   if (UseMomentum)
     {
      // Check Momentum rules
      momCurrent         =iMomentum(NULL,SignalTimeFrame,MomentumPeriod,PRICE_OPEN,SignalCandle); // Momentum Current
      momPrevious        =iMomentum(NULL,SignalTimeFrame,MomentumPeriod,PRICE_OPEN,SignalCandle+1); // Momentum Previous
      if(momCurrent<=MomentumHigh) return(false);
     }
   //   highCurrent         = iHigh(NULL,SignalTimeFrame,SignalCandle);     //High price Current
   //   lowCurrent          = iLow(NULL,SignalTimeFrame,SignalCandle);      //Low Price Current
   if (UseMA_Cross)
     {
      if (MA_Mode==4)
        {
         maLongCurrent=LSMA(MA_SlowPeriod,MA_Price,SignalTimeFrame,SignalCandle);
         //        maLongPrevious = LSMA(MA_SlowPeriod,MA_Price,SignalTimeFrame,SignalCandle+1);
         maShortCurrent=LSMA(MA_FastPeriod,MA_Price,SignalTimeFrame,SignalCandle);
         //        maShortPrevious = LSMA(MA_FastPeriod,MA_Price,SignalTimeFrame,SignalCandle+1);
        }
      else
        {
         maLongCurrent      =iMA(NULL,SignalTimeFrame,MA_SlowPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle);
         //        maLongPrevious      = iMA(NULL,SignalTimeFrame,MA_SlowPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle+1); 
         maShortCurrent     =iMA(NULL,SignalTimeFrame,MA_FastPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle);
         //        maShortPrevious     = iMA(NULL,SignalTimeFrame,MA_FastPeriod,MA_Shift,MA_Mode,MA_Price,SignalCandle+1); 
        }
      // Check if MA Cross and separation is showing a buy so do not take a sell
      if (maShortCurrent>=maLongCurrent) return(false);
     }
   // Check for BUY, SELL, and CLOSE signal
   //   isSelling = (sarCurrent>=Bid && sarPrevious<sarCurrent && momCurrent>MomentumHigh && macdHistCurrent>macdSignalCurrent && stochHistCurrent>StochHigh);
   // If we get this far all rules are met
   return(true);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
  int start() 
  {
   // Check if any open positions
   HandleOpenPositions();
   TradesInThisSymbol=openPositions();
   //+------------------------------------------------------------------+
   //| Check if OK to make new trades                                   |
   //+------------------------------------------------------------------+
   // Only allow 1 trade per Symbol
     if(TradesInThisSymbol > 0) 
     {
     return(0);}
   // If there is no open trade for this pair and this EA
     if(AccountFreeMargin() < MarginCutoff) 
     {
      Print("Not enough money to trade Strategy:", ExpertName);
      return(0);
     }
   lotMM=GetLots();
   //   if(isBuying && !isSelling && !isBuyClosing && !isSellClosing) {  // Check for BUY entry signal
   if(CheckEntryConditionBUY() )
     {
      OpenBuyOrder();
     }
   // if(isSelling && !isBuying && !isBuyClosing && !isSellClosing) {  // Check for SELL entry signal
   if (CheckEntryConditionSELL())
     {
      OpenSellOrder();
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| OpenBuyOrder                                                     |
//| If Stop Loss or TakeProfit are used the values are calculated    |
//| for each trade                                                   |
//+------------------------------------------------------------------+
void OpenBuyOrder()
  {
   int err,ticket;
   double myStopLoss=0, myTakeProfit=0;
   //----
   myStopLoss=0;
   if(StopLoss > 0)myStopLoss=Ask - StopLoss * Point ;
   myTakeProfit=0;
   if (TakeProfit>0) myTakeProfit=Ask + TakeProfit * Point;
   ticket=OrderSend(Symbol(),OP_BUY,lotMM,Ask,Slippage,myStopLoss,myTakeProfit,ExpertName,MagicNumber,0,Green);
   prtAlert ("FarhadHill : OpenBuy for " + DoubleToStr(Ask,4));
   if(ticket<=0)
     {
      err=GetLastError();
      Print("Error opening BUY order [" + ExpertName + "]: (" + err + ") " + ErrorDescription(err));
     }
  }
//+------------------------------------------------------------------+
//| OpenSellOrder                                                    |
//| If Stop Loss or TakeProfit are used the values are calculated    |
//| for each trade                                                   |
//+------------------------------------------------------------------+
void OpenSellOrder()
  {
   int err, ticket;
   double myStopLoss=0, myTakeProfit=0;
//----
   myStopLoss=0;
   if(StopLoss > 0)myStopLoss=Bid + StopLoss * Point;
   myTakeProfit=0;
   if (TakeProfit > 0) myTakeProfit=Bid - TakeProfit * Point;
   ticket=OrderSend(Symbol(),OP_SELL,lotMM,Bid,Slippage,myStopLoss,myTakeProfit,ExpertName,MagicNumber,0,Red);
   prtAlert("FarhadHill : OpenSell for " + DoubleToStr(Bid,4));
   if(ticket<=0)
     {
      err=GetLastError();
      Print("Error opening Sell order [" + ExpertName + "]: (" + err + ") " + ErrorDescription(err));
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
//|                                                                  |
//+------------------------------------------------------------------+
  void prtAlert(string str="") 
  {
   Print(Symbol() + " - " + str);
   Alert(Symbol() + " - " + str);
  }
//+------------------------------------------------------------------+
//| Close Open Position Controls                                     |
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
   if (type=="BUY")
     {
      switch(TrailingStopType)
        {
         case 1: pt=Point*StopLoss;
            if(Bid-os > pt) ModifyOrder(ticket,op,Bid-pt,tp);
            break;
         case 2: if (TrailingStop > 0)
              {
               pt=Point*TrailingStop;
               if(Bid-op > pt && os < Bid - pt) ModifyOrder(ticket,op,Bid - pt,tp);
              }
            break;
         case 3: if (Bid - op > FirstMove * Point)
              {
               TS=op + FirstMove*Point - TrailingStop1 * Point;
               if (os < TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (Bid - op > SecondMove * Point)
              {
               TS=op + SecondMove*Point - TrailingStop2 * Point;
               if (os < TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (Bid - op > ThirdMove * Point)
              {
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
         case 2: if (TrailingStop > 0)
              {
               pt=Point*TrailingStop;
               if(op - Ask > pt && os > Ask+pt) ModifyOrder(ticket,op,Ask+pt,tp);
              }
            break;
         case 3: if (op - Ask > FirstMove * Point)
              {
               TS=op - FirstMove * Point + TrailingStop1 * Point;
               if (os > TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (op - Ask > SecondMove * Point)
              {
               TS=op - SecondMove * Point + TrailingStop2 * Point;
               if (os > TS)
                 {
                  ModifyOrder(ticket,op,TS,tp);
                 }
              }
            if (op - Ask > ThirdMove * Point)
              {
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
   if(MoneyManagement)
     {
      lot=LotsOptimized();
     }
     else 
     {
      lot=Lots;
      if(AccountIsMini)
        {
         if (lot > 1.0) lot=lot/10;
         if (lot < 0.1) lot=0.1;
        }
     }
     //----
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
   if(AccountIsMini)
     {
      lot=MathFloor(lot*10)/10;
      // Use at least 1 mini lot
      if(lot<0.1) lot=0.1;
      if (lot > MaxLots) lot=MaxLots;
     }
     else
     {
      if (lot < 1.0) lot=1.0;
      if (lot > MaxLots) lot=MaxLots;
     }
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
                       if(symbol=="AUDCAD") 
                       {
                        return(1);
                        }
                         else if(symbol=="AUDJPY") 
                        {
                           return(2);
                           }
                            else if(symbol=="AUDNZD") 
                           {
                           return(3);
                           }
                            else if(symbol=="AUDUSD") 
                           {
                           return(4);
                           }
                            else if(symbol=="CHFJPY") 
                           {
                           return(5);
                           }
                            else if(symbol=="EURAUD") 
                           {
                           return(6);
                           }
                            else if(symbol=="EURCAD") 
                           {
                           return(7);
                           }
                            else if(symbol=="EURCHF") 
                           {
                           return(8);
                           }
                            else if(symbol=="EURGBP") 
                           {
                           return(9);
                           }
                            else if(symbol=="EURJPY") 
                           {
                           return(10);
                           }
                            else if(symbol=="EURUSD") 
                           {
                           return(11);
                           }
                            else if(symbol=="GBPCHF") 
                            {
                           return(12);
                           }
                            else if(symbol=="GBPJPY") 
                           {
                           return(13);
                           }
                            else if(symbol=="GBPUSD") 
                           {
                           return(14);
                           }
                            else if(symbol=="NZDUSD") 
                           {
                           return(15);
                           }
                            else if(symbol=="USDCAD") 
                           {
                           return(16);
                           }
                            else if(symbol=="USDCHF") 
                           {
                           return(17);
                           }
                            else if(symbol=="USDJPY") 
                           {
                           return(18);
                           }
                            else 
                           {
                        Comment("unexpected Symbol");
                        return(0);
                       }
                    }
//+------------------------------------------------------------------+