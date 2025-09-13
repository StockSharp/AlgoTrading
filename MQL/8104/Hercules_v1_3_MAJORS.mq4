//+----------------------------------------------------------------------+
//|                                                        10hrCross.mq4 |
//|                                                         David J. Lin |
//|                                                                      |
//|Based on a 72-period crossover strategy by something_witty IBFX forum |
//|      Vince ( forexportfolio@hotmail.com )                            |  
//|                                                                      |
//|  - Trigger = price exceeds Trigger pips above/below crossover        |
//|              of current price and 72-period SMA                      |
//|  - Stoploss = low of last bar if long, high of last bar if short     |
//|  - Double order executed at trigger - you can takeprofit with one    |
//|     and let the other one ride with trailing stop.                   |
//|    (Trailing stop is applied to both orders.)                        |
//|  - Timeframe = recommended: H1 or longer, but this EA can be applied |
//|     to any timeframe.                                                |
//|  - Pairs = Any, but with current parameters,                         |
//|     EURUSD shows greatest profit in 2006                             |
//|  - Money Management = stoploss.  WARNING:  this EA does NOT take     |
//|     margin considerations into account, so beware of margin          |
//|                                                                      |
//|Coded by David J. Lin                                                 |
//|                                                                      |
//|Evanston, IL, September 11, 2006                                      |
//|                                                                      |
//|TakeLong, TakeShort, and TrailingAlls coded by Patrick (IBFX tutorial)|
//+----------------------------------------------------------------------+
//| 07/04/08 - Added Money Management routine by Azmel.                  |
//|          - Added H4 RSI 14 filter and H10 High/Low filter.           |
//|          - Added H4 72 SMA 32 filter.                                |
//| 08/04/08 - Replaced SMA filter with Envelope filter.                 |
//|          - Added Blackout time period.                               |
//+----------------------------------------------------------------------+
#property copyright ""
#property link      ""


                          // USD/CHF, EUR/USD


extern double Lots=0.01;          // lots to trade (fractional values ok)
extern bool   MoneyManagement=true;
extern double Risk=2.5;
extern int Trigger=38;            // pips above crossover to trigger order   
extern int Slippage=5;            // pips slippage allowed
extern int TrailingStop=90;       // pips to trail both orders 
extern int TakeProfit1=210;         // pips take profit order #1
extern int TakeProfit2=280;         // pips take profit order #2
 
extern int MA1Period=1;           // EMA(1) acts as trigger line to gauge immediate price action 
extern int MA1Shift=0;            // Shift
extern int MA1Method=MODE_EMA;    // Mode
extern int MA1Price=PRICE_CLOSE;  // Method

extern int MA2Period=72;          // SMA(72) acts as base line
extern int MA2Shift=32;            // Shift
extern int MA2Method=MODE_SMA;    // Mode
extern int MA2Price=PRICE_OPEN;  // Method

extern double RSI.Upper = 55;
extern double RSI.Lower = 45;
extern int    BlackoutPeriod = 144;

string BlackoutStatus;
int    BlackoutStart;

double Leverage;
double BarsCount;
int i;
double RSI;
double ENU;
double ENL;
double ENU2;
double ENL2;
double High10H;
double Low10H;

bool flag_check=true;             // flag to gauge order status                     
int magic1=1234;                  // order #1's magic number
int magic2=4321;                  // order #2's magic number
datetime lasttime=0;              // stores current bar's time to trigger MA calculation
datetime crosstime=0;             // stores most recent crossover time
double crossprice;                // stores price at crossover, calculate 4 point average

double fast1;                     // stores MA values up to 3 completed bars ago
double fast2;                     // fast = current price action, approximated by EMA(1)
double fast3;                     // slow = base line, SMA(10)
double slow1;
double slow2;
double slow3;

int init()
{
   BlackoutStatus="NONE";
// hello world
 return(0);
}

int deinit()
{
// goodbye world
 return(0);
}

//===========================================================================================
//===========================================================================================

int start()         // main cycle
{
   //MONEY MANAGEMENT ROUTINE BY AZMEL
   if (MoneyManagement)
   {
      Leverage=AccountLeverage();
      Lots=MathFloor((AccountBalance()*Risk*(Leverage/100))/(MarketInfo(Symbol(),MODE_LOTSIZE)*MarketInfo(Symbol(),MODE_MINLOT)))*MarketInfo(Symbol(),MODE_MINLOT);
   }


 OrderStatus();     // Check order status
 if(flag_check)     
  CheckTrigger();   // Trigger order execution
 else
  MonitorOrders();  // Monitor open orders
}

