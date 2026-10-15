import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class crypto_analysis_strategy(Strategy):
    RSI_LENGTH = 14
    BAND_LENGTH = 20
    BAND_WIDTH = 2

    def __init__(self):
        super(crypto_analysis_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", Decimal(0.1)).SetGreaterThanZero()
        self._use_money_tp = self.Param("UseMoneyTakeProfit", False)
        self._money_tp = self.Param("MoneyTakeProfit", Decimal(100)).SetNotNegative()
        self._use_percent_tp = self.Param("UsePercentTakeProfit", False)
        self._percent_tp = self.Param("PercentTakeProfit", Decimal(1)).SetNotNegative()
        self._enable_money_trailing = self.Param("EnableMoneyTrailing", False)
        self._money_trail_target = self.Param("MoneyTrailTarget", Decimal(50)).SetNotNegative()
        self._money_trail_stop = self.Param("MoneyTrailStop", Decimal(20)).SetNotNegative()
        self._stop_loss = self.Param("StopLossPips", 50).SetNotNegative()
        self._take_profit = self.Param("TakeProfitPips", 100).SetNotNegative()
        self._trailing_stop = self.Param("TrailingStopPips", 30).SetNotNegative()
        self._use_break_even = self.Param("UseBreakEven", True)
        self._break_even_trigger = self.Param("BreakEvenTriggerPips", 30).SetNotNegative()
        self._break_even_offset = self.Param("BreakEvenOffsetPips", 2).SetNotNegative()
        self._fast_ma = self.Param("FastMaPeriod", 6).SetGreaterThanZero()
        self._slow_ma = self.Param("SlowMaPeriod", 85).SetGreaterThanZero()
        self._momentum_period = self.Param("MomentumPeriod", 14).SetGreaterThanZero()
        self._momentum_buy = self.Param("MomentumBuyThreshold", Decimal(0.3)).SetNotNegative()
        self._momentum_sell = self.Param("MomentumSellThreshold", Decimal(0.3)).SetNotNegative()
        self._macd_fast_len = self.Param("MacdFastLength", 12).SetGreaterThanZero()
        self._macd_slow_len = self.Param("MacdSlowLength", 26).SetGreaterThanZero()
        self._macd_signal_len = self.Param("MacdSignalLength", 9).SetGreaterThanZero()
        self._use_equity_stop = self.Param("UseEquityStop", False)
        self._equity_risk = self.Param("EquityRiskPercent", Decimal(10)).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._momentum_type = self.Param("MomentumCandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._macd_type = self.Param("MacdCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RSI_LENGTH
        self._primary = []
        self._momentum_closes = []
        self._momentum_deviations = []
        self._macd_fast = None
        self._macd_slow = None
        self._macd_signal = None
        self._macd_ready = False
        self._macd_count = 0

        self._entry_price = Decimal.Zero
        self._stop_price = None
        self._take_price = None
        self._best_price = Decimal.Zero
        self._initial_equity = Decimal.Zero
        self._peak_equity = Decimal.Zero
        self._money_trail_peak = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, self._momentum_type.Value), (self.Security, self._macd_type.Value)]

    def OnReseted(self):
        super(crypto_analysis_strategy, self).OnReseted()
        self._rsi.Reset()
        self._primary = []
        self._momentum_closes = []
        self._momentum_deviations = []
        self._macd_fast = self._macd_slow = self._macd_signal = None
        self._macd_ready = False
        self._macd_count = 0
        self._reset_trade()
        self._initial_equity = Decimal.Zero
        self._peak_equity = Decimal.Zero

    def OnStarted2(self, time):
        super(crypto_analysis_strategy, self).OnStarted2(time)

        self._initial_equity = self._portfolio_value(Decimal.Zero)
        self._peak_equity = self._initial_equity

        self.SubscribeCandles(self._momentum_type.Value).Bind(self._process_momentum).Start()
        self.SubscribeCandles(self._macd_type.Value).Bind(self._process_macd).Start()
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_primary).Start()

    def _process_momentum(self, candle):
        if candle.State != CandleStates.Finished:
            return

        period = self._momentum_period.Value
        self._momentum_closes.append(candle.ClosePrice)
        keep = period + 5
        if len(self._momentum_closes) > keep:
            del self._momentum_closes[:len(self._momentum_closes) - keep]

        if len(self._momentum_closes) <= period:
            return

        previous = self._momentum_closes[-1 - period]
        if previous == Decimal.Zero:
            return

        momentum = candle.ClosePrice / previous * Decimal(100)
        self._momentum_deviations.append(Math.Abs(momentum - Decimal(100)))
        if len(self._momentum_deviations) > 3:
            del self._momentum_deviations[:-3]

    def _process_macd(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        self._macd_count += 1
        self._macd_fast = self._ema(self._macd_fast, close, self._macd_fast_len.Value)
        self._macd_slow = self._ema(self._macd_slow, close, self._macd_slow_len.Value)
        main = self._macd_fast - self._macd_slow
        self._macd_signal = self._ema(self._macd_signal, main, self._macd_signal_len.Value)
        self._macd_ready = self._macd_count >= self._macd_slow_len.Value + self._macd_signal_len.Value

    def _process_primary(self, candle):
        if candle.State != CandleStates.Finished:
            return

        required = max(max(self._fast_ma.Value, self._slow_ma.Value), self.BAND_LENGTH + 1)

        self._primary.append((candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice))
        if len(self._primary) > required + 5:
            del self._primary[:len(self._primary) - required - 5]

        rsi_value = process_value(self._rsi, candle.ClosePrice, candle.ServerTime, True)

        if self.Position != 0 and self._apply_risk(candle):
            return

        if (len(self._primary) < required or not self._rsi.IsFormed or len(self._momentum_deviations) < 3 or
                not self._macd_ready or self._macd_signal is None or self._macd_fast is None or self._macd_slow is None):
            return

        previous = self._primary[-2]
        upper, lower = self._bollinger_previous()
        fast = self._lwma(self._fast_ma.Value)
        slow = self._lwma(self._slow_ma.Value)
        rsi = to_decimal(rsi_value)
        momentum = max(self._momentum_deviations)
        macd_main = self._macd_fast - self._macd_slow
        macd_signal = self._macd_signal
        fifty = Decimal(50)

        long_signal = (previous[2] <= lower and fast < slow and rsi > fifty and
                       momentum > self._momentum_buy.Value and macd_main > macd_signal)
        short_signal = (previous[1] >= upper and fast < slow and rsi < fifty and
                        momentum > self._momentum_sell.Value and macd_main < macd_signal)

        if long_signal and self.Position <= 0:
            self._enter(Sides.Buy, candle.ClosePrice)
        elif short_signal and self.Position >= 0:
            self._enter(Sides.Sell, candle.ClosePrice)

    def _enter(self, side, price):
        volume = self._order_volume.Value + Math.Abs(self.Position)
        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._entry_price = price
        pip = self._pip_size()

        if self._stop_loss.Value > 0:
            distance = Decimal(self._stop_loss.Value) * pip
            self._stop_price = price - distance if side == Sides.Buy else price + distance
        else:
            self._stop_price = None

        if self._take_profit.Value > 0:
            distance = Decimal(self._take_profit.Value) * pip
            self._take_price = price + distance if side == Sides.Buy else price - distance
        else:
            self._take_price = None

        self._best_price = price
        self._money_trail_peak = None

    def _apply_risk(self, candle):
        floating = self._floating_pnl(candle.ClosePrice)
        equity = self._portfolio_value(self._initial_equity) + floating
        if equity > self._peak_equity:
            self._peak_equity = equity

        # The levels are checked as they stood before this candle; moving them afterwards keeps
        # a move made from this candle's high or low from being hit by the same candle.
        if self._is_level_hit(candle):
            return self._flatten()

        if self._use_money_tp.Value and floating >= self._money_tp.Value:
            return self._flatten()

        if (self._use_percent_tp.Value and self._initial_equity > 0 and
                floating >= self._initial_equity * self._percent_tp.Value / Decimal(100)):
            return self._flatten()

        if self._enable_money_trailing.Value:
            # Once armed, the peak is tracked and the giveback checked even below the target.
            if self._money_trail_peak is not None:
                if floating > self._money_trail_peak:
                    self._money_trail_peak = floating
            elif floating > self._money_trail_target.Value:
                self._money_trail_peak = floating

            if self._money_trail_peak is not None and self._money_trail_peak - floating >= self._money_trail_stop.Value:
                return self._flatten()

        if (self._use_equity_stop.Value and self._peak_equity > 0 and
                (self._peak_equity - equity) / self._peak_equity * Decimal(100) > self._equity_risk.Value):
            return self._flatten()

        self._move_levels(candle)
        return False

    def _is_level_hit(self, candle):
        if self.Position > 0:
            return ((self._stop_price is not None and candle.LowPrice <= self._stop_price) or
                    (self._take_price is not None and candle.HighPrice >= self._take_price))

        return ((self._stop_price is not None and candle.HighPrice >= self._stop_price) or
                (self._take_price is not None and candle.LowPrice <= self._take_price))

    def _move_levels(self, candle):
        pip = self._pip_size()

        if self.Position > 0:
            if (self._use_break_even.Value and
                    candle.HighPrice - self._entry_price >= Decimal(self._break_even_trigger.Value) * pip):
                self._raise_stop(self._entry_price + Decimal(self._break_even_offset.Value) * pip)

            # The stop trails only a new high beyond the best price since the entry.
            if candle.HighPrice > self._best_price:
                self._best_price = candle.HighPrice

                if self._trailing_stop.Value > 0:
                    self._raise_stop(self._best_price - Decimal(self._trailing_stop.Value) * pip)
        else:
            if (self._use_break_even.Value and
                    self._entry_price - candle.LowPrice >= Decimal(self._break_even_trigger.Value) * pip):
                self._lower_stop(self._entry_price - Decimal(self._break_even_offset.Value) * pip)

            if candle.LowPrice < self._best_price:
                self._best_price = candle.LowPrice

                if self._trailing_stop.Value > 0:
                    self._lower_stop(self._best_price + Decimal(self._trailing_stop.Value) * pip)

    def _raise_stop(self, level):
        if self._stop_price is None or level > self._stop_price:
            self._stop_price = level

    def _lower_stop(self, level):
        if self._stop_price is None or level < self._stop_price:
            self._stop_price = level

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
        else:
            return False

        self._reset_trade()
        return True

    def _floating_pnl(self, price):
        if self.Position == 0 or self._entry_price == Decimal.Zero:
            return Decimal.Zero

        direction = Decimal(1) if self.Position > 0 else Decimal(-1)
        multiplier = Decimal(1)
        if self.Security is not None and self.Security.Multiplier is not None:
            multiplier = self.Security.Multiplier
        return (price - self._entry_price) * direction * Math.Abs(self.Position) * multiplier

    def _portfolio_value(self, fallback):
        portfolio = self.Portfolio
        if portfolio is not None:
            if portfolio.CurrentValue is not None:
                return portfolio.CurrentValue
            if portfolio.BeginValue is not None:
                return portfolio.BeginValue
        return fallback

    def _bollinger_previous(self):
        end = len(self._primary) - 1
        length = Decimal(self.BAND_LENGTH)

        total = Decimal.Zero
        for i in range(end - self.BAND_LENGTH, end):
            total += self._primary[i][3]

        mean = total / length
        squares = Decimal.Zero
        for i in range(end - self.BAND_LENGTH, end):
            deviation = self._primary[i][3] - mean
            squares += deviation * deviation

        std = Decimal(Math.Sqrt(Decimal.ToDouble(squares / length)))
        width = Decimal(self.BAND_WIDTH)
        return mean + width * std, mean - width * std

    def _lwma(self, period):
        start = len(self._primary) - period
        total = Decimal.Zero
        weights = Decimal.Zero
        for i in range(period):
            weight = Decimal(i + 1)
            bar = self._primary[start + i]
            typical = (bar[1] + bar[2] + bar[3]) / Decimal(3)
            total += typical * weight
            weights += weight
        return total / weights

    def _pip_size(self):
        step = Decimal.Zero
        if self.Security is not None and self.Security.PriceStep is not None:
            step = self.Security.PriceStep
        if step <= 0:
            return Decimal(0.0001)
        if step == Decimal(0.00001) or step == Decimal(0.001):
            return step * Decimal(10)
        return step

    @staticmethod
    def _ema(previous, value, period):
        if previous is None:
            return value
        alpha = Decimal(2) / (Decimal(period) + Decimal(1))
        return previous + alpha * (value - previous)

    def _reset_trade(self):
        self._entry_price = Decimal.Zero
        self._stop_price = None
        self._take_price = None
        self._best_price = Decimal.Zero
        self._money_trail_peak = None

    def CreateClone(self):
        return crypto_analysis_strategy()
