//+------------------------------------------------------------------+
//|                                                     DragSLTP.mq4 |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, TheXpert"
#property link      "mqlite@gmail.com"

// maximum acceptable slippage for the price
extern int        Slippage          =  3;

// maximum loop count for orders opening if failed due to requoting
extern int        TimesToRepeat     =  3;

extern color      SL_Color = Red;
extern int        SL_Style = 3;            // StopLoss line
extern color      TP_Color = Green;
extern int        TP_Style = 3;            // TakeProfit line

extern bool       ConfirmActions = false;

extern bool       AutoSetSL = true;
extern double     SLPoints = 300;

extern bool       AutoSetTP = false;
extern double     TPPoints = 30;


string Symbol_;

///=========================================================================
/// GUI
///=========================================================================

void SetText(string name, datetime time, double price, string text, int size = 10, color clr = White)
{
   ObjectCreate(name, OBJ_TEXT, 0, time, price);
   
   ObjectSet(name, OBJPROP_PRICE1, price);
   ObjectSet(name, OBJPROP_TIME1, time);

   ObjectSetText(name, text, size, "", clr);
}

void SetHLine(string name, double price, color clr = Green, int style = 2, int width = 0)
{
   ObjectCreate(name, OBJ_HLINE, 0, 0, price); 
   
   ObjectSet(name, OBJPROP_PRICE1, price);
   ObjectSet(name, OBJPROP_STYLE, style);
   ObjectSet(name, OBJPROP_COLOR, clr);
   ObjectSet(name, OBJPROP_WIDTH, width);
}

///=========================================================================
/// Trading
///=========================================================================

void WaitForContext()
{
   while (IsTradeContextBusy() && !IsStopped())
   {
      Sleep(20);
   }
}

bool NeedToRetry(int errorCode)
{
   switch (errorCode)
   {
      case 4   : return (true);
      case 129 : return (true);
      case 130 : return (true);
      case 135 : return (true);
      case 138 : return (true);
      case 146 : return (true);
      
      default: return (false);
   }
}

#define IDOK 1
#define IDCANCEL 2

bool Confirmed(string action)
{
   if (!ConfirmActions) return (true);
   
   int result = MessageBox("Do you really want to\n" + action + "?", "Please confirm", IDOK);
   return (result == IDOK);
}

void CheckForSLTP()
{
   if (!AutoSetSL && !AutoSetTP) return;
   
   double slevel = MarketInfo(Symbol_, MODE_STOPLEVEL);
   double sl = MathMax(SLPoints, slevel);
   double tp = MathMax(TPPoints, slevel);
   
   for(int i = OrdersTotal() - 1; i >= 0; i--)
   {
      // already closed
      if(!OrderSelect(i, SELECT_BY_POS)) continue;
      // not current symbol
      if(OrderSymbol() != Symbol_) continue;
      
      double newSL = 0;
      double newTP = 0;

      RefreshRates();

      switch(OrderType())
      {
         case OP_BUY:
            newSL = NormalizeDouble(Bid - sl*Point, Digits);
            newTP = NormalizeDouble(Ask + tp*Point, Digits);
            break;
            
         case OP_SELL:
            newSL = NormalizeDouble(Ask + sl*Point, Digits);
            newTP = NormalizeDouble(Bid - tp*Point, Digits);
            break;
      }
      
      bool needModify = false;
      
      if (!AutoSetSL || OrderStopLoss() != 0)
      {
         newSL = OrderStopLoss();
      }
      else
      {
         needModify = true;
      }
      
      if (!AutoSetTP || OrderTakeProfit() != 0)
      {
         newTP = OrderTakeProfit();
      }
      else
      {
         needModify = true;
      }
      
      if (needModify)
      {
         OrderModify(OrderTicket(), OrderOpenPrice(), newSL, newTP, OrderExpiration());
      }
   }
}

///=========================================================================
/// Misc
///=========================================================================

string ID()
{
   static string result;
   if (StringLen(result) == 0) result = "DSLTP " + " ";
   
   return (result);
}

int IDLen()
{
   static int size = -1;
   if (size == -1) size = StringLen(ID());
   
   return (size);
}

datetime GetTime(int i)
{
   if (i >= 0) return (Time[i]);
   return (Time[0] - i*60*Period());
}

///=========================================================================
/// DND Implementation
///=========================================================================

