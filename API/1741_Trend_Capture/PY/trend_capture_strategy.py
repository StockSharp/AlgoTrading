import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trend_capture_strategy(Strategy):
    def __init__(self):
        super(trend_capture_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._prev_sar = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_capture_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._prev_sar = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(trend_capture_strategy, self).OnStarted(time)
        sar = ParabolicSar()
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        self.SubscribeCandles(self.candle_type).Bind(sar, ema, self.process_candle).Start()

    def process_candle(self, candle, sar_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sv = float(sar_value)
        ev = float(ema_value)

        if not self._has_prev:
            self._prev_close = close
            self._prev_ema = ev
            self._prev_sar = sv
            self._has_prev = True
            return

        above_sar = close > sv
        below_sar = close < sv
        prev_above_sar = self._prev_close > self._prev_sar
        prev_below_sar = self._prev_close < self._prev_sar

        if not prev_above_sar and above_sar and close > ev and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif not prev_below_sar and below_sar and close < ev and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_ema = ev
        self._prev_sar = sv

    def CreateClone(self):
        return trend_capture_strategy()
