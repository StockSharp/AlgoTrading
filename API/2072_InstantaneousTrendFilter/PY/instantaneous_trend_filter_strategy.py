import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class instantaneous_trend_filter_strategy(Strategy):
    def __init__(self):
        super(instantaneous_trend_filter_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._alpha = self.Param("Alpha", 0.07) \
            .SetDisplay("Alpha", "Filter smoothing coefficient", "Indicator")
        self._k0 = 0.0
        self._k1 = 0.0
        self._k2 = 0.0
        self._k3 = 0.0
        self._k4 = 0.0
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._itrend_prev1 = 0.0
        self._itrend_prev2 = 0.0
        self._trigger_prev = 0.0
        self._bars = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def alpha(self):
        return self._alpha.Value

    def OnReseted(self):
        super(instantaneous_trend_filter_strategy, self).OnReseted()
        self._bars = 0
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._itrend_prev1 = 0.0
        self._itrend_prev2 = 0.0
        self._trigger_prev = 0.0
        self._k0 = 0.0
        self._k1 = 0.0
        self._k2 = 0.0
        self._k3 = 0.0
        self._k4 = 0.0

    def OnStarted(self, time):
        super(instantaneous_trend_filter_strategy, self).OnStarted(time)
        self._bars = 0
        a = float(self.alpha)
        a2 = a * a
        self._k0 = a - a2 / 4.0
        self._k1 = 0.5 * a2
        self._k2 = a - 0.75 * a2
        self._k3 = 2.0 * (1.0 - a)
        self._k4 = (1.0 - a) * (1.0 - a)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._bars < 2:
            itrend = close
        elif self._bars < 4:
            itrend = (close + 2.0 * self._prev_close + self._prev_prev_close) / 4.0
        else:
            itrend = (self._k0 * close + self._k1 * self._prev_close -
                      self._k2 * self._prev_prev_close + self._k3 * self._itrend_prev1 -
                      self._k4 * self._itrend_prev2)

        trigger = 2.0 * itrend - self._itrend_prev2

        cross_down = self._trigger_prev > self._itrend_prev1 and trigger < itrend
        cross_up = self._trigger_prev < self._itrend_prev1 and trigger > itrend

        if cross_down and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_up and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._itrend_prev2 = self._itrend_prev1
        self._itrend_prev1 = itrend
        self._trigger_prev = trigger
        self._prev_prev_close = self._prev_close
        self._prev_close = close
        self._bars += 1

    def CreateClone(self):
        return instantaneous_trend_filter_strategy()
