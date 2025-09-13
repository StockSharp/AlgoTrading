#property link      "waddahattar@hotmail.com"
 
extern int     Step=120;
extern double  FirstLot=0.1;
extern double  IncLot=0;
extern double  MinProfit=450;
extern int     Magic = 2008;

double gLotSell=0;
double gLotBuy=0;
double LSP,LBP;

int init()
{
  Comment("Waddah Attar Win");
  GlobalVariableSet("OldBalance",AccountBalance());
  return(0);
}
int deinit()
{
  Comment("");
  return(0);
}
int start()
{
  double i;
  double sl,p;

  if (AccountEquity()>=GlobalVariableGet("OldBalance")+MinProfit)
  {
    DeletePendingOrders(Magic);
    CloseOrders(Magic);
    GlobalVariableSet("OldBalance",0);
  }

  GlobalVariableSet("OldBalance",AccountBalance());

  if (MyOrdersTotal(Magic)==0)
  {
    OrderSend(Symbol(),OP_BUYLIMIT,FirstLot,Ask-Step*Point,3,0,0,"",Magic,0,Green);
    OrderSend(Symbol(),OP_SELLLIMIT,FirstLot,Bid+Step*Point,3,0,0,"",Magic,0,Red);
  }

  LSP=GetLastSellPrice(Magic);
  LBP=GetLastBuyPrice(Magic);
  
  if((LSP-Bid)<=5*Point)
  {
    OrderSend(Symbol(),OP_SELLLIMIT,gLotSell+IncLot,LSP+Step*Point,3,0,0,"",Magic,0,Red);
  }

  if((Ask-LBP)<=5*Point)
  {
    OrderSend(Symbol(),OP_BUYLIMIT,gLotBuy+IncLot,LBP-Step*Point,3,0,0,"",Magic,0,Red);
  }

  return(0);
}

int DeletePendingOrders(int Magic)
{
  int total  = OrdersTotal();
  
  for (int cnt = total-1 ; cnt >= 0 ; cnt--)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol() && (OrderType()!=OP_BUY || OrderType()!=OP_SELL))
    {
      OrderDelete(OrderTicket());
    }
  }
  return(0);
}

int CloseOrders(int Magic)
{
  int total  = OrdersTotal();
  
  for (int cnt = total-1 ; cnt >= 0 ; cnt--)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol())
    {
      if (OrderType()==OP_BUY)
      {
        OrderClose(OrderTicket(),OrderLots(),Bid,3);
      }
      
      if (OrderType()==OP_SELL)
      {
        OrderClose(OrderTicket(),OrderLots(),Ask,3);
      }
    }
  }
  return(0);
}

int MyOrdersTotal(int Magic)
{
  int c=0;
  int total  = OrdersTotal();

  for (int cnt = 0 ; cnt < total ; cnt++)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol())
    {
      c++;
    }
  }
  return(c);
}

double GetLastBuyPrice(int Magic)
{
  int total=OrdersTotal()-1;

  for (int cnt = total ; cnt >=0 ; cnt--)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol() && (OrderType()==OP_BUYLIMIT || OrderType()==OP_BUY))
    {
      gLotBuy=OrderLots();
      return(OrderOpenPrice());
      break;
    }
  }
  return(0);
}

double GetLastSellPrice(int Magic)
{
  int total=OrdersTotal()-1;

  for (int cnt = total ; cnt >=0 ; cnt--)
  {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if (OrderMagicNumber() == Magic && OrderSymbol()==Symbol() && (OrderType()==OP_SELLLIMIT ||OrderType()==OP_SELL))
    {
      gLotSell=OrderLots();
      return(OrderOpenPrice());
      break;
    }
  }
  return(100000);
}

