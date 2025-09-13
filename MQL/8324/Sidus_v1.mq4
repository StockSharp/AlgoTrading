//+------------------------------------------------------------------+
//|                                          Name:     Sidus v1.mq4  |
//|                                         E-mai:  Falmera@mail.ru  |
//|                                           ICQ:        436265161  |
//+------------------------------------------------------------------+

// Параметры

extern int       FastEMA=23;
extern int       SlowEMA=62;
extern int       FastEMA2=18;
extern int       SlowEMA2=54;
extern int       RSIPeriod=67;
extern int       RSIPeriod2=97;
extern int       b1 = 63;
extern int       c1 = 59;
extern int       b12 = -57;
extern int       c12 = 60;
extern int       pipdiffCurrent2 = 24;
extern int       rsi_sig2 = -97;
extern int       tp=95;
extern int       st=100;
extern int       tp2=17;
extern int       st2=69;
extern double    Lots = 0.50;

//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
int start()
  {
//----
int MagicNumber = 12345;
int MagicNumber2 = 12346;

double ExtMapBuffer1=iMA(NULL,0,FastEMA,0,MODE_EMA,PRICE_CLOSE,1);
double ExtMapBuffer2=iMA(NULL,0,SlowEMA,0,MODE_EMA,PRICE_CLOSE,1);
double rsi_sig = iRSI(NULL, 0, RSIPeriod, PRICE_CLOSE, 1);  
double pipdiffCurrent=(ExtMapBuffer1-ExtMapBuffer2);   
//----
double ExtMapBuffer12=iMA(NULL,0,FastEMA2,0,MODE_EMA,PRICE_CLOSE,1);
double ExtMapBuffer22=iMA(NULL,0,SlowEMA2,0,MODE_EMA,PRICE_CLOSE,1);
double rsi_sig2 = iRSI(NULL, 0, RSIPeriod2, PRICE_CLOSE, 1);  
pipdiffCurrent2=(ExtMapBuffer1-ExtMapBuffer2);
//----
double LotsToBid;
double LotsToBid2;
if (pipdiffCurrent<b1 && rsi_sig<c1)
  {
    if (Volume[0]>10)return(0);
      if(GetActiveOrders(MagicNumber)) return(0);
         OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-st*Point,Ask+tp*Point,"My order 1",MagicNumber,0,Green);   
     }
//-----------------------------------
if (pipdiffCurrent2>b12 && rsi_sig2>c12)
      {
    if (Volume[0]>10)return(0);
      if(GetActiveOrders2(MagicNumber2)) return(0);
        OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+st2*Point,Bid-tp2*Point,"My order 2",MagicNumber2,0,Maroon); 
      }               
               
               
   return(0);
  }
//+------------------------------------------------------------------+
bool GetActiveOrders(int MagicNumber)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber) continue;
      
      if(OrderType() == OP_SELL || OrderType() == OP_BUY)
      {
         return (true);
      }
   }
   
   return (false);
}
bool GetActiveOrders2(int MagicNumber2)
{
   for(int i = 0; i < OrdersTotal(); i++)
   {
      // already closed
      if(OrderSelect(i, SELECT_BY_POS) == false) continue;
      // not current symbol
      if(OrderSymbol() != Symbol()) continue;
      // order was opened in another way
      if(OrderMagicNumber() != MagicNumber2) continue;
      
      if(OrderType() == OP_SELL || OrderType() == OP_BUY)
      {
         return (true);
      }
   }
   
   return (false);
}


