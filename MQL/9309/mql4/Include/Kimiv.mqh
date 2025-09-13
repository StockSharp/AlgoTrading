//------- ���������� ���������� -----------------------------------------------+
color  clOpenBuy     = LightBlue;      // ���� ������ �������� �������
color  clOpenSell    = LightCoral;     // ���� ������ �������� �������
color  clDelete      = Yellow;         // ���� ������ �������� ������
color  clCloseBuy    = DimGray;
color  clCloseSell   = SteelBlue;
color  clModifyBuy   = MediumVioletRed;
color  clModifySell  = MediumSeaGreen;
int    Slippage      = 3;              // ��������������� ����
int    NumberOfTry   = 3;              // ���������� ������� ������� �����
bool   UseSound      = false;           // ������������ �������� �����
string NameFileSound = "c:\WINDOWS\Media\tada.wav";  //���� � ��� ��������� ����� ��� �������� ������
bool   gbDisabled    = false;          // ���������� ���������

extern bool   AllSymbols   = True;     // ������� �� ��������� ���� ��������
extern int    Magic        = -1;       // ������������� �������
extern bool   TSProfitOnly = True;     // ������� ������ ������
extern int    TStop.Buy    = 50;       // ������ ����� � ������� ��� �������
extern int    TStop.Sell   = 35;       // ������ ����� � ������� ��� ������
extern int    TrailingStep = 3;        // ��� ����� � �������
extern int    LevelProfit = 25;        // ������� ������� � �������
extern int    LevelWLoss  = 1;         // ������� ��������� � �������

extern bool   ShowComment  = True;     // ���������� �����������


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
    default:		     return("UnknownPeriod");
  }
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

#include <stdlib.mqh>                  // ����������� ����������

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������  : 13.06.2007                                                      |
//|  �������� : ��������� ������. ������ ������� ��� ������ �� �������.        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL ��� "" - ������� ������)          |
//|    op - ��������                                                           |
//|    ll - ���                                                                |
//|    pp - ����                                                               |
//|    sl - ������� ����                                                       |
//|    tp - ������� ����                                                       |
//|    mn - Magic Number                                                       |
//|    ex - ���� ���������                                                     |
//+----------------------------------------------------------------------------+
void SetOrder(string sy, int op, double ll, double pp,
              double sl=0, double tp=0, int mn=0, datetime ex=0) {
  color clOpen;
  int   err, ticket;

  if (sy=="" || sy=="0") sy=Symbol();
  if (op==OP_BUYLIMIT || op==OP_BUYSTOP) clOpen=clOpenBuy; else clOpen=clOpenSell;
  ticket=OrderSend(sy, op, ll, pp, Slippage, sl, tp, "", mn, ex, clOpen);
  if (ticket<0) {
    err=GetLastError();
    Print("Error(",err,") set ",GetNameOP(op),": ",ErrorDescription(err));
    Print("Ask=",Ask," Bid=",Bid," sy=",sy," ll=",ll,
          " pp=",pp," sl=",sl," tp=",tp," mn=",mn);
  }
}
//+----------------------------------------------------------------------------+

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.03.2008                                                     |
//|  �������� : ���������� ���� ������������� �������.                         |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �����)                    |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    ot - ����� ��������             ( 0   - ����� ����� ���������)          |
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
//|  ������  : 13.06.2007                                                      |
//|  �������� : �������� �������. ������ ������� ��� ������ �� �������.        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ("" - ������� ������)                   |
//|    op - ��������                                                           |
//|    ll - ���                                                                |
//|    sl - ������� ����                                                       |
//|    tp - ������� ����                                                       |
//|    mn - MagicNumber                                                        |
//+----------------------------------------------------------------------------+
void OpenPosition_for_test(string sy, int op, double ll, double sl=0, double tp=0, int mn=0) {
  color  clOpen;
  double pp;
  int    err, ticket;
 
  if (sy=="") sy=Symbol();
  if (op==OP_BUY) {
    pp=MarketInfo(sy, MODE_ASK); clOpen=clOpenBuy;
  } else {
    pp=MarketInfo(sy, MODE_BID); clOpen=clOpenSell;
  }
  ticket=OrderSend(sy, op, ll, pp, Slippage, sl, tp, "", mn, 0, clOpen);
  if (ticket<0) {
    err=GetLastError();
    Print("Error(",err,") open ",GetNameOP(op),": ",ErrorDescription(err));
    Print("Ask=",Ask," Bid=",Bid," sy=",sy," ll=",ll,
          " pp=",pp," sl=",sl," tp=",tp," mn=",mn);
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 21.03.2008                                                     |
//|  �������� : ��������� ������� � ���������� � �����.                       |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL ��� "" - ������� ������)          |
//|    op - ��������                                                           |
//|    ll - ���                                                                |
//|    sl - ������� ����                                                       |
//|    tp - ������� ����                                                       |
//|    mn - MagicNumber                                                        |
//+----------------------------------------------------------------------------+
int OpenPosition(string sy, int op, double ll, double sl=0, double tp=0, int mn=0) {
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
  return(ticket);
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
//|    cl - ���� ������ �����������                                            |
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
//| �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                    |
//+----------------------------------------------------------------------------+
//| ������   : 28.11.2006                                                      |
//| �������� : �������� �������                                                |
//+----------------------------------------------------------------------------+
//| ���������:                                                                 |
//|   sy - ������������ �����������   ( ""  - ����� ������,                    |
//|                                    NULL - ������� ������)                  |
//|   op - ��������                   (  -1 - ����� �����)                     |
//|   mn - MagicNumber                (  -1 - ����� �����)                     |
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
//| �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                    |
//+----------------------------------------------------------------------------+
//| ������   : 07.10.2006                                                      |
//| �������� : ����� ���������� ��������.                                      |
//+----------------------------------------------------------------------------+
//| ���������:                                                                 |
//|   sy - ������������ �����������     (NULL - ������� ������)                |
//|   tf - ���������                    (  0  - ������� ��)                    |
//|   mode - ��� ��������               (MODE_LOWER|MODE_UPPER)                |
//+----------------------------------------------------------------------------+
double FindNearFractal(string sy="0", int tf=0, int mode=MODE_LOWER) {
  if (sy=="" || sy=="0") sy=Symbol();
  double f=0;
  int d=MarketInfo(sy, MODE_DIGITS), s;
  if (d==0) if (StringFind(sy, "JPY")<0) d=4; else d=2;
 
  for (s=2; s<100; s++) {
    f=iFractals(sy, tf, mode, s);
    if (f!=0) return(NormalizeDouble(f, d));
  }
  Print("FindNearFractal(): ������� �� ������");
  return(0);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 28.11.2006                                                     |
//|  �������� : ���������� ���� ������������� ������ �� ������� ����.          |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �����)                    |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    lo - ���                        ( 0   - ����� ���)                      |
//+----------------------------------------------------------------------------+
bool ExistOrdersByLot(string sy="", int op=-1, int mn=-1, double lo=0) {
  int i, k=OrdersTotal(), ot;
  lo=NormalizeDouble(lo, 2);

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      ot=OrderType();
      if (ot>1 && ot<6) {
        if ((OrderSymbol()==sy || sy=="") && (op<0 || ot==op)) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (lo<=0 || NormalizeDouble(OrderLots(), 2)==lo) return(True);
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
//|  ������   : 28.11.2006                                                     |
//|  �������� : ���������� ������ ���� ���������� ������������� ������ ��� -1  |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double GetLotLastOrder(string sy="", int op=-1, int mn=-1) {
  datetime o;
  double   l=-1;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()>1 && OrderType()<6) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (o<OrderOpenTime()) {
                o=OrderOpenTime();
                l=OrderLots();
              }
            }
          }
        }
      }
    }
  }
  return(l);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 28.11.2006                                                     |
//|  �������� : ���������� ���� ��������� ���������� ������ ��� 0.             |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 20.04.2007                                                     |
//|  �������� : ���������� ������ ������ ��� ������� �� ������                 |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    ti - ����� ������, �������                                              |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int IndexByTicket(int ti, string sy="", int op=-1, int mn=-1) {
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if ((mn<0 || OrderMagicNumber()==mn) && OrderTicket()==ti) return(i);
      }
    }
  }
  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 28.11.2006                                                     |
