//+------------------------------------------------------------------+
//|                                              TradeAlgorithms.mqh |
//|                               Copyright © 2011, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "2011,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
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
//| Buying                                                           |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after wich the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 int StopLoss,               // Stop loss in points
 int Takeprofit              // Take profit in points
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
//----
   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   double point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   double Ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
   if(!digit || !point || !Ask) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- Determine distance to Stop Loss (in price chart units)
   if(StopLoss)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,digit);
     }
   else request.sl=0.0;

//---- Determine distance to Take Profit (in price chart units)
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,digit);
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," open ============ >>>");
      //Print(comment);
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
//| Selling                                                          |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after wich the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 int StopLoss,               // Stop loss in points
 int Takeprofit              // Take profit in points
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
//----
   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   double point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   double Bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   if(!digit || !point || !Bid) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- Determine distance to Stop Loss (in price chart units)
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,digit);
     }
   else request.sl=0.0;

//---- Determine distance to Take Profit (in price chart units)
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,digit);
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," open ============ >>>");
      //Print(comment);
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
//| Buying                                                           |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after wich the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 double dStopLoss,           // Stop loss (in price chart units)
 double dTakeprofit          // Take profit (in price chart units)
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
//----
   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   double point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   double Ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
   if(!digit || !point || !Ask) return(false);

//---- correcting the distances for stop loss and take profit (in price chart units)
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," open ============ >>>");
      //Print(comment);
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
//| Selling                                                          |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // deal allowing flag
 const string symbol,        // deal trading pair
 const datetime &TimeLevel,  // the time, after wich the next deal will be performed after the current one
 double Money_Management,    // MM
 int Margin_Mode,            // lot size calculation method
 uint deviation,             // slippage
 double dStopLoss,           // Stop loss (in price chart units)
 double dTakeprofit          // Take profit (in price chart units)
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
//----
   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   double point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   double Bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   if(!digit || !point || !Bid) return(true);

//---- correcting the distances for stop loss and take profit (in price chart units)
   if(!dStopCorrect(symbol,dStopLoss,dTakeprofit,PosType)) return(false);
   int StopLoss=int((dStopLoss-Bid)/point);
//----
   double volume=SellLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): Incorrect volume for a trade request structure");
      return(false);
     }

//---- initializing structure of the MqlTradeRequest to open BUY position
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," open ============ >>>");
      //Print(comment);
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

//---- check, if there is a BUY position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_BUY) return(false);
     }
   else return(false);

//---- getting calculation data 
   double volume=PositionGetDouble(POSITION_VOLUME);
   double MaxLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX);
   double Bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   if(!volume || !MaxLot || !Bid) return(false);

//---- checking the lot for the maximum allowable value       
   if(volume>MaxLot) volume=MaxLot;

//---- initializing structure of the MqlTradeRequest to close BUY position
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
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Closing Buy position at ",symbol," ============ >>>");
   Print(comment);

//---- send order to close position to trade server
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," closed ============ >>>");
      //Print(comment);
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

//---- check, if there is a BUY position
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_SELL)return(false);
     }
   else return(false);

//---- getting calculation data 
   double volume=PositionGetDouble(POSITION_VOLUME);
   double MaxLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX);
   double Ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
   if(!volume || !MaxLot || !Ask) return(false);

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

//---- send order to close position to trade server
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," closed ============ >>>");
      //Print(comment);
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
 int Takeprofit              // Take profit in points
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
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,digit);
      if(request.sl<PositionGetDouble(POSITION_SL)) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Determine distance to Take Profit (in price chart units)
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy position at ",symbol," modified ============ >>>");
      //Print(comment);
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
 int Takeprofit              // Take profit in points
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
   int digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   double point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   double Bid=SymbolInfoDouble(symbol,SYMBOL_BID);
   if(!digit || !point || !Bid) return(true);

