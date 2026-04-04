import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *
from StockSharp.Algo.Indicators import DecimalIndicatorValue


class vr_zver_v2_strategy(Strategy):
    """Port of the VR-ZVER v2 expert advisor with triple EMA confirmation and stochastic/RSI filters."""

    def __init__(self):
        super(vr_zver_v2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Time frame for signal generation", "General")

        self._fixed_volume = self.Param("FixedVolume", Decimal(1)) \
            .SetDisplay("Fixed Volume", "Use fixed volume when greater than zero", "Risk")
        self._risk_percent = self.Param("RiskPercent", Decimal(10)) \
            .SetDisplay("Risk %", "Risk percentage used when fixed volume is zero", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", Decimal(10000)) \
            .SetDisplay("Stop Loss (pips)", "Full stop distance expressed in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", Decimal(15000)) \
            .SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", Decimal(8000)) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", Decimal(3000)) \
            .SetDisplay("Trailing Step (pips)", "Additional distance before trailing updates", "Risk")
        self._breakeven_pips = self.Param("BreakevenPips", Decimal(5000)) \
            .SetDisplay("Breakeven (pips)", "Move stop to entry after this profit", "Risk")

        self._allow_longs = self.Param("AllowLongs", True) \
            .SetDisplay("Allow Longs", "Permit buy trades", "General")
        self._allow_shorts = self.Param("AllowShorts", True) \
            .SetDisplay("Allow Shorts", "Permit sell trades", "General")

        self._use_ma_filter = self.Param("UseMovingAverageFilter", True) \
            .SetDisplay("Use MA Filter", "Require triple EMA alignment", "Indicators")
        self._fast_period = self.Param("FastMaPeriod", 3).SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Length of the fast EMA", "Indicators")
        self._slow_period = self.Param("SlowMaPeriod", 5).SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Length of the slow EMA", "Indicators")
        self._very_slow_period = self.Param("VerySlowMaPeriod", 7).SetGreaterThanZero() \
            .SetDisplay("Very Slow EMA", "Length of the very slow EMA", "Indicators")

        self._use_stochastic = self.Param("UseStochastic", False) \
            .SetDisplay("Use Stochastic", "Enable stochastic confirmation", "Indicators")
        self._stoch_k_period = self.Param("StochasticKPeriod", 42).SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "Number of periods for %K", "Indicators")
        self._stoch_d_period = self.Param("StochasticDPeriod", 5).SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "Smoothing period for %D", "Indicators")
        self._stoch_smooth = self.Param("StochasticSmooth", 7).SetGreaterThanZero() \
            .SetDisplay("Stochastic Smooth", "Final smoothing for stochastic", "Indicators")
        self._stoch_upper = self.Param("StochasticUpperLevel", Decimal(60)) \
            .SetDisplay("Stochastic Upper", "Upper threshold for short signals", "Indicators")
        self._stoch_lower = self.Param("StochasticLowerLevel", Decimal(40)) \
            .SetDisplay("Stochastic Lower", "Lower threshold for long signals", "Indicators")

        self._use_rsi = self.Param("UseRsi", False) \
            .SetDisplay("Use RSI", "Enable RSI filter", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Length of the RSI", "Indicators")
        self._rsi_upper = self.Param("RsiUpperLevel", Decimal(60)) \
            .SetDisplay("RSI Upper", "Upper threshold for short entries", "Indicators")
        self._rsi_lower = self.Param("RsiLowerLevel", Decimal(40)) \
            .SetDisplay("RSI Lower", "Lower threshold for long entries", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(vr_zver_v2_strategy, self).OnReseted()
        self._pip_size = Decimal(0)
        self._fast_ma = None
        self._slow_ma = None
        self._very_slow_ma = None
        self._stochastic = None
        self._rsi = None
        self._reset_trade()

    def OnStarted2(self, time):
        super(vr_zver_v2_strategy, self).OnStarted2(time)

        self._pip_size = self._calculate_pip_size()
        self._reset_trade()

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self._fast_period.Value
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self._slow_period.Value
        self._very_slow_ma = ExponentialMovingAverage()
        self._very_slow_ma.Length = self._very_slow_period.Value

        self._stochastic = None
        if self._use_stochastic.Value:
            self._stochastic = StochasticOscillator()
            self._stochastic.K.Length = self._stoch_k_period.Value
            self._stochastic.D.Length = self._stoch_d_period.Value

        self._rsi = None
        if self._use_rsi.Value:
            self._rsi = RelativeStrengthIndex()
            self._rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._fast_ma, self._slow_ma, self._very_slow_ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawIndicator(area, self._very_slow_ma)
            if self._stochastic is not None:
                self.DrawIndicator(area, self._stochastic)
            if self._rsi is not None:
                self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, very_slow_val):
        if candle.State != CandleStates.Finished:
            return

        # Process stochastic and RSI manually only when enabled.
        stoch_value = None
        if self._use_stochastic.Value and self._stochastic is not None:
            stoch_value = self._stochastic.Process(candle)

        rsi_val = Decimal(50)
        if self._use_rsi.Value and self._rsi is not None:
            rsi_input = DecimalIndicatorValue(self._rsi, candle.ClosePrice, candle.CloseTime)
            rsi_input.IsFinal = True
            rsi_result = self._rsi.Process(rsi_input)
            if rsi_result.IsFormed:
                rsi_val = rsi_result.ToDecimal()
            else:
                rsi_val = Decimal(50)

        if self._use_ma_filter.Value and (not self._fast_ma.IsFormed or not self._slow_ma.IsFormed or not self._very_slow_ma.IsFormed):
            return

        if self._use_stochastic.Value and not self._stochastic.IsFormed:
            return

        if self._use_rsi.Value and not self._rsi.IsFormed:
            return

        # Manage the active position before evaluating new signals.
        self._update_risk_management(candle)

        # Aggregate votes from all enabled filters.
        filters = 0
        up_votes = 0
        down_votes = 0

        if self._use_ma_filter.Value:
            filters += 1
            if fast_val > slow_val and slow_val > very_slow_val:
                up_votes += 1
            elif fast_val < slow_val and slow_val < very_slow_val:
                down_votes += 1

        if self._use_stochastic.Value and stoch_value is not None:
            try:
                stoch_k = stoch_value.K
                stoch_d = stoch_value.D
                if stoch_k is None or stoch_d is None:
                    return
                filters += 1
                if stoch_d < stoch_k and self._stoch_lower.Value > stoch_k:
                    up_votes += 1
                if stoch_d > stoch_k and self._stoch_upper.Value < stoch_k:
                    down_votes += 1
            except:
                return

        if self._use_rsi.Value:
            filters += 1
            if rsi_val < self._rsi_lower.Value:
                up_votes += 1
            if rsi_val > self._rsi_upper.Value:
                down_votes += 1

        if filters == 0:
            return

        long_signal = self._allow_longs.Value and up_votes == filters
        short_signal = self._allow_shorts.Value and down_votes == filters

        # Only open a new trade when there is no active position.
        if self.Position == 0:
            if long_signal:
                self._try_enter_long(candle)
            elif short_signal:
                self._try_enter_short(candle)

    def _try_enter_long(self, candle):
        volume = self._calculate_entry_volume()
        if volume <= 0:
            return

        self.BuyMarket(volume)

        self._entry_price = candle.ClosePrice
        self._be_activated = False
        self._trail_stop = None

        stop_offset = Decimal.Multiply(self._stop_loss_pips.Value, self._pip_size) if self._stop_loss_pips.Value > 0 else Decimal(0)
        take_offset = Decimal.Multiply(self._take_profit_pips.Value, self._pip_size) if self._take_profit_pips.Value > 0 else Decimal(0)

        self._stop_price = Decimal.Subtract(self._entry_price, stop_offset) if stop_offset > 0 else None
        self._take_price = Decimal.Add(self._entry_price, take_offset) if take_offset > 0 else None

    def _try_enter_short(self, candle):
        volume = self._calculate_entry_volume()
        if volume <= 0:
            return

        self.SellMarket(volume)

        self._entry_price = candle.ClosePrice
        self._be_activated = False
        self._trail_stop = None

        stop_offset = Decimal.Multiply(self._stop_loss_pips.Value, self._pip_size) if self._stop_loss_pips.Value > 0 else Decimal(0)
        take_offset = Decimal.Multiply(self._take_profit_pips.Value, self._pip_size) if self._take_profit_pips.Value > 0 else Decimal(0)

        self._stop_price = Decimal.Add(self._entry_price, stop_offset) if stop_offset > 0 else None
        self._take_price = Decimal.Subtract(self._entry_price, take_offset) if take_offset > 0 else None

    def _update_risk_management(self, candle):
        if self.Position > 0:
            self._handle_breakeven_long(candle)
            self._handle_trailing_long(candle)

            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_trade()
            elif self._take_price is not None and candle.HighPrice >= self._take_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_trade()
        elif self.Position < 0:
            self._handle_breakeven_short(candle)
            self._handle_trailing_short(candle)

            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_trade()
            elif self._take_price is not None and candle.LowPrice <= self._take_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_trade()
        else:
            self._reset_trade()

    def _handle_breakeven_long(self, candle):
        if self._be_activated or self._breakeven_pips.Value <= 0:
            return
        trigger = Decimal.Add(self._entry_price, Decimal.Multiply(self._breakeven_pips.Value, self._pip_size))
        if candle.HighPrice >= trigger:
            self._be_activated = True
            self._update_long_stop(self._entry_price)

    def _handle_breakeven_short(self, candle):
        if self._be_activated or self._breakeven_pips.Value <= 0:
            return
        trigger = Decimal.Subtract(self._entry_price, Decimal.Multiply(self._breakeven_pips.Value, self._pip_size))
        if candle.LowPrice <= trigger:
            self._be_activated = True
            self._update_short_stop(self._entry_price)

    def _handle_trailing_long(self, candle):
        if self._trailing_stop_pips.Value <= 0:
            return
        distance = Decimal.Multiply(self._trailing_stop_pips.Value, self._pip_size)
        if distance <= 0:
            return
        step = Decimal.Multiply(self._trailing_step_pips.Value, self._pip_size)
        desired_stop = Decimal.Subtract(candle.ClosePrice, distance)

        if self._trail_stop is None:
            activation_price = Decimal.Add(Decimal.Add(self._entry_price, distance), step)
            if candle.HighPrice >= activation_price:
                self._trail_stop = desired_stop
                self._update_long_stop(desired_stop)
        elif desired_stop > Decimal.Add(self._trail_stop, step):
            self._trail_stop = desired_stop
            self._update_long_stop(desired_stop)

    def _handle_trailing_short(self, candle):
        if self._trailing_stop_pips.Value <= 0:
            return
        distance = Decimal.Multiply(self._trailing_stop_pips.Value, self._pip_size)
        if distance <= 0:
            return
        step = Decimal.Multiply(self._trailing_step_pips.Value, self._pip_size)
        desired_stop = Decimal.Add(candle.ClosePrice, distance)

        if self._trail_stop is None:
            activation_price = Decimal.Subtract(Decimal.Subtract(self._entry_price, distance), step)
            if candle.LowPrice <= activation_price:
                self._trail_stop = desired_stop
                self._update_short_stop(desired_stop)
        elif desired_stop < Decimal.Subtract(self._trail_stop, step):
            self._trail_stop = desired_stop
            self._update_short_stop(desired_stop)

    def _update_long_stop(self, new_level):
        if self._stop_price is None or new_level > self._stop_price:
            self._stop_price = new_level

    def _update_short_stop(self, new_level):
        if self._stop_price is None or new_level < self._stop_price:
            self._stop_price = new_level

    def _calculate_entry_volume(self):
        if self._fixed_volume.Value > 0:
            return self._adjust_volume(self._fixed_volume.Value)

        stop_offset = Decimal.Multiply(self._stop_loss_pips.Value, self._pip_size) if self._stop_loss_pips.Value > 0 else Decimal(0)
        if stop_offset <= 0:
            return self._adjust_volume(self.Volume)

        risk_volume = self._get_risk_volume(stop_offset)
        return self._adjust_volume(risk_volume)

    def _get_risk_volume(self, stop_offset):
        if stop_offset <= 0:
            return Decimal(0)

        sec = self.Security
        price_step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(0)
        step_price = Decimal(0)
        try:
            sp = self.GetSecurityValue[Decimal](Level1Fields.StepPrice)
            if sp is not None:
                step_price = sp
        except:
            step_price = Decimal(0)

        if price_step <= 0 or step_price <= 0:
            return Decimal(0)

        loss_per_unit = Decimal.Multiply(Decimal.Divide(stop_offset, price_step), step_price)
        if loss_per_unit <= 0:
            return Decimal(0)

        portfolio = self.Portfolio
        equity = portfolio.CurrentValue if portfolio is not None and portfolio.CurrentValue is not None else Decimal(0)
        if equity <= 0:
            return Decimal(0)

        risk_amount = Decimal.Divide(Decimal.Multiply(equity, self._risk_percent.Value), Decimal(100))
        if risk_amount <= 0:
            return Decimal(0)

        return Decimal.Divide(risk_amount, loss_per_unit)

    def _adjust_volume(self, volume):
        if volume <= 0:
            return Decimal(0)

        sec = self.Security
        if sec is None:
            return volume

        step = sec.VolumeStep if sec.VolumeStep is not None else Decimal(0)

        if step > 0:
            steps = Math.Floor(Decimal.Divide(volume, step))
            adjusted = Decimal.Multiply(steps, step)
            if adjusted <= 0:
                adjusted = step
            return adjusted

        return volume if volume > 0 else Decimal(0)

    def _calculate_pip_size(self):
        sec = self.Security
        step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(0)
        if step <= 0:
            return Decimal(1)
        return step

    def _reset_trade(self):
        self._entry_price = Decimal(0)
        self._stop_price = None
        self._take_price = None
        self._trail_stop = None
        self._be_activated = False

    def CreateClone(self):
        return vr_zver_v2_strategy()
