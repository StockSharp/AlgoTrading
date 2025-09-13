//+------------------------------------------------------------------+
//|                                                      X trader.mq4 |
//|                            Copyright © 2012, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

//---- input parameters
extern double    Lots=0.1;
extern int       TakeProfit=150;
extern int       StopLoss=100;
extern string    Ma1="First Ma settings";
extern int       Ma1Period=16;
extern int       Ma1Shift=8;
extern int       Ma1Method=0;
extern int       Ma1AppliedPrice=4;
extern string    Ma2="Second Ma settings";
extern int       Ma2Period=1;
extern int       Ma2Shift=0;
extern int       Ma2Method=0;
extern int       Ma2AppliedPrice=4;
extern int       MagicNumber=320101;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   Alert("Visit www.FxAutomated.com for more goodies!");
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert starts                                  |
//+------------------------------------------------------------------+
int start()
  {
//----
int StopMultd,Slip=5;


int digits=MarketInfo("EURUSD",MODE_DIGITS);
StopMultd=10;
double TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
// stop loss
double SL=NormalizeDouble(StopLoss*StopMultd,Digits); 

double Slippage=NormalizeDouble(Slip*StopMultd,Digits);

// Calculate stop loss
double slb=NormalizeDouble(Ask-SL*Point,Digits);
double sls=NormalizeDouble(Bid+SL*Point,Digits);

// Calculate take profit
double tpb=NormalizeDouble(Ask+TP*Point,Digits);
double tps=NormalizeDouble(Bid-TP*Point,Digits);

// Ma strategy one
double MA1_bc=iMA(NULL,0,Ma1Period,Ma1Shift,Ma1Method,Ma1AppliedPrice,0);
double MA1_bp=iMA(NULL,0,Ma1Period,Ma1Shift,Ma1Method,Ma1AppliedPrice,1);
double MA1_bl=iMA(NULL,0,Ma1Period,Ma1Shift,Ma1Method,Ma1AppliedPrice,2);



// Ma constant
double MA2_bc=iMA(NULL,0,Ma2Period,Ma2Shift,Ma2Method,Ma2AppliedPrice,0);
double MA2_bp=iMA(NULL,0,Ma2Period,Ma2Shift,Ma2Method,Ma2AppliedPrice,1);
double MA2_bl=iMA(NULL,0,Ma2Period,Ma2Shift,Ma2Method,Ma2AppliedPrice,2);



//-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
          if(OrderMagicNumber()==MagicNumber) {int halt1=1;}
 
        }
     }
}
//-------------------------------------------------------------------+

//-------------------------------------------------------------------+
// trading strategy
//-------------------------------------------------------------------+

Comment("For more goodies, managed accounts, forex signals and premium EAs visit www.FxAutomated.com");


if(halt1!=1){// halt1

// Sell criteria
if ((MA1_bc>MA2_bc)&&(MA1_bp>MA2_bp)&&(MA1_bl<MA2_bl)) //Signal Sell
 {
   int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"X trader order ",MagicNumber,0,Green);
   if(opensell<1){int sellfail=1;}
 }
 
 // Buy criteria
 if ((MA1_bc<MA2_bc)&&(MA1_bp<MA2_bp)&&(MA1_bl>MA2_bl)) //Signal Buy
 {
   int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"X trader order ",MagicNumber,0,Blue);
   if(openbuy<1){int buyfail=1;}
 }
 
 }// halt1

//-----------------------------------------------------------------------------------------------------
if(OrdersTotal()>0){
  for(i=1; i<=OrdersTotal(); i++){          // Cycle searching in orders
  
      if (OrderSelect(i-1,SELECT_BY_POS)==true){ // If the next is available
        
          if((OrderMagicNumber()==MagicNumber&&OrderType()==OP_BUY)&&(OrderTakeProfit()==0||OrderStopLoss()==0)) { OrderModify(OrderTicket(),0,slb,tpb,0,CLR_NONE); }
          if((OrderMagicNumber()==MagicNumber&&OrderType()==OP_SELL)&&(OrderTakeProfit()==0||OrderStopLoss()==0)) { OrderModify(OrderTicket(),0,sls,tps,0,CLR_NONE); }

        }
     }
}

//-------------------------------------------------------------------+
// Error processing
//-------------------------------------------------------------------+
if(buyfail==1||sellfail==1){
int Error=GetLastError();
  if(Error==130){Alert("Wrong stops. Retrying."); RefreshRates();}
  if(Error==133){Alert("Trading prohibited.");}
  if(Error==2){Alert("Common error.");}
  if(Error==146){Alert("Trading subsystem is busy. Retrying."); Sleep(500); RefreshRates();}

}

//-------------------------------------------------------------------
   return(0);
  }
//+-----------------------------------