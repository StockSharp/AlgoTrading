//+------------------------------------------------------------------+
//|                                                   MACD trader.mq4|
//|                                                           Fuccer |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Fuccer"
#property link      ""
#include <stdlib.mqh>
//---- input parameters
//extern double TP=20;
//extern double SL=40;//lots to operate
extern int Trail=20;
extern double lots=0.1;
extern int StartFromHour=8;
extern int EndToHour=16;
extern int metric=20;
/*
          если время (8 до 16)
          то если цена вошла в сз - буй, стопселл; пауза 10 сек
          если цена вошла в кз - селл, стопбуй; пауза 10 сек
          если цена задела ревлонг - клозе буй; пауза 10 сек
          если цена задела ревшорт - клозе селл; пауза 10 сек
          если ордера уже есть
          то  (по ордерам)если бай и валюта тру и цена>стоплоса на трейл - корректировать стоплос вверх; пауза 10 сек
          если селл и валюта тру и цена<стоплоса на трейл - корректировать стоплос вниз; пауза 10 сек
          иначе закрыть все ордера нах
*/
int curh,tld;
double cs2,cs1,cnow;
bool pend=false;
int action=0;
int stp=0;
double rtl=0,rts=0;
double prevtick=0,lwr,lwb,lrr,lbb;
bool tenb=true;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
{
   prevtick=Close[0];
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
     if (curh!=Time[0])
     {
                 //- reversal prediction high(low) = camarilla RES&SUP c+(h-l)*1.1/4),c-(h-l)*1.1/4) 
                 //- центр трехцветки = camarilla pivot
                 //- maximum overbot/oversold = classic pivots R3,S3
                 //- пропорции трехцветки в пипсах: 20:40:20 - общее всегда 80 (для фунта наверно гад подгонял)
      tenb=true; // one time per current bar one trade is allowed
      double pivot=(iHigh(Symbol(),1440,1)+iLow(Symbol(),1440,1)+iClose(Symbol(),1440,1))/3;
      lwb=NormalizeDouble(pivot+metric*Point,4);
      lbb=NormalizeDouble(pivot+2*metric*Point,4);
      lwr=NormalizeDouble(pivot-metric*Point,4);
      lrr=NormalizeDouble(pivot-2*metric*Point,4);
      rtl=NormalizeDouble(MathMax(iClose(Symbol(),1440,1)+(iHigh(Symbol(),1440,1)-iLow(Symbol(),1440,1))*1.1/2,lrr-20*Point),4);
      rts=NormalizeDouble(MathMin( iClose(Symbol(),1440,1)-(iHigh(Symbol(),1440,1)-iLow(Symbol(),1440,1))*1.1/2,lrr-20*Point),4);
//----
      string cmt="Current levelz: \n Long RP: "+rtl+"\n Long enter: "+lwb+"\n Short enter: "+lwr+"\n Short RP: "+rts;
      cmt= cmt+"\n Time: "+TimeHour(Time[0]);
      if ((TimeHour(Time[0])<StartFromHour)||(TimeHour(Time[0])>EndToHour)) {cmt= cmt+ " (offmarket)";}
      if (!IsTesting()) Comment(cmt);
     curh=Time[0];
     }
   if ((TimeHour(Time[0])>=StartFromHour)&&(TimeHour(Time[0])<=EndToHour))
     {
      if ((prevtick<lwb)&&(Close[0]>=lwb)) iecsol();
      if ((prevtick>lwr)&&(Close[0]<=lwr)) ieclos();
      trailstop();
     }
   if ((TimeHour(Time[0])<StartFromHour)||(TimeHour(Time[0])>EndToHour)) {closeall();}
//----
   prevtick=Close[0];
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int closeall()
  {
   bool ordex=false;
   if (OrdersTotal()>0)
     {
      int num=0;
      for(num=0;num<=OrdersTotal();num++)
        {
         OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
         if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol()))
           {
            if (OrderType()==OP_SELL)         {OrderClose(OrderTicket(),OrderLots(),Ask,3,Teal);Sleep(5000);return(0);}
            if (OrderType()==OP_BUY)          {OrderClose(OrderTicket(),OrderLots(),Bid,3,Teal);Sleep(5000);return(0);}
            if (OrderType()==OP_SELLSTOP)     {OrderDelete(OrderTicket());Sleep(5000);return(0);}
            if (OrderType()==OP_SELLLIMIT)    {OrderDelete(OrderTicket());Sleep(5000);return(0);}
            if (OrderType()==OP_BUYSTOP)      {OrderDelete(OrderTicket());Sleep(5000);return(0);}
            if (OrderType()==OP_BUYLIMIT)     {OrderDelete(OrderTicket());Sleep(5000);return(0);}
           return(0);
           }
         }
       }
     }