//---- initializing structure of the MqlTradeRequest to open BUY position
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- Determine distance to Stop Loss (in price chart units)
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,digit);
      double laststop=PositionGetDouble(POSITION_SL);
      if(request.sl>laststop && laststop) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- Determine distance to Take Profit (in price chart units)
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
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
      //StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell position at ",symbol," modified ============ >>>");
      //Print(comment);
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
      //---- Getting the name of a global 
      //---- variable for storing the time limit
      string G_Name_=GetTimeLevelName(symbol,trade_operation);

      //---- Checking for the time limit expiration for the previous deal 
      if(TimeCurrent()<GlobalVariableGet(G_Name_)) return(false);
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
//---- Getting the name of a global 
//---- variable for storing the time limit
   string G_Name_=GetTimeLevelName(symbol,trade_operation);
   GlobalVariableSet(GetTimeLevelName(symbol,trade_operation),TradeTimeLevel);
//----
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
 Margin_Mode external variable specifies the lot value calculation   | 
 method                                                              |
 0 - MM for an account free funds                                    |
 1 - MM for an account balance                                       |
 2 - MM for losses share from an account free funds                  |
 3 - MM for losses share from an account balance                     |
 by default - MM for an account free funds                           |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 if Money_Management is below zero,  trade function                  | 
 uses Money_Management absolute value rounded to the                 |
  nearest standard value as a lot size.                              |
*///                                                                 |
//+------------------------------------------------------------------+
double BuyLotCount_
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
            Print(__FUNCTION__,": Incorrect stop-loss!!!");
            STOPLOSS=0;
           }
         //---- 
         if(!StopCorrect(symbol,STOPLOSS))return(-1);
         double TICKVALUE=SymbolInfoDouble(symbol,SYMBOL_TRADE_TICK_VALUE);
         if(TICKVALUE==0.0) return(-1);
         //----
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=margin/(TICKVALUE*STOPLOSS);
         break;
        }

      //---- Lot calculation considering losses share from an account balance
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Incorrect stop-loss!!!");
            STOPLOSS=0;
           }
         //---- 
         if(!StopCorrect(symbol,STOPLOSS))return(-1);
         double TICKVALUE=SymbolInfoDouble(symbol,SYMBOL_TRADE_TICK_VALUE);
         if(TICKVALUE==0.0) return(-1);
         //---- 
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=margin/(TICKVALUE*STOPLOSS);
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

//---- normalizing the lot size to the nearest standard value 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_BUY)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| Lot size calculation for opening a short position                |  
//+------------------------------------------------------------------+
/*                                                                   |
 Margin_Mode external variable specifies the lot value calculation   | 
 method                                                              |
 0 - MM for an account free funds                                    |
 1 - MM for an account balance                                       |
 2 - MM for losses share from an account free funds                  |
 3 - MM for losses share from an account balance                     |
 by default - MM for an account free funds                           |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 if Money_Management is below zero,  trade function                  | 
 uses Money_Management absolute value rounded to the                 |
  nearest standard value as a lot size.                              |
*///                                                                 |
//+------------------------------------------------------------------+
double SellLotCount_
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
            Print(__FUNCTION__,": Incorrect stop-loss!!!");
            STOPLOSS=0;
           }
         //---- 
         if(!StopCorrect(symbol,STOPLOSS))return(-1);
         double TICKVALUE=SymbolInfoDouble(symbol,SYMBOL_TRADE_TICK_VALUE);
         if(TICKVALUE==0.0) return(-1);
         int SPREAD=int(SymbolInfoInteger(symbol,SYMBOL_SPREAD));
         if(SPREAD==0) return(-1);
         //----
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=margin/(TICKVALUE *(STOPLOSS+SPREAD));

         //---- normalizing the lot size to the nearest standard value 
         double LOTSTEP=SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP);
         if(LOTSTEP<=0) return(-1);
         Lot=LOTSTEP*MathFloor(Lot/LOTSTEP);
         break;
        }

      //---- Lot calculation considering losses share from an account balance
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": Incorrect stop-loss!!!");
            STOPLOSS=0;
           }
         //---- 
         if(!StopCorrect(symbol,STOPLOSS))return(-1);
         double TICKVALUE=SymbolInfoDouble(symbol,SYMBOL_TRADE_TICK_VALUE);
         if(TICKVALUE==0.0) return(-1);
         int SPREAD=int(SymbolInfoInteger(symbol,SYMBOL_SPREAD));
         if(SPREAD==0.0) return(-1);
         //---- 
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=margin/(TICKVALUE *(STOPLOSS+SPREAD));
        }

      //---- Lot calculation considering account free funds by default
      default:
        {
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
        }
     }
