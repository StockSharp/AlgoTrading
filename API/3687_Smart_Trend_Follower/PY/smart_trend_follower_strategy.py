import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class smart_trend_follower_strategy(Strategy):

    # Signal mode constants
    CROSS_MA = 0
    TREND = 1

    def __init__(self):
        super(smart_trend_follower_strategy, self).__init__()

        self._signal_mode = self.Param("SignalMode", self.CROSS_MA) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._initial_volume = self.Param("InitialVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Initial Volume", "Starting order volume in lots", "Money Management")
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Volume Multiplier", "Martingale multiplier applied to additional entries", "Money Management")
        self._layer_distance_pips = self.Param("LayerDistancePips", 200.0) \
            .SetNotNegative() \
            .SetDisplay("Layer Distance", "Pip distance before adding another order", "Money Management")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast moving average period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 28) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow moving average period", "Indicators")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "%K lookback length", "Indicators")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "%D smoothing length", "Indicators")
        self._stochastic_slowing = self.Param("StochasticSlowing", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Slowing", "Extra smoothing for %K", "Indicators")
        self._take_profit_pips = self.Param("TakeProfitPips", 500.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit", "Target distance in pips", "Risk Management")
        self._stop_loss_pips = self.Param("StopLossPips", 0.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss", "Protective distance in pips", "Risk Management")

        self._fast_sma = None
        self._slow_sma = None
        self._stochastic = None
        self._long_entries = []
        self._short_entries = []
        self._prev_fast = None
        self._prev_slow = None
        self._pip_size = 0.0
        self._long_exit_requested = False
        self._short_exit_requested = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SignalMode(self):
        return self._signal_mode.Value

    @SignalMode.setter
    def SignalMode(self, value):
        self._signal_mode.Value = value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    @InitialVolume.setter
    def InitialVolume(self, value):
        self._initial_volume.Value = value

    @property
    def Multiplier(self):
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def LayerDistancePips(self):
        return self._layer_distance_pips.Value

    @LayerDistancePips.setter
    def LayerDistancePips(self, value):
        self._layer_distance_pips.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @StochasticKPeriod.setter
    def StochasticKPeriod(self, value):
        self._stochastic_k_period.Value = value

    @property
    def StochasticDPeriod(self):
        return self._stochastic_d_period.Value

    @StochasticDPeriod.setter
    def StochasticDPeriod(self, value):
        self._stochastic_d_period.Value = value

    @property
    def StochasticSlowing(self):
        return self._stochastic_slowing.Value

    @StochasticSlowing.setter
    def StochasticSlowing(self, value):
        self._stochastic_slowing.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    def OnReseted(self):
        super(smart_trend_follower_strategy, self).OnReseted()
        self._fast_sma = None
        self._slow_sma = None
        self._stochastic = None
        self._long_entries = []
        self._short_entries = []
        self._prev_fast = None
        self._prev_slow = None
        self._pip_size = 0.0
        self._long_exit_requested = False
        self._short_exit_requested = False

    def OnStarted2(self, time):
        super(smart_trend_follower_strategy, self).OnStarted2(time)

        self._fast_sma = SimpleMovingAverage()
        self._fast_sma.Length = max(1, self.FastPeriod)
        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = max(1, self.SlowPeriod)
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = max(1, self.StochasticKPeriod)
        self._stochastic.D.Length = max(1, self.StochasticDPeriod)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self._fast_sma, self._slow_sma, self._process_candle) \
            .Start()

        self._pip_size = self._calculate_pip_size()

    def OnOwnTradeReceived(self, trade):
        super(smart_trend_follower_strategy, self).OnOwnTradeReceived(trade)

        price = float(trade.Trade.Price)
        volume = float(trade.Trade.Volume)

        if trade.Order.Side == Sides.Buy:
            volume = self._reduce_entries(self._short_entries, volume)
            if volume > 0:
                self._long_entries.append([price, volume])
        elif trade.Order.Side == Sides.Sell:
            volume = self._reduce_entries(self._long_entries, volume)
            if volume > 0:
                self._short_entries.append([price, volume])

        if self._get_total_volume(self._long_entries) <= 0:
            self._long_entries.clear()
            self._long_exit_requested = False

        if self._get_total_volume(self._short_entries) <= 0:
            self._short_entries.clear()
            self._short_exit_requested = False

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)

        self._manage_exits(candle)

        # Signal detection
        signal = 0  # 0=None, 1=Buy, 2=Sell

        if self.SignalMode == self.CROSS_MA:
            if self._prev_fast is not None and self._prev_slow is not None:
                cross_buy = fast < slow and self._prev_slow < self._prev_fast
                cross_sell = fast > slow and self._prev_slow > self._prev_fast
                if cross_buy:
                    signal = 1
                elif cross_sell:
                    signal = 2
        else:
            bullish = float(candle.ClosePrice) > float(candle.OpenPrice)
            bearish = float(candle.ClosePrice) < float(candle.OpenPrice)
            if fast > slow and bullish:
                signal = 1
            elif fast < slow and bearish:
                signal = 2

        if signal != 0:
            self._process_signal(signal, float(candle.ClosePrice))

        self._prev_fast = fast
        self._prev_slow = slow

    def _process_signal(self, signal, reference_price):
        pip = self._pip_size if self._pip_size > 0 else 1.0

        if signal == 1:  # Buy
            short_volume = self._get_total_volume(self._short_entries)
            if short_volume > 0:
                if not self._short_exit_requested:
                    self._short_exit_requested = True
                    self.BuyMarket(float(short_volume))
                return

            long_count = len(self._long_entries)
            requested = self._calculate_requested_volume(long_count)
            volume = self._prepare_next_volume(requested)
            if volume <= 0:
                return

            if long_count == 0:
                self.BuyMarket(float(volume))
                return

            lowest = self._get_extreme_price(self._long_entries, True)
            threshold = lowest - float(self.LayerDistancePips) * pip
            if reference_price <= threshold:
                self.BuyMarket(float(volume))

        elif signal == 2:  # Sell
            long_volume = self._get_total_volume(self._long_entries)
            if long_volume > 0:
                if not self._long_exit_requested:
                    self._long_exit_requested = True
                    self.SellMarket(float(long_volume))
                return

            short_count = len(self._short_entries)
            requested = self._calculate_requested_volume(short_count)
            volume = self._prepare_next_volume(requested)
            if volume <= 0:
                return

            if short_count == 0:
                self.SellMarket(float(volume))
                return

            highest = self._get_extreme_price(self._short_entries, False)
            threshold = highest + float(self.LayerDistancePips) * pip
            if reference_price >= threshold:
                self.SellMarket(float(volume))

    def _manage_exits(self, candle):
        pip = self._pip_size if self._pip_size > 0 else 1.0

        long_volume = self._get_total_volume(self._long_entries)
        if long_volume > 0 and not self._long_exit_requested:
            average = self._get_average_price(self._long_entries)
            take_profit = average + float(self.TakeProfitPips) * pip if float(self.TakeProfitPips) > 0 else None
            stop_loss = average - float(self.StopLossPips) * pip if float(self.StopLossPips) > 0 else None

            if take_profit is not None and float(candle.HighPrice) >= take_profit:
                self._long_exit_requested = True
                self.SellMarket(float(long_volume))
                return

            if stop_loss is not None and float(candle.LowPrice) <= stop_loss:
                self._long_exit_requested = True
                self.SellMarket(float(long_volume))
                return

        short_volume = self._get_total_volume(self._short_entries)
        if short_volume > 0 and not self._short_exit_requested:
            average = self._get_average_price(self._short_entries)
            take_profit = average - float(self.TakeProfitPips) * pip if float(self.TakeProfitPips) > 0 else None
            stop_loss = average + float(self.StopLossPips) * pip if float(self.StopLossPips) > 0 else None

            if take_profit is not None and float(candle.LowPrice) <= take_profit:
                self._short_exit_requested = True
                self.BuyMarket(float(short_volume))
                return

            if stop_loss is not None and float(candle.HighPrice) >= stop_loss:
                self._short_exit_requested = True
                self.BuyMarket(float(short_volume))

    def _calculate_requested_volume(self, existing_count):
        if self.InitialVolume <= 0:
            return 0.0
        result = float(self.InitialVolume)
        if existing_count > 0 and self.Multiplier > 0:
            result *= float(self.Multiplier) ** existing_count if float(self.Multiplier) >= 1 else 1.0
        return result

    def _prepare_next_volume(self, requested):
        if requested <= 0:
            return 0.0
        return requested

    @staticmethod
    def _reduce_entries(entries, volume):
        idx = 0
        while volume > 0 and idx < len(entries):
            entry = entries[idx]
            if volume >= entry[1]:
                volume -= entry[1]
                entries.pop(idx)
            else:
                entry[1] -= volume
                volume = 0
        return volume

    @staticmethod
    def _get_total_volume(entries):
        total = 0.0
        for e in entries:
            total += e[1]
        return total

    @staticmethod
    def _get_average_price(entries):
        total_vol = 0.0
        weighted = 0.0
        for e in entries:
            weighted += e[0] * e[1]
            total_vol += e[1]
        if total_vol <= 0:
            return 0.0
        return weighted / total_vol

    @staticmethod
    def _get_extreme_price(entries, for_long):
        if len(entries) == 0:
            return 0.0
        extreme = entries[0][0]
        for i in range(1, len(entries)):
            price = entries[i][0]
            if for_long:
                if price < extreme:
                    extreme = price
            else:
                if price > extreme:
                    extreme = price
        return extreme

    def _calculate_pip_size(self):
        security = self.Security
        if security is None:
            return 0.0
        step = security.PriceStep
        if step is None or float(step) <= 0:
            return 0.0
        decimals = security.Decimals
        if decimals == 3 or decimals == 5:
            return float(step) * 10.0
        return float(step)

    def CreateClone(self):
        return smart_trend_follower_strategy()
