//+------------------------------------------------------------------+
//|                                                     OpenTime.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern string _____1_____      = "Настройки закрытия позиции";
extern bool   TimeClose        = True;     // Время закрытия позиции позиции
extern string CloseTime        = "20:50";  // Время закрытия позиции
extern bool   Trailing         = False;    // Включить траление позиций
extern double TrailingStop     = 300;      // Размер трала
extern int    TrailingStep     = 3;        // Шаг трала

extern string _____2_____      = "Настройки открытия позиции";
extern string TimeTrade        = "18:50";  // Время открытия позиции
extern int    Duration         = 300;      // Продолжительность в секундах
extern bool   Sell             = True;     // True-Sell
extern bool   Buy              = False;    // True-Buy
extern double Lots             = 0.1;      // Размер лота
extern int    StopLoss         = 0;        // Размер стопа в пунктах
extern int    TakeProfit       = 0;        // Размер тейка в пунктах

extern string _____3_____      = " Параметры советника";
extern int    MagicNumber      = 89888;    //Магическое число ордеров
int    NumberAccount           = 0;        // Номер торгового счёта
bool   UseSound                = True;     // Использовать звуковой сигнал
string NameFileSound           = "expert.wav"; // Наименование звукового файла
bool   ShowComment             = True;     // Показывать комментарий
bool   MarketWatch             = False;    // Запросы под исполнение "Market Watch".
extern int    Slippage         = 3;        // Проскальзывание цены
extern int    NumberOfTry      = 5;        // Количество торговых попыток
                                                
//------- Глобальные переменные советника -------------------------------------+
bool  gbDisabled = False;         // Флаг блокировки советника
bool  gbNoInit   = False;         // Флаг неудачной инициализации
color clOpenBuy  = LightBlue;     // Цвет значка открытия покупки
color clOpenSell = LightCoral;    // Цвет значка открытия продажи

//------- Подключение внешних модулей -----------------------------------------+
#include <stdlib.mqh>        // Стандартная библиотека МТ4


//+----------------------------------------------------------------------------+
//|                                                                            |
//|  ПРЕДОПРЕДЕЛЁННЫЕ ФУНКЦИИ                                                  |
//|                                                                            |
//+----------------------------------------------------------------------------+
//|  Функция инициализации                                                     |
//+----------------------------------------------------------------------------+
void init() {
  gbNoInit=False;
  if (!IsTradeAllowed()) {
    Message("Для нормальной работы советника необходимо\n"+
            "Разрешить советнику торговать");
    gbNoInit=True; return;
  }
  if (!IsLibrariesAllowed()) {
    Message("Для нормальной работы советника необходимо\n"+
            "Разрешить импорт из внешних экспертов");
    gbNoInit=True; return;
  }
  if (!IsTesting()) {
    if (IsExpertEnabled()) Message("Советник будет запущен следующим тиком");
    else Message("Отжата кнопка \"Разрешить запуск советников\"");
  }
}

//+----------------------------------------------------------------------------+
//|  Функция деинициализации                                                   |
//+----------------------------------------------------------------------------+
void deinit() { if (!IsTesting()) Comment(""); }

//+----------------------------------------------------------------------------+
//|  expert start function                                                     |
//+----------------------------------------------------------------------------+
void start() { 
  
  double sl=0, tp=0;  
  if (gbDisabled) {
    Message("Критическая ошибка! Советник ОСТАНОВЛЕН!"); return;
  }
  if (gbNoInit) {
    Message("Не удалось инициализировать советник!"); return;
  }
  if (!IsTesting()) {
    if (NumberAccount>0 && NumberAccount!=AccountNumber()) {
      Comment("Торговля на счёте: "+AccountNumber()+" ЗАПРЕЩЕНА!");
      return;
    } else Comment("");
    if (ShowComment) {
      string st="CurTime="+TimeToStr(TimeCurrent(), TIME_MINUTES)
               +"  TimeTrade="+TimeTrade
               +IIFs(TimeClose,"  CloseTime="+CloseTime, "")  
               +"  Позиция="
               +IIFs(Sell,"  Sell", "")
               +IIFs(Buy,"  Buy", "")
               +"  Lots="+DoubleToStr(Lots, 1)
               +IIFs(MarketWatch, "  MarketWatch", "")
               ;
      Comment(st);
    } else Comment("");
  }
//----------------------------------------------------------------------------------+
 if(TimeClose) 
  {
   if (TimeCurrent()>=StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+CloseTime)
   && TimeCurrent()<StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+CloseTime)+Duration)
   ClosePositions(NULL,-1,MagicNumber);
  }
//----------------------------------------------------------------------------------+
 if(Trailing)SimpleTrailing(NULL, -1,MagicNumber);
