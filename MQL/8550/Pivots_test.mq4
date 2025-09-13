//+------------------------------------------------------------------+
//|                                                  Pivots_test.mq4 |
//|                                       Copyright © 2008, ZerkMax. |
//|                                                      zma@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, ZerkMax."
#property link      "zma@mail.ru"




extern double Lots           = 0.1;
extern int    TrailingStop   = 30;
extern int    magicnumber    = 777;



int prevtime;


int ticketbuy, ticketsell;
//--- Подключение библиотеки автооптимизатора


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
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
int start()
  {
//----


   int i=0;  
   int total = OrdersTotal();   
   for(i = 0; i <= total; i++) 
     {
      if(TrailingStop>0)  
       {                 
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == magicnumber) 
         {
         TrailingStairs(OrderTicket(),TrailingStop);
         }
       }
      }

   if(Bars == prevtime) 
       return(0);
   prevtime = Bars;
   if(!IsTradeAllowed()) 
     {
       prevtime = Bars;
       return(0);
     }


   
   double R2=NormalizeDouble(iCustom(NULL,0, "Pivots",5,1),2);
   double R1=NormalizeDouble(iCustom(NULL,0, "Pivots",4,1),2);
   double Piv=NormalizeDouble(iCustom(NULL,0, "Pivots",3,1),2);
   double S1=NormalizeDouble(iCustom(NULL,0, "Pivots",2,1),2);
   double S2=NormalizeDouble(iCustom(NULL,0, "Pivots",1,1),2);


ticketbuy=0;
ticketsell=0;
   for(i=1; i<=OrdersTotal(); i++)          
    { 
     if (OrderSelect(i-1,SELECT_BY_POS)==true) 
      {                                         
        int Tip=OrderType();                    
        if (Tip==2) ticketbuy=OrderTicket();  
        if (Tip==5) ticketsell=OrderTicket(); 
      }                              
    }

if (OrdersTotal()!=0)
{
   if (ticketbuy==0) 
   {
     if (ticketsell!=0) OrderDelete(ticketsell,0);
     OrderSend(Symbol(),OP_BUYLIMIT,Lots,Piv,3,S2,R2,"Pivots_Buy",magicnumber,0,Green);
     OrderSend(Symbol(),OP_SELLSTOP,Lots,Piv,3,R2,S2,"Pivots_Sell",magicnumber,0,Green);
   }
   if (ticketsell==0) 
   {
     if (ticketbuy!=0) OrderDelete(ticketbuy,0);
     OrderSend(Symbol(),OP_BUYLIMIT,Lots,Piv,3,S2,R2,"Pivots_Buy",magicnumber,0,Green);
     OrderSend(Symbol(),OP_SELLSTOP,Lots,Piv,3,R2,S2,"Pivots_Sell",magicnumber,0,Green);
   }
}
else
{
     OrderSend(Symbol(),OP_BUYLIMIT,Lots,Piv,3,S2,R2,"Pivots_Buy",magicnumber,0,Green);
     OrderSend(Symbol(),OP_SELLSTOP,Lots,Piv,3,R2,S2,"Pivots_Sell",magicnumber,0,Green);
}
    



//----
   return(0);
  }
//+------------------------------------------------------------------+

void TrailingStairs(int ticket,int trldistance)
   {
    int Spred=Ask - Bid;
    if (OrderType()==OP_BUY)
      {
       if((Bid-OrderOpenPrice())>(Point*trldistance))
         {
          if(OrderStopLoss()<Bid-Point*trldistance || (OrderStopLoss()==0))
            {
             OrderModify(ticket,OrderOpenPrice(),Bid-Point*trldistance,OrderTakeProfit(),0,Green);
             
            }
         }
       }
     if (OrderType()==OP_SELL)
       {
        if((OrderOpenPrice()-Ask)>(Point*trldistance))
          {
           if((OrderStopLoss()>(Ask+Point*trldistance)) || (OrderStopLoss()==0))
             {
              OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*trldistance,OrderTakeProfit(),0,Red);
             }
          }
        }
    }

