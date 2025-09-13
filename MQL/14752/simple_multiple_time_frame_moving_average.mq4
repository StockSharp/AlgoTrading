
//+------------------------------------------------------------------+
//|                    Simple Multiple Time Frame Moving Average.mq4 |
//|                             Forest Kirschbaum: forest4k@gmail.com|
//|                                           2016, Forest Kirschbaum|
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
//Inspired by:
//+------------------------------------------------------------------+
//|                                               Moving Average.mq4 |
//|                   Copyright 2005-2014, MetaQuotes Software Corp. |
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                       AllAverages_v2.5_Stats.mq4 |
//|                             Copyright © 2007-09, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//|     Statistics module modification Copyright © 2010 Nakagava Ltd.|
//|                                   http://www.prekybaforex.lt     |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                                       RSI_MA.mq4 |
//|                   Copyright © 2008,  AEliseev k800elik@gmail.com |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright   "2016, Forest Kirschbaum"
#property link        "http://www.mql4.com"
#property description "Simple Multiple Time Frame Moving Average"

#define MAGICMA  2016
//--- Inputs
input double Lots          =0.01;
input int    MovingPeriod  =5;
input int    MovingShift   =0;
input double slippage = 10;


//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//---
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//--- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break

//--- return lot size
   if(lot<0.01) lot=0.01;
   return(lot);
  }
  



//+----------------------------------------------------------------------------------------------------------------------+
//| Check for open order conditions -- copy the moving average out of here into the close section to test more ma's      |
//+----------------------------------------------------------------------------------------------------------------------+
void CheckForOpen()
  {
   int    res;
   if(Volume[0]>1) return;

 
//--MAMonthly----------------------------------------------------------------------------------------------------------------  
   double mam[100],mamclose[100];
   int    km,limitmam=ArraySize(mam);
//--- go trading only for first tiks of new bar

   
  //--- get ma
   for(km=0; km<limitmam; km++)
      {
         mam[km]=iMA(NULL,PERIOD_MN1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,km);
         mamclose[km]=iMA(NULL,PERIOD_MN1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,km);
      }
    
//MAWeekly----------------------------------------------------------------------------------------------------------------      
   double maw[100],mawclose[100];
   int    kw,limitmaw=ArraySize(maw);
   
  //--- get ma
   for(kw=0; kw<limitmaw; kw++)
      {
         maw[kw]=iMA(NULL,PERIOD_W1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kw);
         mawclose[kw]=iMA(NULL,PERIOD_W1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kw);
      }
   
//MADaily----------------------------------------------------------------------------------------------------------------      
   double mad[100],madclose[100];
   int    kd,limitmad=ArraySize(mad);
   
  //--- get ma
   for(kd=0; kd<limitmad; kd++)
      {
         mad[kd]=iMA(NULL,PERIOD_D1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kd);
         madclose[kd]=iMA(NULL,PERIOD_D1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kd);
      }
      
//MAFourHour--------------------------------------------------------------------------------------------------------------      
   double mafh[100],mafhclose[100];
   int    kfh,limitmafh=ArraySize(mafh);

   
  //--- get ma
   for(kfh=0; kfh<limitmafh; kfh++)
      {
         mafh[kfh]=iMA(NULL,PERIOD_H4,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kfh);
         mafhclose[kfh]=iMA(NULL,PERIOD_H4,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kfh);
      }      
    
//MAOneHour--------------------------------------------------------------------------------------------------------------      
   double mah[100],mahclose[100];
   int    kh,limitmah=ArraySize(mah);
   
  //--- get ma
   for(kh=0; kh<limitmah; kh++)
      {
         mah[kh]=iMA(NULL,PERIOD_H1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kh);
         mahclose[kh]=iMA(NULL,PERIOD_H1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kh);
      }      
      
//MAThirtyMinutes--------------------------------------------------------------------------------------------------------      
   double mat[100],matclose[100];
   int    kt,limitmat=ArraySize(mat);
   
  //--- get ma
   for(kt=0; kt<limitmat; kt++)
      {
         mat[kt]=iMA(NULL,PERIOD_M30,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kt);
         matclose[kt]=iMA(NULL,PERIOD_M30,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kt);
      }      


//MAFifteenMinutes--------------------------------------------------------------------------------------------------------      
   double maft[100],maftclose[100];
   int    kft,limitmaft=ArraySize(maft);
   
  //--- get ma
   for(kft=0; kft<limitmaft; kft++)
      {
         maft[kft]=iMA(NULL,PERIOD_M15,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kft);
         maftclose[kft]=iMA(NULL,PERIOD_M15,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kft);
      } 



//--- sell conditions

   if ((mah[1]<mah[2]) && (mafh[1]<mafh[2]) ) 
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,slippage,0,0,"",MAGICMA,0,Red);
      return;
     }
