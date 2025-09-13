//+------------------------------------------------------------------+
//|  oneTrade.mq4             29.07.2010 - 07:51
//|  
//+------------------------------------------------------------------+
#property copyright "hansH"
#property link      ""

// ----
extern bool   autoTrading    = true;
// ----
       bool   CommentDaten   = true;
// *****************************************************
// manuelle Entries
       int    OrderTyp       =    0;  // extern // 0=Long&Short, 1=Long, 2=Short;

// Entries
       double shortEntry, longEntry, TakeProfit, StopLoss;
extern int    Risk           =    1; // Entry-Range +/-50 Pips

//----       
extern double Lots           =  0.1;

// TakeProfit
extern double OrderTP        =  100; // 100 Pips
extern double OrderSL        = 1000; //1000 
       double TP, SL;      
           
// Trading-Zeiten
extern int    Start_Std      =    7;  // 6 Uhr TradeBeginn dtsch. Zeit
       
extern int    Schluss        =   18;  // 11, 21, 22 oder / bis
extern int    Closing        =   20;  // 22:00 Uhr
       int    ZD;                     // 1=WZ, 2=SZ Zeit-Differenz
       int    inTime;                 // Tradingzeit 
extern int    HoldingHours   =    0;

// Zeit-Differennz-Korrektur      
       int    Sommerzeit     =   87;  // DayOfYear Beginn Sommerzeit
       int    Winterzeit     =  297;  // DayOfYear Beginn Winterzeit

// ----
extern bool   Rectangle      = false;
       double dailyOpen, dailyHigh, dailyLow;
// ----
extern string TRADING°TESTER ="=== oneTrade ===";

// ----
       double clsBuyOrder, clsSellOrder;
       double opLongPos, opShortPos, shortTP, longTP;
       string OrderTxt, TxtTime, TxtEntry;
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
   
   ObjectDelete("Trend"); ObjectDelete("Trend_Label");
  }
  
//---------
int start()
  {
 //ZD=0;
if(DayOfYear()>=Sommerzeit && DayOfYear()<=Winterzeit) ZD=2; else ZD=1;
if(TimeHour(Time[0])<=5) { clsBuyOrder=0; clsSellOrder=0; }

// ---- Tradingzeit 1=ja, 0=nein
    if(DayOfWeek()>0 && DayOfWeek()<=5
    && TimeHour(Time[0])>=Start_Std-ZD     
    && TimeHour(Time[0])< Schluss-ZD)
        
    inTime=1; else inTime=0;
    
// ----
if(Close[0]>10)  { Dig=2; }  else
if(Close[0]<10)  { Dig=4; }

// ---
if(OrderTP!=0) TP=OrderTP*10; else TP=0;
if(OrderSL!=0) SL=OrderSL*10; else SL=0;

// ************************************************************************
// ********** Selektion ********** (Anfang) ********** Selektion **********
      dailyHigh = iHigh(NULL,1440,0);
      dailyLow  = iLow (NULL,1440,0); 
      dailyOpen = iOpen(NULL,1440,0);

// ********** Selektion ********** (Ende) ********** Selektion **********
// ************************************************************************
           
// --------------------------------------------------------------
// -------- bilanzierte u. offene Positionen ermitteln ----------
// --------------------------------------------------------------
int cnt = OrdersHistoryTotal();

// ---- History
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

 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

 if(OrderType()==OP_BUY  && OrderSymbol()==Symbol()) { cntOpLong=cntOpLong+1; }
 if(OrderType()==OP_SELL && OrderSymbol()==Symbol()) { cntOpShort=cntOpShort+1;}
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
      shortEntry=0; }
   
    if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
    { opLongPos=OrderOpenPrice(); longTP= (Bid-opLongPos)/Point;  col2=SpringGreen;
      shortEntry=OrderOpenPrice();  }
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
   ObjectSetText("shortTP","("+DoubleToStr(shortTP/10,0)+")",9,"Tahoma", col1a);
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
   ObjectSetText("longTP","("+DoubleToStr(longTP/10,0)+")",9,"Tahoma", col2a);
   ObjectSet("longTP", OBJPROP_CORNER, 0);
   ObjectSet("longTP", OBJPROP_XDISTANCE, 126);
   ObjectSet("longTP", OBJPROP_YDISTANCE, 11); 
   }
} // if(CommentDaten
  
