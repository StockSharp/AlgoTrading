//+------------------------------------------------------------------+
//|                                            GoldWarrior02bMT4.mq4 |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Nick Bilak"
#property link "www.forex-tsd.com"
//----
extern double Lots =0.1;
extern double StopLoss=1000;
extern double TakeProfit=150;
extern double TrailingStop=0;
extern int ZN=1,ZM=0, per=21,d=3,depth=12,deviation=5,backstep=3,mgod=2005,porog=300,test=1,
imps=30,impb=-30,k1=30,k2=60;
//----
double LastTradeTime;
double kors=0.30,korb=0.15,ZZ2=0,ZZ3=0,cci0=0,cci1=0,nimp =0,wpr0=0,wpr1=0,summa=0,
       down=0,imp=0,mlot=0,ZZ0=0,ssum=0,bsum=0;
int cnt=0,j=0,ssig=0,bsig=0,b=0,bsb=0,sbo=0,bloks=0,blokb=0,pm=0,s=0,blok=1;
//----
// per   - period for all indicators 
// d     - number of full periods when calculating 
// depth,deviation,backstep - ZigZag indicator parameters 
// mgod  - testing year 
// porog - cut-off takeprofit when closing positions out 
// test  - current info output flag, 1 - output to journal
// imps  - positive impulse value for buy signal
// impb  - negative impulse value for buy signal
// k1 & k2 - multiplier for sizes of hedge orders of 1st and 2nd level
// k2/k1=2 - must be!
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void SetArrow(datetime t, double p, int k, color c)  
  {
   ObjectSet("Arrow", OBJPROP_TIME1 , t);
   ObjectSet("Arrow", OBJPROP_PRICE1 , p);
   ObjectSet("Arrow", OBJPROP_ARROWCODE, k);
   ObjectSet("Arrow", OBJPROP_COLOR , c);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start() 
  {
//----
   LastTradeTime=GlobalVariableGet("LastTradeTime");
   if (CurTime()-LastTradeTime<15) return(0);
   if (mgod!=Year()) j=j+1;
   if (j==10000) j=0;
     if (k2<2*k1) 
     {
      if (!IsTesting()) Comment("Increase K2 for normal work!",
      "\n","K2 must be not less than 2*K1");
      return(0);
     }
     if (sbo==0 && AccountBalance()<2000) 
     {
      if (!IsTesting()) Comment("For normal work you must have 2000$ on your account");
      return(0);
     }
   imp=iCustom(NULL,0,"DayImpuls",per,d,0,1);
   if (imp>10000) imp=0;
   nimp=iCustom(NULL,0,"DayImpuls",per,d,0,0);
   if (nimp>10000) nimp=0;
   ZZ3=iCustom(NULL,0,"ZigZag",depth,deviation,backstep,0,1);
   ZZ2=iCustom(NULL,0,"ZigZag",depth,deviation,backstep,0,0);
   //cci1=iCCI(NULL,0,per,PRICE_CLOSE,0);
   //wpr1=iWPR(NULL,0,per,0);
   cci1=iCCI(NULL,0,per,PRICE_CLOSE,1);
   cci0=iCCI(NULL,0,per,PRICE_CLOSE,0);
   //wpr1=iWPR(NULL,0,per,1);
   //wpr0=iWPR(NULL,0,per,0);
   if ((ZZ3>0.01 || ZZ2>0.01) && ((cci0<cci1 && cci1>50 && cci0>30 && nimp<0 && imp>0) ||
          (cci0>200 && cci1>cci0 && nimp>100 && imp>nimp))) 
          {
      //   if ((ZZ3!=0 || ZZ2!=0) //sell signal
      //   && cci0 && cci1>0 
      //   && cci0>0 
      //   && nimp<0 
      //   && imp>0) 
      //   { 
      SetArrow(Time[0],High[0]+5*Point,242,GreenYellow);
      ssig=1;
      //if (!IsTesting()) 
      Comment("ZZ0=",NormalizeDouble(ZZ2,Digits)," ZZ1=",NormalizeDouble(ZZ3,Digits)," CCI0=",NormalizeDouble(cci1,Digits)," Impuls=",NormalizeDouble(nimp,Digits),
      "\n","If iZigZag line is up - sell now");
     }
   if ((ZZ3>0.01 || ZZ2>0.01) && ((cci0>cci1 && cci1<-50 && cci0<-30 && nimp>0 && imp<0)) ||
          (cci0<-200 && cci1<cci0 && nimp<-100 && imp<nimp)) 
          {
      //  if ((ZZ3!=0 || ZZ2!=0) //buy signal
      //  && cci0>cci1 
      //  && cci1<0 
      //  && cci0<0 
      //  && nimp>0 
      //  && imp<0)   
      //{
      SetArrow(Time[0],Low[0]-5*Point,241,Gold);
      bsig=1;
      //if (!IsTesting()) 
      Comment("ZZ0=",NormalizeDouble(ZZ2,Digits)," ZZ1=",NormalizeDouble(ZZ3,Digits)," CCI0=",NormalizeDouble(cci1,Digits)," Impuls=",NormalizeDouble(imp,Digits),
      "\n","If iZigZag line is down - buy now");
     }
     if ((ZZ2<0.01 && ZZ3<0.01) || sbo!=0 || (imp>imps && imp<impb)) 
     {
      //    if ((ZZ2==0 && ZZ3==0)     //signal of no position
      //    || sbo!=0            //signal of having open orders
      //    || (imp<=imps && imp>=impb))   
      //    {
      ssig=0;//disallow sell
      bsig=0; //disallow buy
      if (!IsTesting()) Comment("ZZ0=",NormalizeDouble(ZZ2,Digits)," ZZ1=",NormalizeDouble(ZZ3,Digits)," CCI0=",NormalizeDouble(cci1,Digits)," Impuls=",NormalizeDouble(imp,Digits),
      "\n"," Signals are absent");
     }
   sbo=0;s=0;b=0;summa=0;ssum=0;bsum=0;
     for(cnt=0; cnt<OrdersTotal(); cnt++) 
     {
        if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) 
        {
         if (OrderSymbol()==Symbol()) sbo=sbo+1;//have open trades
           if ((OrderType()==OP_SELL && OrderSymbol()==Symbol()) || (OrderType()==OP_BUY && OrderSymbol()==Symbol())) {
            summa=summa+OrderProfit();// profit of open trade
           }
           if ((OrderType()==OP_SELL && OrderSymbol()==Symbol())) 
           {
            s=s+1;//open trade is sell
            ssum=ssum+OrderProfit();//sell trade profit
           }
           if (OrderType()==OP_BUY && OrderSymbol()==Symbol())  
           {
            b=b+1;//have open buy
            bsum=bsum+OrderProfit();//buy trade profit
           }
        }
     }
     if (blok==0) 
     { //add to position and set hedge order of 1st level
      if (s==1
            && summa>30 //add to position when profit > 30
            && cci0>150
            && nimp>50
                && imp>nimp)   
                {
         mlot=k1*Lots;
         int ticket=OrderSend(Symbol(),OP_SELL,mlot,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"Sculp_Sell",1,0,Yellow);
         bsb=0; //разрешение на открытие хеджа второго уровня
         blok=1; //разрешение на открытие хеджа второго уровня
         return(0);
        }
      if (s==1 //set hedge 1st level for sell position
            && summa>0
            && cci0<-150
            && nimp<-50
                && imp<nimp)
                {
         mlot=k1*Lots;
         ticket=OrderSend(Symbol(),OP_BUY,mlot,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"Sculp_BUY",1,0,Yellow);
         //Setorder(OP_BUY,mlot,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,Gold);
         bsb=0; //allow open hedge 2nd level
         blok=1;
         return(0);
        }
      if (s==1 //set hedge 1st level for sell position
            && summa<-30
            && cci0<-150
            && nimp<-50
                && imp<nimp)
                {
         mlot=k1*Lots;
         ticket=OrderSend(Symbol(),OP_BUY,mlot,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"Sculp_BUY",1,0,Yellow);
         //Setorder(OP_BUY,mlot,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,Gold);
         bsb=0; //allow open hedge 2nd level
         blok=1;
         return(0);
        }
      //}
      if (b==1 //set hedge 1st level for buy position
            && summa<-30
            && cci0>150
            && nimp>50
                && imp>nimp)   
                {
         mlot=k1*Lots;
         ticket=OrderSend(Symbol(),OP_SELL,mlot,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"Sculp_Sell",1,0,Yellow);
         bsb=0; //alow 2nd lev hedge
         blok=1; //alow 2nd lev hedge
         return(0);
        }
      if (b==1 //add to buy position when profit > 30
            && summa>30
            && cci0<-150
            && nimp<-50
                && imp<nimp)
                {
         mlot=k1*Lots;
         ticket=OrderSend(Symbol(),OP_BUY,mlot,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"Sculp_BUY",1,0,Yellow);
         bsb=0;//alow 2nd lev hedge
         blok=1;//alow 2nd lev hedge
         return(0);
        }
      if (b==1
            && summa>0
            && cci0>150
            && nimp>50
                && imp>nimp)
                {
         mlot=k1*Lots;
         ticket=OrderSend(Symbol(),OP_SELL,mlot,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"Sculp_Sell",1,0,Yellow);
         bsb=0;//alow 2nd lev hedge
         blok=1;//alow 2nd lev hedge
         return(0);
        }
     }
   if (blok==1 // set 2nd level hedge
         && (b+s)<=2
         && summa<-2500  //current loss
             && bsb==0)
             {   //allow open 2nd level hedge
      // set 2nd lev. hedge for buy pos.
        if(((b==1 && s==1) || b==2 || (b==1 && s==0)) && bsum<0 && bsum>ssum && cci0>150 && nimp>50)
        {
         mlot=k2*Lots;
         ticket=OrderSend(Symbol(),OP_SELL,mlot,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"Sculp_Sell",1,0,Yellow);
         //Setorder(OP_SELL,mlot,Bid,3,Bid+StopLoss*Point,Bid -TakeProfit*Point,Gold);
         bsb=1;
         //porog=100;
         return(0);
        }
        if(((s==1 && b==1)  || s==2  || (s==1 && b==0)) && ssum<0 && bsum<ssum && cci0<-150 && nimp<-50)   
        {
         mlot=k2*Lots;
         ticket=OrderSend(Symbol(),OP_BUY,mlot,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"Sculp_BUY",1,0,Yellow);
         bsb=1;
         //porog=100;
         return(0);
        }
     }//*/
     if (sbo==0) 
     { //do not have open trades
      bloks=0;//alow sell
      blokb=0;//allow buy
      pm=0; //disallow closing
      bsb=1;//disallow 2nd lev. hedge once more
     }//*/
   if (summa<0 && down>summa)
      down=(MathRound(summa)); //loss
     if (test==1) 
     { //print to log or screen
      if (!IsTesting()) Print ("Data: ",Year(),".",Month(),".",Day()," Time ",Hour(),":",Minute(),":",Seconds(),
      " Bloks=",bloks," Blokb=",blokb, " Blok=",blok," ZZ0=",MathRound(ZZ2),
      " ZZ1=",MathRound(ZZ3)," CCI0=",MathRound(cci0)," Imp=",MathRound(nimp),
      " Prof=",MathRound(summa)," DDown=",MathRound(down/30)," BSB=",bsb);
      if (j<=2) if (!IsTesting()) Comment(" ");
      } 
         else  
      {
      if (!IsTesting()) Comment ("Data: ",Year(),".",Month(),".",Day()," Time ",Hour(),":",Minute(),":",Seconds(),
      " Bloks=",bloks," Blokb=",blokb," Blok=",blok," ZZ0=",MathRound(ZZ2),
      " ZZ1=",MathRound(ZZ3)," CCI0=",MathRound(cci0)," Imp=",MathRound(nimp),
      " Prof=",MathRound(summa)," DDown=",MathRound(down/30));
     }//*/ 
   if (summa>porog) // profit margin
      pm=1;
     if (pm==1) 
     { //close trades by profit
        for(cnt=0; cnt<OrdersTotal(); cnt++) 
        {
           if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) 
           {
              if (OrderType()==OP_SELL && OrderSymbol()==Symbol()) 
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,5,Red);
               return(0);
              }
              if (OrderType()==OP_BUY && OrderSymbol()==Symbol()) 
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,5,Red);
               return(0);
              }
           }
        } //*/
     }
   if (AccountFreeMargin()>=2000 //open new positions
         && sbo==0 //have no open positions
         && (Minute()==14 || Minute()==29 || Minute()==44 || Minute()==59) //determin end of bar
             && Seconds()>=45) 
             { //and time of entry
      mlot=Lots;
        if (ssig==1 && bloks==0) 
        {
         SetArrow(Time[0],High[0]+5*Point,242,Red);
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"Sculp_Sell",1,0,Yellow);
         blokb=1;//disallow second buy entry 
         bsb=1; //disallow second hedge of 2nd lev
         blok=0; //allow add position and hedge 1st lev.
         GlobalVariableSet("LastTradeTime",CurTime());
         return(0);
        }
        if (bsig==1 && blokb==0) 
        {
         SetArrow(Time[0],Low[0]-5*Point,241,Gold);
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"Sculp_Sell",1,0,Yellow);
         bloks=1;//disallow second sell entry 
         bsb=1; //disallow second hedge of 2nd lev
         blok=0; //allow add position and hedge 1st lev.
         GlobalVariableSet("LastTradeTime",CurTime());
         return(0);
        }
     }
   //----  
   return(0);
  }
//+------------------------------------------------------------------+