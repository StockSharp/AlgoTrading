import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from datatype_extensions import *


class three_ema_cross_strategy(Strategy):
    """Three EMA Cross strategy.

    Uses a fast and slow EMA to generate entry signals while a longer trend EMA acts
    as a filter. A long position is opened after a recent bullish crossover when
    price pulls back to the fast average and stays above the trend average. The
    position is closed when the fast EMA drops back under the slow EMA.
    """

    def __init__(self):
        super(three_ema_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_length = self.Param("FastEmaLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Moving Averages")
        self._slow_length = self.Param("SlowEmaLength", 20) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Moving Averages")
        self._trend_length = self.Param("TrendEmaLength", 100) \
            .SetDisplay("Trend EMA", "Trend EMA length", "Moving Averages")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._cross_back_bars = self.Param("CrossBackBars", 10) \
            .SetDisplay("Cross Back Bars", "Check cross in the last X candles", "Strategy")

        self._fast_ema = None
        self._slow_ema = None
        self._trend_ema = None

        self._previous_fast = 0.0
        self._previous_slow = 0.0
        self._crossover_occurred = False
        self._bars_since_cross = 0
        self._entry_price = 0.0
        self._is_long = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def fast_ema_length(self):
        return self._fast_length.Value

    @fast_ema_length.setter
    def fast_ema_length(self, value):
        self._fast_length.Value = value

    @property
    def slow_ema_length(self):
        return self._slow_length.Value

    @slow_ema_length.setter
    def slow_ema_length(self, value):
        self._slow_length.Value = value

    @property
    def trend_ema_length(self):
        return self._trend_length.Value

    @trend_ema_length.setter
    def trend_ema_length(self, value):
        self._trend_length.Value = value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def cross_back_bars(self):
        return self._cross_back_bars.Value

    @cross_back_bars.setter
    def cross_back_bars(self, value):
        self._cross_back_bars.Value = value

    def OnReseted(self):
        super(three_ema_cross_strategy, self).OnReseted()
        self._previous_fast = 0.0
        self._previous_slow = 0.0
        self._crossover_occurred = False
        self._bars_since_cross = 0
        self._entry_price = 0.0
        self._is_long = False

    def OnStarted(self, time):
        super(three_ema_cross_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_ema_length
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_ema_length
        self._trend_ema = ExponentialMovingAverage()
        self._trend_ema.Length = self.trend_ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._trend_ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawIndicator(area, self._trend_ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_value, slow_value, trend_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._trend_ema.IsFormed:
            return

        current_price = candle.ClosePrice
        low_price = candle.LowPrice

        if self._previous_fast and self._previous_slow:
            crossed = self._previous_fast <= self._previous_slow and fast_value > slow_value
            if crossed:
                self._crossover_occurred = True
                self._bars_since_cross = 0

        if self._crossover_occurred:
            self._bars_since_cross += 1
            if self._bars_since_cross > self.cross_back_bars:
                self._crossover_occurred = False

        if (self._crossover_occurred and current_price >= fast_value and
                low_price <= fast_value and trend_value <= current_price and self.Position == 0):
            self._entry_price = float(current_price)
            self._is_long = True
            volume = self.Volume
            self.BuyMarket(volume)

        if (self.Position > 0 and self._previous_fast > self._previous_slow and fast_value < slow_value):
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = 0.0

        if self.Position > 0 and self._entry_price:
            self._check_stop_loss(current_price)

        self._previous_fast = fast_value
        self._previous_slow = slow_value

    def _check_stop_loss(self, current_price):
        threshold = self.stop_loss_percent / 100.0
        stop_price = self._entry_price * (1.0 - threshold)
        if current_price <= stop_price:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(f"Stop loss triggered at {current_price}")
            self._entry_price = 0.0

    def CreateClone(self):
        return three_ema_cross_strategy()
