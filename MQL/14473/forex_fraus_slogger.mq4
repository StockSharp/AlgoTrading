//+------------------------------------------------------------------+
//|                                          forex_fraus_slogger.mq4 |
//+------------------------------------------------------------------+
#property copyright "Dima Z"

int SL=0;
int TP=0;

extern bool   AllPositions=True; // Управлять всеми позициями
extern bool   ProfitTrailing = True;  // Тралить только профит
extern int    TrailingStop   = 30;    // Фиксированный размер трала
extern int    TrailingStep   = 1;     // Шаг трала
extern bool   UseSound       = True;  // Использовать звуковой сигнал
extern string NameFileSound="Zvon.wav";  // Наименование звукового файла
extern double Risk_percent = 1.0;
extern double maxLots = 1.0;
extern double minLots = 0.01;
int mn=1;
int err;

extern int MAGIC=777;
extern double Lots=0.01;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
     {
      for(int i=0; i<OrdersTotal(); i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(AllPositions || OrderSymbol()==Symbol())
              {
               TrailingPositions();
              }
           }
        }
     }
   Call_MM();
   OpenPattern();//открываем сделки при мересечении

   return(0);
  }

int okbuy,oksell;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpenPattern()
  {
   double op,sl,tp;
   double WPRur=iEnvelopes(Symbol(),0,1,0,1,PRICE_CLOSE,0.1,MODE_UPPER,0);
   if(WPRur<Bid) {okbuy=1;}
   if(WPRur>Bid && okbuy==1)
     {
      okbuy=0;
      if(timecontrol()==1)
        {
         op=Bid;if(SL>0){sl=Bid+SL*Point*mn;}if(TP>0){tp=Bid-TP*Point*mn;}
         err=OrderSend(Symbol(),OP_SELL,Lots,NormalizeDouble(op,Digits),3,NormalizeDouble(sl,Digits),NormalizeDouble(tp,Digits),"4 FORTRADER.RU",MAGIC,0,Red);
         if(err<0){Print("OrderSend()-  Ошибка OP_SELL.  op "+op+" sl "+sl+" tp "+tp+" "+GetLastError());}
        }
      CloseAllPos(1);
     }
   double WPRur2=iEnvelopes(Symbol(),0,1,0,1,PRICE_CLOSE,0.1,MODE_LOWER,0);
   if(WPRur2>Bid) {oksell=1;}

   if(WPRur2<Bid && oksell==1)
     {
      oksell=0;
      if(timecontrol()==1)

        {
         op=Ask;if(SL>0){sl=Ask-SL*Point*mn;}if(TP>0){tp=Ask+TP*Point*mn;}
         err=OrderSend(Symbol(),OP_BUY,Lots,NormalizeDouble(op,Digits),3,NormalizeDouble(sl,Digits),NormalizeDouble(tp,Digits),"6 FORTRADER.RU",MAGIC,0,Blue);
         if(err<0){Print("OrderSend()-  Ошибка OP_BUY.  op "+op+" sl "+sl+" tp "+tp+" "+GetLastError());}
        }
      CloseAllPos(0);
     }
  }
//Закрываем все позиции по типу
int CloseAllPos(int type)
  {//Описание функции: http://fxnow.ru/blog.php?user=Yuriy&blogentry_id=72
   int buy=1; int sell=1;
   int i,b=0;

   if(type==1)
     {
      while(buy==1)
        {
         buy=0;
         for(i=0;i<OrdersTotal();i++)
           {
            if(true==OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
              {
               if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
                 {buy=1;if(OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet)){};}
                 }else{buy=0;
              }
           }
         if(buy==0){return(0);}
        }
     }

   if(type==0)
     {
      while(sell==1)
        {
         sell=0;
         for(i=0;i<OrdersTotal();i++)
           {
            if(true==OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
              {
               if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
                 {sell=1;if(OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet)){}; }
                 }else{sell=0;
              }
           }

         if(sell==0){return(0);}
        }
     }
   return(0);
  }
//проверяет есть ли открытые ордера
int ChPos(int type)
  {//подробное описание: http://fxnow.ru/blog.php?user=Yuriy&blogentry_id=100

   int i;int col;
   for(i=1; i<=OrdersTotal(); i++)
     {
      if(OrderSelect(i-1,SELECT_BY_POS)==true)
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && type==1 && OrderMagicNumber()==MAGIC){col=1;}
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && type==0 && OrderMagicNumber()==MAGIC){col=1;}
        }
     }
   return(col);
  }
//суммирует результат позиций по типу
int SummPos(int type)
  {//подробное описание: http://fxnow.ru/blog.php?user=Yuriy&blogentry_id=100

   int i;double summ;
   for(i=1; i<=OrdersTotal(); i++)
     {
      if(OrderSelect(i-1,SELECT_BY_POS)==true)
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && type==1 && OrderMagicNumber()==MAGIC){summ=summ+OrderProfit();}
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && type==0 && OrderMagicNumber()==MAGIC){summ=summ+OrderProfit();}
        }
     }
   return(summ);
  }

extern int time=0; //1 - включено, 0 - выключено.
extern int starttime= 7;
extern int stoptime = 17;
//Ограничение по времени
int timecontrol()
  {// Подробное описание http://fxnow.ru/blog.php?user=Yuriy&blogentry_id=1
   if(((Hour()>=0 && Hour()<=stoptime-1) || (Hour()>=starttime && Hour()<=23)) && starttime>stoptime)
     {
      return(1);
     }
   if((Hour()>=starttime && Hour()<=stoptime-1) && starttime<stoptime)
     {
      return(1);
     }

   if(time==0){ return(1);}

   return(0);
     }void TrailingPositions() {
   double pBid,pAsk,pp;

   pp=MarketInfo(OrderSymbol(),MODE_POINT);
   if(OrderType()==OP_BUY)
     {
      pBid=MarketInfo(OrderSymbol(),MODE_BID);
      if(!ProfitTrailing || (pBid-OrderOpenPrice())>TrailingStop*pp)
        {
         if(OrderStopLoss()<pBid-(TrailingStop+TrailingStep-1)*pp)
           {
            ModifyStopLoss(pBid-TrailingStop*pp);
            return;
           }
        }
     }
   if(OrderType()==OP_SELL)
     {
      pAsk=MarketInfo(OrderSymbol(),MODE_ASK);
      if(!ProfitTrailing || OrderOpenPrice()-pAsk>TrailingStop*pp)
        {
         if(OrderStopLoss()>pAsk+(TrailingStop+TrailingStep-1)*pp || OrderStopLoss()==0)
           {
            ModifyStopLoss(pAsk+TrailingStop*pp);
            return;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Перенос уровня StopLoss                                          |
//| Параметры:                                                       |
//|   ldStopLoss - уровень StopLoss                                  |
//+------------------------------------------------------------------+
void ModifyStopLoss(double ldStopLoss)
  {
   bool fm;

   fm=OrderModify(OrderTicket(),OrderOpenPrice(),ldStopLoss,OrderTakeProfit(),0,CLR_NONE);
   if(fm && UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Call_MM()
  {
   Lots=AccountFreeMargin()/100000*Risk_percent;

   Lots=MathMin(maxLots,MathMax(minLots,Lots));
   if(minLots<0.1)
      Lots=NormalizeDouble(Lots,2);
   else
     {
      if(minLots<1) Lots=NormalizeDouble(Lots,1);
      else          Lots=NormalizeDouble(Lots,0);
     }
  }
//+------------------------------------------------------------------+
