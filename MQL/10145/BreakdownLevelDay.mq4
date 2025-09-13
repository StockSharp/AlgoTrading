//+------------------------------------------------------------------+
//| Daily Breakdown                            BreakdownLevelDay.mq4 |
//|                               Copyright © 2011, Hlystov Vladimir |
//|                                                cmillion@narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, http://cmillion.narod.ru"
#property link      "cmillion@narod.ru"
//--------------------------------------------------------------------
extern string TimeSet      = "07:32";  //Order place time, if TimeSet = "00:00", the EA works on the breakdown of the previous day. 
extern int    Delta        = 6,        //Order price shift (in points) from High/Low price
              SL           = 120,      //Stop Loss (in points)
              TP           = 90,       //Take Profit (in points)
              risk         = 0,        //if 0, fixed lot is used
              NoLoss       = 0,        //if 0, it doesn't uses break-even
              trailing     = 0;        //if 0, it doesn't uses trailing
extern double Lot          = 0.10;     //used only if risk = 0
extern bool   OpenStop     = true;     //Place stop orders if order is opened
extern color  color_BAR    = DarkBlue; //info color
//--------------------------------------------------------------------
double        MaxPrice,MinPrice;
int           STOPLEVEL,magic=123321,tip,TimeBarbuy,TimeBarSell,LastDay;
//--------------------------------------------------------------------
int init()
{
   STOPLEVEL = MarketInfo(Symbol(),MODE_STOPLEVEL);
   if (SL < STOPLEVEL) SL = STOPLEVEL;
   if (TP < STOPLEVEL) TP = STOPLEVEL;
   if (NoLoss   < STOPLEVEL && NoLoss   != 0) NoLoss   = STOPLEVEL;
   if (trailing < STOPLEVEL && trailing != 0) trailing = STOPLEVEL;
   Comment("Copyright © 2011 cmillion@narod.ru\n BreakdownLevelDay input parameters"+"\n"+
      "TimeSet   "   , TimeSet,           "\n",
      "Delta       " , Delta,             "\n",
      "SL           ", SL,                "\n",
      "TP          " , TP,                "\n",
      "Lot          ", DoubleToStr(Lot,2),"\n",
      "risk         ", risk,              "\n",
      "NoLoss    "   , NoLoss,            "\n",
      "trailing     ", trailing);
   if (TimeSet=="00:00") LastDay=1;
}
//--------------------------------------------------------------------
int start()
{
   if (OpenStop) magic=TimeDay(CurTime());
   if (!IsDemo()) {Comment("Demo version, contact cmillion@narod.ru");return;}
   //-----------------------------------------------------------------
   int buy,sel,error;
   bool BUY=false,SEL=false;
   for (int i=0; i<OrdersTotal(); i++)
   {  if (OrderSelect(i, SELECT_BY_POS)==true)
      {  
         if (OrderSymbol()!=Symbol() || OrderMagicNumber()!=magic) continue;
         tip=OrderType();
         if (tip==0) BUY = true;
         if (tip==1) SEL = true;
         if (tip==4) buy++;
         if (tip==5) sel++;
      }   
   }
   if ((BUY||SEL)&&(buy!=0||sel!=0)) DelAllStop();          // delete stop orders if order opened
   if ( BUY||SEL) 
   {
      if (trailing!=0) TrailingStop(trailing);
      if (NoLoss!=0) No_Loss(NoLoss);
      return;                                              // opened order exist
   }
   if (TimeStr(CurTime())!=TimeSet) return;                // if time isn't equal to order set time
   
   int expiration=CurTime()+(23-TimeHour(CurTime()))*3600+(60-TimeMinute(CurTime()))*60;  //set order expiration time
   double TrPr,StLo;
   if (risk!=0) Lot = LOT(); 
   if (buy<1&&TimeBarbuy!=TimeDay(CurTime()))
   {
      MaxPrice=iHigh(NULL,1440,LastDay)+NormalizeDouble(Delta*Point,Digits);
      if (Ask+STOPLEVEL*Point>MaxPrice) MaxPrice = NormalizeDouble(Ask+STOPLEVEL*Point,Digits);
      if (TP!=0) TrPr = NormalizeDouble(MaxPrice + TP * Point,Digits);                 
      if (SL!=0) StLo = NormalizeDouble(MaxPrice - SL * Point,Digits);                 
      error=OrderSend(Symbol(),OP_BUYSTOP ,Lot,MaxPrice,3,StLo,TrPr,"BUYSTOP BLD",magic,expiration,Blue);
      if (error==-1) Alert("Error BUYSTOP ",GetLastError(),"   ",Symbol(),"   Lot ",Lot,"   Price ",MaxPrice,"   SL ",StLo,"   TP ",TrPr,"   expiration ",expiration);
      else TimeBarbuy=TimeDay(CurTime());
   }
   if (sel<1&&TimeBarSell!=TimeDay(CurTime()))
   {
      MinPrice=iLow(NULL,1440,LastDay)-NormalizeDouble(Delta*Point,Digits);
      if (Bid-STOPLEVEL*Point<MinPrice) MinPrice = NormalizeDouble(Bid-STOPLEVEL*Point,Digits);
      if (TP!=0) TrPr = NormalizeDouble(MinPrice - TP * Point,Digits);                 
      if (SL!=0) StLo = NormalizeDouble(MinPrice + SL * Point,Digits);   
      error=OrderSend(Symbol(),OP_SELLSTOP,Lot,MinPrice,3,StLo,TrPr,"SELLSTOP BLD",magic,expiration,Red );
      if (error==-1) Alert("Error SELLSTOP ",GetLastError(),"   ",Symbol(),"   Lot ",Lot,"   Price ",MinPrice,"   SL ",StLo,"   TP ",TrPr,"   expiration ",expiration);
      else TimeBarSell=TimeDay(CurTime());
   }
   if (buy<1&&sel<1)
   {
      ObjectDelete("bar0");
      ObjectCreate("bar0", OBJ_RECTANGLE, 0, 0,0, 0,0);
      ObjectSet   ("bar0", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet   ("bar0", OBJPROP_COLOR, color_BAR);
      ObjectSet   ("bar0", OBJPROP_BACK,  true);
      ObjectSet   ("bar0", OBJPROP_TIME1 ,iTime( NULL, 1440, 0));
      ObjectSet   ("bar0", OBJPROP_PRICE1,MaxPrice);
      ObjectSet   ("bar0", OBJPROP_TIME2 ,CurTime());
      ObjectSet   ("bar0", OBJPROP_PRICE2,MinPrice);
   }
   return(0);
}
//--------------------------------------------------------------------
void DelAllStop()
{
   int tip;
   for (int i=0; i<OrdersTotal(); i++)
   {                                               
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
      {
         if (OrderSymbol()!=Symbol()||OrderMagicNumber()!=magic) continue;
         tip=OrderType();
         if (tip==4||tip==5) OrderDelete(OrderTicket());
      }   
   }
}
//--------------------------------------------------------------------
void TrailingStop(int trailing)
{
   double TrPr,StLo;
   int tip;
   bool error;
   color col;
   for (int i=0; i<OrdersTotal(); i++) 
   {
      if (OrderSelect(i, SELECT_BY_POS)==true)
      {
         tip = OrderType();
         if (tip<2 && OrderSymbol()==Symbol())
         {
            if (OrderMagicNumber()!=magic) continue;
            if (tip==0) //Buy               
            {  
               StLo = Bid - trailing*Point;          
               if (StLo > OrderStopLoss() && StLo > OrderOpenPrice()) 
                  {error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);Comment("Trailing "+OrderTicket());Sleep(500);}
            }                                         
            if (tip==1) //Sell               
            {                                         
               StLo = Ask + trailing*Point;            
               if (StLo < OrderStopLoss() && StLo < OrderOpenPrice()) 
                  {error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);Comment("Trailing "+OrderTicket());Sleep(500);}
            } 
            if (error==false && SL!=0) Alert("Error SELLSTOP ",GetLastError(),"   ",Symbol(),"   SL ",StLo);
         }//tip<2
      }//OrderSelect
   }//for
}
//------------------------------------------------------------------+
double LOT()
{
   double MINLOT = MarketInfo(Symbol(),MODE_MINLOT);
   double LOT = AccountFreeMargin()*risk/100/MarketInfo(Symbol(),MODE_MARGINREQUIRED)/15;
   if (LOT>MarketInfo(Symbol(),MODE_MAXLOT)) LOT = MarketInfo(Symbol(),MODE_MAXLOT);
   if (LOT<MINLOT) LOT = MINLOT;
   if (MINLOT<0.1) LOT = NormalizeDouble(LOT,2); else LOT = NormalizeDouble(LOT,1);
   return(LOT);
}
//------------------------------------------------------------------+
void No_Loss(int NoLoss)
{
   double TrPr,StLo;
   int tip;
   bool error;
   color col;
   for (int i=0; i<OrdersTotal(); i++) 
   {
      if (OrderSelect(i, SELECT_BY_POS)==true)
      {
         tip = OrderType();
         if (tip<2 && OrderSymbol()==Symbol())
         {
            if (OrderMagicNumber()!=magic) continue;
            if (tip==0) //Buy
            {  
               if (OrderStopLoss()>=OrderOpenPrice()) return;
               StLo = Bid - NoLoss*Point;          
               if (StLo > OrderStopLoss() && StLo > OrderOpenPrice()) 
                  {error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);Comment("Trailing "+OrderTicket());Sleep(500);}
            }                                         
            if (tip==1) //Sell               
            {                                         
               if (OrderStopLoss()<=OrderOpenPrice()) return;
               StLo = Ask + NoLoss*Point;            
               if (StLo < OrderStopLoss() && StLo < OrderOpenPrice()) 
                  {error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);Comment("Trailing "+OrderTicket());Sleep(500);}
            } 
            if (error==false && SL!=0) Alert("Error SELLSTOP ",GetLastError(),"   ",Symbol(),"   SL ",StLo);
         }//tip<2
      }//OrderSelect
   }//for
}
//------------------------------------------------------------------+
string TimeStr(int taim)
{
   string sTaim;
   int HH=TimeHour(taim);     // Hour                  
   int MM=TimeMinute(taim);   // Minute   
   if (HH<10) sTaim = StringConcatenate(sTaim,"0",DoubleToStr(HH,0));
   else       sTaim = StringConcatenate(sTaim,DoubleToStr(HH,0));
   if (MM<10) sTaim = StringConcatenate(sTaim,":0",DoubleToStr(MM,0));
   else       sTaim = StringConcatenate(sTaim,":",DoubleToStr(MM,0));
   return(sTaim);
}
//--------------------------------------------------------------------