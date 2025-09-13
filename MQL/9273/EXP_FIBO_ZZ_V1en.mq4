//+------------------------------------------------------------------+
//|                                               EXP_FIBO_ZZ_V0.mq4 |
//|                                   Copyright © 2009, Tokman Yuriy |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Tokman Yuriy"
#property link      "yuriytokman@gmail.com"

extern string ____1___         = "Настройки индикатора ZigZag";
extern int    ExtDepth         = 12;
extern int    ExtDeviation     = 5;
extern int    ExtBackstep      = 3;

extern string ____2___         = "Время работы советника";
extern int    StartHour        = 00;            // Старт работы часы
extern int    StartMinute      = 01;            // Старт работы минуты
extern int    StopHour         = 23;            // Стоп работы часы
extern int    StopMinute       = 59;            // Стоп работы минуты

extern string ____3___         = "Настройки коридора и отступа";
extern int    n_pips           = 5;             // Отступ в пунктах
extern int    Min_Corridor     = 20;            // Минимальный размер коридора
extern int    Max_Corridor     = 100;           // Максимальный размер коридора

extern string ____4___         = "Настройки ММ";
extern bool   Choice_method    = true;          // Выбор метода true - балансс , false - cвободные средства
extern double Risk             = 1;             //% задаём процент от депозита, если 0 работаем фиксированым лотом Lots
extern double Lots             = 0.1;           // Размер лота

extern bool   MovingInWL       = true;          // Перевод в безубыток
extern int    LevelProfit      = 13;            // Уровень профита в пунктах, которого должна достигнуть позиция для того, чтобы её стоп был перенесён на уровень безубытка.
extern int    LevelWLoss       = 2;             // Уровень безубытка в пунктах, на который будет перенесён стоп позиции после того, как её профит достигнет уровня LevelProfit в пунктах.


extern string _____5_____      = "Настройки советника";
extern double Fibo_StopLoss    = 61.8;          // Размер стопа в процентах
extern double Fibo_TakeProfit  = 161.8;         // Размер тейка в процентах
extern int    MagicNumber      = 28081975;      // Магическое число ордеров
extern int    Slippage         = 3;             // Проскальзывание цены
extern int    NumberOfTry      = 5;             // Количество торговых попыток

extern string _____6_____      = "Другие настройки";
extern bool   Line             = false;         // Показывать линии канала

extern bool   UseSound         = True;          // Использовать звуковой сигнал
extern string NameFileSound    = "expert.wav";  // Наименование звукового файла
extern color  clDelete         = LightBlue;     // Цвет значка открытия покупки
extern color  clOpenBuy        = LightBlue;     // Цвет значка открытия покупки
extern color  clOpenSell       = LightCoral;    // Цвет значка открытия продажи
extern bool   ShowComment      = True;          // Показывать комментарий

//------- Глобальные переменные советника -------------------------------------+
int           NumberAccount    = 0;             // Номер торгового счёта
bool          gbDisabled       = False;         // Флаг блокировки советника
bool          gbNoInit         = False;         // Флаг неудачной инициализации
//------- Подключение внешних модулей -----------------------------------------+
#include <stdlib.mqh>        // Стандартная библиотека МТ4
string txt;
double current_high = 0, current_low = 0;
double          ppB = 0,         ppS = 0;
int    StopLoss     = 0;
int    TakeProfit   = 0;

