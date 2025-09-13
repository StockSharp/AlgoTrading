//+------------------------------------------------------------------+
//|                                                labouchere_ea.mq4 |
//|                                         Copyright 2016, eevviill |
//|                                     http://alievtm.blogspot.com/ |
//+------------------------------------------------------------------+
#property copyright "eevviill"
#property link "http://alievtm.blogspot.com/"
#property version "1.4"
#property strict

extern string os="Lots and Stops settings";
extern string Lot="0.01,0.02,0.01,0.02,0.01,0.01,0.01,0.01";
extern bool new_recycle=true;
extern int StopLoss=40;
extern int TakeProfit=50;

extern string emp1 = "/////////////////////////////////////////////////////////";
extern string slug = "Additional settings";
extern bool use_transform_4_dig=true;
//перевод на 4-знак
extern bool use_revers=false;
//открывать вместо бай-сел и наоборот
extern bool use_oposite_exit=false;
//использовать выход по обратному сигналу
extern int Slippage=2;
//проскальзывание
extern int Magic=4335;
//номер присваиваемый для ордера
extern string comment="lab";
//коментарий к ордеру
int MaxAttempts=64;
//макс количество попыток открыть или закрыть ордер
double pause_if_busy=0.2;
//количество секунд паузы перед повторной попыткой модифицировать, закрыть или открыть ордер если торговый поток занят
bool use_data_from_closed_candle=true;
//работа по закрытию свечи

extern string emp2="/////////////////////////////////////////////////////////";
extern string V_R="Work time settings";
extern bool use_work_time=false;
extern string start1= "08:00";
extern string stop1 = "16:00";
extern string start2= "";
extern string stop2 = "";
extern string start3= "";
extern string stop3 = "";

extern string emp3="/////////////////////////////////////////////////////////";
extern string ind1_name="Stochastic";
extern int Kperiod = 10;
extern int Dperiod = 190;
extern int slowing=40;
extern ENUM_MA_METHOD method=MODE_SMA;
extern ENUM_STO_PRICE price_field=STO_LOWHIGH;

int buy,sell,Orders_Total;
int Sig_p;
double point;
int nor_lot=2;
int CC;
int prevbars;
datetime prev_day,start_1,stop_1,start_2,stop_2,start_3,stop_3;
double lots_buf[];
datetime last_closed_order_time;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   Lot_to_buf();

   point=Point;
   if(use_transform_4_dig && (Digits==3 || Digits==5))
      point*=10;

   prevbars=Bars;

   if(MarketInfo(Symbol(),MODE_LOTSTEP)==0.1) nor_lot=1;

   if(use_data_from_closed_candle) CC=1;

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
//no bars
   if(Bars<20) return;

//trading is not allowed
   if(!IsTradeAllowed()) return;


//SL && TP 
   if(StopLoss!=0 || TakeProfit!=0)
      SL_TP_f();

//new bar
   if(use_data_from_closed_candle)
     {
      if(Bars==prevbars) return;
      prevbars=Bars;
     }

//signal to enter
   Sig_p=Sig_f();

//revers
   if(use_revers)
     {
      if(Sig_p==1) Sig_p=-1;
      else
         if(Sig_p==-1) Sig_p=1;
     }

//exit
   if(use_oposite_exit && Sig_p!=0) close_f();

//time filter
   if(use_work_time && work_time_f()) return;


//prof/lose chek
   chek_new_buf_data();

//enter
   if(Sig_p!=0) open_f();

  }
