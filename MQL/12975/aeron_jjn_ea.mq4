//+------------------------------------------------------------------+
//|                                       Aeron-AllPairs-2012-v7.mq4 |
//|                                 Copyright © 2010, AeronInfo.Com |
//|                                         http://www.aeroninfo.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, AeronInfo.Com"
#property link      "http://www.aeroninfo.com"
#include <WinUser32.mqh>
extern int MagicNumber=1237322;
extern string Comments="Aeron_JJN";
extern double Lots=0.10;
extern int ResetTime=10;
//---- indicator parameters
string     __Copyright__               = "http://jjnewark.atw.hu";
int        AtrPeriod                   = 8;
extern double     DojiDiff1=0.001;
extern double     DojiDiff2=0.0004;
color      BuyColor                    = YellowGreen;
color      SellColor                   = OrangeRed;
color      FontColor                   = Black;
int        DisplayDecimals             = 4;
int        PosX                        = 25;
int        PosY                        = 25;
bool       SoundAlert                  = false;
extern bool TrailSL=true;
extern int TrailPips=10;
//---- indicator buffers
double Atr;
int POS_n_BUY;
int POS_n_SELL;
int POS_n_BUYSTOP;
int POS_n_SELLSTOP;
int POS_n_BUYLIMIT;
int POS_n_SELLLIMIT;
int POS_n_total;
int int4,x;
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
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double SpreadAditive=0;
   if(Digits==5)
     {
      SpreadAditive=MarketInfo(Symbol(),MODE_SPREAD)/10.0*0.0001;
     }
   if(TrailSL==true)
     {
      int zz;
      for(zz=0;zz<=OrdersTotal()-1;zz++)
        {
         if(OrderSelect(zz,SELECT_BY_POS,MODE_TRADES))
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
               if(OrderType()==OP_BUY)
                  if(Bid-OrderOpenPrice()>=TrailPips*0.0001 && OrderStopLoss()<Bid-TrailPips*0.0001-SpreadAditive)
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TrailPips*0.0001+SpreadAditive,OrderTakeProfit(),0,Yellow))
                        continue;
        }
      for(zz=0;zz<=OrdersTotal()-1;zz++)
        {
         if(OrderSelect(zz,SELECT_BY_POS,MODE_TRADES))
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
               if(OrderType()==OP_SELL)
                  if(OrderOpenPrice()-Ask>=TrailPips*0.0001 && OrderStopLoss()>Ask+TrailPips*0.0001+SpreadAditive)
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TrailPips*0.0001-SpreadAditive,OrderTakeProfit(),0,Yellow))
                        continue;
        }
     }
   if(Digits==4)
     {
      SpreadAditive=MarketInfo(Symbol(),MODE_SPREAD)*0.0001;
     }
//Alert(SpreadAditive);
   for(int int6=0; int6<=OrdersTotal()-1;int6++)
     {
      if(OrderSelect(int6,SELECT_BY_POS,MODE_TRADES))
         if(OrderSymbol()==Symbol() && (OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP) && OrderMagicNumber()==MagicNumber)
            if(TimeCurrent()-OrderOpenTime()>ResetTime*60)
               if(OrderDelete(OrderTicket()))
                  continue;
     }
   int lastbullishindex=0;
   int lastbearishindex=0;
   double lastbearishopen=0;
   double lastbullishopen=0;
   Atr=iATR(NULL,0,AtrPeriod,0);
   if(Close[0]>Open[0] && Open[1]-Close[1]>DojiDiff1) // BUY
     {
      int found=0;
      int w=0;
      while(found<1) // search for the last bearish candle
        {
         if(Close[w]<Open[w] && Open[w]-Close[w]>DojiDiff2)
           {
            lastbearishopen=Open[w];
            lastbearishindex=w;
            found++;
           }
         w++;
        }
     }
   else if(Close[0]<Open[0] && Close[1]-Open[1]>DojiDiff1) // SELL
     {

      found=0;
      w=0;
      while(found<1) // search for the last bullish candle
        {
         if(Close[w]>Open[w] && Close[w]-Open[w]>DojiDiff2)
           {
            lastbullishopen=Open[w];
            lastbullishindex=w;
            found++;
           }
         w++;
        }
     }
   else // NO TRADE
     {
      lastbullishindex=0;
      lastbearishindex=0;
      lastbearishopen=0;
      lastbullishopen=0;
     }
   count_position();
   if(Close[0]>Open[0] && Close[0]<lastbearishopen && Open[1]-Close[1]>DojiDiff1) // BUY
     {
      if(POS_n_BUYSTOP+POS_n_BUY==0)
        {
         if(OrderSend(Symbol(),OP_BUYSTOP,Lots,lastbearishopen+SpreadAditive
            ,2,lastbearishopen-Atr,lastbearishopen+Atr
            ,Comments,MagicNumber,0,Green)>0) {}
            }
           }
         else if(Close[0]<Open[0] && Close[0]>lastbullishopen && Close[1]-Open[1]>DojiDiff1) // SELL
           {
            if(POS_n_SELLSTOP+POS_n_SELL==0)
              {
               if(OrderSend(Symbol(),OP_SELLSTOP,Lots,lastbullishopen-SpreadAditive
                  ,2,lastbullishopen+Atr,lastbullishopen-Atr
                  ,Comments,MagicNumber,0,Red)>0){}
                  }
                 }
               else
                 {
                 }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void count_position()
  {
   POS_n_BUY  = 0;
   POS_n_SELL = 0;
   POS_n_BUYSTOP=0;
   POS_n_SELLSTOP = 0;
   POS_n_BUYLIMIT = 0;
   POS_n_SELLLIMIT= 0;
//---
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false || OrderMagicNumber()!=MagicNumber)
        {
         continue;
        }
         if(OrderType()==OP_BUY)
           {
            POS_n_BUY++;
           }
         else if(OrderType()==OP_SELL)
           {
            POS_n_SELL++;
           }
         if(OrderType()==OP_BUYSTOP)
            {
             POS_n_BUYSTOP++;
            }
         else if(OrderType()==OP_SELLSTOP)
            {
             POS_n_SELLSTOP++;
            }
         if(OrderType()==OP_BUYLIMIT)
            {
             POS_n_BUYLIMIT++;
            }
         else if(OrderType()==OP_SELLLIMIT)
            {
             POS_n_SELLLIMIT++;
            }
     }
     POS_n_total=POS_n_BUY+POS_n_SELL+POS_n_BUYSTOP+POS_n_SELLSTOP;
  }
//+------------------------------------------------------------------+
