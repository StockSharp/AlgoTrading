//+------------------------------------------------------------------+
//|                                                CCFp(advisor).mq4 |
//|                                 m_a_sim@mail.ru - Simakov Mikhail|
//|                             http://www.mql4.com/ru/users/m_a_sim |
//+------------------------------------------------------------------+

#property copyright "m_a_sim@mail.ru - Simakov Mikhail"
#property link      "http://www.mql4.com/ru/users/m_a_sim"


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

extern int MA_Method=1;
extern int Price=0;
extern int Fast=3;
extern int Slow=5;
extern int sl=200;
extern double lot=0.1;
extern int magicMAX=101;
extern int magicMIN=201;

int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int bar;
double  H,A0,A1,A2;
int MAX,MIN,MAX1,MIN1;
double maxd,mind;
int start()
  {
//----
      int   i, j,jj,ticket,simMAX,simMIN;
    
//if (bar!=Bars ){
MAX=0;
MIN=0;
maxd=iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,0,1);
mind=iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,0,1);
for (i=0;i<=7;i++){
if (maxd<iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,i,1))
{maxd=iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,i,1);
MAX=i;
}
if (mind>iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,i,1))
{mind=iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,i,1);
MIN=i;
}
}
MAX1=0;
MIN1=0;
for (i=0;i<=7;i++){
if (iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,MAX,2)<iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,i,2))
{MAX1=1;}
if (iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,MIN,2)>iCustom(NULL,0,"CCFp",MA_Method,Price,Fast,Slow,i,2))
{MIN1=1;}
}
//bar=Bars;
//}
Comment(MAX,"  ",MIN);


//MAX=====================================
jj=0;
 if (OrdersTotal()>0){
 for(i=0;i<OrdersTotal();i++){
 OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
 if(OrderMagicNumber()==magicMAX){jj=1;}
 }}
