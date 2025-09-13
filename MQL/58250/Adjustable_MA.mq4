//+------------------------------------------------------------------+
//|                                 Adjustable Moving Average EA.mq4 |
//|                                           Copyright 2025, Manasvi |
//+------------------------------------------------------------------+
#property copyright "Copyright 2025, Manasvi"
#property version   "1.00"
#property strict

enum ENUM_ENTRY_MODE
{
    ENTRY_BUY_ONLY,
    ENTRY_SELL_ONLY,
    ENTRY_BOTH
};

input group "Strategy Settings"
input int FastPeriod = 3;
input int SlowPeriod = 9;
input ENUM_MA_METHOD MAPriceType = MODE_EMA;
int MinGapPoints = 3;
input int SL_Points = 0;
input int TP_Points = 0;
input int TrailStopPoints = 0; // TrailStopPips
input ENUM_ENTRY_MODE EntryMode = ENTRY_BOTH;
string SessionStart = "00:00";
string SessionEnd = "23:59";
 bool CloseOutsideSession = true;
bool TrailOutsideSession = true;

input group "Lot Management"
input double FixedLot = 0.1;
bool EnableAutoLot = false;
double LotPer10kFreeMargin = 1;

input group "Trade Settings"
input int MaxSlippage = 3;
input string TradeComment = "AdjustableMovingAverageEA";

// Internal variables
int PrevBarCount = 0;
double Pip;
int SlippageBuffer;
int PrevSignal = 0;
int UniqueID;
bool TradingAllowed = false;
ENUM_SYMBOL_TRADE_EXECUTION ExecType;
int ShortMA;
int LongMA;

int OnInit()
{
    ShortMA = MathMin(FastPeriod, SlowPeriod);
    LongMA = MathMax(FastPeriod, SlowPeriod);

    if (ShortMA == LongMA)
    {
        Print("Fast and Slow MA periods must differ.");
        return INIT_FAILED;
    }

    Pip = Point;
    SlippageBuffer = MaxSlippage;

    if ((Point == 0.00001) || (Point == 0.001))
    {
        Pip *= 10;
        SlippageBuffer *= 10;
    }
  Comment("EA is alive and checking conditions...");
    UniqueID = Period() + 90235678;
    return INIT_SUCCEEDED;
  

}

void OnTick()
{
    if (ShortMA == LongMA) return;

    TradingAllowed = WithinTradingSession();

    if ((TrailStopPoints > 0) && (TradingAllowed || TrailOutsideSession)) ApplyTrailing();

    if (PrevBarCount == Bars) return;
    PrevBarCount = Bars;

    if ((Bars < LongMA) || (!IsTradeAllowed())) return;

    ExecType = (ENUM_SYMBOL_TRADE_EXECUTION)SymbolInfoInteger(Symbol(), SYMBOL_TRADE_EXEMODE);

    DetectSignal();
}

void DetectSignal()
{
    double FastMA = iMA(NULL, 0, ShortMA, 0, MAPriceType, PRICE_CLOSE, 0);
    double SlowMA = iMA(NULL, 0, LongMA, 0, MAPriceType, PRICE_CLOSE, 0);

    if (PrevSignal == 0)
    {
        if ((FastMA - SlowMA) >= MinGapPoints * Pip) PrevSignal = 1;
        else if ((SlowMA - FastMA) >= MinGapPoints * Pip) PrevSignal = -1;
        return;
    }
    else if (PrevSignal == 1)
    {
        if ((SlowMA - FastMA) >= MinGapPoints * Pip)
        {
            if (TradingAllowed || CloseOutsideSession) CloseOpenTrades();
            if (TradingAllowed && EntryMode != ENTRY_BUY_ONLY) OpenSell();
            PrevSignal = -1;
        }
    }
    else if (PrevSignal == -1)
    {
        if ((FastMA - SlowMA) >= MinGapPoints * Pip)
        {
            if (TradingAllowed || CloseOutsideSession) CloseOpenTrades();
            if (TradingAllowed && EntryMode != ENTRY_SELL_ONLY) OpenBuy();
            PrevSignal = 1;
        }
    }
}

