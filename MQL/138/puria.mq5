//+------------------------------------------------------------------+
//|                                                        Puria.mq5 |
//|                                       Copyright 2010, AM2 Group. |
//|                                         http://www.am2_group.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, AM2 Group."
#property link      "http://www.am2_group.net"
#property version   "1.00"
//--- input parameters
input int      StopLoss=14;      // Stop Loss
input int      TakeProfit=15;    // Take Profit
input int      MA1_Period=75;    // Moving Average 1 period
input int      MA2_Period=85;    // Moving Average 2 period
input int      MA3_Period=5;     // Moving Average 3 period
input int      EA_Magic=12345;   // Magic Number of an EA
input double   Lot=0.1;          // Number of lots to trade
//--- global variables
int macdHandle;    // MACD indicator handle
int ma75Handle;    // Moving Average 1 indicator handle
int ma85Handle;    // Moving Average 2 indicator handle
int ma5Handle;     // Moving Average 3 indicator handle
double macdVal[];  // array to store the numerical values of the MACD indicator
double ma75Val[];  // array to store the numerical values of the Moving Average 1 indicator
double ma85Val[];  // array to store the numerical values of the Moving Average 2 indicator
double ma5Val[];   // array to store the numerical values of the Moving Average 3 indicator
double p_close;    // variable to store close price
int STP,TKP;       // will be used for Stop Loss and Take Profit
bool BuyOne=true,SellOne=true; // only one order
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- do we have enough bars to work with
   if(Bars(_Symbol,_Period)<60) // if total bars is less than 60 bars
     {
      Alert("We have less than 60 bars, EA will now exit!!");
      return(-1);
     }
//--- get handle of the MACD indicator
   macdHandle=iMACD(NULL,0,15,26,1,PRICE_CLOSE);
//--- get handle of the Moving Average indicator
   ma75Handle=iMA(_Symbol,_Period,75,0,MODE_LWMA,PRICE_LOW);
   ma85Handle=iMA(_Symbol,_Period,85,0,MODE_LWMA,PRICE_LOW);
   ma5Handle=iMA(_Symbol,_Period,5,0,MODE_EMA,PRICE_CLOSE);

//--- what if handle returns Invalid Handle
   if(macdHandle<0 || ma75Handle<0 || ma85Handle<0 || ma5Handle<0)
     {
      Alert("Error Creating Handles for indicators - error: ",GetLastError(),"!!");
      return(-1);
     }

//--- memory allocation
   ArrayResize(macdVal,5);
   ArrayResize(ma75Val,5);
   ArrayResize(ma85Val,5);
   ArrayResize(ma5Val,5);

