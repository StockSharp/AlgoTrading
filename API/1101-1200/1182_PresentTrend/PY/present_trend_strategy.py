import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Array, ArgumentException, Decimal, TimeSpan
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order
from StockSharp.Algo.Indicators import AverageTrueRange, IIndicator, MoneyFlowIndex, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class present_trend_strategy(Strategy):
    def __init__(self):
        super(present_trend_strategy, self).__init__()
        self._length = self.Param("Length", 14).SetGreaterThanZero() \
            .SetDisplay("Length", "Shared Wilder ATR and selected RSI/MFI period", "Indicators")
        self._multiplier = self.Param("Multiplier", Decimal.One).SetGreaterThanZero() \
            .SetDisplay("Multiplier", "ATR distance of the causal low/high rail", "Indicators")
        self._is_use_mfi = self.Param("IsUseMfi", False) \
            .SetDisplay("Use MFI", "Use volume-weighted typical-price MFI instead of close RSI", "Indicators")
        self._is_long_enabled = self.Param("IsLongEnabled", True) \
            .SetDisplay("Enable Long", "Allow long entries; short positions still close on an opposite signal", "Trading")
        self._is_short_enabled = self.Param("IsShortEnabled", True) \
            .SetDisplay("Enable Short", "Allow short entries; long positions still close on an opposite signal", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying the rail and oscillator", "General")
        self._reset_state()
        self.OrderRegistering += self._observe_order

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._previous_lines = []
        self._line = None
        self._bar_index = 0
        self._last_long = self._last_short = None
        self._pending_order = None

    def OnReseted(self):
        super(present_trend_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(present_trend_strategy, self).OnStarted2(time)
        if self._length.Value <= 0 or self._multiplier.Value <= 0 \
                or not self._is_long_enabled.Value and not self._is_short_enabled.Value:
            raise ArgumentException("Period and multiplier must be positive and at least one entry direction enabled.")
        self._reset_state()
        atr = AverageTrueRange()
        atr.Length = self._length.Value
        oscillator = MoneyFlowIndex() if self._is_use_mfi.Value else RelativeStrengthIndex()
        oscillator.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(Array[IIndicator]([atr, oscillator]), self.OnProcess, True).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, values):
        if candle.State != CandleStates.Finished or any(value.IsEmpty or not value.IsFormed for value in values):
            return
        atr = values[0].GetValue[Decimal](None)
        bullish = values[1].GetValue[Decimal](None) >= 50
        candidate = candle.LowPrice - self._multiplier.Value * atr if bullish \
            else candle.HighPrice + self._multiplier.Value * atr
        line = candidate if self._line is None else max(self._line, candidate) if bullish else min(self._line, candidate)
        history = self._previous_lines
        signal = None
        if len(history) == 3:
            if history[2] <= history[0] and line > history[1]:
                if self._last_short is not None and (self._last_long is None or self._last_short > self._last_long):
                    signal = Sides.Buy
                self._last_long = self._bar_index
            elif history[2] >= history[0] and line < history[1]:
                if self._last_long is not None and (self._last_short is None or self._last_long > self._last_short):
                    signal = Sides.Sell
                self._last_short = self._bar_index
        self._line = line
        self._previous_lines = (history + [line])[-3:]
        self._bar_index += 1
        pending = self._pending_order
        if signal is None or not self.IsFormedAndOnlineAndAllowTrading() \
                or pending is not None and pending.State != OrderStates.Done and pending.State != OrderStates.Failed \
                or signal == Sides.Buy and self.Position > 0 or signal == Sides.Sell and self.Position < 0:
            return
        enabled = self._is_long_enabled.Value if signal == Sides.Buy else self._is_short_enabled.Value
        if not enabled and self.Position == 0:
            return
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = signal
        order.Volume = abs(self.Position) + (self.Volume if enabled else Decimal.Zero)
        order.Comment = "PresentTrend confirmed cross" if enabled else "PresentTrend opposite close"
        self.RegisterOrder(order)

    def CreateClone(self):
        return present_trend_strategy()
