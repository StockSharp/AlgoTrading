//+------------------------------------------------------------------+
//|                                                  X trader v3.mq4 |
//|                            Copyright © 2013, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

//---- input parameters
extern double    Lots=0.1;
extern int       Slip=5;
extern string    StopSettings="Set stops below";
extern double    TakeProfit=150;
extern double    StopLoss=100;
extern bool      AllowBuy=true;
extern bool      AllowSell=true;
extern bool      CloseOnReverseSignal=true;
extern string    TimeSettings="Set the time range the EA should trade";
extern string    StartTime="00:00";
extern string    EndTime="23:59";
extern string    Ma1="First Ma settings";
extern int       Ma1Period=16;
extern int       Ma1Shift=8;
extern int       Ma1Method=MODE_SMA;
extern int       Ma1AppliedPrice=PRICE_MEDIAN;
extern string    Ma2="Second Ma settings";
extern int       Ma2Period=1;
extern int       Ma2Shift=0;
extern int       Ma2Method=MODE_SMA;
extern int       Ma2AppliedPrice=PRICE_MEDIAN;
extern string    MagicNumbers="Set different magicnumber for each timeframe of a pair";
extern int       MagicNumber=103;


string freeze;


int init()
{
return(0);
}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----

int StopMultd=10;
int Slippage=Slip*StopMultd;
double PipsGained,PointsGained;
color CpColor;

int i,closesell=0,closebuy=0;

double  TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
double  SL=NormalizeDouble(StopLoss*StopMultd,Digits);


//-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
          if(OrderMagicNumber()==MagicNumber&&OrderSymbol()==Symbol()&&OrderType()==OP_BUY) {int haltbuy=1; }
          if(OrderMagicNumber()==MagicNumber&&OrderSymbol()==Symbol()&&OrderType()==OP_SELL) {int haltsell=1; }
                  
          if((OrderType()==OP_BUY)&&(OrderMagicNumber()==MagicNumber)){
          double difference=Bid-OrderOpenPrice();
          PointsGained=NormalizeDouble(difference/Point,Digits);
          PipsGained=PointsGained/10;
          }
          
          if((OrderType()==OP_SELL)&&(OrderMagicNumber()==MagicNumber)){
          difference=OrderOpenPrice()-Ask;
          PointsGained=NormalizeDouble(difference/Point,Digits);
          PipsGained=PointsGained/10;
          }
          
        }
     }
}

//-------------------------------------------------------------------+
// time check
//-------------------------------------------------------------------
if((TimeCurrent()>=StrToTime(StartTime))&&(TimeCurrent()<=StrToTime(EndTime)))
{
int TradeTimeOk=1;
}
else
{ TradeTimeOk=0; }  
//-----------------------------------------------------------------
// indicator checks
//-----------------------------------------------------------------

// Ma strategy one
double MA1_bc=iMA(NULL,0,Ma1Period,Ma1Shift,Ma1Method,Ma1AppliedPrice,0);
double MA1_bp=iMA(NULL,0,Ma1Period,Ma1Shift,Ma1Method,Ma1AppliedPrice,1);
double MA1_bl=iMA(NULL,0,Ma1Period,Ma1Shift,Ma1Method,Ma1AppliedPrice,2);



// Ma strategy two
double MA2_bc=iMA(NULL,0,Ma2Period,Ma2Shift,Ma2Method,Ma2AppliedPrice,0);
double MA2_bp=iMA(NULL,0,Ma2Period,Ma2Shift,Ma2Method,Ma2AppliedPrice,1);
double MA2_bl=iMA(NULL,0,Ma2Period,Ma2Shift,Ma2Method,Ma2AppliedPrice,2);



  //------------------opening criteria------------------------
if((MA1_bc<MA2_bc)&&(MA1_bp<MA2_bp)&&(MA1_bl>MA2_bl))
{ bool buysignal=true;}else{ buysignal=false; }

if((MA1_bc>MA2_bc)&&(MA1_bp>MA2_bp)&&(MA1_bl<MA2_bl))
{ bool sellsignal=true;}else{ sellsignal=false; }


