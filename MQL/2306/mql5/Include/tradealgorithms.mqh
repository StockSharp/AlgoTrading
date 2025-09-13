//+------------------------------------------------------------------+
//|                                              TradeAlgorithms.mqh |
//|                               Copyright © 2013, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//| Trading algorithms for brokers who offer non zero spread!        |
//+------------------------------------------------------------------+ 
#property copyright "2013,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.20"
//+------------------------------------------------------------------+
//|  Calculated lots variants enumeration                            |
//+------------------------------------------------------------------+
enum MarginMode // Type of constant for Margin_Mode trading functions
  {
   FREEMARGIN=0,     //MM considering account free funds
   BALANCE,          //MM considering account balance
   LOSSFREEMARGIN,   //MM for losses share from an account free funds
   LOSSBALANCE,      //MM for losses share from an account balance
   LOT               //Lot should be unchanged
  };
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
//+==================================================================+
//| Trading operations algorithms                                    |
//+==================================================================+

//+------------------------------------------------------------------+
//| Open a long position                                             |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after which the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 int StopLoss,               // Stop loss in points
 int Takeprofit              // Take Profit in points
 )
//BuyPositionOpen(BUY_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!BUY_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;
//---- Checking for the time limit expiration for the previous deal and volume completeness
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Checking, if there is an open position
   if(PositionSelect(symbol)) return(true);

//----
   double volume=BuyLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Incorrect volume for a trade request structure");
      return(false);
     }

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----   
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- Determine distance to Stop Loss in price chart units
   if(StopLoss)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,int(digit));
     }
   else request.sl=0.0;

//---- Determine distance to Take Profit in price chart units
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,int(digit));
     }
   else request.tp=0.0;

//----
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
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Opening Buy position at ",symbol," ============ >>>");
   Print(comment);

//---- open BUY position and check the result of trade request
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      BUY_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," opened ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Open a short position                                            |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after which the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 int StopLoss,               // Stop loss in points
 int Takeprofit              // Take Profit in points
 )
//SellPositionOpen(SELL_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!SELL_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;
//---- Checking for the time limit expiration for the previous deal and volume completeness
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Checking, if there is an open position
   if(PositionSelect(symbol)) return(true);

//----
   double volume=SellLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Incorrect volume for a trade request structure");
      return(false);
     }

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- Initializing structure of the MqlTradeRequest to open SELL position
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- Determine distance to Stop Loss in price chart units
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,int(digit));
     }
   else request.sl=0.0;

//---- Determine distance to Take Profit in price chart units
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,int(digit));
     }
   else request.tp=0.0;
//----
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
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Open Sell position at ",symbol," ============ >>>");
   Print(comment);

//---- open SELL position and check the result of trade request
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      SELL_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," opened ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Open a long position                                             |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after which the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 double dStopLoss,           // Stop loss (in price chart units)
 double dTakeprofit          // Take Profit in price chart units
 )
//BuyPositionOpen(BUY_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!BUY_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;
//---- Checking for the time limit expiration for the previous deal and volume completeness
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Checking, if there is an open position
   if(PositionSelect(symbol)) return(true);

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- correcting the distances for Stop Loss and Take Profit (in price chart units)
   if(!dStopCorrect(symbol,dStopLoss,dTakeprofit,PosType)) return(false);
   int StopLoss=int((Ask-dStopLoss)/point);
//----
   double volume=BuyLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Incorrect volume for a trade request structure");
      return(false);
     }

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl=dStopLoss;
   request.tp=dTakeprofit;
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
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Opening Buy position at ",symbol," ============ >>>");
   Print(comment);

//---- open BUY position and check the result of trade request
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      BUY_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," opened ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Open a short position                                            |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after which the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 double dStopLoss,           // Stop loss (in price chart units)
 double dTakeprofit          // Take Profit in price chart units
 )
