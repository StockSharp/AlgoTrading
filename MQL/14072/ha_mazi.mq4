//+------------------------------------------------------------------+
//|                                                    Ha MaZi v1.00 |
//|                               Copyright 2015, Nikolay Khrushchev |
//|                                             http://www.MqlLab.ru |
//+------------------------------------------------------------------+
#property copyright "MqlLab.ru"
#property link      "http://www.mqllab.com"
#property strict 

extern double              lot                  = 1.0;
extern double              stop_loss            = 70;
extern double              take_profit          = 200;
extern double              noloss               = 30;
extern double              noloss_pips          = 1;
extern double              trall                = 30;
extern double              trall_start          = 40;
extern bool                ma_filter            = true;
extern bool                cycle_filter         = true;
extern bool                close_ma_cross       = false;
extern string              friday_time          = "24:00";
extern double              friday_close_profit  = 50;
extern string              menucomment01        = "========= Параметры индикатора =========";
extern int                 zz_InpDepth          = 13;
extern int                 zz_InpDeviation      = 11;
extern int                 zz_InpBackstep       = 11;
extern int                 ma_period            = 40;
extern ENUM_MA_METHOD      ma_method            = MODE_EMA;
extern ENUM_APPLIED_PRICE  ma_price             = PRICE_CLOSE;
extern string              menucomment02        = "========= Прочие параметры =========";
extern int                 magic                = 9238;
int                        TryToTrade           = 20;
int                        WaitTime             = 1500;
extern int                 CommentSize          = 7;

int      i,r,z,// переменные для пересчетов for
         dg, dig,                               // округление лотов и цены
         last_ticket;                           // последние тикет для пересчета ордеров for
bool     Work=true,Test=false,// флаг разрашения работы и фраг работы в тестере
         long_allowed=true,short_allowed=true; // разрешение покупать и продавать
double   Pp,Mnoj,// Point округленный до 2/4 знака и обратный множитель
         Pp2, Mnoj2,                            // стандартный Point и обратный множитель
         min_lot, max_lot;                      // минимальный и максимальный лот
string   Symb,// валюта инструмента
         CommentBox[],cm2;                      // комментарий на графике
