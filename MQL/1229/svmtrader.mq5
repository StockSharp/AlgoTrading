//+------------------------------------------------------------------+
//|                                                    svmTrader.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

#property indicator_buffers 7

//+---------------------------OVERVIEW-------------------------------+
//| This expert advisor has been written to show how the support vector
//| machine learning tool can potentially be used to signal trades.
//| In this EA, 2 support vector machines are created for a single currency 
//| (one SVM to signal buy trades and the other SVM to signal sell trades). 
//|
//| When a trade is signalled by the SVM's, the EA opens a new buy/sell trade
//| as well as manual stopLoss and takeProfit orders (note: I've chosen not 
//| to use the built-in stopLoss/takeProfit functionality as it is unable
//| to treat multiple trades on a single currency as separate).
//|
//| When the manual stopLoss/takeProfit orders are setup, their ticket
//| numbers are stored in a cache (i.e. tickets[]). If a stopLoss or 
//| takeProfit order is triggered, the cache is searched to find the 
//| ticket number of the matching stopLoss/takeProfit trade, then the
//| matching trade is cancelled. This method allows the EA to take ALL
//| trades that are signalled by the support vector machine and treat 
//| them as seperate trades.
//|
//| Please note that this EA is only designed to demonstrate the capabilities 
//| of the supportvector machine learning tool. I recommend you modify this EA
//| to suit your own particular style of trading.
//|
//| Happy experimenting...
//+------------------------------------------------------------------+


//+---------Support Vector Machine Learning Tool Functions-----------+
//| The following #import statement imports all of the support vector
//| machine learning tool functions into the EA for use. Please note, if
//| you do not import the functions here, the compiler will not let you
//| use any of the functions                                  |
//+------------------------------------------------------------------+
#import "svMachineTool.ex5"
enum ENUM_TRADE {BUY,SELL};
enum ENUM_OPTION {OP_MEMORY,OP_MAXCYCLES,OP_TOLERANCE};
int  initSVMachine(void);
void setIndicatorHandles(int handle,int &indicatorHandles[],int offset,int N);
void setParameter(int handle,ENUM_OPTION option,double value);
bool genOutputs(int handle,ENUM_TRADE trade,int StopLoss,int TakeProfit,double duration);
bool genInputs(int handle);
bool setInputs(int handle,double &Inputs[],int nInputs);
bool setOutputs(int handle,bool &Outputs[]);
bool training(int handle);
bool classify(int handle);
bool classify(int handle,int offset);
bool classify(int handle,double &iput[]);
void  deinitSVMachine(void);
#import

#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\HistoryOrderInfo.mqh>

//+-----------------------Input Variables----------------------------+
input int            takeProfit=100;      //TakeProfit level measured in pips
input int            stopLoss=150;        //StopLoss level measured in pips
input double         hours=6;             //The maximum hypothetical trade duration for calculating training outputs.
input double         risk_exp=5;          //Maximum simultaneous order exposure to the market
input double         Tolerance_Value=0.1; //Error Tolerance value for training the svm (default is 10%)
input int            N_DataPoints=100;    //The number of training points to generate and use.

//+---------------------Indicator Variables--------------------------+
//| Only the default indicator variables have been used here. I
//| recommend you play with these values to see if you get any 
//| better performance with your EA.                    
//+------------------------------------------------------------------+
int bears_period=13;
int bulls_period=13;
int ATR_period=13;
int mom_period=13;
int MACD_fast_period=12;
int MACD_slow_period=26;
int MACD_signal_period=9;
int Stoch_Kperiod=5;
int Stoch_Dperiod=3;
int Stoch_slowing=3;
int Force_period=13;

//+------------------Expert Advisor Variables------------------------+
int       tickets[];
bool       Opn_B,Opn_S;
datetime    New_Time;
int       handleB,handleS;
double       Vol=1;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   New_Time=0;
   int handles[];ArrayResize(handles,7);