//--- buy conditions

     if ((mah[1]>mah[2]) && (mafh[1]>mafh[2]) ) 
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,slippage,0,0,"",MAGICMA,0,Blue);
      return;
     }
//---
  }



//+----------------------------------------------------------------------------------------------------------+
//| Check for close order conditions, check for this before open order, so bot can switch from buy to sell   |
//+----------------------------------------------------------------------------------------------------------+

void CheckForClose()
  {

//--MAMonthly----------------------------------------------------------------------------------------------------------------  
   double mam[100],mamclose[100];
   int    km,limitmam=ArraySize(mam);
//--- go trading only for first tiks of new bar

   
  //--- get ma
   for(km=0; km<limitmam; km++)
      {
         mam[km]=iMA(NULL,PERIOD_MN1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,km);
         mamclose[km]=iMA(NULL,PERIOD_MN1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,km);
      }
    
//MAWeekly----------------------------------------------------------------------------------------------------------------      
   double maw[100],mawclose[100];
   int    kw,limitmaw=ArraySize(maw);
   
  //--- get ma
   for(kw=0; kw<limitmaw; kw++)
      {
         maw[kw]=iMA(NULL,PERIOD_W1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kw);
         mawclose[kw]=iMA(NULL,PERIOD_W1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kw);
      }
   
//MADaily----------------------------------------------------------------------------------------------------------------      
   double mad[100],madclose[100];
   int    kd,limitmad=ArraySize(mad);
   
  //--- get ma
   for(kd=0; kd<limitmad; kd++)
      {
         mad[kd]=iMA(NULL,PERIOD_D1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kd);
         madclose[kd]=iMA(NULL,PERIOD_D1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kd);
      }
      
//MAFourHour--------------------------------------------------------------------------------------------------------------      
   double mafh[100],mafhclose[100];
   int    kfh,limitmafh=ArraySize(mafh);

   
  //--- get ma
   for(kfh=0; kfh<limitmafh; kfh++)
      {
         mafh[kfh]=iMA(NULL,PERIOD_H4,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kfh);
         mafhclose[kfh]=iMA(NULL,PERIOD_H4,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kfh);
      }      
    
//MAOneHour--------------------------------------------------------------------------------------------------------------      
   double mah[100],mahclose[100];
   int    kh,limitmah=ArraySize(mah);
   
  //--- get ma
   for(kh=0; kh<limitmah; kh++)
      {
         mah[kh]=iMA(NULL,PERIOD_H1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kh);
         mahclose[kh]=iMA(NULL,PERIOD_H1,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kh);
      }      
      
//MAThirtyMinutes--------------------------------------------------------------------------------------------------------      
   double mat[100],matclose[100];
   int    kt,limitmat=ArraySize(mat);
   
  //--- get ma
   for(kt=0; kt<limitmat; kt++)
      {
         mat[kt]=iMA(NULL,PERIOD_M30,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kt);
         matclose[kt]=iMA(NULL,PERIOD_M30,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kt);
      }      


//MAFifteenMinutes--------------------------------------------------------------------------------------------------------      
   double maft[100],maftclose[100];
   int    kft,limitmaft=ArraySize(maft);
   
  //--- get ma
   for(kft=0; kft<limitmaft; kft++)
      {
         maft[kft]=iMA(NULL,PERIOD_M15,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kft);
         maftclose[kft]=iMA(NULL,PERIOD_M15,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,kft);
      } 
 
//--- go trading only for first tiks of new bar
   if(Volume[0]>1) return;

//---
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //--- check order type 
      if(OrderType()==OP_BUY)
        {
        if ((mah[1]<mah[2]) || (mafh[1]<mafh[2]) )
           {
            if(!OrderClose(OrderTicket(),OrderLots(),Bid,3,White))
               Print("OrderClose error ",GetLastError());
           }
         break;
        }
      if(OrderType()==OP_SELL)
        {
        if ((mah[1]>mah[2]) || (mafh[1]>mafh[2]) ) 
           {
            if(!OrderClose(OrderTicket(),OrderLots(),Ask,3,White))
               Print("OrderClose error ",GetLastError());
           }
         break;
        }
     }
//---
  }
 
//+------------------------------------------------------------------+
//| OnTick function                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false)
      return;
//--- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//---
  }
//+------------------------------------------------------------------+
