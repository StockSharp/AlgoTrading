//+------------------------------------------------------------------+
//|                                                       FX10EA.mq4 |
//|                                            Copyright © 2005, hdb |
//|                                       http://www.dubois1.net/hdb |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, hdb" // adapted from Fx10Setup by palanka
#property link      "http://www.dubois1.net/hdb"

// DISCLAIMER ***** IMPORTANT NOTE ***** READ BEFORE USING ***** 
// This expert advisor can open and close real positions and hence do real trades and lose real money.
// This is not a 'trading system' but a simple robot that places trades according to fixed rules.
// The author has no pretentions as to the profitability of this system and does not suggest the use
// of this EA other than for testing purposes in demo accounts.
// Use of this system is free - but u may not resell it - and is without any garantee as to its
// suitability for any purpose.
// By using this program you implicitly acknowledge that you understand what it does and agree that 
// the author bears no responsibility for any losses.
// Before using, please also check with your broker that his systems are adapted for the frequest trades
// associated with this expert.

//---- input parameters
extern int       ParamMult=2;
extern int       shift=0;
extern int       fastMAPeriod=5;
extern int       slowMAPeriod=10;
extern int       stochP1=5;
extern int       stochP2=3;
extern int       stochP3=3;
extern int       MACDP1=12;
extern int       MACDP2=26;
extern int       MACDP3=9;
extern int       RSIP1=14;
extern int       RSICutoffL=50;
extern int       RSICutoffS=50;
extern int       StopLoss=20;
extern int       TakeProfit=50;
extern double    Lots=0.1;
extern string    Name="FX10";
extern int       MagicNumber=1010;
extern int       TimeOffsetToEST=6;    // +6 means 6 hours ahead. this will be subtracted from local time to get EST
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//---- 
#property show_inputs                  // shows the parameters - thanks Slawa...    
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
//---- do we have an open order ? 
   bool haveOpen=false;
   int myOrderType=0;
   int myOrderPos=0;
   for(int j=0;j<OrdersTotal();j++)                                // scan all orders and positions...
     {
      OrderSelect(j, SELECT_BY_POS);
      if(OrderSymbol()==Symbol() &&((OrderMagicNumber()==MagicNumber) || (OrderComment()==Name))) // only look if mygrid and symbol...
        {
         haveOpen   =true;
         myOrderType=OrderType();
         myOrderPos =j;
        }
     }
   double fastMA=iMA(NULL, 0, fastMAPeriod * ParamMult, 0, MODE_LWMA, PRICE_CLOSE, shift);
   double slowMA=iMA(NULL, 0, slowMAPeriod * ParamMult, 0, MODE_SMA, PRICE_CLOSE, shift);
   bool   RsiUp =(iRSI(NULL, 0, RSIP1 * ParamMult, PRICE_CLOSE, shift) > RSICutoffL);
   // and iRSI(p1, 1) > iRSI(p1, 2)
   bool   RsiDown= (iRSI(NULL, 0, RSIP1 * ParamMult, PRICE_CLOSE, shift) < RSICutoffS);
   // and iRSI(p1, 2) > iRSI(p1, 1)
   double Stoch0=iStochastic(NULL, 0, stochP1*ParamMult, stochP2*ParamMult, stochP3*ParamMult, MODE_SMA, PRICE_CLOSE, MODE_MAIN, shift);
   double Stoch1=iStochastic(NULL, 0, stochP1*ParamMult, stochP2*ParamMult, stochP3*ParamMult, MODE_SMA, PRICE_CLOSE, MODE_MAIN, shift + 1);
   double StochSig0=iStochastic(NULL, 0, stochP1*ParamMult, stochP2*ParamMult, stochP3*ParamMult, MODE_SMA, PRICE_CLOSE, MODE_SIGNAL, shift);
//----
   bool StochUp=(Stoch0 > Stoch1);
   //   bool StochUp = (Stoch0 > StochSig0);
   // and Stoch0 > Stoch1
   // and Stoch0 >= StochHigh
   bool StochDown=(Stoch0 < Stoch1);
   //   bool StochDown = (Stoch0 < StochSig0);
   // and Stoch0 < Stoch1
   // and Stoch0 <= StochLow
   double MacdCurrent=iMACD(NULL, 0, MACDP1*ParamMult, MACDP2*ParamMult, MACDP3*ParamMult, PRICE_CLOSE, MODE_MAIN, shift);
   double MacdPrevious=iMACD(NULL, 0, MACDP1*ParamMult, MACDP2*ParamMult, MACDP3*ParamMult, PRICE_CLOSE, MODE_MAIN, shift + 1);
   double MacdSig0=iMACD(NULL, 0, MACDP1*ParamMult, MACDP2*ParamMult, MACDP3*ParamMult, PRICE_CLOSE, MODE_SIGNAL, shift);
   bool MacdUp=(MacdCurrent > 0);
   //   bool MacdUp = (MacdCurrent > MacdSig0);
   // and  MacdCurrent > MacdPrevious
   // and MacdCurrent > 50
   bool MacdDown=(MacdCurrent < 0);
   //   bool MacdDown = (MacdCurrent < MacdSig0);
   // and  MacdCurrent < MacdPrevious
   // and MacdCurrent < 50
   bool goLong=false;
   if(fastMA > slowMA && StochUp && RsiUp && MacdUp)goLong=true;
//----
   bool goShort=false;
   if(fastMA < slowMA && StochDown && RsiDown && MacdDown)goShort=true;
   if(haveOpen==true)
     {
      if(goLong==true && myOrderType==OP_SELL )
        {
         OrderSelect(myOrderPos, SELECT_BY_POS);
         int result=OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );
        }
      if(goLong==false && myOrderType==OP_BUY )
        {
         OrderSelect(myOrderPos, SELECT_BY_POS);
         result=OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );
        }
     }
   // Print(Hour()," ",Minute());
   bool rightHours=false;
   //  int currentHour = MathMod(Hour()+24-TimeOffsetToEST,24) ; // this does not work in backtesting
   int currentHour=MathMod(TimeHour(iTime(NULL,PERIOD_H1,0))+24-TimeOffsetToEST,24);// 
   if (( currentHour>=8 && currentHour < 24)||(currentHour>=2 && currentHour < 4 )) rightHours=true;
   if(haveOpen==false && rightHours)
     {
      if(goLong )
        {
         int ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,0,Ask-StopLoss*Point,Ask+TakeProfit*Point,Name,MagicNumber,0,Green);
        }
      if(goShort )
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,0,Bid+StopLoss*Point,Bid-TakeProfit*Point,Name,MagicNumber,0,Red);
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+