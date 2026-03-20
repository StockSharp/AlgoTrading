import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class four_screens_strategy(Strategy):

    def __init__(self):
        super(four_screens_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_is_bull = False
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
        super(four_screens_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)

        if self._prev_ha_open == 0.0 and self._prev_ha_close == 0.0:
            ha_open = (float(candle.OpenPrice) + float(candle.ClosePrice)) / 2.0
            ha_close = (float(candle.OpenPrice) + float(candle.HighPrice)
                        + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
            ha_close = (float(candle.OpenPrice) + float(candle.HighPrice)
                        + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0

        is_bull = ha_close > ha_open
        close = float(candle.ClosePrice)

        if self._has_prev:
            if is_bull and not self._prev_is_bull and close > ema_val and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif not is_bull and self._prev_is_bull and close < ema_val and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_is_bull = is_bull
        self._has_prev = True

    def OnReseted(self):
        super(four_screens_strategy, self).OnReseted()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_is_bull = False
        self._has_prev = False

    def CreateClone(self):
        return four_screens_strategy()
