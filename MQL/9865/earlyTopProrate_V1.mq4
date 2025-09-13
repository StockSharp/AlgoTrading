//+------------------------------------------------------------------+
//| earlyTopProrate_V1.mq4             20.08.2010 - 08:54
//|(earlyOpenTrend.my4)
//|
//|
//| dailyOpen = Basiswert, , DailyHigh, -Low = Selektion
//+------------------------------------------------------------------+
#property copyright "hansH"
#property link ""

extern int    StartHour   =   5;
extern int    EndHour     =  10;
extern int    ClosingHour =  18;
extern int    ZD          =   0;

// ----
       double Lots        = 0.5;
extern int    maxPos      =   1;
       
       int    TP1         =  25;
       int    TP2         =  50;
       int    TP3         =  75; 
       int    setTP       =  35; // TP => OpenPrice             

       int    setSL0      = 100; // SL=> +/-50
       int    setSL1      =  35; // 40
       int    setSL2      =  60; // 90
       int    modiSL      =  25; // setSL2-35=25             

       int    Ratio1      =  30; // nicht ändern!
       int    Ratio2      =  70; // nicht ändern!
       int    Ratio3      = 100; // nicht ändern! 
       int    Phase       =   0;
       
       int    Magic       =170810;

       bool   CommentMode = true;

       bool   SignalBuy   =false;
       bool   SignalSell  =false;

// MoneyManagement
extern int    MMTyp       =    0;
       int    FactorMM2   =    3;
       int    RiskMM2     =   50;

// ----       
       double o1, h1, l1;
       double MAtrend, MAtrend1;
       double modLots, varLots, opLongLots, opShortLots;
       string ProfitPoints;
       int    i;

//+------------------------------------------------------------------+
int init()
{ return(0); }

//+------------------------------------------------------------------+
int deinit()
{ return(0); }

//+------------------------------------------------------------------+
int start()
{
modiSL =  setSL2-35;

// ---- Anzahl offene Positionen/Typ
int cnt, cntOpPos, cntOpLong=0, cntOpShort=0;
double Prof;

 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

 if(OrderType()==OP_BUY  && OrderSymbol()==Symbol())
  { cntOpLong=cntOpLong+1;   cntOpPos=cntOpPos+1; }
 if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
  { cntOpShort=cntOpShort+1; cntOpPos=cntOpPos+1; }
 }

if (cntOpPos<1) { Phase=0; opLongLots=0; opShortLots=0; ProfitPoints=(TP1+", "+TP2+", "+TP3); }

// ----
int x1=0;

      h1 = iHigh(NULL,1440,x1);
      l1 = iLow(NULL,1440,x1); 
      o1 = iOpen(NULL,1440,x1);

if( (h1-o1)>(o1-l1) ) string aktTrend="Up";    else
if( (h1-o1)<(o1-l1) )        aktTrend="Down";  else aktTrend="flat";

// ----
if (Bid>o1
      && aktTrend=="Up"      
      && cntOpPos<1)

SignalBuy=true;

// ----
if (Ask<o1
      && aktTrend=="Down"
      && cntOpPos<1)

SignalSell=true;

//+------------------------------------------------------------------+
//|------------------- Money Management ------------------------------
//+------------------------------------------------------------------+

if (SignalBuy==true || SignalSell==true)
{
if (MMTyp >3) MMTyp=0;
if (MMTyp<=1 && OrdersTotal()<1) Lots=Lots;
if (MMTyp==2 && OrdersTotal()<1) Lots=0.1*MathSqrt(AccountBalance()/1000)*FactorMM2;
if (MMTyp==3 && OrdersTotal()<1) Lots=AccountEquity()/Close[0]/1000*RiskMM2/100;

  if(MMTyp>=2)
  {
  Lots = modLots(Lots);
  double minLot = MarketInfo(Symbol(), MODE_MINLOT);
  double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
  if (Lots<minLot)Lots=minLot;
  if (Lots>maxLot)Lots=maxLot;
  }
}

