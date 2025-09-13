//+------------------------------------------------------------------+
//|                                               MACD Sample 45.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//|        Altered for 2/3/4/5 digit and ECN/STP brokers by SelectFX |
//|                                   Example intended for EURUSD H1 |
//+------------------------------------------------------------------+

#property link      "barrowboy@selectfx.net"

extern string UserSettings = "=== User Settings ===";
extern int MagicNumber = 16384;
extern double Lots = 0.01;
extern string YourOrderComment = "MACD Sample 451";

extern string SystemSettings = "=== System Settings ===";
// Change hours for broker timezone which may change with seasonal clock changes 
// More hours allowed gives more trades but may not be more profit!
extern int StartHour   = 4;   // GMT 
extern int LastHour    = 19;  // GMT

extern double TakeProfitLong = 50.0;     // Pip-sensitive
extern double TakeProfitShort = 75.0;     // Pip-sensitive

extern double StopLossLong = 80.0;       // Pip-sensitive 
extern double StopLossShort = 50.0;       // Pip-sensitive 

extern double TrailingStop = 30.0;   // Pip-sensitive
extern double MACDOpenLevel=3.0;     // Pip-sensitive
extern double MACDCloseLevel=2.0;    // Pip-sensitive
extern double MATrendPeriod=26;


extern string BrokerSettings = "=== Broker Settings ===";
extern bool ECN.Broker = false;  // Set to true if broker is ECN/STP needing stops adding after order
extern int Slippage = 5;         // Pip-sensitive - your choice here
extern int SleepWhenBusy = 500;  // Time in molliseconds to delay next attempt when order channel busy
extern int OrderRetries = 12;    // Number of tries to set stops if error occurs

double MacdCurrent, MacdPrevious, SignalCurrent;
double SignalPrevious, MaCurrent, MaPrevious;
int cnt, ticket, total;

static double sdPoint;
static int RealSlippage;

/*
Version History

45
Altered for 2/3/4/5 digit 

451
Add handling for ECN/STP brokers
Add externals for MagicNumber and order comment
Add stop loss
Add trading hours
Add differential TP & SL for Buy/Sell

452
Corrected MagicNumber not being assessed on order close

*/


#include <stderror.mqh> 
   
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

int init ()
 {
    if (Digits == 2 || Digits == 4) 
     {
      sdPoint = Point;
      RealSlippage = Slippage;
     }
    
    if (Digits == 3 || Digits == 5) 
     {
      sdPoint = Point*10.0000;
      RealSlippage = Slippage * 10;
     }
    
  return(0);  
 
 }

int start()
  {

   
// initial data checks
// it is important to make sure that the expert works with a normal
// chart and the user did not make any mistakes setting external 
// variables (Lots, StopLoss, TakeProfit, 
// TrailingStop) in our case, we check TakeProfit
// on a chart of less than 100 bars
   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);  
     }
  
   

