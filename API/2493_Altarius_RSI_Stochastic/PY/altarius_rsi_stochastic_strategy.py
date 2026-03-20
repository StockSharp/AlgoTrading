import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class altarius_rsi_stochastic_strategy(Strategy):
    def __init__(self):
        super(altarius_rsi_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._base_volume = self.Param("BaseVolume", 0.1)
        self._minimum_volume = self.Param("MinimumVolume", 0.1)
        self._maximum_risk = self.Param("MaximumRisk", 0.1)
        self._decrease_factor = self.Param("DecreaseFactor", 3.0)
        self._primary_stochastic_length = self.Param("PrimaryStochasticLength", 15)
        self._primary_stochastic_k_period = self.Param("PrimaryStochasticKPeriod", 8)
        self._primary_stochastic_d_period = self.Param("PrimaryStochasticDPeriod", 8)
        self._secondary_stochastic_length = self.Param("SecondaryStochasticLength", 10)
        self._secondary_stochastic_k_period = self.Param("SecondaryStochasticKPeriod", 3)
        self._secondary_stochastic_d_period = self.Param("SecondaryStochasticDPeriod", 3)
        self._difference_threshold = self.Param("DifferenceThreshold", 10.0)
        self._primary_buy_limit = self.Param("PrimaryBuyLimit", 50.0)
        self._primary_sell_limit = self.Param("PrimarySellLimit", 55.0)
        self._primary_exit_upper = self.Param("PrimaryExitUpper", 70.0)
        self._primary_exit_lower = self.Param("PrimaryExitLower", 30.0)
        self._rsi_period = self.Param("RsiPeriod", 3)
        self._long_exit_rsi = self.Param("LongExitRsi", 60.0)
        self._short_exit_rsi = self.Param("ShortExitRsi", 40.0)

        self._prev_primary_signal = 0.0
        self._has_prev_signal = False
        self._entry_price = 0.0
        self._position_direction = 0
        self._loss_streak = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def MinimumVolume(self):
        return self._minimum_volume.Value

    @MinimumVolume.setter
    def MinimumVolume(self, value):
        self._minimum_volume.Value = value

    @property
    def MaximumRisk(self):
        return self._maximum_risk.Value

    @MaximumRisk.setter
    def MaximumRisk(self, value):
        self._maximum_risk.Value = value

    @property
    def DecreaseFactor(self):
        return self._decrease_factor.Value

    @DecreaseFactor.setter
    def DecreaseFactor(self, value):
        self._decrease_factor.Value = value

    @property
    def PrimaryStochasticLength(self):
        return self._primary_stochastic_length.Value

    @PrimaryStochasticLength.setter
    def PrimaryStochasticLength(self, value):
        self._primary_stochastic_length.Value = value

    @property
    def PrimaryStochasticKPeriod(self):
        return self._primary_stochastic_k_period.Value

    @PrimaryStochasticKPeriod.setter
    def PrimaryStochasticKPeriod(self, value):
        self._primary_stochastic_k_period.Value = value

    @property
    def PrimaryStochasticDPeriod(self):
        return self._primary_stochastic_d_period.Value

    @PrimaryStochasticDPeriod.setter
    def PrimaryStochasticDPeriod(self, value):
        self._primary_stochastic_d_period.Value = value

    @property
    def SecondaryStochasticLength(self):
        return self._secondary_stochastic_length.Value

    @SecondaryStochasticLength.setter
    def SecondaryStochasticLength(self, value):
        self._secondary_stochastic_length.Value = value

    @property
    def SecondaryStochasticKPeriod(self):
        return self._secondary_stochastic_k_period.Value

    @SecondaryStochasticKPeriod.setter
    def SecondaryStochasticKPeriod(self, value):
        self._secondary_stochastic_k_period.Value = value

    @property
    def SecondaryStochasticDPeriod(self):
        return self._secondary_stochastic_d_period.Value

    @SecondaryStochasticDPeriod.setter
    def SecondaryStochasticDPeriod(self, value):
        self._secondary_stochastic_d_period.Value = value

    @property
    def DifferenceThreshold(self):
        return self._difference_threshold.Value

    @DifferenceThreshold.setter
    def DifferenceThreshold(self, value):
        self._difference_threshold.Value = value

    @property
    def PrimaryBuyLimit(self):
        return self._primary_buy_limit.Value

    @PrimaryBuyLimit.setter
    def PrimaryBuyLimit(self, value):
        self._primary_buy_limit.Value = value

    @property
    def PrimarySellLimit(self):
        return self._primary_sell_limit.Value

    @PrimarySellLimit.setter
    def PrimarySellLimit(self, value):
        self._primary_sell_limit.Value = value

    @property
    def PrimaryExitUpper(self):
        return self._primary_exit_upper.Value

    @PrimaryExitUpper.setter
    def PrimaryExitUpper(self, value):
        self._primary_exit_upper.Value = value

    @property
    def PrimaryExitLower(self):
        return self._primary_exit_lower.Value

    @PrimaryExitLower.setter
    def PrimaryExitLower(self, value):
        self._primary_exit_lower.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def LongExitRsi(self):
        return self._long_exit_rsi.Value

    @LongExitRsi.setter
    def LongExitRsi(self, value):
        self._long_exit_rsi.Value = value

    @property
    def ShortExitRsi(self):
        return self._short_exit_rsi.Value

    @ShortExitRsi.setter
    def ShortExitRsi(self, value):
        self._short_exit_rsi.Value = value

    def OnStarted(self, time):
        super(altarius_rsi_stochastic_strategy, self).OnStarted(time)

        self._prev_primary_signal = 0.0
        self._has_prev_signal = False
        self._entry_price = 0.0
        self._position_direction = 0
        self._loss_streak = 0

        primary_stochastic = StochasticOscillator()
        primary_stochastic.K.Length = self.PrimaryStochasticLength
        primary_stochastic.D.Length = self.PrimaryStochasticDPeriod

        secondary_stochastic = StochasticOscillator()
        secondary_stochastic.K.Length = self.SecondaryStochasticLength
        secondary_stochastic.D.Length = self.SecondaryStochasticDPeriod

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(primary_stochastic, secondary_stochastic, rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, primary_value, secondary_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not primary_value.IsFinal or not secondary_value.IsFinal or not rsi_value.IsFinal:
            return

        primary_main = primary_value.K
        primary_signal = primary_value.D
        secondary_main = secondary_value.K
        secondary_signal = secondary_value.D

        if primary_main is None or primary_signal is None:
            self._update_primary_signal(primary_signal)
            return
        if secondary_main is None or secondary_signal is None:
            self._update_primary_signal(primary_signal)
            return

        primary_main_val = float(primary_main)
        primary_signal_val = float(primary_signal)
        secondary_main_val = float(secondary_main)
        secondary_signal_val = float(secondary_signal)
        rsi_val = float(rsi_value)
        difference = abs(secondary_main_val - secondary_signal_val)

        if self.Position == 0:
            bullish_setup = (primary_main_val > primary_signal_val and
                             primary_main_val < float(self.PrimaryBuyLimit) and
                             difference > float(self.DifferenceThreshold))
            bearish_setup = (primary_main_val < primary_signal_val and
                             primary_main_val > float(self.PrimarySellLimit) and
                             difference > float(self.DifferenceThreshold))

            if bullish_setup:
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                self._position_direction = 1
            elif bearish_setup:
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                self._position_direction = -1
        else:
            if self.Position > 0:
                exit_signal = (rsi_val > float(self.LongExitRsi) and
                               self._has_prev_signal and
                               primary_signal_val < self._prev_primary_signal and
                               primary_signal_val > float(self.PrimaryExitUpper))
                if exit_signal:
                    self._close_position(float(candle.ClosePrice))
            elif self.Position < 0:
                exit_signal = (rsi_val < float(self.ShortExitRsi) and
                               self._has_prev_signal and
                               primary_signal_val > self._prev_primary_signal and
                               primary_signal_val < float(self.PrimaryExitLower))
                if exit_signal:
                    self._close_position(float(candle.ClosePrice))

        self._update_primary_signal(primary_signal_val)

    def _close_position(self, exit_price):
        if self.Position == 0:
            self._position_direction = 0
            self._entry_price = 0.0
            return

        direction = self._position_direction
        entry_price = self._entry_price

        if self.Position > 0:
            self.SellMarket()
        else:
            self.BuyMarket()

        if entry_price > 0.0:
            if direction > 0:
                profit = exit_price - entry_price
                if profit < 0.0:
                    self._loss_streak += 1
                elif profit > 0.0:
                    self._loss_streak = 0
            elif direction < 0:
                profit = entry_price - exit_price
                if profit < 0.0:
                    self._loss_streak += 1
                elif profit > 0.0:
                    self._loss_streak = 0

        self._entry_price = 0.0
        self._position_direction = 0

    def _update_primary_signal(self, signal):
        if signal is not None:
            self._prev_primary_signal = float(signal) if not isinstance(signal, float) else signal
        self._has_prev_signal = True

    def OnReseted(self):
        super(altarius_rsi_stochastic_strategy, self).OnReseted()
        self._prev_primary_signal = 0.0
        self._has_prev_signal = False
        self._entry_price = 0.0
        self._position_direction = 0
        self._loss_streak = 0

    def CreateClone(self):
        return altarius_rsi_stochastic_strategy()
