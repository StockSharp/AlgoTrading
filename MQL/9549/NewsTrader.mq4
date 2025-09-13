//+------------------------------------------------------------------+
//|                                                   NewsTrader.mq4 |
//|                                                     Forex Fellow |
//|                                              www.forexfellow.com |
//+------------------------------------------------------------------+
#property copyright "Forex Fellow"
#property link      "www.forexfellow.com"

int cnt, ticket =0, ticket2=0, total;
extern int lot = 1;
extern int sl = 10;
extern int tp = 10;
extern int bias = 20; //we place our order 20 pips from current price

double orderAsk;
double orderBid;
string OrderCloseDate;
      
int init()
  {
  Print(MarketInfo(Symbol(), MODE_STOPLEVEL));
   return(0);
  }
int deinit()
  {
   return(0);
  }
int start()
  {
   
   //we have to know the time and date of news publication
   //I don't want to write what sombody else has written here http://articles.mql4.com/523
   //we can use this indicator to get the date and time of news publications
   //I have put here some example date and 
   int newsDateYear = 2010;
   int newsDateMonth = 3;
   int newsDateDay = 8;
   int newsDateHour = 1;
   int newsDateMinute = 30;
   
   //we need to open order before news publication
   newsDateMinute -= 10; //10 minutes before publication
   string orderOpenDate = newsDateDay + "-" + newsDateMonth + "-" + newsDateYear 
                                                + " " + newsDateHour + ":" + newsDateMinute + ":00";
   int currentYear = Year();
   int currentMonth = Month();
   int currentDay = Day();
   int currentHour = Hour();
   int currentMinute = Minute();
   
   //we get current time
   string currentDate = currentDay + "-" + currentMonth + "-" + currentYear 
                                                + " " + currentHour + ":" + currentMinute + ":00";
                                                             
   
   if(orderOpenDate == currentDate)
   { 
      //we place 2 orders: buy stop and sell stop
      if(ticket < 1)
      {
         orderAsk = Ask - bias * Point;
         orderBid = Bid - bias * Point;
         ticket=OrderSend(Symbol(),OP_SELLSTOP,lot,orderBid,1,orderAsk+Point*sl,orderBid-tp*Point,"NewsTrader",2,0,Red); 
      }
      if(ticket2 < 1)
      {
         orderAsk = Ask + bias * Point;
         orderBid = Bid + bias * Point;
         ticket2=OrderSend(Symbol(),OP_BUYSTOP,lot,orderAsk,1,orderBid-Point*sl,orderAsk+tp*Point,"NewsTrader",2,0,Green); 
      }         
   }
   
   
   return(0);
  }

