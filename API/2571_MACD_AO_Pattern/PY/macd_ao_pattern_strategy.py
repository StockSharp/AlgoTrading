import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence as MACD
from StockSharp.Algo.Strategies import Strategy


class macd_ao_pattern_strategy(Strategy):
    def __init__(self):
        super(macd_ao_pattern_strategy, self).__init__()

        self._take_profit_pips = self.Param("TakeProfitPips", 60)
        self._stop_loss_pips = self.Param("StopLossPips", 70)
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._macd_fast_period = self.Param("MacdFastPeriod", 12)
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26)
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9)
        self._bearish_extreme_level = self.Param("BearishExtremeLevel", -100.0)
        self._bearish_neutral_level = self.Param("BearishNeutralLevel", -30.0)
        self._bullish_extreme_level = self.Param("BullishExtremeLevel", 100.0)
        self._bullish_neutral_level = self.Param("BullishNeutralLevel", 30.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None

        self._bearish_stage_armed = False
        self._bearish_trigger_ready = False
        self._bearish_signal_pending = False

        self._bullish_stage_armed = False
        self._bullish_trigger_ready = False
        self._bullish_signal_pending = False

        self._stop_price = None
        self._take_price = None

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @MacdFastPeriod.setter
    def MacdFastPeriod(self, value):
        self._macd_fast_period.Value = value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @MacdSlowPeriod.setter
    def MacdSlowPeriod(self, value):
        self._macd_slow_period.Value = value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @MacdSignalPeriod.setter
    def MacdSignalPeriod(self, value):
        self._macd_signal_period.Value = value

    @property
    def BearishExtremeLevel(self):
        return self._bearish_extreme_level.Value

    @BearishExtremeLevel.setter
    def BearishExtremeLevel(self, value):
        self._bearish_extreme_level.Value = value

    @property
    def BearishNeutralLevel(self):
        return self._bearish_neutral_level.Value

    @BearishNeutralLevel.setter
    def BearishNeutralLevel(self, value):
        self._bearish_neutral_level.Value = value

    @property
    def BullishExtremeLevel(self):
        return self._bullish_extreme_level.Value

    @BullishExtremeLevel.setter
    def BullishExtremeLevel(self, value):
        self._bullish_extreme_level.Value = value

    @property
    def BullishNeutralLevel(self):
        return self._bullish_neutral_level.Value

    @BullishNeutralLevel.setter
    def BullishNeutralLevel(self, value):
        self._bullish_neutral_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _get_pip_size(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        decimals = self.Security.Decimals if self.Security is not None and self.Security.Decimals is not None else 0
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnStarted2(self, time):
        super(macd_ao_pattern_strategy, self).OnStarted2(time)

        self.Volume = self.OrderVolume

        self._macd = MACD()
        self._macd.ShortMa.Length = self.MacdFastPeriod
        self._macd.LongMa.Length = self.MacdSlowPeriod

        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._bearish_stage_armed = False
        self._bearish_trigger_ready = False
        self._bearish_signal_pending = False
        self._bullish_stage_armed = False
        self._bullish_trigger_ready = False
        self._bullish_signal_pending = False
        self._stop_price = None
        self._take_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._macd, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, macd_line):
        if candle.State != CandleStates.Finished:
            return

        macd_val = float(macd_line)

        self._handle_position_exit(candle)

        if not self._macd.IsFormed:
            self._update_macd_history(macd_val)
            return

        if self._macd_prev1 is None or self._macd_prev2 is None or self._macd_prev3 is None:
            self._update_macd_history(macd_val)
            return

        macd1 = self._macd_prev1
        macd2 = self._macd_prev2
        macd3 = self._macd_prev3

        bearish_extreme = float(self.BearishExtremeLevel)
        bearish_neutral = float(self.BearishNeutralLevel)
        bullish_extreme = float(self.BullishExtremeLevel)
        bullish_neutral = float(self.BullishNeutralLevel)

        # Bearish sequence
        if macd1 < bearish_extreme and not self._bearish_stage_armed:
            self._bearish_stage_armed = True

        if macd1 > bearish_neutral and self._bearish_stage_armed:
            self._bearish_stage_armed = False
            self._bearish_trigger_ready = True

        bearish_hook = (self._bearish_trigger_ready and
                        macd1 < macd2 and
                        macd2 > macd3 and
                        macd1 < bearish_neutral and
                        macd2 > bearish_neutral)

        if bearish_hook:
            self._bearish_trigger_ready = False
            self._bearish_signal_pending = True

        if macd1 > 0:
            self._reset_bearish_state()

        if self._bearish_signal_pending and self.Position <= 0:
            self.SellMarket()
            pip = self._get_pip_size()
            entry_price = float(candle.ClosePrice)
            self._stop_price = entry_price + int(self.StopLossPips) * pip
            self._take_price = entry_price - int(self.TakeProfitPips) * pip
            self._reset_bearish_state()

        # Bullish sequence
        if macd1 > bullish_extreme and not self._bullish_stage_armed:
            self._bullish_stage_armed = True

        if macd1 < 0:
            self._reset_bullish_state()
        elif macd1 < bullish_neutral and self._bullish_stage_armed:
            self._bullish_stage_armed = False
            self._bullish_trigger_ready = True

        bullish_hook = (self._bullish_trigger_ready and
                        macd1 > macd2 and
                        macd2 < macd3 and
                        macd1 > bullish_neutral and
                        macd2 < bullish_neutral)

        if bullish_hook:
            self._bullish_trigger_ready = False
            self._bullish_signal_pending = True

        if self._bullish_signal_pending and self.Position >= 0:
            self.BuyMarket()
            pip = self._get_pip_size()
            entry_price = float(candle.ClosePrice)
            self._stop_price = entry_price - int(self.StopLossPips) * pip
            self._take_price = entry_price + int(self.TakeProfitPips) * pip
            self._reset_bullish_state()

        self._update_macd_history(macd_val)

    def _handle_position_exit(self, candle):
        if self.Position > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_protection_levels()
                return
            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._reset_protection_levels()
        elif self.Position < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_protection_levels()
                return
            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._reset_protection_levels()

    def _update_macd_history(self, macd_value):
        self._macd_prev3 = self._macd_prev2
        self._macd_prev2 = self._macd_prev1
        self._macd_prev1 = macd_value

    def _reset_bearish_state(self):
        self._bearish_stage_armed = False
        self._bearish_trigger_ready = False
        self._bearish_signal_pending = False

    def _reset_bullish_state(self):
        self._bullish_stage_armed = False
        self._bullish_trigger_ready = False
        self._bullish_signal_pending = False

    def _reset_protection_levels(self):
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(macd_ao_pattern_strategy, self).OnReseted()
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._reset_bearish_state()
        self._reset_bullish_state()
        self._reset_protection_levels()

    def CreateClone(self):
        return macd_ao_pattern_strategy()
