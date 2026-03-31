import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StochasticOscillator

class open_close2_ampn_stochastic_strategy(Strategy):
    def __init__(self):
        super(open_close2_ampn_stochastic_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._maximum_risk = self.Param("MaximumRisk", 0.3) \
            .SetDisplay("Maximum Risk", "Fraction of equity used for sizing and the drawdown guard", "Money Management")
        self._decrease_factor = self.Param("DecreaseFactor", 100.0) \
            .SetDisplay("Decrease Factor", "Divisor applied after losing trades to shrink the next position", "Money Management")
        self._minimum_volume = self.Param("MinimumVolume", 0.1) \
            .SetDisplay("Minimum Volume", "Lowest volume allowed after money management adjustments", "Money Management")
        self._stochastic_length = self.Param("StochasticLength", 9) \
            .SetDisplay("Stochastic Length", "Number of periods used by the Stochastic oscillator", "Indicators")
        self._stochastic_k_length = self.Param("StochasticKLength", 3) \
            .SetDisplay("Stochastic %K", "Smoothing applied to the %K line", "Indicators")
        self._stochastic_d_length = self.Param("StochasticDLength", 3) \
            .SetDisplay("Stochastic %D", "Smoothing applied to the %D signal line", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time-frame used for processing", "General")

        self._previous_open = None
        self._previous_close = None
        self._average_entry_price = 0.0
        self._entry_volume = 0.0
        self._entry_direction = 0
        self._loss_streak = 0

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @property
    def MaximumRisk(self):
        return self._maximum_risk.Value

    @property
    def DecreaseFactor(self):
        return self._decrease_factor.Value

    @property
    def MinimumVolume(self):
        return self._minimum_volume.Value

    @property
    def StochasticLength(self):
        return self._stochastic_length.Value

    @property
    def StochasticKLength(self):
        return self._stochastic_k_length.Value

    @property
    def StochasticDLength(self):
        return self._stochastic_d_length.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(open_close2_ampn_stochastic_strategy, self).OnStarted2(time)

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticKLength
        self._stochastic.D.Length = self.StochasticDLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if not stochastic_value.IsFinal:
            return

        k_val = stochastic_value.K
        d_val = stochastic_value.D
        if k_val is None or d_val is None:
            return

        main = float(k_val)
        signal = float(d_val)

        close_price = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        if self.Position != 0 and self._apply_risk_guard(close_price):
            self._update_previous_prices(candle)
            return

        can_trade = self.IsFormedAndOnlineAndAllowTrading()

        if self._previous_open is not None and self._previous_close is not None:
            prev_open = self._previous_open
            prev_close = self._previous_close

            if self.Position == 0 and can_trade:
                long_signal = main > signal and open_price < prev_open and close_price < prev_close
                short_signal = main < signal and open_price > prev_open and close_price > prev_close

                if long_signal:
                    volume = self._calculate_trade_volume(close_price)
                    if volume > 0:
                        self.BuyMarket(volume)
                elif short_signal:
                    volume = self._calculate_trade_volume(close_price)
                    if volume > 0:
                        self.SellMarket(volume)

            elif self.Position > 0:
                exit_long = main < signal and open_price > prev_open and close_price > prev_close
                if exit_long:
                    self._close_position(close_price)

            elif self.Position < 0:
                exit_short = main > signal and open_price < prev_open and close_price < prev_close
                if exit_short:
                    self._close_position(close_price)

        self._update_previous_prices(candle)

    def _apply_risk_guard(self, close_price):
        max_risk = float(self.MaximumRisk)
        if max_risk <= 0:
            return False
        floating_pnl = self._calculate_floating_pnl(close_price)
        if floating_pnl >= 0:
            return False
        margin_base = self._get_margin_base()
        if margin_base <= 0:
            return False
        limit = margin_base * max_risk
        if abs(floating_pnl) < limit:
            return False
        self._close_position(close_price)
        return True

    def _calculate_trade_volume(self, price):
        volume = float(self.BaseVolume)
        max_risk = float(self.MaximumRisk)

        if self.Portfolio is not None:
            account_value = self.Portfolio.CurrentValue
            if account_value is not None and float(account_value) > 0 and price > 0 and max_risk > 0:
                risk_volume = round(float(account_value) * max_risk / 1000.0, 2)
                if risk_volume > 0:
                    volume = risk_volume

        dec_factor = float(self.DecreaseFactor)
        if dec_factor > 0 and self._loss_streak > 1:
            reduction = volume * self._loss_streak / dec_factor
            volume -= reduction

        min_vol = float(self.MinimumVolume)
        if volume < min_vol:
            volume = min_vol

        return volume

    def _close_position(self, close_price):
        vol = abs(self.Position)
        if vol <= 0:
            self._reset_entry_state()
            return

        if self.Position > 0:
            self.SellMarket(vol)
        else:
            self.BuyMarket(vol)

        if self._entry_direction != 0 and self._average_entry_price > 0:
            profit = close_price - self._average_entry_price if self._entry_direction > 0 else self._average_entry_price - close_price
            if profit < 0:
                self._loss_streak += 1
            elif profit > 0:
                self._loss_streak = 0

        self._reset_entry_state()

    def _update_previous_prices(self, candle):
        self._previous_open = float(candle.OpenPrice)
        self._previous_close = float(candle.ClosePrice)

    def _calculate_floating_pnl(self, price):
        if self.Position == 0:
            return 0.0
        entry_price = self._average_entry_price
        if entry_price == 0:
            return 0.0
        price_move = price - entry_price
        return price_move * self.Position

    def _get_margin_base(self):
        if self.Portfolio is None:
            return 0.0
        blocked = self.Portfolio.BlockedValue
        if blocked is not None and float(blocked) > 0:
            return float(blocked)
        current = self.Portfolio.CurrentValue
        if current is not None and float(current) > 0:
            return float(current)
        return 0.0

    def _reset_entry_state(self):
        self._average_entry_price = 0.0
        self._entry_volume = 0.0
        self._entry_direction = 0

    def _register_entry(self, price, volume, direction):
        if volume <= 0:
            return
        if self._entry_direction != direction:
            self._entry_direction = direction
            self._average_entry_price = price
            self._entry_volume = volume
            return
        total_volume = self._entry_volume + volume
        if total_volume <= 0:
            self._reset_entry_state()
            return
        self._average_entry_price = (self._average_entry_price * self._entry_volume + price * volume) / total_volume
        self._entry_volume = total_volume

    def OnOwnTradeReceived(self, trade):
        super(open_close2_ampn_stochastic_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Order is None or trade.Trade is None:
            return

        price = float(trade.Trade.Price)
        volume = float(trade.Trade.Volume)

        if trade.Order.Side == Sides.Buy:
            if self.Position > 0:
                self._register_entry(price, volume, 1)
            elif self.Position == 0 and self._entry_direction == -1:
                self._evaluate_closed_trade(price)
        elif trade.Order.Side == Sides.Sell:
            if self.Position < 0:
                self._register_entry(price, volume, -1)
            elif self.Position == 0 and self._entry_direction == 1:
                self._evaluate_closed_trade(price)

    def _evaluate_closed_trade(self, exit_price):
        if self._entry_direction == 0 or self._average_entry_price <= 0:
            self._reset_entry_state()
            return
        profit = exit_price - self._average_entry_price if self._entry_direction > 0 else self._average_entry_price - exit_price
        if profit < 0:
            self._loss_streak += 1
        elif profit > 0:
            self._loss_streak = 0
        self._reset_entry_state()

    def OnReseted(self):
        super(open_close2_ampn_stochastic_strategy, self).OnReseted()
        self._previous_open = None
        self._previous_close = None
        self._average_entry_price = 0.0
        self._entry_volume = 0.0
        self._entry_direction = 0
        self._loss_streak = 0

    def CreateClone(self):
        return open_close2_ampn_stochastic_strategy()
