//+------------------------------------------------------------------+
//|  dailyTrendReversal_D1.mq4             04.11.2010 - 13:21
//|  **** Demo-Version ****
//|
//|  2010-11-02 Filter 1 erweitert durch trendSteps
//|
//|  -Empfehlung: TimeFrame: M15
//|               Pairs:     AUDUSD, EURCHF,  EURJPY, EURUSD, GBPUSD, NZDUSD, USDCAD, USDCHF, USDJPY
//|               OrderTyp:  0
//|               OrderSL=0, OrderTP=30. 
//|
//|  -"OrderTyp": 0=Buy & Sell, 1=Buy, 2=Sell 
//|  -Position wird geöffnet, wenn der akt. Kurs über/unter der Eröffnungslinie liegt
//|   und der Abstand des Höchst-/Tiefst-Kurses zum Eröffnungskurs größer ist als der Vergleichskurs. 
//|   z.B. Eröffnung 1.3250, bisheriges iHigh 1.3290 = 40 Pips
//|                                     iLow  1.3220 = 30 Pips : Longposition wird geöffnet!
//|  -Bei Aktivierung von "HoldingHours" wird nach Erreichen die positive Position geschlossen, bei
//|   der negativen Position wird der TakeProfit-Wert auf den Eröffnungskurs gesetzt.
//|  -DayTrading-Ende ist um 18:00 GMT. Alle noch offenen Positionen werden sofort geschlossen.
//|  -Die Aktivierung von "ProfitStop" schließt alle Positionen beim Erreichen dieser Tages-Bilanz.
//+--------------------------------------------------------------------------------------------------+
#property copyright "hansH"
#property link      ""

// ----
extern bool   autoTrading    = true;
extern bool   Reversal       = true; // Position schließen bei Trendwechsel

extern string SelectSteps    = "1=h1-Bid>Risk,  2=o1-l1>Risk,  3=Bid-o1<10";
extern int    trendSteps     =    3; // 1=(h1-Bid)>Risk*digitFactor*Point
                                     // 2=(o1-l1) >=Risk*digitFactor*Point
                                     // 3=(Bid-o1)<=10*digitFactor*Point && Close[0]>Open[0]

//----       
extern double Lots           =  0.1;
       int    OrderTyp       =    0;  // extern // 0=Long&Short, 1=Long, 2=Short;

extern string StopLoss_TakeProfit_ProfitStop = "OrderSL=0, OrderTP=30, ProfitStop=200";       
extern double OrderSL        =    0;
extern double OrderTP        =   30;
extern int    ProfitStop     =  100; // 200 pro 0.1 lots
       double ProfStop, allProfits, maxProfit, allLoss, maxLoss=-0.001, CurrProfits, maxCurrProfit, CurrLoss, maxCurrLoss=-0.001, TP, SL;

// Entries
       double shortEntry, longEntry, TakeProfit, StopLoss;

// ----
// Trading-Zeiten
extern string Trading_Time   = "GMT_Diff = Chart hh:mm - GMT hh:mm";
extern int    GMT_Diff       =    0;  // 0
extern int    GMTstartHour   =    5;  // 5 Server time       
extern int    GMTendHour     =   14;  // 8
extern int    GMTclosingHour =   18;  // 18
       int    inTime;                 // Tradingzeit

// ----
extern string HoldingHours_Risk = "HoldingHours (hh)=10,  Risk=30";       
extern int    HoldingHours   =   10; // 2=Haltezeit 2 Std.
       int    Risk           =   30; // Entry-Range +/-50 Pips
       
// Indicator periods
       string IndicatorPeriods = "perCCI=CCI-Period";
       int    perCCI         =   15;

// ----
       double o1, h1, l1, firstOpen;

       int    digitFactor;
       int    dF;
              
// ----
       int    MagicNumber    = 04112010;
// ----
       bool   CommentDaten   = true;
// ----
       double historyProfit, historyLoss;
       double openProfit, openLoss, openCurrProfit, openCurrLoss ;

       double clsBuyOrder, clsSellOrder;
       double opLongPos, opShortPos, shortTP, longTP;
       string OrderTxt, TxtTime, TxtEntry, TradingTxt="";
       color  col1=Gray, col1a=Gray, col2=Gray, col2a=Gray, col3=Silver;
       int    cnt, cntHistBuy, cntHistSell, cntOpPos, cntOpLong, cntOpShort;
       int    i, Dig;
       
