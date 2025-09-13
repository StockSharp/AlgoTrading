//+------------------------------------------------------------------+
//|                 Developed by www.forex-tsd.com                   |
//|                 Idea from John Taylor v.2.0                      |
//|                                                                  |
//+------------------------------------------------------------------+
#include <stdlib.mqh>
#define MySuperMagic 111020051110
//----
extern int StartHour=8;
extern int EndHour  =20;
extern double Lots =0.1;
//----
double LastBarChecked;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
//---- 
   LastBarChecked=Time[0];
//----
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
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   string cm="Volume ";
   if (Period()==1) cm=cm + "1M";
   if (Period()==5) cm=cm + "5M";
   if (Period()==15) cm=cm + "15M";
   if (Period()==30) cm=cm + "30M";
   if (Period()==60) cm=cm + "1H";
   if (Period()==240) cm=cm + "4H";
   if (Period()==1440) cm=cm + "1D";
   if (Period()==10080) cm=cm + "1W";
   if (Period()==43200) cm=cm + "1M";
   cm=cm + " - ";
   cm=cm + TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS);
   int EAMagic=MySuperMagic + Period();
//------------------------------------------------------------------------------------------------ 
   bool doShort=false;
   bool doLong =false;
   bool hourValid=(Hour()>=StartHour) && (Hour()<=EndHour);
   if((Volume[1] < Volume[2]) && hourValid)
     {
      doLong=true;
      Comment("Up trend");
     }
   if((Volume[1] > Volume[2]) && hourValid)
     {
      doShort=true;
      Comment("Down trend");
     }
   if(Volume[1]==Volume[2] )
     {
      Comment("No trend...");
     }
   if(LastBarChecked!=Time[0])
     {
      int cnt=0;
      while(cnt<OrdersTotal())
        {
         if(OrderSelect (cnt, SELECT_BY_POS)==false) continue;
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==EAMagic)
           {
            int ticket=OrderTicket();
            double oLots=OrderLots();
            double priceClose;
            if (OrderType()==OP_BUY)
              {
               priceClose=Bid;
               if(doLong)
                 {
                  LastBarChecked=Time[0];
                  return(0);
                 }
              }
            else
              {
               priceClose=Ask;
               if(doShort)
                 {
                  LastBarChecked=Time[0];
                  return(0);
                 }
              }
            if(!OrderClose(ticket,oLots,priceClose,7,Red))
              {
               Alert("Error closing trade: " + ErrorDescription(GetLastError()));
               return(0);
              }
           }
         else
           {
            cnt ++;
           }
        }
      if (hourValid)
        {
         if(Volume[1] < Volume[2])
           {
            OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,cm,EAMagic,0,White);
           }
         if(Volume[1] > Volume[2] )
           {
            OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,cm,EAMagic,0,Red);
           }
        }
      LastBarChecked=Time[0];
     }
   return(0);
  }
//+------------------------------------------------------------------+