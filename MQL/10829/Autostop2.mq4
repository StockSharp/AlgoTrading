//+------------------------------------------------------------------+
//|                                              Autostop.mq4 |
//|                            Copyright © 2012, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

extern string    Product="Autostop v2";
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
  
   int StopMultd=10;
   double TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
   double SL=NormalizeDouble(StopLoss*StopMultd,Digits);
   
 //-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
          if((MonitorTakeProfit==true)&&(TakeProfit>0)&&(OrderMagicNumber()==0)){ // monitor tp
          
                // Calculate take profit
                double tpb=NormalizeDouble(OrderOpenPrice()+TP*Point,Digits);
                double tps=NormalizeDouble(OrderOpenPrice()-TP*Point,Digits);
                    
                Comment("Modifying take profit");
                if((OrderType()==OP_BUY)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,OrderStopLoss(),tpb,0,CLR_NONE); }
                if((OrderType()==OP_SELL)&&(OrderTakeProfit()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,OrderStopLoss(),tps,0,CLR_NONE); }

          } // monitor tp
          
          if((MonitorStopLoss==true)&&(StopLoss>0)&&(OrderMagicNumber()==0)){ // monitor sl
          
                   // Calculate stop loss
                   double slb=NormalizeDouble(OrderOpenPrice()-SL*Point,Digits);
                   double sls=NormalizeDouble(OrderOpenPrice()+SL*Point,Digits);

                   Comment("Modifying stop loss");
                   if((OrderType()==OP_BUY)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,slb,OrderTakeProfit(),0,CLR_NONE); }
                   if((OrderType()==OP_SELL)&&(OrderStopLoss()==0)&&(OrderSymbol()==Symbol())){ OrderModify(OrderTicket(),0,sls,OrderTakeProfit(),0,CLR_NONE); }

          }// monitor sl
          Comment("");
        }
     }
}
//----
int Error=GetLastError();
  if(Error==130){Alert("Wrong stops. Retrying."); RefreshRates();}
  if(Error==133){Alert("Trading prohibited.");}
  if(Error==2){Alert("Common error.");}
  if(Error==146){Alert("Trading subsystem is busy. Retrying."); Sleep(500); RefreshRates();}

//----------
   return(0);
  }
//----------

//+-----------------------------------END-------------------------------+
//+---------------------------------------------------------------------+




















































































































int init()
  {
//----
   Alert("Visit www.FxAutomated.com for more goodies!");
//----
   return(0);
  }