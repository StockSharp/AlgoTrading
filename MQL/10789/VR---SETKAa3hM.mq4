//===========================================================================================================================//
// Author VOLDEMAR227 site WWW.TRADING-GO.RU      SKYPE: TRADING-GO          e-mail: TRADING-GO@List.ru
//===========================================================================================================================//
#property copyright "Copyright © 2012, WWW.TRADING-GO.RU ."
#property link      "http://WWW.TRADING-GO.RU"
//WWW.TRADING-GO.RU  full version free
//===========================================================================================================================//
extern string  Comment_1 = "settings";
extern int     Plus= 50;
extern int     TakeProfit = 300;
extern int     Distanciya = 300;
extern double  Lots    = 0.00;
extern double  Percent    = 1;

extern bool    Martin     = true;

extern string  Comment_2 = "signalMA";
extern bool     SignalMA = false;
extern int     PeriodMA1 = 8 ;
extern int     PeriodMA2 = 14 ;

extern string  Comment_3 = "signalRSI";
extern bool    SignalRSI = false;
extern int     PeriodRSI = 14 ;
extern int     up= 50;
extern int     dw= 50;

extern string  Comment_4 = "signalProc";
extern bool    Proc    =true;
extern double  Procent    =1.3;

extern string  Comment_5 = "Monitor";
extern bool    Monitor =  true;
extern int     CORNER = 0;
extern int     Slip=2;
extern int     Magic=1;
int start() 
  {
double Lots_New=0;
 string Symb   =Symbol();               
   double One_Lot=MarketInfo(Symb,MODE_MARGINREQUIRED);
   double Min_Lot=MarketInfo(Symb,MODE_MINLOT);
   double Step   =MarketInfo(Symb,MODE_LOTSTEP);
   double Free   =AccountFreeMargin();        
//--------------------------------------------------------------- 3 --
   if (Lots>0)                               
     {                                        
      double Money=Lots*One_Lot;          
      if(Money<=AccountFreeMargin())         
         Lots_New=Lots;                   
      else                                
         Lots_New=MathFloor(Free/One_Lot/Step)*Step;
     }
//--------------------------------------------------------------- 4 --
   else                                    
     {                                       
      if (Percent > 100)                  
         Percent=100;                    
      if (Percent==0)                
         Lots_New=Min_Lot;                  
      else                      
         Lots_New=MathFloor(Free*Percent/100/One_Lot/Step)*Step;//Расч
     }
//--------------------------------------------------------------- 5 --
   if (Lots_New < Min_Lot)                  
      Lots_New=Min_Lot;                  
   if (Lots_New*One_Lot > AccountFreeMargin())
     {                                         
      return(false);                        
     }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
   ObjectCreate("R",OBJ_LABEL,0,0,0);
   ObjectSet("R",OBJPROP_CORNER,2);
   ObjectSet("R",OBJPROP_XDISTANCE,10);
   ObjectSet("R",OBJPROP_YDISTANCE,10);
   ObjectSetText("R","WWW.TRADING-GO.RU  full version free",15,"Arial Black",Red);
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
   double opB=2000; double opS=0; double orderProfitbuy=0; double Sum_Profitbuy=0; double orderProfitsel;  double Sum_Profitsel; int orderType;
   double LotB=Lots_New;
   double LotS=Lots_New;
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
               double ProfitB=OrderTakeProfit(); double openB=OrderOpenPrice();
               if(openB<opB)
                 {opB=openB;}
              }
            //---------------------------------      
            if(OrderType()==OP_SELL && OrderMagicNumber()==Magic)
              {
               s++;
               LotS=OrderLots();
              double ProfitS=OrderTakeProfit(); double openS=OrderOpenPrice();
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
   double dis   =NormalizeDouble(Distanciya*Point,Digits);
   double spred =NormalizeDouble(MarketInfo(Symbol(),MODE_SPREAD)*Point,Digits);
   double  CORR=NormalizeDouble(Plus            *Point,Digits);
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//   
int sigup=0;
int sigdw=0;
//WWW.TRADING-GO.RU  full version free
if (SignalMA == true)
{ 
//WWW.TRADING-GO.RU  full version free
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
if (SignalRSI == true)
{
//WWW.TRADING-GO.RU  full version  free
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
if (Proc    ==true)
{
   if(cl>min) { double x=NormalizeDouble(cl*100/min-100,2); }
   if(cl<max) { double y=NormalizeDouble(cl*100/max-100,2); }
   
if (Procent*(-1)<=y&&Close[1]>Open[1]){ sigup=1; sigdw=0; }
if (Procent     >=x&&Close[1]<Open[1]){ sigup=0; sigdw=1; }  
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
   int f=0;
   if(Martin==true)
     {
        if (total==0){f=1    ;}
        if (total>=1){f=total;}
         LotB=Lots_New*f;
         LotS=Lots_New*f;
     }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
if(Martin==false)
  {
LotB=LotS;
LotS=LotB;
  }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//  
if((b==0&&sigup==1&&s==0)||(Ask<opB-dis+spred&&b>=1&&s==0)) { OrderSend(Symbol(),OP_BUY ,LotB,Ask,Slip,0,0,"Buy ",Magic,0,Green); }
if((s==0&&sigdw==1&&b==0)||(Bid>opS+dis+spred&&s>=1&&b==0)) { OrderSend(Symbol(),OP_SELL,LotS,Bid,Slip,0,0,"Sell",Magic,0,Green); }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
double TP= NormalizeDouble (spred+TakeProfit*Point,Digits);
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
   for (int iq=total-1; iq>=0; iq--)
   {
    if(OrderSelect(iq, SELECT_BY_POS))
     {
      if(OrderSymbol()==Symbol()&&OrderMagicNumber()==Magic)
       {   
        if (OrderType()==OP_BUY  &&  OrderTakeProfit()==0 && b==1) 
         {
          OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()+TP,Digits),0,Blue);  
         }     
        if (OrderType()==OP_SELL && OrderTakeProfit()==0 && s==1) 
         {
          OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()-TP,Digits),0,Blue);  
         }
}}}
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
if ( Monitor == true )
{
//WWW.TRADING-GO.RU  full version free
}
}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

