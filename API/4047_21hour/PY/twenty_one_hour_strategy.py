import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class twenty_one_hour_strategy(Strategy):
    """Time-based breakout: enter on breakout from previous candle range, close at stop hour."""
    def __init__(self):
        super(twenty_one_hour_strategy, self).__init__()
        self._start_hour = self.Param("StartHour", 10).SetDisplay("Start Hour", "Hour to look for breakout entries", "Schedule")
        self._stop_hour = self.Param("StopHour", 22).SetDisplay("Stop Hour", "Hour to close positions", "Schedule")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(twenty_one_hour_strategy, self).OnReseted()
        self._prev_high = 0
        self._prev_low = 0
        self._has_prev = False
        self._traded_today = False
        self._last_trade_day = -1

    def OnStarted(self, time):
        super(twenty_one_hour_strategy, self).OnStarted(time)
        self._prev_high = 0
        self._prev_low = 0
        self._has_prev = False
        self._traded_today = False
        self._last_trade_day = -1

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.OpenTime.Hour
        day = candle.OpenTime.DayOfYear

        # Reset daily flag
        if day != self._last_trade_day:
            self._traded_today = False
            self._last_trade_day = day

        # Close at stop hour
        start_h = self._start_hour.Value
        stop_h = self._stop_hour.Value

        if hour >= stop_h and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()

        # Entry at start hour window
        if hour >= start_h and hour < stop_h and not self._traded_today and self._has_prev and self.Position == 0:
            close = float(candle.ClosePrice)
            if close > self._prev_high:
                self.BuyMarket()
                self._traded_today = True
            elif close < self._prev_low:
                self.SellMarket()
                self._traded_today = True

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._has_prev = True

    def CreateClone(self):
        return twenty_one_hour_strategy()