//--- let us handle currency pairs with 5 or 3 digit prices
   STP = StopLoss;
   TKP = TakeProfit;
   if(_Digits==5 || _Digits==3)
     {
      STP = STP*10;
      TKP = TKP*10;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- release our indicator handles
   IndicatorRelease(ma75Handle);
   IndicatorRelease(ma85Handle);
   IndicatorRelease(ma5Handle);
   IndicatorRelease(macdHandle);
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
         IsNewBar=true;   // new bar
         if(MQL5InfoInteger(MQL5_DEBUGGING)) Print("New bar",New_Time[0],"old bar",Old_Time);
         Old_Time=New_Time[0];   // saving bar time
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

//--- define some MQL5 Structures we will use for our trade
   MqlTick latest_price;      // To be used for getting recent/latest price quotes
   MqlTradeRequest mrequest;  // To be used for sending our trade requests
   MqlTradeResult mresult;    // To be used to get our trade results
   MqlRates mrate[];          // To be used to store the prices, volumes and spread of each bar   

   ZeroMemory(mrequest);
   ZeroMemory(mresult);

   mrequest.action = TRADE_ACTION_DEAL;        // immediate execution 
   mrequest.type_filling = ORDER_FILLING_FOK;  // order execution type = all or none
   mrequest.symbol = _Symbol;                  // symbol
   mrequest.volume = Lot;                      // number of lots to trade
   mrequest.magic = EA_Magic;                  // Magic Number 
   mrequest.deviation=5;                       // deviation from the current price

/*
     Set arrays as timeseries
*/

//--- the rates arrays
   ArraySetAsSeries(mrate,true);
//--- MACD indicator values
   ArraySetAsSeries(macdVal,true);
//--- MA indicator values
   ArraySetAsSeries(ma75Val,true);
   ArraySetAsSeries(ma85Val,true);
   ArraySetAsSeries(ma5Val,true);

//--- get the last price quote using the MQL5 MqlTick Structure
   if(!SymbolInfoTick(_Symbol,latest_price))
     {
      Alert("Error getting the latest price quote - error:",GetLastError(),"!!");
      return;
     }

//--- get the details of the latest 3 bars
   if(CopyRates(_Symbol,_Period,0,3,mrate)<0)
     {
      Alert("Error copying rates/history data - error:",GetLastError(),"!!");
      return;
     }

//--- copy the new values of our indicators to buffers (arrays) using the handle
   if(CopyBuffer(ma75Handle,0,0,3,ma75Val)<0 || CopyBuffer(ma85Handle,0,0,3,ma85Val)<0
      || CopyBuffer(ma5Handle,0,0,3,ma5Val)<0)
     {
      Alert("Error copying MACD indicator Buffers - error:",GetLastError(),"!!");
      return;
     }
   if(CopyBuffer(macdHandle,0,0,3,macdVal)<0)
     {
      Alert("Error copying Moving Average indicator buffer - error:",GetLastError());
      return;
     }

//--- copy the bar close price for the previous bar prior to the current bar, that is Bar 1
   p_close=mrate[1].close;  // bar 1 close price

/*
    1. Check for the buy conditions : MA-5 crosses MA-75 and MA-85 from below to above, 
       the previous close price is greater than MA-5, the MACD is greater than 0.
*/

//--- declare a variable of boolean type, to hold our Buy Conditions
   bool Buy_Signal=(ma5Val[1]>ma75Val[1]) && (ma5Val[1]>ma85Val[1]         // MA-5 crosses MA-75 and MA-85 from below
                    && p_close > ma5Val[1]                                 // previous close price above MA-5
                    && macdVal[1]>0);                                      // MACD>0
/*   
    2. Check for the sell conditions : MA-5 crosses MA-75 and MA-85 from above to below,
       the previous close price is less than MA-5, the MACD is less than 0.       
*/

//--- declare a variable of boolean type, to hold our Sell Conditions
   bool Sell_Signal = (ma5Val[1]<ma75Val[1]) && (ma5Val[1]<ma85Val[1]       // MA-5 crosses MA-75 и MA-85 сверху вниз
                       && p_close < ma5Val[1]                               // previous close price below MA-5
                       && macdVal[1]<0);                                    // MACD<0


//--- combine all together
   if(Buy_Signal &&                                                         // buy if there is a buy signal
      PositionSelect(Symbol())==false &&                                    // there isn't opened position
      BuyOne)                                                               // place order only if buyone is true
     {
      mrequest.type = ORDER_TYPE_BUY;                                       // buy order
      mrequest.price = NormalizeDouble(latest_price.ask,_Digits);           // last ask price
      mrequest.sl = NormalizeDouble(latest_price.ask - STP*_Point,_Digits); // Stop Loss
      mrequest.tp = NormalizeDouble(latest_price.ask + TKP*_Point,_Digits); // Take Profit 
      OrderSend(mrequest,mresult);                                          // send order
      BuyOne = false;                                                       // only one order for buy - flag
      SellOne = true;                                                       // change one sell order flag 
     }

//--- combine all
   else if(Sell_Signal &&                                                   // sell if there is a sell signal
      PositionSelect(Symbol())==false &&                                    // there isn't opened position
      SellOne)                                                              // place order only if sellone is true
        {
         mrequest.type= ORDER_TYPE_SELL;                                       // sell order
         mrequest.price = NormalizeDouble(latest_price.bid,_Digits);           // last Bid price
         mrequest.sl = NormalizeDouble(latest_price.bid + STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.bid - TKP*_Point,_Digits); // Take Profit
         OrderSend(mrequest,mresult);                                          // send order
         SellOne = false;                                                      // only one order for sell
         BuyOne = true;                                                        // change one buy order flag 
        }
      return;
  }
//+------------------------------------------------------------------+
