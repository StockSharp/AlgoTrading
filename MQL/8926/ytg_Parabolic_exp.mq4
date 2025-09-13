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

extern int    StopLoss         = 500;  // ������ ����� � �������
extern int    TakeProfit       = 500; // ������ ����� � �������
extern double Lotsi = 0.1;
extern int    MagicNumber      = 28081975;   //���������� ����� �������

color clOpenBuy                = LightBlue;     // ���� ������ �������� �������
color clOpenSell               = LightCoral;    // ���� ������ �������� �������
color clCloseBuy               = Blue;     // ���� ������ �������� �������
color clCloseSell              = Coral;    // ���� ������ �������� �������
extern int    Slippage         = 30;            // ��������������� ����
extern int    NumberOfTry      = 5;             // ���������� �������� �������
bool   UseSound                = True;          // ������������ �������� ������
string NameFileSound           = "expert.wav";  // ������������ ��������� �����
int    NumberAccount           = 0;             // ����� ��������� �����
bool   ShowComment             = True;          // ���������� �����������
//------- ���������� ���������� ��������� -------------------------------------+
bool  gbDisabled               = False;         // ���� ���������� ���������
bool  gbNoInit                 = False;         // ���� ��������� �������������
//------- ����������� ������� ������� -----------------------------------------+
#include <stdlib.mqh>        // ����������� ���������� ��4

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
   double SPREAD    = MarketInfo(Symbol(),MODE_SPREAD);//����� � �������
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
    if(!ExistOrders(NULL,OP_BUYLIMIT,MagicNumber) && parab<low && pb>parab+STOPLEVEL*mp)//������ ���
    {//�������������     
     if (StopLoss  >0) sl=parab-StopLoss*mp;   else sl=0;
     if (TakeProfit>0) tp=parab+TakeProfit*mp; else tp=0;     
     SetOrder(NULL,OP_BUYLIMIT,Lotsi,parab,sl,tp,MagicNumber,0);
    }
    if(ExistOrders(NULL,OP_BUYLIMIT,MagicNumber) && pb>parab+STOPLEVEL*mp)//����� ����
    {//������������
     ti = TicketPos(NULL,OP_BUYLIMIT,MagicNumber);
     if (OrderSelect(ti, SELECT_BY_TICKET))
       {
        if (StopLoss  >0) sl=parab-StopLoss*mp;   else sl=0;
        if (TakeProfit>0) tp=parab+TakeProfit*mp; else tp=0;
        ModifyOrder(parab,sl,tp,Turquoise);
       }
    }
//--------------------------------------------------------------sell
    if(!ExistOrders(NULL,OP_SELLLIMIT,MagicNumber) && parab>high && pa<parab-STOPLEVEL*mp)//������ ���
    {//�������������     
     if (StopLoss  >0) sl=parab+StopLoss*mp;   else sl=0;
     if (TakeProfit>0) tp=parab-TakeProfit*mp; else tp=0;     
     SetOrder(NULL,OP_SELLLIMIT,Lotsi,parab,sl,tp,MagicNumber,0);
    }
    if(ExistOrders(NULL,OP_SELLLIMIT,MagicNumber) && pa<parab-STOPLEVEL*mp)//����� ����
    {//������������
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
// ������� �������� ������ ����                                                |
//-----------------------------------------------------------------------------+
bool NevBar(){
   static int PrevTime=0;
   if (PrevTime==iTime(Symbol(),timeframe,0)) return(false);
   PrevTime=iTime(Symbol(),timeframe,0);
   return(true);}
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
//+----------------------------------------------------------------------------+
//|  �����    : ��� ����� �. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  ������   : 13.03.2008                                                     |
//|  �������� : ��������� ������.                                              |
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
      Print("SetOrder(): ��������� ������ �������");
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
      // ������������ �����
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
        Print("SetOrder(): ��������������� ������� ������");
      }
      Print("Error(",err,") set order: ",ErrorDescription(err),", try ",it);
      Print("Ask=",pa,"  Bid=",pb,"  sy=",sy,"  ll=",ll,"  op=",GetNameOP(op),
            "  pp=",pp,"  sl=",sl,"  tp=",tp,"  mn=",mn);
      if (pa==0 && pb==0) Message("SetOrder(): ��������� � ������ ����� ������� ������� "+sy);
      // ���������� ������ ���������
      if (err==2 || err==64 || err==65 || err==133) {
        gbDisabled=True; break;
      }
      // ���������� �����
      if (err==4 || err==131 || err==132) {
        Sleep(1000*300); break;
      }
      // ������� ������ ������� (8) ��� ������� ����� �������� (141)
      if (err==8 || err==141) Sleep(1000*100);
      if (err==139 || err==140 || err==148) break;
      // �������� ������������ ���������� ��������
      if (err==146) while (IsTradeContextBusy()) Sleep(1000*11);
      // ��������� ���� ���������
      if (err==147) {
        ex=0; continue;
      }
      if (err!=135 && err!=138) Sleep(1000*7.7);
    }
  }
}
//+----------------------------------------------------------------------------+
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