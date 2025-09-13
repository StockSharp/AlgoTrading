//+------------------------------------------------------------------+
//|                                                     Exp_TEMA.mq5 |
//|                              Copyright © 2010,  Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru |
//+------------------------------------------------------------------+
//| This multi-currency Expert Advisor trades using                  |
//| the iTEMA indicator signals                                      |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version
#property version   "1.00"
//+-----------------------------------+
//| Expert input parameters           |
//+-----------------------------------+
input string            Symb0 = "EURUSD";
input  bool            Trade0 = true;
input int                Per0 = 15;
input ENUM_APPLIED_PRICE ApPrice0=PRICE_CLOSE;
input int             StLoss0 = 1000;
input int           TkProfit0 = 2000;
input double            Lots0 = 0.1;
input int           Slippage0 = 30;
//+-----------------------------------+
input string            Symb1 = "USDCHF";
input  bool            Trade1 = true;
input int                Per1 = 15;
input ENUM_APPLIED_PRICE ApPrice1=PRICE_CLOSE;
input int             StLoss1 = 1000;
input int           TkProfit1 = 2000;
input double            Lots1 = 0.1;
input int           Slippage1 = 30;
//+-----------------------------------+
input string            Symb2 = "USDJPY";
input  bool            Trade2 = true;
input int                Per2 = 15;
input ENUM_APPLIED_PRICE ApPrice2=PRICE_CLOSE;
input int             StLoss2 = 1000;
input int           TkProfit2 = 2000;
input double            Lots2 = 0.1;
input int           Slippage2 = 30;
//+-----------------------------------+
input string            Symb3 = "USDCAD";
input  bool            Trade3 = true;
input int                Per3 = 15;
input ENUM_APPLIED_PRICE ApPrice3=PRICE_CLOSE;
input int             StLoss3 = 1000;
input int           TkProfit3 = 2000;
input double            Lots3 = 0.1;
input int           Slippage3 = 30;
//+-----------------------------------+
input string            Symb4 = "AUDUSD";
input  bool            Trade4 = true;
input int                Per4 = 15;
input ENUM_APPLIED_PRICE ApPrice4=PRICE_CLOSE;
input int             StLoss4 = 1000;
input int           TkProfit4 = 2000;
input double            Lots4 = 0.1;
input int           Slippage4 = 30;
//+-----------------------------------+
input string            Symb5 = "AUDNZD";
input  bool            Trade5 = true;
input int                Per5 = 15;
input ENUM_APPLIED_PRICE ApPrice5=PRICE_CLOSE;
input int             StLoss5 = 1000;
input int           TkProfit5 = 2000;
input double            Lots5 = 0.1;
input int           Slippage5 = 30;
//+-----------------------------------+
input string            Symb6 = "AUDCAD";
input  bool            Trade6 = true;
input int                Per6 = 15;
input ENUM_APPLIED_PRICE ApPrice6=PRICE_CLOSE;
input int             StLoss6 = 1000;
input int           TkProfit6 = 2000;
input double            Lots6 = 0.1;
input int           Slippage6 = 30;
//+-----------------------------------+
input string            Symb7 = "AUDCHF";
input  bool            Trade7 = true;
input int                Per7 = 15;
input ENUM_APPLIED_PRICE ApPrice7=PRICE_CLOSE;
input int             StLoss7 = 1000;
input int           TkProfit7 = 2000;
input double            Lots7 = 0.1;
input int           Slippage7 = 30;
//+-----------------------------------+
input string            Symb8 = "AUDJPY";
input  bool            Trade8 = true;
input int                Per8 = 15;
input ENUM_APPLIED_PRICE ApPrice8=PRICE_CLOSE;
input int             StLoss8 = 1000;
input int           TkProfit8 = 2000;
input double            Lots8 = 0.1;
input int           Slippage8 = 30;
//+-----------------------------------+
input string            Symb9 = "GBPUSD";
input  bool            Trade9 = true;
input int                Per9 = 15;
input ENUM_APPLIED_PRICE ApPrice9=PRICE_CLOSE;
input int             StLoss9 = 1000;
input int           TkProfit9 = 2000;
input double            Lots9 = 0.1;
input int           Slippage9 = 30;
//+-----------------------------------+
input string            Symb10 = "GBPJPY";
input  bool            Trade10 = true;
input int                Per10 = 15;
input ENUM_APPLIED_PRICE ApPrice10=PRICE_CLOSE;
input int             StLoss10 = 1000;
input int           TkProfit10 = 2000;
input double            Lots10 = 0.1;
input int           Slippage10 = 30;
//+-----------------------------------+
input string            Symb11 = "EURJPY";
input  bool            Trade11 = true;
input int                Per11 = 15;
input ENUM_APPLIED_PRICE ApPrice11=PRICE_CLOSE;
input int             StLoss11 = 1000;
input int           TkProfit11 = 2000;
input double            Lots11 = 0.1;
input int           Slippage11 = 30;
//+------------------------------------------------------------------+
//| Custom TradeSignalCounter() function                             |
//+------------------------------------------------------------------+
bool TradeSignalCounter(int Number,
                        string Symbol_,
                        bool Trade,
                        int period,
                        ENUM_APPLIED_PRICE ApPrice,
                        bool &UpSignal[],
                        bool &DnSignal[],
                        bool &UpStop[],
                        bool &DnStop[])

  {
//---- check if trade is prohibited
   if(!Trade)return(true);

//---- declare variable to store final size of variables arrays
   static int Size_=0;

//---- declare array to store handles of indicators as static variable
   static int Handle[];

   static int Recount[],MinBars[];
   double TEMA[4],dtema1,dtema2;

//---- initialization 
   if(Number+1>Size_) // entering the initialization block only on first start
     {
      Size_=Number+1; // for this number entering the block is prohibited

      //---- change size of variables arrays
      ArrayResize(Handle,Size_);
      ArrayResize(Recount,Size_);
      ArrayResize(MinBars,Size_);

      //---- determine minimum number of bars, sufficient for calculation 
      MinBars[Number]=3*period;

      //---- setting array elements to 0
      DnSignal[Number] = false;
      UpSignal[Number] = false;
      DnStop  [Number] = false;
      UpStop  [Number] = false;

      //---- use array as timeseries
      ArraySetAsSeries(TEMA,true);

      //---- get indicator's handle
      Handle[Number]=iTEMA(Symbol_,0,period,0,ApPrice);
     }

//---- checking the number of bars to be enough for the calculation 
   if(Bars(Symbol_,0)<MinBars[Number])return(true);

//---- Get trade signals 
   if(IsNewBar(Number,Symbol_,0) || Recount[Number]) // entering the block on bar change or on failed copying of data
     {
      DnSignal[Number] = false;
      UpSignal[Number] = false;
      DnStop  [Number] = false;
      UpStop  [Number] = false;

      //---- using handles of the indicator, copy the indicator buffer values
      //---- into static array, specially prepared for this purpose
      if(CopyBuffer(Handle[Number],0,0,4,TEMA)<0)
        {
         Recount[Number]=true; // as data were not received, we should return 
                               // into this block (where trade signals are received) on next tick!
         return(false);        // exiting the TradeSignalCounter() function without receiving trade signals
        }

      //---- all copy operations from indicator buffer are successfully completed
      Recount[Number]=false; // we may not return to this block until next change of bar

      int Digits_=int(SymbolInfoInteger(Symbol_,SYMBOL_DIGITS)+4);
      dtema2 = NormalizeDouble(TEMA[2] - TEMA[3], Digits_);
      dtema1 = NormalizeDouble(TEMA[1] - TEMA[2], Digits_);

      //---- determining the input signals
      if(dtema2 > 0 && dtema1 < 0) DnSignal[Number] = true;
      if(dtema2 < 0 && dtema1 > 0) UpSignal[Number] = true;

      //---- determining the output signals
      if(dtema1 > 0) DnStop[Number] = true;
      if(dtema1 < 0) UpStop[Number] = true;
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Custom TradePerformer() function                                 |
//+------------------------------------------------------------------+
bool TradePerformer(int    Number,
                    string Symbol_,
                    bool   Trade,
                    int    StLoss,
                    int    TkProfit,
                    double Lots,
                    int    Slippage,
                    bool  &UpSignal[],
                    bool  &DnSignal[],
                    bool  &UpStop[],
                    bool  &DnStop[])
  {
//---- check if trade is prohibited
   if(!Trade)return(true);

//---- close opened positions 
   if(UpStop[Number])BuyPositionClose(Symbol_,Slippage);
   if(DnStop[Number])SellPositionClose(Symbol_,Slippage);

//---- open new positions
   if(UpSignal[Number])
      if(BuyPositionOpen(Symbol_,Slippage,Lots,StLoss,TkProfit))
         UpSignal[Number]=false; // this trade signal will be no more on this bar!
//----  
   if(DnSignal[Number])
      if(SellPositionOpen(Symbol_,Slippage,Lots,StLoss,TkProfit))
         DnSignal[Number]=false; // this trade signal will be no more on this bar!
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Open buy position.                                               |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool BuyPositionOpen(const string symbol,
                     ulong deviation,
                     double volume,
                     int StopLoss,
                     int Takeprofit)
  {
//---- declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);

//---- check if there is opened position by exposure for deal
   if(PositionSelect(symbol))return(false);

   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   int stoplevel=int(SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL));
   double point = SymbolInfoDouble(symbol,SYMBOL_POINT);

//---- determine distance to Stop Loss (in price chart units)
   if(StopLoss<stoplevel && StopLoss>0)StopLoss=stoplevel;
   double dStopLoss=StopLoss*point;

//---- determine distance to Take Profit (in price chart units)
   if(Takeprofit<stoplevel && Takeprofit>0)Takeprofit=stoplevel;
   double dTakeprofit=Takeprofit*point;

//---- initializing MqlTradeRequest structure to open a BUY position
   request.type   = ORDER_TYPE_BUY;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_ASK);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
//----
   if(StopLoss   != 0) request.sl = NormalizeDouble(request.price - dStopLoss,   digit); else request.sl = 0.0;
   if(Takeprofit != 0) request.tp = NormalizeDouble(request.price + dTakeprofit, digit); else request.tp = 0.0;
//----
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//----
   string word="";
   StringConcatenate(word,
                     "<<< ============ BuyPositionOpen():   Open Buy position at ",
                     symbol," ============ >>>");
   Print(word);

//--- open the BUY position and check the result of the trade request
   if(!OrderSend(request,result) || result.deal==0)
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Open sell position.                                              |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool SellPositionOpen(const string symbol,
                      ulong deviation,
                      double volume,
                      int StopLoss,
                      int Takeprofit)
  {
//---- declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);

//---- check if there is opened position by exposure for deal
   if(PositionSelect(symbol))return(false);

   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   int stoplevel=int(SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL));
   double point = SymbolInfoDouble(symbol,SYMBOL_POINT);

//---- determine distance to Stop Loss (in price chart units)
   if(StopLoss<stoplevel && StopLoss>0)StopLoss=stoplevel;
   double dStopLoss=StopLoss*point;

//---- determine distance to Take Profit (in price chart units)
   if(Takeprofit<stoplevel && Takeprofit>0)Takeprofit=stoplevel;
   double dTakeprofit=Takeprofit*point;

//---- initializing the MqlTradeRequest structure to open SELL position
   request.type   = ORDER_TYPE_SELL;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_BID);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
//----
   if(StopLoss   != 0) request.sl = NormalizeDouble(request.price + dStopLoss,   digit); else request.sl = 0.0;
   if(Takeprofit != 0) request.tp = NormalizeDouble(request.price - dTakeprofit, digit); else request.tp = 0.0;
//----
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;

//----
   string word="";
   StringConcatenate(word,
                     "<<< ============ SellPositionOpen():   Open Sell position at ",
                     symbol," ============ >>>");
   Print(word);

//---- open the SELL position and check the result of the trade request
   if(!OrderSend(request,result) || result.deal==0)
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Close specified opened buy position.                             |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool BuyPositionClose(const string symbol,ulong deviation)
  {
//---- declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);

//---- check if there is a BUY position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_BUY) return(false);
     }
   else  return(false);

//---- initializing the MqlTradeRequest structure to close the BUY position
   request.type   = ORDER_TYPE_SELL;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_BID);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = PositionGetDouble(POSITION_VOLUME);
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//----
   string word="";
   StringConcatenate(word,
                     "<<< ============ BuyPositionClose():   Close Buy position at ",
                     symbol," ============ >>>");
   Print(word);