//SellPositionOpen(SELL_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!SELL_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;
//---- Checking for the time limit expiration for the previous deal and volume completeness
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- Checking, if there is an open position
   if(PositionSelect(symbol)) return(true);

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- correcting the distances for Stop Loss and Take Profit (in price chart units)
   if(!dStopCorrect(symbol,dStopLoss,dTakeprofit,PosType)) return(false);
   int StopLoss=int((dStopLoss-Bid)/point);
//----
   double volume=SellLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Incorrect volume for a trade request structure");
      return(false);
     }

//---- Initializing structure of the MqlTradeRequest to open SELL position
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Checking correctness of a trade request
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): OrderCheck(): Incorrect data for a trade request structure!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Open Sell position at ",symbol," ============ >>>");
   Print(comment);

//---- open SELL position and check the result of trade request
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): OrderSend(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      SELL_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," opened ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): OrderSend(): Unable to perform a deal!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Closing a long position                                          |
//+------------------------------------------------------------------+
bool BuyPositionClose
(
 bool &Signal,         // deal allowing flag
 const string symbol,  // deal trading pair
 uint deviation        // slippage
 )
  {
//----
   if(!Signal) return(true);

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

//---- Check, if there is a BUY open position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_BUY) return(false);
     }
   else return(false);

   double MaxLot,volume,Bid;
//---- getting calculation data    
   if(!PositionGetDouble(POSITION_VOLUME,volume)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- checking the lot for the maximum allowable value       
   if(volume>MaxLot) volume=MaxLot;

//---- Initializing structure of the MqlTradeRequest to close BUY position
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Checking correctness of a trade request
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Incorrect data for a trade request structure!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }
//----     
   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Close Buy position at ",symbol," ============ >>>");
   Print(comment);

//---- Send order to close position to trade server
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Unable to close the position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," closed ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): Unable to close the position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Closing a short position                                         |
//+------------------------------------------------------------------+
bool SellPositionClose
(
 bool &Signal,         // deal allowing flag
 const string symbol,  // deal trading pair
 uint deviation        // slippage
 )
  {
//----
   if(!Signal) return(true);

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;
//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

//---- Check, if there is a BUY open position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_SELL)return(false);
     }
   else return(false);

   double MaxLot,volume,Ask;
//---- getting calculation data    
   if(!PositionGetDouble(POSITION_VOLUME,volume)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- checking the lot for the maximum allowable value       
   if(volume>MaxLot) volume=MaxLot;

//---- initializing structure of the MqlTradeRequest to close SELL position
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- Checking correctness of a trade request
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): Incorrect data for a trade request structure!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }
//----    
   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Close Sell position at ",symbol," ============ >>>");
   Print(comment);

//---- Send order to close position to trade server
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): Unable to close the position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Signal=false;
     }
   else
     {
      Print(__FUNCTION__,"(): Unable to close the position!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," closed ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Modifying a long position                                        |
//+------------------------------------------------------------------+
bool BuyPositionModify
(
 bool &Modify_Signal,        // modification allowing flag
 const string symbol,        // deal trading pair
 uint deviation,             // slippage
 int StopLoss,               // Stop loss in points
 int Takeprofit              // Take Profit in points
 )
//BuyPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;

//---- Checking, if there is an open position
   if(!PositionSelect(symbol)) return(true);

//---- Declare structures of trade request and result of trade request
   MqlTradeRequest request;
   MqlTradeResult result;

//---- Declaration of the structure of a trade request checking result 
   MqlTradeCheckResult check;

//---- nulling the structures
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- Determine distance to Stop Loss in price chart units
   if(StopLoss)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,int(digit));
      if(request.sl<PositionGetDouble(POSITION_SL)) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Determine distance to Take Profit in price chart units
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,int(digit));
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
bool SellPositionModify
(
 bool &Modify_Signal,        // modification allowing flag
 const string symbol,        // deal trading pair
 uint deviation,             // slippage
 int StopLoss,               // Stop loss in points
 int Takeprofit              // Take Profit in points
 )
//SellPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;

