//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, matusgerman@gmail.com"

#define NONE   0 
#define UP     1 
#define DOWN   2

extern string    separator1 = "------trend settings------";
extern bool      useEMAtrend = true;               // trade in EMA trend
extern int       barsInTrend = 1;                  // how many bars should be in trend
extern double    EMAtrend = 144;                   // EMA period for trend

extern string    separator2 = "------WPR settings------";
extern double    iWPRPeriod = 46;                  // WPR period for enter and exit
extern double    iWPRRetracement = 30;             // minimal retracement for WPR to allow another trade
extern bool      useWPRExit = true;                // use exit with WPR indicator

extern string    separator3 = "------position settings------";
extern double    lots = 0.1;                       // fixed lot size
extern int       maxTrades = 2;                    // max trades allowed for pyramiding
extern double    stop_loss = 50;                   // stop loss
extern double    take_profit = 200;                // take profit
extern bool      useTrailingStop = false;          // use trailing stop
extern double    trailing_stop = 10;               // trail stop loss
extern bool      useUnprofitExit = false;          // exit trade when trade was not in profit in defined amount of bars
extern int       maxUnprofitBars = 5;              // amount of bars not in profit

extern double    magicNumber = 13131;

double stopLoss, takeProfit, trailingStop;

bool buyBE, sellBE;

double signal;
double minAllowedLot, lotStep, maxAllowedLot;

int unprofitBars=0;

double pips2dbl;

datetime barTime=0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----

   Comment("Copyright © 2011, Matus German");
   
   if (Digits == 5 || Digits == 3)    // Adjust for five (5) digit brokers.
   {            
      pips2dbl = Point*10;
   } 
   else 
   {    
      pips2dbl = Point;   
   }
   
   stopLoss = pips2dbl*stop_loss;
   takeProfit = pips2dbl*take_profit;
   trailingStop = pips2dbl*trailing_stop;
   
   minAllowedLot  =  MarketInfo(Symbol(), MODE_MINLOT);    //IBFX= 0.10
   lotStep        =  MarketInfo(Symbol(), MODE_LOTSTEP);   //IBFX= 0.01
   maxAllowedLot  =  MarketInfo(Symbol(), MODE_MAXLOT );   //IBFX=50.00
   
   if(lots < minAllowedLot)
      lots = minAllowedLot;
   if(lots > maxAllowedLot)
      lots = maxAllowedLot;
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{
//----
   if(IsNewBar())
   {
      if(IsProfitOnBar())
         unprofitBars=0;
      else
         unprofitBars++;
   }

// calculate iWPR for the buy/sell signal    
   signal=iWPR(NULL,0,iWPRPeriod,0);
   
// calculate if iWPR has made the defined retracement to permit another buy trade   
   if (signal > -100+iWPRRetracement) 
   {     
      buyBE = 1;
   }

// calculate if iWPR has made the defined retracement to permit another sell trade   
   if (signal < 0-iWPRRetracement) 
   {     
      sellBE = 1;
   }  
   if(!CloseOrderCheck())
      return;

   if(useTrailingStop)
      if(!TrailingStopCheck())
         return;
           
   if(!OpenOrderCheck())
      return; 
      
//----
   return(0);
}

//////////////////////////////////////////////////////////////////////////////////////////////////  
bool EnterBuyCondition()
{  
   if(useEMAtrend)
      if(Trend()!=UP)
         return(false);
         
   if (signal<-99.99 && buyBE==1)
      return (true);      

   return (false);   
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool EnterSellCondition()
{
   if(useEMAtrend)
      if(Trend()!=DOWN)
         return(false);

   if (signal>-0.01  && sellBE==1)
      return (true);

   return (false);   
}

//////////////////////////////////////////////////////////////////////////////////////////////////  
bool ExitBuyCondition()
{  
   if(useUnprofitExit)  
      if(unprofitBars>maxUnprofitBars)
         return(true); 
            
   if(useWPRExit)
   {
      if (signal>-0.01)
      {
         return (true);
      }
   }
   return (false);   
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool ExitSellCondition()
{
   if(useUnprofitExit)  
      if(unprofitBars>maxUnprofitBars)
         return(true);
          
   if(useWPRExit)
   {
      if (signal<-99.99)
      {
         return (true);
      }     
   }  
   return (false); 
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool CloseOrderCheck()
{
   int total=OpenTradesForMNandPairType(magicNumber, Symbol());
   for(int cnt=total-1;cnt>=0;cnt--)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(   OrderType()<=OP_SELL                      // check for opened position 
         && OrderSymbol()==Symbol()                   // check for symbol
         && OrderMagicNumber() == magicNumber)        // my magic number
      {
         if(OrderType()==OP_BUY)   // long position is opened
         {
            // should it be closed?                       
            if(ExitBuyCondition())
            {
               if(OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet)) // close position
               {
                  return (true);
               }
               return(false);
            } 
         }
         else // go to short position
         {
            // should it be closed?        
            if(ExitSellCondition())
            {
               if(OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet)) // close position
               {
                  return (true);  
               }
               return(false);
            }          
         }
      }
   }
   return (true);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool OpenOrderCheck()
{
   int ticket;
   int total=OpenTradesForMNandPairType(magicNumber, Symbol());
   
   if(total<maxTrades)
   {
      // check for long position (BUY) possibility
      if(EnterBuyCondition())
      {
         ticket=OrderSend(Symbol(),OP_BUY,lots,Ask,3, Ask-stopLoss,Ask+takeProfit,"AOS_JoeChalhoub_FXForecasterV1",magicNumber,0,Green);
         if(ticket>0)
         {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) 
            {
               buyBE=0;
               Print("BUY order opened : ",OrderOpenPrice());
               return (true);
            }
         }
         else 
         {
            Print("Error opening BUY order : ",GetLastError());   
            return(false);
         }   
      }
      
      // check for short position (SELL) possibility
      if(EnterSellCondition())
      {
         ticket=OrderSend(Symbol(),OP_SELL,lots,Bid,3, Bid+stopLoss,Bid-takeProfit,"AOS_JoeChalhoub_FXForecasterV1",magicNumber,0,Red);
         if(ticket>0)
         {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            { 
               sellBE=0;
               Print("SELL order opened : ",OrderOpenPrice());
               return (true);
            }
         }
         else Print("Error opening SELL order : ",GetLastError());
         return (false); 
      }
   }
   return (true);   
}

