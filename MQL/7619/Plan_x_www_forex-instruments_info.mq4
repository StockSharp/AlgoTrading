//+------------------------------------------------------------------+
//|                                                       plan x.mq4 |
//|                                   Copyright © 2005, Peter Ingram |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Peter Ingram"
#property link      "http://www.metaquotes.net"
//----
extern double  Lots              =0.1;
extern int     Slippage          =2;
extern int     LongTarget        =25;
extern int     ShortTarget       =20;
extern int     InitialSL         =25;
extern int     TrailStopTrigger  =10;
extern int     TrailStopPips     =5;
extern double  BeginTime         =11;
extern double  EndTime           =15;
extern int     MagicNumber       =411;
extern string  comment           ="plan x";
//----
int shift,c,b,i,s,ticket;
double tsl;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init(){return(0);}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit(){return(0);}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start()
  {
   if(Period()!=PERIOD_M15) {Alert("Expert for 15m Chart!"); return(0);}
   PosCounter();
   //
   shift=iBarShift(NULL,PERIOD_M15,begintime()); //Print(shift);
   //
     if(CurTime()<endtime() && CurTime()>subsequentclose())   
     {//15min timeframe
        if(Close[1]>(Close[shift]+(LongTarget*Point)) && b==0)  
        {
         ticket=OrderSend(Symbol(),
                          OP_BUY,
                          Lots,
                          Ask,
                          Slippage,
                          Ask-(InitialSL*Point),
                          0,//OrderTakeProfit
                          comment,
                          MagicNumber,
                          0,//Order expiration time/date
                          Aqua);
           if(ticket>0)   
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            {  Print(ticket); 
            }
            else Print("Error Opening Buy Order: ",GetLastError());
           return(0);  
            }
           }//buy
        if(Close[1]<(Close[shift]-(ShortTarget*Point)) && s==0)   
        {
         ticket=OrderSend(Symbol(),
                          OP_SELL,
                          Lots,
                          Bid,
                          Slippage,
                          Bid+(InitialSL*Point),
                          0,//OrderTakeProfit
                          comment,
                          MagicNumber,
                          0,//Order expiration time/date
                          Red);
           if(ticket>0)   
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
            {
              Print(ticket); 
            }
            else Print("Error Opening Sell Order: ",GetLastError());
           return(0);  
            }
           }
          }//sell
     if(!IsTesting()) 
     {
        for(i=0;i<=OrdersTotal();i++)   
        {
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
           if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
           {
              if(OrderType()==OP_BUY) 
              {
               Print(OrderOpenPrice(),"  ",OrderStopLoss());
                 if(OrderStopLoss()<OrderOpenPrice() && Bid-OrderOpenPrice()>=(TrailStopTrigger*Point)) 
                 {
                 tsl=OrderOpenPrice()+(TrailStopPips*Point);}
                 if(OrderStopLoss()>OrderOpenPrice() && Bid-OrderStopLoss()>=(TrailStopTrigger*Point))  
                 {
                 tsl=OrderStopLoss()+(TrailStopPips*Point);
                 }
               OrderModify(OrderTicket(),
                           OrderOpenPrice(),
                           tsl,
                           OrderTakeProfit(),
                           OrderExpiration(),
                         LightGreen);
                         }
              if(OrderType()==OP_SELL)   
              {
               Print(OrderOpenPrice(),"  ",OrderStopLoss());
                 if(OrderStopLoss()>OrderOpenPrice() && OrderOpenPrice()-Ask>=(TrailStopTrigger*Point)) 
                 {
                 tsl=OrderOpenPrice()-(TrailStopPips*Point);}
                 if(OrderStopLoss()<OrderOpenPrice() && OrderStopLoss()-Ask>=(TrailStopTrigger*Point))  
                 {
                 tsl=OrderStopLoss()-(TrailStopPips*Point);}
               OrderModify(OrderTicket(),
                           OrderOpenPrice(),
                           tsl,
                           OrderTakeProfit(),
                           OrderExpiration(),
                         HotPink);
                         }
                }
              }
            }//live trailstop
     if(IsTesting()) 
     {
        for(i=0;i<=OrdersTotal();i++)   
        {
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
           if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
           {
              if(OrderType()==OP_BUY) 
              {
               Print(OrderOpenPrice(),"  ",OrderStopLoss());
                 if(OrderStopLoss()<OrderOpenPrice() && Close[0]-OrderOpenPrice()>=(TrailStopTrigger*Point)) 
                 {
                 tsl=OrderOpenPrice()+(TrailStopPips*Point);}
                 if(OrderStopLoss()>OrderOpenPrice() && Close[0]-OrderStopLoss()>=(TrailStopTrigger*Point))  
                 {
                 tsl=OrderStopLoss()+(TrailStopPips*Point);}
               OrderModify(OrderTicket(),
                           OrderOpenPrice(),
                           tsl,
                           OrderTakeProfit(),
                           OrderExpiration(),
                         LightGreen);
                         }
              if(OrderType()==OP_SELL)   
              {
               Print(OrderOpenPrice(),"  ",OrderStopLoss());
                 if(OrderStopLoss()>OrderOpenPrice() && OrderOpenPrice()-Close[0]>=(TrailStopTrigger*Point)) 
                 {
                 tsl=OrderOpenPrice()-(TrailStopPips*Point);}
                 if(OrderStopLoss()<OrderOpenPrice() && OrderStopLoss()-Close[0]>=(TrailStopTrigger*Point))  
                 {
                 tsl=OrderStopLoss()-(TrailStopPips*Point);}
               OrderModify(OrderTicket(),
                           OrderOpenPrice(),
                           tsl,
                           OrderTakeProfit(),
                           OrderExpiration(),
                         HotPink);
                         }
                 }
              }
           }//backtest trailstop
   if(!IsTesting()) printcomments();
  return(0);}
//Functions
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  datetime begintime()   
  {
   string date=TimeToStr(CurTime(),TIME_DATE);
   string hour=DoubleToStr(BeginTime,0);
   string minutes=":00";
   return(StrToTime(date+" "+hour+minutes));
   }//end
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  datetime endtime()   
  {
   string date=TimeToStr(CurTime(),TIME_DATE);
   string hour=DoubleToStr(EndTime,0);
   string minutes=":00";
   return(StrToTime(date+" "+hour+minutes));
   }//end
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  datetime subsequentclose()   
  {
   string date=TimeToStr(CurTime(),TIME_DATE);
   string hour=DoubleToStr(BeginTime,0);
   string minutes=":15";
   return(StrToTime(date+" "+hour+minutes));
   }//end
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void printcomments() 
  { 
  Comment("Last Tick: ",TimeToStr(CurTime(),TIME_DATE|TIME_SECONDS),"\n",
  "Begin Time: ",TimeToStr(begintime(),TIME_DATE|TIME_MINUTES),"\n",
  "Begin Bar Close Price:",Close[shift],"\n",
  "End Time: ",TimeToStr(endtime(),TIME_DATE|TIME_MINUTES));  
  }//end
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void PosCounter() 
  {
   b=0;s=0;
     for(int cnt=0;cnt<=OrdersTotal();cnt++)   
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
        if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
        {
         if(OrderType()==OP_SELL) s++;
        if(OrderType()==OP_BUY)b++;}}}
//+------------------------------------------------------------------+