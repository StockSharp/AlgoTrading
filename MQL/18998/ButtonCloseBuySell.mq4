//+------------------------------------------------------------------+
//|                                           ButtonCloseBuySell.mq4 |
//|                                      Copyright 2017, Erich Focht |
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, Erich Focht (elfo13)"
#property link      ""
#property version   "1.00"
#property strict

//--- input parameters
input int Magic = 0; // Magic number, -1 for all, 0 for manual
input string FilterComment = ""; // Filter by order comment string
input int Slippage = 3; // Maximum allowed slippage

//
double profitB = 0.0, profitS = 0.0;


int OnInit()
{
    ObjectCreate(0, "CloseBuy", OBJ_BUTTON, 0, 0, 0);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_XDISTANCE, 331);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_YDISTANCE, 0);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_XSIZE, 105);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_YSIZE, 16);
    ObjectSetString(0, "CloseBuy", OBJPROP_FONT, "Arial");
    ObjectSetInteger(0, "CloseBuy", OBJPROP_FONTSIZE, 8);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_COLOR, White);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_BGCOLOR, clrDarkSlateBlue);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_BORDER_COLOR, Yellow);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_BORDER_TYPE, BORDER_FLAT);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_BACK, false);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_HIDDEN, true);
    ObjectSetInteger(0, "CloseBuy", OBJPROP_STATE, false);

    ObjectCreate(0,"CloseSell", OBJ_BUTTON, 0, 0, 0);
    ObjectSetInteger(0,"CloseSell", OBJPROP_XDISTANCE, 225);
    ObjectSetInteger(0,"CloseSell", OBJPROP_YDISTANCE, 0);
    ObjectSetInteger(0,"CloseSell", OBJPROP_XSIZE, 105);
    ObjectSetInteger(0,"CloseSell", OBJPROP_YSIZE, 16);
    ObjectSetString(0, "CloseSell", OBJPROP_FONT, "Arial");
    ObjectSetInteger(0, "CloseSell", OBJPROP_FONTSIZE, 8);
    ObjectSetInteger(0, "CloseSell", OBJPROP_COLOR, White);
    ObjectSetInteger(0, "CloseSell", OBJPROP_BGCOLOR, clrDarkMagenta);
    ObjectSetInteger(0, "CloseSell", OBJPROP_BORDER_COLOR, Yellow);
    ObjectSetInteger(0, "CloseSell", OBJPROP_BORDER_TYPE, BORDER_FLAT);
    ObjectSetInteger(0, "CloseSell", OBJPROP_BACK, false);
    ObjectSetInteger(0, "CloseSell", OBJPROP_HIDDEN, true);
    ObjectSetInteger(0, "CloseSell", OBJPROP_STATE, false);
    calc_profits();
    ObjectSetString(0, "CloseBuy", OBJPROP_TEXT,
                    "Close Buys " + DoubleToStr(profitB, 2));
    ObjectSetString(0, "CloseSell", OBJPROP_TEXT,
                    "Close Sells " + DoubleToStr(profitS, 2));
    return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
    ObjectDelete(0,"CloseBuy");
    ObjectDelete(0,"CloseSell");
}

void calc_profits()
{
    profitB = 0.0; profitS = 0.0;
    for (int cnt = 0; cnt < OrdersTotal(); cnt++) {
        if (!OrderSelect(cnt, SELECT_BY_POS))
            continue;  
        if ((Magic >= 0 && OrderMagicNumber() != Magic) || OrderSymbol() != Symbol())
            continue;
        if (StringLen(FilterComment) > 0 && StringCompare(FilterComment, OrderComment(), true) != 0)
            continue;
        if (OrderCloseTime() == 0) {
            if (OrderType() == OP_BUY)
                profitB += OrderProfit() + OrderSwap() + OrderCommission();
            else if (OrderType() == OP_SELL)
                profitS += OrderProfit() + OrderSwap() + OrderCommission();
        }
    }
}

void OnTick()
{
    calc_profits();
    ObjectSetString(0, "CloseBuy", OBJPROP_TEXT,
                    "Close Buys " + DoubleToStr(profitB, 2));
    ObjectSetString(0, "CloseSell", OBJPROP_TEXT,
                    "Close Sells " + DoubleToStr(profitS, 2));
}

void OnChartEvent(const int id, const long &lparam,
                  const double &dparam, const string &sparam)
{
    if (sparam == "CloseBuy") {
        CloseOpenPositions(OP_BUY);
        ObjectSetInteger(0, "CloseBuy", OBJPROP_STATE, false);    
    }
    if (sparam == "CloseSell") {
        CloseOpenPositions(OP_SELL);
        ObjectSetInteger(0, "CloseBuy", OBJPROP_STATE, false);    
    }
}

void CloseOpenPositions(int op)
{
    for (int cnt = OrdersTotal() - 1; cnt >= 0; cnt--) { 
        if (!OrderSelect(cnt, SELECT_BY_POS))
            continue;  
        if ((Magic >= 0 && OrderMagicNumber() != Magic) || OrderSymbol() != Symbol())
            continue;
        if (StringLen(FilterComment) > 0 && StringCompare(FilterComment, OrderComment(), true) != 0)
            continue;
        if (OrderCloseTime() == 0 && OrderType() == op) {
            if (!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(),
                            Slippage, CLR_NONE))
                Alert("Close attempt for order #", OrderTicket(),
                      " returned error code ", GetLastError());
        }
    }
}