//////////////////////////////////////////////////////////////////////////////////////////////////
bool TrailingStopCheck()
{  
   double newStopLoss;
   int total=OrdersTotal();
   for(int cnt=total-1;cnt>=0;cnt--)
   {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(   OrderType()<=OP_SELL                      // check for opened position 
         && OrderSymbol()==Symbol()                   // check for symbol
         && OrderMagicNumber() == magicNumber)        // my magic number
      {
         if(OrderType()==OP_BUY)   // long position is opened
         {
            // should it be modified? 
            newStopLoss = TrailingStopBuy();                      
            if(newStopLoss > 0)
            {
               if(OrderModify(OrderTicket(),OrderOpenPrice(),newStopLoss,OrderTakeProfit(),0,Green)) // modify position
               {
                   return (true);
               }
               return(false);
            } 
         }
         else // go to short position
         {
            // should it be modified? 
            newStopLoss = TrailingStopSell();        
            if(newStopLoss > 0)
            {
               if(OrderModify(OrderTicket(),OrderOpenPrice(),newStopLoss,OrderTakeProfit(),0,Green)) // modify position
               {
                  return (true);
               }
               return(false);
            }          
         }
      }
   }
   return (true);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
double TrailingStopBuy()
{
   double newStopLoss;

   newStopLoss=Ask-stopLoss;
   if((newStopLoss-trailingStop)>OrderStopLoss())
      return (newStopLoss);
   return (0);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
double TrailingStopSell()
{
   double newStopLoss;
   
   newStopLoss=Bid+stopLoss;
   if((newStopLoss+trailingStop)<OrderStopLoss())
      return (newStopLoss);

   return (0);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
int OpenTradesForMNandPairType(int iMN, string sOrderSymbol)
{
   int icnt, itotal, retval;

   retval=0;
   itotal=OrdersTotal();

      for(icnt=itotal-1;icnt>=0;icnt--) // for loop
      {
         OrderSelect(icnt, SELECT_BY_POS, MODE_TRADES);
         // check for opened position, symbol & MagicNumber
         if (OrderSymbol()== sOrderSymbol)
         {
            if (OrderMagicNumber()==iMN) 
               retval++;             
         } // sOrderSymbol
      } // for loop

   return(retval);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
int Trend()
{  
   int count=0;
   int curTrend = NONE; 
   
   for(int j=0;j<barsInTrend;j++)
   {
      if(iMA(NULL,0,EMAtrend,0,MODE_EMA,PRICE_MEDIAN,j+1)>iMA(NULL,0,EMAtrend,0,MODE_EMA,PRICE_MEDIAN,j))
         count--; 
      if(iMA(NULL,0,EMAtrend,0,MODE_EMA,PRICE_MEDIAN,j+1)<iMA(NULL,0,EMAtrend,0,MODE_EMA,PRICE_MEDIAN,j))
         count++; 
   }
   
   if(count == barsInTrend)
      curTrend=UP;
   if(count == -(barsInTrend))
      curTrend=DOWN;
   
   return (curTrend);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
bool IsNewBar()
{
   if( barTime < Time[0]) 
   {
        // we have a new bar opened
      barTime = Time[0];
      return(true);
   }
   return (false);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
bool IsProfitOnBar()
{
   int icnt, itotal;
   itotal=OrdersTotal();
   if(itotal==0)
      return(true);
      
   for(icnt=itotal-1;icnt>=0;icnt--) // for loop
   {
      OrderSelect(icnt, SELECT_BY_POS, MODE_TRADES);
      // check for opened position, symbol & MagicNumber
      if (OrderSymbol()== Symbol() && OrderMagicNumber()==magicNumber)
      {
         if (OrderProfit()>0) 
            return(true);             
      } 
   } // for loop 
   return(false);
}

