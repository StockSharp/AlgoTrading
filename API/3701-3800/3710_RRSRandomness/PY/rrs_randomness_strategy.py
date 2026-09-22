import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates, DataType, Level1Fields, Sides
from StockSharp.Algo.Strategies import Strategy


class rrs_randomness_strategy(Strategy):
    MODE_DOUBLE_SIDE = 0
    MODE_ONE_SIDE = 1
    RISK_FIXED_MONEY = 0
    RISK_BALANCE_PERCENTAGE = 1
    INITIAL_RANDOM_STATE = 3710

    def __init__(self):
        super(rrs_randomness_strategy, self).__init__()

        self._mode = self.Param("Mode", self.MODE_DOUBLE_SIDE) \
            .SetDisplay("Trading Mode", "Alternate every cycle or use random gated entries", "General")
        self._min_volume = self.Param("MinVolume", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Volume", "Minimum volume for a market order", "Lot Settings")
        self._max_volume = self.Param("MaxVolume", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Volume", "Maximum volume for a market order", "Lot Settings")
        self._tp_points = self.Param("TakeProfitPoints", 2000.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit", "TP in price steps", "Protection")
        self._sl_points = self.Param("StopLossPoints", 3000.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss", "SL in price steps", "Protection")
        self._trailing_start = self.Param("TrailingStartPoints", 1500.0) \
            .SetNotNegative() \
            .SetDisplay("Trailing Start", "Profit to enable trailing", "Protection")
        self._trailing_gap = self.Param("TrailingGapPoints", 1000.0) \
            .SetNotNegative() \
            .SetDisplay("Trailing Gap", "Trailing offset", "Protection")
        self._max_spread = self.Param("MaxSpreadPoints", 100.0) \
            .SetNotNegative() \
            .SetDisplay("Max Spread", "Maximum spread for new entries in price steps", "Filters")
        self._slippage = self.Param("SlippagePoints", 3.0) \
            .SetNotNegative() \
            .SetDisplay("Slippage", "Informational slippage in price steps", "Filters")
        self._risk_mode = self.Param("MoneyRiskMode", self.RISK_BALANCE_PERCENTAGE) \
            .SetDisplay("Risk Mode", "Fixed money or portfolio percentage", "Risk Management")
        self._risk_value = self.Param("RiskValue", 5.0) \
            .SetNotNegative() \
            .SetDisplay("Risk Value", "Risk amount in money or percent", "Risk Management")
        self._trade_comment = self.Param("TradeComment", "RRS") \
            .SetDisplay("Trade Comment", "Comment attached to generated orders", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._reset_runtime()

    @property
    def Mode(self):
        return self._mode.Value

    @Mode.setter
    def Mode(self, value):
        self._mode.Value = value

    @property
    def MinVolume(self):
        return self._min_volume.Value

    @MinVolume.setter
    def MinVolume(self, value):
        self._min_volume.Value = value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @MaxVolume.setter
    def MaxVolume(self, value):
        self._max_volume.Value = value

    @property
    def MaxSpreadPoints(self):
        return self._max_spread.Value

    @MaxSpreadPoints.setter
    def MaxSpreadPoints(self, value):
        self._max_spread.Value = value

    @property
    def MoneyRiskMode(self):
        return self._risk_mode.Value

    @MoneyRiskMode.setter
    def MoneyRiskMode(self, value):
        self._risk_mode.Value = value

    @property
    def RiskValue(self):
        return self._risk_value.Value

    @RiskValue.setter
    def RiskValue(self, value):
        self._risk_value.Value = value

    @property
    def TradeComment(self):
        return self._trade_comment.Value

    @TradeComment.setter
    def TradeComment(self, value):
        self._trade_comment.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(rrs_randomness_strategy, self).OnReseted()
        self._reset_runtime()

    def OnStarted2(self, time):
        super(rrs_randomness_strategy, self).OnStarted2(time)

        if float(self._max_volume.Value) < float(self._min_volume.Value):
            raise ValueError("MaxVolume cannot be less than MinVolume.")

        self._reset_runtime()
        self._update_instrument_values()

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.OnProcess) \
            .Start()
        self.SubscribeLevel1() \
            .Bind(self._process_level1) \
            .Start()

    def _reset_runtime(self):
        self._random_state = self.INITIAL_RANDOM_STATE
        self._trailing_stop = None
        self._open_long_next = True
        self._entry_price = 0.0
        self._best_bid = None
        self._best_ask = None
        self._price_step = 0.0
        self._step_price = 0.0

    def _update_instrument_values(self):
        if self.Security is None:
            return

        price_step = self.Security.PriceStep
        if price_step is not None and price_step > 0:
            self._price_step = float(price_step)

    def _process_level1(self, message):
        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)
        price_step = message.TryGetDecimal(Level1Fields.PriceStep)
        step_price = message.TryGetDecimal(Level1Fields.StepPrice)

        if bid is not None and bid > 0:
            self._best_bid = float(bid)
        if ask is not None and ask > 0:
            self._best_ask = float(ask)
        if price_step is not None and price_step > 0:
            self._price_step = float(price_step)
        if step_price is not None and step_price > 0:
            self._step_price = float(step_price)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        if self._apply_protection(close):
            return
        if self._apply_trailing(close):
            return
        if self._apply_risk_control(close):
            return

        self._try_open_trade()

    def _apply_protection(self, price):
        if self.Position == 0 or self._entry_price <= 0:
            return False

        step = self._get_price_step()
        stop_points = float(self._sl_points.Value)
        take_points = float(self._tp_points.Value)

        if self.Position > 0:
            if stop_points > 0 and price <= self._entry_price - stop_points * step:
                return self._close_position("Stop loss")
            if take_points > 0 and price >= self._entry_price + take_points * step:
                return self._close_position("Take profit")
        else:
            if stop_points > 0 and price >= self._entry_price + stop_points * step:
                return self._close_position("Stop loss")
            if take_points > 0 and price <= self._entry_price - take_points * step:
                return self._close_position("Take profit")

        return False

    def _apply_trailing(self, price):
        start_points = float(self._trailing_start.Value)
        gap_points = float(self._trailing_gap.Value)
        if self.Position == 0 or start_points <= 0 or gap_points <= 0:
            self._trailing_stop = None
            return False
        if self._entry_price <= 0:
            return False

        step = self._get_price_step()
        gap = gap_points * step
        trigger = (start_points + gap_points) * step

        if self.Position > 0:
            if price - self._entry_price >= trigger:
                candidate = price - gap
                if self._trailing_stop is None or candidate > self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price <= self._trailing_stop:
                return self._close_position("Trailing stop")
        else:
            if self._entry_price - price >= trigger:
                candidate = price + gap
                if self._trailing_stop is None or candidate < self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price >= self._trailing_stop:
                return self._close_position("Trailing stop")

        return False

    def _apply_risk_control(self, price):
        if self.Position == 0 or self._entry_price <= 0:
            return False

        risk_limit = self._get_risk_limit()
        if risk_limit is None:
            return False

        liquidation_price = price
        if self.Position > 0 and self._best_bid is not None:
            liquidation_price = self._best_bid
        elif self.Position < 0 and self._best_ask is not None:
            liquidation_price = self._best_ask

        if self._calculate_floating_pnl(liquidation_price) <= -risk_limit:
            return self._close_position("Risk control")
        return False

    def _get_risk_limit(self):
        risk = abs(float(self._risk_value.Value))
        if int(self._risk_mode.Value) == self.RISK_FIXED_MONEY:
            return risk

        portfolio_value = 0.0
        if self.Portfolio is not None:
            value = self.Portfolio.CurrentValue
            if value is None or value <= 0:
                value = self.Portfolio.BeginValue
            if value is not None:
                portfolio_value = float(value)

        return portfolio_value * risk / 100.0 if portfolio_value > 0 else None

    def _calculate_floating_pnl(self, market_price):
        direction = 1.0 if self.Position > 0 else -1.0
        difference = (market_price - self._entry_price) * direction
        volume = float(abs(self.Position))

        if self._step_price > 0:
            return difference / self._get_price_step() * self._step_price * volume

        multiplier = 1.0
        if self.Security is not None and self.Security.Multiplier is not None:
            multiplier = float(self.Security.Multiplier)
        return difference * multiplier * volume

    def OnOwnTradeReceived(self, trade):
        super(rrs_randomness_strategy, self).OnOwnTradeReceived(trade)

        if self.Position != 0 and self._entry_price <= 0:
            self._entry_price = float(trade.Trade.TradePrice)
        if self.Position == 0:
            self._entry_price = 0.0
            self._trailing_stop = None

    def _close_position(self, reason):
        volume = abs(self.Position)
        if volume <= 0:
            return False

        side = Sides.Sell if self.Position > 0 else Sides.Buy
        self._submit_market(side, volume, reason)
        self._trailing_stop = None
        return True

    def _try_open_trade(self):
        if self.Position != 0 or not self._is_spread_allowed():
            return

        side = None
        mode = int(self._mode.Value)
        if mode == self.MODE_DOUBLE_SIDE:
            side = Sides.Buy if self._open_long_next else Sides.Sell
            self._open_long_next = not self._open_long_next
        elif mode == self.MODE_ONE_SIDE:
            random_value = self._next_random_int(6)
            if random_value == 1 or random_value == 4:
                side = Sides.Buy
            elif random_value == 0 or random_value == 3:
                side = Sides.Sell

        if side is None:
            return

        volume = self._generate_volume()
        if volume <= 0:
            return

        self._submit_market(side, volume, None)

    def _is_spread_allowed(self):
        max_spread = float(self._max_spread.Value)
        if max_spread <= 0:
            return False
        if self._best_bid is None or self._best_ask is None:
            return True
        if self._best_ask < self._best_bid:
            return False
        spread_points = (self._best_ask - self._best_bid) / self._get_price_step()
        return spread_points <= max_spread

    def _generate_volume(self):
        security_min = 0.0
        security_max = float("inf")
        step = 0.0

        if self.Security is not None:
            if self.Security.MinVolume is not None:
                security_min = float(self.Security.MinVolume)
            if self.Security.MaxVolume is not None:
                security_max = float(self.Security.MaxVolume)
            if self.Security.VolumeStep is not None:
                step = float(self.Security.VolumeStep)

        minimum = max(float(self._min_volume.Value), security_min)
        maximum = min(float(self._max_volume.Value), security_max)
        if maximum < minimum:
            return 0.0

        raw = minimum if minimum == maximum else minimum + (maximum - minimum) * self._next_random_unit()
        if step <= 0:
            return raw

        first = int(-(-minimum // step)) * step
        last = int(maximum // step) * step
        if first > last:
            return 0.0

        aligned = int(raw // step) * step
        return round(max(first, min(aligned, last)), 12)

    def _get_price_step(self):
        return self._price_step if self._price_step > 0 else 0.0001

    def _next_random_unit(self):
        self._random_state = (self._random_state * 1664525 + 1013904223) & 0xFFFFFFFF
        return float(self._random_state) / 4294967296.0

    def _next_random_int(self, maximum):
        return int(self._next_random_unit() * maximum)

    def _submit_market(self, side, volume, reason):
        order = self.CreateOrder(side, 0, volume)
        base_comment = self._trade_comment.Value
        if reason is None or reason == "":
            order.Comment = base_comment
        elif base_comment is None or base_comment == "":
            order.Comment = reason
        else:
            order.Comment = "{0}: {1}".format(base_comment, reason)
        self.RegisterOrder(order)

    def CreateClone(self):
        return rrs_randomness_strategy()
