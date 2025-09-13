#property copyright "Yongbin Yu"
#property link      "http://www.forexcafe.cn"

extern double TakeProfit = 50;
extern double Lots = 1;
//extern double TrailingStop = -30;
double TrailingStop = -30;
extern double StopLoss=30;




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

     int cnt, ticket, total;
     bool CloseFlag=false;
//----
      if(Bars<100)
     {
      Print("bars less than 100");
      return(0);  
     }
   if(TakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0);  // check TakeProfit
     }
      

   total=OrdersTotal();
   if(total<1)
   {
      int obj_total=ObjectsTotal();
      int m;
      double temp_price;
      if (obj_total>=1)
      {
        for(m=obj_total-1;m>=0;m--)
        {
            if (ObjectType(ObjectName(m))==OBJ_HLINE) {
               temp_price=ObjectGet(ObjectName(m),OBJPROP_PRICE1);
               ObjectDelete((ObjectName(m)));   
               //buy
 
               if (temp_price>Bid)
               {
                  ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"test",16384,0,Green);
                  if(ticket>0)
                  {
                     if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
                  }
                  else Print("Error opening BUY order : ",GetLastError());
                  return(0); 
               }
               //sell
               else if (temp_price<Bid)
               {
                  ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"EA_MMA",16384,0,Red);
                  if(ticket>0)
                    {
                     if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
                    }
                  else Print("Error opening SELL order : ",GetLastError()); 
                  return(0); 
               }
            }
           
        }
      }
      
   }

   

   
   // it is important to enter the market correctly, 
   // but it is more important to exit it correctly...   
   
   //make a flag
      int obj_total2;
      obj_total2=ObjectsTotal();
      if (obj_total2>=1)
      {
        for(int m2=obj_total2-1;m2>=0;m2--)
        {
            if (ObjectType(ObjectName(m2))==OBJ_VLINE) {
               CloseFlag=true;
               ObjectDelete((ObjectName(m2)));
            }
            
        }
      }
   //end make a flag
   
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      
      //if there was a opened order , modify its TakeProfit ny Hline
      int obj_total3=ObjectsTotal();
      int m3;
      double temp_tp;
      if (obj_total3>=1)
      {
        for(m3=obj_total3-1;m3>=0;m3--)
        {
            if (ObjectType(ObjectName(m3))==OBJ_HLINE) {
               temp_tp=ObjectGet(ObjectName(m3),OBJPROP_PRICE1);
               ObjectDelete((ObjectName(m3)));   
               //modify the take profit
               if(OrderType()==OP_BUY)  {
                  if (temp_tp>Bid)
                  {
                  OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),temp_tp,0,Blue);
                  }
                  //modify the stopLoss
                  else if (temp_tp<Bid)
                  {
                  OrderModify(OrderTicket(),OrderOpenPrice(),temp_tp,OrderTakeProfit(),0,Red);
                  }
               } else {
                  if (temp_tp>Bid)
                  {
                  OrderModify(OrderTicket(),OrderOpenPrice(),temp_tp,OrderTakeProfit(),0,Red);
                  }
                  //modify the stopLoss
                  else if (temp_tp<Bid)
                  {
                  OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),temp_tp,0,Blue);
                  }
               }
            }
           
        }
      }
      //modify check end
      
      if(OrderType()<=OP_SELL &&   // check for opened position 
         OrderSymbol()==Symbol())  // check for symbol
        {
         if(OrderType()==OP_BUY)   // long position is opened
           {
            // should it be closed?
            
            if(CloseFlag)
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
                 return(0); // exit
                }
                
            // check for trailing stop

            if(TrailingStop>0)   
              {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                     return(0);
                    }
                 }
              }

           }
         else // go to short position
           {
            // should it be closed?
            
            if(CloseFlag)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close position
               return(0); // exit
              }
             
            // check for trailing stop

            if(TrailingStop>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }

           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+