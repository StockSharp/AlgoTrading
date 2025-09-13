//+------------------------------------------------------------------+
//|                                             Exp_ATR_Trailing.mq5 |
//|                             Copyright ฉ 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright ฉ 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Expert Advisor input parameters              |
//+----------------------------------------------+
input int Period_ATR=14;  //ภาR period
input double Sell_Factor=2.0;
input double Buy_Factor=2.0;
input uint Deviation=10;  //slippage
//+----------------------------------------------+

//---- declaration of integer variables for the indicators handles
int InpInd_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Inclusion of CChart class in expert                              |
//+------------------------------------------------------------------+
#include <Charts\Chart.mqh>
//---- declaration of a global variable as CChart type
CChart cchart;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting handle of the ATR_Trailing indicator
   InpInd_Handle=iCustom(Symbol(),PERIOD_CURRENT,"ATR_Trailing",Period_ATR,Sell_Factor,Buy_Factor);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of ATR_Trailing indicator");

//--- resetting error code to zero
   ResetLastError();

//--- cchart object works with the current chart (ID=0) , the expert is attached to
   cchart.Attach(0);

//---- adding of ATR_Trailing indicator on the chart  
   if(!cchart.IndicatorAdd(0,InpInd_Handle)) Print(" Failed to add ATR_Trailing indicator on the chart");

//---- initialization of variables of the start of data calculation
   min_rates_total=Period_ATR;
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
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;

//---- declaration of local variables
   double DnValue[2],UpValue[2],NewStop;

//---- Declaration of static variables
   static bool Recount=true,BuySignal,SellSignal;
   static CIsNewBar NB;

//---- Checking, if there is an open position
   if(!PositionSelect(Symbol())) return;

//---- Getting of direction of an open position
   ENUM_POSITION_TYPE PosType=ENUM_POSITION_TYPE(PositionGetInteger(POSITION_TYPE));
   double LastStop=PositionGetDouble(POSITION_SL);

   if(!LastStop)
     {
      //---- zeroize the modification signal of position
      BuySignal=false;
      SellSignal=false;

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd_Handle,0,0,2,UpValue)<=0) return;
      if(CopyBuffer(InpInd_Handle,1,0,2,DnValue)<=0) return;

      switch(PosType)
        {
         case POSITION_TYPE_SELL:
           {
            NewStop=MathMax(UpValue[0],UpValue[1]);
            double Bid=SymbolInfoDouble(Symbol(),SYMBOL_BID);
            if(!Bid || NewStop<=Bid) return;
            SellSignal=true;
            break;
           }

         case POSITION_TYPE_BUY:
           {
            NewStop=MathMin(DnValue[0],DnValue[1]);
            double Ask=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
            if(!Ask || NewStop>=Ask) return;
            BuySignal=true;
            break;
           }
         default: return;
        }
     }
   else if(NB.IsNewBar(Symbol(),PERIOD_CURRENT) || Recount) // checking for a new bar
     {
      //---- zeroize the signal of re-entering into block
      Recount=false;

      //---- zeroize the modification signal of position
      BuySignal=false;
      SellSignal=false;

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd_Handle,0,0,2,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,0,2,DnValue)<=0) {Recount=true; return;}

      switch(PosType)
        {
         case POSITION_TYPE_SELL:
           {
            NewStop=UpValue[0];
            if(NewStop>=LastStop) return;
            SellSignal=true;
            break;
           }

         case POSITION_TYPE_BUY:
           {
            NewStop=DnValue[0];
            if(NewStop<=LastStop) return;
            BuySignal=true;
            break;
           }
         default: return;
        }
     }
//+----------------------------------------------+
//| Performing deals                             |
//+----------------------------------------------+
//---- Modifying a long position
   dBuyPositionModify(BuySignal,Symbol(),Deviation,NewStop,0.0);

//---- Modifying a short position
   dSellPositionModify(SellSignal,Symbol(),Deviation,NewStop,0.0);
//----
  }
