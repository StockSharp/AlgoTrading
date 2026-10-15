import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

# The README does not give the volume average length.
VOLUME_AVERAGE_LENGTH = 20
PARTIAL_SHARE = 0.33


class ema_crossover_volume_stacked_tp_trailing_sl_strategy(Strategy):
    """
    EMA crossover with volume, stacked take profits and trailing stop strategy.
    The fast EMA crossing the slow EMA opens a position in the crossing direction, reversing an opposite one, when the candle
    volume exceeds its average times VolumeMultiplier. A third of the position is taken at Tp1Multiplier ATR and another third at
    Tp2Multiplier ATR from the entry. After price has moved TrailTriggerMultiplier ATR in favour a trailing stop follows the best
    price at TrailOffsetMultiplier ATR. The ATR is frozen at entry.
    """

    def __init__(self):
        super(ema_crossover_volume_stacked_tp_trailing_sl_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 21).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 55).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.2).SetNotNegative().SetDisplay("Volume Multiplier", "Multiplier of the average volume", "Filters")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length", "Indicators")
        self._tp1_multiplier = self.Param("Tp1Multiplier", 1.5).SetGreaterThanZero().SetDisplay("TP1 ATR", "First take profit in ATR", "Risk")
        self._tp2_multiplier = self.Param("Tp2Multiplier", 2.5).SetGreaterThanZero().SetDisplay("TP2 ATR", "Second take profit in ATR", "Risk")
        self._trail_offset_multiplier = self.Param("TrailOffsetMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Trail Offset ATR", "Trailing distance in ATR", "Risk")
        self._trail_trigger_multiplier = self.Param("TrailTriggerMultiplier", 1.5).SetNotNegative().SetDisplay("Trail Trigger ATR", "Favourable move in ATR that activates trailing", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_average = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._entry_volume = 0.0
        self._best_price = 0.0
        self._tp1_done = False
        self._tp2_done = False
        self._trail_active = False

    def OnReseted(self):
        super(ema_crossover_volume_stacked_tp_trailing_sl_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_crossover_volume_stacked_tp_trailing_sl_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = VOLUME_AVERAGE_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        volume_input = DecimalIndicatorValue(self._volume_average, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_average_value = self._volume_average.Process(volume_input)

        fast = float(fast_value)
        slow = float(slow_value)
        atr = float(atr_value)

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # An exit order sent on this candle is not reflected in Position yet.
        if self.Position != 0 and self._manage_position(candle):
            return

        if not volume_average_value.IsFormed or prev_fast is None or prev_slow is None or atr <= 0:
            return

        volume_average = float(volume_average_value.GetValue[Decimal](None))
        if float(candle.TotalVolume) <= volume_average * float(self._volume_multiplier.Value):
            return

        close = float(candle.ClosePrice)

        if prev_fast <= prev_slow and fast > slow and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._open_position(close, atr)
        elif prev_fast >= prev_slow and fast < slow and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._open_position(close, atr)

    def _open_position(self, price, atr):
        self._entry_price = price
        self._entry_atr = atr
        self._entry_volume = float(self.Volume)
        self._best_price = price
        self._tp1_done = False
        self._tp2_done = False
        self._trail_active = False

    def _partial_volume(self):
        step = float(self.Security.VolumeStep) if self.Security is not None and self.Security.VolumeStep is not None else 0.0
        part = self._entry_volume * PARTIAL_SHARE
        if step > 0:
            part = math.floor(part / step + 1e-9) * step
        return min(part, abs(float(self.Position)))

    def _manage_position(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        atr = self._entry_atr
        trail_distance = float(self._trail_offset_multiplier.Value) * atr
        trigger = float(self._trail_trigger_multiplier.Value) * atr
        tp1 = float(self._tp1_multiplier.Value) * atr
        tp2 = float(self._tp2_multiplier.Value) * atr

        if self.Position > 0:
            if self._trail_active and low <= self._best_price - trail_distance:
                self.SellMarket(self.Position)
                return True

            self._best_price = max(self._best_price, high)

            if not self._trail_active and self._best_price - self._entry_price >= trigger:
                self._trail_active = True

            if not self._tp1_done and high >= self._entry_price + tp1:
                self._tp1_done = True
                part = self._partial_volume()
                if part > 0:
                    self.SellMarket(Decimal(part))
                    return True
            elif self._tp1_done and not self._tp2_done and high >= self._entry_price + tp2:
                self._tp2_done = True
                part = self._partial_volume()
                if part > 0:
                    self.SellMarket(Decimal(part))
                    return True
        else:
            if self._trail_active and high >= self._best_price + trail_distance:
                self.BuyMarket(-self.Position)
                return True

            self._best_price = min(self._best_price, low)

            if not self._trail_active and self._entry_price - self._best_price >= trigger:
                self._trail_active = True

            if not self._tp1_done and low <= self._entry_price - tp1:
                self._tp1_done = True
                part = self._partial_volume()
                if part > 0:
                    self.BuyMarket(Decimal(part))
                    return True
            elif self._tp1_done and not self._tp2_done and low <= self._entry_price - tp2:
                self._tp2_done = True
                part = self._partial_volume()
                if part > 0:
                    self.BuyMarket(Decimal(part))
                    return True

        return False

    def CreateClone(self):
        return ema_crossover_volume_stacked_tp_trailing_sl_strategy()
