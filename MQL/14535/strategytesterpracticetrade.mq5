//+------------------------------------------------------------------+
//|                                  StrategyTesterPracticeTrade.mq5 |          
//|                                Copyright 2015, SearchSurf (RmDj) |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2015, SearchSurf (RmDj)"
#property link        "https://www.mql5.com"
#property version     "1.00"
#property description "This EA is for MT5 and it is  mainly used to Practice Trade on Strategy Tester only."
#property description "The manual command is taken from the existance of a text file in the MT5's COMMON/FILE Folder."
#property description "Command text file as follows: 'buy.txt', 'sell.txt', or 'close.txt', only one must appear each time on the folder."

/*
 NOTE:  (Please read, this is important!!!)
- In order for this EA to use its function on the strategy tester, a textfile command outside MT5's processing is needed for the manual order execution.
- The EA will take the "buy/sell/close" order command mainly on the existance of certain files as "buy.txt", "sell.txt", or "close.txt" at the MT5's COMMON file folder.
  (The text file doesn't need anything on it, it's the presence of the filename that matters.)
- Only one file must exist on the said folder at each command, otherwise, the EA will execute the first one it reads and deletes the file/files.
*/

// Global Variables
input double DLot=0.01;    // Lot Size:
int          Arun_error;   // Any error encountered during run
double       JustifySize;  // Justify lotsize between buy/sell and close
int          Ax,Ay,live;   // Axis X, Axis Y on graph,live=0 or test=1
double       OpenProfit;   // Open position profit
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   Arun_error=0;
   OpenProfit=0;
   live=1;

   ChartSetInteger(0,CHART_EVENT_MOUSE_MOVE,0,1);

// Profit
   ObjectCreate(0,"LABEL1",OBJ_LABEL,0,0,0);
   ObjectSetInteger(0,"LABEL1",OBJPROP_XDISTANCE,40);
   ObjectSetInteger(0,"LABEL1",OBJPROP_YDISTANCE,102);
   ObjectSetInteger(0,"LABEL1",OBJPROP_FONTSIZE,12);
   ObjectSetInteger(0,"LABEL1",OBJPROP_COLOR,clrRed);

// Equity 
   ObjectCreate(0,"LABEL2",OBJ_LABEL,0,0,0);
   ObjectSetInteger(0,"LABEL2",OBJPROP_XDISTANCE,40);
   ObjectSetInteger(0,"LABEL2",OBJPROP_YDISTANCE,50);
   ObjectSetInteger(0,"LABEL2",OBJPROP_FONTSIZE,12);
   ObjectSetInteger(0,"LABEL2",OBJPROP_COLOR,clrRed);

   ChartRedraw();
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectDelete(0,"LABEL");
   ObjectDelete(0,"LABEL1");
   ObjectDelete(0,"LABEL2");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   string candle;
   double CurP;

   JustifySize=DLot;

   if(Arun_error>0)
     {
      Alert("EA detected error: ",Arun_error," -- EA Aborted!!! Pls. close EA now and attend to your open entry/ies.");
      Print("EA detected error: ",Arun_error," -- EA Aborted!!! Pls. close EA now and attend to your open entry/ies.");
      if(!ObjectFind(0,"LABEL")) ObjectDelete(0,"LABEL");
      ObjectCreate(0,"LABEL",OBJ_BUTTON,0,0,0);
      ObjectSetInteger(0,"LABEL",OBJPROP_XDISTANCE,270);
      ObjectSetInteger(0,"LABEL",OBJPROP_YDISTANCE,40);
      ObjectSetInteger(0,"LABEL",OBJPROP_XSIZE,370);
      ObjectSetInteger(0,"LABEL",OBJPROP_YSIZE,30);
      ObjectSetInteger(0,"LABEL",OBJPROP_FONTSIZE,9);
      ObjectSetInteger(0,"LABEL",OBJPROP_COLOR,clrBlack);
      ObjectSetInteger(0,"LABEL",OBJPROP_BGCOLOR,clrCyan);
      ObjectSetString(0,"LABEL",OBJPROP_TEXT,"Error: EA aborted, please close or restart.");
      ChartRedraw();
      return;
     }

