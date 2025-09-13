//+------------------------------------------------------------------+
//|                                            Rock-Trader-Neuro.mq5 |
//|                                Copyright 2013, Suresh Kakkattil. |
//|                                         http://www.Rocktrader.in |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Suresh Kakkattil."
#property link      "http://www.Rocktrader.in"
#property version   "1.00"


//--- input parameters
input int      StopLoss=30;      // Stop Loss
input int      TakeProfit=100;   // Take Profit
input int      EA_Magic=12345;   // EA Magic Number
input double   Lot=1.0;          // Lots to Trade
//--- weight of Neuros values                                                                   
input double w0=0.8;
input double w1=0.4;
input double w2=-0.9;
input double w3=0.0;
input double w4=0.7;
input double w5=-0.2;
input double w6=0.9;
input double w7=0.7;
input double w8=-1.0;
input double w9=0.3;
input double w10=0.5;
input double w11=0.5;
input double w12=0.0;
input double w13=1.0;

//-------------------------

int               iBands_handle;     //  variable for storing the BB indicator handle
double            iBands_Basebuf[];  //  dynamic array for storing BB indicator values
double            iBands_Upperbuf[]; //  dynamic array for storing BB indicator values
double            iBands_Lowerbuf[]; //  dynamic array for storing BB indicator values

double            inputs[14];        // array for storing inputs(This are waights of out Nuero Inputs)
double            weight[14];        // array for storing weights

string            my_symbol;         // variable for storing the symbol
ENUM_TIMEFRAMES   my_timeframe;      // variable for storing the time frame
double            lot_size;          // variable for storing the minimum lot size of the transaction to be performed

double p_close; // Variable to store the close value of a bar
int STP, TKP;   // To be used for Stop Loss & Take Profit values
double out;     // variable for storing the output neuron value


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- Do we have sufficient bars to work
   if(Bars(_Symbol,_Period)<60) // total number of bars is less than 60?
     {
      Alert("We have less than 60 bars on the chart, an Expert Advisor terminated!!");
      return(-1);
     }

//--- save the current chart symbol for further operation of the EA on this very symbol
   my_symbol=Symbol();
//--- save the current time frame of the chart for further operation of the EA on this very time frame
   my_timeframe=PERIOD_CURRENT;
//--- save the minimum lot of the transaction to be performed
   lot_size=SymbolInfoDouble(my_symbol,SYMBOL_VOLUME_MIN);
//--- apply the indicator and get its handle
   iBands_handle=iBands(my_symbol,my_timeframe,20,0,2.0,PRICE_CLOSE);
//--- check the availability of the indicator handle
   if(iBands_handle==INVALID_HANDLE)
     {
      //--- no handle obtained, print the error message into the log file, complete handling the error
      Print("Failed to get the indicator handle");
      return(-1);
     }
//--- add the BB indicator to the price chart
   ChartIndicatorAdd(ChartID(),0,iBands_handle);
//--- set the iBands_Basebuf array indexing as time series
   ArraySetAsSeries(iBands_Basebuf,true);
//--- set the iBands_upper indexing as time series
   ArraySetAsSeries(iBands_Upperbuf,true);
//--- set the iBands lower array indexing as time series
   ArraySetAsSeries(iBands_Lowerbuf,true);
   
//--- place weights into the array
   weight[0]=w0;
   weight[1]=w1;
   weight[2]=w2;
   weight[3]=w3;
   weight[4]=w4;
   weight[5]=w5;
   weight[6]=w6;
   weight[7]=w7;
   weight[8]=w8;
   weight[9]=w9;
   weight[10]=w10;
   weight[11]=w11;
   weight[12]=w12;
   weight[13]=w13;
   
   //--- Let us handle currency pairs with 5 or 3 digit prices instead of 4
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
//--- delete the indicator handle and deallocate the memory space it occupies
   IndicatorRelease(iBands_handle);
//--- free the iBands Base dynamic array of data
   ArrayFree(iBands_Basebuf);
//--- free the iBand lower dynamic array of data
   ArrayFree(iBands_Lowerbuf);
//--- free the iBands upper dynamic array of data
   ArrayFree(iBands_Upperbuf);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- Do we have enough bars to work with
   if(Bars(_Symbol,_Period)<60) // if total bars is less than 60 bars
     {
      Alert("We have less than 60 bars, EA will now exit!!");
      return;
     }  

