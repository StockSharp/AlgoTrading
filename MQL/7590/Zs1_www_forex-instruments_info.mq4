//+------------------------------------------------------------------+
//|                                                   Gen4sp         |
//|                 Copyright © 2000-2007, MetaQuotes Software Corp. |
//|                                         http://www.metaquotes.ru |
//+------------------------------------------------------------------+
#property copyright "Gen4sp"
#property link      ""
//---- input parameters --- trade system for usdchf
extern int Orders_Space=50;
extern int pk=10;
extern double Lots=0.1;
//----
double LastProfit=0;
double StopLoss;
double LastPrice;
double FirstPrice;
double q;
double step;
//----
 int LastOrder=-1;
 int FirstOrder=-1;
 int orders;
 int zone=0;
 int LastZone=0;
 int count=0;
 int firstflag=0;
 int BOrders[];
 int SOrders[];
 int tunelots[20];
 bool GlobalPizdec=false;
 bool flag=false;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   StopLoss=Orders_Space*140;
   //
   tunelots[0]=1;
   tunelots[1]=3;
   tunelots[2]=6;
   tunelots[3]=12;
   tunelots[4]=24;
   tunelots[5]=48;
   tunelots[6]=96;
   tunelots[7]=192;
   tunelots[8]=384;
   tunelots[9]=768;
   tunelots[10]=1536;
   tunelots[11]=3072;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   orders=OrdersTotal();
   //
   if(firstflag!=0){ CheckZone();}
   if(firstflag==0){ OpenFirst();}
     if(flag==true)
     {
      if(firstflag==1){ZoneF1();}
        if(firstflag==2)
        {
           if(FirstOrder==0)
           {
              switch(zone)
              {
                  case -2: Zone_2();break;
                  case -1: Zone_1();break;
                  case 0: Zone0();break;
                  case 1: Zone1();break;
                  case 2: Zone1();break;
              }
          }
                    if(FirstOrder==1)
                    {
                       switch(zone)
                       {
                           case 2: Zone_2();break;
                           case 1: Zone_1();break;
                           case 0: Zone0();break;
                           case -1: Zone1();break;
                           case -2: Zone1();break;
                          }
                       }
                          }
                             }
                                   if ((getTotalProfit()>=0)&&(OrdersTotal()>=3))
                                   {
                                    CloseAllOrders();
                                   }
