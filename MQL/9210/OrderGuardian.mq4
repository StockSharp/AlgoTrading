//+------------------------------------------------------------------+
//|                                                OrderGuardian.mq4 |
//|                                                       Greatshore |
//|                                               greatshore@live.cn |
//+------------------------------------------------------------------+
#property copyright "Greatshore"
#property link      "greatshore@live.cn"

#define TP_PRICE_LINE "OG TP Price Line"
#define SL_PRICE_LINE "OG SL Price Line"

//---- input parameters
extern string  Orders          = "*";           // 管理哪些订单
extern int     TP_Method       = 2;             // 获利方式：1-Envelopes包络线和均线，2-趋势线
extern int     SL_Method       = 2;             // 获利方式：1-Envelopes包络线和均线，2-趋势线, 3-SAR
extern bool    ShowLines       = true;          // 是否显示获利止损线
extern string  SPLIT1          = "=== Parameters for TP ===";
extern color   TP_LineColor    = LimeGreen;     // 获利价格线颜色
extern int     TP_TimeFrame    = 0;             // 获利价计算的时间图周期
extern int     TP_MA_Period    = 31;            // 获利均线周期
extern int     TP_MA_Method    = MODE_EMA;      // 获利均线计算方法
extern int     TP_MA_Price     = PRICE_CLOSE;   // 获利均线计算价格
extern double  TP_Env_Dev      = 0.2;           // 获利Envelopes偏s移百分比
extern int     TP_Shift        = 0;             // 获利价计算的shift值
extern string  SPLIT2          = "=== Parameters of SL ===";
extern color   SL_LineColor    = Red;           // 止损价格线颜色
extern int     SL_TimeFrame    = 0;             // 止损价计算的时间图周期
extern int     SL_MA_Period    = 31;            // 止损均线计算方法
extern int     SL_MA_Method    = MODE_EMA;      // 止损均线计算方法
extern int     SL_MA_Price     = PRICE_CLOSE;   // 止损均线计算价格
extern double  SL_Env_Dev      = 0;             // 止损Envelopes偏移百分比
extern double  SL_SARStep      = 0.02;          // SAR止损的步长
extern double  SL_SARMax       = 0.5;           // SAR止损最大值
extern int     SL_Shift        = 0;             // 止损价计算的shift值

string TPObjName, SLObjName;
int    OrdersID[], OrdersCount, OpType;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
  if ((SL_Method == 3) && (SL_Shift < 1))
    SL_Shift = 1;

  return(0);
}

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
{
  ObjectDelete(TP_PRICE_LINE);
  ObjectDelete(SL_PRICE_LINE);
  Comment("");

  return(0);
}

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{
  int i;
  double TPPrice, SLPrice;
  bool   SetTPObj = false, SetSLObj = false;
  string MesgText;

  GetOrdersID();     // 获取需要管理的订单ID
  if (OpType >= 0)   // 方向一致
  {
    if (TP_Method == 2)
      SetTPObj = FindObject(TPObjName) < 0;
    if (SL_Method == 2)
      SetSLObj = FindObject(SLObjName) < 0;
    if (SetTPObj || SetSLObj)
      SearchObjName(OpType, SetTPObj, SetSLObj);         // 搜寻获利止损线的对象名

    CalcPrice(TPPrice, SLPrice);
    MesgText = "S/L @ ";
    if (SLPrice < 0)
      MesgText = MesgText + " __ ";
    else
      MesgText = MesgText + DoubleToStr(SLPrice, Digits);
    MesgText = MesgText + "   T/P @ ";
    if (TPPrice < 0)
      MesgText = MesgText + " __ ";
    else
      MesgText = MesgText + DoubleToStr(TPPrice, Digits);
    Comment(MesgText);

    if (ShowLines)
      ShowTPSLLines(TPPrice, SLPrice);
    for (i = 0; i < OrdersCount; i++)
    {
      if ((SLPrice > 0) &&
          (((OpType == OP_BUY)  && (Bid <= SLPrice)) ||
           ((OpType == OP_SELL) && (Bid >= SLPrice))))
        CloseOrder(OrdersID[i], 1);
      if ((TPPrice > 0) &&
          (((OpType == OP_BUY)  && (Bid >= TPPrice)) ||
           ((OpType == OP_SELL) && (Bid <= TPPrice))))
        CloseOrder(OrdersID[i], 2);
    }
  }

  return(0);
}
//+------------------------------------------------------------------+

