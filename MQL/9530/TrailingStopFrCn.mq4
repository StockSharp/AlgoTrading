//+------------------------------------------------------------------+
//|                                                 TrailingStop.mq4 |
//|                               Copyright © 2010, Хлыстов Владимир |
//|                                         http://cmillion.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, cmillion@narod.ru"
#property link      "http://cmillion.narod.ru"
//--------------------------------------------------------------------
/*Описание:
Может запускаться как отдельный советник или как скрипт, совместно с любым советником.  
В зависимости от переменной TrailingStop, трейлинг может осуществляться по фракталам, 
по экстремумам прошлых баров или по указанному кол-ву пунктов. 
Если TrailingStop больше 0, то трейлинг будет осуществлен с тем кол-вом пунктов, 
которое указано в переменной TrailingStop. Если TrailingStop меньше ограничения 
минимального уровня стопов, то трейлинг будет с минимальными стопами.
Если TrailingStop = 0 и Tip.Fr.or.Candl=0, то трейлинг будет по фракталам. 
Т.е. для уровня стоплосс выбирается первый соответствующий фрактал.
Если TrailingStop = 0 и Tip.Fr.or.Candl=1, то трейлинг будет по минимумам/максимумам 
прошлых свечей.
Если  Magic = 0, то трейлинг проводится по всем ордерам текущего символа, без учета 
магического номера. Если в переменной Magic указан магический номер, то соответственно 
будет только  трейлинг ордеров с номером Magic.
Если OnlyProfit = true, то модифицируются только профитные ордера
Если OnlyWithoutLoss = true, то вместо трейлинга ордера только переводятся в безубыток
Визуализация:
На экране отображается информация о текущей работе скрипта:
- установки с которыми скрипт запущен
- количество ордеров с которыми скрипт работает
- текущее время
- минимальные уровни выставления стопов (-)
- текущие возможные уровни стопов (ценовые метки)
Советы:
Скрипт заканчивает работу, когда все ордера закрыты. Если программа запущена как советник, 
то выход из программы только вручную.
*/
                                     
//--------------------------------------------------------------------
extern bool OnlyProfit       = true; //только профитные ордера
extern bool OnlyWithoutLoss  = false;//только без убыток
extern int  Magic            = 0;
extern int  TrailingStop     = 0;    //если= 0, то трейлинг по фракталам или свечам
extern int  Tip.Fr.or.Candl  = 1;    //если= 0, то трейлинг по фракталам 
                                     //если= 1, то трейлинг по свечам
