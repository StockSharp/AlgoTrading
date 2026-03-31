import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from System import Decimal
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage, WeightedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


# Trend mode constants
TREND_DIRECT = 0
TREND_COUNTER = 1


class fractal_weight_oscillator_strategy(Strategy):
    def __init__(self):
        super(fractal_weight_oscillator_strategy, self).__init__()

        self._trend_mode = self.Param("TrendMode", TREND_DIRECT)
        self._signal_bar = self.Param("SignalBar", 1)
        self._period = self.Param("Period", 30)
        self._smoothing_length = self.Param("SmoothingLength", 30)
        self._smoothing_method = self.Param("SmoothingMethod", 3)  # 0=None,1=SMA,2=EMA,3=SMMA,4=LWMA
        self._rsi_weight = self.Param("RsiWeight", 1.0)
        self._mfi_weight = self.Param("MfiWeight", 1.0)
        self._wpr_weight = self.Param("WprWeight", 1.0)
        self._de_marker_weight = self.Param("DeMarkerWeight", 1.0)
        self._high_level = self.Param("HighLevel", 60.0)
        self._low_level = self.Param("LowLevel", 40.0)
        self._buy_open_enabled = self.Param("BuyOpenEnabled", True)
        self._sell_open_enabled = self.Param("SellOpenEnabled", True)
        self._buy_close_enabled = self.Param("BuyCloseEnabled", True)
        self._sell_close_enabled = self.Param("SellCloseEnabled", True)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._oscillator_history = []
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous_candle = False
        self._positive_flow = []
        self._negative_flow = []
        self._previous_typical = 0.0
        self._has_previous_typical = False
        self._positive_sum = 0.0
        self._negative_sum = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def TrendMode(self):
        return self._trend_mode.Value

    @TrendMode.setter
    def TrendMode(self, value):
        self._trend_mode.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def RsiWeight(self):
        return self._rsi_weight.Value

    @RsiWeight.setter
    def RsiWeight(self, value):
        self._rsi_weight.Value = value

    @property
    def MfiWeight(self):
        return self._mfi_weight.Value

    @MfiWeight.setter
    def MfiWeight(self, value):
        self._mfi_weight.Value = value

    @property
    def WprWeight(self):
        return self._wpr_weight.Value

    @WprWeight.setter
    def WprWeight(self, value):
        self._wpr_weight.Value = value

    @property
    def DeMarkerWeight(self):
        return self._de_marker_weight.Value

    @DeMarkerWeight.setter
    def DeMarkerWeight(self, value):
        self._de_marker_weight.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def BuyOpenEnabled(self):
        return self._buy_open_enabled.Value

    @BuyOpenEnabled.setter
    def BuyOpenEnabled(self, value):
        self._buy_open_enabled.Value = value

    @property
    def SellOpenEnabled(self):
        return self._sell_open_enabled.Value

    @SellOpenEnabled.setter
    def SellOpenEnabled(self, value):
        self._sell_open_enabled.Value = value

    @property
    def BuyCloseEnabled(self):
        return self._buy_close_enabled.Value

    @BuyCloseEnabled.setter
    def BuyCloseEnabled(self, value):
        self._buy_close_enabled.Value = value

    @property
    def SellCloseEnabled(self):
        return self._sell_close_enabled.Value

    @SellCloseEnabled.setter
    def SellCloseEnabled(self, value):
        self._sell_close_enabled.Value = value

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
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(fractal_weight_oscillator_strategy, self).OnStarted2(time)

        self._oscillator_history = []
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous_candle = False
        self._positive_flow = []
        self._negative_flow = []
        self._previous_typical = 0.0
        self._has_previous_typical = False
        self._positive_sum = 0.0
        self._negative_sum = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.Period
        self._williams_rsi = RelativeStrengthIndex()
        self._williams_rsi.Length = self.Period
        self._de_max_sma = SimpleMovingAverage()
        self._de_max_sma.Length = self.Period
        self._de_min_sma = SimpleMovingAverage()
        self._de_min_sma.Length = self.Period

        self._smoother = self._create_smoother(int(self._smoothing_method.Value), int(self._smoothing_length.Value))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_price = float(candle.OpenPrice)
        typical = (high + low + close) / 3.0
        volume = float(candle.TotalVolume)

        rsi_input = DecimalIndicatorValue(self._rsi, candle.ClosePrice, candle.OpenTime)
        rsi_input.IsFinal = True
        rsi_result = self._rsi.Process(rsi_input)
        wpr_input = DecimalIndicatorValue(self._williams_rsi, candle.ClosePrice, candle.OpenTime)
        wpr_input.IsFinal = True
        wpr_result = self._williams_rsi.Process(wpr_input)

        if not rsi_result.IsFinal or not wpr_result.IsFinal:
            return

        rsi_val = float(rsi_result)
        wpr_val = float(wpr_result)

        mfi = self._calculate_mfi(typical, volume)
        demarker = self._calculate_demarker(candle)

        if mfi is None or demarker is None:
            return

        total_weight = float(self.RsiWeight) + float(self.MfiWeight) + float(self.WprWeight) + float(self.DeMarkerWeight)
        if total_weight <= 0.0:
            return

        weighted = (float(self.RsiWeight) * rsi_val +
                    float(self.MfiWeight) * mfi +
                    float(self.WprWeight) * wpr_val +
                    float(self.DeMarkerWeight) * (demarker * 100.0)) / total_weight

        smoothed = self._apply_smoothing(weighted, candle.OpenTime)
        if smoothed is None:
            return

        self._oscillator_history.append(smoothed)
        self._trim_history()

        signal_bar = int(self.SignalBar)
        if len(self._oscillator_history) < signal_bar + 2:
            return

        current_index = len(self._oscillator_history) - 1 - signal_bar
        if current_index <= 0:
            return

        current = self._oscillator_history[current_index]
        previous = self._oscillator_history[current_index - 1]

        self._check_risk(candle)

        high_lvl = float(self.HighLevel)
        low_lvl = float(self.LowLevel)

        cross_below_low = previous > low_lvl and current <= low_lvl
        cross_above_high = previous < high_lvl and current >= high_lvl

        open_buy = False
        close_buy = False
        open_sell = False
        close_sell = False

        if self.TrendMode == TREND_DIRECT:
            if cross_below_low:
                open_buy = self.BuyOpenEnabled
                close_sell = self.SellCloseEnabled
            if cross_above_high:
                open_sell = self.SellOpenEnabled
                close_buy = self.BuyCloseEnabled
        else:
            if cross_below_low:
                open_sell = self.SellOpenEnabled
                close_buy = self.BuyCloseEnabled
            if cross_above_high:
                open_buy = self.BuyOpenEnabled
                close_sell = self.SellCloseEnabled

        if close_buy and self.Position > 0:
            self.SellMarket()
            self._reset_risk()

        if close_sell and self.Position < 0:
            self.BuyMarket()
            self._reset_risk()

        if open_buy and self.Position <= 0:
            self.BuyMarket()
            self._set_risk_levels(close, True)
        elif open_sell and self.Position >= 0:
            self.SellMarket()
            self._set_risk_levels(close, False)

    def _calculate_mfi(self, typical, volume):
        if not self._has_previous_typical:
            self._previous_typical = typical
            self._has_previous_typical = True
            self._positive_flow = []
            self._negative_flow = []
            self._positive_sum = 0.0
            self._negative_sum = 0.0
            return None

        flow = typical * volume
        positive = flow if typical > self._previous_typical else 0.0
        negative = flow if typical < self._previous_typical else 0.0
        self._previous_typical = typical

        self._positive_sum += positive
        self._negative_sum += negative
        self._positive_flow.append(positive)
        self._negative_flow.append(negative)

        period = int(self.Period)
        if len(self._positive_flow) > period:
            self._positive_sum -= self._positive_flow.pop(0)
            self._negative_sum -= self._negative_flow.pop(0)

        if len(self._positive_flow) < period:
            return None

        if self._negative_sum == 0.0:
            return 100.0

        ratio = self._positive_sum / self._negative_sum
        return 100.0 - 100.0 / (1.0 + ratio)

    def _calculate_demarker(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if not self._has_previous_candle:
            self._previous_high = high
            self._previous_low = low
            self._has_previous_candle = True
            return None

        de_max = max(high - self._previous_high, 0.0)
        de_min = max(self._previous_low - low, 0.0)

        self._previous_high = high
        self._previous_low = low

        de_max_input = DecimalIndicatorValue(self._de_max_sma, Decimal(de_max), candle.OpenTime)
        de_max_input.IsFinal = True
        de_max_result = self._de_max_sma.Process(de_max_input)
        de_min_input = DecimalIndicatorValue(self._de_min_sma, Decimal(de_min), candle.OpenTime)
        de_min_input.IsFinal = True
        de_min_result = self._de_min_sma.Process(de_min_input)

        if not de_max_result.IsFinal or not de_min_result.IsFinal:
            return None

        max_avg = float(de_max_result)
        min_avg = float(de_min_result)
        denom = max_avg + min_avg

        if denom == 0.0:
            return 0.5

        return max_avg / denom

    def _trim_history(self):
        period = int(self.Period)
        smoothing_len = int(self._smoothing_length.Value)
        signal_bar = int(self.SignalBar)
        max_size = signal_bar + max(period, smoothing_len) + 5
        if len(self._oscillator_history) > max_size:
            remove = len(self._oscillator_history) - max_size
            self._oscillator_history = self._oscillator_history[remove:]

    def _check_risk(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_risk()
                return
            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_risk()
        elif self.Position < 0:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_risk()
                return
            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_risk()

    def _set_risk_levels(self, close, is_long):
        self._entry_price = close
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0.0:
            self._stop_price = None
            self._take_price = None
            return

        sl_pts = int(self.StopLossPoints)
        tp_pts = int(self.TakeProfitPoints)

        if sl_pts > 0:
            if is_long:
                self._stop_price = close - step * sl_pts
            else:
                self._stop_price = close + step * sl_pts
        else:
            self._stop_price = None

        if tp_pts > 0:
            if is_long:
                self._take_price = close + step * tp_pts
            else:
                self._take_price = close - step * tp_pts
        else:
            self._take_price = None

    def _create_smoother(self, method, length):
        if method == 0:
            return None
        elif method == 1:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind
        elif method == 2:
            ind = ExponentialMovingAverage()
            ind.Length = length
            return ind
        elif method == 3:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif method == 4:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind

    def _apply_smoothing(self, value, time):
        if self._smoother is None:
            return value
        sm_input = DecimalIndicatorValue(self._smoother, Decimal(value), time)
        sm_input.IsFinal = True
        result = self._smoother.Process(sm_input)
        if result.IsFinal:
            return float(result)
        return None

    def _reset_risk(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(fractal_weight_oscillator_strategy, self).OnReseted()
        self._oscillator_history = []
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous_candle = False
        self._positive_flow = []
        self._negative_flow = []
        self._previous_typical = 0.0
        self._has_previous_typical = False
        self._positive_sum = 0.0
        self._negative_sum = 0.0
        self._reset_risk()

    def CreateClone(self):
        return fractal_weight_oscillator_strategy()