//---- send order to close position to trade server
   if(!OrderSend(request,result))
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Close specified sell opened position.                            |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool SellPositionClose(const string symbol,ulong deviation)
  {
//---- declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);

//---- check if there is a BUY position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_SELL)return(false);
     }
   else return(false);

//---- initializing the MqlTradeRequest structure to close the SELL position
   request.type   = ORDER_TYPE_BUY;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_ASK);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = PositionGetDouble(POSITION_VOLUME);
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//----
   string word="";
   StringConcatenate(word,
                     "<<< ============ SellPositionClose():   Close Sell position at ",
                     symbol," ============ >>>");
   Print(word);

//---- send order to close position to trade server
   if(!OrderSend(request,result))
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- 

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----

//----   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---- declare variables arrays for trade signals  
   static bool UpSignal[12],DnSignal[12],UpStop[12],DnStop[12];

//---- get trade signals
   TradeSignalCounter(0,Symb0,Trade0,Per0,ApPrice0,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(1,Symb1,Trade1,Per1,ApPrice1,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(2,Symb2,Trade2,Per2,ApPrice2,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(3,Symb3,Trade3,Per3,ApPrice3,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(4,Symb4,Trade4,Per4,ApPrice4,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(5,Symb5,Trade5,Per5,ApPrice5,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(6,Symb6,Trade6,Per6,ApPrice6,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(7,Symb7,Trade7,Per7,ApPrice7,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(8,Symb8,Trade8,Per8,ApPrice8,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(9,Symb9,Trade9,Per9,ApPrice9,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(10,Symb10,Trade10,Per10,ApPrice10,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(11,Symb11,Trade11,Per11,ApPrice11,UpSignal,DnSignal,UpStop,DnStop);

//---- perform trade operations
   TradePerformer(0,Symb0,Trade0,StLoss0,TkProfit0,Lots0,Slippage0,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(1,Symb1,Trade1,StLoss1,TkProfit1,Lots1,Slippage1,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(2,Symb2,Trade2,StLoss2,TkProfit2,Lots2,Slippage2,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(3,Symb3,Trade3,StLoss3,TkProfit3,Lots3,Slippage3,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(4,Symb4,Trade4,StLoss4,TkProfit4,Lots4,Slippage4,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(5,Symb5,Trade5,StLoss5,TkProfit5,Lots5,Slippage5,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(6,Symb6,Trade6,StLoss6,TkProfit6,Lots6,Slippage6,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(7,Symb7,Trade7,StLoss7,TkProfit7,Lots7,Slippage7,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(8,Symb8,Trade8,StLoss8,TkProfit8,Lots8,Slippage8,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(9,Symb9,Trade9,StLoss9,TkProfit9,Lots9,Slippage9,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(10,Symb10,Trade10,StLoss10,TkProfit10,Lots10,Slippage10,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(11,Symb11,Trade11,StLoss11,TkProfit11,Lots11,Slippage11,UpSignal,DnSignal,UpStop,DnStop);
//----   
  }
//+------------------------------------------------------------------+
//| IsNewBar() function                                              |
//+------------------------------------------------------------------+
bool IsNewBar(int Number,string symbol,ENUM_TIMEFRAMES timeframe)
  {
//----
   static datetime Told[];
   datetime Tnew[1];
//---- declare variable to store sizes of variables arrays
   static int Size_=0;

//---- change size of variables arrays
   if(Number+1>Size_)
     {
      uint size=Number+1;
      //----
      if(ArrayResize(Told,size)==-1)
        {
         string word="";
         StringConcatenate(word,"IsNewBar( ",Number,
                           " ): Error!!! Failed to change sizes of arrays of variables!!!");
         Print(word);
         //----          
         int error=GetLastError();
         ResetLastError();
         if(error>4000)
           {
            StringConcatenate(word,"IsNewBar( ",Number," ): Error code ",error);
            Print(word);
           }
         //----                                                                                                                                                                                                  
         Size_=-2;
         return(false);
        }
     }

   CopyTime(symbol,timeframe,0,1,Tnew);
   if(Tnew[0]!=Told[Number])
     {
      Told[Number]=Tnew[0];
      return(true);
     }
//----
   return(false);
  }
//+------------------------------------------------------------------+
//| Get the retcode value as string.                                 |
//| INPUT:  no.                                                      |
//| OUTPUT: the retcode value as string.                             |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
string ResultRetcodeDescription(int retcode)
  {
   string str;
//----
   switch(retcode)
     {
      case TRADE_RETCODE_REQUOTE:
         str="Requote";
         break;
      case TRADE_RETCODE_REJECT:
         str="Request rejected";
         break;
      case TRADE_RETCODE_CANCEL:
         str="Request cancelled by trader";
         break;
      case TRADE_RETCODE_PLACED:
         str="Order placed";
         break;
      case TRADE_RETCODE_DONE:
         str="Request done";
         break;
      case TRADE_RETCODE_DONE_PARTIAL:
         str="Request done partially";
         break;
      case TRADE_RETCODE_ERROR:
         str="Common error";
         break;
      case TRADE_RETCODE_TIMEOUT:
         str="Request cancelled by timeout";
         break;
      case TRADE_RETCODE_INVALID:
         str="Invalid request";
         break;
      case TRADE_RETCODE_INVALID_VOLUME:
         str="Invalid volume in request";
         break;
      case TRADE_RETCODE_INVALID_PRICE:
         str="Invalid price in request";
         break;
      case TRADE_RETCODE_INVALID_STOPS:
         str="Invalid stop(s) request";
         break;
      case TRADE_RETCODE_TRADE_DISABLED:
         str="Trade is disabled";
         break;
      case TRADE_RETCODE_MARKET_CLOSED:
         str="Market is closed";
         break;
      case TRADE_RETCODE_NO_MONEY:
         str="No enough money";
         break;
      case TRADE_RETCODE_PRICE_CHANGED:
         str="Price changed";
         break;
      case TRADE_RETCODE_PRICE_OFF:
         str="No quotes for query processing";
         break;
      case TRADE_RETCODE_INVALID_EXPIRATION:
         str="Invalid expiration time in request";
         break;
      case TRADE_RETCODE_ORDER_CHANGED:
         str="Order state changed";
         break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS:
         str="Too frequent requests";
         break;
      default:
         str="Unknown result";
     }
//----
   return(str);
  }
//+------------------------------------------------------------------+
