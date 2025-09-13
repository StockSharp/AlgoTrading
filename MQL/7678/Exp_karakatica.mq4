//+------------------------------------------------------------------+
//|                     Exp_karacatica                               |
//|                     Дмитрий                                      |
//+------------------------------------------------------------------+
extern double   Risk=0.5;
extern int      StopLoss=50;
extern int      TakeProfit=150;
extern int     iPeriod=70;
extern int     OptPeriod=250;
extern int     WorkPeriod=50;
extern int     OptStart=10;
extern int     OptStep=5;
extern int     OptEnd=150;
extern int     Magic_N=12345;
//----
int            lso;
int            WorkedBars;
int            lbt;
//----
bool           ft=true;
bool           DontOpen;
bool           DontOpenBuy;
bool           DontOpenSell;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start()
  {
     if(Symbol()!="EURUSD" || Period()!=15)
     {
      Alert("exp_Karakatica: Пожалуйста подключите меня на EURUSD M15, здесь я работать не хочу:-)");
      return(0);
     }
   if(Bars<OptPeriod+OptEnd+10)return(0);
     if(ft)
     {
      ft=false;
      Optimization();
     }
   if(Bars<iPeriod)return(0);
   double bt=iCustom(NULL,0,"iKarakatica",iPeriod,0,1);
   double st=iCustom(NULL,0,"iKarakatica",iPeriod,1,1);
   int bs,ss;
     if(bt!=0 && bt!=EMPTY_VALUE)
     {
      bs=1;
     }
     if(st!=0 && st!=EMPTY_VALUE)
     {
      ss=1;
     }
     for(int i=OrdersTotal()-1;i>=0;i--)
     {
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
           if(OrderSymbol()==Symbol() && (OrderType()==OP_BUY || OrderType()==OP_SELL))
           {
              if(ss==1 && OrderType()==OP_BUY)
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,0,Red);
              }
              if(bs==1 && OrderType()==OP_SELL)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,0,Blue);
              }
           }
        }
     }
     if(EOrdersTotal()==0)
     {
        if(!DontOpen && !DontOpenBuy && bs==1 && lso!=1){// && Close[1]>ma){
         OrderSend(Symbol(),OP_BUY,LotsOpt(),ND(Ask),2,ND(ND(Ask)-ND(StopLoss*Point)),ND(ND(Ask)+ND(TakeProfit*Point)),NULL,Magic_N);
         lso=1;
        }
        if(!DontOpen && !DontOpenSell && ss==1 && lso!=2){// && Close[1]<ma){
         OrderSend(Symbol(),OP_SELL,LotsOpt(),ND(Bid),2,ND(ND(Bid)+ND(StopLoss*Point)),ND(ND(Bid)-ND(TakeProfit*Point)),NULL,Magic_N);
         lso=2;
        }
     }
   Optimization();
   //-----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  double LotsOpt()
  {
   double ls=MarketInfo(Symbol(),MODE_MINLOT)+MarketInfo(Symbol(),MODE_LOTSTEP)*MathFloor((Risk*AccountBalance()/1000-MarketInfo(Symbol(),MODE_MINLOT))/MarketInfo(Symbol(),MODE_LOTSTEP));
     if(ls>MarketInfo(Symbol(),MODE_MAXLOT))
     {
      ls=MarketInfo(Symbol(),MODE_MAXLOT);
     }
     if(ls<MarketInfo(Symbol(),MODE_MINLOT))
     {
      ls=MarketInfo(Symbol(),MODE_MINLOT);
     }
   return(ls);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int Optimization()
  {
     if(lbt!=Time[0])
     {
      lbt=Time[0];
      WorkedBars--;
     }
     if(WorkedBars<=0)
     {
      WorkedBars=WorkPeriod;
      Print(TimeToStr(Time[0])," Начал оптимизацию");
      double   Spr=Ask-Bid;
      double   BuyProf;
      double   SellProf;
      double   Prof;
      int      OrdType;
      double   OrdOpPr;
      double   MaxProf=-999999999;
      double   BuyMaxProf=-999999999;
      double   SellMaxProf=-999999999;
      int      BestPeriod;
      int      BuyBestPeriod;
      int      SellBestPeriod;
        for(int p=OptStart;p<=OptEnd;p+=OptStep)
        {//период индикатора
         Prof=0;
         BuyProf=0;
         SellProf=0;
         OrdType=0;
           for(int i=OptPeriod;i>=0;i--)
           {
            double bt=iCustom(NULL,0,"iKarakatica",p,0,i+1);
            double st=iCustom(NULL,0,"iKarakatica",p,1,i+1);
            int bs=0,ss=0;
              if(bt!=0 && bt!=EMPTY_VALUE)
              {
               bs=1;
              }
              if(st!=0 && st!=EMPTY_VALUE)
              {
               ss=1;
              }
            //закрытие
              if(OrdType==1 && ss==1)
              {
               BuyProf+=Open[i]-OrdOpPr-Spr;
               Prof+=Open[i]-OrdOpPr-Spr;
               OrdType=0;
              }
              if(OrdType==2 && bs==1)
              {
               SellProf+=OrdOpPr-Open[i]-Spr;
               Prof+=OrdOpPr-Open[i]-Spr;
               OrdType=0;
              }
              if(OrdType==0)
              {
               if(bs==1)OrdType=1;
               if(ss==1)OrdType=2;
               OrdOpPr=Open[i];
              }
           }
           if(OrdType==1)
           {
            BuyProf+=Open[0]-OrdOpPr-Spr;
            Prof+=Open[0]-OrdOpPr-Spr;
           }
           if(OrdType==2 && bs==1)
           {
            SellProf+=OrdOpPr-Open[0]-Spr;
            Prof+=OrdOpPr-Open[0]-Spr;
           }
           if(MaxProf<Prof)
           {
            MaxProf=Prof;
            BestPeriod=p;
           }
           if(BuyMaxProf<BuyProf)
           {
            BuyMaxProf=BuyProf;
            BuyBestPeriod=p;
           }
           if(SellMaxProf<SellProf)
           {
            SellMaxProf=SellProf;
            SellBestPeriod=p;
           }
        }
      DontOpen=false;
      DontOpenBuy=false;
      DontOpenSell=false;
      Print(TimeToStr(Time[0])," Закончил оптимизацию");
        if(BuyMaxProf<0 && SellMaxProf<0)
        {
         DontOpen=true;
         DontOpenBuy=true;
         DontOpenSell=true;
        }
        else
        {
           if(BuyMaxProf==SellMaxProf)
           {
            iPeriod=BestPeriod;
           }
           if(BuyMaxProf>SellMaxProf)
           {
            DontOpenSell=true;
            iPeriod=BuyBestPeriod;
           }
           if(BuyMaxProf<SellMaxProf)
           {
            DontOpenBuy=true;
            iPeriod=SellBestPeriod;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  double ND(double v)
  {
   return(NormalizeDouble(v,Digits));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int EOrdersTotal()
  {
   int tcnt;
     for(int i=0;i<OrdersTotal();i++)
     {
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
           if(OrderSymbol()==Symbol() && OrderMagicNumber()==Magic_N)
           {
              if(OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               tcnt++;
              }
           }
        }
        else
        {
         return(-1);
        }
     }
     //----
   return(tcnt);
  }
//+------------------------------------------------------------------+