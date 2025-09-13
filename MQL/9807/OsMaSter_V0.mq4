//+------------------------------------------------------------------+
//|                                                  OsMaSter_V0.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern int    TF                 = 0;
extern int    fast_ema_period    = 9;
extern int    slow_ema_period    = 26;
extern int    signal_period      = 5;
extern int    applied_price      = 0;
extern int    shift_1            = 0;
extern int    shift_2            = 1;
extern int    shift_3            = 2;
extern int    shift_4            = 3;

extern double Lots               = 0.01;         // Размер лота
extern int    StopLoss           = 50;           // Размер стопа в пунктах
extern int    TakeProfit         = 50;           // Размер тейка в пунктах
extern int    MagicNumber        = 20081975 ;    // магическое число советника
extern int    Slippage           = 3;            // Проскальзывание цены
extern int    NumberOfTry        = 5;            // Количество торговых попыток

bool   UseSound                  = True;         // Использовать звуковой сигнал
string NameFileSound             = "expert.wav"; // Наименование звукового файла
color  clOpenBuy                 = LightBlue;    // Цвет значка открытия Buy
color  clOpenSell                = LightCoral;   // Цвет значка открытия Sell


bool          gbDisabled         = False;        // Блокировка
#include <stdlib.mqh>                            // Стандартная библиотека МТ4
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
//----
 double sl = 0, tp = 0;

if(!ExistPositions(NULL,-1, MagicNumber))
  {
   if (GetSignal()==+1)
     {
      if (StopLoss  >0) sl=Ask-StopLoss*Point;   else sl=0;
      if (TakeProfit>0) tp=Ask+TakeProfit*Point; else tp=0;
      OpenPosition(NULL, OP_BUY, Lots, sl, tp, MagicNumber);      
     }
   if (GetSignal()==-1)
     {
      if (StopLoss  >0) sl=Bid+StopLoss*Point;   else sl=0;
      if (TakeProfit>0) tp=Bid-TakeProfit*Point; else tp=0;
      OpenPosition(NULL, OP_SELL, Lots, sl, tp, MagicNumber);      
     }      
  }   
//----
   return(0);
  }
//+------------------------------------------------------------------+
 int GetSignal()
   {
    double Os_1=iOsMA(Symbol(),TF,fast_ema_period,slow_ema_period,signal_period,applied_price,shift_1);
    double Os_2=iOsMA(Symbol(),TF,fast_ema_period,slow_ema_period,signal_period,applied_price,shift_2);
    double Os_3=iOsMA(Symbol(),TF,fast_ema_period,slow_ema_period,signal_period,applied_price,shift_3);
    double Os_4=iOsMA(Symbol(),TF,fast_ema_period,slow_ema_period,signal_period,applied_price,shift_4);    

    int vSignal = 0;
    if(Os_4>Os_3 && Os_3<Os_2 && Os_2<Os_1) vSignal = 1;//up 
    else
    if(Os_4<Os_3 && Os_3>Os_2 && Os_2>Os_1) vSignal =-1;//down
    
    return (vSignal);
   }
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
void Message(string m) {
  Comment(m);
  if (StringLen(m)>0) Print(m);
}   