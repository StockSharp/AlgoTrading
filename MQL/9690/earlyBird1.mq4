//+------------------------------------------------------------------+
//|  earlyBird1.mq4    (earlyRangeBH1.mq4)        26.05.2010 - 17:10
//|  mit Hedge-Funktion
//|  Range-Break mit RSI-Filter +/-50
//|  TrailingStop/Profit mit Vola-Filter
//|  DayTrading: je 1 Buy- u. Sell-Trade
//+------------------------------------------------------------------+
#property copyright "hansH"
#property link      "finanz.consulting@freenet.de"

string        Name           ="earlyBird1";

// ----
extern bool   autoTrading    = true;
extern bool   HedgeTrading   = true;
// ----
       bool   CommentDaten   = true;
// *****************************************************
// manuelle Entries
extern int    OrderTyp       =    0;  // extern // 0=Long&Short, 1=Long, 2=Short;
 
// TakeProfit
extern double OrderTP        =   25; // 15
           
// StopLoss      
extern double OrderSL        =   50; // 30

// TrailingStop/Profit
extern double TrailingStop   =   15; // 50
extern double TrailingRisk   =    1; // 1.1

// Umwandlung
       double shortEntry, longEntry, TakeProfit, TrailingTP, StopLoss, TrailingSL;
       string Long_Selekt, Short_Selekt;
       
// Einstieg u. Hinweis
       string ShortInfo      = "  short";        
       string LongInfo       = "  long";       

//----       
extern double Lots           =  0.1;

// Trading-Zeiten
extern int    Start_Std      =    7;  // 6 Uhr TradeBeginn dtsch. Zeit
       int    Start_Min      =   15;  //  extern
       
extern int    Schluss        =   15;  // 11, 21, 22 oder / bis
extern int    Closing        =   17;  // 22:00 Uhr
       int    ZD;                     // 1=WZ, 2=SZ Zeit-Differenz
       int    inTime;                 // Tradingzeit 

// Zeit-Differennz-Korrektur      
       int    Sommerzeit     =   87;  // DayOfYear Beginn Sommerzeit
       int    Winterzeit     =  297;  // DayOfYear Beginn Winterzeit

// Break-Time
extern int    TimeStart      =    3;
extern int    TimeEnd        =    7;
datetime      Time_Start, Time_End, RectangleEnd;

// ----
extern int    RectangleHours =   72;

// ----
       bool   PendingView    = true;

// ----
extern string TRADING°TESTER ="=== earlyBird1 ===";
       int    MagicNumber    = 2605101435;
// ----
       double clsBuyOrder, clsSellOrder;
       double opLongPos, opShortPos, shortTP, longTP;
       string OrderTxt, TxtCom, TxtTime, doubleTxt, PendingTyp, RSItrend;
       color  col1=Gray, col2=Gray, col3=Silver, col4=Silver, col5=Silver, col1a=Gray, col2a=Gray;
       double Fakt, RSI;
       int    cntHistBuy, cntHistSell;
       int    i, Dig, opBar, cnt, oLP, oSP, offen, opOrders, opPos, LongIsOpen, ShortIsOpen;
       double Vola, Vola0;
       
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

if(DayOfYear()>=Sommerzeit && DayOfYear()<=Winterzeit) ZD=2; else ZD=1;
if(TimeHour(Time[0])<=5) { clsBuyOrder=0; clsSellOrder=0; }

//  if(TimeHour(Time[0])>= 3-ZD && TimeHour(Time[0])< 10-ZD)  { TimeStart=3; TimeEnd= 7; } else
//  if(TimeHour(Time[0])>=10-ZD && TimeHour(Time[0])< 24   )  { TimeStart=6; TimeEnd=10; }

   Time_Start   = StrToTime(StringConcatenate(Day(),".",Month(),".",Year()," ",TimeStart-ZD,":00"));
   Time_End     = StrToTime(StringConcatenate(Day(),".",Month(),".",Year()," ",TimeEnd-ZD,  ":00"));
   RectangleEnd = StrToTime(StringConcatenate(Day(),".",Month(),".",Year()," ",Schluss-ZD,  ":00"));   

