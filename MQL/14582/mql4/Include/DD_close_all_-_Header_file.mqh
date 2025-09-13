//+------------------------------------------------------------------+
//|  Copyright, Programmed by: Khaled Mohamed Abdalla                |
//|  https://www.mql5.com/en/users/khaled.mohamed                    |
//+------------------------------------------------------------------+
#property copyright "Programmed by: Khaled Mohamed Abdalla"
#property link      "https://www.mql5.com/en/users/khaled.mohamed"
#property strict
#property description "A header file to close the all orders once the drawdoen reach a specific percentage ."

// Header files for the errors description:
#include <stderror.mqh>
#include <stdlib.mqh>

// To use this option, just you need to call the function : ( DD_close ) 
//+------------------------------------------------------------------+
//|                          Global scop                             |
//+------------------------------------------------------------------+
bool Close_All_V;
//+------------------------------------------------------------------+
//|                         Main function                            |
//+------------------------------------------------------------------+
// DD:               Here it is the DD persantge, 100 means never close any order .
// Magic_Number:     Your EA magic number, enter 0 to control the all orders .
void DD_close(int DD,int Magic_Number)
  {
   if(DD(Magic_Number)>=DD)
      Close_All_V=true;
   if(Close_All_V)
      Close_All(Magic_Number);
  }
//+------------------------------------------------------------------+
//|                          Check close                             |
//+------------------------------------------------------------------+
void Check_Close(int Check_Number) // check close order
  {
   if(Check_Number<0) Print("OrderClose failed with error: ",ErrorDescription(GetLastError()));
   else Close_All_V=false;
  }
//+------------------------------------------------------------------+
//|                          Close all                               |
//+------------------------------------------------------------------+
void Close_All(int M_N)
  {
   int Loop=0;
   for(int i=0; Loop<OrdersTotal(); i++)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Loop++;
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==M_N || OrderMagicNumber()==0)
              {
               if(OrderType()==OP_BUY)
                  Check_Close(OrderClose(OrderTicket(),OrderLots(),Bid,100,clrNONE));
               if(OrderType()==OP_SELL)
                  Check_Close(OrderClose(OrderTicket(),OrderLots(),Ask,100,clrNONE));
              }
        }
  }
//+------------------------------------------------------------------+
//|                          Calculate loss                          |
//+------------------------------------------------------------------+
double Loss(int M_N)
  {
   double re=0;
   int Loop=0;
   for(int i=0; Loop<OrdersTotal(); i++)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Loop++;
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==M_N || OrderMagicNumber()==0)
               re=re+OrderProfit();
        }
   return re * -1;
  }
//+------------------------------------------------------------------+
//|                  Calculate drawdown persantage                   |
//+------------------------------------------------------------------+
double DD(int M_N)
  {
   return ( 100 / AccountBalance ( ) ) * Loss ( M_N );
  }
//+------------------------------------------------------------------+
