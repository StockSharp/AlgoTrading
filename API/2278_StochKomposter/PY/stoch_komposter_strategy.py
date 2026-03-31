import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class stoch_komposter_strategy(Strategy):
    def __init__(self):
        super(stoch_komposter_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("K Period", "Stochastic %K calculation period", "Indicators")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Stochastic %D smoothing period", "Indicators")
        self._up_level = self.Param("UpLevel", 70.0) \
            .SetDisplay("Upper Level", "Overbought threshold", "Indicators")
        self._down_level = self.Param("DownLevel", 30.0) \
            .SetDisplay("Lower Level", "Oversold threshold", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._prev_k = None

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def up_level(self):
        return self._up_level.Value

    @property
    def down_level(self):
        return self._down_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stoch_komposter_strategy, self).OnReseted()
        self._prev_k = None

    def OnStarted2(self, time):
        super(stoch_komposter_strategy, self).OnStarted2(time)
        self._prev_k = None
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.k_period
        stochastic.D.Length = self.d_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        if not stoch_value.IsFormed:
            return
        k = stoch_value.K
        if k is None:
            return
        k = float(k)
        if self._prev_k is None:
            self._prev_k = k
            return
        down_lvl = float(self.down_level)
        up_lvl = float(self.up_level)
        prev = self._prev_k
        if prev <= down_lvl and k > down_lvl and self.Position <= 0:
            self.BuyMarket()
        elif prev >= up_lvl and k < up_lvl and self.Position >= 0:
            self.SellMarket()
        self._prev_k = k

    def CreateClone(self):
        return stoch_komposter_strategy()
