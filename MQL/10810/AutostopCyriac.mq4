//+------------------------------------------------------------------+
//|                                                     AutoStop.mq4 |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "panthaigle"
#property link      ""

extern string    AboutAutostop="Automatically sets take profit and stop loss calcul with inital cost on all trades multisymbol";
extern bool      MonitorTakeProfit=true;
extern bool      MonitorStopLoss=true;
extern double    TakeProfit=15;
extern double    StopLoss=20;

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   int digits=MarketInfo("EURUSD",MODE_DIGITS);
   if(digits==5){int StopMultd=10;} else{StopMultd=1;}
   
   double TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
   double SL=NormalizeDouble(StopLoss*StopMultd,Digits);
   double slb=0;
   double sls=0;
   double tpb=0;
   double tps=0;
   
//----
 //-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
               TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
               SL=NormalizeDouble(StopLoss*StopMultd,Digits);
               if((OrderType()==OP_BUY)&&((OrderTakeProfit()==0)))
                { 
                  slb=NormalizeDouble(OrderOpenPrice()-SL*Point,Digits);
                  tpb=NormalizeDouble(OrderOpenPrice()+TP*Point,Digits);
                  OrderModify(OrderTicket(),0,slb,tpb,0,CLR_NONE); 
                }
                if((OrderType()==OP_SELL)&&(OrderTakeProfit()==0))
                { 
                  sls=NormalizeDouble(OrderOpenPrice()+SL*Point,Digits);
                  tps=NormalizeDouble(OrderOpenPrice()-TP*Point,Digits);
                  OrderModify(OrderTicket(),0,sls,tps,0,CLR_NONE); 
                }
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