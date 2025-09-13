//+------------------------------------------------------------------+
//|                                                          aaa.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
extern double step=25;
extern int StepMode=0;
// Если StepMode = 0, то шаг между ордерами фиксированный и равен step
// Если StepMode = 1, то шаг постепенно увеличивается
extern double proffactor=10;
extern double mult=1.5;
extern double lotsbuy=0.01;
extern double lotssell=0.01;  
extern double per_K=200;
extern double per_D=20;
extern double slow=20;
extern double zoneBUY=50;
extern double zoneSELL=50;
extern double Magicbuy=555;
extern double Magicsell=556;
double openpricebuy,openpricesell,lotsbuy2,lotssell2,lastlotbuy,lastlotsell,tpb,tps,cnt,smbuy,smsell,lotstep,
ticketbuy,ticketsell,maxLot,free,balance,lotsell,lotbuy,dig,sig_buy,sig_sell,ask,bid;                           
                                int OrdersTotalMagicbuy(int Magicbuy)
 {
   int j=0;
   int r;
   for (r=0;r<OrdersTotal();r++)
   {
     if(OrderSelect(r,SELECT_BY_POS,MODE_TRADES))
     {
        if (OrderMagicNumber()==Magicbuy) j++;
     }
   }   
 return(j); 
 }

                                int OrdersTotalMagicsell(int Magicsell)
{
   int d=0;
   int n;
   for (n=0;n<OrdersTotal();n++)
   {
     if(OrderSelect(n,SELECT_BY_POS,MODE_TRADES))
     {
        if (OrderMagicNumber()==Magicsell) d++;
     }
   }    
 return(d);
  }     
                                      int orderclosebuy(int ticketbuy)
     {
string symbol = Symbol();
int cnt;
    for(cnt = OrdersTotal(); cnt >= 0; cnt--)
       {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);       
       if(OrderSymbol() == symbol && OrderMagicNumber()==Magicbuy) 
         {
         ticketbuy=OrderTicket();OrderSelect(ticketbuy, SELECT_BY_TICKET, MODE_TRADES);lotsbuy2=OrderLots() ;                         
         double bid = MarketInfo(symbol,MODE_BID); 
         RefreshRates();
         OrderClose(ticketbuy,lotsbuy2,bid,3,Magenta); 
         }
       }
       lotsbuy2=lotsbuy;return(0);
     } 
                                      int orderclosesell(int ticketsell)
     {
string symbol = Symbol();
int cnt;   
    for(cnt = OrdersTotal(); cnt >= 0; cnt--)
       {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);       
       if(OrderSymbol() == symbol && OrderMagicNumber()==Magicsell) 
         {
         ticketsell=OrderTicket();OrderSelect(ticketsell, SELECT_BY_TICKET, MODE_TRADES);lotssell2=OrderLots() ;                         
         double ask = MarketInfo(symbol,MODE_ASK); 
         RefreshRates();
         OrderClose(ticketsell,lotssell2,ask,3, Lime); 
         }
       }
       lotssell2=lotssell;return(0); 
     }
                                                 int start()
  {
//----
  double profitbuy=0;double profitsell=0;
  string symbol = OrderSymbol();
  double spread = MarketInfo(symbol,MODE_SPREAD);
  double minLot = MarketInfo(symbol,MODE_MINLOT);
  if (minLot==0.01){dig=2;maxLot=MarketInfo(symbol,MODE_MAXLOT);}
  if (minLot==0.1){dig=1;maxLot=((AccountBalance()/2)/1000);}
  if(OrdersTotalMagicbuy(Magicbuy)>0)
  {
  double smbuy;
  for (cnt=0;cnt<OrdersTotal();cnt++)
    {
    OrderSelect(cnt,SELECT_BY_POS, MODE_TRADES);
    if (OrderSymbol() == Symbol() && OrderMagicNumber () == Magicbuy) 
      {
      ticketbuy = OrderTicket();OrderSelect(ticketbuy,SELECT_BY_TICKET, MODE_TRADES);
      smbuy = smbuy+OrderLots();openpricebuy = OrderOpenPrice();lastlotbuy = OrderLots();
      }
    }
    {   
    if (smbuy+(NormalizeDouble((lastlotbuy*mult),dig))<maxLot)
      {     
      if(StepMode==0)
        {
        if(Ask<=openpricebuy-step*Point)
          {
          lotsbuy2=lastlotbuy*mult;
          RefreshRates();ticketbuy=OrderSend(Symbol(),OP_BUY,NormalizeDouble(lotsbuy2,dig),Ask,3,0,0,"MartingailExpert",Magicbuy,0,Blue);
          }
        }
      if(StepMode==1)
        {
        if(Ask<=openpricebuy-(step+OrdersTotalMagicbuy(Magicbuy)+OrdersTotalMagicbuy(Magicbuy)-2)*Point)
          {
          lotsbuy2=lastlotbuy*mult;
          RefreshRates();ticketbuy=OrderSend(Symbol(),OP_BUY,NormalizeDouble(lotsbuy2,dig),Ask,3,0,0,"MartingailExpert",Magicbuy,0,Blue);
          } 
        }
      }
    }
  }
  if(OrdersTotalMagicsell(Magicsell)>0)
  {
  double smsell;
  for (cnt=0;cnt<OrdersTotal();cnt++)
    {
    OrderSelect(cnt,SELECT_BY_POS, MODE_TRADES);
    if (OrderSymbol() == Symbol() && OrderMagicNumber () == Magicsell)
      {
      ticketsell = OrderTicket();OrderSelect(ticketsell,SELECT_BY_TICKET, MODE_TRADES);
      smsell = smsell + OrderLots();openpricesell = OrderOpenPrice();lastlotsell = OrderLots();
      }     
    }
    {
    if (smsell+(NormalizeDouble((lastlotsell*mult),dig))<maxLot)
      {
      if(StepMode==0)
        {
        if(Bid>=openpricesell+step*Point)
          {
          lotssell2=lastlotsell*mult;
          RefreshRates();ticketsell=OrderSend(Symbol(),OP_SELL,NormalizeDouble(lotssell2,dig),Bid,3,0,0,"MartingailExpert",Magicsell,0,Red);
          }
        }
      if(StepMode==1)
        {
        if(Bid>=openpricesell+(step+OrdersTotalMagicsell(Magicsell)+OrdersTotalMagicsell(Magicsell)-2)*Point)
          {
          lotssell2=lastlotsell*mult;
          RefreshRates();ticketsell=OrderSend(Symbol(),OP_SELL,NormalizeDouble(lotssell2,dig),Bid,3,0,0,"MartingailExpert",Magicsell,0,Red);
          }
        }
      }
    }  
  }
  if(OrdersTotalMagicbuy(Magicbuy)<1)
  { 
  if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,0,1)>iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)
  && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)>zoneBUY)ticketbuy = OrderSend(Symbol(),OP_BUY,lotsbuy,Ask,3,0,0,"MartingailExpert",Magicbuy,0,Blue);
  }
  if(OrdersTotalMagicsell(Magicsell)<1)
  {
  if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,0,1)<iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)
  && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,1)<zoneSELL)ticketsell = OrderSend(Symbol(),OP_SELL,lotssell,Bid,3,0,0,"MartingailExpert",Magicsell,0,Red);
  }
  for (cnt=0;cnt<OrdersTotal();cnt++)
  {
  OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
  if (OrderSymbol()==Symbol() && OrderMagicNumber () == Magicbuy)
    {
    ticketbuy = OrderTicket();OrderSelect(ticketbuy,SELECT_BY_TICKET, MODE_TRADES);profitbuy = profitbuy+OrderProfit() ;
    openpricebuy = OrderOpenPrice();
    }
  }  
  tpb = (OrdersTotalMagicbuy(Magicbuy)*proffactor*Point)+openpricebuy;
  double bid = MarketInfo(Symbol(),MODE_BID);
  if (profitbuy>0)
  {
  if (Bid>=tpb) orderclosebuy(ticketbuy);
  }
  for (cnt=0;cnt<OrdersTotal();cnt++)
  {   
  OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
  if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magicsell)
    {
    ticketsell = OrderTicket();OrderSelect(ticketsell,SELECT_BY_TICKET, MODE_TRADES);profitsell = profitsell+OrderProfit();
    openpricesell = OrderOpenPrice(); 
    }
  }
  tps = openpricesell-(OrdersTotalMagicsell(Magicsell)*proffactor*Point);
  double ask = MarketInfo(Symbol(),MODE_ASK);    
  if (profitsell>0)
  {
  if (Ask<=tps)orderclosesell(ticketsell);    
  }
  free = AccountFreeMargin();balance = AccountBalance();    
  for (cnt=0;cnt< OrdersTotal();cnt++)
  {   
  OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
  if (OrderSymbol()==Symbol() && OrderMagicNumber () == Magicbuy)  ticketbuy = OrderTicket();
  if (OrderSymbol()==Symbol() && OrderMagicNumber () == Magicsell) ticketsell = OrderTicket();
  }
  if (OrdersTotalMagicbuy(Magicbuy)==0)
  {
  profitbuy=0;ticketbuy=0;tpb=0;
  }
  if (OrdersTotalMagicsell(Magicsell)==0)
  {
  profitsell=0;ticketsell=0;tps=0;
  }
  Comment("FreeMargin = ",NormalizeDouble(free,0),"  Balance = ",NormalizeDouble(balance,0),"  maxLot = ",NormalizeDouble(maxLot,dig),"\n",
  "Totalbuy = ",OrdersTotalMagicbuy(Magicbuy),"  Lot = ",smbuy,"  Totalsell = ",OrdersTotalMagicsell(Magicsell),"  Lot = ",smsell,"\n",
  "---------------------------------------------------------------","\n","Profitbuy = ",profitbuy,"\n",
  "Profitsell = ",profitsell);
