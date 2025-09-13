// The Stops Bot
// Patrick Burns

#include <WinUser32.mqh>
#include <stdlib.mqh>

// defines for managing trade orders
#define RETRYCOUNT    10
#define RETRYDELAY    500

#define LONG          1
#define SHORT         0
#define ALL           2

extern bool   BuyAllow = true;
extern bool   SellAllow = true;
extern double OrderSize = 0.1;
extern int    StopOrderDistance=5;
extern int    MaxOrders=1;
extern int    TakeProfit = 35;
extern int    StopLoss = 8;
extern double TrailingStopStart = 40;
extern double TrailingStop = 30;
extern double TrailingStopStep = 1;
extern string start_time = "00:00"; 
extern string end_time = "23:59";
extern int    MagicNumber = -1;
extern int    Slippage=3;

int orders_total, orders_BUY, orders_SELL, orders_BUYLIMIT, orders_BUYSTOP, orders_SELLLIMIT, orders_SELLSTOP;
datetime lastActTime;
int lastBuyStopTicket, lastSellStopTicket;

bool gOrdSel;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
   if (MagicNumber < 0)
   {
     sub_magicnumber();
   }
   
   if(MarketInfo(Symbol(), MODE_STOPLEVEL)>StopOrderDistance)
   Alert("StopOrderDisance.Pips is not set correctly!");
   
   lastActTime = Time[0];
   
   return(0);
}

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
{
   return(0);
}

int start()
{
   bool isTradeTime = isTradingTime();

   if(Time[0] > lastActTime) 
   {   
      DeletePendings(LONG);
      DeletePendings(SHORT);
      CountOrders(); 
      bool isNewBar = true;
      if(orders_BUY + orders_SELL < MaxOrders)
      {        
         if (isTradeTime)
         {
            if (BuyAllow && orders_BUYSTOP == 0)
            {
               lastBuyStopTicket = DoPendingLot(Symbol(), LONG, OP_BUYSTOP, Open[0] + (Ask - Bid) + StopOrderDistance*Point, OrderSize, StopLoss, TakeProfit, "Bertie The Bat Buy");
            }
            if (SellAllow && orders_SELLSTOP == 0)
            {
               lastSellStopTicket = DoPendingLot(Symbol(), SHORT, OP_SELLSTOP, Open[0] - StopOrderDistance*Point, OrderSize, StopLoss, TakeProfit, "Bertie The Bat Sell");
            }
            if ((BuyAllow && lastBuyStopTicket < 0) || (SellAllow && lastSellStopTicket < 0))
            {
               isNewBar = false; 
               Print("lastBuyStopTicket:",lastBuyStopTicket, " lastSellStopTicket:",lastSellStopTicket);     
            }
         }
      }
      
      if (isNewBar)
      {
          lastActTime = Time[0];
      }
   }
   
   if (OrderSelect(lastBuyStopTicket, SELECT_BY_TICKET))
   {
       if (OrderType() == OP_BUY)
       {
          if (lastSellStopTicket > 0)
          {
             if (OrderDelete(lastSellStopTicket))
             {
                 lastSellStopTicket = 0;
             }
          }
       }
   }
   
   if (OrderSelect(lastSellStopTicket, SELECT_BY_TICKET))
   {
       if (OrderType() == OP_SELL)
       {
          if (lastBuyStopTicket > 0)
          {
             if (OrderDelete(lastBuyStopTicket))
             {
                 lastBuyStopTicket = 0;
             }
          }
       }
   }
   
   tailingStopOrders();
   //
   return(0);
}

bool isTradingTime()
{
   bool isTT = false;

   datetime tm0 = TimeCurrent();
   datetime tm1, tm2;

   tm1 = StrToTime(TimeToStr(tm0, TIME_DATE) + " " + start_time);
   tm2 = StrToTime(TimeToStr(tm0, TIME_DATE) + " " + end_time);

   if (tm1 <= tm2) 
     isTT = tm1 <= tm0 && tm0 <= tm2;

   return (isTT);
}

//  Magic Number - calculated from a sum of account number and ASCII-codes from currency pair                                                                           
int sub_magicnumber ()
{
     string local_currpair = Symbol();
     int local_length = StringLen (local_currpair);
     int local_asciisum = 0;
     int local_counter;

     for (local_counter = 0; local_counter < local_length -1; local_counter++)
        local_asciisum += StringGetChar (local_currpair, local_counter);
     MagicNumber = AccountNumber() + local_asciisum;
     //
     return(0);
}