// -----------------------------------------------------
// ----------------- Text für Comment ------------------
// ----------------------------------------------------- 
// ---- Spread
double spread=MarketInfo(Symbol(),MODE_SPREAD)/10;

// ---- TxtTime
if(autoTrading==false) TxtTime="Trading deaktiviert!  ("+HoldingHours+" Std.)";
 else
 
if(TimeHour(Time[0])<=MathMax(Schluss,Closing)-ZD) {
TxtTime=Start_Std+":00"+" bis "+Schluss+":00 / "+Closing+" Uhr  ("+HoldingHours+" Std.)"; } 
 else
 
if(TimeHour(Time[0]) >MathMax(Schluss,Closing)-ZD) {
TxtTime="DayTrading geschlossen bis "+Start_Std+":00  ("+HoldingHours+" Std.)"; }
   
// ---- OrderTxt
if(OrderTyp==1)        OrderTxt=">>>>> Long <<<<<   "; else
if(OrderTyp==2)        OrderTxt=">>>>> Short <<<<<  "; else
                       OrderTxt="";

// ----------------------------------------------
// ----------------- Comment --------------------
// ----------------------------------------------
if(CommentDaten==true) // "DayOfYear: "+DayOfYear()+"\n" +
   Comment(
       // Standard       
       "\n"+"\n"+

       // Extra s.Selektion ****************************       
       //"O "+DoubleToStr(dailyOpen,Digits-1)+ //" H "+DoubleToStr(dailyHigh,Digits)+" L "+DoubleToStr(dailyLow,Digits)+
       "\n"+
       //"\n"+
       // **********************************************
       
       // Standard
       "Lots: "+DoubleToStr(Lots,1)+"  "+       
       "Spread: "+DoubleToStr(spread,1)+"  "+
       "L/S: "+(cntHistBuy+cntOpLong)+"´"+(cntHistSell+cntOpShort)+"\n"+       
       "TP/SL: "+DoubleToStr(TP/10,0)+" / -"+DoubleToStr(SL/10,0)+
       "  op "+DoubleToStr(dailyOpen,Digits-1)+"\n"+     
       TxtTime+"\n"+
       OrderTxt+"\n"+
       "\n" 
           );                

// ----------------------------------------------
// --------------- Rectangle (Range) ------------
// ----------------------------------------------
  double   EntryL, topL, bottomL, EntryS, topS, bottomS; 
  datetime EntryTimeL,EntryTimeS;
  color    colRangeL=C'0,0,100', colRangeS=C'100,0,0';

// --
int cntRect;
if(OrdersTotal()!=0)
  {
  for(cntRect=0; cntRect<OrdersTotal(); cntRect++)
    {
    OrderSelect(cntRect,SELECT_BY_POS);

     if( OrderType()==OP_SELL && OrderSymbol()==Symbol() )
       { opShortPos=OrderOpenPrice(); shortTP=(opShortPos-Ask)/Point; }
   
     if( OrderType()==OP_BUY && OrderSymbol()==Symbol() )
       { opLongPos=OrderOpenPrice(); longTP= (Bid-opLongPos)/Point; }
    } // for(cntRect=0;   
 } // if(OrdersTotal()!=0)

// --
  ObjectDelete("RangeL"); ObjectDelete("RangeL_Label");
  ObjectDelete("RangeS"); ObjectDelete("RangeS_Label");

  // offene Orders prüfen ...