// ----------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------
int init()
  { }

//----------
int deinit()
  {
   Comment("");
   
   ObjectDelete("bar0"); ObjectDelete("bar0_Label");
   
   ObjectDelete("opShort"); ObjectDelete("opShort_Label");   
   ObjectDelete("opLong"); ObjectDelete("opLong_Label");   
   ObjectDelete("aktuell"); ObjectDelete("aktuell_Label");
   ObjectDelete("longTP"); ObjectDelete("longTP_Label");   
   ObjectDelete("shortTP"); ObjectDelete("shortTP_Label");
   
   ObjectDelete("RangeL"); ObjectDelete("RangeL_Label");
   ObjectDelete("RangeS"); ObjectDelete("RangeS_Label");
   
  ObjectsDeleteAll();
  return(0);

  }
  
//---------
int start()
  {
OrderSL=MathAbs(OrderSL);

// ---- Signale auf false setzen
    if(TimeHour(Time[0])< (GMTstartHour+GMT_Diff)    
    || TimeHour(Time[0])>=(GMTclosingHour+GMT_Diff)) 
    { string trend="", aktTrend="", CCI_Trend=""; clsBuyOrder=0; clsSellOrder=0; }
    
// ---- Tradingzeit 1=ja, 0=nein
    if(DayOfWeek()>0 && DayOfWeek()<=5 && IsDemo()
    && TimeHour(Time[0])>=(GMTstartHour+GMT_Diff)    
    && TimeHour(Time[0])< (GMTendHour+GMT_Diff))
        
    inTime=1; else inTime=0;

// ---- ProfitStop ----
ProfStop=ProfitStop;

// ---- Digits prüfen
if(Bid<10 && Digits==5) { digitFactor=10; dF=1; }
if(Bid<10 && Digits==4) { digitFactor= 1; dF=0; }

if(Bid>10 && Digits==3) { digitFactor=10; dF=1; }
if(Bid>10 && Digits==2) { digitFactor= 1; dF=0; }    
   
// ----
if(Close[0]>10)  { Dig=2; }  else
if(Close[0]<10)  { Dig=4; }

// ---
if(OrderTP!=0) TP=OrderTP*digitFactor; else TP=0;
if(OrderSL!=0) SL=OrderSL*digitFactor; else SL=0;

// ---- Selektion
color  col4a=Gray, col4b=Gray, col4c=Gray;
string TxtSymbol1="l", TxtSymbol2="l", TxtSymbol3="l";

// ---- Filter 0: MA-Trend
double MA0 =iMA(NULL,0,8,0,MODE_SMA,PRICE_OPEN,0);
double MA1 =iMA(NULL,0,8,0,MODE_SMA,PRICE_OPEN,1);

if(MA0>MA1) { TxtSymbol2="l"; col4b=Green; } else
if(MA0<MA1) { TxtSymbol2="l"; col4b=Red; }   else { TxtSymbol2=" "; }

// ---- Filter 3: CCI-Trend
CCI_Trend="";
double CCI0=iCCI(NULL,0,perCCI,PRICE_TYPICAL,0);
double CCI1=iCCI(NULL,0,perCCI,PRICE_TYPICAL,1);
double CCI2=iCCI(NULL,0,perCCI,PRICE_TYPICAL,2);

if(CCI0>=CCI1 && CCI1>=CCI2) { CCI_Trend="Up"; col4c=Green; } else
if(CCI0<=CCI1 && CCI1<=CCI2) { CCI_Trend="Dn"; col4c=Red;   } else CCI_Trend="flat";

// ----
int x1=0;

      h1 = iHigh(NULL,1440,x1);
      l1 = iLow(NULL,1440,x1); 
      o1 = iOpen(NULL,1440,x1);
      
// ---- Filter 1: trend
if (Bid>o1 &&
     (  (trendSteps>=0 && (h1-Bid)>Risk*digitFactor*Point)
     || (trendSteps>=2 && (o1-l1) >=Risk*digitFactor*Point && (Bid-o1)<=10*digitFactor*Point)     
     || (trendSteps>=3 && (Bid-o1)<=10*digitFactor*Point && Close[0]>Open[0])
     )
   )
   { trend="Up";  col4a=Green; }
    else
if (Bid<o1 &&
     (  (trendSteps>=0 && (Bid-l1)>Risk*digitFactor*Point)
     || (trendSteps>=2 && (h1-o1) >=Risk*digitFactor*Point && (o1-Bid)<=10*digitFactor*Point)
     || (trendSteps>=3 && (o1-Bid)<=10*digitFactor*Point && Close[0]<Open[0])
     )
   )
   { trend="Dn";  col4a=Red;   }
    else { trend="flat"; }

// ---- Filter 2: akt. Trend
if( (h1-o1)>(o1-l1) ) { aktTrend="Up";  col4b=Green; }  else
if( (h1-o1)<(o1-l1) ) { aktTrend="Dn";  col4b=Red;   }  else aktTrend="flat";

// --      
if(inTime<=1) {      
int dailyRange=(h1-l1)/Point/digitFactor;
string dR="  (+/-"+dailyRange+")"; } else dR="";

// --      
int aktRange=(Bid-o1)/Point/digitFactor;
string aR="  ("+aktRange+")";

// --------------------------------------------------------------
// -------- bilanzierte u. offene Positionen ermitteln ----------
// --------------------------------------------------------------

// ---- Profit ermitteln
historyProfit=0; historyLoss=0;
  for (cnt=0; cnt < OrdersHistoryTotal(); cnt++) 
  {
  if (!OrderSelect(cnt, SELECT_BY_POS, MODE_HISTORY)) continue;    
     if (TimeDayOfYear(OrderCloseTime()) == DayOfYear()) 
        {
        if ((OrderType()==OP_BUY || OrderType()==OP_SELL))
            {
            historyProfit=historyProfit+OrderProfit();
            if(OrderProfit()<0) historyLoss=historyLoss+OrderProfit();
            }
        }
  }
        
// --
openProfit=0; openLoss=0; openCurrProfit=0; openCurrLoss=0;
  for (cnt=0; cnt < OrdersTotal(); cnt++) 
  {
     if (!OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) continue;    
        {
        if ((OrderType()==OP_BUY || OrderType()==OP_SELL))
            {
              if(OrderSymbol()==Symbol())
              {
              openCurrProfit=openCurrProfit+OrderProfit();
              if(OrderProfit()<0) openCurrLoss=openCurrLoss+OrderProfit();
              }           
            openProfit=openProfit+OrderProfit();
            if(OrderProfit()<0) openLoss=openLoss+OrderProfit();
            }
        }
  }
  
// -- Symbol
       maxCurrProfit=MathMax(maxCurrProfit,openCurrProfit);
       maxCurrLoss=MathMin(maxCurrLoss,openCurrLoss);
// -- all       
       allProfits=historyProfit+openProfit;
       maxProfit=MathMax(maxProfit,allProfits);
       allLoss=historyLoss+openLoss;
       maxLoss=MathMin(maxLoss,allLoss);

// ---- History
int cnt = OrdersHistoryTotal();
cntHistBuy=0; cntHistSell=0;
  
  for (i=0; i < cnt; i++) 
  {
  if (!OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) continue;
    
  if (TimeDayOfYear(OrderCloseTime()) == DayOfYear()) 
    {
    if (OrderSymbol()==Symbol())
      {
      if (OrderType()==OP_BUY)  { clsBuyOrder  = OrderOpenPrice(); cntHistBuy =cntHistBuy +1; }
      if (OrderType()==OP_SELL) { clsSellOrder = OrderOpenPrice(); cntHistSell=cntHistSell+1; }
      }
    } // TimeDayOfYear
  } // for(int

// ---- Anzahl offene Positionen/Symbol
cntOpPos=0;

if(OrdersTotal()!=0)
  {
  for(cnt=0; cnt<OrdersTotal(); cnt++)
   {
   OrderSelect(cnt,SELECT_BY_POS);
   if((OrderType()==OP_SELL || OrderType()==OP_BUY) && OrderSymbol()==Symbol())
       {
       cntOpPos=cntOpPos+1;
       }
   } // for(cnt
  } // if(OrdersTotal

// ---- Anzahl offene Positionen/Typ
cntOpLong=0; cntOpShort=0;
double Prof;

 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

 if(OrderType()==OP_BUY  && OrderSymbol()==Symbol())
  { cntOpLong=cntOpLong+1;   Prof=MathAbs((OrderProfit()*OrderLots()) / ((Bid-OrderOpenPrice())/Point) * 100); }
 if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
  { cntOpShort=cntOpShort+1; Prof=MathAbs((OrderProfit()*OrderLots()) / ((Ask-OrderOpenPrice())/Point) * 100); }
 }

