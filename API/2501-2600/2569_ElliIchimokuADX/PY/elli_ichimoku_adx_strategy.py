import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class elli_ichimoku_adx_strategy(Strategy):
    def __init__(self):
        super(elli_ichimoku_adx_strategy, self).__init__()

        self._take_profit_points = self.Param("TakeProfitPoints", 60.0)
        self._stop_loss_points = self.Param("StopLossPoints", 30.0)
        self._tenkan_period = self.Param("TenkanPeriod", 19)
        self._kijun_period = self.Param("KijunPeriod", 60)
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 120)
        self._adx_period = self.Param("AdxPeriod", 10)
        self._plus_di_high_threshold = self.Param("PlusDiHighThreshold", 10.0)
        self._plus_di_low_threshold = self.Param("PlusDiLowThreshold", 8.0)
        self._baseline_distance_threshold = self.Param("BaselineDistanceThreshold", 5.0)
        self._ichimoku_candle_type = self.Param("IchimokuCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._adx_candle_type = self.Param("AdxCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._previous_plus_di = None
        self._current_plus_di = None
        self._is_adx_ready = False
        self._previous_adx_high = None
        self._previous_adx_low = None
        self._previous_adx_close = None
        self._smoothed_true_range = 0.0
        self._smoothed_plus_dm = 0.0
        self._adx_samples = 0

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def PlusDiHighThreshold(self):
        return self._plus_di_high_threshold.Value

    @PlusDiHighThreshold.setter
    def PlusDiHighThreshold(self, value):
        self._plus_di_high_threshold.Value = value

    @property
    def PlusDiLowThreshold(self):
        return self._plus_di_low_threshold.Value

    @PlusDiLowThreshold.setter
    def PlusDiLowThreshold(self, value):
        self._plus_di_low_threshold.Value = value

    @property
    def BaselineDistanceThreshold(self):
        return self._baseline_distance_threshold.Value

    @BaselineDistanceThreshold.setter
    def BaselineDistanceThreshold(self, value):
        self._baseline_distance_threshold.Value = value

    @property
    def IchimokuCandleType(self):
        return self._ichimoku_candle_type.Value

    @IchimokuCandleType.setter
    def IchimokuCandleType(self, value):
        self._ichimoku_candle_type.Value = value

    @property
    def AdxCandleType(self):
        return self._adx_candle_type.Value

    @AdxCandleType.setter
    def AdxCandleType(self, value):
        self._adx_candle_type.Value = value

    def OnStarted2(self, time):
        super(elli_ichimoku_adx_strategy, self).OnStarted2(time)

        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.TenkanPeriod
        self._ichimoku.Kijun.Length = self.KijunPeriod
        self._ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        self._previous_plus_di = None
        self._current_plus_di = None
        self._is_adx_ready = False
        self._previous_adx_high = None
        self._previous_adx_low = None
        self._previous_adx_close = None
        self._smoothed_true_range = 0.0
        self._smoothed_plus_dm = 0.0
        self._adx_samples = 0

        ichi_sub = self.SubscribeCandles(self.IchimokuCandleType)
        ichi_sub.BindEx(self._ichimoku, self._process_ichimoku)

        if str(self.AdxCandleType) == str(self.IchimokuCandleType):
            ichi_sub.Bind(self._process_adx_candle)
            ichi_sub.Start()
        else:
            ichi_sub.Start()
            adx_sub = self.SubscribeCandles(self.AdxCandleType)
            adx_sub.Bind(self._process_adx_candle).Start()

        tp = float(self.TakeProfitPoints)
        sl = float(self.StopLossPoints)
        sl_unit = Unit(sl, UnitTypes.Absolute) if sl > 0.0 else None
        tp_unit = Unit(tp, UnitTypes.Absolute) if tp > 0.0 else None
        if tp > 0.0 or sl > 0.0:
            self.StartProtection(sl_unit, tp_unit)

    def _process_adx_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._previous_adx_high is None or self._previous_adx_low is None or self._previous_adx_close is None:
            self._previous_adx_high = high
            self._previous_adx_low = low
            self._previous_adx_close = close
            return

        up_move = high - self._previous_adx_high
        down_move = self._previous_adx_low - low
        plus_dm = up_move if (up_move > down_move and up_move > 0.0) else 0.0
        true_range = max(high - low, max(abs(high - self._previous_adx_close), abs(low - self._previous_adx_close)))

        adx_period = int(self.AdxPeriod)
        if self._adx_samples < adx_period:
            self._smoothed_plus_dm += plus_dm
            self._smoothed_true_range += true_range
            self._adx_samples += 1
        else:
            self._smoothed_plus_dm = self._smoothed_plus_dm - (self._smoothed_plus_dm / adx_period) + plus_dm
            self._smoothed_true_range = self._smoothed_true_range - (self._smoothed_true_range / adx_period) + true_range

        if self._adx_samples >= adx_period and self._smoothed_true_range > 0.0:
            self._previous_plus_di = self._current_plus_di
            self._current_plus_di = 100.0 * self._smoothed_plus_dm / self._smoothed_true_range
            self._is_adx_ready = self._previous_plus_di is not None

        self._previous_adx_high = high
        self._previous_adx_low = low
        self._previous_adx_close = close

    def _process_ichimoku(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if self._current_plus_di is None or self._previous_plus_di is None:
            return

        if not self._is_adx_ready:
            return

        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB

        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return

        tenkan_val = float(tenkan)
        kijun_val = float(kijun)
        senkou_a_val = float(senkou_a)
        senkou_b_val = float(senkou_b)
        close = float(candle.ClosePrice)

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if price_step <= 0.0:
            price_step = 1.0

        baseline_distance = abs(tenkan_val - kijun_val) / price_step
        di_high = float(self.PlusDiHighThreshold)
        di_low = float(self.PlusDiLowThreshold)

        has_plus_di_breakout = (self._current_plus_di > di_high and
                                self._previous_plus_di >= di_low and
                                self._current_plus_di >= self._previous_plus_di)

        if not has_plus_di_breakout:
            return

        if baseline_distance < float(self.BaselineDistanceThreshold):
            return

        if self.Position != 0:
            return

        price_above_cloud = (senkou_a_val > senkou_b_val and kijun_val > senkou_a_val and
                             tenkan_val > kijun_val and close > kijun_val)
        price_below_cloud = (senkou_a_val < senkou_b_val and kijun_val < senkou_a_val and
                             tenkan_val < kijun_val and close < kijun_val)

        if price_above_cloud:
            self.BuyMarket()
        elif price_below_cloud:
            self.SellMarket()

    def OnReseted(self):
        super(elli_ichimoku_adx_strategy, self).OnReseted()
        self._previous_plus_di = None
        self._current_plus_di = None
        self._is_adx_ready = False
        self._previous_adx_high = None
        self._previous_adx_low = None
        self._previous_adx_close = None
        self._smoothed_true_range = 0.0
        self._smoothed_plus_dm = 0.0
        self._adx_samples = 0

    def CreateClone(self):
        return elli_ichimoku_adx_strategy()
