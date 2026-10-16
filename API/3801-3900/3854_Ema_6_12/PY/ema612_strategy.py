import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderTypes, OrderStates, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order


class ema612_strategy(Strategy):
    """EMA(6)/EMA(12) cross entries and opposite-cross close-only exits."""

    def __init__(self):
        super(ema612_strategy, self).__init__()
        self._short_ema_length = self.Param("ShortEmaLength", 6).SetGreaterThanZero().SetDisplay(
            "Short EMA", "Period of the fast EMA", "Indicators")
        self._long_ema_length = self.Param("LongEmaLength", 12).SetGreaterThanZero().SetDisplay(
            "Long EMA", "Period of the slow EMA", "Indicators")
        self._use_close_signals = self.Param("UseCloseSignals", True).SetDisplay(
            "Close Signals", "Close on an opposite cross instead of reversing", "Trading")
        self._order_volume = self.Param("OrderVolume", 1.0).SetGreaterThanZero().SetDisplay(
            "Order Volume", "Entry size in lots", "Trading")
        self._trailing_stop_steps = self.Param("TrailingStopSteps", 0.0).SetNotNegative().SetDisplay(
            "Trailing Stop Steps", "Zero disables trailing", "Protection")
        self._take_profit_steps = self.Param("TakeProfitSteps", 0.0).SetNotNegative().SetDisplay(
            "Take Profit Steps", "Zero disables take profit", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay(
            "Candle Type", "Input candle timeframe", "General")
        self._clear_state()

    @property
    def short_ema_length(self):
        return self._short_ema_length.Value

    @property
    def long_ema_length(self):
        return self._long_ema_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _clear_state(self):
        self._previous_fast = None
        self._previous_slow = None
        self._pending_order = None

    def OnReseted(self):
        super(ema612_strategy, self).OnReseted()
        self._clear_state()

    def _normalized_volume(self, volume):
        size = float(volume)
        step = float(self.Security.VolumeStep or 0)
        if step > 0:
            size = math.floor(size / step) * step
        minimum = float(self.Security.MinVolume or 0)
        maximum = float(self.Security.MaxVolume or 0)
        if minimum > 0:
            size = max(size, minimum)
        if maximum > 0:
            size = min(size, maximum)
        return size

    def OnStarted2(self, time):
        super(ema612_strategy, self).OnStarted2(time)
        self._clear_state()
        self.Volume = Decimal(self._normalized_volume(self._order_volume.Value))
        step = float(self.Security.PriceStep or 0)
        if step <= 0:
            raise ValueError("EMA6/12 requires a valid positive PriceStep.")

        take = float(self._take_profit_steps.Value)
        trail = float(self._trailing_stop_steps.Value)
        if take > 0 or trail > 0:
            self.StartProtection(
                Unit(Decimal(take * step), UnitTypes.Absolute) if take > 0 else Unit(),
                Unit(Decimal(trail * step), UnitTypes.Absolute) if trail > 0 else Unit(),
                isStopTrailing=trail > 0,
                useMarketOrders=True, isLocalStop=True)

        fast = ExponentialMovingAverage()
        fast.Length = self.short_ema_length
        slow = ExponentialMovingAverage()
        slow.Length = self.long_ema_length
        self.SubscribeCandles(self.candle_type).Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        f, s = float(fast), float(slow)
        pf, ps = self._previous_fast, self._previous_slow
        self._previous_fast, self._previous_slow = f, s
        if pf is None or ps is None:
            return

        up = pf <= ps and f > s
        down = pf >= ps and f < s
        if not (up or down):
            return
        if (self._pending_order is not None and
                self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return

        side = Sides.Buy if up else Sides.Sell
        position = float(self.Position)
        if position == 0:
            volume = self._normalized_volume(self._order_volume.Value)
            if volume > 0:
                self._submit(side, volume)
        elif self._use_close_signals.Value and (
                side == Sides.Buy and position < 0 or side == Sides.Sell and position > 0):
            self._submit(side, abs(position))

    def _submit(self, side, volume):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = Decimal(volume)
        self._pending_order = order
        self.RegisterOrder(order)

    def CreateClone(self):
        return ema612_strategy()
