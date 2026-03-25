import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class directed_movement_strategy(Strategy):
    def __init__(self):
        super(directed_movement_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._fast_ma_length = self.Param("FastMaLength", 12) \
            .SetDisplay("Fast MA Length", "Period of fast moving average", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 5) \
            .SetDisplay("Slow MA Length", "Period of slow moving average", "Indicators")
        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def fast_ma_length(self):
        return self._fast_ma_length.Value

    @property
    def slow_ma_length(self):
        return self._slow_ma_length.Value

    def OnReseted(self):
        super(directed_movement_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(directed_movement_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.fast_ma_length
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.slow_ma_length
        self.Indicators.Add(self._fast_ma)
        self.Indicators.Add(self._slow_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        t = candle.ServerTime
        input_fast = DecimalIndicatorValue(self._fast_ma, rsi_value, t)
        input_fast.IsFinal = True
        fast_result = self._fast_ma.Process(input_fast)
        if not self._fast_ma.IsFormed:
            return
        fast = float(fast_result)
        input_slow = DecimalIndicatorValue(self._slow_ma, fast, t)
        input_slow.IsFinal = True
        slow_result = self._slow_ma.Process(input_slow)
        if not self._slow_ma.IsFormed:
            self._prev_fast = fast
            self._prev_slow = fast
            return
        slow = float(slow_result)
        if self._prev_fast > self._prev_slow and fast <= slow and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast < self._prev_slow and fast >= slow and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return directed_movement_strategy()
