import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class ma_sar_adx_bind_strategy(Strategy):
    def __init__(self):
        super(ma_sar_adx_bind_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 120)
        self._adx_period = self.Param("AdxPeriod", 18)
        self._sar_step = self.Param("SarStep", 0.02)
        self._sar_max = self.Param("SarMax", 0.1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))

        self._previous_high = None
        self._previous_low = None
        self._previous_close = None
        self._smoothed_plus_dm = 0.0
        self._smoothed_minus_dm = 0.0
        self._smoothed_true_range = 0.0
        self._adx_samples = 0

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @SarStep.setter
    def SarStep(self, value):
        self._sar_step.Value = value

    @property
    def SarMax(self):
        return self._sar_max.Value

    @SarMax.setter
    def SarMax(self, value):
        self._sar_max.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ma_sar_adx_bind_strategy, self).OnStarted(time)

        self._previous_high = None
        self._previous_low = None
        self._previous_close = None
        self._smoothed_plus_dm = 0.0
        self._smoothed_minus_dm = 0.0
        self._smoothed_true_range = 0.0
        self._adx_samples = 0

        ma = SimpleMovingAverage()
        ma.Length = self.MaPeriod

        sar = ParabolicSar()
        sar.Acceleration = float(self.SarStep)
        sar.AccelerationStep = float(self.SarStep)
        sar.AccelerationMax = float(self.SarMax)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, sar, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, moving_average, sar):
        if candle.State != CandleStates.Finished:
            return

        ma_val = float(moving_average)
        sar_val = float(sar)

        plus_di, minus_di, is_ready = self._update_directional_movement(candle)
        if not is_ready:
            return

        close = float(candle.ClosePrice)

        # Exit conditions
        if self.Position > 0 and close < sar_val:
            self.SellMarket()
            return

        if self.Position < 0 and close > sar_val:
            self.BuyMarket()
            return

        # Entry conditions
        bullish_signal = close > ma_val and plus_di >= minus_di and close > sar_val
        bearish_signal = close < ma_val and plus_di <= minus_di and close < sar_val

        if bullish_signal and self.Position <= 0:
            self.BuyMarket()
            return

        if bearish_signal and self.Position >= 0:
            self.SellMarket()

    def _update_directional_movement(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._previous_high is None or self._previous_low is None or self._previous_close is None:
            self._previous_high = high
            self._previous_low = low
            self._previous_close = close
            return (0.0, 0.0, False)

        up_move = high - self._previous_high
        down_move = self._previous_low - low
        plus_dm = up_move if (up_move > down_move and up_move > 0.0) else 0.0
        minus_dm = down_move if (down_move > up_move and down_move > 0.0) else 0.0
        true_range = max(high - low, max(abs(high - self._previous_close), abs(low - self._previous_close)))

        adx_period = int(self.AdxPeriod)

        if self._adx_samples < adx_period:
            self._smoothed_plus_dm += plus_dm
            self._smoothed_minus_dm += minus_dm
            self._smoothed_true_range += true_range
            self._adx_samples += 1
        else:
            self._smoothed_plus_dm = self._smoothed_plus_dm - (self._smoothed_plus_dm / adx_period) + plus_dm
            self._smoothed_minus_dm = self._smoothed_minus_dm - (self._smoothed_minus_dm / adx_period) + minus_dm
            self._smoothed_true_range = self._smoothed_true_range - (self._smoothed_true_range / adx_period) + true_range

        self._previous_high = high
        self._previous_low = low
        self._previous_close = close

        if self._adx_samples < adx_period or self._smoothed_true_range <= 0.0:
            return (0.0, 0.0, False)

        return (
            100.0 * self._smoothed_plus_dm / self._smoothed_true_range,
            100.0 * self._smoothed_minus_dm / self._smoothed_true_range,
            True)

    def OnReseted(self):
        super(ma_sar_adx_bind_strategy, self).OnReseted()
        self._previous_high = None
        self._previous_low = None
        self._previous_close = None
        self._smoothed_plus_dm = 0.0
        self._smoothed_minus_dm = 0.0
        self._smoothed_true_range = 0.0
        self._adx_samples = 0

    def CreateClone(self):
        return ma_sar_adx_bind_strategy()