datetime time_bar;                              // время открытие нулевой свечи
int      time_min,time_hour;
bool     ta=true;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void start()
  {
   if(!Work) return;
   if(!IsTradeAllowed()) return;
// получение сигнала
   if(Hour()!=time_hour || Minute()!=time_min)
     {
      time_hour= Hour();
      time_min = Minute();
      ta=true;
      if(DayOfWeek()==5)
        {
         datetime friday_time_end=StringToTime(TimeToStr(TimeCurrent(),TIME_DATE)+" "+friday_time);
         if(TimeCurrent()>=friday_time_end) ta=false;
        }
     }

   int signal=-1;
   int close=-1;
   double ma=0;
   if(time_bar!=Time[0])
     {
      time_bar=Time[0];
      // расчет МА
      ma=iMA(Symb,0,ma_period,0,ma_method,ma_price,1);
      if(Close[1]<ma) close=0;
      if(Close[1]>ma) close=1;

      // поиск последних 4х точек зиг зага
      int    zz_points=0;
      double zz_point_price[4]={0,0,0,0};
      int    zz_point_bar[4]={0,0,0,0};
      for(i=0;i<500;i++)
        {
         double zz=iCustom(Symb,0,"ZigZag",zz_InpDepth,zz_InpDeviation,zz_InpBackstep,0,i);
         if(zz>0)
           {
            zz_point_price[zz_points]=zz;
            zz_point_bar[zz_points]=i;
            zz_points++;
            if(zz_points>=4) break;
           }
        }
      // анализ тройки точек, с начала 0-1-2, затем 1-2-3
      for(i=0;i<2;i++)
        {
         double   zz_price    = 0;
         double   line_price  = 0;

         int      high_bar    = 0;
         double   high_price  = 0;
         double   high_shift  = 0;

         int      low_bar     = 0;
         double   low_price   = 0;
         double   low_shift   = 0;

         double   price_move  = 0;
         double   price_move2 = 0;
         double   cross_price = 0;

         bool     cycle_in=false;

         if(zz_point_price[2+i]<zz_point_price[1+i])
           { // ожидание сигнала на покупку
            // анализ максимумов 
            price_move=(zz_point_price[1+i]-zz_point_price[0+i])/(zz_point_bar[1+i]-zz_point_bar[0+i]);
            for(r=zz_point_bar[1+i]-1;r>=zz_point_bar[0+i];r--)
              {
               zz_price=zz_point_price[0+i]+price_move*(r-zz_point_bar[0+i]);
               if(high_shift>0)
                 {
                  line_price=NormalizeDouble(zz_point_price[1+i]-price_move2*(zz_point_bar[1+i]-r),dig);
                 }
               if(High[r]-zz_price>high_shift && (High[r]>line_price || high_shift==0))
                 {
                  high_shift=High[r] - zz_price;
                  high_price=High[r];
                  high_bar=r;
                  price_move2=(zz_point_price[1+i]-high_price)/(zz_point_bar[1+i]-high_bar);
                 }
              }

            // расчет точки для пересечения
            price_move=(zz_point_price[1+i]-high_price)/(zz_point_bar[1+i]-high_bar);
            cross_price=NormalizeDouble(zz_point_price[1+i]-price_move*(zz_point_bar[1+i]-1),dig);
            // проверка пересечения
            if(Close[1]>cross_price && Close[2]<=cross_price && (Close[1]>ma || !ma_filter))
              {
               cycle_in=false;
               if(zz_point_bar[1+i]<=zz_point_bar[2+i]/2 && zz_point_bar[0+i]>=zz_point_bar[1+i]-(zz_point_bar[2+i]-zz_point_bar[1+i])/2) cycle_in=true;

               signal=0;
               for(r=2;r<zz_point_bar[0+i];r++) if(Close[r]>cross_price) { signal=-1; break; }

               if(signal==0 && cycle_filter && !cycle_in) signal=-1;

              }
           }

         if(zz_point_price[2+i]>zz_point_price[1+i])
           { // ожидание сигнала на продажу
            // анализ минимумов    
            price_move=(zz_point_price[0+i]-zz_point_price[1+i])/(zz_point_bar[1+i]-zz_point_bar[0+i]);
            for(r=zz_point_bar[1+i]-1;r>=zz_point_bar[0+i];r--)
              {
               zz_price=zz_point_price[0+i]-price_move*(r-zz_point_bar[0+i]);
               if(low_shift>0)
                 {
                  line_price=NormalizeDouble(zz_point_price[1+i]+price_move2*(zz_point_bar[1+i]-r),dig);
                 }
               if(zz_price-Low[r]>low_shift && (Low[r]<line_price || low_shift==0))
                 {
                  low_shift=zz_price - Low[r];
                  low_price=Low[r];
                  low_bar=r;
                  price_move2=(low_price-zz_point_price[1+i])/(zz_point_bar[1+i]-low_bar);
                 }
              }
            // расчет точки для пересечения
            price_move=(low_price-zz_point_price[1+i])/(zz_point_bar[1+i]-high_bar);
            cross_price=NormalizeDouble(zz_point_price[1+i]+price_move*(zz_point_bar[1+i]-1),dig);
            //ObjectSetDouble(0,"123",OBJPROP_PRICE,0,cross_price);
            //ObjectSetInteger(0,"321",OBJPROP_TIME,0,Time[low_bar]);
            // проверка пересечения
            if(Close[1]<cross_price && Close[2]>=cross_price && (Close[1]<ma || !ma_filter))
              {
               cycle_in=false;
               if(zz_point_bar[1+i]<=zz_point_bar[2+i]/2 && zz_point_bar[0+i]>=zz_point_bar[1+i]-(zz_point_bar[2+i]-zz_point_bar[1+i])/2) cycle_in=true;

               signal=1;
               for(r=2;r<zz_point_bar[0+i];r++) if(Close[r]<cross_price) { signal=-1; break; }

               if(signal==1 && cycle_filter && !cycle_in) signal=-1;

              }
           }
        }
     }
// учет ордеров
   last_ticket=-1;
   for(i=OrdersTotal()-1;i>=0;i--) if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) && OrderSymbol()==Symb && OrderMagicNumber()==magic)
     {
      if(last_ticket==OrderTicket()) continue;
      last_ticket=OrderTicket();
      if(close_ma_cross && close==OrderType()) { CloseOrder(OrderTicket()); continue; }
      if(!ta)
        {
         if(OrderType()==0 && Bid-OrderOpenPrice()<=friday_close_profit*Pp) { CloseOrder(OrderTicket()); continue; }
         if(OrderType()==1 && OrderOpenPrice()-Ask<=friday_close_profit*Pp) { CloseOrder(OrderTicket()); continue; }
        }
      if(noloss>0)
        {
         if(OrderType()==0 && NormalizeDouble(OrderOpenPrice(),Digits)>NormalizeDouble(OrderStopLoss(),Digits) && NormalizeDouble(Bid-noloss*Pp,Digits)>=NormalizeDouble(OrderOpenPrice(),Digits)) ModifyOrder(OrderTicket(),-1,0,OrderOpenPrice()+noloss_pips*Pp,0,-1);
         if(OrderType()==1 && (NormalizeDouble(OrderOpenPrice(),Digits)<NormalizeDouble(OrderStopLoss(),Digits) || OrderStopLoss()==0) && NormalizeDouble(Ask+noloss*Pp,Digits)<=NormalizeDouble(OrderOpenPrice(),Digits)) ModifyOrder(OrderTicket(),-1,0,OrderOpenPrice()-noloss_pips*Pp,0,-1);
        }
      if(trall>0)
        {
         if(OrderType()==0 && NormalizeDouble(Bid-(trall+1)*Pp,Digits)>NormalizeDouble(OrderStopLoss(),Digits) && NormalizeDouble(Bid-trall_start*Pp,Digits)>=NormalizeDouble(OrderOpenPrice(),Digits)) ModifyOrder(OrderTicket(),-1,-1,Bid-trall*Pp,-1,-1);
         if(OrderType()==1 && (NormalizeDouble(Ask+(trall+1)*Pp,Digits)<NormalizeDouble(OrderStopLoss(),Digits) || OrderStopLoss()==0) && NormalizeDouble(Ask+trall_start*Pp,Digits)<=NormalizeDouble(OrderOpenPrice(),Digits)) ModifyOrder(OrderTicket(),-1,-1,Ask+trall*Pp,-1,-1);
        }
     }