bool DeletePendings(int direction) 
{   
   bool pendings;
   int i;
   
   CountOrders(); 
   if( (orders_SELLLIMIT != 0 || orders_SELLSTOP != 0) && direction == SHORT) 
   {
      while(orders_SELLLIMIT != 0 || orders_SELLSTOP != 0) 
      {
         for(i = OrdersTotal() - 1; i >= 0; i--) 
         {
            if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
            
            pendings = OrderType() == OP_SELLLIMIT || OrderType() == OP_SELLSTOP;            
            if( OrderMagicNumber() == MagicNumber && OrderSymbol() == Symbol() && pendings ) 
            {              
               gOrdSel = OrderDelete( OrderTicket() );
            }
         }          
         CountOrders();                 
      }
      
      return( true );
   }
   
   if ((orders_BUYLIMIT != 0 || orders_BUYSTOP != 0) && direction == LONG) 
   {      
      while((orders_BUYLIMIT != 0 || orders_BUYSTOP != 0)) 
      {
         for(i = OrdersTotal() - 1; i >= 0; i--) 
         {         
            if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
  
            pendings = OrderType() == OP_BUYLIMIT || OrderType() == OP_BUYSTOP;
            if( OrderMagicNumber() == MagicNumber && OrderSymbol() == Symbol() && pendings) 
            {              
               gOrdSel = OrderDelete(OrderTicket());
            }
         }          
         CountOrders();       
      }
      return( true );
   }
   return(false); 
}

//Count Orders and calculate the group's average entry point & spread
int CountOrders() 
{
   orders_BUY = 0;
   orders_SELL = 0;
   orders_BUYLIMIT = 0;
   orders_BUYSTOP = 0;
   orders_SELLLIMIT = 0;
   orders_SELLSTOP = 0;
   
   for( int i = OrdersTotal() - 1; i >= 0; i--) 
   {   
      if (!OrderSelect( i, SELECT_BY_POS)) continue;
      
      if( OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber ) 
      {                       
         if( OrderType() == OP_BUY ) 
         { 
             orders_BUY++; 
         }
         if( OrderType() == OP_SELL ) 
         { 
             orders_SELL++; 
         }
         if( OrderType() == OP_BUYLIMIT ) 
         { 
             orders_BUYLIMIT++; 
         }
         if( OrderType() == OP_BUYSTOP ) 
         { 
             orders_BUYSTOP++; 
         }
         if( OrderType() == OP_SELLLIMIT ) 
         { 
             orders_SELLLIMIT++; 
         }
         if( OrderType() == OP_SELLSTOP ) 
         { 
             orders_SELLSTOP++; 
         }
      }
   }
   
   orders_total = orders_BUY + orders_SELL;
   
   return(orders_total);
}

void tailingStopOrders() 
{            
    if (TrailingStop <= 0) return;
    if (TrailingStopStart <= 0) return;
    if (TrailingStopStep <= 0) return;
    
    for (int i = OrdersTotal() - 1; i >= 0; i--) 
    {
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;             
      if(OrderMagicNumber() == MagicNumber)
      {
         string symbolName = OrderSymbol();
         double dDigits = MarketInfo(symbolName, MODE_DIGITS);
         double dPoint = MarketInfo(symbolName, MODE_POINT);   

         double dAsk = MarketInfo(symbolName, MODE_ASK);
         double dBid = MarketInfo(symbolName, MODE_BID);
             
         if (OrderType() == OP_BUY) 
         {       
            double SL = dBid - TrailingStop*dPoint;            
            if (dBid-OrderOpenPrice()>=TrailingStopStart*dPoint && SL >= OrderStopLoss() + TrailingStopStep*dPoint) 
            {                 
                modifyOrderByPrice(OrderTicket(), OrderOpenPrice(), SL, OrderTakeProfit());
            }
         }

         if (OrderType() == OP_SELL) 
         {                
            SL = dAsk + TrailingStop*dPoint;
            if (OrderOpenPrice()-dAsk>=TrailingStopStart*dPoint && (SL <= OrderStopLoss() - TrailingStopStep*dPoint|| OrderStopLoss() == 0)) 
            {               
                modifyOrderByPrice(OrderTicket(), OrderOpenPrice(), SL, OrderTakeProfit());
            }
         }
      }
   }
}

