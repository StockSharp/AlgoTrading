//+------------------------------------------------------------------+
//|                                               Multik_SMA_Exp.mq5 | 
//|                         Copyright © 2010,  Nikolay Kositsin, AM2 |
//|                              Khabarovsk,   farria@mail.redcom.ru |
//+------------------------------------------------------------------+
//|                                     Multicurrency Expert Advisor |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version
#property version   "1.04"
//+-------------------------------------+
//|  Input parameters of Expert Advisor |
//+-------------------------------------+
input string            Symb0 = "EURUSD";
input  bool            Trade0 = true;
input int                Per0 = 50;
input ENUM_APPLIED_PRICE ApPrice0 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod0    = MODE_SMA;
input int             StLoss0 = 0;
input int           TkProfit0 = 0;
input double            Lots0 = 1;
input int           Slippage0 = 30;
//+-----------------------------------+
input string            Symb1 = "USDCHF";
input  bool            Trade1 = false;
input int                Per1 = 100;
input ENUM_APPLIED_PRICE ApPrice1 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod1    = MODE_SMA;
input int             StLoss1 = 0;
input int           TkProfit1 = 0;
input double            Lots1 = 0.1;
input int           Slippage1 = 30;
//+-----------------------------------+
input string            Symb2 = "USDJPY";
input  bool            Trade2 = false;
input int                Per2 = 100;
input ENUM_APPLIED_PRICE ApPrice2 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod2    = MODE_SMA;
input int             StLoss2 = 0;
input int           TkProfit2 = 0;
input double            Lots2 = 0.1;
input int           Slippage2 = 30;
//+-----------------------------------+
input string            Symb3 = "USDCAD";
input  bool            Trade3 = false;
input int                Per3 = 100;
input ENUM_APPLIED_PRICE ApPrice3 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod3    = MODE_SMA;
input int             StLoss3 = 0;
input int           TkProfit3 = 0;
input double            Lots3 = 0.1;
input int           Slippage3 = 30;
//+-----------------------------------+
input string            Symb4 = "AUDUSD";
input  bool            Trade4 = false;
input int                Per4 = 100;
input ENUM_APPLIED_PRICE ApPrice4 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod4    = MODE_SMA;
input int             StLoss4 = 0;
input int           TkProfit4 = 0;
input double            Lots4 = 0.1;
input int           Slippage4 = 30;
//+-----------------------------------+
input string            Symb5 = "EURGBP";
input  bool            Trade5 = false;
input int                Per5 = 100;
input ENUM_APPLIED_PRICE ApPrice5 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod5    = MODE_SMA;
input int             StLoss5 = 0;
input int           TkProfit5 = 0;
input double            Lots5 = 0.1;
input int           Slippage5 = 30;
//+-----------------------------------+
input string            Symb6 = "EURAUD";
input  bool            Trade6 = false;
input int                Per6 = 100;
input ENUM_APPLIED_PRICE ApPrice6 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod6    = MODE_SMA;
input int             StLoss6 = 0;
input int           TkProfit6 = 0;
input double            Lots6 = 0.1;
input int           Slippage6 = 30;
//+-----------------------------------+
input string            Symb7 = "EURCHF";
input  bool            Trade7 = false;
input int                Per7 = 100;
input ENUM_APPLIED_PRICE ApPrice7 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod7    = MODE_SMA;
input int             StLoss7 = 0;
input int           TkProfit7 = 0;
input double            Lots7 = 0.1;
input int           Slippage7 = 30;
//+-----------------------------------+
input string            Symb8 = "GBPCHF";
input  bool            Trade8 = false;
input int                Per8 = 100;
input ENUM_APPLIED_PRICE ApPrice8 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod8    = MODE_SMA;
input int             StLoss8 = 0;
input int           TkProfit8 = 0;
input double            Lots8 = 0.1;
input int           Slippage8 = 30;
//+-----------------------------------+
input string            Symb9 = "GBPUSD";
input  bool            Trade9 = true;
input int                Per9 = 50;
input ENUM_APPLIED_PRICE ApPrice9 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod9    = MODE_SMA;
input int             StLoss9 = 0;
input int           TkProfit9 = 0;
input double            Lots9 = 1;
input int           Slippage9 = 30;
//+-----------------------------------+
input string            Symb10 = "GBPJPY";
input  bool            Trade10 = false;
input int                Per10 = 100;
input ENUM_APPLIED_PRICE ApPrice10 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod10    = MODE_SMA;
input int             StLoss10 = 0;
input int           TkProfit10 = 0;
input double            Lots10 = 0.1;
input int           Slippage10 = 30;
//+-----------------------------------+
input string            Symb11 = "EURJPY";
input  bool            Trade11 = false;
input int                Per11 = 100;
input ENUM_APPLIED_PRICE ApPrice11 = PRICE_CLOSE;
input ENUM_MA_METHOD MaMethod11    = MODE_SMA;
input int             StLoss11 = 0;
input int           TkProfit11 = 0;
input double            Lots11 = 0.1;
input int           Slippage11 = 30;
//+-----------------------------------+

