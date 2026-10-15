import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class exp_skyscraper_fix_color_aml_x2_ma_candle_mm_rec_strategy(Strategy):
    def __init__(self):
        super(exp_skyscraper_fix_color_aml_x2_ma_candle_mm_rec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._channel_length = self.Param("ChannelLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Length", "ATR channel length", "Skyscraper")
        self._channel_factor = self.Param("ChannelFactor", Decimal(0.9)) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Factor", "ATR multiplier", "Skyscraper")
        self._aml_length = self.Param("AmlLength", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("AML Length", "Adaptive smoothing length", "ColorAML")
        self._x2_fast_length = self.Param("X2FastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("X2 Fast", "Fast smoothing length", "X2MA")
        self._x2_slow_length = self.Param("X2SlowLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("X2 Slow", "Slow smoothing length", "X2MA")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars between flips", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 500) \
            .SetDisplay("Stop Loss", "Stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 900) \
            .SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")

        self._highs = []
        self._lows = []
        self._closes = []
        self._weighted_prices = []
        self._aml_series = []
        self._fast_series = []
        self._slow_series = []
        self._previous_aml = None
        self._previous_consensus = 0
        self._entry_price = None
        self._cooldown_left = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def ChannelLength(self):
        return self._channel_length.Value
    @property
    def ChannelFactor(self):
        return self._channel_factor.Value
    @property
    def AmlLength(self):
        return self._aml_length.Value
    @property
    def X2FastLength(self):
        return self._x2_fast_length.Value
    @property
    def X2SlowLength(self):
        return self._x2_slow_length.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    def OnReseted(self):
        super(exp_skyscraper_fix_color_aml_x2_ma_candle_mm_rec_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._closes = []
        self._weighted_prices = []
        self._aml_series = []
        self._fast_series = []
        self._slow_series = []
        self._previous_aml = None
        self._previous_consensus = 0
        self._entry_price = None
        self._cooldown_left = 0

    def OnStarted2(self, time):
        super(exp_skyscraper_fix_color_aml_x2_ma_candle_mm_rec_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []
        self._closes = []
        self._weighted_prices = []
        self._aml_series = []
        self._fast_series = []
        self._slow_series = []
        self._previous_aml = None
        self._previous_consensus = 0
        self._entry_price = None
        self._cooldown_left = 0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

    def _calc_ema(self, source, length, target):
        multiplier = Decimal(2) / (Decimal(length) + Decimal(1))
        if len(target) > 0:
            value = source[-1] * multiplier + target[-1] * (Decimal(1) - multiplier)
        else:
            value = source[-1]
        target.append(value)
        return value

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_left > 0:
            self._cooldown_left -= 1

        h = candle.HighPrice
        l = candle.LowPrice
        c = candle.ClosePrice
        o = candle.OpenPrice

        self._highs.append(h)
        self._lows.append(l)
        self._closes.append(c)

        weighted_price = (o + h + l + Decimal(2) * c) / Decimal(5)
        self._weighted_prices.append(weighted_price)

        fast = self._calc_ema(self._closes, self.X2FastLength, self._fast_series)
        slow = self._calc_ema(self._fast_series, self.X2SlowLength, self._slow_series)
        aml = self._calc_ema(self._weighted_prices, self.AmlLength, self._aml_series)

        if self.Position != 0 and self._entry_price is None:
            self._entry_price = c

        if self._try_exit_by_risk(candle):
            return

        req = max(self.ChannelLength, max(self.AmlLength, self.X2FastLength + self.X2SlowLength))
        if len(self._highs) <= req:
            self._previous_aml = aml
            return

        sky_signal = self._get_skyscraper_signal()
        if self._previous_aml is not None:
            if aml > self._previous_aml:
                color_aml_signal = 1
            elif aml < self._previous_aml:
                color_aml_signal = -1
            else:
                color_aml_signal = 0
        else:
            color_aml_signal = 0

        if fast > slow and c >= o:
            x2_ma_signal = 1
        elif fast < slow and c <= o:
            x2_ma_signal = -1
        else:
            x2_ma_signal = 0

        self._previous_aml = aml

        score = sky_signal + color_aml_signal + x2_ma_signal
        if score >= 2:
            consensus = 1
        elif score <= -2:
            consensus = -1
        else:
            consensus = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_consensus = consensus
            return

        if consensus == self._previous_consensus or consensus == 0 or self._cooldown_left > 0:
            self._previous_consensus = consensus
            return

        if consensus > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = None
            else:
                self.BuyMarket()
                self._entry_price = c
            self._cooldown_left = self.CooldownBars
        elif consensus < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
                self._entry_price = None
            else:
                self.SellMarket()
                self._entry_price = c
            self._cooldown_left = self.CooldownBars

        self._previous_consensus = consensus

    def _get_skyscraper_signal(self):
        length = self.ChannelLength
        if len(self._closes) < length or len(self._highs) < length or len(self._lows) < length:
            return 0

        start = len(self._closes) - length
        atr_sum = Decimal(0)
        for i in range(start, len(self._closes)):
            h = self._highs[i]
            l = self._lows[i]
            prev_c = self._closes[i - 1] if i > 0 else self._closes[i]
            tr = max(h - l, max(Math.Abs(h - prev_c), Math.Abs(l - prev_c)))
            atr_sum += tr

        atr = atr_sum / Decimal(length)
        middle = (self._highs[-1] + self._lows[-1]) / Decimal(2)
        upper = middle + atr * self.ChannelFactor
        lower = middle - atr * self.ChannelFactor
        close = self._closes[-1]

        if close > upper:
            return 1
        if close < lower:
            return -1
        return 0

    def _try_exit_by_risk(self, candle):
        if self._entry_price is None or self.Position == 0:
            return False

        sec = self.Security
        step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(1)
        if step <= Decimal(0):
            step = Decimal(1)

        stop_distance = Decimal(self.StopLossPips) * step
        take_distance = Decimal(self.TakeProfitPips) * step

        if self.Position > 0:
            if (stop_distance > Decimal(0) and candle.LowPrice <= self._entry_price - stop_distance) or \
               (take_distance > Decimal(0) and candle.HighPrice >= self._entry_price + take_distance):
                self.SellMarket(self.Position)
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
                return True
        elif self.Position < 0:
            volume = Math.Abs(self.Position)
            if (stop_distance > Decimal(0) and candle.HighPrice >= self._entry_price + stop_distance) or \
               (take_distance > Decimal(0) and candle.LowPrice <= self._entry_price - take_distance):
                self.BuyMarket(volume)
                self._entry_price = None
                self._cooldown_left = self.CooldownBars
                return True

        return False

    def CreateClone(self):
        return exp_skyscraper_fix_color_aml_x2_ma_candle_mm_rec_strategy()
