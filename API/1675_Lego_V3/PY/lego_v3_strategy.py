import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class lego_v3_strategy(Strategy):
    def __init__(self):
        super(lego_v3_strategy, self).__init__()
        self._fast_ma = self.Param("FastMa", 8) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicators")
        self._slow_ma = self.Param("SlowMa", 21) \
            .SetDisplay("Slow MA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._has_prev = False

    @property
    def fast_ma(self):
        return self._fast_ma.Value

    @property
    def slow_ma(self):
        return self._slow_ma.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lego_v3_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(lego_v3_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_ma
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_ma
        atr = StandardDeviation()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, atr):
        if candle.State != CandleStates.Finished:
            return
        if atr <= 0:
            return
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        close = candle.ClosePrice
        # ATR stop check
        if self.Position > 0 and self._entry_price > 0 and close < self._entry_price - 2 * atr:
            self.SellMarket()
            self._entry_price = 0
        elif self.Position < 0 and self._entry_price > 0 and close > self._entry_price + 2 * atr:
            self.BuyMarket()
            self._entry_price = 0
        # MA crossover
        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return lego_v3_strategy()
