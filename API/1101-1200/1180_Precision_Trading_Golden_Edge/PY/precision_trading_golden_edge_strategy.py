import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Array, ArgumentException, Decimal, Int32, TimeSpan
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage, Highest, HullMovingAverage, IIndicator, Lowest, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class precision_trading_golden_edge_strategy(Strategy):
    def __init__(self):
        super(precision_trading_golden_edge_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 14).SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast close EMA period", "Trend")
        self._slow_length = self.Param("SlowLength", 40).SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow close EMA period", "Trend")
        self._hma_length = self.Param("HmaLength", 55).SetRange(2, Int32.MaxValue) \
            .SetDisplay("HMA Length", "Hull close average with integer half and square-root lengths", "Trend")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero() \
            .SetDisplay("RSI Length", "Close momentum period", "Momentum")
        self._rsi_long_threshold = self.Param("RsiLongThreshold", Decimal(55)).SetRange(Decimal.Zero, Decimal(100)) \
            .SetDisplay("Long RSI Threshold", "Strict lower bound for long confirmation", "Momentum")
        self._rsi_short_threshold = self.Param("RsiShortThreshold", Decimal(45)).SetRange(Decimal.Zero, Decimal(100)) \
            .SetDisplay("Short RSI Threshold", "Strict upper bound for short confirmation", "Momentum")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero() \
            .SetDisplay("ATR Length", "Wilder true-range period", "Volatility")
        self._range_length = self.Param("RangeLength", 20).SetGreaterThanZero() \
            .SetDisplay("Range Length", "Current-inclusive high/low window", "Volatility")
        self._min_atr_percent = self.Param("MinAtrPercent", Decimal.Parse("0.05")).SetRange(Decimal.Zero, Decimal.MaxValue) \
            .SetDisplay("Minimum ATR Percent", "Inclusive minimum ATR as percent of current close", "Volatility")
        self._range_atr_multiplier = self.Param("RangeAtrMultiplier", Decimal(3)).SetGreaterThanZero() \
            .SetDisplay("Range ATR Multiplier", "Inclusive minimum highest-lowest span as an ATR multiple", "Volatility")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying all confirmations", "General")
        self._reset_state()
        self.OrderRegistering += self._observe_order

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._is_initialized = False
        self._previous_fast = self._previous_slow = self._previous_hma = Decimal.Zero
        self._pending_order = None

    def OnReseted(self):
        super(precision_trading_golden_edge_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(precision_trading_golden_edge_strategy, self).OnStarted2(time)
        if self._fast_length.Value <= 0 or self._slow_length.Value <= self._fast_length.Value \
                or self._hma_length.Value < 2 or self._rsi_length.Value <= 0 or self._atr_length.Value <= 0 \
                or self._range_length.Value <= 0 or self._min_atr_percent.Value < 0 or self._range_atr_multiplier.Value <= 0 \
                or self._rsi_short_threshold.Value < 0 or self._rsi_long_threshold.Value > 100 \
                or self._rsi_short_threshold.Value >= self._rsi_long_threshold.Value:
            raise ArgumentException("EMA lengths must be ordered, HMA length at least two, other periods/multiplier positive, and RSI thresholds ordered within [0, 100].")
        self._reset_state()
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        hma = HullMovingAverage()
        hma.Length = self._hma_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        highest = Highest()
        highest.Length = self._range_length.Value
        lowest = Lowest()
        lowest.Length = self._range_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(Array[IIndicator]([fast, slow, hma, rsi, atr, highest, lowest]), self.OnProcess, True).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, values):
        if candle.State != CandleStates.Finished or any(value.IsEmpty or not value.IsFormed for value in values):
            return
        fast, slow, hma, rsi, atr, highest, lowest = [value.GetValue[Decimal](None) for value in values]
        volatility_pass = candle.ClosePrice > 0 and atr > 0 \
            and Decimal(100) * atr / candle.ClosePrice >= self._min_atr_percent.Value \
            and highest - lowest >= atr * self._range_atr_multiplier.Value
        pending = self._pending_order
        if self._is_initialized and volatility_pass and self.IsFormedAndOnlineAndAllowTrading() \
                and (pending is None or pending.State == OrderStates.Done or pending.State == OrderStates.Failed):
            side = None
            if self._previous_fast <= self._previous_slow and fast > slow and hma > self._previous_hma \
                    and rsi > self._rsi_long_threshold.Value and self.Position <= 0:
                side = Sides.Buy
            elif self._previous_fast >= self._previous_slow and fast < slow and hma < self._previous_hma \
                    and rsi < self._rsi_short_threshold.Value and self.Position >= 0:
                side = Sides.Sell
            if side is not None:
                order = Order()
                order.Security = self.Security
                order.Portfolio = self.Portfolio
                order.Type = OrderTypes.Market
                order.Side = side
                order.Volume = self.Volume + abs(self.Position)
                order.Comment = "Golden Edge confirmed cross"
                self.RegisterOrder(order)
        self._is_initialized = True
        self._previous_fast = fast
        self._previous_slow = slow
        self._previous_hma = hma

    def CreateClone(self):
        return precision_trading_golden_edge_strategy()
