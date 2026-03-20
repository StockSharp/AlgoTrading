import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class step_stochastic_cross_strategy(Strategy):
    def __init__(self):
        super(step_stochastic_cross_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame", "General")
        self._k_period = self.Param("KPeriod", 14) \
            .SetDisplay("K Period", "Stochastic K period", "Parameters")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Stochastic D period", "Parameters")
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    def OnReseted(self):
        super(step_stochastic_cross_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(step_stochastic_cross_strategy, self).OnStarted(time)
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._has_prev = False
        stoch = StochasticOscillator()
        stoch.K.Length = self.k_period
        stoch.D.Length = self.d_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stoch)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        if value.K is None or value.D is None:
            return
        k_value = float(value.K)
        d_value = float(value.D)
        if not self._has_prev:
            self._prev_k = k_value
            self._prev_d = d_value
            self._has_prev = True
            return
        # When D is above 50, look for K crossing below D (buy signal)
        if d_value > 50.0:
            if self.Position < 0:
                self.BuyMarket()
            if self._prev_k > self._prev_d and k_value <= d_value and self.Position <= 0:
                self.BuyMarket()
        # When D is below 50, look for K crossing above D (sell signal)
        elif d_value < 50.0:
            if self.Position > 0:
                self.SellMarket()
            if self._prev_k < self._prev_d and k_value >= d_value and self.Position >= 0:
                self.SellMarket()
        self._prev_k = k_value
        self._prev_d = d_value

    def CreateClone(self):
        return step_stochastic_cross_strategy()
