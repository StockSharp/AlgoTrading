//+------------------------------------------------------------------+
//|                                              Automatic stops.mq4 |
//|                            Copyright © 2012, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

extern string    ProvidedFreeBy="www.fxautomated.com";
extern string    AboutAutostop="Automatically sets take profit and stop loss.";
extern bool      MonitorTakeProfit=true;
extern bool      MonitorStopLoss=true;
extern double    TakeProfit=30;
extern double    StopLoss=30;

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  
   int digits=MarketInfo("EURUSD",MODE_DIGITS);
   if(digits==5){int StopMultd=10;} else{StopMultd=1;}
   
   double TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
   double SL=NormalizeDouble(StopLoss*StopMultd,Digits);
   
   // Calculate stop loss
   double slb=NormalizeDouble(Ask-SL*Point,Digits);
   double sls=NormalizeDouble(Bid+SL*Point,Digits);

   // Calculate take profit
   double tpb=NormalizeDouble(Ask+TP*Point,Digits);
   double tps=NormalizeDouble(Bid-TP*Point,Digits);
   
//----
 //-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
          if(MonitorTakeProfit==true){ // monitor tp

                if((OrderType()==OP_BUY)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,OrderStopLoss(),tpb,0,CLR_NONE); }
                if((OrderType()==OP_SELL)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,OrderStopLoss(),tps,0,CLR_NONE); }

          } // monitor tp
          
          if(MonitorStopLoss==true){ // monitor sl

                   if((OrderType()==OP_BUY)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,slb,OrderTakeProfit(),0,CLR_NONE); }
                   if((OrderType()==OP_SELL)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,sls,OrderTakeProfit(),0,CLR_NONE); }

          }// monitor sl
          
        }
     }
}
//----
int Error=GetLastError();
  if(Error==130){Alert("Wrong stops. Retrying."); RefreshRates();}
  if(Error==133){Alert("Trading prohibited.");}
  if(Error==2){Alert("Common error.");}
  if(Error==146){Alert("Trading subsystem is busy. Retrying."); Sleep(500); RefreshRates();}

//----
   return(0);
  }
//+------------------------------------------------------------------+