// торговое решение
   if(signal==-1) return;
   if(!ta) return;
   if(signal==0) SendOrder(last_ticket,signal,lot,-1,0,ma-stop_loss*Pp,1,take_profit);
   if(signal==1) SendOrder(last_ticket,signal,lot,-1,0,ma+stop_loss*Pp,1,take_profit);
// end start      
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void init()
  {
   dig=Digits;
   Pp=Point;
   Pp2=Pp;
   Mnoj2=1/Pp;
   if(Pp==0.00001 || Pp==0.001) Pp*=10;
   Mnoj=1/Pp;
   Symb=Symbol();
   if(MarketInfo(Symb,MODE_LOTSTEP)==0.01) dg=2;
   if(MarketInfo(Symb,MODE_LOTSTEP)==0.1)  dg=1;
   min_lot=NormalizeDouble( MarketInfo(Symb,MODE_MINLOT) ,dg);
   max_lot=NormalizeDouble( MarketInfo(Symb,MODE_MAXLOT) ,dg);
   if(IsTesting() && !IsVisualMode()) Test=true;
   int k;
   if(!Test)
     {
      if(CommentSize>0) ArrayResize(CommentBox,CommentSize);
      for(k=0;k<CommentSize;k++) CommentBox[k]="";
     }
//ObjectCreate(0,"123",OBJ_HLINE,0,0,0);
//ObjectSetInteger(0,"123",OBJPROP_COLOR,clrYellow);

//ObjectCreate(0,"321",OBJ_VLINE,0,0,0);
//ObjectSetInteger(0,"321",OBJPROP_COLOR,clrYellow);
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void deinit()
  {
   Comment("");
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция открытия ордера
>>> Параметры:
>>>   int      Ticket   - тикет открываемого ордера      
>>>   int      Type     - тип открываемого ордера (0-BUY, 1-SELL, 2-BUYLIMIT, 3-SELLLIMIT, 4-BUYSTOP, 5-SELLSTOP)
>>>   double   LT       - объем открываемого ордера
>>>   
>>>   double   OP       - цены по которой открываем ордер (если Type равен 0 или 1, задавать не имеет смысла)
>>>   int      ModeSL   - метод задаваемого стоп лоса (0-конкретная цена инструмента, 1-пункты)
>>>   double   SL       - стоп лосс
>>>   int      ModeTP   - метод задаваемого тейк профита (0-конкретная цена инструмента, 1-пункты)
>>>   double   TP       - тейк профит
>>>   string   CM       - комментарий ордера
>>>   int      MG       - меджик ордера
>>>   
>>> Возвращаемые значения:
>>>   Возвращает TRUE при успешном завершении функции. Возвращает FALSE при неудачном завершении функции.
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
bool SendOrder(int &Ticket,int Type,double LT,double OP=-1,int ModeSL=0,double SL=0,int ModeTP=0,double TP=0,string CM="",int MG=-1)
  {
   if(MG==-1) MG=magic;
   color CL;
   int k,LastError;
   bool TickeT;
   if(Type==0) CL=Blue; else  if(Type==1) CL=Red; else  if(Type==2 || Type==4) CL=DarkTurquoise; else if(Type==3 || Type==5) CL=Orange;
// проверка направления
   if(Type==0 || Type==2 || Type==4)
     {
      if(!long_allowed) return(false);
        }else{
      if(!short_allowed) return(false);
     }
// проверка объема
   if(LT*MarketInfo(Symbol(),MODE_MARGINREQUIRED)>AccountFreeMargin())
     {
      PnC(StringConcatenate("[S] Не хватает средств для открытия сделки ",TypeToStr(Type)," объемом: ",DoubleToStr(LT,dg)),1);
      PnC("[S] Советник прекратил работу",1);
      Work=false;
      return(false);
     }
   if(LT<min_lot)
     {
      PnC(StringConcatenate("[S] Объем сделки меньше минимального ",DoubleToStr(min_lot,dg),". Будет открыт минимальный объем"),1);
      LT=min_lot;
     }
   if(LT>max_lot)
     {
      PnC(StringConcatenate("[S] Объем сделки больше максимального ",DoubleToStr(min_lot,dg),". Будет открыт максимальный объем"),1);
      LT=max_lot;
     }
// проверка отложенных ордеров
   double Slv=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;
   switch(Type)
     {
      case 2: if(Ask-OP<Slv) OP=Ask-Slv; break;
      case 3: if(OP-Bid<Slv) OP=Bid+Slv; break;
      case 4: if(OP-Ask<Slv) OP=Ask+Slv; break;
      case 5: if(Bid-OP<Slv) OP=Bid-Slv; break;
     }
// открытие
   for(k=0;k<TryToTrade;k++)
     {
      RefreshRates();
      if(Type==0) OP=Ask;
      if(Type==1) OP=Bid;
      PnC(StringConcatenate("[S] Открытие ордера ",TypeToStr(Type)," объемом: ",DoubleToStr(LT,dg)," по цене: ",DoubleToStr(OP,dig)," меджик: ",MG," комментарий: ",CM),0);
      if(IsTradeAllowed())
        {
         Ticket=OrderSend(Symbol(),Type,LT,NormalizeDouble(OP,dig),3,0,0,CM,MG,0,CL);
           }else{
         PnC(StringConcatenate("[S] Торговый поток занят, ждем ",k),0);
         Sleep(WaitTime);
         continue;
        }
      if(Ticket>0)
        {
         PnC(StringConcatenate("[S] Успешно открыт ордер ",Ticket),0);
         break;
        }
      LastError=Fun_Error(GetLastError());
      switch(LastError)
        {
         case 0:
            if(k==TryToTrade) return(false);
            Sleep(WaitTime);
            break;
         case 1:
            return(false);
         case 2:
            Work=false;
            return(false);
        }
     }
   if(SL==0 && TP==0) return(true);
   if(!OrderSelect(Ticket,SELECT_BY_TICKET))
     {
      PnC(StringConcatenate("[S] Ошибка выбора открытого ордера ",Ticket," для выставления стопов"),1);
      return(false);
     }
// проверка и расчет стопов
   if(SL!=0)
     {
      if(ModeSL==1)
        {
         if(Type==0 || Type==2 || Type==4) SL=OrderOpenPrice()-SL*Pp; else SL=OrderOpenPrice()+SL*Pp;
        }
      if(Type==0 || Type==2 || Type==4)
        {
         if(Bid-SL<Slv && SL!=0) SL=Bid-Slv;
           }else{
         if(SL-Ask<Slv && SL!=0) SL=Ask+Slv;
        }
     }
   if(TP!=0)
     {
      if(ModeTP==1)
        {
         if(Type==0 || Type==2 || Type==4) TP=OrderOpenPrice()+TP*Pp; else TP=OrderOpenPrice()-TP*Pp;
        }
      if(Type==0 || Type==2 || Type==4)
        {
         if(TP-Bid<Slv && TP!=0) TP=Bid+Slv;
           }else{
         if(Ask-TP<Slv && TP!=0) TP=Ask-Slv;
        }
     }
// выставляем стопы
   for(k=0;k<TryToTrade;k++)
     {
      PnC(StringConcatenate("[S] Установка стопов на ордер: ",Ticket," с/л: ",DoubleToStr(SL,dig)," т/п: ",DoubleToStr(TP,dig)),0);
      if(IsTradeAllowed())
        {
         TickeT=OrderModify(Ticket,OrderOpenPrice(),NormalizeDouble(SL,dig),NormalizeDouble(TP,dig),0,CLR_NONE);
           }else{
         PnC(StringConcatenate("[S] Торговый поток занят, ждем ",k),0);
         Sleep(WaitTime);
         continue;
        }
      if(TickeT)
        {
         PnC(StringConcatenate("[S] Успешно модифицирован ордер ",Ticket),0);
         break;
        }
      LastError=Fun_Error(GetLastError());
      switch(LastError)
        {
         case 0:
            if(k==TryToTrade) return(false);
            Sleep(WaitTime);
            break;
         case 1:
            return(false);
         case 2:
            Work=false;
            return(false);
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция модификации ордера
>>> Параметры:
>>>   int      Ticket   - номер ордера
>>>   
>>>   double   OP       - новая цена открытия ордера. Если -1 - останется без изменений
>>>   int      ModeSL   - метод задаваемого стоп лоса (0-конкретная цена инструмента, 1-пункты)
>>>   double   SL       - новый стоп лосс ордера. Если -1 - останется без изменений
>>>   int      ModeTP   - метод задаваемого тейк профита (0-конкретная цена инструмента, 1-пункты)
>>>   double   TP       - новый тейк профит ордера. Если -1 - останется без изменений
>>>   
>>> Возвращаемые значения:
>>>   Возвращает TRUE при успешном завершении функции. Возвращает FALSE при неудачном завершении функции.
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
bool ModifyOrder(int Ticket,double OP=-1,int ModeSL=0,double SL=0,int ModeTP=0,double TP=0)
  {
   bool TickeT;
   int k,LastError;
   TickeT=OrderSelect(Ticket,SELECT_BY_TICKET);
   if(!TickeT)
     {
      PnC("[M] Ошибка выбора ордера для модификации ",0);
      return(false);
     }
   if(OrderCloseTime()>0)
     {
      PnC("[M] Ордер был закрыт или удален",0);
      return(false);
     }
   int Type=OrderType();
   double mo_calcl_price=OrderOpenPrice();
   if(OP>0) mo_calcl_price=OP;
   if(ModeSL==1 && SL!=-1)
     {
      if(Type==0 || Type==2 || Type==4) SL=mo_calcl_price-SL*Pp; else SL=mo_calcl_price+SL*Pp;
     }
   if(ModeTP==1 && TP!=-1)
     {
      if(Type==0 || Type==2 || Type==4) TP=mo_calcl_price+TP*Pp; else TP=mo_calcl_price-TP*Pp;
     }
   string cm;
   if(OP<0) OP=OrderOpenPrice(); else cm="цена " + DoubleToStr(OrderOpenPrice(),dig) + " => " + DoubleToStr(OP,dig) + "; ";
   if(SL<0) SL=OrderStopLoss(); else cm=cm+"с/л " + DoubleToStr(OrderStopLoss(),dig) + " => " + DoubleToStr(SL,dig) + "; ";
   if(TP<0) TP=OrderTakeProfit(); else cm=cm+"т/п " + DoubleToStr(OrderTakeProfit(),dig) + " => " + DoubleToStr(TP,dig) + "; ";
   if(Type==0 || Type==3 || Type==5) cm=cm+"текущая цена Bid: "+DoubleToStr(Bid,dig);
   if(Type==1 || Type==2 || Type==4) cm=cm+"текущая цена Ask: "+DoubleToStr(Ask,dig);
   color modify_color;
   if(MathMod(OrderType(),2.0)==0) modify_color=Aqua; else modify_color=Orange;
   for(k=0;k<TryToTrade;k++)
     {
      PnC(StringConcatenate("[M] Модификация ордера: ",Ticket," ",cm),0);
      if(IsTradeAllowed())
        {
         TickeT=OrderModify(Ticket,NormalizeDouble(OP,dig),NormalizeDouble(SL,dig),NormalizeDouble(TP,dig),0,modify_color);
           }else{
         PnC(StringConcatenate("[M] Торговый поток занят, ждем ",k),0);
         Sleep(WaitTime);
         continue;
        }
      if(TickeT==true)
        {
         PnC(StringConcatenate("[M] Успешно модифицирован ордер ",Ticket),0);
         return(true);
        }
      LastError=Fun_Error(GetLastError());
      switch(LastError)
        {
         case 0:
            if(k==TryToTrade) return(false);
            Sleep(WaitTime);
            break;
         case 1:
            return(false);
         case 2:
            Work=false;
            return(false);
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция закрытия ордера
>>> Параметры:
>>>   int      Ticket   - номер ордера
>>>   
>>>   double   LT       - объем который необходимо закрыть. Если -1 - ордер будет закрыт полностью
>>>   
>>> Возвращаемые значения:
>>>   Возвращает TRUE при успешном завершении функции. Возвращает FALSE при неудачном завершении функции.
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
bool CloseOrder(int Ticket,double LT=-1)
  {
   bool TickeT;
   double OCP;
   int k,LastError;
   TickeT=OrderSelect(Ticket,SELECT_BY_TICKET);
   if(!TickeT)
     {
      PnC("[C] Ошибка выбора ордера для закрытия ",0);
      return(false);
     }
   if(OrderCloseTime()>0)
     {
      PnC("[C] Ордер был закрыт или удален",0);
      return(false);
     }
   int Type=OrderType();
   if(Type>1)
     {
      PnC("[C] Ордер закрыть нельзя, он отложенный ",0);
      return(false);
     }
   if(LT==-1) LT=NormalizeDouble(OrderLots(),dg); else LT=NormalizeDouble(LT,dg);
   for(k=0;k<=TryToTrade;k++)
     {
      RefreshRates();
      if(Type==0) OCP=Bid; else OCP=Ask;
      PnC(StringConcatenate("[C] Закрытие ордера ",TypeToStr(Type)," номер: ",Ticket," объемом: ",DoubleToStr(LT,dg)," по цене: ",DoubleToStr(OCP,dig)),0);
      if(IsTradeAllowed())
        {
         TickeT=OrderClose(Ticket,LT,NormalizeDouble(OCP,dig),30,White);
           }else{
         PnC(StringConcatenate("[C] Торговый поток занят, ждем ",k),0);
         Sleep(WaitTime);
         continue;
        }
      if(TickeT)
        {
         PnC(StringConcatenate("[C] Успешно закрыт ордер ",Ticket),0);
         return(true);
        }
      LastError=Fun_Error(GetLastError());
      switch(LastError)
        {
         case 0:
            if(k==TryToTrade) return(false);
            Sleep(WaitTime);
            break;
         case 1:
            return(false);
         case 2:
            Work=false;
            return(false);
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция удаления ордера
>>> Параметры:
>>>   int      Ticket   - номер ордера
>>>   
>>> Возвращаемые значения:
>>>   Возвращает TRUE при успешном завершении функции. Возвращает FALSE при неудачном завершении функции.
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
bool DeleteOrder(int Ticket)
  {
   bool TickeT;
   int k,LastError;
   TickeT=OrderSelect(Ticket,SELECT_BY_TICKET);
   if(!TickeT)
     {
      PnC(StringConcatenate("[D] Ошибка выбора ордера для закрытия ",Ticket),0);
      return(false);
     }
   if(OrderCloseTime()>0)
     {
      PnC(StringConcatenate("[D] Ордер был закрыт или удален ",Ticket),0);
      return(false);
     }
   int Type=OrderType();
   if(Type<2)
     {
      PnC(StringConcatenate("[D] Ордер удалить нельзя, он уже исполнен ",Ticket),0);
      return(false);
     }
   for(k=0;k<=TryToTrade;k++)
     {
      PnC(StringConcatenate("[D] Удаление ордера: ",Ticket," тип: ",TypeToStr(Type)),0);
      if(IsTradeAllowed())
        {
         TickeT=OrderDelete(Ticket);
           }else{
         PnC(StringConcatenate("[D] Торговый поток занят, ждем ",k),0);
         Sleep(WaitTime);
         continue;
        }
      if(TickeT)
        {
         PnC(StringConcatenate("[D] Успешно удален ордер ",Ticket),0);
         return(true);
        }
      LastError=Fun_Error(GetLastError());
      switch(LastError)
        {
         case 0:
            if(k==TryToTrade) return(false);
            Sleep(WaitTime);
            break;
         case 1:
            return(false);
         case 2:
            Work=false;
            return(false);
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция удаления и закрытия всех ордеров
>>> Возвращаемые значения:
>>>   Возвращает TRUE при успешном завершении функции. Возвращает FALSE при неудачном завершении функции.
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
bool CloseAll()
  {
   for(i=OrdersTotal()-1;i>=0;i--) if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) && OrderSymbol()==Symb && OrderMagicNumber()==magic)
     {
      if(OrderType()<2)
        {
         if(!CloseOrder(OrderTicket())) return(false);
           }else{
         if(!DeleteOrder(OrderTicket())) return(false);
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция удаления всех ордеров
>>> Возвращаемые значения:
>>>   Возвращает TRUE при успешном завершении функции. Возвращает FALSE при неудачном завершении функции.
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
bool DeleteAll()
  {
   for(i=OrdersTotal()-1;i>=0;i--) if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) && OrderSymbol()==Symb && OrderMagicNumber()==magic)
     {
      if(OrderType()>1)
        {
         if(!DeleteOrder(OrderTicket())) return(false);
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция текстовых сообщений
>>> Описание:
>>>   Функция позволяет отображать системные сообщения на экране в виде ленты в левом верхнем углу экрана, 
>>>   заносить их в журнал а также при необходимости отображать уведомление (Alert).
>>>
>>> Параметры:
>>>   string   txt      - текст сообщения
>>>   int      Mode     - тип сообщения (0-только Print, 1-Print и Alert, 2-ничего)
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
void PnC(string txt,int Mode)
  {
   int j;
   string cm;
   if(Mode!=2)
     {
      string HR=DoubleToStr(Hour(),0);    if(StringLen(HR)<2) HR="0"+HR;
      string MN=DoubleToStr(Minute(),0);  if(StringLen(MN)<2) MN="0"+MN;
      string SC=DoubleToStr(Seconds(),0); if(StringLen(SC)<2) SC="0"+SC;
      txt=StringConcatenate(HR,":",MN,":",SC," ",Symb," ",txt);
      Print(txt);
      if(Test) return;
      if(Mode>0) Alert(txt);
      for(j=CommentSize-1;j>=1;j--) CommentBox[j]=CommentBox[j-1];
      CommentBox[0]=txt;
     }
   if(CommentSize>0)
     {
      for(j=CommentSize-1;j>=0;j--) if(CommentBox[j]!="") cm=StringConcatenate(cm,CommentBox[j],"\n");
     }
   if(CommentSize>0)
      cm=StringConcatenate(cm,"\n",cm2);
   else
      cm=StringConcatenate(cm2);
   cm=StringConcatenate(cm,"\n","MqlLab.ru");
   Comment(cm);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция преобразования типа ордера
>>> Описание:
>>>   Преобразование числа, содержащего числовое представление типа ордера в строку
>>>
>>> Параметры:
>>>   int      Type     -  тип ордера (0-BUY, 1-SELL, 2-BUYLIMIT, 3-SELLLIMIT, 4-BUYSTOP, 5-SELLSTOP)
>>>   
>>> Возвращаемые значения:
>>>   В случае успешного исполнения функции возвращает строку с типом переменной. В обратном случае возвращает "NONE".
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
string TypeToStr(int Type)
  {
   switch(Type)
     {
      case 0: return("BUY");
      case 1: return("SELL");
      case 2: return("BUYLIMIT");
      case 3: return("SELLLIMIT");
      case 4: return("BUYSTOP");
      case 5: return("SELLSTOP");
     }
   return("NONE");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция преобразования тайм фрейма
>>> Описание:
>>>   Преобразование тайм фрейма, содержащего числовое представление времени в строку
>>>
>>> Параметры:
>>>   int      pts_period   -  тайм фрейм (0,1,5,15,30,60,240,1440,10080,43200)
>>>   
>>> Возвращаемые значения:
>>>   В случае успешного исполнения функции возвращает строку с тайм фреймом. В обратном случае возвращает "NONE".
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
string PeriodToStr(int pts_period)
  {
   if(pts_period==0) pts_period=Period();
   switch(pts_period)
     {
      case 1:     return("M1");
      case 5:     return("M5");
      case 15:    return("M15");
      case 30:    return("M30");
      case 60:    return("H1");
      case 240:   return("H4");
      case 1440:  return("D1");
      case 10080: return("W1");
      case 43200: return("MN1");
     }
   return("NONE");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
>>>         Функция обработки ошибок
>>> Описание:
>>>   Функция выводит сообщение об ошибке с текстовым описанием, а также возвращает ответ в зависимости от того к какой из трех групп принадлежит ошибка.
>>>   Также в случае ошибок 4110 и 4111 (запрет на торговлю в определенном направлении в общих настройках эксперта) меняет переменные long.allowed и 
>>>   short.allowed на false с тем чтобы в следующий раз функция SendOrder не исполнялась в этом же направлении
>>>
>>> Параметры:
>>>   int      er       - номер ошибки
>>>   
>>> Возвращаемые значения:
>>>   В случае успешного исполнения функции возвращает одиз из трех ответов:
>>>   0 - следует повторить выполнение торговой функции
>>>   1 - следует прекратить выполнение торговой функции
>>>   2 - следует завершить работу советника
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<*/
int Fun_Error(int er)
  {
   switch(er)
     {
      // группа 1: не прекращать попытки
      case 2: PnC("Общая ошибка",0); return(0);
      case 4: PnC("Торговый сервер занят",0); return(0);
      case 8: PnC("Слишком частые запросы",0); return(0);
      case 129: PnC("Неправильная цена",0); return(0);
      case 135: PnC("Цена изменилась",0); return(0);
      case 136: PnC("Нет цен",0); return(0);
      case 137: PnC("Брокер занят",0); return(0);
      case 138: PnC("Новые цены",0); return(0);
      case 141: PnC("Слишком много запросов",0); return(0);
      case 146: PnC("Подсистема торговли занята",0); return(0);
      // группа 2: прекращаем попытки
      case 0: PnC("Ошибка отсутствует",0); return(1);
      case 1: PnC("Нет ошибки, но результат не известен",0); return(1);
      case 3: PnC("Неправильные параметры",0); return(1);
      case 6: PnC("Нет связи с торговым сервером",0); return(1);
      case 128: PnC("Истек срок ожидания совершения сделки",0); return(1);
      case 130: PnC("Неправильные стопы",0); return(1);
      case 131: PnC("Неправильный объем",0); return(1);
      case 132: PnC("Рынок закрыт",0); return(1);
      case 133: PnC("Торговля запрещена",0); return(1);
      case 134: PnC("Недостаточно денег для совершения операции",0); return(1);
      case 139: PnC("Ордер заблокирован и уже обрабатывается",0); return(1);
      case 145: PnC("Модификация запрещена, так как ордер слишком близок к рынку",0); return(1);
      case 148: PnC("Количество открытых и отложенных ордеров достигло предела, установленного брокером",0); return(1);
      case 4000: PnC("Нет ошибки",0); return(1);
      case 4107: PnC("Неправильный параметр цены для торговой функции",0); return(1);
      case 4108: PnC("Ордер не найден",0); return(1);
      case 4110: PnC("BUY позиции не разрешены",0); long_allowed=false; return(1);
      case 4111: PnC("SELL позиции не разрешены",0); short_allowed=false; return(1);
      // группа 3: завершаем работу
      case 5: PnC("Старая версия клиентского терминала",1); return(2);
      case 7: PnC("Недостаточно прав",1); return(2);
      case 9: PnC("Недопустимая операция нарушающая функционирование сервера",1); return(2);
      case 64: PnC("Счет заблокирован",1); return(2);
      case 65: PnC("Неправильный номер счета",1); return(2);
      case 140: PnC("Разрешена только покупка",1); return(2);
      case 147: PnC("Использование даты истечения ордера запрещено брокером",1); return(2);
      case 149: PnC("Попытка открыть противоположную позицию к уже существующей в случае, если хеджирование запрещено",1); return(2);
      case 150: PnC("Попытка закрыть позицию по инструменту в противоречии с правилом FIFO",1); return(2);
      case 4109: PnC("Торговля не разрешена, разрешите торговлю советнику и перезапустите его",1); return(2);
     }
   return(0);
  }
//+------------------------------------------------------------------+