//open a pending order, if failed try again with RETRYCOUNT times
int DoPendingLot(string symbolName, int dir, int pendingType, double entryPrice, double volume, int stop, int take, string comment)  {
   
   int retVal = -1;   
   double sl, tp;
   double l_Point = MarketInfo(symbolName, MODE_POINT); 
   double l_Digits = MarketInfo(symbolName, MODE_DIGITS);
   string info;
   
   for (int i=0; i<RETRYCOUNT; i++) {
     for (int j=0; (j<50) && IsTradeContextBusy(); j++)
         Sleep(100);
      RefreshRates();   

      switch(dir)  {
         case LONG:                               
                if (stop != 0) { sl = entryPrice-(stop*l_Point); }
                else { sl = 0; }
                if (take != 0) { tp = entryPrice +(take*l_Point); }
                else { tp = 0; }
                                              
                info = "Type: " + pendingType + ", \nentryPrice: " + DoubleToStr(entryPrice, l_Digits) + ", \nAsk " + DoubleToStr(MarketInfo(symbolName, MODE_ASK),l_Digits)
                   + ", \nLots " + DoubleToStr(volume, 2) + " , \nStop: " + DoubleToStr(sl, l_Digits)  
                   + ", \nTP: " + DoubleToStr(tp, l_Digits);
                Print(info);
                Comment(info);                
                               
                retVal = OrderSend(symbolName, pendingType, volume, entryPrice, Slippage, sl, tp, comment, MagicNumber, 0, Olive);
                if (retVal < 0)  
                {
                    retVal = OrderSend(symbolName, pendingType, volume, entryPrice, Slippage, 0, 0, comment, MagicNumber, 0, Blue);                    
                    modifyOrderByPrice(retVal, entryPrice, sl, tp);
                }
                break;

         case SHORT:               
                if (stop != 0) { sl = entryPrice+(stop*l_Point); }
                else { sl = 0; }
                if (take != 0) { tp = entryPrice-(take*l_Point); }
                else { tp = 0; }
                
                info = "Type: " + pendingType + ", \nentryPrice: " + DoubleToStr(entryPrice, l_Digits) + ", \nBid " + DoubleToStr(MarketInfo(symbolName, MODE_BID),l_Digits)
                   + ", \nLots " + DoubleToStr(volume, 2) + " , \nStop: " + DoubleToStr(sl, l_Digits)  
                   + ", \nTP: " + DoubleToStr(tp, l_Digits);
              Print(info);
              Comment(info);
          
                retVal = OrderSend(symbolName, pendingType, volume, entryPrice, Slippage, sl, tp, comment, MagicNumber, 0, MediumVioletRed);
                if (retVal < 0)  
                {
                    retVal = OrderSend(symbolName, pendingType, volume, entryPrice, Slippage, 0, 0, comment, MagicNumber, 0, Red);
                    modifyOrderByPrice(retVal, entryPrice, sl, tp);          
                }
                break;
      }
           
      if( retVal > 0 ) { return( retVal ); }
      else {
         Print("DoPending pendingType:", pendingType, " error: \'"+fnErrorDescription(GetLastError())+"\' when setting entry order");
         Sleep(RETRYDELAY);      
      }
      
   }
      
   return( retVal );
}

//modify an order with specified ticket, if failed try again with RETRYCOUNT times
bool modifyOrderByPrice(int ticket, double price, double stopLoss, double takeProfit)  
{
   bool retVal = true;     
   
   if(!OrderSelect(ticket, SELECT_BY_TICKET)) return (false);
   
   for (int i=0; i<RETRYCOUNT; i++) 
   {
     for (int j=0; (j<50) && IsTradeContextBusy(); j++)
        Sleep(100);
      RefreshRates();                        

       if (MathAbs(OrderStopLoss() - stopLoss) > Point || MathAbs(OrderTakeProfit() - takeProfit) > Point)
       {              
           retVal = OrderModify(ticket, price, stopLoss, takeProfit, 0);
       }
         
      if(retVal) { return( true ); } 
      else {
         Print("DoModifyOrder: error \'"+fnErrorDescription(GetLastError())+"\' when modifying order SL:", stopLoss, " TP:", takeProfit, " OrderOpenPrice:",OrderOpenPrice(), " OrderStopLoss:",OrderStopLoss());
         Sleep(RETRYDELAY);      
      }  
   }      
   return( false );
}