//---1+ 

//---- normalizing the lot size to the nearest standard value 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_SELL)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| Lot size calculation for opening a long position                 |   
//+------------------------------------------------------------------+
/*                                                                   |
 Margin_Mode external variable specifies the lot value calculation   | 
 method                                                              |
 0 - MM for an account free funds                                    |
 1 - MM for an account balance                                       |
 2 - MM for losses share from an account free funds                  |
 3 - MM for losses share from an account balance                     |
 4 - minimum lot between 0 and 2                                     |
 5 - minimum lot between 1 and 3                                     |
 by default - MM for an account free funds                           |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 if Money_Management is below zero,  trade function                  | 
 uses Money_Management absolute value rounded to the                 |
  nearest standard value as a lot size.                              |
*///                                                                 |
//+------------------------------------------------------------------+                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    //+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
double BuyLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 int Slippage_
 )
// BuyLotCount(string symbol, double Money_Management, int Margin_Mode, int STOPLOSS)
  {
//----
   if(Money_Management<0) return(BuyLotCount_(symbol,Money_Management,0,0,0));
//----
   double LotA,LotB;
   int MarModeA,MarModeB;
//----
   switch(Margin_Mode)
     {
      case 4:
         MarModeA = 0;
         MarModeB = 2;
         break;
         //----
      case 5:
         MarModeA = 1;
         MarModeB = 3;
         break;
         //----                                                                 
      default: return(BuyLotCount_(symbol,Money_Management,Margin_Mode,STOPLOSS,Slippage_));
     }

   LotA=BuyLotCount_(symbol,Money_Management,MarModeA,STOPLOSS,Slippage_);
   if(LotA==-1) return(-1);
//----          
   LotB=BuyLotCount_(symbol,Money_Management,MarModeB,STOPLOSS,Slippage_);
   if(LotB==-1) return(-1);
//----              
   if(LotA<LotB)
      return(LotA);
   else return(LotB);
//----
  }
//+------------------------------------------------------------------+
//| Lot size calculation for opening a short position                |  
//+------------------------------------------------------------------+   
/*                                                                   |
 Margin_Mode external variable specifies the lot value calculation   | 
 method                                                              |
 0 - MM for an account free funds                                    |
 1 - MM for an account balance                                       |
 2 - MM for losses share from an account free funds                  |
 3 - MM for losses share from an account balance                     |
 4 - minimum lot between 0 and 2                                     |
 5 - minimum lot between 1 and 3                                     |
 by default - MM for an account free funds                           |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 if Money_Management is below zero,  trade function                  | 
 uses Money_Management absolute value rounded to the                 |
  nearest standard value as a lot size.                              |
*///                                                                 |
//+------------------------------------------------------------------+                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    //+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
double SellLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 int Slippage_
 )
