//+------------------------------------------------------------------+
//|                                                         Vita.mq4 |
//|                            Copyright © 2011, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

//---- input parameters
extern string    CandleTrader_v1="www.fxautomated.com for more info and products";
extern double    Lots=0.1;
extern int       Slip=5;
extern double    TakeProfit=500;
extern double    StopLoss=50;
extern bool      Continuation=true;
extern bool      ReverseClose=true;
extern string    SignalsAndManagedAccounts="www.TradingBug.com";

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
int digits=MarketInfo("EURUSD",MODE_DIGITS);
if(digits==5){int StopMultd=10;} else{StopMultd=1;}
int Slippage=Slip*StopMultd;

int MagicNumber1=2001,MagicNumber2=2002,i,closesell=0,closebuy=0;

double TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
double SL=NormalizeDouble(StopLoss*StopMultd,Digits);


double slb=NormalizeDouble(Ask-SL*Point,Digits);
double sls=NormalizeDouble(Bid+SL*Point,Digits);


double tpb=NormalizeDouble(Ask+TP*Point,Digits);
double tps=NormalizeDouble(Bid-TP*Point,Digits);

//-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
          if(OrderMagicNumber()==MagicNumber1) {int halt1=1;}
          if(OrderMagicNumber()==MagicNumber2) {int halt2=1;}

        }
     }
}
//-------------------------------------------------------------------+

//-----------------------------------------------------------------
// Bar checks
//-----------------------------------------------------------------
 if(iOpen(NULL,0,1)<iClose(NULL,0,1)) int BarOneUp=1;
 if(iOpen(NULL,0,1)>iClose(NULL,0,1)) int BarOneDown=1;

 if(iOpen(NULL,0,2)<iClose(NULL,0,2)) int BarTwoUp=1;
 if(iOpen(NULL,0,2)>iClose(NULL,0,2)) int BarTwoDown=1;

 if(iOpen(NULL,0,3)<iClose(NULL,0,3)) int BarThreeUp=1;
 if(iOpen(NULL,0,3)>iClose(NULL,0,3)) int BarThreeDown=1;

 if(iOpen(NULL,0,4)<iClose(NULL,0,4)) int BarFourUp=1;
 if(iOpen(NULL,0,4)>iClose(NULL,0,4)) int BarFourDown=1;
//-----------------------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------------------------
// Opening criteria
//-----------------------------------------------------------------------------------------------------

// Open buy direct
if(BarOneUp==1&&BarTwoDown==1&&BarThreeDown==1&&halt1!=1){
 int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,slb,tpb,"Candle bug buy order",MagicNumber1,0,Blue);
 if(ReverseClose==true)closesell=1;
 }

// Open buy by continuation
if(BarOneUp==1&&BarTwoDown==1&&BarThreeUp==1&&BarFourUp==1&&halt1!=1&&Continuation==true){
  openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,slb,tpb,"Candle bug buy continuation order",MagicNumber1,0,Blue);
  if(ReverseClose==true)closesell=1;
 }


// Open sell direct
if(BarOneDown==1&&BarTwoUp==1&&BarThreeUp==1&&halt2!=1){
 int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,sls,tps,"Candle bug sell order",MagicNumber2,0,Green);
 if(ReverseClose==true)closebuy=1;
 }

// Open sell by continuation
if(BarOneDown==1&&BarTwoUp==1&&BarThreeDown==1&&BarFourDown&&halt2!=1&&Continuation==true){
  opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,sls,tps,"Candle bug sell continuation order",MagicNumber2,0,Green);
  if(ReverseClose==true)closebuy=1;
 }
//-------------------------------------------------------------------------------------------------

//-------------------------------------------------------------------------------------------------
// Closing criteria
//-------------------------------------------------------------------------------------------------

if(closesell==1||closebuy==1){// start

if(OrdersTotal()>0){
  for(i=1; i<=OrdersTotal(); i++){          // Cycle searching in orders
  
      if (OrderSelect(i-1,SELECT_BY_POS)==true){ // If the next is available
        
          if(OrderMagicNumber()==MagicNumber1&&closebuy==1) { OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,CLR_NONE); }
          if(OrderMagicNumber()==MagicNumber2&&closesell==1) { OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,CLR_NONE); }

        }
     }
}


}// stop

if(openbuy<1||opensell<1){ Sleep(1000*60*60*4);}

//-------------------------------------------------------------------
   return(0);
  }
//+------------------------------------------------------------------+