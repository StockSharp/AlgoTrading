import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class e_skoch_open_strategy(Strategy):
    """
    Port of the E-Skoch-Open MetaTrader strategy.
    Reacts to a three-candle closing price pattern and applies
    martingale position sizing together with equity-based stops.
    """

    def __init__(self):
        super(e_skoch_open_strategy, self).__init__()
        self._martingale_multiplier = self.Param("MartingaleMultiplier", 1.6) \
            .SetDisplay("Martingale Mult", "Volume multiplier after losses", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 130.0) \
            .SetDisplay("Stop Loss Points", "Loss distance in adjusted points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 200.0) \
            .SetDisplay("Take Profit Points", "Profit distance in adjusted points", "Risk")
        self._enable_buy = self.Param("EnableBuySignals", True) \
            .SetDisplay("Enable Buy", "Allow opening long positions", "Trading")
        self._enable_sell = self.Param("EnableSellSignals", True) \
            .SetDisplay("Enable Sell", "Allow opening short positions", "Trading")
        self._target_profit_pct = self.Param("TargetProfitPercent", 1.2) \
            .SetDisplay("Target Profit %", "Close all after equity growth", "Risk")
        self._close_on_opposite = self.Param("CloseOnOppositeSignal", False) \
            .SetDisplay("Close On Opposite", "Close positions on opposite signal", "Trading")
        self._max_buy_trades = self.Param("MaxBuyTrades", 1) \
            .SetDisplay("Max Long Trades", "Maximum concurrent long trades", "Risk")
        self._max_sell_trades = self.Param("MaxSellTrades", 1) \
            .SetDisplay("Max Short Trades", "Maximum concurrent short trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for pattern recognition", "Data")
        self._initial_volume = self.Param("InitialOrderVolume", 0.01) \
            .SetDisplay("Initial Volume", "Volume of the first trade", "Trading")

        self._point_value = 0.0
        self._current_volume = 0.0
        self._entry_equity = 0.0
        self._baseline_equity = 0.0
        self._position_tracked = False

        self._close_m1 = None
        self._close_m2 = None
        self._close_m3 = None

        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

        self._active_long_entries = 0
        self._active_short_entries = 0
        self._prev_pattern_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(e_skoch_open_strategy, self).OnReseted()
        self._close_m1 = None
        self._close_m2 = None
        self._close_m3 = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._active_long_entries = 0
        self._active_short_entries = 0
        self._position_tracked = False
        self._point_value = 0.0
        self._current_volume = 0.0
        self._entry_equity = 0.0
        self._baseline_equity = 0.0
        self._prev_pattern_signal = 0

    def OnStarted2(self, time):
        super(e_skoch_open_strategy, self).OnStarted2(time)

        self.Volume = self._initial_volume.Value
        self._point_value = self._calculate_point_value()
        self._current_volume = self._initial_volume.Value
        equity = 0.0
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            equity = float(self.Portfolio.CurrentValue)
        self._baseline_equity = equity
        self._entry_equity = equity
        self._position_tracked = self.Position != 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._check_equity_target()

        if self._check_protection(candle):
            self._update_closes(float(candle.ClosePrice))
            return

        if self._close_m1 is not None and self._close_m2 is not None and self._close_m3 is not None:
            c1 = self._close_m1
            c2 = self._close_m2
            c3 = self._close_m3

            buy_signal = c3 > c2 and c1 < c2
            sell_signal = c3 > c2 and c2 < c1

            pattern_signal = 1 if buy_signal else (-1 if sell_signal else 0)

            if buy_signal and pattern_signal != self._prev_pattern_signal:
                self._handle_buy_signal(candle)
            if sell_signal and pattern_signal != self._prev_pattern_signal:
                self._handle_sell_signal(candle)

            self._prev_pattern_signal = pattern_signal
        else:
            self._prev_pattern_signal = 0

        self._update_closes(float(candle.ClosePrice))

    def _handle_buy_signal(self, candle):
        if not self._enable_buy.Value:
            return
        if self._close_on_opposite.Value and self.Position < 0:
            self.BuyMarket(abs(float(self.Position)))
            return
        if self.Position > 0:
            return
        max_buy = self._max_buy_trades.Value
        if max_buy != -1 and self._active_long_entries >= max_buy:
            return

        volume = self._current_volume
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._active_long_entries += 1
        self._position_tracked = True
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            self._entry_equity = float(self.Portfolio.CurrentValue)
        self._setup_protection(True, float(candle.ClosePrice))

    def _handle_sell_signal(self, candle):
        if not self._enable_sell.Value:
            return
        if self._close_on_opposite.Value and self.Position > 0:
            self.SellMarket(abs(float(self.Position)))
            return
        if self.Position < 0:
            return
        max_sell = self._max_sell_trades.Value
        if max_sell != -1 and self._active_short_entries >= max_sell:
            return

        volume = self._current_volume
        if volume <= 0:
            return

        self.SellMarket(volume)
        self._active_short_entries += 1
        self._position_tracked = True
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            self._entry_equity = float(self.Portfolio.CurrentValue)
        self._setup_protection(False, float(candle.ClosePrice))

    def _check_protection(self, candle):
        if self.Position > 0:
            if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket(abs(float(self.Position)))
                self._reset_protection()
                return True
            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket(abs(float(self.Position)))
                self._reset_protection()
                return True
        elif self.Position < 0:
            if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket(abs(float(self.Position)))
                self._reset_protection()
                return True
            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket(abs(float(self.Position)))
                self._reset_protection()
                return True
        return False

    def _setup_protection(self, is_long, ref_price):
        point = self._point_value
        if point <= 0.0:
            step = 1.0
            if self.Security is not None and self.Security.PriceStep is not None:
                step = float(self.Security.PriceStep)
            point = step if step > 0 else 1.0

        sl = self._stop_loss_points.Value
        tp = self._take_profit_points.Value

        if is_long:
            self._long_stop = ref_price - sl * point if sl > 0 else None
            self._long_take = ref_price + tp * point if tp > 0 else None
            self._short_stop = None
            self._short_take = None
        else:
            self._short_stop = ref_price + sl * point if sl > 0 else None
            self._short_take = ref_price - tp * point if tp > 0 else None
            self._long_stop = None
            self._long_take = None

    def _reset_protection(self):
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def _update_closes(self, close):
        self._close_m3 = self._close_m2
        self._close_m2 = self._close_m1
        self._close_m1 = close

    def _check_equity_target(self):
        if self._target_profit_pct.Value <= 0.0:
            return
        if self._baseline_equity <= 0.0:
            return
        equity = 0.0
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            equity = float(self.Portfolio.CurrentValue)
        growth = (equity - self._baseline_equity) / self._baseline_equity * 100.0
        if growth >= self._target_profit_pct.Value:
            if self.Position > 0:
                self.SellMarket(abs(float(self.Position)))
            elif self.Position < 0:
                self.BuyMarket(abs(float(self.Position)))

    def OnPositionReceived(self, position):
        super(e_skoch_open_strategy, self).OnPositionReceived(position)

        if self.Position == 0:
            if self._position_tracked:
                equity = self._baseline_equity
                if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
                    equity = float(self.Portfolio.CurrentValue)
                if equity >= self._entry_equity:
                    self._current_volume = self._initial_volume.Value
                else:
                    self._current_volume = self._current_volume * self._martingale_multiplier.Value
                self._baseline_equity = equity
                self._position_tracked = False
                self._reset_protection()
                self._active_long_entries = 0
                self._active_short_entries = 0
            else:
                if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
                    self._baseline_equity = float(self.Portfolio.CurrentValue)
        else:
            self._position_tracked = True
            if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
                self._entry_equity = float(self.Portfolio.CurrentValue)

    def _calculate_point_value(self):
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0.0:
            return 0.0
        decimals = self._count_decimals(step)
        return step * 10.0 if decimals == 3 or decimals == 5 else step

    @staticmethod
    def _count_decimals(value):
        value = abs(value)
        decimals = 0
        while value != int(value) and decimals < 10:
            value *= 10.0
            decimals += 1
        return decimals

    def CreateClone(self):
        return e_skoch_open_strategy()
