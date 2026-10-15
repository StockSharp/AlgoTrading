import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from System.Globalization import CultureInfo
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volatility_hft_ea_strategy(Strategy):
    def __init__(self):
        super(volatility_hft_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._minimum_bars = self.Param("MinimumBars", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Minimum Bars", "Minimum completed candles before signal evaluation", "Signal")
        self._order_volume = self.Param("OrderVolume", Decimal(1)) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Volume applied to market orders", "Trading")
        self._fast_ma_length = self.Param("FastMaLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA Length", "Period of the fast SMA", "Signal")
        self._stop_loss_pips = self.Param("StopLossPips", Decimal(15)) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
        self._ma_difference_pips = self.Param("MaDifferencePips", Decimal(15)) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Difference (pips)", "Minimum distance between price and MA", "Signal")
        self._cooldown_bars = self.Param("CooldownBars", 24) \
            .SetDisplay("Cooldown Bars", "Bars to wait after entry or exit", "Signal")

        self._pip_size = Decimal(1)
        self._previous_sma = None
        self._sma_two_bars_ago = None
        self._processed_candles = 0
        self._cooldown_left = 0
        self._entry_price = Decimal(0)
        self._stop_loss_price = None
        self._take_profit_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def OrderVolume(self):
        return self._order_volume.Value
    @property
    def MinimumBars(self):
        return self._minimum_bars.Value
    @property
    def FastMaLength(self):
        return self._fast_ma_length.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def MaDifferencePips(self):
        return self._ma_difference_pips.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(volatility_hft_ea_strategy, self).OnReseted()
        self._previous_sma = None
        self._sma_two_bars_ago = None
        self._processed_candles = 0
        self._cooldown_left = 0
        self._entry_price = Decimal(0)
        self._stop_loss_price = None
        self._take_profit_price = None

    def OnStarted2(self, time):
        super(volatility_hft_ea_strategy, self).OnStarted2(time)
        self._pip_size = self._calculate_pip_size()
        self.Volume = self.OrderVolume
        self._previous_sma = None
        self._sma_two_bars_ago = None
        self._processed_candles = 0
        self._cooldown_left = 0
        self._entry_price = Decimal(0)
        self._stop_loss_price = None
        self._take_profit_price = None
        self._sma_ind = SimpleMovingAverage()
        self._sma_ind.Length = self.FastMaLength
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma_ind, self._on_process).Start()

    def _on_process(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        sv = Decimal(float(sma_value))
        self._manage_active_position(candle)

        if self._cooldown_left > 0:
            self._cooldown_left -= 1

        if not self._sma_ind.IsFormed:
            self._update_sma_history(sv)
            self._processed_candles += 1
            return

        if self._processed_candles < self.MinimumBars:
            self._update_sma_history(sv)
            self._processed_candles += 1
            return

        threshold = Decimal.Multiply(Math.Max(self.MaDifferencePips, Decimal(10)), self._pip_size)

        if self._sma_two_bars_ago is not None and self._cooldown_left == 0:
            close = candle.ClosePrice
            distance = close - sv
            is_breakout = distance >= threshold
            is_slope_positive = (self._previous_sma is not None
                                 and self._previous_sma > self._sma_two_bars_ago
                                 and sv > self._previous_sma)
            is_bullish_bar = candle.ClosePrice > candle.OpenPrice

            if is_breakout and is_slope_positive and is_bullish_bar and self.Position == 0:
                self.Volume = self.OrderVolume
                self.BuyMarket()
                self._cooldown_left = self.CooldownBars
                self._entry_price = candle.ClosePrice
                stop_dist = Decimal.Multiply(self.StopLossPips, self._pip_size)
                self._stop_loss_price = self._entry_price - stop_dist if stop_dist > Decimal(0) else None
                self._take_profit_price = sv

        self._update_sma_history(sv)
        self._processed_candles += 1

    def _manage_active_position(self, candle):
        if self.Position == 0:
            self._entry_price = Decimal(0)
            self._stop_loss_price = None
            self._take_profit_price = None
            return

        exit_volume = Math.Abs(self.Position)

        if self.Position > 0:
            if self._take_profit_price is not None and candle.LowPrice <= self._take_profit_price:
                self.SellMarket(exit_volume)
                self._cooldown_left = self.CooldownBars
                self._entry_price = Decimal(0)
                self._stop_loss_price = None
                self._take_profit_price = None
                return
            if self._stop_loss_price is not None and candle.LowPrice <= self._stop_loss_price:
                self.SellMarket(exit_volume)
                self._cooldown_left = self.CooldownBars
                self._entry_price = Decimal(0)
                self._stop_loss_price = None
                self._take_profit_price = None
        elif self.Position < 0:
            if self._take_profit_price is not None and candle.HighPrice >= self._take_profit_price:
                self.BuyMarket(exit_volume)
                self._cooldown_left = self.CooldownBars
                self._entry_price = Decimal(0)
                self._stop_loss_price = None
                self._take_profit_price = None
                return
            if self._stop_loss_price is not None and candle.HighPrice >= self._stop_loss_price:
                self.BuyMarket(exit_volume)
                self._cooldown_left = self.CooldownBars
                self._entry_price = Decimal(0)
                self._stop_loss_price = None
                self._take_profit_price = None

    def _update_sma_history(self, sma_value):
        self._sma_two_bars_ago = self._previous_sma
        self._previous_sma = sma_value

    def _calculate_pip_size(self):
        sec = self.Security
        step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(1)
        if step <= Decimal(0):
            return Decimal(1)
        decimals = self._get_decimal_places(step)
        if decimals == 3 or decimals == 5:
            return Decimal.Multiply(step, Decimal(10))
        return step

    def _get_decimal_places(self, value):
        text = Math.Abs(value).ToString(CultureInfo.InvariantCulture)
        sep = text.IndexOf('.')
        if sep < 0:
            return 0
        return len(text) - sep - 1

    def CreateClone(self):
        return volatility_hft_ea_strategy()
