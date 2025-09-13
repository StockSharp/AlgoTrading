//+------------------------------------------------------------------+
//|                                                     OpenTiks.mq4 |
//|                                        Copyright © 2008, ZerkMax |
//|                                                      zma@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, ZerkMax"
#property link      "zma@mail.ru"


extern int    TrailingStop   = 30;
extern int    StopLoss       = 0;
extern double Lots           = 0.1;
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


if (High[0]>High[1]&&High[1]>High[2]&&High[2]>High[3]&&Open[0]>Open[1]&&Open[1]>Open[2]&&Open[2]>Open[3]) BuyOp=true;
if (High[0]<High[1]&&High[1]<High[2]&&High[2]<High[3]&&Open[0]<Open[1]&&Open[1]<Open[2]&&Open[2]<Open[3]) SellOp=true;

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

