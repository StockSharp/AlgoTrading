import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class average_change_candle_strategy(Strategy):
    def __init__(self):
        super(average_change_candle_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles", "General")
        self._ema_period = self.Param("EmaPeriod", 12) \
            .SetDisplay("EMA Period", "EMA smoothing period", "Indicators")

        self._prev_smoothed_open = 0.0
        self._prev_smoothed_close = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(average_change_candle_strategy, self).OnReseted()
        self._prev_smoothed_open = 0.0
        self._prev_smoothed_close = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(average_change_candle_strategy, self).OnStarted(time)

        self._prev_smoothed_open = 0.0
        self._prev_smoothed_close = 0.0
        self._initialized = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(ema, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ev = float(ema_value)
        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)

        alpha = 2.0 / (self.EmaPeriod + 1.0)

        if not self._initialized:
            self._prev_smoothed_open = o
            self._prev_smoothed_close = c
            self._initialized = True
            return

        smoothed_open = alpha * o + (1.0 - alpha) * self._prev_smoothed_open
        smoothed_close = alpha * c + (1.0 - alpha) * self._prev_smoothed_close

        prev_bullish = self._prev_smoothed_close > self._prev_smoothed_open
        curr_bullish = smoothed_close > smoothed_open

        if curr_bullish and not prev_bullish and c > ev and self.Position <= 0:
            self.BuyMarket()
        elif not curr_bullish and prev_bullish and c < ev and self.Position >= 0:
            self.SellMarket()

        self._prev_smoothed_open = smoothed_open
        self._prev_smoothed_close = smoothed_close

    def CreateClone(self):
        return average_change_candle_strategy()