//---- Checking, if there is an open position
   if(!PositionSelect(symbol)) return(true);

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
   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- Determine distance to Stop Loss in price chart units
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,int(digit));
      double laststop=PositionGetDouble(POSITION_SL);
      if(request.sl>laststop && laststop) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Determine distance to Take Profit in price chart units
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,int(digit));
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," modified ============ >>>");
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
//| GetTimeLevelName() function                                      |
//+------------------------------------------------------------------+
string GetTimeLevelName(string symbol,ENUM_POSITION_TYPE trade_operation)
  {
//----
   string G_Name_;
//----  
   if(MQL5InfoInteger(MQL5_TESTING)
      || MQL5InfoInteger(MQL5_OPTIMIZATION)
      || MQL5InfoInteger(MQL5_DEBUGGING))
      StringConcatenate(G_Name_,"TimeLevel_",AccountInfoInteger(ACCOUNT_LOGIN),"_",symbol,"_",trade_operation,"_Test_");
   else StringConcatenate(G_Name_,"TimeLevel_",AccountInfoInteger(ACCOUNT_LOGIN),"_",symbol,"_",trade_operation);
//----
   return(G_Name_);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelCheck() function                                   |
//+------------------------------------------------------------------+
bool TradeTimeLevelCheck
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation,
 datetime TradeTimeLevel
 )
  {
//----
   if(TradeTimeLevel>0)
     {
      //---- Checking for the time limit expiration for the previous deal 
      if(TimeCurrent()<GlobalVariableGet(GetTimeLevelName(symbol,trade_operation))) return(false);
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelSet() function                                     |
//+------------------------------------------------------------------+
void TradeTimeLevelSet
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation,
 datetime TradeTimeLevel
 )
  {
//----
   GlobalVariableSet(GetTimeLevelName(symbol,trade_operation),TradeTimeLevel);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelSet() function                                     |
//+------------------------------------------------------------------+
datetime TradeTimeLevelGet
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation
 )
  {
//----
   return(datetime(GlobalVariableGet(GetTimeLevelName(symbol,trade_operation))));
  }
//+------------------------------------------------------------------+
//| TimeLevelGlobalVariableDel() function                            |
//+------------------------------------------------------------------+
void TimeLevelGlobalVariableDel
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation
 )
  {
//----
   if(MQL5InfoInteger(MQL5_TESTING)
      || MQL5InfoInteger(MQL5_OPTIMIZATION)
      || MQL5InfoInteger(MQL5_DEBUGGING))
      GlobalVariableDel(GetTimeLevelName(symbol,trade_operation));
//----
  }
//+------------------------------------------------------------------+
//| GlobalVariableDel_() function                                    |
//+------------------------------------------------------------------+
void GlobalVariableDel_(string symbol)
  {
//----
   TimeLevelGlobalVariableDel(symbol,POSITION_TYPE_BUY);
   TimeLevelGlobalVariableDel(symbol,POSITION_TYPE_SELL);
//----
  }
//+------------------------------------------------------------------+
//| Lot size calculation for opening a long position                 |  
//+------------------------------------------------------------------+
/*                                                                   |
 The Margin_Mode external variable determines the lot size           | 
 calculation                                                         |
 0 - MM for an account free funds                                    |
 1 - MM for an account balance                                       |
 2 - MM for losses share from an account free funds                  |
 3 - MM for losses share from an account balance                     |
 by default - MM for an account free funds                           |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 if Money_Management is below zero,  trade function                  | 
 uses Money_Management absolute value rounded to the                 |
 as the lot size.                                                    |
*///                                                                 |
//+------------------------------------------------------------------+
double BuyLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 uint Slippage_
 )
