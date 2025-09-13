//+------------------------------------------------------------------+
//|                                           Fractured Fractals.mq4 |
//|                 Copyright © 2005, tageiger aka fxid10t@yahoo.com |
//|                MetaTrader_Experts_and_Indicators@yahoogroups.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, tageiger aka fxid10t@yahoo.com"
#property link      "MetaTrader_Experts_and_Indicators@yahoogroups.com"
//----
extern double     MaximumRisk       =0.02;
extern double     DecreaseFactor    =3;
//----
string            TradeSymbol;
string            comment;
double            spread;
int               b.1,s.1,ticket;
double            bs,ss,btsl,stsl,fu,fd,cfu,pfu,cfd,pfd;
static double     pfu.1,pfu.2,pfu.3,pfd.1,pfd.2,pfd.3;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()  {  return(0);  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit(){  return(0);  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
   TradeSymbol=Symbol();
   comment="m Fractured Fractal";
   spread=Ask-Bid;
   fu=iFractals(Symbol(),0,MODE_UPPER,3);//Print(fu);
   fd=iFractals(Symbol(),0,MODE_LOWER,3);//Print(fd);
//----
   if(fu>0) {  cfu=fu; pfu.3=pfu.2;pfu.2=pfu.1;pfu.1=pfu;   }  else pfu=cfu;
   if(fd>0) {  cfd=fd; pfd.3=pfd.2;pfd.2=pfd.1;pfd.1=pfd;   }  else pfd=cfd;
   if(TotalTradesThisSymbol(TradeSymbol)==0)  { b.1=0;s.1=0;   }
     if(TotalTradesThisSymbol(TradeSymbol)>0)  
     {
        for(int cnt=0;cnt<OrdersTotal();cnt++) 
        {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
           if(OrderSymbol()==TradeSymbol) 
           {
            if(OrderMagicNumber()==101)  {b.1=OrderTicket(); }
           if(OrderMagicNumber()==102)  {s.1=OrderTicket(); } 
           }  
        }  
     }
     if(b.1==0 && cfu>0 && pfu.1>0 && cfu>pfu.1) 
     {
      ticket=OrderSend(Symbol(),
                       OP_BUYSTOP,
                       LotsOptimized(),
                       cfu+spread,
                       0,
                       cfd-spread,
                       0,
                       Period()+comment,
                       101,
                       0,
                       Green);
        if(ticket>0)   
        {
         if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
         {  b.1=ticket;  Print(ticket); }
         else Print("Error Opening BuyStop Order: ",GetLastError());
        return(0);  
        }
      }
     if(s.1==0 && cfd>0 && pfd.1>0 && cfd<pfd.1) 
     {
      ticket=OrderSend(Symbol(),
                       OP_SELLSTOP,
                       LotsOptimized(),
                       cfd-spread,
                       0,
                       cfu+spread,
                       0,
                       Period()+comment,
                       102,
                       0,
                       Red);
        if(ticket>0)   
        {
         if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
         {  s.1=ticket;  Print(ticket); }
         else Print("Error Opening BuyStop Order: ",GetLastError());
        return(0);  
        }  
      }
     for(int c=0;c<OrdersTotal();c++)   
     {
      OrderSelect(c,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol() && OrderComment()==Period()+comment)   
        {
           if(OrderType()==OP_BUYSTOP && pfu.1<OrderOpenPrice()) 
           {
           OrderDelete(OrderTicket());   
           }
           if(OrderType()==OP_SELLSTOP && pfd.1>OrderOpenPrice())  
           {
           OrderDelete(OrderTicket());   
           }
           if(OrderType()==OP_BUY && pfu.1>OrderStopLoss() && pfu.1<cfu)  
           {
            OrderModify(OrderTicket(),
                        OrderOpenPrice(),
                        pfu.1-spread,
                        OrderTakeProfit(),
                        0,
                      Aqua);   
                      }
           if(OrderType()==OP_SELL && pfd.1+spread<OrderStopLoss() && pfd.1>cfd) 
           {
            OrderModify(OrderTicket(),
                        OrderOpenPrice()<
                        pfd.1+spread,
                        OrderTakeProfit(),
                        0,
                      HotPink);   
                      }
         OrderSelect(b.1,SELECT_BY_TICKET);   if(OrderCloseTime()>0) {b.1=0;}
        OrderSelect(s.1,SELECT_BY_TICKET);   if(OrderCloseTime()>0) {s.1=0;} 
           }  
        }
   if(!IsTesting()) PrintComments();
   return(0);
  }//end start
//Functions
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void PrintComments() 
  {
    Comment("Current Time: ",TimeToStr(CurTime(),TIME_MINUTES),"\n",
                                    "UpFrac[0] ",cfu,"\n","UpFrac[1] ",pfu.1,"\n",
                                  "DownFrac[0] ",cfd,"\n","DownFrac[1] ",pfd.1);  
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  double LotsOptimized()  
  {
   double lot;
   int    orders=HistoryTotal();
   int    losses=0;
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
     if(DecreaseFactor>0) 
     {
        for(int i=orders-1;i>=0;i--)  
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         if(OrderProfit()>0) break;
        if(OrderProfit()<0) losses++; 
        }
     if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);   
     }
   if(lot<0.1) lot=0.1;
  return(lot);   
  }//end LotsOptimized
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int TotalTradesThisSymbol(string TradeSymbol) 
  {
   int i, TradesThisSymbol=0;
     for(i=0;i<OrdersTotal();i++)  
     {
      OrderSelect(i,SELECT_BY_POS);
      if(OrderSymbol()==TradeSymbol &&
         OrderMagicNumber()==101 ||
       OrderMagicNumber()==102)   {  TradesThisSymbol++;  }   
       }
  return(TradesThisSymbol);  
  }//end TotalTradesThisSymbol
//+------------------------------------------------------------------+