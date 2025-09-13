//+------------------------------------------------------------------+
//|                                                      Xbug_CE.mq4 |
//|                            Copyright © 2011, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

//---- input parameters
extern string    Xbug_free_v3.0="visit www.FxAutomated.com for more!!!";
extern string    EA_Settings="Default settings is for EURUSD H4";
extern double    Lots=0.1;
extern int       Stops=270;
extern int       MaPeriod=19;
extern int       MaShift=15;
extern int       Slip=5;
extern string    FreeForexSignals="www.TradingBug.com";
extern string    ManagedAccounts="www.TradingBug.com";

//+------------------------------------------------------------------+
//| expert starts                                  |
//+------------------------------------------------------------------+
int start()
  {
//----
int StopMultd,Sleeper=4;



int digits=MarketInfo("EURUSD",MODE_DIGITS);
if(digits==5){StopMultd=10;} else{StopMultd=1;}
double TP=NormalizeDouble(Stops*StopMultd,Digits);
double SL=NormalizeDouble(Stops*StopMultd,Digits);
double Slippage=NormalizeDouble(Slip*StopMultd,Digits);

// Calculate stop loss
double slb=NormalizeDouble(Ask-SL*Point,Digits);
double sls=NormalizeDouble(Bid+SL*Point,Digits);

// Calculate take profit
double tpb=NormalizeDouble(Ask+TP*Point,Digits);
double tps=NormalizeDouble(Bid-TP*Point,Digits);

// Ma strategy one
double MA1_bc=iMA(NULL,0,MaPeriod,MaShift,0,4,0);
double MA1_bp=iMA(NULL,0,MaPeriod,MaShift,0,4,1);
double MA1_bl=iMA(NULL,0,MaPeriod,MaShift,0,4,2);



// Ma constant
double MA2_bc=iMA(NULL,0,1,0,0,4,0);
double MA2_bp=iMA(NULL,0,1,0,0,4,1);
double MA2_bl=iMA(NULL,0,1,0,0,4,2);
//-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
          if(OrderMagicNumber()==0001) {int halt1=1;}
 
        }
     }
}
//-------------------------------------------------------------------+


if(halt1!=1){// halt1

// Buy criteria
if ((MA1_bc>MA2_bc)&&(MA1_bp>MA2_bp)&&(MA1_bl<MA2_bl)) //Signal Buy
 {
   int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,slb,tpb,"Xbug_CE order ",0001,0,Blue);
   if(openbuy<1){int buyfail=1;}
 }
 
 // Sell criteria
 if ((MA1_bc<MA2_bc)&&(MA1_bp<MA2_bp)&&(MA1_bl>MA2_bl)) //Signal Sell
 {
   int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,sls,tps,"Xbug_CE order ",0001,0,Green);
   if(opensell<1){int sellfail=1;}
 }
 
 }// halt1


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

 if(openbuy==true||opensell==true)Sleep(1*60*60*1000*Sleeper);
//-------------------------------------------------------------------
   return(0);
  }
//+-----------------------------------