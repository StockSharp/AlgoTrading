import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class auto_kd_strategy(Strategy):
    def __init__(self):
        super(auto_kd_strategy, self).__init__()
        self._kd_period = self.Param("KdPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("KD Period", "Base period for RSV", "Parameters")
        self._k_period = self.Param("KPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("K Period", "%K smoothing", "Parameters")
        self._d_period = self.Param("DPeriod", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("D Period", "%D smoothing", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._prev_k = None
        self._prev_d = None

    @property
    def kd_period(self):
        return self._kd_period.Value

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(auto_kd_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(auto_kd_strategy, self).OnStarted2(time)
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.kd_period
        stochastic.D.Length = self.d_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(stochastic, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        k = stoch_value.K
        d = stoch_value.D
        if k is None or d is None:
            return

        k = float(k)
        d = float(d)

        if self._prev_k is not None and self._prev_d is not None:
            if self._prev_k < self._prev_d and k > d and min(self._prev_k, k) < 30.0 and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self._prev_k > self._prev_d and k < d and max(self._prev_k, k) > 70.0 and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return auto_kd_strategy()