// We will use the static Old_Time variable to serve the bar time.
// At each OnTick execution we will check the current bar time with the saved one.
// If the bar time isn't equal to the saved time, it indicates that we have a new tick.

   static datetime Old_Time;
   datetime New_Time[1];
   bool IsNewBar=false;

// copying the last bar time to the element New_Time[0]
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
 
//--- Do we have enough bars to work with
   int Mybars=Bars(_Symbol,_Period);
   if(Mybars<60) // if total bars is less than 60 bars
     {
      Alert("We have less than 60 bars, EA will now exit!!");
      return;
     }

//--- Define some MQL5 Structures we will use for our trade
   MqlTick latest_price;      // To be used for getting recent/latest price quotes
   MqlTradeRequest mrequest;  // To be used for sending our trade requests
   MqlTradeResult mresult;    // To be used to get our trade results
   MqlRates mrate[];          // To be used to store the prices, volumes and spread of each bar
   ZeroMemory(mrequest);      // Initialization of mrequest structure

//--- Get the last price quote using the MQL5 MqlTick Structure
   ArraySetAsSeries(mrate,true);
   if(!SymbolInfoTick(_Symbol,latest_price))
     {
      Alert("Error getting the latest price quote - error:",GetLastError(),"!!");
      return;
     }

//--- Get the details of the latest 3 bars
   if(CopyRates(_Symbol,_Period,0,3,mrate)<0)
     {
      Alert("Error copying rates/history data - error:",GetLastError(),"!!");
      ResetLastError();
      return;
     }
          
   int err1=0; // variable for storing the results of working with the upper buffer of the Bollinger band indicator
   int err2=0; // variable for storing the results of working with the lower buffer of the Bollinger band indicator
   int err3=0; // variable for storing the results of working with the lower buffer of the Bollinger band indicator
   
//--- copy data from the indicator array to the iBands_upper dynamic array for further work with them
   err1=CopyBuffer(iBands_handle,1,0,ArraySize(inputs)/2,iBands_Upperbuf);
//--- copy data from the indicator array to the iBands_Lower dynamic array for further work with them
   err2=CopyBuffer(iBands_handle,2,0,ArraySize(inputs)/2,iBands_Lowerbuf);
//--- copy data from the indicator array to the iBands_Base dynamic array for further work with them
   err3=CopyBuffer(iBands_handle,0,0,ArraySize(inputs)/2,iBands_Basebuf);
//--- in case of errors, print the relevant error message into the log file and exit the function
   if(err1<0 || err2<0 || err3<0)
     {
      Print("Failed to copy data from the indicator buffer");
      return;
     }
   
   double d1=-1; //lower limit of the normalization range
   double d2=1;  //upper limit of the normalization range
   
//--- minimum value over the range
   double x_min=MathMin(iBands_Lowerbuf[ArrayMinimum(iBands_Lowerbuf)],iBands_Upperbuf[ArrayMinimum(iBands_Upperbuf)]);
   double x_minn=iBands_Basebuf[ArrayMinimum(iBands_Basebuf)];
//--- maximum value over the range
   double x_max=MathMax(iBands_Lowerbuf[ArrayMaximum(iBands_Lowerbuf)],iBands_Upperbuf[ArrayMaximum(iBands_Upperbuf)]);
   double x_maxx=iBands_Basebuf[ArrayMaximum(iBands_Basebuf)];
//--- In the loop, fill in the array of inputs with the pre-normalized indicator values
   for(int i=0;i<ArraySize(inputs)/2;i++)
     {
      inputs[i*2]=((((iBands_Upperbuf[i]-iBands_Lowerbuf[i])/iBands_Basebuf[i])-(x_min+x_minn)*(d2-d1))/((x_maxx+x_max)-(x_min+x_minn)))+d1;
            
     }
//--- store the neuron calculation result in the out variable
   out=CalculateNeuron(inputs,weight);
   
//--- we have no errors, so continue
//--- Do we have positions opened already?
   bool Buy_opened=false;  // variable to hold the result of Buy opened position
   bool Sell_opened=false; // variables to hold the result of Sell opened position

   if(PositionSelect(_Symbol)==true) // we have an opened position
     {
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         Buy_opened=true;  //It is a Buy
        }
      else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         Sell_opened=true; // It is a Sell
        }
     }