//|  �������� : ���������� ���������� �������.                                 |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �����)                    |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int NumberOfOrders(string sy="", int op=-1, int mn=-1) {
  int i, k=OrdersTotal(), ko=0, ot;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      ot=OrderType();
      if (ot>1 && ot<6) {
        if ((OrderSymbol()==sy || sy=="") && (op<0 || ot==op)) {
          if (mn<0 || OrderMagicNumber()==mn) ko++;
        }
      }
    }
  }
  return(ko);
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
        pp=pb; clClose=clCloseBuy;
      } else {
        pp=pa; clClose=clCloseSell;
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
//|  ������   : 19.02.2008                                                     |
//|  �������� : �������� ��� �������, � ������� ������ � ������ ��������       |
//|             �������� ��������� ��������                                    |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    pr - ������                                                             |
//+----------------------------------------------------------------------------+
void ClosePosBySizeProfitInCurrency(string sy="", int op=-1, int mn=-1, double pr=0) {
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=k-1; i>=0; i--) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (OrderProfit()+OrderSwap()>pr) ClosePosBySelect();
          }
        }
      }
    }
  }
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 25.04.2008                                                     |
//|  �������� : �������� ��� �������, � ������� ������ � ������ ��������       |
//|             �������� ��������� ��������                                    |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    pr - ������/������                                                      |
//+----------------------------------------------------------------------------+
void ClosePosBySizeLossInCurrency(string sy="", int op=-1, int mn=-1, double pr=0) {
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=k-1; i>=0; i--) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (OrderProfit()+OrderSwap()<-MathAbs(pr)) ClosePosBySelect();
          }
        }
      }
    }
  }
}


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
void ClosePositions(string sy="", int op=-1, int mn=-1) {
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
//|  ������   : 19.02.2008                                                     |
//|  �������� : �������� ������� �� �������� ���� ������� ����������           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
void ClosePosFirstProfit(string sy="", int op=-1, int mn=-1) {
  int i, k=OrdersTotal();
  if (sy=="0") sy=Symbol();

  // ������� ��������� ���������� �������
  for (i=k-1; i>=0; i--) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (OrderProfit()+OrderSwap()>0) ClosePosBySelect();
          }
        }
      }
    }
  }
  // ����� ��� ���������
  k=OrdersTotal();
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
//|  ������   : 19.02.2008                                                     |
//|  �������� : �������� ����� ������� � ������������ ������������� ��������   |
//|             � ������ ��������                                              |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
void ClosePosWithMaxProfitInCurrency(string sy="", int op=-1, int mn=-1) {
  double pr=0;
  int    i, k=OrdersTotal(), np=-1;

  if (sy=="0") sy=Symbol();
  for (i=k-1; i>=0; i--) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (mn<0 || OrderMagicNumber()==mn) {
          if (pr<OrderProfit()+OrderSwap()) {
            pr=OrderProfit()+OrderSwap();
            np=i;
          }
        }
      }
    }
  }
  if (np>=0) {
    if (OrderSelect(np, SELECT_BY_POS, MODE_TRADES)) {
      ClosePosBySelect();
    }
  }
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���������� � ������� ����� ������ � ���������       |
//|             �������                                                        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ("" ��� NULL - ������� ������)          |
//|    op - �������� ��������          (    -1      - ����� �������)           |
//|    mn - MagicNumber                (    -1      - ����� �����)             |
//+----------------------------------------------------------------------------+
int DistMarketAndPos(string sy="", int op=-1, int mn=-1) {
  double d, p;
  int i, k=OrdersTotal(), r=1000000;

  if (sy=="" || sy=="0") sy=Symbol();
  p=MarketInfo(sy, MODE_POINT);
  if (p==0) if (StringFind(sy, "JPY")<0) p=0.0001; else p=0.01;
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy) && (op<0 || OrderType()==op)) {
        if (mn<0 || OrderMagicNumber()==mn) {
          if (OrderType()==OP_BUY) {
            d=MathAbs(MarketInfo(sy, MODE_ASK)-OrderOpenPrice())/p;
            if (r>d) r=NormalizeDouble(d, 0);
          }
          if (OrderType()==OP_SELL) {
            d=MathAbs(OrderOpenPrice()-MarketInfo(sy, MODE_BID))/p;
            if (r>d) r=NormalizeDouble(d, 0);
          }
        }
      }
    }
  }
  return(r);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���� ������������� ������� ��� ������ ����� �����   |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������        ("" ��� NULL - ������� ������)     |
//|    op - �������� ��������               (    -1      - ����� ��������)     |
//|    mn - MagicNumber                     (    -1      - ����� �����)        |
//|    ds - ���������� � ������� �� �����   (  1000000   - �� ���������)       |
//+----------------------------------------------------------------------------+
bool ExistOPNearMarket(string sy="", int op=-1, int mn=-1, int ds=1000000) {
  int i, k=OrdersTotal(), ot;

  if (sy=="" || sy=="0") sy=Symbol();
  double p=MarketInfo(sy, MODE_POINT);
  if (p==0) if (StringFind(sy, "JPY")<0) p=0.0001; else p=0.01;
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      ot=OrderType();
      if ((OrderSymbol()==sy) && (op<0 || ot==op)) {
        if (mn<0 || OrderMagicNumber()==mn) {
          if (ot==OP_BUY || ot==OP_BUYLIMIT || ot==OP_BUYSTOP) {
            if (MathAbs(MarketInfo(sy, MODE_ASK)-OrderOpenPrice())<ds*p) return(True);
          }
          if (ot==OP_SELL || ot==OP_SELLLIMIT || ot==OP_SELLSTOP) {
            if (MathAbs(OrderOpenPrice()-MarketInfo(sy, MODE_BID))<ds*p) return(True);
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
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���� ������������� ������� �� ���� ��������         |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - �������� ��������          (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    pp - ����                       ( 0   - ����� ����)                     |
//+----------------------------------------------------------------------------+
bool ExistPosByPrice(string sy="", int op=-1, int mn=-1, double pp=0) {
  double px, py;
  int    d, i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            d=MarketInfo(OrderSymbol(), MODE_DIGITS);
            px=NormalizeDouble(pp, d);
            py=NormalizeDouble(OrderOpenPrice(), d);
            if (pp<=0 || px==py) return(True);
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
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ����� ����� �������� �������                        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ( ""  - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - �������� ��������          ( -1  - ����� �������)                  |
//|    mn - MagicNumber                ( -1  - ����� �����)                    |
//+----------------------------------------------------------------------------+
double GetAmountLotFromOpenPos(string sy="", int op=-1, int mn=-1) {
  double l=0;
  int    i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              l+=OrderLots();
            }
          }
        }
      }
    }
  }
  return(l);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ������ ������ ��� ������� �� ������.                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    ti - ����� ������, �������                                              |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int GetIndexByTicket(int ti, string sy="", int op=-1, int mn=-1) {
  int i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if ((mn<0 || OrderMagicNumber()==mn) && OrderTicket()==ti) return(i);
      }
    }
  }
  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 10.05.2008                                                     |
//|  �������� : ���������� ���������� ���� �����.                              |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������� ������� ����                                        |
//|    y - ������ �������� ������� ����                                        |
//+----------------------------------------------------------------------------+
double Correlation(double& x[], double& y[]) {
  double co=0, sa=0, sb=0, sc=0, xs=0, ys=0;
  int    i, k=MathMin(ArraySize(x), ArraySize(y));

  if (k>0) {
    for (i=0; i<k; i++) {
      xs+=x[i]; ys+=y[i];
    }
    xs/=k; ys/=k;
    for (i=0; i<k; i++) {
      sa+=(x[i]-xs)*(y[i]-ys);
      sb+=(x[i]-xs)*(x[i]-xs);
      sc+=(y[i]-ys)*(y[i]-ys);
    }
    sb=MathSqrt(sb*sc);
    if (sb!=0) co=sa/sb;
  }
  return(co);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ������ ��������� �������� ������� ��� -1            |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int GetIndexLastPos(string sy="", int op=-1, int mn=-1) {
  datetime o;
  int      i, k=OrdersTotal(), r=-1;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (o<OrderOpenTime()) {
                o=OrderOpenTime();
                r=i;
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ������ ���� ��������� �������� ������� ��� -1       |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double GetLotLastPos(string sy="", int op=-1, int mn=-1) {
  datetime o;
  double   l=-1;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (o<OrderOpenTime()) {
                o=OrderOpenTime();
                l=OrderLots();
              }
            }
          }
        }
      }
    }
  }
  return(l);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ������������ ������ ���� �� �������� �������        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double GetMaxLotFromOpenPos(string sy="", int op=-1, int mn=-1) {
  double l=0;
  int    i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (l<OrderLots()) l=OrderLots();
            }
          }
        }
      }
    }
  }
  return(l);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ����������� ������ ���� �� �������� �������         |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double GetMinLotFromOpenPos(string sy="", int op=-1, int mn=-1) {
  double l=0;
  int    i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (l==0 || l>OrderLots()) l=OrderLots();
            }
          }
        }
      }
    }
  }
  return(l);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���������� �������.                                 |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int NumberOfPositions(string sy="", int op=-1, int mn=-1) {
  int i, k=OrdersTotal(), kp=0;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) kp++;
          }
        }
      }
    }
  }
  return(kp);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ��������� ������ � ������ ��������                  |