/*
if ((OrdersTotal()>=12))
{
CloseAllOrders();
}*/
                                 return(0);
                                }                                 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      void ZoneF1()
                                      {
                                       flag=false;
                                       CloseFirstOrders();
                                      }                                                                      
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      void Zone_2()
                                      {
                                       flag=false;
                                       if(getTotalProfit()>0)
                                       {
                                       CloseAllOrders();
                                       }
                                       else
                                       {OpenAnother();
                                       }
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      void Zone_1()
                                      {
                                       flag=false;
                                       if(FirstOrder==0){OpenSellOrder();}
                                       if(FirstOrder==1){OpenBuyOrder();}
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      void Zone0()
                                      {
                                       flag=false;
                                       if(FirstOrder==1){OpenSellOrder();}
                                       if(FirstOrder==0){OpenBuyOrder();}
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      void Zone1()
                                      {
                                       flag=false;
                                       if(getTotalProfit()>0){CloseAllOrders();}else{OpenAnother();}
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      bool OpenFirst()
                                      {
                                       if (OrderSend(Symbol(),OP_BUY,NormalizeDouble(Lots,1),Ask,3,Ask-StopLoss*Point,0,zone+":",0,0,Blue)==-1) return(false);
                                       if (OrderSend(Symbol(),OP_SELL,NormalizeDouble(Lots,1),Bid,3,Bid+StopLoss*Point,0,zone+":",0,0,Yellow)==-1) return(false);
                                       FirstPrice=Close[0];
                                       firstflag=1;
                                       flag=false;
                                       return(true);
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      bool OpenBuyOrder()
                                      {
                                       if (OrderSend(Symbol(),OP_BUY,NormalizeDouble(Lots*Lots(),1),Ask,3,Ask-StopLoss*Point,0,zone+":",0,0,Blue)==-1) return(false);
                                       LastOrder=0;
                                       LastPrice=Ask;
                                       return(true);
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      bool OpenSellOrder()
                                      {
                                       if (OrderSend(Symbol(),OP_SELL,NormalizeDouble(Lots*Lots(),1),Bid,3,Bid+StopLoss*Point,0,zone+":",0,0,Yellow)==-1) return(false);
                                       LastOrder=1;
                                       LastPrice=Bid;
                                       return(true);
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      bool OpenAnother()
                                      {
                                       if(LastOrder==0){OpenSellOrder();}
                                       if(LastOrder==1){OpenBuyOrder();}
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      void CloseAllOrders()
                                      {
                                       LastProfit=getTotalProfit();
                                         while(OrdersTotal()>0)
                                         {
                                          OrderSelect(0,SELECT_BY_POS);
                                          if (OrderType()==OP_BUY)
                                          {
                                                 OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,4),3,Orange);
                                          }
                                          else if (OrderType()==OP_SELL)
                                          {
                                                 OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,4),3,Red);
                                          }
                                         }
                                       zone=0;
                                       GlobalPizdec=false;
                                       LastZone=0;
                                       LastOrder=(-1);
                                       FirstPrice=0;
                                       FirstOrder=-1;
                                       firstflag=0;
                                       flag=false;
                                       return(0);
                                      }                                   
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      void CloseFirstOrders()
                                      {
                                         for(int j=0;j<=1;j++)
                                         {
                                          OrderSelect(j,SELECT_BY_POS);
                                            if (OrderType()==OP_BUY && OrderProfit()>0)
                                            {
                                             OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,4),3,Orange);
                                             //OpenSellOrder();
                                            firstflag=2;flag=false;FirstOrder=1;LastOrder=1;return(0);
                                            }
                                          if (OrderType()==OP_SELL && OrderProfit()>0)
                                          { 
                                             OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,4),3,Red);firstflag=2;flag=false;FirstOrder=0;LastOrder=0;return(0);
                                          }
                                         }
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                    double getTotalProfit(){return(AccountEquity()-AccountBalance());
                                    }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      bool CheckZone()
                                      {
                                       if(FirstPrice==0)
                                       {
                                          return(0);
                                       }
                                         if(LastOrder==1)
                                         {
                                          double bid=Bid+Point*pk;
                                          if(bid>=FirstPrice+(Orders_Space*Point*(zone+1))){LastZone=zone;zone++;flag=true;return(0);}
                                          if(bid<=FirstPrice+(Orders_Space*Point*(zone-1))){LastZone=zone;zone--;flag=true;return(0);}
                                         }
                                         if(LastOrder==0)
                                         {
                                          double ask=Ask-Point*pk;
                                          if(ask>=FirstPrice+(Orders_Space*Point*(zone+1))){LastZone=zone;zone++;flag=true;return(0);}
                                          if(ask<=FirstPrice+(Orders_Space*Point*(zone-1))){LastZone=zone;zone--;flag=true;return(0);}
                                         }
                                         if(LastOrder==(-1))
                                         {
                                          if(Close[0]>=FirstPrice+(Orders_Space*Point*(zone+1))){LastZone=zone;zone++;flag=true;return(0);}
                                          if(Close[0]<=FirstPrice+(Orders_Space*Point*(zone-1))){LastZone=zone;zone--;flag=true;return(0);}
                                         }
                                       return(0);
                                      }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                                      double Lots()
                                      {
                                       int k=1;
                                         if((zone==0 || zone==(-1)) && orders>=1 && FirstOrder==0)
                                         {
                                          k=tunelots[OrdersTotal()];
                                         }
                                         if((zone==0 || zone==1) && orders>=1 && FirstOrder==1)
                                         {
                                          k=tunelots[OrdersTotal()];
                                         }
                                       if(k>0){return(k);}else{return(1);
                                       }
                                      }
//+------------------------------------------------------------------+