//+------------------------------------------------------------------+
//| OrderSend
//+------------------------------------------------------------------+
if(Hour()>=StartHour && Hour()<EndHour)
{
 if (SignalBuy==true && cntOpLong==0 && cntOpPos==0)
 {
 OrderSend(Symbol(),OP_BUY,Lots,Ask,0,0,0,"earlyTopProrate",Magic); // ,Blue
 SignalBuy=false;
 cntOpPos=1;
 }
// -- 
 if (SignalSell==true && cntOpShort==0 && cntOpPos==0)
 { 
 OrderSend(Symbol(),OP_SELL,Lots,Bid,0,0,0,"earlyTopProrate",Magic); // ,Red
 SignalSell=false;
 }
} // if(Hour()

// ----
if (CommentMode==true)
  {
  Comment ("\n"+
  //"Balance: "+DoubleToStr(AccountBalance(),0)+" / Equity: "+DoubleToStr(AccountEquity(),0)+"\n"+
  //"                                                                                             dailyOpen, h1, l1"+"\n"+
  "MoneyManagement Typ "+MMTyp+"\n"+
  "Lots, Phase:   "+DoubleToStr(Lots,1)+" ("+Phase+")\n"+  
  "openLots:       "+DoubleToStr(opLongLots,1)+" (L/S) "+DoubleToStr(opShortLots,1)+"\n"+
  //"ProfitPoints:   "+DoubleToStr(TP1,0)+" / "+DoubleToStr(TP2,0)+" / "+DoubleToStr(TP3,0)+"\n"+
  "ProfitPoints:   "+ProfitPoints+"\n"+
  "ModifyPoints: "+setSL1+",  "+setSL2+" / -"+setTP+", -"+setSL0+"\n"+
  //"bestSL-Level: "+modiSL+"\n"+   
  "Trading: "+(StartHour+ZD)+" bis "+(EndHour+ZD)+":00 / cls "+(ClosingHour+ZD)+" Uhr"
          );

  } // if (CommentMode
// ------------------------------------------
// ---- Werte offener Positionen / shortEntry
double opShortPos=0, opLongPos=0;
int    shortTP=0,longTP=0;
color  col1=Gray, col2=Gray, col1a=Gray, col2a=Gray;

// --
if(OrdersTotal()!=0)
  {
  for(cnt=0; cnt<OrdersTotal(); cnt++)
   {
   OrderSelect(cnt,SELECT_BY_POS);
   
    if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
    { opShortPos=OrderOpenPrice(); shortTP=(opShortPos-Ask)/Point; col1=Red;}
   
    if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
    { opLongPos=OrderOpenPrice(); longTP= (Bid-opLongPos)/Point;  col2=SpringGreen; }
   } // for(cnt
  } // if(OrdersTotal  

// ----
   ObjectDelete("longTP"); ObjectDelete("longTP_Label");   
   ObjectDelete("shortTP"); ObjectDelete("shortTP_Label");
   ObjectDelete("opLong"); ObjectDelete("opLong_Label");   
   ObjectDelete("opShort"); ObjectDelete("opShort_Label");

// --------------------------------------------
// ----------------- Objekte ------------------
// --------------------------------------------
if(opShortPos==0) {shortTP=0; col1=Gray; col1a=Gray;} else col1a=Silver;
if(opLongPos==0)  {longTP=0;  col2=Gray; col2a=Gray;} else col2a=Silver; 
 
