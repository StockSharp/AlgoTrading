//+------------------------------------------------------------------+
//|                                                  MA_MirrorEA.mq4 |
//|                                 Copyright © 2010, Thomas Quester |
//|                                                 www.olfolders.de |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Thomas Quester"
#property link      "www.olfolders.de"

#include <ea.mqh>
//---- input parameters
extern int       MovingPeriod=20;
extern int       MovingShift=0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   SetComment("MA_Miror");   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

int Signal(int oldSignal)
{
   return (SignalMA(oldSignal));
}
   
int SignalMA(int oldSignal)
{

  int i=1;
  int signal = oldSignal;
  double ma1, ma2;
  ma1=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_OPEN,i);    
  ma2=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_OPEN,i)-iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,i);
  
  if (ma1 > ma2) signal = OP_BUY;
  if (ma1 < ma2) signal = OP_SELL;
  
  return (signal); 
}

int SignalRandom(int oldSignal)
{
   int signal = oldSignal;
   if (GetNumTickets() == 0)
   {
       int rnd = MathRand();
       if (rnd > 16358)
           signal = OP_BUY;
       else  
           signal = OP_SELL;
   }
   return (signal);
}   

int start()
  {
//----
  
  int    signal,oldSignal;

  oldSignal = -1;
  FindOrders(true);
  if (GetNumTickets() != 0)
  {
     
      oldSignal = GetCommand(0);
  }
  else 
      oldSignal = 1;
      
  signal = Signal(oldSignal);     
  
  if (signal != oldSignal)
  {
      if (GetNumTickets() != 0)
          CloseOrder(GetTicket(0));
      if (signal == OP_BUY)
          Buy(GetLots());
      if (signal == OP_SELL)
          Sell(GetLots());
          
  }  
  
   
//----
   return(0);
  }
//+------------------------------------------------------------------+