bool CheckDrag(string name)
{
   double x = ObjectGet(name, OBJPROP_PRICE1);
   if (GetLastError() != 0) return (false);
   
   if (ObjectSet(name, OBJPROP_PRICE1, x*1.1))
   {
      ObjectSet(name, OBJPROP_PRICE1, x);
      return (false);
   }
   
   return (true);
}

bool CheckDrop(string name)
{
   double x = ObjectGet(name, OBJPROP_PRICE1);
   if (GetLastError() != 0) return (true);
   
   if (ObjectSet(name, OBJPROP_PRICE1, x*1.1))
   {
      ObjectSet(name, OBJPROP_PRICE1, x);
      return (true);
   }
   
   return (false);
}


bool IsDragging = false;
string DragName = "";

double DropPrice;

void OnDropped(string name)
{
   IsDragging = false;

   string s = ObjectDescription(ID() + " HelpText");
   if (s == "") return;
   
   ObjectDelete(ID() + " HelpText");
   
   int t = GetTicketFromName(name);
   if (!OrderSelect(t, SELECT_BY_TICKET)) return;

   int stopLevel = MarketInfo(Symbol_, MODE_STOPLEVEL);
   
   string type = StringSubstr(name, 0, 2);
   
   DropPrice = NormalizeDouble(DropPrice, Digits);
   
   if (!Confirmed(s)) return;
   
   RefreshRates();
   if (type == "SL")
   {
      switch(OrderType())
      {
         case OP_BUY:
            if (DropPrice > Ask)
            {
               int err = 0;
               for (int i = 0; i < TimesToRepeat; i++)
               {
                  WaitForContext();
                  RefreshRates();
                  if (OrderClose(t, OrderLots(), Bid, Slippage))
                  {
                     Print("Order #", t, " closed successfully");
                     err = 0;
                     break;
                  }
                  else
                  {
                     err = GetLastError();
                     if (!NeedToRetry(err)) break;
                     Sleep(100);
                  }
               }
               
               if (err > 0)
               {
                  Print("Close order #", t, " failed, error #", err);
               }
            }
            else
            {
               WaitForContext();
               RefreshRates();
               double sl = NormalizeDouble(MathMin(DropPrice, Bid - stopLevel*Point), Digits);
               if (OrderModify(t, OrderOpenPrice(), sl, OrderTakeProfit(), OrderExpiration()))
               {
                  Print("SL for #", t, " modified successfully");
               }
               else
               {
                  Print("SL modification for #", t, " failed, Error #", GetLastError());
               }
            }
            break;
            
         case OP_SELL:
            if (DropPrice < Bid)
            {
               err = 0;
               for (i = 0; i < TimesToRepeat; i++)
               {
                  WaitForContext();
                  RefreshRates();
                  if (OrderClose(t, OrderLots(), Ask, Slippage))
                  {
                     Print("Order #", t, " closed successfully");
                     err = 0;
                     break;
                  }
                  else
                  {
                     err = GetLastError();
                     if (!NeedToRetry(err)) break;
                     Sleep(100);
                  }
               }
               
               if (err > 0)
               {
                  Print("Close order #", t, " failed, error #", err);
               }
            }
            else
            {
               WaitForContext();
               RefreshRates();
               sl = NormalizeDouble(MathMax(DropPrice, Ask + stopLevel*Point), Digits);
               if (OrderModify(t, OrderOpenPrice(), sl, OrderTakeProfit(), OrderExpiration()))
               {
                  Print("SL for #", t, " modified successfully");
               }
               else
               {
                  Print("SL modification for #", t, " failed, Error #", GetLastError());
               }
            }
            break;
      }
   }
   else if (type == "TP")
   {
      switch(OrderType())
      {
         case OP_BUY:
            if (DropPrice < Bid)
            {
               err = 0;
               for (i = 0; i < TimesToRepeat; i++)
               {
                  WaitForContext();
                  RefreshRates();
                  if (OrderClose(t, OrderLots(), Bid, Slippage))
                  {
                     Print("Order #", t, " closed successfully");
                     err = 0;
                     break;
                  }
                  else
                  {
                     err = GetLastError();
                     if (!NeedToRetry(err)) break;
                     Sleep(100);
                  }
               }
               
               if (err > 0)
               {
                  Print("Close order #", t, " failed, error #", err);
               }
            }
            else
            {
               WaitForContext();
               RefreshRates();
               double tp = NormalizeDouble(MathMax(DropPrice, Ask + stopLevel*Point), Digits);
               if (OrderModify(t, OrderOpenPrice(), OrderStopLoss(), tp, OrderExpiration()))
               {
                  Print("TP for #", t, " modified successfully");
               }
               else
               {
                  Print("TP modification for #", t, " failed, Error #", GetLastError());
               }
            }
            break;
            
         case OP_SELL:
            if (DropPrice > Ask)
            {
               err = 0;
               for (i = 0; i < TimesToRepeat; i++)
               {
                  WaitForContext();
                  RefreshRates();
                  if (OrderClose(t, OrderLots(), Ask, Slippage))
                  {
                     Print("Order #", t, " closed successfully");
                     err = 0;
                     break;
                  }
                  else
                  {
                     err = GetLastError();
                     if (!NeedToRetry(err)) break;
                     Sleep(100);
                  }
               }
               
               if (err > 0)
               {
                  Print("Close order #", t, " failed, error #", err);
               }
            }
            else
            {
               WaitForContext();
               RefreshRates();
               tp = NormalizeDouble(MathMin(DropPrice, Bid - stopLevel*Point), Digits);
               if (OrderModify(t, OrderOpenPrice(), OrderStopLoss(), tp, OrderExpiration()))
               {
                  Print("TP for #", t, " modified successfully");
               }
               else
               {
                  Print("TP modification for #", t, " failed, Error #", GetLastError());
               }
            }
            break;
      }
   }
}

