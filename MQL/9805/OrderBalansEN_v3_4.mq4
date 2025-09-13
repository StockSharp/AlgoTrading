//+------------------------------------------------------------------+
//|                                                 OrderBalans_v3.4 |
//|                               Copyright © 2010, Vladimir Hlystov |
//|                                         http://cmillion.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, cmillion@narod.ru"
#property link      "cmillion@narod.ru"
#property indicator_chart_window
extern color   WhiteColor  = Silver;
extern int     CORNER      = 1;
extern color   NameColor   = Green;
extern color   BuyColor    = Blue;
extern color   SellColor   = Red;
//--------------------------------------------------------------------
double High_Win,Low_Win,balans,MaxLot,AFreeMargin,LINE [25][5],orders[25][10];
int    ORDER,UG;
string DayTek,DayWe;
//--------------------------------------------------------------------
int deinit()
{
   ObjectDelete("_TIME_");
   ObjectDelete("_SHAPKA_");
   ObjectDelete("_BALANS_");
   ObjectDelete("_MARGA_");
   del("order Bay");
   del("order Sell");
   del("order");
}
//--------------------------------------------------------------------
int del(string str)
{
   for(int n=ObjectsTotal()-1; n>=0; n--) 
     {
      string Obj_Name=ObjectName(n);
      if (StringFind(Obj_Name,str,0) != -1)
      {
         ObjectDelete(Obj_Name);
      }
   }
   return;
}
//--------------------------------------------------------------------
int init()
{
if (CORNER>3) return;
   ObjectCreate("_TIME_", OBJ_LABEL, 0, 0, 0);
      ObjectSet("_TIME_", OBJPROP_CORNER, CORNER);
      ObjectSet("_TIME_", OBJPROP_XDISTANCE, 10);
   ObjectCreate("_SHAPKA_", OBJ_LABEL, 0, 0, 0);
      ObjectSet("_SHAPKA_", OBJPROP_CORNER, CORNER);      
      ObjectSet("_SHAPKA_", OBJPROP_XDISTANCE, 10);      
   ObjectCreate("_BALANS_", OBJ_LABEL, 0, 0, 0);
      ObjectSet("_BALANS_", OBJPROP_CORNER, CORNER);      
      ObjectSet("_BALANS_", OBJPROP_XDISTANCE, 10 ); 
   ObjectCreate("_MARGA_", OBJ_LABEL, 0, 0, 0);
      ObjectSet("_MARGA_", OBJPROP_CORNER, CORNER);
      ObjectSet("_MARGA_", OBJPROP_XDISTANCE, 10 );
if (CORNER<2)
{  UG=+10;
      ObjectSet("_TIME_", OBJPROP_YDISTANCE, 13);
      ObjectSet("_BALANS_", OBJPROP_YDISTANCE, 25);
      ObjectSet("_MARGA_", OBJPROP_YDISTANCE, 35);
      ObjectSet("_SHAPKA_", OBJPROP_YDISTANCE, 60);
}
else
{  UG=-10;
      ObjectSet("_TIME_", OBJPROP_YDISTANCE, 5);
      ObjectSet("_BALANS_", OBJPROP_YDISTANCE, 15);
      ObjectSet("_MARGA_", OBJPROP_YDISTANCE, 25);
      ObjectSet("_SHAPKA_", OBJPROP_YDISTANCE, 100);
}
return;
}   
//-------------------------------------------------------------------------------
int start()                                  
{
   DayTek = TimeToStr(TimeCurrent(),TIME_DATE);
   DayWe = DayWeek(TimeDayOfWeek(TimeCurrent()));
   if (CORNER<4) ObjectSetText("_TIME_",StringConcatenate(DayWe," ",TimeToStr(TimeCurrent(),TIME_SECONDS),"  v3"),8,"Arial",WhiteColor);
   AFreeMargin = AccountFreeMargin();
   MaxLot = AFreeMargin/MarketInfo(Symbol(),MODE_MARGINREQUIRED);
   Terminal();
   ORDER = orders[0][0];
   if (ORDER > 0) 
   {
      string   NameLine;
      for (int n=1; n<=orders[0][0]; n++)
      {
         if (orders[n][6]==1) NameLine="order Bay  "+DoubleToStr(orders[n][4],0);
         if (orders[n][6]==-1) NameLine="order Sell "+DoubleToStr(orders[n][4],0);
         ObjectDelete(NameLine);
         ObjectDelete(NameLine+" -");
         if (orders[n][6]== 1) {ObjectCreate(NameLine,OBJ_TREND,0,orders[n][9],orders[n][1],Time[0],Bid);
         ObjectSet(NameLine, OBJPROP_COLOR,BuyColor); 
         ObjectCreate(NameLine+" -",OBJ_ARROW,0,Time[0],Bid,0,0,0,0);}      
         if (orders[n][6]==-1) {ObjectCreate(NameLine,OBJ_TREND,0,orders[n][9],orders[n][1],Time[0],Ask);
         ObjectSet(NameLine, OBJPROP_COLOR,SellColor);
         ObjectCreate(NameLine+" -",OBJ_ARROW,0,Time[0],Ask,0,0,0,0);}
         ObjectSet(NameLine, OBJPROP_STYLE, STYLE_DOT);
         ObjectSet(NameLine, OBJPROP_RAY,   false);
         ObjectSet(NameLine+" -",OBJPROP_ARROWCODE,3);ObjectSet(NameLine+" -",OBJPROP_COLOR,WhiteColor);   
      }
   }
   if (CORNER<4) {
   string txt = StringConcatenate("  BALANCE = ",DoubleToStr(AccountBalance( ),2)," | free ",DoubleToStr(AFreeMargin,2)," | ",DoubleToStr(MaxLot,2));
   ObjectSetText("_BALANS_",txt,8,"Arial",WhiteColor);
   if (balans <0) ObjectSetText("_MARGA_",StringConcatenate("  balans = ",DoubleToStr(balans,2)),8,"Arial",Red);
   else           ObjectSetText("_MARGA_",StringConcatenate("  balans = ",DoubleToStr(balans,2)),8,"Arial",Blue);
   if (ORDER== 0) ObjectSetText("_MARGA_","                ",8,"Arial",0);}
return;
}
//-------------------------------------------------------------------------------
string DayWeek(int day)
{
   switch(day)
   {  case 1: return ("Monday");
      case 2: return ("Tuesday");
      case 3: return ("Wednesday");
      case 4: return ("Thursday");
      case 5: return ("Friday");
      case 6: return ("Saturday");
      case 7: return ("Sunday");
      default: return("");
   }
}
//-------------------------------------------------------------------------------
int Terminal()
{
   int Kol=0,n=0,X_order,Ticket,tip;
   double Y_order;
   string N_order,текст,txt;
          
   ArrayInitialize(orders,0);
   if (ObjectFind("_SHAPKA_")==0)
   {  X_order = ObjectGet("_SHAPKA_", OBJPROP_XDISTANCE);
      Y_order = ObjectGet("_SHAPKA_", OBJPROP_YDISTANCE);
   }
   else {X_order = 10;Y_order=60+10*Kol;}
   for (int i=1; i<=25; i++)
   {  N_order="order "+i;
      ObjectDelete(N_order);
   }
   balans=0;
   for ( i=0; i<OrdersTotal(); i++)
   {  if (OrderSelect(i, SELECT_BY_POS)==true)
      {  if (OrderSymbol()==Symbol())
         {  tip=OrderType();
            if (tip == OP_BUY  || tip == OP_SELL)
            {  Kol++;
               orders[Kol][1] = OrderOpenPrice();
               orders[Kol][2] = OrderStopLoss();
               orders[Kol][3] = OrderTakeProfit();
               orders[Kol][4] = OrderTicket();
               orders[Kol][5] = OrderLots();
               if (tip == OP_BUY) {orders[Kol][6] = 1; текст = "Buy "; }
               else               {orders[Kol][6] =-1; текст = "Sell ";}
               orders[Kol][7] = OrderMagicNumber();
               orders[Kol][9] = OrderOpenTime();
               if (CORNER<4){N_order="order "+Kol;
                  if (orders[Kol][6]==1) orders[Kol][8] = NormalizeDouble((Bid-orders[Kol][1])/Point,0);
                  else                   orders[Kol][8] = NormalizeDouble((orders[Kol][1]-Ask)/Point,0);
                  balans=balans+OrderProfit();
                  ObjectDelete(N_order);
                  ObjectCreate(N_order, OBJ_LABEL, 0, 0, 0);
                  ObjectSet   (N_order, OBJPROP_CORNER, CORNER);
                  ObjectSet   (N_order, OBJPROP_XDISTANCE, X_order);
                  ObjectSet   (N_order, OBJPROP_YDISTANCE, Y_order+UG*Kol);
                  if (orders[Kol][2]!=0) txt = StringConcatenate("  ",DoubleToStr((Bid-orders[Kol][2])/Point,0)); 
                  else txt = "  __";
                  if (orders[Kol][3]!=0) txt = StringConcatenate(txt,"  " ,DoubleToStr((Bid-orders[Kol][3])/Point,0));  
                  else txt = StringConcatenate(txt,"  __   ");
                  ObjectSetText(N_order,StringConcatenate(
                     "  " ,DoubleToStr(orders[Kol][8],0),
                     "  " ,            текст,
                     "  " ,            orders[Kol][4],
                     "   ",DoubleToStr(orders[Kol][1],Digits),
                     "  " ,DoubleToStr(orders[Kol][5] ,2),
                     txt  ,TimeToStr  (TimeCurrent()-orders[Kol][9],TIME_MINUTES)),8,"Arial",WhiteColor);
               }
            }
         }
      }
   }
   orders[0][0] = Kol;
   if (CORNER<4){
   if (orders[0][0]!=0)  ObjectSetText("_SHAPKA_","p  type      order         price    lot   dSL dТР  time",8,"Arial",NameColor);
   else ObjectSetText("_SHAPKA_"," ",8,"Arial",Aquamarine);
   ObjectSet("_SHAPKA_", OBJPROP_CORNER, CORNER);}
return;
}
//-------------------------------------------------------------------------------