// ---- Werte offener Positionen / shortEntry
shortEntry=0;
opShortPos=0; shortTP=0; opLongPos=0; longTP=0;
// --
if(OrdersTotal()!=0)
  {
  for(cnt=0; cnt<OrdersTotal(); cnt++)
   {
   OrderSelect(cnt,SELECT_BY_POS);
   
    if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
    { opShortPos=OrderOpenPrice(); shortTP=(opShortPos-Ask)/Point; col1=Red;
      shortEntry=0;
    }
   
    if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
    { opLongPos=OrderOpenPrice(); longTP= (Bid-opLongPos)/Point;  col2=SpringGreen;
      shortEntry=OrderOpenPrice();
    }
   } // for(cnt
  } // if(OrdersTotal
  
// ---- 
string BidTxt=DoubleToStr(Bid,Digits);
string AskTxt=DoubleToStr(Ask,Digits);

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
if(CommentDaten==true)
{
if(opShortPos!=0)
   {
   ObjectCreate("opShort", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("opShort",DoubleToStr(opShortPos,Dig),9,"Tahoma", col1);
   ObjectSet("opShort", OBJPROP_CORNER, 0);
   ObjectSet("opShort", OBJPROP_XDISTANCE, 3);
   ObjectSet("opShort", OBJPROP_YDISTANCE, 11);

   ObjectCreate("shortTP", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("shortTP","("+DoubleToStr(shortTP/digitFactor,0)+")",9,"Tahoma", col1a);
   ObjectSet("shortTP", OBJPROP_CORNER, 0);
   ObjectSet("shortTP", OBJPROP_XDISTANCE, 44);
   ObjectSet("shortTP", OBJPROP_YDISTANCE, 11);
   }

if(opLongPos!=0)   
   {
   ObjectCreate("opLong", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("opLong",DoubleToStr(opLongPos,Dig),9,"Tahoma", col2);
   ObjectSet("opLong", OBJPROP_CORNER, 0);
   ObjectSet("opLong", OBJPROP_XDISTANCE, 85);
   ObjectSet("opLong", OBJPROP_YDISTANCE, 11);

   ObjectCreate("longTP", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("longTP","("+DoubleToStr(longTP/digitFactor,0)+")",9,"Tahoma", col2a);
   ObjectSet("longTP", OBJPROP_CORNER, 0);
   ObjectSet("longTP", OBJPROP_XDISTANCE, 126);
   ObjectSet("longTP", OBJPROP_YDISTANCE, 11); 
   }

} // if(CommentDaten
  
// -----------------------------------------------------
// ----------------- Text für Comment ------------------
// ----------------------------------------------------- 
// ---- Spread
double spread=MarketInfo(Symbol(),MODE_SPREAD)/digitFactor;

// ---- Profit / Loss
if(cntOpPos!=0) string ProfitLoss=DoubleToStr(maxCurrLoss,2)+" / "+DoubleToStr(maxCurrProfit,2);
else                   ProfitLoss=DoubleToStr(maxLoss,2)+" / "+DoubleToStr(maxProfit,2)+" (all)";

// ---- TxtTime
if(autoTrading==false) TxtTime="DayTrading deaktiviert";
else

if(TimeHour(Time[0]) >MathMax(GMTendHour,GMTclosingHour)) {
TxtTime="DayTrading geschlossen bis "+(GMTstartHour+GMT_Diff)+":00"; }
else

if(TimeHour(Time[0])<=MathMax(GMTendHour,GMTclosingHour)) {
TxtTime=((GMTstartHour+GMT_Diff)+":00 - "+(GMTendHour+GMT_Diff)+":00 / "+(GMTclosingHour+GMT_Diff)+":00 ChartTime");  }

// ----
if(Reversal==true) string TxtReversal="(<R>)"; 

// ---- OrderTxt
if(OrderTyp==1)        OrderTxt=">>>>> Long <<<<<   "; else
if(OrderTyp==2)        OrderTxt=">>>>> Short <<<<<  "; else
                       OrderTxt="";
// ---- Symbol-Margin                       
double newMargin = MarketInfo(Symbol(), MODE_MARGINREQUIRED);

// ----------------------------------------------
// ----------------- Comment --------------------
// ----------------------------------------------
if(CommentDaten==true)
  Comment (   
       "                                                                       "+
       DoubleToStr((h1-o1)/digitFactor/Point,0)+" ["+Risk+"] -"+DoubleToStr((o1-l1)/digitFactor/Point,0)+
       "      Margin: "+DoubleToStr(newMargin*Lots,2)+"  (o1)"+"\n"+
       "Lots, max:      "+DoubleToStr(Lots,2)+"  Steps: "+trendSteps+
       "\n"+  
       "ProfitStop:     "+DoubleToStr(ProfStop,2)+"  ("+DoubleToStr(allProfits,2)+")\n"+
       "Loss / Profit:  "+ProfitLoss+"\n"+ 
       "TP / SL:         "+DoubleToStr(TP/digitFactor,0)+" / -"+DoubleToStr(SL/digitFactor,0)+"  "+TxtReversal+"\n"+ 
       TxtTime+TradingTxt+"\n"+
       OrderTxt+"\n"+
       "\n" 
           );                

// --------------------------------------------
// ---------------- OrderSend -----------------
// --------------------------------------------
if(autoTrading==true && inTime==1)
  {
     if (Bid>o1
      
      && trend=="Up"
      && aktTrend=="Up"      
      && CCI_Trend=="Up"
      && cntOpLong<1
      && inTime==1)
      
     OrderSend(Symbol(),OP_BUY,Lots,Ask,0,0,0,"dailyTrendReversal",MagicNumber,0,Blue);

     if (Ask<o1
      
      && trend=="Dn"
      && aktTrend=="Dn"      
      && CCI_Trend=="Dn"
      && cntOpShort<1
      && inTime==1)
      
     OrderSend(Symbol(),OP_SELL,Lots,Bid,0,0,0,"dailyTrendReversal",MagicNumber,0,Red);
  }
// -----------------------------
// --------- OrderClose --------
// -----------------------------
        for (cnt=0; cnt<OrdersTotal(); cnt++)
          {                                               
          OrderSelect(cnt,SELECT_BY_POS);
          if(GMTclosingHour!=0 && Hour()>=(GMTclosingHour+GMT_Diff))
            {
            if (OrderType()==OP_BUY && OrderSymbol()==Symbol())
             OrderClose(OrderTicket(),OrderLots(),Bid,0);
        
            if (OrderType()==OP_SELL && OrderSymbol()==Symbol())
            OrderClose(OrderTicket(),OrderLots(),Ask,0);
            }   
          } // for (cnt=0
          
// ----------------------------------------------
// ------------- OrderModify TP u. SL -----------
// ----------------------------------------------
if(cntOpPos!=0 && autoTrading==true)
  {
  TakeProfit=TP*Point; StopLoss=SL*Point;

  for(cnt=0; cnt<OrdersTotal(); cnt++)
  {
  OrderSelect(cnt,SELECT_BY_POS);

   if( OrderType()==OP_BUY && OrderSymbol()==Symbol() )
      {
      if(TP!=0 && OrderTakeProfit()==0 && SL!=0 && OrderStopLoss()==0)     
        { OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-StopLoss,OrderOpenPrice()+TakeProfit,0); }
      if(TP!=0 && OrderTakeProfit()==0 && SL==0)     
        { OrderModify(OrderTicket(),OrderOpenPrice(), 0,OrderOpenPrice()+TakeProfit,0); }  
      if(TP==0                         && SL!=0 && OrderStopLoss()==0)     
        { OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-StopLoss, 0,0); }  
      }
// ---- 
   if( OrderType()==OP_SELL && OrderSymbol()==Symbol() )
      {     
      if(TP!=0 && OrderTakeProfit()==0 && SL!=0 && OrderStopLoss()==0)   
        { OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+StopLoss,OrderOpenPrice()-TakeProfit,0); }
      if(TP!=0 && OrderTakeProfit()==0 && SL==0)   
        { OrderModify(OrderTicket(),OrderOpenPrice(), 0,OrderOpenPrice()-TakeProfit,0); }
      if(TP==0                         && SL!=0 && OrderStopLoss()==0)   
        { OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+StopLoss, 0,0); }
      }
// ----          
 } // for(cnt=0
} // if(TP

