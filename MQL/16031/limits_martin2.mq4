//+------------------------------------------------------------------+
//|                                                Limits_Martin.mq4 |
//|                                                 Dmitriy Epshteyn |
//|                                                  setkafx@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Dmitriy Epshteyn"
#property link      "setkafx@mail.ru"
#property version   "1.00"
#property strict

extern int         Limits0_or_Stops1=0;
// если 0 - торгуем лимитными ордерами, если 1 - торгуем стоповыми
extern bool        Last_Price_Limit_Use=false;
//после удаления советником лимитной отложки при появлении рыночного ордера, следующий лимитный ордер такого же типа выставится по той же цене
// (лучше оставить false)
extern bool        Last_Price_Stops_Use=false;
//после удаления советником отложки при появлении рыночного ордера, следующий отложенный стоп ордер такого же типа выставится по той же цене
extern int         Step=200;
//расстояние, на котором выставляются и тянуться ордера за ценой
extern int         Step_Interval=10;
// шаг в пунктах, через который отложенные ордера подтягиваются к рыночной цене
extern int         SL=30;
//стоп лосс
extern int         TP=60;
//тейк профит

extern bool  Martin=true;
//включить мартин 
extern int         Limit=10;
//ограничение количества умножений лота
extern double      Lots=0.01;
//лот
extern bool        MegaLot=true;
// при открытии следующего ордера в случае, если последний ордер (серия убыточных ордеров) закрылся убытком, 
// лот будет рассчитываться таким образом,
// чтобы перекрыть предыдущие убытки и заработать сумму в валюте депозита = кол-во пунктов по тейк профиту, закрытые  стартовым лотом 
extern string A2="_______________Trailing Stop___________";
extern bool Trailing_Use=true;
// включить трейлинг стоп ордеров (работает, если последний ордер закрылся профитом либо в ноль)
extern int Profit_Level_Trailing=100;
// уровень профита в пунктах, при достижении ордера которым начинает работать трейлинг стоп 
extern int TrailingStop=50;
// Расстояние TrailingStop пунктов, на котором стоп лосс ордера будет тянуться за ценой
extern int TrailingStep=50;
// Шаг в пунктах, через который будет перемещаться  стоп лосс ордера 
extern string A3="_______________Breakeven______________";
extern bool Breakeven_Use=true;
// включить безубыток 
extern int Profit_Level=30;
// Ордер выходит в плюс на указанное количество пунктов (для безубытка)
extern int SL_Plus=10;
// Выставляется безубыток ордера+SL_Plus в пунктах
extern int         Slip=5;
// Проскальзывание
extern int         Magic=100;
//индивидуальный номер эксперта

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   ENUM_TIMEFRAMES TF=PERIOD_CURRENT;

   int orders=0;
   int accTotal1=OrdersHistoryTotal();
   for(int h_1=accTotal1-1;h_1>=0;h_1--)
      if(OrderSelect(h_1,SELECT_BY_POS,MODE_HISTORY))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==Magic)
               if(OrderType()==OP_BUY || OrderType()==OP_SELL)
                 {
                  if(OrderCloseTime()>=iTime(NULL,TF,0)) {orders++; }
                  if(OrderCloseTime()<iTime(NULL,TF,0)) {break;}
                 }