if (jj==0 ){
if (MAX==1&&MAX1==1){ticket=OrderSend("EURUSD",OP_BUY ,lot,NormalizeDouble(MarketInfo("EURUSD",MODE_ASK),MarketInfo("EURUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("EURUSD",MODE_ASK),MarketInfo("EURUSD",MODE_DIGITS))-sl*MarketInfo("EURUSD",MODE_POINT),0," ",magicMAX,0, Blue );}
if (MAX==2&&MAX1==1){ticket=OrderSend("GBPUSD",OP_BUY ,lot,NormalizeDouble(MarketInfo("GBPUSD",MODE_ASK),MarketInfo("GBPUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("GBPUSD",MODE_ASK),MarketInfo("GBPUSD",MODE_DIGITS))-sl*MarketInfo("GBPUSD",MODE_POINT),0," ",magicMAX,0, Blue );}
if (MAX==3&&MAX1==1){ticket=OrderSend("USDCHF",OP_SELL,lot,NormalizeDouble(MarketInfo("USDCHF",MODE_BID),MarketInfo("USDCHF",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("USDCHF",MODE_BID),MarketInfo("USDCHF",MODE_DIGITS))+sl*MarketInfo("USDCHF",MODE_POINT),0," ",magicMAX,0, Blue );}
if (MAX==4&&MAX1==1){ticket=OrderSend("USDJPY",OP_SELL,lot,NormalizeDouble(MarketInfo("USDJPY",MODE_BID),MarketInfo("USDJPY",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("USDJPY",MODE_BID),MarketInfo("USDJPY",MODE_DIGITS))+sl*MarketInfo("USDJPY",MODE_POINT),0," ",magicMAX,0, Blue );}
if (MAX==5&&MAX1==1){ticket=OrderSend("AUDUSD",OP_BUY ,lot,NormalizeDouble(MarketInfo("AUDUSD",MODE_ASK),MarketInfo("AUDUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("AUDUSD",MODE_ASK),MarketInfo("AUDUSD",MODE_DIGITS))-sl*MarketInfo("AUDUSD",MODE_POINT),0," ",magicMAX,0, Blue );}
if (MAX==6&&MAX1==1){ticket=OrderSend("USDCAD",OP_SELL,lot,NormalizeDouble(MarketInfo("USDCAD",MODE_BID),MarketInfo("USDCAD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("USDCAD",MODE_BID),MarketInfo("USDCAD",MODE_DIGITS))+sl*MarketInfo("USDCAD",MODE_POINT),0," ",magicMAX,0, Blue );}
if (MAX==7&&MAX1==1){ticket=OrderSend("NZDUSD",OP_BUY ,lot,NormalizeDouble(MarketInfo("NZDUSD",MODE_ASK),MarketInfo("NZDUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("NZDUSD",MODE_ASK),MarketInfo("NZDUSD",MODE_DIGITS))-sl*MarketInfo("NZDUSD",MODE_POINT),0," ",magicMAX,0, Blue );}
}
  
  
  //MIN================================

jj=0;
 if (OrdersTotal()>0){
 for(i=0;i<OrdersTotal();i++){
 OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
 if(OrderMagicNumber()==magicMIN){jj=1;}
 }}

if (jj==0 ){
if (MIN==1&&MIN1==1){ticket=OrderSend("EURUSD",OP_SELL,lot,NormalizeDouble(MarketInfo("EURUSD",MODE_BID),MarketInfo("EURUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("EURUSD",MODE_BID),MarketInfo("EURUSD",MODE_DIGITS))+sl*MarketInfo("EURUSD",MODE_POINT),0," ",magicMIN,0, Blue );}
if (MIN==2&&MIN1==1){ticket=OrderSend("GBPUSD",OP_SELL,lot,NormalizeDouble(MarketInfo("GBPUSD",MODE_BID),MarketInfo("GBPUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("GBPUSD",MODE_BID),MarketInfo("GBPUSD",MODE_DIGITS))+sl*MarketInfo("GBPUSD",MODE_POINT),0," ",magicMIN,0, Blue );}
if (MIN==3&&MIN1==1){ticket=OrderSend("USDCHF",OP_BUY ,lot,NormalizeDouble(MarketInfo("USDCHF",MODE_ASK),MarketInfo("USDCHF",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("USDCHF",MODE_ASK),MarketInfo("USDCHF",MODE_DIGITS))-sl*MarketInfo("USDCHF",MODE_POINT),0," ",magicMIN,0, Blue );}
if (MIN==4&&MIN1==1){ticket=OrderSend("USDJPY",OP_BUY ,lot,NormalizeDouble(MarketInfo("USDJPY",MODE_ASK),MarketInfo("USDJPY",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("USDJPY",MODE_ASK),MarketInfo("USDJPY",MODE_DIGITS))-sl*MarketInfo("USDJPY",MODE_POINT),0," ",magicMIN,0, Blue );}
if (MIN==5&&MIN1==1){ticket=OrderSend("AUDUSD",OP_SELL,lot,NormalizeDouble(MarketInfo("AUDUSD",MODE_BID),MarketInfo("AUDUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("AUDUSD",MODE_BID),MarketInfo("AUDUSD",MODE_DIGITS))+sl*MarketInfo("AUDUSD",MODE_POINT),0," ",magicMIN,0, Blue );}
if (MIN==6&&MIN1==1){ticket=OrderSend("USDCAD",OP_BUY ,lot,NormalizeDouble(MarketInfo("USDCAD",MODE_ASK),MarketInfo("USDCAD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("USDCAD",MODE_ASK),MarketInfo("USDCAD",MODE_DIGITS))-sl*MarketInfo("USDCAD",MODE_POINT),0," ",magicMIN,0, Blue );}
if (MIN==7&&MIN1==1){ticket=OrderSend("NZDUSD",OP_SELL,lot,NormalizeDouble(MarketInfo("NZDUSD",MODE_BID),MarketInfo("NZDUSD",MODE_DIGITS)),5,NormalizeDouble(MarketInfo("NZDUSD",MODE_BID),MarketInfo("NZDUSD",MODE_DIGITS))+sl*MarketInfo("NZDUSD",MODE_POINT),0," ",magicMIN,0, Blue );}
}
//=========================
if (OrdersTotal()>0){
 for(i=0;i<OrdersTotal();i++) { OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()=="EURUSD" && OrderMagicNumber()==magicMAX ){simMAX=1;}
          if (OrderSymbol()=="GBPUSD" && OrderMagicNumber()==magicMAX ){simMAX=2;}
           if (OrderSymbol()=="USDCHF" && OrderMagicNumber()==magicMAX ){simMAX=3;}
            if (OrderSymbol()=="USDJPY" && OrderMagicNumber()==magicMAX ){simMAX=4;}
             if (OrderSymbol()=="AUDUSD" && OrderMagicNumber()==magicMAX ){simMAX=5;}
              if (OrderSymbol()=="USDCAD" && OrderMagicNumber()==magicMAX ){simMAX=6;}
               if (OrderSymbol()=="NZDUSD" && OrderMagicNumber()==magicMAX ){simMAX=7;}
      if (OrderSymbol()=="EURUSD" && OrderMagicNumber()==magicMIN ){simMIN=1;}
          if (OrderSymbol()=="GBPUSD" && OrderMagicNumber()==magicMIN ){simMIN=2;}
           if (OrderSymbol()=="USDCHF" && OrderMagicNumber()==magicMIN ){simMIN=3;}
            if (OrderSymbol()=="USDJPY" && OrderMagicNumber()==magicMIN ){simMIN=4;}
             if (OrderSymbol()=="AUDUSD" && OrderMagicNumber()==magicMIN ){simMIN=5;}
              if (OrderSymbol()=="USDCAD" && OrderMagicNumber()==magicMIN ){simMIN=6;}
               if (OrderSymbol()=="NZDUSD" && OrderMagicNumber()==magicMIN ){simMIN=7;}
          }}
          


if (OrdersTotal()>0){
 for(i=0;i<OrdersTotal();i++) { OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber()==magicMAX){
      if ( MAX!=simMAX ) {
      OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(OrderSymbol(),MODE_ASK),MarketInfo(OrderSymbol(),MODE_DIGITS)),5,Violet);
   OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID),MarketInfo(OrderSymbol(),MODE_DIGITS)),5,Violet); }

    }}}


if (OrdersTotal()>0){
 for(i=0;i<OrdersTotal();i++) { OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber()==magicMIN){
      if ( MIN!=simMIN ) {
      OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(OrderSymbol(),MODE_ASK),MarketInfo(OrderSymbol(),MODE_DIGITS)),5,Violet); 
     OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID),MarketInfo(OrderSymbol(),MODE_DIGITS)),5,Violet); }
 
    }}}

   
   return(0);
  }
//+------------------------------------------------------------------+