//+------------------------------------------------------------------+
//| Modifying a long position                                        |
//+------------------------------------------------------------------+
bool dBuyPositionModify
(
 bool &Modify_Signal,        // modification allowing flag
 const string symbol,        // deal trading pair
 uint deviation,             // slippage
 double StopLoss,            // Stop loss in absolute value of price chart
 double Takeprofit           // Take profit in absolute value of price chart
 )
//dBuyPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;

//---- Checking, if there is an open position
   if(!PositionSelect(symbol)) return(true);
   if(PositionGetInteger(POSITION_TYPE)!=PosType) return(true);

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;

//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);
//----
   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   double point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   double Ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
   if(!digit || !point || !Ask) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- Determine distance to Stop Loss (in price chart units)
   if(StopLoss)
     {
      int nStopLoss=int((Ask-StopLoss)/point);
      if(nStopLoss<0) return(false);
      if(!StopCorrect(symbol,nStopLoss))return(false);
      double dStopLoss=nStopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,digit);
      if(request.sl<PositionGetDouble(POSITION_SL)) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Determine distance to Take Profit (in price chart units)
   if(Takeprofit)
     {
      int nTakeprofit=int((Takeprofit-Ask)/point);
      if(nTakeprofit<0) return(false);
      if(!StopCorrect(symbol,nTakeprofit))return(false);
      double dTakeprofit=nTakeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,digit);
      if(request.tp<PositionGetDouble(POSITION_TP)) request.tp=PositionGetDouble(POSITION_TP);
     }
   else request.tp=PositionGetDouble(POSITION_TP);

//----   
   if(request.tp==PositionGetDouble(POSITION_TP) && request.sl==PositionGetDouble(POSITION_SL)) return(true);
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Checking correctness of a trade request
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Incorrect data for a trade request structure!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Modifying Buy position at ",symbol," ============ >>>");
   Print(comment);

//---- Modify BUY position and check the result of trade request
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Unable to modify position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Modify_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," modified ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Unable to modify position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Modifying a short position                                       |
//+------------------------------------------------------------------+
bool dSellPositionModify
(
 bool &Modify_Signal,        // modification allowing flag
 const string symbol,        // deal trading pair
 uint deviation,             // slippage
 double StopLoss,            // Stop loss in absolute value of price chart
 double Takeprofit           // Take profit in absolute value of price chart
 )
//dSellPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;

//---- Checking, if there is an open position
   if(!PositionSelect(symbol)) return(true);
   if(PositionGetInteger(POSITION_TYPE)!=PosType) return(true);

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;

//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);
//----
   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   double point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   double Ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
   if(!digit || !point || !Ask) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_SELL;
   request.price  = Ask;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- Determine distance to Stop Loss (in price chart units)
   if(StopLoss!=0)
     {
      int nStopLoss=int((StopLoss-Ask)/point);
      if(nStopLoss<0) return(false);
      if(!StopCorrect(symbol,nStopLoss))return(false);
      double dStopLoss=nStopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,digit);
      double laststop=PositionGetDouble(POSITION_SL);
      if(request.sl>laststop && laststop) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Determine distance to Take Profit (in price chart units)
   if(Takeprofit!=0)
     {
      int nTakeprofit=int((Ask-Takeprofit)/point);
      if(nTakeprofit<0) return(false);
      if(!StopCorrect(symbol,nTakeprofit))return(false);
      double dTakeprofit=nTakeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,digit);
      double lasttake=PositionGetDouble(POSITION_TP);
      if(request.tp>lasttake && lasttake) request.tp=PositionGetDouble(POSITION_TP);
     }
   else request.tp=PositionGetDouble(POSITION_TP);

//----   
   if(request.tp==PositionGetDouble(POSITION_TP) && request.sl==PositionGetDouble(POSITION_SL)) return(true);
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Checking correctness of a trade request
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Incorrect data for a trade request structure!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Modifying Sell position at ",symbol," ============ >>>");
   Print(comment);

//---- Modifying SELL position and checking the result of a trade request
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Unable to modify position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Modify_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," modified ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Unable to modify position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| correction of a pending order size to an acceptable value        |
//+------------------------------------------------------------------+
bool StopCorrect(string symbol,int &Stop)
  {
//----
   int Extrem_Stop=int(SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL));
   if(!Extrem_Stop) return(false);
   if(Stop<Extrem_Stop) Stop=Extrem_Stop;
