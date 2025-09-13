//+------------------------------------------------------------------+
//|                                                   Envelope 2.mq4 |
//|                                                         tageiger |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "tageiger"
#property link      "http://www.metaquotes.net"
//---- input parameters
extern int        BandTimeFrame     =0; //BBand time frame: 0=chart,60=1hr,240=4hr, etc.
extern int        BandPeriod        =144;
extern int        BandDeviation     =4;
extern int        BandShift         =0;
extern int        DataShift         =0;
extern int        Ma_BandTSL        =1;//0=iMA trailing stoploss  1=Opposite Band TSL
extern double     FirstTP           =21.0;
extern double     SecondTP          =34.0;
extern double     ThirdTP           =55.0;
extern int        TimeOpen          =0;
extern int        TimeClose         =23;
extern double     Lots              =0.1;
extern double     MaximumRisk       =0.02;
extern double     DecreaseFactor    =3;
//----
int               b1,b2,b3,s1,s2,s3;
double            TSL               =0;
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int      p=0;p=BandPeriod;
   int      btf=0;btf=BandTimeFrame;
   double   d=0;d=BandDeviation;
   double   btp1,btp2,btp3,stp1,stp2,stp3;
   double   bline=0,sline=0,ma=0;
   int      cnt, ticket, total;
//----
   ma=iMA(NULL,btf,p,BandShift,1,PRICE_CLOSE,DataShift);
   bline=iBands(NULL,btf,p,d,BandShift,PRICE_CLOSE,MODE_UPPER,DataShift);
   sline=iBands(NULL,btf,p,d,BandShift,PRICE_CLOSE,MODE_LOWER,DataShift);
