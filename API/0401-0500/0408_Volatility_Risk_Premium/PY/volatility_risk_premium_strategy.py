import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal, Func, Nullable, MidpointRounding, InvalidOperationException
from StockSharp.Messages import DataType, CandleStates, OrderStates, Sides, SecurityTypes, OptionTypes
from StockSharp.BusinessEntities import Security
from StockSharp.Algo.Derivatives import BlackScholes, DerivativesHelper
from StockSharp.Algo.Indicators import StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

SELL_COMMENT = "Sell option"
HEDGE_COMMENT = "Delta hedge"
CLOSE_HEDGE_COMMENT = "Close delta hedge"
BUY_BACK_COMMENT = "Buy back option: "


class volatility_risk_premium_strategy(Strategy):
    """
    Volatility risk premium strategy. Sells an out-of-the-money option while its Black-Scholes implied
    volatility exceeds the realized volatility of the underlying, keeps the short delta-hedged with the
    underlying on every bar and buys the option back at expiration, on a realized volatility spike or on
    a vega stop.
    """

    def __init__(self):
        super(volatility_risk_premium_strategy, self).__init__()

        self._option = self.Param[Security]("Option", None) \
            .SetDisplay("Option", "Option contract to sell; its underlying is the strategy security", "General") \
            .SetRequired()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Bars on which volatility is measured, the option is priced and the hedge is rebalanced", "General")
        self._realized_vol_period = self.Param("RealizedVolPeriod", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("Realized Vol Period", "Number of bar-to-bar log returns in the realized volatility window", "Volatility")
        self._trading_hours_per_year = self.Param("TradingHoursPerYear", 8760.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trading Hours Per Year", "Hours a year the underlying trades; realized volatility is annualized by the bars they hold (8760 round the clock, about 6240 for FX)", "Volatility")
        self._spike_ratio = self.Param("SpikeRatio", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Spike Ratio", "Realized volatility, as a multiple of its level at the sale, that counts as a spike", "Risk")
        self._vega_stop_points = self.Param("VegaStopPoints", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Vega Stop (vol points)", "Rise of implied volatility above its level at the sale that stops out the short", "Risk")
        self._dividend_yield = self.Param("DividendYield", 0.0) \
            .SetDisplay("Dividend Yield", "Annual dividend yield of the underlying, or the foreign rate for FX options", "Option Model")

        self._clear_state()

    @property
    def Option(self):
        return self._option.Value

    @Option.setter
    def Option(self, value):
        self._option.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RealizedVolPeriod(self):
        return self._realized_vol_period.Value

    @RealizedVolPeriod.setter
    def RealizedVolPeriod(self, value):
        self._realized_vol_period.Value = value

    @property
    def TradingHoursPerYear(self):
        return self._trading_hours_per_year.Value

    @TradingHoursPerYear.setter
    def TradingHoursPerYear(self, value):
        self._trading_hours_per_year.Value = value

    @property
    def SpikeRatio(self):
        return self._spike_ratio.Value

    @SpikeRatio.setter
    def SpikeRatio(self, value):
        self._spike_ratio.Value = value

    @property
    def VegaStopPoints(self):
        return self._vega_stop_points.Value

    @VegaStopPoints.setter
    def VegaStopPoints(self, value):
        self._vega_stop_points.Value = value

    @property
    def DividendYield(self):
        return self._dividend_yield.Value

    @DividendYield.setter
    def DividendYield(self, value):
        self._dividend_yield.Value = value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.CandleType))
        if self.Option is not None:
            result.append((self.Option, self.CandleType))
        return result

    def OnReseted(self):
        super(volatility_risk_premium_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        # Reject an incomplete contract before any subscription starts.
        self._validate_option()

        super(volatility_risk_premium_strategy, self).OnStarted2(time)
        self._clear_state()

        self._time_frame = self.CandleType.Arg
        # Variance per bar times the bars traded in a year; time to expiry stays in calendar time, as the option model needs.
        self._annualization = Decimal(Math.Sqrt(float(self.TradingHoursPerYear) * float(TimeSpan.TicksPerHour) / float(self._time_frame.Ticks)))
        self._model = BlackScholes(self.Option, self.Security, self, self.Option.ExpiryDate)
        # The annual risk-free rate every strategy carries discounts the strike.
        self._model.RiskFree = self.RiskFreeRate
        self._model.Dividend = Decimal(self.DividendYield)
        self._return_deviation = StandardDeviation()
        self._return_deviation.Length = self.RealizedVolPeriod
        self.Indicators.Add(self._return_deviation)

        underlying = self.SubscribeCandles(self.CandleType)
        underlying.Bind(self._process_underlying_candle).Start()

        # The second positional argument is isFinishedOnly, not the instrument.
        self.SubscribeCandles(self.CandleType, security=self.Option) \
            .Bind(self._process_option_candle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, underlying)
            self.DrawOwnTrades(area)

    def _validate_option(self):
        option = self.Option

        if self.Security is None or option is None or str(option.Id).lower() == str(self.Security.Id).lower():
            raise InvalidOperationException("Option must be an option contract distinct from the strategy security.")

        if option.Type != SecurityTypes.Option or option.OptionType is None or option.Strike is None or option.Strike <= 0 or option.ExpiryDate is None:
            raise InvalidOperationException("Option must be an option contract with its type, a positive strike and an expiry.")

        if option.UnderlyingSecurityId is None or str(option.UnderlyingSecurityId).lower() != str(self.Security.Id).lower():
            raise InvalidOperationException("Option must be written on the strategy security, which measures realized volatility and carries the hedge.")

        frame = self.CandleType.Arg
        if not self.CandleType.IsTFCandles or not isinstance(frame, TimeSpan) or frame <= TimeSpan.Zero:
            raise InvalidOperationException("CandleType must be a time-frame candle type for the Option bars.")

    def _clear_state(self):
        self._model = None
        self._return_deviation = None
        self._time_frame = TimeSpan.Zero
        self._annualization = Decimal.Zero
        self._previous_close = None
        self._realized_vol = None
        self._underlying_time = None
        self._underlying_close = Decimal.Zero
        self._option_time = None
        self._option_close = Decimal.Zero
        self._matched_time = None
        self._buy_back_time = None
        self._last_implied_vol = None
        self._entry_implied_vol = Decimal.Zero
        self._entry_realized_vol = Decimal.Zero

    def _process_underlying_candle(self, candle):
        if candle.State != CandleStates.Finished or (self._underlying_time is not None and candle.OpenTime <= self._underlying_time):
            return

        close = candle.ClosePrice

        if self._previous_close is not None and self._previous_close > 0 and close > 0:
            log_return = Decimal(Math.Log(Decimal.ToDouble(close / self._previous_close)))
            indicator_input = DecimalIndicatorValue(self._return_deviation, log_return, candle.OpenTime)
            indicator_input.IsFinal = True
            deviation = self._return_deviation.Process(indicator_input).GetValue[Decimal](None)
            self._realized_vol = deviation * self._annualization if self._return_deviation.IsFormed else None

        self._previous_close = close
        self._underlying_time = candle.OpenTime
        self._underlying_close = close

        self._process_underlying_bar()
        self._process_matched_bar()

    def _process_option_candle(self, candle):
        if candle.State != CandleStates.Finished or (self._option_time is not None and candle.OpenTime <= self._option_time):
            return

        self._option_time = candle.OpenTime
        self._option_close = candle.ClosePrice
        self._process_matched_bar()

    # Expiration, the spike exit and the hedge need no option price, so they never wait for the option to trade.
    # A hedge left without its option no longer offsets anything and is closed.
    def _process_underlying_bar(self):
        realized_vol = self._realized_vol
        if realized_vol is None or not self.IsFormedAndOnlineAndAllowTrading() or self._has_active_orders():
            return

        option_position = self._option_position()

        if option_position < 0:
            self._manage_short(self._underlying_time, realized_vol, -option_position)
        elif option_position == 0:
            self._close_hedge()

    def _manage_short(self, open_time, realized_vol, short_contracts):
        time = open_time + self._time_frame

        # The last bar that closes before expiration is the last chance to buy the option back.
        if self.Option.ExpiryDate - time <= self._time_frame:
            self._buy_back(open_time, short_contracts, "expiration")
        elif realized_vol >= Decimal(self.SpikeRatio) * self._entry_realized_vol:
            self._buy_back(open_time, short_contracts, "volatility spike")
        elif self._last_implied_vol is not None:
            self._rebalance(time, self._last_implied_vol, short_contracts)

    # The sale and the vega stop price the option, so they need its close from the same bar as the underlying's.
    def _process_matched_bar(self):
        open_time = self._underlying_time
        if open_time is None or self._option_time != open_time or self._matched_time == open_time:
            return

        self._matched_time = open_time

        time = open_time + self._time_frame
        remaining = self.Option.ExpiryDate - time
        realized_vol = self._realized_vol

        if realized_vol is None or remaining <= TimeSpan.Zero:
            return

        implied_vol = self._implied_volatility(time)
        if implied_vol is None:
            return

        self._last_implied_vol = implied_vol

        # One option trade per bar: a bar that bought the option back does not sell it again.
        if self._buy_back_time == open_time or not self.IsFormedAndOnlineAndAllowTrading() or self._has_active_orders():
            return

        option_position = self._option_position()

        if option_position < 0 and implied_vol - self._entry_implied_vol >= Decimal(self.VegaStopPoints) / Decimal(100):
            self._buy_back(open_time, -option_position, "vega stop")
        elif option_position == 0 and self.Position == 0:
            self._try_sell(time, remaining, realized_vol, implied_vol)

    def _try_sell(self, time, remaining, realized_vol, implied_vol):
        strike = self.Option.Strike
        if self.Option.OptionType == OptionTypes.Call:
            is_out_of_the_money = strike > self._underlying_close
        else:
            is_out_of_the_money = strike < self._underlying_close

        if implied_vol <= realized_vol or not is_out_of_the_money or remaining <= self._time_frame:
            return

        self.LogInfo("Selling {0} option(s): IV={1} above RV={2}.".format(self.Volume, implied_vol, realized_vol))

        self._entry_implied_vol = implied_vol
        self._entry_realized_vol = realized_vol
        self._send_order(Sides.Sell, self.Volume, self.Option, SELL_COMMENT)
        self._rebalance(time, implied_vol, self.Volume)

    def _buy_back(self, open_time, short_contracts, reason):
        self.LogInfo("Buying back {0} option(s) on {1}: IV={2}, RV={3}.".format(short_contracts, reason, self._last_implied_vol, self._realized_vol))

        self._buy_back_time = open_time
        self._send_order(Sides.Buy, short_contracts, self.Option, BUY_BACK_COMMENT + reason)
        self._close_hedge()

    def _rebalance(self, time, implied_vol, short_contracts):
        delta = self._model.Delta(time, implied_vol, self._underlying_close)
        if delta is None:
            return

        contract_size = self.Option.Multiplier
        multiplier = contract_size if contract_size is not None and contract_size > 0 else Decimal.One
        # Holding delta units of the underlying per unit of the short option neutralizes it: long for a call, short for a put.
        target = short_contracts * multiplier * delta
        step = self.Security.VolumeStep if self.Security.VolumeStep is not None else Decimal.Zero

        if step > 0:
            target = Math.Round(target / step, MidpointRounding.AwayFromZero) * step

        change = target - self.Position

        if change != 0:
            self._send_order(Sides.Buy if change > 0 else Sides.Sell, Math.Abs(change), self.Security, HEDGE_COMMENT)

    def _close_hedge(self):
        position = self.Position
        if position != 0:
            self._send_order(Sides.Sell if position > 0 else Sides.Buy, Math.Abs(position), self.Security, CLOSE_HEDGE_COMMENT)

    def _implied_volatility(self, time):
        model = self._model
        asset_price = self._underlying_close
        premium = Func[Decimal, Nullable[Decimal]](lambda deviation: model.Premium(time, deviation, asset_price))
        percent = DerivativesHelper.ImpliedVolatility(self._option_close, premium)
        return None if percent is None else percent / Decimal(100)

    def _option_position(self):
        position = self.GetPositionValue(self.Option, self.Portfolio)
        return position if position is not None else Decimal.Zero

    def _has_active_orders(self):
        for order in self.Orders:
            if order.State != OrderStates.Done and order.State != OrderStates.Failed:
                return True
        return False

    def _send_order(self, side, volume, security, comment):
        order = self.CreateOrder(side, Decimal.Zero, volume, security)
        order.Comment = comment
        self.RegisterOrder(order)

    def CreateClone(self):
        return volatility_risk_premium_strategy()
