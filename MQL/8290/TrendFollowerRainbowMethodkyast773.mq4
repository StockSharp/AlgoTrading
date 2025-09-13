//+------------------------------------------------------------------+
//|                                            TrendFollowerV001.mq4 |
//|                                                  Simon Schwegler |
//|                                                sschwegler@gmx.net|
//|            Edited By : yast77 at 04,sept,2008                    |
//|            Reason    : Remove warning when compiling             |
//+------------------------------------------------------------------+
#property link      "http://www.metaquotes.net"


//---- input parameters
extern int       iBarsAfterSignal=2;//2;
extern int       iTakeProfit=17;
extern int       iStopLoss=30;
extern int       iTrailingStop=45;

extern int       iStartTradingHour=1;//8;
extern int       iEndTradingHour=23;//18;

//Macd-Parameters
double dMacd1 = 5;
double dMacd2 = 35;
double dMacd3 = 5;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
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
//| Calculate Optimal Lot Size                                       |
//+------------------------------------------------------------------+  
double LotsOptimized()
{
   double dFreeMargin = AccountFreeMargin();
   double dRisk = dFreeMargin/10;
   double Lots = 0.1;//dRisk/(50*iStopLoss);
   return(Lots);
}
//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//---- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
  } 
//+------------------------------------------------------------------+
//| Macd entry Condition                                            |
//+------------------------------------------------------------------+
int MacdEntryCondition()
  {
  
   double dMacdMain = iMACD(Symbol(),NULL,dMacd1,dMacd2,dMacd3,PRICE_CLOSE,MODE_MAIN,1);
   double dMacdSignal = iMACD(Symbol(),NULL,dMacd1,dMacd2,dMacd3,PRICE_CLOSE,MODE_SIGNAL,1);
   
   if((dMacdMain>0) && (dMacdSignal>0)) return (1);   //long
   if((dMacdMain<0) && (dMacdSignal<0)) return (-1);  //short
   else return (0);                                   //neutral
  }
//+------------------------------------------------------------------+
//| Laguerre entry Condition                                             |
//+------------------------------------------------------------------+ 
int LaguerreEntryCondition()
  {
   if( (iCustom(Symbol(),NULL,"Laguerre",0,1)> 0.15) && (iCustom(Symbol(),NULL,"Laguerre",0,2) < 0.15) )
   {
    return (1);
   }
   if( (iCustom(Symbol(),NULL,"Laguerre",0,1) < 0.75) && (iCustom(Symbol(),NULL,"Laguerre",0,2) > 0.75) )
   {
    return (-1);
   }
   else return (0);
  }

