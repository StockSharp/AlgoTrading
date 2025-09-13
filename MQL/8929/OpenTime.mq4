//+------------------------------------------------------------------+
//|                                                     OpenTime.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern string _____1_____      = "��������� �������� �������";
extern bool   TimeClose        = True;     // ����� �������� ������� �������
extern string CloseTime        = "20:50";  // ����� �������� �������
extern bool   Trailing         = False;    // �������� �������� �������
extern double TrailingStop     = 300;      // ������ �����
extern int    TrailingStep     = 3;        // ��� �����

extern string _____2_____      = "��������� �������� �������";
extern string TimeTrade        = "18:50";  // ����� �������� �������
extern int    Duration         = 300;      // ����������������� � ��������
extern bool   Sell             = True;     // True-Sell
extern bool   Buy              = False;    // True-Buy
extern double Lots             = 0.1;      // ������ ����
extern int    StopLoss         = 0;        // ������ ����� � �������
extern int    TakeProfit       = 0;        // ������ ����� � �������

extern string _____3_____      = " ��������� ���������";
extern int    MagicNumber      = 89888;    //���������� ����� �������
int    NumberAccount           = 0;        // ����� ��������� �����
bool   UseSound                = True;     // ������������ �������� ������
string NameFileSound           = "expert.wav"; // ������������ ��������� �����
bool   ShowComment             = True;     // ���������� �����������
bool   MarketWatch             = False;    // ������� ��� ���������� "Market Watch".
extern int    Slippage         = 3;        // ��������������� ����
extern int    NumberOfTry      = 5;        // ���������� �������� �������
                                                
//------- ���������� ���������� ��������� -------------------------------------+
bool  gbDisabled = False;         // ���� ���������� ���������
bool  gbNoInit   = False;         // ���� ��������� �������������
color clOpenBuy  = LightBlue;     // ���� ������ �������� �������
color clOpenSell = LightCoral;    // ���� ������ �������� �������

//------- ����������� ������� ������� -----------------------------------------+
#include <stdlib.mqh>        // ����������� ���������� ��4


//+----------------------------------------------------------------------------+
//|                                                                            |
//|  ����������˨���� �������                                                  |
//|                                                                            |
//+----------------------------------------------------------------------------+
//|  ������� �������������                                                     |
//+----------------------------------------------------------------------------+
void init() {
  gbNoInit=False;
  if (!IsTradeAllowed()) {
    Message("��� ���������� ������ ��������� ����������\n"+
            "��������� ��������� ���������");
    gbNoInit=True; return;
  }
  if (!IsLibrariesAllowed()) {
    Message("��� ���������� ������ ��������� ����������\n"+
            "��������� ������ �� ������� ���������");
    gbNoInit=True; return;
  }
  if (!IsTesting()) {
    if (IsExpertEnabled()) Message("�������� ����� ������� ��������� �����");
    else Message("������ ������ \"��������� ������ ����������\"");
  }
}

//+----------------------------------------------------------------------------+
//|  ������� ���������������                                                   |
//+----------------------------------------------------------------------------+
void deinit() { if (!IsTesting()) Comment(""); }

//+----------------------------------------------------------------------------+
//|  expert start function                                                     |
//+----------------------------------------------------------------------------+
void start() { 
  
  double sl=0, tp=0;  
  if (gbDisabled) {
    Message("����������� ������! �������� ����������!"); return;
  }
  if (gbNoInit) {
    Message("�� ������� ���������������� ��������!"); return;
  }
  if (!IsTesting()) {
    if (NumberAccount>0 && NumberAccount!=AccountNumber()) {
      Comment("�������� �� �����: "+AccountNumber()+" ���������!");
      return;
    } else Comment("");
    if (ShowComment) {
      string st="CurTime="+TimeToStr(TimeCurrent(), TIME_MINUTES)
               +"  TimeTrade="+TimeTrade
               +IIFs(TimeClose,"  CloseTime="+CloseTime, "")  
               +"  �������="
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
//|  ���������������� �������                                                  |
//|                                                                            |
//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 06.03.2008                                                     |
//|  �������� : ���������� ���� ������������� �������                          |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    ot - ����� ��������             ( 0   - ����� ����� ��������)           |
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ���������� ������������ �������� ��������                      |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    op - ������������� �������� ��������                                    |
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ���������� ������������ ����������                             |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    TimeFrame - ��������� (���������� ������)      (0 - ������� ��)         |
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.02.2008                                                     |
//|  �������� : ���������� ���� �� ���� �������� ������������ �� �������.      |
//+----------------------------------------------------------------------------+
string IIFs(bool condition, string ifTrue, string ifFalse) {
  if (condition) return(ifTrue); else return(ifFalse);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ����� ��������� � ������� � � ������                           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ����� ���������                                                     |
//+----------------------------------------------------------------------------+
void Message(string m) {
  Comment(m);
  if (StringLen(m)>0) Print(m);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 28.11.2006                                                     |
//|  �������� : ����������� ������ �������������� ���������� ������.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    pp - ���� ��������� ������                                              |
//|    sl - ������� ������� �����                                              |
//|    tp - ������� ������� �����                                              |
//|    ex - ���� ���������                                                     |
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 10.04.2008                                                     |
//|  �������� : ��������� ������� �� �������� ����.                            |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL ��� "" - ������� ������)          |
//|    op - ��������                                                           |
//|    ll - ���                                                                |
//|    sl - ������� ����                                                       |
//|    tp - ������� ����                                                       |
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
      Print("OpenPosition(): ��������� ������ �������");
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
      if (pa==0 && pb==0) Message("��������� � ������ ����� ������� ������� "+sy);
      // ����� ��������� �� ������
      Print("Error(",err,") opening position: ",ErrorDescription(err),", try ",it);
      Print("Ask=",pa," Bid=",pb," sy=",sy," ll=",ll," op=",GetNameOP(op),
            " pp=",pp," sl=",sl," tp=",tp," mn=",mn);
      // ���������� ������ ���������
      if (err==2 || err==64 || err==65 || err==133) {
        gbDisabled=True; break;
      }
      // ���������� �����
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : �������� ������� �� �������� ����                              |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������  : 19.02.2008                                                      |
//|  ��������: �������� ����� �������������� ��������� �������                 |
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
  } else Print("������������ �������� ��������. Close ",GetNameOP(OrderType()));
 } 
//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 11.09.2008                                                     |
//|  �������� : ������������� ������� ������� ������                           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ( ""  - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   ( -1  - ����� �������)                  |
//|    mn - MagicNumber                ( -1  - ����� �����)                    |
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

