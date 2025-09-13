//+------------------------------------------------------------------+
//|                                            ytg_Parabolic_exp.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern int timeframe  = 240;
extern double step    = 0.009;
extern double maximum = 0.2;

extern int    StopLoss         = 500;  // Размер стопа в пунктах
extern int    TakeProfit       = 500; // Размер тейка в пунктах
extern double Lotsi = 0.1;
extern int    MagicNumber      = 28081975;   //Магическое число ордеров

color clOpenBuy                = LightBlue;     // Цвет значка открытия покупки
color clOpenSell               = LightCoral;    // Цвет значка открытия продажи
color clCloseBuy               = Blue;     // Цвет значка закрытия покупки
color clCloseSell              = Coral;    // Цвет значка закрытия продажи
extern int    Slippage         = 30;            // Проскальзывание цены
extern int    NumberOfTry      = 5;             // Количество торговых попыток
bool   UseSound                = True;          // Использовать звуковой сигнал
string NameFileSound           = "expert.wav";  // Наименование звукового файла
int    NumberAccount           = 0;             // Номер торгового счёта
bool   ShowComment             = True;          // Показывать комментарий
//------- Глобальные переменные советника -------------------------------------+
bool  gbDisabled               = False;         // Флаг блокировки советника
bool  gbNoInit                 = False;         // Флаг неудачной инициализации
//------- Подключение внешних модулей -----------------------------------------+
#include <stdlib.mqh>        // Стандартная библиотека МТ4

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   int    dg=MarketInfo(OrderSymbol(), MODE_DIGITS);  
   double parab = NormalizeDouble(iSAR(Symbol(),timeframe,step,maximum,0),dg);   
   double sl=0, tp=0,ti=0,new_sl,new_tp;
   double SPREAD    = MarketInfo(Symbol(),MODE_SPREAD);//Спрэд в пунктах
   double STOPLEVEL    = MarketInfo(Symbol(),MODE_STOPLEVEL);   
   double low = iLow(Symbol(),timeframe,0);
   double high = iHigh(Symbol(),timeframe,0);   
   double mp=MarketInfo(Symbol(), MODE_POINT);
   double pa=MarketInfo(Symbol(), MODE_ASK);
   double pb=MarketInfo(Symbol(), MODE_BID);
   