//+------------------------------------------------------------------+
//| Custom TradeSignalCounter() function                             |
//+------------------------------------------------------------------+
bool TradeSignalCounter(int Number,
                        string Symbol_,
                        bool Trade,
                        int period,
                        ENUM_APPLIED_PRICE ApPrice,
                        ENUM_MA_METHOD MaMethod,
                        bool &UpSignal[],
                        bool &DnSignal[],
                        bool &UpStop[],
                        bool &DnStop[])
  {
//--- is trade allowed
   if(!Trade)return(true);

//--- array size
   static int Size_=0;

//--- array for handles
   static int Handle[];

   static int Recount[],MinBars[];
   double SMA[],dsma1,dsma2;

//--- initialization
   if(Number+1>Size_) // Initalization only at first start
     {
      Size_=Number+1;

      //---- Resize arrays
      ArrayResize(SMA,4);
      ArrayResize(Handle,Size_);
      ArrayResize(Recount,Size_);
      ArrayResize(MinBars,Size_);
      
      ArrayInitialize(Handle,0);
      ArrayInitialize(Recount,0);
      ArrayInitialize(MinBars,0);

      //---- determine minimal number of bars, sufficient for the calculation
      MinBars[Number]=3*period;

      //---- fill arrays with initial values
      DnSignal[Number] = false;
      UpSignal[Number] = false;
      DnStop  [Number] = false;
      UpStop  [Number] = false;

      //---- set as timeseries
      ArraySetAsSeries(SMA,true);

      //--- get handle of the indicator
      Handle[Number]=iMA(Symbol_,0,period,0,MODE_SMA,ApPrice);
     }

//--- check the number of bars
   if(Bars(Symbol_,0)<MinBars[Number])return(true);

//--- get trade signals
   if(IsNewBar(Number,Symbol_,0) || Recount[Number]) // Only at new bar or in the case of copy failed
     {
      DnSignal[Number] = false;
      UpSignal[Number] = false;
      DnStop  [Number] = false;
      UpStop  [Number] = false;

      //--- using indicator handles, copying the values of the indicator buffer
      //--- to special static array
      if(CopyBuffer(Handle[Number],0,0,4,SMA)<0)
        {
         Recount[Number]=true; // we haven't a data yet, so go here at new tick

         return(false); // return from the TradeSignalCounter() without trading signals
        }

      //---- All data from the indicator's buffers have been copied successfully
      Recount[Number]=false; // don't go here until the new bar

      dsma2 = NormalizeDouble(SMA[2] - SMA[3], _Digits);      // MA for 2-3
      dsma1 = NormalizeDouble(SMA[1] - SMA[2], _Digits);      // MA for 1-2

      //---- Determine entry signals
      if(dsma2 > 0 && dsma1 > 0) DnSignal[Number] = true;    // buy if MA is falling at 1-2 and 2-3
      if(dsma2 < 0 && dsma1 < 0) UpSignal[Number] = true;    // buy if MA is growing at 1-2 and 2-3

      //---- Determine exist signals
      if(dsma1 < 0) DnStop[Number] = true;                   // sell if MA is growing at 1-2
      if(dsma1 > 0) UpStop[Number] = true;                   // sell if MA is falling at 1-2
     }
//---
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
//---
//--- Is trading allowed
   if(!Trade)return(true);

//---- Close opened positions
   if(UpStop[Number])BuyPositionClose(Symbol_,Slippage);
   if(DnStop[Number])SellPositionClose(Symbol_,Slippage);

//---- Open new positions
   if(UpSignal[Number])
      if(BuyPositionOpen(Symbol_,Slippage,Lots,StLoss,TkProfit))
         UpSignal[Number]=false; //We will not use the signal on this bar!
//----  
   if(DnSignal[Number])
      if(SellPositionOpen(Symbol_,Slippage,Lots,StLoss,TkProfit))
         DnSignal[Number]=false; //We will not use the signal on this bar!
//---
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
//--- declare structures for trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

//--- is there any opened position?
   if(!PositionSelect(symbol))
     {
      //--- initialize the MqlTradeRequest structure to open BUY position
      request.type   = ORDER_TYPE_BUY;
      request.price  = SymbolInfoDouble(symbol, SYMBOL_ASK);
      request.action = TRADE_ACTION_DEAL;
      request.symbol = symbol;
      request.volume = Money_M();
      request.sl = 0;
      request.tp = 0;
      request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
      request.type_filling=ORDER_FILLING_FOK;
      //---
      string word="";
      StringConcatenate(word,
                        "<<< ============ BuyPositionOpen():   Open Buy position on ",
                        symbol," ============ >>>");
      Print(word);

      //--- open BUY position and check trade server return code
      if(!OrderSend(request,result) || result.deal==0)
        {
         Print(ResultRetcodeDescription(result.retcode));
         return(false);
        }
     }
//---
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
//--- declare structures for trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

//--- is there any opened position?
   if(!PositionSelect(symbol))
     {
      //--- Initialize the MqlTradeRequest structure to open SELL position
      request.type   = ORDER_TYPE_SELL;
      request.price  = SymbolInfoDouble(symbol, SYMBOL_BID);
      request.action = TRADE_ACTION_DEAL;
      request.symbol = symbol;
      request.volume = Money_M();
      request.sl = 0;
      request.tp = 0;
      request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
      request.type_filling=ORDER_FILLING_FOK;

      //---
      string word="";
      StringConcatenate(word,
                        "<<< ============ SellPositionOpen():   Open Sell position on ",
                        symbol," ============ >>>");
      Print(word);

      //--- open SELL position and check trade server return code
      if(!OrderSend(request,result) || result.deal==0)
        {
         Print(ResultRetcodeDescription(result.retcode));
         return(false);
        }
     }
//---
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
//---
//--- declare a variables for trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

//--- check opened BUY position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_BUY) return(false);
     }
   else  return(false);

