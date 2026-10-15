import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class hybrid_ea_strategy(Strategy):
    def __init__(self):
        super(hybrid_ea_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for stops", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hybrid_ea_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(hybrid_ea_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, rsi, atr):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        close = candle.ClosePrice
        # EMA cross up + RSI above 50 => long
        if self._prev_fast <= self._prev_slow and fast > slow and rsi > 50:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
        # EMA cross down + RSI below 50 => short
        elif self._prev_fast >= self._prev_slow and fast < slow and rsi < 50:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
        # Exit long
        elif self.Position > 0:
            if fast < slow or (atr > 0 and self._entry_price > 0 and close <= self._entry_price - atr * 2):
                self.SellMarket()
                self._entry_price = 0
        # Exit short
        elif self.Position < 0:
            if fast > slow or (atr > 0 and self._entry_price > 0 and close >= self._entry_price + atr * 2):
                self.BuyMarket()
                self._entry_price = 0
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return hybrid_ea_strategy()
