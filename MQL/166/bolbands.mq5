//+------------------------------------------------------------------+
//|                                                     BolBands.mq5 |
//|                                             Copyright 2010, AM2. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, AM2."
#property link      "http://www.mql5.com"
#property version   "1.05"
//--- input parameters
input int bands_period= 20;        // Bollinger Bands period
input int dema_period= 20;         // DEMA period
input int bands_shift = 0;         // Bollinger Bands shift
input double deviation= 2;         // Standard deviation
input double   Lot=0.5;            // Lots to trade
//--- global variables
int BolBandsHandle;                // Bolinger Bands handle
int demaHandle;                    // DEMA handle
double BBUp[],BBLow[],BBMidle[];   // dynamic arrays for numerical values of Bollinger Bands
double demaVal[];                  // dynamic array for numerical values of Moving Average 
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- Do we have sufficient bars to work
   if(Bars(_Symbol,_Period)<60) // total number of bars is less than 60?
     {
      Alert("We have less than 60 bars on the chart, an Expert Advisor terminated!!");
      return(-1);
     }
//--- get handle of the Bollinger Bands and DEMA indicators
   BolBandsHandle=iBands(NULL,PERIOD_M30,bands_period,bands_shift,deviation,PRICE_CLOSE);
   demaHandle=iDEMA(NULL,PERIOD_D1,dema_period,0,PRICE_CLOSE);
//--- Check for Invalid Handle
   if((BolBandsHandle<0) || (demaHandle<0))
     {
      Alert("Error in creation of indicators - error: ",GetLastError(),"!!");
      return(-1);
     }

   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- release indicator handles
   IndicatorRelease(BolBandsHandle);
   IndicatorRelease(demaHandle);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- we will use the static Old_Time variable to serve the bar time.
//--- at each OnTick execution we will check the current bar time with the saved one.
//--- if the bar time isn't equal to the saved time, it indicates that we have a new tick.

   static datetime Old_Time;
   datetime New_Time[1];
   bool IsNewBar=false;

//--- copying the last bar time to the element New_Time[0]
   int copied=CopyTime(_Symbol,_Period,0,1,New_Time);
   if(copied>0) // ok, the data has been copied successfully
     {
      if(Old_Time!=New_Time[0]) // if old time isn't equal to new bar time
        {
         IsNewBar=true;   // if it isn't a first call, the new bar has appeared
         if(MQL5InfoInteger(MQL5_DEBUGGING)) Print("We have new bar here ",New_Time[0]," old time was ",Old_Time);
         Old_Time=New_Time[0];            // saving bar time
        }
     }
   else
     {
      Alert("Error in copying historical times data, error =",GetLastError());
      ResetLastError();
      return;
     }

//--- EA should only check for new trade if we have a new bar
   if(IsNewBar==false)
     {
      return;
     }

//--- do we have enough bars to work with
   int Mybars=Bars(_Symbol,_Period);
   if(Mybars<60) // if total bars is less than 60 bars
     {
      Alert("We have less than 60 bars, EA will now exit!!");
      return;
     }

   MqlRates mrate[];          // To be used to store the prices, volumes and spread of each bar   

/*
     Let's make sure our arrays values for the Rates and Indicators 
     is stored serially similar to the timeseries array
*/

// the rates arrays
   ArraySetAsSeries(mrate,true);
   ArraySetAsSeries(demaVal,true);
// the indicator arrays
   ArraySetAsSeries(BBUp,true);
   ArraySetAsSeries(BBLow,true);
   ArraySetAsSeries(BBMidle,true);

//--- Get the details of the latest 3 bars
   if(CopyRates(_Symbol,_Period,0,3,mrate)<0)
     {
      Alert("Error copying rates/history data - error:",GetLastError(),"!!");
      return;
     }

//--- Copy the new values of our indicators to buffers (arrays) using the handle
   if(CopyBuffer(BolBandsHandle,0,0,3,BBMidle)<0 || CopyBuffer(BolBandsHandle,1,0,3,BBUp)<0
      || CopyBuffer(BolBandsHandle,2,0,3,BBLow)<0)
     {
      Alert("Error copying Bollinger Bands indicator Buffers - error:",GetLastError(),"!!");
      return;
     }

   if(CopyBuffer(demaHandle,0,0,3,demaVal)<0)
     {
      Alert("Error copying DEMA indicator buffer - error:",GetLastError());
      return;
     }

   double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);   // Ask price
   double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);   // Bid price

//--- Declare bool type variables to hold our Buy and Sell Conditions
   bool Buy_Condition =(mrate[1].close > BBLow[1] && mrate[1].open < BBLow[1] &&  // White (bull) candle crossed the Lower Band from below to above
                        demaVal[0]>demaVal[1] && demaVal[1]>demaVal[2]);          // and DEMA is growing up

   bool Sell_Condition = (mrate[1].close < BBUp[1] && mrate[1].open > BBUp[1] &&  // Black (bear) candle crossed the Upper Band from above to below
                          demaVal[0]<demaVal[1] && demaVal[1]<demaVal[2]);        // and DEMA is falling down

   bool Buy_Close=(mrate[1].close<BBUp[1] && mrate[1].open>BBUp[1]);              // Black candle crossed the Upper Band from above to below

   bool Sell_Close=(mrate[1].close>BBLow[1] && mrate[1].open<BBLow[1]);           // White candle crossed the Lower Band from below to above

   if(Buy_Condition && !PositionSelect(_Symbol))    // Open long position
     {                                              // DEÌÀ is growing up
      LongPositionOpen();                           // and white candle crossed the Lower Band from below to above
     }

   if(Sell_Condition && !PositionSelect(_Symbol))   // Open short position
     {                                              // DEÌÀ is falling down
      ShortPositionOpen();                          // and Black candle crossed the Upper Band from above to below
     }

   if(Buy_Close && PositionSelect(_Symbol))         // Close long position
     {                                              // Black candle crossed the Upper Band from above to below
      LongPositionClose();
     }

   if(Sell_Close && PositionSelect(_Symbol))        // Close short position
     {                                              // White candle crossed the Lower Band from below to above
      ShortPositionClose();
     }

   return;
  }
