
#property copyright "© 2008 BJF Trading Group"
#property link      "www.iticsoftware.com"

#define major   1
#define minor   1

extern string _tmp1_ = " --- Trade params ---";
extern string TradeTime = "3:00-21:20";
extern double Lots = 0.1;
extern int StopLoss = 0;
extern int TakeProfit = 0;
extern int Slippage = 3;
extern int Magic = 20080829;

extern string _tmp2_ = " --- i-Regr ---";
extern int Regr.degree = 3;
extern double Regr.kstd = 1.0;
extern int Regr.bars = 250;
extern int Regr.shift = 0;

extern string _tmp3_ = " --- Trailing ---";
extern bool TrailingOn = false;
extern int TrailingStart = 30;
extern int TrailingSize = 30;

extern string _tmp4_ = " --- Chart ---";
extern color clBuy = DodgerBlue;
extern color clSell = Red;
extern color clModify = Silver;
extern color clClose = Gold;

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

#include <stdlib.mqh>
#include <stderror.mqh>

int RepeatN = 3;
int BuyCnt, SellCnt;

void init() 
{
}

void deinit() 
{
}

void start() 
{
  //-----
  
  if (TrailingOn) TrailPositions();  
  
  //-----

  string ind_name = "i-Regr";
 
  double R.M0 = iCustom(NULL, 0, ind_name,
    Regr.degree, Regr.kstd, Regr.bars, Regr.shift,  
    0, 0);
  double R.M1 = iCustom(NULL, 0, ind_name,
    Regr.degree, Regr.kstd, Regr.bars, Regr.shift,  
    0, 1);
  
  double R.U0 = iCustom(NULL, 0, ind_name,
    Regr.degree, Regr.kstd, Regr.bars, Regr.shift,  
    1, 0);
  double R.U1 = iCustom(NULL, 0, ind_name,
    Regr.degree, Regr.kstd, Regr.bars, Regr.shift,  
    1, 1);

  double R.L0 = iCustom(NULL, 0, ind_name,
    Regr.degree, Regr.kstd, Regr.bars, Regr.shift,  
    2, 0);
  double R.L1 = iCustom(NULL, 0, ind_name,
    Regr.degree, Regr.kstd, Regr.bars, Regr.shift,  
    2, 1);
  
  //-----
  
  if (Bid >= R.M0)
  {
    if (CloseOrders(OP_BUY) > 0) return;
  }

  if (Bid <= R.M0)
  {
    if (CloseOrders(OP_SELL) > 0) return;
  }
  
  //-----
  
  if (!IsTradeTime()) return;

  
if (iHigh(NULL, PERIOD_D1, 1) - iLow(NULL, PERIOD_D1, 1) > 150*Point) 
{
CloseOrders(OP_BUY);
CloseOrders(OP_SELL);
  return;
}

  if (OrdersCountBar0(0) > 0) return;
  
  RecountOrders();
  
  //if (BuyCnt+SellCnt > 0) return;

  //-----
     
  double price, sl, tp;
  int ticket;

  //if (High[0] >= R.M0 && Open[0] < R.M0 && R.M0 > R.M1)
  
  if (Low[0] <= R.L0)
  {
    if (BuyCnt > 0) return;
    //if (CloseOrders(OP_SELL) > 0) return;
    
    //-----    
    
    for (int i=0; i<RepeatN; i++)
    {
      RefreshRates();
      price = Ask;
  
      sl = If(StopLoss > 0, price - StopLoss*Point, 0);
      tp = If(TakeProfit > 0, price + TakeProfit*Point, 0);

      ticket = Buy(Symbol(), GetLots(), price, sl, tp, Magic);
      if (ticket > 0) break;
    }

    return;
  }

  //if (Low[0] <= R.M0 && Open[0] > R.M0 && R.M0 < R.M1)
  if (High[0] >= R.U0)
  {
    if (SellCnt > 0) return;
    //if (CloseOrders(OP_BUY) > 0) return;
    
    //-----    
    
    for (i=0; i<RepeatN; i++)
    {  
      RefreshRates();
      price = Bid;
    
      sl = If(StopLoss > 0, price + StopLoss*Point, 0);
      tp = If(TakeProfit > 0, price - TakeProfit*Point, 0);

      ticket = Sell(Symbol(), GetLots(), price, sl, tp, Magic);
      if (ticket > 0) break;
    }

    return;
  } 
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

double If(bool cond, double if_true, double if_false)
{
  if (cond) return (if_true);
  return (if_false);
}

void split(string& arr[], string str, string sym) 
{
  ArrayResize(arr, 0);
  string item;
  int pos, size;
  
  int len = StringLen(str);
  for (int i=0; i < len;) 
  {
    pos = StringFind(str, sym, i);
    if (pos == -1) pos = len;
    
    item = StringSubstr(str, i, pos-i);
    item = StringTrimLeft(item);
    item = StringTrimRight(item);
    
    size = ArraySize(arr);
    ArrayResize(arr, size+1);
    arr[size] = item;
    
    i = pos+1;
  }
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

double GetLots() 
{
  return (Lots);
}

void RecountOrders()
{
  BuyCnt = 0;
  SellCnt = 0;

  int cnt = OrdersTotal();
  for (int i=0; i < cnt; i++) 
  {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;
    
    int type = OrderType();
    if (type == OP_BUY) BuyCnt++;
    if (type == OP_SELL) SellCnt++;
  }
}

bool IsTradeTime()
{
  if (TradeTime == "0:00-24:00") return (true);
  if (TradeTime == "00:00-24:00") return (true);

  datetime tm1, tm2;

  string TI[];
  split(TI, TradeTime, "-");
  if (ArraySize(TI) != 2) return (false);
    
  datetime tm0 = TimeCurrent();
  tm1 = StrToTime(TimeToStr(tm0, TIME_DATE) + " " + TI[0]);
  tm2 = StrToTime(TimeToStr(tm0, TIME_DATE) + " " + TI[1]);

  bool isTm = false; 
  if (tm1 <= tm2) 
    isTm = isTm || (tm1 <= tm0 && tm0 < tm2);
  else
    isTm = isTm || (tm1 <= tm0 || tm0 < tm2);
  
  return (isTm);
}

int OrdersCountBar0(int TF)
{
  int orders = 0;

  int cnt = OrdersTotal();
  for (int i=0; i<cnt; i++) 
  {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;

    if (OrderOpenTime() >= iTime(NULL, TF, 0)) orders++;
  }

  cnt = OrdersHistoryTotal();
  for (i=0; i<cnt; i++) 
  {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;

    if (OrderOpenTime() >= iTime(NULL, TF, 0)) orders++;
  }
 
  return (orders);
}

int CloseOrders(int type) 
{  
  int cnt = OrdersTotal();
  for (int i=cnt-1; i >= 0; i--) 
  {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;
    
    if (OrderType() != type) continue;
    
    if (type == OP_BUY) 
    {
      RefreshRates();
      CloseOrder(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID));
      continue;
    }
    
    if (type == OP_SELL) 
    {
      RefreshRates();
      CloseOrder(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK));
      continue;
    }    
  }
  
  int orders = 0;
  cnt = OrdersTotal();
  for (i = 0; i < cnt; i++) 
  {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;
    
    if (OrderType() == type) orders++;
  }
  
  return (orders); 
}