// ----------------------------------------------
// ----------------- Reversal -------------------
// ----------------------------------------------
if (cntOpPos>0)
{ 
  for (cnt=0;cnt<OrdersTotal();cnt++)
  { 
  OrderSelect(cnt, SELECT_BY_POS); 
    if (OrderSymbol()==Symbol()) 
    {
      if (OrderType()==OP_BUY)
      {
      if(Reversal==true
       && Bid<o1
       && (  (trendSteps>=0 && (Bid-l1)>Risk*digitFactor*Point)
          || (trendSteps>=2 && (h1-o1) >=Risk*digitFactor*Point && (o1-Bid)<=10*digitFactor*Point)
          )      
       //&& trend=="Dn"
       && aktTrend=="Dn"
       && CCI_Trend=="Dn"            
      )      
        { OrderClose(OrderTicket(),Lots,Bid,0); TradingTxt=", Trendwechsel Long -> Short!"; }
      }
// --      
      if (OrderType()==OP_SELL)
      {
      if(Reversal==true
      && Bid>o1
       && (  (trendSteps>=0 && (h1-Bid)>Risk*digitFactor*Point)
          || (trendSteps>=2 && (o1-l1) >=Risk*digitFactor*Point && (Bid-o1)<=10*digitFactor*Point)     
          )
       //&& trend=="Up"
       && aktTrend=="Up"
       && CCI_Trend=="Up"        
      )
        { OrderClose(OrderTicket(),Lots,Ask,0); TradingTxt=", Trendwechsel Short -> Long!"; }     
      }
    } //  if (OrderSymbol()
  } // for (cnt=0
} // if (cntOpPos>0)