// ----
if(opShortPos!=0)
   {
   ObjectCreate("opShort", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("opShort",DoubleToStr(opShortPos,Digits-1),9,"Tahoma", col1);
   ObjectSet("opShort", OBJPROP_CORNER, 0);
   ObjectSet("opShort", OBJPROP_XDISTANCE, 3);
   ObjectSet("opShort", OBJPROP_YDISTANCE, 11);

   ObjectCreate("shortTP", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("shortTP","("+DoubleToStr(shortTP/10,0)+")",9,"Tahoma", col1a);
   ObjectSet("shortTP", OBJPROP_CORNER, 0);
   ObjectSet("shortTP", OBJPROP_XDISTANCE, 44);
   ObjectSet("shortTP", OBJPROP_YDISTANCE, 11);
   }

if(opLongPos!=0)   
   {
   ObjectCreate("opLong", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("opLong",DoubleToStr(opLongPos,Digits-1),9,"Tahoma", col2);
   ObjectSet("opLong", OBJPROP_CORNER, 0);
   ObjectSet("opLong", OBJPROP_XDISTANCE, 85);
   ObjectSet("opLong", OBJPROP_YDISTANCE, 11);

   ObjectCreate("longTP", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("longTP","("+DoubleToStr(longTP/10,0)+")",9,"Tahoma", col2a);
   ObjectSet("longTP", OBJPROP_CORNER, 0);
   ObjectSet("longTP", OBJPROP_XDISTANCE, 126);
   ObjectSet("longTP", OBJPROP_YDISTANCE, 11); 
   }

//----
//return(0);
//} // int start()

//+------------------------------------------------------------------+
if (cntOpPos>0)
{ 
  for (cnt=0;cnt<OrdersTotal();cnt++)
  { 
  OrderSelect(cnt, SELECT_BY_POS); 
   if (OrderSymbol()==Symbol() && OrderMagicNumber()==Magic) 
   {
  
    if (OrderType()==OP_BUY)
    {
     opLongLots=OrderLots();
     /*
     //---- SL- u. TP-Modify 
     if (OrderStopLoss()==0)
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-modiSL*10*Point,OrderTakeProfit(),0);
     return(0);
     }  
     */
     //---- setTP **** 35 **** / TP auf OrderOpenPrice() setzen 
     if (setTP>0
     && Bid-OrderOpenPrice()<=-setTP*10*Point     
     && OrderTakeProfit()==0) 
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice(),0);
     return(0);
     }
     
     //---- setSL0 **** 100 **** / BUY-SL od. TP setzen 
     if (setSL0>0
     && Bid-OrderOpenPrice()<=-setSL0*10*Point)
     {
     //OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-50*10*Point,OrderTakeProfit(),0);
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice()-50*10*Point,0);
     return(0);
     }  
     
     //---- setSL1 / BUY-SL erhöhen auf OrderOpenPrice() 
     if (setSL1>0
     && Bid-OrderOpenPrice()>=setSL1*10*Point
     && OrderStopLoss()<OrderOpenPrice()) 
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0);
     return(0);
     }
  
     //---- setSL2 / BUY-SL erhöhen auf OrderOpenPrice()+modiSL
     if (setSL2>0
     && Bid-OrderOpenPrice()>=setSL2*10*Point
     && OrderStopLoss()<OrderOpenPrice()+modiSL*10*Point) 
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+modiSL*10*Point,OrderTakeProfit(),0);
     return(0);
     }

//---- TP1
     if(Bid-OrderOpenPrice()>=TP1*10*Point
     && Phase==0
     && TP1>0) 
     {
     OrderClose(OrderTicket(),modLots(OrderLots()*Ratio1/100),Bid,0);
     Phase=1;
     return(0);
     }
   
//---- TP2
     if(Bid-OrderOpenPrice()>=TP2*10*Point
     && Phase==1
     && TP2>0) 
     {
     OrderClose(OrderTicket(),modLots(OrderLots()*Ratio2/100),Bid,0);
     Phase=2;
     return(0);
     }
     
//---- TP3 **** 75 ****
     if(Bid-OrderOpenPrice()>=TP3*10*Point
     && Phase==2
     && TP3>0) 
     {
     OrderClose(OrderTicket(),modLots(OrderLots()*Ratio3/100),Bid,0);
     Phase=3;
     return(0);
     }     
    } // if (OrderType()==OP_BUY)

// ----
    if (OrderType()==OP_SELL)
    {
     opShortLots=OrderLots();
     /*
     //---- SL- u. TP-Modify 
     if (OrderStopLoss()==0)
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+modiSL*10*Point,OrderTakeProfit(),0);
     return(0);
     }
     */
//---- setTP **** 35 ****  / TP auf OrderOpenPrice() setzen
     if (setTP>0
     && OrderOpenPrice()-Ask<=-setTP*10*Point     
     && OrderTakeProfit()==0) 
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice(),0);
     return(0);
     }
     
