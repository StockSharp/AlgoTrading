import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from collections import deque

class e_regression_channel_strategy(Strategy):
    """
    Polynomial regression channel strategy. Calculates a regression midline with
    standard deviation bands and trades mean reversion between the bands and midline.
    """

    def __init__(self):
        super(e_regression_channel_strategy, self).__init__()
        self._regression_length = self.Param("RegressionLength", 100) \
            .SetDisplay("Regression Length", "Number of bars used for regression", "Regression")
        self._degree = self.Param("Degree", 3) \
            .SetDisplay("Degree", "Polynomial degree for the regression", "Regression")
        self._std_multiplier = self.Param("StdDevMultiplier", 1.0) \
            .SetDisplay("Std Dev Multiplier", "Width multiplier for the regression bands", "Regression")
        self._stop_loss_points = self.Param("StopLossPoints", 500.0) \
            .SetDisplay("Stop Loss", "Protective stop in absolute points (0 disables)", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0) \
            .SetDisplay("Take Profit", "Target in absolute points (0 disables)", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary candle type used for trading", "General")

        self._closes = deque()
        self._previous_mid = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(e_regression_channel_strategy, self).OnReseted()
        self._closes.clear()
        self._previous_mid = None

    def OnStarted2(self, time):
        super(e_regression_channel_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        length = self._regression_length.Value
        ema.Length = max(2, length)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

        tp = self._take_profit_points.Value
        sl = self._stop_loss_points.Value
        tp_unit = Unit(float(tp), UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(float(sl), UnitTypes.Absolute) if sl > 0 else None
        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

    def _process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        reg_len = self._regression_length.Value

        self._closes.append(close)
        if len(self._closes) > reg_len:
            self._closes.popleft()

        if len(self._closes) < reg_len:
            return

        prices = list(self._closes)
        coeffs = self._poly_fit(prices, self._degree.Value)
        current_index = len(prices) - 1
        mid = self._poly_eval(coeffs, current_index)
        std = self._calc_std(prices, coeffs) * self._std_multiplier.Value
        upper = mid + std
        lower = mid - std

        if self.Position > 0 and close >= mid:
            self.SellMarket()
            self._previous_mid = mid
            return
        if self.Position < 0 and close <= mid:
            self.BuyMarket()
            self._previous_mid = mid
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_mid = mid
            return

        if float(candle.LowPrice) <= lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif float(candle.HighPrice) >= upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._previous_mid = mid

    @staticmethod
    def _poly_fit(y, degree):
        n = len(y)
        actual_degree = min(degree, max(1, n - 1))
        size = actual_degree + 1
        matrix = [[0.0] * (size + 1) for _ in range(size)]

        for row in range(size):
            for col in range(size):
                s = 0.0
                for i in range(n):
                    s += i ** (row + col)
                matrix[row][col] = s

            s_y = 0.0
            for i in range(n):
                s_y += y[i] * (i ** row)
            matrix[row][size] = s_y

        for i in range(size):
            pivot = matrix[i][i]
            if pivot == 0.0:
                for k in range(i + 1, size):
                    if matrix[k][i] != 0.0:
                        matrix[i], matrix[k] = matrix[k], matrix[i]
                        pivot = matrix[i][i]
                        break
            if pivot == 0.0:
                continue
            for j in range(i, size + 1):
                matrix[i][j] /= pivot
            for row in range(size):
                if row == i:
                    continue
                factor = matrix[row][i]
                if factor == 0.0:
                    continue
                for col in range(i, size + 1):
                    matrix[row][col] -= factor * matrix[i][col]

        return [matrix[i][size] for i in range(size)]

    @staticmethod
    def _poly_eval(coeffs, x):
        result = 0.0
        power = 1.0
        for c in coeffs:
            result += c * power
            power *= x
        return result

    @staticmethod
    def _calc_std(values, coeffs):
        n = len(values)
        if n == 0:
            return 0.0
        s = 0.0
        for i in range(n):
            fitted = e_regression_channel_strategy._poly_eval(coeffs, i)
            diff = values[i] - fitted
            s += diff * diff
        return math.sqrt(s / n)

    def CreateClone(self):
        return e_regression_channel_strategy()