//===========================================================================================
//===========================================================================================

void OrderStatus()          // Check order status
{
 int trade;                 // dummy variable to cycle through trades
 int trades=OrdersTotal();  // total number of pending/open orders
 flag_check=true;           // first assume we have no open/pending orders
 for(trade=0;trade<trades;trade++)
 {
  OrderSelect(trade,SELECT_BY_POS,MODE_TRADES); 
  if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==magic1||OrderMagicNumber()==magic2))
   flag_check=false;        // deselect flag_check if there are open/pending orders for this pair
 }
return(0);
}

//===========================================================================================
//===========================================================================================

void CheckTrigger()         // Trigger order execution
{
 double triggerprice;       // price to trigger order execution
 double StopLoss;           // stop-loss price
 bool flag;                 // flag to indicate whether to go long or go short

 if(lasttime==Time[0])      // only need to calculate MA values at the start of each bar
  {
//Calculate Indicators
   // up to 3 completed bars ago:
   fast1=iMA(NULL,0,MA1Period,MA1Shift,MA1Method,MA1Price,1); 
   fast2=iMA(NULL,0,MA1Period,MA1Shift,MA1Method,MA1Price,2);
   fast3=iMA(NULL,0,MA1Period,MA1Shift,MA1Method,MA1Price,3);  
   slow1=iMA(NULL,0,MA2Period,MA2Shift,MA2Method,MA2Price,1); 
   slow2=iMA(NULL,0,MA2Period,MA2Shift,MA2Method,MA2Price,2);
   slow3=iMA(NULL,0,MA2Period,MA2Shift,MA2Method,MA2Price,3);    
//Check for MA cross
  if(fast1>slow1 && fast2<slow2) // cross up 1 bar ago
   {
    flag=true;
    crossprice=(fast1+fast2+slow1+slow2)/4.0;
    crosstime=Time[1]; 
    triggerprice=crossprice+(Trigger*Point);
   }
   else if(fast2>slow2 && fast3<slow3) // cross up 2 bars ago
   {
    flag=true;
    crossprice=(fast2+fast3+slow2+slow3)/4.0;
    crosstime=Time[2];
    triggerprice=crossprice+(Trigger*Point);
   }
   
   if(fast1<slow1 && fast2>slow2) // cross down 1 bar ago
   {
    flag=false;
    crossprice=(fast1+fast2+slow1+slow2)/4.0;
    crosstime=Time[1];
    triggerprice=crossprice-(Trigger*Point);
   }
   else if(fast2<slow2 && fast3>slow3) // cross down 2 bars ago
   {
    flag=false;
    crossprice=(fast2+fast3+slow2+slow3)/4.0;
    crosstime=Time[2];
    triggerprice=crossprice-(Trigger*Point);
   }
  } 
  lasttime=Time[0]; 

//Display countdown timer (seconds left)
  int countdown = (2*Period()*60)-(CurTime()-crosstime);
  
  if (countdown>=0)
  {
     double triggerpips=((Ask+Bid)/2.0-crossprice)/Point;
     Comment("\n","Hercules v1.3","\n\n"
                 ,"Copyright © 2006-2008 Edward Clark","\n"
                 ,"EA coded by David Lin"        ,"\n"
                 ,"Modificatios by Azmel Ainul","\n\n"
                 ,"Minutes in window of opportunity = ", countdown/60,". Cross-Price = ", crossprice, ". Pips from Trigger = ", triggerpips , ".");
  }
  else
  {
     Comment("\n","Hercules v1.3","\n\n"
                 ,"Copyright © 2006-2008 Edward Clark","\n"
                 ,"EA coded by David Lin"        ,"\n"
                 ,"Modificatios by Azmel Ainul");
  }
  

   BarsCount=120/Period();
   High10H=0;
   Low10H=Low[1];
   for(i=1;i<=BarsCount;i++)
   {
      High10H=MathMax(High10H,High[i]);
      Low10H=MathMin(Low10H,Low[i]);
   }
   
   RSI=iRSI(Symbol(),PERIOD_H1,10,PRICE_TYPICAL,0);
//   SMA=iMA(Symbol(),PERIOD_H4,72,32,MODE_SMA,PRICE_CLOSE,0);
   ENU=iEnvelopes(NULL, PERIOD_D1, 24,MODE_SMA,80,PRICE_CLOSE,0.99,MODE_UPPER,0);
   ENL=iEnvelopes(NULL, PERIOD_D1, 24,MODE_SMA,80,PRICE_CLOSE,0.99,MODE_LOWER,0);
      
   ENU2=iEnvelopes(NULL, PERIOD_H4, 96,MODE_SMA,24,PRICE_CLOSE,0.10,MODE_UPPER,0);
   ENL2=iEnvelopes(NULL, PERIOD_H4, 96,MODE_SMA,24,PRICE_CLOSE,0.10,MODE_LOWER,0);
//
//Enter Long 
//      
 if(Ask>=triggerprice&&flag==true&&countdown>=0)
 {       
  StopLoss = iLow(NULL,0,4);  
  if(RSI>RSI.Upper && Ask>High10H && Ask>ENU && Ask>ENU2 && BlackoutStatus=="NONE")
  {
  OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,StopLoss,TakeLong(Ask,TakeProfit1),NULL,magic1,0,Blue);
  OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,StopLoss,TakeLong(Ask,TakeProfit2),NULL,magic2,0,Blue);
  BlackoutStatus="ACTIVE";
  BlackoutStart=CurTime();
  }
 }//Long 
