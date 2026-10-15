import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class color_metro_stochastic_strategy(Strategy):
    def __init__(self):
        super(color_metro_stochastic_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("K Period", "K calculation period", "Indicator")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "D smoothing period", "Indicator")
        self._slowing = self.Param("Slowing", 3) \
            .SetDisplay("Slowing", "Additional smoothing", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_k = None
        self._prev_d = None

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def slowing(self):
        return self._slowing.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_metro_stochastic_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(color_metro_stochastic_strategy, self).OnStarted2(time)
        self._prev_k = None
        self._prev_d = None

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

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent))

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
            if self._prev_k <= self._prev_d and k > d and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_k >= self._prev_d and k < d and self.Position >= 0:
                self.SellMarket()

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return color_metro_stochastic_strategy()