// (string symbol, double Money_Management, int Margin_Mode, int STOPLOSS)
  {
//----
   if(Money_Management<0) return(SellLotCount_(symbol,Money_Management,0,0,0));
//----
   double LotA,LotB;
   int MarModeA,MarModeB;
//----
   switch(Margin_Mode)
     {
      case 4:
         MarModeA = 0;
         MarModeB = 2;
         break;
         //----
      case 5:
         MarModeA = 1;
         MarModeB = 3;
         break;
         //----                                                                 
      default: return(SellLotCount_(symbol,Money_Management,Margin_Mode,STOPLOSS,Slippage_));
     }

   LotA=SellLotCount_(symbol,Money_Management,MarModeA,STOPLOSS,Slippage_);
   if(LotA==-1) return(-1);
//----          
   LotB=SellLotCount_(symbol,Money_Management,MarModeB,STOPLOSS,Slippage_);
   if(LotB==-1) return(-1);
//----              
   if(LotA<LotB)
      return(LotA);
   else return(LotB);
//----
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

   int Stop,digit;
   double point,dStop,ExtrStop,ExtrTake;

//---- getting the minimum distance to a pending order 
   Stop=0;
   if(!StopCorrect(symbol,Stop))return(false);

//----
   digit=int(SymbolInfoInteger(symbol,SYMBOL_DIGITS));
   point=SymbolInfoDouble(symbol,SYMBOL_POINT);
   if(!digit || !point) return(true);
   dStop=Stop*point;

//---- correction of a pending order size for a long position
   if(trade_operation==POSITION_TYPE_BUY)
     {
      double Ask=SymbolInfoDouble(symbol,SYMBOL_ASK);
      if(!Ask) return(false);

      ExtrStop=NormalizeDouble(Ask-dStop,digit);
      ExtrTake=NormalizeDouble(Ask+dStop,digit);

      if(dStopLoss>ExtrStop) dStopLoss=ExtrStop;
      if(dTakeprofit<ExtrTake) dTakeprofit=ExtrTake;
     }

//---- correction of a pending order size for a short position
   if(trade_operation==POSITION_TYPE_SELL)
     {
      double Bid=SymbolInfoDouble(symbol,SYMBOL_BID);
      if(!Bid) return(false);

      ExtrStop=NormalizeDouble(Bid+dStop,digit);
      ExtrTake=NormalizeDouble(Bid-dStop,digit);

      if(dStopLoss<ExtrStop) dStopLoss=ExtrStop;
      if(dTakeprofit>ExtrTake) dTakeprofit=ExtrTake;
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
   double Step=SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP);
   double MaxLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX);
   double MinLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN);
   if(!Step || !MaxLot || !MinLot) return(false);

//---- normalizing the lot size to the nearest standard value 
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
//| Limitation of a lot size by a deposit capacity                   |
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
   double Step=SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP);
   double MaxLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX);
   double MinLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN);
   if(!Step || !MaxLot || !MinLot) return(false);

   double ExtremLot=GetLotForOpeningPos(symbol,trade_operation,freemargin);
//---- normalizing the lot size to the nearest standard value 
   ExtremLot=Step*MathFloor(ExtremLot/Step);

   if(ExtremLot<MinLot) return(false); // funds are insufficient even for a minimum lot!
   if(Lot>ExtremLot) Lot=ExtremLot; // cutting the lot size down to the deposit capacity!
   if(Lot>MaxLot) Lot=MaxLot; // cutting the lot size down to the maximum permissible one
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| Lot size calculation for opening a position with lot_margin      |
//+------------------------------------------------------------------+
double GetLotForOpeningPos(string symbol,ENUM_POSITION_TYPE direction,double lot_margin)
  {
//----
   double price,n_margin;
   if(direction==POSITION_TYPE_BUY)  price=SymbolInfoDouble(symbol,SYMBOL_ASK);
   if(direction==POSITION_TYPE_SELL) price=SymbolInfoDouble(symbol,SYMBOL_BID);
   if(!price) return(NULL);
   
   if(!OrderCalcMargin(ENUM_ORDER_TYPE(direction),symbol,1,price,n_margin) || !n_margin) return(0);
   double lot=lot_margin/n_margin;

//---- get trade constants
   double LOTSTEP=SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP);
   double MaxLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX);
   double MinLot=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN);
   if(!LOTSTEP || !MaxLot || !MinLot) return(0);
   
