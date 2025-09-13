//===========================================================================================================================//
// Author VOLDEMAR227 site WWW.TRADING-GO.RU      SKYPE: TRADING-GO          e-mail: TRADING-GO@List.ru
//===========================================================================================================================//
#property copyright "Copyright © 2012, WWW.TRADING-GO.RU ."
#property link      "http://WWW.TRADING-GO.RU"
//===========================================================================================================================//
extern int     Correlyciya= 50;
extern int     TakeProfit = 300;
extern double  Lot        = 0.1;
extern double  Procent    =1.3;
extern bool    Martin     = true;
extern int     Slip=2;
int Magic=1;
int start() 
  {
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
   ObjectCreate("R",OBJ_LABEL,0,0,0);
   ObjectSet("R",OBJPROP_CORNER,2);
   ObjectSet("R",OBJPROP_XDISTANCE,10);
   ObjectSet("R",OBJPROP_YDISTANCE,10);
   ObjectSetText("R","WWW.TRADING-GO.RU",10,"Arial",Red);
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
   double opB=2000; double opS=0; double orderProfitbuy=0; double Sum_Profitbuy=0; double orderProfitsel;  double Sum_Profitsel; int orderType;
   double LotB=Lot;
   double LotS=Lot;
   int total=OrdersTotal();
   int b=0,s=0,n=0;
   for(int i=total-1; i>=0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS))
        {
         if(OrderSymbol()==Symbol()      )
           {
            n++;
            if(OrderType()==OP_BUY && OrderMagicNumber()==Magic)
              {
               b++;
               LotB=OrderLots();
               int tikketB=OrderTicket(); double ProfitB=OrderTakeProfit(); double openB=OrderOpenPrice();
               if(openB<opB)
                 {opB=openB;}
              }
            //---------------------------------      
            if(OrderType()==OP_SELL && OrderMagicNumber()==Magic)
              {
               s++;
               LotS=OrderLots();
               int tikketS=OrderTicket(); double ProfitS=OrderTakeProfit(); double openS=OrderOpenPrice();
               if(openS>opS)
                 {opS=openS;}
              }
           }
        }
     }
   double max = NormalizeDouble(iHigh(Symbol(),1440,0),Digits);
   double min = NormalizeDouble(iLow (Symbol(),1440,0),Digits);
   double opp=NormalizeDouble(iOpen(Symbol(),1440,0),Digits);
   double cl=NormalizeDouble(iClose(Symbol(),1440,0),Digits);
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
   if(cl>min)
     {
      double x=NormalizeDouble(cl*100/min-100,2);
     }
//--------------
   if(cl<max)
     {
      double y=NormalizeDouble(cl*100/max-100,2);
     }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

   double dis   =NormalizeDouble(TakeProfit*Point,Digits);
   double spred =NormalizeDouble(MarketInfo(Symbol(),MODE_SPREAD)*Point,Digits);
   double  CORR=NormalizeDouble(Correlyciya            *Point,Digits);
   if(Martin==true)
     {
      if(n>=1){for(int P=100; P>=0; P--)
        {
         if(n==P&&n>=1) {LotB=LotB*P;}
         if(n==P&&n>=1) {LotS=LotS*P;}
        }
     }
  }
if(Martin==false)
  {
   if(b==1||s==1) {LotB=LotS*1;LotS=LotB*1;}
   if(b==2||s==2) {LotS=LotS*1;LotB=LotB*1;}
   if(b==3||s==3) {LotS=LotS*1;LotB=LotB*1;}

   if(b==4||s==4) {LotB=LotS*3;LotS=LotB*3;}
   if(b==5||s==5) {LotS=LotS*3;LotB=LotB*3;}
   if(b==6||s==6) {LotS=LotS*3;LotB=LotB*3;}

   if(b==7||s==7) {LotB=LotS*6;LotS=LotB*6;}
   if(b==8||s==8) {LotS=LotS*6;LotB=LotB*6;}
   if(b==9||s==9) {LotS=LotS*6;LotB=LotB*6;}
  }
if((b==0&&Procent*(-1)<=y&&s==0&&Close[1]>Open[1])||(Ask<opB-dis-spred&&b>=1&&s==0)) { OrderSend(Symbol(),OP_BUY ,LotB,Ask,Slip,0,0,"???????? ??? ?2",Magic,0,Green); }
if((s==0&&Procent     >=x&&b==0&&Close[1]<Open[1])||(Bid>opS+dis-spred&&s>=1&&b==0)) { OrderSend(Symbol(),OP_SELL,LotS,Bid,Slip,0,0,"???????? ??? ?2",Magic,0,Green); }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
double TPB= NormalizeDouble (openB+spred+TakeProfit*Point,Digits);
double TPS= NormalizeDouble (openS+spred-TakeProfit*Point,Digits);
if(ProfitB==0&&b>=1) { OrderModify(tikketB,openB,  OrderStopLoss(),TPB, 0,Blue); }
if(ProfitS==0&&s>=1) { OrderModify(tikketS,openS,  OrderStopLoss(),TPS, 0,Blue); }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

double nn=0,bb=0;
for(int ui=total-1; ui>=0; ui--)
  {
   if(OrderSelect(ui,SELECT_BY_POS))
     {
      if(OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY && OrderMagicNumber()==Magic)
           {
            double op=OrderOpenPrice();
            double llot=OrderLots();
            double itog=op*llot;
            bb=bb+itog;
            nn=nn+llot;
            double factb=bb/nn;
           }
        }
     }
  }
double nnn=0,bbb=0;
for(int usi=total-1; usi>=0; usi--)
  {
   if(OrderSelect(usi,SELECT_BY_POS))
     {
      if(OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_SELL && OrderMagicNumber()==Magic)
           {
            double ops=OrderOpenPrice();
            double llots=OrderLots();
            double itogs=ops*llots;
            bbb=bbb+itogs;
            nnn=nnn+llots;
            double facts=bbb/nnn;
           }
        }
     }
  }

for(int uui=total-1; uui>=0; uui--)
  {
   if(OrderSelect(uui,SELECT_BY_POS))
     {
      if(OrderSymbol()==Symbol())
        {
         if(b>=2 && OrderType()==OP_BUY && OrderMagicNumber()==Magic)
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),factb+CORR,0,Blue);
           }
         if(s>=2 && OrderType()==OP_SELL && OrderMagicNumber()==Magic)
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),facts-CORR,0,Blue);
           }
        }
     }
  }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
}
//+------------------------------------------------------------------+