//---- setSL0 **** 100 **** / SELL-SL od. TP setzen
     if (setSL0>0
     && OrderOpenPrice()-Ask<=-setSL0*10*Point)
     {
     //OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+50*10*Point,OrderTakeProfit(),0);
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderTakeProfit()+50*10*Point,0); 
     return(0);
     }
 
//---- setSL1 / SELL-SL senken auf OrderOpenPrice()
     if (setSL1>0
     && OrderOpenPrice()-Ask>=setSL1*10*Point
     && OrderStopLoss()>OrderOpenPrice()) 
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0);
     return(0);
     }
     
     //---- setSL2 / SELL-SL senken auf OrderOpenPrice()-modiSL
     if (setSL2>0
     && OrderOpenPrice()-Ask>=setSL2*10*Point
     && OrderStopLoss()>OrderOpenPrice()-modiSL*10*Point) 
     {
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-modiSL*10*Point,OrderTakeProfit(),0);
     return(0);
     }

//---- TP1
     if(OrderOpenPrice()-Ask>=TP1*10*Point
     && Phase==0
     && TP1>0) 
     {
     OrderClose(OrderTicket(),modLots(OrderLots()*Ratio1/100),Ask,0);
     Phase=1;
     return(0);
     }
//---- TP2
     if(OrderOpenPrice()-Ask>=TP2*10*Point
     && Phase==1
     && TP2>0) 
     {
     OrderClose(OrderTicket(),modLots(OrderLots()*Ratio2/100),Ask,0);
     Phase=2;
     return(0);
     }
     
//---- TP3 **** 75 ****
     if(OrderOpenPrice()-Ask>=TP3*10*Point
     && Phase==2
     && TP3>0) 
     {
     OrderClose(OrderTicket(),modLots(OrderLots()*Ratio3/100),Ask,0);
     Phase=3;
     return(0);
     }
     
    } // if (OrderType()==OP_SELL)
    
// ----
//if(Phase==0)
  {   
  if (opLongLots==Lots   || opShortLots==Lots
  || (opLongLots==0 && opShortLots==0) )     { Phase=0; ProfitPoints=(TP1+", "+TP2+", "+TP3); } else 
  if((opLongLots >Lots/2 && opShortLots==0)
  || (opShortLots>Lots/2 && opLongLots ==0)) { Phase=1; ProfitPoints=(TP2+", "+TP3); }          else
  if((opLongLots <Lots/2 && opShortLots==0)
  || (opShortLots<Lots/2 && opLongLots ==0)) { Phase=2; ProfitPoints=TP3; }                     else
                                             { Phase=3; ProfitPoints="k.A."; }
  }
/*
// ----
ProfitPoints="";
if(Phase==0) ProfitPoints=(TP1+", "+TP2+", "+TP3);
if(Phase==1) ProfitPoints=(TP2+", "+TP3);
if(Phase==2) ProfitPoints=TP3;
if(Phase>=3) ProfitPoints="k.A.";
*/
// ----------------------------------
// --------- DayTradingClose --------
// ----------------------------------
if(ClosingHour!=0)
   {
   if (OrderType()==OP_BUY  && Hour()>=ClosingHour)
       OrderClose(OrderTicket(),opLongLots,Bid,0); // nicht: OrderLots() 0.1
        
   if (OrderType()==OP_SELL && Hour()>=ClosingHour)
       OrderClose(OrderTicket(),opShortLots,Ask,0); // nicht: OrderLots() 0.1
   } // if(ClosingHour!=0)
   
// ----
  } // if (OrderSymbol()
 } // for (int cnt
} // if (cntOpPos>0)
//----
return(0);
} // int start()

// -----------------------------------
// ---- modLots (MoneyManagement) ----
// -----------------------------------
double modLots(double varLots)
 {
double lotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
double tempDouble = varLots + lotStep/2;
       tempDouble /= lotStep;
int tempInt = tempDouble;
return (tempInt*lotStep);
 }

// ******************************************************************************