// BuyLotCount_(string symbol, double Money_Management, int Margin_Mode, int STOPLOSS,Slippage_)
  {
//----
   double margin,Lot;

//---1+ LOT SIZE CALCULATION FOR OPENING A POSITION
   if(Money_Management<0) Lot=MathAbs(Money_Management);
   else
   switch(Margin_Mode)
     {
      //---- Lot calculation considering account free funds
      case  0:
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
         break;

         //---- Lot calculation considering account balance
      case  1:
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
         break;

         //---- Lot calculation considering losses share from an account free funds             
      case  2:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Incorrect Stop Loss!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price_open)) return(-1);

         //---- Determine distance to Stop Loss in price chart units
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open-STOPLOSS*point,int(digit));

         double profit;
         OrderCalcProfit(ORDER_TYPE_BUY,symbol,1,price_open,price_close,profit);
         if(!profit) return(-1);

         //---- Losses calculation considering account free funds
         double Loss=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- Lot calculation considering losses share from an account balance
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Incorrect Stop Loss!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price_open)) return(-1);

         //---- Determine distance to Stop Loss in price chart units
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open-STOPLOSS*point,int(digit));

         double profit;
         OrderCalcProfit(ORDER_TYPE_BUY,symbol,1,price_open,price_close,profit);
         if(!profit) return(-1);

         //---- Losses calculation considering account balance
         double Loss=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //Lot calculation should be unchanged
      case  4:
        {
         Lot=MathAbs(Money_Management);
         break;
        }

      //---- Lot calculation considering account free funds by default
      default:
        {
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
        }
     }
//---1+    

//---- lot size normalization to the nearest standard value 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_BUY)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| Lot size calculation for opening a short position                |  
//+------------------------------------------------------------------+
/*                                                                   |
 The Margin_Mode external variable determines the lot size           | 
 calculation                                                         |
 0 - MM for an account free funds                                    |
 1 - MM for an account balance                                       |
 2 - MM for losses share from an account free funds                  |
 3 - MM for losses share from an account balance                     |
 by default - MM for an account free funds                           |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 if Money_Management is below zero,  trade function                  | 
 uses Money_Management absolute value rounded to the                 |
 as the lot size.                                                    |
*///                                                                 |
//+------------------------------------------------------------------+
double SellLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 uint Slippage_
 )
// (string symbol, double Money_Management, int Margin_Mode, int STOPLOSS)
  {
//----
   double margin,Lot;

//---1+ LOT SIZE CALCULATION FOR OPENING A POSITION
   if(Money_Management<0) Lot=MathAbs(Money_Management);
   else
   switch(Margin_Mode)
     {
      //---- Lot calculation considering account free funds
      case  0:
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
         break;

         //---- Lot calculation considering account balance
      case  1:
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
         break;

         //---- Lot calculation considering losses share from an account free funds             
      case  2:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Incorrect Stop Loss!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_BID,price_open)) return(-1);

         //---- Determine distance to Stop Loss in price chart units
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open+
                                            STOPLOSS*point,int(digit));

         double profit;
         OrderCalcProfit(ORDER_TYPE_SELL,symbol,1,price_open,price_close,profit);
         if(!profit) return(-1);

         //---- Losses calculation considering account free funds
         double Loss=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- Lot calculation considering losses share from an account balance
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Incorrect Stop Loss!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_BID,price_open)) return(-1);

         //---- Determine distance to Stop Loss in price chart units
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open+STOPLOSS*point,int(digit));

         double profit;
         OrderCalcProfit(ORDER_TYPE_SELL,symbol,1,price_open,price_close,profit);
         if(!profit) return(-1);

         //---- Losses calculation considering account balance
         double Loss=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //Lot calculation should be unchanged
      case  4:
        {
         Lot=MathAbs(Money_Management);
         break;
        }

      //---- Lot calculation considering account free funds by default
      default:
        {
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
        }
     }
//---1+ 

