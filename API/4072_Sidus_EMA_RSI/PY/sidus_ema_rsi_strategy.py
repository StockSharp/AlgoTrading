import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex

class sidus_ema_rsi_strategy(Strategy):
    def __init__(self):
        super(sidus_ema_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 12) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 21) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    def OnStarted(self, time):
        super(sidus_ema_rsi_strategy, self).OnStarted(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastPeriod
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self._rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        rsi_val = float(rsi_value)

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        bullish_cross = self._prev_fast <= self._prev_slow and fast_val > slow_val
        bearish_cross = self._prev_fast >= self._prev_slow and fast_val < slow_val

        # Exit existing positions on opposite cross
        if self.Position > 0 and bearish_cross:
            self.SellMarket()
        elif self.Position < 0 and bullish_cross:
            self.BuyMarket()

        # Entry on crossover confirmed by RSI
        if self.Position == 0:
            if bullish_cross and rsi_val > 50:
                self.BuyMarket()
            elif bearish_cross and rsi_val < 50:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def OnReseted(self):
        super(sidus_ema_rsi_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def CreateClone(self):
        return sidus_ema_rsi_strategy()