// ---- Tradingzeit 1=ja, 0=nein
    if(DayOfWeek()>0 && DayOfWeek()<=5
    && ((TimeHour(Time[0])==Start_Std-ZD && TimeMinute(Time[0])>=Start_Min)
    || TimeHour(Time[0])>Start_Std-ZD)     
    && TimeHour(Time[0])< Schluss-ZD)
        
    inTime=1; else inTime=0;
    
// ----
if(Close[0]>10)  {Fakt=100;   Dig=2; }  else
if(Close[0]<10)  {Fakt=10000; Dig=4; }

// ----
if(TrailingStop!=0) TrailingSL=TrailingStop/Fakt;

// ************************************************************************
// ********** Selektion ********** (Anfang) ********** Selektion **********

// ---- Breakwerte
      RSI  = iRSI(NULL,0,14,PRICE_OPEN,0);   

      int    BarStart = iBarShift(NULL,0,Time_Start,false);
      int    BarEnd   = iBarShift(NULL,0,Time_End  ,false);

double Top    = iHigh(NULL,0,iHighest(NULL,0,MODE_HIGH,BarStart-BarEnd,BarEnd))+2/Fakt;
double Bottom = iLow (NULL,0,iLowest (NULL,0,MODE_LOW, BarStart-BarEnd,BarEnd))-2/Fakt;

// ---- Entries
       if(RSI> 50) longEntry=Top;     else longEntry =0;
       if(RSI<=50) shortEntry=Bottom; else shortEntry=0;

// ---- Vola 
double V1, V2, V3, V4, V5, V6, V7, V8, V9, V10, V11, V12, V13, V14, V15, V16;
     V1=High[i+1]-Low[i+1]; V2=High[i+2]-Low[i+2]; V3=High[i+3]-Low[i+3]; V4=High[i+4]-Low[i+4];
     V5=High[i+5]-Low[i+5]; V6=High[i+6]-Low[i+6]; V7=High[i+7]-Low[i+7]; V8=High[i+8]-Low[i+8];     
     V9=High[i+9]-Low[i+9]; V10=High[i+10]-Low[i+10]; V11=High[i+11]-Low[i+11]; V12=High[i+12]-Low[i+12];     
     V13=High[i+13]-Low[i+13]; V14=High[i+14]-Low[i+14]; V15=High[i+15]-Low[i+15]; V16=High[i+16]-Low[i+16];     
     
     Vola=(V1+V2+V3+V4+V5+V6+V7+V8+V9+V10+V11+V12+V13+V14+V15+V16)*Fakt/16;
     Vola0=(High[i]-Low[i])*Fakt;
       
// ---- Rectangle  
         ObjectDelete("bar0"); ObjectDelete("bar0_Label");
     
         ObjectCreate("bar0", OBJ_RECTANGLE, 0, 0,0, 0,0);
         ObjectSet   ("bar0", OBJPROP_STYLE, STYLE_SOLID);
         ObjectSet   ("bar0", OBJPROP_COLOR, C'60,60,60');
         ObjectSet   ("bar0", OBJPROP_BACK,  true);
         ObjectSet   ("bar0", OBJPROP_TIME1 ,Time_End); // Time_Start // Time_End-1*60
         ObjectSet   ("bar0", OBJPROP_PRICE1,Top);
         ObjectSet   ("bar0", OBJPROP_TIME2 ,MathMin(TimeCurrent(),RectangleEnd)); // Time_End od. TimeCurrent()
         ObjectSet   ("bar0", OBJPROP_PRICE2,Bottom);

// ---- SL und TP         
   StopLoss  =MathMin(OrderSL/Fakt,   MathMax(OrderSL/Fakt,Top-Bottom));         
   TakeProfit=MathMin(OrderTP/Fakt, MathMin(OrderSL/Fakt,Top-Bottom)); 

