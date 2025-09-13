//+------------------------------------------------------------------+
//|                                                TrueSort_1001.mq4 |
//|                                                TrueSort(v 1.0.01)|
//|                                                           MaxBau |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MaxBau"
#property link      ""

extern double lots = 0.1;
extern int stloss = 100;
extern int magicnum = 5000;

double level_10, level_10_old;
double ma_10[4], ma_20[4], ma_50[4], ma_100[4], ma_200[4];
double ma_10c, ma_20c, ma_50c, ma_100c, ma_200c;

int my_order;
int w_m = 0;

int init()
{
  return(0);
}

int deinit()
{
  return(0);
}

void setstopbuy()
{
  OrderSelect (my_order, SELECT_BY_TICKET);
  if (OrderStopLoss() < OrderOpenPrice())
  {
    if (NormalizeDouble((Bid-OrderOpenPrice())/Point,Digits) > stloss)
      OrderModify (my_order, OrderOpenPrice(), Bid-stloss*Point, 0, 0, Red);
  }
  else
    if (OrderStopLoss() > OrderOpenPrice())
    {
      if (NormalizeDouble((Bid-OrderStopLoss())/Point,Digits) > stloss)
        OrderModify (my_order, OrderOpenPrice(), Bid-stloss*Point, 0, 0, Red);
    } 
}

void setstopsell()
{
  OrderSelect (my_order, SELECT_BY_TICKET);
  if (OrderStopLoss() > OrderOpenPrice())
  {
    if (NormalizeDouble((OrderOpenPrice()-Ask)/Point,Digits) > stloss)
      OrderModify (my_order, OrderOpenPrice(), Ask+stloss*Point, 0, 0, Red);
  }
  else
    if (OrderStopLoss() < OrderOpenPrice())
    {
      if (NormalizeDouble((OrderStopLoss()-Ask)/Point,Digits) > stloss)
        OrderModify (my_order, OrderOpenPrice(), Ask+stloss*Point, 0, 0, Red);
    } 
}

void checkopenedorders ()
{
  my_order = 0;
  w_m = 0;
  int ordstotal = OrdersTotal();
  for (int i = 0; i < ordstotal; i++)
  {
    OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
    if(OrderMagicNumber() == magicnum)
    {
      my_order = OrderTicket();
      if (OrderType() == OP_BUY)
      {
        w_m = 1;
        setstopbuy();
      }
      if (OrderType() == OP_SELL)
      {
        w_m = 2;
        setstopsell();
      }
    }
  }
}

void eq_level()
{
  level_10 = iMA (NULL, 0, 10, 0, MODE_SMA, PRICE_CLOSE, 1);
  return;
}

bool isChanges ()
{
  double res = 0;
  res = NormalizeDouble (level_10,Digits) - NormalizeDouble (level_10_old,Digits) ;
  if (res != 0)
  {
    return (true);
  }
  else return (false);
}

void saveparam ()
{
  level_10_old  = level_10;
  return;
}

/*
void eq_mas_5()
{
  ma_10 = iMA (NULL, 0, 10, 0, MODE_SMA, PRICE_CLOSE, 3);
  ma_20 = iMA (NULL, 0, 20, 0, MODE_SMA, PRICE_CLOSE, 3);
  ma_50 = iMA (NULL, 0, 50, 0, MODE_SMA, PRICE_CLOSE, 3);
  ma_100 = iMA (NULL, 0, 100, 0, MODE_SMA, PRICE_CLOSE, 3);
  ma_200 = iMA (NULL, 0, 200, 0, MODE_SMA, PRICE_CLOSE, 3);
  return;
}
*/
void eq_mas()
{
  for (int i = 0; i < 3; i++)
  {
    ma_10[i] = iMA (NULL, 0, 10, 0, MODE_SMA, PRICE_CLOSE, i+1);
    ma_20[i] = iMA (NULL, 0, 20, 0, MODE_SMA, PRICE_CLOSE, i+1);
    ma_50[i] = iMA (NULL, 0, 50, 0, MODE_SMA, PRICE_CLOSE, i+1);
    ma_100[i] = iMA (NULL, 0, 100, 0, MODE_SMA, PRICE_CLOSE, i+1);
    ma_200[i] = iMA (NULL, 0, 200, 0, MODE_SMA, PRICE_CLOSE, i+1);
  }
  return;
}

