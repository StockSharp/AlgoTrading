import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fractured_fractals_strategy(Strategy):

    def __init__(self):
        super(fractured_fractals_strategy, self).__init__()
        self._maximum_risk_percent = self.Param("MaximumRiskPercent", 2.0)
        self._decrease_factor = self.Param("DecreaseFactor", 10.0)
        self._expiration_hours = self.Param("ExpirationHours", 1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._high_buffer = []
        self._low_buffer = []
        self._last_up_fractal = None
        self._last_down_fractal = None
        self._up_youngest = None
        self._up_middle = None
        self._up_old = None
        self._down_youngest = None
        self._down_middle = None
        self._down_old = None
        self._buy_stop_level = None
        self._sell_stop_level = None
        self._long_stop_level = None
        self._short_stop_level = None
        self._buy_stop_expiry = None
        self._sell_stop_expiry = None
        self._buy_stop_volume = 0.0
        self._sell_stop_volume = 0.0
        self._entry_price = 0.0
        self._consecutive_losses = 0

    @property
    def MaximumRiskPercent(self):
        return self._maximum_risk_percent.Value

    @property
    def DecreaseFactor(self):
        return self._decrease_factor.Value

    @property
    def ExpirationHours(self):
        return self._expiration_hours.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(fractured_fractals_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnOwnTradeReceived(self, trade):
        super(fractured_fractals_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos != 0 and self._entry_price == 0.0:
            self._entry_price = float(trade.Trade.Price)
        if pos == 0:
            self._entry_price = 0.0

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._high_buffer.append(float(candle.HighPrice))
        self._low_buffer.append(float(candle.LowPrice))

        if len(self._high_buffer) > 5:
            self._high_buffer.pop(0)
        if len(self._low_buffer) > 5:
            self._low_buffer.pop(0)

        if len(self._high_buffer) < 5 or len(self._low_buffer) < 5:
            return

        self._detect_fractals()
        self._check_protective_stops(candle)
        self._validate_pending_levels(candle.CloseTime)
        self._check_pending_triggers(candle)
        self._update_trailing_stops()

        if float(self.Position) == 0:
            if not self._try_set_buy_stop_level(candle.CloseTime):
                self._try_set_sell_stop_level(candle.CloseTime)

    def _detect_fractals(self):
        highs = list(self._high_buffer)
        lows = list(self._low_buffer)

        up_fractal = None
        down_fractal = None

        if highs[2] > highs[0] and highs[2] > highs[1] and highs[2] > highs[3] and highs[2] > highs[4]:
            up_fractal = highs[2]

        if lows[2] < lows[0] and lows[2] < lows[1] and lows[2] < lows[3] and lows[2] < lows[4]:
            down_fractal = lows[2]

        if up_fractal is not None and not self._are_equal(self._last_up_fractal, up_fractal):
            self._last_up_fractal = up_fractal
            self._up_old = self._up_middle
            self._up_middle = self._up_youngest
            self._up_youngest = up_fractal

        if down_fractal is not None and not self._are_equal(self._last_down_fractal, down_fractal):
            self._last_down_fractal = down_fractal
            self._down_old = self._down_middle
            self._down_middle = self._down_youngest
            self._down_youngest = down_fractal

    def _check_protective_stops(self, candle):
        pos = float(self.Position)
        if pos > 0 and self._long_stop_level is not None:
            if float(candle.LowPrice) <= self._long_stop_level:
                self.SellMarket(abs(pos))
                self._long_stop_level = None
                self._consecutive_losses += 1
                return

        if pos < 0 and self._short_stop_level is not None:
            if float(candle.HighPrice) >= self._short_stop_level:
                self.BuyMarket(abs(pos))
                self._short_stop_level = None
                self._consecutive_losses += 1
                return

    def _update_trailing_stops(self):
        pos = float(self.Position)
        if pos > 0 and self._down_youngest is not None:
            if self._long_stop_level is None or self._down_youngest > self._long_stop_level:
                self._long_stop_level = self._down_youngest
        elif pos <= 0:
            self._long_stop_level = None

        if pos < 0 and self._up_youngest is not None:
            if self._short_stop_level is None or self._up_youngest < self._short_stop_level:
                self._short_stop_level = self._up_youngest
        elif pos >= 0:
            self._short_stop_level = None

    def _validate_pending_levels(self, current_time):
        if self._buy_stop_level is not None and self._up_youngest is not None:
            if self._up_youngest < self._buy_stop_level and not self._are_equal(self._up_youngest, self._buy_stop_level):
                self._buy_stop_level = None
                self._buy_stop_expiry = None

        if self._sell_stop_level is not None and self._down_youngest is not None:
            if self._down_youngest > self._sell_stop_level and not self._are_equal(self._down_youngest, self._sell_stop_level):
                self._sell_stop_level = None
                self._sell_stop_expiry = None

        if self._buy_stop_level is not None and self._buy_stop_expiry is not None and current_time >= self._buy_stop_expiry:
            self._buy_stop_level = None
            self._buy_stop_expiry = None

        if self._sell_stop_level is not None and self._sell_stop_expiry is not None and current_time >= self._sell_stop_expiry:
            self._sell_stop_level = None
            self._sell_stop_expiry = None

        if float(self.Position) != 0:
            self._buy_stop_level = None
            self._sell_stop_level = None
            self._buy_stop_expiry = None
            self._sell_stop_expiry = None

    def _check_pending_triggers(self, candle):
        pos = float(self.Position)
        if self._buy_stop_level is not None and float(candle.HighPrice) >= self._buy_stop_level and pos <= 0:
            buy_level = self._buy_stop_level
            vol = self._buy_stop_volume if self._buy_stop_volume > 0 else float(self.Volume)
            if vol > 0:
                if pos < 0:
                    self.BuyMarket(abs(pos))
                self.BuyMarket(vol)
                self._entry_price = buy_level
                self._long_stop_level = self._down_youngest
            self._buy_stop_level = None
            self._buy_stop_expiry = None

        pos = float(self.Position)
        if self._sell_stop_level is not None and float(candle.LowPrice) <= self._sell_stop_level and pos >= 0:
            sell_level = self._sell_stop_level
            vol = self._sell_stop_volume if self._sell_stop_volume > 0 else float(self.Volume)
            if vol > 0:
                if pos > 0:
                    self.SellMarket(abs(pos))
                self.SellMarket(vol)
                self._entry_price = sell_level
                self._short_stop_level = self._up_youngest
            self._sell_stop_level = None
            self._sell_stop_expiry = None

    def _try_set_buy_stop_level(self, time):
        pos = float(self.Position)
        if pos > 0 or self._buy_stop_level is not None:
            return False

        if self._up_youngest is None or self._up_middle is None or self._down_youngest is None:
            return False

        up = self._up_youngest
        middle = self._up_middle
        stop = self._down_youngest

        if up <= middle or stop >= up:
            return False

        volume = self._calculate_order_volume(up, stop, True)
        if volume <= 0:
            return False

        self._buy_stop_level = up
        self._buy_stop_volume = volume
        if self.ExpirationHours > 0:
            self._buy_stop_expiry = time + TimeSpan.FromHours(self.ExpirationHours)
        else:
            self._buy_stop_expiry = None
        return True

    def _try_set_sell_stop_level(self, time):
        pos = float(self.Position)
        if pos < 0 or self._sell_stop_level is not None:
            return

        if self._down_youngest is None or self._down_middle is None or self._up_youngest is None:
            return

        down = self._down_youngest
        middle = self._down_middle
        stop = self._up_youngest

        if down >= middle or stop <= down:
            return

        volume = self._calculate_order_volume(down, stop, False)
        if volume <= 0:
            return

        self._sell_stop_level = down
        self._sell_stop_volume = volume
        if self.ExpirationHours > 0:
            self._sell_stop_expiry = time + TimeSpan.FromHours(self.ExpirationHours)
        else:
            self._sell_stop_expiry = None

    def _calculate_order_volume(self, entry_price, stop_price, is_buy):
        if is_buy:
            risk_per_unit = entry_price - stop_price
        else:
            risk_per_unit = stop_price - entry_price

        if risk_per_unit <= 0:
            return 0.0

        portfolio = self.Portfolio
        portfolio_value = 0.0
        if portfolio is not None and portfolio.CurrentValue is not None:
            portfolio_value = float(portfolio.CurrentValue)
        if portfolio_value <= 0:
            vol = float(self.Volume)
            portfolio_value = vol * entry_price if vol > 0 else 0.0

        risk_amount = portfolio_value * (float(self.MaximumRiskPercent) / 100.0)
        if risk_amount <= 0:
            return 0.0

        volume = risk_amount / risk_per_unit

        if float(self.DecreaseFactor) > 0 and self._consecutive_losses > 1:
            volume -= volume * (self._consecutive_losses / float(self.DecreaseFactor))

        if volume <= 0:
            return 0.0

        min_vol = float(self.Volume) if float(self.Volume) > 0 else 1.0
        return max(volume, min_vol)

    def _are_equal(self, first, second):
        if first is None:
            return False
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.00000001
        return abs(first - second) <= step / 2.0

    def OnReseted(self):
        super(fractured_fractals_strategy, self).OnReseted()
        self._high_buffer = []
        self._low_buffer = []
        self._last_up_fractal = None
        self._last_down_fractal = None
        self._up_youngest = None
        self._up_middle = None
        self._up_old = None
        self._down_youngest = None
        self._down_middle = None
        self._down_old = None
        self._buy_stop_level = None
        self._sell_stop_level = None
        self._long_stop_level = None
        self._short_stop_level = None
        self._buy_stop_expiry = None
        self._sell_stop_expiry = None
        self._buy_stop_volume = 0.0
        self._sell_stop_volume = 0.0
        self._entry_price = 0.0
        self._consecutive_losses = 0

    def CreateClone(self):
        return fractured_fractals_strategy()
