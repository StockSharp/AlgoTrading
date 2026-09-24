import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates, Sides


class captain_backtest_model_strategy(Strategy):
    """
    Captain Backtest Model.
    Builds the morning range, fixes the day's bias on the first range break,
    waits for a retracement, then enters on a close through the previous candle.
    """

    def __init__(self):
        super(captain_backtest_model_strategy, self).__init__()

        self._prev_range_start = self.Param("PrevRangeStart", TimeSpan(6, 0, 0))
        self._prev_range_end = self.Param("PrevRangeEnd", TimeSpan(10, 0, 0))
        self._take_start = self.Param("TakeStart", TimeSpan(10, 0, 0))
        self._take_end = self.Param("TakeEnd", TimeSpan(11, 15, 0))
        self._trade_start = self.Param("TradeStart", TimeSpan(10, 0, 0))
        self._trade_end = self.Param("TradeEnd", TimeSpan(16, 0, 0))
        self._risk = self.Param("Risk", 25.0)
        self._reward = self.Param("Reward", 75.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._session_date = None
        self._range_high = None
        self._range_low = None
        self._bias = 0
        self._retracement_seen = False
        self._traded_today = False
        self._previous_candle = None
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(captain_backtest_model_strategy, self).OnReseted()
        self._reset_session(None)

    def OnStarted2(self, time):
        super(captain_backtest_model_strategy, self).OnStarted2(time)
        self._reset_session(None)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date
        if self._session_date != date:
            if self._session_date is not None and self.Position != 0:
                self.ClosePosition()
            self._reset_session(date)

        t = candle.OpenTime.TimeOfDay
        prev_range_start = self._prev_range_start.Value
        prev_range_end = self._prev_range_end.Value
        take_start = self._take_start.Value
        take_end = self._take_end.Value
        trade_start = self._trade_start.Value
        trade_end = self._trade_end.Value
        risk = float(self._risk.Value)
        reward = float(self._reward.Value)

        if t >= prev_range_start and t < prev_range_end:
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)
            self._range_high = high if self._range_high is None else max(self._range_high, high)
            self._range_low = low if self._range_low is None else min(self._range_low, low)
            self._previous_candle = candle
            return

        if self.Position > 0 and self._entry_price > 0:
            if float(candle.LowPrice) <= self._entry_price - risk or float(candle.HighPrice) >= self._entry_price + reward:
                self.ClosePosition()
                self._previous_candle = candle
                return
        elif self.Position < 0 and self._entry_price > 0:
            if float(candle.HighPrice) >= self._entry_price + risk or float(candle.LowPrice) <= self._entry_price - reward:
                self.ClosePosition()
                self._previous_candle = candle
                return

        if t >= trade_end:
            if self.Position != 0:
                self.ClosePosition()
            self._previous_candle = candle
            return

        if self._bias == 0 and self._range_high is not None and self._range_low is not None and t >= take_start and t <= take_end:
            broke_high = float(candle.HighPrice) > self._range_high
            broke_low = float(candle.LowPrice) < self._range_low
            if broke_high != broke_low:
                self._bias = 1 if broke_high else -1

        if self._bias == 0 or self._traded_today or t < trade_start or t > trade_end or self._previous_candle is None:
            self._previous_candle = candle
            return

        if self._bias > 0:
            if float(candle.ClosePrice) < float(candle.OpenPrice) or float(candle.LowPrice) < float(self._previous_candle.LowPrice):
                self._retracement_seen = True

            if self._retracement_seen and float(candle.ClosePrice) > float(self._previous_candle.HighPrice):
                self._enter(Sides.Buy, float(candle.ClosePrice))
        else:
            if float(candle.ClosePrice) > float(candle.OpenPrice) or float(candle.HighPrice) > float(self._previous_candle.HighPrice):
                self._retracement_seen = True

            if self._retracement_seen and float(candle.ClosePrice) < float(self._previous_candle.LowPrice):
                self._enter(Sides.Sell, float(candle.ClosePrice))

        self._previous_candle = candle

    def _enter(self, side, price):
        if self._traded_today:
            return

        self._traded_today = True
        self._entry_price = price

        if side == Sides.Buy:
            self.BuyMarket()
        else:
            self.SellMarket()

    def _reset_session(self, date):
        self._session_date = date
        self._range_high = None
        self._range_low = None
        self._bias = 0
        self._retracement_seen = False
        self._traded_today = False
        self._previous_candle = None
        self._entry_price = 0.0

    def CreateClone(self):
        return captain_backtest_model_strategy()