//---- normalizing the lot size to the nearest standard value 
   lot=LOTSTEP*MathFloor(lot/LOTSTEP);

//---- checking the lot for the minimum allowable value
   if(lot<MinLot) lot=0;
//---- checking the lot for the maximum allowable value       
   if(lot>MaxLot) lot=MaxLot;
//----
   return(lot);
  }
//+------------------------------------------------------------------+
//| Lot size calculation for opening a position with lot_margin      |
//+------------------------------------------------------------------+
double GetLotForOpeningPos_(string symbol,ENUM_POSITION_TYPE direction,double lot_margin)
  {
//----
   double lot=0;
   string ZERO="";

//---- getting a leverage size
   long leverage=AccountInfoInteger(ACCOUNT_LEVERAGE);
   if(!leverage) return(0);

//---- getting a contract size
   double lot_size=SymbolInfoDouble(symbol,SYMBOL_TRADE_CONTRACT_SIZE);
   if(!lot_size) return(0);

//---- getting account currency
   string account_currency;
   if(!SymbolInfoString(symbol,SYMBOL_CURRENCY_MARGIN,account_currency)) return(0);

//---- margin currency   
   string margin_currency;
   if(!SymbolInfoString(symbol,SYMBOL_CURRENCY_MARGIN,margin_currency)) return(0);

//---- profit currency
   string profit_currency;
   if(!SymbolInfoString(symbol,SYMBOL_CURRENCY_MARGIN,profit_currency)) return(0);

//---- calculation currency
   string calc_currency="";
//---- reverse quote - true, direct quote - false
   bool mode=false;

//---- if profit currency and account currency are equal
   if(profit_currency==account_currency)
     {
      calc_currency=symbol;
      mode=true;
     }

//---- if margin currency and account currency are equal
   if(margin_currency==account_currency)
     {
      calc_currency=symbol;
      //---- just return the contract value, multiplied by the number of lots
      return(lot_margin*leverage/lot_size);
     }

//---- if calculation currency is still not determined, then we have cross currency
   if(calc_currency=="")
     {
      calc_currency=GetSymbolByCurrencies(margin_currency,account_currency);
      mode=true;
      //---- if obtained value is equal to NULL, then this symbol is not found
      if(calc_currency==NULL)
        {
         //---- let's try to do it in the opposite way
         calc_currency=GetSymbolByCurrencies(account_currency,margin_currency);
         mode=false;
        }
     }

//---- if calculation currency is still not found 
   if(calc_currency=="" || calc_currency==NULL)
     {
      Print(__FUNCTION__,": Can't find calculation currency for symbol combination "+symbol);
      return(0);
     }

//---- we know calculation currency, let's get its last prices
   MqlTick tick;
   SymbolInfoTick(calc_currency,tick);

//---- now we have everything for calculation 
   double calc_price;
//---- calculate for buying
   if(direction==POSITION_TYPE_BUY)
     {
      //---- reverse quote
      if(mode)
        {
         //---- calculate using Buy price for reverse quote
         calc_price=tick.ask;
         lot=lot_margin*leverage/(lot_size*calc_price);
        }
      //---- direct quote 
      else
        {
         //---- calculate using Sell price for direct quote
         calc_price=tick.bid;
         lot=lot_margin*leverage*calc_price/lot_size;
        }
     }

//---- calculate for selling
   if(direction==POSITION_TYPE_SELL)
     {
      //---- reverse quote
      if(mode)
        {
         //---- calculate using Sell price for reverse quote
         calc_price=tick.bid;
         lot=lot_margin*leverage/(lot_size*calc_price);
        }
      //---- direct quote 
      else
        {
         //---- calculate using Buy price for direct quote
         calc_price=tick.ask;
         lot=lot_margin*leverage*calc_price/lot_size;
        }
     }
//---- return result - amount of equity in account currency, required to open position in specified volume
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
//|  History verification for uploading                              |
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
