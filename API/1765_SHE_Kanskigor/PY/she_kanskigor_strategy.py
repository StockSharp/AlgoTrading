import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class she_kanskigor_strategy(Strategy):
    def __init__(self):
        super(she_kanskigor_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_prev_open = 0.0
        self._prev_prev_close = 0.0
        self._candle_count = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(she_kanskigor_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_prev_open = 0.0
        self._prev_prev_close = 0.0
        self._candle_count = 0

    def OnStarted(self, time):
        super(she_kanskigor_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        self.SubscribeCandles(self.candle_type).Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._candle_count += 1

        if self._candle_count < 3:
            self._prev_prev_open = self._prev_open
            self._prev_prev_close = self._prev_close
            self._prev_open = float(candle.OpenPrice)
            self._prev_close = float(candle.ClosePrice)
            return

        ev = float(ema_value)
        close = float(candle.ClosePrice)

        two_bearish = self._prev_prev_open > self._prev_prev_close and self._prev_open > self._prev_close
        two_bullish = self._prev_prev_open < self._prev_prev_close and self._prev_open < self._prev_close

        if two_bearish and close > ev and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif two_bullish and close < ev and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_open = self._prev_open
        self._prev_prev_close = self._prev_close
        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)

    def CreateClone(self):
        return she_kanskigor_strategy()
