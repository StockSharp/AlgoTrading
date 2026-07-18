import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class _RegressionChannelState(object):
    """Internal regression channel calculator."""

    def __init__(self, length, degree, multiplier, shift):
        self._length = length
        self._degree = max(1, min(3, degree))
        self._multiplier = multiplier
        self._shift = shift
        self._closes = []
        self._upper_history = []
        self.Upper = None
        self.Middle = None
        self.Lower = None
        self.Slope = None
        self.High = None
        self.Low = None
        self.IsReady = False

    def process(self, candle):
        self.High = float(candle.HighPrice)
        self.Low = float(candle.LowPrice)

        self._closes.append(float(candle.ClosePrice))
        if len(self._closes) > self._length:
            self._closes.pop(0)

        if len(self._closes) < self._length:
            self.IsReady = False
            self.Upper = None
            self.Middle = None
            self.Lower = None
            self.Slope = None
            return

        values = self._closes[:]
        coeffs = self._poly_fit(values, self._degree)

        index = len(values) - 1 - min(self._shift, len(values) - 1)
        mid = self._poly_eval(coeffs, index)

        sum_sq = 0.0
        for i in range(len(values)):
            estimate = self._poly_eval(coeffs, i)
            diff = values[i] - estimate
            sum_sq += diff * diff

        std = math.sqrt(sum_sq / len(values))
        upper = mid + std * self._multiplier
        lower = mid - std * self._multiplier

        self._upper_history.append(upper)
        if len(self._upper_history) > self._length + 1:
            self._upper_history.pop(0)

        slope = None
        if len(self._upper_history) > self._length:
            slope = upper - self._upper_history[0]

        self.Upper = upper
        self.Middle = mid
        self.Lower = lower
        self.Slope = slope
        self.IsReady = True

    def _poly_fit(self, values, degree):
        n = len(values)
        order = min(degree, n - 1)
        size = order + 1
        # Build augmented matrix
        matrix = [[0.0] * (size + 1) for _ in range(size)]

        for row in range(size):
            for col in range(size):
                s = 0.0
                for i in range(n):
                    s += i ** (row + col)
                matrix[row][col] = s

            sy = 0.0
            for i in range(n):
                sy += values[i] * (i ** row)
            matrix[row][size] = sy

        # Gauss-Jordan elimination
        for i in range(size):
            if matrix[i][i] == 0:
                swap_row = i + 1
                while swap_row < size and matrix[swap_row][i] == 0:
                    swap_row += 1
                if swap_row < size:
                    matrix[i], matrix[swap_row] = matrix[swap_row], matrix[i]

            pivot = matrix[i][i]
            if pivot == 0:
                continue

            for j in range(i, size + 1):
                matrix[i][j] /= pivot

            for k in range(size):
                if k == i:
                    continue
                factor = matrix[k][i]
                if factor == 0:
                    continue
                for j in range(i, size + 1):
                    matrix[k][j] -= factor * matrix[i][j]

        return [matrix[i][size] for i in range(size)]

    def _poly_eval(self, coeffs, x):
        y = 0.0
        power = 1.0
        for c in coeffs:
            y += c * power
            power *= x
        return y


