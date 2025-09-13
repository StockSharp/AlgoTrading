#property copyright "Copyright 2018, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#include <Math\Stat\Normal.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Expert\Expert.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;
input int      StopLoss=15;      // Stop Loss
input int      TakeProfit=15;   // Take Profit
input int      EA_Magic=12345;   // EA Magic Number


int Ma_Handle1;
double MA_Val1[];
int Ma_Handle2;
double MA_Val2[];
double p_close; // Variable to store the close value of a bar
int STP, TKP;   // To be used for Stop Loss & Take Profit values
struct statParam
  {
  double mean;
  double median;
  double var;
  double stdev;
  double skew;
  double kurt;
  };
//----------------------------------------------------------------------------
int OnInit()
  {
//--- get handle of the iFractals indicator
   //Fractal=iFractals(Symbol(),PERIOD_D1);
//---
   
   Ma_Handle1=iMA(_Symbol,_Period,5,0,MODE_SMA,PRICE_CLOSE);
   Ma_Handle2=iMA(_Symbol,_Period,25,0,MODE_SMA,PRICE_CLOSE);
   STP = StopLoss;
   TKP = TakeProfit;
   if(Ma_Handle1<0||Ma_Handle2<0)
     {
      Alert("Error Creating Handles for indicators - error: ",GetLastError(),"!!");
      return(-1);
     }
   if(_Digits==5 || _Digits==3)
     {
      STP = STP*10;
      TKP = TKP*10;
     }
     
     
     
   int bars=Bars(_Symbol,_Period); 
   if(bars>0) 
     { 
      Print("Number of bars in the terminal history for the symbol-period at the moment = ",bars); 
     } 
   else  //no available bars 
     { 
      //--- data on the symbol might be not synchronized with data on the server 
      bool synchronized=false; 
      //--- loop counter 
      int attempts=0; 
      // make 5 attempts to wait for synchronization 
      while(attempts<5) 
        { 
         if(SeriesInfoInteger(Symbol(),0,SERIES_SYNCHRONIZED)) 
           { 
            //--- synchronization done, exit 
            synchronized=true; 
            break; 
           } 
         //--- increase the counter 
         attempts++; 
         //--- wait 10 milliseconds till the next iteration 
         Sleep(10); 
        } 
      //--- exit the loop after synchronization 
      if(synchronized) 
        { 
         Print("Number of bars in the terminal history for the symbol-period at the moment = ",bars); 
         Print("The first date in the terminal history for the symbol-period at the moment = ", 
               (datetime)SeriesInfoInteger(Symbol(),0,SERIES_FIRSTDATE)); 
         Print("The first date in the history for the symbol on the server = ", 
               (datetime)SeriesInfoInteger(Symbol(),0,SERIES_SERVER_FIRSTDATE)); 
        } 
      //--- synchronization of data didn't happen 
      else 
        { 
         Print("Failed to get number of bars for ",_Symbol); 
        } 
     }  
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
    IndicatorRelease(Ma_Handle1);
    IndicatorRelease(Ma_Handle2);
  }

  
  void OnTick()
  {
 
   
    //ArraySetAsSeries(Price_vector,true);  
    if(Bars(_Symbol,_Period)<60) // if total bars is less than 60 bars
     {
      Alert("We have less than 60 bars, EA will now exit!!");
      return;
     }  
     
  
// copying the last bar time to the element New_Time[0]
   static datetime Old_Time;
   datetime New_Time[1];
   bool IsNewBar=false;
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
/*
     Let's make sure our arrays values for the Rates, ADX Values and MA values 
     is store serially similar to the timeseries array
*/
// the rates arrays
   ArraySetAsSeries(mrate,true);
  ArraySetAsSeries(MA_Val1,true);

   if(!SymbolInfoTick(_Symbol,latest_price))
     {
      Alert("Error getting the latest price quote - error:",GetLastError(),"!!");
      return;
     }
     
 
//--- Get the details of the latest 3 bars
   if(CopyRates(_Symbol,_Period,0,5,mrate)<0)
     {
      Alert("Error copying rates/history data - error:",GetLastError(),"!!");
      ResetLastError();
      return;
     }
   if(CopyBuffer(Ma_Handle1,0,0,5,MA_Val1)<0)
     {
      Alert("Error copying rates/history data - error:",GetLastError(),"!!");
      ResetLastError();
      return;
     }
   if(CopyBuffer(Ma_Handle2,0,0,5,MA_Val2)<0)
     {
      Alert("Error copying rates/history data - error:",GetLastError(),"!!");
      ResetLastError();
      return;
     }

   
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
   double val[];
   double Close[];
   CopyClose(Symbol(),PERIOD_CURRENT,0,5,Close);
   CopyClose(Symbol(),PERIOD_CURRENT,0,5,val);
   ArraySetAsSeries(Close,true);
   for (int i=0;i<5;i++){
      val[i]=MA_Val1[i]-Close[i];
   }
   ArraySetAsSeries(val,true);
   
   
   bool Buy_Condition_1=(val[0]<-0.0015); // MA-8 Increasing upwards
   bool Buy_Condition_2=(MA_Val1[0]>MA_Val1[2]);


//--- Putting all together   
   if(Buy_Condition_1 && Buy_Condition_2)
     {
      //if(Buy_Condition_2)
      //  {
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
         mrequest.tp = NormalizeDouble(MA_Val1[0],_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                            // currency pair
         mrequest.volume = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);                                                 // number of lots to trade
         mrequest.magic = 15489;                                             // Order Magic Number
         mrequest.type = ORDER_TYPE_BUY;                                        // Buy Order
         mrequest.type_filling = ORDER_FILLING_FOK;                             // Order execution type
         mrequest.deviation=100;                                                // Deviation from current price
         double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
         //--- call of the checking function
         //--- if there are insufficient funds to perform the operation
         if(margin<free_margin)
           {
            
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
           }}
     //   }
     }
   
//--- Declare bool type variables to hold our Sell Conditions

   /*bool Sell_Condition_1=(val[0]>0.0012); // MA-8 Increasing upwards
   bool Sell_Condition_2=(MA_Val2[0]<MA_Val2[2]);


//--- Putting all together
   if(Sell_Condition_1 && Sell_Condition_2)
     {
  //    if(Sell_Condition_2)
   //     {
         // any opened Sell position?
         if(Sell_opened)
           {
            Alert("We already have a Sell position!!!");
            return;    // Don't open a new Sell Position
           }
         ZeroMemory(mrequest);
         mrequest.action=TRADE_ACTION_DEAL;          
                               // immediate order execution
         mrequest.price = NormalizeDouble(latest_price.bid,_Digits);           // latest Bid price
         mrequest.sl = NormalizeDouble(latest_price.bid + STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.bid - TKP*_Point,_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                          // currency pair
         mrequest.volume = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);                                              // number of lots to trade
         mrequest.magic = EA_Magic;                                          // Order Magic Number
         mrequest.type= ORDER_TYPE_SELL;                                     // Sell Order
         mrequest.type_filling = ORDER_FILLING_FOK;                          // Order execution type
         mrequest.deviation=100;                                             // Deviation from current price
         //--- send order
         double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
         //--- call of the checking function
         //--- if there are insufficient funds to perform the operation
         if(margin<free_margin)
           {
            //--- report the error and return false
            
         
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
           }}}
           
       */    
           
   return;                                
  }