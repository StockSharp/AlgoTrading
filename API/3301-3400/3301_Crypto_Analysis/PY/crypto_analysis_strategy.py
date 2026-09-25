import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class crypto_analysis_strategy(Strategy):
    def __init__(self):
        super(crypto_analysis_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero()
        self._use_money_tp = self.Param("UseMoneyTakeProfit", False)
        self._money_tp = self.Param("MoneyTakeProfit", 100.0).SetNotNegative()
        self._use_percent_tp = self.Param("UsePercentTakeProfit", False)
        self._percent_tp = self.Param("PercentTakeProfit", 1.0).SetNotNegative()
        self._enable_money_trailing = self.Param("EnableMoneyTrailing", False)
        self._money_trail_target = self.Param("MoneyTrailTarget", 50.0).SetNotNegative()
        self._money_trail_stop = self.Param("MoneyTrailStop", 20.0).SetNotNegative()
        self._stop_loss = self.Param("StopLossPips", 50).SetNotNegative()
        self._take_profit = self.Param("TakeProfitPips", 100).SetNotNegative()
        self._trailing_stop = self.Param("TrailingStopPips", 30).SetNotNegative()
        self._use_break_even = self.Param("UseBreakEven", True)
        self._break_even_trigger = self.Param("BreakEvenTriggerPips", 30).SetNotNegative()
        self._break_even_offset = self.Param("BreakEvenOffsetPips", 2).SetNotNegative()
        self._fast_ma = self.Param("FastMaPeriod", 6).SetGreaterThanZero()
        self._slow_ma = self.Param("SlowMaPeriod", 85).SetGreaterThanZero()
        self._momentum_period = self.Param("MomentumPeriod", 14).SetGreaterThanZero()
        self._momentum_buy = self.Param("MomentumBuyThreshold", 0.3).SetNotNegative()
        self._momentum_sell = self.Param("MomentumSellThreshold", 0.3).SetNotNegative()
        self._macd_fast_len = self.Param("MacdFastLength", 12).SetGreaterThanZero()
        self._macd_slow_len = self.Param("MacdSlowLength", 26).SetGreaterThanZero()
        self._macd_signal_len = self.Param("MacdSignalLength", 9).SetGreaterThanZero()
        self._use_equity_stop = self.Param("UseEquityStop", False)
        self._equity_risk = self.Param("EquityRiskPercent", 10.0).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._momentum_type = self.Param("MomentumCandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._macd_type = self.Param("MacdCandleType", DataType.TimeFrame(TimeSpan.FromDays(30)))

        self._primary = []
        self._momentum_closes = []
        self._momentum_deviations = []
        self._macd_fast = None
        self._macd_slow = None
        self._macd_signal = None
        self._macd_count = 0

        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None
        self._initial_equity = 0.0
        self._peak_equity = 0.0
        self._money_trail_peak = None
        self._entry_candle_time = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, self._momentum_type.Value), (self.Security, self._macd_type.Value)]

    def OnReseted(self):
        super(crypto_analysis_strategy, self).OnReseted()
        self._primary = []
        self._momentum_closes = []
        self._momentum_deviations = []
        self._macd_fast = self._macd_slow = self._macd_signal = None
        self._macd_count = 0
        self._reset_trade()
        self._initial_equity = 0.0
        self._peak_equity = 0.0

    def OnStarted2(self, time):
        super(crypto_analysis_strategy, self).OnStarted2(time)
        if self.Portfolio is not None:
            value = self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else self.Portfolio.BeginValue
            self._initial_equity = float(value) if value is not None else 0.0
        self._peak_equity = self._initial_equity

        self.SubscribeCandles(self._momentum_type.Value).Bind(self._process_momentum).Start()
        self.SubscribeCandles(self._macd_type.Value).Bind(self._process_macd).Start()
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_primary).Start()

    def _process_momentum(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._momentum_closes.append(float(candle.ClosePrice))
        period = int(self._momentum_period.Value)
        if len(self._momentum_closes) > period + 5:
            del self._momentum_closes[:len(self._momentum_closes) - period - 5]
        if len(self._momentum_closes) <= period:
            return
        previous = self._momentum_closes[-1-period]
        if previous == 0:
            return
        deviation = abs(self._momentum_closes[-1] / previous * 100.0 - 100.0)
        self._momentum_deviations.append(deviation)
        if len(self._momentum_deviations) > 3:
            del self._momentum_deviations[:-3]

    def _process_macd(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        self._macd_count += 1
        self._macd_fast = self._ema(self._macd_fast, close, int(self._macd_fast_len.Value))
        self._macd_slow = self._ema(self._macd_slow, close, int(self._macd_slow_len.Value))
        main = self._macd_fast - self._macd_slow
        self._macd_signal = self._ema(self._macd_signal, main, int(self._macd_signal_len.Value))

    def _process_primary(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._primary.append((float(candle.OpenPrice), float(candle.HighPrice), float(candle.LowPrice), float(candle.ClosePrice)))
        keep = max(int(self._slow_ma.Value) + 5, 30)
        if len(self._primary) > keep:
            del self._primary[:-keep]

        if self.Position != 0 and self._apply_risk(candle):
            return

        if (len(self._primary) < max(int(self._slow_ma.Value), 21) or len(self._momentum_deviations) < 3 or
                self._macd_signal is None or self._macd_count < int(self._macd_slow_len.Value) + int(self._macd_signal_len.Value)):
            return

        previous = self._primary[-2]
        upper, lower = self._bollinger_previous(20, 2.0)
        fast = self._lwma(int(self._fast_ma.Value))
        slow = self._lwma(int(self._slow_ma.Value))
        rsi = self._rsi(14)
        momentum = max(self._momentum_deviations)
        main = self._macd_fast - self._macd_slow
        signal = self._macd_signal

        long_signal = (previous[2] <= lower and fast < slow and rsi > 50.0 and
                       momentum >= float(self._momentum_buy.Value) and main > signal)
        short_signal = (previous[1] >= upper and fast < slow and rsi < 50.0 and
                        momentum >= float(self._momentum_sell.Value) and main < signal)

        if long_signal and self.Position <= 0:
            self._enter(Sides.Buy, float(candle.ClosePrice), candle.OpenTime)
        elif short_signal and self.Position >= 0:
            self._enter(Sides.Sell, float(candle.ClosePrice), candle.OpenTime)

    def _enter(self, side, price, candle_time):
        volume = float(self._order_volume.Value) + abs(float(self.Position))
        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._entry_price = price
        self._entry_candle_time = candle_time
        pip = self._pip_size()
        sl = int(self._stop_loss.Value) * pip
        tp = int(self._take_profit.Value) * pip
        self._stop_price = (price - sl if side == Sides.Buy else price + sl) if sl > 0 else None
        self._take_price = (price + tp if side == Sides.Buy else price - tp) if tp > 0 else None
        self._best_price = price
        self._money_trail_peak = None

    def _apply_risk(self, candle):
        if self._entry_candle_time is not None and candle.OpenTime <= self._entry_candle_time:
            return False

        close = float(candle.ClosePrice)
        floating = self._floating_pnl(close)
        base = self._initial_equity
        if self.Portfolio is not None:
            value = self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else self.Portfolio.BeginValue
            if value is not None:
                base = float(value)
        equity = base + floating
        self._peak_equity = max(self._peak_equity, equity)

        if bool(self._use_money_tp.Value) and floating >= float(self._money_tp.Value):
            return self._flatten()
        if bool(self._use_percent_tp.Value) and self._initial_equity > 0 and floating >= self._initial_equity * float(self._percent_tp.Value) / 100.0:
            return self._flatten()

        if bool(self._enable_money_trailing.Value) and floating >= float(self._money_trail_target.Value):
            self._money_trail_peak = floating if self._money_trail_peak is None else max(self._money_trail_peak, floating)
            if self._money_trail_peak - floating >= float(self._money_trail_stop.Value):
                return self._flatten()

        if bool(self._use_equity_stop.Value) and self._peak_equity > 0:
            dd = (self._peak_equity - equity) / self._peak_equity * 100.0
            if dd >= float(self._equity_risk.Value):
                return self._flatten()

        pip = self._pip_size()
        if self.Position > 0:
            high = float(candle.HighPrice)
            self._best_price = high if self._best_price is None else max(self._best_price, high)

            if bool(self._use_break_even.Value) and high - self._entry_price >= int(self._break_even_trigger.Value) * pip:
                candidate = self._entry_price + int(self._break_even_offset.Value) * pip
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = candidate

            if int(self._trailing_stop.Value) > 0:
                candidate = self._best_price - int(self._trailing_stop.Value) * pip
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.LowPrice) <= self._stop_price) or
                    (self._take_price is not None and high >= self._take_price)):
                return self._flatten()
        else:
            low = float(candle.LowPrice)
            self._best_price = low if self._best_price is None else min(self._best_price, low)

            if bool(self._use_break_even.Value) and self._entry_price - low >= int(self._break_even_trigger.Value) * pip:
                candidate = self._entry_price - int(self._break_even_offset.Value) * pip
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = candidate

            if int(self._trailing_stop.Value) > 0:
                candidate = self._best_price + int(self._trailing_stop.Value) * pip
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.HighPrice) >= self._stop_price) or
                    (self._take_price is not None and low <= self._take_price)):
                return self._flatten()

        return False

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
        if self.Position == 0 or self._entry_price == 0:
            return 0.0
        direction = 1.0 if self.Position > 0 else -1.0
        multiplier = float(self.Security.Multiplier) if self.Security is not None and self.Security.Multiplier is not None else 1.0
        return (price - self._entry_price) * direction * abs(float(self.Position)) * multiplier

    def _bollinger_previous(self, period, deviations):
        end = len(self._primary) - 1
        values = [x[3] for x in self._primary[end-period:end]]
        mean = sum(values) / float(len(values))
        variance = sum((v - mean) ** 2 for v in values) / float(len(values))
        std = math.sqrt(variance)
        return mean + deviations * std, mean - deviations * std

    def _lwma(self, period):
        values = self._primary[-period:]
        weights = list(range(1, period + 1))
        typical = [((x[1] + x[2] + x[3]) / 3.0) for x in values]
        return sum(v * w for v, w in zip(typical, weights)) / float(sum(weights))

    def _rsi(self, period):
        gains = losses = 0.0
        for i in range(len(self._primary) - period, len(self._primary)):
            diff = self._primary[i][3] - self._primary[i-1][3]
            if diff > 0:
                gains += diff
            else:
                losses -= diff
        if losses == 0:
            return 100.0
        rs = gains / losses
        return 100.0 - 100.0 / (1.0 + rs)

    def _pip_size(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0:
            return 0.0001
        return step * 10.0 if abs(step - 0.00001) < 1e-12 or abs(step - 0.001) < 1e-12 else step

    @staticmethod
    def _ema(previous, value, period):
        if previous is None:
            return value
        alpha = 2.0 / (period + 1.0)
        return previous + alpha * (value - previous)

    def _reset_trade(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None
        self._money_trail_peak = None
        self._entry_candle_time = None

    def CreateClone(self):
        return crypto_analysis_strategy()