/*//=============================
int bstop(double bslev)
{
bool ordex=false;
   if (OrdersTotal()>0)
   {
   int num=0;
      for (num=0;num<=OrdersTotal();num++)
      {
         OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
            if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&((OrderType()==OP_BUY)||(OrderType()==OP_BUYSTOP))) ordex=true;
        }
      }
   if (ordex==false)
   { 
   int ticket=OrderSend(Symbol(),OP_BUYSTOP,lots,bslev,3,bslev-SL*Point,bslev+TP*Point,DoubleToStr(Period(),0),0,0,Blue);
   if (ticket<0) Comment(ErrorDescription(GetLastError()));Sleep(20000);
   }
return(0);
 } 
//=============================
int sstop(double sslev)
{
bool ordex=false;
   if (OrdersTotal()>0)
   {
   int num=0;
      for (num=0;num<=OrdersTotal();num++)
      {
         OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
            if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&((OrderType()==OP_SELL)||(OrderType()==OP_SELLSTOP))) ordex=true;
      }
    }
if (ordex==false)
   { 
   int ticket=OrderSend(Symbol(),OP_SELLSTOP,lots,sslev,3,sslev+SL*Point,sslev-TP*Point,DoubleToStr(Period(),0),0,0,Blue);
   if (ticket<0) Comment(ErrorDescription(GetLastError()));Sleep(10000);
   }
   return(0);
 } 
//=============================
*/
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int iecsol()
  {
   bool ordex=false;
   if (OrdersTotal()>0)
     {
      int num=0;
      for(num=0;num<=OrdersTotal();num++)
        {
         OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
         if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&(OrderType()==OP_BUY)) {tenb=false;return(0);}
         if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&(OrderType()==OP_SELL)){tenb=false;return(0);}
        }
      }
   if ((ordex==false)&&(tenb==true))
     { int ticket=OrderSend(Symbol(),OP_BUY,lots,Ask,3,lwr,rtl,DoubleToStr(Period(),0),0,0,Blue);
     Sleep(10000);tenb=false;
     }
   }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int ieclos()
  {
   bool ordex=false;
   if (OrdersTotal()>0)
     {
      int num=0;
      for(num=0;num<=OrdersTotal();num++)
        {
         OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
         if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&(OrderType()==OP_SELL)) {tenb=false;return(0);}
         if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&(OrderType()==OP_BUY))  {tenb=false;return(0);}
        }
     }
   if ((ordex==false)&&(tenb==true))
     { 
     int ticket=OrderSend(Symbol(),OP_SELL,lots,Bid,3,lwb,rts,DoubleToStr(Period(),0),0,0,Red);
     Sleep(10000);tenb=false;
     }
    }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int trailstop()
  {
   bool ordex=false;
   if (OrdersTotal()>0)
     {
      int num=0;
      for(num=0;num<=OrdersTotal();num++)
        {
            OrderSelect(num,SELECT_BY_POS,MODE_TRADES);
         if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&(OrderType()==OP_SELL)&&(Trail>0)&&(OrderOpenPrice()>Close[0]+Trail*Point)&&(OrderStopLoss()>Close[0]+Trail*Point))
         {
            OrderModify(OrderTicket(),OrderOpenPrice(),Close[0]+Trail*Point,rts,0,0);Sleep(5000);
         }
         if ((StrToInteger(OrderComment())==Period())&&(OrderSymbol()==Symbol())&&(OrderType()==OP_BUY)&&(Trail>0)&&(OrderOpenPrice()<Close[0]-Trail*Point)&&(OrderStopLoss()<Close[0]-Trail*Point))
         {
            OrderModify(OrderTicket(),OrderOpenPrice(),Close[0]-Trail*Point,rtl,0,0);Sleep(5000);
         }
       }
     }
   }
//+------------------------------------------------------------------+