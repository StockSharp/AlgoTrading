//+------------------------------------------------------------------+
//|                                                  X trader v2.mq4 |
//|                            Copyright © 2013, www.FxAutomated.com |
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

string Trend;

//+------------------------------------------------------------------+
//| expert starts                                  |
//+------------------------------------------------------------------+
int start()
  {
//----
int StopMultd,Slip=5;


StopMultd=10;
double TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
// stop loss
double SL=NormalizeDouble(StopLoss*StopMultd,Digits); 

double Slippage=NormalizeDouble(Slip*StopMultd,Digits);


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
          if(OrderMagicNumber()==MagicNumber&&OrderSymbol()==Symbol()) {int halt1=1;}
 
        }
     }
}
//-------------------------------------------------------------------+

//-------------------------------------------------------------------+
// trading strategy
//-------------------------------------------------------------------+


if(halt1!=1){// halt1

// Sell criteria
if ((MA1_bc>MA2_bc)&&(MA1_bp>MA2_bp)&&(MA1_bl<MA2_bl)&&Trend!="SELL") //Signal Sell
 {
   int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"X trader 2 trade ",MagicNumber,0,Green);
   if(opensell<1){int sellfail=1;}
   Trend="SELL";
 }
 
 // Buy criteria
 if ((MA1_bc<MA2_bc)&&(MA1_bp<MA2_bp)&&(MA1_bl>MA2_bl)&&Trend!="BUY") //Signal Buy
 {
   int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"X trader 2 trade ",MagicNumber,0,Blue);
   if(openbuy<1){int buyfail=1;}
   Trend="BUY";
 }
 
 }// halt1

//-----------------------------------------------------------------------------------------------------
if(OrdersTotal()>0){
  for(i=1; i<=OrdersTotal(); i++){          // Cycle searching in orders
  
      if (OrderSelect(i-1,SELECT_BY_POS)==true){ // If the next is available
      
                // Calculate take profit
                double tpb=NormalizeDouble(OrderOpenPrice()+TP*Point,Digits);
                double tps=NormalizeDouble(OrderOpenPrice()-TP*Point,Digits);
                // Calculate stop loss
                double slb=NormalizeDouble(OrderOpenPrice()-SL*Point,Digits);
                double sls=NormalizeDouble(OrderOpenPrice()+SL*Point,Digits);
        
          if((OrderMagicNumber()==MagicNumber&&OrderType()==OP_BUY)&&(OrderTakeProfit()==0||OrderStopLoss()==0)&&OrderSymbol()==Symbol()) { OrderModify(OrderTicket(),0,slb,tpb,0,CLR_NONE); }
          if((OrderMagicNumber()==MagicNumber&&OrderType()==OP_SELL)&&(OrderTakeProfit()==0||OrderStopLoss()==0)&&OrderSymbol()==Symbol()) { OrderModify(OrderTicket(),0,sls,tps,0,CLR_NONE); }

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
//+----------------------END-------------


































































int init()
{
Alert("Visit www.FxAutomated.com for more goodies!");
return(0);
}