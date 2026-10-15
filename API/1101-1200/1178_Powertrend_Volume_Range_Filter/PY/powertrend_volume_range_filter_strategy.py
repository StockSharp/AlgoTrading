import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import Array, ArgumentException, Decimal, TimeSpan
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order
from StockSharp.Algo.Indicators import AverageDirectionalIndex, IIndicator
from StockSharp.Algo.Strategies import Strategy


class powertrend_volume_range_filter_strategy(Strategy):
    def __init__(self):
        super(powertrend_volume_range_filter_strategy, self).__init__()
        self._range_period = self.Param("RangePeriod", 40).SetGreaterThanZero() \
            .SetDisplay("Range Period", "Volume-weighted absolute close changes", "Range")
        self._range_multiplier = self.Param("RangeMultiplier", Decimal.Parse("1.5")).SetGreaterThanZero() \
            .SetDisplay("Range Multiplier", "Multiplier of the volume-weighted price-change range", "Range")
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Native directional-index period", "Filters")
        self._adx_threshold = self.Param("AdxThreshold", Decimal(20)).SetRange(Decimal.Zero, Decimal(100)) \
            .SetDisplay("ADX Threshold", "Strict lower bound on formed ADX for entries", "Filters")
        self._vwma_period = self.Param("VwmaPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("VWMA Period", "Volume-weighted average of formed filter values", "Filters")
        self._is_adx_filter_enabled = self.Param("IsAdxFilterEnabled", False) \
            .SetDisplay("ADX Enabled", "Require formed ADX above its threshold", "Filters")
        self._is_high_low_filter_enabled = self.Param("IsHighLowFilterEnabled", False) \
            .SetDisplay("High/Low Enabled", "Require both a higher high and a higher low", "Filters")
        self._is_vwma_filter_enabled = self.Param("IsVwmaFilterEnabled", False) \
            .SetDisplay("VWMA Enabled", "Require close above the formed filter-value VWMA", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying price and true total volume", "General")
        self._reset_state()
        self.OrderRegistering += self._observe_order

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._changes = deque()
        self._filtered = deque()
        self._previous_close = self._previous_high = self._previous_low = self._filter = None
        self._weighted_range = self._range_volume = self._weighted_filter = self._filter_volume = Decimal.Zero
        self._pending_order = None

    def OnReseted(self):
        super(powertrend_volume_range_filter_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(powertrend_volume_range_filter_strategy, self).OnStarted2(time)
        if self._range_period.Value <= 0 or self._range_multiplier.Value <= 0 or self._adx_period.Value <= 0 \
                or self._adx_threshold.Value < 0 or self._adx_threshold.Value > 100 or self._vwma_period.Value <= 0:
            raise ArgumentException("Range/ADX/VWMA periods and multiplier must be positive; ADX threshold must be within [0, 100].")
        self._reset_state()
        subscription = self.SubscribeCandles(self.candle_type)
        if self._is_adx_filter_enabled.Value:
            adx = AverageDirectionalIndex()
            adx.Length = self._adx_period.Value
            subscription.BindEx(Array[IIndicator]([adx]), self.OnProcess, True).Start()
        else:
            subscription.Bind(self._without_adx).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _without_adx(self, candle):
        self.OnProcess(candle, [])

    def _submit(self, side, volume):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = volume
        self.RegisterOrder(order)

    def OnProcess(self, candle, values):
        if candle.State != CandleStates.Finished:
            return
        higher_high_low = self._previous_high is not None and self._previous_low is not None \
            and candle.HighPrice > self._previous_high and candle.LowPrice > self._previous_low
        self._previous_high = candle.HighPrice
        self._previous_low = candle.LowPrice
        if self._previous_close is not None:
            delta = abs(candle.ClosePrice - self._previous_close)
            self._changes.append((delta, candle.TotalVolume))
            self._weighted_range += delta * candle.TotalVolume
            self._range_volume += candle.TotalVolume
            if len(self._changes) > self._range_period.Value:
                removed = self._changes.popleft()
                self._weighted_range -= removed[0] * removed[1]
                self._range_volume -= removed[1]
        self._previous_close = candle.ClosePrice
        if len(self._changes) < self._range_period.Value or self._range_volume <= 0:
            return
        width = self._range_multiplier.Value * self._weighted_range / self._range_volume
        prior = self._filter
        if prior is None:
            self._filter = candle.ClosePrice
        elif candle.ClosePrice > prior + width:
            self._filter = candle.ClosePrice - width
        elif candle.ClosePrice < prior - width:
            self._filter = candle.ClosePrice + width
        self._filtered.append((self._filter, candle.TotalVolume))
        self._weighted_filter += self._filter * candle.TotalVolume
        self._filter_volume += candle.TotalVolume
        if len(self._filtered) > self._vwma_period.Value:
            removed = self._filtered.popleft()
            self._weighted_filter -= removed[0] * removed[1]
            self._filter_volume -= removed[1]
        pending = self._pending_order
        if prior is None or not self.IsFormedAndOnlineAndAllowTrading() \
                or pending is not None and pending.State != OrderStates.Done and pending.State != OrderStates.Failed:
            return
        if self.Position > 0 and candle.ClosePrice < prior - width:
            self._submit(Sides.Sell, self.Position)
            return
        if self.Position != 0 or width <= 0 or candle.ClosePrice <= prior + width or self._filter <= prior \
                or self._is_high_low_filter_enabled.Value and not higher_high_low \
                or self._is_vwma_filter_enabled.Value and (len(self._filtered) < self._vwma_period.Value \
                    or self._filter_volume <= 0 or candle.ClosePrice <= self._weighted_filter / self._filter_volume):
            return
        if self._is_adx_filter_enabled.Value:
            value = values[0]
            strength = value.MovingAverage
            if not value.IsFormed or strength is None or strength <= self._adx_threshold.Value:
                return
        self._submit(Sides.Buy, self.Volume)

    def CreateClone(self):
        return powertrend_volume_range_filter_strategy()
