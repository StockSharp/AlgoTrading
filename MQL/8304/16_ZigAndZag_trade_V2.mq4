//+------------------------------------------------------------------+
//|                                             ZigAndZag_trader.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
#include <stdlib.mqh>
//---- input parameters
extern double    Lots=0.1;//торгуемый лот
extern int       ZZbar=1;//баров назад с которого берем сигнал к торговле
extern int       Closebar=3;//баров назад с которого берем сигнал к закрытию 
extern int       CloseBars=5;//запрет закрытия на кол-во баров с момента откр ордера 
extern int       Maxord=10;//количество торгуемых ордеров(пока оставить 1)воможно будет мультиордерная система
extern int       Sl=0;//стоплос подбирается при оптимизации
extern int       Tp=0;//тейк подбирается при оптимизации
extern int       bu=0;//
extern int       Rew=0;//реверс позиций
extern int       ClosePos=1;//Разрешает закрытие позиций по сигналу индикатора
extern bool      Drive=false;
extern bool      _Bu=false;
extern bool      Autolot=true;//включение - отключение автолота
extern int       magic=78977;//магик
//-----------------------
static int prevtime = 0 ;
bool buy,sell,close,NumberOfTry=3,UseSound=false;
bool first=true,newday=true;
double MinLot, MaxLot, MarginMinLot;   
int    MinLotDgts,Slippage=3,period,GrossPeriod,GrossTrand=0;
string NameFileSound = "expert.wav";   // Наименование звукового файла
color  clOpenBuy     = Blue;           // Цвет значка открытия Buy
color  clOpenSell    = Red;            // Цвет значка открытия Sell
bool   MarketWatch=false;
bool   gbDisabled    = False;          // Блокировка
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
void FirstCalc()
{
// 
// Предстартовый расчет. Выполняется один раз - при запуске
// (и каждый раз после сбоя связи).
//
      first=false;
//+--- Минимальный и максимальный размеры лотов
   MinLot=MarketInfo(Symbol(),MODE_MINLOT);
   if(MinLot<0.1) MinLotDgts=2; // размерность минлота   
   else
   {
      if(MinLot<1.0) MinLotDgts=1;
      else MinLotDgts=0;
   }
   MaxLot=MarketInfo(Symbol(),MODE_MAXLOT);   
//----
   return;
}
//+------------------------------------------------------------------+
void init(){
   if(Period()==5){period= 15;GrossPeriod=30;}
   if(Period()==15){period= 30;GrossPeriod=60;}
   if(Period()==30){period= 60;GrossPeriod=240;}
   if(Period()==60){period= 240;GrossPeriod=1440;}
   if(Period()==240){period= 1440;GrossPeriod=10080;}
   if(Period()==1440){period= 10080;GrossPeriod=43200;}
   if(Period()<5||Period()>10080){GrossPeriod=0;}return;}
//-------------------------------------------------------------------   
int start()
  {
   if(NewDay()){newday=true;}
   if(GrossPeriod==0){Comment("Не правильный период, Рабoта остановленна!"); return(0);}
   if(first==true) FirstCalc();
// Ждем, когда сформируется новый бар
   if (Time[0] == prevtime) return(0);
      prevtime = Time[0];  
buy=false;sell=false;         close=false;
//------------Перевод в безубыток------------------------------------+
if(OrdersTotal()>0&&_Bu){
  for(int x=0;x<OrdersTotal();x++){
   if(OrderSelect(x,SELECT_BY_POS,MODE_TRADES)){
    Bu(OrderTicket());}}}
//------------Читаем инфу из индикатора------------------------------+
 //if(iCustom(NULL,GrossPeriod,"ZigAndZag",5,ZZbar)!=0){GrossTrand=1;SetArrow(241,3,true,Red);}
 //if(iCustom(NULL,GrossPeriod,"ZigAndZag",6,ZZbar)!=0){GrossTrand=(-1);SetArrow(242,3,false,Red);}
 if(iCustom(NULL,period,"ZigAndZag",4,Closebar)!=0){close=true;SetArrow(251,3,false,White);}
 if(iCustom(NULL,GrossPeriod,"ZigAndZag",5,ZZbar)!=0){buy=true;SetArrow(241,1,true,Blue);}
 if(iCustom(NULL,GrossPeriod,"ZigAndZag",6,ZZbar)!=0){sell=true;SetArrow(242,1,false,Red);}
 //Comment(close+"\n"+buy+"\n"+sell+"\n"+OrdersTotal());
 Comment(GrossPeriod+"\n"+GrossTrand);
//------------Открытие ордеров---------------------------------------+
 if(Rew==0&&newday){//OpenPosition("",OP_BUY,Lots,Lots,Tp,magic)
 if(buy&&OrdersTotal()<Maxord){OpenPosition("",OP_BUY,Lots,Sl,Tp,magic);buy=false;newday=false; } 
 if(sell&&OrdersTotal()<Maxord){OpenPosition("",OP_SELL,Lots,Sl,Tp,magic);sell=false;newday=false; }}
 if(Rew>0&&newday){
 if(buy&&OrdersTotal()<Maxord){OpenPosition("",OP_SELL,Lots,Sl,Tp,magic);sell=false;newday=false; } 
 if(sell&&OrdersTotal()<Maxord){OpenPosition("",OP_BUY,Lots,Sl,Tp,magic);buy=false; newday=false;}} 
//------------Закрытие ордеров---------------------------------------+ 
 if(OrdersTotal()>0){
  if(((GetPosType()==0&&sell)||(GetPosType()==1&&buy))){
  for(int i=0;i<OrdersTotal();i++){
   if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)){
    if(ClosePos==0){
     if(iBarShift(NULL,CloseBars,OrderOpenTime())>0){del(OrderTicket());close=false;}}
    if(ClosePos>0){del(OrderTicket());close=false;} 
     }}}} 