void CloseOpenTrades()
{
    for (int i = 0; i < OrdersTotal(); i++)
    {
        if (!OrderSelect(i, SELECT_BY_POS)) continue;
        if (OrderSymbol() == Symbol() && OrderMagicNumber() == UniqueID)
        {
            for (int j = 0; j < 10; j++)
            {
                RefreshRates();
                bool result = (OrderType() == OP_BUY) ? OrderClose(OrderTicket(), OrderLots(), Bid, SlippageBuffer) : OrderClose(OrderTicket(), OrderLots(), Ask, SlippageBuffer);
                if (result) break;
                else Print("OrderClose failed: ", GetLastError());
            }
        }
    }
}

int OpenSell()
{
    double SL = 0, TP = 0;
    RefreshRates();

    if (ExecType != SYMBOL_TRADE_EXECUTION_MARKET)
    {
        if (SL_Points > 0) SL = Bid + SL_Points * Pip;
        if (TP_Points > 0) TP = Bid - TP_Points * Pip;
    }

    double lot = CalculateLot();
    string volumeCheck;
    if (!CheckVolumeValue(lot, volumeCheck))
    {
        Print("Sell Order Not Placed: ", volumeCheck);
        return -1;
    }

    int ticket = OrderSend(Symbol(), OP_SELL, lot, Bid, SlippageBuffer, SL, TP, TradeComment, UniqueID);
    if (ticket <= 0) Print("Sell order failed: ", GetLastError());
    return ticket;
}


int OpenBuy()
{
    double SL = 0, TP = 0;
    RefreshRates();

    if (ExecType != SYMBOL_TRADE_EXECUTION_MARKET)
    {
        if (SL_Points > 0) SL = Ask - SL_Points * Pip;
        if (TP_Points > 0) TP = Ask + TP_Points * Pip;
    }

    double lot = CalculateLot();
    string volumeCheck;
    if (!CheckVolumeValue(lot, volumeCheck))
    {
        Print("Buy Order Not Placed: ", volumeCheck);
        return -1;
    }

    int ticket = OrderSend(Symbol(), OP_BUY, lot, Ask, SlippageBuffer, SL, TP, TradeComment, UniqueID);
    if (ticket <= 0) Print("Buy order failed: ", GetLastError());
    return ticket;
}


void ApplyTrailing()
{
    for (int i = 0; i < OrdersTotal(); i++)
    {
        if (!OrderSelect(i, SELECT_BY_POS)) continue;
        if (OrderMagicNumber() == UniqueID && OrderSymbol() == Symbol())
        {
            RefreshRates();
            if (OrderType() == OP_BUY && Bid - OrderOpenPrice() >= TrailStopPoints * Pip)
            {
                if ((Bid - TrailStopPoints * Pip) - OrderStopLoss() > Point() / 2)
                    OrderModify(OrderTicket(), OrderOpenPrice(), Bid - TrailStopPoints * Pip, OrderTakeProfit(), 0);
            }
            else if (OrderType() == OP_SELL && OrderOpenPrice() - Ask >= TrailStopPoints * Pip)
            {
                if ((OrderStopLoss() - (Ask + TrailStopPoints * Pip) > Point() / 2) || OrderStopLoss() == 0)
                    OrderModify(OrderTicket(), OrderOpenPrice(), Ask + TrailStopPoints * Pip, OrderTakeProfit(), 0);
            }
        }
    }
}

double CalculateLot()
{
    if (!EnableAutoLot) return FixedLot;
    double calculated = NormalizeDouble((AccountFreeMargin() / 10000) * LotPer10kFreeMargin, 1);
    return (calculated <= 0) ? FixedLot : calculated;
}

bool WithinTradingSession()
{
    datetime now = TimeCurrent();
    return (now >= StringToTime(SessionStart) && now <= StringToTime(SessionEnd) + 59);
} //+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume, string &description)
{
   double min_volume = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);
   if (volume < min_volume)
   {
      description = StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f", min_volume);
      return false;
   }

   double max_volume = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);
   if (volume > max_volume)
   {
      description = StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f", max_volume);
      return false;
   }

   double volume_step = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_STEP);
   int ratio = (int)MathRound(volume / volume_step);
   if (MathAbs(ratio * volume_step - volume) > 0.0000001)
   {
      description = StringFormat("Volume is not a multiple of SYMBOL_VOLUME_STEP=%.2f, closest valid volume: %.2f",
                                 volume_step, ratio * volume_step);
      return false;
   }

   description = "Correct volume value";
   return true;
}
