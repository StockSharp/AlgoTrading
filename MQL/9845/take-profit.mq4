//+------------------------------------------------------------------+
//|                                                  Take-profit.mq4 |
//|                                    Copyright © 2010, Christopher |                                                       
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Christopher"
#property link      "zma@mail.ru"

extern int    Shift1          = 0;
extern int    Shift2          = 1;
extern int    Shift3          = 2;
extern int    Shift4          = 3;
extern int    TrailingStop   = 1;
extern int    StopLoss       = 0;
extern double ProfitTarget    =1;
extern double Lots           = 1;
extern bool RiskManagement=true; //money management
extern double RiskPercent=1; //risk in percentage
extern int    magicnumber    = 777;
extern bool   PolLots        = true;
extern int    MaxOrders      =  1;

int prevtime;
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

   //profit target
   
   if(AccountEquity()>(AccountBalance()+ProfitTarget))
   {
      CloseOrders(magicnumber);
      return(0);
   }
   
   //risk management
   
   bool MM=RiskManagement;
   if(MM){if(RiskPercent<1||RiskPercent>100){Comment("Invalid Risk Value.");return(0);}
   else{Lots=MathFloor((AccountFreeMargin()*AccountLeverage()*RiskPercent*Point*100)/(Ask*MarketInfo(Symbol(),MODE_LOTSIZE)*
   MarketInfo(Symbol(),MODE_MINLOT)))*MarketInfo(Symbol(),MODE_MINLOT);}}
   if(MM==false){Lots=Lots;}

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

bool BuyOp=false;
bool SellOp=false;


if (High[Shift1]>High[Shift2]&&High[Shift2]>High[Shift3]&&High[Shift3]>High[Shift4]&&Open[Shift1]>Open[Shift2]&&Open[Shift2]>Open[Shift3]&&Open[Shift3]>Open[Shift4]) BuyOp=true;
if (High[Shift1]<High[Shift2]&&High[Shift2]<High[Shift3]&&High[Shift3]<High[Shift4]&&Open[Shift1]<Open[Shift2]&&Open[Shift2]<Open[Shift3]&&Open[Shift3]<Open[Shift4]) SellOp=true;

   if(Time[0] == prevtime) 
       return(0);
   prevtime = Time[0];
   if(!IsTradeAllowed()) 
     {
       prevtime = Time[1];
       return(0);
     }


   if (total < MaxOrders || MaxOrders == 0)
     {   
       if(BuyOp)
        { 
         if (StopLoss!=0)
          {
           OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-(StopLoss*Point),0,"OpenTiks_Buy",magicnumber,0,Green);
          }
         else
          {
           OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"OpenTiks_Buy",magicnumber,0,Green);
          }
        }
       if(SellOp)
        { 
         if (StopLoss!=0)
          {
           OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+(StopLoss*Point),0,"OpenTiks_Sell",magicnumber,0,Red);
          } 
         else 
          {
           OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"OpenTiks_Sell",magicnumber,0,Red);
          }
        }
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
             if (PolLots)
             if (NormalizeDouble(OrderLots()/2,2)>MarketInfo(Symbol(), MODE_MINLOT))
               {
               OrderClose(ticket,NormalizeDouble(OrderLots()/2,2),Ask,3,Green);
               }
             else
               {
               OrderClose(ticket,OrderLots(),Ask,3,Green);
               }
            }
         }
       }
     else
       {
        if((OrderOpenPrice()-Ask)>(Point*trldistance))
          {
           if((OrderStopLoss()>(Ask+Point*trldistance)) || (OrderStopLoss()==0))
             {
              OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*trldistance,OrderTakeProfit(),0,Red);
             if (PolLots)
             if (NormalizeDouble(OrderLots()/2,2)>MarketInfo(Symbol(), MODE_MINLOT))
               {
               OrderClose(ticket,NormalizeDouble(OrderLots()/2,2),Bid,3,Green);
               }
             else
               {
               OrderClose(ticket,OrderLots(),Bid,3,Green);
               }
             }
          }
        }
    }

//|---------close buy orders

int CloseOrders(int Magic)
{
  int result,total=OrdersTotal();

  for (int cnt=total-1;cnt>=0;cnt--)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if(OrderMagicNumber()==Magic&&OrderSymbol()==Symbol())
    {
      if(OrderType()==OP_BUY)
      {
        OrderClose(OrderTicket(),OrderLots(),Bid,3);
        switch(OrderType())
        {
          case OP_BUYLIMIT:
          case OP_BUYSTOP:
          result=OrderDelete(OrderTicket());
        }
      }
      if(OrderType()==OP_SELL)
      {
        OrderClose(OrderTicket(),OrderLots(),Ask,3);
        switch(OrderType())
        {
          case OP_SELLLIMIT:
          case OP_SELLSTOP:
          result=OrderDelete(OrderTicket());
        }
      }
    }
  }
  return(0);
}