//----
   return(0);
  }
//-------------------------------------------------------------------+
int GetPosType(){
 for(int i=0;i<OrdersTotal();i++){
  if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)){return(OrderType());}}}
bool NewDay(){if(TimeHour(TimeCurrent())==0&&TimeMinute(TimeCurrent())<10){return(true);}return(false);}     
//+------------------------------------------------------------------+
void SetArrow(int kod,int razm,bool ApDn,color col){if(Drive){
 string Name=TimeToStr(TimeLocal(),TIME_DATE|TIME_MINUTES);
 if(!ApDn){double prs=Ask+20*Point;}else{prs=Bid-20*Point;}
 //Comment(prs+"\n"+Name);
 if(!ObjectCreate(Name,OBJ_ARROW,0,0,0,0,0,0,0)){Comment(GetLastError());return;}
 if(!ObjectSet(Name,OBJPROP_ARROWCODE,kod)){Comment(GetLastError());}
 if(!ObjectSet(Name,OBJPROP_STYLE,DRAW_ARROW)){Comment(GetLastError());}
 if(!ObjectSet(Name,OBJPROP_TIME1,Time[0])){Comment(GetLastError());}
 if(!ObjectSet(Name,OBJPROP_PRICE1,prs)){Comment(GetLastError());}
 if(!ObjectSet(Name,OBJPROP_WIDTH,razm)){Comment(GetLastError());} 
 if(!ObjectSet(Name,OBJPROP_COLOR,col)){Comment(GetLastError());}}

 return;}

//-----------------------------------------------------------------------------+
//  Функция закрывает рыночный ордер с переданным ей магиком                   |
//-----------------------------------------------------------------------------+
int del(int ticket){
int err;
GetLastError();//обнуляем ошику
OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES);
string symbol = OrderSymbol();
//----------------------------
if(OrderType()==OP_BUY){
 RefreshRates();
  double prise = MarketInfo(symbol,MODE_BID);
   OrderClose(ticket,OrderLots(),prise,3,Green);
    err = GetLastError();}
if(OrderType()==OP_SELL){
 RefreshRates();
  prise = MarketInfo(symbol,MODE_ASK);
   OrderClose(ticket,OrderLots(),prise,3,Green);
    err = GetLastError();}
//---------------------------        
if (err == 0&&UseSound){PlaySound("expert.wav");}
 else{PlaySound("timeout.wav");Print(err);} 
  return(err);}
