//+------------------------------------------------------------------+
//|                                                   MTrendLine.mq4 |
//|                      Copyright © 2006-2008, Stab-Invest[Dreamer] |
//|                                               www.stab-invest.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006-2008, Stab-Invest[Dreamer]"
#property link      "www.stab-invest.ru"

#define major   1
#define minor   0

extern string _tmp1_ = " --- Section 1 ---";
extern int Ticker1 = 0;
extern string TrendLine1 = "";
extern int Dist1 = 10;

extern string _tmp2_ = " --- Section 2 ---";
extern int Ticker2 = 0;
extern string TrendLine2 = "";
extern int Dist2 = 10;

extern string _tmp3_ = " --- Section 3 ---";
extern int Ticker3 = 0;
extern string TrendLine3 = "";
extern int Dist3 = 10;

extern string _tmp4_ = " --- Colors ---";
extern color clModify = Gold;

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

#include <stdlib.mqh>
#include <stderror.mqh>

void init () 
{
}

void deinit() 
{
}

void start()
{
  modify(Ticker1, TrendLine1, Dist1);
  modify(Ticker2, TrendLine2, Dist2);
  modify(Ticker3, TrendLine3, Dist3);
}

void modify(int Ticker, string TrendLine, int Dist) 
{
  if (StringLen(TrendLine) == 0) return;
  if (ObjectFind(TrendLine) == -1) return;
  if (ObjectType(TrendLine) != OBJ_TREND) return;

  if (!OrderSelect(Ticker, SELECT_BY_TICKET)) return;
  if (OrderCloseTime() > 0) return;

  //-----
  
  double TL0 = ObjectGetValueByShift(TrendLine, 0);
  TL0 = NormalizeDouble(TL0, Digits);
  //Comment(TL0);
  
  if (TL0 == 0) return;

  
  int type = OrderType();
  if (type == OP_BUYLIMIT || type == OP_SELLLIMIT || type == OP_BUYSTOP || type == OP_SELLSTOP)
  {
    double open_price = OrderOpenPrice();
    open_price = NormalizeDouble(open_price, Digits);

    if (open_price == TL0 + Dist*Point) return;
    
    //-----

    double price, sl, tp, sl_pt, tp_pt;
          
    price = TL0 + Dist*Point;
    int StopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) +1;

    if (type == OP_BUYLIMIT)
    {
      if (Ask-price < StopLevel*Point) return;  
    }
    
    if (type == OP_BUYSTOP)
    {
      if (price-Ask < StopLevel*Point) return;
    }
    
    if (type == OP_SELLLIMIT)
    {
      if (price-Bid < StopLevel*Point) return;
    }
    
    if (type == OP_SELLSTOP)
    {
      if (Bid-price < StopLevel*Point) return;
    }


    if (OrderStopLoss() == 0)
      sl_pt = 0;
    else
      sl_pt = MathAbs(OrderOpenPrice() - OrderStopLoss());

    if (OrderTakeProfit() == 0)
      tp_pt = 0;
    else
      tp_pt = MathAbs(OrderOpenPrice() - OrderTakeProfit());

    
    if (type == OP_BUYLIMIT || type == OP_BUYSTOP)
    {
      sl = If(sl_pt > 0, price - sl_pt, 0);
      tp = If(tp_pt > 0, price + tp_pt, 0);
    }

    if (type == OP_SELLLIMIT || type == OP_SELLSTOP)
    {
      sl = If(sl_pt > 0, price + sl_pt, 0);
      tp = If(tp_pt > 0, price - tp_pt, 0);
    }
    
    Print("OrderModify => price: ", DoubleToStr(price, Digits), 
      " sl: ", DoubleToStr(sl, Digits), 
      " tp: ", DoubleToStr(tp, Digits));
    
    bool ret = OrderModify(OrderTicket(), price, sl, tp, OrderExpiration(), clModify);
    
    if (!ret)
    {
      int code = GetLastError();
      if (code != ERR_NO_ERROR) Print("OrderModify failed => ", ErrorDescription(code));
    }
  }
}

double If(bool cond, double if_true, double if_false)
{
  if (cond) return (if_true);
  return (if_false);
}