// === 获取订单ID ===
void GetOrdersID()
{
  int i, n, t, o;
  bool all;
  
  n = OrdersTotal();
  ArrayResize(OrdersID, n);
  all = StringFind(Orders, "*") >= 0;
  OpType = -1;
  for (i = 0, OrdersCount = 0; i < n; i++)
  {
    OrderSelect(i, SELECT_BY_POS);
    if (Symbol() == OrderSymbol())
    {
      t = OrderTicket();
      if (all || (StringFind(Orders, DoubleToStr(t, 0)) >= 0))
      {
        o = OrderType();
        if (o < 2)
        {
          if ((OpType >= 0) && (o != OpType))
          {
            OpType = -1;
            break;
          }
          else
          {
            OpType = o;
            OrdersID[OrdersCount] = t;
            OrdersCount++;
          }
        }
      }
    }
  }
  if (OrdersCount == 0)
  {
    if (ObjectFind(TP_PRICE_LINE) >= 0)
      ObjectDelete(TP_PRICE_LINE);
    if (ObjectFind(SL_PRICE_LINE) >= 0)
      ObjectDelete(SL_PRICE_LINE);
  }
}

// === 寻找获利止损线 ===
void SearchObjName(int Type, bool GetTPObj = true, bool GetSLObj = true)
{
  int    i, ObjType, iAbove, iBelow, iTP, iSL;
  double MinAbove, MaxBelow, y1, y2;
  string ObjName;
  
  MinAbove = 999999;
  MaxBelow = 0;
  iAbove   = -1;
  iBelow   = -1;
  for (i = 0; i < ObjectsTotal(); i++)
  {
    ObjName = ObjectName(i);
    ObjType = ObjectType(ObjName);
    switch (ObjType)
    {
      case OBJ_TREND :
      case OBJ_TRENDBYANGLE :
        y1 = CalcLineValue(ObjName, 0, 1, ObjType);
        y2 = y1;
        break;
      case OBJ_CHANNEL :
        y1 = CalcLineValue(ObjName, 0, MODE_UPPER, ObjType);
        y2 = CalcLineValue(ObjName, 0, MODE_LOWER, ObjType);
        break;
      default :
        y1 = -1;
        y2 = -1;
    }
    if ((y1 > 0) && (y1 < Bid) && (y1 > MaxBelow))         // 两条线都在当前价下方
    {
      MaxBelow = y1;
      iBelow   = i;
    }
    else if ((y2 > Bid) && (y2 < MinAbove))    // 两条线都在当前价上方
    {
      MinAbove = y2;
      iAbove   = i;
    }
    else                // 两条线一上一下
    {
      if ((y1 > 0) && (y1 < MinAbove))
      {
        MinAbove = y1;
        iAbove   = i;
      }
      if (y2 > MaxBelow)
      {
        MaxBelow = y2;
        iBelow   = i;
      }
    }
  }

  switch (Type)
  {
    case OP_BUY :
      iTP = iAbove;
      iSL = iBelow;
      break;
    case OP_SELL :
      iTP = iBelow;
      iSL = iAbove;
      break;
    default :
      iTP = -1;
      iSL = -1;
  }
  if (GetTPObj)
  {
    if (iTP >= 0)
      TPObjName = ObjectName(iTP);
  }
  if (GetSLObj)
  {
    if (iSL >= 0)
      SLObjName = ObjectName(iSL);
  }
}

// === 计算获利价和止损价 ===
void CalcPrice(double &TPPrice, double &SLPrice)
{
  // 获利价
  switch (TP_Method)
  {
    case 1 :
      TPPrice = (1 + TP_Env_Dev * 0.01) * iMA(NULL, TP_TimeFrame, TP_MA_Period, 0, TP_MA_Method, TP_MA_Price, TP_Shift);
      break;
    case 2 :
      TPPrice = CalcLineValue(TPObjName, TP_Shift, 1 + OpType);
      break;
    default :
      TPPrice = -1;
  }
  // 止损价
  switch (SL_Method)
  {
    case 1 :
      SLPrice = (1 + SL_Env_Dev * 0.01) * iMA(NULL, SL_TimeFrame, SL_MA_Period, 0, SL_MA_Method, TP_MA_Price, SL_Shift);
      break;
    case 2 :
      SLPrice = CalcLineValue(SLObjName, SL_Shift, 2 - OpType);
      break;
    case 3 :
      SLPrice = iSAR(NULL, SL_TimeFrame, SL_SARStep, SL_SARMax, SL_Shift);
      break;
    default :
      SLPrice = -1;
  }
}

