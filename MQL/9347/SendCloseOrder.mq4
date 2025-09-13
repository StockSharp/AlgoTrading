//+------------------------------------------------------------------+
//|                                               SendCloseOrder.mq4 |
//|                               Copyright © 2009, Vladimir Hlystov |
//|  v 1.00 ”станавливает или закрывает ордера при пересечении линий |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Vladimir Hlystov"
#property link      "cmillion@narod.ru"
//-------------------------------------------------------------------
extern bool   DRAW_SELL    = true;  //рисовать отрезки Sell
extern bool   DRAW_BUY     = true;  //рисовать отрезки BUY
extern bool   DRAW_CLOSE1  = true;  //рисовать отрезки Close1
extern bool   DRAW_CLOSE2  = true;  //рисовать отрезки Close2
extern int    MAX_ORDER    = 1;     //максимальное колличество ордеров
extern double lot          = 0.10;  //лот
extern color  resi         = Brown;
extern color  supp         = MediumBlue;
extern color  clos         = DarkViolet;
//-------------------------------------------------------------------
int init()
{
   return(0);
}
//-------------------------------------------------------------------
int deinit()
{
   ObjectDelete("LINES SELL");
   ObjectDelete("LINES BUY");
   ObjectDelete("LINES CLOSE1");
   ObjectDelete("LINES CLOSE2");
   ObjectDelete("LINES SELL n");
   ObjectDelete("LINES BUY n");
   ObjectDelete("LINES CLOSE1 n");
   ObjectDelete("LINES CLOSE2 n");
   return(0);
}
//-------------------------------------------------------------------
int start()
{
   int bar1,bar2,bar3;
   if (DRAW_SELL && ObjectFind("LINES SELL")==-1)
   {
      bar3 = searcFR(0,1);bar2 = searcFR(bar3,-1);bar1 = searcFR(bar2,1);
      drawline("LINES SELL",resi,Time[bar1],High[bar1],Time[bar3],High[bar3]);
   }
   if (DRAW_CLOSE1 && ObjectFind("LINES CLOSE1")==-1)
   {
      bar3 = searcFR(0,1);bar2 = searcFR(bar3,-1);bar1 = searcFR(bar2,1);
      drawline("LINES CLOSE1",clos,Time[bar1],High[bar1]+15*Point,Time[bar3],High[bar3]+15*Point);
   }
   if (DRAW_BUY && ObjectFind("LINES BUY")==-1)
   {
      bar3 = searcFR(0,-1);bar2 = searcFR(bar3,1);bar1 = searcFR(bar2,-1);
      drawline("LINES BUY",supp,Time[bar1],Low[bar1],Time[bar3],Low[bar3]);
    }
   if (DRAW_CLOSE2 && ObjectFind("LINES CLOSE2")==-1)
   {
      bar3 = searcFR(0,-1);bar2 = searcFR(bar3,1);bar1 = searcFR(bar2,-1);
      drawline("LINES CLOSE2",clos,Time[bar1],Low[bar1]-15*Point,Time[bar3],Low[bar3]-15*Point);
    }
   string order = checkapp();
   if (order=="LINES CLOSE1"||order=="LINES CLOSE2") CLOSEORDER();
   if (OrdersTotal()<MAX_ORDER)
   {
      if (order=="LINES SELL") OrderSend(Symbol(),OP_SELL,lot,Bid,2,0,0,"LINES SELL",0,0);
      if (order=="LINES BUY" ) OrderSend(Symbol(),OP_BUY, lot,Ask,2,0,0,"LINES BUY ",0,0);
   }
   Comment(order);
}
//-------------------------------------------------------------------------
void CLOSEORDER()
{
   for (int i=0; i<OrdersTotal(); i++)
   {                                               
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
      {
         if (OrderType()==OP_BUY)
            OrderClose(OrderTicket(),OrderLots(),Bid,3,CLR_NONE);// «акрываем BUY
         if (OrderType()==OP_SELL)
            OrderClose(OrderTicket(),OrderLots(),Ask,3,CLR_NONE);// «акрываем Sell
      }   
   }
}
//-------------------------------------------------------------------------
int searcFR(int bar, int UP_DN)
{  while(true)//ищем 1 фрактал после bar
   {  bar++;
      if (Fractal(bar) == UP_DN) return(bar);} 
   return(0);  
}
//--------------------------------------------------------------------------
int Fractal(int br)
{  if (br <= 2) return(0);
   if (High[br] >= High[br+1] && High[br] > High[br+2] && High[br] >= High[br-1] && High[br] > High[br-2]) return( 1);
   if (Low [br] <= Low [br+1] && Low [br] < Low [br+2] && Low [br] <= Low [br-1] && Low [br] < Low [br-2]) return(-1);
   return(0);
}
//-------------------------------------------------------------------
int drawline(string Name, color col,int X1,double Y1,int X2,double Y2)
{
   if (ObjectFind(Name)==0)return; //≈сли обьект существует
   int X1g=Time[0];
   int X2g=Time[0]+Period()*1200;
   double Y1g=Y1+(Y2-Y1)*(X1g-X1)/(X2-X1);
   double Y2g=Y1+(Y2-Y1)*(X2g-X1)/(X2-X1);
   ObjectCreate(Name, OBJ_TREND, 0,X1g,Y1g,X2g,Y2g);
   ObjectSet   (Name, OBJPROP_COLOR, col);
   ObjectSet   (Name, OBJPROP_STYLE, STYLE_SOLID);
   ObjectSet   (Name, OBJPROP_WIDTH, 2);
   ObjectSet   (Name, OBJPROP_BACK,  false);
   ObjectSet   (Name, OBJPROP_RAY,   false);
   return;
}
//----------------------------------------------------------------------- проверка всех линий "граница"
string checkapp()
{
   int X_1,X_2,X_3;
   double Y_1,Y_2,Y_3;
   double shift_Y = (WindowPriceMax()-WindowPriceMin()) / 50;
   color col;
   for(int n=ObjectsTotal()-1; n>=0; n--) 
   {
      string Obj_N=ObjectName(n);
      if (ObjectType(Obj_N)!=OBJ_TREND) continue;
      if (StringFind(Obj_N,"LINES ",0)!=-1)//найден обьект-тренд к которому вычисл€етс€ приближение
      {
         X_1 = ObjectGet(Obj_N, OBJPROP_TIME1); 
         X_2 = ObjectGet(Obj_N, OBJPROP_TIME2); 
         ObjectDelete (Obj_N+" n");
         if (X_1>X_2 ||  X_2<Time[0]) {continue;}//ObjectDelete(Obj_N);
         Y_1 = ObjectGet(Obj_N, OBJPROP_PRICE1);
         Y_2 = ObjectGet(Obj_N, OBJPROP_PRICE2);
         col= ObjectGet(Obj_N, OBJPROP_COLOR);
         ObjectCreate (Obj_N+" n", OBJ_TEXT,0,X_1-Period()*60,Y_1+shift_Y,0,0,0,0);
         ObjectSetText(Obj_N+" n",StringSubstr(Obj_N,6,5) ,7,"Arial");
         ObjectSet    (Obj_N+" n", OBJPROP_COLOR, col);
         if (X_1<=Time[0] && X_2>=Time[0])//попадает во временной диапазон
         {
            X_3=Time[0];Y_3=Y_1+(Y_2-Y_1)*(X_3-X_1)/(X_2-X_1);//уравнение пр€мой
            if (Y_3>=Bid&&Y_3<=Ask)
            { 
               return(Obj_N);
            }
         }
      }
   }
}
//------------------------------------------------------------------------------------------- 