void eq_mas_0()
{
  ma_10c = iMA (NULL, 0, 10, 0, MODE_SMA, PRICE_CLOSE, 0);
  ma_20c = iMA (NULL, 0, 20, 0, MODE_SMA, PRICE_CLOSE, 0);
  ma_50c = iMA (NULL, 0, 50, 0, MODE_SMA, PRICE_CLOSE, 0);
  ma_100c = iMA (NULL, 0, 100, 0, MODE_SMA, PRICE_CLOSE, 0);
  ma_200c = iMA (NULL, 0, 200, 0, MODE_SMA, PRICE_CLOSE, 0);
  return;
}

bool check_buy ()
{
  //eq_mas_0 ();
  
  //if (ma_10 > ma_20 && ma_20 > ma_50 && ma_50 > ma_100 && ma_100 > ma_200)
  //  if (ma_10c > ma_20c && ma_20c > ma_50c && ma_50c > ma_100c && ma_100c > ma_200c)
  if (ma_10[0] > ma_20[0] && ma_20[0] > ma_50[0] && ma_50[0] > ma_100[0] && ma_100[0] > ma_200[0])
    if (ma_10[1] > ma_20[1] && ma_20[1] > ma_50[1] && ma_50[1] > ma_100[1] && ma_100[1] > ma_200[1])
      if (ma_10[2] > ma_20[2] && ma_20[2] > ma_50[2] && ma_50[2] > ma_100[2] && ma_100[2] > ma_200[2])
        if (iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,0) > 25 && iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,0) > iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,1))
      return (true);
  return (false);
}

bool check_sell ()
{
  //eq_mas_0 ();
  //if (ma_10 < ma_20 && ma_20 < ma_50 && ma_50 < ma_100 && ma_100 < ma_200)
  //  if (ma_10c < ma_20c && ma_20c < ma_50c && ma_50c < ma_100c && ma_100c < ma_200c)
  if (ma_10[0] < ma_20[0] && ma_20[0] < ma_50[0] && ma_50[0] < ma_100[0] && ma_100[0] < ma_200[0])
    if (ma_10[1] < ma_20[1] && ma_20[1] < ma_50[1] && ma_50[1] < ma_100[1] && ma_100[1] < ma_200[1])
      if (ma_10[2] < ma_20[2] && ma_20[2] < ma_50[2] && ma_50[2] < ma_100[2] && ma_100[2] < ma_200[2])
        if (iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,0) > 25 && iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,0) > iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,1))
      return (true);
  return (false);
}

void do_it (int workmode)
{
  switch (workmode)
  {
    case 0:
            my_order = OrderSend (Symbol(), OP_BUY, lots, Ask, 3, ma_100c, 0, "My order", magicnum, 0, Red);     
            break;
    case 1:
            my_order = OrderSend (Symbol(), OP_SELL, lots, Bid, 3, ma_100c, 0, "My order", magicnum, 0, Red);           
            break;
  }
  return;
}

void checkforclose ()
{
  eq_mas_0();
  switch (w_m)
  {
    case 1: 
            if (ma_10c <= ma_20c || ma_20c <= ma_50c || ma_50c <= ma_100c || ma_100c <= ma_200c)
              OrderClose (my_order, lots, Bid, 3, Blue);
            break;
    case 2: 
            if (ma_10c >= ma_20c || ma_20c >= ma_50c || ma_50c >= ma_100c || ma_100c >= ma_200c)
              OrderClose (my_order, lots, Ask, 3, Blue);
            break;
  
  }
  return;
}

int start()
{
  checkopenedorders ();
  
  eq_level();
  if (isChanges() == false) return(0);
  saveparam();
  
  eq_mas();
  if (w_m == 0)
  {
    if (check_buy() == true) do_it (0);
    if (check_sell() == true) do_it (1);
  }
  else checkforclose ();
  return(0);
}