//----
   for(int ii=0; ii<2; ii+=2)
     {
      ObjectDelete("rect"+ii);
      ObjectCreate("rect"+ii,OBJ_HLINE, 0, 0,tps);
      ObjectSet("rect"+ii, OBJPROP_COLOR, Red);
      ObjectSet("rect"+ii, OBJPROP_WIDTH, 1);
      ObjectSet("rect"+ii, OBJPROP_RAY, False);
      }    
   for(int rr=0; rr<2; rr+=2)
      {
      ObjectDelete("rect1"+rr);
      ObjectCreate("rect1"+rr,OBJ_HLINE, 0, 0,tpb);      
      ObjectSet("rect1"+rr, OBJPROP_COLOR, Blue);
      ObjectSet("rect1"+rr, OBJPROP_WIDTH, 1);
      ObjectSet("rect1"+rr, OBJPROP_RAY, False);     
     }
   return(0);
}  
//+------------------------------------------------------------------+rSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magicbuy)
        {
        ticketbuy = OrderTicket();OrderSelect(ticketbuy,SELECT_BY_TICKET, MODE_TRADES);
        openpricebuy = OrderOpenPrice();slbuy= OrderStopLoss();ticketbuy = OrderTicket();
        }
      }           
    
    if(Bid-openpricebuy>Point*TrailingStop)
      {
      if((slbuy<Bid-Point*TrailingStop)|| (slbuy==0))
        {
         if(TrailingStop>0)
           {
           OrderModify(ticketbuy,openpricebuy,NormalizeDouble(Bid-Point*TrailingStop,dig2),0,0,Gold);
           return(0);
           }
        }
     }
  }
  if (OrdersTotalMagicsell(Magicsell)>1)
  {
    for (cnt=0;cnt<OrdersTotal();cnt++)
    {   
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magicsell)
        {
        ticketsell = OrderTicket();OrderSelect(ticketsell,SELECT_BY_TICKET, MODE_TRADES);
        openpricesell = OrderOpenPrice(); slsell= OrderStopLoss();
        }
     }                
  
   if(openpricesell-Ask>Point*TrailingStop)
      {
      if((slsell>Ask+Point*TrailingStop)|| (slsell==0))
         {
         if(TrailingStop>0)
            {
            OrderModify(ticketsell,openpricesell,NormalizeDouble(Ask+Point*TrailingStop,dig2),0,0,Gold);
             return(0);       
            }
         }
      }       
   }
   Comment("FreeMargin = ",NormalizeDouble(free,0),"  Balance = ",NormalizeDouble(balance,0),"  maxLot = ",NormalizeDouble(maxLot,dig),"\n",
  "Totalbuy = ",OrdersTotalMagicbuy(Magicbuy),"  Lot = ",smbuy,"  Totalsell = ",OrdersTotalMagicsell(Magicsell),"  Lot = ",smsell,"\n",
  "---------------------------------------------------------------","\n","Profitbuy = ",profitbuy,"\n",
  "Profitsell = ",profitsell);
   return(0);
}  
//+------------------------------------------------------------------+