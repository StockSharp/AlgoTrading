//+------------------------------------------------------------------+
//|                               Name := BronzeWarrioir01           |
//|                               converted to MT4 by suffic369@yahoo|
//|                               http://www.metaquotes.ru           |
//+------------------------------------------------------------------+
extern int MagicNumber=6290102;
extern double Lots=0.1;
extern int Slippage=3;
//----
extern double lTakeProfit=0;
extern double sTakeProfit=0;
extern double lStopLoss=0;
extern double sStopLoss=0;
extern double lTrailingStop=0;
extern double sTrailingStop=0;
//extern int mgod=2005;
extern int FrMarg=3000;
extern int porog=500;
extern int per=14;
extern int d=3;
extern int test=0;
extern int workb=-50;
extern int works=50;
extern int pred=100;
extern int sliv=-2000;
extern int mm=30;
 //bool ft=true, first=true,two=false ;
 //int mlot=0,j=0,s=0,b=0,os=0,ob=0,pr=4,summa=0,sell=0,buy=0;
 //int cnt=0,//Top=0,
 // inul=0,ione=0,pm=0,down=0,bloks=0,blokb=0,ps=0,wpr=0,cci=0,zz=0;
int down=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int init() 
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int deinit() 
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
   //if mgod!=year then exit;
   //j=j+1;
   //if j>=1000 then j=0;
   double mlot=Lots;
/*
if (ft) 
{  
   if (Point>0.002) 
   { 
      pr=2;ft=false;
   }
} // pr: 2 or 4,价格小数点后的多少位数
*/
   int Total=OrdersTotal();
   int sbo=0; int s=0; int b=0; int pendings=0; int bpendings=0;int spendings=0;int summa=0; int ssum=0; int bsum=0;
//----
     for( int i=0; i<Total; i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
         if((OrderType()==OP_SELLSTOP || OrderType()==OP_BUYSTOP)
            && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) pendings=pendings+1;
         if(OrderType()==OP_BUYSTOP
            && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) bpendings=bpendings+1;
         if(OrderType()==OP_SELLSTOP
            && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) spendings=spendings+1;
         if(OrderType()<=OP_SELL
            && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) sbo=sbo+1;//have open trades
           if (OrderMagicNumber()==MagicNumber && OrderSymbol()==Symbol() && (OrderType()==OP_SELL || OrderType()==OP_BUY)) 
           {
            summa=summa+OrderProfit();// profit of open trade
           }
           if (OrderType()==OP_SELL && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
           {
            s=s+1;//open trade is sell
            //ssum=ssum+OrderProfit();//sell trade profit
           }
           if (OrderType()==OP_BUY && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)  
           {
            b=b+1;//have open buy
            //bsum=bsum+OrderProfit();//buy trade profit
           }
        }
     }
     if (b+s==0)
     {
      int pm=0;int ps=0;
     }
   if (summa<0 && summa< down)     down=MathRound(summa);
   //If (CurTime-LastTradeTime<15)  return(0);    
     if (s+b+bpendings+spendings==0 && AccountBalance()==AccountFreeMargin() && AccountBalance()<5000) 
     {
      Comment(" ");return(0);
     }
   double inul=iCustom(NULL,0,"DayImpuls",per,d,0,0);
   double ione=iCustom(NULL,0,"DayImpuls",per,d,0,1);
   double wpr=iWPR(NULL,0,per,0);
   double cci=iCCI(NULL,0,per,PRICE_CLOSE,0);
   double zz=0; // ZZ2=iCustom(NULL,0,"ZigZag",depth,deviation,backstep,0,0);
