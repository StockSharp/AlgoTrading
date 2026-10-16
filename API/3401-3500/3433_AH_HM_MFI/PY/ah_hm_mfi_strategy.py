import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex, SimpleMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class ah_hm_mfi_strategy(Strategy):
    def __init__(self):
        super(ah_hm_mfi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._mfi_period = self.Param("MfiPeriod", 47)
        self._ma_period = self.Param("MaPeriod", 5)
        self._hammer_entry_threshold = self.Param("HammerEntryThreshold", 40.0)
        self._hanging_entry_threshold = self.Param("HangingEntryThreshold", 60.0)
        self._mfi_upper_exit_level = self.Param("MfiUpperExitLevel", 70.0)
        self._mfi_lower_exit_level = self.Param("MfiLowerExitLevel", 30.0)
        self._reset_runtime()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MfiPeriod(self):
        return self._mfi_period.Value

    @MfiPeriod.setter
    def MfiPeriod(self, value):
        self._mfi_period.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def HammerEntryThreshold(self):
        return self._hammer_entry_threshold.Value

    @HammerEntryThreshold.setter
    def HammerEntryThreshold(self, value):
        self._hammer_entry_threshold.Value = value

    @property
    def HangingEntryThreshold(self):
        return self._hanging_entry_threshold.Value

    @HangingEntryThreshold.setter
    def HangingEntryThreshold(self, value):
        self._hanging_entry_threshold.Value = value

    @property
    def MfiUpperExitLevel(self):
        return self._mfi_upper_exit_level.Value

    @MfiUpperExitLevel.setter
    def MfiUpperExitLevel(self, value):
        self._mfi_upper_exit_level.Value = value

    @property
    def MfiLowerExitLevel(self):
        return self._mfi_lower_exit_level.Value

    @MfiLowerExitLevel.setter
    def MfiLowerExitLevel(self, value):
        self._mfi_lower_exit_level.Value = value

    def _reset_runtime(self):
        self._previous_candle = None
        self._previous_ma = None
        self._previous_mfi = None

    def OnReseted(self):
        super(ah_hm_mfi_strategy, self).OnReseted()
        self._reset_runtime()

    def OnStarted2(self, time):
        super(ah_hm_mfi_strategy, self).OnStarted2(time)
        self._reset_runtime()
        self._mfi = MoneyFlowIndex()
        self._mfi.Length = self.MfiPeriod
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod
        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        mfi_input = CandleIndicatorValue(self._mfi, candle)
        mfi_input.IsFinal = True
        mfi_value = self._mfi.Process(mfi_input)
        ma_value = process_float(self._ma, candle.ClosePrice, candle.OpenTime, True)

        if not mfi_value.IsFormed or not ma_value.IsFormed:
            self._remember(
                candle,
                float(ma_value) if ma_value.IsFormed else None,
                float(mfi_value) if mfi_value.IsFormed else None
            )
            return

        mfi = float(mfi_value)
        ma = float(ma_value)

        if self._previous_mfi is not None:
            prior = self._previous_mfi
            if self.Position > 0 and (
                    (prior <= self.MfiUpperExitLevel and mfi > self.MfiUpperExitLevel)
                    or (prior >= self.MfiLowerExitLevel and mfi < self.MfiLowerExitLevel)):
                self.ClosePosition()
                self._remember(candle, ma, mfi)
                return

            if self.Position < 0 and (
                    (prior <= self.MfiLowerExitLevel and mfi > self.MfiLowerExitLevel)
                    or (prior <= self.MfiUpperExitLevel and mfi > self.MfiUpperExitLevel)):
                self.ClosePosition()
                self._remember(candle, ma, mfi)
                return

        previous = self._previous_candle
        if self.Position == 0 and previous is not None and self._previous_ma is not None:
            candle_open = float(candle.OpenPrice)
            candle_close = float(candle.ClosePrice)
            candle_high = float(candle.HighPrice)
            candle_low = float(candle.LowPrice)
            body = abs(candle_close - candle_open)
            bar_range = candle_high - candle_low
            if body > 0 and bar_range > 0:
                upper_shadow = candle_high - max(candle_open, candle_close)
                lower_shadow = min(candle_open, candle_close) - candle_low
                body_upper_third = min(candle_open, candle_close) >= candle_high - bar_range / 3.0
                lower_tail = (body_upper_third and lower_shadow >= body * 2.5
                              and upper_shadow <= body * 0.5)
                previous_midpoint = (float(previous.OpenPrice) + float(previous.ClosePrice)) / 2.0
                previous_close = float(previous.ClosePrice)
                if (lower_tail and candle_open < previous_close
                        and previous_midpoint < self._previous_ma
                        and mfi <= self.HammerEntryThreshold):
                    self.BuyMarket()
                elif (lower_tail and candle_open > previous_close
                      and previous_midpoint > self._previous_ma
                      and mfi >= self.HangingEntryThreshold):
                    self.SellMarket()

        self._remember(candle, ma, mfi)

    def _remember(self, candle, ma, mfi):
        self._previous_candle = candle
        self._previous_ma = ma
        self._previous_mfi = mfi

    def CreateClone(self):
        return ah_hm_mfi_strategy()
