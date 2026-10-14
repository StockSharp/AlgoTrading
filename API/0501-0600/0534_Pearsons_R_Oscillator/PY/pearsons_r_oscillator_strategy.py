import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class pearsons_r_oscillator_strategy(Strategy):
    """
    Pearson's R oscillator strategy.
    On every candle the closes of the last MinPeriod, MinPeriod + Step, ... MaxPeriod bars are fitted with a linear regression
    and the period with the strongest Pearson correlation is chosen. When that correlation is at least IdealPositive or at most
    IdealNegative, a channel is drawn around the regression line at Deviations standard deviations of the residuals. A close
    crossing above the upper line goes long and a close crossing below the lower line goes short, reversing an opposite
    position. A long closes when the close crosses below the midline and a short when it crosses above it.
    """

    def __init__(self):
        super(pearsons_r_oscillator_strategy, self).__init__()
        self._min_period = self.Param("MinPeriod", 48) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Period", "Shortest regression period", "Regression")
        self._max_period = self.Param("MaxPeriod", 360) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Period", "Longest regression period", "Regression")
        self._step = self.Param("Step", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Step", "Increment between tested periods", "Regression")
        self._ideal_positive = self.Param("IdealPositive", 0.85) \
            .SetDisplay("Ideal Positive", "Correlation that confirms an upward channel", "Regression")
        self._ideal_negative = self.Param("IdealNegative", -0.85) \
            .SetDisplay("Ideal Negative", "Correlation that confirms a downward channel", "Regression")
        self._deviations = self.Param("Deviations", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviations", "Channel width in standard deviations", "Regression")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        # Prefix sums of the closes (shifted by the first close), their squares and index-weighted values.
        self._sum_y = []
        self._sum_yy = []
        self._sum_iy = []
        self._origin = None
        self._prev_close = None
        self._prev_channel = None
        self._last_channel = None

    def OnReseted(self):
        super(pearsons_r_oscillator_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(pearsons_r_oscillator_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        if self._origin is None:
            self._origin = close
        y = close - self._origin
        index = len(self._sum_y)

        self._sum_y.append((self._sum_y[index - 1] if index > 0 else 0.0) + y)
        self._sum_yy.append((self._sum_yy[index - 1] if index > 0 else 0.0) + y * y)
        self._sum_iy.append((self._sum_iy[index - 1] if index > 0 else 0.0) + index * y)

        channel = self._find_channel(index)

        prev_close = self._prev_close
        prev_channel = self._prev_channel
        self._prev_close = close
        self._prev_channel = channel if channel is not None else self._last_channel

        if channel is not None:
            self._last_channel = channel

        if prev_close is None or prev_channel is None or self._last_channel is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        prev_upper, prev_mid, prev_lower = prev_channel
        upper, mid, lower = self._last_channel

        if channel is not None and prev_close <= prev_upper and close > upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif channel is not None and prev_close >= prev_lower and close < lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and prev_close >= prev_mid and close < mid:
            self.SellMarket(self.Position)
        elif self.Position < 0 and prev_close <= prev_mid and close > mid:
            self.BuyMarket(-self.Position)

    def _find_channel(self, last):
        best_r = 0.0
        best_period = 0
        best_slope = 0.0
        best_intercept = 0.0
        best_std = 0.0

        period = self._min_period.Value
        max_period = self._max_period.Value
        step = self._step.Value

        while period <= max_period:
            if period > last + 1:
                break

            start = last - period + 1
            n = float(period)
            sy = self._sum_y[last] - (self._sum_y[start - 1] if start > 0 else 0.0)
            syy = self._sum_yy[last] - (self._sum_yy[start - 1] if start > 0 else 0.0)
            # Local x runs from 0 to period - 1.
            sxy = self._sum_iy[last] - (self._sum_iy[start - 1] if start > 0 else 0.0) - start * sy
            sx = n * (n - 1) / 2
            sxx = (n - 1) * n * (2 * n - 1) / 6

            cov_xy = sxy - sx * sy / n
            var_x = sxx - sx * sx / n
            var_y = syy - sy * sy / n

            if var_x > 0 and var_y > 0:
                r = cov_xy / math.sqrt(var_x * var_y)
                if abs(r) > abs(best_r):
                    slope = cov_xy / var_x
                    best_r = r
                    best_period = period
                    best_slope = slope
                    best_intercept = (sy - slope * sx) / n
                    best_std = math.sqrt(max(0.0, var_y - slope * cov_xy) / n)

            period += step

        if best_period == 0 or (best_r < float(self._ideal_positive.Value) and best_r > float(self._ideal_negative.Value)):
            return None

        mid = best_intercept + best_slope * (best_period - 1) + self._origin
        width = float(self._deviations.Value) * best_std
        return (mid + width, mid, mid - width)

    def CreateClone(self):
        return pearsons_r_oscillator_strategy()
