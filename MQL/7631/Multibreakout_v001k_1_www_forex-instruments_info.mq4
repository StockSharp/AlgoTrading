//+------------------------------------------------------------------+
//|                 Need To Do List:                                 |
//|                 For v0.2                                         |
//|                 1. Place only one order for 1 lot.               |
//|                 2. Sell off 0.1 lot when TP levels are met.      |
//+------------------------------------------------------------------+
#property copyright "Provided by SBFX forum members"
#property link      "http://www.strategybuilderfx.com"
//----
#include <stdlib.mqh>
#include <WinUser32.mqh>
//----
extern double  LotSize=0.1;
extern int     BrokerOffsetToGMT      =0;
extern int     NumberOfOrdersPerSide  =20;    // NumberOfOrdersPerSide X TakeProfitIncrement = Highest TakeProfit Level
extern int     TakeProfitIncrement    =5;
extern bool    Trade1                 =true;
extern int     Time1                  =6;
extern bool    Trade2                 =true;
extern int     Time2                  =12;
extern bool    Trade3                 =true;
extern int     Time3                  =18;
extern bool    Trade4                 =true;
extern int     Time4                  =0;
extern int     ExitMinute             =55;
extern int     StopLoss               =20;
extern int     PipsForEntry           =5;
extern int     BreakEven              =10;
extern bool    MovingBreakEven        =true;
extern int     MovingBEHoursToStart   =3;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
   int cnt, ticket;
     if(Bars<100) 
     {
      Print("bars less than 100");
      return(0);
     }
   double   BE,MBE;
   double   PFE=PipsForEntry;
   double   CurrentHour=TimeHour(CurTime());
   double   CurrentMinute=TimeMinute(CurTime());
   if((CurrentHour==Time1+1 && Trade1) || (CurrentHour==Time2+1 && Trade2) ||
            (CurrentHour==Time3+1 && Trade3) || (CurrentHour==Time4+1 && Trade4)) 
            {
      double highprice=iHigh(NULL,60,1);
      double lowprice=iLow(NULL,60,1);
     }
   int      total=OrdersTotal();
   double   Spread=Ask-Bid;
   double   hprice=Spread+highprice+PFE*Point;
   double   lprice=lowprice-PFE*Point;
   double   PreviousBarHigh=iHigh(NULL,0,1);
   double   PreviousBarLow=iLow(NULL,0,1);
   double   Previous2ndBarHigh=iHigh(NULL,0,2);
   double   Previous2ndBarLow=iLow(NULL,0,2);
   int      TPI=TakeProfitIncrement;
   int      i,j;
   int      MBEHour1=Time1+BrokerOffsetToGMT+MovingBEHoursToStart;
   int      MBEHour2=Time2+BrokerOffsetToGMT+MovingBEHoursToStart;
   int      MBEHour3=Time3+BrokerOffsetToGMT+MovingBEHoursToStart;
   int      MBEHour4=Time4+BrokerOffsetToGMT+MovingBEHoursToStart;
   bool     need_long =true;
   bool     need_short=true;
   bool     MovingBE1 =false;
   bool     MovingBE2 =false;
   bool     MovingBE3 =false;
   bool     MovingBE4 =false;
   bool     FirstBE   =false;
   // First update existing orders.
     if(MBEHour1>=24) 
     {
      MBEHour1=MBEHour1-24;
     }
     if(MBEHour2>=24) 
     {
      MBEHour2=MBEHour2-24;
     }
     if(MBEHour3>=24) 
     {
      MBEHour3=MBEHour3-24;
     }
     if(MBEHour4>=24) 
     {
      MBEHour4=MBEHour4-24;
     }
   if(Hour()>=MBEHour1 && MovingBreakEven) MovingBE1=true;
   if(Hour()>=MBEHour2 && MovingBreakEven) MovingBE2=true;
   if(Hour()>=MBEHour3 && MovingBreakEven) MovingBE3=true;
   if(Hour()>=MBEHour4 && MovingBreakEven) MovingBE4=true;
   //
   double   SL=StopLoss*Point;