//--------------------------------------------------------------------
int  delta, n,DIGITS;
datetime TIME;
double BID,ASK,POINT;
//--------------------------------------------------------------------
int start()                                  
{
   ObjectCreate("info", OBJ_LABEL, 0, 0, 0);
   ObjectSet("info", OBJPROP_CORNER, 1);      
   ObjectSet("info", OBJPROP_XDISTANCE, 200 ); 
   ObjectSet("info", OBJPROP_YDISTANCE, 0);
   string txt;
   POINT=MarketInfo(Symbol(),MODE_POINT);
   DIGITS=MarketInfo(Symbol(),MODE_DIGITS);
   if (TrailingStop!=0 && TrailingStop<MarketInfo(Symbol(),MODE_STOPLEVEL)) 
      TrailingStop=MarketInfo(Symbol(),MODE_STOPLEVEL);
   if (Magic==0) txt=StringConcatenate("total orders ",Symbol()); 
   else  txt=StringConcatenate("orders ",Symbol()," Magic ",Magic);
   if (TrailingStop!=0) txt=StringConcatenate("  ",TrailingStop," п "); 
   else  if (Tip.Fr.or.Candl==0) txt=" Fractals "; else txt=" по свечам ";
   Comment("Start TrailingStop ",txt,TimeToStr(TimeCurrent(),TIME_MINUTES));
   while(true)
   {
      RefreshRates();
      BID = MarketInfo(Symbol(),MODE_BID);
      ASK = MarketInfo(Symbol(),MODE_ASK);
      TIME = iTime(Symbol(),0,0);
      delta = MarketInfo(Symbol(),MODE_STOPLEVEL);
      if (delta<TrailingStop)delta=TrailingStop;
      ObjectSetText("info",StringConcatenate("TrailingStop ",txt," Orders ", n,"  ",
      TimeToStr(TimeCurrent(),TIME_SECONDS)),8,"Arial",Aqua);
      TrailingStop();
      if (n==0) break;
      Sleep(1000);
   }
   Comment("Нет открытых ордеров. Закрытие скрипта ",
   TimeToStr(TimeCurrent(),TIME_MINUTES));
   ObjectDelete("SL Buy");
   ObjectDelete("SL Sell");
   ObjectDelete("info");
   ObjectDelete("SL-");
   ObjectDelete("SL+");
}
//--------------------------------------------------------------------
void TrailingStop()
{
   int tip,Ticket;
   bool error;
   double StLo,OSL,OOP;
   n=0;
   for (int i=0; i<OrdersTotal(); i++) 
   {  if (OrderSelect(i, SELECT_BY_POS)==true)
      {  tip = OrderType();
         if (tip<2 && OrderSymbol()==Symbol() && (OrderMagicNumber()==Magic || Magic==0))
         {
            OSL   = OrderStopLoss();
            OOP   = OrderOpenPrice();
            Ticket = OrderTicket();
            if (tip==OP_BUY)             
            {  n++;
               StLo = SlLastBar(1,BID,Tip.Fr.or.Candl,TrailingStop);        
               if (StLo <= OOP && OnlyProfit) continue;
               if (OSL  >= OOP && OnlyWithoutLoss) continue;
               if (StLo > OSL)
               {  error=OrderModify(Ticket,OOP,NormalizeDouble(StLo,DIGITS),
                  OrderTakeProfit(),0,White);
                  Comment("TrailingStop ",Ticket," ",TimeToStr(TimeCurrent(),TIME_MINUTES));
                  Sleep(500);
                  if (!error) Comment("Error order ",Ticket," TrailingStop ",
                              GetLastError(),"   ",Symbol(),"   SL ",StLo);
               }
            }                                         
            if (tip==OP_SELL)        
            {  n++;
               StLo = SlLastBar(-1,ASK,Tip.Fr.or.Candl,TrailingStop);  
               if (StLo==0) continue;        
               if (StLo >= OOP && OnlyProfit) continue;
               if (OSL  >= OOP && OnlyWithoutLoss) continue;
               if (StLo < OSL || OSL==0 )
               {  error=OrderModify(Ticket,OOP,NormalizeDouble(StLo,DIGITS),
                  OrderTakeProfit(),0,White);
                  Comment("TrailingStop "+Ticket," ",TimeToStr(TimeCurrent(),TIME_MINUTES));
                  Sleep(500);
                  if (!error) Comment("Error order ",Ticket," TrailingStop ",
                              GetLastError(),"   ",Symbol(),"   SL ",StLo);
               }
            } 
         }
      }
   }
}
//--------------------------------------------------------------------
double SlLastBar(int tip,double price, int tipFr, int tral)
{
   double fr;
   int jj,ii;
   if (tral!=0)
   {
      if (tip==1) fr = BID - tral*POINT;  
      else fr = ASK + tral*POINT;  
   }
   else
   {
      if (tipFr==0)
      {
         if (tip== 1)
         for (ii=1; ii<100; ii++) 
         {
            fr = iFractals(NULL,0,MODE_LOWER,ii);
            if (fr!=0) if (price-delta*POINT > fr) break;
            else fr=0;
         }
         if (tip==-1)
         for (jj=1; jj<100; jj++) 
         {
            fr = iFractals(NULL,0,MODE_UPPER,jj);
            if (fr!=0) if (price+delta*POINT < fr) break;
            else fr=0;
         }
      }
      else
      {
         if (tip== 1)
         for (ii=1; ii<100; ii++) 
         {
            fr = iLow(NULL,0,ii);
            if (fr!=0) if (price-delta*POINT > fr) break;
            else fr=0;
         }
         if (tip==-1)
         for (jj=1; jj<100; jj++) 
         {
            fr = iHigh(NULL,0,jj);
            if (price+delta*POINT < fr) break;
            else fr=0;
         }
      }
   }
   if (tip== 1)
   {
      ObjectDelete("SL Buy");
      ObjectDelete("SL-");
      ObjectCreate("SL Buy",OBJ_ARROW,0,TIME,fr,0,0,0,0);                     
      ObjectSet   ("SL Buy",OBJPROP_ARROWCODE,6);
      ObjectSet   ("SL Buy",OBJPROP_COLOR, Blue);
      ObjectCreate("SL-",OBJ_ARROW,0,TIME,price-delta*POINT,0,0,0,0);                     
      ObjectSet   ("SL-",OBJPROP_ARROWCODE,4);
      ObjectSet   ("SL-",OBJPROP_COLOR, Blue);
   }
   if (tip==-1)
   {
      ObjectDelete("SL Sell");
      ObjectDelete("SL+");
      ObjectCreate("SL Sell",OBJ_ARROW,0,TIME,fr,0,0,0,0);
      ObjectSet   ("SL Sell",OBJPROP_ARROWCODE,6);
      ObjectSet   ("SL Sell", OBJPROP_COLOR, Green);
      ObjectCreate("SL+",OBJ_ARROW,0,TIME,price+delta*POINT,0,0,0,0);                     
      ObjectSet   ("SL+",OBJPROP_ARROWCODE,4);
      ObjectSet   ("SL+",OBJPROP_COLOR, Green);
   }
   return(fr);
}
//--------------------------------------------------------------------