//return error message for each kind of errorCode
string fnErrorDescription(int error_code)
{
    string error_string;

    switch( error_code ) {
        case 0:
        case 1:   error_string="no error";                                                  break;
        case 2:   error_string="common error";                                              break;
        case 3:   error_string="invalid trade parameters";                                  break;
        case 4:   error_string="trade server is busy";                                      break;
        case 5:   error_string="old version of the client terminal";                        break;
        case 6:   error_string="no connection with trade server";                           break;
        case 7:   error_string="not enough rights";                                         break;
        case 8:   error_string="too frequent requests";                                     break;
        case 9:   error_string="malfunctional trade operation (never returned error)";      break;
        case 64:  error_string="account disabled";                                          break;
        case 65:  error_string="invalid account";                                           break;
        case 128: error_string="trade timeout";                                             break;
        case 129: error_string="invalid price";                                             break;
        case 130: error_string="invalid stops";                                             break;
        case 131: error_string="invalid trade volume";                                      break;
        case 132: error_string="market is closed";                                          break;
        case 133: error_string="trade is disabled";                                         break;
        case 134: error_string="not enough money";                                          break;
        case 135: error_string="price changed";                                             break;
        case 136: error_string="off quotes";                                                break;
        case 137: error_string="broker is busy (never returned error)";                     break;
        case 138: error_string="requote";                                                   break;
        case 139: error_string="order is locked";                                           break;
        case 140: error_string="long positions only allowed";                               break;
        case 141: error_string="too many requests";                                         break;
        case 145: error_string="modification denied because order too close to market";     break;
        case 146: error_string="trade context is busy";                                     break;
        case 147: error_string="expirations are denied by broker";                          break;
        case 148: error_string="amount of open and pending orders has reached the limit";   break;
        case 149: error_string="hedging is prohibited";                                     break;
        case 150: error_string="prohibited by FIFO rules";                                  break;
        case 4000: error_string="no error (never generated code)";                          break;
        case 4001: error_string="wrong function pointer";                                   break;
        case 4002: error_string="array index is out of range";                              break;
        case 4003: error_string="no memory for function call stack";                        break;
        case 4004: error_string="recursive stack overflow";                                 break;
        case 4005: error_string="not enough stack for parameter";                           break;
        case 4006: error_string="no memory for parameter string";                           break;
        case 4007: error_string="no memory for temp string";                                break;
        case 4008: error_string="not initialized string";                                   break;
        case 4009: error_string="not initialized string in array";                          break;
        case 4010: error_string="no memory for array\' string";                             break;
        case 4011: error_string="too long string";                                          break;
        case 4012: error_string="remainder from zero divide";                               break;
        case 4013: error_string="zero divide";                                              break;
        case 4014: error_string="unknown command";                                          break;
        case 4015: error_string="wrong jump (never generated error)";                       break;
        case 4016: error_string="not initialized array";                                    break;
        case 4017: error_string="dll calls are not allowed";                                break;
        case 4018: error_string="cannot load library";                                      break;
        case 4019: error_string="cannot call function";                                     break;
        case 4020: error_string="expert function calls are not allowed";                    break;
        case 4021: error_string="not enough memory for temp string returned from function"; break;
        case 4022: error_string="system is busy (never generated error)";                   break;
        case 4050: error_string="invalid function parameters count";                        break;
        case 4051: error_string="invalid function parameter value";                         break;
        case 4052: error_string="string function internal error";                           break;
        case 4053: error_string="some array error";                                         break;
        case 4054: error_string="incorrect series array using";                             break;
        case 4055: error_string="custom indicator error";                                   break;
        case 4056: error_string="arrays are incompatible";                                  break;
        case 4057: error_string="global variables processing error";                        break;
        case 4058: error_string="global variable not found";                                break;
        case 4059: error_string="function is not allowed in testing mode";                  break;
        case 4060: error_string="function is not confirmed";                                break;
        case 4061: error_string="send mail error";                                          break;
        case 4062: error_string="string parameter expected";                                break;
        case 4063: error_string="integer parameter expected";                               break;
        case 4064: error_string="double parameter expected";                                break;
        case 4065: error_string="array as parameter expected";                              break;
        case 4066: error_string="requested history data in update state";                   break;
        case 4099: error_string="end of file";                                              break;
        case 4100: error_string="some file error";                                          break;
        case 4101: error_string="wrong file name";                                          break;
        case 4102: error_string="too many opened files";                                    break;
        case 4103: error_string="cannot open file";                                         break;
        case 4104: error_string="incompatible access to a file";                            break;
        case 4105: error_string="no order selected";                                        break;
        case 4106: error_string="unknown symbol";                                           break;
        case 4107: error_string="invalid price parameter for trade function";               break;
        case 4108: error_string="invalid ticket";                                           break;
        case 4109: error_string="trade is not allowed in the expert properties";            break;
        case 4110: error_string="longs are not allowed in the expert properties";           break;
        case 4111: error_string="shorts are not allowed in the expert properties";          break;
        case 4200: error_string="object is already exist";                                  break;
        case 4201: error_string="unknown object property";                                  break;
        case 4202: error_string="object is not exist";                                      break;
        case 4203: error_string="unknown object type";                                      break;
        case 4204: error_string="no object name";                                           break;
        case 4205: error_string="object coordinates error";                                 break;
        case 4206: error_string="no specified subwindow";                                   break;
        default:   error_string="unknown error";
    }

    return(error_string);
}
   