class multi_time_frame_trader_strategy(Strategy):
    """Multi time frame regression channel strategy with 3 timeframes."""

    def __init__(self):
        super(multi_time_frame_trader_strategy, self).__init__()

        self._degree = self.Param("Degree", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Polynomial Degree", "Degree for regression channel", "Regression")
        self._std_multiplier = self.Param("StdMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Std Multiplier", "Standard deviation multiplier", "Regression")
        self._bars = self.Param("Bars", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Regression Bars", "Bars for regression and slope", "Regression")
        self._shift = self.Param("Shift", 0) \
            .SetDisplay("Shift", "Bars to shift regression evaluation", "Regression")
        self._use_trading = self.Param("UseTrading", True) \
            .SetDisplay("Use Trading", "Enable order execution", "Trading")

        self._m1_type = DataType.TimeFrame(TimeSpan.FromMinutes(5))
        self._m5_type = DataType.TimeFrame(TimeSpan.FromHours(1))
        self._h1_type = DataType.TimeFrame(TimeSpan.FromHours(4))

        self._m1_state = None
        self._m5_state = None
        self._h1_state = None
        self._position_side = None  # 'buy' or 'sell'
        self._stop_price = None
        self._target_price = None

    @property
    def Degree(self):
        return int(self._degree.Value)
    @property
    def StdMultiplier(self):
        return float(self._std_multiplier.Value)
    @property
    def Bars(self):
        return int(self._bars.Value)
    @property
    def Shift(self):
        return int(self._shift.Value)
    @property
    def UseTrading(self):
        return self._use_trading.Value

    def OnStarted2(self, time):
        super(multi_time_frame_trader_strategy, self).OnStarted2(time)

        degree = max(1, min(3, self.Degree))
        bars = max(1, self.Bars)
        shift = max(0, min(self.Shift, bars - 1))
        multiplier = max(0.1, self.StdMultiplier)

        self._m1_state = _RegressionChannelState(bars, degree, multiplier, shift)
        self._m5_state = _RegressionChannelState(bars, degree, multiplier, shift)
        self._h1_state = _RegressionChannelState(bars, degree, multiplier, shift)
        self._position_side = None
        self._stop_price = None
        self._target_price = None

        m1_sub = self.SubscribeCandles(self._m1_type)
        m1_sub.Bind(self._process_m1).Start()

        m5_sub = self.SubscribeCandles(self._m5_type)
        m5_sub.Bind(self._process_m5).Start()

        h1_sub = self.SubscribeCandles(self._h1_type)
        h1_sub.Bind(self._process_h1).Start()

    def _process_m1(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._m1_state.process(candle)
        self._try_manage_position(candle)

        if not self.UseTrading:
            return

        if self._m1_state is None or self._m5_state is None or self._h1_state is None:
            return
        if not self._m1_state.IsReady or not self._m5_state.IsReady or not self._h1_state.IsReady:
            return

        slope_h1 = self._h1_state.Slope
        if slope_h1 is None:
            return

        m5_upper = self._m5_state.Upper
        m5_middle = self._m5_state.Middle
        m5_lower = self._m5_state.Lower
        m1_upper = self._m1_state.Upper
        m1_lower = self._m1_state.Lower
        if m5_upper is None or m5_middle is None or m5_lower is None or m1_upper is None or m1_lower is None:
            return

        m5_high = self._m5_state.High
        m5_low = self._m5_state.Low
        m1_high = self._m1_state.High
        m1_low = self._m1_state.Low
        if m5_high is None or m5_low is None or m1_high is None or m1_low is None:
            return

        close = float(candle.ClosePrice)

        # Short setup
        if slope_h1 < 0 and self.Position >= 0:
            if m5_high >= m5_upper and m1_high >= m1_upper:
                half_width = abs(m5_upper - m5_middle) / 2.0
                stop = close + half_width
                target = m5_middle
                self._enter_short(stop, target)
                return

        # Long setup
        if slope_h1 > 0 and self.Position <= 0:
            if m5_low <= m5_lower and m1_low <= m1_lower:
                half_width = abs(m5_middle - m5_lower) / 2.0
                stop = close - half_width
                target = m5_middle
                self._enter_long(stop, target)

    def _process_m5(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._m5_state.process(candle)

    def _process_h1(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._h1_state.process(candle)

    def _try_manage_position(self, candle):
        if not self.UseTrading or self._position_side is None:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self._position_side == 'buy':
            if self._stop_price is not None and lo <= self._stop_price:
                self._exit_long()
                return
            if self._target_price is not None and h >= self._target_price:
                self._exit_long()
        elif self._position_side == 'sell':
            if self._stop_price is not None and h >= self._stop_price:
                self._exit_short()
                return
            if self._target_price is not None and lo <= self._target_price:
                self._exit_short()

    def _enter_long(self, stop, target):
        self.BuyMarket()
        self._position_side = 'buy'
        self._stop_price = stop
        self._target_price = target

    def _enter_short(self, stop, target):
        self.SellMarket()
        self._position_side = 'sell'
        self._stop_price = stop
        self._target_price = target

    def _exit_long(self):
        self.SellMarket()
        self._position_side = None
        self._stop_price = None
        self._target_price = None

    def _exit_short(self):
        self.BuyMarket()
        self._position_side = None
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(multi_time_frame_trader_strategy, self).OnReseted()
        self._position_side = None
        self._stop_price = None
        self._target_price = None
        self._m1_state = None
        self._m5_state = None
        self._h1_state = None

    def CreateClone(self):
        return multi_time_frame_trader_strategy()
