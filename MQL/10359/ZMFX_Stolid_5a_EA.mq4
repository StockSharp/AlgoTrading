//+------------------------------------------------------------------+
//|                                                                  |
//|                                 ZMFX Stolid 5a EA.mq4            |
//|                                 ZoomInForex.com                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "ZoomInForex.com"

//---- input parameters
extern double    MiniLots=0.01;
extern double    MaxLot=10;
extern bool RiskControl = false;
extern int MaxTrades=30;
extern int SL=0;
extern int TP=0;
extern int BE=0;

double InitLots;
int ticket;

string symbol="EURUSD";
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
  return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
datetime Time0=0;

void start()
{

double PowerRisk;

symbol=Symbol();


 //Start 

    //Lot Size Check
if(MaxLot > MarketInfo(symbol,MODE_MAXLOT)) MaxLot=MarketInfo(symbol,MODE_MAXLOT);
      
if(InitLots < MarketInfo(symbol,MODE_MINLOT)) InitLots=MarketInfo(symbol,MODE_MINLOT);

   //cal Lot size
   if(RiskControl) {
      PowerRisk=AccountBalance()/10000;
      if(PowerRisk < 1) PowerRisk=1;
      
      InitLots = MiniLots *PowerRisk;
      if(InitLots > MaxLot) InitLots = MaxLot;
      Print("RiskControl PowerRisk=",PowerRisk," InitLots=",InitLots);
      }
   if(RiskControl==false) InitLots=MiniLots;
      
  // Make Order
double RSI,RSI1,RSIP,RSI15,RSI15P,S4H,SD,EMA1,EMA2,down,up;


   RSI=iRSI(0,0,11,PRICE_CLOSE,0);
   RSI1=iRSI(0,0,11,PRICE_CLOSE,1);
   RSIP=iRSI(0,0,11,PRICE_CLOSE,1);
   RSI15=iRSI(0,PERIOD_M15,11,PRICE_CLOSE,0);
   RSI15P=iRSI(0,PERIOD_M15,11,PRICE_CLOSE,1);
   S4H=iStochastic(symbol,PERIOD_H4,30,3,3,MODE_SMA,0,MODE_MAIN,0);
   SD=iStochastic(symbol,PERIOD_D1,30,3,3,MODE_SMA,0,MODE_MAIN,0);
   EMA1=iMA(symbol,PERIOD_H1,50,0,MODE_SMMA,PRICE_CLOSE,0);
   EMA2=iMA(symbol,PERIOD_H1,100,0,MODE_SMMA,PRICE_CLOSE,0);
   down=0;
   up=0;
 //  S1=iStochastic(0,0,30,3,3,MODE_SMA,0,MODE_MAIN,0);
  
//          TREND
 if((Time0!=Time[0]) && S4H<50.0 && EMA1<EMA2) down=1;
 if((Time0!=Time[0]) && S4H>50.0 && EMA1>EMA2) up=1;
 
 
 //         LAST BAR (DOWN or UP)
 double barclose, baropen, barup=0, bardown=0;
 barclose=iClose(NULL,0,1);
 baropen=iOpen(NULL,0,1);
 if(barclose>baropen) barup=1;
 if(barclose<baropen) bardown=1;


//          POSITIONS
if(up==1 && bardown==1 && RSI1<30.0 && RSI15<30.0)  {ticket=OrderSend(symbol,OP_BUY,InitLots*2,Ask,10,SL,TP,0,0,0,Black); Time0=Time[0];}
  else 
if(up==1 && bardown==1 && RSI1<30.0)  {ticket=OrderSend(symbol,OP_BUY,InitLots,Ask,10,SL,TP,0,0,0,Lime); Time0=Time[0];}
 
 
if(down==1 && barup==1 && RSI1>70.0 && RSI15>70.0)  {ticket=OrderSend(symbol,OP_SELL,InitLots*2,Bid,10,SL,TP,0,0,0,Black); Time0=Time[0];}
  else
if(down==1 && barup==1 && RSI1>70.0)  {ticket=OrderSend(symbol,OP_SELL,InitLots,Bid,10,SL,TP,0,0,0,Red); Time0=Time[0];}


  
  // closing LONG

int total=OrdersTotal();
for(int cnt=total-1;cnt>=0;cnt--)

     {
      OrderSelect(cnt, SELECT_BY_POS);
      if(   // check for opened position 
         OrderSymbol()==Symbol())  // check for symbol
        {
         if(OrderType()==OP_BUY)   // LONG position is opened
           {
            // should it be closed?
            if(RSI>70.0 || EMA1<EMA2 || (S4H<50.0 && RSI>50.0) )
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
                  // exit
                }
            // check for trailing stop
            
           }
         else // go to SHORT position
           {
            // should it be closed?
            if(RSI<30.0 || EMA1>EMA2 || (S4H>50.0 && RSI<50.0) )
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close position
                // exit
              }
            // check for trailing stop
            
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+


