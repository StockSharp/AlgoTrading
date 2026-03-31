import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from collections import deque
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    WilliamsR,
    RelativeStrengthIndex,
    SmoothedMovingAverage,
    SimpleMovingAverage,
    DecimalIndicatorValue,
)

class fuzzy_logic_legacy_strategy(Strategy):
    def __init__(self):
        super(fuzzy_logic_legacy_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._long_threshold = self.Param("LongThreshold", 0.75) \
            .SetDisplay("Long Threshold", "Decision level for long entries", "Trading")
        self._short_threshold = self.Param("ShortThreshold", 0.25) \
            .SetDisplay("Short Threshold", "Decision level for short entries", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 60.0) \
            .SetDisplay("Stop Loss (points)", "Stop loss distance in price steps", "Risk Management")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 35.0) \
            .SetDisplay("Trailing Stop (points)", "Trailing stop distance in price steps", "Risk Management")
        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Volume per trade when MM is disabled", "Trading")

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = 13
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = 8
        self._lips = SmoothedMovingAverage()
        self._lips.Length = 5
        self._ao_fast = SimpleMovingAverage()
        self._ao_fast.Length = 5
        self._ao_slow = SimpleMovingAverage()
        self._ao_slow.Length = 34
        self._ac_average = SimpleMovingAverage()
        self._ac_average.Length = 5

        self._jaw_buffer = [None] * 9
        self._teeth_buffer = [None] * 6
        self._lips_buffer = [None] * 4
        self._jaw_count = 0
        self._teeth_count = 0
        self._lips_count = 0

        self._ac_history = [0.0] * 5
        self._ac_count = 0

        self._de_max_queue = deque()
        self._de_min_queue = deque()
        self._de_max_sum = 0.0
        self._de_min_sum = 0.0
        self._previous_high = None
        self._previous_low = None

        self._williams_indicator = None
        self._rsi_indicator = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def LongThreshold(self):
        return self._long_threshold.Value

    @property
    def ShortThreshold(self):
        return self._short_threshold.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    def OnStarted2(self, time):
        super(fuzzy_logic_legacy_strategy, self).OnStarted2(time)

        self._williams_indicator = WilliamsR()
        self._williams_indicator.Length = 14
        self._rsi_indicator = RelativeStrengthIndex()
        self._rsi_indicator.Length = 14

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._williams_indicator, self._rsi_indicator, self.ProcessCandle).Start()

        ps = self.Security.PriceStep if self.Security is not None else None
        step = float(ps) if ps is not None else 1.0
        sl_pts = float(self.StopLossPoints)
        sl = Unit(sl_pts * step, UnitTypes.Absolute) if sl_pts > 0 else None
        trailing = float(self.TrailingStopPoints) > 0

        self.StartProtection(None, sl, isStopTrailing=trailing, useMarketOrders=True)

    def ProcessCandle(self, candle, williams_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        hl2 = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0

        jaw_input = DecimalIndicatorValue(self._jaw, hl2, candle.OpenTime)
        jaw_input.IsFinal = True
        jaw_value = self._jaw.Process(jaw_input)
        teeth_input = DecimalIndicatorValue(self._teeth, hl2, candle.OpenTime)
        teeth_input.IsFinal = True
        teeth_value = self._teeth.Process(teeth_input)
        lips_input = DecimalIndicatorValue(self._lips, hl2, candle.OpenTime)
        lips_input.IsFinal = True
        lips_value = self._lips.Process(lips_input)
        ao_fast_input = DecimalIndicatorValue(self._ao_fast, hl2, candle.OpenTime)
        ao_fast_input.IsFinal = True
        ao_fast_value = self._ao_fast.Process(ao_fast_input)
        ao_slow_input = DecimalIndicatorValue(self._ao_slow, hl2, candle.OpenTime)
        ao_slow_input.IsFinal = True
        ao_slow_value = self._ao_slow.Process(ao_slow_input)

        if (not jaw_value.IsFinal or not teeth_value.IsFinal or not lips_value.IsFinal
                or not ao_fast_value.IsFinal or not ao_slow_value.IsFinal):
            self._update_demarker(candle)
            return

        jaw_shifted = self._update_shift_buffer(self._jaw_buffer, 8, float(jaw_value), 'jaw')
        teeth_shifted = self._update_shift_buffer(self._teeth_buffer, 5, float(teeth_value), 'teeth')
        lips_shifted = self._update_shift_buffer(self._lips_buffer, 3, float(lips_value), 'lips')

        if jaw_shifted is None or teeth_shifted is None or lips_shifted is None:
            self._update_demarker(candle)
            return

        ao = float(ao_fast_value) - float(ao_slow_value)
        ac_avg_input = DecimalIndicatorValue(self._ac_average, ao, candle.OpenTime)
        ac_avg_input.IsFinal = True
        ac_average_value = self._ac_average.Process(ac_avg_input)
        if not ac_average_value.IsFinal:
            self._update_demarker(candle)
            return

        ac = ao - float(ac_average_value)
        demarker = self._update_demarker(candle)
        if demarker is None:
            self._update_ac_history(ac)
            return

        if not williams_value.IsFinal or not rsi_value.IsFinal:
            self._update_ac_history(ac)
            return

        if self._ac_count < len(self._ac_history):
            self._update_ac_history(ac)
            return

        sum_gator = abs(jaw_shifted - teeth_shifted) + abs(teeth_shifted - lips_shifted)
        wpr = float(williams_value)
        rsi = float(rsi_value)
        decision = self._calculate_decision(sum_gator, wpr, demarker, rsi)

        if self.IsFormedAndOnlineAndAllowTrading() and self.Position == 0:
            volume = float(self.FixedVolume)
            if decision < float(self.ShortThreshold):
                self.SellMarket(volume)
            elif decision > float(self.LongThreshold):
                self.BuyMarket(volume)

        self._update_ac_history(ac)

    def _update_shift_buffer(self, buffer, shift, value, name):
        for i in range(shift):
            buffer[i] = buffer[i + 1]
        buffer[shift] = value

        count_attr = '_' + name + '_count'
        filled = getattr(self, count_attr)
        if filled >= shift:
            return buffer[0]
        setattr(self, count_attr, filled + 1)
        return None

    def _update_demarker(self, candle):
        if self._previous_high is None or self._previous_low is None:
            self._previous_high = float(candle.HighPrice)
            self._previous_low = float(candle.LowPrice)
            return None

        de_max = max(float(candle.HighPrice) - self._previous_high, 0.0)
        de_min = max(self._previous_low - float(candle.LowPrice), 0.0)

        self._previous_high = float(candle.HighPrice)
        self._previous_low = float(candle.LowPrice)

        if len(self._de_max_queue) == 14:
            self._de_max_sum -= self._de_max_queue.popleft()
            self._de_min_sum -= self._de_min_queue.popleft()

        self._de_max_queue.append(de_max)
        self._de_min_queue.append(de_min)
        self._de_max_sum += de_max
        self._de_min_sum += de_min

        if len(self._de_max_queue) < 14:
            return None

        denominator = self._de_max_sum + self._de_min_sum
        return 0.0 if denominator == 0 else self._de_max_sum / denominator

    def _update_ac_history(self, ac):
        for i in range(len(self._ac_history) - 1, 0, -1):
            self._ac_history[i] = self._ac_history[i - 1]
        self._ac_history[0] = ac
        if self._ac_count < len(self._ac_history):
            self._ac_count += 1

    def _calculate_decision(self, sum_gator, wpr, demarker, rsi):
        rang = [[0.0] * 5 for _ in range(5)]
        summary = [0.0] * 5

        gator_levels = [100, 200, 300, 400, 400, 300, 200, 100]
        wpr_levels = [-95, -90, -80, -75, -25, -20, -10, -5]
        demarker_levels = [0.15, 0.20, 0.25, 0.30, 0.70, 0.75, 0.80, 0.85]
        rsi_levels = [25, 30, 35, 40, 60, 65, 70, 75]
        weights = [0.133, 0.133, 0.133, 0.268, 0.333]

        # Gator membership
        if sum_gator < gator_levels[0]:
            rang[0][0] = 0.5
            rang[0][4] = 0.5
        if gator_levels[0] <= sum_gator < gator_levels[1]:
            part = (sum_gator - gator_levels[0]) / (gator_levels[1] - gator_levels[0])
            rang[0][0] = (1.0 - part) / 2.0
            rang[0][1] = (1.0 - rang[0][0] * 2.0) / 2.0
            rang[0][4] = rang[0][0]
            rang[0][3] = rang[0][1]
        if gator_levels[1] <= sum_gator < gator_levels[2]:
            rang[0][1] = 0.5
            rang[0][3] = 0.5
        if gator_levels[2] <= sum_gator < gator_levels[3]:
            part = (sum_gator - gator_levels[2]) / (gator_levels[3] - gator_levels[2])
            rang[0][1] = (1.0 - part) / 2.0
            rang[0][2] = 1.0 - rang[0][1] * 2.0
            rang[0][3] = rang[0][1]
        if sum_gator >= gator_levels[3]:
            rang[0][2] = 1.0

        # WPR membership
        if wpr < wpr_levels[0]:
            rang[1][0] = 1.0
        if wpr_levels[0] <= wpr < wpr_levels[1]:
            part = (wpr - wpr_levels[0]) / (wpr_levels[1] - wpr_levels[0])
            rang[1][0] = 1.0 - part
            rang[1][1] = 1.0 - rang[1][0]
        if wpr_levels[1] <= wpr < wpr_levels[2]:
            rang[1][1] = 1.0
        if wpr_levels[2] <= wpr < wpr_levels[3]:
            part = (wpr - wpr_levels[2]) / (wpr_levels[3] - wpr_levels[2])
            rang[1][1] = 1.0 - part
            rang[1][2] = 1.0 - rang[1][1]
        if wpr_levels[3] <= wpr < wpr_levels[4]:
            rang[1][2] = 1.0
        if wpr_levels[4] <= wpr < wpr_levels[5]:
            part = (wpr - wpr_levels[4]) / (wpr_levels[5] - wpr_levels[4])
            rang[1][2] = 1.0 - part
            rang[1][3] = 1.0 - rang[1][2]
        if wpr_levels[5] <= wpr < wpr_levels[6]:
            rang[1][3] = 1.0
        if wpr_levels[6] <= wpr < wpr_levels[7]:
            part = (wpr - wpr_levels[6]) / (wpr_levels[7] - wpr_levels[6])
            rang[1][3] = 1.0 - part
            rang[1][4] = 1.0 - rang[1][3]
        if wpr >= wpr_levels[7]:
            rang[1][4] = 1.0

        # AC membership
        ac = self._ac_history
        temp_ac_buy = 0.0
        if ac[0] < ac[1] and ac[0] < 0 and ac[1] < 0:
            temp_ac_buy = 2.0
        if ac[0] < ac[1] and ac[1] < ac[2] and ac[0] < 0 and ac[1] < 0 and ac[2] < 0:
            temp_ac_buy = 3.0
        if (ac[0] < ac[1] and ac[1] < ac[2] and ac[2] < ac[3]
                and ac[0] < 0 and ac[1] < 0 and ac[2] < 0 and ac[3] < 0):
            temp_ac_buy = 4.0
        if (ac[0] < ac[1] and ac[1] < ac[2] and ac[2] < ac[3] and ac[3] < ac[4]
                and ac[0] < 0 and ac[1] < 0 and ac[2] < 0 and ac[3] < 0 and ac[4] < 0):
            temp_ac_buy = 5.0

        temp_ac_sell = 0.0
        if ac[0] > ac[1] and ac[0] > 0 and ac[1] > 0:
            temp_ac_sell = 2.0
        if ac[0] > ac[1] and ac[1] > ac[2] and ac[0] > 0 and ac[1] > 0 and ac[2] > 0:
            temp_ac_sell = 3.0
        if (ac[0] > ac[1] and ac[1] > ac[2] and ac[2] > ac[3]
                and ac[0] > 0 and ac[1] > 0 and ac[2] > 0 and ac[3] > 0):
            temp_ac_sell = 4.0
        if (ac[0] > ac[1] and ac[1] > ac[2] and ac[2] > ac[3] and ac[3] > ac[4]
                and ac[0] > 0 and ac[1] > 0 and ac[2] > 0 and ac[3] > 0 and ac[4] > 0):
            temp_ac_sell = 5.0

        ac_levels = [5, 4, 3, 2, 2, 3, 4, 5]
        if temp_ac_buy == ac_levels[0] or temp_ac_buy == ac_levels[1]:
            rang[2][0] = 1.0
        if temp_ac_buy == ac_levels[2] or temp_ac_buy == ac_levels[3]:
            rang[2][1] = 1.0
        if temp_ac_sell == ac_levels[4] or temp_ac_sell == ac_levels[5]:
            rang[2][3] = 1.0
        if temp_ac_sell == ac_levels[6] or temp_ac_sell == ac_levels[7]:
            rang[2][4] = 1.0
        if rang[2][0] == 0 and rang[2][1] == 0 and rang[2][3] == 0 and rang[2][4] == 0:
            rang[2][2] = 1.0

        # DeMarker membership
        if demarker < demarker_levels[0]:
            rang[3][0] = 1.0
        if demarker_levels[0] <= demarker < demarker_levels[1]:
            part = (demarker - demarker_levels[0]) / (demarker_levels[1] - demarker_levels[0])
            rang[3][0] = 1.0 - part
            rang[3][1] = 1.0 - rang[3][0]
        if demarker_levels[1] <= demarker < demarker_levels[2]:
            rang[3][1] = 1.0
        if demarker_levels[2] <= demarker < demarker_levels[3]:
            part = (demarker - demarker_levels[2]) / (demarker_levels[3] - demarker_levels[2])
            rang[3][1] = 1.0 - part
            rang[3][2] = 1.0 - rang[3][1]
        if demarker_levels[3] <= demarker < demarker_levels[4]:
            rang[3][2] = 1.0
        if demarker_levels[4] <= demarker < demarker_levels[5]:
            part = (demarker - demarker_levels[4]) / (demarker_levels[5] - demarker_levels[4])
            rang[3][2] = 1.0 - part
            rang[3][3] = 1.0 - rang[3][2]
        if demarker_levels[5] <= demarker < demarker_levels[6]:
            rang[3][3] = 1.0
        if demarker_levels[6] <= demarker < demarker_levels[7]:
            part = (demarker - demarker_levels[6]) / (demarker_levels[7] - demarker_levels[6])
            rang[3][3] = 1.0 - part
            rang[3][4] = 1.0 - rang[3][3]
        if demarker >= demarker_levels[7]:
            rang[3][4] = 1.0

        # RSI membership
        if rsi < rsi_levels[0]:
            rang[4][0] = 1.0
        if rsi_levels[0] <= rsi < rsi_levels[1]:
            part = (rsi - rsi_levels[0]) / (rsi_levels[1] - rsi_levels[0])
            rang[4][0] = 1.0 - part
            rang[4][1] = 1.0 - rang[4][0]
        if rsi_levels[1] <= rsi < rsi_levels[2]:
            rang[4][1] = 1.0
        if rsi_levels[2] <= rsi < rsi_levels[3]:
            part = (rsi - rsi_levels[2]) / (rsi_levels[3] - rsi_levels[2])
            rang[4][1] = 1.0 - part
            rang[4][2] = 1.0 - rang[4][1]
        if rsi_levels[3] <= rsi < rsi_levels[4]:
            rang[4][2] = 1.0
        if rsi_levels[4] <= rsi < rsi_levels[5]:
            part = (rsi - rsi_levels[4]) / (rsi_levels[5] - rsi_levels[4])
            rang[4][2] = 1.0 - part
            rang[4][3] = 1.0 - rang[4][2]
        if rsi_levels[5] <= rsi < rsi_levels[6]:
            rang[4][3] = 1.0
        if rsi_levels[6] <= rsi < rsi_levels[7]:
            part = (rsi - rsi_levels[6]) / (rsi_levels[7] - rsi_levels[6])
            rang[4][3] = 1.0 - part
            rang[4][4] = 1.0 - rang[4][3]
        if rsi >= rsi_levels[7]:
            rang[4][4] = 1.0

        for x in range(4):
            for y in range(4):
                summary[x] += rang[y][x] * weights[x]

        decision = 0.0
        for x in range(4):
            decision += summary[x] * (0.2 * (x + 1) - 0.1)

        return decision

    def OnReseted(self):
        super(fuzzy_logic_legacy_strategy, self).OnReseted()
        self._jaw_buffer = [None] * 9
        self._teeth_buffer = [None] * 6
        self._lips_buffer = [None] * 4
        self._jaw_count = 0
        self._teeth_count = 0
        self._lips_count = 0
        self._ac_history = [0.0] * 5
        self._ac_count = 0
        self._de_max_queue.clear()
        self._de_min_queue.clear()
        self._de_max_sum = 0.0
        self._de_min_sum = 0.0
        self._previous_high = None
        self._previous_low = None

    def CreateClone(self):
        return fuzzy_logic_legacy_strategy()