// ********** Selektion ********** (Ende) ********** Selektion **********
// **********************************************************************
           
// --------------------------------------------------------------
// ---- bilanzierte, offene Positionen u. Pendings ermitteln ----
// --------------------------------------------------------------
int cnt = OrdersHistoryTotal();

// ---- geschlossene Positionen
cntHistBuy=0; cntHistSell=0;
  
  for (int i=0; i < cnt; i++) 
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
opOrders=0;

if(OrdersTotal()!=0)
  {
  for(cnt=0; cnt<OrdersTotal(); cnt++)
   {
   OrderSelect(cnt,SELECT_BY_POS);
   if((OrderType()==OP_SELL || OrderType()==OP_BUY) && OrderSymbol()==Symbol())
       {
       opOrders=opOrders+1;
       }
   } // for(cnt
  } // if(OrdersTotal

// ---- Anzahl offene Positionen/Typ
LongIsOpen=0; ShortIsOpen=0;

 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

 if(OrderType()==OP_BUY  && OrderSymbol()==Symbol()) { LongIsOpen=LongIsOpen+1; }
 if(OrderType()==OP_SELL && OrderSymbol()==Symbol()) { ShortIsOpen=ShortIsOpen+1; }
 }

// --------------------------------------------------------------------------------
string lEntryTxt=DoubleToStr(longEntry,Digits);
string sEntryTxt=DoubleToStr(shortEntry,Digits);

// ----
int    DiffL, DiffS;

// ----
string BidTxt=DoubleToStr(Bid,Digits);
string AskTxt=DoubleToStr(Ask,Digits);

// ----
string longTxt, shortTxt;
if(longEntry!=0)  longTxt =DoubleToStr(longEntry,Digits);
if(shortEntry!=0) shortTxt=DoubleToStr(shortEntry,Digits);

// ----
   ObjectDelete("longTP"); ObjectDelete("longTP_Label");   
   ObjectDelete("shortTP"); ObjectDelete("shortTP_Label");
   ObjectDelete("opLong"); ObjectDelete("opLong_Label");   
   ObjectDelete("opShort"); ObjectDelete("opShort_Label");

// ----  
if(OrdersTotal()!=0)
  {
  opShortPos=0; shortTP=0; opLongPos=0; longTP=0;
    
  for(cnt=0; cnt<OrdersTotal(); cnt++)
   {
   OrderSelect(cnt,SELECT_BY_POS);
   
    if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
    { opShortPos=OrderOpenPrice(); shortTP=(opShortPos-Ask)*Fakt; col1=Red; }
   
    if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
    { opLongPos=OrderOpenPrice(); longTP= (Bid-opLongPos)*Fakt;  col2=SpringGreen; }
   } // for(cnt
  } // if(OrdersTotal
  
// --------------------------------------------
// ----------------- Objekte ------------------
// --------------------------------------------
if(opShortPos==0) {shortTP=0; col1=Gray; col1a=Gray;}
if(opLongPos==0)  {longTP=0;  col2=Gray; col2a=Gray;}
 
// ----
if(shortTP< 0) col1a=Yellow; if(shortTP> 0) col1a=Aqua;
if(longTP < 0) col2a=Yellow; if(longTP > 0) col2a=Aqua; 

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
   ObjectSetText("shortTP","("+DoubleToStr(shortTP,0)+")",9,"Tahoma", col1a);
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
   ObjectSetText("longTP","("+DoubleToStr(longTP,0)+")",9,"Tahoma", col2a);
   ObjectSet("longTP", OBJPROP_CORNER, 0);
   ObjectSet("longTP", OBJPROP_XDISTANCE, 126);
   ObjectSet("longTP", OBJPROP_YDISTANCE, 11); 
   }