//+------------------------------------------------------------------+
//| Ema Crossover                                                    |
//+------------------------------------------------------------------+
int EmaCrossover()
{
   //Calculate the last 2 bars before now
   int iLastSignals[];
   double fasterEMAnow, fasterEMAprevious, fasterEMAafter;
   double slowerEMAnow, slowerEMAprevious, slowerEMAafter;
   
      fasterEMAnow = iMA(NULL, 0, 4, 0, MODE_EMA, PRICE_CLOSE, 1);
      fasterEMAprevious = iMA(NULL, 0, 4, 0, MODE_EMA, PRICE_CLOSE, 2);
      //fasterEMAafter = iMA(NULL, 0, 4, 0, MODE_EMA, PRICE_CLOSE, 3);

      slowerEMAnow = iMA(NULL, 0, 8, 0, MODE_EMA, PRICE_CLOSE, 1);
      slowerEMAprevious = iMA(NULL, 0, 8, 0, MODE_EMA, PRICE_CLOSE, 2);
      //slowerEMAafter = iMA(NULL, 0, 8, 0, MODE_EMA, PRICE_CLOSE, 3);
      
      if ((fasterEMAnow > slowerEMAnow) && (fasterEMAprevious < slowerEMAprevious))
      {
       return (1);
      }
      if ((fasterEMAnow < slowerEMAnow) && (fasterEMAprevious > slowerEMAprevious))
      {
       return (-1);
      }
   
   return (0);
    
}
//+------------------------------------------------------------------+
//| Check for Rainbow conditions                                     |
//+------------------------------------------------------------------+
int RainbowEntryCondition()
{
 //Red
 double rb00 = iCustom(Symbol(),NULL,"RainbowMMA_01",0,1);
 double rb01 = iCustom(Symbol(),NULL,"RainbowMMA_01",1,1);
 double rb02 = iCustom(Symbol(),NULL,"RainbowMMA_01",2,1);
 double rb03 = iCustom(Symbol(),NULL,"RainbowMMA_01",3,1);
 
 double rb10 = iCustom(Symbol(),NULL,"RainbowMMA_02",0,1);
 double rb11 = iCustom(Symbol(),NULL,"RainbowMMA_02",1,1);
 double rb12 = iCustom(Symbol(),NULL,"RainbowMMA_02",2,1);
 double rb13 = iCustom(Symbol(),NULL,"RainbowMMA_02",3,1);
 
 double rb20 = iCustom(Symbol(),NULL,"RainbowMMA_03",0,1);
 double rb21 = iCustom(Symbol(),NULL,"RainbowMMA_03",1,1);
 
 //Green
 double rb22 = iCustom(Symbol(),NULL,"RainbowMMA_03",2,1);
 double rb23 = iCustom(Symbol(),NULL,"RainbowMMA_03",3,1);
 
 double rb30 = iCustom(Symbol(),NULL,"RainbowMMA_04",0,1);
 double rb31 = iCustom(Symbol(),NULL,"RainbowMMA_04",1,1);
 double rb32 = iCustom(Symbol(),NULL,"RainbowMMA_04",2,1);
 double rb33 = iCustom(Symbol(),NULL,"RainbowMMA_04",3,1);
 
 double rb40 = iCustom(Symbol(),NULL,"RainbowMMA_05",0,1);
 double rb41 = iCustom(Symbol(),NULL,"RainbowMMA_05",1,1);
 double rb42 = iCustom(Symbol(),NULL,"RainbowMMA_05",2,1);
 double rb43 = iCustom(Symbol(),NULL,"RainbowMMA_05",3,1);
 
 //Calculate the Rainbows
 if (rb00 >= rb01 && rb01 >= rb02 && rb02 >= rb03)
 {
  if (rb10 >= rb11 && rb11 >= rb12 && rb12 >= rb13)
  {
   if (rb20 >= rb21 && rb21 >= rb22 && rb22 >= rb23)
   {
    if (rb30 >= rb31 && rb31 >= rb32 && rb32 >= rb33)
    {
     if (rb40 >= rb41 && rb41 >= rb42 && rb42 >= rb43)
     return (1);
    }
   }
  }
 }
 if (rb00 <= rb01 && rb01 <= rb02 && rb02 <= rb03)
 {
  if (rb10 <= rb11 && rb11 <= rb12 && rb12 <= rb13)
  {
   if (rb20 <= rb21 && rb21 <= rb22 && rb22 <= rb23)
   {
    if (rb30 <= rb31 && rb31 <= rb32 && rb32 <= rb33)
    {
     if (rb40 <= rb41 && rb41 <= rb42 && rb42 <= rb43)
     return (-1);
    }
   }
  }
 }
 return (0);
}
//+------------------------------------------------------------------+
//| Check for MFI Condition  (Against)                               |
//+------------------------------------------------------------------+
int MFIEntryCondition()
{
double MFI = iMFI(NULL,0,13,1);

   if (iMFI(Symbol(),NULL,14,1) < 40)
      return (1);
   if (iMFI(Symbol(),NULL,14,1) > 60)
      return (-1);
}
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   
   if (Hour() <= iStartTradingHour || Hour() >= iEndTradingHour) return;
   
   //Check for Ema signal
   if( EmaCrossover() == 0 ) return; 
   
   if (EmaCrossover() == 1)
   {
      if ( (MacdEntryCondition() == 1) && (LaguerreEntryCondition() == 1) && (RainbowEntryCondition() == 1) && (MFIEntryCondition() == 1))
      {
      OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,Ask-iStopLoss*Point,Ask+iTakeProfit*Point,"",1337,0,Green);
      }
   }
   if (EmaCrossover() == -1)
   {
      if ( (MacdEntryCondition() == -1) && (LaguerreEntryCondition() == -1) && (RainbowEntryCondition() == -1) && (MFIEntryCondition() == -1))
      {
      OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Bid+iStopLoss*Point,Bid-iTakeProfit*Point,"",1337,0,Red);
      }
   }
   return;
   
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {

//---- check for history and trading
   if (Bars<100 || IsTradeAllowed()==false) return;
      

//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   
   if (CalculateCurrentOrders(Symbol()) != 0)
   {
      if(OrderType()==OP_BUY)
      {
         if(Bid-OrderOpenPrice()>Point*iTrailingStop)
         {
            if(OrderStopLoss()<Bid-Point*iTrailingStop)
            {
               OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*iTrailingStop,OrderTakeProfit(),0,Blue);
            }
         }
      }
      if(OrderType()==OP_SELL)
      {
         if(OrderOpenPrice()-Ask > Point*iTrailingStop)
         {
            if(OrderStopLoss() > Ask+Point*iTrailingStop)
            {
               OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*iTrailingStop,OrderTakeProfit(),0,Blue);
            }
         }
      }
   }
//----
   Comment("Order buy: ",OP_BUY,"\nOrder Sell: ",OP_SELL);
   /*Comment("Line1: ",iCustom(Symbol(),PERIOD_M5,"RainbowMMA_01",0,0),"\n",
            "Line2: ",iCustom(Symbol(),PERIOD_M5,"RainbowMMA_01",1,0),"\n",
            "Line3: ",iCustom(Symbol(),PERIOD_M5,"RainbowMMA_01",2,0),"\n",
            "Line4: ",iCustom(Symbol(),PERIOD_M5,"RainbowMMA_01",3,0),"\n");*/
//----
   return(0);
  }
//+------------------------------------------------------------------+