//расчёт функций
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
char Sig_f()
  {
//объявление индикаторов
   double green_l=iStochastic(Symbol(),0,Kperiod,Dperiod,slowing,method,price_field,MODE_MAIN,CC);
   double green_l_pre=iStochastic(Symbol(),0,Kperiod,Dperiod,slowing,method,price_field,MODE_MAIN,CC+1);
   double red_l=iStochastic(Symbol(),0,Kperiod,Dperiod,slowing,method,price_field,MODE_SIGNAL,CC);
   double red_l_pre=iStochastic(Symbol(),0,Kperiod,Dperiod,slowing,method,price_field,MODE_SIGNAL,CC+1);

//сигнал для бай
   if(green_l_pre<=red_l_pre && green_l>red_l) return(1);
   if(green_l_pre>=red_l_pre && green_l<red_l) return(-1);

   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void open_f()
  {
   CountOpenedPositions_f();
   if(Orders_Total>0) return;

///////////// LOT /////////////////
   if(ArraySize(lots_buf)<=1)
     {
      if(!new_recycle) return;
      else
         Lot_to_buf();
     }

   double Lotss=lots_buf[0]+lots_buf[ArraySize(lots_buf)-1];
//check free margin
   if(MarketInfo(Symbol(),MODE_MARGINREQUIRED)*Lotss>AccountFreeMargin()) {Alert("Not enouth money to open order "+string(Lotss)+" lots!");return;}

///////////// OPEN SET ///////////// 
//price
   double price=0;
//way open
   int open_type=0;
   if(Sig_p>0) open_type=OP_BUY;
   if(Sig_p<0) open_type=OP_SELL;

///////////// MAIN /////////////  
   int ticket_op=-1;
   for(int j_op = 0; j_op < MaxAttempts; j_op++)
     {
      while(IsTradeContextBusy()) Sleep(int(pause_if_busy*1000));
      RefreshRates();

      if(Sig_p>0) price=Ask;
      if(Sig_p<0) price=Bid;
      ticket_op=OrderSend(Symbol(),open_type,Lotss,price,Slippage,0,0,comment,Magic,0,clrNONE);
      if(ticket_op>-1)break;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void close_f()
  {
//Выход
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               bool ticket_ex=false;
               for(int j_ex=0;j_ex<MaxAttempts; j_ex++)
                 {
                  while(IsTradeContextBusy()) Sleep(int(pause_if_busy*1000));
                  RefreshRates();

                  if(OrderType()==OP_BUY && use_oposite_exit && Sig_p<0) ticket_ex=OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrNONE);
                  if(OrderType()==OP_SELL && use_oposite_exit && Sig_p>0) ticket_ex=OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrNONE);
                  if(ticket_ex==true)break;
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CountOpenedPositions_f()
  {
   buy=0;
   sell=0;
   Orders_Total=0;

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)      buy++;
               if(OrderType()==OP_SELL)     sell++;
              }
           }
        }
     }

   Orders_Total=buy+sell;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SL_TP_f()
  {
   double modify_price_SL;
   double modify_price_TP;

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               while(IsTradeContextBusy()) Sleep(int(pause_if_busy*1000));
               RefreshRates();
               /////////////////////////////////////////////////
               if(OrderType()==OP_BUY)
                 {
                  modify_price_SL=OrderOpenPrice()-StopLoss*point;
                  if(StopLoss==0) modify_price_SL=OrderStopLoss();
                  modify_price_TP=OrderOpenPrice()+TakeProfit*point;
                  if(TakeProfit==0) modify_price_TP=OrderTakeProfit();

                  if(((StopLoss>0 && OrderStopLoss()==0) || StopLoss==0)
                     && ((TakeProfit>0 && OrderTakeProfit()==0) || TakeProfit==0))
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(modify_price_SL,Digits),NormalizeDouble(modify_price_TP,Digits),0,clrNONE)) continue;
                  else
                  if(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrNONE)) {Alert("Order "+string(OrderTicket())+" closed via stops are out of price!");continue;}
                 }
               /////////////////////////
               if(OrderType()==OP_SELL)
                 {
                  modify_price_SL=OrderOpenPrice()+StopLoss*point;
                  if(StopLoss==0) modify_price_SL=OrderStopLoss();
                  modify_price_TP=OrderOpenPrice()-TakeProfit*point;
                  if(TakeProfit==0) modify_price_TP=OrderTakeProfit();

                  if(((StopLoss>0 && OrderStopLoss()==0) || StopLoss==0)
                     && ((TakeProfit>0 && OrderTakeProfit()==0) || TakeProfit==0))
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(modify_price_SL,Digits),NormalizeDouble(modify_price_TP,Digits),0,clrNONE)) continue;
                  else
                  if(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrNONE)) {Alert("Order "+string(OrderTicket())+" closed via stops are out of price!");continue;}
                 }
               //////////////////////////////////////////////// 
              }
           }
        }
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool work_time_f()
  {
   datetime time_current=TimeCurrent();

   if(iTime(Symbol(),PERIOD_D1,0)!=prev_day)
     {
      prev_day=iTime(Symbol(),PERIOD_D1,0);

      start_1=StringToTime(start1);
      stop_1=StringToTime(stop1);
      start_2=StringToTime(start2);
      stop_2=StringToTime(stop2);
      start_3=StringToTime(start3);
      stop_3=StringToTime(stop3);
     }//end if new day

   if(
      (start_1==stop_1 || ((start_1<stop_1 && (time_current<start_1 || time_current>stop_1)) || (start_1>stop_1 && (time_current<start_1 && time_current>stop_1))))
      && (start_2==stop_2 ||  ((start_2<stop_2 && (time_current<start_2 || time_current>stop_2)) || (start_2>stop_2 && (time_current<start_2 && time_current>stop_2))))
      && (start_3==stop_3 ||  ((start_3<stop_3 && (time_current<start_3 || time_current>stop_3)) || (start_3>stop_3 && (time_current<start_3 && time_current>stop_3))))
      ) return(false);

   return (true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Lot_to_buf()
  {
   string str_spl[];
   int size=StringSplit(Lot,StringGetCharacter(",",0),str_spl);
   ArrayResize(lots_buf,size);

   for(int i=0;i<size;i++)
     {
      lots_buf[i]=double(str_spl[i]);
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
char chek_prev_order_f()
  {
   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if(OrderMagicNumber()==Magic)
           {
            if(OrderSymbol()==Symbol())
              {
               ////
               if(OrderType()==OP_SELL)
                 {
                  if(OrderOpenTime()<=last_closed_order_time) return(0);
                  last_closed_order_time=OrderOpenTime();

                  if(OrderProfit()+OrderSwap()+OrderCommission()<=0) return(-1);
                  else
                     return(1);
                 }
               /////
               if(OrderType()==OP_BUY)
                 {
                  if(OrderOpenTime()<=last_closed_order_time) return(0);
                  last_closed_order_time=OrderOpenTime();

                  if(OrderProfit()+OrderSwap()+OrderCommission()<=0) return(-1);
                  else
                     return(1);
                 }
               /////
              }
           }
        }
     }

   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void chek_new_buf_data()
  {
   char pof_lose=chek_prev_order_f();
   if(pof_lose<0)
     {
      ArrayResize(lots_buf,ArraySize(lots_buf)+1);
      lots_buf[ArraySize(lots_buf)-1]=lots_buf[0]+lots_buf[ArraySize(lots_buf)-2];
     }
   if(pof_lose>0)
     {
      ArrayCopy(lots_buf,lots_buf,0,1);
      ArrayResize(lots_buf,ArraySize(lots_buf)-2);
     }
  }
//+------------------------------------------------------------------+