//--- Prepare the structure of MqlTradeRequest type for BUY position close
   request.type   = ORDER_TYPE_SELL;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_BID);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = PositionGetDouble(POSITION_VOLUME);
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//---
   string word="";
   StringConcatenate(word,
                     "<<< ============ BuyPositionClose():   Close Buy position on ",
                     symbol," ============ >>>");
   Print(word);

//--- send order to close position to trade server
   if(!OrderSend(request,result))
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//---
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
//---
//--- declare a variables for trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

//--- check opened Sell position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_SELL)return(false);
     }
   else return(false);

//--- prepare the structure of MqlTradeRequest type for SELL position close
   request.type   = ORDER_TYPE_BUY;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_ASK);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = PositionGetDouble(POSITION_VOLUME);
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//---
   string word="";
   StringConcatenate(word,
                     "<<< ============ SellPositionClose():   Close Sell position on",
                     symbol," ============ >>>");
   Print(word);

//--- send order to close position to trade server
   if(!OrderSend(request,result))
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- 

//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
//---   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- 
//--- declare arrays for trading signals
   static bool UpSignal[12],DnSignal[12],UpStop[12],DnStop[12];

//--- get trading signals
   TradeSignalCounter(0,Symb0,Trade0,Per0,ApPrice0,MaMethod0,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(1,Symb1,Trade1,Per1,ApPrice1,MaMethod1,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(2,Symb2,Trade2,Per2,ApPrice2,MaMethod2,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(3,Symb3,Trade3,Per3,ApPrice3,MaMethod3,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(4,Symb4,Trade4,Per4,ApPrice4,MaMethod4,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(5,Symb5,Trade5,Per5,ApPrice5,MaMethod5,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(6,Symb6,Trade6,Per6,ApPrice6,MaMethod6,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(7,Symb7,Trade7,Per7,ApPrice7,MaMethod7,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(8,Symb8,Trade8,Per8,ApPrice8,MaMethod8,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(9,Symb9,Trade9,Per9,ApPrice9,MaMethod9,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(10,Symb10,Trade10,Per10,ApPrice10,MaMethod10,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(11,Symb11,Trade11,Per11,ApPrice11,MaMethod11,UpSignal,DnSignal,UpStop,DnStop);

//--- perform trade operations
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
//---   
  }
//+------------------------------------------------------------------+
//| IsNewBar() function                                              |
//+------------------------------------------------------------------+
bool IsNewBar(int Number,string symbol,ENUM_TIMEFRAMES timeframe)
  {
//---
   static datetime Told[];
   datetime Tnew[1];
//--- declare a variable for array sizes
   static int Size_=0;

//--- resize arrrays
   if(Number+1>Size_)
     {
      uint size=Number+1;
      //----
      if(ArrayResize(Told,size)==-1)
        {
         string word="";
         StringConcatenate(word,"IsNewBar( ",Number,
                           " ): Error!!! Array resize failed!!!");
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
//---
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
//---
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
      case TRADE_RETCODE_NO_CHANGES:
         str="No changes in request";
         break;
      case TRADE_RETCODE_SERVER_DISABLES_AT:
         str="Autotrading disabled by server";
         break;
      case TRADE_RETCODE_CLIENT_DISABLES_AT:
         str="Autotrading disabled by client terminal";
         break;
      case TRADE_RETCODE_LOCKED:
         str="Request locked for processing";
         break;
      case TRADE_RETCODE_FROZEN:
         str="Order or position frozen";
         break;
      case TRADE_RETCODE_INVALID_FILL:
         str="Invalid order filling type";
         break;
      case TRADE_RETCODE_CONNECTION:
         str="No connection with the trade server";
         break;
      case TRADE_RETCODE_ONLY_REAL:
         str="Operation is allowed only for live accounts";
         break;
      case TRADE_RETCODE_LIMIT_ORDERS:
         str="The number of pending orders has reached the limit";
         break;
      case TRADE_RETCODE_LIMIT_VOLUME:
         str="The volume of orders and positions for the symbol has reached the limit";
         break;
      default:
         str="Unknown result";
     }
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Returns volume of the position                                   |
//+------------------------------------------------------------------+
double Money_M()
  {
   double Lots=AccountInfoDouble(ACCOUNT_FREEMARGIN)/100000*10;
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