//---- lot size normalization to the nearest standard value 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_SELL)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| correction of a pending order size to an acceptable value        |
//+------------------------------------------------------------------+
bool StopCorrect(string symbol,int &Stop)
  {
//----
   long Extrem_Stop;
   if(!SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL,Extrem_Stop)) return(false);
   if(Stop<Extrem_Stop) Stop=int(Extrem_Stop);
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| correction of a pending order size to an acceptable value        |
//+------------------------------------------------------------------+
bool dStopCorrect
(
 string symbol,
 double &dStopLoss,
 double &dTakeprofit,
 ENUM_POSITION_TYPE trade_operation
 )
// dStopCorrect(symbol,dStopLoss,dTakeprofit,trade_operation)
  {
//----
   if(!dStopLoss && !dTakeprofit) return(true);

   if(dStopLoss<0)
     {
      Print(__FUNCTION__,"(): Stop loss negative value!");
      return(false);
     }

   if(dTakeprofit<0)
     {
      Print(__FUNCTION__,"(): Take profit negative value!");
      return(false);
     }
//---- 
   int Stop;
   long digit;
   double point,dStop,ExtrStop,ExtrTake;

//---- getting the minimum distance to a pending order 
   Stop=0;
   if(!StopCorrect(symbol,Stop))return(false);
//----   
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(false);
   dStop=Stop*point;

//---- correction of a pending order size for a long position
   if(trade_operation==POSITION_TYPE_BUY)
     {
      double Ask;
      if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(false);

      ExtrStop=NormalizeDouble(Ask-dStop,int(digit));
      ExtrTake=NormalizeDouble(Ask+dStop,int(digit));

      if(dStopLoss>ExtrStop && dStopLoss) dStopLoss=ExtrStop;
      if(dTakeprofit<ExtrTake && dTakeprofit) dTakeprofit=ExtrTake;
     }

//---- correction of a pending order size for a short position
   if(trade_operation==POSITION_TYPE_SELL)
     {
      double Bid;
      if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(false);

      ExtrStop=NormalizeDouble(Bid+dStop,int(digit));
      ExtrTake=NormalizeDouble(Bid-dStop,int(digit));

      if(dStopLoss<ExtrStop && dStopLoss) dStopLoss=ExtrStop;
      if(dTakeprofit>ExtrTake && dTakeprofit) dTakeprofit=ExtrTake;
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Correction of a lot size to the nearest acceptable value         |
//+------------------------------------------------------------------+
bool LotCorrect
(
 string symbol,
 double &Lot,
 ENUM_POSITION_TYPE trade_operation
 )
//LotCorrect(string symbol, double& Lot, ENUM_POSITION_TYPE trade_operation)
  {
//---- getting calculation data   
   double Step,MaxLot,MinLot;   
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,Step)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(false);

//---- lot size normalization to the nearest standard value 
   Lot=Step*MathFloor(Lot/Step);

//---- checking the lot for the minimum allowable value
   if(Lot<MinLot) Lot=MinLot;
//---- checking the lot for the maximum allowable value       
   if(Lot>MaxLot) Lot=MaxLot;

//---- checking the funds sufficiency
   if(!LotFreeMarginCorrect(symbol,Lot,trade_operation))return(false);
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| limitation of a lot size by a deposit capacity                   |
//+------------------------------------------------------------------+
bool LotFreeMarginCorrect
(
 string symbol,
 double &Lot,
 ENUM_POSITION_TYPE trade_operation
 )
//(string symbol, double& Lot, ENUM_POSITION_TYPE trade_operation)
  {
//---- checking the funds sufficiency
   double freemargin=AccountInfoDouble(ACCOUNT_FREEMARGIN);
   if(freemargin<=0) return(false);

//---- getting calculation data   
   double Step,MaxLot,MinLot;   
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,Step)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(false);

   double ExtremLot=GetLotForOpeningPos(symbol,trade_operation,freemargin);
//---- lot size normalization to the nearest standard value 
   ExtremLot=Step*MathFloor(ExtremLot/Step);

   if(ExtremLot<MinLot) return(false); // funds are insufficient even for a minimum lot!
   if(Lot>ExtremLot) Lot=ExtremLot; // cutting the lot size down to the deposit capacity!
   if(Lot>MaxLot) Lot=MaxLot; // cutting the lot size down to the maximum permissible one
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| lot size calculation for opening a position with lot_margin      |
//+------------------------------------------------------------------+
double GetLotForOpeningPos(string symbol,ENUM_POSITION_TYPE direction,double lot_margin)
  {
//----
   double price=0.0,n_margin;
   if(direction==POSITION_TYPE_BUY)  if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price)) return(0);
   if(direction==POSITION_TYPE_SELL) if(!SymbolInfoDouble(symbol,SYMBOL_BID,price)) return(0);
   if(!price) return(NULL);

   if(!OrderCalcMargin(ENUM_ORDER_TYPE(direction),symbol,1,price,n_margin) || !n_margin) return(0);
   double lot=lot_margin/n_margin;

//---- getting trade constants
   double LOTSTEP,MaxLot,MinLot;   
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,LOTSTEP)) return(0);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(0);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(0);

