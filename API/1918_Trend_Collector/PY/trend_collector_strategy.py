import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class trend_collector_strategy(Strategy):
    def __init__(self):
        super(trend_collector_strategy, self).__init__()
        self._fast_ma_length = self.Param("FastMaLength", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Length", "Fast EMA length", "Parameters")
        self._slow_ma_length = self.Param("SlowMaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA Length", "Slow EMA length", "Parameters")
        self._stochastic_upper = self.Param("StochasticUpper", 60.0) \
            .SetDisplay("Stochastic Upper", "Upper stochastic level", "Parameters")
        self._stochastic_lower = self.Param("StochasticLower", 40.0) \
            .SetDisplay("Stochastic Lower", "Lower stochastic level", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_ma = None
        self._slow_ma = None

    @property
    def fast_ma_length(self):
        return self._fast_ma_length.Value
    @property
    def slow_ma_length(self):
        return self._slow_ma_length.Value
    @property
    def stochastic_upper(self):
        return self._stochastic_upper.Value
    @property
    def stochastic_lower(self):
        return self._stochastic_lower.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_collector_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(trend_collector_strategy, self).OnStarted(time)
        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.fast_ma_length
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.slow_ma_length
        stochastic = StochasticOscillator()
        stochastic.K.Length = 14
        stochastic.D.Length = 3
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        fast_result = self._fast_ma.Process(candle.ClosePrice, candle.OpenTime, True)
        slow_result = self._slow_ma.Process(candle.ClosePrice, candle.OpenTime, True)
        if not fast_result.IsFormed or not slow_result.IsFormed or not stoch_value.IsFormed:
            return
        fast = float(fast_result)
        slow = float(slow_result)
        k_val = stoch_value.K
        if k_val is None:
            return
        stoch_k = float(k_val)
        # Buy: fast EMA above slow EMA, stochastic oversold
        if self.Position <= 0 and fast > slow and stoch_k < float(self.stochastic_lower):
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell: fast EMA below slow EMA, stochastic overbought
        elif self.Position >= 0 and fast < slow and stoch_k > float(self.stochastic_upper):
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return trend_collector_strategy()