//----
     if (test==1) 
     {  //default test=0
      Print("Data: ",Year(),".",Month(),".",Day(),"  Pozz=",s+b,"  Impuls=",MathRound(inul),
         "  WPR=",MathRound(wpr),"  CCI=",MathRound(cci),"  ZZ=",zz,"  Price=",Open[0],"  Prof=",MathRound(summa),"  Down=",MathAbs(down));
      //if j<=2 then Comment("  ");
     }
     if (test==0) 
     {
      Comment("Data: ",Year(),".",Month(),".",Day(),"  Time ",Hour(),":",Minute(),"\n","Pozz=",s+b,"  Impuls=",MathRound(inul),
           "  WPR=",MathRound(wpr),"  CCI=",MathRound(cci),"  Price=",Open[0],"  Prof=",MathRound(summa),"  Down=",MathAbs(down));
     }
   if (summa>=porog)  pm=1;
     if (pm==1) 
     {
        for( i=0; i<Total; i++) 
        {
           if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
           {
              if (OrderType()==OP_SELL && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
              {
               OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, DarkOrange);
               return(0);
              }
              if (OrderType()==OP_BUY && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)  
              {
               OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, MediumSeaGreen);
               return(0);
              }
           }
        }
     }
   if (summa<sliv)  ps=1;
     if (ps==1) 
     {
        for( i=0; i<Total; i++) 
        {
           if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
           {
              if (OrderType()==OP_SELL && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) 
              {
               OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, DarkOrange);
               return(0);
              }
              if (OrderType()==OP_BUY && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)  
              {
               OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, MediumSeaGreen);
               return(0);
              }
           }
        }
     }
   if (s+b==0) { int bloks=0;int blokb=0;}
     if (AccountFreeMargin()>=FrMarg && s+b<2) 
     {
        if (bloks==0) 
        {
           if (inul>works && ione>inul && wpr>-15 && cci>150 && s==0) 
           {
            //Top=Bid;
            blokb=1;
            if (sStopLoss>0)  double sl= Bid+sStopLoss*Point; else sl= 0;
            if (sTakeProfit>0)  double tp= Bid-sTakeProfit*Point; else tp= 0;
            OrderSend(Symbol(),OP_SELL,mlot,Bid,Slippage,sl,tp,"sell"+MagicNumber,MagicNumber,0,Red);
            //Setorder(OP_SELL,mlot,PriceBid,3,Bid+StopLoss*point,Bid-TakeProfit*point,Gold);
            return(0);
           }
           if (b==0 && s==1 && (summa<=-pred/2 || summa>=pred)) 
           {
            if (lStopLoss>0)   sl= Ask-lStopLoss*Point; else sl= 0;
            if (lTakeProfit>0)   tp= Ask+lTakeProfit*Point; else tp= 0;
            OrderSend(Symbol(),OP_BUY,mm*mlot,Ask,Slippage,sl,tp,"buy"+MagicNumber,MagicNumber,0,Blue);
            //Setorder(OP_BUY,mm*mlot,PriceAsk,3,Ask-StopLoss*point,Ask+TakeProfit*point,Gold);
            return(0);
           }
        }
        if (blokb==0) 
        {
           if (inul<workb && ione<inul && wpr<-85 && cci<-150 && b==0) 
           {
            //Top=Ask;
            bloks=1;
            if (lStopLoss>0)   sl= Ask-lStopLoss*Point; else sl= 0;
            if (lTakeProfit>0)   tp= Ask+lTakeProfit*Point; else tp= 0;
            OrderSend(Symbol(),OP_BUY,mlot,Ask,Slippage,sl,tp,"buy"+MagicNumber,MagicNumber,0,Blue);
            //Setorder(OP_BUY,mlot,PriceAsk,3,Ask-StopLoss*point,Ask+TakeProfit*point,Gold);
            return(0);
           }
           if (s==0 && b==1 && (summa<=-pred/2 || summa>=pred)) 
           {
            if (sStopLoss>0)  sl= Bid+sStopLoss*Point; else sl= 0;
            if (sTakeProfit>0)  tp= Bid-sTakeProfit*Point; else tp= 0;
            OrderSend(Symbol(),OP_SELL,mm*mlot,Bid,Slippage,sl,tp,"sell"+MagicNumber,MagicNumber,0,Red);
            //Setorder(OP_SELL,mm*mlot,PriceBid,3,Bid+StopLoss*point,Bid-TakeProfit*point,Gold);
            return(0);
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+