//--------------история  ордеров--------------
   int loss=0; // кол-во ордеров, закрытых в подряд с убытком
   double loss_profit=0;
   for(int h_2=accTotal1-1;h_2>=0;h_2--)
      if(OrderSelect(h_2,SELECT_BY_POS,MODE_HISTORY))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==Magic)
              {
               if(OrderProfit()<0) {loss++;loss_profit+=OrderProfit()+OrderSwap()+OrderCommission(); }
               if(OrderProfit()>0) {break;}
              }
   int    OP_TYPE=-1;
   double h_Lot=0; // лот последнего ордера
   double last_profit=0; // профит последнего закрытого ордера
   for(int h_3=accTotal1-1;h_3>=0;h_3--)
      if(OrderSelect(h_3,SELECT_BY_POS,MODE_HISTORY))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==Magic)
              {
               if(OrderType()==OP_BUY || OrderType()==OP_SELL) {h_Lot=OrderLots(); last_profit=OrderProfit()+OrderSwap()+OrderCommission();OP_TYPE=OrderType(); break;}
              }
   double last_limit_price=0; // цена открытия последнего удаленного лимитного ордера
   int OP_TYPE_LIMIT=-1; // тип последнего удаленного лимитного ордера
   for(int h_4=accTotal1-1;h_4>=0;h_4--)
      if(OrderSelect(h_4,SELECT_BY_POS,MODE_HISTORY))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==Magic)
              {
               if(OrderType()==OP_BUYLIMIT || OrderType()==OP_SELLLIMIT) {last_limit_price=OrderOpenPrice(); OP_TYPE_LIMIT=OrderType(); break;}
              }
   double last_stop_price=0; // цена открытия последнего удаленного лимитного ордера
   int OP_TYPE_STOPS=-1; // тип последнего удаленного лимитного ордера
   for(int h_5=accTotal1-1;h_5>=0;h_5--)
      if(OrderSelect(h_5,SELECT_BY_POS,MODE_HISTORY))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==Magic)
              {
               if(OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP) {last_stop_price=OrderOpenPrice(); OP_TYPE_STOPS=OrderType(); break;}
              }

//-------------------лот------------------
   double sl = NormalizeDouble(SL*Point,Digits);
   double tp = NormalizeDouble(TP*Point,Digits);


   double lots_step=MarketInfo(Symbol(),MODE_LOTSTEP);
   int lots_digits = 0;
   if(lots_step==0.01) {lots_digits=2;}
   if(lots_step==0.1)  {lots_digits=1;}
   if(lots_step==1.0)  {lots_digits=0;}

   double Lot=0;
   if((last_profit>=0) || (loss>=Limit)) {Lot=Lots;}
   if(MegaLot==true && last_profit<0 && tp/Point*h_Lot *MarketInfo(Symbol(),MODE_TICKVALUE)<=MathAbs(loss_profit) && loss<Limit) {Lot=NormalizeDouble(((MathAbs(loss_profit)+TP*Lots*MarketInfo(Symbol(),MODE_TICKVALUE))/tp*Point/MarketInfo(Symbol(),MODE_TICKVALUE)),2); }
   if(MegaLot==false && last_profit<0 && tp/Point*h_Lot *MarketInfo(Symbol(),MODE_TICKVALUE)<=MathAbs(loss_profit) && loss<Limit) {Lot=NormalizeDouble(((MathAbs(loss_profit))/tp*Point/MarketInfo(Symbol(),MODE_TICKVALUE)),2); }

   if(last_profit<0 && tp/Point*h_Lot *MarketInfo(Symbol(),MODE_TICKVALUE)>MathAbs(loss_profit) && loss<Limit) {Lot=h_Lot; }

   double lots_test=Lot;
   lots_test=NormalizeDouble(MathCeil((lots_test)/lots_step)*lots_step,lots_digits);
   if(lots_test<Lot) {Lot=Lots;}

   if(Martin==false) {Lot=Lots;}
   if(Lot<MarketInfo(Symbol(),MODE_MINLOT)) {Lot=MarketInfo(Symbol(),MODE_MINLOT);}
   if(Lot>MarketInfo(Symbol(),MODE_MAXLOT)) {Lot=MarketInfo(Symbol(),MODE_MAXLOT);}

   bool open_buy=true,open_sell=true;
   if(AccountFreeMarginCheck(Symbol(),OP_BUY,Lot)<=0) {Lot=Lots;}
   if(AccountFreeMarginCheck(Symbol(),OP_SELL,Lot)<=0) {Lot=Lots;}

   if(AccountFreeMarginCheck(Symbol(),OP_BUY,Lot)<=0) {open_buy=false;}
   if(AccountFreeMarginCheck(Symbol(),OP_SELL,Lot)<=0) {open_sell=false;}

   if(open_buy==false || open_sell==false) {Comment("Not enough money to open a lot buy=",DoubleToStr(Lot,2)," or lot sell=",DoubleToStr(Lot,2));}
