import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class urdala_trol_strategy(Strategy):
    def __init__(self):
        super(urdala_trol_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._ema_length = self.Param("EmaLength", 14) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._trailing_percent = self.Param("TrailingPercent", 1.5) \
            .SetDisplay("Trailing %", "Trailing stop percent", "Risk")

        self._high_since_entry = 0.0
        self._low_since_entry = 1e18
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def TrailingPercent(self):
        return self._trailing_percent.Value

    def OnReseted(self):
        super(urdala_trol_strategy, self).OnReseted()
        self._high_since_entry = 0.0
        self._low_since_entry = 1e18
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(urdala_trol_strategy, self).OnStarted2(time)
        self._high_since_entry = 0.0
        self._low_since_entry = 1e18
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        ev = float(ema_value)

        if not self._has_prev:
            self._prev_close = close
            self._prev_ema = ev
            self._has_prev = True
            return

        if self.Position > 0:
            if high > self._high_since_entry:
                self._high_since_entry = high
            trail_stop = self._high_since_entry * (1.0 - self.TrailingPercent / 100.0)
            if close < trail_stop:
                self.SellMarket()
                self._high_since_entry = 0.0
                self._low_since_entry = 1e18
                self._prev_close = close
                self._prev_ema = ev
                return
        elif self.Position < 0:
            if low < self._low_since_entry:
                self._low_since_entry = low
            trail_stop = self._low_since_entry * (1.0 + self.TrailingPercent / 100.0)
            if close > trail_stop:
                self.BuyMarket()
                self._high_since_entry = 0.0
                self._low_since_entry = 1e18
                self._prev_close = close
                self._prev_ema = ev
                return

        bullish_cross = self._prev_close <= self._prev_ema and close > ev
        bearish_cross = self._prev_close >= self._prev_ema and close < ev

        if bullish_cross and self.Position <= 0:
            self.BuyMarket()
            self._high_since_entry = high
            self._low_since_entry = 1e18
        elif bearish_cross and self.Position >= 0:
            self.SellMarket()
            self._low_since_entry = low
            self._high_since_entry = 0.0

        self._prev_close = close
        self._prev_ema = ev

    def CreateClone(self):
        return urdala_trol_strategy()
