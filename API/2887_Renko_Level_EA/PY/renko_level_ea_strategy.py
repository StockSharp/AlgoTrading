import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class renko_level_ea_strategy(Strategy):
    def __init__(self):
        super(renko_level_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "Data")
        self._brick_size = self.Param("BrickSize", 3000) \
            .SetDisplay("Brick Size", "Renko block size in price steps", "Renko Levels")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Reverse Signals", "Invert long and short actions", "Trading")

        self._upper_level = 0.0
        self._lower_level = 0.0
        self._previous_upper = None
        self._levels_initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def BrickSize(self):
        return self._brick_size.Value

    @property
    def ReverseSignals(self):
        return self._reverse_signals.Value

    def OnReseted(self):
        super(renko_level_ea_strategy, self).OnReseted()
        self._upper_level = 0.0
        self._lower_level = 0.0
        self._previous_upper = None
        self._levels_initialized = False

    def OnStarted(self, time):
        super(renko_level_ea_strategy, self).OnStarted(time)
        self._upper_level = 0.0
        self._lower_level = 0.0
        self._previous_upper = None
        self._levels_initialized = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if price_step <= 0:
            price_step = 1.0

        close = float(candle.ClosePrice)

        if not self._update_levels(close, price_step):
            return

        if self._previous_upper is None:
            self._previous_upper = self._upper_level
            return

        tolerance = price_step / 2.0
        if abs(self._previous_upper - self._upper_level) <= tolerance:
            return

        is_up_move = self._upper_level > self._previous_upper

        if self.ReverseSignals:
            is_up_move = not is_up_move

        if is_up_move:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        else:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._previous_upper = self._upper_level

    def _update_levels(self, close, price_step):
        step_count = self.BrickSize
        if step_count <= 0:
            return False

        if not self._levels_initialized:
            _, rnd, floor = self._calculate_bounds(close, price_step, step_count)
            self._upper_level = rnd
            self._lower_level = floor
            self._levels_initialized = True
            return True

        if self._lower_level <= close <= self._upper_level:
            return False

        new_ceil, new_round, new_floor = self._calculate_bounds(close, price_step, step_count)
        tolerance = price_step / 2.0

        if close < self._lower_level:
            if abs(new_round - self._lower_level) <= tolerance:
                return False
            self._upper_level = new_ceil
            self._lower_level = new_round
            return True

        if close > self._upper_level:
            if abs(new_round - self._upper_level) <= tolerance:
                return False
            self._lower_level = new_floor
            self._upper_level = new_round
            return True

        return False

    def _calculate_bounds(self, price, price_step, step_count):
        normalized_step = float(step_count)
        ratio = price / price_step / normalized_step
        rounded = round(ratio)
        price_round = rounded * normalized_step * price_step

        ceil_ratio = (price_round + normalized_step / 2.0 * price_step) / price_step / normalized_step
        import math
        ceil_count = math.ceil(ceil_ratio)
        price_ceil = ceil_count * normalized_step * price_step

        floor_ratio = (price_round - normalized_step / 2.0 * price_step) / price_step / normalized_step
        floor_count = math.floor(floor_ratio)
        price_floor = floor_count * normalized_step * price_step

        return (price_ceil, price_round, price_floor)

    def CreateClone(self):
        return renko_level_ea_strategy()