void WhileDragging(string name)
{
   double ask = MarketInfo(Symbol_, MODE_ASK);
   double bid = MarketInfo(Symbol_, MODE_BID);
   
   int t = GetTicketFromName(name);
   int stopLevel = MarketInfo(Symbol_, MODE_STOPLEVEL);
   
   string text = "";
   
   string type = StringSubstr(name, 0, 2);
   double nowPrice = NormalizeDouble(ObjectGet(name, OBJPROP_PRICE1), Digits);
   DropPrice = nowPrice;
   
   if (!OrderSelect(t, SELECT_BY_TICKET))
   {
      type = "";
      text = "Invalid order (#" + t + ")";
   }
   
   int orderType = OrderType();
   
   
   if (type == "SL")
   {
      text =      "set SL for #" + t + " (" + 
                  DoubleToStr(OrderLots(), 2) + " " +
                  Type(OrderType()) + " at " + 
                  DoubleToStr(OrderOpenPrice(), Digits) + ") to ";
                  
      switch(OrderType())
      {
         case OP_BUY:
            if (nowPrice > ask)
            {
               text =   "Close #" + t + " (" + 
                        DoubleToStr(OrderLots(), 2) + " " +
                        Type(OrderType()) + " at " + 
                        DoubleToStr(OrderOpenPrice(), Digits) + ")";
            }
            else
            {
               text = text + DoubleToStr(MathMin(nowPrice, bid - stopLevel*Point), Digits);
            }
            break;
         case OP_SELL:
            if (nowPrice < bid)
            {
               text =   "Close #" + t + " (" + 
                        DoubleToStr(OrderLots(), 2) + " " +
                        Type(OrderType()) + " at " + 
                        DoubleToStr(OrderOpenPrice(), Digits) + ")";
            }
            else
            {
               text = text + DoubleToStr(MathMax(nowPrice, ask + stopLevel*Point), Digits);
            }
            break;
      }
   }
   else if (type == "TP")
   {
      text =      "set TP for #" + t + " (" + 
                  DoubleToStr(OrderLots(), 2) + " " +
                  Type(OrderType()) + " at " + 
                  DoubleToStr(OrderOpenPrice(), Digits) + ") to ";
                  
      switch(OrderType())
      {
         case OP_BUY:
            if (nowPrice < bid)
            {
               text =   "Close #" + t + " (" + 
                        DoubleToStr(OrderLots(), 2) + " " +
                        Type(OrderType()) + " at " + 
                        DoubleToStr(OrderOpenPrice(), Digits) + ")";
            }
            else
            {
               text = text + DoubleToStr(MathMax(nowPrice, bid + stopLevel*Point), Digits);
            }
            break;
         case OP_SELL:
            if (nowPrice > ask)
            {
               text =   "Close #" + t + " (" + 
                        DoubleToStr(OrderLots(), 2) + " " +
                        Type(OrderType()) + " at " + 
                        DoubleToStr(OrderOpenPrice(), Digits) + ")";
            }
            else
            {
               text = text + DoubleToStr(MathMin(nowPrice, ask - stopLevel*Point), Digits);
            }
            break;
      }                  
   }
   
   datetime time = GetTime(WindowFirstVisibleBar() - WindowBarsPerChart()/2);
   SetText(ID() + " HelpText", time, nowPrice + Point, text);
}

