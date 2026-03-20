import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class robot_danu_strategy(Strategy):
    def __init__(self):
        super(robot_danu_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 28) \
            .SetDisplay("Fast ZigZag Length", "Lookback for fast ZigZag", "ZigZag")
        self._slow_length = self.Param("SlowLength", 56) \
            .SetDisplay("Slow ZigZag Length", "Lookback for slow ZigZag", "ZigZag")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_fast = 0.0
        self._last_fast_high = 0.0
        self._last_fast_low = 0.0
        self._fast_direction = 0
        self._last_slow = 0.0
        self._last_slow_high = 0.0
        self._last_slow_low = 0.0
        self._slow_direction = 0

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
        super(robot_danu_strategy, self).OnReseted()
        self._last_fast = 0.0
        self._last_fast_high = 0.0
        self._last_fast_low = 0.0
        self._fast_direction = 0
        self._last_slow = 0.0
        self._last_slow_high = 0.0
        self._last_slow_low = 0.0
        self._slow_direction = 0

    def OnStarted(self, time):
        super(robot_danu_strategy, self).OnStarted(time)
        fast_high = Highest()
        fast_high.Length = self.fast_length
        fast_low = Lowest()
        fast_low.Length = self.fast_length
        slow_high = Highest()
        slow_high.Length = self.slow_length
        slow_low = Lowest()
        slow_low.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_high, fast_low, slow_high, slow_low, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_high, fast_low, slow_high, slow_low):
        if candle.State != CandleStates.Finished:
            return
        # Update fast ZigZag pivot
        if candle.HighPrice >= fast_high and self._fast_direction != 1:
            self._last_fast = candle.HighPrice
            self._last_fast_high = candle.HighPrice
            self._fast_direction = 1
        elif candle.LowPrice <= fast_low and self._fast_direction != -1:
            self._last_fast = candle.LowPrice
            self._last_fast_low = candle.LowPrice
            self._fast_direction = -1
        # Update slow ZigZag pivot
        if candle.HighPrice >= slow_high and self._slow_direction != 1:
            self._last_slow = candle.HighPrice
            self._last_slow_high = candle.HighPrice
            self._slow_direction = 1
        elif candle.LowPrice <= slow_low and self._slow_direction != -1:
            self._last_slow = candle.LowPrice
            self._last_slow_low = candle.LowPrice
            self._slow_direction = -1
        # Trading logic: compare fast and slow pivots
        if self._last_fast > self._last_slow and self.Position >= 0:
            { if (self.Position > 0) SellMarket(); SellMarket(); }
        elif self._last_fast < self._last_slow and self.Position <= 0:
            { if (self.Position < 0) BuyMarket(); BuyMarket(); }

    def CreateClone(self):
        return robot_danu_strategy()