//|             �������� � ����������� ���� �������                           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������             (""   - ����� ������,         |
//|                                               NULL - ������� ������)       |
//|    op - ��������                             (-1   - ����� �������)        |
//|    mn - MagicNumber                          (-1   - ����� �����)          |
//|    dt - ���� � ����� � �������� � 1970 ����  ( 0   - � ������ �������)     |
//+----------------------------------------------------------------------------+
double GetProfitFromDateInCurrency(string sy="", int op=-1, int mn=-1, datetime dt=0)
{
  double p=0;
  int    i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (dt<OrderCloseTime()) {
              p+=OrderProfit()+OrderCommission()+OrderSwap();
            }
          }
        }
      }
    }
  }
  return(p);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ��������� ������ �������� ������� � ������ �������� |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double GetProfitOpenPosInCurrency(string sy="", int op=-1, int mn=-1) {
  double p=0;
  int    i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            p+=OrderProfit()+OrderCommission()+OrderSwap();
          }
        }
      }
    }
  }
  return(p);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ��������� ������ �������� ������� � �������         |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int GetProfitOpenPosInPoint(string sy="", int op=-1, int mn=-1) {
  double p;
  int    i, k=OrdersTotal(), pr=0;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (mn<0 || OrderMagicNumber()==mn) {
          p=MarketInfo(OrderSymbol(), MODE_POINT);
          if (p==0) if (StringFind(OrderSymbol(), "JPY")<0) p=0.0001; else p=0.01;
          if (OrderType()==OP_BUY) {
            pr+=(MarketInfo(OrderSymbol(), MODE_BID)-OrderOpenPrice())/p;
          }
          if (OrderType()==OP_SELL) {
            pr+=(OrderOpenPrice()-MarketInfo(OrderSymbol(), MODE_ASK))/p;
          }
        }
      }
    }
  }
  return(pr);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ����� ��������� �������� ������� ��� -1             |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int GetTicketLastPos(string sy="", int op=-1, int mn=-1) {
  datetime o;
  int      i, k=OrdersTotal(), r=-1;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ��� ��������� �������� ������� ��� -1               |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int GetTypeLastClosePos(string sy="", int mn=-1) {
  datetime t;
  int      i, k=OrdersHistoryTotal(), r=-1;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if ((OrderSymbol()==sy || sy=="") && (mn<0 || OrderMagicNumber()==mn)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (t<OrderCloseTime()) {
            t=OrderCloseTime();
            r=OrderType();
          }
        }
      }
    }
  }
  return(r);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ��� ��������� �������� ������� ��� -1               |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int GetTypeLastOpenPos(string sy="", int mn=-1) {
  datetime t;
  int      i, k=OrdersTotal(), r=-1;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (mn<0 || OrderMagicNumber()==mn)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (t<OrderOpenTime()) {
            t=OrderOpenTime();
            r=OrderType();
          }
        }
      }
    }
  }
  return(r);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.05.2008                                                     |
//|  �������� : ���������� ���� �������� ��������� ������� �� �����.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
bool isCloseLastPosByStop(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   ocp, osl;
  int      dg, i, j=-1, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) {
                t=OrderCloseTime();
                j=i;
              }
            }
          }
        }
      }
    }
  }
  if (OrderSelect(j, SELECT_BY_POS, MODE_HISTORY)) {
    dg=MarketInfo(sy, MODE_DIGITS);
    if (dg==0) if (StringFind(OrderSymbol(), "JPY")<0) dg=4; else dg=2;
    ocp=NormalizeDouble(OrderClosePrice(), dg);
    osl=NormalizeDouble(OrderStopLoss(), dg);
    if (ocp==osl) return(True);
  }
  return(False);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.05.2008                                                     |
//|  �������� : ���������� ���� �������� ��������� ������� �� �����.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
bool isCloseLastPosByTake(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   ocp, otp;
  int      dg, i, j=-1, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) {
                t=OrderCloseTime();
                j=i;
              }
            }
          }
        }
      }
    }
  }
  if (OrderSelect(j, SELECT_BY_POS, MODE_HISTORY)) {
    dg=MarketInfo(sy, MODE_DIGITS);
    if (dg==0) if (StringFind(OrderSymbol(), "JPY")<0) dg=4; else dg=2;
    ocp=NormalizeDouble(OrderClosePrice(), dg);
    otp=NormalizeDouble(OrderTakeProfit(), dg);
    if (ocp==otp) return(True);
  }
  return(False);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���� ����������� ��������� �������.                 |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
bool isLossLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  int      i, j=-1, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) {
                t=OrderCloseTime();
                j=i;
              }
            }
          }
        }
      }
    }
  }
  if (OrderSelect(j, SELECT_BY_POS, MODE_HISTORY)) {
    if (OrderProfit()<0) return(True);
  }
  return(False);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���� ������ �������.                                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
bool isTradeToDay(string sy="", int op=-1, int mn=-1) {
  int i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (TimeDay  (OrderOpenTime())==Day()
              &&  TimeMonth(OrderOpenTime())==Month()
              &&  TimeYear (OrderOpenTime())==Year()) return(True);
            }
          }
        }
      }
    }
  }
  k=OrdersTotal();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (TimeDay  (OrderOpenTime())==Day()
              &&  TimeMonth(OrderOpenTime())==Month()
              &&  TimeYear (OrderOpenTime())==Year()) return(True);
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
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ����� ���� �������� ��������� ������� ��� -1.       |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ("" ��� NULL - ������� ������)          |
//|    tf - ���������                  (    0       - ������� ���������)       |
//|    op - ��������                   (   -1       - ����� �������)           |
//|    mn - MagicNumber                (   -1       - ����� �����)             |
//+----------------------------------------------------------------------------+
int NumberOfBarCloseLastPos(string sy="0", int tf=0, int op=-1, int mn=-1) {
  datetime t;
  int      i, k=OrdersHistoryTotal();

  if (sy=="" || sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) t=OrderCloseTime();
            }
          }
        }
      }
    }
  }
  return(iBarShift(sy, tf, t, True));
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ����� ���� �������� ��������� ������� ��� -1.       |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ("" ��� NULL - ������� ������)          |
//|    tf - ���������                  (    0       - ������� ���������)       |
//|    op - ��������                   (   -1       - ����� �������)           |
//|    mn - MagicNumber                (   -1       - ����� �����)             |
//+----------------------------------------------------------------------------+
int NumberOfBarOpenLastPos(string sy="0", int tf=0, int op=-1, int mn=-1) {
  datetime t;
  int      i, k=OrdersTotal();

  if (sy=="" || sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) t=OrderOpenTime();
            }
          }
        }
      }
    }
  }
  return(iBarShift(sy, tf, t, True));
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���������� ��������� �������, �������� �������.     |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int NumberOfLossPosToday(string sy="", int op=-1, int mn=-1) {
  datetime t;
  int      i, k=OrdersHistoryTotal(), kp=0;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              t=OrderCloseTime();
              if (Year()==TimeYear(t) && DayOfYear()==TimeDayOfYear(t)) {
                if (OrderProfit()<0) kp++;
              }
            }
          }
        }
      }
    }
  }
  return(kp);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���� �������� ��������� �������� �������.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double PriceCloseLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=0;
  int      i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) {
                t=OrderCloseTime();
                r=OrderClosePrice();
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���� �������� ��������� �������� �������.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double PriceOpenLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=0;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 03.06.2008                                                     |
//|  �������� : ���������� ���� �������� ��������� �������� �������.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double PriceOpenLastClosePos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=0;
  int      i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) {
                t=OrderCloseTime();
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 04.06.2008                                                     |
//|  �������� : ���������� ���� �������� ��������� �������.                    |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double PriceOpenNearPos(string sy="", int op=-1, int mn=-1) {
  double mi, oop=0, p;
  int    i, k=OrdersTotal(), pp=0;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (OrderType()==OP_BUY)  mi=MarketInfo(OrderSymbol(), MODE_ASK);
            if (OrderType()==OP_SELL) mi=MarketInfo(OrderSymbol(), MODE_BID);
            p=MarketInfo(OrderSymbol(), MODE_POINT);
            if (p==0) if (StringFind(sy, "JPY")<0) p=0.0001; else p=0.01;
            if (pp==0 || pp>MathAbs(OrderOpenPrice()-mi)/p) {
              pp=MathAbs(OrderOpenPrice()-mi)/p;
              oop=OrderOpenPrice();
            }
          }
        }
      }
    }
  }
  return(oop);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 05.06.2008                                                     |
//|  �������� : ���������� ����� ��������� � ����� ������� �� ���� ��������.   |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int TicketNearPos(string sy="", int op=-1, int mn=-1) {
  double mi, p;
  int    i, k=OrdersTotal(), pp=0, ti=0;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (OrderType()==OP_BUY)  mi=MarketInfo(OrderSymbol(), MODE_ASK);
            if (OrderType()==OP_SELL) mi=MarketInfo(OrderSymbol(), MODE_BID);
            p=MarketInfo(OrderSymbol(), MODE_POINT);
            if (p==0) if (StringFind(sy, "JPY")<0) p=0.0001; else p=0.01;
            if (pp==0 || pp>MathAbs(OrderOpenPrice()-mi)/p) {
              pp=MathAbs(OrderOpenPrice()-mi)/p;
              ti=OrderTicket();
            }
          }
        }
      }
    }
  }
  return(ti);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 07.06.2008                                                     |
//|  �������� : ���������� ��� ��������� � ����� ������� ��� -1.               |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int TypeNearPos(string sy="", int op=-1, int mn=-1) {
  double mi, p;
  int    i, k=OrdersTotal(), pp=0, ty=-1;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (OrderType()==OP_BUY)  mi=MarketInfo(OrderSymbol(), MODE_ASK);
            if (OrderType()==OP_SELL) mi=MarketInfo(OrderSymbol(), MODE_BID);
            p=MarketInfo(OrderSymbol(), MODE_POINT);
            if (p==0) if (StringFind(sy, "JPY")<0) p=0.0001; else p=0.01;
            if (pp==0 || pp>MathAbs(OrderOpenPrice()-mi)/p) {
              pp=MathAbs(OrderOpenPrice()-mi)/p;
              ty=OrderType();
            }
          }
        }
      }
    }
  }
  return(ty);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ����� �������� ��������� �������� �������.          |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
datetime TimeOpenLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) t=OrderOpenTime();
            }
          }
        }
      }
    }
  }
  return(t);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ����� �������� ��������� �������� �������.          |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
datetime TimeSetLastOrder(string sy="", int op=-1, int mn=-1) {
  datetime t;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) t=OrderOpenTime();
            }
          }
        }
      }
    }
  }
  return(t);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 10.06.2008                                                     |
