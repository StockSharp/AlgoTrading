import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import (WeightedMovingAverage, RelativeStrengthIndex,
                                        MovingAverageConvergenceDivergenceSignal, RateOfChange,
                                        StochasticK, SimpleMovingAverage)
from StockSharp.Algo.Strategies import Strategy


class _leg(object):
    def __init__(self, order, side, entry_time):
        self.order = order
        self.side = side
        self.entry_time = entry_time
        self.filled_volume = 0.0
        self.filled_value = 0.0
        self.fill_price = None
        self.best_price = 0.0
        self.stop_price = None
        self.take_price = None


class risk_reward_ratio_strategy(Strategy):
    def __init__(self):
        super(risk_reward_ratio_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._fast_ma = self.Param("FastMaPeriod", 6).SetGreaterThanZero()
        self._slow_ma = self.Param("SlowMaPeriod", 85).SetGreaterThanZero()
        self._momentum_threshold = self.Param("MomentumThreshold", 0.3).SetNotNegative()
        self._reward_ratio = self.Param("RewardRatio", 2.0).SetGreaterThanZero()
        self._stop_loss = self.Param("StopLossPips", 20).SetGreaterThanZero()
        self._max_positions = self.Param("MaxPositions", 10).SetGreaterThanZero()
        self._enable_trailing = self.Param("EnableTrailing", True)
        self._trailing_stop = self.Param("TrailingStopPips", 40).SetNotNegative()
        self._enable_break_even = self.Param("EnableBreakEven", True)
        self._break_even_trigger = self.Param("BreakEvenTriggerPips", 30).SetNotNegative()
        self._break_even_offset = self.Param("BreakEvenOffsetPips", 30).SetNotNegative()
        self._exit_switch = self.Param("ExitSwitch", False)

        self._legs = []
        self._momentum_distances = []

        self._fast_lwma = None
        self._slow_lwma = None
        self._rsi = None
        self._macd = None
        self._momentum = None
        self._fast_stochastic_k = None
        self._slow_stochastic_k = None
        self._fast_stochastic_main = None
        self._slow_stochastic_main = None
        self._slow_stochastic_signal = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(risk_reward_ratio_strategy, self).OnReseted()
        self._legs = []
        self._momentum_distances = []

    def OnStarted2(self, time):
        super(risk_reward_ratio_strategy, self).OnStarted2(time)

        self._fast_lwma = WeightedMovingAverage()
        self._fast_lwma.Length = int(self._fast_ma.Value)
        self._slow_lwma = WeightedMovingAverage()
        self._slow_lwma.Length = int(self._slow_ma.Value)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = 12
        self._macd.Macd.LongMa.Length = 26
        self._macd.SignalMa.Length = 9
        self._momentum = RateOfChange()
        self._momentum.Length = 14
        self._fast_stochastic_k = StochasticK()
        self._fast_stochastic_k.Length = 5
        self._slow_stochastic_k = StochasticK()
        self._slow_stochastic_k.Length = 21
        self._fast_stochastic_main = SimpleMovingAverage()
        self._fast_stochastic_main.Length = 2
        self._slow_stochastic_main = SimpleMovingAverage()
        self._slow_stochastic_main.Length = 4
        self._slow_stochastic_signal = SimpleMovingAverage()
        self._slow_stochastic_signal.Length = 10

        self.SubscribeCandles(self._candle_type.Value).BindEx(
            self._fast_lwma, self._slow_lwma, self._rsi, self._macd, self._momentum,
            self._fast_stochastic_k, self._slow_stochastic_k, self._process, True).Start()

    def OnOwnTradeReceived(self, trade):
        super(risk_reward_ratio_strategy, self).OnOwnTradeReceived(trade)

        if trade is None or trade.Order is None or trade.Trade is None:
            return

        for leg in self._legs:
            if leg.order == trade.Order:
                self._add_fill(leg, trade)
                break

    def _process(self, candle, fast_lwma_value, slow_lwma_value, rsi_value, macd_value, momentum_value,
                 fast_k_value, slow_k_value):
        if candle.State != CandleStates.Finished:
            return

        fast_main = self._update_fast_stochastic(fast_k_value)
        slow_signal = self._update_slow_stochastic(slow_k_value)
        self._update_momentum(momentum_value)

        if bool(self._exit_switch.Value):
            self._flatten()
            return

        if self._apply_risk(candle):
            return

        if fast_main is None or slow_signal is None:
            return

        if (not self._fast_lwma.IsFormed or not self._slow_lwma.IsFormed or not self._rsi.IsFormed
                or not self._macd.IsFormed or not self._momentum.IsFormed):
            return

        macd_main = macd_value.Macd
        macd_signal = macd_value.Signal
        if macd_main is None or macd_signal is None:
            return

        macd_main = float(macd_main)
        macd_signal = float(macd_signal)
        rsi = float(rsi_value)
        fast_lwma = float(fast_lwma_value)
        slow_lwma = float(slow_lwma_value)
        burst = max(self._momentum_distances) if self._momentum_distances else 0.0
        threshold = float(self._momentum_threshold.Value)

        long_signal = (fast_main > slow_signal and rsi > 50.0 and fast_lwma > slow_lwma and
                       macd_main > macd_signal and macd_main > 0 and burst >= threshold)
        short_signal = (fast_main < slow_signal and rsi < 50.0 and fast_lwma < slow_lwma and
                        macd_main < macd_signal and macd_main < 0 and burst >= threshold)

        volume = float(self._trade_volume.Value)
        max_exposure = int(self._max_positions.Value) * volume

        if long_signal and self.Position >= 0 and float(self.Position) + volume <= max_exposure:
            self._enter(Sides.Buy, candle.OpenTime)
        elif short_signal and self.Position <= 0 and abs(float(self.Position) - volume) <= max_exposure:
            self._enter(Sides.Sell, candle.OpenTime)

    def _update_fast_stochastic(self, fast_k_value):
        if not self._fast_stochastic_k.IsFormed or fast_k_value.IsEmpty:
            return None

        main = self._fast_stochastic_main.Process(fast_k_value)
        if not self._fast_stochastic_main.IsFormed or main.IsEmpty:
            return None
        return float(main)

    def _update_slow_stochastic(self, slow_k_value):
        if not self._slow_stochastic_k.IsFormed or slow_k_value.IsEmpty:
            return None

        main = self._slow_stochastic_main.Process(slow_k_value)
        if not self._slow_stochastic_main.IsFormed or main.IsEmpty:
            return None

        signal = self._slow_stochastic_signal.Process(main)
        if not self._slow_stochastic_signal.IsFormed or signal.IsEmpty:
            return None
        return float(signal)

    def _update_momentum(self, momentum_value):
        if not self._momentum.IsFormed or momentum_value.IsEmpty:
            return

        # |ROC| equals the momentum's distance from 100.
        self._momentum_distances.append(abs(float(momentum_value)))

        if len(self._momentum_distances) > 3:
            self._momentum_distances.pop(0)

    def _enter(self, side, candle_time):
        volume = self._trade_volume.Value
        order = self.BuyMarket(volume) if side == Sides.Buy else self.SellMarket(volume)

        leg = _leg(order, side, candle_time)
        self._legs.append(leg)

        # The emulator can fill a market order before BuyMarket or SellMarket returns it.
        for trade in list(self.MyTrades):
            if trade.Order == order and trade.Trade is not None:
                self._add_fill(leg, trade)

    def _add_fill(self, leg, trade):
        price = float(trade.Trade.TradePrice)
        volume = float(trade.Trade.TradeVolume)
        leg.filled_volume += volume
        leg.filled_value += price * volume
        leg.fill_price = leg.filled_value / leg.filled_volume
        self._set_initial_levels(leg, leg.fill_price)

    def _set_initial_levels(self, leg, fill):
        leg.best_price = fill

        pip = self._pip()
        if pip is None:
            leg.stop_price = None
            leg.take_price = None
            return

        stop_distance = int(self._stop_loss.Value) * pip
        take_distance = stop_distance * float(self._reward_ratio.Value)

        if leg.side == Sides.Buy:
            leg.stop_price = fill - stop_distance
            leg.take_price = fill + take_distance
        else:
            leg.stop_price = fill + stop_distance
            leg.take_price = fill - take_distance

    def _apply_risk(self, candle):
        pip = self._pip()
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        exit_sell = 0.0
        exit_buy = 0.0

        for i in range(len(self._legs) - 1, -1, -1):
            leg = self._legs[i]
            if leg.fill_price is None or candle.OpenTime <= leg.entry_time:
                continue

            fill = leg.fill_price
            is_long = leg.side == Sides.Buy
            leg.best_price = max(leg.best_price, high) if is_long else min(leg.best_price, low)

            if pip is not None:
                self._move_stop(leg, fill, pip)

            if is_long:
                hit = ((leg.stop_price is not None and low <= leg.stop_price) or
                       (leg.take_price is not None and high >= leg.take_price))
            else:
                hit = ((leg.stop_price is not None and high >= leg.stop_price) or
                       (leg.take_price is not None and low <= leg.take_price))

            if not hit:
                continue

            if is_long:
                exit_sell += leg.filled_volume
            else:
                exit_buy += leg.filled_volume

            del self._legs[i]

        if exit_sell > 0:
            self.SellMarket(exit_sell)

        if exit_buy > 0:
            self.BuyMarket(exit_buy)

        return exit_sell > 0 or exit_buy > 0

    def _move_stop(self, leg, fill, pip):
        is_long = leg.side == Sides.Buy
        advance = leg.best_price - fill if is_long else fill - leg.best_price

        if bool(self._enable_break_even.Value) and advance >= int(self._break_even_trigger.Value) * pip:
            offset = int(self._break_even_offset.Value) * pip
            self._tighten_stop(leg, fill + offset if is_long else fill - offset)

        if bool(self._enable_trailing.Value) and int(self._trailing_stop.Value) > 0:
            trail = int(self._trailing_stop.Value) * pip

            if advance >= trail:
                self._tighten_stop(leg, leg.best_price - trail if is_long else leg.best_price + trail)

    @staticmethod
    def _tighten_stop(leg, candidate):
        if leg.stop_price is None or (candidate > leg.stop_price if leg.side == Sides.Buy else candidate < leg.stop_price):
            leg.stop_price = candidate

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))

        self._legs = []

    def _pip(self):
        security = self.Security
        if security is None or security.PriceStep is None:
            return None

        step = float(security.PriceStep)
        return step if step > 0 else None

    def CreateClone(self):
        return risk_reward_ratio_strategy()
