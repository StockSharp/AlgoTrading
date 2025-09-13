/*=============================================================
 Info:    Godbot EA   //  Timeframe M5 EURUSD
 Name:    Godbot.mq4
 Author:  Erich Pribitzer
 Version: 1.2.1
 Update:  2011-03-28 
 Notes:   while(true) changed to while (binx<OL_ORDER_BUFFER_SIZE)
 Version: 1.2
 Update:  2011-03-28 
 Notes:   option GB_TRADE_TF added
 Version: 1.1
 Update:  2011-03-28 
 Notes:   Mulit Order added
 Version: 1.0
 Update:  2010-09-12 
 Notes:   ---
  
 Copyright (C) 2011 Erich Pribitzer 
 Email: seizu@gmx.at
=============================================================*/

#property copyright "Copyright © 2011, Erich Pribitzer"
#property link      "http://www.wartris.com"


#include <BasicH.mqh>
#include <OrderLibE.mqh>
#include <OrderLibH.mqh>


// Godbot EA timeframe M5

extern string  GB_SETTINGS      = "==== GODBOT EA V1.2/M5 SETTINGS ====";      //
extern int     GB_TRADE_TF      = PERIOD_M5;
extern int     GB_BANDS_PERIOD  = 23;
extern int     GB_BANDS_DEVIA   = 2;
extern int     GB_BANDS_SHIFT   = 0;
extern int     GB_MA            = 178;
extern int     GB_BANDS_TF      = PERIOD_M30;

extern int     GB_DEMA_PERIOD   = 56;
extern int     GB_DEMA_TF       = PERIOD_D1;
// extern bool    GB_TIMEFILTER    = true;
int            GB_oldBar        = 0;
double         GB_dema[];



int init()
  {
   Print("Init GodBot V1.2");
   BA_Init();
   OL_Init(OL_ALLOW_ORDER,OL_RISK_PERC,OL_RISK_PIPS,OL_PROFIT_PIPS,OL_TRAILING_STOP,OL_LOT_SIZE,OL_INITIAL_LOT,OL_CUSTOM_TICKVALUE,OL_SLIP_PAGE,OL_STOPSBYMODIFY,OL_MAX_LOT,OL_MAX_ORDERS,OL_MAGIC,OL_ORDER_DUPCHECK,OL_OPPOSITE_CLOSE, OL_ORDER_COLOR, OL_MYSQL_LOG);   
   ArrayResize(GB_dema,BA_INIT_BARS);   
   
   return(0);
  }

int deinit()
  {
   OL_Deinit();  
   return(0);
  }

int start()
  {
  
   OL_ReadBuffer();
     
//----

   if(GB_oldBar!=iBars(NULL,GB_TRADE_TF))
   {
      GB_oldBar=iBars(NULL,GB_TRADE_TF);
         
      if(GB_oldBar<BA_INIT_BARS) return;
   
      double bandHi=iBands(NULL,GB_BANDS_TF,GB_BANDS_PERIOD, GB_BANDS_DEVIA,GB_BANDS_SHIFT,PRICE_CLOSE,1,1);
      double bandLow=iBands(NULL,GB_BANDS_TF,GB_BANDS_PERIOD, GB_BANDS_DEVIA,GB_BANDS_SHIFT,PRICE_CLOSE,2,1);
      GB_DEMA(Symbol(),GB_DEMA_TF,GB_DEMA_PERIOD,PRICE_CLOSE,GB_dema);
   
      double open=iOpen(NULL,GB_BANDS_TF,1);
      double close=iClose(NULL,GB_BANDS_TF,1);
      double ma1 = iMA(NULL,GB_BANDS_TF,GB_MA,0,MODE_EMA,PRICE_CLOSE,1);
      double ma2 = iMA(NULL,GB_BANDS_TF,GB_MA,0,MODE_EMA,PRICE_CLOSE,2);
      
   
      bool buyClose=(close < bandHi && open > bandHi);
      bool sellClose=(close > bandLow && open < bandLow); 
      bool buy =(close > bandLow && open < bandLow && GB_dema[0] > GB_dema[1] && GB_dema[1] > GB_dema[2] && ma2  < ma1);
      bool sell = (close < bandHi && open > bandHi && GB_dema[0] < GB_dema[1] && GB_dema[1] < GB_dema[2] && ma2  > ma1);
   
      OL_SyncBuffer();   
   
      // close sell
      int binx=0;
      if(sellClose )
      { 

         while (binx<OL_ORDER_BUFFER_SIZE)
         {   
            binx=OL_enumOrderList(binx,OP_SELL);
            if(binx<0) break;
            OL_setOrderProperty(binx,OL_FLAG,OL_FL_CLOSE);
            Print("CLOSE....SELL"); 
            binx++;
         }
      }

      // close buy
      binx=0;
      if(buyClose)
      {      
    
         while (binx<OL_ORDER_BUFFER_SIZE)
         {   
            binx=OL_enumOrderList(binx,OP_BUY);
            if(binx<0) break;
            OL_setOrderProperty(binx,OL_FLAG,OL_FL_CLOSE);  
            Print("CLOSE....BUY");  
            binx++;
         }
      }



      if(buy)
      {
         Print("ORDER....BUY");
         OL_addOrderProperty(OL_TYPE,OP_BUY);
         //OL_addOrderProperty(OL_OPRICE,Ask);
         OL_addOrderProperty(OL_RISKPERC,0);
         OL_addOrderDescription(OP_BUY);
         if(OL_registerOrder()==-1)
            Print("OL_registerOrder failed!!!");                   
      }
   

      if(sell)
      {
         Print("ORDER....SELL");
         OL_addOrderProperty(OL_TYPE,OP_SELL);
         //OL_addOrderProperty(OL_OPRICE,Bid);
         OL_addOrderProperty(OL_RISKPERC,0);
         OL_addOrderDescription(OP_SELL);
         if(OL_registerOrder()==-1)
            Print("OL_registerOrder failed!!!"); 
      }   
   }

   OL_processOrder();         // Close/Modify or Open order(s) 
   OL_WriteBuffer();  

//----
   return(0);
  }

void GB_DEMA(string symbol, int timeframe, int dema_period, int applied_price,double &buffer[])
{
      static double lastEMA, lastEMA_of_EMA;
      double weight=2.0/(1.0+dema_period);
      double ret=0;
      int limit=BA_INIT_BARS;
      
      
      lastEMA          = iClose(symbol, timeframe, limit);
      lastEMA_of_EMA   = iClose(symbol, timeframe, limit);
      
      limit--;
      
      for(int i=limit; i >= 0; i--)
      {
         lastEMA        =weight*iClose(symbol, timeframe,i)   + (1.0-weight)*lastEMA;
         lastEMA_of_EMA  =weight*lastEMA   + (1.0-weight)*lastEMA_of_EMA;        
         buffer[i]=(2.0*lastEMA - lastEMA_of_EMA);
      } 
}   