int cnt0buy=0, cnt0sell=0 ;
for (i=0; i<OrdersTotal(); i++) 
 {
  if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    
  if(OrderSymbol()==Symbol())
   {
    if(OrderType()==OP_BUY)
      { EntryL     = OrderOpenPrice();
        EntryTimeL = OrderOpenTime();
        if(Rectangle==false) { // OrderTakeProfit()==0        
        topL       = OrderOpenPrice();
        bottomL    = OrderOpenPrice()+30*Point; }
        else {
        topL       = OrderTakeProfit();
        bottomL    = OrderOpenPrice(); }        
// ----
datetime leftL   =EntryTimeL;
datetime rightL  =TimeCurrent();

// ----
ObjectCreate("RangeL",OBJ_RECTANGLE,0,leftL,topL,rightL,bottomL);
ObjectSet("RangeL",OBJPROP_BACK,true);
if(OrderTakeProfit()==0) ObjectSet("RangeL",OBJPROP_COLOR,Green);
   else ObjectSet("RangeL",OBJPROP_COLOR,colRangeL);

      } // if(OrderType

// ----
    if(OrderType()==OP_SELL && opShortPos!=0)
      { EntryS     = OrderOpenPrice();
        EntryTimeS = OrderOpenTime();
        if(Rectangle==false) { // OrderTakeProfit()==0
        topS       = OrderOpenPrice();
        bottomS    = OrderOpenPrice()-30*Point; }
        else {
        topS       = OrderOpenPrice();
        bottomS    = OrderTakeProfit(); }
// ----
datetime leftS   =EntryTimeS;
datetime rightS  =TimeCurrent();

// ----
ObjectCreate("RangeS",OBJ_RECTANGLE,0,leftS,topS,rightS,bottomS);
ObjectSet("RangeS",OBJPROP_BACK,true);
if(OrderTakeProfit()==0) ObjectSet("RangeS",OBJPROP_COLOR,Red); 
   else ObjectSet("RangeS",OBJPROP_COLOR,colRangeS);

      } // if(OrderType
   } // if(OrderSymbol()==Symbol())  
 } // for(int

// --------------------------------------------
// ---------------- OrderSend -----------------
// --------------------------------------------

      dailyHigh = iHigh(NULL,1440,0);
      dailyLow = iLow(NULL,1440,0); 
      dailyOpen = iOpen(NULL,1440,0);
// ----
     if ((Bid>dailyOpen && (dailyOpen-dailyLow)>Risk*10*Point)
      && cntOpPos<1 && cntHistBuy<1
      && inTime==1)
     OrderSend(Symbol(),OP_BUY,Lots,Ask,0,0,0,"oneTrade Long",0,0);

     if ((Ask<dailyOpen && (dailyHigh-dailyOpen)>Risk*10*Point)
      && cntOpPos<1 && cntHistSell<1
      && inTime==1)
     OrderSend(Symbol(),OP_SELL,Lots,Bid,0,0,0,"oneTrade Short",0,0);
     
// ---- OrderClose
          for (cnt=0; cnt<OrdersTotal(); cnt++)
            {                                               
            OrderSelect(cnt,SELECT_BY_POS);
            
            if (OrderType()==OP_BUY && Hour()>=Closing-ZD)
             OrderClose(OrderTicket(),OrderLots(),Bid,0);
        
            if (OrderType()==OP_SELL && Hour()>=Closing-ZD)
            OrderClose(OrderTicket(),OrderLots(),Ask,0);
            }   


// ----------------------------------------------
// ------------- OrderModify TP u. SL -----------
// ----------------------------------------------
if(   cntOpPos!=0
   && TP!=0 && SL!=0
   && autoTrading==true)
{
   TakeProfit=TP*Point; StopLoss=SL*Point;

 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

  if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && OrderTakeProfit()==0 && OrderStopLoss()==0)
    {
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-StopLoss,OrderOpenPrice()+TakeProfit,0);
    }
   
// ---- 
 if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && OrderTakeProfit()==0 && OrderStopLoss()==0)
   {
   OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+StopLoss,OrderOpenPrice()-TakeProfit,0);
   }
 } // for(cnt=0
} // if(TP


// ----------------------------------------------
// --------- DayTrade-Closing -------------------
// ----------------------------------------------
if(  cntOpPos!=0)
{ 
 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

     if(OrderType()==OP_BUY && OrderSymbol()==Symbol()
       && (Closing!=0 && TimeHour(Time[0])>=Closing-ZD)
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
       && (Closing!=0 && TimeHour(Time[0])>=Closing-ZD)
       || (HoldingHours!=0 && TimeCurrent()-OrderOpenTime()>=HoldingHours*3600)
       )
     { 
     if(Ask>OrderOpenPrice())
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice(),0);
     if(Ask<OrderOpenPrice())
     OrderClose(OrderTicket(),Lots,Ask,0);          
     }
 } // for(cnt=0;
} // if(Closing!=0

// ----
  } // start
return(0);       
//+------------------------------------------------------------------+