//---- lot size normalization to the nearest standard value 
   lot=LOTSTEP*MathFloor(lot/LOTSTEP);

//---- checking the lot for the minimum allowable value
   if(lot<MinLot) lot=0;
//---- checking the lot for the maximum allowable value       
   if(lot>MaxLot) lot=MaxLot;
//----
   return(lot);
  }
//+------------------------------------------------------------------+
//| Return symbol with specified margin currency and profit currency |
//+------------------------------------------------------------------+
string GetSymbolByCurrencies(string margin_currency,string profit_currency)
  {
//---- in loop process all symbols, that are shown in Market Watch window
   int total=SymbolsTotal(true);
   for(int numb=0; numb<total; numb++)
     {
      //---- get symbol name by number in Market Watch window
      string symbolname=SymbolName(numb,true);

      //---- get margin currency
      string m_cur=SymbolInfoString(symbolname,SYMBOL_CURRENCY_MARGIN);

      //---- get profit currency (profit on price change)
      string p_cur=SymbolInfoString(symbolname,SYMBOL_CURRENCY_PROFIT);

      //---- if symbol matches both currencies, return symbol name
      if(m_cur==margin_currency && p_cur==profit_currency) return(symbolname);
     }
//----    
   return(NULL);
  }
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
      case TRADE_RETCODE_REJECT: str="Request is rejected"; break;
      case TRADE_RETCODE_CANCEL: str="Request is canceled by trader"; break;
      case TRADE_RETCODE_PLACED: str="Order is placed"; break;
      case TRADE_RETCODE_DONE: str="Request executed"; break;
      case TRADE_RETCODE_DONE_PARTIAL: str="Request is executed partially"; break;
      case TRADE_RETCODE_ERROR: str="Request processing error"; break;
      case TRADE_RETCODE_TIMEOUT: str="Request timed out";break;
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
      case TRADE_RETCODE_LIMIT_ORDERS: str="Pending orders have reached the limit"; break;
      case TRADE_RETCODE_LIMIT_VOLUME: str="Volume of orders and positions for this symbol has reached the limit"; break;
      default: str="Unknown result";
     }
//----
   return(str);
  }