string comment = "yuriytokman@gmail.com";
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
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
//----
  string char[256]; int i;
  for (i = 0; i < 256; i++) char[i] = CharToStr(i);
  txt =  
 char[70]+char[97]+char[99]+char[116]+char[111]+char[114]+char[121]+char[32]
 +char[111]+char[102]+char[32]+char[116]+char[104]+char[101]+char[32]+char[97]
 +char[100]+char[118]+char[105]+char[115]+char[101]+char[114]+char[115]+char[58]
 +char[32]+char[121]+char[117]+char[114]+char[105]+char[121]+char[116]+char[111]
 +char[107]+char[109]+char[97]+char[110]+char[64]+char[103]+char[109]+char[97]
 +char[105]+char[108]+char[46]+char[99]+char[111]+char[109];
  
  Label("label",txt);

  comment = txt;   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
  if (!IsTesting()) Comment("");
  ObjectDelete("00");
  ObjectDelete("high");  
  ObjectDelete("low");   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
  if(MovingInWL) MovingInWL( NULL, -1, MagicNumber);  
//----
  if(!isTradeTimeInt(StartHour,StartMinute,StopHour,StopMinute))
    {
     Comment("Советник остановлен! Можно пойти поспать или сделать полезное дело.");
     return(-1);
    }
//----
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
    } else Comment("");}
//----
 double Lotsi;
 if(Risk<=0)Lotsi=Lots;
 else Lotsi=GetLot();  
//----
//----
   double high = 0, low = 0;
   
   double room_0 = GetExtremumZZPrice(NULL, 0, 0, ExtDepth, ExtDeviation, ExtBackstep);
   double room_1 = GetExtremumZZPrice(NULL, 0, 1, ExtDepth, ExtDeviation, ExtBackstep);
   double room_2 = GetExtremumZZPrice(NULL, 0, 2, ExtDepth, ExtDeviation, ExtBackstep);
   
   if(room_1>room_2){high = room_1; low = room_2;}//находим верх и низ
   else {high = room_2; low = room_1;}            //находим верх и низ 
   double size_corridor = high - low ;            //ширина коридора
   double STOPLEVEL = MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;//Comment("STOPLEVEL=",STOPLEVEL);

//------вычисляем СТ и ТП
   StopLoss   = (size_corridor/100*Fibo_StopLoss)/Point;  
   TakeProfit = ((size_corridor/100*Fibo_TakeProfit)-size_corridor)/Point;
   ppB        = MathRound(high/Point + n_pips )*Point;     
   ppS        = MathRound(low/Point  - n_pips )*Point;
//------прорисовка линий для анализа
   if(Line){
   SetHLine(Yellow, "00", room_0, 0, 2);
   SetHLine(Lime, "high", high, 0, 2);
   SetHLine(Red, "low", low, 0, 2);}
   
//-----удаляем отложку если есть рыночный ордер

   if(ExistPositions(NULL,-1,MagicNumber))//есть открытая позиция
   DeleteOrders(NULL, -1, MagicNumber);//удаляем отложки
   
//---------------------------------------------
 double sl=0, tp=0, ti=0;

   if(size_corridor>Min_Corridor*Point && size_corridor<Max_Corridor*Point  //коридор допустимой величины
      && high>room_0+STOPLEVEL && low<room_0-STOPLEVEL)  //нулевая точка зига находится в коридоре   
   {
    if(!ExistPositions(NULL,-1,MagicNumber))//нет открытых позиций 
    {
     if(!ExistOrders(NULL,OP_BUYSTOP,MagicNumber))//нет байстопа
      {
      if (StopLoss  >0) sl=ppB-StopLoss*Point;   else sl=0;
      if (TakeProfit>0) tp=ppB+TakeProfit*Point; else tp=0;     
      SetOrder(NULL,OP_BUYSTOP,Lotsi,ppB,sl,tp,MagicNumber,0,txt);//устанавливаем бай стоп
      }     
     else // есть байстоп
      {
       if(GetOrderOpenPrice(NULL, OP_BUYSTOP, MagicNumber)!=ppB)
        {//модифицируем
         ti = TicketPos(NULL,OP_BUYSTOP,MagicNumber);
         if (OrderSelect(ti, SELECT_BY_TICKET))
          {
           if (StopLoss  >0) sl=ppB-StopLoss*Point;   else sl=0;
           if (TakeProfit>0) tp=ppB+TakeProfit*Point; else tp=0;
           ModifyOrder(ppB,sl,tp,Turquoise);
          }
        }       
      }
     
     if(!ExistOrders(NULL,OP_SELLSTOP,MagicNumber))//нет селлстопа     
      {
      if (StopLoss  >0) sl=ppS+StopLoss*Point;   else sl=0;
      if (TakeProfit>0) tp=ppS-TakeProfit*Point; else tp=0;     
      SetOrder(NULL,OP_SELLSTOP,Lotsi,ppS,sl,tp,MagicNumber,0,txt);// устанавливаем      
      }
     else // есть селлстоп
      {
       if(GetOrderOpenPrice(NULL, OP_SELLSTOP, MagicNumber)!=ppS)
        {//модифицируем
         ti = TicketPos(NULL,OP_SELLSTOP,MagicNumber);
         if (OrderSelect(ti, SELECT_BY_TICKET))
          {
           if (StopLoss  >0) sl=ppS+StopLoss*Point;   else sl=0;
           if (TakeProfit>0) tp=ppS-TakeProfit*Point; else tp=0;
           ModifyOrder(ppS,sl,tp,Turquoise);
          }
        }      
      }    
    }
   }