// ----   
   ObjectCreate("aktuell", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("aktuell",StringSubstr(BidTxt,0,6),9,"Tahoma", col3);
   ObjectSet("aktuell", OBJPROP_CORNER, 0);
   ObjectSet("aktuell", OBJPROP_XDISTANCE, 172);
   ObjectSet("aktuell", OBJPROP_YDISTANCE, 11); 
   
// ---- Pending-Typ
if(cntHistBuy <=0 && cntHistSell ==0 && RSI>50 && LongIsOpen<=0)  {PendingTyp="*up*";   col4=Aqua; }  else
if(cntHistBuy <=0 && cntHistSell !=0 && RSI>50 && LongIsOpen<=0)  {PendingTyp="*up*";   col4=Aqua; }  else
if(cntHistBuy <=0 && cntHistSell !=0 && RSI<50 && LongIsOpen<=0)  {PendingTyp="*up*";   col4=Olive; } else

if(cntHistSell<=0 && cntHistBuy  ==0 && RSI<50 && ShortIsOpen<=0) {PendingTyp="*down*"; col4=Red; }   else
if(cntHistSell<=0 && cntHistBuy  !=0 && RSI<50 && ShortIsOpen<=0) {PendingTyp="*down*"; col4=Red; }   else
if(cntHistSell<=0 && cntHistBuy  !=0 && RSI>50 && ShortIsOpen<=0) {PendingTyp="*down*"; col4=Olive; } else
                                                                  {PendingTyp="-flat-"; col4=Gray; }
  
   ObjectCreate("Trend", OBJ_LABEL, 0, 0, 0);
   ObjectSetText("Trend",PendingTyp,12,"Arial Black", col4);
   ObjectSet("Trend", OBJPROP_CORNER, 0);
   ObjectSet("Trend", OBJPROP_XDISTANCE, 218);
   ObjectSet("Trend", OBJPROP_YDISTANCE, 4);
        
} // if(CommentDaten==true)
  
// -----------------------------------------------------
// ----------------- Text für Comment ------------------
// ----------------------------------------------------- 
double spread=MarketInfo(Symbol(),MODE_SPREAD)/10;

// ---- Risk
if(Vola0>=Vola*TrailingRisk && opOrders==1) string trail="Trailing!"; else trail="";

// ---- TxtCom
if(Period()<5)
   TxtCom="Periode ändern: von M5 bis H1"; else TxtCom="";

// ---- TxtTime
if(Start_Min<10) TxtTime=(Start_Std+":0"+Start_Min+" bis "+Schluss+":00 / "+Closing+" Uhr");
 else TxtTime=(Start_Std+":"+Start_Min+" bis "+Schluss+":00 / "+Closing+" Uhr");  
   
// ---- OrderTxt
if(autoTrading==false) OrderTxt="Trading deaktiviert!"; else

if(inTime==0)
if(Start_Min<10) OrderTxt="DayTrading geschlossen bis "+Start_Std+":0"+Start_Min; else
   OrderTxt="DayTrading geschlossen bis "+Start_Std+":"+Start_Min; else

if(OrderTyp==1)        OrderTxt=">>>>> Long <<<<<   "; else
if(OrderTyp==2)        OrderTxt=">>>>> Short <<<<<  "; else
                       OrderTxt="";

