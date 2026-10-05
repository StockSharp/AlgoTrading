import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from collections import deque
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class costar_strategy(Strategy):
    """
    COSTAR strategy.
    A least-squares line is fitted to the last Length closes and the standard deviation of its residuals, times Multiplier,
    builds bands around the line's current value. A close crossing back above the lower band buys and a close crossing back
    below the upper band sells short, reversing an opposite position. A long closes when the close crosses above the
    regression line and a short when it crosses below it.
    """

    def __init__(self):
        super(costar_strategy, self).__init__()
        self._length = self.Param("Length", 100).SetRange(2, 10000).SetDisplay("Length", "Regression length", "Indicators")
        self._multiplier = self.Param("Multiplier", 1.0).SetGreaterThanZero().SetDisplay("Multiplier", "Residual deviation multiplier for the bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = deque()
        self._prev_close = None
        self._prev_line = None
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(costar_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(costar_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        length = self._length.Value

        self._closes.append(close)
        while len(self._closes) > length:
            self._closes.popleft()

        if len(self._closes) < length:
            self._prev_close = close
            return

        line, deviation = self._calculate_regression(list(self._closes))
        multiplier = float(self._multiplier.Value)
        upper = line + deviation * multiplier
        lower = line - deviation * multiplier

        pc = self._prev_close
        pl = self._prev_line
        pu = self._prev_upper
        plo = self._prev_lower

        self._prev_close = close
        self._prev_line = line
        self._prev_upper = upper
        self._prev_lower = lower

        if pc is None or pl is None or pu is None or plo is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_above_lower = pc <= plo and close > lower
        cross_below_upper = pc >= pu and close < upper

        if cross_above_lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_below_upper and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and pc <= pl and close > line:
            self.SellMarket(self.Position)
        elif self.Position < 0 and pc >= pl and close < line:
            self.BuyMarket(-self.Position)

    @staticmethod
    def _calculate_regression(values):
        n = len(values)
        sum_x = 0.0
        sum_y = 0.0
        sum_xy = 0.0
        sum_xx = 0.0
        for i, y in enumerate(values):
            sum_x += i
            sum_y += y
            sum_xy += i * y
            sum_xx += i * i

        denominator = n * sum_xx - sum_x * sum_x
        slope = 0.0 if denominator == 0 else (n * sum_xy - sum_x * sum_y) / denominator
        intercept = (sum_y - slope * sum_x) / n

        sum_squares = 0.0
        for i, y in enumerate(values):
            residual = y - (intercept + slope * i)
            sum_squares += residual * residual

        line = intercept + slope * (n - 1)
        return line, math.sqrt(sum_squares / n)

    def CreateClone(self):
        return costar_strategy()