//|  �������� : ���������� ���������� ����� ����� ����� ���������� ����������. |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������        ("" ��� NULL - ������� ������)     |
//|    tf - ���������                       (    0       - ������� ��)         |
//+----------------------------------------------------------------------------+
int BarsBetweenLastFractals(string sy="", int tf=0) {
  double fu=0, fd=0;
  int    i, nu=0, nd=0;

  if (sy=="" || sy=="0") sy=Symbol();
  for (i=2; i<100; i++) {
    fu=iFractals(sy, tf, MODE_UPPER, i);
    if (fu!=0) {
      if (nu==0) nu=i;
    }
    fd=iFractals(sy, tf, MODE_LOWER, i);
    if (fd!=0) {
      if (nd==0) nd=i;
    }
    if (nu>0 && nd>0) return(MathAbs(nu-nd));
  }
  Print("BarsBetweenLastFractals(): �������� �� �������");
  return(-1);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���������� ������ ����� �������� ��������� �������. |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
datetime SecondsAfterCloseLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  int      i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) t=OrderCloseTime();
            }
          }
        }
      }
    }
  }
  return(TimeCurrent()-t);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 19.02.2008                                                     |
//|  �������� : ���������� ���������� ������ ����� �������� ��������� �������. |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
datetime SecondsAfterOpenLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) t=OrderOpenTime();
            }
          }
        }
      }
    }
  }
  return(TimeCurrent()-t);
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 16.06.2008                                                     |
//|  �������� : �������� �������, ��������������� �������                      |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
void DeleteOppositeOrders(string sy="", int op=-1, int mn=-1) {
  bool eb, es;

  switch (op) {
    case OP_BUY : eb=ExistPositions(sy, OP_BUY , mn); break;
    case OP_SELL: es=ExistPositions(sy, OP_SELL, mn); break;
    default:      eb=ExistPositions(sy, OP_BUY , mn);
                  es=ExistPositions(sy, OP_SELL, mn); break;
  }

  if (eb) {
    DeleteOrders(sy, OP_SELLLIMIT, mn);
    DeleteOrders(sy, OP_SELLSTOP , mn);
  }
  if (es) {
    DeleteOrders(sy, OP_BUYLIMIT, mn);
    DeleteOrders(sy, OP_BUYSTOP , mn);
  }
}


//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ��������� ����� �������� ������� �� ��������                   |
//|             � ���������� ������ ���������� �������� ��� -1.                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ������ ���������                                                    |
//|    e - �������� ��������                                                   |
//+----------------------------------------------------------------------------+
int ArraySearchDouble(double& m[], double e) {
  for (int i=0; i<ArraySize(m); i++) {
    if (m[i]==e) return(i);
  }
  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ��������� ����� �������� ������� �� ��������                   |
//|             � ���������� ������ ���������� �������� ��� -1.                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ������ ���������                                                    |
//|    e - �������� ��������                                                   |
//+----------------------------------------------------------------------------+
int ArraySearchInt(int& m[], int e) {
  for (int i=0; i<ArraySize(m); i++) {
    if (m[i]==e) return(i);
  }
  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 04.03.2008                                                     |
//|  �������� : ��������� ����� �������� ������� �� ��������                   |
//|             � ���������� ������ ���������� �������� ��� -1.                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ������ ���������                                                    |
//|    e - �������� ��������                                                   |
//+----------------------------------------------------------------------------+
int ArraySearchString(string& m[], string e) {
  for (int i=0; i<ArraySize(m); i++) {
    if (m[i]==e) return(i);
  }
  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 16.06.2008                                                     |
//|  �������� : ���������� ������ ���� ��������� �������� ������� ��� -1       |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double GetLotLastClosePos(string sy="", int op=-1, int mn=-1) {
  datetime o;
  double   l=-1;
  int      i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (o<OrderCloseTime()) {
                o=OrderCloseTime();
                l=OrderLots();
              }
            }
          }
        }
      }
    }
  }
  return(l);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 17.05.2008                                                     |
