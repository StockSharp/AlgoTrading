//+-----------------------------------------------------------------+
// On all MT4 market execution accounts (ECN, ECN Zero and Pro),     |
//preset SL/TP levels are not permitted. If a client wishes to add   |
//SL/TP levels, the client must modify the existing position after   |
//the order is opened. This EA helps a client effortly set a preset  |
//stop loss to a trade  a fraction of a second after it is entered.  |
//This especially helps in preventing huge losses whenever one is    |
//scalping on various financial instruments.                         |
//Code contributed by Immanuel Maduagwuna                            |
//+------------------------------------------------------------------+

#property copyright   "Copyright 2020, MetaQuotes Software Corp."
#property link        "www.mql5.com"
#property description " ECN Trade Modifier       "
#property description "                         this expert advisor sets"
#property description "                         takeprofit and stoploss to specified"
#property description "                         values on all open postions"
#property version     "1.37"
#property strict

input double TakeProfit=18.0;//Take Profit (pips)
input double StopLoss=12.0;//Stop Loss (pips)

//Global Variables

double  OrderStoploss;//OrderStopLoss
double  OrderTakeprofit;//OrderTakeProfit
double  OrderopenPrice;//OrderOpenPrice
int Ordertype;//OrderType, 0 for BUY, 1 for SELL...
string Ordersymbol;//OrderSymbol

int OpenTrades;//Total Number of Positions in Terminal
double Lots;//Lots of Selected Order
bool Modify;//returns True or False depending on the success of the amended order.


int Ticket;//Ticket Number assigned to Order in Terminal


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
  {

//--- User Validation

  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+



//+------------------------------------------------------------------+
//| The ModifyStops Function gets called for every                   |
//| value of integer "CurrentTrade"                                  |  
//+------------------------------------------------------------------+
void ModifyStops(int CurrentTrade)
  {
   if(OrderSelect(CurrentTrade, SELECT_BY_POS, MODE_TRADES)==true)
     {
      Lots=OrderLots();
      Ticket=OrderTicket();
      Ordertype=OrderType();
      Ordersymbol= OrderSymbol();
      OrderStoploss=OrderStopLoss();
      OrderopenPrice=OrderOpenPrice();
      OrderTakeprofit=OrderTakeProfit();

      double ask=MarketInfo(Ordersymbol,MODE_ASK);
      double point=MarketInfo(Ordersymbol,MODE_POINT);
      double bid=MarketInfo(Ordersymbol,MODE_BID);
      int digits=(int)MarketInfo(Ordersymbol,MODE_DIGITS);
      double brokerstoplevel=MarketInfo(Ordersymbol,MODE_STOPLEVEL);
      
      double Multiplier=1.0;
      if(digits==5||digits==3) Multiplier=10.0;

      if(Ordertype==0)//BUY Condition
        {
         if(OrderStoploss== 0.0&&OrderTakeprofit==0.0&&brokerstoplevel<StopLoss*Multiplier)
           {
            Modify=OrderModify(Ticket,OrderopenPrice,NormalizeDouble((bid-StopLoss*Multiplier*point),digits),NormalizeDouble((bid+TakeProfit*Multiplier*point),digits), 0,clrGhostWhite);
            if(!Modify) 
               Print("Error in OrderModify. Code=",GetLastError()); 
            else 
               Print("Order Modified Values Set to Scalper TakeProfit & StopLoss Parameters"); 
           }
        }
      if(Ordertype==1)//SELL Condition
        {
         if(OrderStoploss== 0.0&&OrderTakeprofit==0.0&&brokerstoplevel<StopLoss*Multiplier)
           {
            Modify=OrderModify(Ticket,OrderopenPrice,NormalizeDouble((ask+StopLoss*Multiplier*point),digits),NormalizeDouble((ask-TakeProfit*Multiplier*point),digits), 0,clrGhostWhite);
            if(!Modify) 
               Print("Error in OrderModify. Code=",GetLastError()); 
            else 
               Print("Order Modified Values Set to Scalper TakeProfit & StopLoss Parameters"); 
           }
        }
      Comment("Order Position "+ string(CurrentTrade+1) + " Initialized "+" Ticket Number "+string(Ticket));
      }
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {

//Get the current number of trade positions running on the terminal and assign this value to the variable "OpenTrades"
   OpenTrades=OrdersTotal();

//Initiate Loop Operator for the ModifyStops Function
   for(int CurrentTrade=0; CurrentTrade<=OpenTrades; CurrentTrade++)

 //Call the ModifyStops Function to Modify Trades One by One
      ModifyStops(CurrentTrade);

//+------------------------------------------------------------------+
  }
//+------------------------------------------------------------------+
