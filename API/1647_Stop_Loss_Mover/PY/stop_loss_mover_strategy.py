import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class stop_loss_mover_strategy(Strategy):
    def __init__(self):
        super(stop_loss_mover_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._stop_mult = self.Param("StopMult", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Stop Mult", "StdDev multiplier for initial stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._is_stop_moved = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def stop_mult(self):
        return self._stop_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stop_loss_mover_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._is_stop_moved = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(stop_loss_mover_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_length
        std_dev = StandardDeviation()
        std_dev.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, std_val):
        if candle.State != CandleStates.Finished:
            return
        if std_val <= 0:
            return
        close = candle.ClosePrice
        # Check stop-loss hit
        if self.Position > 0 and self._stop_price > 0 and close <= self._stop_price:
            self.SellMarket()
            self._entry_price = 0
            self._stop_price = 0
            self._is_stop_moved = False
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        elif self.Position < 0 and self._stop_price > 0 and close >= self._stop_price:
            self.BuyMarket()
            self._entry_price = 0
            self._stop_price = 0
            self._is_stop_moved = False
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        # Move stop to break-even when price moves favorably by 2*stdDev
        if self.Position > 0 and not self._is_stop_moved and self._entry_price > 0:
            if close >= self._entry_price + 2 * std_val:
                self._stop_price = self._entry_price
                self._is_stop_moved = True
        elif self.Position < 0 and not self._is_stop_moved and self._entry_price > 0:
            if close <= self._entry_price - 2 * std_val:
                self._stop_price = self._entry_price
                self._is_stop_moved = True
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        # Entry signals: EMA crossover
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - self.stop_mult * std_val
            self._is_stop_moved = False
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + self.stop_mult * std_val
            self._is_stop_moved = False
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return stop_loss_mover_strategy()
