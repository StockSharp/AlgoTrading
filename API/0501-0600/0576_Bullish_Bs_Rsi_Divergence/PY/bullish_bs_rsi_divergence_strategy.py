import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class StopTypes:
    """Trailing stop types."""
    None_ = 0
    Percent = 1
    Atr = 2


class bullish_bs_rsi_divergence_strategy(Strategy):
    """
    Bullish B's RSI Divergence strategy.
    RSI pivots are bars whose RSI is below (pivot low) or above (pivot high) the PivotLookbackLeft bars before and the
    PivotLookbackRight bars after them; a pivot is confirmed PivotLookbackRight bars later and is compared with the previous pivot
    of the same kind when that one lies RangeLower..RangeUpper bars back. Long only: a regular bullish divergence (price lower low,
    RSI higher low) or a hidden one (price higher low, RSI lower low) opens a long. The long closes on a regular bearish divergence
    (price higher high, RSI lower high), when RSI crosses above TakeProfitRsiLevel, or on the optional trailing stop that follows
    the close by StopLoss percent or by AtrMultiplier times ATR(AtrLength).
    """

    def __init__(self):
        super(bullish_bs_rsi_divergence_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 9).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "RSI")
        self._pivot_lookback_right = self.Param("PivotLookbackRight", 3).SetGreaterThanZero().SetDisplay("Pivot Lookback Right", "Bars after a pivot that confirm it", "Pivots")
        self._pivot_lookback_left = self.Param("PivotLookbackLeft", 1).SetGreaterThanZero().SetDisplay("Pivot Lookback Left", "Bars before a pivot that it must beat", "Pivots")
        self._take_profit_rsi_level = self.Param("TakeProfitRsiLevel", 80.0).SetDisplay("Take Profit RSI Level", "RSI level whose upward cross closes the long", "Exit")
        self._range_upper = self.Param("RangeUpper", 60).SetGreaterThanZero().SetDisplay("Range Upper", "Maximum bars between two compared pivots", "Pivots")
        self._range_lower = self.Param("RangeLower", 5).SetNotNegative().SetDisplay("Range Lower", "Minimum bars between two compared pivots", "Pivots")
        self._stop_type = self.Param("StopType", StopTypes.None_).SetDisplay("Stop Type", "Trailing stop type", "Exit")
        self._stop_loss = self.Param("StopLoss", 5.0).SetNotNegative().SetDisplay("Stop Loss %", "Trailing stop distance for the percent stop", "Exit")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period for the ATR stop", "Exit")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiple for the ATR stop", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._window = []
        self._bar_index = 0
        self._last_low_pivot_bar = None
        self._last_low_pivot_rsi = Decimal(0)
        self._last_low_pivot_price = Decimal(0)
        self._last_high_pivot_bar = None
        self._last_high_pivot_rsi = Decimal(0)
        self._last_high_pivot_price = Decimal(0)
        self._prev_rsi = None
        self._trailing_stop = None

    def OnReseted(self):
        super(bullish_bs_rsi_divergence_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bullish_bs_rsi_divergence_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        prev_rsi = self._prev_rsi
        self._prev_rsi = rsi

        left = self._pivot_lookback_left.Value
        right = self._pivot_lookback_right.Value

        self._bar_index += 1
        self._window.append((rsi, candle.LowPrice, candle.HighPrice))
        size = left + right + 1
        while len(self._window) > size:
            self._window.pop(0)

        bullish = False
        bearish = False

        if len(self._window) == size:
            center = self._window[left]
            pivot_bar = self._bar_index - right

            is_low = True
            is_high = True
            for i in range(size):
                if i == left:
                    continue
                if self._window[i][0] <= center[0]:
                    is_low = False
                if self._window[i][0] >= center[0]:
                    is_high = False

            if is_low:
                if self._last_low_pivot_bar is not None and self._in_range(self._last_low_pivot_bar):
                    regular = center[1] < self._last_low_pivot_price and center[0] > self._last_low_pivot_rsi
                    hidden = center[1] > self._last_low_pivot_price and center[0] < self._last_low_pivot_rsi
                    bullish = regular or hidden
                self._last_low_pivot_bar = pivot_bar
                self._last_low_pivot_rsi = center[0]
                self._last_low_pivot_price = center[1]

            if is_high:
                if self._last_high_pivot_bar is not None and self._in_range(self._last_high_pivot_bar):
                    bearish = center[2] > self._last_high_pivot_price and center[0] < self._last_high_pivot_rsi
                self._last_high_pivot_bar = pivot_bar
                self._last_high_pivot_rsi = center[0]
                self._last_high_pivot_price = center[2]

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if self._trailing_stop is not None and candle.LowPrice <= self._trailing_stop:
                self.SellMarket(self.Position)
                self._trailing_stop = None
                return

            target = Decimal(self._take_profit_rsi_level.Value)
            target_cross = prev_rsi is not None and prev_rsi <= target and rsi > target
            if bearish or target_cross:
                self.SellMarket(self.Position)
                self._trailing_stop = None
                return

            self._update_trailing_stop(candle.ClosePrice, atr_value)
        elif self.Position == 0 and bullish:
            self.BuyMarket(self.Volume)
            self._trailing_stop = None
            self._update_trailing_stop(candle.ClosePrice, atr_value)

    def _in_range(self, prev_pivot_bar):
        # Pivots are compared only when the previous one lies RangeLower..RangeUpper bars before the bar preceding this one.
        bars = self._bar_index - 1 - prev_pivot_bar
        return self._range_lower.Value <= bars <= self._range_upper.Value

    def _update_trailing_stop(self, close, atr_value):
        stop_type = self._stop_type.Value
        candidate = None
        if stop_type == StopTypes.Percent and self._stop_loss.Value > 0:
            candidate = close * (Decimal(1) - Decimal(self._stop_loss.Value) / Decimal(100))
        elif stop_type == StopTypes.Atr and atr_value.IsFormed:
            candidate = close - atr_value.GetValue[Decimal](None) * Decimal(self._atr_multiplier.Value)

        if candidate is None:
            return

        self._trailing_stop = candidate if self._trailing_stop is None else max(self._trailing_stop, candidate)

    def CreateClone(self):
        return bullish_bs_rsi_divergence_strategy()