//+------------------------------------------------------------------+
//| The following statements are used to initialize the indicators to be used for the support 
//| vector machine. The handles returned are stored to an int[] array. I have used standard 
//| indicators in this case however, you can also you custom indicators if desired
//+------------------------------------------------------------------+
   handles[0]=iBearsPower(Symbol(),0,bears_period);
   handles[1]=iBullsPower(Symbol(),0,bulls_period);
   handles[2]=iATR(Symbol(),0,ATR_period);
   handles[3]=iMomentum(Symbol(),0,mom_period,PRICE_TYPICAL);
   handles[4]=iMACD(Symbol(),0,MACD_fast_period,MACD_slow_period,MACD_signal_period,PRICE_TYPICAL);
   handles[5]=iStochastic(Symbol(),0,Stoch_Kperiod,Stoch_Dperiod,Stoch_slowing,MODE_SMA,STO_LOWHIGH);
   handles[6]=iForce(Symbol(),0,Force_period,MODE_SMA,VOLUME_TICK);

//----------Initialize, Setup and Training of the Buy-Signal support vector machine----------
   handleB=initSVMachine();            //initializes a new SVM and stores the handle to 'handleB'
   setIndicatorHandles(handleB,handles,0,N_DataPoints);   //passes the initialized indicators to the SVM with desired offset and number of data points
   setParameter(handleB,OP_TOLERANCE,Tolerance_Value);   //Sets the maximum error tolerance for SVM training
   genInputs(handleB);               //generate inputs using the initialized indicators
   genOutputs(handleB,BUY,stopLoss,takeProfit,hours);   //generates the outputs based on the desired parameters for taking hypothetical trades

//----------Initialize, Setup and Training of the Sell-Signal support vector machine----------
   handleS=initSVMachine();            //initializes a new SVM and stores the handle to 'handleS'
   setIndicatorHandles(handleS,handles,0,N_DataPoints);   //passes the initialized indicators to the SVM with desired offset and number of data points
   setParameter(handleS,OP_TOLERANCE,Tolerance_Value);   //Sets the maximum error tolerance for SVM training
   genInputs(handleS);               //generate inputs using the initialized indicators
   genOutputs(handleS,SELL,stopLoss,takeProfit,hours);   //generates the outputs based on the desired parameters for taking hypothetical trades

   training(handleB);   //executes training on the Buy-Signal support vector machine
   training(handleS);     //executes training on the Sell-Signal support vector machine   
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   deinitSVMachine();   //cleans-up all of the memory used by the SVMs to avoid memory leakage.
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(New_Time!=iTime(Symbol(),0,0)) //executes statement if a new candle is created on the current chart timeframe
     {
      New_Time=iTime(Symbol(),0,0);   //resets the New_Time variable to the current bar time
      OnBar();            //calls the OnBar() function
     }
  }
//+------------------------------------------------------------------+
//| This function takes currency symbol, timeframe and index as an 
//| input and returns the time for that bar.
//+------------------------------------------------------------------+
datetime iTime(string symbol,ENUM_TIMEFRAMES timeframe,int index)
  {
   datetime Time[1];
   ArraySetAsSeries(Time,true);
   int copied=CopyTime(symbol,timeframe,index,1,Time);
   return(Time[0]);
  }
//+------------------------------------------------------------------+
//| This function is called whenever a new trade is executed. This function
//| is used to determine whether a stopLoss/takeProfit order has been
//| executed and if it has, delete the matching stopLoss/takeProfit order
//+------------------------------------------------------------------+
void OnTrade()
  {
   COrderInfo order;
   CTrade trade;
   int t;
   for(int i=0;i<OrdersTotal();i++)
     {
      order.SelectByIndex(i);
      t=(int)order.Ticket();
      if(!order.Select(tickets[t]))
        {
         trade.OrderDelete(t);
        }
     }
  }
//+------------------------------------------------------------------+
//| The OnBar function calls the classify() function from the support
//| vector machine learning tool. This classify function will generate
//| new inputs using current indicator data and then use the trained
//| SVM's to assess the inputs and generate a buy/sell signal. If a buy
//| or sell signal is generated, the Open_Order function is called
//+------------------------------------------------------------------+
void OnBar(void)
  {
   Opn_B=classify(handleB);
   Opn_S=classify(handleS);

   if(Opn_B || Opn_S) Open_Order();
  }
