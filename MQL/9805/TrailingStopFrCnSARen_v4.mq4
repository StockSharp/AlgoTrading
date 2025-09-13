//+------------------------------------------------------------------+
//|                                          TrailingStopFrCnSAR.mq4 |
//|                              Copyright © 2010, Khlystov Vladimir |
//|                                         http://cmillion.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, cmillion@narod.ru"
#property link      "http://cmillion.narod.ru"
#property show_inputs
//--------------------------------------------------------------------
extern string  parameters.trailing  = "0-off  1-Candle  2-Fractals  3-Velocity  4-Parabolic  >4-pips";
extern int     TrailingStop         = 1;      // 0 off 
extern int     StepTrall            = 0;      // step Thrall, moving not less than StepTrall n 
extern int     delta                = 0;      // offset from the fractal or candles or Parabolic 
extern bool    only_Profit          = true;   // sweep only profitable orders 
extern bool    only_NoLoss          = false;  // instead of simply translating Thrall in bezubytok 
extern bool    only_SL              = false;  // sweep only those orders that already have SL 
extern bool    SymbolAll            = false;  // trail all the tools 
extern bool    GeneralNoLoss        = true;   // on general profitsextern 

string         parameters.Parabolic = "";
extern double  Step                 = 0.02;
extern double  Maximum              = 0.2;
extern int     Magic                = 0;
extern bool    visualization        = true;
extern int     VelocityPeriodBar    = 30;
extern double  K_Velocity           = 1.0;    //magnification stoploss of Velocity

