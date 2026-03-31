import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    ExponentialMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
)
from StockSharp.Algo.Strategies import Strategy
from collections import deque


class smoothing_average_crossover_strategy(Strategy):
    def __init__(self):
        super(smoothing_average_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._ma_length = self.Param("MaLength", 60) \
            .SetDisplay("MA Length", "Period of the smoothing average", "Moving Average")
        self._ma_shift = self.Param("MaShift", 3) \
            .SetDisplay("MA Shift", "Horizontal shift applied to the average", "Moving Average")
        self._entry_delta_pips = self.Param("EntryDeltaPips", 60.0) \
            .SetDisplay("Entry Delta pips", "Distance from MA to trigger entries", "Trading Rules")
        self._close_delta_coefficient = self.Param("CloseDeltaCoefficient", 1.0) \
            .SetDisplay("Close Delta Coefficient", "Multiplier applied to entry delta for exits", "Trading Rules")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Reverse Signals", "Invert long and short logic", "Trading Rules")
        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetDisplay("Trade Volume", "Order volume for each entry", "Risk")

        self._ma_shift_buffer = deque()
        self._entry_delta = 0.0
        self._close_delta = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def ma_shift(self):
        return self._ma_shift.Value

    @property
    def entry_delta_pips(self):
        return self._entry_delta_pips.Value

    @property
    def close_delta_coefficient(self):
        return self._close_delta_coefficient.Value

    @property
    def reverse_signals(self):
        return self._reverse_signals.Value

    @property
    def trade_volume(self):
        return self._trade_volume.Value

    def OnReseted(self):
        super(smoothing_average_crossover_strategy, self).OnReseted()
        self._ma_shift_buffer = deque()
        self._entry_delta = 0.0
        self._close_delta = 0.0

    def OnStarted2(self, time):
        super(smoothing_average_crossover_strategy, self).OnStarted2(time)

        self.Volume = self.trade_volume

        pip = self._calculate_pip_size()
        self._entry_delta = pip * float(self.entry_delta_pips)
        self._close_delta = self._entry_delta * float(self.close_delta_coefficient)

        ma = SimpleMovingAverage()
        ma.Length = self.ma_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

        self.StartProtection(None, None)

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        shifted_ma = self._apply_shift(float(ma_value))

        close = float(candle.ClosePrice)

        entry_upper = shifted_ma + self._entry_delta
        entry_lower = shifted_ma - self._entry_delta
        close_upper = shifted_ma + self._close_delta
        close_lower = shifted_ma - self._close_delta

        tv = float(self.trade_volume)

        if self.Position == 0:
            if not self.reverse_signals:
                if close > entry_upper:
                    vol = tv + max(0.0, -self.Position)
                    if vol > 0:
                        self.BuyMarket(vol)
                    return
                if close < entry_lower:
                    vol = tv + max(0.0, self.Position)
                    if vol > 0:
                        self.SellMarket(vol)
                    return
            else:
                if close > entry_upper:
                    vol = tv + max(0.0, self.Position)
                    if vol > 0:
                        self.SellMarket(vol)
                    return
                if close < entry_lower:
                    vol = tv + max(0.0, -self.Position)
                    if vol > 0:
                        self.BuyMarket(vol)
                    return
        else:
            if not self.reverse_signals:
                if self.Position < 0 and close > close_upper:
                    self.BuyMarket(abs(self.Position))
                if self.Position > 0 and close < close_lower:
                    self.SellMarket(self.Position)
            else:
                if self.Position > 0 and close < close_lower:
                    self.SellMarket(self.Position)
                if self.Position < 0 and close > close_upper:
                    self.BuyMarket(abs(self.Position))

    def _apply_shift(self, current_value):
        shift = self.ma_shift
        if shift <= 0:
            return current_value

        if len(self._ma_shift_buffer) < shift:
            shifted = current_value
        else:
            shifted = self._ma_shift_buffer[0]

        self._ma_shift_buffer.append(current_value)

        if len(self._ma_shift_buffer) > shift:
            self._ma_shift_buffer.popleft()

        return shifted

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 0.0001
        step = sec.PriceStep
        if step is None or float(step) <= 0:
            return 0.0001
        step_val = float(step)
        digits = int(round(Math.Log10(1.0 / step_val)))
        if digits == 3 or digits == 5:
            return step_val * 10.0
        return step_val

    def CreateClone(self):
        return smoothing_average_crossover_strategy()
