import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class center_of_gravity_mean_reversion_strategy(Strategy):
    """
    Center of Gravity regression channel mean reversion strategy.
    Approximates price with polynomial regression and builds a standard deviation envelope.
    Buys at lower band, sells at upper band.
    """

    def __init__(self):
        super(center_of_gravity_mean_reversion_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe used to build the regression channel", "General")
        self._bars_back = self.Param("BarsBack", 125) \
            .SetDisplay("Bars Back", "Number of historical bars used for regression", "Channel")
        self._polynomial_degree = self.Param("PolynomialDegree", 2) \
            .SetDisplay("Polynomial Degree", "Degree of polynomial regression", "Channel")
        self._std_multiplier = self.Param("StdMultiplier", 1.0) \
            .SetDisplay("Std Multiplier", "Multiplier applied to close price standard deviation", "Channel")
        self._stop_loss_distance = self.Param("StopLossDistance", 0.0) \
            .SetDisplay("Stop Loss Distance", "Optional stop loss distance in price units", "Risk")
        self._take_profit_distance = self.Param("TakeProfitDistance", 0.0) \
            .SetDisplay("Take Profit Distance", "Optional take profit distance in price units", "Risk")

        self._closes = []
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(center_of_gravity_mean_reversion_strategy, self).OnReseted()
        self._closes = []
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(center_of_gravity_mean_reversion_strategy, self).OnStarted2(time)
        self._closes = []
        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        bars_back = self._bars_back.Value
        max_count = bars_back + 1

        self._closes.append(close)
        while len(self._closes) > max_count:
            self._closes.pop(0)

        if len(self._closes) < max_count:
            return

        result = self._try_calculate_bands()
        if result is None:
            return

        center, upper, lower = result

        if self._check_long_exit(candle):
            return

        if self.Position > 0 and close >= upper:
            self.SellMarket()
            self._entry_price = 0.0
            return

        if self.Position < 0 and close <= lower:
            self.BuyMarket()
            self._entry_price = 0.0
            return

        if close <= lower and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
        elif close >= upper and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close

    def _try_calculate_bands(self):
        degree = self._polynomial_degree.Value
        count = len(self._closes)
        lookback = self._bars_back.Value
        data_length = lookback + 1

        if count < data_length or degree < 1:
            return None

        size = degree + 1
        sum_powers = [0.0] * (2 * degree + 1)

        for power in range(2 * degree + 1):
            s = 0.0
            for n in range(lookback + 1):
                s += n ** power
            sum_powers[power] = s

        matrix = [[0.0] * size for _ in range(size)]
        rhs = [0.0] * size

        for row in range(size):
            for col in range(size):
                matrix[row][col] = sum_powers[row + col]
            s = 0.0
            for n in range(lookback + 1):
                price = self._closes[count - 1 - n]
                s += price * (n ** row)
            rhs[row] = s

        result = self._solve_linear_system(matrix, rhs, size)
        if result is None:
            return None

        center_value = result[0]
        if center_value != center_value:  # NaN check
            return None

        total = 0.0
        for i in range(count - data_length, count):
            total += self._closes[i]
        mean = total / data_length

        variance = 0.0
        for i in range(count - data_length, count):
            diff = self._closes[i] - mean
            variance += diff * diff
        variance /= data_length

        std = Math.Sqrt(max(variance, 0)) * self._std_multiplier.Value

        center = center_value
        upper = center + std
        lower = center - std
        return (center, upper, lower)

    def _solve_linear_system(self, matrix, rhs, size):
        for k in range(size):
            pivot_row = k
            pivot_value = abs(matrix[k][k])

            for i in range(k + 1, size):
                value = abs(matrix[i][k])
                if value > pivot_value:
                    pivot_value = value
                    pivot_row = i

            if pivot_value < 1e-10:
                return None

            if pivot_row != k:
                matrix[k], matrix[pivot_row] = matrix[pivot_row], matrix[k]
                rhs[k], rhs[pivot_row] = rhs[pivot_row], rhs[k]

            pivot = matrix[k][k]
            if abs(pivot) < 1e-10:
                return None

            for col in range(k, size):
                matrix[k][col] /= pivot
            rhs[k] /= pivot

            for row in range(size):
                if row == k:
                    continue
                factor = matrix[row][k]
                if abs(factor) < 1e-12:
                    continue
                for col in range(k, size):
                    matrix[row][col] -= factor * matrix[k][col]
                rhs[row] -= factor * rhs[k]

        return rhs

    def _check_long_exit(self, candle):
        if self.Position > 0 and self._entry_price > 0:
            stop_loss = self._stop_loss_distance.Value
            take_profit = self._take_profit_distance.Value

            if stop_loss > 0 and float(candle.LowPrice) <= self._entry_price - stop_loss:
                self.SellMarket()
                self._entry_price = 0.0
                return True

            if take_profit > 0 and float(candle.HighPrice) >= self._entry_price + take_profit:
                self.SellMarket()
                self._entry_price = 0.0
                return True
        elif self.Position <= 0:
            self._entry_price = 0.0

        return False

    def CreateClone(self):
        return center_of_gravity_mean_reversion_strategy()