//|  �������� : ���������� �������� ������������� �������� �������.            |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������� ��������� ����                                      |
//+----------------------------------------------------------------------------+
double ArrayMax(double& x[]) {
  if (ArraySize(x)>0) return(x[ArrayMaximum(x)]);
  else {
    Print("ArrayMax(): ������ ����!");
    return(0);
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 17.05.2008                                                     |
//|  �������� : ���������� �������� ������������ �������� �������.             |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������� ��������� ����                                      |
//+----------------------------------------------------------------------------+
double ArrayMin(double& x[]) {
  if (ArraySize(x)>0) return(x[ArrayMinimum(x)]);
  else {
    Print("ArrayMin(): ������ ����!");
    return(0);
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 16.05.2008                                                     |
//|  �������� : ���������� ������� �������������� ��������� �������.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������� ��������� ����                                      |
//+----------------------------------------------------------------------------+
double ArrayAvg(double& x[]) {
  double s=0;
  int    i, k=ArraySize(x);

  for (i=0; i<k; i++) s+=x[i];
  if (k>0) s/=k; else Print("ArrayAvg(): ������ ����!");

  return(s);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 16.06.2008                                                     |
//|  �������� : ���������� ������� �������������� ��������� �������.           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������� ��������� ����                                      |
//+----------------------------------------------------------------------------+
double ArrayAvGeom(double& x[]) {
  double s=1, k=ArraySize(x);
  int    i;

  for (i=0; i<k; i++) s*=x[i];
  if (k>0) s=MathPow(s, 1/k); else Print("ArrayAvGeom(): ������ ����!");

  return(s);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 30.03.2008                                                     |
//|  �������� : ��������� ������� OBJ_HLINE �������������� �����               |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    cl - ���� �����                                                         |
//|    nm - ������������               ("" - ����� �������� �������� ����)     |
//|    p1 - ������� �������            (0  - Bid)                              |
//|    st - ����� �����                (0  - ������� �����)                    |
//|    wd - ������ �����               (0  - �� ���������)                     |
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

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 02.07.2008                                                     |
//|  �������� : ��������� ������� OBJ_VLINE ������������ �����                 |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    cl - ���� �����                                                         |
//|    nm - ������������               ("" - ����� �������� �������� ����)     |
//|    t1 - �����                      (0  - ����� �������� �������� ����)     |
//|    st - ����� �����                (0  - ������� �����)                    |
//|    wd - ������ �����               (1  - �� ���������)                     |
//+----------------------------------------------------------------------------+
void SetVLine(color cl, string nm="", datetime t1=0, int st=0, int wd=1) {
  if (nm=="") nm=DoubleToStr(Time[0], 0);
  if (t1<=0) t1=Time[0];
  if (ObjectFind(nm)<0) ObjectCreate(nm, OBJ_VLINE, 0, 0,0);
  ObjectSet(nm, OBJPROP_TIME1, t1);
  ObjectSet(nm, OBJPROP_COLOR, cl);
  ObjectSet(nm, OBJPROP_STYLE, st);
  ObjectSet(nm, OBJPROP_WIDTH, wd);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.10.2007                                                     |
//|  �������� : ��������� ������� OBJ_TREND ��������� �����                    |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    cl - ���� �����                                                         |
//|    nm - ������������               (  ""  - ����� �������� �������� ����)  |
//|    t1 - ����� �������� ����        (  0   - Time[10]                       |
//|    p1 - ������� �������            (  0   - Low[10])                       |
//|    t2 - ����� �������� ����        (  0   - ������� ���)                   |
//|    p2 - ������� �������            (  0   - Bid)                           |
//|    ry - ���                        (False - �� ���������)                  |
//|    st - ����� �����                (  0   - ������� �����)                 |
//|    wd - ������ �����               (  1   - �� ���������)                  |
//+----------------------------------------------------------------------------+
void SetTLine(color cl, string nm="",
              datetime t1=0, double p1=0, datetime t2=0, double p2=0,
              bool ry=False, int st=0, int wd=1) {
  if (nm=="") nm=DoubleToStr(Time[0], 0);
  if (t1<=0) t1=Time[10];
  if (p1<=0) p1=Low[10];
  if (t2<=0) t2=Time[0];
  if (p2<=0) p2=Bid;
  if (ObjectFind(nm)<0) ObjectCreate(nm, OBJ_TREND, 0, 0,0, 0,0);
  ObjectSet(nm, OBJPROP_TIME1 , t1);
  ObjectSet(nm, OBJPROP_PRICE1, p1);
  ObjectSet(nm, OBJPROP_TIME2 , t2);
  ObjectSet(nm, OBJPROP_PRICE2, p2);
  ObjectSet(nm, OBJPROP_COLOR , cl);
  ObjectSet(nm, OBJPROP_RAY   , ry);
  ObjectSet(nm, OBJPROP_STYLE , st);
  ObjectSet(nm, OBJPROP_WIDTH , wd);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.10.2007                                                     |
//|  �������� : ��������� ������� OBJ_TRENDBYANGLE ��������� ����� �� ����     |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    cl - ���� �����                                                         |
//|    nm - ������������               (  ""  - ����� �������� �������� ����)  |
//|    t1 - ����� �������� ����        (  0   - Time[10]                       |
//|    p1 - ������� �������            (  0   - Low[10])                       |
//|    t2 - ����� �������� ����        (  0   - ����� �������� �������� ����)  |
//|    p2 - ������� �������            (  0   - �� ����)                       |
//|    an - ����                       (  0   - �� ���������)                  |
//|    ry - ���                        (False - �� ���)                        |
//|    st - ����� �����                (  0   - ������� �����)                 |
//|    wd - ������ �����               (  1   - �� ���������)                  |
//+----------------------------------------------------------------------------+
void SetTLineByAngle(color cl, string nm="",
              datetime t1=0, double p1=0, datetime t2=0, double p2=0,
              double an=0, bool ry=False, int st=0, int wd=1) {
  if (nm=="") nm=DoubleToStr(Time[0], 0);
  if (t1<=0) t1=Time[10];
  if (p1<=0) p1=Low[10];
  if (t2<=0) t2=Time[0];
  if (ObjectFind(nm)<0) ObjectCreate(nm, OBJ_TRENDBYANGLE, 0, 0,0);
  ObjectSet(nm, OBJPROP_TIME1 , t1);
  ObjectSet(nm, OBJPROP_PRICE1, p1);
  ObjectSet(nm, OBJPROP_TIME2 , t2);
  if (p2>0) ObjectSet(nm, OBJPROP_PRICE2, p2);
  else ObjectSet(nm, OBJPROP_ANGLE, an);
  ObjectSet(nm, OBJPROP_COLOR, cl);
  ObjectSet(nm, OBJPROP_RAY  , ry);
  ObjectSet(nm, OBJPROP_STYLE, st);
  ObjectSet(nm, OBJPROP_WIDTH, wd);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.10.2007                                                     |
//|  �������� : ��������� ������ �� �������, ������� OBJ_ARROW.                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    cd - ��� ������                                                         |
//|    cl - ���� ������                                                        |
//|    nm - ������������               ("" - ����� �������� �������� ����)     |
//|    t1 - ����� �������� ����        (0  - ������� ���)                      |
//|    p1 - ������� �������            (0  - Bid)                              |
//|    sz - ������ ������              (0  - �� ���������)                     |
//+----------------------------------------------------------------------------+
void SetArrow(int cd, color cl,
              string nm="", datetime t1=0, double p1=0, int sz=0) {
  if (nm=="") nm=DoubleToStr(Time[0], 0);
  if (t1<=0) t1=Time[0];
  if (p1<=0) p1=Bid;
  if (ObjectFind(nm)<0) ObjectCreate(nm, OBJ_ARROW, 0, 0,0);
  ObjectSet(nm, OBJPROP_TIME1    , t1);
  ObjectSet(nm, OBJPROP_PRICE1   , p1);
  ObjectSet(nm, OBJPROP_ARROWCODE, cd);
  ObjectSet(nm, OBJPROP_COLOR    , cl);
  ObjectSet(nm, OBJPROP_WIDTH    , sz);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.10.2007                                                     |
//|  �������� : ��������� ��������� �����, ������ OBJ_LABEL.                   |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    nm - ������������ �������                                               |
//|    tx - �����                                                              |
//|    cl - ���� �����                                                         |
//|    xd - ���������� X � ��������                                            |
//|    yd - ���������� Y � ��������                                            |
//|    cr - ����� ���� ��������        (0 - ����� �������,                     |
//|                                     1 - ������ �������,                    |
//|                                     2 - ����� ������,                      |
//|                                     3 - ������ ������ )                    |
//|    fs - ������ ������              (9 - �� ���������  )                    |
//+----------------------------------------------------------------------------+
void SetLabel(string nm, string tx, color cl, int xd, int yd, int cr=0, int fs=9) {
  if (ObjectFind(nm)<0) ObjectCreate(nm, OBJ_LABEL, 0, 0,0);
  ObjectSetText(nm, tx, fs);
  ObjectSet(nm, OBJPROP_COLOR    , cl);
  ObjectSet(nm, OBJPROP_XDISTANCE, xd);
  ObjectSet(nm, OBJPROP_YDISTANCE, yd);
  ObjectSet(nm, OBJPROP_CORNER   , cr);
  ObjectSet(nm, OBJPROP_FONTSIZE , fs);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.10.2007                                                     |
//|  �������� : ��������� ���������� ����� ����������� ���� ������.            |
//|             ������ ������ ������� ����� ��������� ����� �����.            |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������              x[0], x[1] - ������ ������              |
//|                                    x[2], x[3] - ������ ������              |
//|    y - ������ �������              y[0], y[1] - ������ ������              |
//|                                    y[0], y[1] - ������ ������              |
//|    t - ������ ������� ���������    t[0]       - ��������                   |
//|                                    t[1]       - ��������                   |
//+----------------------------------------------------------------------------+
void CrossPointOfLines(double& x[], double& y[], double& t[]) {
  double z=(y[3]-y[2])*(x[1]-x[0])-(y[1]-y[0])*(x[3]-x[2]);
  ArrayResize(t, 2);
  ArrayInitialize(t, 0.0);

  if (z==0) Print("CrossPointOfLines(): �� ������� ����� ����� �����������!");
  else {
    double xy1=x[1]*y[0]-x[0]*y[1];
    double xy2=x[3]*y[2]-x[2]*y[3];
    t[0]=NormalizeDouble((xy1*(x[3]-x[2])-xy2*(x[1]-x[0]))/z, 0);
    t[1]=(xy1*(y[3]-y[2])-xy2*(y[1]-y[0]))/z;
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.10.2007                                                     |
//|  �������� : ��������� ������� OBJ_REGRESSION ����� �������� ���������.     |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    cl - ���� �����                                                         |
//|    nm - ������������               ( ""   - ����� �������� �������� ����)  |
//|    t1 - ����� �������� ����        (  0   - Time[10])                      |
//|    t2 - ����� �������� ����        (  0   - Time[0])                       |
//|    ry - ���                        (False - �� ���������)                  |
//|    st - ����� �����                (  0   - ������� �����)                 |
//|    wd - ������ �����               (  1   - �� ���������)                  |
//+----------------------------------------------------------------------------+
void SetRegression(color cl, string nm="", datetime t1=0, datetime t2=0,
                    bool ry=False, int st=STYLE_SOLID, int wd=1) {
  if (nm=="") nm=DoubleToStr(Time[0], 0);
  if (t1<=0) t1=Time[10];
  if (t2<=0) t2=Time[0];
  if (ObjectFind(nm)<0) ObjectCreate(nm, OBJ_REGRESSION, 0, 0,0, 0,0);
  ObjectSet(nm, OBJPROP_TIME1, t1);
  ObjectSet(nm, OBJPROP_TIME2, t2);
  ObjectSet(nm, OBJPROP_COLOR, cl);
  ObjectSet(nm, OBJPROP_RAY  , ry);
  ObjectSet(nm, OBJPROP_STYLE, st);
  ObjectSet(nm, OBJPROP_WIDTH, wd);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 07.10.2006                                                     |
//|  �������� : ���������� ����� ���� ���������� ������� �� ��� ������.        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL ��� "" - ������� ������)          |
//|    tf - ���������                  (      0     - ������� ��)              |
//|    ne - ����� ����������           (      0     - ���������)               |
//|    dp - ExtDepth                                                           |
//|    dv - ExtDeviation                                                       |
//|    bs - ExtBackstep                                                        |
//+----------------------------------------------------------------------------+
int GetExtremumZZBar(string sy="", int tf=0, int ne=0, int dp=12, int dv=5, int bc=3) {
  if (sy=="" || sy=="0") sy=Symbol();
  double zz;
  int    i, k=iBars(sy, tf), ke=0;

  for (i=0; i<k; i++) {
    zz=iCustom(sy, tf, "ZigZag", dp, dv, bc, 0, i);
    if (zz!=0) {
      ke++;
      if (ke>ne) return(i);
    }
  }
  Print("GetExtremumZZBar(): ��������� ������� ����� ",ne," �� ������");
  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 07.10.2006                                                     |
//|  �������� : ���������� ��������� ������� �� ��� ������.                    |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL ��� "" - ������� ������)          |
//|    tf - ���������                  (      0     - ������� ��)              |
//|    ne - ����� ����������           (      0     - ���������)               |
//|    dp - ExtDepth                                                           |
//|    dv - ExtDeviation                                                       |
//|    bs - ExtBackstep                                                        |
//+----------------------------------------------------------------------------+
double GetExtremumZZPrice(string sy="", int tf=0, int ne=0, int dp=12, int dv=5, int bs=3) {
  if (sy=="" || sy=="0") sy=Symbol();
  double zz;
  int    i, k=iBars(sy, tf), ke=0;

  for (i=1; i<k; i++) {
    zz=iCustom(sy, tf, "ZigZag", dp, dv, bs, 0, i);
    if (zz!=0) {
      ke++;
      if (ke>ne) return(zz);
    }
  }
  Print("GetExtremumZZPrice(): ��������� ������� ����� ",ne," �� ������");
  return(0);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 13.08.2008                                                     |
//|  �������� : ���������� ����� ���� �������� �� ��� ������.                  |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������        ("" ��� NULL - ������� ������)     |
//|    tf - ���������                       (    0       - ������� ��)         |
//|    nf - ����� ��������                  (    0       - ���������)          |
//+----------------------------------------------------------------------------+
int GetFractalBar(string sy="0", int tf=0, int nf=0) {
  if (sy=="" || sy=="0") sy=Symbol();
  double f=0;
  int    i, k=iBars(sy, tf), kf;

  for (i=2; i<k; i++) {
    f=iFractals(sy, tf, MODE_LOWER, i);
    if (f!=0) {
      kf++;
      if (kf>nf) return(i);
    }
    f=iFractals(sy, tf, MODE_UPPER, i);
    if (f!=0) {
      kf++;
      if (kf>nf) return(i);
    }
  }
  Print("GetFractalBar(): ������� �� ������");
  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 07.10.2006                                                     |
//|  �������� : ���������� ������� ������� ���������� ������� ��������         |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ("" ��� NULL - ������� ������)          |
//|    tf - ���������                  (    0       - ������� ���������)       |
//|    nl - ���������� ����� �����                                             |
//|    nr - ���������� ����� ������                                            |
//+----------------------------------------------------------------------------+
double GetNearestDownFractal(string sy="0", int tf=0, int nl=2, int nr=2) {
  bool f;
  int  fb, i, nb=-1;

  if (sy=="" || sy=="0") sy=Symbol();
  if (nl<1) nl=1;
  if (nr<1) nr=1;

  fb=nr;
  while (nb<0) {
    fb++;
    f=True;
    for (i=fb; i>fb-nr; i--) {
      if (iLow(sy, tf, i)>iLow(sy, tf, i-1)) { f=False; break; }
    }
    if (f) {
      for (i=fb; i<fb+nl; i++) {
        if (iLow(sy, tf, i)>iLow(sy, tf, i+1)) { f=False; break; }
      }
      if (f) { nb=fb; break; }
    }
  }

  return(iLow(sy, tf, nb));
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 07.10.2006                                                     |
//|  �������� : ���������� ��������� ������� �������                           |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL - ������� ������)                 |
//|    tf - ���������                  ( 0 - ������� ���������)                |
//|    nl - ���������� ����� �����                                             |
//|    nr - ���������� ����� ������                                            |
//+----------------------------------------------------------------------------+
double GetNearestUpFractal(string sy="0", int tf=0, int nl=2, int nr=2) {
  bool f;
  int  fb, i, nb=-1;

  if (sy=="" || sy=="0") sy=Symbol();
  if (nl<1) nl=1;
  if (nr<1) nr=1;

  fb=nr;
  while (nb<0) {
    fb++;
    f=True;
    for (i=fb; i>fb-nr; i--) {
      if (iHigh(sy, tf, i)<iHigh(sy, tf, i-1)) { f=False; break; }
    }
    if (f) {
      for (i=fb; i<fb+nl; i++) {
        if (iHigh(sy, tf, i)<iHigh(sy, tf, i+1)) { f=False; break; }
      }
      if (f) { nb=fb; break; }
    }
  }

  return(iHigh(sy, tf, nb));
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 02.03.2008                                                     |
//|  �������� : ������������ ��������� ��� ��������� �������������� ��4.       |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    TimeFrame - ��������� (���������� ������)      (0 - ������� ��)         |
//+----------------------------------------------------------------------------+
int CorrectTF(int TimeFrame=0) {
  if (TimeFrame==0) TimeFrame=Period();
  if (TimeFrame< PERIOD_M5                         ) return(PERIOD_M1);
  if (TimeFrame>=PERIOD_M5  && TimeFrame<PERIOD_M15) return(PERIOD_M5);
  if (TimeFrame>=PERIOD_M15 && TimeFrame<PERIOD_M30) return(PERIOD_M15);
  if (TimeFrame>=PERIOD_M30 && TimeFrame<PERIOD_H1 ) return(PERIOD_M30);
  if (TimeFrame>=PERIOD_H1  && TimeFrame<PERIOD_H4 ) return(PERIOD_H1);
  if (TimeFrame>=PERIOD_H4  && TimeFrame<PERIOD_D1 ) return(PERIOD_H4);
  if (TimeFrame>=PERIOD_D1  && TimeFrame<PERIOD_W1 ) return(PERIOD_D1);
  if (TimeFrame>=PERIOD_W1  && TimeFrame<PERIOD_MN1) return(PERIOD_W1);
  if (TimeFrame>=PERIOD_MN1                        ) return(PERIOD_MN1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 12.05.2008                                                     |
//|  �������� : ���������� ���� ������ ��������                                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|                                    (-2 - �����������)                      |
//|                                    (-1 - �������)                          |
//|    nq - ����� ��������             ( 0 - �������)                          |
//|                                    ( 1 - ���������)                        |
//|                                    ( 2 - �����������)                      |
//+----------------------------------------------------------------------------+
datetime DateBeginQuarter(int nq=0) {
  int ye=Year()-MathFloor(nq/4);
  nq=MathMod(nq, 4);
  int mo=Month()-MathMod(Month()+2, 3)+3*nq;
  if (mo<1) {
    mo+=12;
    ye--;
  }
  if (mo>12) {
    mo-=12;
    ye++;
  }

  return(StrToTime(ye+"."+mo+".01"));
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 13.05.2008                                                     |
//|  �������� : ���������� ���� ������������ �� ������ ������                  |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|                                    (-2 - �������������� ������)            |
//|                                    (-1 - ���������� ������)                |
//|    nn - ����� ������               ( 0 - ������� ������)                   |
//|                                    ( 1 - ��������� ������)                 |
//|                                    ( 2 - ����������� ������)               |
//+----------------------------------------------------------------------------+
datetime DateOfMonday(int nn=0) {
  datetime dt=StrToTime(TimeToStr(TimeCurrent(), TIME_DATE));

  while (TimeDayOfWeek(dt)!=1) dt-=24*60*60;
  dt+=nn*7*24*60*60;

  return (dt);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 07.08.2008                                                     |
//|  �������� : ���������� ������� ���� ��������� �� ��� ����������� ������.   |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    n - ����� �������� ����                                                 |
//+----------------------------------------------------------------------------+
int Fibonacci(int n) {
  int a=0, b=0, i=1, s=0;

  if (n==1) s=1;
  if (n>1) {
    s=1;
    while (i<n) {
      i++;
      a=b;
      b=s;
      s=a+b;
    }
  }
  return(s);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ���������� ������������ ������ ��.                             |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    mm - ������������� ������ ��                                            |
//+----------------------------------------------------------------------------+
string GetNameMA(int mm) {
  switch (mm) {
    case MODE_SMA : return("SMA");
    case MODE_EMA : return("EMA");
    case MODE_SMMA: return("SMMA");
    case MODE_LWMA: return("LWMA");
    default       : return("Unknown Method");
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ���������� ������� ������� � ������� ����� ����� ������.       |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������        ("" ��� NULL - ������� ������)     |
//|    tf - ���������                       (    0       - ������� ���������)  |
//|    n2 - ����� ������ ����               (    2       - ������ ���)         |
//|    n1 - ����� ������� ����              (    1       - ������ ���)         |
//|  ������������ ��������:                                                    |
//|    ������������� - ����� ������ N2 � N1 ��� ���� �����                     |
//|    ������������� - ����� ������ N2 � N1 ���� �������� �����                |
//+----------------------------------------------------------------------------+
int GetPriceDiffInPoint(string sy="0", int tf=0, int n2=2, int n1=1) {
  if (sy=="" || sy=="0") sy=Symbol();
  double p=MarketInfo(sy, MODE_POINT);
  int    d=MarketInfo(sy, MODE_DIGITS);
  int    dd=0, k=iBars(sy, tf);

  if (n1>k || n2>k)
    Print("GetPriceDiffInPoint(): ������������ ����� ��� ",sy," ",GetNameTF(tf));
  else {
    if (n1>0 && n2>0) {
      int d1=NormalizeDouble((iHigh(sy, tf, n1)-iLow(sy, tf, n2))/p, d);
      int d2=NormalizeDouble((iLow(sy, tf, n1)-iHigh(sy, tf, n2))/p, d);

      if (MathAbs(d1)>MathAbs(d2)) dd=d1;
      if (MathAbs(d1)<MathAbs(d2)) dd=d2;
      if (MathAbs(d1)==MathAbs(d2)) {
        if (iOpen(sy, tf, n2)>iClose(sy, tf, n1)) dd=d2; else dd=d1;
      }
    }
  }

  return(dd);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.09.2005                                                     |
//|  �������� : ���������� ������������ ���� ����.                             |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    Applied_Price - ��� ����                                                |
//+----------------------------------------------------------------------------+
string GetTypePrice(int Applied_Price=0) {
  switch (Applied_Price) {
    case PRICE_CLOSE   : return("Close");
    case PRICE_OPEN    : return("Open");
    case PRICE_HIGH    : return("High");
    case PRICE_LOW     : return("Low");
    case PRICE_MEDIAN  : return("Median");
    case PRICE_TYPICAL : return("Typical");
    case PRICE_WEIGHTED: return("Weighted");
    default            : return("Unknown Type Price");
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 20.05.2008                                                     |
//|  �������� : ��������� ������ �������� �������� ���������.                  |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������� ��������� ����                                      |
//|    y - ������ �������� �������� ���������                                  |
//+----------------------------------------------------------------------------+
void ArrayLR(double& x[], double& y[]) {
  double a, b, c, sx=0, sx2=0, sxy=0, sy=0;
  int    i, n=ArraySize(x);

  if (n>1) {
    for (i=0; i<n; i++) {
      sx+=i+1;
      sy+=x[i];
      sxy+=(i+1)*x[i];
      sx2+=(i+1)*(i+1);
    }
    a=sx*sy-n*sxy;
    c=sx*sx-n*sx2;
    if (c!=0) a=a/c; else a=0;
    b=(sy-a*sx)/n;
    ArrayResize(y, n);
    for (i=0; i<n; i++) y[i]=a*(i+1)+b;
  } else Print("ArrayLR(): ������������� ���������� ��������� ����! n=", n);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 21.06.2008                                                     |
//|  �������� : ���������� ���� - �������� ������ ��������� �������������.     |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    x - ������ �������� ��������� ����                                      |
//|    d - �������� �������� ��������� ����, ���������� ������ ����� �������   |
//+----------------------------------------------------------------------------+
double ArrayMo(double& x[], int d=4) {
  double e, s=0;
  double m[][2];             // ��������� ������:
                             //  ������� 1 - ���������� ��������
                             //  ������� 2 - ��������
  int    i, k=ArraySize(x);
  int    n;                  // ����� ������ ���������� ������� m
  int    r;                  // ���������� ����� �� ��������� ������� m

  if (k>0) {
    for (i=0; i<k; i++) {
      e=NormalizeDouble(x[i], d);
      n=ArraySearchDouble(m, e);
      if (n<0) {
        r=ArrayRange(m, 0);
        ArrayResize(m, r+1);
        m[r][0]++;
        m[r][1]=e;
      } else m[n][0]++;
    }
    ArraySort(m, WHOLE_ARRAY, 0, MODE_DESCEND);
    s=m[0][1];
  } else Print("ArrayMo(): ������ ����!");

  return(s);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 30.07.2008                                                     |
//|  �������� : ���������� ���� ������������� ������� �� ���� ���������        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    pp - ����                       (-1   - ����� ����)                     |
//+----------------------------------------------------------------------------+
bool ExistOrdersByPrice(string sy="", int op=-1, int mn=-1, double pp=-1) {
  int d, i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (OrderType()>1 && OrderType()<6) {
          d=MarketInfo(OrderSymbol(), MODE_DIGITS);
          pp=NormalizeDouble(pp, d);
          if (pp<0 || pp==NormalizeDouble(OrderOpenPrice(), d)) {
            if (mn<0 || OrderMagicNumber()==mn) return(True);
          }
        }
      }
    }
  }
  return(False);
}

//+----------------------------------------------------------------------------+
//|  �����   : ��� ����� �. aka KimIV,  http://www.kimiv.ru                    |
//+----------------------------------------------------------------------------+
//|  ������  : 13.06.2007                                                      |
//|  ��������: �������� ����� �������������� ��������� �������                 |
//+----------------------------------------------------------------------------+
void ClosePosBySelectLite() {
  double pp;

  if (OrderType()==OP_BUY) {
    pp=MarketInfo(OrderSymbol(), MODE_BID);
    OrderClose(OrderTicket(), OrderLots(), pp, Slippage, clCloseBuy);
  }
  if (OrderType()==OP_SELL) {
    pp=MarketInfo(OrderSymbol(), MODE_ASK);
    OrderClose(OrderTicket(), OrderLots(), pp, Slippage, clCloseSell);
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 03.08.2008                                                     |
//|  �������� : ������������ ���������� ������� �� �����.                      |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    mo - ������ ���������� ������� �� �����                                 |
//|    mn - MagicNumber                          (-1 - ����� �����)            |
//+----------------------------------------------------------------------------+
void CountOrders(int& mo[], int mn=-1) {
  int i, k=OrdersTotal();

  if (ArraySize(mo)!=6) ArrayResize(mo, 6);
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (mn<0 || OrderMagicNumber()==mn) mo[OrderType()]++;
    }
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 28.03.2008                                                     |
//|  �������� : ����������� ������. ������ ������� ��� ������ �� �������.      |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    pp - ���� �������� �������, ��������� ������                            |
//|    sl - ������� ������� �����                                              |
//|    tp - ������� ������� �����                                              |
//|    ex - ���� ���������                                                     |
//+----------------------------------------------------------------------------+
void ModifyOrderLite(double pp=-1, double sl=0, double tp=0, datetime ex=0) {
  int    dg=MarketInfo(OrderSymbol(), MODE_DIGITS), er;
  double op=NormalizeDouble(OrderOpenPrice() , dg);
  double os=NormalizeDouble(OrderStopLoss()  , dg);
  double ot=NormalizeDouble(OrderTakeProfit(), dg);
  color  cl;

  if (pp<=0) pp=OrderOpenPrice();
  if (sl<0 ) sl=OrderStopLoss();
  if (tp<0 ) tp=OrderTakeProfit();
  
  pp=NormalizeDouble(pp, dg);
  sl=NormalizeDouble(sl, dg);
  tp=NormalizeDouble(tp, dg);

  if (pp!=op || sl!=os || tp!=ot) {
    if (MathMod(OrderType(), 2)==0) cl=clModifyBuy; else cl=clModifySell;
    if (!OrderModify(OrderTicket(), pp, sl, tp, ex, cl)) {
      er=GetLastError();
      Print("Error(",er,") modifying order: ",ErrorDescription(er));
      Print("Ask=",Ask," Bid=",Bid," sy=",OrderSymbol(),
            " op="+GetNameOP(OrderType())," pp=",pp," sl=",sl," tp=",tp);
    }
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 18.07.2008                                                     |
//|  �������� : ���������� ���� �� ���� �������� ������������ �� �������.      |
//+----------------------------------------------------------------------------+
color IIFc(bool condition, color ifTrue, color ifFalse) {
  if (condition) return(ifTrue); else return(ifFalse);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.02.2008                                                     |
//|  �������� : ���������� ���� �� ���� �������� ������������ �� �������.      |
//+----------------------------------------------------------------------------+
double IIFd(bool condition, double ifTrue, double ifFalse) {
  if (condition) return(ifTrue); else return(ifFalse);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 01.02.2008                                                     |
//|  �������� : ���������� ���� �� ���� �������� ������������ �� �������.      |
//+----------------------------------------------------------------------------+
int IIFi(bool condition, int ifTrue, int ifFalse) {
  if (condition) return(ifTrue); else return(ifFalse);
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
//|  ������   : 27.08.2008                                                     |
//|  �������� : ���������� ���� ������������� � ������� ������� ��� ������,    |
//|           : �������� (���������) ����� ������.                            |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� ��������)                 |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    d1 - ����� ��������             ( 0   - ����� ����� ��������)           |
//|    d2 - ����� ��������             ( 0   - ����� ����� ��������)           |
//+----------------------------------------------------------------------------+
bool ExistInHistoryCloseBetween(string sy="", int op=-1, int mn=-1,
                                datetime d1=0, datetime d2=0) {
  int i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (mn<0 || OrderMagicNumber()==mn) {
          if (d1<=OrderCloseTime() && (d2==0 || d2>=OrderCloseTime())) return(True);
        }
      }
    }
  }
  return(False);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 27.08.2008                                                     |
//|  �������� : ���������� ���� ������������� � ������� ������� ��� ������,    |
//|           : �������� (��������������) ����� ������.                        |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//|    d1 - ����� ��������             ( 0   - ����� ����� ��������)           |
//|    d2 - ����� ��������             ( 0   - ����� ����� ��������)           |
//+----------------------------------------------------------------------------+
bool ExistInHistoryOpenBetween(string sy="", int op=-1, int mn=-1,
                               datetime d1=0, datetime d2=0) {
  int i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if ((OrderSymbol()==sy || sy=="") && (op<0 || OrderType()==op)) {
        if (mn<0 || OrderMagicNumber()==mn) {
          if (d1<=OrderOpenTime() && (d2==0 || d2>=OrderOpenTime())) return(True);
        }
      }
    }
  }
  return(False);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 06.08.2008                                                     |
//|  �������� : ���������� ���� ������� ������ ��� ������� � ������� �� �������|
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
bool ExistInHistoryToDay(string sy="", int op=-1, int mn=-1) {
  int i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (op<0 || OrderType()==op) {
          if (mn<0 || OrderMagicNumber()==mn) {
            if (TimeDay  (OrderOpenTime())==Day()
            &&  TimeMonth(OrderOpenTime())==Month()
            &&  TimeYear (OrderOpenTime())==Year()) return(True);
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
//|  ������   : 06.08.2008                                                     |
//|  �������� : ���������� ���� TakeProfit ��������� �������� ������� ��� -1.  |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double TakeProfitLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=-1;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) {
                t=OrderOpenTime();
                r=OrderTakeProfit();
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 20.10.2008                                                     |
//|  �������� : ���������� ���� TakeProfit ��������� �������� ������� ��� -1.  |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   (-1   - ����� �������)                  |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
double TakeProfitLastClosePos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=-1;
  int      i, k=OrdersHistoryTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderCloseTime()) {
                t=OrderCloseTime();
                r=OrderTakeProfit();
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
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 11.09.2008                                                     |
//|  �������� : ������� ������ ����� � ���������                               |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   ( ""  - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    op - ��������                   ( -1  - ����� �������)                  |
//|    mn - MagicNumber                ( -1  - ����� �����)                    |
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
            if (!TSProfitOnly || pp-OrderOpenPrice()>TStop.Buy*po) {
              if (OrderStopLoss()<pp-(TStop.Buy+TrailingStep-1)*po) {
                ModifyOrder(-1, pp-TStop.Buy*po, -1);
              }
            }
          }
          if (OrderType()==OP_SELL) {
            pp=MarketInfo(OrderSymbol(), MODE_ASK);
            if (!TSProfitOnly || OrderOpenPrice()-pp>TStop.Sell*po) {
              if (OrderStopLoss()>pp+(TStop.Sell+TrailingStep-1)*po || OrderStopLoss()==0) {
                ModifyOrder(-1, pp+TStop.Sell*po, -1);
              }
            }
          }
        }
      }
    }
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 27.10.2008                                                     |
//|  �������� : ���������� Z-���� ��������� ����.                              |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    arr - ������ �������� ��������� ����                                    |
//+----------------------------------------------------------------------------+
double ArrayZ(double& arr[]) {
  double x, z;
  int    i, l=0, n=ArraySize(arr), r=1, w=0;

  for (i=0; i<n; i++) {
    if (i==0) r=1;
    else {
      if (arr[i-1]*arr[i]<0) r++;
    }
    if (arr[i]>0) w++; else l++;
  }

  if (n>2) {
    if (w>0 && l>0) {
      x=2*w*l;
      if (x!=n) z=(n*(r-0.5)-x)/MathSqrt(x*(x-n)/(n-1));
    } else {
      if (l==0) z=100; else z=-100;
      Print("ArrayZ(): ��� ����������� �����!");
    }
    return(z);
  } else {
    Print("ArrayZ(): � ������� ������������ ���������!");
    return(0);
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 31.10.2008                                                     |
//|  �������� : ��������� �������� �������� ������� � �������� ��������.       |
//|             ���������� ������ ������ ������� ��� -1,                       |
//|             ���� �� ������� ������ �������.                                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ������ ���������                                                    |
//|    i - ������ ��������                                                     |
//+----------------------------------------------------------------------------+
int ArrayDeleteInt(int& m[], int i) {
  int j, k=ArraySize(m);

  if (i>=0 && i<k) {
    for (j=i; j<k; j++) m[j]=m[j+1];
    k=ArrayResize(m, k-1);
    return(k);
  } else Print("ArrayDeleteInt(): �������� ������ �������� �������! i=", i);

  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 31.10.2008                                                     |
//|  �������� : ��������� �������� �������� ������� � �������� ��������.       |
//|             ���������� ������ ������ ������� ��� -1,                       |
//|             ���� �� ������� ������ �������.                                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ������ ���������                                                    |
//|    i - ������ ��������                                                     |
//+----------------------------------------------------------------------------+
int ArrayDeleteDouble(double& m[], int i) {
  int j, k=ArraySize(m);

  if (i>=0 && i<k) {
    for (j=i; j<k; j++) m[j]=m[j+1];
    k=ArrayResize(m, k-1);
    return(k);
  } else Print("ArrayDeleteDouble(): �������� ������ �������� �������! i=", i);

  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 31.10.2008                                                     |
//|  �������� : ��������� �������� �������� ������� � �������� ��������.       |
//|             ���������� ������ ������ ������� ��� -1,                       |
//|             ���� �� ������� ������ �������.                                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ������ ���������                                                    |
//|    i - ������ ��������                                                     |
//+----------------------------------------------------------------------------+
int ArrayDeleteString(string& m[], int i) {
  int j, k=ArraySize(m);

  if (i>=0 && i<k) {
    for (j=i; j<k; j++) m[j]=m[j+1];
    k=ArrayResize(m, k-1);
    return(k);
  } else Print("ArrayDeleteString(): �������� ������ �������� �������! i=", i);

  return(-1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 31.10.2008                                                     |
//|  �������� : ��������� ������� �������� ������� � �������� ��������.        |
//|             ���������� ������ ������ �������.                              |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    m - ������ ��������� ���� double                                        |
//|    e - �������� ��������                                                   |
//|    i - ������ ��������                  (-1 - �������� � ����� �������)    |
//+----------------------------------------------------------------------------+
int ArrayInsertDouble(double& m[], double e, int i=-1) {
  int j, k=ArraySize(m);

  ArrayResize(m, k+1);
  if (i>=0 && i<k) {
    for (j=k; j>i; j--) m[j]=m[j-1];
    m[i]=e;
  } else m[k]=e;

  return(k+1);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 04.11.2008                                                     |
//|  �������� : ��������� ����������� ���������� ��������� ���������� �������. |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    a - ������ ���������                                                    |
//|    r - ������� ����������          (     0       - ������ (� �������� 0))  |
//|    m - ����������� ����������      (MODE_ASCEND  - �� �����������,         |
//|                                     MODE_DESCEND - �� ��������)            |
//+----------------------------------------------------------------------------+
void BubbleSort2(double& a[][], int r=0, int m=MODE_ASCEND) {
  double t;
  int    e, i, j;
  int    k=ArrayRange(a, 1);      // ���������� �������
  int    n=ArrayRange(a, 0);      // ���������� �����

  if (r<0) r=0;
  if (r>k) r=k;

  for (i=n-1; i>0; i--) {
    for (j=0; j<i; j++) {
      if (m==MODE_ASCEND) {
        // �� �����������
        if (a[j][r]>a[j+1][r]) {
          for (e=0; e<k; e++) {
            t=a[j][e];
            a[j][e]=a[j+1][e];
            a[j+1][e]=t;
          }
        }
      } else {
        // �� ��������
        if (a[j][r]<a[j+1][r]) {
          for (e=0; e<k; e++) {
            t=a[j][e];
            a[j][e]=a[j+1][e];
            a[j+1][e]=t;
          }
        }
      }
    }
  }
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 13.10.2008                                                     |
//|  �������� : ���������� ��� ���������� ��������� ������ ��� -1             |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (""   - ����� ������,                   |
//|                                     NULL - ������� ������)                 |
//|    mn - MagicNumber                (-1   - ����� �����)                    |
//+----------------------------------------------------------------------------+
int GetTypeLastDeleted(string sy="", int mn=-1) {
  datetime t;
  int      i, k=OrdersHistoryTotal(), r=-1;

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) {
      if ((OrderSymbol()==sy || sy=="") && (mn<0 || OrderMagicNumber()==mn)) {
        if (OrderType()>1 && OrderType()<6 && t<OrderCloseTime()) {
          t=OrderCloseTime();
          r=OrderType();
        }
      }
    }
  }
  return(r);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 28.03.2008                                                     |
//|  �������� : ���������� ������ ����������� ���� ��� -1.                     |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL ��� "" - ������� ������)          |
//|    tf - ���������                  (          0 - ������� ���������)       |
//|    ty - ��� ��������� ������       (          0 - H-L, 1 - O-C)            |
//|    co - ����� ��������� ���������  (          0 - ��� ��������)            |
//|    in - ������ ���������� ����     (          0 - ������� ���)             |
//+----------------------------------------------------------------------------+
int iBarLargest(string sy="", int tf=0, int ty=0, int co=0, int in=0) {
  if (sy=="" || sy=="0") sy=Symbol();
  if (tf<=0) tf=Period();
  if (in< 0) in=0;
  if (co<=0) co=iBars(sy, tf)-in;

  double r, rb=0;       // ������ ����
  int    i, nb=-1;      // ������� � ����� ����

  for (i=co+in; i>=in; i--) {
    if (ty>0) r=MathAbs(iOpen(sy, tf, i)-iClose(sy, tf, i));
    else r=iHigh(sy, tf, i)-iLow(sy, tf, i);
    if (rb<r) {
      rb=r;
      nb=i;
    }
  }

  return(nb);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 26.02.2008                                                     |
//|  �������� : ���������� ��������� ����� ���� �� ������ �����.               |
//|           : ��������� ����� ���������� � 1 (�������).                      |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    tf - ���������                       (0 - ������� ���������)            |
//|    dt - ���� � ����� �������� ����      (0 - ������� �����)                |
//+----------------------------------------------------------------------------+
int iBarOfDayCalc(int tf=0, datetime dt=0) {
  if (tf<=0) tf=Period();
  if (dt<=0) dt=TimeCurrent();
  if (tf>PERIOD_D1) {
    Print("iBarOfDayCalc(): ��������� ������ ���� ������ ��� ����� D1");
    return(0);
  }
  double ms=MathMod(dt/60, 1440);      // ���������� ����� �� ������ �����
  int    bd=MathFloor(ms/tf)+1;        // ����� ���� �� ������ �����

  return(bd);
}

//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 26.02.2008                                                     |
//|  �������� : ���������� �������� ����� ���� �� ������ �����.                |
//+----------------------------------------------------------------------------+
//|  ���������:                                                                |
//|    sy - ������������ �����������   (NULL ��� "" - ������� ������)          |
//|    tf - ���������                  (          0 - ������� ���������)       |
//|    dt - ���� � ����� �������� ���� (          0 - ������� �����)           |
//+----------------------------------------------------------------------------+
int iBarOfDayReal(string sy="", int tf=0, datetime dt=0) {
  if (sy=="" || sy=="0") sy=Symbol();
  if (tf<=0) tf=Period();
  if (dt<=0) dt=TimeCurrent();
  if (tf>PERIOD_D1) {
    Print("iBarOfDayReal(): ��������� ������ ���� ������ ��� ����� D1");
    return(0);
  }

  int cd=TimeDay(dt);                       // ������� ���� ������
  int nb=iBarShift(sy, tf, dt, False);      // ����� �������� ����
  int bd=0;                                 // ����� ���� �� ������ �����

  while(TimeDay(iTime(sy, tf, nb))==cd) {
    nb++;
    bd++;
  }

  return(bd);
}

