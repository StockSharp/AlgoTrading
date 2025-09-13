//+------------------------------------------------------------------+
//|                                                      CloseMaster |
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
input datetime CloseTime=D'2030.12.31';     //Close Date and Time
input bool DeletePO = true;                 //Delete Pending Orders
input bool CloseOO = true;                  //Close Open Orders
input bool CloseT = true;                   //Close Terminal
//-------------------------------------------------------------------------
// Variables
//-------------------------------------------------------------------------
int i,oo,po;

bool w;
//-------------------------------------------------------------------------
// 1. Main function
//-------------------------------------------------------------------------
void OnTick(void)
  {

   Comment("Copyright © 2016, Il Anokhin\n"+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS));

//--- 1.1. Deleting pending orders and closing open orders ----------------

   oo=0; po=0;

   for(i=OrdersTotal()-1;i>=0;i--)
     {

      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {

         if(OrderType()==OP_BUY || OrderType()==OP_SELL) oo++;

         if(OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP) po++;

         if(OrderType()==OP_BUYLIMIT || OrderType()==OP_SELLLIMIT) po++;

         if(TimeCurrent()>=CloseTime && CloseOO==true && OrderType()==OP_BUY) w=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),90);

         if(TimeCurrent()>=CloseTime && CloseOO==true && OrderType()==OP_SELL) w=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),90);

         if(TimeCurrent()>=CloseTime && DeletePO==true && OrderType()==OP_BUYSTOP) w=OrderDelete(OrderTicket());

         if(TimeCurrent()>=CloseTime && DeletePO==true && OrderType()==OP_SELLSTOP) w=OrderDelete(OrderTicket());

         if(TimeCurrent()>=CloseTime && DeletePO==true && OrderType()==OP_BUYLIMIT) w=OrderDelete(OrderTicket());

         if(TimeCurrent()>=CloseTime && DeletePO==true && OrderType()==OP_SELLLIMIT) w=OrderDelete(OrderTicket());

        }

     }

//--- 1.2. Removing Expert Advisor ----------------------------------------

   if(TimeCurrent()>=CloseTime && CloseT==true)
     {

      if(DeletePO==true && CloseOO==false && po==0) ExpertRemove();

      if(DeletePO==false && CloseOO==true && oo==0) ExpertRemove();

      if(DeletePO==true && CloseOO==true && oo==0 && po==0) ExpertRemove();

     }

//--- 1.3. End of main function -------------------------------------------

   return;

  }
//-------------------------------------------------------------------------
// 2. Deinitialization and Closing Terminal
//-------------------------------------------------------------------------
int deinit()
  {

   if(TimeCurrent()>=CloseTime && CloseT==true) TerminalClose(0);

   return(0);

  }
//-------------------------------------------------------------------------