// ----------------------------------------------
// ----------------- Comment --------------------
// ----------------------------------------------
if(CommentDaten==true) // "DayOfYear: "+DayOfYear()+"\n" +
   Comment(
       // Standard       
       "\n"+
       //"\n"+

       // Extra s.Selektion ****************************
       "Vola:  ("+DoubleToStr(Vola,1)+") "+DoubleToStr(Vola0,2)+
       "  Range: "+TimeStart+"-"+TimeEnd+" Uhr"+"  "+trail+"\n"+
       "Break / Long:  "+DoubleToStr(Top,Dig)+
       "   RSI "+DoubleToStr(RSI,1)+"\n"+       
       "          Short:  "+DoubleToStr(Bottom,Dig)+
       "   L -S "+DoubleToStr((Top-Bottom)*Fakt,1)+"\n"+
       "\n"+
       // **********************************************
       
       // Standard
//     "   Spread: "+DoubleToStr(spread,1)+"\n"+
       "Lots: "+DoubleToStr(Lots,1)+doubleTxt+"  opPos: "+opOrders+"  L/S: "+(cntHistBuy+LongIsOpen)+"´"+(cntHistSell+ShortIsOpen)+
       "\n"+
       "TP: +"+DoubleToStr(TakeProfit*Fakt,0)+"  SL -"+DoubleToStr(StopLoss*Fakt,0)+
       "\n" +
       "Trail SL (TP):  "+DoubleToStr(TrailingStop,0)+"  ("+DoubleToStr(TrailingStop/2,1)+")\n" +
       TxtTime+"\n"+
       "\n"+
       TxtCom+"\n"+
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
  oSP=0; oLP=0;
  for(cntRect=0; cntRect<OrdersTotal(); cntRect++)
    {
    OrderSelect(cntRect,SELECT_BY_POS);

     if( OrderType()==OP_SELL && OrderSymbol()==Symbol() )
       { opShortPos=OrderOpenPrice(); shortTP=(opShortPos-Ask)*Fakt; oSP=oSP+1; }
   
     if( OrderType()==OP_BUY && OrderSymbol()==Symbol() )
       { opLongPos=OrderOpenPrice(); longTP= (Bid-opLongPos)*Fakt; oLP=oLP+1; }
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
    if(OrderType()==OP_BUY && OrderTakeProfit()!=0 && opLongPos!=0)
      { EntryL     = OrderOpenPrice();
        EntryTimeL = OrderOpenTime();
        topL       = OrderTakeProfit();
        bottomL    = OrderOpenPrice()+(Ask-Bid);
// ----
datetime leftL   =EntryTimeL;
datetime rightL  =EntryTimeL+(RectangleHours*60*60);

// ----
ObjectCreate("RangeL",OBJ_RECTANGLE,0,leftL,topL,rightL,bottomL);
ObjectSet("RangeL",OBJPROP_BACK,true);
ObjectSet("RangeL",OBJPROP_COLOR,colRangeL);

      } // if(OrderType

// ----
    if(OrderType()==OP_SELL && OrderTakeProfit()!=0 && opShortPos!=0)
      { EntryS     = OrderOpenPrice();
        EntryTimeS = OrderOpenTime();
        topS       = OrderOpenPrice();
        bottomS    = OrderTakeProfit()-(Ask-Bid);
// ----
datetime leftS   =EntryTimeS;
datetime rightS  =EntryTimeS+(RectangleHours*60*60);

// ----
ObjectCreate("RangeS",OBJ_RECTANGLE,0,leftS,topS,rightS,bottomS);
ObjectSet("RangeS",OBJPROP_BACK,true);
ObjectSet("RangeS",OBJPROP_COLOR,colRangeS);

      } // if(OrderType
   } // if(OrderSymbol()==Symbol())  
 } // for(int

// --------------------------------------------
// ------------- Trade / OrderSend ------------
// --------------------------------------------
 if(autoTrading==true
    && opOrders==0
    && inTime==1)
    
  {
// ---- open Long
  if(longEntry!=0
   && Low[0]<longEntry  
   && Bid<=longEntry+1/Fakt && Bid>=longEntry-2/Fakt

   && cntHistBuy==0
   && OrderTyp!=2)
       {
       OrderSend(Symbol(),OP_BUY,Lots,Ask,0, 0, 0,"oneTrade",MagicNumber,0);
       }

// ---- open Short
  if(shortEntry!=0
   && High[0]>shortEntry  
   && Bid>=shortEntry-1/Fakt && Bid<=shortEntry+2/Fakt

   && cntHistSell==0    
   && OrderTyp!=1)
       {
       OrderSend(Symbol(),OP_SELL,Lots,Bid,0, 0, 0,"oneTrade",MagicNumber,0);
       }

  } // autoTrading 

