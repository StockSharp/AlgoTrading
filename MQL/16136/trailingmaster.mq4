//+------------------------------------------------------------------+
//|                                                   TrailingMaster |
//|                                       Copyright 2016, Il Anokhin |
//|                           http://www.mql5.com/en/users/ilanokhin |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Il Anokhin"
#property link "http://www.mql5.com/en/users/ilanokhin"
#property description ""
#property strict
//-------------------------------------------------------------------------
// Inputs
//-------------------------------------------------------------------------
input int TS = 50;                  //Trailing Stop (pips)
input bool UC = false;              //Use Certain Order Comment
input string Comm = "";             //Certain Order Comment
input bool UM = false;              //Use Certain Order Magic Number
input int Magic = 12345;            //Certain Order Magic Number
//-------------------------------------------------------------------------
// Variables
//-------------------------------------------------------------------------
int i;
double pip;
bool w;
//-------------------------------------------------------------------------
// 1. Main function
//-------------------------------------------------------------------------
void OnTick(void)
  {
   Comment("Copyright © 2016, Il Anokhin\n"+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS));

//--- 1.1. Define pip -----------------------------------------------------
   if(Digits==4 || Digits<=2) pip=Point;
   if(Digits==5 || Digits==3) pip=Point*10;

//--- 1.2. Trailing -------------------------------------------------------
   for(i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         if(OrderSymbol()==Symbol() && TS>0 && OrderProfit()>0)
           {
            if(UC==true && OrderComment()==Comm && UM==true && OrderMagicNumber()==Magic && OrderType()==OP_BUY && OrderOpenPrice()+TS*pip<=Bid && OrderStopLoss()<Bid-TS*pip) w=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TS*pip,OrderTakeProfit(),0);
            if(UC==true && OrderComment()==Comm && UM==true && OrderMagicNumber()==Magic && OrderType()==OP_SELL && OrderOpenPrice()-TS*pip>=Ask && (OrderStopLoss()>Ask+TS*pip || OrderStopLoss()==0)) w=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TS*pip,OrderTakeProfit(),0);
            if(UC==true && OrderComment()==Comm && UM==false && OrderType()==OP_BUY && OrderOpenPrice()+TS*pip<=Bid && OrderStopLoss()<Bid-TS*pip) w=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TS*pip,OrderTakeProfit(),0);
            if(UC==true && OrderComment()==Comm && UM==false && OrderType()==OP_SELL && OrderOpenPrice()-TS*pip>=Ask && (OrderStopLoss()>Ask+TS*pip || OrderStopLoss()==0)) w=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TS*pip,OrderTakeProfit(),0);
            if(UC==false && UM==true && OrderMagicNumber()==Magic && OrderType()==OP_BUY && OrderOpenPrice()+TS*pip<=Bid && OrderStopLoss()<Bid-TS*pip) w=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TS*pip,OrderTakeProfit(),0);
            if(UC==false && UM==true && OrderMagicNumber()==Magic && OrderType()==OP_SELL && OrderOpenPrice()-TS*pip>=Ask && (OrderStopLoss()>Ask+TS*pip || OrderStopLoss()==0)) w=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TS*pip,OrderTakeProfit(),0);
            if(UC==false && UM==false && OrderType()==OP_BUY && OrderOpenPrice()+TS*pip<=Bid && OrderStopLoss()<Bid-TS*pip) w=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TS*pip,OrderTakeProfit(),0);
            if(UC==false && UM==false && OrderType()==OP_SELL && OrderOpenPrice()-TS*pip>=Ask && (OrderStopLoss()>Ask+TS*pip || OrderStopLoss()==0)) w=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TS*pip,OrderTakeProfit(),0);
           }
        }
     }

//--- 1.3. End of main function -------------------------------------------
   return;
  }
//-------------------------------------------------------------------------
