//+------------------------------------------------------------------+
//|                         Doji Arrows                              |
//|                                                                  |
//+------------------------------------------------------------------+
/*
This Expert was mand from an indicator made by Chris (HyPip).
I do not know if it will place a trade, you will have to debug it yourself.
I made it to simply show everyone how easy it is to create an Expert.
Every week I hear "I am not a programmer please make this"
or "I do not know how"
The truth is I do not know how either, but I've made 40 or 50 experts in recent months
many from indicators just like this one. All I can tell you is CUT AND PASTE
Enjoy and debug.
bobammax
*/
#property copyright ""
#property link      ""
//----
#property indicator_buffers 4
#property indicator_color1 Red
#property indicator_color2 Blue
#property indicator_color3 Blue
#property indicator_color4 Red
//----
extern double TakeProfit=25;
extern double Lots=1;
extern double TrailingStop=10;
extern double InitialStop=20;
//----
extern int slip=0;//exits only
extern double lp=300;
extern double sp=30;
datetime BarTime;
//---- input parameters
//extern int RISK=3;
extern double thresholdB=0.0001;
extern double thresholdS=-0.0001;
extern int SSP=9;
extern int CountBars=2000;
//---- buffers
double val1[];
double val2[];
double val3[];
double val4[];
double red0,red1,red2;
double blue0,blue1,blue2;
double cci0,cci1,rsi0,dpo0,dpo1;
double plusdi,minusdi,main;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
   string short_name;
//---- indicator line
   IndicatorBuffers(4);
   SetIndexStyle(0,DRAW_ARROW);
   SetIndexArrow(0,234);
   //
   SetIndexStyle(1,DRAW_ARROW);
   SetIndexArrow(1,233);
   //
   SetIndexStyle(2,DRAW_ARROW);
   SetIndexArrow(2,253);
   //
   SetIndexStyle(3,DRAW_ARROW);
   SetIndexArrow(3,253);
   //
   SetIndexBuffer(0,val1);
   SetIndexBuffer(1,val2);
   SetIndexBuffer(2,val3);
   SetIndexBuffer(3,val4);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Doji Arrows                                                      |
//+------------------------------------------------------------------+
int start()
  {
   if(BarTime==Time[0]) {return(0);}
   //new bar, update bartime
   BarTime=Time[0];
//----
   SetIndexDrawBegin(0,Bars-CountBars+SSP+1);
   SetIndexDrawBegin(1,Bars-CountBars+SSP+1);
   int i,counted_bars=IndicatorCounted();
   int K;
   bool uptrend,downtrend,ExitBuy,ExitSell,old,old2,old3,old4;
//----
   if(Bars<=SSP+1) return(0);
//---- initial zero
   if(counted_bars<SSP+1)
     {
      for(i=1;i<=0;i++) val1[CountBars-i]=0.0;
      for(i=1;i<=0;i++) val2[CountBars-i]=0.0;
      for(i=1;i<=0;i++) val3[CountBars-i]=0.0;
      for(i=1;i<=0;i++) val4[CountBars-i]=0.0;
     }
//----
   i=CountBars-SSP-1;
   while(i>=0)
     {
      //dpo1=iCustom(NULL,0,"DPO",7,800,0,i+1);
      val1[i]=0.0;
      val2[i]=0.0;
      val3[i]=0.0;
      val4[i]=0.0;
      //buy signal
      if ((Close[i+2]==Open[i+2]) &&
      (Close[i+1]>High[i+2]) /*&&
      (iCCI(NULL,0,14,PRICE_CLOSE,i)>100)*/)
         uptrend=true; //else uptrend=false;
      if ((!(Close[i+2]==Open[i+2])) &&
         (!(Close[i+1]>High[i+2])))
         uptrend=false;
      if ((! uptrend==old) && (uptrend==true))
        {
         //PlaySound("alert.wav");
         val2[i]=Low[i]-5*Point;
         //Alert(TimeMonth(CurTime()),"/",TimeDay(CurTime())," at ",TimeHour(CurTime()),":",TimeMinute(CurTime()),"   -   Possible buy on ",Symbol()," ", Period());
        }
      //sell signal
      if ((Close[i+2]==Open[i+2]) &&
      (Close[i+1]<Low[i+2]) /*&&
      (iCCI(NULL,0,14,PRICE_CLOSE,i)<-100)*/)
         downtrend=true; //else downtrend=false;
      if ((!(Close[i+2]==Open[i+2])) &&
         (!(Close[i+1]<Low[i+2])))
         downtrend=false;
      if ((! downtrend==old2) && (downtrend==true))
        {
         //PlaySound("alert.wav");
         val1[i]=High[i]+5*Point;
         //Alert(TimeMonth(CurTime()),"/",TimeDay(CurTime())," at ",TimeHour(CurTime()),":",TimeMinute(CurTime()),"   -   Possible buy on ",Symbol()," ", Period());
        }
      old=uptrend;
      old2=downtrend;
      old3=ExitBuy;
      old4=ExitSell;
      i--;
      // order section//----------------------------
      int OrderForThisSymbol=0;
        for(int x=0;x<OrdersTotal();x++)
        {
         OrderSelect(x, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==Symbol()) OrderForThisSymbol++;
        }//end for
        if(uptrend==true&& OrderForThisSymbol==0)
        {
         Alert("Buy order triggered for "+Symbol()+" on the "+Period()+" minute chart.");
         int BuyOrderTicket=OrderSend(Symbol(),OP_BUY,Lots,Ask,slip,Ask-(InitialStop*Point),Ask+(TakeProfit*Point),"Buy Order placed at "+CurTime(),0,0,Green);
        }
        if(downtrend==true && OrderForThisSymbol==0)
        {
         Alert("Sell order triggered for "+Symbol()+" on the "+Period()+" minute chart.");
         int SellOrderTicket=OrderSend(Symbol(),OP_SELL,Lots,Bid,slip,Bid+(InitialStop*Point),Bid-(TakeProfit*Point),"Sell Order placed at "+CurTime(),0,0,Red);
        }
         {
         }
      // ---------------- TRAILING STOP
      if(TrailingStop>0)
        {
         OrderSelect(0, SELECT_BY_POS, MODE_TRADES);
         if((OrderType()==OP_BUY || OrderType()==OP_BUYSTOP) &&
    (OrderSymbol()==Symbol()))
           {
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-
Point*TrailingStop,OrderTakeProfit(),0,Aqua);
                     return(0);
                    }
                 }
              }
           }
         if((OrderType()==OP_SELL || OrderType()==OP_SELLSTOP) &&
    (OrderSymbol()==Symbol()))
           {
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  if(OrderStopLoss()==0.0 ||
                     OrderStopLoss()>(Ask+Point*TrailingStop))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice
(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Aqua);
                     return(0);
                    }
                 }
              }
           }
        }
     }
  }
//----  
   return(0);
//+------------------------------------------------------------------+