// --------------------------------------------
// -------------- Hedge / OrderSend -----------
// --------------------------------------------
 if(autoTrading==true
    && HedgeTrading==true
    && opOrders!=0
    && inTime==1)
    
  {
// ---- open Long
  if(longEntry!=0
   && LongIsOpen==0 && ShortIsOpen!=0
   && cntHistBuy==0
   
   && Low[0]<longEntry   
   && Bid<=longEntry+1/Fakt && Bid>=longEntry-2/Fakt

   && OrderTyp!=2)
       {
       OrderSend(Symbol(),OP_BUY,Lots,Ask,0, 0, 0,"oneHedge",MagicNumber,0,Green);
       }

// ---- open Short
  if(shortEntry!=0
   && ShortIsOpen==0 && LongIsOpen!=0
   && cntHistSell==0
   
   && High[0]>shortEntry   
   && Bid>=shortEntry-1/Fakt && Bid<=shortEntry+2/Fakt
    
   && OrderTyp!=1)
       {
       OrderSend(Symbol(),OP_SELL,Lots,Bid,0, 0, 0,"oneHedge",MagicNumber,0,Crimson);
       }

  } // autoTrading 

// ----------------------------------------------
// ------------- OrderModify TP u. SL -----------
// ----------------------------------------------
if(   opOrders!=0
// && inTime==1
   && (OrderTP!=0 || OrderSL!=0)
   && autoTrading==true)

{
 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

 if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
   {
   if(StopLoss!=0 && OrderStopLoss()==0)
   OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-StopLoss,OrderTakeProfit(),0);
   if(TakeProfit!=0 && OrderTakeProfit()==0)
   OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice()+TakeProfit,0);
   
   if(StopLoss==0 && OrderStopLoss()!=0)
   OrderModify(OrderTicket(),OrderOpenPrice(),0,OrderTakeProfit(),0);
   if(TakeProfit==0 && OrderTakeProfit()!=0)
   OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),0,0);      
   }

// ---- 
 if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
   {
   if(StopLoss!=0 && OrderStopLoss()==0)
   OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+StopLoss,OrderTakeProfit(),0);
   if(TakeProfit!=0 && OrderTakeProfit()==0)
   OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice()-TakeProfit,0);   

   if(StopLoss==0 && OrderStopLoss()!=0)
   OrderModify(OrderTicket(),OrderOpenPrice(),0,OrderTakeProfit(),0);
   if(TakeProfit==0 && OrderTakeProfit()!=0)
   OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),0,0);      
   }
 } // for(cnt=0;
} // if(OrderTP


// ----------------------------------------------
// --------------- TrailingSL/TP ----------------
// ----------------------------------------------
if(  opOrders!=0
  && TrailingStop!=0
  && Vola0 > Vola*TrailingRisk)
 
{
 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

  if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && opOrders==1)
    {
     if( Bid > OrderOpenPrice()+TrailingSL
      && Bid-TrailingSL > OrderStopLoss() )
      {
      OrderModify(OrderTicket(),OrderOpenPrice(),Bid-StopLoss,Bid+TrailingSL/2,0);
      } 
    }

// ---- 
  if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && opOrders==1)
    {
    if( Ask < OrderOpenPrice()-TrailingSL 
     && Ask+TrailingSL < OrderStopLoss() )
     {    
     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+StopLoss,Ask-TrailingSL/2,0);    
     } 
    }
 } // for(cnt=0;
} // if(TrailingStop

// ----------------------------------------------
// --------- DayTrade-Closing -------------------
// ----------------------------------------------
if(  opOrders!=0
  && Closing!=0 && TimeHour(Time[0])>=Closing-ZD)
{ 
 for(cnt=0; cnt<OrdersTotal(); cnt++)
 {
 OrderSelect(cnt,SELECT_BY_POS);

     if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
     {
     if(Bid<OrderOpenPrice())
     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),OrderOpenPrice(),0);
     if(Bid>OrderOpenPrice())
     OrderClose(OrderTicket(),Lots,Bid,0);     
     }
   
// ----   
     if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
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