//+------------------------------------------------------------------+
//|                                                  MultiHedg_1.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"
//--1
extern bool      Use_Symbol1=true;
extern string    Symbol1 = "EURUSD";
extern double    Symbol1_Lot = 0.1;
//--2
extern bool      Use_Symbol2=true;
extern string    Symbol2 = "GBPUSD" ;
extern double    Symbol2_Lot = 0.2;
//--3
extern bool      Use_Symbol3=true;
extern string    Symbol3= "GBPJPY";
extern double    Symbol3_Lot = 0.3;
//--4
extern bool      Use_Symbol4=true;
extern string    Symbol4= "EURCAD";
extern double    Symbol4_Lot = 0.4;
//--5
extern bool      Use_Symbol5=true;
extern string    Symbol5= "USDCHF";
extern double    Symbol5_Lot = 0.5;
//--6
extern bool      Use_Symbol6=true;
extern string    Symbol6= "USDJPY";
extern double    Symbol6_Lot = 0.6;
//--7
extern bool      Use_Symbol7=false;
extern string    Symbol7= "USDCHF";
extern double    Symbol7_Lot = 0.7;
//--8
extern bool      Use_Symbol8=false;
extern string    Symbol8= "GBPUSD";
extern double    Symbol8_Lot = 0.8;
//--9
extern bool      Use_Symbol9=false;
extern string    Symbol9= "EURUSD";
extern double    Symbol9_Lot = 0.9;
//--10
extern bool      Use_Symbol10=false;
extern string    Symbol10= "USDJPY";
extern double    Symbol10_Lot = 1;
//----
extern string _____1_____      = "Настройки закрытия позиции";
extern bool   TimeClose        = True;     // Время закрытия позиции позиции
extern string CloseTime        = "20:50";  // Время закрытия позиции
extern bool   ClosePercent     = True;     //закрытия позиции по процентам
extern double PercentProfit    = 1.00;     // Процент профита
extern double PercentLoss      = 55.00;     // Процент убытка

extern string _____2_____      = "Настройки открытия позиции";
extern bool   Sell       = False;      // True-Sell, False-Buy
extern string TimeTrade  = "19:51";    // Время открытия позиции
extern int    Duration   = 300;        // Продолжительность в секундах

extern string _____3_____      = " Параметры советника";
 int    MagicNumber   = 28081975;
 int    NumberAccount = 0;            // Номер торгового счёта
 bool   UseSound      = True;         // Использовать звуковой сигнал
 string NameFileSound = "expert.wav"; // Наименование звукового файла
 bool   ShowComment   = True;         // Показывать комментарий
 int    Slippage      = 30;            // Проскальзывание цены
 int    NumberOfTry   = 5;            // Количество торговых попыток

//------- Глобальные переменные советника -------------------------------------+
bool  gbDisabled = False;         // Флаг блокировки советника
bool  gbNoInit   = False;         // Флаг неудачной инициализации
color clOpenBuy  = LightBlue;     // Цвет значка открытия покупки
color clOpenSell = LightCoral;    // Цвет значка открытия продажи

//------- Подключение внешних модулей -----------------------------------------+
#include <stdlib.mqh>        // Стандартная библиотека МТ4