// Copy the bar close price for the previous bar prior to the current bar, that is Bar 1
   p_close=mrate[1].close;  // bar 1 close price

//--- Declare bool type variables to hold our Buy Conditions
   bool Buy_Condition_1=(out<0); // Neuron Higher than out
   //--- Putting all together   
   if(Buy_Condition_1 )
     {
       // any opened Buy position?
         if(Buy_opened)
           {
            Alert("We already have a Buy Position!!!");
            return;    // Don't open a new Buy Position
           }
         ZeroMemory(mrequest);
         mrequest.action = TRADE_ACTION_DEAL;                                  // immediate order execution
         mrequest.price = NormalizeDouble(latest_price.ask,_Digits);           // latest ask price
         mrequest.sl = NormalizeDouble(latest_price.ask - STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.ask + TKP*_Point,_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                            // currency pair
         mrequest.volume = Lot;                                                 // number of lots to trade
         mrequest.magic = EA_Magic;                                             // Order Magic Number
         mrequest.type = ORDER_TYPE_BUY;                                        // Buy Order
         mrequest.type_filling = ORDER_FILLING_RETURN;                             // Order execution type
         mrequest.deviation=100;                                                // Deviation from current price
         //--- send order
         OrderSend(mrequest,mresult);
         // get the result code
         if(mresult.retcode==10009 || mresult.retcode==10008) //Request is completed or order placed
           {
            Alert("A Buy order has been successfully placed with Ticket#:",mresult.order,"!!");
           }
         else
           {
            Alert("The Buy order request could not be completed -error:",GetLastError());
            ResetLastError();           
            return;
           }
        
     }

//--- Declare bool type variables to hold our Sell Conditions
   bool Sell_Condition_1 = (out>0);  //Neuron Lower than out
   
//--- Putting all together
   if(Sell_Condition_1 )
     {
      // any opened Sell position?
         if(Sell_opened)
           {
            Alert("We already have a Sell position!!!");
            return;    // Don't open a new Sell Position
           }
         ZeroMemory(mrequest);
         mrequest.action=TRADE_ACTION_DEAL;                                // immediate order execution
         mrequest.price = NormalizeDouble(latest_price.bid,_Digits);           // latest Bid price
         mrequest.sl = NormalizeDouble(latest_price.bid + STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.bid - TKP*_Point,_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                          // currency pair
         mrequest.volume = Lot;                                              // number of lots to trade
         mrequest.magic = EA_Magic;                                          // Order Magic Number
         mrequest.type= ORDER_TYPE_SELL;                                     // Sell Order
         mrequest.type_filling = ORDER_FILLING_RETURN;                          // Order execution type
         mrequest.deviation=100;                                             // Deviation from current price
         //--- send order
         OrderSend(mrequest,mresult);
         // get the result code
         if(mresult.retcode==10009 || mresult.retcode==10008) //Request is completed or order placed
           {
            Alert("A Sell order has been successfully placed with Ticket#:",mresult.order,"!!");
           }
         else
           {
            Alert("The Sell order request could not be completed -error:",GetLastError());
            ResetLastError();
            return;
           }
        
     }
   return;
  }
//+------------------------------------------------------------------+
//|   Neuron calculation function                                    |
//+------------------------------------------------------------------+
double CalculateNeuron(double &x[],double &w[])
  {
//--- variable for storing the weighted sum of inputs
   double NET=0.0;
//--- Using a loop we obtain the weighted sum of inputs based on the number of inputs
   for(int n=0;n<ArraySize(x);n++)
     {
      NET+=x[n]*w[n];
     }
//--- multiply the weighted sum of inputs by the additional coefficient
   NET*=2;
//--- send the weighted sum of inputs to the activation function and return its value
   return(ActivateNeuron(NET));
  }
//+------------------------------------------------------------------+
//|   Activation function                                            |
//+------------------------------------------------------------------+
double ActivateNeuron(double x)
  {
//--- variable for storing the activation function results
   double Out;
//--- hyperbolic tangent function
   Out=(exp(x)-exp(-x))/(exp(x)+exp(-x));
//--- return the activation function value
   return(Out);
  }
//+------------------------------------------------------------------+