//--------------------------------------------------------------------
int  STOPLEVEL,n,DIGITS;
double BID,ASK,POINT;
string  SymbolTral,TekSymbol;
//--------------------------------------------------------------------
int start()                                  
{
   SymbolTral = Symbol();
   ObjectCreate("info",OBJ_LABEL,0,0,0);
   ObjectSet("info",OBJPROP_CORNER,2);      
   ObjectSet("info",OBJPROP_XDISTANCE,0); 
   ObjectSet("info",OBJPROP_YDISTANCE,15);
   ObjectCreate("info1",OBJ_LABEL,0,0,0);
   ObjectSet("info1",OBJPROP_CORNER,2);      
   ObjectSet("info1",OBJPROP_XDISTANCE,0); 
   ObjectSet("info1",OBJPROP_YDISTANCE,5);
   ObjectSetText("info1","Copyright © 2010, http://cmillion.narod.ru",8,"Arial",Gold);
   string Simb,txt="Set TrailingStop v4  ";
   if (TrailingStop==1) txt=StringConcatenate(txt,"by candles","\n"); 
   if (TrailingStop==2) txt=StringConcatenate(txt,"by Fractals","\n"); 
   if (TrailingStop==3) txt=StringConcatenate(txt,"by Velocity","\n"); 
   if (TrailingStop==4) txt=StringConcatenate(txt,"by Parabolic","\n"); 
   if (TrailingStop> 4) txt=StringConcatenate(txt,"by ",TrailingStop," pips","\n"); 
   if (Magic==0) txt=StringConcatenate(txt,"Magic all","\n"); 
   else  txt=StringConcatenate(txt,"Magic ",Magic,"\n");
   if (SymbolAll) {txt=StringConcatenate(txt,"All Symbol","\n");Simb="  All Symbols";}
   else  Simb=StringConcatenate("  ",SymbolTral);

   if (GeneralNoLoss) txt=StringConcatenate(txt,"sweep of the level without loss","\n");
   if (only_Profit) txt=StringConcatenate(txt,"only profitable orders","\n");
   if (only_NoLoss) txt=StringConcatenate(txt,"only translate into a loss without","\n");
   if (only_SL) txt=StringConcatenate(txt,"only orders with exhibited SL","\n");

   while(true)
   {
      RefreshRates();
      STOPLEVEL=MarketInfo(SymbolTral,MODE_STOPLEVEL);
      if (TrailingStop<STOPLEVEL) TrailingStop=STOPLEVEL;
      if (ObjectFind("info1")==-1 || ObjectFind("info")==-1) break;
      TrailingStop();
      ObjectSetText("info",StringConcatenate("Orders ", n,Simb),8,"Arial",Gold);
      if (n==0) break;
      Sleep(500);
      Comment(txt);
   }
   Comment("Closing script ",TimeToStr(TimeCurrent(),TIME_SECONDS));
   ObjectDelete("info");
   ObjectDelete("info1");
   ObjectDelete("SL Buy");
   ObjectDelete("STOPLEVEL-");
   ObjectDelete("SL Sell");
   ObjectDelete("STOPLEVEL+");
   ObjectDelete("NoLossSell");
   ObjectDelete("NoLossSell_");
   ObjectDelete("NoLossBuy");
   ObjectDelete("NoLossBuy_");
}
//--------------------------------------------------------------------
void TrailingStop()
{
   int tip,Ticket;
   bool error;
   double StLo,OSL,OOP,NoLoss;
   n=0;
   for (int i=OrdersTotal(); i>=0; i--) 
   {  if (OrderSelect(i, SELECT_BY_POS)==true)
      {  tip = OrderType();
         TekSymbol=OrderSymbol();
         if (tip<2 && (TekSymbol==SymbolTral || SymbolAll) && (OrderMagicNumber()==Magic || Magic==0))
         {
            POINT  = MarketInfo(TekSymbol,MODE_POINT);
            DIGITS = MarketInfo(TekSymbol,MODE_DIGITS);
            BID    = MarketInfo(TekSymbol,MODE_BID);
            ASK    = MarketInfo(TekSymbol,MODE_ASK);
            OSL    = OrderStopLoss();
            OOP    = OrderOpenPrice();
            Ticket = OrderTicket();
            if (tip==OP_BUY)             
            {  n++;
               if (GeneralNoLoss) NoLoss = TProfit(1,TekSymbol);
               OrderSelect(i, SELECT_BY_POS);
               StLo = SlLastBar(1,BID); 
               if ((StLo < NoLoss && GeneralNoLoss) || NoLoss==0) continue;
               if (StLo < OOP && only_Profit && !GeneralNoLoss) continue;
               if (OSL  >= OOP && only_NoLoss) continue;
               if (OSL  == 0 && only_SL) continue;
               if (StLo > OSL+StepTrall*POINT)
               {  error=OrderModify(Ticket,OOP,NormalizeDouble(StLo,DIGITS),OrderTakeProfit(),0,White);
                  Comment(TekSymbol,"  TrailingStop ",Ticket," ",TimeToStr(TimeCurrent(),TIME_MINUTES));
                  if (!error) Print(TekSymbol,"  Error order ",Ticket," TrailingStop ",
                              GetLastError(),"   ",SymbolTral,"   SL ",StLo);
               }
            }                                         
            if (tip==OP_SELL)        
            {  n++;
               if (GeneralNoLoss) NoLoss = TProfit(-1,TekSymbol); 
               OrderSelect(i, SELECT_BY_POS);
               StLo = SlLastBar(-1,ASK);  
               if (StLo > NoLoss && GeneralNoLoss) continue;
               if (StLo==0) continue;        
               if (StLo > OOP && only_Profit && !GeneralNoLoss) continue;
               if (OSL  <= OOP && only_NoLoss) continue;
               if (OSL  == 0 && only_SL) continue;
               if (StLo < OSL-StepTrall*POINT || OSL==0 )
               {  error=OrderModify(Ticket,OOP,NormalizeDouble(StLo,DIGITS),OrderTakeProfit(),0,White);
                  Comment(TekSymbol,"  TrailingStop "+Ticket," ",TimeToStr(TimeCurrent(),TIME_MINUTES));
                  if (!error) Print(TekSymbol,"  Error order ",Ticket," TrailingStop ",
                              GetLastError(),"   ",SymbolTral,"   SL ",StLo);
               }
            } 
         }
      }
   }
}
//--------------------------------------------------------------------
double SlLastBar(int tip,double price)
{
   double fr=0;
   int jj,ii;
   if (TrailingStop>4)
   {
      if (tip==1) fr = price - TrailingStop*POINT;  
      else        fr = price + TrailingStop*POINT;  
   }
   else
   {
      //------------------------------------------------------- by Fractals
      if (TrailingStop==2)
      {
         if (tip== 1)
         for (ii=1; ii<100; ii++) 
         {
            fr = iFractals(TekSymbol,0,MODE_LOWER,ii);
            if (fr!=0) {fr-=delta*POINT; if (price-STOPLEVEL*POINT > fr) break;}
            else fr=0;
         }
         if (tip==-1)
         for (jj=1; jj<100; jj++) 
         {
            fr = iFractals(TekSymbol,0,MODE_UPPER,jj);
            if (fr!=0) {fr+=delta*POINT; if (price+STOPLEVEL*POINT < fr) break;}
            else fr=0;
         }
      }
      //------------------------------------------------------- by candles
      if (TrailingStop==1)
      {
         if (tip== 1)
         for (ii=1; ii<500; ii++) 
         {
            fr = iLow(TekSymbol,0,ii)-delta*POINT;
            if (fr!=0) if (price-STOPLEVEL*POINT > fr) break;
            else fr=0;
         }
         if (tip==-1)
         for (jj=1; jj<500; jj++) 
         {
            fr = iHigh(TekSymbol,0,jj)+delta*POINT;
            if (fr!=0) if (price+STOPLEVEL*POINT < fr) break;
            else fr=0;
         }
      }   
      //------------------------------------------------------- by Velocity
      if (TrailingStop==3)
      {
         double Velocity_0 = iCustom(TekSymbol,0,"Velocity",VelocityPeriodBar,2,0);
         double Velocity_1 = iCustom(TekSymbol,0,"Velocity",VelocityPeriodBar,2,1);
         if (tip== 1)
         {
            if(Velocity_0>Velocity_1) fr = price - (delta-Velocity_0+Velocity_1)*POINT*K_Velocity;
            else fr=0;
         }
         if (tip==-1)
         {
            if(Velocity_1>Velocity_0) fr = price + (delta+Velocity_1-Velocity_0)*POINT*K_Velocity;
            else fr=0;
         }
      }
      //------------------------------------------------------- by PSAR
      if (TrailingStop==4)
      {
         double PSAR = iSAR(TekSymbol,0,Step,Maximum,0);
         if (tip== 1)
         {
            if(price-STOPLEVEL*POINT > PSAR) fr = PSAR - delta*POINT;
            else fr=0;
         }
         if (tip==-1)
         {
            if(price+STOPLEVEL*POINT < PSAR) fr = PSAR + delta*POINT;
            else fr=0;
         }
      }
   }
   //-------------------------------------------------------
   if (visualization && TekSymbol==SymbolTral)
   {
      if (tip== 1)
      {  
         if (fr!=0){
         ObjectDelete("SL Buy");
         ObjectCreate("SL Buy",OBJ_ARROW,0,Time[0]+Period()*60,fr,0,0,0,0);                     
         ObjectSet   ("SL Buy",OBJPROP_ARROWCODE,6);
         ObjectSet   ("SL Buy",OBJPROP_COLOR, Blue);}
         if (STOPLEVEL>0){
         ObjectDelete("STOPLEVEL-");
         ObjectCreate("STOPLEVEL-",OBJ_ARROW,0,Time[0]+Period()*60,price-STOPLEVEL*POINT,0,0,0,0);                     
         ObjectSet   ("STOPLEVEL-",OBJPROP_ARROWCODE,4);
         ObjectSet   ("STOPLEVEL-",OBJPROP_COLOR, Blue);}
      }
      if (tip==-1)
      {
         if (fr!=0){
         ObjectDelete("SL Sell");
         ObjectCreate("SL Sell",OBJ_ARROW,0,Time[0]+Period()*60,fr,0,0,0,0);
         ObjectSet   ("SL Sell",OBJPROP_ARROWCODE,6);
         ObjectSet   ("SL Sell", OBJPROP_COLOR, Pink);}
         if (STOPLEVEL>0){
         ObjectDelete("STOPLEVEL+");
         ObjectCreate("STOPLEVEL+",OBJ_ARROW,0,Time[0]+Period()*60,price+STOPLEVEL*POINT,0,0,0,0);                     
         ObjectSet   ("STOPLEVEL+",OBJPROP_ARROWCODE,4);
         ObjectSet   ("STOPLEVEL+",OBJPROP_COLOR, Pink);}
      }
   }
   return(fr);
}
//-------------------------------------------------------------------- calculation of total (general) TP
double TProfit(int tip,string Symb)
{
   int b,s;
   double price,price_b,price_s,lot,SLb,SLs,lot_s,lot_b;
   for (int j=0; j<OrdersTotal(); j++)
   {  if (OrderSelect(j,SELECT_BY_POS,MODE_TRADES)==true)
      {  if ((Magic==OrderMagicNumber() || Magic==0) && OrderSymbol()==Symb)
         {
            price = OrderOpenPrice();
            lot   = OrderLots();
            if (OrderType()==OP_BUY ) {price_b += price*lot; lot_b+=lot; b++;}                     
            if (OrderType()==OP_SELL) {price_s += price*lot; lot_s+=lot; s++;}
         }  
      }  
   }
   //--------------------------------------
   if (visualization && Symb==SymbolTral)
   {
      ObjectDelete("NoLossBuy");
      ObjectDelete("NoLossBuy_");
      ObjectDelete("NoLossSell");
      ObjectDelete("NoLossSell_");
   }
   if (b!=0) 
   {  SLb = price_b/lot_b;
      if (visualization && Symb==SymbolTral){
         ObjectCreate("NoLossBuy",OBJ_ARROW,0,Time[0]+Period()*60*5,SLb,0,0,0,0);                     
         ObjectSet   ("NoLossBuy",OBJPROP_ARROWCODE,6);
         ObjectSet   ("NoLossBuy",OBJPROP_COLOR, Blue);         
         ObjectCreate("NoLossBuy_",OBJ_ARROW,0,Time[0]+Period()*60*5,SLb,0,0,0,0);                     
         ObjectSet   ("NoLossBuy_",OBJPROP_ARROWCODE,200);
         ObjectSet   ("NoLossBuy_",OBJPROP_COLOR, Blue);}
   }
   if (s!=0) 
   {  SLs = price_s/lot_s;
      if (visualization && Symb==SymbolTral){
         ObjectCreate("NoLossSell",OBJ_ARROW,0,Time[0]+Period()*60*5,SLs,0,0,0,0);                     
         ObjectSet   ("NoLossSell",OBJPROP_ARROWCODE,6);
         ObjectSet   ("NoLossSell",OBJPROP_COLOR, Pink);         
         ObjectCreate("NoLossSell_",OBJ_ARROW,0,Time[0]+Period()*60*5,SLs,0,0,0,0);                     
         ObjectSet   ("NoLossSell_",OBJPROP_ARROWCODE,202);
         ObjectSet   ("NoLossSell_",OBJPROP_COLOR, Pink);}
   }
if (tip== 1) return(SLb);
if (tip==-1) return(SLs);
}
//--------------------------------------------------------------------

