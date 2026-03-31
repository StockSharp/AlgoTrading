import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
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

    def OnReseted(self):
        super(altarius_rsi_stochastic_strategy, self).OnReseted()
        self._prev_primary_signal = 0.0
        self._has_prev_signal = False
        self._entry_price = 0.0
        self._position_direction = 0
        self._loss_streak = 0

    def OnStarted2(self, time):
        super(altarius_rsi_stochastic_strategy, self).OnStarted2(time)

        self._prev_primary_signal = 0.0
        self._has_prev_signal = False
        self._entry_price = 0.0
        self._position_direction = 0
        self._loss_streak = 0

        primary_stochastic = StochasticOscillator()
        primary_stochastic.K.Length = int(self._primary_stochastic_length.Value)
        primary_stochastic.D.Length = int(self._primary_stochastic_d_period.Value)

        secondary_stochastic = StochasticOscillator()
        secondary_stochastic.K.Length = int(self._secondary_stochastic_length.Value)
        secondary_stochastic.D.Length = int(self._secondary_stochastic_d_period.Value)

        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_period.Value)

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

        if self.Position != 0:
            portfolio = self.Portfolio
            account_value = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0
            risk_limit = account_value * float(self._maximum_risk.Value)
            pnl = float(self.PnL)
            if pnl < 0.0 and risk_limit > 0.0 and abs(pnl) >= risk_limit:
                self._close_position(float(candle.ClosePrice))
                self._update_primary_signal(primary_signal_val)
                return

        can_trade = self.IsFormedAndOnlineAndAllowTrading()

        if self.Position == 0:
            if not can_trade:
                self._update_primary_signal(primary_signal_val)
                return

            bullish_setup = (primary_main_val > primary_signal_val and
                             primary_main_val < float(self._primary_buy_limit.Value) and
                             difference > float(self._difference_threshold.Value))
            bearish_setup = (primary_main_val < primary_signal_val and
                             primary_main_val > float(self._primary_sell_limit.Value) and
                             difference > float(self._difference_threshold.Value))

            if bullish_setup:
                volume = self._calculate_trade_volume()
                if volume > 0.0:
                    self.BuyMarket(volume)
                    self._entry_price = float(candle.ClosePrice)
                    self._position_direction = 1
            elif bearish_setup:
                volume = self._calculate_trade_volume()
                if volume > 0.0:
                    self.SellMarket(volume)
                    self._entry_price = float(candle.ClosePrice)
                    self._position_direction = -1
        elif can_trade:
            if self.Position > 0:
                exit_signal = (rsi_val > float(self._long_exit_rsi.Value) and
                               self._has_prev_signal and
                               primary_signal_val < self._prev_primary_signal and
                               primary_signal_val > float(self._primary_exit_upper.Value))
                if exit_signal:
                    self._close_position(float(candle.ClosePrice))
            elif self.Position < 0:
                exit_signal = (rsi_val < float(self._short_exit_rsi.Value) and
                               self._has_prev_signal and
                               primary_signal_val > self._prev_primary_signal and
                               primary_signal_val < float(self._primary_exit_lower.Value))
                if exit_signal:
                    self._close_position(float(candle.ClosePrice))

        self._update_primary_signal(primary_signal_val)

    def _calculate_trade_volume(self):
        volume = float(self._base_volume.Value)
        min_vol = float(self._minimum_volume.Value)

        portfolio = self.Portfolio
        if portfolio is not None and portfolio.CurrentValue is not None:
            value = float(portfolio.CurrentValue)
            if value > 0.0:
                risk_volume = round(value * float(self._maximum_risk.Value) / 1000.0, 2)
                if risk_volume > 0.0:
                    volume = risk_volume

        dec_factor = float(self._decrease_factor.Value)
        if dec_factor > 0.0 and self._loss_streak > 1:
            reduction = volume * self._loss_streak / dec_factor
            volume = max(volume - reduction, min_vol)

        if volume < min_vol:
            volume = min_vol

        return volume

    def _close_position(self, exit_price):
        vol = abs(float(self.Position))
        if vol <= 0.0:
            self._position_direction = 0
            self._entry_price = 0.0
            return

        direction = self._position_direction
        entry_price = self._entry_price

        if self.Position > 0:
            self.SellMarket(vol)
        else:
            self.BuyMarket(vol)

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

    def CreateClone(self):
        return altarius_rsi_stochastic_strategy()
