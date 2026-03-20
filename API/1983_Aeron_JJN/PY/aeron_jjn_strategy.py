import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class aeron_jjn_strategy(Strategy):

    def __init__(self):
        super(aeron_jjn_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(aeron_jjn_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)

        if self._has_prev:
            prev_bull = self._prev_close > self._prev_open
            prev_bear = self._prev_close < self._prev_open
            curr_bull = float(candle.ClosePrice) > float(candle.OpenPrice)
            curr_bear = float(candle.ClosePrice) < float(candle.OpenPrice)

            if prev_bear and curr_bull and float(candle.ClosePrice) > ema_val and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif prev_bull and curr_bear and float(candle.ClosePrice) < ema_val and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)
        self._has_prev = True

    def OnReseted(self):
        super(aeron_jjn_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def CreateClone(self):
        return aeron_jjn_strategy()