//-------------------------------------------------------------------+
// функция переводит СЛ в бу или на нужный уровень                   +
//+------------------------------------------------------------------+
 int Bu(int ti=0)
 {
  int err;double sl;
  if(ti==0){return(-1);}
  if(OrderSelect(ti,SELECT_BY_TICKET,MODE_TRADES)&&(OrderCloseTime()==0)){
    if(bu==0){sl=OrderOpenPrice();}else{
      if(OrderType()==OP_BUY) {sl=NormalizeDouble((OrderOpenPrice()+bu*Point),Digits);}
       if(OrderType()==OP_SELL){sl=NormalizeDouble((OrderOpenPrice()-bu*Point),Digits);}} 
    if(OrderType()==OP_BUY){
     if(sl<MarketInfo(OrderSymbol(),MODE_BID)-(MarketInfo(OrderSymbol(),MODE_STOPLEVEL)*Point)&&sl!=OrderStopLoss()){ 
      err = OrderModify(ti,OrderOpenPrice(),sl,OrderTakeProfit(),0,White);}} 
    if(OrderType()==OP_SELL){
     if(sl>MarketInfo(OrderSymbol(),MODE_ASK)+(MarketInfo(OrderSymbol(),MODE_STOPLEVEL)*Point)&&sl!=OrderStopLoss()){ 
      err = OrderModify(ti,OrderOpenPrice(),sl,OrderTakeProfit(),0,White);}}}      
 return(err);
 }
//--------------------------------------------------------------------+  
//-----------------------------------------------------------------------------+
//___________________КИМУ_______РЕСПЕКТ________И_____УВАЖУХА___________________|
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 01.09.2005                                                     |
//|  Описание : Вывод сообщения в коммент и в журнал                           |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    m - текст сообщения                                                     |
//+----------------------------------------------------------------------------+
void Message(string m) {
  Comment(m);
  if (StringLen(m)>0) Print(m);
}

//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 28.11.2006                                                     |
//|  Описание : Модификация одного предварительно выбранного ордера.           |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    pp - цена установки ордера                                              |
//|    sl - ценовой уровень стопа                                              |
//|    tp - ценовой уровень тейка                                              |
//|    ex - дата истечения                                                     |
//+----------------------------------------------------------------------------+
void ModifyOrder(double pp=-1, double sl=0, double tp=0, datetime ex=0) {
  bool   fm;
  color  cl=Red;
  double op, pa, pb, os, ot;
  int    dg=MarketInfo(OrderSymbol(), MODE_DIGITS), er, it;

  if (pp<=0) pp=OrderOpenPrice();
  if (sl<0 ) sl=OrderStopLoss();
  if (tp<0 ) tp=OrderTakeProfit();
  
  pp=NormalizeDouble(pp, dg);
  sl=NormalizeDouble(sl, dg);
  tp=NormalizeDouble(tp, dg);
  op=NormalizeDouble(OrderOpenPrice() , dg);
  os=NormalizeDouble(OrderStopLoss()  , dg);
  ot=NormalizeDouble(OrderTakeProfit(), dg);

  if (pp!=op || sl!=os || tp!=ot) {
    for (it=1; it<=NumberOfTry; it++) {
      if (!IsTesting() && (!IsExpertEnabled() || IsStopped())) break;
      while (!IsTradeAllowed()) Sleep(5000);
      RefreshRates();
      fm=OrderModify(OrderTicket(), pp, sl, tp, ex, cl);
      if (fm) {
        if (UseSound) PlaySound(NameFileSound); break;
      } else {
        er=GetLastError();
        pa=MarketInfo(OrderSymbol(), MODE_ASK);
        pb=MarketInfo(OrderSymbol(), MODE_BID);
        Print("Error(",er,") modifying order: ",ErrorDescription(er),", try ",it);
        Print("Ask=",pa,"  Bid=",pb,"  sy=",OrderSymbol(),
              "  op="+GetNameOP(OrderType()),"  pp=",pp,"  sl=",sl,"  tp=",tp);
        Sleep(1000*10);
      }
    }
  }
}

