import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bitcoin_exponential_profit_strategy(Strategy):
    def __init__(self):
        super(bitcoin_exponential_profit_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 120) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 450) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def fast_length(self):
        return self._fast_length.Value
    @property
    def slow_length(self):
        return self._slow_length.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bitcoin_exponential_profit_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted2(self, time):
        super(bitcoin_exponential_profit_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = float(fast_value)
            self._prev_slow = float(slow_value)
            return
        if self._prev_fast <= self._prev_slow and fast_value > slow_value and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_value < slow_value and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = float(fast_value)
        self._prev_slow = float(slow_value)

    def CreateClone(self):
        return bitcoin_exponential_profit_strategy()
