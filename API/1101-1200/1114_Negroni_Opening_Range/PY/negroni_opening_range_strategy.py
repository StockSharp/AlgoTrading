import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

# Direction values, matching the C# TradeDirection enum.
DIRECTION_LONG_ONLY = 0
DIRECTION_SHORT_ONLY = 1
DIRECTION_LONG_SHORT = 2


class negroni_opening_range_strategy(Strategy):
    """
    Negroni opening range strategy.
    Each day the high and low of the pre-market window (or of the opening range window when UsePreMarketRange is off) form the range.
    Inside the trading session a close above the range high goes long and a close below the range low goes short, within the allowed
    Direction and at most MaxTradesPerDay entries a day. Any open position is closed at CloseTime. Times are UTC candle open times.
    """

    def __init__(self):
        super(negroni_opening_range_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 3).SetGreaterThanZero().SetDisplay("Max Trades Per Day", "Maximum entries per day", "Trading")
        self._direction = self.Param("Direction", DIRECTION_LONG_SHORT).SetDisplay("Direction", "Allowed trade direction (0 LongOnly, 1 ShortOnly, 2 LongShort)", "Trading")
        self._session_start = self.Param("SessionStart", TimeSpan(9, 30, 0)).SetDisplay("Session Start", "Start of the trading session", "Session")
        self._session_end = self.Param("SessionEnd", TimeSpan(14, 0, 0)).SetDisplay("Session End", "End of the trading session", "Session")
        self._close_time = self.Param("CloseTime", TimeSpan(16, 0, 0)).SetDisplay("Close Time", "Time when any open position is closed", "Session")
        self._use_pre_market_range = self.Param("UsePreMarketRange", True).SetDisplay("Use Pre-Market Range", "Use the pre-market range instead of the opening range", "Range")
        self._pre_market_start = self.Param("PreMarketStart", TimeSpan(8, 0, 0)).SetDisplay("Pre-Market Start", "Start of the pre-market window", "Range")
        self._pre_market_end = self.Param("PreMarketEnd", TimeSpan(9, 0, 0)).SetDisplay("Pre-Market End", "End of the pre-market window", "Range")
        self._open_range_start = self.Param("OpenRangeStart", TimeSpan(9, 5, 0)).SetDisplay("Open Range Start", "Start of the opening range window", "Range")
        self._open_range_end = self.Param("OpenRangeEnd", TimeSpan(9, 30, 0)).SetDisplay("Open Range End", "End of the opening range window", "Range")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_date = None
        self._range_high = None
        self._range_low = None
        self._trades_today = 0

    def OnReseted(self):
        super(negroni_opening_range_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(negroni_opening_range_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date
        if self._current_date is None or date != self._current_date:
            self._current_date = date
            self._range_high = None
            self._range_low = None
            self._trades_today = 0

        tod = candle.OpenTime.TimeOfDay.TotalMinutes
        if self._use_pre_market_range.Value:
            range_start = self._pre_market_start.Value.TotalMinutes
            range_end = self._pre_market_end.Value.TotalMinutes
        else:
            range_start = self._open_range_start.Value.TotalMinutes
            range_end = self._open_range_end.Value.TotalMinutes

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if range_start <= tod < range_end:
            self._range_high = high if self._range_high is None else max(self._range_high, high)
            self._range_low = low if self._range_low is None else min(self._range_low, low)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if tod >= self._close_time.Value.TotalMinutes:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        if tod < range_end or tod < self._session_start.Value.TotalMinutes or tod >= self._session_end.Value.TotalMinutes:
            return

        if self._range_high is None or self._range_low is None or self._trades_today >= self._max_trades_per_day.Value:
            return

        close = float(candle.ClosePrice)
        direction = self._direction.Value
        allow_long = direction != DIRECTION_SHORT_ONLY
        allow_short = direction != DIRECTION_LONG_ONLY

        if allow_long and close > self._range_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._trades_today += 1
        elif allow_short and close < self._range_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._trades_today += 1

    def CreateClone(self):
        return negroni_opening_range_strategy()