int name_op;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
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
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
void deinit() { if (!IsTesting()) Comment("");}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  if (gbDisabled) {
    Message("Критическая ошибка! Советник ОСТАНОВЛЕН!"); return;
  }
  if (gbNoInit) {
    Message("Не удалось инициализировать советник!"); return;
  }
  if(TimeCurrent()>D'2009.04.05 00:00')
   {
    Comment("Советник ОСТАНОВЛЕН! Обратитесь к разработчику yuriytokman@gmail.com");
    return;
   }
  else Comment("");  
  if (!IsTesting()) {
    if (NumberAccount>0 && NumberAccount!=AccountNumber()) {
      Comment("Торговля на счёте: "+AccountNumber()+" ЗАПРЕЩЕНА!");
      return;
    } else Comment("");

    if (ShowComment) {
      string st="\nCurTime="+TimeToStr(TimeCurrent(), TIME_MINUTES)
               +"  TimeTrade="+TimeTrade
               +IIFs(TimeClose,"  CloseTime="+CloseTime, "")
               +IIFs(ClosePercent,"  PercentProfit="+DoubleToStr(PercentProfit, 2)
               +"  PercentLoss="+DoubleToStr(PercentLoss, 2), "")
               +"  Позиция="+GetNameOP(Sell)
               +IIFs(Use_Symbol1,"\n\nSymbol 1 = "+Symbol1, "")               //1
               +IIFs(Use_Symbol1,"  Lot= "+DoubleToStr(Symbol1_Lot,2), "")
               +IIFs(Use_Symbol2,"\nSymbol 2 = "+Symbol2, "")               //2
               +IIFs(Use_Symbol2,"  Lot= "+DoubleToStr(Symbol2_Lot,2), "")               
               +IIFs(Use_Symbol3,"\nSymbol 3 = "+Symbol3, "")               //3
               +IIFs(Use_Symbol3,"  Lot= "+DoubleToStr(Symbol3_Lot,2), "")               
               +IIFs(Use_Symbol4,"\nSymbol 4 = "+Symbol4, "")               //4
               +IIFs(Use_Symbol4,"  Lot="+DoubleToStr(Symbol4_Lot,2), "")               
               +IIFs(Use_Symbol5,"\nSymbol 5 = "+Symbol5, "")               //5
               +IIFs(Use_Symbol5,"  Lot= "+DoubleToStr(Symbol5_Lot,2), "")
               +IIFs(Use_Symbol6,"\nSymbol 6 = "+Symbol6, "")               //6
               +IIFs(Use_Symbol6,"  Lot= "+DoubleToStr(Symbol6_Lot,2), "")               
               +IIFs(Use_Symbol7,"\nSymbol 7 = "+Symbol7, "")               //7
               +IIFs(Use_Symbol7,"  Lot = "+DoubleToStr(Symbol7_Lot,2), "")               
               +IIFs(Use_Symbol8,"\nSymbol 8 = "+Symbol8, "")               //8
               +IIFs(Use_Symbol8,"  Lot = "+DoubleToStr(Symbol8_Lot,2), "")                              
               +IIFs(Use_Symbol9,"\nSymbol 9 = "+Symbol9, "")               //9
               +IIFs(Use_Symbol9,"  Lot= "+DoubleToStr(Symbol9_Lot,2), "")
               +IIFs(Use_Symbol10,"\nSymbol 10 = "+Symbol10, "")               //10
               +IIFs(Use_Symbol10,"  Lot= "+DoubleToStr(Symbol10_Lot,2), "")                                             
               //+"PercentProfit="+DoubleToStr(PercentProfit, 2)
               //+"  PercentLoss="+DoubleToStr(PercentLoss, 2)+"\n"
               +"\n\nТекущие: Баланс="+DoubleToStr(AccountBalance(), 2)
               +"\n              Эквити="+DoubleToStr(AccountEquity(), 2)
               +"\n              Прибыль="+DoubleToStr((AccountEquity()/AccountBalance()-1)*100,3)+" %"
               ;
      Comment(st);
    } else Comment("");
  }
//----
  if(Sell)name_op = OP_SELL;
  else    name_op = OP_BUY;
//----
 if(ClosePercent)
  {
   if (AccountEquity()>=AccountBalance()*(1+PercentProfit/100)
   ||  AccountEquity()<=AccountBalance()*(1-PercentLoss  /100))
   ClosePositions("",-1,MagicNumber);   
  }
//----
 if(TimeClose) 
  {
   if (TimeCurrent()>=StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+CloseTime)
   && TimeCurrent()<StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+CloseTime)+Duration)
   ClosePositions("",-1,MagicNumber);
  }
//----
  if (TimeCurrent()>=StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+TimeTrade)
  && TimeCurrent()<StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+TimeTrade)+Duration)
  {
//----------------------------1  
   if (Use_Symbol1 && !ExistPositions(Symbol1,name_op,MagicNumber)) 
    OpenPosition(Symbol1,name_op,Symbol1_Lot,0,0,MagicNumber);
//----------------------------2       
   if (Use_Symbol2 && !ExistPositions(Symbol2,name_op,MagicNumber))
    OpenPosition(Symbol2,name_op,Symbol2_Lot,0,0,MagicNumber);
//----------------------------3    
   if (Use_Symbol3 && !ExistPositions(Symbol3,name_op,MagicNumber))
    OpenPosition(Symbol3,name_op,Symbol3_Lot,0,0,MagicNumber);
//----------------------------4    
   if (Use_Symbol4 && !ExistPositions(Symbol4,name_op,MagicNumber))
    OpenPosition(Symbol4,name_op,Symbol4_Lot,0,0,MagicNumber);
//----------------------------5    
   if (Use_Symbol5 && !ExistPositions(Symbol5,name_op,MagicNumber))
    OpenPosition(Symbol5,name_op,Symbol5_Lot,0,0,MagicNumber);
//----------------------------6    
   if (Use_Symbol6 && !ExistPositions(Symbol6,name_op,MagicNumber)) 
   OpenPosition(Symbol6,name_op,Symbol6_Lot,0,0,MagicNumber);
//----------------------------7   
   if (Use_Symbol7 && !ExistPositions(Symbol7,name_op,MagicNumber))
    OpenPosition(Symbol7,name_op,Symbol7_Lot,0,0,MagicNumber);
//----------------------------8    
   if (Use_Symbol8 && !ExistPositions(Symbol8,name_op,MagicNumber))
    OpenPosition(Symbol8,name_op,Symbol8_Lot,0,0,MagicNumber);
//----------------------------9    
   if (Use_Symbol9 && !ExistPositions(Symbol9,name_op,MagicNumber))
    OpenPosition(Symbol9,name_op,Symbol9_Lot,0,0,MagicNumber);
//----------------------------10    
   if (Use_Symbol10 && !ExistPositions(Symbol10,name_op,MagicNumber))
    OpenPosition(Symbol10,name_op,Symbol10_Lot,0,0,MagicNumber);   
  }    
//----
   return(0);
  }
//+------------------------------------------------------------------+
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