// If there's an open position, display the Profit every tick change.
   if(OpenPosition()=="none") OpenProfit=0;
   else
     {
      PositionSelect(_Symbol);
      OpenProfit=NormalizeDouble(PositionGetDouble(POSITION_PROFIT),2);
     }
   ChartTimePriceToXY(0,0,StringToTime(GetBarDetails("time",3,0)),GetBarPrice("close",3,0),Ax,Ay);
   ObjectSetInteger(0,"LABEL1",OBJPROP_XDISTANCE,Ax);
   ObjectSetInteger(0,"LABEL1",OBJPROP_YDISTANCE,Ay);
   ObjectSetString(0,"LABEL1",OBJPROP_TEXT,"  Profit: "+DoubleToString(OpenProfit,2));
   ObjectSetString(0,"LABEL2",OBJPROP_TEXT,"EQUITY: "+DoubleToString(AccountInfoDouble(ACCOUNT_EQUITY),2));

   ChartRedraw();

   CurP=NormalizeDouble(GetBarPrice("close",3,0),_Digits);

// %%%%%%%%%  Below codes will keep checking at every tick for a specific (buy.txt,sell.txt,close.txt) command file at the MT5 Common Folder %%%%%%%%%%%%%%%%%

// If a buy.txt is found at the MT5 COMMON folder, Buy order is executed at default lot size as stated in the input parameter.
   if(FileIsExist("buy.txt",FILE_COMMON))
     {
      FileDelete("sell.txt",FILE_COMMON);
      FileDelete("buy.txt",FILE_COMMON);
      FileDelete("close.txt",FILE_COMMON);
      ExecuteTrade("buy",CurP,JustifySize);
     }

// If a sell.txt is found, Sell order is executed at default lot size as stated in the input parameter.
   if(FileIsExist("sell.txt",FILE_COMMON))
     {
      FileDelete("sell.txt",FILE_COMMON);
      FileDelete("buy.txt",FILE_COMMON);
      FileDelete("close.txt",FILE_COMMON);
      ExecuteTrade("sell",CurP,JustifySize);
     }

// if a close.txt is found, this will close any open position with the maximum volume size stated on the position. 
   if(FileIsExist("close.txt",FILE_COMMON))
     {

      FileDelete("sell.txt",FILE_COMMON);
      FileDelete("buy.txt",FILE_COMMON);
      FileDelete("close.txt",FILE_COMMON);

      if(OpenPosition()=="buy")
        {
         PositionSelect(_Symbol);
         JustifySize=NormalizeDouble(PositionGetDouble(POSITION_VOLUME),2);
         ExecuteTrade("sell",CurP,JustifySize);
        }

      if(OpenPosition()=="sell")
        {
         PositionSelect(_Symbol);
         JustifySize=NormalizeDouble(PositionGetDouble(POSITION_VOLUME),2);
         ExecuteTrade("buy",CurP,JustifySize);
        }
     }
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//---
   if(live==1)
     {
      // Remarks:
      ObjectCreate(0,"LABEL",OBJ_BUTTON,0,0,0);
      ObjectSetInteger(0,"LABEL",OBJPROP_XDISTANCE,270);
      ObjectSetInteger(0,"LABEL",OBJPROP_YDISTANCE,40);
      ObjectSetInteger(0,"LABEL",OBJPROP_XSIZE,370);
      ObjectSetInteger(0,"LABEL",OBJPROP_YSIZE,30);
      ObjectSetInteger(0,"LABEL",OBJPROP_FONTSIZE,9);
      ObjectSetInteger(0,"LABEL",OBJPROP_COLOR,clrBlack);
      ObjectSetInteger(0,"LABEL",OBJPROP_BGCOLOR,clrCyan);
      ObjectSetString(0,"LABEL",OBJPROP_TEXT,"NOTE: This EA is for Practice Trade on Strategy Tester only!!!");
      ChartRedraw();
      live=0;
     }
  }
//+------------------------------------------------------------------+
//|    Candle Bar Details                                            |
//+------------------------------------------------------------------+
//entry- (datetime) "time", (long) "tickVOL", (int) "spread", (long) "realVOL", maxbar- number of bars to initiate, bar- at which bar to get details
//entry- (datetime)  "lasttime", (ulong) "lastVOL", (long in millisec) "lastupdate", (uint) "TICKflag"  === all for latest only... 
string GetBarDetails(string entry,int maxbar,int bar) // bars based on the current (present) occurance of point of time.
  {
   string bardetails;

   MqlTick latestP;
   MqlRates BarRates[];

   ArraySetAsSeries(BarRates,true);

//--- last price quote:
   if(!SymbolInfoTick(_Symbol,latestP))
     {
      Alert("Error getting the latest price quote - error:1002");
      Arun_error=1002;
      return("error");
     }

//--- Get the details of the latest maxbars bars
   if(CopyRates(_Symbol,_Period,0,maxbar,BarRates)<0)
     {
      Alert("Error copying rates/history data - error:1002");
      Arun_error=1002;
      return("error");
     }

   if(entry=="time") bardetails=TimeToString(BarRates[bar].time);                 // datetime
   if(entry=="tickVOL") bardetails=IntegerToString(BarRates[bar].tick_volume);    // long ... 
   if(entry=="spread") bardetails=IntegerToString(BarRates[bar].spread);          // int
   if(entry=="realVOL") bardetails=TimeToString(BarRates[bar].real_volume);       // datetime

   if(entry=="lasttime") bardetails=TimeToString(latestP.time);                   // datetime
   if(entry=="lastVOL") bardetails=IntegerToString(latestP.volume);               // ulong 
   if(entry=="lastupdate") bardetails=IntegerToString(latestP.time_msc);          // long in milliseconds
   if(entry=="lastTickflag") bardetails=TimeToString(latestP.flags);              // uint

   return(bardetails);  // ...don't forget to convert the returned string result value to its designated data type.
  }