// ----------------------------------------------
// --------- DayTrade-Closing -------------------
// ----------------------------------------------
if(  cntOpPos!=0)
{ 
 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

     if(OrderType()==OP_BUY && OrderSymbol()==Symbol()
       && (GMTclosingHour!=0 && TimeHour(Time[0])>=(GMTclosingHour+GMT_Diff))
       || (HoldingHours!=0 && TimeCurrent()-OrderOpenTime()>=HoldingHours*3600)
       )
     {
     if(Bid<OrderOpenPrice())
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice(),0);
     if(Bid>OrderOpenPrice())
     OrderClose(OrderTicket(),Lots,Bid,0);     
     }
   
// ----   
     if(OrderType()==OP_SELL && OrderSymbol()==Symbol()
       && (GMTclosingHour!=0 && TimeHour(Time[0])>=(GMTclosingHour+GMT_Diff))
       || (HoldingHours!=0 && TimeCurrent()-OrderOpenTime()>=HoldingHours*3600)
       )
     { 
     if(Ask>OrderOpenPrice())
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice(),0);
     if(Ask<OrderOpenPrice())
     OrderClose(OrderTicket(),Lots,Ask,0);          
     }
 } // for(cnt=0;
} // if(GMTclosingHour!=0

// ----------------------------------
// --------- ProfitStop-Close -------
// ----------------------------------
if(allProfits>=ProfStop)
   {
   autoTrading=false; GMTendHour=GMTstartHour; TradingTxt=", ProfitStop erreicht!";
   
   if (OrderType()==OP_BUY )
       OrderClose(OrderTicket(),OrderLots(),Bid,0);
        
   if (OrderType()==OP_SELL)
       OrderClose(OrderTicket(),OrderLots(),Ask,0);
   } // if((GMTclosingHour+GMT_Diff)!=0)

// ----
  } // start
return(0);       
//+------------------------------------------------------------------+