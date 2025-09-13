//+------------------------------------------------------------------+
//|                                                    RealTrend.mqh |
//|                               Copyright © Evgeniy Trofimov, 2010 |
//|                                  http://forum.mql4.com/ru/16793/ |
//+------------------------------------------------------------------+
int Slippage = 3;
//+------------------------------------------------------------------+
int SELL_pips (double lt, double fd_SL, double fd_TP, int magic = 0, string comment = "")
{
    double sl, tp;
    int ticket = -1; //продаём по цене Bid
    int LE = 135;
//----
    if (lt < MarketInfo (Symbol(), MODE_MINLOT)) return (0);
    if (lt > MarketInfo (Symbol(), MODE_MAXLOT)) lt = MarketInfo (Symbol(), MODE_MAXLOT);
    if (lt > AccountFreeMargin() * 0.90 / MarketInfo (Symbol(), MODE_MARGINREQUIRED))
    {lt = AccountFreeMargin() * 0.90 / MarketInfo (Symbol(), MODE_MARGINREQUIRED);}
    lt = NormalizeDouble (lt, MathAbs (MathLog (MarketInfo (Symbol(), MODE_LOTSTEP)) / MathLog (10.0)) + 0.5);
    while (LE > 134 && LE < 139)
    {
        /*if (sl_pips > 0)
        {
            if (sl_pips < MarketInfo (Symbol(), MODE_STOPLEVEL)) sl_pips = MarketInfo (Symbol(), MODE_STOPLEVEL);
            sl = NormalizeDouble (Bid + sl_pips * Point, Digits);
        }
        if (tp_pips > 0)
        {
            if (tp_pips < MarketInfo (Symbol(), MODE_STOPLEVEL)) tp_pips = MarketInfo (Symbol(), MODE_STOPLEVEL);
            tp = NormalizeDouble (Bid - tp_pips * Point, Digits);
        }*/
        ticket = OrderSend (Symbol(), OP_SELL, lt, Bid, Slippage, fd_SL, fd_TP, comment, magic, 0, Red); 
        LE = GetLastError();
        Sleep (5000);
        RefreshRates();
   }
   if (ticket > 0) Sleep (10000);
//----
   return (ticket);
}
//+------------------------------------------------------------------+
int BUY_pips (double lt, double fd_SL, double fd_TP, int magic = 0, string comment = "")
{
    double sl, tp;
    int ticket = -1; //покупаем по цене Ask
    int LE = 135;
//----
    if (lt < MarketInfo (Symbol(), MODE_MINLOT)) return (0);
    if (lt > MarketInfo (Symbol(), MODE_MAXLOT)) lt = MarketInfo (Symbol(), MODE_MAXLOT);
    if (lt > AccountFreeMargin() * 0.90 / MarketInfo (Symbol(), MODE_MARGINREQUIRED))
    {lt=AccountFreeMargin() * 0.90 / MarketInfo (Symbol(), MODE_MARGINREQUIRED);}
    lt = NormalizeDouble (lt, MathAbs (MathLog (MarketInfo (Symbol(), MODE_LOTSTEP)) / MathLog (10.0)) + 0.5);
    while (LE > 134 && LE < 139)
    {
        /*if (sl_pips > 0)
        {
            sl_pips = MathMax (sl_pips, MarketInfo (Symbol(), MODE_STOPLEVEL));
            sl = NormalizeDouble (Ask - sl_pips * Point, Digits);
        }
        if (tp_pips > 0)
        {
            tp_pips = MathMax (tp_pips, MarketInfo (Symbol(), MODE_STOPLEVEL));
            tp = NormalizeDouble (Ask + tp_pips * Point, Digits);
        }*/
        ticket = OrderSend (Symbol(), OP_BUY, lt, Ask, Slippage, fd_SL, fd_TP, comment, magic, 0, Blue); 
        LE = GetLastError();
        Sleep (5000);
        RefreshRates();
    }
    if (ticket > 0) Sleep (10000);
//----
    return (ticket);
}
//+------------------------------------------------------------------+
//|  Создание отложенных ордеров на продажу (текущая цена выше price)|
//+------------------------------------------------------------------+
int SELLSleep (double price, double lt, double sl = 0, double tp = 0, int magic = 0, string comment = "")
{
    double stoplimit = (MarketInfo (Symbol(), MODE_STOPLEVEL) + Slippage) * Point;
    double spread = MarketInfo (Symbol(), MODE_SPREAD) * Point;
    int ticket = -1; //продаём по цене Bid
//----
    Print ("Попытка создать отложенный ордер на продажу по цене ", price, "; SL = ", sl, "; TP = ", tp, "; Ask = ", Ask, "; Bid = ", Bid);   
    price = NormalizeDouble (price, Digits);
    if (Bid - price < stoplimit)
    {
        Print ("Превышена минимальная граница выставления отложенного ордера на продажу по цене открытия");
        Print ("Минимальная граница ", stoplimit / Point, " пунктов");
        return (0);
    }
    if (sl > 0)
    {
        sl = NormalizeDouble (sl, Digits);
        if (sl - price - spread < stoplimit)
        {
            Print ("SELLSleep: Неправильная граница StopLoss. Минимум: ", stoplimit / Point, " пунктов от цены Bid");
            return (0);
        }
    }
    if (tp > 0)
    {
        tp = NormalizeDouble (tp, Digits);
        if (price - tp < stoplimit)
        {
            Print ("SELLSleep: Неправильная граница TakeProfit. Минимум: ", stoplimit / Point, " пунктов от цены Ask");
            return (0);
        }
    }
    ticket = OrderSend (Symbol(), OP_SELLSTOP, lt, price, Slippage, sl, tp, comment, magic, 0, Red); 
    if (ticket > 0) Sleep (10000);
//----
    return (ticket);
}
//+------------------------------------------------------------------+
//|  Создание отложенных ордеров на покупку (текущая цена ниже price)|
//+------------------------------------------------------------------+
int BUYSleep (double price, double lt, double sl = 0, double tp = 0, int magic = 0, string comment = "")
{
    double stoplimit = (MarketInfo (Symbol(), MODE_STOPLEVEL) + Slippage) * Point;
    double spread = MarketInfo (Symbol(), MODE_SPREAD) * Point;
    int ticket = -1; //покупаем по цене Ask
//----
    Print ("Попытка создать отложенный ордер на покупку по цене ", price, "; SL = ", sl, "; TP = ", tp, "; Ask = ", Ask, "; Bid = ", Bid);
    price = NormalizeDouble (price, Digits);
    if (price - Ask < stoplimit)
    {
        Print ("Превышена минимальная граница выставления отложенного ордера на покупку по цене открытия");
        Print ("Минимальная граница ", stoplimit / Point, " пунктов");
        return (0);
    }
    if (sl > 0)
    {
        sl = NormalizeDouble (sl, Digits);
        if (price - spread - sl < stoplimit)
        {
            Print ("BUYSleep: Неправильная граница StopLoss. Минимум: ", stoplimit / Point, " пунктов от цены Bid");
            return (0);
        }
    }
    if (tp > 0)
    {
        tp = NormalizeDouble (tp, Digits);
        if (tp - price < stoplimit)
        {
            Print ("BUYSleep: Неправильная граница TakeProfit. Минимум: ", stoplimit / Point, " пунктов от цены Ask");
            return (0);
        }
    }
    ticket = OrderSend (Symbol(), OP_BUYSTOP, lt, price, Slippage, sl, tp, comment, magic, 0, Blue); 
    if (ticket > 0) Sleep (10000);
//----
    return (ticket);
}
//+------------------------------------------------------------------+
int Modify (int ticket, double sl = 0.0, double tp = 0.0, bool fb_NULL = false)
{
    double stoplimit = MarketInfo (Symbol(), MODE_STOPLEVEL) * Point;
//----
    if (OrderSelect (ticket, SELECT_BY_TICKET))
    {
        sl = NormalizeDouble (sl, Digits);
        tp = NormalizeDouble (tp, Digits);
      
        if (OrderType() == OP_BUY)
        {
            if (sl > 0)
            {
                if (OrderStopLoss() > 0)
                {if (OrderStopLoss() + Slippage * Point >= sl) return(0);}
                if (Bid - sl < stoplimit)
                {
                    Print ("Modify: Слишком близкий стоп для BUY (", (Bid - sl) / Point, " пунктов). Требуется не менее: ", stoplimit / Point);
                    return (0);
                }
            }
            if (tp > 0)
            {
                if (tp - Ask < stoplimit)
                {
                    Print ("Modify: Слишком близкий профит для BUY (", (tp - Ask) / Point, " пунктов). Требуется не менее: ", stoplimit / Point);
                    return (0);
                }
            }
        }
        else if (OrderType() == OP_SELL)
        {
            if (sl > 0)
            {
                if (OrderStopLoss() > 0)
                {if (OrderStopLoss() - Slippage * Point <= sl) return (0);}
                if (sl - Ask < stoplimit)
                {
                    Print ("Modify: Слишком близкий стоп для SELL (", (sl - Ask) / Point, " пунктов). Требуется не менее: ", stoplimit / Point);
                    return (0);
                }
            }
            if (tp > 0)
            {
                if (Bid - tp < stoplimit)
                {
                    Print ("Modify: Слишком близкий профит для SELL (", (Bid - tp) / Point," пунктов). Требуется не менее: ", stoplimit / Point);
                    return (0);
                }
            }
        }
        if ((sl > 0 && tp > 0) || fb_NULL)
        {if (!OrderModify (ticket, OrderOpenPrice(), sl, tp, 0)) Print ("Ошибка OrderModify(): ", GetLastError());}
        else if (sl > 0)
        {if (!OrderModify (ticket, OrderOpenPrice(), sl, OrderTakeProfit(), 0)) Print ("Ошибка OrderModify(): ", GetLastError());}
        else if (tp > 0)
        {if (!OrderModify (ticket, OrderOpenPrice(), OrderStopLoss(), tp, 0)) Print ("Ошибка OrderModify(): ", GetLastError());}
    }
//----
    return (0);
}
//+------------------------------------------------------------------+
//|  Возвращает true, если позиция с определённым магическим числом  |
//|  открыта после TimeOpenCandle                                    |
//+------------------------------------------------------------------+
bool OrderExist (datetime TimeOpenCandle, int fMagic = 0)
{
    int j = OrdersTotal();
//----
    for (int i = 0; i < j; i++)
    {
        if (OrderSelect (i, SELECT_BY_POS))
        {
            if (OrderMagicNumber() == fMagic || fMagic == 0)
            {
                if (OrderOpenTime() >= TimeOpenCandle)
                {return (true);}
            }
        }
    }// Next i
//----
   return (false);
}
//+------------------------------------------------------------------+

