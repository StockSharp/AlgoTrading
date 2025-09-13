//+------------------------------------------------------------------+
//|                                                         Elli.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern double TakeProfit        = 60;
extern double StopLoss          = 30;
extern double Risk              = 5;
extern int    MagicNumber       = 5101979;

extern int    tenkan            = 19;
extern int    kijun             = 60;
extern int    senkou_b          = 120;
extern int    po                = 20;

extern int    adx_timeframe     = 1;              //1,5,15
extern int    adx_period        = 10;             //6
extern int    adx_applied_price = 0;              //0-6
extern int    convert_high      = 13;             //7-20
extern int    convert_low       = 6;              //1-7

//------- ������� ��������� ������� -------------------------------------------+

bool          MarketWatch       = False;          // �������� ����������
int           Slippage          = 3;              // ��������������� ����
int           NumberOfTry       = 3;              // ���������� �������� �������

//------- ���������� ���������� ������� ---------------------------------------+
bool          gbDisabled        = False;          // ����������
bool          UseSound          = True;           // ������������ �������� ������
string        NameFileSound     = "expert.wav";   // ������������ ��������� �����
color         clOpenBuy         = LightBlue;      // ���� ������ �������� Buy
color         clOpenSell        = LightCoral;     // ���� ������ �������� Sell

//------- ����������� ������� ������� ------------------------------------------
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
//----
   int ticket, total;
   double Lots = GetLot(),sl=0,tp=0;

   total=OrdersTotal();
   
   if(total<1) 
    {
      if(Signal()==1)
       {
        if (StopLoss  >0) sl=Ask-StopLoss*Point;   else sl=0;
        if (TakeProfit>0) tp=Ask+TakeProfit*Point; else tp=0;
        OpenPosition(NULL, OP_BUY, Lots, sl, tp, MagicNumber);
       }
      
      if(Signal()==-1)
       {
        if (StopLoss  >0) sl=Bid+StopLoss*Point;   else sl=0;
        if (TakeProfit>0) tp=Bid-TakeProfit*Point; else tp=0;
        OpenPosition(NULL, OP_SELL, Lots, sl, tp, MagicNumber);
       }
     return(0);
     
    }   
//----
   return(0);
  }
//+------------------------------------------------------------------+
////////////////////////////////////////////////////////////////////////////////////////////
 double Signal()
  {
    double adx_plus_0 = iADX(NULL,adx_timeframe,adx_period,adx_applied_price,1,0);
    double adx_plus_1 = iADX(NULL,adx_timeframe,adx_period,adx_applied_price,1,1);
    double adx_minus_0 = iADX(NULL,adx_timeframe,adx_period,adx_applied_price,2,0);
    double adx_minus_1 = iADX(NULL,adx_timeframe,adx_period,adx_applied_price,2,1);
  
    double ts = iIchimoku(NULL,60,tenkan,kijun,senkou_b,1,0),
           ks = iIchimoku(NULL,60,tenkan,kijun,senkou_b,2,0),
           sa = iIchimoku(NULL,60,tenkan,kijun,senkou_b,3,0),
           sb = iIchimoku(NULL,60,tenkan,kijun,senkou_b,4,0),
        close = iClose(NULL,60,0),
           cf = MathAbs(ts-ks)/Point;
        
    int ytg_Signal = 0;
        
    if (ts>ks && ks>sa && sa>sb && close>ks && adx_plus_1<convert_low && adx_plus_0 >convert_high && cf > po )
     ytg_Signal = 1 ;
    else
    if (ts<ks && ks<sa && sa<sb && close<ks && adx_minus_1<convert_low && adx_minus_0>convert_high && cf > po )
     ytg_Signal = -1 ;
    
    return (ytg_Signal);
    
  }
/////////////////////////////////////////////////////////////////////////////////////////////  
//+----------------------------------------------------------------------------+
//|  �����    : ���� ������� ,  yuriytokman@gmail.com                          |
//+----------------------------------------------------------------------------+
//|  �������� : ������ ���� �� �������� ��������                               |
//+----------------------------------------------------------------------------+ 
 double GetLot()
  {
  
   double MinLot = MarketInfo(Symbol(),MODE_MINLOT);
   double MaxLot = MarketInfo(Symbol(),MODE_MAXLOT);
   double Prots = Risk/100;
   double Lotsi=MathFloor(AccountFreeMargin()*Prots/MarketInfo(Symbol(),MODE_MARGINREQUIRED)
               /MarketInfo(Symbol(),MODE_LOTSTEP))*MarketInfo(Symbol(),MODE_LOTSTEP);// ����
   if(Lotsi<MinLot)Lotsi=MinLot;
   if(Lotsi>MaxLot)Lotsi=MaxLot;               
   return(Lotsi);
  }
/////////////////////////////////////////////////////////////////////////////////////////////
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
    case OP_BUYLIMIT : return("BuyLimit");
    case OP_SELLLIMIT: return("SellLimit");
    case OP_BUYSTOP  : return("BuyStop");
    case OP_SELLSTOP : return("SellStop");
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
    if (!IsTesting() && IsStopped()) {
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