//+------------------------------------------------------------------+
//|    Candle Bar Prices                                             |
//+------------------------------------------------------------------+
//entry- open,high,low,close,bid,ask,last, maxbar- number of bars to initiate, bar- at which bar to get price
double GetBarPrice(string price,int maxbar,int bar)
  {
   double barprice=0;

   MqlTick latestP;
   MqlRates BarRates[];

   ArraySetAsSeries(BarRates,true);

//--- last price quote:
   if(!SymbolInfoTick(_Symbol,latestP))
     {
      Alert("Error getting the latest price quote - error:1003");
      Arun_error=1003;
      return(0);
     }

//--- price details of the latest maxbar bars:
   if(CopyRates(_Symbol,_Period,0,maxbar,BarRates)<0)
     {
      Alert("Error copying rates/history data - error:1003");
      Arun_error=1003;
      return(0);
     }

// for Previous completed bar, where 0-last current one in progress. 
   if(price=="open") barprice=BarRates[bar].open;
   if(price=="close") barprice=BarRates[bar].close; // if bar=0 , its close price is same as current price bid
   if(price=="high") barprice=BarRates[bar].high;
   if(price=="low") barprice=BarRates[bar].low;

// for Current Bar in Progress:
   if(price=="bid") barprice=latestP.bid;
   if(price=="ask") barprice=latestP.ask;
   if(price=="last") barprice=latestP.last;

   return(barprice);
  }
//+------------------------------------+
//| Execute TRADE                      |
//+------------------------------------+  
bool ExecuteTrade(string Entry,double ThePrice,double lot) // Entry = buy or sell / returns true if successfull.
  {
   bool success;

   success=true;

   MqlTradeRequest mreq; // for trade send request.
   MqlTradeResult mresu; // get trade result.
   ZeroMemory(mreq); // Initialize trade send request.

   Print("Order Initialized");
   mreq.action = TRADE_ACTION_DEAL;                                   // immediate order execution
   if(Entry=="buy") mreq.price = NormalizeDouble(ThePrice,_Digits);   // should be latest bid price
   if(Entry=="sell") mreq.price = NormalizeDouble(ThePrice,_Digits);  // should be latest ask price
   mreq.symbol = _Symbol;                                             // currency pair
   mreq.volume = lot;                                                 // number of lots to trade
   mreq.magic = 11119;                                                // Order Magic Number
   if(Entry=="sell") mreq.type = ORDER_TYPE_SELL;                     // Sell Order
   if(Entry=="buy") mreq.type = ORDER_TYPE_BUY;                       // Buy Order
   mreq.type_filling = ORDER_FILLING_FOK;                             // Order execution type
   mreq.deviation=100;                                                // Deviation from current price
//--- send order
   if(!OrderSend(mreq,mresu))
     {
      Alert("Order Not Sent: ",GetLastError());
      ResetLastError();
      success=false;
     }

// Result code
   if(mresu.retcode==10009 || mresu.retcode==10008) //Request is completed or order placed       
     {
      if(Entry=="SELL") Print("A Sell order has been successfully placed with Ticket#:",mresu.order,"!!");
      if(Entry=="BUY") Print("A Buy order has been successfully placed with Ticket#:",mresu.order,"!!");
     }
   else
     {
      Alert("The Order not completed -error:",GetLastError());
      ResetLastError();
      success=false;
     }

   if(success==false)
     {
      Alert("Error ORDER FAILED!!! - error:1004");
      Arun_error=1004;
     }
   return(success);
  }
//+------------------------------------+
//|  Check if there's an open position | 
//+------------------------------------+ 
string OpenPosition() // Returns "none", "buy", "sell"
  {
   string post;

   post="none";

   if(PositionSelect(_Symbol)==true) // open position 
     {
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         post="buy";
        }
      else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         post="sell";
        }
     }

   return(post);
  }
//+------------------------------------------------------------------+