// === 计算直线在某个k线的值 ===
double CalcLineValue(string ObjName, int Shift, int ValueIndex = 1, int ObjType = -1)
{
  double y1, y2, delta, ret;
  int    i;
  
  if ((ObjType < 0) && (StringLen(ObjName) > 0))
    ObjType = ObjectType(ObjName);
  switch (ObjType)
  {
    case OBJ_TREND :
    case OBJ_TRENDBYANGLE :
      ret = LineGetValueByShift(ObjName, Shift);
      break;
    case OBJ_CHANNEL :
      i     = GetBarShift(Symbol(), 0, ObjectGet(ObjName, OBJPROP_TIME3));
      delta = ObjectGet(ObjName, OBJPROP_PRICE3) - LineGetValueByShift(ObjName, i);
      y1 = LineGetValueByShift(ObjName, Shift);
      y2 = y1 + delta;
      if (ValueIndex == MODE_UPPER)
        ret = MathMax(y1, y2);
      else if (ValueIndex == MODE_LOWER)
        ret = MathMin(y1, y2);
      else
        ret = -1;      
      break;
    default :
      ret = -1;
  }
  return(ret);
}

// === 显示获利止损价水平线 ===
void ShowTPSLLines(double TPPrice, double SLPrice)
{
  if (TPPrice < 0)
    ObjectDelete(TP_PRICE_LINE);
  else
  {
    if (FindObject(TP_PRICE_LINE) < 0)
    {
      ObjectCreate(TP_PRICE_LINE, OBJ_HLINE, 0, 0, 0);
      ObjectSet(TP_PRICE_LINE, OBJPROP_COLOR, TP_LineColor);
      ObjectSet(TP_PRICE_LINE, OBJPROP_STYLE, STYLE_DASHDOTDOT);
      ObjectSet(TP_PRICE_LINE, OBJPROP_WIDTH, 1);    
    }
    ObjectMove(TP_PRICE_LINE, 0, Time[0], TPPrice);
  }

  if (SLPrice < 0)
    ObjectDelete(SL_PRICE_LINE);
  else
  {
    if (FindObject(SL_PRICE_LINE) < 0)
    {
      ObjectCreate(SL_PRICE_LINE, OBJ_HLINE, 0, 0, 0);
      ObjectSet(SL_PRICE_LINE, OBJPROP_COLOR, SL_LineColor);
      ObjectSet(SL_PRICE_LINE, OBJPROP_STYLE, STYLE_DASHDOTDOT);
      ObjectSet(SL_PRICE_LINE, OBJPROP_WIDTH, 1);    
    }
    ObjectMove(SL_PRICE_LINE, 0, Time[0], SLPrice);
  }
}

// === 查找对象 ===
int FindObject(string Name)
{
  if (StringLen(Name) <= 0)
    return(-1);
  else
    return(ObjectFind(Name));
}

// === 平仓 ===
void CloseOrder(int Ticket, int type)
{
  double ClosePrice;
  string str[2] = {"TP", "SL"};
  
  if (OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
  {
    if (OrderType() == OP_BUY)
      ClosePrice = MarketInfo(Symbol(), MODE_BID);
    else
      ClosePrice = MarketInfo(Symbol(), MODE_ASK);
    if (OrderClose(Ticket, OrderLots(), ClosePrice, 0))
      Print("Order #", Ticket, " was closed successfully at ", str[type], " ", ClosePrice);
    else
      Print("Order #", Ticket, " reached ", str[type], " ", ClosePrice, ", but failed to close for error ", GetLastError());
  }
}

// === 计算直线上的值 ===
double LineGetValueByShift(string ObjName, int Shift)
{
  double i1, i2, i, y1, y2, y;
  
  i1 = GetBarShift(Symbol(), 0, ObjectGet(ObjName, OBJPROP_TIME1));
  i2 = GetBarShift(Symbol(), 0, ObjectGet(ObjName, OBJPROP_TIME2));
  y1 = ObjectGet(ObjName, OBJPROP_PRICE1);
  y2 = ObjectGet(ObjName, OBJPROP_PRICE2);
  if (i1 < i2)
  {
    i  = i1;
    i1 = i2;
    i2 = i;
    y  = y1;
    y1 = y2;
    y2 = y;
  }
  if (Shift > i1)
    y = (y2 - y1) / (i2 - i1) * (Shift - i1) + y1;
  else
    y = ObjectGetValueByShift(ObjName, Shift);
    
  return(y);
}

// === 取时间值的shift数 ===
int GetBarShift(string symbol, int timeframe, datetime time)
{
  int now;
  
  now = iTime(symbol, timeframe, 0);
  if (time < now + timeframe * 60)
    return(iBarShift(symbol, timeframe, time));
  else
  {
    if (timeframe == 0)
      timeframe = Period();
    return((now - time) / timeframe / 60);
  }
}