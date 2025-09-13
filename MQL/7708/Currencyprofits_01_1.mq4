//+------------------------------------------------------------------+
//|                                              TrendScalper_TR.mq4 |
//|                      Copyright © 2004, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
#property copyright "Testing"
#property link      "http://www.metaquotes.net/"
//---- input parameters
extern double    LotsIfNoMM=0.1;
extern int       StopLoss=50;
extern int       Slip=5;
extern int       MM_Mode=0;
extern int       MM_Risk=40;
//----
double Opentrades,orders,first,mode,Ilo,sym,b,tmp,GridCellPoint,OpenOrderStopLoss;
double b4signal,Signal,Triggerline,b4Triggerline,Nowsignal,NowTriggerline,sl,LastOpByExpert,LastBarChecked;
int cnt,cnt2,OpenPosition,notouchbar,PendingOrderTicket,OpenOrderTicket;
bool test;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//---- 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   //return(0);
   //if ( ! IsTesting() ) Comment(" Trailingstop    ",  b, "\n","      Tick no. ", iVolume(NULL,0,0),"\n"," Lots    ",Ilo);
   /**********************************Money and Risk Management***************************************
   Changing the value of mm will give you several money management options
   mm = 0 : Single 1 lot orders.
   mm = -1 : Fractional lots/ balance X the risk factor.(use for Mini accts)
   mm = 1 : Full lots/ balance X the risk factor up to 100 lot orders.(use for Regular accounts)
   ***************************************************************************************************
   RISK FACTOR:
   risk can be anything from 1 up. 
   Factor of 5 adds a lot for every $20,000.00 added to the balance. 
   Factor of 10 adds a lot with every $10.000.00 added to the balance.
   The higher the risk,  the easier it is to blow your margin..
   **************************************************************************************************/
     if (MM_Mode < 0)  
     {
      Ilo=MathCeil(AccountBalance()*MM_Risk/10000)/10;
        if (Ilo > 100) 
        {
         Ilo=100;
        }
      }
       else 
      {
      Ilo=LotsIfNoMM;
     }
   if (MM_Mode > 0)
     {
      Ilo=MathCeil(AccountBalance()*MM_Risk/10000)/10;
      if (Ilo > 1)
        {
         Ilo=MathCeil(Ilo);
        }
      if (Ilo < 1)
        {
         Ilo=1;
        }
      if (Ilo > 100)
        {
         Ilo=100;
        }
     }
   Opentrades=0;
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      if(OrderSelect (cnt, SELECT_BY_POS)==false) continue;
      if(OrderSymbol()==Symbol())
        {
         Opentrades=Opentrades+1;
        }
     }
   if (Opentrades!=0)
     {
      if (OrderType()==OP_BUY)
        {
         if (Bid>=iHigh(Symbol(),0,Highest(Symbol(),0,MODE_HIGH,6,1)))
           {
            OrderClose(OrderTicket(),OrderLots(),Bid,Slip,Red);
            Opentrades--;
           }
        }
      else if (OrderType()==OP_SELL)
           {
            if (Bid<=iLow(Symbol(),0,Lowest(Symbol(),0,MODE_LOW,6,1)))
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,Slip,Red);
               Opentrades--;
              }
           }
     }
   if (Opentrades==0)
     {
      if (iMA(Symbol(),0,6,0,MODE_EMA,PRICE_CLOSE,1)>iMA(Symbol(),0,12,0,MODE_SMA,PRICE_CLOSE,1) && Bid<=iLow(Symbol(),0,Lowest(Symbol(),0,MODE_LOW,6,1)))
        {
         OrderSend(Symbol(),OP_BUY,Ilo,Ask,Slip,Ask-StopLoss*Point,0,"CP",0,0,Blue);
        }
      if (iMA(Symbol(),0,6,0,MODE_EMA,PRICE_CLOSE,1)<iMA(Symbol(),0,12,0,MODE_SMA,PRICE_CLOSE,1) && Bid>=iHigh(Symbol(),0,Highest(Symbol(),0,MODE_HIGH,6,1)))
        {
         OrderSend(Symbol(),OP_SELL,Ilo,Bid,Slip,Bid+StopLoss*Point,0,"CP",0,0,Blue);
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+