//+------------------------------------------------------------------+
//| This function is called to open a new trade and create stopLoss and
//| takeProfit orders.
//+------------------------------------------------------------------+
void Open_Order(void)
  {
   CTrade trade;
   double CoE=Current_Order_Exposure();   //calculates your current order exposure to the market 
   bool executed=false;
   if(Opn_B==true && CoE<=risk_exp) //a new trade is opened only if Opn_B is true and your current order exposure is less than risk_exp
     {
      //-------------Opens a new buy position------------------
      trade.SetDeviationInPoints(50);
      executed=trade.PositionOpen(Symbol(),ORDER_TYPE_BUY,Vol,NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),Digits()),0,0,"");
      if(executed) //if the buy position is successfully opened, it calls the Insert_Stops() function to create stopLoss and takeProfit orders
        {
         Insert_Stops(BUY,NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),Digits()));
         Opn_B=false;
        }
     }
   if(Opn_S==true && CoE<=risk_exp) //a new trade is opened only if Opn_S is true and your current order exposure is less than risk_exp
     {
      //-------------Opens a new sell position------------------
      trade.SetDeviationInPoints(50);
      executed=trade.PositionOpen(Symbol(),ORDER_TYPE_SELL,Vol,NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),Digits()),0,0,"");
      if(executed)
        {
         Insert_Stops(SELL,NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),Digits()));
         Opn_S=false;
        }
     }
  }
//+------------------------------------------------------------------+
//| This function calculates your current order exposure to the market
//| This is done by summing all of the order volumes of the currently
//| open orders, then dividing this value by two (it is divided by two
//| because the orders are setup in pairs such that is one order is triggered
//| the matching order is immediately deleted). Therefore, worst case
//| scenario is that only half of the total orders in your order list 
//| will be executed at one time.
//+------------------------------------------------------------------+
double Current_Order_Exposure()
  {
   COrderInfo order;
   double Volume=0.00;
   for(int i=0;i<OrdersTotal();i++)
     {
      order.SelectByIndex(i);
      Volume=Volume+order.VolumeCurrent();
     }
   return(Volume/2);
  }
//+------------------------------------------------------------------+
//| This function is used to insert new stopLoss and takeProfit orders
//| following a new trade.
//+------------------------------------------------------------------+
void Insert_Stops(ENUM_TRADE trade,double Price)
  {
   CTrade trade1,trade2,trade3,trade4;
   int ticket1,ticket2;
   if(trade==BUY)
     {
      trade1.SetDeviationInPoints(50);
      trade1.SellStop(Vol,Price-(Pip_Size()*stopLoss),Symbol(),0.0,0.0,ORDER_TIME_GTC,0,"");
      ticket1=(int)trade1.ResultOrder();
      trade2.SetDeviationInPoints(50);
      trade2.SellLimit(Vol,Price+(Pip_Size()*takeProfit),Symbol(),0.0,0.0,ORDER_TIME_GTC,0,"");
      ticket2=(int)trade2.ResultOrder();
      cacheTicketNumber(ticket1,ticket2);
     }
   if(trade==SELL)
     {
      trade3.SetDeviationInPoints(50);
      trade3.BuyStop(Vol,Price+(Pip_Size()*stopLoss),Symbol(),0.0,0.0,ORDER_TIME_GTC,0,"");
      ticket1=(int)trade3.ResultOrder();
      trade4.SetDeviationInPoints(50);
      trade4.BuyLimit(Vol,Price-(Pip_Size()*takeProfit),Symbol(),0.0,0.0,ORDER_TIME_GTC,0,"");
      ticket2=(int)trade4.ResultOrder();
      cacheTicketNumber(ticket1,ticket2);
     }
  }
//+------------------------------------------------------------------+
//| This function takes the ticket numbers of matchine stopLoss and 
//| takeProfit orders and saves them to a cache. This cache is used to
//| find the matching order ticket number for any given order.
//+------------------------------------------------------------------+
void cacheTicketNumber(int ticket1,int ticket2)
  {
   if(MathMax(ticket1,ticket2)>(ArraySize(tickets)-1)) ArrayResize(tickets,MathMax(ticket1,ticket2)+1);
   tickets[ticket1]=ticket2;
   tickets[ticket2]=ticket1;
  }
//+------------------------------------------------------------------+
//| This function calculates the value of 1 pip on the current symbol.
//+------------------------------------------------------------------+
double Pip_Size(void)
  {
   double v=SymbolInfoDouble(Symbol(),SYMBOL_BID);
   int deci=(int)MathRound(MathAbs(MathRound(MathLog(v/10000)/MathLog(10))));
   double out=NormalizeDouble(MathPow(10,-1*deci),deci);
   return(out);
  }
//+------------------------------------------------------------------+
