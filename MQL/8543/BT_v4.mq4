
#property copyright "© skymaster"

#include <stdlib.mqh>
#include <stderror.mqh>

#define major   4
#define minor   0

extern string _tmp1_ = " --- Trade parameters ---";
extern double AppPrice = 0.0;
extern bool OBuy = true;
extern double Lots = 0.1;
extern int StopLoss = 140;
extern int TakeProfit = 180;
extern int Slippage = 3;
extern int Magic = 20050107;

extern string _tmp2_ = " --- MM section ---";
extern bool EnableMM = false;
extern double LotBalancePcnt = 10;
extern double MinLot = 0.1;
extern double MaxLot = 5;
extern int LotPrec = 1;


extern string _tmp4_ = " --- Orders color ---";
extern color clBuy = DodgerBlue;
extern color clSell = Crimson;

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

void init () {
  if (!IsTesting()) Comment("");
}

void deinit() {
  if (!IsTesting()) Comment("");
}

void start() {

  //-----
  
  if (false)
    if (OrdersCount0() > 0) return;

  //-----

  int BuyCnt = 0;
  int SellCnt = 0;
  
  int cnt = OrdersTotal();
  for (int i=0; i < cnt; i++) {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;
    
    int type = OrderType();
    if (type == OP_BUY) BuyCnt++;
    if (type == OP_SELL) SellCnt++;
  }  

  //-----

    
  double price, sl, tp;

  if (Close[1]>AppPrice && Close[2]<=AppPrice)
  {  
    if (BuyCnt > 0) return;
    if (OBuy == false) return;
    
    if (SellCnt > 0) {
      SellCnt = CloseOrders(OP_SELL);
      if (SellCnt > 0) return;
    }
      
    {
      price = Ask;
  
      sl = 0;
      tp = 0;
      if (StopLoss > 0) sl = price - StopLoss*Point;
      if (TakeProfit > 0) tp = price + TakeProfit*Point;

      Buy(Symbol(), GetLots(), price, sl, tp, Magic);
    }
    
    return;
  }

  if (Close[1]<AppPrice && Close[2]>=AppPrice)
  {
    if (SellCnt > 0) return;
    if (OBuy == true) return;
    
    if (BuyCnt > 0) {
      BuyCnt = CloseOrders(OP_BUY);
      if (BuyCnt > 0) return;
    }

    {      
      price = Bid;
    
      sl = 0;
      tp = 0;
      if (StopLoss > 0) sl = price + StopLoss*Point;
      if (TakeProfit > 0) tp = price - TakeProfit*Point;

      Sell(Symbol(), GetLots(), price, sl, tp, Magic);
    }
    
    return;
  } 
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

double GetLots() 
{
  if (!EnableMM) return (Lots);
  
  double Bal0 = 1000;
  if (LotPrec == 2) Bal0 = 100;
  
  double lots = NormalizeDouble(LotBalancePcnt/100*AccountBalance()/Bal0, LotPrec);
  lots = MathMax(lots, MinLot);
  lots = MathMin(lots, MaxLot);
  
  return (lots);
}

int CloseOrders(int type) {
  
  int cnt = OrdersTotal();
  for (int i=cnt-1; i >= 0; i--) {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;
    
    if (OrderType() != type) continue;
    
    if (OrderType() == OP_BUY) {
      CloseOrder(OrderTicket(), OrderLots(), Bid);
    }
    else if (OrderType() == OP_SELL) {
      CloseOrder(OrderTicket(), OrderLots(), Ask);
    }    
  }
  
  int orders = 0;
  cnt = OrdersTotal();
  for (i = 0; i < cnt; i++) {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;
    
    if (OrderType() == type) orders++;
  }
  
  return (orders); 
}

int OrdersCount0()
{
  int orders = 0;

  int cnt = OrdersTotal();
  for (int i=0; i<cnt; i++) {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
 
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;

    if (OrderOpenTime() >= Time[0]) orders++;
  }

  cnt = OrdersHistoryTotal();
  for (i=0; i<cnt; i++) {
    if (!OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) continue;
 
    if (OrderSymbol() != Symbol()) continue;
    if (OrderMagicNumber() != Magic) continue;

    if (OrderOpenTime() >= Time[0]) orders++;
  }
 
  return (orders);
}

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

int SleepOk = 2000;
int SleepErr = 6000;

int Buy(string symbol, double lot, double price, double sl, double tp, int magic, string comment="") {
  RefreshRates();  
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

int Sell(string symbol, double lot, double price, double sl, double tp, int magic, string comment="") {
  RefreshRates();  
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

bool CloseOrder(int ticket, double lot, double price) {

  RefreshRates();
  if (!OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) return(false);
  int dig = MarketInfo(OrderSymbol(), MODE_DIGITS);

  string _lot = DoubleToStr(lot, 2);
  string _price = DoubleToStr(price, dig);

  Print("CloseOrder ", ticket, ", ", _lot, ", ", _price, ", ", Slippage);
  
  bool res = OrderClose(ticket, lot, price, Slippage);
  if (res) {
    Sleep(SleepOk);
    return (res);
  } 	
   	
  int code = GetLastError();
  Print("CloseOrder failed: ", ErrorDescription(code), " (", code, ")");
  Sleep(SleepErr);
	
  return (false);
}