//----
   return(true);
  }
//+------------------------------------------------------------------+
//|  New bar appearing moment detection algorithm                    |
//+------------------------------------------------------------------+  
class CIsNewBar
  {
   //----
public:
   //---- new bar appearing moment detection function
   bool IsNewBar(string symbol,ENUM_TIMEFRAMES timeframe)
     {
      //---- getting the time of the current bar appearing
      datetime TNew=datetime(SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE));

      if(TNew!=m_TOld && TNew) // checking for a new bar
        {
         m_TOld=TNew;
         return(true); // a new bar has appeared!
        }
      //----
      return(false); // there are no new bars yet!
     };

   //---- class constructor    
                     CIsNewBar(){m_TOld=-1;};

protected: datetime m_TOld;
   //---- 
  };
//+------------------------------------------------------------------+
//| Returning a string result of a trading operation by its code     |
//+------------------------------------------------------------------+
string ResultRetcodeDescription(int retcode)
  {
   string str;
//----
   switch(retcode)
     {
      case TRADE_RETCODE_REQUOTE: str="Requote"; break;
      case TRADE_RETCODE_REJECT: str="Request rejected"; break;
      case TRADE_RETCODE_CANCEL: str="Request cancelled by trader"; break;
      case TRADE_RETCODE_PLACED: str="Order is placed"; break;
      case TRADE_RETCODE_DONE: str="Request is executed"; break;
      case TRADE_RETCODE_DONE_PARTIAL: str="Request is executed partially"; break;
      case TRADE_RETCODE_ERROR: str="Request processing error"; break;
      case TRADE_RETCODE_TIMEOUT: str="Request is cancelled because of a time out";break;
      case TRADE_RETCODE_INVALID: str="Invalid request"; break;
      case TRADE_RETCODE_INVALID_VOLUME: str="Invalid request volume"; break;
      case TRADE_RETCODE_INVALID_PRICE: str="Invalid request price"; break;
      case TRADE_RETCODE_INVALID_STOPS: str="Invalid request stops"; break;
      case TRADE_RETCODE_TRADE_DISABLED: str="Trading is forbidden"; break;
      case TRADE_RETCODE_MARKET_CLOSED: str="Market is closed"; break;
      case TRADE_RETCODE_NO_MONEY: str="Insufficient funds for request execution"; break;
      case TRADE_RETCODE_PRICE_CHANGED: str="Prices have changed"; break;
      case TRADE_RETCODE_PRICE_OFF: str="No quotes for request processing"; break;
      case TRADE_RETCODE_INVALID_EXPIRATION: str="Invalid order expiration date in the request"; break;
      case TRADE_RETCODE_ORDER_CHANGED: str="Order state has changed"; break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS: str="Too many requests"; break;
      case TRADE_RETCODE_NO_CHANGES: str="No changes in the request"; break;
      case TRADE_RETCODE_SERVER_DISABLES_AT: str="Autotrading is disabled by the server"; break;
      case TRADE_RETCODE_CLIENT_DISABLES_AT: str="Autotrading is disabled by the client terminal"; break;
      case TRADE_RETCODE_LOCKED: str="Request is blocked for processing"; break;
      case TRADE_RETCODE_FROZEN: str="Order or position has been frozen"; break;
      case TRADE_RETCODE_INVALID_FILL: str="Unsupported type of order execution for the balance is specified "; break;
      case TRADE_RETCODE_CONNECTION: str="No connection with trade server"; break;
      case TRADE_RETCODE_ONLY_REAL: str="Operation is allowed only for real accounts"; break;
      case TRADE_RETCODE_LIMIT_ORDERS: str="Limit for the number of pending orders has been reached"; break;
      case TRADE_RETCODE_LIMIT_VOLUME: str="Limit for orders and positions volume for this symbol has been reached"; break;
      default: str="Unknown result";
     }
//----
   return(str);
  }
//+------------------------------------------------------------------+
