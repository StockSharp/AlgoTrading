import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class clouds_trade2_strategy(Strategy):
    # FractalTypes: 0=Up, 1=Down
    FRACTAL_UP = 0
    FRACTAL_DOWN = 1

    def __init__(self):
        super(clouds_trade2_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0)
        self._stop_loss_offset = self.Param("StopLossOffset", 0.005)
        self._take_profit_offset = self.Param("TakeProfitOffset", 0.005)
        self._trailing_stop_offset = self.Param("TrailingStopOffset", 0.0)
        self._trailing_step_offset = self.Param("TrailingStepOffset", 0.0005)
        self._min_profit_currency = self.Param("MinProfitCurrency", 10.0)
        self._min_profit_points = self.Param("MinProfitPoints", 0.001)
        self._use_fractals = self.Param("UseFractals", True)
        self._use_stochastic = self.Param("UseStochastic", False)
        self._one_trade_per_day = self.Param("OneTradePerDay", True)
        self._k_period = self.Param("KPeriod", 5)
        self._d_period = self.Param("DPeriod", 3)
        self._slowing_period = self.Param("SlowingPeriod", 3)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._stochastic = None

        # Stochastic history
        self._prior_k = 0.0
        self._prior_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_prior_stoch = False
        self._has_last_stoch = False

        # Fractal buffer (5-candle rolling window)
        self._h1 = 0.0
        self._h2 = 0.0
        self._h3 = 0.0
        self._h4 = 0.0
        self._h5 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._l4 = 0.0
        self._l5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_buffer_count = 0

        # Position management
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._last_entry_date = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(clouds_trade2_strategy, self).OnStarted2(time)

        if self._use_stochastic.Value:
            self._stochastic = StochasticOscillator()
            self._stochastic.K.Length = self._k_period.Value
            self._stochastic.D.Length = self._d_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Process stochastic manually if enabled
        stoch_signal = 0
        if self._use_stochastic.Value and self._stochastic is not None:
            stoch_result = self._stochastic.Process(candle)
            if not stoch_result.IsEmpty and self._stochastic.IsFormed:
                stoch_signal = self._evaluate_stochastic_signal(stoch_result)

        # Update fractals
        self._update_fractals(candle)
        fractal_signal = self._get_fractal_signal()

        # Handle open position
        self._handle_open_position(candle)

        # Determine combined signal
        signal = 0
        if stoch_signal == 2 or fractal_signal == 2:
            signal = 2
        elif stoch_signal == 1 or fractal_signal == 1:
            signal = 1

        if signal == 0:
            return

        if self.Position != 0:
            return

        if self._one_trade_per_day.Value and self._last_entry_date is not None and self._last_entry_date == candle.OpenTime.Date:
            return

        if signal == 1:
            self.BuyMarket(self._order_volume.Value)
            self._initialize_targets(float(candle.ClosePrice), True)
            self._last_entry_date = candle.OpenTime.Date
        elif signal == 2:
            self.SellMarket(self._order_volume.Value)
            self._initialize_targets(float(candle.ClosePrice), False)
            self._last_entry_date = candle.OpenTime.Date

    def _evaluate_stochastic_signal(self, stoch_value):
        # Extract K and D values from the complex indicator value
        try:
            current_k = float(stoch_value.GetValue[0]) if hasattr(stoch_value, 'GetValue') else None
            current_d = float(stoch_value.GetValue[1]) if hasattr(stoch_value, 'GetValue') else None
        except:
            try:
                current_k = float(stoch_value)
                current_d = None
            except:
                return 0

        if current_k is None:
            return 0
        if current_d is None:
            current_d = current_k

        if not self._has_last_stoch:
            self._last_k = current_k
            self._last_d = current_d
            self._has_last_stoch = True
            return 0

        if not self._has_prior_stoch:
            self._prior_k = self._last_k
            self._prior_d = self._last_d
            self._last_k = current_k
            self._last_d = current_d
            self._has_prior_stoch = True
            return 0

        sell_signal = self._last_d >= 80.0 and self._prior_d <= self._prior_k and self._last_d >= self._last_k
        buy_signal = self._last_d <= 20.0 and self._prior_d >= self._prior_k and self._last_d <= self._last_k

        self._prior_k = self._last_k
        self._prior_d = self._last_d
        self._last_k = current_k
        self._last_d = current_d

        if sell_signal:
            return 2
        if buy_signal:
            return 1
        return 0

    def _update_fractals(self, candle):
        # Shift the rolling window
        self._h1 = self._h2
        self._h2 = self._h3
        self._h3 = self._h4
        self._h4 = self._h5
        self._h5 = float(candle.HighPrice)

        self._l1 = self._l2
        self._l2 = self._l3
        self._l3 = self._l4
        self._l4 = self._l5
        self._l5 = float(candle.LowPrice)

        if self._fractal_buffer_count < 5:
            self._fractal_buffer_count += 1
            return

        up_fractal = self._h3 > self._h1 and self._h3 > self._h2 and self._h3 > self._h4 and self._h3 > self._h5
        down_fractal = self._l3 < self._l1 and self._l3 < self._l2 and self._l3 < self._l4 and self._l3 < self._l5

        if up_fractal:
            self._register_fractal(self.FRACTAL_UP)
        if down_fractal:
            self._register_fractal(self.FRACTAL_DOWN)

    def _get_fractal_signal(self):
        if not self._use_fractals.Value:
            return 0
        if self._latest_fractal is None or self._previous_fractal is None:
            return 0

        # Two consecutive up fractals = sell signal
        if self._latest_fractal == self.FRACTAL_UP and self._previous_fractal == self.FRACTAL_UP:
            return 2

        # Two consecutive down fractals = buy signal
        if self._latest_fractal == self.FRACTAL_DOWN and self._previous_fractal == self.FRACTAL_DOWN:
            return 1

        return 0

    def _register_fractal(self, fractal_type):
        self._previous_fractal = self._latest_fractal
        self._latest_fractal = fractal_type

    def _handle_open_position(self, candle):
        if self.Position == 0:
            return

        if self.Position > 0:
            self._update_trailing(candle, True)

            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(abs(self.Position))
                self._reset_trade_state()
                return

            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(abs(self.Position))
                self._reset_trade_state()
                return

            profit = (float(candle.ClosePrice) - self._entry_price) * self.Position
            price_gain = float(candle.ClosePrice) - self._entry_price

            if self._min_profit_currency.Value > 0 and profit >= self._min_profit_currency.Value:
                self.SellMarket(abs(self.Position))
                self._reset_trade_state()
                return

            if self._min_profit_points.Value > 0 and price_gain >= self._min_profit_points.Value:
                self.SellMarket(abs(self.Position))
                self._reset_trade_state()

        elif self.Position < 0:
            self._update_trailing(candle, False)

            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_trade_state()
                return

            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(abs(self.Position))
                self._reset_trade_state()
                return

            profit = (self._entry_price - float(candle.ClosePrice)) * abs(self.Position)
            price_gain = self._entry_price - float(candle.ClosePrice)

            if self._min_profit_currency.Value > 0 and profit >= self._min_profit_currency.Value:
                self.BuyMarket(abs(self.Position))
                self._reset_trade_state()
                return

            if self._min_profit_points.Value > 0 and price_gain >= self._min_profit_points.Value:
                self.BuyMarket(abs(self.Position))
                self._reset_trade_state()

    def _update_trailing(self, candle, is_long):
        if self._trailing_stop_offset.Value <= 0:
            return

        if is_long:
            profit_distance = float(candle.ClosePrice) - self._entry_price
            if profit_distance > self._trailing_stop_offset.Value + self._trailing_step_offset.Value:
                new_stop = float(candle.ClosePrice) - self._trailing_stop_offset.Value
                if self._stop_price is None or new_stop > self._stop_price + self._trailing_step_offset.Value:
                    self._stop_price = new_stop
        else:
            profit_distance = self._entry_price - float(candle.ClosePrice)
            if profit_distance > self._trailing_stop_offset.Value + self._trailing_step_offset.Value:
                new_stop = float(candle.ClosePrice) + self._trailing_stop_offset.Value
                if self._stop_price is None or new_stop < self._stop_price - self._trailing_step_offset.Value:
                    self._stop_price = new_stop

    def _initialize_targets(self, entry_price, is_long):
        self._entry_price = entry_price

        if is_long:
            self._stop_price = entry_price - self._stop_loss_offset.Value if self._stop_loss_offset.Value > 0 else None
            self._take_profit_price = entry_price + self._take_profit_offset.Value if self._take_profit_offset.Value > 0 else None
        else:
            self._stop_price = entry_price + self._stop_loss_offset.Value if self._stop_loss_offset.Value > 0 else None
            self._take_profit_price = entry_price - self._take_profit_offset.Value if self._take_profit_offset.Value > 0 else None

    def _reset_trade_state(self):
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0

    def OnReseted(self):
        super(clouds_trade2_strategy, self).OnReseted()
        self._stochastic = None
        self._prior_k = 0.0
        self._prior_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_prior_stoch = False
        self._has_last_stoch = False
        self._h1 = 0.0
        self._h2 = 0.0
        self._h3 = 0.0
        self._h4 = 0.0
        self._h5 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._l4 = 0.0
        self._l5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_buffer_count = 0
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._last_entry_date = None

    def CreateClone(self):
        return clouds_trade2_strategy()
