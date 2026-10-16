import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.MatchingEngine")

from System import TimeSpan, Decimal
from StockSharp.Messages import (
    DataType, CandleStates, Sides, OrderStates, OrderTypes,
    Unit, UnitTypes, ShrinkRules,
)
from StockSharp.Algo import TraderHelper
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from StockSharp.MatchingEngine import StopOrderCondition


class graal_fractal_channel_strategy(Strategy):
    """Confirmed five-bar fractals, optional channel filters and Williams exits."""

    def __init__(self):
        super(graal_fractal_channel_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero()
        self._stop_loss_pips = self.Param("StopLossPips", 500.0).SetNotNegative()
        self._take_profit_pips = self.Param("TakeProfitPips", 500.0).SetNotNegative()
        self._offset_pips = self.Param("OffsetPips", 5.0).SetNotNegative()
        self._channel_period = self.Param("ChannelPeriod", 14).SetGreaterThanZero()
        self._use_fractal_channel = self.Param("UseFractalChannel", False)
        self._depth_percent = self.Param("DepthPercent", 25.0)
        self._use_high_low_channel = self.Param("UseHighLowChannel", False)
        self._orientation_percent = self.Param("OrientationPercent", 20.0)
        self._allow_flat_trading = self.Param("AllowFlatTrading", True)
        self._flat_threshold_pips = self.Param("FlatThresholdPips", 20.0)
        self._use_williams_exit = self.Param("UseWilliamsExit", False)
        self._williams_period = self.Param("WilliamsPeriod", 14).SetGreaterThanZero()
        self._williams_threshold = self.Param("WilliamsThreshold", 30.0)
        self._use_counter_orders = self.Param("UseCounterOrders", False)
        self._single_position = self.Param("SinglePosition", False)
        self._signal_age_limit = self.Param("SignalAgeLimit", 3).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._clear_state()

    @property
    def order_volume(self): return self._order_volume.Value

    @property
    def channel_period(self): return self._channel_period.Value

    @property
    def williams_period(self): return self._williams_period.Value

    @property
    def candle_type(self): return self._candle_type.Value

    def _clear_state(self):
        self._bars = []
        self._closes = []
        self._upper_fractal = None
        self._lower_fractal = None
        self._pending_side = None
        self._pending_age = 0
        self._finished_bars = 0
        self._entry_order = None
        self._counter_order = None
        self._closing = False

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(graal_fractal_channel_strategy, self).OnReseted()
        self._clear_state()

    def _pip_size(self):
        step = float(self.Security.PriceStep or 0)
        if step <= 0:
            raise ValueError("Graal Fractal Channel requires a positive instrument price step")
        return step * (10.0 if self.Security.Decimals in (3, 5) else 1.0)

    def _normalized_volume(self, size):
        size = float(size)
        step = float(self.Security.VolumeStep or 0)
        if step > 0:
            size = math.floor(size / step) * step
        minimum = float(self.Security.MinVolume or 0)
        maximum = float(self.Security.MaxVolume or 0)
        if minimum > 0:
            size = max(size, minimum)
        if maximum > 0:
            size = min(size, maximum)
        return max(size, 0.0)

    @staticmethod
    def _pending(order):
        return order is not None and order.State not in (OrderStates.Done, OrderStates.Failed)

    def _cancel_counter(self):
        if self._counter_order is not None and self._counter_order.State == OrderStates.Active:
            self.CancelOrder(self._counter_order)
        self._counter_order = None

    def OnStarted2(self, time):
        super(graal_fractal_channel_strategy, self).OnStarted2(time)
        self._clear_state()
        self.Volume = Decimal(self.order_volume)
        pip = self._pip_size()
        take = float(self._take_profit_pips.Value)
        stop = float(self._stop_loss_pips.Value)
        self.StartProtection(
            Unit(Decimal(take * pip), UnitTypes.Absolute) if take > 0 else Unit(),
            Unit(Decimal(stop * pip), UnitTypes.Absolute) if stop > 0 else Unit(),
            useMarketOrders=True, isLocalStop=True,
        )
        wpr = WilliamsR()
        wpr.Length = self.williams_period
        self.SubscribeCandles(self.candle_type).Bind(wpr, self._process_candle).Start()

    def _passes_filters(self, close, side):
        if len(self._closes) < self.channel_period:
            return False
        highest = max(self._closes)
        lowest = min(self._closes)
        width = highest - lowest
        if not self._allow_flat_trading.Value and width < float(self._flat_threshold_pips.Value) * self._pip_size():
            return False
        if self._use_fractal_channel.Value:
            high = self._upper_fractal
            low = self._lower_fractal
            if high is None or low is None or high <= low:
                return False
            inner = (high - low) * float(self._depth_percent.Value) / 100.0
            if close < low + inner or close > high - inner:
                return False
        if self._use_high_low_channel.Value:
            if width <= 0:
                return False
            allowed = width * float(self._orientation_percent.Value) / 100.0
            if side == Sides.Buy and close < highest - allowed:
                return False
            if side == Sides.Sell and close > lowest + allowed:
                return False
        return True

    def _process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        self._finished_bars += 1
        if self._pending_side is not None:
            self._pending_age += 1
            if self._pending_age > self._signal_age_limit.Value:
                self._pending_side = None

        self._bars.append((float(candle.HighPrice), float(candle.LowPrice)))
        if len(self._bars) > 5:
            self._bars.pop(0)
        close = float(candle.ClosePrice)
        self._closes.append(close)
        if len(self._closes) > self.channel_period:
            self._closes.pop(0)

        if len(self._bars) == 5:
            center = self._bars[2]
            upper = all(center[0] > bar[0] for i, bar in enumerate(self._bars) if i != 2)
            lower = all(center[1] < bar[1] for i, bar in enumerate(self._bars) if i != 2)
            if upper:
                self._upper_fractal = center[0]
            if lower:
                self._lower_fractal = center[1]
            if upper != lower:
                self._pending_side = Sides.Sell if upper else Sides.Buy
                self._pending_age = 0
            elif upper:
                self._pending_side = None

        if not self._use_counter_orders.Value or self.Position == 0:
            self._cancel_counter()

        if self._closing:
            if self.Position != 0:
                return
            self._closing = False

        if self._use_williams_exit.Value and self._finished_bars >= self.williams_period:
            wpr = float(wpr_value)
            threshold = float(self._williams_threshold.Value)
            if (self.Position > 0 and wpr > -threshold or
                    self.Position < 0 and wpr < -100.0 + threshold):
                self._cancel_counter()
                self._pending_side = None
                self._closing = True
                self.ClosePosition()
                return

        if self._pending_side is None or self._pending(self._entry_order):
            return
        side = self._pending_side
        if not self._passes_filters(close, side):
            return
        if self._single_position.Value and (
                side == Sides.Buy and self.Position > 0 or
                side == Sides.Sell and self.Position < 0):
            return
        volume = self._normalized_volume(self.order_volume)
        if volume <= 0:
            return
        if side == Sides.Buy and self.Position < 0 or side == Sides.Sell and self.Position > 0:
            volume += abs(float(self.Position))

        self._cancel_counter()
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = Decimal(volume)
        order.Comment = "Graal fractal entry"
        self._entry_order = order
        self.RegisterOrder(order)
        self._pending_side = None

    def OnOwnTradeReceived(self, trade):
        super(graal_fractal_channel_strategy, self).OnOwnTradeReceived(trade)
        if self.Position == 0 or not self._use_counter_orders.Value:
            self._cancel_counter()
            return
        if trade is None or trade.Order != self._entry_order or trade.Trade is None:
            return

        self._cancel_counter()
        side = Sides.Sell if self.Position > 0 else Sides.Buy
        if side == Sides.Sell and self._lower_fractal is None:
            return
        if side == Sides.Buy and self._upper_fractal is None:
            return
        offset = float(self._offset_pips.Value) * self._pip_size()
        price = (self._lower_fractal - offset if side == Sides.Sell
                 else self._upper_fractal + offset)
        if price <= 0:
            return
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Conditional
        order.Side = side
        order.Volume = Decimal(abs(float(self.Position)))
        condition = StopOrderCondition()
        condition.ActivationPrice = TraderHelper.ShrinkPrice(self.Security, Decimal(price), ShrinkRules.Auto)
        order.Condition = condition
        order.Comment = "Graal counter stop"
        self._counter_order = order
        self.RegisterOrder(order)

    def CreateClone(self):
        return graal_fractal_channel_strategy()
