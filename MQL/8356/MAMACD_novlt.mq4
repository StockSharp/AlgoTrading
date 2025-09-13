//+------------------------------------------------------------------+
//|                                                       MAMACD.mq4 |
//|                                                     FORTRADER.RU |
//|         http://forum.fortrader.ru/showthread.php?p=1183#post1183 |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"


//---- input parameters
extern int       MA1=85;
extern int       MA2=75;
extern int       MA3=5;
extern int fastema=15;
extern int lowema=26;
extern int sl=15;
extern int tp=15;

/*
extern int vltbars=10;//количество баров для подсчета волатильности
extern double deliter=1.5; //делитель текущей волатильности
extern double stoppercent=0.50; // от 1 до 99

extern bool timecontrol=false;
extern string starttime = "07:00:00";
extern string stoptime = "17:00:00";
*/

extern double Lots=0.1;

int startb,starts;

double stoplevel;
int init()
{
 stoplevel=MarketInfo(Symbol(),MODE_SPREAD)+MarketInfo(Symbol(),MODE_STOPLEVEL);
}

  

int start()
  {int buy,sell;

  
    buy=0;sell=0;
     for(int  i=0;i<OrdersTotal();i++)
         {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
           if(OrderType()==OP_BUY){buy=1;}
           if(OrderType()==OP_SELL){sell=1;}
         }  
         

         
double wma1 =iMA(NULL,0,MA1,0,MODE_LWMA,PRICE_LOW,1);
double wma2 =iMA(NULL,0,MA2,0,MODE_LWMA,PRICE_LOW,1);
double ema1 =iMA(NULL,0,MA3,0,MODE_EMA,PRICE_CLOSE,1);

   double macdcurr =iMACD(NULL,0,lowema,lowema,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,lowema,fastema,1,PRICE_CLOSE,MODE_MAIN,2);


 if(ema1<wma1 && ema1<wma2)startb=1;
 if(ema1>wma1 && ema1>wma2)starts=1;

 if(ema1>wma1 && ema1>wma2 && startb==1 && (macdcurr>0 || macdcurr>macdlast) && buy==0)
 {
  
 Print("BUY Bid: "+Bid+" sl: "+sl+" TakeProfit: "+tp);
 OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Ask-sl*Point,Ask+tp*Point,"FORTRADER.RU",0,0,Red);
 
 startb=0;
 }
 
  if(ema1<wma1 && ema1<wma2 && starts==1 && (macdcurr<0 || macdcurr<macdlast)&& sell==0)
 {
  
  Print("SELL Bid: "+Bid+" sl: "+sl+" TakeProfit: "+tp);
 OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Bid+sl*Point,Bid-tp*Point,"FORTRADER.RU",0,0,Red);
 
 starts=0;
 }
  
  

   return(0);
  }