//----
   total=OrdersTotal();
   if(OrdersTotal()==0)
   {b1=0;b2=0;b3=0;s1=0;s2=0;s3=0;}
   if(OrdersTotal()>0)
     {
      //Print("Total Orders:",OrdersTotal());
      //Print(b1," ",b2," ",b3," ",s1," ",s2," ",s3);
      for(cnt=0;cnt<total;cnt++)
        {
         OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
         if(OrderMagicNumber()==2)
         {b1=OrderTicket(); }
         if(OrderMagicNumber()==4)
         {b2=OrderTicket(); }
         if(OrderMagicNumber()==6)
         {b3=OrderTicket(); }
         if(OrderMagicNumber()==1)
         {s1=OrderTicket(); }
         if(OrderMagicNumber()==3)
         {s2=OrderTicket(); }
         if(OrderMagicNumber()==5)
         {s3=OrderTicket(); }
        }
     }
   if(b1==0)
     {
      if(Hour()>TimeOpen && Hour()<TimeClose)
        {
         if(bline>Close[0] && sline<Close[0])
           {
            btp1=(NormalizeDouble(bline,4))+(FirstTP*Point);
            ticket=OrderSend(Symbol(),
                              OP_BUYSTOP,
                              LotsOptimized(),
                              (NormalizeDouble(bline,4)),
                              0,
                              (NormalizeDouble(sline,4)),
                              btp1,
                              "",
                              2,
                              TimeClose,
                              Aqua);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                 {
                  b1=ticket;
                  Print(ticket);
                 }
               else Print("Error Opening BuyStop Order: ",GetLastError());
               return(0);
              }
           }
        }
     }
   if(b2==0)
     {
      if(Hour()>TimeOpen && Hour()<TimeClose)
        {
         if(bline>Close[0] && sline<Close[0])
           {
            btp2=(NormalizeDouble(bline,4))+(SecondTP*Point);
            ticket=OrderSend(Symbol(),
                              OP_BUYSTOP,
                              LotsOptimized(),
                              (NormalizeDouble(bline,4)),
                              0,
                              (NormalizeDouble(sline,4)),
                              btp2,
                              "",
                              4,
                              TimeClose,
                              Aqua);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                 {
                  b2=ticket;
                  Print(ticket);
                 }
               else Print("Error Opening BuyStop Order: ",GetLastError());
               return(0);
              }
           }
        }
     }
   if(b3==0)
     {
      if(Hour()>TimeOpen && Hour()<TimeClose)
        {
         if(bline>Close[0] && sline<Close[0])
           {
            btp3=(NormalizeDouble(bline,4))+(ThirdTP*Point);
            ticket=OrderSend(Symbol(),
                              OP_BUYSTOP,
                              LotsOptimized(),
                              (NormalizeDouble(bline,4)),
                              0,
                              (NormalizeDouble(sline,4)),
                              btp3,
                              "",
                              6,
                              TimeClose,
                              Aqua);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                 {
                  b3=ticket;
                  Print(ticket);
                 }
               else Print("Error Opening BuyStop Order: ",GetLastError());
               return(0);
              }
           }
        }
     }
   if(s1==0)
     {
      if(Hour()>TimeOpen && Hour()<TimeClose)
        {
         if(bline>Close[0] && sline<Close[0])
           {
            stp1=NormalizeDouble(sline,4)-(FirstTP*Point);
            ticket=OrderSend(Symbol(),
                              OP_SELLSTOP,
                              LotsOptimized(),
                              (NormalizeDouble(sline,4)),
                              0,
                              (NormalizeDouble(bline,4)),
                              stp1,
                              "",
                              1,
                              TimeClose,
                              HotPink);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                 {
                  s1=ticket;
                  Print(ticket);
                 }
               else Print("Error Opening SellStop Order: ",GetLastError());
               return(0);
              }
           }
        }
     }
   if(s2==0)
     {
      if(Hour()>TimeOpen && Hour()<TimeClose)
        {
         if(bline>Close[0] && sline<Close[0])
           {
            stp2=NormalizeDouble(sline,4)-(SecondTP*Point);
            ticket=OrderSend(Symbol(),
                              OP_SELLSTOP,
                              LotsOptimized(),
                              (NormalizeDouble(sline,4)),
                              0,
                              (NormalizeDouble(bline,4)),
                              stp2,
                              "",
                              3,
                              TimeClose,
                              HotPink);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                 {
                  s2=ticket;
                  Print(ticket);
                 }
               else Print("Error Opening SellStop Order: ",GetLastError());
               return(0);
              }
           }
        }
     }
   if(s3==0)
     {
      if(Hour()>TimeOpen && Hour()<TimeClose)
        {
         if(bline>Close[0] && sline<Close[0])
           {
            stp3=NormalizeDouble(sline,4)-(ThirdTP*Point);
            ticket=OrderSend(Symbol(),
                              OP_SELLSTOP,
                              LotsOptimized(),
                              (NormalizeDouble(sline,4)),
                              0,
                              (NormalizeDouble(bline,4)),
                              stp3,
                              "",
                              5,
                              TimeClose,
                              HotPink);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                 {
                  s3=ticket;
                  Print(ticket);
                 }
               else Print("Error Opening SellStop Order: ",GetLastError());
               return(0);
              }
           }
        }
     }
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY)
        {
         if(Ma_BandTSL==0) {TSL=NormalizeDouble(ma,4); }
         if(Ma_BandTSL==1) {TSL=NormalizeDouble(sline,4); }
         if(Close[0]>OrderOpenPrice())
           {
            if((Close[0]>sline) && (TSL>OrderStopLoss()))
              {
               double bsl;bsl=TSL;
               OrderModify(OrderTicket(),
                           OrderOpenPrice(),
                           bsl,
                           OrderTakeProfit(),
                           0,//Order expiration server date/time
                           Green);
               Sleep(10000);
              }
           }
        }
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_SELL)
        {
         if(Ma_BandTSL==0) {TSL=NormalizeDouble(ma,4); }
         if(Ma_BandTSL==1) {TSL=NormalizeDouble(bline,4); }
         if(Close[0]<OrderOpenPrice())
           {
            if((Close[0]<bline) && (TSL<OrderStopLoss()))
              {
               double ssl;ssl=TSL;
               OrderModify(OrderTicket(),
                           OrderOpenPrice(),
                           ssl,
                           OrderTakeProfit(),
                           0,//Order expiration server date/time
                           Red);
               Sleep(10000);
              }
           }
        }
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()==OP_BUYSTOP)
        {
         if(bline<OrderOpenPrice())
           {
            if(OrderTicket()==b1)
              {
               btp1=(NormalizeDouble(bline,4))+(FirstTP*Point);
               OrderModify(   b1,
                              NormalizeDouble(bline,4),
                              NormalizeDouble(sline,4),
                              btp1,
                              0,//datetime expirtation
                              MediumSeaGreen);
               //return;
              }
            if(OrderTicket()==b2)
              {
               btp2=(NormalizeDouble(bline,4))+(SecondTP*Point);
               OrderModify(   b2,
                              NormalizeDouble(bline,4),
                              NormalizeDouble(sline,4),
                              btp2,
                              0,//datetime expirtation
                              MediumSeaGreen);
               //return;
              }
            if(OrderTicket()==b3)
              {
               btp3=(NormalizeDouble(bline,4))+(ThirdTP*Point);
               OrderModify(   b3,
                              NormalizeDouble(bline,4),
                              NormalizeDouble(sline,4),
                              btp3,
                              0,//datetime expirtation
                              MediumSeaGreen);
               //return;
              }
           }
        }
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()==OP_SELLSTOP)
        {
         if(sline>OrderOpenPrice())
           {
            if(OrderTicket()==s1)
              {
               stp1=(NormalizeDouble(sline,4))-(FirstTP*Point);
               OrderModify(   s1,
                              NormalizeDouble(sline,4),
                              NormalizeDouble(bline,4),
                              stp1,
                              0,//datetime expirtation
                              LightCoral);
               //return;
              }
            if(OrderTicket()==s2)
              {
               stp2=(NormalizeDouble(sline,4))-(SecondTP*Point);
               OrderModify(   s2,
                              NormalizeDouble(sline,4),
                              NormalizeDouble(bline,4),
                              stp2,
                              0,//datetime expirtation
                              LightCoral);
               //return;
              }
            if(OrderTicket()==s3)
              {
               stp3=(NormalizeDouble(sline,4))-(ThirdTP*Point);
               OrderModify(   s3,
                              NormalizeDouble(sline,4),
                              NormalizeDouble(bline,4),
                              stp3,
                              0,//datetime expirtation
                              LightCoral);
               //return;
              }
           }
        }
      OrderSelect(b1,SELECT_BY_TICKET);
      if(OrderClosePrice()>0) {b1=0;}
      OrderSelect(b2,SELECT_BY_TICKET);
      if(OrderClosePrice()>0) {b2=0;}
      OrderSelect(b3,SELECT_BY_TICKET);
      if(OrderClosePrice()>0) {b3=0;}
      OrderSelect(s1,SELECT_BY_TICKET);
      if(OrderClosePrice()>0) {s1=0;}
      OrderSelect(s2,SELECT_BY_TICKET);
      if(OrderClosePrice()>0) {s2=0;}
      OrderSelect(s3,SELECT_BY_TICKET);
      if(OrderClosePrice()>0) {s3=0;}
/*      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);   
      if(Hour()==23 && OrderType()==OP_BUYSTOP)
         {
         OrderDelete(OrderTicket());
         if(OrderTicket()==b1) {b1=0; return;}
         if(OrderTicket()==b2) {b2=0; return;}
         if(OrderTicket()==b3) {b3=0; return;}                  
         }
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);            
      if(Hour()==23 && OrderType()==OP_SELLSTOP)
         {
         OrderDelete(OrderTicket());
         if(OrderTicket()==s1) {s1=0; return;}
         if(OrderTicket()==s2) {s2=0; return;}
         if(OrderTicket()==s3) {s3=0; return;}
         }*/
     }
  }
//+------------------------------------------------------------------+