///=========================================================================
/// Implementation
///=========================================================================

int GetTicketFromName(string name)
{
   string ticket = StringSubstr(name, IDLen() + 3);
   
   int space = StringFind(ticket, " ");
   if (space == -1) return (-1);
   
   return (StrToInteger(StringSubstr(ticket, 0, space)));
}

int init()
{
   IsDragging = false;
   DragName = "";
   
   Symbol_ = Symbol();
}

int start()
{
   if (IsTesting()) return (0);
   
   while (!IsStopped())
   {
      if (IsDragging)
      {
         if (CheckDrop(DragName))
         {
            OnDropped(DragName);
         }
         else
         {
            WhileDragging(DragName);
         }
      }
      else
      {
         CheckForSLTP();
         CheckLines();
      }
   
      WindowRedraw();
   
      Sleep(50);
   }
   
   return(0);
}

void deinit()
{
   int count = ObjectsTotal();
   for (int i = count - 1; i >= 0; i--)
   {
      string name = ObjectName(i);
      if (StringFind(name, ID()) != -1)
      {
         ObjectDelete(name);
      }
   }
}

void CheckLines() 
{
   double stopLevel = MarketInfo(Symbol_, MODE_STOPLEVEL);
   double tickSize = MarketInfo(Symbol_, MODE_TICKSIZE);
   int    r;                  // тикет искомой позиции
   int    t[];                // массив тикетов существующих позиций

   // заполнение массива тикетов существующих позиций
   ArrayResize(t, 0);

   for (int i = OrdersTotal() - 1; i >= 0; i--) 
   {
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
      if (OrderSymbol() != Symbol_) continue;
      if (OrderType() != OP_BUY && OrderType() != OP_SELL) continue;
      
      if (OrderStopLoss() > 0 || OrderTakeProfit() > 0)
      {
         r = ArraySize(t);
         ArrayResize(t, r + 1);
         t[r] = OrderTicket();
      }
   }

   // удаление лишних (ненужных) линий
   for (i = ObjectsTotal() - 1; i >= 0; i--)
   {
      string name = ObjectName(i);
      if (StringFind(name, ID()) == -1 || ObjectType(name) != OBJ_HLINE) continue;

      r = GetTicketFromName(name);
      if (ArraySearchInt(t, r) < 0 || !OrderSelect(r, SELECT_BY_TICKET))
      {
         ObjectDelete(name);
         continue;
      }
   }
   
   for (i = ArraySize(t) - 1; i >= 0; i--)
   {
      if (!OrderSelect(t[i], SELECT_BY_TICKET)) continue;
      
      double sl = OrderStopLoss();
      double tp = OrderTakeProfit();
      
      if (sl != 0)
      {
         SetHLine("SL " + ID() + " " + t[i], sl, SL_Color);
         
         if (!IsDragging)
         {
            if (CheckDrag("SL " + ID() + " " + t[i]))
            {
               IsDragging = true;
               DragName = "SL " + ID() + " " + t[i];
            }
         }
      }
      
      if (tp != 0)
      {
         SetHLine("TP " + ID() + " " + t[i], tp, TP_Color);

         if (!IsDragging)
         {
            if (CheckDrag("TP " + ID() + " " + t[i]))
            {
               IsDragging = true;
               DragName = "TP " + ID() + " " + t[i];
            }
         }
      }
   }
}

int ArraySearchInt(int a[], int what) 
{
   for (int i = 0; i < ArraySize(a); i++) 
   {
      if (a[i] == what) return(i);
   }
   return(-1);
}

string Type(int type)
{
   switch (type)
   {
      case OP_BUY:      return ("Buy");
      case OP_SELL:     return ("Sell");
      case OP_BUYLIMIT: return ("Buy Limit");
      case OP_SELLLIMIT:return ("Sell Limit");
      case OP_BUYSTOP:  return ("Buy Stop");
      case OP_SELLSTOP: return ("Sell Stop");
   }
   return ("Unknown");
}}
   return ("Unknown");
}