//------------------------------closing criteria--------------
if((MA1_bc>MA2_bc)&&(MA1_bp>MA2_bp)&&(MA1_bl<MA2_bl)){closebuy=1; }else{ closebuy=0; }
if((MA1_bc<MA2_bc)&&(MA1_bp<MA2_bp)&&(MA1_bl>MA2_bl)){closesell=1; }else{ closesell=0; }




//-----------------------------------------------------------------------------------------------------
// Opening criteria
//-----------------------------------------------------------------------------------------------------


// Open buy
 if((buysignal==true)&&(closebuy!=1)&&(freeze!="Buying trend")&&(TradeTimeOk==1)){
 
   if(AllowBuy==true&&haltbuy!=1)int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"Stochastic rsi buy order",MagicNumber,0,Blue);
 
 }


// Open sell
 if((sellsignal==true)&&(closesell!=1)&&(freeze!="Selling trend")&&(TradeTimeOk==1)){

 if(AllowSell==true&&haltsell!=1) int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"Stochastic rsi sell order",MagicNumber,0,Red);

 }
 


if(buysignal==true){freeze="Buying trend";}
if(sellsignal==true){freeze="Selling trend";}


//-------------------------------------------------------------------------------------------------
// Closing criteria
//-------------------------------------------------------------------------------------------------

if(closesell==1||closebuy==1||openbuy<1||opensell<1){// start
if(OrdersTotal()>0){
  for(i=1; i<=OrdersTotal(); i++){          // Cycle searching in orders
  
      if (OrderSelect(i-1,SELECT_BY_POS)==true){ // If the next is available
          if(CloseOnReverseSignal==true){
          if(OrderMagicNumber()==MagicNumber&&closebuy==1&&OrderType()==OP_BUY&&OrderSymbol()==Symbol()) { OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,CLR_NONE); }
          if(OrderMagicNumber()==MagicNumber&&closesell==1&&OrderType()==OP_SELL&&OrderSymbol()==Symbol()) { OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,CLR_NONE); }
          }
          // set stops
          
                // Calculate take profit
                double tpb=NormalizeDouble(OrderOpenPrice()+TP*Point,Digits);
                double tps=NormalizeDouble(OrderOpenPrice()-TP*Point,Digits);
                // Calculate stop loss
                double slb=NormalizeDouble(OrderOpenPrice()-SL*Point,Digits);
                double sls=NormalizeDouble(OrderOpenPrice()+SL*Point,Digits);

          
          if(TakeProfit>0){// if tp not 0
          if((OrderMagicNumber()==MagicNumber)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())&&(OrderType()==OP_BUY)){ OrderModify(OrderTicket(),0,OrderStopLoss(),tpb,0,CLR_NONE); }
          if((OrderMagicNumber()==MagicNumber)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())&&(OrderType()==OP_SELL)){ OrderModify(OrderTicket(),0,OrderStopLoss(),tps,0,CLR_NONE); }
          }
          
          if(StopLoss>0){// if sl not 0
          if((OrderMagicNumber()==MagicNumber)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())&&(OrderType()==OP_BUY)){ OrderModify(OrderTicket(),0,slb,OrderTakeProfit(),0,CLR_NONE);  }
          if((OrderMagicNumber()==MagicNumber)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())&&(OrderType()==OP_SELL)){ OrderModify(OrderTicket(),0,sls,OrderTakeProfit(),0,CLR_NONE);  }
          }
          

          
        } // if available
     } // cycle
}// orders total


}// stop



//----
int Error=GetLastError();
  if(Error==130){Alert("Wrong stops. Retrying."); RefreshRates();}
  if(Error==133){Alert("Trading prohibited.");}
  if(Error==2){Alert("Common error.");}
  if(Error==146){Alert("Trading subsystem is busy. Retrying."); Sleep(500); RefreshRates();}

//----

//-------------------------------------------------------------------
   return(0);
  }
//+--------------------------------End----------------------------------+








































































































int deinit()
{
  Alert("Visit www.fxautomated.com for more forex tools");
  return;
}