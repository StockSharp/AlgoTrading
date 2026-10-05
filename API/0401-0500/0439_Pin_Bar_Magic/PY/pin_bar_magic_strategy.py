import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

PIN_BAR_WICK_RATIO = 0.66


class pin_bar_magic_strategy(Strategy):
    """
    Pin Bar Magic Strategy.
    A bullish pin bar whose tail pierces one of the averages while Fast EMA > Medium EMA > Slow SMA arms a buy stop at the
    pin bar high; a bearish pin bar in the opposite fan arms a sell stop at its low. An entry not triggered within
    CancelEntryBars candles is cancelled. The stop sits ATR * AtrMultiplier from the entry and the size risks EquityRisk percent
    of the account on that distance. Positions close when the fast EMA crosses the medium EMA against them.
    """

    def __init__(self):
        super(pin_bar_magic_strategy, self).__init__()
        self._equity_risk = self.Param("EquityRisk", 3.0).SetGreaterThanZero().SetDisplay("Equity Risk %", "Percent of the account risked per trade", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 0.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the stop distance", "Risk")
        self._slow_sma_length = self.Param("SlowSmaLength", 50).SetGreaterThanZero().SetDisplay("Slow SMA Period", "Slow SMA period", "Indicators")
        self._medium_ema_length = self.Param("MediumEmaLength", 18).SetGreaterThanZero().SetDisplay("Medium EMA Period", "Medium EMA period", "Indicators")
        self._fast_ema_length = self.Param("FastEmaLength", 6).SetGreaterThanZero().SetDisplay("Fast EMA Period", "Fast EMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Indicators")
        self._cancel_entry_bars = self.Param("CancelEntryBars", 3).SetGreaterThanZero().SetDisplay("Cancel Entry Bars", "Candles after which an untriggered entry is cancelled", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_medium = None
        self._pending_level = None
        self._pending_stop_distance = 0.0
        self._pending_volume = 0.0
        self._pending_side = 0
        self._pending_bars_left = 0
        self._stop_price = None

    def OnReseted(self):
        super(pin_bar_magic_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(pin_bar_magic_strategy, self).OnStarted2(time)

        self._reset_state()

        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self._slow_sma_length.Value
        medium_ema = ExponentialMovingAverage()
        medium_ema.Length = self._medium_ema_length.Value
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(slow_sma, medium_ema, fast_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, slow_sma)
            self.DrawIndicator(area, medium_ema)
            self.DrawIndicator(area, fast_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, slow_value, medium_value, fast_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not slow_value.IsFormed or not medium_value.IsFormed or not fast_value.IsFormed or not atr_value.IsFormed:
            return

        slow = float(slow_value.GetValue[Decimal](None))
        medium = float(medium_value.GetValue[Decimal](None))
        fast = float(fast_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))

        prev_fast = self._prev_fast
        prev_medium = self._prev_medium
        self._prev_fast = fast
        self._prev_medium = medium

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_down = prev_fast is not None and prev_medium is not None and prev_fast >= prev_medium and fast < medium
        cross_up = prev_fast is not None and prev_medium is not None and prev_fast <= prev_medium and fast > medium

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        position = float(self.Position)

        # Exits of the open position: the ATR stop or the fast/medium cross against it.
        if position > 0 and ((self._stop_price is not None and low <= self._stop_price) or cross_down):
            self.SellMarket(self.Position)
            self._stop_price = None
            position = 0.0
        elif position < 0 and ((self._stop_price is not None and high >= self._stop_price) or cross_up):
            self.BuyMarket(-self.Position)
            self._stop_price = None
            position = 0.0

        # A pending stop entry triggers when the candle trades through its level.
        if self._pending_level is not None:
            level = self._pending_level
            if self._pending_side > 0 and high >= level:
                self.BuyMarket(Decimal(self._pending_volume + max(0.0, -position)))
                position = self._pending_volume
                self._stop_price = level - self._pending_stop_distance
                self._clear_pending()
            elif self._pending_side < 0 and low <= level:
                self.SellMarket(Decimal(self._pending_volume + max(0.0, position)))
                position = -self._pending_volume
                self._stop_price = level + self._pending_stop_distance
                self._clear_pending()
            else:
                self._pending_bars_left -= 1
                if self._pending_bars_left <= 0:
                    self._clear_pending()

        rng = high - low
        if rng <= 0:
            return

        body_low = min(open_price, close)
        body_high = max(open_price, close)
        bullish_pin_bar = body_low - low > PIN_BAR_WICK_RATIO * rng
        bearish_pin_bar = high - body_high > PIN_BAR_WICK_RATIO * rng

        fan_up = fast > medium and medium > slow
        fan_down = fast < medium and medium < slow

        bull_pierce = any(low < ma and open_price > ma and close > ma for ma in (fast, medium, slow))
        bear_pierce = any(high > ma and open_price < ma and close < ma for ma in (fast, medium, slow))

        stop_distance = atr * float(self._atr_multiplier.Value)
        if stop_distance <= 0:
            return

        if fan_up and bullish_pin_bar and bull_pierce and position <= 0:
            self._arm_entry(1, high, stop_distance)
        elif fan_down and bearish_pin_bar and bear_pierce and position >= 0:
            self._arm_entry(-1, low, stop_distance)

    def _arm_entry(self, side, level, stop_distance):
        volume = self._calculate_volume(stop_distance)
        if volume <= 0:
            return

        self._pending_side = side
        self._pending_level = level
        self._pending_stop_distance = stop_distance
        self._pending_volume = volume
        self._pending_bars_left = self._cancel_entry_bars.Value

    def _clear_pending(self):
        self._pending_level = None
        self._pending_side = 0
        self._pending_bars_left = 0

    def _calculate_volume(self, stop_distance):
        equity = 0.0
        if self.Portfolio is not None:
            if self.Portfolio.CurrentValue is not None:
                equity = float(self.Portfolio.CurrentValue)
            elif self.Portfolio.BeginValue is not None:
                equity = float(self.Portfolio.BeginValue)
        if equity <= 0:
            return float(self.Volume)

        volume = equity * float(self._equity_risk.Value) / 100.0 / stop_distance

        security = self.Security
        step = float(security.VolumeStep) if security is not None and security.VolumeStep is not None else 0.0
        if step > 0:
            volume = math.floor(volume / step) * step
            # Keep the volume an exact multiple of the step after the float round trip.
            volume = round(volume, max(0, -int(math.floor(math.log10(step)))))

        if security is not None and security.MaxVolume is not None and float(security.MaxVolume) > 0 and volume > float(security.MaxVolume):
            volume = float(security.MaxVolume)

        return volume

    def CreateClone(self):
        return pin_bar_magic_strategy()