//+------------------------------------------------------------------+
//|                                                HistoryLoader.mqh |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Loading history for a multi-currency Expert Advisor              |
//+------------------------------------------------------------------+
int LoadHistory(datetime StartDate,           // start data for history uploading
                string LoadedSymbol,          // the symbol of requested historical data
                ENUM_TIMEFRAMES LoadedPeriod) // timeframe of requested historical data
  {
//----+ 
//Print(__FUNCTION__, ": Start load ", LoadedSymbol+ " , " + EnumToString(LoadedPeriod) + " from ", StartDate);
   int res=CheckLoadHistory(LoadedSymbol,LoadedPeriod,StartDate);
   switch(res)
     {
      case -1 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Unknown symbol ", LoadedSymbol);               break;
      case -2 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Requested bars more than max bars in chart "); break;
      case -3 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Program was stopped ");                        break;
      case -4 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Indicator shouldn't load its own data ");      break;
      case -5 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Load failed ");                                break;
      case  0 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded OK ");  */                           break;
      case  1 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded previously ");  */                   break;
      case  2 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded previously and built ");  */         break;
      default : { /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Unknown result "); */}
     }
/* 
   if (res > 0)
    {   
     bars = Bars(LoadedSymbol, LoadedPeriod);
     Print(__FUNCTION__, "(", LoadedSymbol, " ", GetPeriodName(LoadedPeriod), "): First date ", first_date, " - ", bars, " bars");
    }
   */
//----+
   return(res);
  }
//+------------------------------------------------------------------+
//|  history verification for uploading                              |
//+------------------------------------------------------------------+
int CheckLoadHistory(string symbol,ENUM_TIMEFRAMES period,datetime start_date)
  {
//----+
   datetime first_date=0;
   datetime times[100];
//--- check symbol & period
   if(symbol == NULL || symbol == "") symbol = Symbol();
   if(period == PERIOD_CURRENT)     period = Period();
//--- check if symbol is selected in the MarketWatch
   if(!SymbolInfoInteger(symbol,SYMBOL_SELECT))
     {
      if(GetLastError()==ERR_MARKET_UNKNOWN_SYMBOL) return(-1);
      if(!SymbolSelect(symbol,true)) Print(__FUNCTION__,"(): Failed to add a symbol ",symbol," to the MarketWatch window!!!");
     }
//--- check if data is present
   SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date);
   if(first_date>0 && first_date<=start_date) return(1);
//--- don't ask for load of its own data if it is an indicator
   if(MQL5InfoInteger(MQL5_PROGRAM_TYPE)==PROGRAM_INDICATOR && Period()==period && Symbol()==symbol)
      return(-4);
//--- second attempt
   if(SeriesInfoInteger(symbol,PERIOD_M1,SERIES_TERMINAL_FIRSTDATE,first_date))
     {
      //--- there is loaded data to build timeseries
      if(first_date>0)
        {
         //--- force timeseries build
         CopyTime(symbol,period,first_date+PeriodSeconds(period),1,times);
         //--- check date
         if(SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date))
            if(first_date>0 && first_date<=start_date) return(2);
        }
     }
//--- max bars in chart from terminal options
   int max_bars=TerminalInfoInteger(TERMINAL_MAXBARS);
//--- load symbol history info
   datetime first_server_date=0;
   while(!SeriesInfoInteger(symbol,PERIOD_M1,SERIES_SERVER_FIRSTDATE,first_server_date) && !IsStopped())
      Sleep(5);
//--- fix start date for loading
   if(first_server_date>start_date) start_date=first_server_date;
   if(first_date>0 && first_date<first_server_date)
      Print(__FUNCTION__,"(): Warning: first server date ",first_server_date," for ",symbol,
            " does not match to first series date ",first_date);
//--- load data step by step
   int fail_cnt=0;
   while(!IsStopped())
     {
      //--- wait for timeseries build
      while(!SeriesInfoInteger(symbol,period,SERIES_SYNCHRONIZED) && !IsStopped())
         Sleep(5);
      //--- ask for built bars
      int bars=Bars(symbol,period);
      if(bars>0)
        {
         if(bars>=max_bars) return(-2);
         //--- ask for first date
         if(SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date))
            if(first_date>0 && first_date<=start_date) return(0);
        }
      //--- copying of next part forces data loading
      int copied=CopyTime(symbol,period,bars,100,times);
      if(copied>0)
        {
         //--- check for data
         if(times[0]<=start_date) return(0);
         if(bars+copied>=max_bars) return(-2);
         fail_cnt=0;
        }
      else
        {
         //--- no more than 100 failed attempts
         fail_cnt++;
         if(fail_cnt>=100) return(-5);
         Sleep(10);
        }
     }
//----+ stopped
   return(-3);
  }
//+------------------------------------------------------------------+