//----
   Comment("CurrentHour is  ",CurrentHour,"\n",
           "CurrentMinute is ",CurrentMinute);
   if((CurrentHour==Time1 && CurrentMinute>=ExitMinute && Trade1) || (CurrentHour==Time2 && CurrentMinute>=ExitMinute && Trade2) ||
            (CurrentHour==Time3 && CurrentMinute>=ExitMinute && Trade3) || (CurrentHour==Time4 && CurrentMinute>=ExitMinute && Trade4)) 
            {
        for(i=total-1;i>=0;i--) 
        {
         OrderSelect(i, SELECT_BY_POS);
           if(OrderSymbol()==Symbol()) 
           {
            int type  =OrderType();
            bool result=false;
              switch(type)
              {
                  //Close opened long positions
                  case OP_BUY       : result=OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );
                     break;
                     //Close opened short positions
                  case OP_SELL      : result=OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );
                     break;
                     //Close pending orders
                     //case OP_BUYLIMIT  :
                  case OP_BUYSTOP   :
                     //case OP_SELLLIMIT :
                  case OP_SELLSTOP  : result=OrderDelete( OrderTicket() );
                 }
                 if(result==false)
                 {
                  Alert("Order " , OrderTicket() , " failed to close. Error:" , GetLastError() );
                  Sleep(3000);
                 }
              }//End Chart Orders
                 }//End for loop
                    }//End Closing All Open and Pending Orders
                          else
                          {
                             for(cnt=0;cnt<total;cnt++) 
                             {
                              OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
                                if(OrderSymbol()==Symbol()) 
                                {
                                   if(OrderType()==OP_BUY) 
                                   {
                                    need_long=false;
                                    if((MovingBE1 && BreakEven>0 && FirstBE)||
                                       (MovingBE2 && BreakEven>0 && FirstBE)||
                                       (MovingBE3 && BreakEven>0 && FirstBE)||
                                             (MovingBE4 && BreakEven>0 && FirstBE)) 
                                             {
                                         if(PreviousBarLow>Previous2ndBarLow) 
                                         {
                                          MBE=PreviousBarLow;
                                          OrderModify(OrderTicket(),OrderOpenPrice(),MBE,OrderTakeProfit(),0,White);
                                         }// End MBE fix
                                      }//End MovingBE && BE>0
                                      else
                                      {
                                         if(!FirstBE) 
                                         {
                                            if(BreakEven>0) 
                                            {
                                               if((Bid-OrderOpenPrice())>(Point*BreakEven)) 
                                               {
                                                BE=OrderOpenPrice();
                                                OrderModify(OrderTicket(),OrderOpenPrice(),BE,OrderTakeProfit(),0,White);
                                                FirstBE=true;
                                               }// End BE fix
                                            }// End BE>0
                                         }//End !FirstBE
                                      }//End else
                                   }//End OP_BUY 
                                   if(OrderType()==OP_SELL) 
                                   {
                                    need_short=false;
                                    if((MovingBE1 && BreakEven>0 && FirstBE)||
                                       (MovingBE2 && BreakEven>0 && FirstBE)||
                                       (MovingBE3 && BreakEven>0 && FirstBE)||
                                             (MovingBE4 && BreakEven>0 && FirstBE)) 
                                             {
                                         if(PreviousBarHigh>Previous2ndBarHigh) 
                                         {
                                          MBE=PreviousBarHigh;
                                          OrderModify(OrderTicket(),OrderOpenPrice(),MBE,OrderTakeProfit(),0,White);
                                         }// End MBE fix
                                      }//End MovingBE && BE>0
                                      else{
                                         if(!FirstBE) 
                                         {
                                            if(BreakEven>0) 
                                            {
                                               if((OrderOpenPrice()-Ask)>(Point*BreakEven)) 
                                               {
                                                BE=OrderOpenPrice();
                                                OrderModify(OrderTicket(),OrderOpenPrice(),BE,OrderTakeProfit(),0,White);
                                                FirstBE=true;
                                               }// End BE fix
                                            }// End BE>0
                                         }//End !FirstBE
                                      }//End else
                                   }//End OP_SELL
                                   if(OrderType()==OP_BUYSTOP) 
                                   {
                                    need_long=false;
                                   }
                                   if(OrderType()==OP_SELLSTOP) 
                                   {
                                    need_short=false;
                                   }
                                }//End OrderSymbol()==Symbol()
                             }//End for loop
                             if(AccountFreeMargin()<(1000*LotSize)) 
                             {
                              Print("We have no money. Free Margin = ", AccountFreeMargin());
                              return(0);
                             }
                           if((CurrentHour-1==Time1 && Trade1) || (CurrentHour-1==Time2 && Trade2) ||
                                    (CurrentHour-1==Time3 && Trade3) || (CurrentHour-1==Time4 && Trade4)) 
                                    {
                                if(need_long) 
                                {
                                   for(i=1;i<NumberOfOrdersPerSide+1;i++) 
                                   {
                                    int hticket=OrderSend(Symbol(),OP_BUYSTOP,LotSize,hprice,3,hprice-SL,hprice+((TPI*i)*Point),"MultiBreakOut_v0.1",255+i,0,Green);
                                   }
                                   if(hticket<(i+1)) 
                                   {
                                    int herror=GetLastError();
                                    Print("Error = ",ErrorDescription(herror));
                                   }
                                }//End need_long
                                if(need_short) 
                                {
                                   for(j=1;j<NumberOfOrdersPerSide+1;j++) 
                                   {
                                    int lticket=OrderSend(Symbol(),OP_SELLSTOP,LotSize,lprice,3,lprice+SL,lprice-((TPI*j)*Point),"MultiBreakOut_v0.1",355+j,0,Red);
                                   }
                                   if(lticket<(j+1)) 
                                   {
                                    int lerror=GetLastError();
                                    Print("Error = ",ErrorDescription(lerror));
                                   }
                                }//End need_short
                             }//End Entry
                          }//End Else
                       }//End Start
//+------------------------------------------------------------------+