//
//Enter Short 
//
 if(Bid<=triggerprice&&flag==false&&countdown>=0)
 {
  StopLoss = iHigh(NULL,0,4);
  if(RSI<RSI.Lower && Bid<Low10H && Bid<ENL && Bid<ENL2 && BlackoutStatus=="NONE")
  {
  OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,StopLoss,TakeShort(Bid,TakeProfit1),NULL,magic1,0,Red);
  OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,StopLoss,TakeShort(Bid,TakeProfit2),NULL,magic2,0,Red);
  BlackoutStatus="ACTIVE";
  BlackoutStart=CurTime();
  }
 }//Shrt
 
 if(BlackoutStatus=="ACTIVE")
 {
    if(CurTime()>BlackoutStart+(BlackoutPeriod*3600))
    {
       BlackoutStatus="NONE";
    }
 }
 
return(0);
}

//===========================================================================================
//===========================================================================================

void MonitorOrders()           //Monitor open orders
{

//
// More sophisticated exit monitoring system may be needed here
// 
 
 TrailingAlls(TrailingStop);   //Trailing Stop
 return(0);
}

//===========================================================================================
//===========================================================================================

double TakeLong(double price,int take)  // function to calculate takeprofit if long
{
 if(take==0)
  return(0.0); // if no take profit
 return(price+(take*Point)); 
             // plus, since the take profit is above us for long positions
}

//===========================================================================================
//===========================================================================================

double TakeShort(double price,int take)  // function to calculate takeprofit if short
{
 if(take==0)
  return(0.0); // if no take profit
 return(price-(take*Point)); 
             // minus, since the take profit is below us for short positions
}

//===========================================================================================
//===========================================================================================

void TrailingAlls(int trail)             // client-side trailing stop
{
 if(trail==0)
  return;
  
 double stopcrnt;
 double stopcal;
  
 int trade;
 int trades=OrdersTotal();
 for(trade=0;trade<trades;trade++)
 {
  OrderSelect(trade,SELECT_BY_POS,MODE_TRADES);
  
  if(OrderSymbol()!=Symbol())
   continue;

//Long 
  if(OrderType()==OP_BUY&&(OrderMagicNumber()!=magic1||OrderMagicNumber()!=magic2))
  {
   stopcrnt=OrderStopLoss();
   stopcal=Bid-(trail*Point); 
   if(stopcrnt==0)
   {
    OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Blue);
   }
   else
   {
    if(stopcal>stopcrnt)
    {
     OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Blue);
    }
   }
  }//Long 
  
//Short 
  if(OrderType()==OP_SELL&&(OrderMagicNumber()!=magic1||OrderMagicNumber()!=magic2))
  {
   stopcrnt=OrderStopLoss();
   stopcal=Ask+(trail*Point); 
   if(stopcrnt==0)
   {
    OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Red);
   }
   else
   {
    if(stopcal<stopcrnt)
    {
     OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Red);
    }
   }
  }//Short   
  
  
 } //for
}