import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class stochastic_histogram_strategy(Strategy):
    def __init__(self):
        super(stochastic_histogram_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("K Period", "Stochastic %K period", "Stochastic")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Stochastic %D period", "Stochastic")
        self._high_level = self.Param("HighLevel", 80.0) \
            .SetDisplay("High Level", "Upper threshold", "Stochastic")
        self._low_level = self.Param("LowLevel", 20.0) \
            .SetDisplay("Low Level", "Lower threshold", "Stochastic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for calculation", "General")
        self._prev_k = None
        self._prev_d = None

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_histogram_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(stochastic_histogram_strategy, self).OnStarted2(time)
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

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        if not stoch_value.IsFormed:
            return
        k = stoch_value.K
        d = stoch_value.D
        if k is None or d is None:
            return
        k = float(k)
        d = float(d)
        if self._prev_k is not None and self._prev_d is not None:
            pk = self._prev_k
            pd = self._prev_d
            if pk <= pd and k > d and k < float(self.low_level) and self.Position <= 0:
                self.BuyMarket()
            elif pk >= pd and k < d and k > float(self.high_level) and self.Position >= 0:
                self.SellMarket()
        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return stochastic_histogram_strategy()