//--------------

   double step=NormalizeDouble(Step*Point,Digits);
   double step_interval=NormalizeDouble(Step_Interval*Point,Digits);
   double stops=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;

   double TS=NormalizeDouble(TrailingStop*Point,Digits);
   double shag=NormalizeDouble(TrailingStep*Point,Digits);
   double sl_Plus=NormalizeDouble(SL_Plus*Point,Digits);
   double profit_level=NormalizeDouble(Profit_Level*Point,Digits);
   double profit_level_tr=NormalizeDouble(Profit_Level_Trailing*Point,Digits);

   int b=0,s=0,n=0,blimit=0,slimit=0,bs=0,ss=0,total=OrdersTotal();
   for(int i1=total-1; i1>=0; i1--)
      if(OrderSelect(i1,SELECT_BY_POS))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==Magic)
              {
               if(OrderType()==OP_BUY)
                 {
                  b++;n++;

                  if(SL>0 && TP>0 && OrderStopLoss()==0 && Bid>NormalizeDouble(OrderOpenPrice()-sl+stops,Digits) && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-sl,Digits)
                     && OrderTakeProfit()==0 && Ask<NormalizeDouble(OrderOpenPrice()+tp-stops,Digits) && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()+tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-sl,Digits),NormalizeDouble(OrderOpenPrice()+tp,Digits),0,0);if(!mod) Print("Error modification block 1=",GetLastError());}

                  if(SL>0 && OrderStopLoss()==0 && Bid>NormalizeDouble(OrderOpenPrice()-sl+stops,Digits) && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-sl,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-sl,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 2=",GetLastError());}
                  if(SL>0 && OrderStopLoss()==0 && Bid<NormalizeDouble(OrderOpenPrice()-sl+stops,Digits) && OrderStopLoss()!=NormalizeDouble(Bid-stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid-stops,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 3=",GetLastError());}

                  if(TP>0 && OrderTakeProfit()==0 && Ask<NormalizeDouble(OrderOpenPrice()+tp-stops,Digits) && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()+tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()+tp,Digits),0,0);if(!mod) Print("Error modification block 4=",GetLastError());}

                  if(TP>0 && OrderTakeProfit()==0 && Ask>NormalizeDouble(OrderOpenPrice()+tp-stops,Digits) && OrderTakeProfit()!=NormalizeDouble(Ask+stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(Ask+stops,Digits),0,0);if(!mod) Print("Error modification block 5=",GetLastError());}
                  //---
                  if(last_profit>=0 && Trailing_Use==true && Bid>NormalizeDouble(Ask-TS+stops,Digits) && Ask-OrderStopLoss()>=TS && Bid-OrderOpenPrice()>=profit_level_tr && 
                     (Ask-TS)-OrderStopLoss()>=shag && OrderStopLoss()!=NormalizeDouble(Ask-TS,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Ask-TS,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 6=",GetLastError());}
                  if(last_profit>=0 && Breakeven_Use==true && Bid>=NormalizeDouble(OrderOpenPrice()+profit_level,Digits) && Bid>NormalizeDouble(OrderOpenPrice()+sl_Plus+stops,Digits)
                     && NormalizeDouble(OrderOpenPrice()+sl_Plus,Digits)>OrderStopLoss()
                     && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()+sl_Plus,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+sl_Plus,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 7=",GetLastError());}

                 }
               if(OrderType()==OP_SELL)
                 {
                  s++;n++;
                  if(SL>0 && TP>0 && OrderStopLoss()==0 && Ask<NormalizeDouble(OrderOpenPrice()+sl-stops,Digits) && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()+sl,Digits)
                     && OrderTakeProfit()==0 && Bid>NormalizeDouble(OrderOpenPrice()-tp+stops,Digits) && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()-tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+sl,Digits),NormalizeDouble(OrderOpenPrice()-tp,Digits),0,0);if(!mod) Print("Error modification block 6=",GetLastError());}

                  if(SL>0 && OrderStopLoss()==0 && Ask<NormalizeDouble(OrderOpenPrice()+sl-stops,Digits) && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()+sl,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+sl,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 7=",GetLastError());}
                  if(SL>0 && OrderStopLoss()==0 && Ask>NormalizeDouble(OrderOpenPrice()+sl-stops,Digits) && OrderStopLoss()!=NormalizeDouble(Ask+stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Ask+stops,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 8=",GetLastError());}
                  if(TP>0 && OrderTakeProfit()==0 && Bid>NormalizeDouble(OrderOpenPrice()-tp+stops,Digits) && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()-tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()-tp,Digits),0,0);if(!mod) Print("Error modification block 9=",GetLastError());}
                  if(TP>0 && OrderTakeProfit()==0 && Bid<NormalizeDouble(OrderOpenPrice()-tp+stops,Digits) && OrderTakeProfit()!=NormalizeDouble(Bid-stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(Bid-stops,Digits),0,0);if(!mod) Print("Error modification block 10=",GetLastError());}
                  //----
                  if(last_profit>=0 && Trailing_Use==true && OrderStopLoss()>0 && Bid<NormalizeDouble(Bid+TS-stops,Digits) && OrderStopLoss()-Bid>=TS && OrderOpenPrice()-Ask>=profit_level_tr && 
                     OrderStopLoss()-(Bid+TS)>=shag && OrderStopLoss()!=NormalizeDouble(Bid+TS,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid+TS,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 13=",GetLastError());}

                  if(last_profit>=0 && Trailing_Use==true && OrderStopLoss()==0 && Bid<NormalizeDouble(Bid+TS-stops,Digits) && OrderOpenPrice()-Ask>=profit_level_tr && OrderStopLoss()!=NormalizeDouble(Bid+TS,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid+TS,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 14=",GetLastError());}

                  if(last_profit>=0 && Breakeven_Use==true && OrderStopLoss()>0 && Ask<=NormalizeDouble(OrderOpenPrice()-profit_level,Digits) && Ask<NormalizeDouble(OrderOpenPrice()-sl_Plus-stops,Digits)
                     && NormalizeDouble(OrderOpenPrice()-sl_Plus,Digits)<OrderStopLoss()
                     && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-sl_Plus,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-sl_Plus,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 15=",GetLastError());}
                  if(last_profit>=0 && Breakeven_Use==true && OrderStopLoss()==0 && Ask<=NormalizeDouble(OrderOpenPrice()-profit_level,Digits) && Ask<NormalizeDouble(OrderOpenPrice()-sl_Plus-stops,Digits)
                     && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-sl_Plus,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-sl_Plus,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 16=",GetLastError());}

                 }
               if(OrderType()==OP_BUYLIMIT)
                 {
                  blimit++;

                  if(SL>0 && TP>0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),NormalizeDouble(Bid-step-sl,Digits),NormalizeDouble(Bid-step+tp,Digits),0,0);if(!mod) Print("Error modification block 11=",GetLastError());}
                  if(SL>0 && TP==0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),NormalizeDouble(Bid-step-sl,Digits),0,0,0);if(!mod) Print("Error modification block 12=",GetLastError());}
                  if(SL==0 && TP>0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),0,NormalizeDouble(Bid-step+tp,Digits),0,0);if(!mod) Print("Error modification block 13=",GetLastError());}
                  if(SL==0 && TP==0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),0,0,0,0);if(!mod) Print("Error modification=",GetLastError());}

                  if(SL>0 && sl>stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-sl,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-sl,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 14=",GetLastError());}
                  if(SL>0 && sl<=stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-stops,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 15=",GetLastError());}

                  if(TP>0 && tp>stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()+tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()+tp,Digits),0,0);if(!mod) Print("Error modification block 16=",GetLastError());}
                  if(TP>0 && tp<=stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()+stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()+stops,Digits),0,0);if(!mod) Print("Error modification block 17=",GetLastError());}
                 }
               if(OrderType()==OP_SELLLIMIT)
                 {
                  slimit++;
                  if(SL>0 && TP>0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),NormalizeDouble(Ask+step+sl,Digits),NormalizeDouble(Ask+step-tp,Digits),0,0);if(!mod) Print("Error modification 18",GetLastError());}
                  if(SL>0 && TP==0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),NormalizeDouble(Ask+step+sl,Digits),0,0,0);if(!mod) Print("Error modification block 19=",GetLastError());}
                  if(SL==0 && TP>0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),0,NormalizeDouble(Ask+step-tp,Digits),0,0);if(!mod) Print("Error modification block 20=",GetLastError());}
                  if(SL==0 && TP==0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),0,0,0,0);if(!mod) Print("Error modification block 27=",GetLastError());}

                  if(SL>0 && sl>stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()+sl,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+sl,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 21=",GetLastError());}
                  if(SL>0 && sl<=stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()+stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+stops,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 22=",GetLastError());}

                  if(TP>0 && tp>stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()-tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()-tp,Digits),0,0);if(!mod) Print("Error modification block 23=",GetLastError());}
                  if(TP>0 && tp<=stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()-stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()-stops,Digits),0,0);if(!mod) Print("Error modification block 24=",GetLastError());}
                 }
               if(OrderType()==OP_BUYSTOP)
                 {
                  bs++;

                  if(SL>0 && TP>0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),NormalizeDouble(Ask+step-sl,Digits),NormalizeDouble(Ask+step+tp,Digits),0,0);if(!mod) Print("Error modification block 11=",GetLastError());}
                  if(SL>0 && TP==0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),NormalizeDouble(Ask+step-sl,Digits),0,0,0);if(!mod) Print("Error modification block 12=",GetLastError());}
                  if(SL==0 && TP>0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),0,NormalizeDouble(Ask+step+tp,Digits),0,0);if(!mod) Print("Error modification block 13=",GetLastError());}
                  if(SL==0 && TP==0 && OrderOpenPrice()-Ask>step && OrderOpenPrice()-(Ask+step)>step_interval && OrderOpenPrice()!=NormalizeDouble(Ask+step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Ask+step,Digits),0,0,0,0);if(!mod) Print("Error modification=",GetLastError());}

                  if(SL>0 && sl>stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-sl,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-sl,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 14=",GetLastError());}
                  if(SL>0 && sl<=stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()-stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()-stops,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 15=",GetLastError());}

                  if(TP>0 && tp>stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()+tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()+tp,Digits),0,0);if(!mod) Print("Error modification block 16=",GetLastError());}
                  if(TP>0 && tp<=stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()+stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()+stops,Digits),0,0);if(!mod) Print("Error modification block 17=",GetLastError());}
                 }
               if(OrderType()==OP_SELLSTOP)
                 {
                  ss++;
                  if(SL>0 && TP>0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),NormalizeDouble(Bid-step+sl,Digits),NormalizeDouble(Bid-step-tp,Digits),0,0);if(!mod) Print("Error modification 18",GetLastError());}
                  if(SL>0 && TP==0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),NormalizeDouble(Bid-step+sl,Digits),0,0,0);if(!mod) Print("Error modification block 19=",GetLastError());}
                  if(SL==0 && TP>0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),0,NormalizeDouble(Bid-step-tp,Digits),0,0);if(!mod) Print("Error modification block 20=",GetLastError());}
                  if(SL==0 && TP==0 && Bid-OrderOpenPrice()>step && (Bid-step)-OrderOpenPrice()>step_interval && OrderOpenPrice()!=NormalizeDouble(Bid-step,Digits))
                    {bool mod=OrderModify(OrderTicket(),NormalizeDouble(Bid-step,Digits),0,0,0,0);if(!mod) Print("Error modification block 27=",GetLastError());}

                  if(SL>0 && sl>stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()+sl,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+sl,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 21=",GetLastError());}
                  if(SL>0 && sl<=stops && OrderStopLoss()!=NormalizeDouble(OrderOpenPrice()+stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(OrderOpenPrice()+stops,Digits),OrderTakeProfit(),0,0);if(!mod) Print("Error modification block 22=",GetLastError());}

                  if(TP>0 && tp>stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()-tp,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()-tp,Digits),0,0);if(!mod) Print("Error modification block 23=",GetLastError());}
                  if(TP>0 && tp<=stops && OrderTakeProfit()!=NormalizeDouble(OrderOpenPrice()-stops,Digits))
                    {bool mod=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(OrderOpenPrice()-stops,Digits),0,0);if(!mod) Print("Error modification block 24=",GetLastError());}
                 }

              }

   double buylimit_open=0,selllimit_open=0;
   int sig_slimit=0,sig_blimit=0;
   if(n==0&&blimit==0&&step>stops&&Last_Price_Limit_Use==false) {buylimit_open=NormalizeDouble(Bid-step,Digits);sig_blimit=1; }
   if(n==0&&slimit==0&&step>stops&&Last_Price_Limit_Use==false) {selllimit_open=NormalizeDouble(Ask+step,Digits);sig_slimit=1;}
   if(n==0&&blimit==0&&step<=stops&&Last_Price_Limit_Use==false) {buylimit_open=NormalizeDouble(Bid-stops,Digits);sig_blimit=1; }
   if(n==0&&slimit==0&&step<=stops&&Last_Price_Limit_Use==false) {selllimit_open=NormalizeDouble(Ask+stops,Digits);sig_slimit=1;}


   if(n==0 && blimit==0 && OP_TYPE_LIMIT==OP_BUYLIMIT && Bid>=last_limit_price+stops && Last_Price_Limit_Use==true) {buylimit_open=NormalizeDouble(last_limit_price,Digits);sig_blimit=1; }
   if(n==0 && blimit==0 && OP_TYPE_LIMIT!=OP_BUYLIMIT && Bid>=Bid-step+stops && Last_Price_Limit_Use==true) {buylimit_open=NormalizeDouble(Bid-step,Digits);sig_blimit=1; }
   if(n==0&&blimit==0&&OP_TYPE_LIMIT==OP_BUYLIMIT&&Bid<=last_limit_price+stops&&Last_Price_Limit_Use==true) {buylimit_open=NormalizeDouble(Bid-stops,Digits);sig_blimit=1; }
   if(n==0 && blimit==0 && OP_TYPE_LIMIT!=OP_BUYLIMIT && Bid<=Bid-step+stops && Last_Price_Limit_Use==true) {buylimit_open=NormalizeDouble(Bid-stops,Digits);sig_blimit=1; }

   if(n==0 && slimit==0 && OP_TYPE_LIMIT==OP_SELLLIMIT && Ask<=last_limit_price-stops && Last_Price_Limit_Use==true) {selllimit_open=NormalizeDouble(last_limit_price,Digits);sig_slimit=1; }
   if(n==0 && slimit==0 && OP_TYPE_LIMIT!=OP_SELLLIMIT && Ask<=Ask+step-stops && Last_Price_Limit_Use==true) {selllimit_open=NormalizeDouble(Ask+step,Digits);sig_slimit=1; }
   if(n==0&&slimit==0&&OP_TYPE_LIMIT==OP_SELLLIMIT&&Ask>=last_limit_price-stops&&Last_Price_Limit_Use==true) {selllimit_open=NormalizeDouble(Ask+stops,Digits);sig_slimit=1; }
   if(n==0 && slimit==0 && OP_TYPE_LIMIT!=OP_SELLLIMIT && Ask>=Ask+step-stops && Last_Price_Limit_Use==true) {selllimit_open=NormalizeDouble(Ask+stops,Digits);sig_slimit=1; }

   if(sig_slimit==1 && open_sell==true && Limits0_or_Stops1==0) {int open=OrderSend(Symbol(),OP_SELLLIMIT,Lot,selllimit_open,Slip,0,0,NULL,Magic,0,Red);if(open>0) {return;} if(open<0) {Print("OrderSend failed #",GetLastError());return;}}
   if(sig_blimit==1 && open_buy==true && Limits0_or_Stops1==0) {int open=OrderSend(Symbol(),OP_BUYLIMIT,Lot,buylimit_open,Slip,0,0,NULL,Magic,0,Blue);if(open>0) {return;} if(open<0) {Print("OrderSend failed #",GetLastError());return;}}

   double buystop_open=0,sellstop_open=0;
   int sig_bstop=0,sig_sstop=0;
   if(n==0&&bs==0&&step>stops&&Last_Price_Stops_Use==false) {buystop_open=NormalizeDouble(Ask+step,Digits);sig_bstop=1; }
   if(n==0&&ss==0&&step>stops&&Last_Price_Stops_Use==false) {sellstop_open=NormalizeDouble(Bid-step,Digits);sig_sstop=1;}
   if(n==0&&bs==0&&step<=stops&&Last_Price_Stops_Use==false) {buystop_open=NormalizeDouble(Ask+stops,Digits);sig_bstop=1; }
   if(n==0&&ss==0&&step<=stops&&Last_Price_Stops_Use==false) {sellstop_open=NormalizeDouble(Bid-stops,Digits);sig_sstop=1;}


   if(n==0 && bs==0 && OP_TYPE_STOPS==OP_BUYSTOP && Ask<=last_stop_price-stops && Last_Price_Stops_Use==true) {buystop_open=NormalizeDouble(last_stop_price,Digits);sig_bstop=1; }
   if(n==0 && bs==0 && OP_TYPE_STOPS!=OP_BUYSTOP && Ask<=Ask+step-stops && Last_Price_Stops_Use==true) {buystop_open=NormalizeDouble(Ask+step,Digits);sig_bstop=1; }
   if(n==0 && bs==0 && OP_TYPE_STOPS==OP_BUYSTOP && Ask>=last_stop_price+stops && Last_Price_Stops_Use==true) {buystop_open=NormalizeDouble(Ask+stops,Digits);sig_bstop=1; }
   if(n==0 && bs==0 && OP_TYPE_STOPS!=OP_BUYSTOP && Ask>=Ask+step-stops && Last_Price_Stops_Use==true) {buystop_open=NormalizeDouble(Ask+stops,Digits);sig_bstop=1; }

   if(n==0 && ss==0 && OP_TYPE_STOPS==OP_SELLSTOP && Bid>=last_stop_price+stops && Last_Price_Stops_Use==true) {sellstop_open=NormalizeDouble(last_stop_price,Digits);sig_sstop=1; }
   if(n==0 && ss==0 && OP_TYPE_STOPS!=OP_SELLSTOP && Bid>=Bid-step+stops && Last_Price_Stops_Use==true) {sellstop_open=NormalizeDouble(Bid-step,Digits);sig_sstop=1; }
   if(n==0 && ss==0 && OP_TYPE_STOPS==OP_SELLSTOP && Bid<=last_stop_price+stops && Last_Price_Stops_Use==true) {sellstop_open=NormalizeDouble(Bid-stops,Digits);sig_sstop=1; }
   if(n==0 && ss==0 && OP_TYPE_STOPS!=OP_SELLSTOP && Bid<=Bid-step+stops && Last_Price_Stops_Use==true) {sellstop_open=NormalizeDouble(Bid-stops,Digits);sig_sstop=1; }

   if(sig_sstop==1 && open_sell==true && Limits0_or_Stops1==1) {int open=OrderSend(Symbol(),OP_SELLSTOP,Lot,sellstop_open,Slip,0,0,NULL,Magic,0,Red);if(open>0) {return;} if(open<0) {Print("OrderSend failed #",GetLastError());return;}}
   if(sig_bstop==1 && open_buy==true && Limits0_or_Stops1==1) {int open=OrderSend(Symbol(),OP_BUYSTOP,Lot,buystop_open,Slip,0,0,NULL,Magic,0,Blue);if(open>0) {return;} if(open<0) {Print("OrderSend failed #",GetLastError());return;}}

   for(int lim_del=total-1; lim_del>=0; lim_del--)
      if(OrderSelect(lim_del,SELECT_BY_POS))
         if(OrderSymbol()==Symbol())
            if(OrderMagicNumber()==Magic)
               if(OrderType()>1)
                 {
                  if(n>0) {int cl=OrderDelete(OrderTicket());} // удаляем отложку при появлении рыночного ордера
                 }

  }
//+------------------------------------------------------------------+
