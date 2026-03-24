import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class lego4_beta_strategy(Strategy):
    def __init__(self):
        super(lego4_beta_strategy, self).__init__()
        self._fast_ma_length = self.Param("FastMaLength", 5) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 20) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fast_ma_length(self):
        return self._fast_ma_length.Value

    @property
    def slow_ma_length(self):
        return self._slow_ma_length.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lego4_beta_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(lego4_beta_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_ma_length
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_ma_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, rsi):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        # EMA cross up + RSI not overbought => long
        if self._prev_fast <= self._prev_slow and fast > slow and rsi < 70:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # EMA cross down + RSI not oversold => short
        elif self._prev_fast >= self._prev_slow and fast < slow and rsi > 30:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        # RSI exit: overbought close long
        elif self.Position > 0 and rsi > 75:
            self.SellMarket()
        # RSI exit: oversold close short
        elif self.Position < 0 and rsi < 25:
            self.BuyMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return lego4_beta_strategy()