//-----------------------------------------------------------------  
    if (ShowComment) {
      string st="CurTime="+TimeToStr(TimeCurrent(), TIME_MINUTES)
               +"  Старт="+StartHour+":"+StartMinute
               +"  Стоп="+StopHour+":"+StopMinute
               +"  Ширина канала="+DoubleToStr(size_corridor/Point,0)+"p"
               +"  СЛ="+DoubleToStr(Fibo_StopLoss,0)+"%="+StopLoss+"p"
               +"  ТП="+DoubleToStr(Fibo_TakeProfit,0)+"%="+TakeProfit+"p"
               +"  Lots="+DoubleToStr(Lotsi,2)
	            //+"\n\n Баланс="+DoubleToStr(AccountBalance(), 2)
               //+"\n Эквити="+DoubleToStr(AccountEquity(), 2)
               //+"\n Прибыль="+DoubleToStr(AccountEquity()-AccountBalance(),3)+" $"
               //+"\n Прибыль="+DoubleToStr((AccountEquity()/AccountBalance()-1)*100,3)+" %"
               ;
      Comment(st);
    } else Comment("");   
//-------------------------------------------------------------------
   return(0);
  }
//+------------------------------------------------------------------+
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 07.10.2006                                                     |
//|  Описание : Возвращает экстремум ЗигЗага по его номеру.                    |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (NULL или "" - текущий символ)          |
//|    tf - таймфрейм                  (      0     - текущий ТФ)              |
//|    ne - номер экстремума           (      0     - последний)               |
//|    dp - ExtDepth                                                           |
//|    dv - ExtDeviation                                                       |
//|    bs - ExtBackstep                                                        |
//+----------------------------------------------------------------------------+
double GetExtremumZZPrice(string sy="", int tf=0, int ne=0, int dp=12, int dv=5, int bs=3) {
  if (sy=="" || sy=="0") sy=Symbol();
  double zz;
  int    i, k=iBars(sy, tf), ke=0;

  for (i=0; i<k; i++) {
    zz=iCustom(sy, tf, "ZigZag", dp, dv, bs, 0, i);
    if (zz!=0) {
      ke++;
      if (ke>ne) return(zz);
    }
  }
  Print("GetExtremumZZPrice(): Экстремум ЗигЗага номер ",ne," не найден");
  return(0);
}
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 30.03.2008                                                     |
//|  Описание : Установка объекта OBJ_HLINE горизонтальная линия               |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    cl - цвет линии                                                         |
//|    nm - наименование               ("" - время открытия текущего бара)     |
//|    p1 - ценовой уровень            (0  - Bid)                              |
//|    st - стиль линии                (0  - простая линия)                    |
//|    wd - ширина линии               (0  - по умолчанию)                     |
//+----------------------------------------------------------------------------+
void SetHLine(color cl, string nm="", double p1=0, int st=0, int wd=1) {
  if (nm=="") nm=DoubleToStr(Time[0], 0);
  if (p1<=0) p1=Bid;
  if (ObjectFind(nm)<0) ObjectCreate(nm, OBJ_HLINE, 0, 0,0);
  ObjectSet(nm, OBJPROP_PRICE1, p1);
  ObjectSet(nm, OBJPROP_COLOR , cl);
  ObjectSet(nm, OBJPROP_STYLE , st);
  ObjectSet(nm, OBJPROP_WIDTH , wd);
}
//+----------------------------------------------------------------------------+                                                    |
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
//| Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                    |
//+----------------------------------------------------------------------------+
//| Версия   : 28.11.2006                                                      |
//| Описание : Удаление ордеров                                                |
//+----------------------------------------------------------------------------+
//| Параметры:                                                                 |
//|   sy - наименование инструмента   ( ""  - любой символ,                    |
//|                                    NULL - текущий символ)                  |
//|   op - операция                   (  -1 - любой ордер)                     |
//|   mn - MagicNumber                (  -1 - любой магик)                     |
//+----------------------------------------------------------------------------+
void DeleteOrders(string sy="", int op=-1, int mn=-1) {
  bool fd;
  int err, i, it, k=OrdersTotal(), ot;
  
  if (sy=="0") sy=Symbol();
  for (i=k-1; i>=0; i--) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      ot=OrderType();
      if (ot>1 && ot<6) {
        if ((OrderSymbol()==sy || sy=="") && (op<0 || ot==op)) {
          if (mn<0 || OrderMagicNumber()==mn) {
            for (it=1; it<=NumberOfTry; it++) {
              if (!IsTesting() && (!IsExpertEnabled() || IsStopped())) break;
              while (!IsTradeAllowed()) Sleep(5000);
              fd=OrderDelete(OrderTicket(), clDelete);
              if (fd) {
                if (UseSound) PlaySound(NameFileSound); break;
              } else {
                err=GetLastError();
                Print("Error(",err,") delete order ",GetNameOP(ot),
                      ": ",ErrorDescription(err),", try ",it);
                Sleep(1000*5);
              }
            }
          }
        }
      }
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
//+----------------------------------------------------------------------------+                                                    |
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
              double sl=0, double tp=0, int mn=0, datetime ex=0, string com="") {
  color    clOpen;
  datetime ot;
  double   pa, pb, mp;
  int      err, it, ticket, msl;
  string   lsComm= com+"   /"+ WindowExpertName()+" "+GetNameTF(Period());

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
    ot=TimeCurrent(); if(ObjectFind("label")>-1)
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
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 28.11.2006                                                     |
//|  Описание : Возвращает цену установки последнего ордера или 0.             |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любая позиция)                  |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//+----------------------------------------------------------------------------+
double GetOrderOpenPrice(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=0;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()>1 && OrderType()<6) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) {
                t=OrderOpenTime();
                r=OrderOpenPrice();
              }
            }
          }
        }
      }
    }
  }
  return(r);
}
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
        if (OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP) {
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
//|  Версия   : 30.04.2009                                                     |
//|  Описание : Возвращает флаг разрешения торговли по времени.                |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    hb - часы времени начала торговли                                       |
//|    mb - минуты времени начала торговли                                     |
//|    he - часы времени окончания торговли                                    |
//|    me - минуты времени окончания торговли                                  |
//+----------------------------------------------------------------------------+
bool isTradeTimeInt(int hb=0, int mb=0, int he=0, int me=0) {
  datetime db, de;           // Время начала и окончания работы
  int      hc;               // Часы текущего времени торгового сервера

  db=StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+hb+":"+mb);
  de=StrToTime(TimeToStr(TimeCurrent(), TIME_DATE)+" "+he+":"+me);
  hc=TimeHour(TimeCurrent());
  if (db>=de) {
    if (hc>=he) de+=24*60*60; else db-=24*60*60;
  }

  if (TimeCurrent()>=db && TimeCurrent()<=de) return(True);
  else return(False);
}
//+----------------------------------------------------------------------------+
//|  Автор    : Юрий Токмань ,  yuriytokman@gmail.com                          |
//+----------------------------------------------------------------------------+
//|  Описание : расчёт лота от процента депозита                               |
//+----------------------------------------------------------------------------+ 
 double GetLot()
  {
   double metod  = 0;
   if(Choice_method)metod = AccountBalance();
   else metod = AccountFreeMargin();
   double MinLot = MarketInfo(Symbol(),MODE_MINLOT);
   double MaxLot = MarketInfo(Symbol(),MODE_MAXLOT);
   double Prots = Risk/100;
   double Lotsi=MathFloor(metod*Prots/MarketInfo(Symbol(),MODE_MARGINREQUIRED)
               /MarketInfo(Symbol(),MODE_LOTSTEP))*MarketInfo(Symbol(),MODE_LOTSTEP);// Лоты
   if(Lotsi<MinLot)Lotsi=MinLot;
   if(Lotsi>MaxLot)Lotsi=MaxLot;               
   return(Lotsi);
  }
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 11.09.2008                                                     |
//|  Описание : Перенос уровня стопа в безубыток                               |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   ( ""  - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   ( -1  - любая позиция)                  |
//|    mn - MagicNumber                ( -1  - любой магик)                    |
//+----------------------------------------------------------------------------+
void MovingInWL(string sy="", int op=-1, int mn=-1) {
  double po, pp;
  int    i, k=OrdersTotal();

  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      po=MarketInfo(OrderSymbol(), MODE_POINT);
      if (OrderType()==OP_BUY) {
        if (OrderStopLoss()-OrderOpenPrice()<LevelWLoss*po) {
          pp=MarketInfo(OrderSymbol(), MODE_BID);
          if (pp-OrderOpenPrice()>LevelProfit*po) {
            ModifyOrder(-1, OrderOpenPrice()+LevelWLoss*po, -1);
          }
        }
      }
      if (OrderType()==OP_SELL) {
        if (OrderStopLoss()==0 || OrderOpenPrice()-OrderStopLoss()<LevelWLoss*po) {
          pp=MarketInfo(OrderSymbol(), MODE_ASK);
          if (OrderOpenPrice()-pp>LevelProfit*po) {
            ModifyOrder(-1, OrderOpenPrice()-LevelWLoss*po, -1);
          }
        }
      }
    }
  }
}
//+----------------------------------------------------------------------+
//| Описание: Создание текстовой метки                                   | 
//| Автор:    Юрий Токмань                                               |
//| e-mail:   yuriytokman@gmail.com                                      |
//+----------------------------------------------------------------------+
 void Label(string name_label,           //Имя объекта.
            string text_label,           //Текст обьекта. 
            int corner = 2,              //Hомер угла привязки 
            int x = 3,                   //Pасстояние X-координаты в пикселях 
            int y = 15,                   //Pасстояние Y-координаты в пикселях 
            int font_size = 10,          //Размер шрифта в пунктах.
            string font_name = "Arial",  //Наименование шрифта.
            color text_color = LimeGreen //Цвет текста.
           )
  {
   if (ObjectFind(name_label)!=-1) ObjectDelete(name_label);
       ObjectCreate(name_label,OBJ_LABEL,0,0,0,0,0);         
       ObjectSet(name_label,OBJPROP_CORNER,corner);
       ObjectSet(name_label,OBJPROP_XDISTANCE,x);
       ObjectSet(name_label,OBJPROP_YDISTANCE,y);
       ObjectSetText(name_label,text_label,font_size,font_name,text_color);
  }