//+------------------------------------------------------------------+
//|                                                   MAVA-Xonax.mq4 |
//|                                        Copyright 2015, Xonax.ru. |
//|                                             http://www.xonax.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Xonax.ru."
#property link      "http://www.xonax.ru"
#property version   "1.00"
#property strict
//--- input parameters
input int      periodMA=6;
input int      timeframe=240;
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   double MAopen1,MAclose1,MAopen2,MAclose2,MAmax,MAmin;
   double TakePr,StopL;
   static datetime BarTime=0; //Enter these variables to the calculations were performed 1 time for a specified timeframe 
   datetime TempTime;         // and not every tick
   string simvol="EURUSD";
   int Dig;
   TempTime=iTime(simvol,timeframe,1);
   Dig=_Digits;
   if(BarTime!=TempTime)
     {
      BarTime=TempTime;
      MAopen1=iMA(simvol,timeframe,periodMA,0,MODE_EMA,1,1);
      MAopen2=iMA(simvol,timeframe,periodMA,0,MODE_EMA,1,2);
      MAclose1=iMA(simvol,timeframe,periodMA,0,MODE_EMA,0,1);
      MAclose2=iMA(simvol,timeframe,periodMA,0,MODE_EMA,0,2); //calculate the value of signal indicators
      MAmax=iMA(simvol,timeframe,periodMA,0,MODE_EMA,2,1);
      MAmin=iMA(simvol,timeframe,periodMA,0,MODE_EMA,3,1);
      if(MAopen2>MAclose2 && MAopen1<MAclose1) //under the conditions - buy
        {
         TakePr=NormalizeDouble(MAmax-MAmin,Dig);
         StopL=NormalizeDouble(2*(MAopen1-MAmin),Dig);
         if(StopL>400*_Point) StopL=400*_Point;
         if(TakePr<600*_Point) TakePr=600*_Point;
         int ticket=OrderSend(simvol,OP_BUY,0.1,Ask,30,Ask-StopL,Ask+TakePr,NULL,0,0,clrRed);
         if(ticket<0)
           {
            Print("OrderSend error #",GetLastError());
            BarTime=0;
           }
        }
      if(MAopen2<MAclose2 && MAopen1>MAclose1) //under the conditions - sell
        {
         TakePr=NormalizeDouble(MAmax-MAmin,Dig);
         StopL=NormalizeDouble(2*(MAmax-MAclose1),Dig);
         if(StopL>400*_Point) StopL=400*_Point;
         if(TakePr<600*_Point) TakePr=600*_Point;
         int ticket=OrderSend(simvol,OP_SELL,0.1,Bid,30,Bid+StopL,Bid-TakePr,NULL,0,0,clrBlue);
         if(ticket<0)
           {
            Print("OrderSend error #",GetLastError());
            BarTime=0;
           }
        }
     }
  }
//+------------------------------------------------------------------+
