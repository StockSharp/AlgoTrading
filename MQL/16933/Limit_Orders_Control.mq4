//+------------------------------------------------------------------+
//|                                         Limit Orders Control.mq4 |
//|                                              Copyright 2015, Tor |
//|                                             http://einvestor.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Tor"
#property link      "http://einvestor.ru/"
#property version   "1.00"
#property strict

input int MagicNumber=0;
input bool WriteComments=true;

#include <stderror.mqh>
#include <stdlib.mqh>

static bool work=true;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   work=true;
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   int countOrders=0;

   if(OrdersTotal()>0)
     {
      for(int c=0; c<OrdersTotal(); c++)
        {
         if(OrderSelect(c,SELECT_BY_POS))
           {
            if(OrderMagicNumber()==MagicNumber && OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUYLIMIT || OrderType()==OP_BUYSTOP || OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP)
                 {
                  countOrders++;
                 }
              }
           }
        }
     }

   string txt="";
   if(WriteComments)
     {
      txt=txt+"We have "+(string)countOrders+" open orders\n";
      if(work)
        {
         txt=txt+"Limit Orders Control - work\n";
           }else{
         txt=txt+"Limit Orders Control - STOPPED\n";
        }
      Comment(txt);
     }
   if(!work){ return; }

   if(countOrders<2)
     {
      DeleteOrders();
     }
   if(countOrders<1)
     {
      work=false;
      Alert("Limit Orders Control - Stopped ",_Symbol);
     }

  }
//+------------------------------------------------------------------+

void DeleteOrders()
  {
   int t;
   if(IsTradeAllowed())
     {
      for(int c=0; c<=OrdersTotal();c++)
        {
         if(OrderSelect(c,SELECT_BY_POS,MODE_TRADES))
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)//
              {
               if(OrderType()==OP_BUYLIMIT || OrderType()==OP_BUYSTOP || OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP)
                 {
                  for(t=0; t<=5; t++)
                    {
                     RefreshRates();
                     int ticket=OrderDelete(OrderTicket(),clrRed);
                     int e=GetLastError();
                     if(e==0)
                       {
                        break;
                          }else{
                        Print("Try ",c+1," delete orders, error : ",ErrorDescription(e));
                        Sleep(100);
                       }
                    }
                 }
              }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