//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 06.03.2008                                                     |
//|  Описание : Возвращает флаг существования позиций                          |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любая позиция)                  |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//|    ot - время открытия             ( 0   - любое время открытия)           |
//+----------------------------------------------------------------------------+
bool ExistPositions(string sy="", int op=-1, int mn=-1, datetime ot=0) {
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (ot<=OrderOpenTime()) return(True);
            }
          }
        }
      }
    }
  }
  return(False);
}
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 01.09.2005                                                     |
//|  Описание : Возвращает наименование торговой операции                      |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    op - идентификатор торговой операции                                    |
//+----------------------------------------------------------------------------+
string GetNameOP(int op) {
  switch (op) {
    case OP_BUY      : return("Buy");
    case OP_SELL     : return("Sell");
    case OP_BUYLIMIT : return("BuyLimit");
    case OP_SELLLIMIT: return("SellLimit");
    case OP_BUYSTOP  : return("BuyStop");
    case OP_SELLSTOP : return("SellStop");
    default          : return("Unknown Operation");
  }
}
//-----------------------------------------------------------------------------
double GetOrderLot(){
   for (int i=0; i<OrdersTotal(); i++){
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)){double lot = OrderLots();}}
     if(lot==0){lot=Lots;}else{lot=lot+Lots;}
     return(lot);}
     
  
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 10.04.2008                                                     |
//|  Описание : Открывает позицию по рыночной цене.                            |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (NULL или "" - текущий символ)          |
//|    op - операция                                                           |
//|    ll - лот                                                                |
//|    sl - уровень стоп                                                       |
//|    tp - уровень тейк                                                       |
//|    mn - MagicNumber                                                        |
//+----------------------------------------------------------------------------+
void OpenPosition(string sy, int op, double ll, double Sl=0, double Tp=0, int mn=0) {
  
  color    clOpen;
  datetime ot;
  double   pp, pa, pb, sl, tp;
  int      dg, err, it, ticket=0;
  string   lsComm="";
  if(Autolot){ll=GetOrderLot();}
  
  if (op<1){
   if (Sl>0) sl=MarketInfo(Symbol(),MODE_ASK)-Sl*Point; else sl=0;
   if (Tp>0) tp=MarketInfo(Symbol(),MODE_ASK)+Tp*Point; else tp=0;
  }else{
   if (Sl>0) sl=MarketInfo(Symbol(),MODE_BID)+Sl*Point; else sl=0;
   if (Tp>0) tp=MarketInfo(Symbol(),MODE_BID)-Tp*Point; else tp=0;
   }   
  if (sy=="" || sy=="0") sy=Symbol();
  if (op==OP_BUY) clOpen=clOpenBuy; else clOpen=clOpenSell;
  for (it=1; it<=NumberOfTry; it++) {
    if (!IsTesting() && IsStopped()) {
      Print("OpenPosition(): Остановка работы функции");
      break;
    }
    while (!IsTradeAllowed()) Sleep(5000);
    RefreshRates();
    dg=MarketInfo(sy, MODE_DIGITS);
    pa=MarketInfo(sy, MODE_ASK);
    pb=MarketInfo(sy, MODE_BID);
    if (op==OP_BUY) pp=pa; else pp=pb;
    pp=NormalizeDouble(pp, dg);
    ot=TimeCurrent();
    if (MarketWatch)
      ticket=OrderSend(sy, op, ll, pp, Slippage, 0, 0, lsComm, mn, 0, clOpen);
    else
      ticket=OrderSend(sy, op, ll, pp, Slippage, sl, tp, lsComm, mn, 0, clOpen);
    if (ticket>0) {
      if (UseSound) PlaySound(NameFileSound); break;
    } else {
      err=GetLastError();
      if (pa==0 && pb==0) Message("Проверьте в Обзоре рынка наличие символа "+sy);
      // Вывод сообщения об ошибке
      Print("Error(",err,") opening position: ",ErrorDescription(err),", try ",it);
      Print("Ask=",pa," Bid=",pb," sy=",sy," ll=",ll," op=",GetNameOP(op),
            " pp=",pp," sl=",sl," tp=",tp," mn=",mn);
      // Блокировка работы советника
      if (err==2 || err==64 || err==65 || err==133) {
        gbDisabled=True; break;
      }
      // Длительная пауза
      if (err==4 || err==131 || err==132) {
        Sleep(1000*300); break;
      }
      if (err==128 || err==142 || err==143) {
        Sleep(1000*66.666);
        if (ExistPositions(sy, op, mn, ot)) {
          if (UseSound) PlaySound(NameFileSound); break;
        }
      }
      if (err==140 || err==148 || err==4110 || err==4111) break;
      if (err==141) Sleep(1000*100);
      if (err==145) Sleep(1000*17);
      if (err==146) while (IsTradeContextBusy()) Sleep(1000*11);
      if (err!=135) Sleep(1000*7.7);
    }
  }
  if (MarketWatch && ticket>0 && (sl>0 || tp>0)) {
    if (OrderSelect(ticket, SELECT_BY_TICKET)) ModifyOrder(-1, sl, tp);
  }
}
//+----------------------------------------------------------------------------+