//+------------------------------------------------------------------+
//| Open Long position                                               |
//+------------------------------------------------------------------+
void LongPositionOpen()
  {
   MqlTradeRequest mrequest;                             // Will be used for trade requests
   MqlTradeResult mresult;                               // Will be used for results of trade requests
   
   ZeroMemory(mrequest);
   ZeroMemory(mresult);
   
   double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);    // Ask price
   double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);    // Bid price

   if(!PositionSelect(_Symbol))
     {
      mrequest.action = TRADE_ACTION_DEAL;               // Immediate order execution
      mrequest.price = NormalizeDouble(Ask,_Digits);     // Lastest Ask price
      mrequest.sl = 0;                                   // Stop Loss
      mrequest.tp = 0;                                   // Take Profit
      mrequest.symbol = _Symbol;                         // Symbol
      mrequest.volume = Lot;                             // Number of lots to trade
      mrequest.magic = 0;                                // Magic Number
      mrequest.type = ORDER_TYPE_BUY;                    // Buy Order
      mrequest.type_filling = ORDER_FILLING_FOK;         // Order execution type
      mrequest.deviation=5;                              // Deviation from current price
      OrderSend(mrequest,mresult);                       // Send order
     }
  }
//+------------------------------------------------------------------+
//| Open Short position                                              |
//+------------------------------------------------------------------+
void ShortPositionOpen()
  {
   MqlTradeRequest mrequest;                             // Will be used for trade requests
   MqlTradeResult mresult;                               // Will be used for results of trade requests
   
   ZeroMemory(mrequest);
   ZeroMemory(mresult);
   
   double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);    // Ask price
   double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);    // Bid price

   if(!PositionSelect(_Symbol))
     {
      mrequest.action = TRADE_ACTION_DEAL;               // Immediate order execution
      mrequest.price = NormalizeDouble(Bid,_Digits);     // Lastest Bid price
      mrequest.sl = 0;                                   // Stop Loss
      mrequest.tp = 0;                                   // Take Profit
      mrequest.symbol = _Symbol;                         // Symbol
      mrequest.volume = Lot;                             // Number of lots to trade
      mrequest.magic = 0;                                // Magic Number
      mrequest.type= ORDER_TYPE_SELL;                    // Sell order
      mrequest.type_filling = ORDER_FILLING_FOK;         // Order execution type
      mrequest.deviation=5;                              // Deviation from current price
      OrderSend(mrequest,mresult);                       // Send order
     }
  }
//+------------------------------------------------------------------+
//| Close Long position                                              |
//+------------------------------------------------------------------+
void LongPositionClose()
  {
   MqlTradeRequest mrequest;                             // Will be used for trade requests
   MqlTradeResult mresult;                               // Will be used for results of trade requests
   
   ZeroMemory(mrequest);
   ZeroMemory(mresult);
   
   double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);    // Ask price
   double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);    // Bid price

   if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
     {
      mrequest.action = TRADE_ACTION_DEAL;               // Immediate order execution
      mrequest.price = NormalizeDouble(Bid,_Digits);     // Lastest Bid price
      mrequest.sl = 0;                                   // Stop Loss
      mrequest.tp = 0;                                   // Take Profit
      mrequest.symbol = _Symbol;                         // Symbol
      mrequest.volume = Lot;                             // Number of lots to trade
      mrequest.magic = 0;                                // Magic Number
      mrequest.type= ORDER_TYPE_SELL;                    // Sell order
      mrequest.type_filling = ORDER_FILLING_FOK;         // Order execution type
      mrequest.deviation=5;                              // Deviation from current price
      OrderSend(mrequest,mresult);                       // Send order
     }
  }
//+------------------------------------------------------------------+
//| Close Short position                                             |
//+------------------------------------------------------------------+
void ShortPositionClose()
  {
   MqlTradeRequest mrequest;                             // Will be used for trade requests
   MqlTradeResult mresult;                               // Will be used for results of trade requests
   
   ZeroMemory(mrequest);
   ZeroMemory(mresult);
   
   double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);    // Ask price
   double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);    // Bid price

   if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
     {
      mrequest.action = TRADE_ACTION_DEAL;               // Immediate order execution
      mrequest.price = NormalizeDouble(Ask,_Digits);     // Latest ask price
      mrequest.sl = 0;                                   // Stop Loss
      mrequest.tp = 0;                                   // Take Profit
      mrequest.symbol = _Symbol;                         // Symbol
      mrequest.volume = Lot;                             // Number of lots to trade
      mrequest.magic = 0;                                // Magic Number
      mrequest.type = ORDER_TYPE_BUY;                    // Buy order
      mrequest.type_filling = ORDER_FILLING_FOK;         // Order execution type
      mrequest.deviation=5;                              // Deviation from current price
      OrderSend(mrequest,mresult);                       // Send order
     }
  }
//+------------------------------------------------------------------+
