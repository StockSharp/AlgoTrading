import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class cloudzs_trade2_strategy(Strategy):
    """Stochastic reversals combined with fractal confirmations. Mirrors the
    original cloudzs trade 2 MetaTrader expert with trailing stop management."""

    # Fractal type constants
    FRACTAL_UP = 0
    FRACTAL_DOWN = 1

    def __init__(self):
        super(cloudzs_trade2_strategy, self).__init__()

        self._lot_splitter = self.Param("LotSplitter", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading")
        self._max_volume = self.Param("MaxVolume", 0.0) \
            .SetDisplay("Max Volume", "Maximum volume limit (0 disables the cap)", "Trading")
        self._take_profit_offset = self.Param("TakeProfitOffset", 0.0) \
            .SetDisplay("Take Profit", "Take profit distance in price units", "Risk")
        self._trailing_stop_offset = self.Param("TrailingStopOffset", 0.01) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk")
        self._stop_loss_offset = self.Param("StopLossOffset", 0.05) \
            .SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk")
        self._min_profit_offset = self.Param("MinProfitOffset", 0.0) \
            .SetDisplay("Min Profit", "Minimum profit to keep after pullback", "Risk")
        self._profit_points_offset = self.Param("ProfitPointsOffset", 0.0) \
            .SetDisplay("Profit Points", "Favorable move required before min profit rule", "Risk")
        self._k_period = self.Param("KPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("%K Period", "Base length of the stochastic oscillator", "Indicators")
        self._d_period = self.Param("DPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("%D Period", "Smoothing length for the stochastic signal", "Indicators")
        self._slowing_period = self.Param("SlowingPeriod", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("Slowing", "Additional smoothing length for %K", "Indicators")
        self._method = self.Param("Method", 3) \
            .SetDisplay("Method", "Original MQL MA method identifier", "Indicators")
        self._price_mode = self.Param("PriceMode", 1) \
            .SetDisplay("Price Mode", "Original MQL price mode identifier", "Indicators")
        self._use_stochastic_condition = self.Param("UseStochasticCondition", True) \
            .SetDisplay("Use Stochastic", "Enable stochastic reversal filter", "Signals")
        self._use_fractal_condition = self.Param("UseFractalCondition", True) \
            .SetDisplay("Use Fractals", "Enable double fractal confirmation", "Signals")
        self._close_on_opposite = self.Param("CloseOnOpposite", True) \
            .SetDisplay("Close On Opposite", "Exit when the opposite signal fires", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe for the strategy", "General")

        self._stochastic = None
        self._previous_k = 0.0
        self._previous_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_previous = False
        self._has_last = False

        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._high5 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._low4 = 0.0
        self._low5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_seed_count = 0

        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._max_favorable_move = 0.0
        self._last_exit_date = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LotSplitter(self):
        return self._lot_splitter.Value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @property
    def TakeProfitOffset(self):
        return self._take_profit_offset.Value

    @property
    def TrailingStopOffset(self):
        return self._trailing_stop_offset.Value

    @property
    def StopLossOffset(self):
        return self._stop_loss_offset.Value

    @property
    def MinProfitOffset(self):
        return self._min_profit_offset.Value

    @property
    def ProfitPointsOffset(self):
        return self._profit_points_offset.Value

    @property
    def KPeriod(self):
        return self._k_period.Value

    @property
    def DPeriod(self):
        return self._d_period.Value

    @property
    def SlowingPeriod(self):
        return self._slowing_period.Value

    @property
    def UseStochasticCondition(self):
        return self._use_stochastic_condition.Value

    @property
    def UseFractalCondition(self):
        return self._use_fractal_condition.Value

    @property
    def CloseOnOpposite(self):
        return self._close_on_opposite.Value

    def OnReseted(self):
        super(cloudzs_trade2_strategy, self).OnReseted()
        self._stochastic = None
        self._previous_k = 0.0
        self._previous_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_previous = False
        self._has_last = False

        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._high5 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._low4 = 0.0
        self._low5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_seed_count = 0

        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._max_favorable_move = 0.0
        self._last_exit_date = None

    def OnStarted2(self, time):
        super(cloudzs_trade2_strategy, self).OnStarted2(time)

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.KPeriod
        self._stochastic.D.Length = self.DPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self._process_candle).Start()

    def _process_candle(self, candle, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_fractals(candle)

        stochastic_signal = self._evaluate_stochastic_signal(stochastic_value) \
            if self.UseStochasticCondition else 0
        fractal_signal = self._evaluate_fractal_signal() \
            if self.UseFractalCondition else 0

        combined_signal = 0
        if stochastic_signal == 2 or fractal_signal == 2:
            combined_signal = 2
        elif stochastic_signal == 1 or fractal_signal == 1:
            combined_signal = 1

        self._manage_open_position(candle, combined_signal)

        if self.Position != 0:
            return

        if self._last_exit_date is not None and self._last_exit_date == candle.OpenTime.Date:
            return

        if combined_signal == 0:
            return

        volume = self._calculate_order_volume(float(candle.ClosePrice))
        if volume <= 0:
            return

        self.Volume = volume

        if combined_signal == 1:
            self.BuyMarket()
            self._initialize_targets(float(candle.ClosePrice), True)
        elif combined_signal == 2:
            self.SellMarket()
            self._initialize_targets(float(candle.ClosePrice), False)

    def _evaluate_stochastic_signal(self, stochastic_value):
        if self._stochastic is None:
            return 0

        k_raw = stochastic_value.K
        d_raw = stochastic_value.D

        if k_raw is None or d_raw is None:
            return 0

        current_k = float(k_raw)
        current_d = float(d_raw)

        if not self._has_last:
            self._last_k = current_k
            self._last_d = current_d
            self._has_last = True
            return 0

        if not self._has_previous:
            self._previous_k = self._last_k
            self._previous_d = self._last_d
            self._last_k = current_k
            self._last_d = current_d
            self._has_previous = True
            return 0

        sell_signal = self._last_d >= 80 and self._previous_d <= self._previous_k and self._last_d >= self._last_k
        buy_signal = self._last_d <= 20 and self._previous_d >= self._previous_k and self._last_d <= self._last_k

        self._previous_k = self._last_k
        self._previous_d = self._last_d
        self._last_k = current_k
        self._last_d = current_d

        if sell_signal:
            return 2
        if buy_signal:
            return 1
        return 0

    def _update_fractals(self, candle):
        self._high1 = self._high2
        self._high2 = self._high3
        self._high3 = self._high4
        self._high4 = self._high5
        self._high5 = float(candle.HighPrice)

        self._low1 = self._low2
        self._low2 = self._low3
        self._low3 = self._low4
        self._low4 = self._low5
        self._low5 = float(candle.LowPrice)

        if self._fractal_seed_count < 5:
            self._fractal_seed_count += 1
            return

        up_fractal = self._high3 > self._high1 and self._high3 > self._high2 and \
            self._high3 > self._high4 and self._high3 > self._high5
        down_fractal = self._low3 < self._low1 and self._low3 < self._low2 and \
            self._low3 < self._low4 and self._low3 < self._low5

        if up_fractal:
            self._register_fractal(self.FRACTAL_UP)
        if down_fractal:
            self._register_fractal(self.FRACTAL_DOWN)

    def _register_fractal(self, fractal_type):
        self._previous_fractal = self._latest_fractal
        self._latest_fractal = fractal_type

    def _evaluate_fractal_signal(self):
        if self._latest_fractal is None or self._previous_fractal is None:
            return 0

        if self._latest_fractal == self.FRACTAL_UP and self._previous_fractal == self.FRACTAL_UP:
            return 2  # sell
        if self._latest_fractal == self.FRACTAL_DOWN and self._previous_fractal == self.FRACTAL_DOWN:
            return 1  # buy
        return 0

    def _manage_open_position(self, candle, combined_signal):
        if self.Position == 0:
            return

        if self.Position > 0:
            self._manage_long_position(candle, combined_signal)
        else:
            self._manage_short_position(candle, combined_signal)

    def _manage_long_position(self, candle, combined_signal):
        self._update_trailing_stop(candle, True)

        if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
            self.SellMarket()
            self._finalize_exit(candle)
            return

        if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
            self.SellMarket()
            self._finalize_exit(candle)
            return

        current_gain = float(candle.ClosePrice) - self._entry_price
        favorable = float(candle.HighPrice) - self._entry_price
        if favorable > self._max_favorable_move:
            self._max_favorable_move = favorable

        pp_offset = float(self.ProfitPointsOffset)
        mp_offset = float(self.MinProfitOffset)
        if pp_offset > 0 and self._max_favorable_move >= pp_offset and current_gain <= mp_offset:
            self.SellMarket()
            self._finalize_exit(candle)
            return

        if self.CloseOnOpposite and combined_signal == 2:
            self.SellMarket()
            self._finalize_exit(candle)

    def _manage_short_position(self, candle, combined_signal):
        self._update_trailing_stop(candle, False)

        if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket()
            self._finalize_exit(candle)
            return

        if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
            self.BuyMarket()
            self._finalize_exit(candle)
            return

        current_gain = self._entry_price - float(candle.ClosePrice)
        favorable = self._entry_price - float(candle.LowPrice)
        if favorable > self._max_favorable_move:
            self._max_favorable_move = favorable

        pp_offset = float(self.ProfitPointsOffset)
        mp_offset = float(self.MinProfitOffset)
        if pp_offset > 0 and self._max_favorable_move >= pp_offset and current_gain <= mp_offset:
            self.BuyMarket()
            self._finalize_exit(candle)
            return

        if self.CloseOnOpposite and combined_signal == 1:
            self.BuyMarket()
            self._finalize_exit(candle)

    def _update_trailing_stop(self, candle, is_long):
        trailing = float(self.TrailingStopOffset)
        if trailing <= 0:
            return

        if is_long:
            potential_stop = float(candle.ClosePrice) - trailing
            if self._stop_price is None or potential_stop > self._stop_price:
                if potential_stop > self._entry_price:
                    self._stop_price = potential_stop
        else:
            potential_stop = float(candle.ClosePrice) + trailing
            if self._stop_price is None or potential_stop < self._stop_price:
                if potential_stop < self._entry_price:
                    self._stop_price = potential_stop

    def _initialize_targets(self, entry_price, is_long):
        self._entry_price = entry_price
        self._max_favorable_move = 0.0

        sl_offset = float(self.StopLossOffset)
        tp_offset = float(self.TakeProfitOffset)

        if sl_offset > 0:
            self._stop_price = entry_price - sl_offset if is_long else entry_price + sl_offset
        else:
            self._stop_price = None

        if tp_offset > 0:
            self._take_profit_price = entry_price + tp_offset if is_long else entry_price - tp_offset
        else:
            self._take_profit_price = None

    def _finalize_exit(self, candle):
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._max_favorable_move = 0.0
        self._last_exit_date = candle.OpenTime.Date

    def _calculate_order_volume(self, price):
        if price <= 0:
            return float(self.LotSplitter)

        lot_splitter = float(self.LotSplitter)
        estimated = lot_splitter
        normalized = Math.Floor(estimated * 10.0) / 10.0

        if normalized <= 0:
            normalized = lot_splitter

        max_vol = float(self.MaxVolume)
        if max_vol > 0 and normalized > max_vol:
            normalized = max_vol

        return normalized

    def CreateClone(self):
        return cloudzs_trade2_strategy()
