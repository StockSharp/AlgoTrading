import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class t3_ma_direction_change_strategy(Strategy):
    """Double-smoothed EMA slope direction change with signal delay and StartProtection."""
    def __init__(self):
        super(t3_ma_direction_change_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 4).SetGreaterThanZero().SetDisplay("EMA Length", "Length of EMA for double smoothing", "Indicator")
        self._ma_shift = self.Param("MaShift", 0).SetNotNegative().SetDisplay("EMA Shift", "Shift applied to smoothed EMA", "Indicator")
        self._signal_bar_offset = self.Param("SignalBarOffset", 1).SetNotNegative().SetDisplay("Signal Delay", "Candles to wait before acting on signal", "Trading rules")
        self._sl_points = self.Param("StopLossPoints", 20.0).SetNotNegative().SetDisplay("Stop Loss (steps)", "SL distance in price steps", "Risk management")
        self._tp_points = self.Param("TakeProfitPoints", 125.0).SetNotNegative().SetDisplay("Take Profit (steps)", "TP distance in price steps", "Risk management")
        self._cooldown = self.Param("SignalCooldownBars", 12).SetGreaterThanZero().SetDisplay("Signal Cooldown", "Bars to wait after entries/exits", "Trading rules")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(t3_ma_direction_change_strategy, self).OnReseted()
        self._recent_smoothed = []
        self._pending_signals = []
        self._prev_direction = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(t3_ma_direction_change_strategy, self).OnStarted2(time)
        self._recent_smoothed = []
        self._pending_signals = []
        self._prev_direction = 0
        self._cooldown_remaining = 0

        self._ema_price = ExponentialMovingAverage()
        self._ema_price.Length = self._ma_length.Value
        self._ema_smooth = ExponentialMovingAverage()
        self._ema_smooth.Length = self._ma_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

        sl_val = float(self._sl_points.Value)
        tp_val = float(self._tp_points.Value)
        sl_unit = Unit(sl_val, UnitTypes.Absolute) if sl_val > 0 else None
        tp_unit = Unit(tp_val, UnitTypes.Absolute) if tp_val > 0 else None
        self.StartProtection(sl_unit, tp_unit)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        inp = DecimalIndicatorValue(self._ema_price, candle.ClosePrice, candle.OpenTime)
        inp.IsFinal = True
        ema_price_result = self._ema_price.Process(inp)
        ema_smooth_result = self._ema_smooth.Process(ema_price_result)
        if not ema_smooth_result.IsFormed:
            self._enqueue_signal(0)
            return

        smoothed_val = float(ema_smooth_result)
        shift = self._ma_shift.Value
        required = shift + 2

        self._recent_smoothed.append(smoothed_val)
        if len(self._recent_smoothed) > required:
            self._recent_smoothed.pop(0)
        if len(self._recent_smoothed) < required:
            self._enqueue_signal(0)
            return

        current_idx = len(self._recent_smoothed) - 1 - shift
        prev_idx = len(self._recent_smoothed) - 2 - shift
        current = self._recent_smoothed[current_idx]
        previous = self._recent_smoothed[prev_idx]

        if current > previous:
            direction = 1
        elif current < previous:
            direction = -1
        else:
            direction = self._prev_direction

        signal = 0
        if self._prev_direction == -1 and direction == 1:
            signal = 1
        elif self._prev_direction == 1 and direction == -1:
            signal = -1

        self._prev_direction = direction
        self._enqueue_signal(signal)

    def _enqueue_signal(self, signal):
        self._pending_signals.append(signal)
        offset = self._signal_bar_offset.Value
        while len(self._pending_signals) > offset:
            ready = self._pending_signals.pop(0)
            self._execute_signal(ready)

    def _execute_signal(self, direction):
        if direction == 0 or self._cooldown_remaining > 0:
            return

        if direction > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown.Value
        elif direction < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown.Value

    def CreateClone(self):
        return t3_ma_direction_change_strategy()