void TrailPositions()
{
  int cnt = OrdersTotal();
  for (int i=0; i<cnt; i++) 
  {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;

    int type = OrderType();
    if (type == OP_BUY) 
    {
      if (Bid-OrderOpenPrice() > TrailingStart*Point) 
      {
        if (OrderStopLoss() < Bid - (TrailingSize+1)*Point) 
        {
          OrderModify(OrderTicket(), OrderOpenPrice(), Bid-TrailingSize*Point, OrderTakeProfit(), 0, clModify);
        }
      }
    }

    if (type == OP_SELL)
    {
      if (OrderOpenPrice()-Ask > TrailingStart*Point) 
      {
        if (OrderStopLoss() > Ask + (TrailingSize+1)*Point || OrderStopLoss() == 0) 
        {
          OrderModify(OrderTicket(), OrderOpenPrice(), Ask+TrailingSize*Point, OrderTakeProfit(), 0, clModify);
        }
      }
    }
  }
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

int SleepOk = 2000;
int SleepErr = 6000;

int Buy(string symbol, double lot, double price, double sl, double tp, int magic, string comment="") 
{
  int dig = MarketInfo(symbol, MODE_DIGITS);

  price = NormalizeDouble(price, dig);
  sl = NormalizeDouble(sl, dig);
  tp = NormalizeDouble(tp, dig);
    
  string _lot = DoubleToStr(lot, 2);
  string _price = DoubleToStr(price, dig);
  string _sl = DoubleToStr(sl, dig);
  string _tp = DoubleToStr(tp, dig);

  Print("Buy \"", symbol, "\", ", _lot, ", ", _price, ", ", Slippage, ", ", _sl, ", ", _tp, ", ", magic, ", \"", comment, "\"");

  int res = OrderSend(symbol, OP_BUY, lot, price, Slippage, sl, tp, comment, magic, 0, clBuy);
  if (res >= 0) {
    Sleep(SleepOk);
    return (res);
  } 	
   	
  int code = GetLastError();
  Print("Error opening BUY order: ", ErrorDescription(code), " (", code, ")");
  Sleep(SleepErr);
	
  return (-1);
}

int Sell(string symbol, double lot, double price, double sl, double tp, int magic, string comment="") 
{
  int dig = MarketInfo(symbol, MODE_DIGITS);

  price = NormalizeDouble(price, dig);
  sl = NormalizeDouble(sl, dig);
  tp = NormalizeDouble(tp, dig);
  
  string _lot = DoubleToStr(lot, 2);
  string _price = DoubleToStr(price, dig);
  string _sl = DoubleToStr(sl, dig);
  string _tp = DoubleToStr(tp, dig);

  Print("Sell \"", symbol, "\", ", _lot, ", ", _price, ", ", Slippage, ", ", _sl, ", ", _tp, ", ", magic, ", \"", comment, "\"");
  
  int res = OrderSend(symbol, OP_SELL, lot, price, Slippage, sl, tp, comment, magic, 0, clSell);
  if (res >= 0) {
    Sleep(SleepOk);
    return (res);
  } 	
   	
  int code = GetLastError();
  Print("Error opening SELL order: ", ErrorDescription(code), " (", code, ")");
  Sleep(SleepErr);
	
  return (-1);
}

bool CloseOrder(int ticket, double lot, double price) 
{
  if (!OrderSelect(ticket, SELECT_BY_TICKET)) return(false);
  if (OrderCloseTime() > 0) return(false);
  
  int dig = MarketInfo(OrderSymbol(), MODE_DIGITS);
  string _lot = DoubleToStr(lot, 2);
  string _price = DoubleToStr(price, dig);

  Print("CloseOrder ", ticket, ", ", _lot, ", ", _price, ", ", Slippage);
  
  bool res = OrderClose(ticket, lot, price, Slippage, clClose);
  if (res) {
    Sleep(SleepOk);
    return (res);
  } 	
   	
  int code = GetLastError();
  Print("CloseOrder failed: ", ErrorDescription(code), " (", code, ")");
  Sleep(SleepErr);
	
  return (false);
}

