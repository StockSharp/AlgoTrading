import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class stalin_strategy(Strategy):
    def __init__(self):
        super(stalin_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 14) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        self._rsi_length = self.Param("RsiLength", 17) \
            .SetDisplay("RSI Length", "RSI period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stalin_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(stalin_strategy, self).OnStarted(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_length
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        rsi_value = float(rsi_value)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        buy_signal = self._prev_fast <= self._prev_slow and fast > slow and rsi_value > 50.0
        sell_signal = self._prev_fast >= self._prev_slow and fast < slow and rsi_value < 50.0
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return stalin_strategy()