//----------------------------------------------------------------------------------+  
  if (TimeCurrent()>=StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+TimeTrade)
  && TimeCurrent()<StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+TimeTrade)+Duration)
  {
    if (!ExistPositions("", OP_BUY, MagicNumber)&&Buy)
      { 
       if (StopLoss  >0) sl=Ask-StopLoss*Point;   else sl=0;
       if (TakeProfit>0) tp=Ask+TakeProfit*Point; else tp=0;
       OpenPosition(NULL, OP_BUY, Lots, sl, tp, MagicNumber);
      }
    if (!ExistPositions("", OP_SELL, MagicNumber)&&Sell)
      {
       if (StopLoss  >0) sl=Bid+StopLoss*Point;   else sl=0;
       if (TakeProfit>0) tp=Bid-TakeProfit*Point; else tp=0;
       OpenPosition(NULL, OP_SELL, Lots, sl, tp, MagicNumber);
      }
  }
}
//+----------------------------------------------------------------------------+
//|                                                                            |
//|  ПОЛЬЗОВАТЕЛЬСКИЕ ФУНКЦИИ                                                  |
//|                                                                            |
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
    case OP_BUYLIMIT : return("Buy Limit");
    case OP_SELLLIMIT: return("Sell Limit");
    case OP_BUYSTOP  : return("Buy Stop");
    case OP_SELLSTOP : return("Sell Stop");
    default          : return("Unknown Operation");
  }
}

//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 01.09.2005                                                     |
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

//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 01.02.2008                                                     |
//|  Описание : Возвращает одно из двух значений взависимости от условия.      |
//+----------------------------------------------------------------------------+
string IIFs(bool condition, string ifTrue, string ifFalse) {
  if (condition) return(ifTrue); else return(ifFalse);
}

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
  color  cl;
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
void OpenPosition(string sy, int op, double ll, double sl=0, double tp=0, int mn=0) {
  color    clOpen;
  datetime ot;
  double   pp, pa, pb;
  int      dg, err, it, ticket=0;
  string   lsComm=WindowExpertName()+" "+GetNameTF(Period());

  if (sy=="" || sy=="0") sy=Symbol();
  if (op==OP_BUY) clOpen=clOpenBuy; else clOpen=clOpenSell;
  for (it=1; it<=NumberOfTry; it++) {
    if (!IsTesting() && (!IsExpertEnabled() || IsStopped())) {
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
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 19.02.2008                                                     |
//|  Описание : Закрытие позиций по рыночной цене                              |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любая позиция)                  |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//+----------------------------------------------------------------------------+
void ClosePositions(string sy="", int op=-1, int mn=-1) {//ClosePositions(NULL,-1,MagicNumber)
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=k-1; i>=0; i--) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) ClosePosBySelect();
        }
      }
    }
  }
}
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия  : 19.02.2008                                                      |
//|  Описание: Закрытие одной предварительно выбранной позиции                 |
//+----------------------------------------------------------------------------+
void ClosePosBySelect() {
  bool   fc;
  color  clClose;
  double ll, pa, pb, pp;
  int    err, it;

  if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
    for (it=1; it<=NumberOfTry; it++) {
      if (!IsTesting() && (!IsExpertEnabled() || IsStopped())) break;
      while (!IsTradeAllowed()) Sleep(5000);
      RefreshRates();
      pa=MarketInfo(OrderSymbol(), MODE_ASK);
      pb=MarketInfo(OrderSymbol(), MODE_BID);
      if (OrderType()==OP_BUY) {
        pp=pb; clClose=Green;
      } else {
        pp=pa; clClose=Red;
      }
      ll=OrderLots();
      fc=OrderClose(OrderTicket(), ll, pp, Slippage, clClose);
      if (fc) {
        if (UseSound) PlaySound(NameFileSound); break;
      } else {
        err=GetLastError();
        if (err==146) while (IsTradeContextBusy()) Sleep(1000*11);
        Print("Error(",err,") Close ",GetNameOP(OrderType())," ",
              ErrorDescription(err),", try ",it);
        Print(OrderTicket(),"  Ask=",pa,"  Bid=",pb,"  pp=",pp);
        Print("sy=",OrderSymbol(),"  ll=",ll,"  sl=",OrderStopLoss(),
              "  tp=",OrderTakeProfit(),"  mn=",OrderMagicNumber());
        Sleep(1000*5);
      }
    }
  } else Print("Некорректная торговая операция. Close ",GetNameOP(OrderType()));
 } 
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 11.09.2008                                                     |
//|  Описание : Сопровождение позиций простым тралом                           |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   ( ""  - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   ( -1  - любая позиция)                  |
//|    mn - MagicNumber                ( -1  - любой магик)                    |
//+----------------------------------------------------------------------------+
void SimpleTrailing(string sy="", int op=-1, int mn=-1) {
  double po, pp;
  int    i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        po=MarketInfo(OrderSymbol(), MODE_POINT);
        if (mn<0 || OrderMagicNumber()==mn) {
          if (OrderType()==OP_BUY) {
            pp=MarketInfo(OrderSymbol(), MODE_BID);
            if ( pp-OrderOpenPrice()>TrailingStop*po) {
              if (OrderStopLoss()<pp-(TrailingStop+TrailingStep-1)*po) {
                ModifyOrder(-1, pp-TrailingStop*po, -1);
              }
            }
          }
          if (OrderType()==OP_SELL) {
            pp=MarketInfo(OrderSymbol(), MODE_ASK);
            if ( OrderOpenPrice()-pp>TrailingStop*po) {
              if (OrderStopLoss()>pp+(TrailingStop+TrailingStep-1)*po || OrderStopLoss()==0) {
                ModifyOrder(-1, pp+TrailingStop*po, -1);
              }
            }
          }
        }
      }
    }
  }
}  