//----
   if(NevBar())
   {
//-------------------------------------------------------------buy
    if(!ExistOrders(NULL,OP_BUYLIMIT,MagicNumber) && parab<low && pb>parab+STOPLEVEL*mp)//ордера нет
    {//устанавливаем     
     if (StopLoss  >0) sl=parab-StopLoss*mp;   else sl=0;
     if (TakeProfit>0) tp=parab+TakeProfit*mp; else tp=0;     
     SetOrder(NULL,OP_BUYLIMIT,Lotsi,parab,sl,tp,MagicNumber,0);
    }
    if(ExistOrders(NULL,OP_BUYLIMIT,MagicNumber) && pb>parab+STOPLEVEL*mp)//ордер есть
    {//модифицируем
     ti = TicketPos(NULL,OP_BUYLIMIT,MagicNumber);
     if (OrderSelect(ti, SELECT_BY_TICKET))
       {
        if (StopLoss  >0) sl=parab-StopLoss*mp;   else sl=0;
        if (TakeProfit>0) tp=parab+TakeProfit*mp; else tp=0;
        ModifyOrder(parab,sl,tp,Turquoise);
       }
    }
//--------------------------------------------------------------sell
    if(!ExistOrders(NULL,OP_SELLLIMIT,MagicNumber) && parab>high && pa<parab-STOPLEVEL*mp)//ордера нет
    {//устанавливаем     
     if (StopLoss  >0) sl=parab+StopLoss*mp;   else sl=0;
     if (TakeProfit>0) tp=parab-TakeProfit*mp; else tp=0;     
     SetOrder(NULL,OP_SELLLIMIT,Lotsi,parab,sl,tp,MagicNumber,0);
    }
    if(ExistOrders(NULL,OP_SELLLIMIT,MagicNumber) && pa<parab-STOPLEVEL*mp)//ордер есть
    {//модифицируем
     ti = TicketPos(NULL,OP_SELLLIMIT,MagicNumber);
     if (OrderSelect(ti, SELECT_BY_TICKET))
       {
        if (StopLoss  >0) sl=parab+StopLoss*mp;   else sl=0;
        if (TakeProfit>0) tp=parab-TakeProfit*mp; else tp=0;
        ModifyOrder(parab,sl,tp,Turquoise);
       }
    }
//-----------------------------------------------------------------
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//+----------------------------------------------------------------------------+
// Функция контроля нового бара                                                |
//-----------------------------------------------------------------------------+
bool NevBar(){
   static int PrevTime=0;
   if (PrevTime==iTime(Symbol(),timeframe,0)) return(false);
   PrevTime=iTime(Symbol(),timeframe,0);
   return(true);}
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 12.03.2008                                                     |
//|  Описание : Возвращает флаг существования ордеров.                         |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любой ордер)                    |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//|    ot - время открытия             ( 0   - любое время установки)          |
//+----------------------------------------------------------------------------+
bool ExistOrders(string sy="", int op=-1, int mn=-1, datetime ot=0) {
  int i, k=OrdersTotal(), ty;
 
  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      ty=OrderType();
      if (ty>1 && ty<6) {
        if ((OrderSymbol()==sy || sy=="") && (op<0 || ty==op)) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (ot<=OrderOpenTime()) return(True);
          }
        }
      }
    }
  }
  return(False);
}
//+----------------------------------------------------------------------------+
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 13.03.2008                                                     |
//|  Описание : Установка ордера.                                              |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (NULL или "" - текущий символ)          |
//|    op - операция                                                           |
//|    ll - лот                                                                |
//|    pp - цена                                                               |
//|    sl - уровень стоп                                                       |
//|    tp - уровень тейк                                                       |
//|    mn - Magic Number                                                       |
//|    ex - Срок истечения                                                     |
//+----------------------------------------------------------------------------+
void SetOrder(string sy, int op, double ll, double pp,
              double sl=0, double tp=0, int mn=0, datetime ex=0) {
  color    clOpen;
  datetime ot;
  double   pa, pb, mp;
  int      err, it, ticket, msl;
  string   lsComm=WindowExpertName()+" "+GetNameTF(Period());

  if (sy=="" || sy=="0") sy=Symbol();
  msl=MarketInfo(sy, MODE_STOPLEVEL);
  if (op==OP_BUYLIMIT || op==OP_BUYSTOP) clOpen=clOpenBuy; else clOpen=clOpenSell;
  if (ex>0 && ex<TimeCurrent()) ex=0;
  for (it=1; it<=NumberOfTry; it++) {
    if (!IsTesting() && (!IsExpertEnabled() || IsStopped())) {
      Print("SetOrder(): Остановка работы функции");
      break;
    }
    while (!IsTradeAllowed()) Sleep(5000);
    RefreshRates();
    ot=TimeCurrent();
    ticket=OrderSend(sy, op, ll, pp, Slippage, sl, tp, lsComm, mn, ex, clOpen);
    if (ticket>0) {
      if (UseSound) PlaySound(NameFileSound); break;
    } else {
      err=GetLastError();
      if (err==128 || err==142 || err==143) {
        Sleep(1000*66);
        if (ExistOrders(sy, op, mn, ot)) {
          if (UseSound) PlaySound(NameFileSound); break;
        }
        Print("Error(",err,") set order: ",ErrorDescription(err),", try ",it);
        continue;
      }
      mp=MarketInfo(sy, MODE_POINT);
      pa=MarketInfo(sy, MODE_ASK);
      pb=MarketInfo(sy, MODE_BID);
      // Неправильные стопы
      if (err==130) {
        switch (op) {
          case OP_BUYLIMIT:
            if (pp>pa-msl*mp) pp=pa-msl*mp;
            if (sl>pp-(msl+1)*mp) sl=pp-(msl+1)*mp;
            if (tp>0 && tp<pp+(msl+1)*mp) tp=pp+(msl+1)*mp;
            break;
          case OP_BUYSTOP:
            if (pp<pa+(msl+1)*mp) pp=pa+(msl+1)*mp;
            if (sl>pp-(msl+1)*mp) sl=pp-(msl+1)*mp;
            if (tp>0 && tp<pp+(msl+1)*mp) tp=pp+(msl+1)*mp;
            break;
          case OP_SELLLIMIT:
            if (pp<pb+msl*mp) pp=pb+msl*mp;
            if (sl>0 && sl<pp+(msl+1)*mp) sl=pp+(msl+1)*mp;
            if (tp>pp-(msl+1)*mp) tp=pp-(msl+1)*mp;
            break;
          case OP_SELLSTOP:
            if (pp>pb-msl*mp) pp=pb-msl*mp;
            if (sl>0 && sl<pp+(msl+1)*mp) sl=pp+(msl+1)*mp;
            if (tp>pp-(msl+1)*mp) tp=pp-(msl+1)*mp;
            break;
        }
        Print("SetOrder(): Скорректированы ценовые уровни");
      }
      Print("Error(",err,") set order: ",ErrorDescription(err),", try ",it);
      Print("Ask=",pa,"  Bid=",pb,"  sy=",sy,"  ll=",ll,"  op=",GetNameOP(op),
            "  pp=",pp,"  sl=",sl,"  tp=",tp,"  mn=",mn);
      if (pa==0 && pb==0) Message("SetOrder(): Проверьте в обзоре рынка наличие символа "+sy);
      // Блокировка работы советника
      if (err==2 || err==64 || err==65 || err==133) {
        gbDisabled=True; break;
      }
      // Длительная пауза
      if (err==4 || err==131 || err==132) {
        Sleep(1000*300); break;
      }
      // Слишком частые запросы (8) или слишком много запросов (141)
      if (err==8 || err==141) Sleep(1000*100);
      if (err==139 || err==140 || err==148) break;
      // Ожидание освобождения подсистемы торговли
      if (err==146) while (IsTradeContextBusy()) Sleep(1000*11);
      // Обнуление даты истечения
      if (err==147) {
        ex=0; continue;
      }
      if (err!=135 && err!=138) Sleep(1000*7.7);
    }
  }
}
//+----------------------------------------------------------------------------+
//|  Описание : Возвращает наименование таймфрейма                             |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    TimeFrame - таймфрейм (количество секунд)      (0 - текущий ТФ)         |
//+----------------------------------------------------------------------------+
string GetNameTF(int TimeFrame=0) {
  if (TimeFrame==0) TimeFrame=Period();
  switch (TimeFrame) {
    case PERIOD_M1:  return("M1");
    case PERIOD_M5:  return("M5");
    case PERIOD_M15: return("M15");
    case PERIOD_M30: return("M30");
    case PERIOD_H1:  return("H1");
    case PERIOD_H4:  return("H4");
    case PERIOD_D1:  return("Daily");
    case PERIOD_W1:  return("Weekly");
    case PERIOD_MN1: return("Monthly");
    default:         return("UnknownPeriod");
  }
}
//|  Описание : Возвращает наименование торговой операции                      |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    op - идентификатор торговой операции                                    |
//+----------------------------------------------------------------------------+
string GetNameOP(int op) {
  switch (op) {
    case OP_BUY      : return("Buy");
    case OP_SELL     : return("Sell");
    case OP_BUYLIMIT : return("Buy Limit");
    case OP_SELLLIMIT: return("Sell Limit");
    case OP_BUYSTOP  : return("Buy Stop");
    case OP_SELLSTOP : return("Sell Stop");
    default          : return("Unknown Operation");
  }
}
//+----------------------------------------------------------------------------+
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
//|    cl - цвет значка модификации                                            |
//+----------------------------------------------------------------------------+
void ModifyOrder(double pp=-1, double sl=0, double tp=0, color cl=CLR_NONE) {
  bool   fm;
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
      fm=OrderModify(OrderTicket(), pp, sl, tp, 0, cl);
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
//|  Версия   : 19.02.2008                                                     |
//|  Описание : Возвращает тикет последней открытой позиции или -1             |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любая позиция)                  |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//+----------------------------------------------------------------------------+
int TicketPos(string sy="", int op=-1, int mn=-1) {
  datetime o;
  int      i, k=OrdersTotal(), r=-1;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUYLIMIT || OrderType()==OP_SELLLIMIT) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (o<OrderOpenTime()) {
                o=OrderOpenTime();
                r=OrderTicket();
              }
            }
          }
        }
      }
    }
  }
  return(r);
}