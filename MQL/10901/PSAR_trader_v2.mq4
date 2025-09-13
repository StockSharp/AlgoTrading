//+------------------------------------------------------------------+
//|                                               PSAR trader v2.mq4 |
//|                            Copyright © 2012, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

//---- input parameters
extern double    Lots=0.1;
extern int       Slip=5;
extern string    StopSettings="Set stops below";
extern double    TakeProfit=50;
extern double    StopLoss=50;
extern string    PSARsettings="Parabolic sar settings follow";
extern double    Step    =0.001;   //Parabolic setting
extern double    Maximum =0.2;    //Parabolic setting
extern bool      CloseOnOppositeSignal=true;
extern string    TimeSettings="Set the hour range the EA should trade";
extern int       StartHour=0;
extern int       EndHour=23;
extern int       MagicNumber=220101;

string Trend;
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----

int StopMultd=10;
int Slippage=Slip*StopMultd;

int i,closesell=0,closebuy=0;

//------------------------------------------------------------

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
          if(OrderMagicNumber()==MagicNumber&&OrderSymbol()==Symbol()) {int halt=1;}


        }
     }
}
//-------------------------------------------------------------------+
// time check
//-------------------------------------------------------------------
if((Hour()>=StartHour)&&(Hour()<=EndHour))
{
int TradeTimeOk=1;
}
else
{ TradeTimeOk=0; }
//-----------------------------------------------------------------
// Bar checks
//-----------------------------------------------------------------

 
 //-------------------------------------------------------------------

//-----------------------------------------------------------------------------------------------------
// Opening criteria
//-----------------------------------------------------------------------------------------------------

// Open buy
 if((iSAR(NULL, 0,Step,Maximum, 0)<iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)>iClose(NULL,0,1))&&(TradeTimeOk==1)&&(halt!=1)&&Trend!="BUY"){
 int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"PSAR trader buy order",MagicNumber,0,Blue);
 if(CloseOnOppositeSignal==true)closesell=1;
 Trend="BUY";
 }


// Open sell
 if((iSAR(NULL, 0,Step,Maximum, 0)>iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)<iClose(NULL,0,1))&&(TradeTimeOk==1)&&(halt!=1)&&Trend!="SELL"){
 int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"PSAR trader sell order",MagicNumber,0,Green);
 if(CloseOnOppositeSignal==true)closebuy=1;
 Trend="SELL";
 }


//-------------------------------------------------------------------------------------------------

//-------------------------------------------------------------------------------------------------
// Closing criteria
//-------------------------------------------------------------------------------------------------

if(closesell==1||closebuy==1||openbuy<1||opensell<1){// start

if(OrdersTotal()>0){
  for(i=1; i<=OrdersTotal(); i++){          // Cycle searching in orders
  
      if (OrderSelect(i-1,SELECT_BY_POS)==true){ // If the next is available
        
          if(OrderMagicNumber()==MagicNumber&&closebuy==1&&OrderSymbol()==Symbol()&&OrderType()==OP_BUY) { OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,CLR_NONE); }
          if(OrderMagicNumber()==MagicNumber&&closesell==1&&OrderSymbol()==Symbol()&&OrderType()==OP_SELL) { OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,CLR_NONE); }
          
          // set stops
          
                // Calculate take profit
                double tpb=NormalizeDouble(OrderOpenPrice()+TP*Point,Digits);
                double tps=NormalizeDouble(OrderOpenPrice()-TP*Point,Digits);
                // Calculate stop loss
                double slb=NormalizeDouble(OrderOpenPrice()-SL*Point,Digits);
                double sls=NormalizeDouble(OrderOpenPrice()+SL*Point,Digits);
          
          if((OrderMagicNumber()==MagicNumber)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())&&OrderType()==OP_BUY){ OrderModify(OrderTicket(),0,OrderStopLoss(),tpb,0,CLR_NONE); }
          if((OrderMagicNumber()==MagicNumber)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())&&OrderType()==OP_SELL){ OrderModify(OrderTicket(),0,OrderStopLoss(),tps,0,CLR_NONE); }
          if((OrderMagicNumber()==MagicNumber)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())&&OrderType()==OP_BUY){ OrderModify(OrderTicket(),0,slb,OrderTakeProfit(),0,CLR_NONE); }
          if((OrderMagicNumber()==MagicNumber)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())&&OrderType()==OP_SELL){ OrderModify(OrderTicket(),0,sls,OrderTakeProfit(),0,CLR_NONE); }

        }
     }
}


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
//+------------------------------END------------------------------------+
















































































int init()
  {
//----
   Alert("Visit www.FxAutomated.com for more goodies!");
//----
   return(0);
  }