// to simplify the coding and speed up access
// data are put into internal variables
   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
   MaCurrent=iMA(NULL,0,MATrendPeriod,0,MODE_EMA,PRICE_CLOSE,0);
   MaPrevious=iMA(NULL,0,MATrendPeriod,0,MODE_EMA,PRICE_CLOSE,1);

   total=OrdersTotal();
   if(total<1) 
     {
      // no opened orders identified
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }
      // check for long position (BUY) possibility
      if (MacdCurrent<0 && MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious)
       if (MathAbs(MacdCurrent)>(MACDOpenLevel*sdPoint) && MaCurrent>MaPrevious)
        if (TradingHours() == true)
         {
          if (ECN.Broker == false) ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,RealSlippage,Ask-StopLossLong*sdPoint,Ask+TakeProfitLong*sdPoint,YourOrderComment,MagicNumber,0,Green);
          else
            {
              ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,RealSlippage,0,0,YourOrderComment,MagicNumber,0,Green); // Send order without stops
           
              if (ticket > -1) AddLiteralStopsByPips(ticket, OP_BUY, StopLossLong, TakeProfitLong); 
           
            }        
 
          return(0); 
         }
      // check for short position (SELL) possibility
      if (MacdCurrent>0 && MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious)
       if (MacdCurrent>(MACDOpenLevel*sdPoint) && MaCurrent<MaPrevious)
        if (TradingHours() == true)
         {
          if (ECN.Broker == false) ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,RealSlippage,Bid+StopLossShort*sdPoint,Bid-TakeProfitShort*sdPoint,YourOrderComment,MagicNumber,0,Red);
          else
            {
              ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,RealSlippage,0,0,YourOrderComment,MagicNumber,0,Red);
             
              if (ticket > -1) AddLiteralStopsByPips(ticket, OP_SELL, StopLossShort, TakeProfitShort);
           
            }

          return(0); 
         }
      return(0);
     }
   // it is important to enter the market correctly, 
   // but it is more important to exit it correctly...   
   
   
   for(cnt=0;cnt<total;cnt++) // cycle through all orders
     {
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
          if (OrderType()<=OP_SELL)                // check for opened position 
           if (OrderSymbol()==Symbol())            // check for symbol
            if (OrderMagicNumber() == MagicNumber) // check for MagicNumber
              {
                  if(OrderType()==OP_BUY)   // long position is opened
                    {
                     // should it be closed?
                     if (MacdCurrent>0 && MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious)
                       if (MacdCurrent>(MACDCloseLevel*sdPoint))
                         {
                          OrderClose(OrderTicket(),OrderLots(),Bid,RealSlippage,Violet); // close position
                          return(0); // exit
                         }
                           // check for trailing stop
                           if(TrailingStop>0)  
                             {                 
                              if(Bid-OrderOpenPrice()>sdPoint*TrailingStop)
                                {
                                 if(OrderStopLoss()<Bid-sdPoint*TrailingStop)
                                   {
                                    OrderModify(OrderTicket(),OrderOpenPrice(),Bid-sdPoint*TrailingStop,OrderTakeProfit(),0,Green);
                                    return(0);
                                   }
                                }
                             }
                    }  // long position is opened
                    
                  else // go to short position
                    {
                     // should it be closed?
                     if (MacdCurrent<0 && MacdCurrent>SignalCurrent)
                      if (MacdPrevious<SignalPrevious && MathAbs(MacdCurrent)>(MACDCloseLevel*sdPoint))
                       {
                        OrderClose(OrderTicket(),OrderLots(),Ask,RealSlippage,Violet); // close position
                        return(0); // exit
                       }
                           // check for trailing stop
                           if(TrailingStop>0)  
                             {                 
                              if((OrderOpenPrice()-Ask)>(sdPoint*TrailingStop))
                                {
                                 if((OrderStopLoss()>(Ask+sdPoint*TrailingStop)) || (OrderStopLoss()==0))
                                   {
                                    OrderModify(OrderTicket(),OrderOpenPrice(),Ask+sdPoint*TrailingStop,OrderTakeProfit(),0,Red);
                                    return(0);
                                   }
                                }
                       }
                    }  // go to short position
             }
     } // cycle through all orders
    
    
     
   return(0);
  }


void AddLiteralStopsByPips(int iTicketToGo, int iType, int iSL, int iTP)
{
  int iDigits, iNumRetries, iError;
  double dAsk, dBid, dSL, dTP;
  
  iDigits = MarketInfo(Symbol(), MODE_DIGITS);

  if(OrderSelect(iTicketToGo, SELECT_BY_TICKET)==true) // SELECT_BY_TICKET
    {
       // is server or context busy - try n times to submit the order
       iNumRetries=OrderRetries;
      
       while(iNumRetries>0)    // Retries Block  
          {
             if (!IsTradeAllowed()) Sleep(SleepWhenBusy);
             
               RefreshRates();
               
               if (iType == OP_BUY)
                 {
                   dAsk = MarketInfo(Symbol(), MODE_ASK);
                   
                   dSL=NormalizeDouble(dAsk-iSL*sdPoint, iDigits);
                   dTP=NormalizeDouble(dAsk+iTP*sdPoint, iDigits);
                 }

               if (iType == OP_SELL)
                 {
                   dBid = MarketInfo(Symbol(), MODE_BID);
                   
                   dSL=NormalizeDouble(dBid+iSL*sdPoint, iDigits);
                   dTP=NormalizeDouble(dBid-iTP*sdPoint, iDigits);
                 }
                          
              OrderModify(OrderTicket(), OrderOpenPrice(), dSL, dTP, 0);
                
              iError = GetLastError();
                  
              if (iError==0) iNumRetries = 0;
 
              else  // retry if error is "busy", otherwise give up
                 {            
                     if(iError==ERR_SERVER_BUSY || iError==ERR_TRADE_CONTEXT_BUSY || iError==ERR_BROKER_BUSY || iError==ERR_NO_CONNECTION || iError == ERR_COMMON_ERROR
                       || iError==ERR_TRADE_TIMEOUT || iError==ERR_INVALID_PRICE || iError==ERR_OFF_QUOTES || iError==ERR_PRICE_CHANGED || iError==ERR_REQUOTE)
                        { 
                           Print("ECN Stops Not Added Error: ", iError);
                           Sleep(SleepWhenBusy);
                           iNumRetries--;
                        }
                      else
                        {
                           iNumRetries = 0;
                           Print("ECN Stops Not Added Error: ", iError);
                           Alert("ECN Stops Not Added Error: ", iError);
                        }
                 }
                 
         }   // Retries Block 
      

    } // SELECT_BY_TICKET
  else
   {
     Print("ECN Stops Invalid Ticket: ", iTicketToGo);
     Alert("ECN Stops Invalid Ticket: ", iTicketToGo);   
 
   }

    
}

bool TradingHours()
 {
  // Check if OK to Open Orders
  if (Hour() >= StartHour)
    if ((Hour() <= LastHour)) return (true);
  
  return (false);
 }
 
  
// the end.