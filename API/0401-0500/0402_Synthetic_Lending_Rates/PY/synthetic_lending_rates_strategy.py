import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal, InvalidOperationException
from StockSharp.Messages import DataType, OrderStates
from StockSharp.Algo.Strategies import Strategy, StrategyTradingModes
from StockSharp.BusinessEntities import Security

# Roles an instrument plays; one instrument may play several.
_FUNDING_RATE = 1 << 0
_LENDING_RATE = 1 << 1
_DERIVATIVE_LEG = 1 << 2
_ON_CHAIN_LEG = 1 << 3
_LEGS = _DERIVATIVE_LEG | _ON_CHAIN_LEG


def _lot_size(security):
    multiplier = security.Multiplier
    return multiplier if multiplier is not None and multiplier > Decimal.Zero else Decimal.One


def _traded_value(bar, leg, period):
    # A leg without a bar of the period traded nothing in it.
    if bar is None or bar.OpenTime != period:
        return Decimal.Zero

    return bar.TotalVolume * bar.ClosePrice * _lot_size(leg)


class synthetic_lending_rates_strategy(Strategy):
    """
    Captures the spread between the synthetic lending rate implied by perpetual swap funding and an
    on-chain lending yield: it lends in the venue that pays more, borrows in the one that pays less
    and keeps the two legs dollar-neutral.

    Security is the derivative market's leg and OnChainLegSecurity the DeFi platform's; buying a leg
    lends in its venue and selling it borrows there. Each rate counts with the latest close of its
    series, whenever that series last printed, and the pair is judged once per period of the legs.
    The spread thresholds are in the units of the two rate series; the defaults assume both are
    quoted as annualized percent.
    """

    def __init__(self):
        super(synthetic_lending_rates_strategy, self).__init__()

        self._funding_rate_security = self.Param[Security]("FundingRateSecurity", None) \
            .SetDisplay("Funding Rate", "Synthetic lending rate implied by perpetual swap funding", "Rates") \
            .SetRequired()

        self._lending_rate_security = self.Param[Security]("LendingRateSecurity", None) \
            .SetDisplay("Lending Rate", "Lending yield of the DeFi platform", "Rates") \
            .SetRequired()

        self._on_chain_leg_security = self.Param[Security]("OnChainLegSecurity", None) \
            .SetDisplay("On-Chain Leg", "Instrument bought to lend and sold to borrow on the DeFi platform", "Legs") \
            .SetRequired()

        self._entry_threshold = self.Param("EntryThreshold", Decimal(5)) \
            .SetNotNegative() \
            .SetDisplay("Entry Threshold", "Rate spread above which the pair is opened", "Spread")

        self._exit_threshold = self.Param("ExitThreshold", Decimal(1)) \
            .SetDisplay("Exit Threshold", "Spread in favour of the open pair at or below which it has reverted", "Spread")

        self._spread_cap = self.Param("SpreadCap", Decimal(50)) \
            .SetGreaterThanZero() \
            .SetDisplay("Spread Cap", "Spread at or above which the pair is closed and none is opened", "Risk")

        self._min_leg_turnover = self.Param("MinLegTurnover", Decimal(1000000)) \
            .SetNotNegative() \
            .SetDisplay("Min Leg Turnover", "Value each leg must trade in a bar; below it the liquidity stop closes the pair", "Risk")

        self._leg_notional = self.Param("LegNotional", Decimal(10000)) \
            .SetGreaterThanZero() \
            .SetDisplay("Leg Notional", "Value held long and short in each leg", "Trading")

        self._rebalance_bars = self.Param("RebalanceBars", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("Rebalance Bars", "Bars between resizing both legs back to the leg notional", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time-frame bars sampled from both rate series and both legs", "General")

        self._reset_state()

    @property
    def FundingRateSecurity(self):
        return self._funding_rate_security.Value

    @FundingRateSecurity.setter
    def FundingRateSecurity(self, value):
        self._funding_rate_security.Value = value

    @property
    def LendingRateSecurity(self):
        return self._lending_rate_security.Value

    @LendingRateSecurity.setter
    def LendingRateSecurity(self, value):
        self._lending_rate_security.Value = value

    @property
    def OnChainLegSecurity(self):
        return self._on_chain_leg_security.Value

    @OnChainLegSecurity.setter
    def OnChainLegSecurity(self, value):
        self._on_chain_leg_security.Value = value

    @property
    def EntryThreshold(self):
        return self._entry_threshold.Value

    @EntryThreshold.setter
    def EntryThreshold(self, value):
        self._entry_threshold.Value = value

    @property
    def ExitThreshold(self):
        return self._exit_threshold.Value

    @ExitThreshold.setter
    def ExitThreshold(self, value):
        self._exit_threshold.Value = value

    @property
    def SpreadCap(self):
        return self._spread_cap.Value

    @SpreadCap.setter
    def SpreadCap(self, value):
        self._spread_cap.Value = value

    @property
    def MinLegTurnover(self):
        return self._min_leg_turnover.Value

    @MinLegTurnover.setter
    def MinLegTurnover(self, value):
        self._min_leg_turnover.Value = value

    @property
    def LegNotional(self):
        return self._leg_notional.Value

    @LegNotional.setter
    def LegNotional(self, value):
        self._leg_notional.Value = value

    @property
    def RebalanceBars(self):
        return self._rebalance_bars.Value

    @RebalanceBars.setter
    def RebalanceBars(self, value):
        self._rebalance_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(security, self.CandleType) for security in self._streams()]

    def OnReseted(self):
        super(synthetic_lending_rates_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._funding_bar = None
        self._lending_bar = None
        self._derivative_bar = None
        self._on_chain_bar = None
        self._period = None
        self._bars_since_sizing = 0

    def OnStarted2(self, time):
        # Reject before subscribing: a missing instrument would silently fall back to Security.
        self._validate_inputs()

        super(synthetic_lending_rates_strategy, self).OnStarted2(time)

        derivative_subscription = None

        # One subscription per instrument, even when it plays several roles. Unfinished bars are taken too:
        # a bar built from trades is marked finished only by the instrument's next trade, however late.
        for stream in self._streams():
            roles = self._roles_of(stream)
            # The second positional argument is isFinishedOnly.
            subscription = self.SubscribeCandles(self.CandleType, False, stream)
            subscription.Bind(self._stream_handler(roles)).Start()

            if (roles & _DERIVATIVE_LEG) != 0:
                derivative_subscription = subscription

        area = self.CreateChartArea()

        if area is not None:
            self.DrawCandles(area, derivative_subscription)
            self.DrawOwnTrades(area)

    def _validate_inputs(self):
        if self.Security is None:
            raise InvalidOperationException("Security must be set to the derivative market's leg.")

        if self.FundingRateSecurity is None:
            raise InvalidOperationException("FundingRateSecurity must be set.")

        if self.LendingRateSecurity is None:
            raise InvalidOperationException("LendingRateSecurity must be set.")

        if self.OnChainLegSecurity is None:
            raise InvalidOperationException("OnChainLegSecurity must be set.")

        if self._is_same(self.FundingRateSecurity, self.LendingRateSecurity):
            raise InvalidOperationException("LendingRateSecurity must be a different rate series than FundingRateSecurity.")

        if self._is_same(self.OnChainLegSecurity, self.Security):
            raise InvalidOperationException("OnChainLegSecurity must be a different instrument than Security.")

        if self.CandleType is None or not self.CandleType.IsTFCandles:
            raise InvalidOperationException("CandleType must be a time-frame candle type, so that all four streams share periods.")

        if self.ExitThreshold >= self.EntryThreshold:
            raise InvalidOperationException("ExitThreshold must be below EntryThreshold.")

        if self.SpreadCap <= self.EntryThreshold:
            raise InvalidOperationException("SpreadCap must be above EntryThreshold.")

    def _stream_handler(self, roles):
        return lambda candle: self._process_candle(roles, candle)

    def _process_candle(self, roles, candle):
        # Streams arrive in time order, so the first leg bar of a later period means every stream has delivered
        # all of the pending one. Periods are set by the legs, whatever the cadence of the rate series.
        if (roles & _LEGS) != 0 and (self._period is None or candle.OpenTime > self._period):
            if self._period is not None:
                self._decide(self._period)

            self._period = candle.OpenTime

        if (roles & _FUNDING_RATE) != 0:
            self._funding_bar = candle

        if (roles & _LENDING_RATE) != 0:
            self._lending_bar = candle

        if (roles & _DERIVATIVE_LEG) != 0:
            self._derivative_bar = candle

        if (roles & _ON_CHAIN_LEG) != 0:
            self._on_chain_bar = candle

    def _decide(self, period):
        # No spread exists until both rate series have printed.
        if self._funding_bar is None or self._lending_bar is None:
            return

        # Closing needs only the right to reduce positions; nothing is decided while an order is still working.
        if not self.IsFormedAndOnlineAndAllowTrading(StrategyTradingModes.ReducePositionOnly) or self._has_working_orders():
            return

        spread = self._funding_bar.ClosePrice - self._lending_bar.ClosePrice
        is_liquid = _traded_value(self._derivative_bar, self.Security, period) >= self.MinLegTurnover \
            and _traded_value(self._on_chain_bar, self.OnChainLegSecurity, period) >= self.MinLegTurnover

        derivative_position = self._position_of(self.Security)
        on_chain_position = self._position_of(self.OnChainLegSecurity)

        if derivative_position == Decimal.Zero and on_chain_position == Decimal.Zero:
            opens = is_liquid and Math.Abs(spread) > self.EntryThreshold and Math.Abs(spread) < self.SpreadCap \
                and self.IsFormedAndOnlineAndAllowTrading()

            if opens and self._resize_legs(Math.Sign(spread), Decimal.Zero, Decimal.Zero):
                self._bars_since_sizing = 0

            return

        # +1 while the derivative leg lends (long) and the on-chain leg borrows (short), -1 the other way round.
        lending_side = Math.Sign(derivative_position) if derivative_position != Decimal.Zero else -Math.Sign(on_chain_position)

        if Decimal(lending_side) * spread <= self.ExitThreshold or Math.Abs(spread) >= self.SpreadCap or not is_liquid:
            self._flatten(self.Security, derivative_position)
            self._flatten(self.OnChainLegSecurity, on_chain_position)
            return

        # Resizing may enlarge a leg, so it waits for full trading rights.
        self._bars_since_sizing += 1

        if self._bars_since_sizing < self.RebalanceBars or not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._bars_since_sizing = 0
        self._resize_legs(lending_side, derivative_position, on_chain_position)

    def _resize_legs(self, lending_side, derivative_position, on_chain_position):
        derivative_units = self._to_units(self.Security, self._derivative_bar)
        on_chain_units = self._to_units(self.OnChainLegSecurity, self._on_chain_bar)

        # A leg that cannot be sized would leave the pair unhedged, so neither leg moves.
        if derivative_units <= Decimal.Zero or on_chain_units <= Decimal.Zero:
            return False

        side = Decimal(lending_side)
        self._move_to(self.Security, side * derivative_units, derivative_position)
        self._move_to(self.OnChainLegSecurity, -side * on_chain_units, on_chain_position)
        return True

    def _to_units(self, leg, bar):
        # Sized at the leg's last traded price, in lots of the leg's lot size.
        lot_value = Decimal.Zero if bar is None else bar.ClosePrice * _lot_size(leg)

        if lot_value <= Decimal.Zero:
            return Decimal.Zero

        units = self._round_down(self.LegNotional / lot_value, leg)
        return units if units >= self._min_volume(leg) else Decimal.Zero

    def _move_to(self, security, target, position):
        difference = target - position
        volume = self._round_down(Math.Abs(difference), security)

        if volume <= Decimal.Zero or volume < self._min_volume(security):
            return

        if difference > Decimal.Zero:
            self.BuyMarket(volume, security)
        else:
            self.SellMarket(volume, security)

    def _flatten(self, security, position):
        if position > Decimal.Zero:
            self.SellMarket(position, security)
        elif position < Decimal.Zero:
            self.BuyMarket(-position, security)

    def _position_of(self, security):
        position = self.GetPositionValue(security, self.Portfolio)
        return position if position is not None else Decimal.Zero

    def _has_working_orders(self):
        for order in self.Orders:
            if order.State != OrderStates.Done and order.State != OrderStates.Failed:
                return True

        return False

    def _roles_of(self, stream):
        roles = 0

        if self._is_same(stream, self.FundingRateSecurity):
            roles |= _FUNDING_RATE

        if self._is_same(stream, self.LendingRateSecurity):
            roles |= _LENDING_RATE

        if self._is_same(stream, self.Security):
            roles |= _DERIVATIVE_LEG

        if self._is_same(stream, self.OnChainLegSecurity):
            roles |= _ON_CHAIN_LEG

        return roles

    def _streams(self):
        streams = []

        for security in (self.FundingRateSecurity, self.LendingRateSecurity, self.Security, self.OnChainLegSecurity):
            if security is not None and not any(self._is_same(security, known) for known in streams):
                streams.append(security)

        return streams

    def _round_down(self, volume, security):
        step = security.VolumeStep

        if step is not None and step > Decimal.Zero:
            return Math.Floor(volume / step) * step

        return volume

    def _min_volume(self, security):
        return security.MinVolume if security.MinVolume is not None else Decimal.Zero

    def _is_same(self, left, right):
        return left is not None and right is not None and str(left.Id).lower() == str(right.Id).lower()

    def CreateClone(self):
        return synthetic_lending_rates_strategy()
