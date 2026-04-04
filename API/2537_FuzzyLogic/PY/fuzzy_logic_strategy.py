import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR, RelativeStrengthIndex, SmoothedMovingAverage, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class fuzzy_logic_strategy(Strategy):
    def __init__(self):
        super(fuzzy_logic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._buy_threshold = self.Param("BuyThreshold", 0.15)
        self._sell_threshold = self.Param("SellThreshold", 0.85)
        self._stop_loss_points = self.Param("StopLossPoints", 60.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 40.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 0.0)
        self._williams_period = self.Param("WilliamsPeriod", 14)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._demarker_period = self.Param("DeMarkerPeriod", 14)

        self._jaw_buffer = [None] * 9
        self._teeth_buffer = [None] * 6
        self._lips_buffer = [None] * 4
        self._jaw_count = 0
        self._teeth_count = 0
        self._lips_count = 0

        self._ac_history = [0.0] * 5
        self._ac_count = 0

        self._de_max_queue = []
        self._de_min_queue = []
        self._de_max_sum = 0.0
        self._de_min_sum = 0.0
        self._previous_high = None
        self._previous_low = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BuyThreshold(self):
        return self._buy_threshold.Value

    @BuyThreshold.setter
    def BuyThreshold(self, value):
        self._buy_threshold.Value = value

    @property
    def SellThreshold(self):
        return self._sell_threshold.Value

    @SellThreshold.setter
    def SellThreshold(self, value):
        self._sell_threshold.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def WilliamsPeriod(self):
        return self._williams_period.Value

    @WilliamsPeriod.setter
    def WilliamsPeriod(self, value):
        self._williams_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def DeMarkerPeriod(self):
        return self._demarker_period.Value

    @DeMarkerPeriod.setter
    def DeMarkerPeriod(self, value):
        self._demarker_period.Value = value

    def OnStarted2(self, time):
        super(fuzzy_logic_strategy, self).OnStarted2(time)

        self._jaw_buffer = [None] * 9
        self._teeth_buffer = [None] * 6
        self._lips_buffer = [None] * 4
        self._jaw_count = 0
        self._teeth_count = 0
        self._lips_count = 0
        self._ac_history = [0.0] * 5
        self._ac_count = 0
        self._de_max_queue = []
        self._de_min_queue = []
        self._de_max_sum = 0.0
        self._de_min_sum = 0.0
        self._previous_high = None
        self._previous_low = None

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

        self._williams = WilliamsR()
        self._williams.Length = self.WilliamsPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._williams, self._rsi, self.ProcessCandle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        stop_distance = float(self.TrailingStopPoints) if float(self.TrailingStopPoints) > 0.0 else float(self.StopLossPoints)
        sl_unit = Unit(stop_distance * step, UnitTypes.Absolute) if stop_distance > 0.0 else Unit()
        tp_unit = Unit(float(self.TakeProfitPoints) * step, UnitTypes.Absolute) if float(self.TakeProfitPoints) > 0.0 else Unit()
        self.StartProtection(sl_unit, tp_unit)

    def ProcessCandle(self, candle, wpr_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        hl2 = (high + low) / 2.0

        _di = DecimalIndicatorValue(self._jaw, Decimal(hl2), candle.OpenTime)
        _di.IsFinal = True
        jaw_result = self._jaw.Process(_di)
        _di = DecimalIndicatorValue(self._teeth, Decimal(hl2), candle.OpenTime)
        _di.IsFinal = True
        teeth_result = self._teeth.Process(_di)
        _di = DecimalIndicatorValue(self._lips, Decimal(hl2), candle.OpenTime)
        _di.IsFinal = True
        lips_result = self._lips.Process(_di)
        _di = DecimalIndicatorValue(self._ao_fast, Decimal(hl2), candle.OpenTime)
        _di.IsFinal = True
        ao_fast_result = self._ao_fast.Process(_di)
        _di = DecimalIndicatorValue(self._ao_slow, Decimal(hl2), candle.OpenTime)
        _di.IsFinal = True
        ao_slow_result = self._ao_slow.Process(_di)

        if not jaw_result.IsFinal or not teeth_result.IsFinal or not lips_result.IsFinal or not ao_fast_result.IsFinal or not ao_slow_result.IsFinal:
            self._update_demarker(candle)
            return

        jaw_shifted = self._update_shift_buffer_jaw(float(jaw_result))
        teeth_shifted = self._update_shift_buffer_teeth(float(teeth_result))
        lips_shifted = self._update_shift_buffer_lips(float(lips_result))

        if jaw_shifted is None or teeth_shifted is None or lips_shifted is None:
            self._update_demarker(candle)
            return

        ao = float(ao_fast_result) - float(ao_slow_result)
        _di = DecimalIndicatorValue(self._ac_average, Decimal(ao), candle.OpenTime)
        _di.IsFinal = True
        ac_avg_result = self._ac_average.Process(_di)
        if not ac_avg_result.IsFinal:
            self._update_demarker(candle)
            return

        ac = ao - float(ac_avg_result)
        demarker = self._update_demarker(candle)
        if demarker is None:
            self._update_ac_history(ac)
            return

        if not wpr_value.IsFinal or not rsi_value.IsFinal:
            self._update_ac_history(ac)
            return

        if self._ac_count < 5:
            self._update_ac_history(ac)
            return

        sum_gator = abs(jaw_shifted - teeth_shifted) + abs(teeth_shifted - lips_shifted)
        wpr = float(wpr_value)
        rsi = float(rsi_value)
        decision = self._calculate_decision(sum_gator, wpr, demarker, rsi)

        if self.Position == 0:
            if decision > float(self.SellThreshold):
                self.SellMarket()
            elif decision < float(self.BuyThreshold):
                self.BuyMarket()

        self._update_ac_history(ac)

    def _update_shift_buffer_jaw(self, value):
        shift = 8
        for i in range(shift):
            self._jaw_buffer[i] = self._jaw_buffer[i + 1]
        self._jaw_buffer[shift] = value
        if self._jaw_count >= shift:
            return self._jaw_buffer[0]
        self._jaw_count += 1
        return None

    def _update_shift_buffer_teeth(self, value):
        shift = 5
        for i in range(shift):
            self._teeth_buffer[i] = self._teeth_buffer[i + 1]
        self._teeth_buffer[shift] = value
        if self._teeth_count >= shift:
            return self._teeth_buffer[0]
        self._teeth_count += 1
        return None

    def _update_shift_buffer_lips(self, value):
        shift = 3
        for i in range(shift):
            self._lips_buffer[i] = self._lips_buffer[i + 1]
        self._lips_buffer[shift] = value
        if self._lips_count >= shift:
            return self._lips_buffer[0]
        self._lips_count += 1
        return None

    def _update_demarker(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._previous_high is None or self._previous_low is None:
            self._previous_high = high
            self._previous_low = low
            return None

        de_max = max(high - self._previous_high, 0.0)
        de_min = max(self._previous_low - low, 0.0)

        self._previous_high = high
        self._previous_low = low

        period = int(self.DeMarkerPeriod)
        if len(self._de_max_queue) == period:
            self._de_max_sum -= self._de_max_queue.pop(0)
            self._de_min_sum -= self._de_min_queue.pop(0)

        self._de_max_queue.append(de_max)
        self._de_min_queue.append(de_min)
        self._de_max_sum += de_max
        self._de_min_sum += de_min

        if len(self._de_max_queue) < period:
            return None

        denominator = self._de_max_sum + self._de_min_sum
        if denominator == 0.0:
            return 0.0
        return self._de_max_sum / denominator

    def _update_ac_history(self, ac):
        for i in range(len(self._ac_history) - 1, 0, -1):
            self._ac_history[i] = self._ac_history[i - 1]
        self._ac_history[0] = ac
        if self._ac_count < len(self._ac_history):
            self._ac_count += 1

    def _calculate_decision(self, sum_gator, wpr, demarker, rsi):
        rang = [[0.0] * 5 for _ in range(5)]
        summary = [0.0] * 5

        gator_levels = [0.010, 0.020, 0.030, 0.040, 0.040, 0.030, 0.020, 0.010]
        wpr_levels = [-95.0, -90.0, -80.0, -75.0, -25.0, -20.0, -10.0, -5.0]
        ac_levels = [5.0, 4.0, 3.0, 2.0, 2.0, 3.0, 4.0, 5.0]
        demarker_levels = [0.15, 0.20, 0.25, 0.30, 0.70, 0.75, 0.80, 0.85]
        rsi_levels = [25.0, 30.0, 35.0, 40.0, 60.0, 65.0, 70.0, 75.0]
        weights = [0.133, 0.133, 0.133, 0.268, 0.333]

        # 1) Gator oscillator membership
        if sum_gator < gator_levels[0]:
            rang[0][0] = 0.5
            rang[0][4] = 0.5
        if sum_gator >= gator_levels[0] and sum_gator < gator_levels[1]:
            part = (sum_gator - gator_levels[0]) / (gator_levels[1] - gator_levels[0])
            rang[0][0] = (1.0 - part) / 2.0
            rang[0][1] = (1.0 - rang[0][0] * 2.0) / 2.0
            rang[0][4] = rang[0][0]
            rang[0][3] = rang[0][1]
        if sum_gator >= gator_levels[1] and sum_gator < gator_levels[2]:
            rang[0][1] = 0.5
            rang[0][3] = 0.5
        if sum_gator >= gator_levels[2] and sum_gator < gator_levels[3]:
            part = (sum_gator - gator_levels[2]) / (gator_levels[3] - gator_levels[2])
            rang[0][1] = (1.0 - part) / 2.0
            rang[0][2] = 1.0 - rang[0][1] * 2.0
            rang[0][3] = rang[0][1]
        if sum_gator >= gator_levels[3]:
            rang[0][2] = 1.0

        # 2) Williams %R membership
        if wpr < wpr_levels[0]:
            rang[1][0] = 1.0
        if wpr >= wpr_levels[0] and wpr < wpr_levels[1]:
            part = (wpr - wpr_levels[0]) / (wpr_levels[1] - wpr_levels[0])
            rang[1][0] = 1.0 - part
            rang[1][1] = 1.0 - rang[1][0]
        if wpr >= wpr_levels[1] and wpr < wpr_levels[2]:
            rang[1][1] = 1.0
        if wpr >= wpr_levels[2] and wpr < wpr_levels[3]:
            part = (wpr - wpr_levels[2]) / (wpr_levels[3] - wpr_levels[2])
            rang[1][1] = 1.0 - part
            rang[1][2] = 1.0 - rang[1][1]
        if wpr >= wpr_levels[3] and wpr < wpr_levels[4]:
            rang[1][2] = 1.0
        if wpr >= wpr_levels[4] and wpr < wpr_levels[5]:
            part = (wpr - wpr_levels[4]) / (wpr_levels[5] - wpr_levels[4])
            rang[1][2] = 1.0 - part
            rang[1][3] = 1.0 - rang[1][2]
        if wpr >= wpr_levels[5] and wpr < wpr_levels[6]:
            rang[1][3] = 1.0
        if wpr >= wpr_levels[6] and wpr < wpr_levels[7]:
            part = (wpr - wpr_levels[6]) / (wpr_levels[7] - wpr_levels[6])
            rang[1][3] = 1.0 - part
            rang[1][4] = 1.0 - rang[1][3]
        if wpr >= wpr_levels[7]:
            rang[1][4] = 1.0

        # 3) Acceleration/Deceleration oscillator sequences
        h = self._ac_history
        temp_ac_buy = 0.0
        if h[0] < h[1] and h[0] < 0.0 and h[1] < 0.0:
            temp_ac_buy = 2.0
        if h[0] < h[1] and h[1] < h[2] and h[0] < 0.0 and h[1] < 0.0 and h[2] < 0.0:
            temp_ac_buy = 3.0
        if h[0] < h[1] and h[1] < h[2] and h[2] < h[3] and h[0] < 0.0 and h[1] < 0.0 and h[2] < 0.0 and h[3] < 0.0:
            temp_ac_buy = 4.0
        if h[0] < h[1] and h[1] < h[2] and h[2] < h[3] and h[3] < h[4] and h[0] < 0.0 and h[1] < 0.0 and h[2] < 0.0 and h[3] < 0.0 and h[4] < 0.0:
            temp_ac_buy = 5.0

        temp_ac_sell = 0.0
        if h[0] > h[1] and h[0] > 0.0 and h[1] > 0.0:
            temp_ac_sell = 2.0
        if h[0] > h[1] and h[1] > h[2] and h[0] > 0.0 and h[1] > 0.0 and h[2] > 0.0:
            temp_ac_sell = 3.0
        if h[0] > h[1] and h[1] > h[2] and h[2] > h[3] and h[0] > 0.0 and h[1] > 0.0 and h[2] > 0.0 and h[3] > 0.0:
            temp_ac_sell = 4.0
        if h[0] > h[1] and h[1] > h[2] and h[2] > h[3] and h[3] > h[4] and h[0] > 0.0 and h[1] > 0.0 and h[2] > 0.0 and h[3] > 0.0 and h[4] > 0.0:
            temp_ac_sell = 5.0

        if temp_ac_buy == ac_levels[0] or temp_ac_buy == ac_levels[1]:
            rang[2][0] = 1.0
        if temp_ac_buy == ac_levels[2] or temp_ac_buy == ac_levels[3]:
            rang[2][1] = 1.0
        if temp_ac_sell == ac_levels[4] or temp_ac_sell == ac_levels[5]:
            rang[2][3] = 1.0
        if temp_ac_sell == ac_levels[6] or temp_ac_sell == ac_levels[7]:
            rang[2][4] = 1.0
        if rang[2][0] == 0.0 and rang[2][1] == 0.0 and rang[2][3] == 0.0 and rang[2][4] == 0.0:
            rang[2][2] = 1.0

        # 4) DeMarker membership
        if demarker < demarker_levels[0]:
            rang[3][0] = 1.0
        if demarker >= demarker_levels[0] and demarker < demarker_levels[1]:
            part = (demarker - demarker_levels[0]) / (demarker_levels[1] - demarker_levels[0])
            rang[3][0] = 1.0 - part
            rang[3][1] = 1.0 - rang[3][0]
        if demarker >= demarker_levels[1] and demarker < demarker_levels[2]:
            rang[3][1] = 1.0
        if demarker >= demarker_levels[2] and demarker < demarker_levels[3]:
            part = (demarker - demarker_levels[2]) / (demarker_levels[3] - demarker_levels[2])
            rang[3][1] = 1.0 - part
            rang[3][2] = 1.0 - rang[3][1]
        if demarker >= demarker_levels[3] and demarker < demarker_levels[4]:
            rang[3][2] = 1.0
        if demarker >= demarker_levels[4] and demarker < demarker_levels[5]:
            part = (demarker - demarker_levels[4]) / (demarker_levels[5] - demarker_levels[4])
            rang[3][2] = 1.0 - part
            rang[3][3] = 1.0 - rang[3][2]
        if demarker >= demarker_levels[5] and demarker < demarker_levels[6]:
            rang[3][3] = 1.0
        if demarker >= demarker_levels[6] and demarker < demarker_levels[7]:
            part = (demarker - demarker_levels[6]) / (demarker_levels[7] - demarker_levels[6])
            rang[3][3] = 1.0 - part
            rang[3][4] = 1.0 - rang[3][3]
        if demarker >= demarker_levels[7]:
            rang[3][4] = 1.0

        # 5) RSI membership
        if rsi < rsi_levels[0]:
            rang[4][0] = 1.0
        if rsi >= rsi_levels[0] and rsi < rsi_levels[1]:
            part = (rsi - rsi_levels[0]) / (rsi_levels[1] - rsi_levels[0])
            rang[4][0] = 1.0 - part
            rang[4][1] = 1.0 - rang[4][0]
        if rsi >= rsi_levels[1] and rsi < rsi_levels[2]:
            rang[4][1] = 1.0
        if rsi >= rsi_levels[2] and rsi < rsi_levels[3]:
            part = (rsi - rsi_levels[2]) / (rsi_levels[3] - rsi_levels[2])
            rang[4][1] = 1.0 - part
            rang[4][2] = 1.0 - rang[4][1]
        if rsi >= rsi_levels[3] and rsi < rsi_levels[4]:
            rang[4][2] = 1.0
        if rsi >= rsi_levels[4] and rsi < rsi_levels[5]:
            part = (rsi - rsi_levels[4]) / (rsi_levels[5] - rsi_levels[4])
            rang[4][2] = 1.0 - part
            rang[4][3] = 1.0 - rang[4][2]
        if rsi >= rsi_levels[5] and rsi < rsi_levels[6]:
            rang[4][3] = 1.0
        if rsi >= rsi_levels[6] and rsi < rsi_levels[7]:
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
        super(fuzzy_logic_strategy, self).OnReseted()
        self._jaw_buffer = [None] * 9
        self._teeth_buffer = [None] * 6
        self._lips_buffer = [None] * 4
        self._jaw_count = 0
        self._teeth_count = 0
        self._lips_count = 0
        self._ac_history = [0.0] * 5
        self._ac_count = 0
        self._de_max_queue = []
        self._de_min_queue = []
        self._de_max_sum = 0.0
        self._de_min_sum = 0.0
        self._previous_high = None
        self._previous_low = None

    def CreateClone(self):
        return fuzzy_logic_strategy()
