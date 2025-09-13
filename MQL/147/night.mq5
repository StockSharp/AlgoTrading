//+------------------------------------------------------------------+
//|                                                        Night.mq5 |
//|                                       Copyright 2010, AM2 Group. |
//|                                         http://www.am2_group.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, AM2 Group."
#property link      "http://www.am2_group.net"
#property version   "1.00"

//--- input parameters
input int      StopLoss=40;        // Stop Loss
input int      TakeProfit=20;      // Take Profit  
input int      Stoch_OverSold=30;  // Stochastic oversold level
input int      Stoch_OverBought=70;// Stochastic overbought level
input double   Lot=1;              // Lots to trade
input int      EA_Magic=1072010;   // Magic Number

//--- global variables
int stochHandle;                   // Handle of the Stochastic indicator
double stochVal[];                 // array to serve the value of Stochastic indicator
int STP,TKP;                       // will be used for Stop Loss è Take Profit
double Lots=0.1;                   // Lots to Trade
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- Get handle for Stochastic indicator
   stochHandle=iStochastic(NULL,PERIOD_M15,5,3,3,MODE_EMA,STO_CLOSECLOSE);
//---
//--- Let us handle brokers that offers 3/5 digit prices
   STP = StopLoss;
   TKP = TakeProfit;
   if(_Digits==5 || _Digits==3)
     {
      STP = STP*10;
      TKP = TKP*10;
     }
//--- memory allocation
   ArrayResize(stochVal,5);
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
//--- Release our indicator handle
   IndicatorRelease(stochHandle);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
//--- Define some MQL5 Structures we will use for our trade
   MqlTradeRequest mrequest;   // To be used for sending trade requests
   MqlTradeResult mresult;     // To be used to get trade results  
   MqlDateTime dt;
   datetime t[];
   ZeroMemory(mrequest);
   ZeroMemory(mresult);

// let's serialize the buffers copied
   ArraySetAsSeries(stochVal,true);
   ArraySetAsSeries(t,true);
   TimeCurrent(dt);

   int i=(dt.hour+1)*60;
   if(CopyTime(Symbol(),0,0,i,t)<0)
     {
      Print("Error copying time data !");
      return;
     }
//--- Get historical data of the last 3 bars
   if(CopyBuffer(stochHandle,0,0,3,stochVal)<0)
     {
      Alert("Error copying of Stochastic indicator buffer - error:",GetLastError());
      return;
     }
/*
    1. Checking of buy conditions:
    Stochastic has fall below the oversold level
*/
   double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);            // best ask
   double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);            // bets bid

//--- declare the variable of boolean type, that will be used for checking trade conditions

   bool Buy_Condition=(stochVal[1]<Stoch_OverSold);               // Stochastic is below the oversold level

//--- combine all together

   if(Buy_Condition && (!PositionSelect(_Symbol)))
     {
      if(dt.hour>=21 || dt.hour<6)
        {
         mrequest.action = TRADE_ACTION_DEAL;                       // immediate order execution
         mrequest.price = Ask;                                      // last ask price
         mrequest.sl = NormalizeDouble(Ask - STP*_Point,_Digits);   // Stop Loss
         mrequest.tp = NormalizeDouble(Ask + TKP*_Point,_Digits);   // Take Profit
         mrequest.symbol = _Symbol;                                 // symbol
         mrequest.volume = CalculateVolume();                       // trade volume
         mrequest.magic = EA_Magic;                                 // Magic Number
         mrequest.type = ORDER_TYPE_BUY;                            // buy order
         mrequest.type_filling = ORDER_FILLING_FOK;                 // order filling type = all or none
         mrequest.deviation=5;                                      // slippage from the current price
         OrderSend(mrequest,mresult);                               // send order
                                                                    // trade server return code analysis
         if(mresult.retcode==10009 || mresult.retcode==10008)
           {
            Alert("A Buy order has been successfully placed with Ticket#:",mresult.order);
           }
         else
           {
            Alert("The Buy order request could not be completed -error:",GetLastError());
            ResetLastError();
            return;
           }
        }
     }
/*
    2. Checking for buy condition :
    Stochastic grow below the overbought level 80 and began to fall on hourly bar
*/

//--- declare the variable of boolean type, that will be used for checking of sell conditions
   bool Sell_Condition=(stochVal[1]>Stoch_OverBought);            // Stochastic is above the overbought level

//--- Combine all together
   if(Sell_Condition && (!PositionSelect(_Symbol)))
     {
      if(dt.hour>=21 || dt.hour<6)
        {
         mrequest.action = TRADE_ACTION_DEAL;                      // immediate order execution
         mrequest.price = Bid;                                     // last Bid price
         mrequest.sl = NormalizeDouble(Bid + STP*_Point,_Digits);  // Stop Loss
         mrequest.tp = NormalizeDouble(Bid - TKP*_Point,_Digits);  // Take Profit
         mrequest.symbol = _Symbol;                                // symbol
         mrequest.volume = CalculateVolume();                      // trade volume
         mrequest.magic = EA_Magic;                                // Magic Number
         mrequest.type= ORDER_TYPE_SELL;                           // sell order
         mrequest.type_filling = ORDER_FILLING_FOK;                // order filling type = all or none
         mrequest.deviation=5;                                     // slippage from the current price
         OrderSend(mrequest,mresult);                              // send order
                                                                   // trade server return code analysis
         if(mresult.retcode==10009 || mresult.retcode==10008)
           {
            Alert("A Sell order has been successfully placed with Ticket#:",mresult.order);
           }
         else
           {
            Alert("The Sell order request could not be completed -error:",GetLastError());
            ResetLastError();
            return;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Calculate Lot Volume                                             |
//+------------------------------------------------------------------+
double CalculateVolume()
  {
   Lots=AccountInfoDouble(ACCOUNT_FREEMARGIN)/100000*10;
   Lots=MathMin(5,MathMax(0.1,Lots));
   if(Lots<0.1)
      Lots=NormalizeDouble(Lots,2);
   else
     {
      if(Lots<1) Lots=NormalizeDouble(Lots,1);
      else       Lots=NormalizeDouble(Lots,0);
     }
   return(Lots);
  }
//+------------------------------------------------------------------+
